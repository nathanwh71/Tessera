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

    // A pipeline file with no fault. The first refusal tests below each change one thing in it, so
    // every fault the reader finds comes from that change.
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
        Assert.True(FilterTests.Clean(table.Chain, [FilterTests.Place(1, 0.5, 51.5, ("name", "Far"))]).Stop.HasValue);
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

    // The JSON reader lets a null stand in a list; it is refused with a reason, not a crash.
    [Fact]
    public void Refuses_a_table_that_is_null()
    {
        var read = PipelineFile.Read("""{ "source": "data/places.gdb", "output": "db/seed/0002-places.sql", "tables": [null] }""", Macau);

        Assert.Equal("tables: each entry must be an object that names its table.", Assert.Single(read.Error));
    }

    [Fact]
    public void Names_every_fault_at_once()
    {
        var bad = """
            { "source": "x.gdb", "output": "db/seed/parks.sql", "tables": [
              { "table": "Food.Cafes", "filters": [ { "filter": "dropDuplicates" }, { "filter": "dropNearDuplicates", "field": "name" } ] },
              { "table": "outdoors.parks", "filters": [ { "filter": "insideArea" } ] } ] }
            """;

        var problems = PipelineFile.Read(bad, areaFromSettings: Maybe<Area>.None).Error;

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
    private static CleaningPlan Plan(StopWhen? rules, params (string Name, Handler<Cleaning> Step)[] filters) =>
        new(Cafes(rules), [.. filters.Select(filter => filter.Name)],
            Pipes.All(filters.Select(filter => filter.Step)) >> Steps.StopWhen(rules ?? PipelineFile.Defaults));

    // Runs a plan's chain on the rows, as the program does for each table.
    private static Cleaning Clean(CleaningPlan plan, ImmutableArray<Row> rows) => plan.Chain.Run(Cleaning.Start(plan.Table, rows));

    // The filter the stop-rule tests drop rows with, since dropping by a missing name is easy to
    // count.
    private static readonly (string, Handler<Cleaning>) RequireName = (RequireFields.Name, RequireFields.Create(["name"]));

    // Ten places, the first `named` of them with a name. With ten rows, each unnamed row is ten
    // per cent of the table: one row meets the default limit of 10 %, and a second crosses it
    // (US-36, test 2).
    private static ImmutableArray<Row> Ten(int named) =>
        [.. Enumerable.Range(1, 10).Select(number => FilterTests.Place(number, 113.55, 22.19, ("name", number <= named ? $"Place {number}" : null)))];

    [Fact]
    public void Stops_a_table_that_drops_more_than_ten_per_cent_by_default()
    {
        var report = Clean(Plan(null, RequireName), Ten(named: 8));

        Assert.Equal("food.cafes: 2 of 10 rows were dropped (20 %), more than the 10 % allowed.", report.Stop);
    }

    [Fact]
    public void Lets_a_table_set_its_own_rules()
    {
        Assert.True(Clean(Plan(new StopWhen(DroppedOverPercent: 25), RequireName), Ten(named: 8)).Stop.HasNoValue);
        Assert.Equal("food.cafes: only 8 rows remain, fewer than the 9 required.",
            Clean(Plan(new StopWhen(DroppedOverPercent: 50, RowsUnder: 9), RequireName), Ten(named: 8)).Stop);
    }

    // The chain PipelineFile.Read builds ends with the stop rules: the table's own when it has
    // them, else the file's, else the defaults.
    [Fact]
    public void Ends_each_chain_from_the_file_with_the_closest_stop_rules()
    {
        var file = Good
            .Replace("\"tables\":", "\"stopWhen\": { \"droppedOverPercent\": 5 },\n  \"tables\":", StringComparison.Ordinal)
            .Replace("{ \"filter\": \"insideArea\", \"onFail\": \"stop\" },", "", StringComparison.Ordinal);
        var fileRules = Assert.Single(PipelineFile.Read(file, Macau).Value.Tables);
        Assert.Equal("food.cafes: 1 of 10 rows were dropped (10 %), more than the 5 % allowed.", Clean(fileRules, Ten(named: 9)).Stop);

        var own = file.Replace("\"layer\": \"Cafes\",", "\"layer\": \"Cafes\", \"stopWhen\": { \"droppedOverPercent\": 20 },", StringComparison.Ordinal);
        Assert.True(Clean(Assert.Single(PipelineFile.Read(own, Macau).Value.Tables), Ten(named: 9)).Stop.HasNoValue);
    }

    [Fact]
    public void Runs_filters_in_order_and_ends_at_the_first_stop()
    {
        var plan = Plan(new StopWhen(DroppedOverPercent: 100),
            (RequireFields.Name, RequireFields.Create(["name"], OnFail.Stop)),
            (TidyText.Name, TidyText.Create()));

        var report = Clean(plan, Ten(named: 9));

        Assert.Equal("requireFields stopped the run at row 10: name is missing.", report.Stop);
        Assert.All(report.Log, line => Assert.Equal(RequireFields.Name, line.Filter));
    }

    [Fact]
    public void Logs_each_filter_and_each_row_it_touched()
    {
        var pipeline = PipelineFile.Read(Good, Macau).Value.Pipeline;
        var plan = Plan(new StopWhen(DroppedOverPercent: 50), (TidyText.Name, TidyText.Create()), RequireName);
        var done = Clean(plan, Ten(named: 9));

        var log = Report.Log("data/macau.pipeline.json", pipeline, [(plan, done)], "wrote it.", new DateTimeOffset(2026, 10, 15, 13, 0, 0, TimeSpan.FromHours(8)));

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

    // GeoJSON has no index, so a pipeline of several tables copies it once into a GeoPackage; a
    // geodatabase, or a single table, reads the source as it is.
    [Theory]
    [InlineData("data/places.geojson", 9, true)]
    [InlineData("data/places.GeoJSON", 2, true)]
    [InlineData("data/places.json", 9, true)]
    [InlineData("data/places.geojson", 1, false)]
    [InlineData("data/macau.gdb", 9, false)]
    public void Copies_only_a_GeoJSON_source_of_several_tables(string source, int tables, bool copy) =>
        Assert.Equal(copy, SeedScript.CopyFirst(source, tables));

    [Fact]
    public void Copies_the_source_as_it_is_into_a_GeoPackage() =>
        Assert.Equal(["-f", "GPKG", "/work/source.gpkg", "/data/places.geojson"],
            SeedScript.CopyArguments("/data/places.geojson", "/work/source.gpkg"));

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

        var (rows, columns) = GeoJson.Read(json).Value;

        Assert.Equal(["id", "name"], columns);
        Assert.Equal("cafes-001", rows[0].Value("id"));
    }

    // GDAL may fail part-way and leave a file that is not a FeatureCollection; the reader says why,
    // and the run stops with its log, rather than crashing.
    [Theory]
    [InlineData("""{"type":"FeatureCollection","features":[{"type":""")]
    [InlineData("""{"type":"FeatureCollection"}""")]
    [InlineData("""{"type":"FeatureCollection","features":{}}""")]
    public void Refuses_GeoJSON_that_is_no_FeatureCollection(string json) =>
        Assert.True(GeoJson.Read(json).IsFailure);

    [Fact]
    public void Keeps_columns_in_order_and_values_by_kind_through_GeoJSON()
    {
        var json = """{"type":"FeatureCollection","features":[{"type":"Feature","properties":{"name":"A","floors":3,"height":2.5,"open":true,"note":null},"geometry":{"type":"Point","coordinates":[113.55,22.19]}}]}""";

        var (rows, columns) = GeoJson.Read(json).Value;
        var (again, _) = GeoJson.Read(GeoJson.Write(rows, columns)).Value;

        Assert.Equal(["name", "floors", "height", "open", "note"], columns);
        Assert.Equal(rows[0].Values, again[0].Values);
        Assert.Equal(3L, Assert.IsType<long>(again[0].Values["floors"]));
        Assert.Equal(new Coordinate(113.55, 22.19), again[0].Shape.GetValueOrThrow().Coordinate);
    }
}
