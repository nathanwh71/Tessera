using System.Diagnostics;

namespace Tessera.Etl.Tests;

/// <summary>npm run inspect: reading ogrinfo's description, and the starting pipeline file it prints.</summary>
public sealed class InspectionTests
{
    // What GDAL 3.8's `ogrinfo -json -so -al` writes for a geodatabase of cafés and parks, cut down.
    private const string Described = """
        { "layers": [
          { "name": "Cafes", "featureCount": 7, "fields": [ { "name": "id" }, { "name": "Name" } ],
            "geometryFields": [ { "name": "SHAPE", "type": "PointZ", "coordinateSystem": { "projjson": { "id": { "authority": "EPSG", "code": 4979 } } } } ] },
          { "name": "Bus Stops 2026", "featureCount": 1200, "fields": [ { "name": "code" } ],
            "geometryFields": [ { "name": "SHAPE", "type": "MultiLineString", "coordinateSystem": null } ] } ] }
        """;

    [Fact]
    public void Reads_each_layers_count_shape_system_and_fields()
    {
        var layers = Inspection.Read(Described);

        Assert.Equal(("Cafes", 7L, "PointZ", (string?)"EPSG:4979", false), (layers[0].Name, layers[0].Count, layers[0].Shape, layers[0].System, layers[0].Lines));
        Assert.Equal(["id", "Name"], layers[0].Fields);
        Assert.Equal((1200L, "MultiLineString", (string?)null, true), (layers[1].Count, layers[1].Shape, layers[1].System, layers[1].Lines));
        Assert.Contains("1,200 rows of MultiLineString, in an unknown system", Inspection.Summary("x.gdb", layers), StringComparison.Ordinal);
    }

    [Fact]
    public void Makes_plain_table_names_and_the_next_script_name()
    {
        Assert.Equal(["bus_stops_2026", "_2026_stops", "places"], new[] { "Bus Stops 2026", "2026 Stops", "!!" }.Select(Inspection.Plain));
        Assert.Equal("0003-macau.sql", Inspection.NextScript(["db/seed/0001-macau-places.sql", "db/seed/0002-parks.sql"], "data/Macau.gdb/"));
        Assert.Equal("0001-parks.sql", Inspection.NextScript([], "parks.geojson"));
    }

    // The starting file must pass the pipeline's own reader, or the maintainer's first run fails.
    [Fact]
    public void Writes_a_starting_file_the_pipeline_accepts()
    {
        var starter = Inspection.Starter("data/macau.gdb", "db/seed/0002-macau.sql", Inspection.Read(Described), split: null);

        var (pipeline, tables) = PipelineFile.Read(starter, FilterTests.Macau).Value;
        Assert.Equal(["places.cafes", "places.bus_stops_2026"], tables.Select(table => table.Table.Table));
        Assert.Equal(("Cafes", false), (pipeline.Tables[0].Layer, pipeline.Tables[0].Lines));
        Assert.True(pipeline.Tables[1].Lines);
        Assert.Contains("repairShapes", tables[1].Filters);
        Assert.Contains("requireFields", tables[0].Filters);
    }

    [Fact]
    public void Splits_one_layer_into_a_table_for_each_value()
    {
        var one = Inspection.Read(Described)[..1];

        var starter = Inspection.Starter("data/macau-pois.geojson", "db/seed/0002-macau.sql", one, ("kind", ["cafe", "O'Brien's"]));

        var (pipeline, _) = PipelineFile.Read(starter, FilterTests.Macau).Value;
        Assert.Equal(["places.cafe", "places.o_brien_s"], pipeline.Tables.Select(table => table.Table));
        Assert.Equal(["\"kind\" = 'cafe'", "\"kind\" = 'O''Brien''s'"], pipeline.Tables.Select(table => table.Where));
        Assert.All(pipeline.Tables, table => Assert.Null(table.Layer));
    }

    [Fact]
    public async Task Inspects_a_geodatabase_and_prints_a_file_that_runs()
    {
        var folder = Directory.CreateTempSubdirectory("tessera-inspect-test-").FullName;
        var gdb = Path.Combine(folder, "places.gdb");
        var fixtures = Path.Combine(AppContext.BaseDirectory, "fixtures");
        (await Gdal.Run(["-f", "OpenFileGDB", gdb, Path.Combine(fixtures, "parks.geojson"), "-nln", "Parks", "-nlt", "MULTIPOLYGON"], [folder, fixtures])).Value.ToString();

        var start = new ProcessStartInfo("dotnet", [Path.Combine(AppContext.BaseDirectory, "Tessera.Etl.dll"), "inspect", gdb])
        {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
        };
        using var process = Process.Start(start)!;
        var printed = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, process.ExitCode);
        Assert.Contains("2 rows of MultiPolygon, in EPSG:4326", printed, StringComparison.Ordinal);
        var starter = printed[printed.IndexOf('{', StringComparison.Ordinal)..];
        var (pipeline, _) = PipelineFile.Read(starter, FilterTests.Macau).Value;
        Assert.Equal("0001-places.sql", Path.GetFileName(pipeline.Output));
        Assert.Equal("places.parks", Assert.Single(pipeline.Tables).Table);
    }
}
