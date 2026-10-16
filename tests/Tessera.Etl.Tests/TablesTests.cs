using Tessera.Etl.Filters;

namespace Tessera.Etl.Tests;

/// <summary>
/// One table's whole path from GDAL's GeoJSON to SQL (Tables.cs), checked with GeoJSON written
/// here, so each way a table can end is tested without GDAL or a file.
/// </summary>
public sealed class TablesTests
{
    // Two cafés, the second with no name.
    private const string TwoCafes = """
        {"type":"FeatureCollection","features":[
          {"type":"Feature","properties":{"name":"Café Uno"},"geometry":{"type":"Point","coordinates":[113.55,22.19]}},
          {"type":"Feature","properties":{"name":null},"geometry":{"type":"Point","coordinates":[113.56,22.2]}}]}
        """;

    // A plan that drops, or with OnFail.Stop stops at, a café with no name, and lets any share of
    // rows be dropped.
    private static CleaningPlan RequireName(OnFail onFail = OnFail.Drop) =>
        new(new TablePlan("food.cafes"), [RequireFields.Name],
            RequireFields.Create(["name"], onFail) >> Steps.StopWhen(new StopWhen(DroppedOverPercent: 100)));

    [Fact]
    public void Cleans_a_table_and_writes_its_SQL()
    {
        var (outcome, sql) = Tables.Load(RequireName(), TwoCafes).Value;

        Assert.Equal((2, 1), (outcome.Read, outcome.Kept));
        Assert.Single(sql, statement => statement.StartsWith("INSERT", StringComparison.Ordinal));
    }

    [Fact]
    public void Fails_GeoJSON_it_cannot_read_before_any_cleaning()
    {
        var failure = Tables.Load(RequireName(), "{").Error;

        Assert.IsType<Failure.Unreadable>(failure);
        Assert.True(failure.Cleaned.HasNoValue);
    }

    // A stop comes after the cleaning, so it keeps the counts for the log.
    [Fact]
    public void Fails_a_table_a_filter_stops_and_keeps_its_counts()
    {
        var failure = Tables.Load(RequireName(OnFail.Stop), TwoCafes).Error;

        Assert.Equal("stopped. requireFields stopped the run at row 2: name is missing. No script written.", failure.Message);
        Assert.Equal(2, failure.Cleaned.Value.Read);
    }

    // Two columns that become one name: the rows were cleaned, but SQL cannot hold them.
    [Fact]
    public void Fails_rows_SQL_cannot_hold_and_keeps_their_counts()
    {
        var clash = TwoCafes.Replace("\"name\":null", "\"name\":\"Café Dos\",\"Name\":\"x\"", StringComparison.Ordinal);

        var failure = Tables.Load(RequireName(), clash).Error;

        Assert.IsType<Failure.Refused>(failure);
        Assert.Equal(2, failure.Cleaned.Value.Kept);
    }
}
