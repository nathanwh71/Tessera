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
    /// The deepest level the service serves unless <c>DefaultMaxZoom</c> says otherwise. At level 22
    /// a tile spans about 10 metres, a doorway on the map (UI-09); deeper levels add nothing for points.
    /// </summary>
    public const int MaxZoom = 22;

    /// <summary>
    /// Checks that the tile exists: z within the levels served, and x and y inside that level's
    /// grid. A tile that does not exist is not found, as in the Java build (ADR 0006 rule 6).
    /// </summary>
    public static Result<TileAddress, string> Of(int z, int x, int y, int minZoom = 0, int maxZoom = MaxZoom) =>
        z < minZoom || z > maxZoom || z > 30 || x < 0 || y < 0 || x >= 1 << z || y >= 1 << z
            ? Result.Refuse<TileAddress, string>("No tile has those coordinates.")
            : Result.Ok<TileAddress, string>(new TileAddress(z, x, y));
}

/// <summary>The tile query, built in Sql from the layer, the tile and the settings (US-07).</summary>
public static class TileQuery
{
    public static Query For(Layer layer, TileAddress tile, TileSettings settings) =>
        TileQueries.Tile(layer.Source, new TileCell(tile.Z, tile.X, tile.Y), new TileLimits(settings.MaxFeatures, settings.Buffer, settings.Resolution));
}

/// <summary>The headers that tell a browser how to treat a tile it has received (N-18).</summary>
public static class TileHeaders
{
    /// <summary>
    /// <c>public, max-age=N</c> lets a browser keep the tile for N seconds and ask nothing; with
    /// 0, <c>no-cache</c> makes it check with the service before it reuses a copy (N-18 c1, c2).
    /// </summary>
    public static string CacheControl(int cacheSeconds) =>
        cacheSeconds > 0 ? $"public, max-age={cacheSeconds}" : "no-cache";
}

/// <summary>
/// A tile's bytes, with its headers, ready to write (US-07, N-18, N-19, N-20). The packed form goes
/// out when the browser accepts gzip; <c>Vary: Accept-Encoding</c> tells any cache between that the
/// two forms differ (N-20 c3).
/// </summary>
public sealed record TileResult(KeptTile Tile, bool Packed, string CacheControl) : IResult
{
    public string Tag => Tile.Tag;

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
/// 304 'Not Modified': the browser's copy is still good, so no body travels (N-19 c2). It carries
/// the same tag and cache rule a 200 would, as RFC 9110 asks, so the browser refreshes its copy's life.
/// </summary>
public sealed record NotModifiedResult(string Tag, string CacheControl) : IResult
{
    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status304NotModified;
        context.Response.Headers.CacheControl = CacheControl;
        context.Response.Headers.ETag = Tag;
        context.Response.Headers.Vary = "Accept-Encoding";
        return Task.CompletedTask;
    }
}

/// <summary>What the browser said about the answer it holds and the forms it accepts.</summary>
public sealed record TileRequest(Option<string> IfNoneMatch, bool AcceptsGzip)
{
    public static readonly TileRequest Plain = new(Option.None<string>(), AcceptsGzip: false);

    public static TileRequest Of(HttpRequest request) => new(
        request.Headers.IfNoneMatch.ToString() is { Length: > 0 } tag ? Option.Some(tag) : Option.None<string>(),
        TilePack.AcceptsGzip(request.Headers.AcceptEncoding.ToString()));
}

/// <summary>Runs the tile query and gives the tile's bytes, or none for an empty tile.</summary>
public delegate Task<Result<Option<byte[]>, DatabaseError>> TileReader(Query query, CancellationToken cancel);

/// <summary>
/// Answers a tile request: from a kept copy when there is one, else from the database (US-07, N-19).
/// </summary>
/// <remarks>
/// The reader is a function, not the database itself, so a test can count the queries with a
/// double and prove that a second request runs none (N-19 test 3).
/// When a crowd asks for one tile no copy holds yet, the first request starts the query and every
/// later one waits for that same answer, so the database builds each tile once (N-24). The queries
/// in flight sit in a map keyed by tile; each leaves the map the moment it ends.
/// </remarks>
/// <param name="catalogue">The list of layers as it stands, or none before the first load (N-23).</param>
/// <param name="read">Runs the tile query.</param>
/// <param name="settings">The tile's limits and how long a copy lives.</param>
/// <param name="cache">The copies already built.</param>
/// <param name="stopping">Ends a shared query when the service stops; one browser leaving never does.</param>
public sealed class TileServer(Func<Option<LayerCatalogue>> catalogue, TileReader read, TileSettings settings, TileCache cache, CancellationToken stopping = default)
{
    private readonly ConcurrentDictionary<TileKey, Lazy<Task<Result<KeptTile, DatabaseError>>>> _inFlight = new();

    /// <summary>
    /// Finds the layer, checks the address, then looks for a kept copy before it asks the database.
    /// An unknown layer is refused before any query runs (US-07 test 1), so a hostile name never
    /// reaches the database at all.
    /// </summary>
    public async Task<IResult> Answer(string layerId, int z, int x, int y, TileRequest request, CancellationToken cancel)
    {
        var target =
            // Before the first list of layers loads, nothing can be found, so the browser is asked to wait (N-23 c4).
            from layers in catalogue().OrRefuse(Problems.Unavailable("The list of layers is not ready yet."))
            // The detail never repeats the request's layer id: an answer should not echo what a stranger sent.
            from layer in layers.Find(layerId).OrRefuse(Problems.NotFound("No published layer has that name."))
            from tile in TileAddress.Of(z, x, y, settings.MinZoom, settings.MaxZoom).MapError(Problems.NotFound)
            select (Layer: layer, Tile: tile);

        return await target.Match(
            found => cache.Find(new TileKey(found.Layer.Id, z, x, y)).Match(
                kept => Task.FromResult(Reply(kept, request)),
                () => Build(found.Layer, found.Tile, request, cancel)),
            refusal => Task.FromResult(refusal)).ConfigureAwait(false);
    }

    // Joins the query in flight for this tile, or starts it. Lazy makes sure that when two requests
    // race to add the same tile, only one query starts. Each request waits on its own token, so a
    // browser that leaves stops waiting without cancelling the query for the rest.
    private async Task<IResult> Build(Layer layer, TileAddress tile, TileRequest request, CancellationToken cancel)
    {
        var key = new TileKey(layer.Id, tile.Z, tile.X, tile.Y);
        var flight = _inFlight.GetOrAdd(key, _ => Flight(key, layer, tile));
        return Answer(await flight.Value.WaitAsync(cancel).ConfigureAwait(false), request);
    }

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
            // A table revoked since start-up answers 42501; it has left, so it is not found (US-03 c6).
            // A database that cannot answer now asks the browser to try again (N-22); anything else is a fault.
            error => error.IsMissingTable ? Problems.NotFound("No published layer has that name.")
                : error.IsUnavailable ? Problems.Unavailable("The database cannot be reached. Try again shortly.")
                : Problems.Answer(StatusCodes.Status500InternalServerError, Option.None<string>()));

    // The browser that already holds this tile gets 304 and no body; any other gets the bytes, packed if it can unpack them.
    private IResult Reply(KeptTile tile, TileRequest request) =>
        request.IfNoneMatch.Match(header => TileTag.Matches(header, tile.Tag), () => false)
            ? new NotModifiedResult(tile.Tag, TileHeaders.CacheControl(settings.CacheSeconds))
            : new TileResult(tile, request.AcceptsGzip, TileHeaders.CacheControl(settings.CacheSeconds));
}

/// <summary>The tile route: <c>/{layer}/{z}/{x}/{y}.pbf</c>, and the same ending <c>.mvt</c> (US-07).</summary>
public static class TileRoutes
{
    public const string ContentType = "application/vnd.mapbox-vector-tile";

    private static readonly string[] ReadMethods = [HttpMethods.Get, HttpMethods.Head];

    public static IEndpointRouteBuilder MapTiles(this IEndpointRouteBuilder routes, LiveCatalogue catalogue, Database database, TileSettings settings, TileCache cache)
    {
        var stopping = routes.ServiceProvider.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        var server = new TileServer(() => catalogue.Current, database.Single<byte[]>, settings, cache, stopping);
        Task<IResult> Serve(string layer, int z, int x, int y, HttpRequest request, CancellationToken cancel) =>
            server.Answer(layer, z, x, y, TileRequest.Of(request), cancel);

        // HEAD asks for the headers alone; the web server leaves the body out. Both builds answer it
        // (ADR 0006 rule 6).
        routes.MapMethods("/{layer}/{z:int}/{x:int}/{y:int}.pbf", ReadMethods, Serve);
        routes.MapMethods("/{layer}/{z:int}/{x:int}/{y:int}.mvt", ReadMethods, Serve);
        return routes;
    }
}
