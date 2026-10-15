using System.Collections.Immutable;
using System.Text.Json;
using Tessera.Etl;

// Turns a file of places into a seed script for DbUp, cleaning it on the way (ADR 0009, US-24):
//
//   npm run etl -- data/macau.pipeline.json
//
// Extract: GDAL's ogr2ogr reads each table from the source, such as a geodatabase's feature class,
//          into GeoJSON, in longitude and latitude.
// Transform: the pipeline file's filters clean each table's rows, in the order it lists them.
// Load:    ogr2ogr writes the cleaned rows as SQL, and the tables join into one numbered script.
//
// Every run writes a log beside the scripts' folder, in db/seed-logs/, of what each filter did to
// each row. A run stops, and writes no script, when a filter set to stop finds a bad row, or when
// a table breaks its stop rules. It ends with an exit code: 0 when it wrote the script, 1 when it
// stopped or failed, 2 when the command or the pipeline file is wrong.

if (args is ["inspect", var inspected, .. var options])
    return await Inspect(inspected, options);
if (args is not [var pipelineFile])
    return Fail(2, "Usage: npm run etl -- PIPELINE.json", "       npm run inspect -- SOURCE [--split FIELD]");
if (!File.Exists(pipelineFile))
    return Fail(2, $"{pipelineFile}: no such file.");

var read = PipelineFile.Read(File.ReadAllText(pipelineFile), AreaFromSettings("tessera.json"));
if (read.IsFailure)
    return Fail(2, [.. read.Error]);
var (pipeline, tables) = read.Value;

var source = Path.GetFullPath(pipeline.Source);
if (!File.Exists(source) && !Directory.Exists(source))
    return Fail(2, $"{pipeline.Source}: no such file or folder.");
// DbUp runs a script once, known by its name, and never again; a changed script would change nothing.
if (File.Exists(pipeline.Output))
    return Fail(2, $"{pipeline.Output} already exists. Give new data a new script, with the next number.");

var logFile = Path.Combine($"{Path.GetDirectoryName(pipeline.Output)}-logs", Path.ChangeExtension(Path.GetFileName(pipeline.Output), ".log"));
var work = Directory.CreateTempSubdirectory("tessera-etl-").FullName;
var folders = new[] { Path.GetDirectoryName(source)!, work };
var reports = ImmutableArray.CreateBuilder<TableReport>();
var parts = ImmutableArray.CreateBuilder<(TablePlan, string)>();
try
{
    foreach (var plan in tables)
    {
        var table = plan.Table;
        Console.WriteLine($"{table.Table}: extracting{(table.Layer is { } layer ? $" {layer}" : "")}...");
        var extracted = Path.Combine(work, $"{table.Table}.extracted.geojson");
        var extract = await Gdal.Run(SeedScript.ExtractArguments(source, extracted, table), folders);
        if (extract.IsFailure)
            return Finish(1, $"{table.Table}: extraction failed. {extract.Error}");

        var (rows, columns) = GeoJson.Read(File.ReadAllText(extracted));
        var report = Cleaning.Clean(plan, rows, pipeline.StopWhen);
        reports.Add(report);
        if (report.Stop is { } stop)
            return Finish(1, $"stopped. {stop} No script written.");

        Console.WriteLine($"{table.Table}: read {report.Read}, kept {report.Kept.Length}, dropped {report.Dropped}.");
        var cleaned = Path.Combine(work, $"{table.Table}.cleaned.geojson");
        File.WriteAllText(cleaned, GeoJson.Write(report.Kept, columns));
        var load = await Gdal.Run(SeedScript.LoadArguments(cleaned, table), folders);
        if (load.IsFailure)
            return Finish(1, $"{table.Table}: loading failed. {load.Error}");
        parts.Add((table, load.Value));
    }

    var script = SeedScript.Join(pipelineFile, parts.ToImmutable());
    if (script.IsFailure)
        return Finish(1, script.Error);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pipeline.Output))!);
    File.WriteAllText(pipeline.Output, script.Value);
    var kept = reports.Sum(report => report.Kept.Length);
    return Finish(0, $"wrote {pipeline.Output}: {parts.Count} tables, {kept} rows. Read it, then commit it; 'Tessera seed' runs it once.");
}
finally
{
    Directory.Delete(work, recursive: true);
}

// Writes the log, prints the result and returns the exit code.
int Finish(int code, string result)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(logFile))!);
    File.WriteAllText(logFile, Cleaning.Log(pipelineFile, pipeline, reports.ToImmutable(), result, DateTimeOffset.Now));
    (code == 0 ? Console.Out : Console.Error).WriteLine($"{(code == 0 ? "Done" : "Stopped")}: {result}");
    Console.WriteLine($"The log is in {logFile}.");
    return code;
}

// Describes a source's layers, and prints a starting pipeline file for it (npm run inspect).
// --split FIELD makes one table for each value of FIELD, as the Macau places need for 'layer'.
static async Task<int> Inspect(string inspected, string[] options)
{
    var source = Path.GetFullPath(inspected);
    if (!File.Exists(source) && !Directory.Exists(source))
        return Fail(2, $"{inspected}: no such file or folder.");
    string[] folders = [Path.GetDirectoryName(source)!];
    var described = await Gdal.Info(["-json", "-so", "-al", source], folders);
    if (described.IsFailure)
        return Fail(1, described.Error);
    var layers = Inspection.Read(described.Value);
    Console.WriteLine(Inspection.Summary(inspected, layers));

    (string, ImmutableArray<string>)? split = null;
    if (options is ["--split", var field])
    {
        if (layers.Length != 1)
            return Fail(2, "--split needs a source with one layer.");
        if (!layers[0].Fields.Contains(field))
            return Fail(2, $"--split: {layers[0].Name} has no field '{field}'.");
        // OGR SQL, GDAL's own small SQL, lists each value of the field once.
        var values = await Gdal.Run(["-f", "CSV", "/vsistdout/", source, "-sql", $"SELECT DISTINCT \"{field}\" FROM \"{layers[0].Name}\""], folders);
        if (values.IsFailure)
            return Fail(1, values.Error);
        split = (field, [.. values.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(1).Select(value => value.Trim('"')).Where(value => value.Length > 0).Order(StringComparer.Ordinal)]);
    }
    else if (options.Length > 0)
        return Fail(2, "Usage: npm run inspect -- SOURCE [--split FIELD]");

    var existing = Directory.Exists("db/seed") ? Directory.GetFiles("db/seed", "*.sql") : [];
    var output = $"db/seed/{Inspection.NextScript(existing, inspected)}";
    Console.WriteLine("A starting pipeline file. Save it beside the data, such as data/NAME.pipeline.json,");
    Console.WriteLine("then set each table's schema and name, and its filters, before npm run etl:");
    Console.WriteLine();
    Console.WriteLine(Inspection.Starter(inspected, output, layers, split));
    return 0;
}

static int Fail(int code, params string[] problems)
{
    foreach (var problem in problems) Console.Error.WriteLine(problem);
    return code;
}

// The area's box from tessera.json's Area section, when the file is here, so a pipeline need not repeat it.
static Area? AreaFromSettings(string settingsFile)
{
    if (!File.Exists(settingsFile)) return null;
    using var settings = JsonDocument.Parse(File.ReadAllText(settingsFile));
    if (!settings.RootElement.TryGetProperty("Area", out var area)) return null;
    double Degrees(string key) => area.GetProperty(key).GetDouble();
    return new Area(Degrees("MinLongitude"), Degrees("MinLatitude"), Degrees("MaxLongitude"), Degrees("MaxLatitude"));
}
