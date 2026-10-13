using Tessera.Catalogue;
using Tessera.Sql;

namespace Tessera.UnitTests;

/// <summary>N-23: the list of layers is swapped whole, kept through a failure, and loaded in any order.</summary>
public sealed class LiveCatalogueTests
{
    private static LayerCatalogue With(params string[] tables) =>
        LayerCatalogue.FromRows(tables.Select(table => new CatalogueRow("food", table, "geom", 4326, "POINT", "[]")));

    private static Task<Result<LayerCatalogue, DatabaseError>> Ok(LayerCatalogue catalogue) =>
        Task.FromResult(Result.Ok<LayerCatalogue, DatabaseError>(catalogue));

    private static Task<Result<LayerCatalogue, DatabaseError>> Down() =>
        Task.FromResult(Result.Refuse<LayerCatalogue, DatabaseError>(new DatabaseError("08006", "down")));

    private static IEnumerable<string> Ids(LiveCatalogue live) =>
        live.Current.Match<IEnumerable<string>>(found => found.Layers.Keys.Order(StringComparer.Ordinal), () => ["none"]);

    [Fact]
    public async Task Holds_no_list_until_the_first_load_succeeds()
    {
        var answers = new Queue<Func<Task<Result<LayerCatalogue, DatabaseError>>>>([Down, () => Ok(With("cafes"))]);
        var live = new LiveCatalogue(_ => answers.Dequeue()(), (_, _) => { }, _ => { });

        await live.Refresh(CancellationToken.None);
        Assert.Equal(["none"], Ids(live));

        await live.Refresh(CancellationToken.None);
        Assert.Equal(["food.cafes"], Ids(live));
    }

    // N-23 c5: a database that blinks does not unpublish every layer.
    [Fact]
    public async Task Keeps_the_last_good_list_through_a_failure()
    {
        var answers = new Queue<Func<Task<Result<LayerCatalogue, DatabaseError>>>>([() => Ok(With("cafes")), Down]);
        var failures = 0;
        var live = new LiveCatalogue(_ => answers.Dequeue()(), (_, _) => { }, _ => failures++);

        await live.Refresh(CancellationToken.None);
        await live.Refresh(CancellationToken.None);

        Assert.Equal(["food.cafes"], Ids(live));
        Assert.Equal(1, failures);
    }

    [Fact]
    public async Task Reports_the_layers_that_left()
    {
        var answers = new Queue<LayerCatalogue>([With("cafes", "markets"), With("markets", "restaurants")]);
        var departed = new List<string>();
        var live = new LiveCatalogue(_ => Ok(answers.Dequeue()), (old, fresh) => departed.AddRange(LiveCatalogue.Departed(old, fresh)), _ => { });

        await live.Refresh(CancellationToken.None);
        await live.Refresh(CancellationToken.None);

        Assert.Equal(["food.cafes"], departed);
        Assert.Equal(["food.markets", "food.restaurants"], Ids(live));
    }

    // N-23 c4: the loop tries again and again until the database answers, then refreshes.
    [Fact]
    public async Task Tries_until_the_database_answers()
    {
        var calls = 0;
        var live = new LiveCatalogue(_ => ++calls < 3 ? Down() : Ok(With("cafes")), (_, _) => { }, _ => { });
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var loop = live.Run(TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(10), TimeProvider.System, stop.Token);
        while (live.Current.Match(_ => false, () => true) && !stop.IsCancellationRequested)
            await Task.Delay(10, CancellationToken.None);
        await stop.CancelAsync();
        await loop;

        Assert.Equal(["food.cafes"], Ids(live));
        Assert.True(calls >= 3);
    }

    // CatalogueRefresh 0: the list loads once, and the loop ends.
    [Fact]
    public async Task Loads_once_when_refresh_is_zero()
    {
        var calls = 0;
        var live = new LiveCatalogue(_ => { calls++; return Ok(With("cafes")); }, (_, _) => { }, _ => { });

        await live.Run(TimeSpan.Zero, TimeSpan.FromMilliseconds(10), TimeProvider.System, CancellationToken.None);

        Assert.Equal(1, calls);
    }
}
