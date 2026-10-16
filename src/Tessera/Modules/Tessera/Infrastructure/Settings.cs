using System.ComponentModel.DataAnnotations;

// The settings that govern Tessera's use of the database: the pool's limits and how often the list of
// layers is read (US-00, ADR 0003). The module's door, TesseraModule, hands them to the code here.

namespace Modules.Tessera.Infrastructure;

/// <summary>
/// The limits on the service's use of the database (N-22). A <b><i>connection pool</i></b> keeps a few
/// connections open for requests to share, since opening a fresh one for each request is slow. The
/// section <c>Database</c> in tessera.json.
/// </summary>
public sealed record DatabaseSettings
{
    /// <summary>
    /// The most connections the pool holds open at once. Four let <b><i>tiles</i></b> build side by
    /// side yet keep the database's load small: two copies of the service hold eight in all (N-24).
    /// The cap of 1000 catches a slip of the keyboard.
    /// </summary>
    [Range(1, 1000)]
    public int PoolSize { get; init; } = 4;

    /// <summary>
    /// How long a connection lives before the pool replaces it, so no connection grows old holding
    /// the server's memory; an hour makes the cost of a fresh one small.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ConnectionLifetimeSeconds { get; init; } = 3600;

    /// <summary>
    /// The longest a query may run before the database cancels it, so a runaway query cannot hold a
    /// connection for ever, and its request answers 503 (N-22 c2); 0 means no limit.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int QueryTimeoutSeconds { get; init; } = 10;

    /// <summary>
    /// The longest a request waits to open a connection or to borrow a free one; past it, the request
    /// answers 503 rather than waiting on, so the browser tries again later (N-22 c2).
    /// </summary>
    [Range(1, 600)]
    public int ConnectionWaitSeconds { get; init; } = 3;
}

/// <summary>
/// How often the service rereads the list of tables it may read, so a table <b><i>granted</i></b>
/// later appears without a restart (N-23). 0 means it reads the list once. The section
/// <c>Catalogue</c> in tessera.json.
/// </summary>
public sealed record CatalogueSettings
{
    [Range(0, int.MaxValue)]
    public int RefreshSeconds { get; init; } = 30;
}
