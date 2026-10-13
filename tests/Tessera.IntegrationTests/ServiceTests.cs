using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.VectorTiles.Mapbox;
using Tessera.Catalogue;
using Tessera.Import;
using Tessera.Service;
using Tessera.Settings;
using Tessera.Sql;

namespace Tessera.IntegrationTests;

/// <summary>
/// The whole service over real HTTP: the seed imported and granted, the catalogue loaded, the
/// service listening on a port of its own (US-00, US-07, S-01, S-06, US-23 c5).
/// </summary>
public sealed class ServiceFixture : IAsyncLifetime
{
    public DatabaseFixture Database { get; } = new();

    public int Port { get; } = Http.FreePort();

    public HttpClient Client { get; private set; } = new();

    private WebApplication? _app;
    private Database? _reader;

    public async ValueTask InitializeAsync()
    {
        await Database.InitializeAsync();
        var box = new AreaBox(113.52, 22.10, 113.61, 22.22);
        var seed = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "data", "macau-pois.geojson"));
        await using (var publisher = Sql.Database.Open(Database.Publisher))
            (await Importer.Run(publisher, seed, box)).Match(_ => 0, error => throw new InvalidOperationException(error.Message));
        await Database.GrantPlaces();

        // The same cafés in Web Mercator, for US-07 c3: a layer in each coordinate system.
        await Database.Owner("""
            CREATE SCHEMA stage;
            CREATE TABLE stage.cafes_3857 AS SELECT id, name, ST_Transform(geom, 3857)::geometry(Point, 3857) AS geom FROM food.cafes;
            GRANT USAGE ON SCHEMA stage TO tessera_reader;
            GRANT SELECT ON stage.cafes_3857 TO tessera_reader;
            """);

        _reader = Sql.Database.Open(Database.Reader);
        var catalogue = (await LayerCatalogue.Load(_reader)).Match(found => found, error => throw new InvalidOperationException(error.Message));
        var settings = new ServiceSettings(
            new Area("Macau", box, new HomeView(113.55, 22.19, 14)),
            new MapSettings(Preview: true),
            new TileSettings(10000, 64, 60, 64),
            Port,
            Database.Reader);

        // The built map, copied beside the tests by the service's project reference (US-23 c4).
        _app = TesseraService.Build(settings, catalogue, _reader, Path.Combine(AppContext.BaseDirectory, "wwwroot"));
        await _app.StartAsync();
        Client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{Port}") };
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
        if (_reader is not null)
            await _reader.DisposeAsync();
        await Database.DisposeAsync();
    }
}

public sealed class ServiceTests(ServiceFixture service) : IClassFixture<ServiceFixture>
{
    // cafes-001 lies at longitude 113.577677, latitude 22.158784.
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

    private static readonly (int X, int Y) Tile = TileOf(Longitude, Latitude, Zoom);

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

    // N-19 c1, c2 and test 2: a tile carries a weak tag, and a request that sends it back gets
    // 304 with no body and the same headers.
    [Fact]
    public async Task Answers_304_to_a_browser_that_sends_the_tag_back()
    {
        using var first = await Get($"/food.cafes/{Zoom}/{Tile.X}/{Tile.Y}.pbf");
        var tag = first.Headers.ETag;
        Assert.NotNull(tag);
        Assert.True(tag.IsWeak);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/food.cafes/{Zoom}/{Tile.X}/{Tile.Y}.pbf");
        request.Headers.IfNoneMatch.Add(tag);
        using var second = await service.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
        Assert.Equal(tag, second.Headers.ETag);
        Assert.Equal("public, max-age=60", second.Headers.CacheControl?.ToString());
        Assert.Empty(await second.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
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

        var script = System.Text.RegularExpressions.Regex.Match(html, @"src=""(/assets/[^""]+\.js)""").Groups[1].Value;
        using var asset = await Get(script);
        Assert.Equal(HttpStatusCode.OK, asset.StatusCode);
        Assert.Equal("text/javascript", asset.Content.Headers.ContentType?.MediaType);
    }

    // US-00: an address no route knows answers a 404 problem document too, and so does a page named
    // for anything but one of the nine layers.
    [Theory]
    [InlineData("/nothing-here")]
    [InlineData("/index.html")]
    [InlineData("/food.zoos.html")]
    public async Task Answers_an_unknown_address_with_a_problem_document(string path)
    {
        using var answer = await Get(path);

        Assert.Equal(HttpStatusCode.NotFound, answer.StatusCode);
        Assert.Equal("application/problem+json", answer.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Answers_a_tile_outside_the_grid_with_400()
    {
        using var answer = await Get("/food.cafes/1/5/0.pbf");

        Assert.Equal(HttpStatusCode.BadRequest, answer.StatusCode);
        Assert.Equal("application/problem+json", answer.Content.Headers.ContentType?.MediaType);
    }

    // S-01 test 4: an attack changes nothing.
    [Theory]
    [InlineData("x;%20DROP%20TABLE%20cafes")]
    [InlineData("food.cafes'%3B%20--")]
    public async Task Answers_an_attack_with_404_and_keeps_every_row(string layer)
    {
        using var answer = await Get($"/{layer}/0/0/0.pbf");

        Assert.Equal(HttpStatusCode.NotFound, answer.StatusCode);
        Assert.Equal(100L, await service.Database.OwnerValue<long>("SELECT count(*) FROM food.cafes"));
    }

    // S-06 test 1 and 2: malformed targets get 400 before any route runs, and the answer hides the target.
    [Theory]
    [InlineData("/food.cafes/%2e%2e/1/2/3.pbf")]
    [InlineData("/food.cafes%2F15/1/2.pbf")]
    [InlineData("/food.cafes\\15/1/2.pbf")]
    [InlineData("/food.cafes/15/1/2.pbf%zz")]
    [InlineData("/food.cafes/15/1/2.pbf%01")]
    [InlineData("/food.c%61fes/15/1/2.pbf")]
    public async Task Refuses_a_malformed_target_before_any_route(string target)
    {
        var answer = await Http.Raw(service.Port, target);

        Assert.StartsWith("HTTP/1.1 400", answer, StringComparison.Ordinal);
        Assert.Contains("application/problem+json", answer, StringComparison.Ordinal);
        Assert.Contains("\"title\":\"Bad Request\"", answer, StringComparison.Ordinal);
        Assert.DoesNotContain("food.cafes", answer, StringComparison.Ordinal);
    }

    // Kestrel, the web server inside .NET, refuses an encoded null (%00) and a raw control character
    // itself, before any of Tessera's code runs. The request never reaches a route, as S-06 c1 asks,
    // but the answer has an empty body, not a problem document: S-06 c2 breaks for these two cases.
    [Theory]
    [InlineData("/food.cafes/15/1/2.pbf%00")]
    [InlineData("/food.cafes/15/1/	2.pbf")]
    public async Task Leaves_kestrel_to_refuse_a_null_or_control_character(string target)
    {
        var answer = await Http.Raw(service.Port, target);

        Assert.StartsWith("HTTP/1.1 400", answer, StringComparison.Ordinal);
        Assert.DoesNotContain("food.cafes", answer, StringComparison.Ordinal);
    }

    // US-00 c2 and test 1: the run command itself starts the service, which answers on its port.
    [Fact]
    public async Task Starts_from_the_run_command_and_answers()
    {
        var port = Http.FreePort();
        var start = new System.Diagnostics.ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "Tessera.Service.dll"));
        start.Environment["TESSERA_HTTP_PORT"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        start.Environment["DATABASE_URL"] = $"postgresql://{service.Database.Reader.User}:{service.Database.Reader.Password}@{service.Database.Reader.Host}:{service.Database.Reader.Port}/tessera";
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
    }

    // S-06 test 3: a clean target reaches US-07's route.
    [Fact]
    public async Task Lets_a_clean_raw_target_reach_the_route() =>
        Assert.StartsWith("HTTP/1.1 200", await Http.Raw(service.Port, $"/food.cafes/{Zoom}/{Tile.X}/{Tile.Y}.pbf"), StringComparison.Ordinal);
}
