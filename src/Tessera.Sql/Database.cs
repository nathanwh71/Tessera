using System.Collections.Immutable;
using Dapper;
using Npgsql;
using Tessera.Kernel;

namespace Tessera.Sql;

/// <summary>Why the database refused a query: its five-character SQLSTATE code and its message.</summary>
/// <remarks>
/// PostgreSQL labels each failure with a SQLSTATE code. Two matter to Tessera: 42501, "insufficient
/// privilege", when a table's grant has gone (US-03 c6), and 42P01, "undefined table".
/// </remarks>
public sealed record DatabaseError(string SqlState, string Message)
{
    public bool IsMissingTable => SqlState is PostgresErrorCodes.InsufficientPrivilege or PostgresErrorCodes.UndefinedTable;
}

/// <summary>
/// The one gate between Tessera and PostgreSQL (S-01 c2, ADR 0001 rule 3). Every query passes
/// through here as a <see cref="Query"/>, and no other project references Npgsql or Dapper.
/// </summary>
/// <remarks>
/// This class sits at the edge of the Sql module, where a database can fail, so it turns each
/// database failure into a <see cref="Result{T, TError}"/> value. A connection pool keeps a few
/// connections open between requests, so a tile need not pay for a new one each time.
/// </remarks>
public sealed class Database : IAsyncDisposable
{
    private readonly NpgsqlDataSource _source;

    private Database(NpgsqlDataSource source) => _source = source;

    public static Database Open(DatabaseAddress address) =>
        new(NpgsqlDataSource.Create(new NpgsqlConnectionStringBuilder
        {
            Host = address.Host,
            Port = address.Port,
            Database = address.Database,
            Username = address.User,
            Password = address.Password,
            // The name shows in pg_stat_activity, so the maintainer can tell Tessera's connections apart.
            ApplicationName = "tessera",
        }.ConnectionString));

    /// <summary>Runs a query and maps each row to <typeparamref name="T"/> by column name.</summary>
    public Task<Result<ImmutableArray<T>, DatabaseError>> Rows<T>(Query query, CancellationToken cancel = default) =>
        Run(async connection => (await connection.QueryAsync<T>(Command(query, null, cancel)).ConfigureAwait(false)).ToImmutableArray());

    /// <summary>Runs a query that returns one value, or none.</summary>
    public Task<Result<Option<T>, DatabaseError>> Single<T>(Query query, CancellationToken cancel = default) where T : class =>
        Run(async connection => (await connection.QuerySingleOrDefaultAsync<T>(Command(query, null, cancel)).ConfigureAwait(false)).ToOption());

    /// <summary>
    /// Runs every step in one transaction: all of them take effect, or none does (US-24 c6).
    /// </summary>
    /// <remarks>
    /// A transaction groups commands so the database applies them together. If any step refuses, or
    /// the database fails, the whole group rolls back, and no reader ever sees half of it.
    /// </remarks>
    public async Task<Result<T, DatabaseError>> InTransaction<T>(
        Func<Session, Task<Result<T, DatabaseError>>> steps, CancellationToken cancel = default)
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
        catch (PostgresException error)
        {
            return Result.Refuse<T, DatabaseError>(new DatabaseError(error.SqlState, error.MessageText));
        }
    }

    public ValueTask DisposeAsync() => _source.DisposeAsync();

    private async Task<Result<T, DatabaseError>> Run<T>(Func<NpgsqlConnection, Task<T>> work)
    {
        try
        {
            await using var connection = await _source.OpenConnectionAsync().ConfigureAwait(false);
            return Result.Ok<T, DatabaseError>(await work(connection).ConfigureAwait(false));
        }
        catch (PostgresException error)
        {
            return Result.Refuse<T, DatabaseError>(new DatabaseError(error.SqlState, error.MessageText));
        }
    }

    internal static CommandDefinition Command(Query query, NpgsqlTransaction? transaction, CancellationToken cancel)
    {
        var parameters = new DynamicParameters();
        for (var index = 0; index < query.Parameters.Length; index++)
            parameters.Add($"p{index}", query.Parameters[index]);
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
