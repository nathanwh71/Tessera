using Tessera.Etl.Filters;

namespace Tessera.Etl.Tests;

/// <summary>
/// <b><i>Filters</i></b> joined with <c>&gt;&gt;</c>, and columns that filters add or remove: a
/// chain must behave as its filters do one after another, and the script must hold the columns
/// the last filter left (US-36, criterion 1).
/// </summary>
public sealed class ChainTests
{
    // The same area and the same way to make a place as FilterTests, so a chain is checked on
    // the very rows each filter was checked on alone.
    private static readonly Area Macau = FilterTests.Macau;

    private static Row Place(int number, double longitude, double latitude, params (string, object?)[] values) =>
        FilterTests.Place(number, longitude, latitude, values);

    [Fact]
    public void Runs_the_second_filter_on_what_the_first_kept_and_joins_their_logs()
    {
        var clean = SwapCoordinates.Create(Macau) >> InsideArea.Create(Macau) >> RequireFields.Create(["name"]);

        var outcome = clean([Place(1, 22.19, 113.55, ("name", "Swapped")), Place(2, 0.5, 51.5, ("name", "Far")), Place(3, 113.55, 22.19)]);

        Assert.Equal([1], outcome.Rows.Select(row => row.Number));
        Assert.Equal(
            ["Row 1: fixed by swapCoordinates: swapped longitude and latitude", "Row 2: dropped by insideArea: its shape lies outside the area", "Row 3: dropped by requireFields: name is missing"],
            outcome.Log.Select(line => line.ToString()));
    }

    [Fact]
    public void Ends_the_chain_at_a_stop()
    {
        var ran = false;
        Filter later = rows =>
        {
            ran = true;
            return Chain.Keep(rows);
        };

        var outcome = (InsideArea.Create(Macau, OnFail.Stop) >> later)([Place(1, 0.5, 51.5)]);

        Assert.NotNull(outcome.Stop);
        Assert.False(ran);
    }

    // Keep changes nothing, so a chain that starts or ends with it gives what the filter alone gives.
    [Fact]
    public void Starts_with_a_filter_that_changes_nothing()
    {
        var rows = ImmutableArray.Create(Place(1, 113.55, 22.19), Place(2, 0.5, 51.5));
        var alone = InsideArea.Create(Macau)(rows);

        foreach (var chained in new[] { (Chain.Keep >> InsideArea.Create(Macau))(rows), (InsideArea.Create(Macau) >> Chain.Keep)(rows) })
        {
            Assert.Equal(alone.Rows, chained.Rows);
            Assert.Equal(alone.Log, chained.Log);
        }
    }

    [Fact]
    public void Writes_a_column_a_filter_adds_and_drops_one_a_filter_removes()
    {
        var rows = ImmutableArray.Create(Place(1, 113.55, 22.19, ("name", "A"), ("owner_phone", "123"), ("note", null)));
        Filter addArea = input => Judge.Changes(input, "addArea", row => (row with { Values = row.Values.SetItem("area_m2", 12.5) }, "added area_m2"));
        Filter dropPhone = input => Judge.Changes(input, "dropPhone", row => (row with { Values = row.Values.Remove("owner_phone") }, "removed owner_phone"));

        var kept = (addArea >> dropPhone)(rows).Rows;
        var columns = GeoJson.Columns(kept, ["name", "owner_phone", "note"]);

        // The empty source column stays; the removed one goes; the new one follows the source's.
        Assert.Equal(["name", "note", "area_m2"], columns);
        var (again, _) = GeoJson.Read(GeoJson.Write(kept, ["name", "owner_phone", "note"]));
        Assert.Equal(12.5, again[0].Value("area_m2"));
        Assert.False(again[0].Values.ContainsKey("owner_phone"));
    }
}
