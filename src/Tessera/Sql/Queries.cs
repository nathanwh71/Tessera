using System.Collections.Immutable;
using Dapper;
using Tessera.Catalogue;
using Tessera.Settings;
using Tessera.Tiles;

// Every SQL text Tessera sends lives in this file (S-01 c2), so a reviewer reads them all in one
// place. Values travel as named parameters, such as @z, never in the text. A name, such as a table's,
// cannot travel as a parameter, so it enters the text quoted by Quote.

namespace Tessera.Sql;

public static class Queries
{
    /// <summary>
    /// Wraps a table, schema or column name in double quotes, doubling any quote inside, so the
    /// database reads it as one name and never as part of a command (S-02): <c>x; DROP TABLE cafes</c>
    /// stays one odd name.
    /// </summary>
    public static string Quote(string name) => $"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    /// <summary>The smallest question: an answer proves the database is up and lets the service sign in (N-25).</summary>
    public static readonly Query Ping = new("SELECT 1");

    /// <summary>
    /// Every table and view with a typed geometry column that the signed-in role may read (US-03).
    /// </summary>
    /// <remarks>
    /// The database keeps the guest list, so the query asks it. pg_class lists tables ('r', 'p') and
    /// views ('v', 'm'), never functions (US-03 c5). A typed geometry column keeps its SRID, the number
    /// of its coordinate system, in atttypmod; an untyped one gives 0 and is left out. DISTINCT ON keeps
    /// each table's first geometry column. columns lists the other columns of simple types, which a
    /// tile carries; keycolumns lists the primary key, which fixes the order of features (ADR 0006
    /// rule 7). Both travel as JSON text, since Dapper cannot fill a list from a database array.
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
                       AND pt.typname IN ('text', 'varchar', 'bpchar', 'int2', 'int4', 'int8', 'float4', 'float8', 'numeric', 'bool')
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
    public static Query Tile(Layer layer, TileAddress tile, TileSettings limits) =>
        new($"""
            SELECT ST_AsMVT(tile.*, @layer, @resolution, 'tessera_shape') FROM (
              SELECT ST_AsMVTGeom(ST_Transform(t.{layer.Geometry}, 3857), ST_TileEnvelope(@z, @x, @y), @resolution, @buffer, true) AS tessera_shape{string.Concat(layer.Properties.Select(name => $", {name}"))}
              FROM {layer.Table} AS t
              WHERE t.{layer.Geometry} && ST_Transform(ST_TileEnvelope(@z, @x, @y, margin => @margin), @srid){(layer.Order.IsDefaultOrEmpty ? "" : "\n  ORDER BY " + string.Join(", ", layer.Order.Select(name => $"t.{name}")))}
              LIMIT @limit
            ) AS tile
            """,
            new
            {
                layer = layer.Id, resolution = limits.Resolution, z = tile.Z, x = tile.X, y = tile.Y, buffer = limits.Buffer,
                margin = (double)limits.Buffer / limits.Resolution, srid = layer.Srid, limit = limits.MaxFeatures,
            });

    // The import's queries (US-24). Each that makes something says IF NOT EXISTS, so the import may
    // run again on a database it has filled before. 'table' is the quoted "schema"."name".

    public static Query CreateSchema(string schema) => new($"CREATE SCHEMA IF NOT EXISTS {schema}");

    /// <summary>A layer's table: the id, its primary key, and a point in longitude and latitude (SRID 4326).</summary>
    public static Query CreateTable(string table) =>
        new($"CREATE TABLE IF NOT EXISTS {table} (\"id\" text PRIMARY KEY, \"geom\" geometry(Point, 4326) NOT NULL)");

    public static Query AddColumn(string table, string column, string type) =>
        new($"ALTER TABLE {table} ADD COLUMN IF NOT EXISTS {column} {type}");

    /// <summary>
    /// A new type for a column whose values changed kind between two files. The import runs it after
    /// it empties the table, so no old value, such as the word 'closed', has to become a number.
    /// </summary>
    public static Query ChangeColumnType(string table, string column, string type) =>
        new($"ALTER TABLE {table} ALTER COLUMN {column} TYPE {type} USING {column}::text::{type}");

    /// <summary>Removes a column the new file no longer carries, so no tile carries a column of nothing.</summary>
    public static Query DropColumn(string table, string column) => new($"ALTER TABLE {table} DROP COLUMN IF EXISTS {column}");

    /// <summary>A spatial index sorts shapes by where they lie, so a tile finds its places without reading the table (US-24 c1).</summary>
    public static Query CreateIndex(string table, string index) => new($"CREATE INDEX IF NOT EXISTS {index} ON {table} USING GIST (\"geom\")");

    /// <summary>
    /// Empties the table with DELETE, inside the import's transaction, so readers see the old rows
    /// until the new ones are in (US-24 c6). TRUNCATE would lock out every reader until the end.
    /// </summary>
    public static Query Empty(string table) => new($"DELETE FROM {table}");

    /// <summary>One row; the id, the position and each value travel as parameters, in the columns' order.</summary>
    public static Query Insert(string table, ImmutableArray<string> columns, string id, double longitude, double latitude, ImmutableArray<object?> values)
    {
        var parameters = new DynamicParameters(new { id, longitude, latitude });
        for (var index = 0; index < values.Length; index++)
            parameters.Add($"p{index}", values[index]);
        return new(
            $"INSERT INTO {table} (\"id\", \"geom\"{string.Concat(columns.Select(name => $", {name}"))}) VALUES (@id, ST_SetSRID(ST_MakePoint(@longitude, @latitude), 4326){string.Concat(columns.Select((_, index) => $", @p{index}"))})",
            parameters);
    }

    /// <summary>Fresh statistics, which the database reads to decide when the spatial index pays.</summary>
    public static Query Analyse(string table) => new($"ANALYZE {table}");

    /// <summary>A table's column types, by name; the names here are values to compare, so they travel as parameters.</summary>
    public static Query ColumnTypes(string schema, string table) =>
        new("SELECT column_name AS name, data_type AS type FROM information_schema.columns WHERE table_schema = @schema AND table_name = @table", new { schema, table });
}
