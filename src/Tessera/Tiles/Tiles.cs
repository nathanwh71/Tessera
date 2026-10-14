using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tessera.Catalogue;
using Tessera.Http;
using Tessera.Kernel;
using Tessera.Settings;
using Tessera.Sql;

// This file answers the request at the heart of Tessera: 'send me this tile of this layer' (US-07).
// A vector tile is one square of the map sent as data, the shapes of the places within it, for the
// browser to draw itself. The file belongs to the Tiles module; TesseraService calls MapTiles to
// add the tile addresses to the service, and each request then reaches TileServer.Answer.

namespace Tessera.Tiles;

/// <summary>
/// The address of one tile: zoom level z, column x and row y (US-07).
/// </summary>
/// <remarks>
/// A web map cuts the world into squares. Level 0 is one square; each level splits every square
/// into four, so level z has 2^z columns and 2^z rows, numbered from 0 at the top left. That doubling
/// is binary at work: one more bit for x and one more for y at each level.
/// </remarks>
public sealed record TileAddress(int Z, int X, int Y)
{
    /// <summary>
    /// The deepest level the service serves unless the <c>DefaultMaxZoom</c> setting says otherwise.
    /// At level 22 a tile spans about 10 metres, a doorway on the map (UI-09); deeper levels add
    /// nothing for points.
    /// </summary>
    public const int MaxZoom = 22;

    /// <summary>
    /// Checks that the tile exists: z within the levels served, and x and y inside that level's
    /// grid. A tile that does not exist is refused as not found, as in the Java build, the twin of this
    /// service written in the language Java (ADR 0006 rule 7).
    /// </summary>
    /// <remarks>
    /// The test <c>z &gt; 30</c> keeps <c>1 &lt;&lt; z</c>, which means 2^z, inside a whole number's
    /// 32 bits, whatever the settings say; past 30 the doubling would overflow and give nonsense.
    /// </remarks>
    public static Result<TileAddress, string> Of(int z, int x, int y, int minZoom = 0, int maxZoom = MaxZoom) =>
        z < minZoom || z > maxZoom || z > 30 || x < 0 || y < 0 || x >= 1 << z || y >= 1 << z
            ? Result.Refuse<TileAddress, string>("No tile has those coordinates.")
            : Result.Ok<TileAddress, string>(new TileAddress(z, x, y));
}

/// <summary>
/// The tile query, which the Sql module builds from the layer, the tile and the settings (US-07).
/// Only Sql writes database queries (ADR 0001); this class hands it the pieces.
/// </summary>
public static class TileQuery
{
    /// <summary>The query that returns this tile's bytes for this layer, within the settings' limits.</summary>
    public static Query For(Layer layer, TileAddress tile, TileSettings settings) =>
        TileQueries.Tile(layer.Source, new TileCell(tile.Z, tile.X, tile.Y), new TileLimits(settings.MaxFeatures, settings.Buffer, settings.Resolution));
}

/// <summary>
/// The headers, lines of extra information sent ahead of a tile, that tell a browser how long it
/// may keep the tile (N-18).
/// </summary>
public static class TileHeaders
{
    /// <summary>
    /// The <c>Cache-Control</c> header. <c>public, max-age=N</c> lets a browser keep the tile for
    /// N seconds and reuse it with no request; with 0 seconds, <c>no-cache</c> makes it check with
    /// the service before it reuses a copy (N-18 c1, c2).
    /// </summary>
    public static string CacheControl(int cacheSeconds) =>
        cacheSeconds > 0 ? $"public, max-age={cacheSeconds}" : "no-cache";
}

/// <summary>
/// A tile's bytes, with its headers, ready to send (US-07, N-18, N-19, N-20). The packed form, made
/// smaller with gzip, goes out when the browser can unpack it. <c>Vary: Accept-Encoding</c> warns any
/// cache on the way, a store of answers between browser and service, that the two forms differ, so
/// it hands each browser the form that browser asked for (N-20 c3).
/// </summary>
public sealed record TileResult(KeptTile Tile, bool Packed, string CacheControl) : IResult
{
    /// <summary>
    /// The tile's ETag, the label a browser sends back to ask whether its copy is still good.
    /// </summary>
    public string Tag => Tile.Tag;

    /// <summary>Writes the headers, then the plain or packed bytes, to the browser.</summary>
    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.Headers.CacheControl = CacheControl;
        context.Response.Headers.ETag = Tile.Tag;
        context.Response.Headers.Vary = "Accept-Encoding";
        if (Packed)
            context.Response.Headers.ContentEncoding = "gzip";
        return Results.Bytes(Packed ? Tile.Packed : Tile.Bytes, TileRoutes.ContentType).ExecuteAsync(context);
    }
}

/// <summary>
/// The answer 304 'Not Modified': the browser's copy is still good, so no body travels (N-19 c2).
/// Every answer carries a status code, a number that says how the request went: 200 means 'here it
/// is', 304 'use your own copy'. This one carries the same tag and cache rule a 200 would, as the
/// HTTP standard RFC 9110 asks, so the browser restarts its copy's life.
/// </summary>
public sealed record NotModifiedResult(string Tag, string CacheControl) : IResult
{
    /// <summary>Writes the status code and headers, with no body.</summary>
    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status304NotModified;
        context.Response.Headers.CacheControl = CacheControl;
        context.Response.Headers.ETag = Tag;
        context.Response.Headers.Vary = "Accept-Encoding";
        return Task.CompletedTask;
    }
}

/// <summary>
/// What the browser said about the copy it holds, if any, and whether it can unpack gzip.
/// </summary>
public sealed record TileRequest(Option<string> IfNoneMatch, bool AcceptsGzip)
{
    /// <summary>A request that holds no copy and accepts only plain bytes, for the tests.</summary>
    public static readonly TileRequest Plain = new(Option.None<string>(), AcceptsGzip: false);

    /// <summary>Reads the two headers that matter from a web request; an empty header counts as none.</summary>
    public static TileRequest Of(HttpRequest request) => new(
        request.Headers.IfNoneMatch.ToString() is { Length: > 0 } tag ? Option.Some(tag) : Option.None<string>(),
        TilePack.AcceptsGzip(request.Headers.AcceptEncoding.ToString()));
}

/// <summary>
/// Runs the tile query and gives the tile's bytes, or none for an empty tile. A
/// delegate, as here, names the shape of a function so another function can take one as input.
/// </summary>
public delegate Task<Result<Option<byte[]>, DatabaseError>> TileReader(Query query, CancellationToken cancel);

/// <summary>
/// Answers a tile request: from a kept copy when there is one, else from the database (US-07, N-19).
/// </summary>
/// <remarks>
/// <para>
/// The reader is a function, not the database itself, so a test can pass a stand-in that counts
/// the queries and prove that a second request runs none (N-19 test 3).
/// </para>
/// <para>
/// When a crowd asks at once for a tile no copy holds yet, the first request starts the query and
/// every later one waits for that same answer, so the database builds each tile once (N-24). The
/// queries in flight sit in a table keyed by tile; each leaves the table the moment it ends.
/// </para>
/// </remarks>
/// <param name="catalogue">Gives the list of layers as it stands, or none before the first load (N-23).</param>
/// <param name="read">Runs the tile query.</param>
/// <param name="settings">The tile's limits and how long a copy lives.</param>
/// <param name="cache">The copies already built.</param>
/// <param name="stopping">
/// A cancellation token, a signal that tells work to stop. This one stops a shared query only when
/// the service stops; one browser leaving never does.
/// </param>
/// <param name="layerChanged">
/// Called when a tile query names a column the table no longer holds, so the list of layers is read
/// again at once rather than at its next turn; none in a test that needs no list.
/// </param>
public sealed class TileServer(Func<Option<LayerCatalogue>> catalogue, TileReader read, TileSettings settings, TileCache cache, CancellationToken stopping = default, Action? layerChanged = null)
{
    // ConcurrentDictionary is a table that many threads, lines of work running side by side, may
    // change at once in safety.
    private readonly ConcurrentDictionary<TileKey, Lazy<Task<Result<KeptTile, DatabaseError>>>> _inFlight = new();

    /// <summary>
    /// Finds the layer, checks the address, then looks for a kept copy before it asks the database.
    /// An unknown layer is refused before any query runs (US-07 test 1), so a hostile name never
    /// reaches the database.
    /// </summary>
    /// <remarks>
    /// The method is async: while it waits for the database it lets its thread serve other requests,
    /// and resumes at each <c>await</c> once the answer comes. The <c>from … select</c> lines run
    /// three checks in turn; the first to fail gives the answer, and the rest are skipped.
    /// </remarks>
    /// <returns>
    /// The tile, a 304, or a problem document, the short error report the Http module writes:
    /// 404, 503 or 500.
    /// </returns>
    public async Task<IResult> Answer(string layerId, int z, int x, int y, TileRequest request, CancellationToken cancel)
    {
        var target =
            // Before the first list of layers loads, no layer can be found, so the service answers
            // 503 and the browser asks again later (N-23 c4).
            from layers in catalogue().OrRefuse(Problems.Unavailable("The list of layers is not ready yet."))
            // The detail never repeats the layer name from the request: an answer should not echo
            // what a stranger sent.
            from layer in layers.Find(layerId).OrRefuse(Problems.NotFound("No published layer has that name."))
            from tile in TileAddress.Of(z, x, y, settings.MinZoom, settings.MaxZoom).MapError(Problems.NotFound)
            select (Layer: layer, Tile: tile);

        return await target.Match(
            found => cache.Find(new TileKey(found.Layer.Id, z, x, y)).Match(
                kept => Task.FromResult(Reply(kept, request)),
                () => Build(found.Layer, found.Tile, request, cancel)),
            refusal => Task.FromResult(refusal)).ConfigureAwait(false);
    }

    // Joins the query in flight for this tile, or starts it. Lazy runs its work only once, so when two
    // requests race to add the same tile, only one query starts. Each request waits on its own token,
    // so a browser that leaves stops waiting without cancelling the query for the rest.
    private async Task<IResult> Build(Layer layer, TileAddress tile, TileRequest request, CancellationToken cancel)
    {
        var key = new TileKey(layer.Id, tile.Z, tile.X, tile.Y);
        var flight = _inFlight.GetOrAdd(key, _ => Flight(key, layer, tile));
        return Answer(await flight.Value.WaitAsync(cancel).ConfigureAwait(false), request);
    }

    // One query for one tile. It runs on the service's own stopping token, not a browser's, and
    // keeps the tile it builds; an empty tile is kept as zero bytes.
    private Lazy<Task<Result<KeptTile, DatabaseError>>> Flight(TileKey key, Layer layer, TileAddress tile)
    {
        Lazy<Task<Result<KeptTile, DatabaseError>>>? flight = null;
        flight = new Lazy<Task<Result<KeptTile, DatabaseError>>>(async () =>
        {
            try
            {
                return (await read(TileQuery.For(layer, tile, settings), stopping).ConfigureAwait(false)).Map(bytes =>
                {
                    var built = KeptTile.Of(bytes.OrElse([]), cache.Now);
                    cache.Keep(key, built);
                    return built;
                });
            }
            finally
            {
                // Removes this flight only, never a newer one for the same tile.
                _inFlight.TryRemove(KeyValuePair.Create(key, flight!));
            }
        });
        return flight;
    }

    private IResult Answer(Result<KeptTile, DatabaseError> built, TileRequest request) =>
        built.Match(
            tile => Reply(tile, request),
            // A table whose grant was withdrawn since start-up fails with error 42501 ('insufficient
            // privilege'); the layer has left, so the answer is 404 'not found' (US-03 c6). A column an
            // import dropped since the list of layers was last read fails with 42703: the layer has
            // changed, so the list is read again at once, and the browser asked to try again in a few
            // seconds. A database that cannot answer now gives 503 too (N-22); anything else is the
            // service's own fault, 500.
            error => error.IsMissingTable ? Problems.NotFound("No published layer has that name.")
                : error.IsMissingColumn ? LayerChanged()
                : error.IsUnavailable ? Problems.Unavailable("The database cannot be reached. Try again shortly.")
                : Problems.Answer(StatusCodes.Status500InternalServerError, Option.None<string>()));

    private IResult LayerChanged()
    {
        layerChanged?.Invoke();
        return Problems.Unavailable("The layer has changed. Try again shortly.");
    }

    // A browser that already holds this tile gets 304 and no body; any other gets the bytes,
    // packed if it can unpack them.
    private IResult Reply(KeptTile tile, TileRequest request) =>
        request.IfNoneMatch.Match(header => TileTag.Matches(header, tile.Tag), () => false)
            ? new NotModifiedResult(tile.Tag, TileHeaders.CacheControl(settings.CacheSeconds))
            : new TileResult(tile, request.AcceptsGzip, TileHeaders.CacheControl(settings.CacheSeconds));
}

/// <summary>
/// The tile route, the address pattern that leads a request here: <c>/{layer}/{z}/{x}/{y}.pbf</c>,
/// and the same with the ending <c>.mvt</c> (US-07).
/// </summary>
public static class TileRoutes
{
    /// <summary>The content type, the label that tells the browser the bytes are a vector tile.</summary>
    public const string ContentType = "application/vnd.mapbox-vector-tile";

    private static readonly string[] ReadMethods = [HttpMethods.Get, HttpMethods.Head];

    /// <summary>
    /// Adds both tile routes to the service. TesseraService calls this once at start-up.
    /// </summary>
    public static IEndpointRouteBuilder MapTiles(this IEndpointRouteBuilder routes, LiveCatalogue catalogue, Database database, TileSettings settings, TileCache cache)
    {
        var stopping = routes.ServiceProvider.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        // A tile that meets a dropped column asks for the list of layers again. Many tiles may ask at
        // once; the flag lets one reread run at a time, and the rest find it under way.
        var rereading = 0;
        void Reread()
        {
            if (Interlocked.Exchange(ref rereading, 1) == 0)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await catalogue.Refresh(stopping).ConfigureAwait(false);
                    }
                    finally
                    {
                        Volatile.Write(ref rereading, 0);
                    }
                });
        }

        var server = new TileServer(() => catalogue.Current, database.Single<byte[]>, settings, cache, stopping, Reread);
        Task<IResult> Serve(string layer, int z, int x, int y, HttpRequest request, CancellationToken cancel) =>
            server.Answer(layer, z, x, y, TileRequest.Of(request), cancel);

        // A GET request asks for a tile; a HEAD request asks for its headers alone, and the web
        // server leaves the body out. Both builds answer both (ADR 0006 rule 7).
        routes.MapMethods("/{layer}/{z:int}/{x:int}/{y:int}.pbf", ReadMethods, Serve);
        routes.MapMethods("/{layer}/{z:int}/{x:int}/{y:int}.mvt", ReadMethods, Serve);
        return routes;
    }
}
