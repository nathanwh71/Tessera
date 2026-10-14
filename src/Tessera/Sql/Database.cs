using System.Collections.Immutable;
using Dapper;
using Npgsql;
using Tessera.Kernel;

namespace Tessera.Sql;

/// <summary>Why the database refused a query: its SQLSTATE code and its message.</summary>
/// <remarks>
/// PostgreSQL, the database, labels each failure with a SQLSTATE code: five characters, of which the
/// first two name its class, such as 08 for a failed connection. Two codes mean a table has left the
/// list: 42501, 'insufficient privilege', when the table's grant (its permission to read) has gone
/// (US-03 c6), and 42P01, 'undefined table'. The codes in <see cref="IsUnavailable"/> mean the
/// database cannot answer now but may soon, so the service says 'try again' rather than 'fault' (N-22).
/// </remarks>
public sealed record DatabaseError(string SqlState, string Message)
{
    /// <summary>True when the table has gone, or its grant has: 42501 or 42P01 (US-03 c6).</summary>
    public bool IsMissingTable => SqlState is PostgresErrorCodes.InsufficientPrivilege or PostgresErrorCodes.UndefinedTable;

    /// <summary>
    /// True when a query names a column the table no longer holds (42703, 'undefined column'), as
    /// after an import drops a column the list of layers still names, until the list is read again.
    /// </summary>
    public bool IsMissingColumn => SqlState is PostgresErrorCodes.UndefinedColumn;

    /// <summary>
    /// True when waiting may cure the failure: the connection failed (class 08), a query ran past
    /// its time (57014), the server is shutting down, crashing or starting (57P01 to 57P03), or it has
    /// no room for another connection (53300).
    /// </summary>
    public bool IsUnavailable =>
        SqlState.StartsWith("08", StringComparison.Ordinal)
        || SqlState is PostgresErrorCodes.QueryCanceled or PostgresErrorCodes.AdminShutdown or PostgresErrorCodes.CrashShutdown
            or PostgresErrorCodes.CannotConnectNow or PostgresErrorCodes.TooManyConnections;
}

/// <summary>
/// The limits on the service's use of the database (N-22): the most connections the pool holds, how
/// long each lives, the longest a query may run, and the longest a request waits for a free
/// connection. A connection is an open line to the database; a connection pool keeps a few open for
/// requests to share.
/// </summary>
/// <remarks>
/// The Java build's tuning guide chose the defaults by measurement: 4 connections, each living one
/// hour, 10 seconds for a query and 3 seconds to wait.
/// </remarks>
public sealed record PoolLimits(int MaxConns, TimeSpan MaxConnLifeTime, TimeSpan CommandTimeout, TimeSpan ConnectionTimeout)
{
    public static readonly PoolLimits Default = new(4, TimeSpan.FromHours(1), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(3));
}

/// <summary>
/// The one gate between Tessera and PostgreSQL (S-01 c2, ADR 0001 rule 3). It belongs to the Sql
/// module: the Catalogue, Tiles and Import modules and the health check send every query through it
/// as a <see cref="Query"/>, and Program.cs opens it at start-up. No other project references Npgsql
/// or Dapper, the two libraries that talk to PostgreSQL.
/// </summary>
/// <remarks>
/// A database can fail at any moment, so this class, at the edge of the Sql module, turns each
/// failure into a <see cref="Result{T, TError}"/>, a value that holds either the answer or the reason
/// for a refusal, rather than letting an error escape. A connection pool keeps a few connections open
/// between requests, so a tile need not pay to open a new one each time.
/// <para>
/// Queries must run in the order they arrive (N-26, ADR 0006 rule 7). When every connection is busy,
/// Npgsql's pool lends the next free one to the request that has waited longest, so it keeps that
/// order itself. The Java build needs a gate in front of its pool for this; here an integration test,
/// one that runs against a real database, proves the pool's order.
/// </para>
/// <para>
/// The health check's ping has a pool of its own, of one connection, so it never waits behind tiles:
/// a busy service is still a healthy one (N-26 c3).
/// </para>
/// </remarks>
public sealed class Database : IAsyncDisposable
{
    private readonly NpgsqlDataSource _source;
    private readonly NpgsqlDataSource _ping;

    private Database(NpgsqlDataSource source, NpgsqlDataSource ping) => (_source, _ping) = (source, ping);

    /// <summary>The longest a ping may take, from connecting to the answer.</summary>
    internal static readonly TimeSpan PingLimit = TimeSpan.FromSeconds(1);

    /// <summary>Opens the pools with the <see cref="PoolLimits.Default"/> limits.</summary>
    public static Database Open(DatabaseAddress address) => Open(address, PoolLimits.Default);

    /// <summary>
    /// Opens a pool of connections within the given limits, and a second pool of one for the ping. A
    /// query past its time is cancelled on the server too, so it frees its connection; a request that
    /// finds every connection busy waits for one, then gives up.
    /// </summary>
    public static Database Open(DatabaseAddress address, PoolLimits limits) =>
        new(
            Source(address, limits, "tessera"),
            // One connection is enough: a ping is quick, and the watch sends one at a time. The ping
            // gets one second to connect and one second to answer.
            Source(address, limits with { MaxConns = 1, CommandTimeout = PingLimit, ConnectionTimeout = PingLimit }, "tessera-health", ping: true));

    private static NpgsqlDataSource Source(DatabaseAddress address, PoolLimits limits, string name, bool ping = false) =>
        NpgsqlDataSource.Create(new NpgsqlConnectionStringBuilder
        {
            Host = address.Host,
            Port = address.Port,
            Database = address.Database,
            Username = address.User,
            Password = address.Password,
            // The name shows in pg_stat_activity, the database's list of open connections, so the
            // maintainer can tell Tessera's connections apart.
            ApplicationName = name,
            MaxPoolSize = limits.MaxConns,
            ConnectionLifetime = (int)limits.MaxConnLifeTime.TotalSeconds,
            CommandTimeout = (int)limits.CommandTimeout.TotalSeconds,
            // Npgsql's Timeout, in seconds, covers both opening a connection and waiting for a free one
            // in the pool. It counts whole seconds, and 0 means 'wait for ever', so the wait is rounded
            // up: DbPoolConnectionTimeout of 250 ms becomes one second, not an endless wait.
            Timeout = WholeSeconds(limits.ConnectionTimeout),
            // Planning a query costs time, so a query text sent three times on a connection becomes a
            // prepared statement: the database plans it once and reuses the plan, as the Java build's
            // driver does (prepareThreshold=3). The tile query's text differs only by layer, so nine
            // plans cover every tile. Measured on 14 October 2026, with no tile cache, this raised the
            // rate from 1,580 to 2,408 tiles a second.
            MaxAutoPrepare = 32,
            AutoPrepareMinUsages = 3,
            // After a timeout Npgsql asks the server to cancel the query and, by default, waits 2,000
            // milliseconds for its reply; a frozen server never replies. The ping waits for none (-1),
            // so a slow ping lasts one second, not three.
            CancellationTimeout = ping ? -1 : 2000,
        }.ConnectionString);

    /// <summary>
    /// True when the database answers <c>SELECT 1</c> within a second, through the ping's own pool
    /// (N-25, N-26 c3). The ping keeps its own clock: a frozen database may hold Npgsql's own limits
    /// for longer, and a step it gives up on is left to finish, or fail, out of sight.
    /// </summary>
    public async Task<bool> Ping(CancellationToken stop)
    {
        try
        {
            await PingOnce(stop).WaitAsync(PingLimit, stop).ConfigureAwait(false);
            return true;
        }
        catch (Exception error) when (error is TimeoutException or NpgsqlException && !stop.IsCancellationRequested)
        {
            return false;
        }
    }

    private async Task PingOnce(CancellationToken stop)
    {
        await using var connection = await _ping.OpenConnectionAsync(stop).ConfigureAwait(false);
        await connection.QueryAsync<int>(Command(HealthQueries.Ping, null, stop)).ConfigureAwait(false);
    }

    /// <summary>
    /// A wait in whole seconds, rounded up and never below one, for Npgsql's <c>Timeout</c>, which
    /// counts whole seconds and reads 0 as 'wait for ever'.
    /// </summary>
    internal static int WholeSeconds(TimeSpan wait) => Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds));

    /// <summary>
    /// Runs a query and turns each row into a <typeparamref name="T"/>, matching columns to fields by name.
    /// </summary>
    public Task<Result<ImmutableArray<T>, DatabaseError>> Rows<T>(Query query, CancellationToken cancel = default) =>
        Run(async connection => (await connection.QueryAsync<T>(Command(query, null, cancel)).ConfigureAwait(false)).ToImmutableArray(), cancel);

    /// <summary>Runs a query that returns one value, or none.</summary>
    public Task<Result<Option<T>, DatabaseError>> Single<T>(Query query, CancellationToken cancel = default) where T : class =>
        Run(async connection => (await connection.QuerySingleOrDefaultAsync<T>(Command(query, null, cancel)).ConfigureAwait(false)).ToOption(), cancel);

    /// <summary>
    /// Runs every step in one transaction: all of them take effect, or none does (US-24 c6).
    /// </summary>
    /// <remarks>
    /// A transaction is a group of changes the database makes all together or not at all. If any
    /// step refuses, or the database fails, the whole group rolls back (is undone), and no reader ever
    /// sees half of it.
    /// <para>
    /// The steps refuse with the caller's own error type, so a step that refuses for a reason of its
    /// own, such as an odd column name, needs no database error to carry it; <paramref name="refused"/>
    /// turns a database failure into that same type.
    /// </para>
    /// </remarks>
    public async Task<Result<T, TError>> InTransaction<T, TError>(
        Func<Session, Task<Result<T, TError>>> steps, Func<DatabaseError, TError> refused, CancellationToken cancel = default)
    {
        try
        {
            await using var connection = await _source.OpenConnectionAsync(cancel).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancel).ConfigureAwait(false);
            var result = await steps(new Session(connection, transaction, cancel)).ConfigureAwait(false);
            await result.Match(
                _ => transaction.CommitAsync(cancel),
                _ => transaction.RollbackAsync(cancel)).ConfigureAwait(false);
            return result;
        }
        // A failure inside a step lands here too, and the transaction, closed unfinished, rolls back.
        catch (NpgsqlException error)
        {
            return Result.Refuse<T, TError>(refused(Error(error)));
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _source.DisposeAsync().ConfigureAwait(false);
        await _ping.DisposeAsync().ConfigureAwait(false);
    }

    // A cancellation token is a signal a caller can fire to stop work it no longer needs. It reaches
    // the wait for a connection too, so a query whose token fires leaves the pool's queue at once
    // (N-26 c2).
    private async Task<Result<T, DatabaseError>> Run<T>(Func<NpgsqlConnection, Task<T>> work, CancellationToken cancel)
    {
        try
        {
            await using var connection = await _source.OpenConnectionAsync(cancel).ConfigureAwait(false);
            return Result.Ok<T, DatabaseError>(await work(connection).ConfigureAwait(false));
        }
        catch (NpgsqlException error)
        {
            return Result.Refuse<T, DatabaseError>(Error(error));
        }
    }

    // A PostgresException is the database's own refusal, with its SQLSTATE. Npgsql reports a query
    // past its time, or a wait for a connection past its time, with a TimeoutException inside; 57014,
    // 'query cancelled', names both. Any other NpgsqlException means the database never answered,
    // such as when it is down; 08006, 'connection failure', names that case.
    private static DatabaseError Error(NpgsqlException error) => error switch
    {
        PostgresException refusal => new DatabaseError(refusal.SqlState, refusal.MessageText),
        { InnerException: TimeoutException } => new DatabaseError(PostgresErrorCodes.QueryCanceled, error.Message),
        _ => new DatabaseError(PostgresErrorCodes.ConnectionFailure, error.Message),
    };

    internal static CommandDefinition Command(Query query, NpgsqlTransaction? transaction, CancellationToken cancel)
    {
        var parameters = new DynamicParameters();
        for (var index = 0; index < query.Parameters.Length; index++)
            // Dapper turns a parameter of null into the database's NULL, so SqlNull becomes null here.
            parameters.Add($"p{index}", query.Parameters[index] is SqlNull ? null : query.Parameters[index]);
        return new CommandDefinition(query.Text, parameters, transaction, cancellationToken: cancel);
    }

    /// <summary>The steps of one transaction. It runs queries on the transaction's own connection.</summary>
    /// <remarks>
    /// Its methods always return Ok: a database failure throws instead, so it ends every step at once
    /// and reaches <see cref="Database.InTransaction{T, TError}"/>, which rolls the whole group back.
    /// </remarks>
    public sealed class Session
    {
        private readonly NpgsqlConnection _connection;
        private readonly NpgsqlTransaction _transaction;
        private readonly CancellationToken _cancel;

        internal Session(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancel) =>
            (_connection, _transaction, _cancel) = (connection, transaction, cancel);

        /// <summary>
        /// Runs a query that changes the database, and returns the number of rows it changed.
        /// </summary>
        public async Task<Result<int, DatabaseError>> Execute(Query query) =>
            Result.Ok<int, DatabaseError>(await _connection.ExecuteAsync(Command(query, _transaction, _cancel)).ConfigureAwait(false));

        public async Task<Result<ImmutableArray<T>, DatabaseError>> Rows<T>(Query query) =>
            Result.Ok<ImmutableArray<T>, DatabaseError>(
                (await _connection.QueryAsync<T>(Command(query, _transaction, _cancel)).ConfigureAwait(false)).ToImmutableArray());
    }
}
