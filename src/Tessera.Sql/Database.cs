using System.Collections.Immutable;
using Dapper;
using Npgsql;
using Tessera.Kernel;

namespace Tessera.Sql;

/// <summary>Why the database refused a query: its five-character SQLSTATE code and its message.</summary>
/// <remarks>
/// PostgreSQL labels each failure with a SQLSTATE code; the first two characters name its class.
/// Two codes mean a table has left: 42501, "insufficient privilege", when a table's grant has gone
/// (US-03 c6), and 42P01, "undefined table". Others mean the database cannot answer now but may
/// soon, so the service says 'try again' rather than 'fault' (N-22).
/// </remarks>
public sealed record DatabaseError(string SqlState, string Message)
{
    public bool IsMissingTable => SqlState is PostgresErrorCodes.InsufficientPrivilege or PostgresErrorCodes.UndefinedTable;

    /// <summary>
    /// True when waiting may cure the failure: the connection failed (class 08), a query ran past
    /// its time (57014), the server is shutting down or starting (57P01 to 57P03), or it has no
    /// room for another connection (53300).
    /// </summary>
    public bool IsUnavailable =>
        SqlState.StartsWith("08", StringComparison.Ordinal)
        || SqlState is PostgresErrorCodes.QueryCanceled or PostgresErrorCodes.AdminShutdown or PostgresErrorCodes.CrashShutdown
            or PostgresErrorCodes.CannotConnectNow or PostgresErrorCodes.TooManyConnections;
}

/// <summary>
/// The limits on the service's use of the database (N-22): the most connections the pool holds,
/// how long each lives, the longest a query may run, and the longest a request waits for a free
/// connection. The Java build's tuning guide chose the defaults by measurement.
/// </summary>
public sealed record PoolLimits(int MaxConns, TimeSpan MaxConnLifeTime, TimeSpan CommandTimeout, TimeSpan ConnectionTimeout)
{
    public static readonly PoolLimits Default = new(4, TimeSpan.FromHours(1), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(3));
}

/// <summary>
/// The one gate between Tessera and PostgreSQL (S-01 c2, ADR 0001 rule 3). Every query passes
/// through here as a <see cref="Query"/>, and no other project references Npgsql or Dapper.
/// </summary>
/// <remarks>
/// This class sits at the edge of the Sql module, where a database can fail, so it turns each
/// database failure into a <see cref="Result{T, TError}"/> value. A connection pool keeps a few
/// connections open between requests, so a tile need not pay for a new one each time.
/// <para>
/// When every connection is busy, Npgsql's pool lends the next free one to the request that has
/// waited longest, so queries run in the order they arrive (N-26, ADR 0006 rule 7). The Java
/// build needs a gate in front of its pool for this; Npgsql's pool keeps the order itself, and
/// an integration test proves it.
/// </para>
/// <para>
/// The health check's ping has a pool of its own, of one connection, so it never waits behind
/// tiles: a busy service is still a healthy one (N-26 c3).
/// </para>
/// <para>
/// A <see cref="DatabaseWatch"/> pings on that pool. While it says the database is down, every
/// query is refused at once with 08006, which the service answers 503, rather than wait out the
/// pool's timeout on a database that hangs (N-28).
/// </para>
/// </remarks>
public sealed class Database : IAsyncDisposable
{
    private readonly NpgsqlDataSource _source;
    private readonly NpgsqlDataSource _ping;

    private Database(NpgsqlDataSource source, NpgsqlDataSource ping)
    {
        (_source, _ping) = (source, ping);
        Watch = new DatabaseWatch(PingOnce);
    }

    /// <summary>
    /// Whether the database is up (N-28). Nothing pings until the program starts its loop with
    /// <see cref="DatabaseWatch.Run"/>, so until then it says up, as for the import.
    /// </summary>
    public DatabaseWatch Watch { get; }

    /// <summary>How long a ping may take to connect and to answer.</summary>
    internal static readonly TimeSpan PingLimit = TimeSpan.FromSeconds(1);

    public static Database Open(DatabaseAddress address) => Open(address, PoolLimits.Default);

    /// <summary>
    /// Opens a pool of connections within the given limits. A query past its time is cancelled on
    /// the server too, so it frees its connection; a request that finds every connection busy
    /// waits for one, then gives up.
    /// </summary>
    public static Database Open(DatabaseAddress address, PoolLimits limits) =>
        new(
            Source(address, limits, "tessera"),
            // One connection is enough: a ping is quick, and the watch asks one at a time. A ping that
            // takes longer than a second is slow, both to connect and to answer.
            Source(address, limits with { MaxConns = 1, CommandTimeout = PingLimit, ConnectionTimeout = PingLimit }, "tessera-health", ping: true));

    private static NpgsqlDataSource Source(DatabaseAddress address, PoolLimits limits, string name, bool ping = false) =>
        NpgsqlDataSource.Create(new NpgsqlConnectionStringBuilder
        {
            Host = address.Host,
            Port = address.Port,
            Database = address.Database,
            Username = address.User,
            Password = address.Password,
            // The name shows in pg_stat_activity, so the maintainer can tell Tessera's connections apart.
            ApplicationName = name,
            MaxPoolSize = limits.MaxConns,
            ConnectionLifetime = (int)limits.MaxConnLifeTime.TotalSeconds,
            CommandTimeout = (int)limits.CommandTimeout.TotalSeconds,
            // Npgsql's Timeout covers both opening a connection and waiting for a free one in the pool.
            Timeout = (int)limits.ConnectionTimeout.TotalSeconds,
            // A query text sent three times on a connection becomes a prepared statement: the database
            // plans it once and reuses the plan, as the Java build's driver does (prepareThreshold=3).
            // The tile query's text differs only by layer, so nine plans cover every tile. Measured on
            // 14 October 2026, with no tile cache, this raised the rate from 1,580 to 2,408 tiles a second.
            MaxAutoPrepare = 32,
            AutoPrepareMinUsages = 3,
            // After a timeout Npgsql asks the server to cancel the query and, by default, waits two
            // seconds for its reply; a frozen server never replies. The ping waits for none, so a
            // slow ping lasts one second, not three.
            CancellationTimeout = ping ? -1 : 2000,
        }.ConnectionString);

    /// <summary>
    /// One ping, <c>SELECT 1</c>, through the ping's own pool, so it never waits behind tile queries
    /// (N-26 c3). A line that will not open is <see cref="Sql.Ping.Refused"/>; an open line that does
    /// not answer within a second is <see cref="Sql.Ping.Slow"/> (N-28).
    /// </summary>
    private async Task<Ping> PingOnce(CancellationToken stop)
    {
        // A backstop half a second beyond Npgsql's own limits, in case either waits longer than it says.
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(stop);
        limit.CancelAfter(PingLimit + TimeSpan.FromMilliseconds(500));
        NpgsqlConnection connection;
        try
        {
            connection = await _ping.OpenConnectionAsync(limit.Token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is NpgsqlException or OperationCanceledException && !stop.IsCancellationRequested)
        {
            return Sql.Ping.Refused;
        }
        await using (connection.ConfigureAwait(false))
        {
            try
            {
                await connection.QueryAsync<int>(Command(HealthQueries.Ping, null, limit.Token)).ConfigureAwait(false);
                return Sql.Ping.Answered;
            }
            catch (Exception error) when (error is NpgsqlException or OperationCanceledException && !stop.IsCancellationRequested)
            {
                return Sql.Ping.Slow;
            }
        }
    }

    /// <summary>Runs a query and maps each row to <typeparamref name="T"/> by column name.</summary>
    public Task<Result<ImmutableArray<T>, DatabaseError>> Rows<T>(Query query, CancellationToken cancel = default) =>
        Run(async connection => (await connection.QueryAsync<T>(Command(query, null, cancel)).ConfigureAwait(false)).ToImmutableArray(), cancel);

    /// <summary>Runs a query that returns one value, or none.</summary>
    public Task<Result<Option<T>, DatabaseError>> Single<T>(Query query, CancellationToken cancel = default) where T : class =>
        Run(async connection => (await connection.QuerySingleOrDefaultAsync<T>(Command(query, null, cancel)).ConfigureAwait(false)).ToOption(), cancel);

    /// <summary>
    /// Runs every step in one transaction: all of them take effect, or none does (US-24 c6).
    /// </summary>
    /// <remarks>
    /// A transaction groups commands so the database applies them together. If any step refuses, or
    /// the database fails, the whole group rolls back, and no reader ever sees half of it.
    /// The steps refuse with the caller's own error type, so a step that refuses for a reason of its
    /// own, such as an odd column name, needs no database error to carry it; <paramref name="refused"/>
    /// turns a database failure into that same type.
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

    // The token reaches the wait for a connection too, so a query whose token is cancelled steps out
    // of the pool's queue at once rather than holding a place in it (N-26 c2). A tile query passes
    // the service's stop token, not the browser's, so it stays in the queue when the browser leaves:
    // other people may share it (N-24).
    // While the watch says down, the query is refused before it waits for a connection (N-28).
    private async Task<Result<T, DatabaseError>> Run<T>(Func<NpgsqlConnection, Task<T>> work, CancellationToken cancel)
    {
        if (!Watch.Up)
        {
            return Result.Refuse<T, DatabaseError>(Down);
        }
        try
        {
            await using var connection = await _source.OpenConnectionAsync(cancel).ConfigureAwait(false);
            return Result.Ok<T, DatabaseError>(await work(connection).ConfigureAwait(false));
        }
        catch (NpgsqlException error)
        {
            var failure = Error(error);
            if (LostConnection(failure))
            {
                Watch.LostConnection();
            }
            return Result.Refuse<T, DatabaseError>(failure);
        }
    }

    private static readonly DatabaseError Down = new(PostgresErrorCodes.ConnectionFailure, "The database is down; no query is sent until it answers again.");

    /// <summary>
    /// Whether a failed query proves the database is down: SQLSTATE class 08, a failed connection,
    /// or 57P01 to 57P03, a server shutting down, crashing or starting. A timeout, 57014, is no
    /// proof, since a busy database times out too; the watch's pings judge that case.
    /// </summary>
    internal static bool LostConnection(DatabaseError failure) =>
        failure.SqlState.StartsWith("08", StringComparison.Ordinal)
        || failure.SqlState is PostgresErrorCodes.AdminShutdown or PostgresErrorCodes.CrashShutdown or PostgresErrorCodes.CannotConnectNow;

    // A PostgresException is the database's own refusal, with its SQLSTATE. Npgsql reports a query
    // past its time, or a wait for a connection past its time, with a TimeoutException inside; 57014,
    // "query cancelled", names both. Any other NpgsqlException means the database never answered,
    // such as when it is down; 08006, "connection failure", names that case.
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
    public sealed class Session
    {
        private readonly NpgsqlConnection _connection;
        private readonly NpgsqlTransaction _transaction;
        private readonly CancellationToken _cancel;

        internal Session(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancel) =>
            (_connection, _transaction, _cancel) = (connection, transaction, cancel);

        public async Task<Result<int, DatabaseError>> Execute(Query query) =>
            Result.Ok<int, DatabaseError>(await _connection.ExecuteAsync(Command(query, _transaction, _cancel)).ConfigureAwait(false));

        public async Task<Result<ImmutableArray<T>, DatabaseError>> Rows<T>(Query query) =>
            Result.Ok<ImmutableArray<T>, DatabaseError>(
                (await _connection.QueryAsync<T>(Command(query, _transaction, _cancel)).ConfigureAwait(false)).ToImmutableArray());
    }
}
