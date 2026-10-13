using System.Collections.Immutable;
using Tessera.Import;
using Tessera.Service;
using Tessera.Settings;
using Tessera.Sql;

// Two ways to run the one program (ADR 0001):
//   Tessera.Service                 serves tiles and the map
//   Tessera.Service import <file>   loads the places from a GeoJSON file, then stops (US-24)
// An exit code of 0 means success; any other number tells the start-up script something failed.

return args is ["import", var path]
    ? await Import(path)
    : args.Length == 0
        ? await Serve()
        : Usage();

// The service starts whether or not the database answers yet, so the two may start in either
// order; until the list of layers loads, tiles answer 503 (N-23 c4).
static async Task<int> Serve() =>
    await SettingsLoader.LoadService().Match(
        async settings =>
        {
            await using var database = Database.Open(settings.Db.Address, new PoolLimits(
                settings.Db.PoolMaxConns, settings.Db.PoolMaxConnLifeTime, settings.Db.Timeout, settings.Db.PoolConnectionTimeout));
            var mapFolder = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            await TesseraService.Build(settings, database, mapFolder).RunAsync();
            return 0;
        },
        errors => Task.FromResult(Fail(errors)));

static async Task<int> Import(string path) =>
    await SettingsLoader.LoadImport().Match(
        async settings =>
        {
            if (!File.Exists(path))
                return FailWith($"No file at {path}.");
            await using var database = Database.Open(settings.Database);
            return (await Importer.Run(database, await File.ReadAllTextAsync(path), settings.Box)).Match(
                summary =>
                {
                    Console.WriteLine(summary);
                    return 0;
                },
                error => FailWith(error.Message));
        },
        errors => Task.FromResult(Fail(errors)));

// US-00 c4: the service refuses to start and names each setting it lacks.
static int Fail(ImmutableArray<SettingsError> errors)
{
    foreach (var error in errors)
        Console.Error.WriteLine(error);
    return 1;
}

static int FailWith(string problem)
{
    Console.Error.WriteLine(problem);
    return 1;
}

static int Usage()
{
    Console.Error.WriteLine("Usage: Tessera.Service            serve tiles and the map");
    Console.Error.WriteLine("       Tessera.Service import FILE  load the places from a GeoJSON file");
    return 2;
}
