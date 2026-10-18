using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Tessera.IntegrationTests;

/// <summary>
/// One fresh copy of the packaged database for a test class (US-01 c3): the image db/Dockerfile
/// builds, with the shipped script that makes each <b><i>role</i></b>, started in Docker.
/// </summary>
/// <remarks>
/// Docker runs a program inside a 'container', a sealed box with its own files and network, so a
/// test gets a real PostgreSQL that vanishes afterwards. Testcontainers starts and stops the box.
/// </remarks>
public sealed class DatabaseFixture : IAsyncLifetime
{
    /// <summary>
    /// The database's name and the three roles' passwords. They may sit in plain sight: each
    /// container lives only as long as its tests and holds test data alone. The role script
    /// reads the reader's and the publisher's from <b><i>environment variable</i></b>s, as in
    /// production; the owner's stays private, since only this fixture signs in as the owner.
    /// </summary>
    public const string Name = "tessera";
    public const string ReaderPassword = "reader-test";
    public const string PublisherPassword = "publisher-test";
    private const string OwnerPassword = "owner-test";

    // Built once for every class; Docker reuses the layers, so later builds take a moment.
    private static readonly Lazy<Task<IFutureDockerImage>> Image = new(async () =>
    {
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetGitDirectory(), "db")
            .WithName("tessera-db-test:latest")
            .WithCleanUp(false)
            .Build();
        await image.CreateAsync().ConfigureAwait(false);
        return image;
    });

    /// <summary>The running database; null until InitializeAsync starts it.</summary>
    private PostgreSqlContainer? _container;

    /// <summary>
    /// Npgsql's connection string for tessera_reader, the role the service signs in as, so a test
    /// sees exactly what the service may see and no more (US-01 test 1).
    /// </summary>
    public string Reader => Connection("tessera_reader", ReaderPassword);

    /// <summary>The same sign-in as a database URL, the form DATABASE_URL takes.</summary>
    public string ReaderUrl => $"postgresql://tessera_reader:{ReaderPassword}@{_container!.Hostname}:{_container.GetMappedPublicPort(5432)}/{Name}";

    /// <summary>
    /// The connection string for tessera_publisher, the role that runs each
    /// <b><i>seed script</i></b> (ADR 0009), so the seeding tests run with its rights alone.
    /// </summary>
    public string Publisher => Connection("tessera_publisher", PublisherPassword);

    /// <summary>
    /// Starts the database. The owner is 'postgres', who may make the stage's tables and grants; the
    /// two passwords go in as the variables the role script reads, since it refuses to start
    /// without them.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        _container = new PostgreSqlBuilder(await Image.Value.ConfigureAwait(false))
            .WithDatabase(Name)
            .WithUsername("postgres")
            .WithPassword(OwnerPassword)
            .WithEnvironment("TESSERA_READER_PASSWORD", ReaderPassword)
            .WithEnvironment("TESSERA_PUBLISHER_PASSWORD", PublisherPassword)
            .Build();
        await _container.StartAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Stops and removes the database. The check covers a start that failed before the container
    /// existed, so the real failure is the one reported.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Freezes the database, as <c>docker pause</c> does: every process in the container stops,
    /// yet the machine still accepts each connection, so a client waits on a server that never
    /// answers. A database that hangs this way is harder on the service than one that has stopped.
    /// </summary>
    public Task Pause() => _container!.PauseAsync();

    /// <summary>Lets a frozen database run again.</summary>
    public Task Unpause() => _container!.UnpauseAsync();

    /// <summary>Runs SQL as the database's owner, to set the stage: make test tables, grant, revoke.</summary>
    public async Task Owner(string sql)
    {
        await using var connection = new NpgsqlConnection(_container!.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Reads one value as the owner, who sees every table and setting, so a test can check what the
    /// reader cannot see, such as row counts before a grant or a role's settings.
    /// </summary>
    public async Task<T> OwnerValue<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(_container!.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>Grants the four schemas and their nine tables to the service's role, as the publisher would.</summary>
    public Task GrantPlaces() => Owner("""
        GRANT USAGE ON SCHEMA food, outdoors, culture, transport TO tessera_reader;
        GRANT SELECT ON ALL TABLES IN SCHEMA food, outdoors, culture, transport TO tessera_reader;
        """);

    /// <summary>
    /// A connection string for one role. Docker maps the container's port 5432 to a free port on
    /// this machine, so two test classes' databases never clash; the string names that port.
    /// </summary>
    private string Connection(string user, string password) => new NpgsqlConnectionStringBuilder
    {
        Host = _container!.Hostname,
        Port = _container.GetMappedPublicPort(5432),
        Database = Name,
        Username = user,
        Password = password,
    }.ConnectionString;
}
