using Tessera.Sql;

namespace Tessera.UnitTests;

/// <summary>
/// N-28: the rules by which the service decides the database is up or down, the same as the Java
/// build's, and the watch that applies them. A fake ping stands in for the database.
/// </summary>
public sealed class DatabaseWatchTests
{
    private static WatchState After(params Ping[] pings) => pings.Aggregate(WatchState.Start, (state, ping) => state.After(ping));

    [Fact]
    public void Starts_up_trusting_the_database() => Assert.True(WatchState.Start.Up);

    [Fact]
    public void Calls_a_refused_line_proof_of_down() => Assert.False(After(Ping.Refused).Up);

    [Fact]
    public void Lets_one_slow_ping_pass_since_a_busy_database_may_miss_one() => Assert.True(After(Ping.Slow).Up);

    [Fact]
    public void Calls_two_slow_pings_in_a_row_down() => Assert.False(After(Ping.Slow, Ping.Slow).Up);

    [Fact]
    public void Starts_the_count_again_after_an_answer() => Assert.True(After(Ping.Slow, Ping.Answered, Ping.Slow).Up);

    [Fact]
    public void Says_up_after_one_answer()
    {
        Assert.True(After(Ping.Refused, Ping.Answered).Up);
        Assert.True(After(Ping.Slow, Ping.Slow, Ping.Answered).Up);
    }

    [Fact]
    public void Stays_down_on_a_slow_ping() => Assert.False(After(Ping.Refused, Ping.Slow).Up);

    [Fact]
    public void Calls_a_lost_connection_proof_of_down() => Assert.False(WatchState.Start.AfterLostConnection().Up);

    [Fact]
    public async Task Records_each_pings_verdict_and_announces_each_change()
    {
        var pings = new Queue<Ping>([Ping.Refused, Ping.Refused, Ping.Answered]);
        var watch = new DatabaseWatch(_ => Task.FromResult(pings.Dequeue()));
        var changes = new List<bool>();
        watch.Changed += changes.Add;

        await watch.Check(TestContext.Current.CancellationToken);
        await watch.Check(TestContext.Current.CancellationToken);
        Assert.False(watch.Up);
        await watch.Check(TestContext.Current.CancellationToken);

        Assert.True(watch.Up);
        Assert.Equal([false, true], changes);
    }

    [Fact]
    public void Says_down_at_once_on_a_lost_connection()
    {
        var watch = new DatabaseWatch(_ => Task.FromResult(Ping.Answered));
        watch.LostConnection();
        Assert.False(watch.Up);
    }

    // After a slow ping the loop pings again at once, but only once in a row, so a ping that fails
    // at once can never spin it.
    [Fact]
    public async Task Hurries_once_after_a_slow_ping()
    {
        var count = 0;
        var watch = new DatabaseWatch(_ =>
        {
            Interlocked.Increment(ref count);
            return Task.FromResult(Ping.Slow);
        });
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var loop = watch.Run(TimeSpan.FromSeconds(10), TimeProvider.System, stop.Token);

        await Task.Delay(200, TestContext.Current.CancellationToken);

        Assert.False(watch.Up);
        Assert.Equal(2, Volatile.Read(ref count));
        await stop.CancelAsync();
        await loop;
    }

    // A timeout is no proof, since a busy database times out too; a failed connection or a server
    // shutting down is.
    [Theory]
    [InlineData("08006", true)]
    [InlineData("08001", true)]
    [InlineData("57P01", true)]
    [InlineData("57P03", true)]
    [InlineData("57014", false)]
    [InlineData("42P01", false)]
    public void Knows_which_failures_prove_the_database_down(string state, bool proof) =>
        Assert.Equal(proof, Database.LostConnection(new DatabaseError(state, "")));

    // DbPoolConnectionTimeout counts milliseconds, from 250, but Npgsql counts whole seconds and reads
    // 0 as 'wait for ever': a short wait must round up, never down to 0.
    [Theory]
    [InlineData(250, 1)]
    [InlineData(999, 1)]
    [InlineData(1000, 1)]
    [InlineData(3000, 3)]
    [InlineData(3500, 4)]
    public void Rounds_the_pools_wait_up_to_whole_seconds(int milliseconds, int seconds) =>
        Assert.Equal(seconds, Database.WholeSeconds(TimeSpan.FromMilliseconds(milliseconds)));
}
