using System.Collections.Immutable;

namespace Tessera.Sql;

/// <summary>
/// Every query the service sends, as a named function (S-01 c2, ADR 0001 rule 3). No other project
/// can build a query: <see cref="Query.Of"/> is internal to this one, so the compiler proves that
/// every SQL text in Tessera is written here, with names from the catalogue and values as bound
/// parameters.
/// </summary>
/// <remarks>
/// Each function takes a record rather than a row of values of one type, so a caller cannot swap
/// two numbers by mistake and still compile (ADR 0004).
/// </remarks>
public static class CatalogueQueries
{
    /// <summary>
    /// Lists every table and view with a typed geometry column that the signed-in role may read.
    /// </summary>
    /// <remarks>
    /// pg_class lists tables and views; relkind tells them apart: 'r' a table, 'p' a partitioned
    /// table, 'v' a view, 'm' a view that stores its rows. A function never appears in pg_class, so
    /// no function becomes a layer (US-03 c5). has_table_privilege asks the database, as the
    /// service's own role, whether a grant lets it read the table, so the database decides what is
    /// published. A typed column carries its SRID in its "type modifier", atttypmod; for an untyped
    /// column, or one typed without an SRID, postgis_typmod_srid gives 0, so the last condition
    /// leaves both out (US-24: an untyped column is not published).
    /// DISTINCT ON keeps one geometry column for each table: the first, by column number. The
    /// column names travel as JSON text, since Dapper cannot fill a list from a database array.
    /// </remarks>
    public static readonly Query List = Query.Of($"""
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
                     ORDER BY p.attnum))::text AS columns
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
}

/// <summary>The table, geometry column and property columns of a layer, as the catalogue quoted them.</summary>
public sealed record LayerSource(TableName Table, QuotedName Geometry, int Srid, ImmutableArray<QuotedName> Properties, string LayerId);

/// <summary>One tile's address on the web map's grid: zoom level z, column x, row y.</summary>
public sealed record TileCell(int Z, int X, int Y);

/// <summary>The most features one tile may carry, and how far past its edge it reaches, in tile units.</summary>
public sealed record TileLimits(int MaxFeatures, int Buffer);

public static class TileQueries
{
    /// <summary>
    /// A vector tile stores positions as whole numbers inside a square 4096 units wide, the usual
    /// size for Mapbox vector tiles.
    /// </summary>
    public const int Extent = 4096;

    /// <summary>
    /// Builds one tile of one layer in one query, inside the database (US-07).
    /// </summary>
    /// <remarks>
    /// ST_TileEnvelope gives the tile's square in Web Mercator metres. The WHERE clause turns that
    /// square into the layer's own coordinate system, never the table's shapes into Web Mercator,
    /// so the spatial index applies and each tile reads only the rows it needs (US-07 critical
    /// point). ST_AsMVTGeom then moves each shape into the tile's 4096 grid, and ST_AsMVT packs the
    /// rows into one Mapbox vector tile. The margin widens the search square by the buffer, so a
    /// symbol near an edge still appears on both tiles.
    /// </remarks>
    public static Query Tile(LayerSource layer, TileCell tile, TileLimits limits) =>
        Query.Of($"""
            SELECT ST_AsMVT(tile.*, {layer.LayerId}, {Extent}, 'tessera_shape') FROM (
              SELECT ST_AsMVTGeom(ST_Transform(t.{layer.Geometry}, 3857), ST_TileEnvelope({tile.Z}, {tile.X}, {tile.Y}), {Extent}, {limits.Buffer}, true) AS tessera_shape{new NameList(layer.Properties, LeadingComma: true)}
              FROM {layer.Table} AS t
              WHERE t.{layer.Geometry} && ST_Transform(ST_TileEnvelope({tile.Z}, {tile.X}, {tile.Y}, margin => {(double)limits.Buffer / Extent}), {layer.Srid})
              LIMIT {limits.MaxFeatures}
            ) AS tile
            """);
}

/// <summary>A column the import creates or fills: its quoted name and its type.</summary>
public sealed record ImportColumn(QuotedName Name, SqlType Type);

/// <summary>One layer's table as the import shapes it: the table, its primary key, its geometry, its spatial index and its property columns.</summary>
public sealed record ImportTable(TableName Table, QuotedName Id, QuotedName Geometry, QuotedName Index, ImmutableArray<ImportColumn> Columns);

/// <summary>One place's row: its id, its position in degrees, and a value for each column, in the table's column order.</summary>
public sealed record ImportRow(string Id, double Longitude, double Latitude, ImmutableArray<object> Values);

public static class ImportQueries
{
    /// <summary>"IF NOT EXISTS" lets the import run again on a database it has filled before (US-24).</summary>
    public static Query CreateSchema(QuotedName schema) => Query.Of($"CREATE SCHEMA IF NOT EXISTS {schema}");

    public static Query CreateTable(ImportTable table) =>
        Query.Of($"CREATE TABLE IF NOT EXISTS {table.Table} ({table.Id} {SqlType.Text} PRIMARY KEY, {table.Geometry} {SqlType.Point4326} NOT NULL)");

    public static Query AddColumn(ImportTable table, ImportColumn column) =>
        Query.Of($"ALTER TABLE {table.Table} ADD COLUMN IF NOT EXISTS {column.Name} {column.Type}");

    /// <summary>
    /// Gives a column that exists a new type, for a property whose values changed kind between two
    /// files. PostgreSQL turns each old value into text, boolean or a number by its own rules.
    /// </summary>
    public static Query ChangeColumnType(ImportTable table, ImportColumn column) =>
        Query.Of($"ALTER TABLE {table.Table} ALTER COLUMN {column.Name} TYPE {column.Type} USING {column.Name}::text::{column.Type}");

    /// <summary>
    /// A spatial index (GIST) sorts shapes by where they lie, so a tile finds its few places without
    /// reading the whole table (US-24 c1).
    /// </summary>
    public static Query CreateIndex(ImportTable table) =>
        Query.Of($"CREATE INDEX IF NOT EXISTS {table.Index} ON {table.Table} USING GIST ({table.Geometry})");

    /// <summary>
    /// DELETE empties the table inside the one transaction, and readers keep seeing the old rows
    /// until the new ones are all in (US-24 c6). TRUNCATE would be faster, but it locks the table
    /// until the import ends, so every tile request would wait.
    /// </summary>
    public static Query Empty(ImportTable table) => Query.Of($"DELETE FROM {table.Table}");

    /// <summary>One row; every value travels as a bound parameter, in the column list's order.</summary>
    public static Query Insert(ImportTable table, ImportRow row) =>
        Query.Of($"INSERT INTO {table.Table} ({table.Id}, {table.Geometry}{new NameList([.. table.Columns.Select(column => column.Name)], LeadingComma: true)}) VALUES ({row.Id}, ST_SetSRID(ST_MakePoint({row.Longitude}, {row.Latitude}), 4326){new ValueList(row.Values)})");

    /// <summary>ANALYZE gathers the statistics the database uses to choose the spatial index (US-24 c1).</summary>
    public static Query Analyse(ImportTable table) => Query.Of($"ANALYZE {table.Table}");

    /// <summary>The types of a table's columns, by name, so the import can see which changed.</summary>
    public static Query ColumnTypes(ImportTable table) =>
        Query.Of($"SELECT column_name AS name, data_type AS type FROM information_schema.columns WHERE table_schema = {table.Table.Schema.Raw} AND table_name = {table.Table.Table.Raw}");
}

public static class HealthQueries
{
    /// <summary>The smallest question the database can answer: it proves the database is up and lets the service in (N-25).</summary>
    public static readonly Query Ping = Query.Of($"SELECT 1");
}
