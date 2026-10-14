using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tessera;
using Tessera.Seeding;
using Tessera.Settings;

// The program's starting point. One program does both jobs (ADR 0001):
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
// may start in either order; until the list of layers loads, tiles answer 503 (N-23 c4).
static async Task<int> Serve()
{
    Microsoft.AspNetCore.Builder.WebApplication app;
    try
    {
        app = TesseraService.Build();
    }
    catch (Exception error) when (error is OptionsValidationException or AggregateException or InvalidOperationException)
    {
        return Fail(TesseraSettings.Problems(error));
    }

    await app.RunAsync();
    return 0;
}

// Runs the seed scripts DbUp has not yet run, and names each one. It needs no web server, only the
// settings, so it builds a plain host to read and check them.
static int Seed()
{
    var builder = Host.CreateApplicationBuilder();
    TesseraSettings.AddFile(builder.Configuration);
    TesseraSettings.Register(builder.Services, builder.Configuration, TesseraSettings.PublisherDatabase);
    using var host = builder.Build();
    try
    {
        TesseraSettings.Check(host.Services);
    }
    catch (Exception error) when (error is OptionsValidationException or AggregateException or InvalidOperationException)
    {
        return Fail(TesseraSettings.Problems(error));
    }

    var ran = SeedScripts.Run(host.Services.GetRequiredService<IOptions<DatabaseConnection>>().Value.Text, SeedScripts.Folder);
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

static int Usage()
{
    Console.Error.WriteLine("Usage: Tessera                   serve tiles and the map");
    Console.Error.WriteLine("       Tessera seed               run each new SQL script of places once");
    return 2;
}
