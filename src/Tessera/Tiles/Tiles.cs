using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tessera.Catalogue;
using Tessera.Kernel;
using Tessera.Settings;
using Tessera.Sql;

// This file answers the request at the heart of Tessera: 'send me this tile of this layer' (US-07).
// A vector tile is one square of the map sent as data, the shapes of the places within it, for the
// browser to draw itself. TesseraService calls MapTiles at start-up; each request then reaches
// TileServer.Answer.

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
/// The label a browser keeps with a tile, so it can later ask 'is mine still good?' (N-19). HTTP
/// calls it an ETag. It comes from a hash of the tile's bytes, so the same bytes always give the same
/// tag; Nginx compares it and answers 304 (ADR 0008). It is weak, <c>W/"…"</c>, because one tag
/// covers the plain and the gzip form.
/// </summary>
public static class TileTag
{
    public static string Of(byte[] bytes) => $"W/\"{Convert.ToHexStringLower(SHA256.HashData(bytes).AsSpan(0, 16))}\"";
}

/// <summary>
/// Runs the tile query and gives the tile's bytes, or none for an empty tile. A test passes a
/// stand-in in place of the database.
/// </summary>
public delegate Task<Result<Option<byte[]>, DatabaseError>> TileReader(Query query, CancellationToken cancel);

/// <summary>
/// Answers a tile request from the database (US-07). Nginx, in front, keeps tiles, packs them and
/// lets one query serve a crowd (ADR 0008), so this class builds each tile it is asked for.
/// </summary>
/// <param name="catalogue">Gives the list of layers as it stands, or none before the first load (N-23).</param>
/// <param name="read">Runs the tile query.</param>
/// <param name="settings">The tile's limits and how long a browser and Nginx may keep it.</param>
/// <param name="layerChanged">
/// Called when a tile query names a column the table no longer holds, so the list of layers is read
/// again at once; none in a test that needs no list.
/// </param>
public sealed class TileServer(Func<Option<LayerCatalogue>> catalogue, TileReader read, TileSettings settings, Action? layerChanged = null)
{
    /// <summary>
    /// Finds the layer, checks the address, then runs the query. An unknown layer is refused before
    /// any query runs (US-07 test 1), so a hostile name never reaches the database.
    /// </summary>
    public async Task<IResult> Answer(string layerId, int z, int x, int y, CancellationToken cancel)
    {
        var target =
            // Before the first list of layers loads, the service answers 503 (N-23 c4).
            from layers in catalogue().OrRefuse(Problems.Unavailable("The list of layers is not ready yet."))
            // The detail never repeats the layer name from the request.
            from layer in layers.Find(layerId).OrRefuse(Problems.NotFound("No published layer has that name."))
            from tile in TileAddress.Of(z, x, y, settings.MinZoom, settings.MaxZoom).MapError(Problems.NotFound)
            select (Layer: layer, Tile: tile);

        return await target.Match(
            async found => Answer(await read(TileQuery.For(found.Layer, found.Tile, settings), cancel).ConfigureAwait(false)),
            refusal => Task.FromResult(refusal)).ConfigureAwait(false);
    }

    private IResult Answer(Result<Option<byte[]>, DatabaseError> built) =>
        built.Match(
            bytes => new TileResult(bytes.OrElse([]), TileHeaders.CacheControl(settings.CacheSeconds)),
            // A table whose grant was withdrawn gives 42501: the layer has left, so 404 (US-03 c6).
            // A column an import dropped gives 42703: the list is read again, and the browser asked
            // to try again shortly. A database that cannot answer now gives 503 (N-22); anything
            // else is the service's own fault, 500.
            error => error.IsMissingTable ? Problems.NotFound("No published layer has that name.")
                : error.IsMissingColumn ? LayerChanged()
                : error.IsUnavailable ? Problems.Unavailable("The database cannot be reached. Try again shortly.")
                : Problems.Fault());

    private IResult LayerChanged()
    {
        layerChanged?.Invoke();
        return Problems.Unavailable("The layer has changed. Try again shortly.");
    }
}

/// <summary>A tile's bytes with its two headers: how long it may be kept, and its tag.</summary>
public sealed record TileResult(byte[] Bytes, string CacheControl) : IResult
{
    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.Headers.CacheControl = CacheControl;
        context.Response.Headers.ETag = TileTag.Of(Bytes);
        return Results.Bytes(Bytes, TileRoutes.ContentType).ExecuteAsync(context);
    }
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
    public static IEndpointRouteBuilder MapTiles(this IEndpointRouteBuilder routes, LiveCatalogue catalogue, Database database, TileSettings settings)
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

        var server = new TileServer(() => catalogue.Current, database.Single<byte[]>, settings, Reread);
        Task<IResult> Serve(string layer, int z, int x, int y, CancellationToken cancel) => server.Answer(layer, z, x, y, cancel);

        // A GET request asks for a tile; a HEAD request asks for its headers alone, and the web
        // server leaves the body out. Both builds answer both (ADR 0006 rule 7).
        routes.MapMethods("/{layer}/{z:int}/{x:int}/{y:int}.pbf", ReadMethods, Serve);
        routes.MapMethods("/{layer}/{z:int}/{x:int}/{y:int}.mvt", ReadMethods, Serve);
        return routes;
    }
}
