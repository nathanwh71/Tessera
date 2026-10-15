using System.Diagnostics;

namespace Tessera.Etl.Tests;

/// <summary>
/// npm run inspect is a TypeScript script (scripts/inspect.ts, ADR 0002); its own tests check its
/// parts. This test checks the one thing they cannot: the starting file it prints for a real
/// <b><i>geodatabase</i></b> passes this <b><i>pipeline</i></b>'s own reader, so the
/// maintainer's first run does not fail.
/// </summary>
public sealed class InspectTests
{
    // The repository's root: the folder above the tests that holds Tessera.slnx.
    private static string Root()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "Tessera.slnx"))) folder = folder.Parent;
        return folder?.FullName ?? throw new InvalidOperationException("No Tessera.slnx above the tests.");
    }

    [Fact]
    public async Task Prints_a_starting_file_the_pipeline_accepts()
    {
        var folder = Directory.CreateTempSubdirectory("tessera-inspect-test-").FullName;
        var gdb = Path.Combine(folder, "places.gdb");
        var fixtures = Path.Combine(AppContext.BaseDirectory, "fixtures");
        string[] folders = [folder, fixtures];
        // .Value throws when GDAL fails, so a broken build of the geodatabase stops the test here,
        // with GDAL's own message, rather than later with a puzzling one.
        (await Gdal.Run(["-f", "OpenFileGDB", gdb, Path.Combine(fixtures, "cafes.geojson"), "-nln", "Cafes"], folders)).Value.ToString();
        (await Gdal.Run(["-f", "OpenFileGDB", "-update", gdb, Path.Combine(fixtures, "parks.geojson"), "-nln", "Parks", "-nlt", "MULTIPOLYGON"], folders)).Value.ToString();

        var start = new ProcessStartInfo("node", [Path.Combine(Root(), "scripts", "inspect.ts"), gdb])
        {
            WorkingDirectory = Root(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(start)!;
        var printed = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, process.ExitCode);
        Assert.Contains("7 rows of PointZ", printed, StringComparison.Ordinal);
        Assert.Contains("2 rows of MultiPolygon, in EPSG:4326", printed, StringComparison.Ordinal);
        var (pipeline, tables) = PipelineFile.Read(printed[printed.IndexOf('{', StringComparison.Ordinal)..], FilterTests.Macau).Value;
        Assert.Equal(["places.cafes", "places.parks"], pipeline.Tables.Select(table => table.Table));
        Assert.Equal(["Cafes", "Parks"], pipeline.Tables.Select(table => table.Layer));
        Assert.Contains("repairShapes", tables[1].Filters);
        Assert.Matches(@"^db/seed/\d{4}-places\.sql$", pipeline.Output);
    }
}
