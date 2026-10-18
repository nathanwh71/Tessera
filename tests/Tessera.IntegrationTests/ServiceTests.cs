using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Modules.Tessera.Infrastructure.Seeding;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.VectorTiles.Mapbox;

namespace Tessera.IntegrationTests;

/// <summary>
/// The whole service over real HTTP: the seed loaded and granted, the <b><i>catalogue</i></b>
/// loaded, the service listening on a port of its own (US-00, US-07, S-01, S-06, US-23).
/// </summary>
public sealed class ServiceFixture : IAsyncLifetime
{
    /// <summary>
    /// The class's own database, a free port and a client aimed at it, so this class shares no rows
    /// and no port with any other class running at the same time. The client starts empty, and
    /// InitializeAsync aims a new one at the service as soon as it has started.
    /// </summary>
    public DatabaseFixture Database { get; } = new();

    public int Port { get; } = Http.FreePort();

    public HttpClient Client { get; private set; } = new();

    /// <summary>The running service; null until InitializeAsync builds it.</summary>
    private WebApplication? _app;

    /// <summary>
    /// Readies the stage in the order the release does: the database, the <b><i>seed script</i></b>
    /// as the publisher, the <b><i>grant</i></b>, then the service.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        await Database.InitializeAsync();
        SeedScripts.Run(Database.Publisher, SeedScripts.Folder).Match(_ => 0, error => throw new InvalidOperationException(error));
        await Database.GrantPlaces();

        // The same cafés in Web Mercator, for US-07 c3: a layer in each coordinate system.
        await Database.Owner("""
            CREATE SCHEMA stage;
            CREATE TABLE stage.cafes_3857 AS SELECT id, name, ST_Transform(geom, 3857)::geometry(Point, 3857) AS geom FROM food.cafes;
            GRANT USAGE ON SCHEMA stage TO tessera_reader;
            GRANT SELECT ON stage.cafes_3857 TO tessera_reader;
            """);


        // The real service, as Program.cs builds it, on a free port and the test database, so each
        // test reaches it through real HTTP (US-00 c2). It serves the built map, which the service's
        // project reference copies beside the tests (US-23 c4). The class fixes the buffer at 64, apart
        // from the shipped 256, as the other tile tests do, so its tests hold when that default changes.
        _app = TesseraHost.Build(new Dictionary<string, string?>
        {
            ["Urls"] = $"http://127.0.0.1:{Port}",
            ["DATABASE_URL"] = Database.ReaderUrl,
            ["Tiles:Buffer"] = "64",
        });
        await _app.StartAsync();
        Client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{Port}") };

        // The list of layers loads in the background (N-23); until it does, a tile answers 503.
        for (var attempt = 0; attempt < 100; attempt++)
        {
            using var answer = await Client.GetAsync("/food.cafes/0/0/0.pbf");
            if (answer.StatusCode != HttpStatusCode.ServiceUnavailable)
                return;
            await Task.Delay(100);
        }

        throw new InvalidOperationException("The list of layers did not load within ten seconds.");
    }

    /// <summary>
    /// Stops in the reverse order of the start: the client and the service first, so no request
    /// meets a database that has already gone.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
        await Database.DisposeAsync();
    }
}

/// <summary>
/// US-07, S-01, N-18, N-19, N-25 and US-23 through the running service: what a browser receives
/// for a <b><i>tile</i></b>, the map page, an unknown address and an attack. Unit tests prove each
/// rule alone; these prove the parts are wired together.
/// </summary>
public sealed class ServiceTests(ServiceFixture service) : IClassFixture<ServiceFixture>
{
    // cafes-001 lies at longitude 113.577677, latitude 22.158784. Level 15 is the map's home level
    // (ADR 0006), where a tile is about a kilometre wide.
    private const double Longitude = 113.577677;
    private const double Latitude = 22.158784;
    private const int Zoom = 15;

    /// <summary>
    /// The tile that holds a point at a zoom level: the Web Mercator formula every web map uses.
    /// x grows eastward from longitude -180; y grows southward from about latitude 85.
    /// </summary>
    private static (int X, int Y) TileOf(double longitude, double latitude, int zoom)
    {
        var size = 1 << zoom;
        var x = (int)Math.Floor((longitude + 180) / 360 * size);
        var radians = latitude * Math.PI / 180;
        var y = (int)Math.Floor((1 - (Math.Log(Math.Tan(radians) + (1 / Math.Cos(radians))) / Math.PI)) / 2 * size);
        return (x, y);
    }

    /// <summary>
    /// The tile that holds cafes-001, worked out once, so every test asks for the same one.
    /// </summary>
    private static readonly (int X, int Y) Tile = TileOf(Longitude, Latitude, Zoom);

    /// <summary>
    /// A GET through the shared client, with the test's own cancel signal, so a test stopped early
    /// stops its request too.
    /// </summary>
    private Task<HttpResponseMessage> Get(string path) => service.Client.GetAsync(path, TestContext.Current.CancellationToken);

    // US-07 c1, and N-18 c1 and test 2: a tile carries the minute the browser may keep it.
    [Fact]
    public async Task Serves_a_tile_as_a_mapbox_vector_tile()
    {
        using var answer = await Get($"/food.cafes/{Zoom}/{Tile.X}/{Tile.Y}.pbf");

        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
        Assert.Equal("application/vnd.mapbox-vector-tile", answer.Content.Headers.ContentType?.MediaType);
        Assert.Equal("public, max-age=60", answer.Headers.CacheControl?.ToString());
    }

    // N-19 c1: a tile carries a weak tag, made from its bytes, which Nginx compares (ADR 0008).
    [Fact]
    public async Task Tags_a_tile_by_its_bytes()
    {
        using var first = await Get($"/food.cafes/{Zoom}/{Tile.X}/{Tile.Y}.pbf");
        using var second = await Get($"/food.cafes/{Zoom}/{Tile.X}/{Tile.Y}.pbf");

        Assert.True(first.Headers.ETag?.IsWeak);
        Assert.Equal(first.Headers.ETag, second.Headers.ETag);
    }

    // N-19 c2: a browser that sends the tag back, with its copy still good, gets 304 and no body. The
    // answer keeps its Cache-Control, so the browser keeps the copy for another max-age.
    [Fact]
    public async Task Answers_a_matching_tag_with_304()
    {
        using var first = await Get($"/food.cafes/{Zoom}/{Tile.X}/{Tile.Y}.pbf");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/food.cafes/{Zoom}/{Tile.X}/{Tile.Y}.pbf");
        request.Headers.IfNoneMatch.Add(first.Headers.ETag!);
        using var again = await service.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotModified, again.StatusCode);
        Assert.Equal("public, max-age=60", again.Headers.CacheControl?.ToString());
        Assert.Empty(await again.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    // A watcher, such as a script that checks a tile's tag, may ask with HEAD for the headers alone
    // (ADR 0008); the answer carries the same tag as a GET's, with no body sent.
    [Fact]
    public async Task Answers_head_with_the_headers_alone()
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, $"/food.cafes/{Zoom}/{Tile.X}/{Tile.Y}.pbf");
        using var head = await service.Client.SendAsync(request, TestContext.Current.CancellationToken);
        using var get = await Get($"/food.cafes/{Zoom}/{Tile.X}/{Tile.Y}.pbf");

        Assert.Equal(HttpStatusCode.OK, head.StatusCode);
        Assert.Equal(get.Headers.ETag, head.Headers.ETag);
        Assert.Empty(await head.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    // US-07 c2.
    [Fact]
    public async Task Serves_the_same_body_for_mvt()
    {
        var pbf = await service.Client.GetByteArrayAsync($"/food.cafes/{Zoom}/{Tile.X}/{Tile.Y}.pbf", TestContext.Current.CancellationToken);
        var mvt = await service.Client.GetByteArrayAsync($"/food.cafes/{Zoom}/{Tile.X}/{Tile.Y}.mvt", TestContext.Current.CancellationToken);

        Assert.NotEmpty(pbf);
        Assert.Equal(pbf, mvt);
    }

    // US-07 c3, and US-07 test 2: the features of a WGS 84 layer and a Web Mercator layer sit at the
    // right positions, and carry their properties.
    [Theory]
    [InlineData("food.cafes")]
    [InlineData("stage.cafes_3857")]
    public async Task Places_each_feature_where_it_lies(string layer)
    {
        var bytes = await service.Client.GetByteArrayAsync($"/{layer}/{Zoom}/{Tile.X}/{Tile.Y}.pbf", TestContext.Current.CancellationToken);
        var tile = new MapboxTileReader().Read(new MemoryStream(bytes), new NetTopologySuite.IO.VectorTiles.Tiles.Tile(Tile.X, Tile.Y, Zoom));

        var cafe = tile.Layers.Single().Features.Single(feature => (string)feature.Attributes["id"] == "cafes-001");
        var point = (Point)cafe.Geometry;

        // One unit of the 4096 grid at level 15 is about 0.3 metres, or 0.000003 degrees.
        Assert.Equal(Longitude, point.X, 0.00001);
        Assert.Equal(Latitude, point.Y, 0.00001);
        Assert.False(string.IsNullOrEmpty((string)cafe.Attributes["name"]));
    }

    // US-07 c4 and c5, test 3: an unknown layer answers 404 as a problem document.
    [Fact]
    public async Task Answers_an_unknown_layer_with_a_problem_document()
    {
        using var answer = await Get("/food.zoos/0/0/0.pbf");
        using var body = JsonDocument.Parse(await answer.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.NotFound, answer.StatusCode);
        // N-18 c3: no browser keeps a problem document.
        Assert.Equal("no-store", answer.Headers.CacheControl?.ToString());
        Assert.Equal("application/problem+json", answer.Content.Headers.ContentType?.MediaType);
        Assert.Equal("about:blank", body.RootElement.GetProperty("type").GetString());
        Assert.Equal("Not Found", body.RootElement.GetProperty("title").GetString());
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
    }

    // US-23 c1, c2 and ADR 0006: the page carries the area, at / and at /{layer}.html, with the
    // browser's rules in its headers; its assets go out as Vite built them.
    [Theory]
    [InlineData("/")]
    [InlineData("/food.cafes.html")]
    public async Task Serves_the_map_page_with_the_area_written_in(string path)
    {
        using var answer = await Get(path);
        var html = await answer.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
        Assert.Equal("text/html", answer.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<script id=\"tessera-area\" type=\"application/json\">{\"name\":\"Macau\"", html, StringComparison.Ordinal);
        Assert.Contains("<div id=\"root\"></div>", html, StringComparison.Ordinal);
        Assert.Contains("default-src 'self'", answer.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
        Assert.True(answer.Headers.CacheControl?.NoCache);
        Assert.Equal("nosniff", answer.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-referrer", answer.Headers.GetValues("Referrer-Policy").Single());

        var script = System.Text.RegularExpressions.Regex.Match(html, @"src=""(?<script>/assets/[^""]+\.js)""", System.Text.RegularExpressions.RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1)).Groups["script"].Value;
        using var asset = await Get(script);
        Assert.Equal(HttpStatusCode.OK, asset.StatusCode);
        Assert.Equal("text/javascript", asset.Content.Headers.ContentType?.MediaType);
    }

    // S-06 c3 and test 3, and US-23 c7: an address no route knows answers a 404 problem document
    // too, and so does a page named for anything but one of the nine layers.
    [Theory]
    [InlineData("/nothing-here")]
    [InlineData("/index.html")]
    [InlineData("/food.zoos.html")]
    public async Task Answers_an_unknown_address_with_a_problem_document(string path)
    {
        using var answer = await Get(path);

        Assert.Equal(HttpStatusCode.NotFound, answer.StatusCode);
        Assert.Equal("application/problem+json", answer.Content.Headers.ContentType?.MediaType);
        // No detail, so a stranger learns nothing more.
        Assert.DoesNotContain("detail", await answer.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    // Level 1 has two columns, 0 and 1, so column 5 names no tile. The service refuses it with 404
    // before any query runs, and says so as a problem document (US-07 c5), as for an unknown layer.
    [Fact]
    public async Task Answers_a_tile_outside_the_grid_with_404()
    {
        using var answer = await Get("/food.cafes/1/5/0.pbf");

        Assert.Equal(HttpStatusCode.NotFound, answer.StatusCode);
        Assert.Equal("application/problem+json", answer.Content.Headers.ContentType?.MediaType);
    }

    // S-01 c4 and test 3: an attack changes nothing.
    [Theory]
    [InlineData("x;%20DROP%20TABLE%20cafes")]
    [InlineData("food.cafes'%3B%20--")]
    public async Task Answers_an_attack_with_404_and_keeps_every_row(string layer)
    {
        using var answer = await Get($"/{layer}/0/0/0.pbf");

        Assert.Equal(HttpStatusCode.NotFound, answer.StatusCode);
        Assert.Equal(100L, await service.Database.OwnerValue<long>("SELECT count(*) FROM food.cafes"));
    }

    // S-06 c3, SR-11: '%2e' is a dot in disguise. Kestrel decodes it and drops the folder before
    // '..', so '/x/%2e%2e/food.cafes/0/0/0.pbf' once reached the tile route and answered 200. The
    // service now reads the address as it was sent and refuses one with a dot folder in disguise,
    // in any mix of cases, before it picks a route. Nginx refuses it first (edge.ts); this covers a
    // copy reached directly.
    [Theory]
    [InlineData("/x/%2e%2e/food.cafes/0/0/0.pbf")]
    [InlineData("/x/%2E%2e/food.cafes/0/0/0.pbf")]
    [InlineData("/x/.%2E/food.cafes/0/0/0.pbf")]
    [InlineData("/food.cafes/%2e/0/0/0.pbf")]
    [InlineData("/x/%2e%2e/health")]
    public async Task Refuses_a_dot_folder_in_disguise_with_400(string target)
    {
        var answer = await Http.Raw(service.Port, target, TestContext.Current.CancellationToken);
        var body = answer[(answer.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4)..];

        Assert.StartsWith("HTTP/1.1 400 ", answer, StringComparison.Ordinal);
        Assert.Contains("Content-Type: application/problem+json", answer, StringComparison.Ordinal);
        Assert.Contains("\"title\":\"Bad Request\"", body, StringComparison.Ordinal);
        // S-06 c2: the answer never repeats the address.
        Assert.DoesNotContain("food.cafes", body, StringComparison.Ordinal);
    }

    // S-06 c4: a clean address, and a name that merely holds an encoded letter, still reach their routes.
    [Theory]
    [InlineData("/food.cafes/0/0/0.pbf")]
    [InlineData("/food%2Ecafes/0/0/0.pbf")]
    public async Task Lets_a_clean_address_reach_the_tile_route(string target)
    {
        var answer = await Http.Raw(service.Port, target, TestContext.Current.CancellationToken);

        Assert.StartsWith("HTTP/1.1 200 ", answer, StringComparison.Ordinal);
    }

    // S-06 c3, SR-11: a backslash is a letter to the service, not a folder's edge, so an address
    // that hides its folders behind backslashes matches no route and answers a 404 problem document.
    [Theory]
    [InlineData("/food.cafes\\0\\0\\0.pbf")]
    [InlineData("/food.cafes/0/0\\0.pbf")]
    [InlineData("/x/..\\food.cafes/0/0/0.pbf")]
    public async Task Answers_a_backslash_with_404(string target)
    {
        var answer = await Http.Raw(service.Port, target, TestContext.Current.CancellationToken);

        Assert.StartsWith("HTTP/1.1 404 ", answer, StringComparison.Ordinal);
        Assert.Contains("Content-Type: application/problem+json", answer, StringComparison.Ordinal);
    }

    // S-06 c1: Nginx refuses an encoded null first (edge.ts). A copy reached directly refuses it
    // too: Kestrel, ASP.NET Core's web server, answers 400 with an empty body before any route or
    // problem document runs, so the answer repeats nothing.
    [Fact]
    public async Task Answers_an_encoded_null_with_an_empty_400()
    {
        var answer = await Http.Raw(service.Port, "/food.cafes/0/0/0.pbf%00", TestContext.Current.CancellationToken);

        Assert.StartsWith("HTTP/1.1 400 ", answer, StringComparison.Ordinal);
        Assert.Contains("Content-Length: 0", answer, StringComparison.Ordinal);
    }

    // S-06 c1: Kestrel lets a raw control character, such as byte 1 or DEL, into the path, so the
    // service refuses it itself, with the 400 problem document of a dot folder in disguise.
    [Theory]
    [InlineData("/food.cafes/0/0/0.pbf\u0001")]
    [InlineData("/food\u001fcafes/0/0/0.pbf")]
    [InlineData("/food.cafes/0/0/\u007f0.pbf")]
    public async Task Answers_a_raw_control_character_with_400(string target)
    {
        var answer = await Http.Raw(service.Port, target, TestContext.Current.CancellationToken);

        Assert.StartsWith("HTTP/1.1 400 ", answer, StringComparison.Ordinal);
        Assert.Contains("Content-Type: application/problem+json", answer, StringComparison.Ordinal);
        Assert.DoesNotContain("food", answer[(answer.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4)..], StringComparison.Ordinal);
    }

    // A tile, the map page and /health are read, never written: each allows GET and HEAD alone. Any
    // other method answers 405, 'Method Not Allowed', as a problem document whose Allow header
    // names the two methods the address takes (RFC 9110, section 15.5.6).
    [Theory]
    [InlineData("POST", "/food.cafes/0/0/0.pbf")]
    [InlineData("DELETE", "/food.cafes/0/0/0.pbf")]
    [InlineData("POST", "/health")]
    [InlineData("DELETE", "/health")]
    [InlineData("PUT", "/health")]
    public async Task Answers_any_method_but_get_and_head_with_405(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using var answer = await service.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, answer.StatusCode);
        Assert.Equal("application/problem+json", answer.Content.Headers.ContentType?.MediaType);
        Assert.Equal(["GET", "HEAD"], answer.Content.Headers.Allow.Order(StringComparer.Ordinal));
    }

    // N-25: HEAD asks /health for its status alone, as a watcher may.
    [Fact]
    public async Task Answers_head_on_health()
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, "/health");
        using var answer = await service.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
    }

    // US-00 c2 and test 1: the run command itself starts the service, which answers on its port.
    [Fact]
    public async Task Starts_from_the_run_command_and_answers()
    {
        var port = Http.FreePort();
        var start = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "Tessera.dll"));
        start.Environment["Urls"] = $"http://127.0.0.1:{port}";
        start.Environment["DATABASE_URL"] = service.Database.ReaderUrl;
        using var program = System.Diagnostics.Process.Start(start)!;
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            var status = HttpStatusCode.ServiceUnavailable;
            for (var attempt = 0; attempt < 100 && status != HttpStatusCode.OK; attempt++)
            {
                try
                {
                    using var answer = await client.GetAsync($"/food.cafes/{Zoom}/{Tile.X}/{Tile.Y}.pbf", TestContext.Current.CancellationToken);
                    status = answer.StatusCode;
                    // 503 until the list of layers loads (N-23).
                    if (status != HttpStatusCode.OK)
                        await Task.Delay(100, TestContext.Current.CancellationToken);
                }
                catch (HttpRequestException)
                {
                    // Not listening yet: the program is still starting.
                    await Task.Delay(100, TestContext.Current.CancellationToken);
                }
            }

            Assert.Equal(HttpStatusCode.OK, status);
        }
        finally
        {
            program.Kill(entireProcessTree: true);
            await program.WaitForExitAsync(TestContext.Current.CancellationToken);
        }

        // The log holds no line for each request: under load those lines filled 140 MB a minute.
        var log = await program.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Now listening on", log, StringComparison.Ordinal);
        Assert.DoesNotContain("Request starting", log, StringComparison.Ordinal);
    }

    // N-25 c1 and test 1: with the list loaded and the database answering, /health says UP.
    [Fact]
    public async Task Says_up_when_the_service_can_serve_tiles()
    {
        using var answer = await Get("/health");

        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
        Assert.Equal("""{"status":"UP"}""", await answer.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("application/json", answer.Content.Headers.ContentType?.MediaType);
        Assert.True(answer.Headers.CacheControl?.NoStore);
    }
}
