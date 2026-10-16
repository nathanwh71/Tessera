using Common.Infrastructure;
using Microsoft.Extensions.Options;
using Modules.Tessera;
using Modules.Tessera.Infrastructure.Seeding;
using Modules.Tessera.Infrastructure.Sql;

// The program's starting point, the template's Host. One program does both jobs (ADR 0001, ADR 0010):
//   Tessera                  serves tiles and the map, behind Nginx (ADR 0008)
//   Tessera seed             runs each new SQL script of places once, then stops (US-24, ADR 0009)
// It ends with an exit code: 0 for success, 1 for a failure, 2 for a command it did not understand.

return args switch
{
    [] => await Serve(),
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
        app = TesseraModule.Build();
    }
    // The three ways a settings check fails: a value outside its limits, several problems at once,
    // and a key the binder cannot place. Any other exception is a fault, and escapes.
    catch (Exception error)
        when (error is OptionsValidationException or AggregateException or InvalidOperationException)
    {
        return Fail(Settings.Problems(error));
    }

    await app.RunAsync();
    return 0;
}

// Runs the seed scripts DbUp has not yet run, and names each one. It needs no web server, only the
// settings, so it builds a plain host to read and check them.
static int Seed()
{
    var builder = Host.CreateApplicationBuilder();
    Settings.AddFile(builder.Configuration);
    TesseraModule.AddSettings(builder.Services);
    DatabaseConnections.Register(builder.Services, builder.Configuration, DatabaseConnections.PublisherDatabase);
    using var host = builder.Build();
    try
    {
        Settings.Check(host.Services);
    }
    catch (Exception error)
        when (error is OptionsValidationException or AggregateException or InvalidOperationException)
    {
        return Fail(Settings.Problems(error));
    }

    var connection = host.Services.GetRequiredService<IOptions<DatabaseConnection>>().Value.Text;
    var ran = SeedScripts.Run(connection, SeedScripts.Folder);
    if (ran.IsFailure)
        return Fail([ran.Error]);

    Console.WriteLine(ran.Value.Count == 0
        ? "The seed is up to date."
        : string.Join(Environment.NewLine, [.. ran.Value.Select(name => $"Ran {name}"),
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
