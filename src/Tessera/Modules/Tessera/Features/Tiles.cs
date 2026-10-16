using System.Security.Cryptography;
using Common.Api;
using CSharpFunctionalExtensions;
using Microsoft.Net.Http.Headers;
using Modules.Tessera.Domain;
using Modules.Tessera.Infrastructure;
using Modules.Tessera.Infrastructure.Sql;
using IResult = Microsoft.AspNetCore.Http.IResult;

// The request at the heart of Tessera: 'send me this tile of this layer' (US-07). A vector tile is
// one square of the map sent as data, the shapes of the places within it, for the browser to draw.
// Nginx, in front, keeps tiles and packs them (ADR 0008), so this file builds each tile it is asked
// for. ASP.NET Core answers 304 to a browser whose kept copy is still good, as Nginx does.

namespace Modules.Tessera.Features;

/// <summary>
/// Runs the tile query and gives the tile's bytes, or none for an empty tile. A test passes a stand-in.
/// </summary>
public delegate Task<Result<Maybe<byte[]>, DatabaseError>> TileReader(Query query, CancellationToken cancel);

/// <summary>Answers a tile request from the database (US-07).</summary>
/// <param name="catalogue">
/// Gives the <b><i>catalogue</i></b>, the list of <b><i>layers</i></b>, as it stands, or none before
/// the first load (N-23).
/// </param>
/// <param name="read">Runs the tile query.</param>
/// <param name="settings">The tile's limits and how long a browser and Nginx may keep it.</param>
public sealed class TileServer(Func<Maybe<LayerCatalogue>> catalogue, TileReader read, TileSettings settings)
{
    /// <summary>
    /// The registered media type of a <b><i>vector tile</i></b>, which US-07 c1 asks of every tile,
    /// so a client knows what the bytes hold.
    /// </summary>
    public const string ContentType = "application/vnd.mapbox-vector-tile";

    /// <summary>
    /// <c>public, max-age=N</c> lets a browser, and Nginx, keep the tile in its <b><i>cache</i></b> N
    /// seconds; with 0, <c>no-cache</c> makes each check first (N-18).
    /// </summary>
    public static string CacheControl(int seconds) => seconds > 0 ? $"public, max-age={seconds}" : "no-cache";

    /// <summary>
    /// The tile's ETag, the label a browser sends back to ask 'is mine still good?' (N-19). It comes from
    /// a hash of the bytes, so equal bytes give an equal tag; it is a <b><i>weak ETag</i></b>,
    /// <c>W/"…"</c>, because one tag covers the plain and the gzip form (N-20 c4). Sixteen of the
    /// hash's 32 bytes keep the tag short, yet two different tiles still never share one in practice.
    /// </summary>
    public static EntityTagHeaderValue Tag(byte[] bytes) =>
        new($"\"{Convert.ToHexStringLower(SHA256.HashData(bytes).AsSpan(0, 16))}\"", isWeak: true);

    /// <summary>
    /// Finds the layer, checks the address, then runs the query. An unknown layer is refused before any
    /// query runs (US-07 test 1), and the answer never repeats the name a stranger sent. The
    /// parameters take the route's names, so ASP.NET Core fills them from the address.
    /// </summary>
    public async Task<IResult> Answer(string layer, int z, int x, int y, CancellationToken cancel)
    {
        if (!catalogue().TryGetValue(out var layers))
            return Problems.Unavailable("The list of layers is not ready yet.");
        if (!layers.Find(layer).TryGetValue(out var found))
            return Problems.NotFound("No published layer has that name.");
        if (!TileAddress.Of(z, x, y, settings.MinZoom, settings.MaxZoom).TryGetValue(out var tile))
            return Problems.NotFound("No tile has those coordinates.");

        var built = await read(Queries.Tile(found, tile, settings), cancel).ConfigureAwait(false);
        // An empty tile is still a tile: 200 with no bytes, so the map draws nothing there, and the
        // browser and Nginx keep the empty answer like any other.
        return built.Match(
            bytes => new TileResult(bytes.GetValueOrDefault([]), CacheControl(settings.CacheSeconds)),
            // A withdrawn grant gives 42501: the layer has left, so 404 (US-03 c6). A column a seed script
            // dropped gives 42703: the browser is asked to try again shortly, and the list of layers
            // heals at its next refresh, within Catalogue.RefreshSeconds (N-23). A database that cannot
            // answer now gives 503 (N-22); anything else is the service's fault.
            error => error.IsMissingTable ? Problems.NotFound("No published layer has that name.")
                : error.IsMissingColumn ? Problems.Unavailable("The layer has changed. Try again shortly.")
                : error.IsUnavailable ? Problems.Unavailable("The database cannot be reached. Try again shortly.")
                : Problems.Fault());
    }

    /// <summary>
    /// A tile's bytes with its two headers: how long it may be kept, and its tag. ASP.NET Core writes
    /// the tag, and answers 304 with no body when the browser's <c>If-None-Match</c> holds the same
    /// one (N-19 c2), so only Cache-Control is written here.
    /// </summary>
    public sealed record TileResult(byte[] Bytes, string CacheControl) : IResult
    {
        public Task ExecuteAsync(HttpContext context)
        {
            context.Response.Headers.CacheControl = CacheControl;
            return TypedResults.Bytes(Bytes, ContentType, entityTag: Tag(Bytes)).ExecuteAsync(context);
        }
    }
}

/// <summary>
/// Adds the tile routes to the web service; TesseraModule.cs calls it, so the program needs to know
/// nothing of how a tile is built.
/// </summary>
public static class TileRoutes
{
    /// <summary>
    /// Adds <c>/{layer}/{z}/{x}/{y}.pbf</c>, the ending pg_tileserv uses, so its clients work unchanged,
    /// and the same with <c>.mvt</c>, the format's own name, which gives the same bytes (US-07 c1, c2).
    /// </summary>
    public static IEndpointRouteBuilder MapTiles(
        this IEndpointRouteBuilder routes, LiveCatalogue catalogue, Database database, TileSettings settings)
    {
        var server = new TileServer(() => catalogue.Current, database.Single<byte[]>, settings);
        // HEAD asks for the headers alone, as a watcher checking a tag may.
        string[] read = [HttpMethods.Get, HttpMethods.Head];
        routes.MapMethods("/{layer}/{z:int}/{x:int}/{y:int}.pbf", read, server.Answer);
        routes.MapMethods("/{layer}/{z:int}/{x:int}/{y:int}.mvt", read, server.Answer);
        return routes;
    }
}
