using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Tessera.IntegrationTests;

/// <summary>
/// One fresh copy of the packaged database for a test class (US-01 c3): the image db/Dockerfile
/// builds, with the shipped role script, started in Docker.
/// </summary>
/// <remarks>
/// Docker runs a program inside a "container", a sealed box with its own files and network, so a
/// test gets a real PostgreSQL that vanishes afterwards. Testcontainers starts and stops the box.
/// </remarks>
public sealed class DatabaseFixture : IAsyncLifetime
{
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

    private PostgreSqlContainer? _container;

    public DatabaseAddress Reader => Address("tessera_reader", ReaderPassword);

    public DatabaseAddress Publisher => Address("tessera_publisher", PublisherPassword);

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

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>Runs SQL as the database's owner, to set the stage: make test tables, grant, revoke.</summary>
    public async Task Owner(string sql)
    {
        await using var connection = new NpgsqlConnection(_container!.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Reads one value as the owner.</summary>
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

    private DatabaseAddress Address(string user, string password) =>
        new(_container!.Hostname, _container.GetMappedPublicPort(5432), Name, user, password);
}
