using System.Collections.Immutable;

// Every SQL query Tessera sends lives in this file, as a named function in the Sql module (S-01 c2,
// ADR 0001 rule 3). The Catalogue module calls CatalogueQueries to list the layers, the Tiles module
// calls TileQueries for each map tile, the Import module calls ImportQueries to load the seed file,
// and the health check calls HealthQueries. Database.cs sends them all.

namespace Tessera.Sql;

/// <summary>
/// The query that lists the layers, for the Catalogue module (US-03). Every query the service sends
/// is a named function in this file (S-01 c2, ADR 0001 rule 3). No other project can build a query,
/// since <see cref="Query.Of"/> is internal to this one, so the compiler proves that every SQL text
/// in Tessera is written here, with names from the catalogue and values as bound parameters, sent
/// apart from the SQL text so they can never change what a query does.
/// </summary>
/// <remarks>
/// Each function takes a record, a type with named fields, rather than a row of plain values of one
/// type, so a caller who swaps two numbers by mistake gets a compile error (ADR 0004).
/// </remarks>
public static class CatalogueQueries
{
    /// <summary>
    /// Lists every table and view with a typed geometry column that the signed-in role may read.
    /// </summary>
    /// <remarks>
    /// The database decides what is published, so the query asks it rather than keeping a list of its
    /// own. PostgreSQL, the database, keeps lists of its own tables in system tables named pg_...;
    /// pg_class lists tables and views, and relkind tells them apart: 'r' a table, 'p' a partitioned
    /// table, 'v' a view (a saved query that reads like a table), 'm' a view that stores its rows. A
    /// function never appears in pg_class, so no database function becomes a layer (US-03 c5).
    /// <para>
    /// has_table_privilege asks the database, as the service's own role (the user name it signs in
    /// as), whether a grant, a permission given to a role, lets it read the table;
    /// has_schema_privilege asks the same of the schema, the named folder that holds the table. The
    /// database's own folders, pg_catalog and information_schema, are left out.
    /// </para>
    /// <para>
    /// A geometry column holds shapes. A typed one names its kind of shape and its SRID, the number
    /// that names its coordinate system, and keeps the SRID in its 'type modifier', atttypmod.
    /// postgis_typmod_srid comes from PostGIS, the database's add-on for maps; for an untyped column,
    /// or one typed without an SRID, it gives 0, so the last condition of the WHERE clause leaves both
    /// out (US-24: an untyped column is not published).
    /// </para>
    /// <para>
    /// DISTINCT ON keeps one geometry column for each table: the first, by column number. columns
    /// lists the table's other columns of simple types, text, numbers and true-or-false, which a tile
    /// carries as properties. The column names travel as JSON text, since Dapper, the library that
    /// turns rows into C# values, cannot fill a list from a database array. keycolumns lists the
    /// primary key's columns, those whose values name each row, in the key's own order; a view has
    /// none, so its list is empty (ADR 0006 rule 7).
    /// </para>
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
}

/// <summary>
/// What the tile query needs to know of one layer: its table, geometry column, SRID, property columns
/// and id, every name already quoted by the catalogue.
/// </summary>
/// <remarks>
/// <paramref name="Order"/> holds the primary key's columns, which fix the order of features in a
/// tile; it is empty for a view.
/// </remarks>
public sealed record LayerSource(TableName Table, QuotedName Geometry, int Srid, ImmutableArray<QuotedName> Properties, string LayerId, ImmutableArray<QuotedName> Order);

/// <summary>
/// One tile's address on the web map's grid: zoom level z, column x, row y. At zoom z the world is
/// 2 to the power z tiles wide and as many high.
/// </summary>
public sealed record TileCell(int Z, int X, int Y);

/// <summary>
/// The limits on one tile: the most features it may carry, how far past its edge it reaches (the
/// buffer), and how many units wide it is. The buffer and the width are both in tile units; the width
/// is pg_tileserv's <c>DefaultResolution</c> setting, 4096 unless set.
/// </summary>
public sealed record TileLimits(int MaxFeatures, int Buffer, int Resolution = TileQueries.Extent);

/// <summary>
/// The query that builds a map tile; the Tiles module calls it for each tile a browser asks for (US-07).
/// </summary>
public static class TileQueries
{
    /// <summary>
    /// A vector tile, a map square sent as a compact list of shapes rather than a picture, stores each
    /// position as a whole number inside a square 4096 units wide by default, the usual size in the
    /// Mapbox Vector Tile format; the <c>DefaultResolution</c> setting may change it.
    /// </summary>
    public const int Extent = 4096;

    /// <summary>
    /// Builds one tile of one layer in a single query, inside the database (US-07).
    /// </summary>
    /// <remarks>
    /// The WHERE clause turns the tile's square into the layer's own coordinate system, never the
    /// table's shapes into the tile's, so the spatial index applies and each tile reads only the rows
    /// it needs (US-07 critical point). A spatial index sorts shapes by where they lie, like a book's
    /// index; it knows each shape only in the table's own system, so a query that turned every shape
    /// first would have to read them all.
    /// <para>
    /// ST_TileEnvelope gives the tile's square in Web Mercator metres (SRID 3857), the flat square of
    /// the web map, and <c>&amp;&amp;</c> asks whether two shapes' boxes overlap. The margin widens the
    /// search square by the buffer, as a share of the tile's width, so a symbol near an edge still
    /// appears on both tiles. ST_AsMVTGeom then moves each shape into the tile's grid of whole
    /// numbers, and ST_AsMVT packs the rows into one Mapbox vector tile.
    /// </para>
    /// <para>
    /// Where two names clash on the map, the same one must win every time, and both builds must send
    /// the same bytes for the same tile (ADR 0006 rule 7). So features come out in the order of the
    /// table's key, as in the Java build.
    /// </para>
    /// </remarks>
    public static Query Tile(LayerSource layer, TileCell tile, TileLimits limits) =>
        Query.Of($"""
            SELECT ST_AsMVT(tile.*, {layer.LayerId}, {limits.Resolution}, 'tessera_shape') FROM (
              SELECT ST_AsMVTGeom(ST_Transform(t.{layer.Geometry}, 3857), ST_TileEnvelope({tile.Z}, {tile.X}, {tile.Y}), {limits.Resolution}, {limits.Buffer}, true) AS tessera_shape{new NameList(layer.Properties, LeadingComma: true)}
              FROM {layer.Table} AS t
              WHERE t.{layer.Geometry} && ST_Transform(ST_TileEnvelope({tile.Z}, {tile.X}, {tile.Y}, margin => {(double)limits.Buffer / limits.Resolution}), {layer.Srid}){new OrderBy(layer.Order)}
              LIMIT {limits.MaxFeatures}
            ) AS tile
            """);
}

/// <summary>A column the import creates or fills: its quoted name and its type.</summary>
public sealed record ImportColumn(QuotedName Name, SqlType Type);

/// <summary>
/// One layer's table as the import shapes it: the table, its primary key, its geometry column, its
/// spatial index and its property columns.
/// </summary>
public sealed record ImportTable(TableName Table, QuotedName Id, QuotedName Geometry, QuotedName Index, ImmutableArray<ImportColumn> Columns);

/// <summary>
/// One place's row: its id, its position in degrees, and a value for each column, in the table's
/// column order.
/// </summary>
public sealed record ImportRow(string Id, double Longitude, double Latitude, ImmutableArray<object> Values);

/// <summary>
/// The queries the Import module runs to load the seed file (US-24). Each one that makes something
/// says 'IF NOT EXISTS', so the import may run again on a database it has filled before.
/// </summary>
public static class ImportQueries
{
    /// <summary>Makes a group's schema, the named folder that holds its tables.</summary>
    public static Query CreateSchema(QuotedName schema) => Query.Of($"CREATE SCHEMA IF NOT EXISTS {schema}");

    /// <summary>
    /// Makes a layer's table with two columns: the id, its primary key, and the geometry, a point in
    /// longitude and latitude (SRID 4326). Each property gets a column of its own afterwards.
    /// </summary>
    public static Query CreateTable(ImportTable table) =>
        Query.Of($"CREATE TABLE IF NOT EXISTS {table.Table} ({table.Id} {SqlType.Text} PRIMARY KEY, {table.Geometry} {SqlType.Point4326} NOT NULL)");

    public static Query AddColumn(ImportTable table, ImportColumn column) =>
        Query.Of($"ALTER TABLE {table.Table} ADD COLUMN IF NOT EXISTS {column.Name} {column.Type}");

    /// <summary>
    /// Gives an existing column a new type, for a property whose values changed kind between two
    /// files. The USING clause turns each old value into text and then into the new type, by
    /// PostgreSQL's own rules.
    /// </summary>
    /// <remarks>
    /// The import runs this after it empties the table, so no old value has to turn: before
    /// 14 October 2026 it ran first, and a word such as 'closed' in a column becoming a number made
    /// the database refuse, and the whole import roll back.
    /// </remarks>
    public static Query ChangeColumnType(ImportTable table, ImportColumn column) =>
        Query.Of($"ALTER TABLE {table.Table} ALTER COLUMN {column.Name} TYPE {column.Type} USING {column.Name}::text::{column.Type}");

    /// <summary>
    /// Removes a column the new file no longer carries, so no tile carries a column of nothing but
    /// NULLs, the database's mark for an empty cell. IF EXISTS makes it safe to run twice. A view
    /// built on the column makes the database refuse, and the whole import roll back, rather than
    /// break the view.
    /// </summary>
    public static Query DropColumn(ImportTable table, QuotedName column) =>
        Query.Of($"ALTER TABLE {table.Table} DROP COLUMN IF EXISTS {column}");

    /// <summary>
    /// A spatial index, here of the GIST kind, sorts shapes by where they lie, so a tile finds its few
    /// places without reading the whole table (US-24 c1).
    /// </summary>
    public static Query CreateIndex(ImportTable table) =>
        Query.Of($"CREATE INDEX IF NOT EXISTS {table.Index} ON {table.Table} USING GIST ({table.Geometry})");

    /// <summary>
    /// Empties the table with DELETE, inside the import's one transaction, a group of changes the
    /// database makes all together or not at all, so readers keep seeing the old rows until the new
    /// ones are all in (US-24 c6).
    /// </summary>
    /// <remarks>
    /// TRUNCATE would be faster, but it locks the table, barring every reader, until the import ends,
    /// so every tile request would wait.
    /// </remarks>
    public static Query Empty(ImportTable table) => Query.Of($"DELETE FROM {table.Table}");

    /// <summary>
    /// One row. Every value travels as a bound parameter, in the column list's order. ST_MakePoint
    /// builds the point from longitude then latitude, and ST_SetSRID marks it as SRID 4326.
    /// </summary>
    public static Query Insert(ImportTable table, ImportRow row) =>
        Query.Of($"INSERT INTO {table.Table} ({table.Id}, {table.Geometry}{new NameList([.. table.Columns.Select(column => column.Name)], LeadingComma: true)}) VALUES ({row.Id}, ST_SetSRID(ST_MakePoint({row.Longitude}, {row.Latitude}), 4326){new ValueList(row.Values)})");

    /// <summary>
    /// ANALYZE gathers statistics, counts of the values a table holds, which the database reads to
    /// decide when the spatial index pays (US-24 c1).
    /// </summary>
    public static Query Analyse(ImportTable table) => Query.Of($"ANALYZE {table.Table}");

    /// <summary>
    /// The types of a table's columns, by name, from information_schema, the database's standard list
    /// of its tables and columns, so the import can see which changed. The schema and table names
    /// travel as bound parameters, since here they are values to compare, not names in the SQL.
    /// </summary>
    public static Query ColumnTypes(ImportTable table) =>
        Query.Of($"SELECT column_name AS name, data_type AS type FROM information_schema.columns WHERE table_schema = {table.Table.Schema.Raw} AND table_name = {table.Table.Table.Raw}");
}

/// <summary>The query the health check sends.</summary>
public static class HealthQueries
{
    /// <summary>
    /// The smallest question the database can answer: an answer proves the database is up and lets
    /// the service sign in (N-25).
    /// </summary>
    public static readonly Query Ping = Query.Of($"SELECT 1");
}
