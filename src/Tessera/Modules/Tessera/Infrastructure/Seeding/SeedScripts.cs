using CSharpFunctionalExtensions;
using DbUp;
using Modules.Tessera.Domain;
using Modules.Tessera.Infrastructure.Sql;
using Npgsql;

namespace Modules.Tessera.Infrastructure.Seeding;

/// <summary>
/// Loads the places with DbUp: it runs each new <b><i>seed script</i></b>, a SQL file of places, once,
/// in the order of the scripts' names, and records each one in a <b><i>journal</i></b> table, so it
/// never runs twice (ADR 0009).
/// The command <c>Tessera seed</c> runs it, signed in as tessera_publisher (US-24).
/// </summary>
/// <remarks>
/// <c>npm run etl</c> writes the scripts from the places' own files, cleaning them on the way.
/// A script that replaces a table drops it first, and its <b><i>grants</i></b> go with it, so the new
/// table waits for the publisher's grant, as every table does (US-24 c5).
/// </remarks>
public static class SeedScripts
{
    /// <summary>
    /// Where DbUp records each script it has run: tessera.schemaversions. The journal keeps a
    /// <b><i>schema</i></b>, a named folder of tables, of its own, apart from the four schemas of
    /// places, so the record of the seed never mixes with the places it loaded.
    /// </summary>
    public const string JournalSchema = "tessera";

    /// <summary>The journal's table, by DbUp's usual name, so a reader who knows DbUp finds it.</summary>
    public const string JournalTable = "schemaversions";

    /// <summary>The scripts the release carries, beside the program.</summary>
    public static string Folder => Path.Combine(AppContext.BaseDirectory, "seed");

    /// <summary>
    /// Runs each script DbUp has not run before, each in a transaction of its own: a script takes
    /// effect whole, or not at all, and its record in the journal goes with it. Returns the names
    /// of the scripts it ran, or the name of the script that failed, and why.
    /// </summary>
    public static Result<IReadOnlyList<string>, string> Run(string connection, string folder)
    {
        // DbUp makes its journal table but not the schema that holds it.
        try
        {
            using var session = new NpgsqlConnection(connection);
            session.Open();
            using var command = new NpgsqlCommand(Queries.JournalSchema(SqlName.Quote(JournalSchema)).Text, session);
            command.ExecuteNonQuery();
        }
        catch (NpgsqlException error)
        {
            return Result.Failure<IReadOnlyList<string>, string>($"The database refused the seed: {error.Message}");
        }

        var upgrade = DeployChanges.To.PostgresqlDatabase(connection)
            .WithScriptsFromFileSystem(folder)
            .WithTransactionPerScript()
            // DbUp would otherwise read $name$ in a script as a value to fill in, and could change a place's text.
            .WithVariablesDisabled()
            .JournalToPostgresqlTable(JournalSchema, JournalTable)
            // The program prints its own short report, so DbUp's step-by-step log goes nowhere.
            .LogToNowhere()
            .Build();
        var result = upgrade.PerformUpgrade();
        return result.Successful
            ? Result.Success<IReadOnlyList<string>, string>([.. result.Scripts.Select(script => script.Name)])
            : Result.Failure<IReadOnlyList<string>, string>(
                $"{result.ErrorScript?.Name ?? "The seed"} failed: {result.Error.Message}");
    }
}
