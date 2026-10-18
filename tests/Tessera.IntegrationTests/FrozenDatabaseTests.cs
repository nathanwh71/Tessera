using System.Diagnostics;
using Modules.Tessera.Infrastructure;
using Modules.Tessera.Infrastructure.Sql;

namespace Tessera.IntegrationTests;

/// <summary>
/// N-22 c2 and N-29: with the database frozen, a query gives up within the limits the settings
/// promise, ConnectionWaitSeconds and QueryTimeoutSeconds together, so the service answers its own
/// 503 before Nginx's proxy_read_timeout runs out.
/// </summary>
/// <remarks>
/// The class has a database of its own, since freezing it would stall any other test that shared
/// it.
/// </remarks>
public sealed class FrozenDatabaseTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    // Short limits, so the test runs in seconds; the rule is the same at the shipped 3 and 10.
    private static readonly DatabaseSettings Short = new() { PoolSize = 1, ConnectionWaitSeconds = 1, QueryTimeoutSeconds = 2 };

    // The pool already holds an open connection, as a copy that served a tile a moment ago does,
    // so the query meets the frozen database on that connection rather than when it opens one.
    // Npgsql, left to itself, then spends a further two connection waits trying to cancel the
    // query on a server that never answers: 4 seconds here, and 16 at the shipped settings.
    [Fact]
    public async Task Gives_up_on_a_frozen_database_within_the_settings_limits()
    {
        await using var reader = Database.Open(database.Reader, Short);
        Assert.True((await reader.Rows<int>(Queries.Ping, TestContext.Current.CancellationToken)).IsSuccess);

        await database.Pause();
        try
        {
            var clock = Stopwatch.StartNew();
            var answer = await reader.Rows<int>(Queries.Ping, TestContext.Current.CancellationToken);

            Assert.IsType<DatabaseError.Unavailable>(answer.Error);
            // The limits' sum, 3 seconds, and half a second for the work around them.
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(3.5), $"took {clock.Elapsed}");
        }
        finally
        {
            await database.Unpause();
        }
    }
}
