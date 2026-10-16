using System.Collections.Immutable;
using CSharpFunctionalExtensions;

namespace Tessera.Etl;

/// <summary>
/// GDAL's arguments for each step, and the joining of its SQL into one <b><i>seed script</i></b>
/// for DbUp (ADR 0009).
/// Every function here is pure, so the tests check each choice without running GDAL.
/// </summary>
public static class SeedScript
{
    /// <summary>
    /// True when one copy of the source into a GeoPackage, a file with an index, saves time: the
    /// source is GeoJSON, which has no index, so GDAL reads the whole file again for each table it
    /// extracts, and the pipeline file names more than one table. On 90,000 places in nine tables,
    /// the copy took 4.5 seconds and cut each extract from 4.2 seconds to 0.2. A geodatabase has
    /// indexes of its own, so it gains nothing from a copy.
    /// </summary>
    public static bool CopyFirst(string source, int tables) =>
        tables > 1 && Path.GetExtension(source).ToLowerInvariant() is ".geojson" or ".json";

    /// <summary>
    /// ogr2ogr's arguments to copy the source, as it is, into a GeoPackage. No choice of the
    /// extract's is made here, so each table's extract reads the copy as it would the source.
    /// </summary>
    public static ImmutableArray<string> CopyArguments(string source, string geoPackage) =>
        ["-f", "GPKG", geoPackage, source];

    /// <summary>
    /// ogr2ogr's arguments to extract one table to GeoJSON, ready for the filters. Each choice:
    /// - EPSG:4326 puts every shape in longitude and latitude, whatever system the source uses.
    /// - -dim XY drops heights and measures, which a flat vector tile cannot carry.
    /// - CONVERT_TO_LINEAR turns true curves, such as a geodatabase's arcs, into short straight
    ///   lines, which PostGIS's tile functions read.
    /// - -resolveDomains writes a coded value's description, such as 'Museum', in place of its code.
    /// - The layer, when given, picks one feature class of a geodatabase; -where picks rows.
    /// </summary>
    public static ImmutableArray<string> ExtractArguments(string source, string geoJson, TablePlan table) =>
    [
        "-f", "GeoJSON", geoJson, source,
        .. table.Layer is { } layer ? [layer] : Array.Empty<string>(),
        "-t_srs", "EPSG:4326",
        "-dim", "XY",
        "-nlt", "CONVERT_TO_LINEAR",
        "-resolveDomains",
        .. table.Where is { } where ? ["-where", where] : Array.Empty<string>(),
    ];

    /// <summary>
    /// ogr2ogr's arguments to load one cleaned table as SQL. PGDump writes SQL rather than loading a
    /// database. Each choice:
    /// - PG_USE_COPY NO writes INSERT statements; DbUp sends a script as plain SQL and cannot feed COPY.
    /// - CREATE_SCHEMA OFF, because the script makes each schema once, at its head.
    /// - GEOMETRY_NAME geom names the shape column as the shipped script, 0001-macau-places.sql,
    ///   names it, so every table looks alike to a person reading the database.
    /// - FID fid: GDAL numbers the rows 1, 2, 3 in the file's order, and the number is the primary
    ///   key, which fixes the order of features in a tile (US-24 test 2).
    /// - EPSG:4326 again, so the script declares the column's <b><i>SRID</i></b>, the number that
    ///   names its coordinate system, as 4326 (US-24, criterion 2).
    /// - PROMOTE_TO_MULTI, for lines and areas, stores one part and many parts in one column type.
    /// - DROP_TABLE stays at GDAL's default, IF_EXISTS, so a new script replaces the table whole.
    /// GDAL writes the SQL to <paramref name="sqlFile"/>, in the work folder, so the program reads
    /// one table's SQL at a time rather than hold every table's until the end.
    /// </summary>
    public static ImmutableArray<string> LoadArguments(string geoJson, string sqlFile, TablePlan table)
    {
        var (schema, name) = SchemaAndName(table);
        return
        [
            "--config", "PG_USE_COPY", "NO",
            "-f", "PGDump", sqlFile, geoJson,
            "-nln", name,
            "-lco", $"SCHEMA={schema}",
            "-lco", "CREATE_SCHEMA=OFF",
            "-lco", "GEOMETRY_NAME=geom",
            "-lco", "FID=fid",
            "-t_srs", "EPSG:4326",
            .. table.Lines ? ["-nlt", "PROMOTE_TO_MULTI"] : Array.Empty<string>(),
        ];
    }

    /// <summary>
    /// The table's <b><i>schema</i></b> and name, which GDAL and the SQL take apart. The split is
    /// safe: the pipeline file's check has already refused any name not written schema.table.
    /// </summary>
    public static (string Schema, string Name) SchemaAndName(TablePlan table)
    {
        var parts = table.Table.Split('.');
        return (parts[0], parts[1]);
    }

    /// <summary>
    /// Joins the tables' SQL into one script: the <see cref="Head"/>, then each table's
    /// <see cref="Part"/>, each piece ending in a line break. The program writes the same pieces to a
    /// file one table at a time; this joins them in memory, for the tests.
    /// </summary>
    public static Result<string, string> Join(string pipelineFile, ImmutableArray<(TablePlan Table, string Sql)> parts)
    {
        var pieces = new List<string>(Head(pipelineFile, parts.Select(part => part.Table)));
        foreach (var (table, sql) in parts)
        {
            var part = Part(table, sql);
            if (part.IsFailure) return Result.Failure<string, string>(part.Error);
            pieces.AddRange(part.Value);
        }
        return Result.Success<string, string>(string.Concat(pieces.Select(piece => piece + "\n")));
    }

    /// <summary>
    /// The script's first lines: who made it, a warning never to change it, and each
    /// <b><i>schema</i></b> its tables need, made once.
    /// </summary>
    public static ImmutableArray<string> Head(string pipelineFile, IEnumerable<TablePlan> tables) =>
    [
        $"-- Made by 'npm run etl' from {pipelineFile}. DbUp runs this script once (ADR 0009).",
        "-- Never change its SQL after it has run: DbUp would not run it again. Write a new script instead.",
        .. tables.Select(table => SchemaAndName(table).Schema).Distinct().Select(schema => $"CREATE SCHEMA IF NOT EXISTS \"{schema}\";"),
    ];

    /// <summary>
    /// One table's piece of the script: its SQL, then fresh statistics, which let PostgreSQL plan
    /// the tile query well. DbUp runs the script in one transaction, so all of it takes effect or
    /// none does; GDAL's own BEGIN and COMMIT lines would end that transaction early, so they go. A
    /// table with no rows is refused (US-24, criterion 3).
    /// </summary>
    public static Result<ImmutableArray<string>, string> Part(TablePlan table, string sql)
    {
        if (!sql.Contains("INSERT INTO", StringComparison.Ordinal))
            return Result.Failure<ImmutableArray<string>, string>($"{table.Table}: GDAL wrote no rows.");
        var (schema, name) = SchemaAndName(table);
        return Result.Success<ImmutableArray<string>, string>([WithoutTransaction(sql), $"ANALYZE \"{schema}\".\"{name}\";"]);
    }

    // GDAL writes one BEGIN line before the table and one COMMIT line at the end. Only those two go:
    // a place's own text may hold the same words on a line of their own.
    private static string WithoutTransaction(string sql)
    {
        var lines = sql.TrimEnd().Split('\n');
        var begin = Array.IndexOf(lines, "BEGIN;");
        var commit = Array.LastIndexOf(lines, "COMMIT;");
        return string.Join('\n', lines.Where((_, index) => index != begin && index != commit));
    }
}
