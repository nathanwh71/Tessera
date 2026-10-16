using System.ComponentModel.DataAnnotations;
using CSharpFunctionalExtensions;
using Npgsql;

// The database's address, as the environment gives it (US-00 c4). It lives in the Infrastructure,
// the one layer that may use the database client, Npgsql (ADR 0010, rule 2).

namespace Modules.Tessera.Infrastructure.Sql;

/// <summary>
/// Npgsql's connection string, made only by parsing a database URL, so holding one proves the URL was
/// well formed.
/// </summary>
/// <remarks>
/// F# would write a <b><i>single-case union</i></b>, <c>ConnectionString of string</c>, with the same
/// aim: <b><i>parse, don't validate</i></b>. A check that a string is good leaves a plain string,
/// which the next reader must trust; parsing it into its own type leaves a value that cannot be bad.
/// </remarks>
public sealed record ConnectionString
{
    private ConnectionString(string text) => Text = text;

    /// <summary>The text Npgsql reads.</summary>
    public string Text { get; }

    /// <summary>
    /// Turns <c>postgresql://user:password@host:port/database</c> into Npgsql's connection string;
    /// with no port, PostgreSQL's usual 5432. A password may hold any character, so a character such
    /// as '@' arrives percent-encoded, here %40. A missing or malformed URL gives the reason.
    /// </summary>
    public static Result<ConnectionString, string> FromUrl(Maybe<string> given)
    {
        if (!given.TryGetValue(out var url) || url.Length == 0)
            return Result.Failure<ConnectionString, string>("missing or empty");
        // PostgreSQL's own tools accept both 'postgresql' and 'postgres', so a URL copied from them
        // reads.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("postgresql" or "postgres")
            || uri.UserInfo.Split(':', 2) is not [{ Length: > 0 } user, var password] || uri.AbsolutePath.Length < 2)
            return Result.Failure<ConnectionString, string>("expected postgresql://user:password@host:port/database");
        return new ConnectionString(new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port < 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath[1..]),
            Username = Uri.UnescapeDataString(user),
            Password = Uri.UnescapeDataString(password),
        }.ConnectionString);
    }
}

/// <summary>The database's connection, parsed from the URL in <see cref="Variable"/> and checked at start-up.</summary>
public sealed class DatabaseConnection : IValidatableObject
{
    /// <summary>The <b><i>environment variable</i></b> that held the URL, named in any problem.</summary>
    public string Variable { get; set; } = DatabaseConnections.ReaderDatabase;

    /// <summary>
    /// The URL, parsed once when the settings are read: the connection string, or why the URL was
    /// refused. ASP.NET's settings need a setter, so it starts refused, as for a missing variable.
    /// </summary>
    public Result<ConnectionString, string> Parsed { get; set; } = ConnectionString.FromUrl(Maybe<string>.None);

    /// <summary>
    /// Refuses a missing or malformed URL and names its variable, so the service will not start
    /// without a well-formed address for its database (US-00 c4).
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext context) =>
        Parsed.IsFailure ? [new ValidationResult($"{Variable}: {Parsed.Error}")] : [];
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
            .Configure(connection => (connection.Variable, connection.Parsed) = (variable, ConnectionString.FromUrl(configuration[variable].AsMaybe())))
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
