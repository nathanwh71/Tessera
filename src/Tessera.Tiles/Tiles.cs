using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
    /// The deepest level the service serves. At level 22 a tile spans about 10 metres, a doorway
    /// on the map (UI-09); deeper levels add nothing for points.
    /// </summary>
    public const int MaxZoom = 22;

    /// <summary>Checks that the tile exists: z from 0 to 22, and x and y inside that level's grid.</summary>
    public static Result<TileAddress, string> Of(int z, int x, int y) =>
        z is < 0 or > MaxZoom
            ? Result.Refuse<TileAddress, string>($"The zoom level must lie from 0 to {MaxZoom}.")
            : x < 0 || y < 0 || x >= 1 << z || y >= 1 << z
                ? Result.Refuse<TileAddress, string>($"At level {z}, x and y must lie from 0 to {(1 << z) - 1}.")
                : Result.Ok<TileAddress, string>(new TileAddress(z, x, y));
}

/// <summary>The SQL that builds one tile, in one query, inside the database (US-07).</summary>
public static class TileQuery
{
    /// <summary>
    /// A vector tile stores positions as whole numbers inside a square 4096 units wide, the usual
    /// size for Mapbox vector tiles.
    /// </summary>
    public const int Extent = 4096;

    /// <summary>
    /// Builds the tile query for one layer and one tile. Every value travels as a bound parameter;
    /// only the catalogue's quoted names enter the text (S-01).
    /// </summary>
    /// <remarks>
    /// ST_TileEnvelope gives the tile's square in Web Mercator metres. The WHERE clause turns that
    /// square into the layer's own coordinate system, never the table's shapes into Web Mercator,
    /// so the spatial index applies and each tile reads only the rows it needs (US-07 critical
    /// point). ST_AsMVTGeom then moves each shape into the tile's 4096 grid, and ST_AsMVT packs the
    /// rows into one Mapbox vector tile. The margin widens the search square by the buffer, so a
    /// symbol near an edge still appears on both tiles.
    /// </remarks>
    public static Query For(Layer layer, TileAddress tile, TileSettings settings) =>
        Query.Of($"""
            SELECT ST_AsMVT(tile.*, {layer.Id}, {Extent}, 'tessera_shape') FROM (
              SELECT ST_AsMVTGeom(ST_Transform(t.{layer.Geometry}, 3857), ST_TileEnvelope({tile.Z}, {tile.X}, {tile.Y}), {Extent}, {settings.Buffer}, true) AS tessera_shape{Columns(layer)}
              FROM {layer.Table} AS t
              WHERE t.{layer.Geometry} && ST_Transform(ST_TileEnvelope({tile.Z}, {tile.X}, {tile.Y}, margin => {(double)settings.Buffer / Extent}), {layer.Srid})
              LIMIT {settings.MaxFeatures}
            ) AS tile
            """);

    // ', "name", "group" …' for every property column, so each tile carries them all (US-24).
    private static NameList Columns(Layer layer) => new(layer.Properties, LeadingComma: true);
}

/// <summary>The tile route: <c>/{layer}/{z}/{x}/{y}.pbf</c>, and the same ending <c>.mvt</c> (US-07).</summary>
public static class TileRoutes
{
    public const string ContentType = "application/vnd.mapbox-vector-tile";

    public static IEndpointRouteBuilder MapTiles(this IEndpointRouteBuilder routes, LayerCatalogue catalogue, Database database, TileSettings settings)
    {
        routes.MapGet("/{layer}/{z:int}/{x:int}/{y:int}.pbf", (string layer, int z, int x, int y, CancellationToken cancel) =>
            Serve(catalogue, database, settings, layer, z, x, y, cancel));
        routes.MapGet("/{layer}/{z:int}/{x:int}/{y:int}.mvt", (string layer, int z, int x, int y, CancellationToken cancel) =>
            Serve(catalogue, database, settings, layer, z, x, y, cancel));
        return routes;
    }

    /// <summary>
    /// Finds the layer, checks the address, then asks the database. An unknown layer is refused
    /// before any query runs (US-07 test 1), so a hostile name never reaches the database at all.
    /// </summary>
    public static async Task<IResult> Serve(
        LayerCatalogue catalogue, Database database, TileSettings settings, string layerId, int z, int x, int y, CancellationToken cancel)
    {
        var request =
            // The detail never repeats the request's layer id: an answer should not echo what a stranger sent.
            from layer in catalogue.Find(layerId).OrRefuse(Problems.NotFound("No published layer has that name."))
            from tile in TileAddress.Of(z, x, y).MapError(Problems.BadRequest)
            select TileQuery.For(layer, tile, settings);

        return await request.Match(
            async query => (await database.Single<byte[]>(query, cancel).ConfigureAwait(false)).Match(
                bytes => Results.Bytes(bytes.OrElse([]), ContentType),
                // A table revoked since start-up answers 42501; it has left, so it is not found (US-03 c6).
                error => error.IsMissingTable
                    ? Problems.NotFound("No published layer has that name.")
                    : Problems.Answer(StatusCodes.Status500InternalServerError, Option.None<string>())),
            refusal => Task.FromResult(refusal)).ConfigureAwait(false);
    }
}
