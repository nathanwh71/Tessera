using System.Collections.Immutable;
using Tessera.Settings;

// Every SQL text Tessera sends lives in this file (S-01 c2), so a reviewer reads them all in one
// place. Values travel as named parameters, such as @z, never in the text. A name, such as a table's,
// cannot travel as a parameter, so it enters the text quoted by Quote.

namespace Tessera.Sql;

/// <summary>
/// What the tile query needs to know of a layer. Every name but the id is already quoted for SQL.
/// The Catalogue module builds one from each layer, so Sql needs nothing of Catalogue's (ADR 0001).
/// </summary>
/// <param name="Id">The layer's id, schema and name, such as <c>food.cafes</c>.</param>
/// <param name="Table">The quoted table, such as <c>"food"."cafes"</c>.</param>
/// <param name="Geometry">The quoted column that holds each place's shape.</param>
/// <param name="Srid">The number of the layer's coordinate system.</param>
/// <param name="Properties">The quoted columns each tile carries beside the shape (US-07).</param>
/// <param name="Order">The quoted primary key, which fixes the order of features; empty for a view.</param>
public sealed record TileSource(string Id, string Table, string Geometry, int Srid, ImmutableArray<string> Properties, ImmutableArray<string> Order);

public static class Queries
{
    /// <summary>
    /// Wraps a table, schema or column name in double quotes, doubling any quote inside, so the
    /// database reads it as one name and never as part of a command (S-01 c3): <c>x; DROP TABLE cafes</c>
    /// stays one odd name.
    /// </summary>
    public static string Quote(string name) => $"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    /// <summary>
    /// The smallest question: an answer proves the database is up and lets the service sign in (N-25).
    /// </summary>
    public static readonly Query Ping = new("SELECT 1");

    /// <summary>
    /// Every table and view with a typed geometry column that the signed-in role may read (US-03).
    /// </summary>
    /// <remarks>
    /// The database keeps the guest list, so the query asks it. pg_class lists tables ('r', 'p') and
    /// views ('v', 'm'), never functions (US-03 c5). A typed geometry column keeps its SRID, the number
    /// of its coordinate system, in atttypmod; an untyped one gives 0 and is left out. DISTINCT ON keeps
    /// each table's first geometry column. columns lists the other columns of simple types, which a
    /// tile carries; keycolumns lists the primary key, which fixes the order of features (US-24
    /// test 2). Both travel as JSON text, since Dapper cannot fill a list from a database array.
    /// </remarks>
    public static readonly Query Layers = new("""
        SELECT DISTINCT ON (n.nspname, c.relname)
               n.nspname::text AS schema,
               c.relname::text AS name,
               a.attname::text AS geometrycolumn,
               postgis_typmod_srid(a.atttypmod) AS srid,
               postgis_typmod_type(a.atttypmod) AS geometrytype,
               to_json(ARRAY(SELECT p.attname::text
                     FROM pg_attribute p
                     JOIN pg_type pt ON pt.oid = p.atttypid
                     WHERE p.attrelid = c.oid AND p.attnum > 0 AND NOT p.attisdropped AND p.attnum <> a.attnum
                       AND pt.typname IN ('text', 'varchar', 'bpchar', 'int2', 'int4', 'int8',
                                          'float4', 'float8', 'numeric', 'bool')
                     ORDER BY p.attnum))::text AS columns,
               to_json(ARRAY(SELECT k.attname::text
                     FROM pg_index i
                     JOIN pg_attribute k ON k.attrelid = i.indrelid AND k.attnum = ANY (i.indkey)
                     WHERE i.indrelid = c.oid AND i.indisprimary
                     ORDER BY array_position(i.indkey::int2[], k.attnum)))::text AS keycolumns
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum > 0 AND NOT a.attisdropped
        JOIN pg_type t ON t.oid = a.atttypid AND t.typname = 'geometry'
        WHERE c.relkind IN ('r', 'p', 'v', 'm')
          AND n.nspname NOT IN ('pg_catalog', 'information_schema')
          AND has_schema_privilege(n.oid, 'USAGE')
          AND has_table_privilege(c.oid, 'SELECT')
          AND postgis_typmod_srid(a.atttypmod) > 0
        ORDER BY n.nspname, c.relname, a.attnum
        """);

    /// <summary>One tile of one layer, built inside the database (US-07).</summary>
    /// <remarks>
    /// The WHERE clause turns the tile's square into the layer's own coordinate system, never the
    /// table's shapes into the tile's, so the spatial index applies and the tile reads only its own
    /// rows. The margin widens the square by the buffer, so a symbol near an edge shows on both
    /// tiles. ST_AsMVTGeom moves each shape into the tile's grid; ST_AsMVT packs the rows into one
    /// vector tile. Features come out in key order, so the same name wins a clash every time.
    /// </remarks>
    public static Query Tile(TileSource layer, int z, int x, int y, TileSettings limits)
    {
        // Each property the tile carries, after its shape, such as ', "name", "type"'.
        var properties = string.Concat(layer.Properties.Select(name => $", {name}"));
        // A table's key fixes the order of features (US-24 test 2); a view has no key, so no order.
        var order = layer.Order.IsDefaultOrEmpty
            ? ""
            : "\n  ORDER BY " + string.Join(", ", layer.Order.Select(name => $"t.{name}"));
        return new(
            $"""
            SELECT ST_AsMVT(tile.*, @layer, @resolution, 'tessera_shape') FROM (
              SELECT ST_AsMVTGeom(ST_Transform(t.{layer.Geometry}, 3857), ST_TileEnvelope(@z, @x, @y),
                                  @resolution, @buffer, true) AS tessera_shape{properties}
              FROM {layer.Table} AS t
              WHERE t.{layer.Geometry} && ST_Transform(ST_TileEnvelope(@z, @x, @y, margin => @margin), @srid){order}
              LIMIT @limit
            ) AS tile
            """,
            new
            {
                layer = layer.Id, resolution = limits.Resolution, z, x, y,
                buffer = limits.Buffer, margin = (double)limits.Buffer / limits.Resolution, srid = layer.Srid,
                limit = limits.MaxFeatures,
            });
    }

    /// <summary>
    /// The schema of the seed's journal, the table where DbUp records each script it has run (ADR 0009).
    /// </summary>
    public static Query JournalSchema(string schema) => new($"CREATE SCHEMA IF NOT EXISTS {schema}");
}
