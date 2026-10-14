namespace Tessera.Sql;

/// <summary>
/// Knows whether the database is up, so the service can say so at once (N-28, ADR 0006 rule 6).
/// It belongs to the Sql module: <see cref="Database"/> makes one, refuses every query while it says
/// down, and tells it when a query loses its connection. In the Service module a background task
/// runs its loop, and the health check reads it to answer <c>/health</c>.
/// </summary>
/// <remarks>
/// <para>
/// The reason it exists: Npgsql, the library Tessera uses to talk to PostgreSQL, fails at once when
/// a connection is refused, but a database that hangs, behind a broken network or a stuck disk,
/// refuses nothing. Each request would then wait out the whole timeout of the pool, the set of open
/// connections the service reuses.
/// </para>
/// <para>
/// So the watch pings once a second on a pool of its own, and keeps the verdict in one field that
/// any thread, one of the lines of work the computer runs side by side, may read at no cost. After a
/// slow ping it pings again at once, so a frozen database is called down in about two seconds. It
/// hurries once in a row only, so a ping that fails at once can never spin the loop. The Java build,
/// Tessera's twin written in Java, watches the same way.
/// </para>
/// </remarks>
/// <param name="ping">Sends one ping and reports what it found; <see cref="Database"/> supplies it.</param>
public sealed class DatabaseWatch(Func<CancellationToken, Task<Ping>> ping)
{
    // A lock is a gate only one thread may pass at a time.
    private readonly Lock _lock = new();
    private WatchState _state = WatchState.Start;

    /// <summary>
    /// Raised with the new verdict, true for up, each time it changes, so the program can log it. An
    /// event is a list of functions the class calls when something happens.
    /// </summary>
    public event Action<bool>? Changed;

    /// <summary>Whether queries may go to the database.</summary>
    /// <remarks>
    /// Volatile.Read makes sure a thread sees the latest state another thread wrote, not an old copy.
    /// </remarks>
    public bool Up => Volatile.Read(ref _state).Up;

    /// <summary>
    /// Pings once, records the answer, and returns the ping. It returns a <c>Task</c>, a promise of a
    /// value that arrives later, so the program serves other requests while the ping waits.
    /// </summary>
    public async Task<Ping> Check(CancellationToken cancel)
    {
        var found = await ping(cancel).ConfigureAwait(false);
        Record(state => state.After(found));
        return found;
    }

    /// <summary>A query failed for want of a connection: the database is down until a ping answers.</summary>
    public void LostConnection() => Record(state => state.AfterLostConnection());

    /// <summary>
    /// Pings every <paramref name="every"/> until <paramref name="stop"/> fires. The service passes one
    /// second. <paramref name="clock"/> times each wait; it comes in as a parameter, so a test may pass
    /// a clock of its own.
    /// </summary>
    public async Task Run(TimeSpan every, TimeProvider clock, CancellationToken stop)
    {
        var hurried = false;
        while (!stop.IsCancellationRequested)
        {
            var slow = await Check(stop).ConfigureAwait(false) == Ping.Slow;
            // A slow ping earns one more at once, without the wait; a second slow ping then waits.
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

    // One thread at a time swaps the state, so each change is announced once. The announcement runs
    // after the lock is let go, so a slow listener holds up no other thread.
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
