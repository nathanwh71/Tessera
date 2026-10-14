using System.Collections.Immutable;
using Tessera.Import;
using Tessera.Service;
using Tessera.Settings;
using Tessera.Sql;

// The program's starting point: the first lines that run. It sits in the Service project, the one
// part that joins every module, and one program does both jobs (ADR 0001). The operator, or the
// start-up script, runs it in one of two ways:
//   Tessera                         serves tiles and the map
//   Tessera import <file>           loads the places from a GeoJSON file, a text file that
//                                   describes places on a map, then stops (US-24)
// The program ends with an exit code, a number it hands back to whatever started it: 0 means
// success, 1 a failure, and 2 a command it did not understand.

return args is ["import", var path]
    ? await Import(path)
    : args.Length == 0
        ? await Serve()
        : Usage();

// Serves until the service is told to stop. The service starts whether or not the database answers
// yet, so the two may start in either order; until the list of layers loads, tile requests get 503,
// the status code, or number in the answer, that means 'try again later' (N-23 c4). The map's built
// files sit in wwwroot, a folder beside the program.
static async Task<int> Serve() =>
    await SettingsLoader.Load(SettingsLoader.ReaderDatabase).Match(
        async loaded =>
        {
            await using var database = Database.Open(loaded.Database, loaded.Settings.Database);
            var mapFolder = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            await TesseraService.Build(loaded.Settings, database, mapFolder).RunAsync();
            return 0;
        },
        errors => Task.FromResult(Fail(errors)));

// Loads one file of places into the database and prints how many places each layer received.
static async Task<int> Import(string path) =>
    await SettingsLoader.Load(SettingsLoader.PublisherDatabase).Match(
        async loaded =>
        {
            if (!File.Exists(path))
                return FailWith($"No file at {path}.");
            await using var database = Database.Open(loaded.Database);
            return (await Importer.Run(database, await File.ReadAllTextAsync(path), loaded.Settings.Area)).Match(
                summary =>
                {
                    Console.WriteLine(summary);
                    return 0;
                },
                error => FailWith(error.Message));
        },
        errors => Task.FromResult(Fail(errors)));

// The service refuses to start and names each setting it lacks or cannot read (US-00 c4).
static int Fail(ImmutableArray<string> errors)
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

// The command was neither of the two the program knows; exit code 2 tells the two cases apart.
static int Usage()
{
    Console.Error.WriteLine("Usage: Tessera                   serve tiles and the map");
    Console.Error.WriteLine("       Tessera import FILE        load the places from a GeoJSON file");
    return 2;
}
