using Modules.Tessera.Domain;

// Every SQL text Tessera sends lives in this file (S-01 c2), so a reviewer reads them all in one
// place. Values travel as named parameters, such as @z, never in the text. A name, such as a table's,
// cannot travel as a parameter, so it enters the text as a SqlName, which only quoting makes.

namespace Modules.Tessera.Infrastructure.Sql;

public static class Queries
{
    /// <summary>
    /// The smallest question: an answer proves the database is up and lets the service sign in (N-25).
    /// </summary>
    public static readonly Query Ping = new("SELECT 1");

    /// <summary>
    /// Every table and view with a typed geometry column that the signed-in <b><i>role</i></b> may read
    /// (US-03).
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

    /// <summary>One <b><i>tile</i></b> of one <b><i>layer</i></b>, built inside the database (US-07).</summary>
    /// <remarks>
    /// The WHERE clause turns the tile's square into the layer's own coordinate system, never the
    /// table's shapes into the tile's, so the <b><i>spatial index</i></b> applies and the tile reads
    /// only its own rows (US-07). The margin widens the square by the buffer, so a symbol near an edge
    /// shows on both tiles. ST_AsMVTGeom moves each shape into the tile's grid and, with its last
    /// argument true, clips it at the buffer, so a tile carries no more than it can draw. ST_AsMVT
    /// packs the rows into one <b><i>vector tile</i></b>, told that the column <c>tessera_shape</c>
    /// holds the shape: a name no table of places is likely to share. Features come out in key
    /// order: where two labels overlap, the map draws the first, so the same one shows every time.
    /// LIMIT caps the features at <c>Tiles.MaxFeatures</c>, so a crowded square stays small and quick
    /// to build.
    /// </remarks>
    public static Query Tile(Layer layer, TileAddress tile, TileSettings limits)
    {
        // Each property the tile carries, after its shape, such as ', "name", "type"'.
        var properties = string.Concat(layer.Properties.Select(name => $", {name.Sql}"));
        // A table's key fixes the order of features (US-24 test 2); a view has no key, so no order.
        var order = layer.Order.IsDefaultOrEmpty
            ? ""
            : "\n  ORDER BY " + string.Join(", ", layer.Order.Select(name => $"t.{name.Sql}"));
        return new(
            $"""
            SELECT ST_AsMVT(tile.*, @layer, @resolution, 'tessera_shape') FROM (
              SELECT ST_AsMVTGeom(ST_Transform(t.{layer.Geometry.Sql}, 3857), ST_TileEnvelope(@z, @x, @y),
                                  @resolution, @buffer, true) AS tessera_shape{properties}
              FROM {layer.Table.Sql} AS t
              WHERE t.{layer.Geometry.Sql} && ST_Transform(ST_TileEnvelope(@z, @x, @y, margin => @margin), @srid){order}
              LIMIT @limit
            ) AS tile
            """,
            new
            {
                layer = layer.Id, resolution = limits.Resolution, z = tile.Z, x = tile.X, y = tile.Y,
                buffer = limits.Buffer, margin = (double)limits.Buffer / limits.Resolution, srid = layer.Srid,
                limit = limits.MaxFeatures,
            });
    }

    /// <summary>
    /// The schema of the seed's <b><i>journal</i></b>, the table where DbUp records each script it has
    /// run (ADR 0009). DbUp makes the table but not its schema, so the seed sends this first. It lives
    /// here since every SQL text sits in this file (S-01 c2); IF NOT EXISTS lets it run at every start.
    /// </summary>
    public static Query JournalSchema(SqlName schema) => new($"CREATE SCHEMA IF NOT EXISTS {schema.Sql}");
}
