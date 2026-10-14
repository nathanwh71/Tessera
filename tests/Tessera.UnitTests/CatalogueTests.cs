using Microsoft.Extensions.Logging.Abstractions;
using Tessera.Catalogue;
using Tessera.Sql;

namespace Tessera.UnitTests;

/// <summary>US-03 test 2 and N-23: a row becomes a layer, and the live list is swapped whole and kept through a failure.</summary>
public sealed class CatalogueTests
{
    private static readonly CatalogueRow Cafes = new("food", "cafes", "geom", 4326, "POINT", """["layer","name"]""");

    private static LayerCatalogue With(params string[] tables) =>
        LayerCatalogue.FromRows(tables.Select(table => new CatalogueRow("food", table, "geom", 4326, "POINT", "[]")));

    private static Task<Result<LayerCatalogue, DatabaseError>> Ok(LayerCatalogue catalogue) =>
        Task.FromResult(Result.Success<LayerCatalogue, DatabaseError>(catalogue));

    private static Task<Result<LayerCatalogue, DatabaseError>> Down() =>
        Task.FromResult(Result.Failure<LayerCatalogue, DatabaseError>(new DatabaseError("08006", "down")));

    private static LiveCatalogue Live(Func<CancellationToken, Task<Result<LayerCatalogue, DatabaseError>>> load, TimeSpan every = default) =>
        new(load, every, TimeSpan.FromMilliseconds(10), NullLogger.Instance);

    private static string[] Ids(LiveCatalogue live) => live.Current?.Layers.Keys.Order(StringComparer.Ordinal).ToArray() ?? ["none"];

    [Fact]
    public void Turns_a_row_into_a_layer_whose_id_is_schema_dot_name()
    {
        var layer = LayerCatalogue.ToLayer(Cafes);

        Assert.Equal("food.cafes", layer.Id);
        Assert.Equal("\"food\".\"cafes\"", layer.Table);
        Assert.Equal(["\"layer\"", "\"name\""], layer.Properties);
        Assert.Equal(4326, layer.Srid);
    }

    [Fact]
    public void Finds_a_layer_exactly_and_case_sensitively()
    {
        var catalogue = LayerCatalogue.FromRows([Cafes]);

        Assert.NotNull(catalogue.Find("food.cafes"));
        Assert.Null(catalogue.Find("Food.Cafes"));
        Assert.Null(catalogue.Find("food"));
    }

    // US-03 test 3: each column the query returns maps to one field of the row, by name.
    [Fact]
    public void Names_each_column_after_a_field_of_the_row()
    {
        foreach (var field in typeof(CatalogueRow).GetConstructors().Single().GetParameters().Select(parameter => parameter.Name!.ToLowerInvariant()))
            Assert.Contains($" AS {field}", Queries.Layers.Text, StringComparison.Ordinal);
    }

    // US-03 c5: a function never becomes a layer; the query reads pg_class, where functions never appear.
    [Fact]
    public void Lists_tables_and_views_alone() =>
        Assert.Contains("c.relkind IN ('r', 'p', 'v', 'm')", Queries.Layers.Text, StringComparison.Ordinal);

    [Fact]
    public async Task Holds_no_list_until_the_first_load_succeeds()
    {
        var answers = new Queue<Func<Task<Result<LayerCatalogue, DatabaseError>>>>([Down, () => Ok(With("cafes"))]);
        var live = Live(_ => answers.Dequeue()());

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
        var live = Live(_ => answers.Dequeue()());

        await live.Refresh(CancellationToken.None);
        await live.Refresh(CancellationToken.None);

        Assert.Equal(["food.cafes"], Ids(live));
    }

    // N-23 c4: the loop tries again until the database answers.
    [Fact]
    public async Task Tries_until_the_database_answers()
    {
        var calls = 0;
        var live = Live(_ => ++calls < 3 ? Down() : Ok(With("cafes")), TimeSpan.FromSeconds(30));

        await live.StartAsync(TestContext.Current.CancellationToken);
        for (var wait = 0; wait < 100 && live.Current is null; wait++)
            await Task.Delay(10, TestContext.Current.CancellationToken);
        await live.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["food.cafes"], Ids(live));
        Assert.True(calls >= 3);
    }

    // RefreshSeconds 0: the list loads once, and the loop ends.
    [Fact]
    public async Task Loads_once_when_refresh_is_zero()
    {
        var calls = 0;
        var live = Live(_ => { calls++; return Ok(With("cafes")); });

        await live.StartAsync(TestContext.Current.CancellationToken);
        await (live.ExecuteTask ?? Task.CompletedTask);

        Assert.Equal(1, calls);
    }
}
