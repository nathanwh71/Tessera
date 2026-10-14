namespace Tessera.Sql;

/// <summary>
/// Knows whether the database is up, so the service can say so at once (N-28, ADR 0006 rule 6).
/// <see cref="Database"/> makes one, refuses every query while it says down, and tells it when a
/// query loses its connection; the health check reads it for <c>/health</c>.
/// </summary>
/// <remarks>
/// Npgsql fails at once when a connection is refused, but a database that hangs, behind a broken
/// network or a stuck disk, refuses nothing: each request would wait out the pool's whole timeout.
/// So the watch pings once a second on the ping pool, and keeps the verdict in one field any thread
/// may read at no cost. After a slow ping it pings again at once, so a frozen database is called
/// down in about two seconds; it hurries once in a row only, so a ping that fails at once can never
/// spin the loop. The Java build's watch works the same way.
/// </remarks>
public sealed class DatabaseWatch(Func<CancellationToken, Task<Ping>> ping)
{
    private readonly Lock _lock = new();
    private WatchState _state = WatchState.Start;

    /// <summary>Raised with the new verdict each time it changes, so the program can log it.</summary>
    public event Action<bool>? Changed;

    /// <summary>Whether queries may go to the database.</summary>
    public bool Up => Volatile.Read(ref _state).Up;

    /// <summary>Pings once, records the answer, and returns the ping.</summary>
    public async Task<Ping> Check(CancellationToken cancel)
    {
        var found = await ping(cancel).ConfigureAwait(false);
        Record(state => state.After(found));
        return found;
    }

    /// <summary>A query failed for want of a connection: the database is down until a ping answers.</summary>
    public void LostConnection() => Record(state => state.AfterLostConnection());

    /// <summary>Pings every <paramref name="every"/> until <paramref name="stop"/> fires.</summary>
    public async Task Run(TimeSpan every, TimeProvider clock, CancellationToken stop)
    {
        var hurried = false;
        while (!stop.IsCancellationRequested)
        {
            var slow = await Check(stop).ConfigureAwait(false) == Ping.Slow;
            if (slow && !hurried)
            {
                hurried = true;
                continue;
            }
            hurried = false;
            try
            {
                await Task.Delay(every, clock, stop).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
                // The service is stopping; the loop ends.
            }
        }
    }

    // One thread at a time swaps the state, so each change is announced once.
    private void Record(Func<WatchState, WatchState> next)
    {
        bool? changed = null;
        lock (_lock)
        {
            var updated = next(_state);
            if (updated.Up != _state.Up)
            {
                changed = updated.Up;
            }
            Volatile.Write(ref _state, updated);
        }
        if (changed is { } up)
        {
            Changed?.Invoke(up);
        }
    }
}
