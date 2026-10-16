using System.ComponentModel.DataAnnotations;
using CSharpFunctionalExtensions;
using Npgsql;

// The database's address, as the environment gives it (US-00 c4). It lives in the Infrastructure,
// the one layer that may use the database client, Npgsql (ADR 0010, rule 2).

namespace Modules.Tessera.Infrastructure.Sql;

/// <summary>The database's connection, from the URL in <see cref="Variable"/>, checked at start-up.</summary>
public sealed class DatabaseConnection : IValidatableObject
{
    /// <summary>The <b><i>environment variable</i></b> that held the URL, named in any problem.</summary>
    public string Variable { get; set; } = DatabaseConnections.ReaderDatabase;

    /// <summary>
    /// The URL as the environment gave it; null when the variable is missing, so the check can say so.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>Npgsql's connection string, or empty text while the URL fails the check.</summary>
    public string Text => DatabaseConnections.ConnectionString(Url).GetValueOrDefault("");

    /// <summary>
    /// Refuses a missing or malformed URL and names its variable, so the service will not start
    /// without a well-formed address for its database (US-00 c4).
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext context) =>
        DatabaseConnections.ConnectionString(Url) is { IsFailure: true } refused
            ? [new ValidationResult($"{Variable}: {refused.Error}")]
            : [];
}

/// <summary>
/// Reads and checks the database's URL. It lives here, not with the other settings, since only the
/// Infrastructure may use Npgsql, whose connection string the URL becomes (ADR 0010, rule 2).
/// </summary>
public static class DatabaseConnections
{
    /// <summary>The database URL the service reads tiles with; pg_tileserv uses the same name.</summary>
    public const string ReaderDatabase = "DATABASE_URL";

    /// <summary>
    /// The database URL the seed writes places with. It signs in as a second <b><i>role</i></b>,
    /// tessera_publisher, so the service never holds the right to write while it serves tiles
    /// (ADR 0001).
    /// </summary>
    public const string PublisherDatabase = "TESSERA_PUBLISHER_DATABASE_URL";

    /// <summary>
    /// Reads the database URL from <paramref name="variable"/>, such as DATABASE_URL, and checks it
    /// when the program starts, with every other setting (Settings.Check).
    /// </summary>
    public static void Register(IServiceCollection services, IConfiguration configuration, string variable) =>
        services.AddOptions<DatabaseConnection>()
            .Configure(connection => (connection.Variable, connection.Url) = (variable, configuration[variable]))
            .ValidateDataAnnotations()
            .ValidateOnStart();

    /// <summary>
    /// Turns <c>postgresql://user:password@host:port/database</c> into Npgsql's connection string;
    /// with no port, PostgreSQL's usual 5432. A password may hold any character, so a character such
    /// as '@' arrives percent-encoded, here %40.
    /// </summary>
    public static Result<string, string> ConnectionString(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return Result.Failure<string, string>("missing or empty");
        // PostgreSQL's own tools accept both 'postgresql' and 'postgres', so a URL copied from them
        // reads.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("postgresql" or "postgres")
            || uri.UserInfo.Split(':', 2) is not [{ Length: > 0 } user, var password] || uri.AbsolutePath.Length < 2)
            return Result.Failure<string, string>("expected postgresql://user:password@host:port/database");
        return Result.Success<string, string>(new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port < 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath[1..]),
            Username = Uri.UnescapeDataString(user),
            Password = Uri.UnescapeDataString(password),
        }.ConnectionString);
    }
}
