using CSharpFunctionalExtensions;
using Modules.Tessera.Domain;
using Modules.Tessera.Infrastructure.Sql;

namespace Modules.Tessera.Infrastructure;

/// <summary>
/// The list of <b><i>layers</i></b> as it stands now, reread every <paramref name="every"/> so a table
/// given a <b><i>grant</i></b>, the database's word for a permission, after start-up is served without
/// a restart (N-23). .NET runs it as a <b><i>hosted service</i></b>, a loop that starts and stops with
/// the program.
/// </summary>
/// <remarks>
/// A request must read one whole list, never half of two, so the class swaps the whole list in one
/// step. Until the first load succeeds there is no list, and tiles answer 503 (N-23 c3); it then tries
/// every <paramref name="retry"/>. A failed refresh keeps the last good list (N-23 c4), so a database
/// that blinks does not unpublish every layer.
/// </remarks>
/// <param name="load">Reads the list; a test passes a stand-in.</param>
/// <param name="every">The wait between refreshes; zero reads the list once.</param>
/// <param name="retry">The wait between tries before the first load succeeds.</param>
/// <param name="log">Where the class reports a new list or a failed read.</param>
public sealed class LiveCatalogue(
    Func<CancellationToken, Task<Result<LayerCatalogue, DatabaseError>>> load,
    TimeSpan every,
    TimeSpan retry,
    ILogger log) : BackgroundService
{
    // Null before the first load. Requests read this field on many threads, strands of work the
    // computer runs side by side, while the loop writes it on another. Interlocked.Exchange swaps a
    // whole list in as one step, so no request can see it half done.
    private LayerCatalogue? _current;

    /// <summary>
    /// The list as it stands, or null before the first load. Volatile.Read fetches the field afresh
    /// each time, so a request sees the list another thread swapped in last, never a stale copy a
    /// processor kept in its own small, fast memory.
    /// </summary>
    public LayerCatalogue? Current => Volatile.Read(ref _current);

    /// <summary>
    /// Reads the list once; on success it becomes the current list. The loop below calls it; it is
    /// public so a test can load a list without starting the loop.
    /// </summary>
    public async Task Refresh(CancellationToken cancel)
    {
        var loaded = await load(cancel).ConfigureAwait(false);
        if (loaded.IsFailure)
        {
            log.LogWarning(
                "Could not read the list of layers ({SqlState}): {Message}",
                loaded.Error.SqlState,
                loaded.Error.Message);
            return;
        }

        var old = Interlocked.Exchange(ref _current, loaded.Value);
        // The log speaks only when the set of layers changes, not at every refresh, so it stays short.
        // Ids compare by their exact characters, since PostgreSQL holds "Food"."Cafes" and food.cafes
        // as two tables (US-03 test 2).
        if (old is null || !old.Layers.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(loaded.Value.Layers.Keys))
            log.LogInformation("The service publishes {Count} layers.", loaded.Value.Layers.Count);
    }

    /// <summary>
    /// The loop .NET runs in the background. It tries every <c>retry</c> until the first load succeeds
    /// (N-23 c3), then rereads every <c>every</c> (N-23 c1), or stops once a zero says read only once.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Refresh(stoppingToken).ConfigureAwait(false);
                if (Current is not null && every <= TimeSpan.Zero)
                    return;
                await Task.Delay(Current is null ? retry : every, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The service is stopping; the loop ends with it.
        }
    }
}
