using Tessera.Etl.Filters;

namespace Tessera.Etl.Tests;

/// <summary>
/// The <b><i>pipeline file</i></b>, the <b><i>stop rules</i></b>, the log, and GDAL's arguments:
/// each checked without running GDAL, so they run fast and on any machine (US-24, test 5).
/// </summary>
public sealed class PipelineTests
{
    // Macau's box, as tessera.json holds it, standing in for the settings file's area.
    private static readonly Area Macau = new(113.52, 22.10, 113.61, 22.22);

    // A pipeline file with no fault. Each refusal test below changes one key in it, so the one
    // change is the only fault the reader can find.
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
        var (pipeline, tables) = PipelineFile.Read(Good, Macau).Value;

        Assert.Equal(Macau, pipeline.Area);
        var table = Assert.Single(tables);
        Assert.Equal(("food.cafes", "Cafes"), (table.Table.Table, table.Table.Layer));
        Assert.Equal(["insideArea", "requireFields"], table.Filters);
        // The chain stops at a place outside the area, since the file says "onFail": "stop".
        Assert.NotNull(table.Chain([FilterTests.Place(1, 0.5, 51.5, ("name", "Far"))]).Stop);
    }

    // Each filter reads its own settings, so one that belongs to another filter is refused.
    [Fact]
    public void Refuses_a_setting_that_belongs_to_another_filter()
    {
        var read = PipelineFile.Read(Good.Replace("{ \"filter\": \"insideArea\", \"onFail\": \"stop\" }", "{ \"filter\": \"insideArea\", \"metres\": 25 }", StringComparison.Ordinal), Macau);

        Assert.Equal("food.cafes: insideArea: 'metres' is not a setting; its settings are onFail.", Assert.Single(read.Error));
    }

    [Fact]
    public void Refuses_a_filter_that_lacks_a_setting_it_needs()
    {
        var read = PipelineFile.Read(Good.Replace(", \"fields\": [\"name\"]", "", StringComparison.Ordinal), Macau);

        Assert.Equal("food.cafes: requireFields: needs fields.", Assert.Single(read.Error));
    }

    [Fact]
    public void Refuses_a_misspelt_setting()
    {
        var read = PipelineFile.Read(Good.Replace("\"fields\"", "\"feilds\"", StringComparison.Ordinal), Macau);

        Assert.Equal(
            ["food.cafes: requireFields: 'feilds' is not a setting; its settings are fields, onFail.", "food.cafes: requireFields: needs fields."],
            read.Error);
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
        Assert.Contains("Food.Cafes: dropNearDuplicates: needs metres.", problems);
        Assert.Contains(problems, problem => problem.Contains("insideArea: needs an area", StringComparison.Ordinal));
    }

    // One table for the tests below, with the stop rules a test gives it.
    private static TablePlan Cafes(StopWhen? rules = null) => new("food.cafes", "Cafes", StopWhen: rules);

    // A cleaning plan built by hand, as PipelineFile.Read would build it, so the stop rules are
    // checked without a pipeline file.
    private static CleaningPlan Plan(StopWhen? rules, params (string Name, Filter Filter)[] filters) =>
        new(Cafes(rules), [.. filters.Select(filter => filter.Name)], filters.Select(filter => filter.Filter).Aggregate(Chain.Keep, (a, b) => a >> b));

    // The filter the stop-rule tests drop rows with, since dropping by a missing name is easy to
    // count.
    private static readonly (string, Filter) RequireName = (RequireFields.Name, RequireFields.Create(["name"]));

    // Ten places, the first `named` of them with a name. With ten rows, each unnamed row is ten
    // per cent of the table, so one row crosses the default limit of 10 % (US-36, test 2).
    private static ImmutableArray<Row> Ten(int named) =>
        [.. Enumerable.Range(1, 10).Select(number => FilterTests.Place(number, 113.55, 22.19, ("name", number <= named ? $"Place {number}" : null)))];

    [Fact]
    public void Stops_a_table_that_drops_more_than_ten_per_cent_by_default()
    {
        var report = Cleaning.Clean(Plan(null, RequireName), Ten(named: 8), pipelineRules: null);

        Assert.Equal("food.cafes: 2 of 10 rows were dropped (20 %), more than the 10 % allowed.", report.Stop);
    }

    [Fact]
    public void Lets_a_table_set_its_own_rules()
    {
        Assert.Null(Cleaning.Clean(Plan(new StopWhen(DroppedOverPercent: 25), RequireName), Ten(named: 8), null).Stop);
        Assert.Equal("food.cafes: only 8 rows remain, fewer than the 9 required.",
            Cleaning.Clean(Plan(new StopWhen(DroppedOverPercent: 50, RowsUnder: 9), RequireName), Ten(named: 8), null).Stop);
    }

    [Fact]
    public void Runs_filters_in_order_and_ends_at_the_first_stop()
    {
        var plan = Plan(new StopWhen(DroppedOverPercent: 100),
            (RequireFields.Name, RequireFields.Create(["name"], OnFail.Stop)),
            (TidyText.Name, TidyText.Create()));

        var report = Cleaning.Clean(plan, Ten(named: 9), null);

        Assert.Equal("requireFields stopped the run at row 10: name is missing.", report.Stop);
        Assert.All(report.Outcome.Log, line => Assert.Equal(RequireFields.Name, line.Filter));
    }

    [Fact]
    public void Logs_each_filter_and_each_row_it_touched()
    {
        var pipeline = PipelineFile.Read(Good, Macau).Value.Pipeline;
        var report = Cleaning.Clean(Plan(new StopWhen(DroppedOverPercent: 50), (TidyText.Name, TidyText.Create()), RequireName), Ten(named: 9), null);

        var log = Cleaning.Log("data/macau.pipeline.json", pipeline, [report], "wrote it.", new DateTimeOffset(2026, 10, 15, 13, 0, 0, TimeSpan.FromHours(8)));

        Assert.Contains("Tessera ETL log, 2026-10-15 13:00:00 +08:00", log, StringComparison.Ordinal);
        Assert.Contains("  tidyText: kept 10, fixed 0, dropped 0", log, StringComparison.Ordinal);
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
