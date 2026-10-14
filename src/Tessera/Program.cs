using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tessera;
using Tessera.Import;
using Tessera.Settings;
using Tessera.Sql;

// The program's starting point. One program does both jobs (ADR 0001):
//   Tessera                  serves tiles and the map, behind Nginx (ADR 0008)
//   Tessera import <file>    loads the places from a GeoJSON file, then stops (US-24)
// It ends with an exit code: 0 for success, 1 for a failure, 2 for a command it did not understand.

return args switch
{
    [] => await Serve(),
    ["import", var path] => await Import(path),
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

// Loads one file of places and prints how many places each layer received. It needs no web server,
// only the settings, so it builds a plain host to read and check them.
static async Task<int> Import(string path)
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
    if (!File.Exists(path))
        return Fail([$"No file at {path}."]);

    await using var database = Database.Open(host.Services.GetRequiredService<IOptions<DatabaseConnection>>().Value.Text);
    var imported = await Importer.Run(database, await File.ReadAllTextAsync(path), host.Services.GetRequiredService<IOptions<AreaSettings>>().Value);
    if (imported.IsFailure)
        return Fail([imported.Error.Message]);

    Console.WriteLine(imported.Value);
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
    Console.Error.WriteLine("       Tessera import FILE        load the places from a GeoJSON file");
    return 2;
}
