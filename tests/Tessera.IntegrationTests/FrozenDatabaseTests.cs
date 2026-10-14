using Tessera.Sql;

namespace Tessera.IntegrationTests;

/// <summary>
/// N-28: a database that hangs refuses nothing, so the service must notice on its own. These tests
/// freeze a database of their own, so no other test waits on it.
/// </summary>
public sealed class FrozenDatabaseTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    // N-28 c1 and test 3: frozen, the watch says down within about four seconds: up to one asleep,
    // one for the slow ping, and up to one and a half for the connection that hangs. The bound is
    // five, for a busy test machine. Meanwhile a query is refused at once; thawed, the watch says up
    // within two and a half, and queries run again.
    [Fact]
    public async Task Notices_a_frozen_database_and_its_return()
    {
        await using var reader = Database.Open(database.Reader);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var loop = reader.Watch.Run(TimeSpan.FromSeconds(1), TimeProvider.System, stop.Token);
        await Until(() => reader.Watch.Up, TimeSpan.FromSeconds(2));
        var clock = System.Diagnostics.Stopwatch.StartNew();

        await database.Freeze();
        try
        {
            await Until(() => !reader.Watch.Up, TimeSpan.FromSeconds(8));
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(5), $"down after {clock.Elapsed}");

            var refused = System.Diagnostics.Stopwatch.StartNew();
            var answer = await reader.Rows<int>(HealthQueries.Ping, TestContext.Current.CancellationToken);
            Assert.True(answer.Match(_ => false, error => error.IsUnavailable));
            Assert.True(refused.Elapsed < TimeSpan.FromMilliseconds(100), $"refused after {refused.Elapsed}");
        }
        finally
        {
            await database.Thaw();
        }

        clock.Restart();
        await Until(() => reader.Watch.Up, TimeSpan.FromSeconds(5));
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(2.5), $"up after {clock.Elapsed}");
        Assert.True((await reader.Rows<int>(HealthQueries.Ping, TestContext.Current.CancellationToken)).Match(_ => true, _ => false));

        await stop.CancelAsync();
        await loop;
    }

    private static async Task Until(Func<bool> condition, TimeSpan within)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && clock.Elapsed < within)
            await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.True(condition(), $"the condition did not hold within {within}");
    }
}
