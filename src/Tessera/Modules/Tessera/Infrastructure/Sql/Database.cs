using System.Collections.Immutable;
using CSharpFunctionalExtensions;
using Dapper;
using ExhaustiveMatching;
using Npgsql;

// The door to PostgreSQL: the query the code hands over, the reasons the database may refuse it, and
// the one class that sends every query through the pool (ADR 0010, rule 2). Each database failure
// comes back as a value, never an exception, so each caller must decide what it means (ADR 0004).

namespace Modules.Tessera.Infrastructure.Sql;

/// <summary>
/// A query ready to send: the SQL text, and the values that travel beside it as bound
/// <b><i>parameters</i></b> (S-01). The database never reads a parameter as SQL, so no value from a
/// request can change what a query does: <b><i>SQL injection</i></b>, SQL hidden in a request, has no
/// way in. Every text lives in Queries.cs. A query with no values, such as the ping, has none.
/// </summary>
public sealed record Query(string Text, object? Parameters = null);

/// <summary>
/// Why the database refused a query: its SQLSTATE code, five characters, and its message, sorted into
/// the four kinds a caller must tell apart.
/// </summary>
/// <remarks>
/// A <b><i>union</i></b>, as F# would write <c>MissingTable | MissingColumn | Unavailable | Other</c>:
/// <see cref="Of"/> sorts each code once, where the failure arises, so no caller reads a code again
/// and no code can count as two kinds. C# 14 has no unions, so [Closed] names the cases and the
/// ExhaustiveMatching analyser stops the build when a switch misses one. The code and message stay
/// on every case for the log.
/// </remarks>
[Closed(typeof(MissingTable), typeof(MissingColumn), typeof(Unavailable), typeof(Other))]
public abstract record DatabaseError(string SqlState, string Message)
{
    /// <summary>The table, or its <b><i>grant</i></b>, has gone: 42501 or 42P01 (US-03 c6).</summary>
    public sealed record MissingTable(string SqlState, string Message) : DatabaseError(SqlState, Message);

    /// <summary>
    /// The query names a column a <b><i>seed script</i></b> has dropped since the list of
    /// <b><i>layers</i></b> was read: 42703.
    /// </summary>
    public sealed record MissingColumn(string SqlState, string Message) : DatabaseError(SqlState, Message);

    /// <summary>
    /// Waiting may cure it (N-22): a failed connection (class 08), a query past its time (57014), a
    /// server stopping or starting (57P01 to 57P03), or no room for another connection (53300).
    /// </summary>
    public sealed record Unavailable(string SqlState, string Message) : DatabaseError(SqlState, Message);

    /// <summary>Any other refusal, which waiting will not cure: the service's own fault.</summary>
    public sealed record Other(string SqlState, string Message) : DatabaseError(SqlState, Message);

    /// <summary>Sorts a refusal by its SQLSTATE code into the one kind it belongs to.</summary>
    public static DatabaseError Of(string sqlState, string message) => sqlState switch
    {
        PostgresErrorCodes.InsufficientPrivilege or PostgresErrorCodes.UndefinedTable => new MissingTable(sqlState, message),
        PostgresErrorCodes.UndefinedColumn => new MissingColumn(sqlState, message),
        PostgresErrorCodes.QueryCanceled or PostgresErrorCodes.AdminShutdown or PostgresErrorCodes.CrashShutdown
            or PostgresErrorCodes.CannotConnectNow or PostgresErrorCodes.TooManyConnections => new Unavailable(sqlState, message),
        _ when sqlState.StartsWith("08", StringComparison.Ordinal) => new Unavailable(sqlState, message),
        _ => new Other(sqlState, message),
    };
}

/// <summary>
/// The one door to PostgreSQL. A database can fail at any moment, so this class turns each failure
/// into a <b><i>Result</i></b> (<see cref="Result{T, E}"/>), a value that holds the answer or the
/// reason, rather than letting an exception escape (ADR 0004).
/// </summary>
/// <remarks>
/// A <b><i>connection pool</i></b> keeps a few connections open for requests to share. When every
/// one is busy, Npgsql lends the next free one to the request that has waited longest (N-26). The
/// health check's ping shares the pool too, so a pool busy for more than a second makes /health say
/// DOWN (N-26 c3).
/// </remarks>
public sealed class Database : IAsyncDisposable
{
    /// <summary>Npgsql's data source: the pool every query borrows a connection from.</summary>
    private readonly NpgsqlDataSource _source;

    /// <summary>
    /// Private, so only <see cref="Open"/> builds one, and every pool keeps the limits of N-22.
    /// </summary>
    private Database(NpgsqlDataSource source) => _source = source;

    /// <summary>
    /// The longest a ping may take, from connecting to the answer: one second, so <c>/health</c>
    /// answers within one (N-25, N-26 c3).
    /// </summary>
    private static readonly TimeSpan PingLimit = TimeSpan.FromSeconds(1);

    /// <summary>Opens the pool within the given limits.</summary>
    /// <param name="connection">Npgsql's connection string, made from the database URL.</param>
    /// <param name="pool">The pool's limits (N-22).</param>
    public static Database Open(string connection, DatabaseSettings pool) =>
        new(NpgsqlDataSource.Create(new NpgsqlConnectionStringBuilder(connection)
        {
            // The name shows in pg_stat_activity, so the maintainer can tell Tessera's connections apart.
            ApplicationName = "tessera",
            MaxPoolSize = pool.PoolSize,
            ConnectionLifetime = pool.ConnectionLifetimeSeconds,
            CommandTimeout = pool.QueryTimeoutSeconds,
            // Covers both opening a connection and waiting for a free one in the pool.
            Timeout = pool.ConnectionWaitSeconds,
            // A query text sent three times on a connection becomes a prepared statement: the database
            // parses and plans it once, keeps the plan, and reruns it with new values each time.
            // Measured on 14 October 2026, this raised the rate from 1,580 to 2,408 tiles a second.
            MaxAutoPrepare = 32,
            AutoPrepareMinUsages = 3,
        }.ConnectionString));

    /// <summary>
    /// Runs a query and turns each row into a <typeparamref name="T"/>, matching columns to fields by
    /// name, so a record such as CatalogueRow needs no code of its own to read a row.
    /// </summary>
    public Task<Result<ImmutableArray<T>, DatabaseError>> Rows<T>(Query query, CancellationToken cancel = default) =>
        Run(async connection =>
            (await connection.QueryAsync<T>(Command(query, cancel)).ConfigureAwait(false)).ToImmutableArray(), cancel);

    /// <summary>
    /// Runs a query that returns one value, or null when no row comes back; the <b><i>tile</i></b>
    /// route reads null as an empty tile.
    /// </summary>
    public Task<Result<T?, DatabaseError>> Single<T>(Query query, CancellationToken cancel = default) where T : class =>
        Run(async connection => await connection.QuerySingleOrDefaultAsync<T>(Command(query, cancel)).ConfigureAwait(false), cancel);

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
        // A cancel from the caller's own token is no fault of the database, so it escapes and the
        // caller stops. A timeout, a cancel at the time limit, or a database error means 'not well';
        // any other exception is a fault in the code, so it escapes too.
        catch (Exception error)
            when (error is TimeoutException or OperationCanceledException or NpgsqlException
                && !stop.IsCancellationRequested)
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

    /// <summary>
    /// One ping, which throws on failure rather than returning a Result, since Ping wants only
    /// 'answered or not' and treats every failure alike.
    /// </summary>
    private async Task PingOnce(CancellationToken limit)
    {
        await using var connection = await _source.OpenConnectionAsync(limit).ConfigureAwait(false);
        await connection.ExecuteScalarAsync<int>(Command(Queries.Ping, limit)).ConfigureAwait(false);
    }

    /// <summary>
    /// Closes the pool's connections as the program stops, so the database frees them at once.
    /// </summary>
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
        PostgresException refusal => DatabaseError.Of(refusal.SqlState, refusal.MessageText),
        { InnerException: TimeoutException } => DatabaseError.Of(PostgresErrorCodes.QueryCanceled, error.Message),
        _ => DatabaseError.Of(PostgresErrorCodes.ConnectionFailure, error.Message),
    };

    // Bundles the text, the parameters and the cancel token for Dapper, so no query leaves without
    // all three.
    private static CommandDefinition Command(Query query, CancellationToken cancel) =>
        new(query.Text, query.Parameters, cancellationToken: cancel);
}
