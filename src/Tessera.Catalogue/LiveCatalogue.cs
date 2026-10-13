using System.Collections.Immutable;
using Tessera.Kernel;
using Tessera.Sql;

namespace Tessera.Catalogue;

/// <summary>
/// The list of layers as it stands now, reread from the database every <c>CatalogueRefresh</c>
/// seconds, so a table granted after start-up is served without a restart (N-23).
/// </summary>
/// <remarks>
/// Each list is an immutable <see cref="LayerCatalogue"/>; this class holds one reference to the
/// current list and swaps it whole, so a request always reads one complete list, never half of two.
/// It is the one place in the module where state changes over time (ADR 0004: state at an edge).
/// Until the first load succeeds there is no list at all, and the service answers 503 rather than
/// stopping, so the service and the database may start in either order (N-23 c4).
/// </remarks>
public sealed class LiveCatalogue(
    Func<CancellationToken, Task<Result<LayerCatalogue, DatabaseError>>> load,
    Action<LayerCatalogue, LayerCatalogue> changed,
    Action<DatabaseError> failed)
{
    private LayerCatalogue? _current;

    /// <summary>A live catalogue that reads the database as the service's role.</summary>
    public static LiveCatalogue From(Database database, Action<LayerCatalogue, LayerCatalogue> changed, Action<DatabaseError> failed) =>
        new(cancel => LayerCatalogue.Load(database, cancel), changed, failed);

    /// <summary>The list as it stands, or none before the first load succeeds.</summary>
    public Option<LayerCatalogue> Current => Volatile.Read(ref _current).ToOption();

    /// <summary>
    /// Reads the list once and, on success, makes it the current one and reports what changed. On
    /// failure the last good list stays, since a database that blinks should not unpublish every layer.
    /// </summary>
    public async Task<Result<LayerCatalogue, DatabaseError>> Refresh(CancellationToken cancel)
    {
        var result = await load(cancel).ConfigureAwait(false);
        result.Match(
            fresh =>
            {
                var old = Interlocked.Exchange(ref _current, fresh);
                changed(old ?? new LayerCatalogue(ImmutableDictionary<string, Layer>.Empty), fresh);
                return 0;
            },
            error =>
            {
                failed(error);
                return 0;
            });
        return result;
    }

    /// <summary>
    /// Refreshes until <paramref name="stop"/> fires: every <paramref name="retry"/> until the first
    /// load succeeds, then every <paramref name="every"/>. With <paramref name="every"/> at zero the
    /// list is read until it loads once, and then never again.
    /// </summary>
    public async Task Run(TimeSpan every, TimeSpan retry, TimeProvider clock, CancellationToken stop)
    {
        try
        {
            while (!stop.IsCancellationRequested)
            {
                var loaded = (await Refresh(stop).ConfigureAwait(false)).Match(_ => true, _ => false) || Current.Match(_ => true, () => false);
                if (loaded && every <= TimeSpan.Zero)
                    return;
                await Task.Delay(loaded ? every : retry, clock, stop).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            // The service is stopping; the loop ends with it.
        }
    }

    /// <summary>The ids of layers in the old list that the new list has dropped.</summary>
    public static ImmutableArray<string> Departed(LayerCatalogue old, LayerCatalogue fresh) =>
        [.. old.Layers.Keys.Where(id => !fresh.Layers.ContainsKey(id)).Order(StringComparer.Ordinal)];
}
