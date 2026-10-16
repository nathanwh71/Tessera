using Tessera.Etl.Filters;

namespace Tessera.Etl.Tests;

/// <summary>
/// The kernel, Pipes.cs, and steps joined with <c>&gt;&gt;</c>: a chain must behave as its
/// <b><i>filters</i></b> do one after another (US-36, criterion 1), a stop must end it, and the
/// script must hold the columns the last filter left.
/// </summary>
public sealed class ChainTests
{
    // The same area, rows and runner as FilterTests, so a chain is checked on rows made just as
    // each filter's own tests make them.
    private static readonly Area Macau = FilterTests.Macau;

    private static Row Place(int number, double longitude, double latitude, params (string, object?)[] values) =>
        FilterTests.Place(number, longitude, latitude, values);

    private static Cleaning Clean(Handler<Cleaning> steps, ImmutableArray<Row> rows) => FilterTests.Clean(steps, rows);

    // A step that only notes its name, so the kernel's tests can see which steps ran, and in what
    // order, with no rows at all.
    private static Handler<List<string>> Note(string name) => next => seen =>
    {
        seen.Add(name);
        return next(seen);
    };

    [Fact]
    public void Runs_steps_in_the_order_written_and_hands_back_the_context()
    {
        var seen = (Note("a") >> Note("b") >> Note("c")).Run([]);
        Assert.Equal(["a", "b", "c"], seen);
        Assert.Equal(["a", "b"], Pipes.All([Note("a"), Note("b")]).Run([]));
        Assert.Empty(Pipes.All<List<string>>([]).Run([]));
    }

    // A step receives the rest of the chain, so it can run code after the steps that follow it:
    // the power the handler shape adds over a plain function, used here to wrap the rest.
    [Fact]
    public void Lets_a_step_run_code_after_the_rest_of_the_chain()
    {
        Handler<List<string>> around = next => seen =>
        {
            seen.Add("before");
            var after = next(seen);
            after.Add("after");
            return after;
        };
        Assert.Equal(["before", "a", "after"], (around >> Note("a")).Run([]));
    }

    [Fact]
    public void Skips_a_step_whose_test_fails_and_runs_the_rest()
    {
        var chain = Pipes.When(seen => seen.Count > 0, Note("only when something ran")) >> Note("last");
        Assert.Equal(["last"], chain.Run([]));
        Assert.Equal(["first", "only when something ran", "last"], (Note("first") >> chain).Run([]));
    }

    [Fact]
    public void Runs_the_second_filter_on_what_the_first_kept_and_joins_their_logs()
    {
        var clean = SwapCoordinates.Create(Macau) >> InsideArea.Create(Macau) >> RequireFields.Create(["name"]);
        var outcome = Clean(clean, [Place(1, 22.19, 113.55, ("name", "Swapped")), Place(2, 0.5, 51.5, ("name", "Far")), Place(3, 113.55, 22.19)]);

        Assert.Equal([1], outcome.Rows.Select(row => row.Number));
        Assert.Equal(
            ["Row 1: fixed by swapCoordinates: swapped longitude and latitude", "Row 2: dropped by insideArea: its shape lies outside the area", "Row 3: dropped by requireFields: name is missing"],
            outcome.Log.Select(line => line.ToString()));
    }

    [Fact]
    public void Ends_the_chain_at_a_stop()
    {
        var ran = false;
        Handler<Cleaning> later = next => cleaning =>
        {
            ran = true;
            return next(cleaning);
        };
        var outcome = Clean(InsideArea.Create(Macau, OnFail.Stop) >> later, [Place(1, 0.5, 51.5)]);
        Assert.NotNull(outcome.Stop);
        Assert.False(ran);
    }

    // Identity changes nothing, so a chain that starts or ends with it gives what the filter alone gives.
    [Fact]
    public void Starts_with_a_step_that_changes_nothing()
    {
        var rows = ImmutableArray.Create(Place(1, 113.55, 22.19), Place(2, 0.5, 51.5));
        var alone = Clean(InsideArea.Create(Macau), rows);
        foreach (var chained in new[] { Clean(Pipes.Identity<Cleaning>() >> InsideArea.Create(Macau), rows), Clean(InsideArea.Create(Macau) >> Pipes.Identity<Cleaning>(), rows) })
        {
            Assert.Equal(alone.Rows, chained.Rows);
            Assert.Equal(alone.Log, chained.Log);
        }
    }

    [Fact]
    public void Writes_a_column_a_filter_adds_and_drops_one_a_filter_removes()
    {
        var rows = ImmutableArray.Create(Place(1, 113.55, 22.19, ("name", "A"), ("owner_phone", "123"), ("note", null)));
        var addArea = Steps.Filter("addArea", OnFail.Drop, Steps.EachRow(row => Verdict.Fix(row with { Values = row.Values.SetItem("area_m2", 12.5) }, "added area_m2")));
        var dropPhone = Steps.Filter("dropPhone", OnFail.Drop, Steps.EachRow(row => Verdict.Fix(row with { Values = row.Values.Remove("owner_phone") }, "removed owner_phone")));
        var kept = Clean(addArea >> dropPhone, rows).Rows;

        var columns = GeoJson.Columns(kept, ["name", "owner_phone", "note"]);

        // The empty source column stays; the removed one goes; the new one follows the source's.
        Assert.Equal(["name", "note", "area_m2"], columns);
        var (again, _) = GeoJson.Read(GeoJson.Write(kept, ["name", "owner_phone", "note"]));
        Assert.Equal(12.5, again[0].Value("area_m2"));
        Assert.False(again[0].Values.ContainsKey("owner_phone"));
    }
}
