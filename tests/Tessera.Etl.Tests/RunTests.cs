using System.Diagnostics;

namespace Tessera.Etl.Tests;

/// <summary>
/// Whole runs of the pipeline, from a file geodatabase that GDAL builds from the fixtures: seven
/// cafés with one fault each, and two parks, one an area whose edge crosses itself. The runs use the
/// machine's ogr2ogr, or GDAL's Docker image when it has none.
/// </summary>
public sealed class RunTests
{
    private static async Task<string> Geodatabase(string folder)
    {
        var gdb = Path.Combine(folder, "places.gdb");
        var fixtures = Path.Combine(AppContext.BaseDirectory, "fixtures");
        string[] folders = [folder, fixtures];
        (await Gdal.Run(["-f", "OpenFileGDB", gdb, Path.Combine(fixtures, "cafes.geojson"), "-nln", "Cafes"], folders)).Value.ToString();
        (await Gdal.Run(["-f", "OpenFileGDB", "-update", gdb, Path.Combine(fixtures, "parks.geojson"), "-nln", "Parks", "-nlt", "MULTIPOLYGON"], folders)).Value.ToString();
        return gdb;
    }

    private static string Pipeline(string gdb, string cafesFilters, string parksFilters = """{ "filter": "repairShapes" }""") => $$"""
        {
          "source": "{{gdb}}",
          "output": "seed/0002-test-places.sql",
          "area": { "minLongitude": 113.52, "minLatitude": 22.10, "maxLongitude": 113.61, "maxLatitude": 22.22 },
          "stopWhen": { "droppedOverPercent": 60 },
          "tables": [
            { "table": "food.cafes", "layer": "Cafes", "filters": [ {{cafesFilters}} ] },
            { "table": "outdoors.parks", "layer": "Parks", "lines": true, "filters": [ {{parksFilters}} ] }
          ]
        }
        """;

    // Runs the built tool in `folder`, as `npm run etl` would, and returns its exit code.
    private static async Task<int> Etl(string folder, string pipeline)
    {
        File.WriteAllText(Path.Combine(folder, "pipeline.json"), pipeline);
        var start = new ProcessStartInfo("dotnet", [Path.Combine(AppContext.BaseDirectory, "Tessera.Etl.dll"), "pipeline.json"])
        {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(start)!;
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        return process.ExitCode;
    }

    [Fact]
    public async Task Cleans_a_geodatabase_into_one_script_and_logs_each_row()
    {
        var folder = Directory.CreateTempSubdirectory("tessera-etl-test-").FullName;
        var gdb = await Geodatabase(folder);

        var code = await Etl(folder, Pipeline(gdb, """
            { "filter": "dropEmptyShapes" }, { "filter": "swapCoordinates" }, { "filter": "insideArea" },
            { "filter": "tidyText" }, { "filter": "requireFields", "fields": ["name"] },
            { "filter": "dropDuplicateIds", "field": "id" }, { "filter": "dropNearDuplicates", "field": "name", "metres": 25 }
            """));

        Assert.Equal(0, code);
        var script = File.ReadAllText(Path.Combine(folder, "seed/0002-test-places.sql"));
        // Of the seven cafés, the swapped one is mended and kept; four are dropped.
        Assert.Equal(3, script.Split('\n').Count(line => line.StartsWith("INSERT INTO \"food\".\"cafes\"", StringComparison.Ordinal)));
        Assert.Contains("'Café Uno'", script, StringComparison.Ordinal);
        Assert.Contains("'Swapped Café'", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Far Away", script, StringComparison.Ordinal);
        Assert.Contains("'MULTIPOLYGON',2", script, StringComparison.Ordinal);
        Assert.DoesNotContain("BEGIN;", script, StringComparison.Ordinal);

        var log = File.ReadAllText(Path.Combine(folder, "seed-logs/0002-test-places.log"));
        Assert.Contains("Row 2: fixed by swapCoordinates: swapped longitude and latitude", log, StringComparison.Ordinal);
        Assert.Contains("Row 5: dropped by dropNearDuplicates: its name matches row 1, 15 m away", log, StringComparison.Ordinal);
        Assert.Contains("Row 1: fixed by repairShapes", log, StringComparison.Ordinal);
        Assert.Contains("Result: wrote seed/0002-test-places.sql: 2 tables, 5 rows.", log, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stops_at_a_bad_row_writes_no_script_and_logs_why()
    {
        var folder = Directory.CreateTempSubdirectory("tessera-etl-test-").FullName;
        var gdb = await Geodatabase(folder);

        var code = await Etl(folder, Pipeline(gdb, """{ "filter": "insideArea", "onFail": "stop" }"""));

        Assert.Equal(1, code);
        Assert.False(File.Exists(Path.Combine(folder, "seed/0002-test-places.sql")));
        var log = File.ReadAllText(Path.Combine(folder, "seed-logs/0002-test-places.log"));
        // Row 2 has its longitude and latitude swapped, and nothing here swaps it back.
        Assert.Contains("Row 2: stopped by insideArea: its shape lies outside the area", log, StringComparison.Ordinal);
        Assert.Contains("Result: stopped. insideArea stopped the run at row 2", log, StringComparison.Ordinal);
    }
}
