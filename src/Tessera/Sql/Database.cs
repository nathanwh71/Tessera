using System.Collections.Immutable;
using CSharpFunctionalExtensions;
using Dapper;
using Npgsql;
using Tessera.Settings;

namespace Tessera.Sql;

/// <summary>
/// A query ready to send: the SQL text, and the values that travel beside it as bound parameters
/// (S-01). The database never reads a parameter as SQL, so no value from a request can change what
/// a query does. Every text lives in Queries.cs.
/// </summary>
public sealed record Query(string Text, object? Parameters = null);

/// <summary>Why the database refused a query: its SQLSTATE code, five characters, and its message.</summary>
public sealed record DatabaseError(string SqlState, string Message)
{
    /// <summary>The table, or its grant, has gone: 42501 or 42P01 (US-03 c6).</summary>
    public bool IsMissingTable => SqlState is PostgresErrorCodes.InsufficientPrivilege or PostgresErrorCodes.UndefinedTable;

    /// <summary>The query names a column a seed script has dropped since the list of layers was read: 42703.</summary>
    public bool IsMissingColumn => SqlState is PostgresErrorCodes.UndefinedColumn;

    /// <summary>
    /// Waiting may cure it (N-22): a failed connection (class 08), a query past its time (57014), a
    /// server stopping or starting (57P01 to 57P03), or no room for another connection (53300).
    /// </summary>
    public bool IsUnavailable =>
        SqlState.StartsWith("08", StringComparison.Ordinal)
        || SqlState is PostgresErrorCodes.QueryCanceled or PostgresErrorCodes.AdminShutdown or PostgresErrorCodes.CrashShutdown
            or PostgresErrorCodes.CannotConnectNow or PostgresErrorCodes.TooManyConnections;
}

/// <summary>
/// The one door to PostgreSQL. A database can fail at any moment, so this class turns each failure
/// into a <see cref="Result{T, E}"/>, a value that holds the answer or the reason, rather than letting
/// an exception escape (ADR 0004).
/// </summary>
/// <remarks>
/// A pool keeps a few connections open for requests to share. When every one is busy, Npgsql lends
/// the next free one to the request that has waited longest (N-26). The health check's ping shares
/// the pool too, so a pool busy for more than a second makes /health say DOWN (N-26 c3).
/// </remarks>
public sealed class Database : IAsyncDisposable
{
    private readonly NpgsqlDataSource _source;

    private Database(NpgsqlDataSource source) => _source = source;

    /// <summary>The longest a ping may take, from connecting to the answer.</summary>
    private static readonly TimeSpan PingLimit = TimeSpan.FromSeconds(1);

    /// <summary>Opens the pool within the given limits, or the defaults.</summary>
    /// <param name="connection">Npgsql's connection string, made from the database URL.</param>
    /// <param name="limits">The pool's limits (N-22).</param>
    public static Database Open(string connection, DatabaseSettings? limits = null)
    {
        var pool = limits ?? new DatabaseSettings();
        return new(NpgsqlDataSource.Create(new NpgsqlConnectionStringBuilder(connection)
        {
            // The name shows in pg_stat_activity, so the maintainer can tell Tessera's connections apart.
            ApplicationName = "tessera",
            MaxPoolSize = pool.PoolSize,
            ConnectionLifetime = pool.ConnectionLifetimeSeconds,
            CommandTimeout = pool.QueryTimeoutSeconds,
            // Covers both opening a connection and waiting for a free one in the pool.
            Timeout = pool.ConnectionWaitSeconds,
            // A query text sent three times on a connection becomes a prepared statement, planned once.
            // Measured on 14 October 2026, this raised the rate from 1,580 to 2,408 tiles a second.
            MaxAutoPrepare = 32,
            AutoPrepareMinUsages = 3,
        }.ConnectionString));
    }

    /// <summary>Runs a query and turns each row into a <typeparamref name="T"/>, matching columns to fields by name.</summary>
    public Task<Result<ImmutableArray<T>, DatabaseError>> Rows<T>(Query query, CancellationToken cancel = default) =>
        Run(async connection => (await connection.QueryAsync<T>(Command(query, cancel)).ConfigureAwait(false)).ToImmutableArray(), cancel);

    /// <summary>Runs a query that returns one value, or null for none.</summary>
    public Task<Result<T?, DatabaseError>> Single<T>(Query query, CancellationToken cancel = default) where T : class =>
        Run(connection => connection.QuerySingleOrDefaultAsync<T>(Command(query, cancel)), cancel);

    /// <summary>
    /// True when the database answers <c>SELECT 1</c> within a second, the wait for a free connection
    /// included (N-25, N-26 c3). The ping keeps its own clock, since a frozen database can hold
    /// Npgsql's limits for longer; a step it gives up on finishes, or fails, out of sight.
    /// </summary>
    public async Task<bool> Ping(CancellationToken stop)
    {
        // The second token takes the ping out of the pool's queue when the second runs out, so a
        // ping that gave up never takes a connection a tile is waiting for.
        var limit = CancellationTokenSource.CreateLinkedTokenSource(stop);
        limit.CancelAfter(PingLimit);
        try
        {
            await PingOnce(limit.Token).WaitAsync(PingLimit, stop).ConfigureAwait(false);
            return true;
        }
        catch (Exception error) when (error is TimeoutException or OperationCanceledException or NpgsqlException && !stop.IsCancellationRequested)
        {
            return false;
        }
        finally
        {
            // Cancelled before it is disposed, so the queue hears of it whichever clock ran out first.
            await limit.CancelAsync().ConfigureAwait(false);
            limit.Dispose();
        }
    }

    private async Task PingOnce(CancellationToken limit)
    {
        await using var connection = await _source.OpenConnectionAsync(limit).ConfigureAwait(false);
        await connection.ExecuteScalarAsync<int>(Command(Queries.Ping, limit)).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => _source.DisposeAsync();

    // The cancellation token reaches the wait for a connection too, so a request that gives up leaves
    // the pool's queue at once (N-26 c2).
    private async Task<Result<T, DatabaseError>> Run<T>(Func<NpgsqlConnection, Task<T>> work, CancellationToken cancel)
    {
        try
        {
            await using var connection = await _source.OpenConnectionAsync(cancel).ConfigureAwait(false);
            return Result.Success<T, DatabaseError>(await work(connection).ConfigureAwait(false));
        }
        catch (NpgsqlException error)
        {
            return Result.Failure<T, DatabaseError>(Error(error));
        }
    }

    // A PostgresException is the database's own refusal, with its SQLSTATE. A query, or a wait for a
    // connection, past its time carries a TimeoutException inside: 57014. Any other failure means the
    // database never answered: 08006, 'connection failure'.
    private static DatabaseError Error(NpgsqlException error) => error switch
    {
        PostgresException refusal => new DatabaseError(refusal.SqlState, refusal.MessageText),
        { InnerException: TimeoutException } => new DatabaseError(PostgresErrorCodes.QueryCanceled, error.Message),
        _ => new DatabaseError(PostgresErrorCodes.ConnectionFailure, error.Message),
    };

    private static CommandDefinition Command(Query query, CancellationToken cancel) =>
        new(query.Text, query.Parameters, cancellationToken: cancel);
}
