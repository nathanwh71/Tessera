using Common.Infrastructure;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Modules.Tessera.Infrastructure.Seeding;
using Modules.Tessera.Infrastructure.Sql;

// The program's starting point, the template's Host. One program does both jobs (ADR 0001, ADR 0010):
//   Tessera                  serves tiles and the map, behind Nginx (ADR 0008)
//   Tessera seed             runs each new SQL script of places once, then stops (US-24, ADR 0009)
// It ends with an exit code: 0 for success, 1 for a failure, 2 for a command it did not understand.

return args switch
{
    [] => await Serve().ConfigureAwait(false),
    ["seed"] => Seed(),
    _ => Usage(),
};

// Serves until told to stop. The service starts whether or not the database answers yet, so the two
// may start in either order; until the list of layers loads, tiles answer 503 (N-23 c3).
static async Task<int> Serve()
{
    Microsoft.AspNetCore.Builder.WebApplication app;
    try
    {
        app = TesseraHost.Build();
    }
    // The two ways a settings check fails: one section's problems, or several sections' at once.
    // A misspelt key or a value that is not a number is one problem among the rest (US-00 c4). Any
    // other exception is a fault, and escapes.
    catch (Exception error) when (error is OptionsValidationException or AggregateException)
    {
        return Fail(Settings.Problems(error));
    }

    await app.RunAsync().ConfigureAwait(false);
    return 0;
}

// Runs the seed scripts DbUp has not yet run, and names each one. It needs one setting alone, the
// publisher's database URL, so it reads that and nothing else: a fault in another section, such as
// Tiles, stays the service's to report, and the seed still runs.
static int Seed()
{
    var configuration = new ConfigurationBuilder();
    Settings.AddFile(configuration);
    var variable = DatabaseConnections.PublisherDatabase;
    // Each step runs only when the one before it succeeded; the first failure skips the rest and
    // goes straight to Fail.
    // MapError names its three types because, given a string, the library would otherwise pick the
    // overload that returns its plain Result<T>, and Bind would then find no step to run.
    return ConnectionString.FromUrl(configuration.Build()[variable])
        .MapError<ConnectionString, string, string>(why => $"{variable}: {why}")
        .Bind(connection => SeedScripts.Run(connection.Text, SeedScripts.Folder))
        .Match(Report, why => Fail([why]));
}

// Names each script the seed ran, or says there was none to run.
static int Report(IReadOnlyList<string> ran)
{
    Console.WriteLine(ran.Count == 0
        ? "The seed is up to date."
        : string.Join(Environment.NewLine, [.. ran.Select(name => $"Ran {name}"),
            "A new table waits for its grant: npm run publish."]));
    return 0;
}

// The program names every problem it found, so one failed start shows everything to fix (US-00 c4).
static int Fail(IEnumerable<string> problems)
{
    foreach (var problem in problems)
        Console.Error.WriteLine(problem);
    return 1;
}

// Any other command shows the two the program knows, so a mistyped word teaches rather than fails
// in silence.
static int Usage()
{
    Console.Error.WriteLine("Usage: Tessera                   serve tiles and the map");
    Console.Error.WriteLine("       Tessera seed               run each new SQL script of places once");
    return 2;
}
