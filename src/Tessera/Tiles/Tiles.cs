using System.Security.Cryptography;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tessera.Catalogue;
using Tessera.Settings;
using Tessera.Sql;
using IResult = Microsoft.AspNetCore.Http.IResult;

// The request at the heart of Tessera: 'send me this tile of this layer' (US-07). A vector tile is
// one square of the map sent as data, the shapes of the places within it, for the browser to draw.
// Nginx, in front, keeps tiles, packs them and answers 304 (ADR 0008), so this file builds each tile
// it is asked for.

namespace Tessera.Tiles;

/// <summary>
/// One tile's address: zoom level z, column x and row y. Level 0 is one square of the world; each
/// level splits every square into four, so level z has 2^z columns and 2^z rows.
/// </summary>
public sealed record TileAddress(int Z, int X, int Y)
{
    /// <summary>
    /// The address, or null when no such tile exists: z outside the levels served, or x and y outside
    /// that level's grid. <c>z &gt; 30</c> keeps <c>1 &lt;&lt; z</c> inside a 32-bit number.
    /// </summary>
    public static TileAddress? Of(int z, int x, int y, int minZoom = 0, int maxZoom = 22) =>
        z < minZoom || z > maxZoom || z > 30 || x < 0 || y < 0 || x >= 1 << z || y >= 1 << z ? null : new(z, x, y);
}

/// <summary>Runs the tile query and gives the tile's bytes, or null for an empty tile. A test passes a stand-in.</summary>
public delegate Task<Result<byte[]?, DatabaseError>> TileReader(Query query, CancellationToken cancel);

/// <summary>Answers a tile request from the database (US-07).</summary>
/// <param name="catalogue">Gives the list of layers as it stands, or null before the first load (N-23).</param>
/// <param name="read">Runs the tile query.</param>
/// <param name="settings">The tile's limits and how long a browser and Nginx may keep it.</param>
/// <param name="layerChanged">Asks for the list of layers again; null in a test that needs no list.</param>
public sealed class TileServer(Func<LayerCatalogue?> catalogue, TileReader read, TileSettings settings, Action? layerChanged = null)
{
    public const string ContentType = "application/vnd.mapbox-vector-tile";

    /// <summary>
    /// <c>public, max-age=N</c> lets a browser, and Nginx, keep the tile N seconds; with 0, <c>no-cache</c>
    /// makes each check first (N-18).
    /// </summary>
    public static string CacheControl(int seconds) => seconds > 0 ? $"public, max-age={seconds}" : "no-cache";

    /// <summary>
    /// The tile's ETag, the label a browser sends back to ask 'is mine still good?' (N-19). It comes from
    /// a hash of the bytes, so equal bytes give an equal tag; it is weak, <c>W/"…"</c>, because one tag
    /// covers the plain and the gzip form.
    /// </summary>
    public static string Tag(byte[] bytes) => $"W/\"{Convert.ToHexStringLower(SHA256.HashData(bytes).AsSpan(0, 16))}\"";

    /// <summary>
    /// Finds the layer, checks the address, then runs the query. An unknown layer is refused before any
    /// query runs (US-07 test 1), and the answer never repeats the name a stranger sent. The
    /// parameters take the route's names, so ASP.NET Core fills them from the address.
    /// </summary>
    public async Task<IResult> Answer(string layer, int z, int x, int y, CancellationToken cancel)
    {
        if (catalogue() is not { } layers)
            return Problems.Unavailable("The list of layers is not ready yet.");
        if (layers.Find(layer) is not { } found)
            return Problems.NotFound("No published layer has that name.");
        if (TileAddress.Of(z, x, y, settings.MinZoom, settings.MaxZoom) is not { } tile)
            return Problems.NotFound("No tile has those coordinates.");

        var built = await read(Queries.Tile(found, tile, settings), cancel).ConfigureAwait(false);
        return built.Match(
            bytes => new TileResult(bytes ?? [], CacheControl(settings.CacheSeconds)),
            // A withdrawn grant gives 42501: the layer has left, so 404 (US-03 c6). A column an import
            // dropped gives 42703: the list is read again, and the browser asked to try again shortly.
            // A database that cannot answer now gives 503 (N-22); anything else is the service's fault.
            error => error.IsMissingTable ? Problems.NotFound("No published layer has that name.")
                : error.IsMissingColumn ? LayerChanged()
                : error.IsUnavailable ? Problems.Unavailable("The database cannot be reached. Try again shortly.")
                : Problems.Fault());
    }

    private IResult LayerChanged()
    {
        layerChanged?.Invoke();
        return Problems.Unavailable("The layer has changed. Try again shortly.");
    }

    /// <summary>A tile's bytes with its two headers: how long it may be kept, and its tag.</summary>
    public sealed record TileResult(byte[] Bytes, string CacheControl) : IResult
    {
        public Task ExecuteAsync(HttpContext context)
        {
            context.Response.Headers.CacheControl = CacheControl;
            context.Response.Headers.ETag = Tag(Bytes);
            return Results.Bytes(Bytes, ContentType).ExecuteAsync(context);
        }
    }
}

public static class TileRoutes
{
    /// <summary>
    /// Adds <c>/{layer}/{z}/{x}/{y}.pbf</c>, and the same with <c>.mvt</c>, for GET and HEAD (US-07).
    /// </summary>
    public static IEndpointRouteBuilder MapTiles(this IEndpointRouteBuilder routes, LiveCatalogue catalogue, Database database, TileSettings settings)
    {
        // Many tiles may meet a dropped column at once; the flag lets one reread run at a time.
        var rereading = 0;
        void Reread()
        {
            if (Interlocked.Exchange(ref rereading, 1) == 0)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await catalogue.Refresh(CancellationToken.None).ConfigureAwait(false);
                    }
                    finally
                    {
                        Volatile.Write(ref rereading, 0);
                    }
                });
        }

        var server = new TileServer(() => catalogue.Current, database.Single<byte[]>, settings, Reread);
        string[] read = [HttpMethods.Get, HttpMethods.Head];
        routes.MapMethods("/{layer}/{z:int}/{x:int}/{y:int}.pbf", read, server.Answer);
        routes.MapMethods("/{layer}/{z:int}/{x:int}/{y:int}.mvt", read, server.Answer);
        return routes;
    }
}
