namespace Tessera.Etl.Tests;

/// <summary>The pipeline file, the stop rules, the log, and GDAL's arguments: each checked without running GDAL.</summary>
public sealed class PipelineTests
{
    private static readonly Area Macau = new(113.52, 22.10, 113.61, 22.22);

    private const string Good = """
        {
          "source": "data/macau.gdb",
          "output": "db/seed/0002-macau-gdb.sql",
          "tables": [
            { "table": "food.cafes", "layer": "Cafes", "filters": [
              { "filter": "insideArea", "onFail": "stop" },
              { "filter": "requireFields", "fields": ["name"] } ] }
          ]
        }
        """;

    [Fact]
    public void Reads_a_pipeline_and_takes_the_area_from_the_settings()
    {
        var pipeline = PipelineFile.Read(Good, Macau).Value;

        Assert.Equal(Macau, pipeline.Area);
        var table = Assert.Single(pipeline.Tables);
        Assert.Equal(("food.cafes", "Cafes"), (table.Table, table.Layer));
        Assert.Equal([OnFail.Stop, OnFail.Drop], table.Filters.Select(step => step.OnFail));
    }

    [Fact]
    public void Refuses_a_misspelt_setting()
    {
        var read = PipelineFile.Read(Good.Replace("\"fields\"", "\"feilds\"", StringComparison.Ordinal), Macau);

        Assert.Contains("feilds", Assert.Single(read.Error), StringComparison.Ordinal);
    }

    [Fact]
    public void Names_every_fault_at_once()
    {
        var bad = """
            { "source": "x.gdb", "output": "db/seed/parks.sql", "tables": [
              { "table": "Food.Cafes", "filters": [ { "filter": "dropDuplicates" }, { "filter": "dropNearDuplicates", "field": "name" } ] },
              { "table": "outdoors.parks", "filters": [ { "filter": "insideArea" } ] } ] }
            """;

        var problems = PipelineFile.Read(bad, areaFromSettings: null).Error;

        Assert.Equal(5, problems.Length);
        Assert.Contains(problems, problem => problem.StartsWith("output:", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("schema.table", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("'dropDuplicates' is no filter", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("metres above 0", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("insideArea needs an area", StringComparison.Ordinal));
    }

    private static TablePlan Cafes(StopWhen? rules = null, params FilterStep[] steps) => new("food.cafes", "Cafes", null, false, [.. steps], rules);

    private static ImmutableArray<Row> Ten(int named) =>
        [.. Enumerable.Range(1, 10).Select(number => FilterTests.Place(number, 113.55, 22.19, ("name", number <= named ? $"Place {number}" : null)))];

    [Fact]
    public void Stops_a_table_that_drops_more_than_ten_per_cent_by_default()
    {
        var report = Cleaning.Clean(Cafes(null, new FilterStep("requireFields", Fields: ["name"])), Ten(named: 8), Macau, pipelineRules: null);

        Assert.Equal("food.cafes: 2 of 10 rows were dropped (20 %), more than the 10 % allowed.", report.Stop);
    }

    [Fact]
    public void Lets_a_table_set_its_own_rules()
    {
        var steps = new FilterStep("requireFields", Fields: ["name"]);

        Assert.Null(Cleaning.Clean(Cafes(new StopWhen(DroppedOverPercent: 25), steps), Ten(named: 8), Macau, null).Stop);
        Assert.Equal("food.cafes: only 8 rows remain, fewer than the 9 required.",
            Cleaning.Clean(Cafes(new StopWhen(DroppedOverPercent: 50, RowsUnder: 9), steps), Ten(named: 8), Macau, null).Stop);
    }

    [Fact]
    public void Runs_filters_in_order_and_ends_at_the_first_stop()
    {
        var table = Cafes(new StopWhen(DroppedOverPercent: 100),
            new FilterStep("requireFields", OnFail.Stop, Fields: ["name"]),
            new FilterStep("tidyText"));

        var report = Cleaning.Clean(table, Ten(named: 9), Macau, null);

        Assert.Equal("requireFields stopped the run at row 10: name is missing.", report.Stop);
        Assert.Single(report.Steps);
    }

    [Fact]
    public void Logs_each_filter_and_each_row_it_touched()
    {
        var pipeline = PipelineFile.Read(Good, Macau).Value;
        var report = Cleaning.Clean(Cafes(new StopWhen(DroppedOverPercent: 50), new FilterStep("requireFields", Fields: ["name"])), Ten(named: 9), Macau, null);

        var log = Cleaning.Log("data/macau.pipeline.json", pipeline, [report], "wrote it.", new DateTimeOffset(2026, 10, 15, 13, 0, 0, TimeSpan.FromHours(8)));

        Assert.Contains("Tessera ETL log, 2026-10-15 13:00:00 +08:00", log, StringComparison.Ordinal);
        Assert.Contains("  requireFields: kept 9, fixed 0, dropped 1", log, StringComparison.Ordinal);
        Assert.Contains("  Row 10: dropped by requireFields: name is missing", log, StringComparison.Ordinal);
        Assert.EndsWith("Result: wrote it.\n", log.ReplaceLineEndings("\n"), StringComparison.Ordinal);
    }

    [Fact]
    public void Extracts_one_layer_flat_and_in_longitude_and_latitude()
    {
        var args = SeedScript.ExtractArguments("/data/macau.gdb", "/work/cafes.geojson", Cafes() with { Where = "kind = 'cafe'" });

        Assert.Equal(["-f", "GeoJSON", "/work/cafes.geojson", "/data/macau.gdb", "Cafes"], args.Take(5));
        Assert.Contains("-resolveDomains", args);
        Assert.Equal(["-where", "kind = 'cafe'"], args.TakeLast(2));
        Assert.Equal("XY", args[args.IndexOf("-dim") + 1]);
    }

    [Fact]
    public void Loads_lines_and_areas_as_multiple_parts_and_points_as_they_are()
    {
        Assert.DoesNotContain("PROMOTE_TO_MULTI", SeedScript.LoadArguments("/work/c.geojson", Cafes()));
        Assert.Contains("PROMOTE_TO_MULTI", SeedScript.LoadArguments("/work/p.geojson", Cafes() with { Lines = true }));
        Assert.Equal(["--config", "PG_USE_COPY", "NO"], SeedScript.LoadArguments("/work/c.geojson", Cafes()).Take(3));
    }

    [Fact]
    public void Joins_the_tables_without_GDALs_own_transaction()
    {
        var sql = string.Join('\n', "BEGIN;", "INSERT INTO \"food\".\"cafes\" VALUES ('COMMIT;", "COMMIT;", "');", "COMMIT;", "");

        var lines = SeedScript.Join("p.json", [(Cafes(), sql)]).Value.Split('\n');

        Assert.DoesNotContain("BEGIN;", lines);
        Assert.Single(lines, line => line == "COMMIT;");
        Assert.Contains("CREATE SCHEMA IF NOT EXISTS \"food\";", lines);
        Assert.Contains("ANALYZE \"food\".\"cafes\";", lines);
    }

    // GDAL writes a text field named id as the feature's own id, outside its properties.
    [Fact]
    public void Reads_a_features_own_id_as_the_id_column()
    {
        var json = """{"type":"FeatureCollection","features":[{"type":"Feature","id":"cafes-001","properties":{"name":"A"},"geometry":null}]}""";

        var (rows, columns) = GeoJson.Read(json);

        Assert.Equal(["id", "name"], columns);
        Assert.Equal("cafes-001", rows[0].Value("id"));
    }

    [Fact]
    public void Keeps_columns_in_order_and_values_by_kind_through_GeoJSON()
    {
        var json = """{"type":"FeatureCollection","features":[{"type":"Feature","properties":{"name":"A","floors":3,"height":2.5,"open":true,"note":null},"geometry":{"type":"Point","coordinates":[113.55,22.19]}}]}""";

        var (rows, columns) = GeoJson.Read(json);
        var (again, _) = GeoJson.Read(GeoJson.Write(rows, columns));

        Assert.Equal(["name", "floors", "height", "open", "note"], columns);
        Assert.Equal(rows[0].Values, again[0].Values);
        Assert.Equal(3L, Assert.IsType<long>(again[0].Value("floors")));
        Assert.Equal(new Coordinate(113.55, 22.19), again[0].Shape!.Coordinate);
    }
}
