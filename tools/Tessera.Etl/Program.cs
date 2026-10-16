using System.Collections.Immutable;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Tessera.Etl;

// Turns a file of places into a seed script for DbUp, cleaning it on the way (ADR 0009, US-24,
// US-36):
//
//   npm run etl -- data/macau.pipeline.json
//
// Extract: GDAL's ogr2ogr reads each table from the source, such as a geodatabase's feature class,
//          into GeoJSON, in longitude and latitude.
// Transform: a chain of steps cleans each table's rows: the pipeline file's filters, in the order
//          it lists them, then the stop rules (Pipes.cs, Steps.cs).
// Load:    ogr2ogr writes the cleaned rows as SQL, and the tables join into one numbered script.
//
// Every run that passes its checks writes a log beside the scripts' folder, in db/seed-logs/, of
// what each filter did to each row; a wrong command, pipeline file, source or script name stops
// before that, with no log, since no row has been read. A run stops, and writes no script, when a
// filter set to stop finds a bad row, or when a table breaks its stop rules. It ends with an exit
// code, so npm and other scripts can tell the outcomes apart: 0 when it wrote the script, 1 when
// it stopped or failed, 2 when a check fails before any row is read.

if (args is not [var pipelineFile])
    return Fail(2, "Usage: npm run etl -- PIPELINE.json");
if (!File.Exists(pipelineFile))
    return Fail(2, $"{pipelineFile}: no such file.");

var read = PipelineFile.Read(File.ReadAllText(pipelineFile), AreaFromSettings("tessera.json"));
if (read.IsFailure)
    return Fail(2, [.. read.Error]);
var (pipeline, tables) = read.Value;

// A full path, since Gdal mounts each folder at the same path inside Docker, and a relative path
// would point elsewhere in there.
var source = Path.GetFullPath(pipeline.Source);
if (!File.Exists(source) && !Directory.Exists(source))
    return Fail(2, $"{pipeline.Source}: no such file or folder.");
// DbUp runs a script once, known by its name, and never again; a changed script would change nothing.
if (File.Exists(pipeline.Output))
    return Fail(2, $"{pipeline.Output} already exists. Give new data a new script, with the next number.");

var logFile = Path.Combine($"{Path.GetDirectoryName(pipeline.Output)}-logs", Path.ChangeExtension(Path.GetFileName(pipeline.Output), ".log"));
// A fresh folder for the GeoJSON between the steps, kept out of the repository and deleted at the
// end, however the run ends.
var work = Directory.CreateTempSubdirectory("tessera-etl-").FullName;
// The folders GDAL's Docker image must see: the source's, to read it, and the work folder, for the
// GeoJSON. The SQL comes back on standard output, so the scripts' folder needs no mount.
var folders = new[] { Path.GetDirectoryName(source)!, work };
// Each table's counts and log lines, for the log; its rows go once its SQL is written, so the run
// holds one table's rows at a time, not every table's.
var cleaned = ImmutableArray.CreateBuilder<(CleaningPlan, Outcome)>();
var parts = ImmutableArray.CreateBuilder<(TablePlan, string)>();
try
{
    // GeoJSON has no index, so for a pipeline of several tables GDAL copies it once into a
    // GeoPackage in the work folder, and each table's extract reads the copy (SeedScript.CopyFirst).
    var reading = source;
    if (SeedScript.CopyFirst(source, tables.Length))
    {
        Console.WriteLine($"{pipeline.Source}: copying into a GeoPackage, which each table reads...");
        reading = Path.Combine(work, "source.gpkg");
        var copy = await Gdal.Run(SeedScript.CopyArguments(source, reading), folders);
        if (copy.IsFailure)
            return Finish(1, $"{pipeline.Source}: the copy failed. {copy.Error}");
    }

    foreach (var plan in tables)
    {
        var table = plan.Table;
        Console.WriteLine($"{table.Table}: extracting{(table.Layer is { } layer ? $" {layer}" : "")}...");
        var extracted = Path.Combine(work, $"{table.Table}.extracted.geojson");
        var extract = await Gdal.Run(SeedScript.ExtractArguments(reading, extracted, table), folders);
        if (extract.IsFailure)
            return Finish(1, $"{table.Table}: extraction failed. {extract.Error}");

        var geoJson = GeoJson.Read(File.ReadAllText(extracted));
        if (geoJson.IsFailure)
            return Finish(1, $"{table.Table}: GDAL's GeoJSON could not be read. {geoJson.Error}");
        var (rows, columns) = geoJson.Value;
        // The whole transform step: the table's chain of filters and stop rules, run on its rows.
        var done = plan.Chain.Run(Cleaning.Start(table, rows));
        cleaned.Add((plan, done.Outcome));
        if (done.Stop.TryGetValue(out var stop))
            return Finish(1, $"stopped. {stop} No script written.");

        Console.WriteLine($"{table.Table}: read {done.Read}, kept {done.Rows.Length}, dropped {done.Dropped}.");
        var cleanFile = Path.Combine(work, $"{table.Table}.cleaned.geojson");
        File.WriteAllText(cleanFile, GeoJson.Write(done.Rows, columns));
        var load = await Gdal.Run(SeedScript.LoadArguments(cleanFile, table), folders);
        if (load.IsFailure)
            return Finish(1, $"{table.Table}: loading failed. {load.Error}");
        parts.Add((table, load.Value));
    }

    var script = SeedScript.Join(pipelineFile, parts.ToImmutable());
    if (script.IsFailure)
        return Finish(1, script.Error);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pipeline.Output))!);
    File.WriteAllText(pipeline.Output, script.Value);
    var kept = cleaned.Sum(table => table.Item2.Kept);
    return Finish(0, $"wrote {pipeline.Output}: {parts.Count} tables, {kept} rows. Read it, then commit it; 'Tessera seed' runs it once.");
}
finally
{
    Directory.Delete(work, recursive: true);
}

// Writes the log, prints the result and returns the exit code. Every success, stop and
// failure after the checks comes through here, so a stopped run leaves a log as surely as a
// finished one (US-36, criterion 4).
int Finish(int code, string result)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(logFile))!);
    File.WriteAllText(logFile, Report.Log(pipelineFile, pipeline, cleaned.ToImmutable(), result, DateTimeOffset.Now));
    (code == 0 ? Console.Out : Console.Error).WriteLine($"{(code == 0 ? "Done" : "Stopped")}: {result}");
    Console.WriteLine($"The log is in {logFile}.");
    return code;
}

// Prints the problems and returns the exit code, with no log: Fail serves the checks before any
// row is read, so a log would have nothing to report.
static int Fail(int code, params string[] problems)
{
    foreach (var problem in problems) Console.Error.WriteLine(problem);
    return code;
}

// The area's box from tessera.json's Area section, when the file is here, so a pipeline need not
// repeat it; none when the file or its Area section is missing.
static Maybe<Area> AreaFromSettings(string settingsFile)
{
    if (!File.Exists(settingsFile)) return Maybe<Area>.None;
    using var settings = JsonDocument.Parse(File.ReadAllText(settingsFile));
    if (!settings.RootElement.TryGetProperty("Area", out var area)) return Maybe<Area>.None;
    double Degrees(string key) => area.GetProperty(key).GetDouble();
    return new Area(Degrees("MinLongitude"), Degrees("MinLatitude"), Degrees("MaxLongitude"), Degrees("MaxLatitude"));
}
