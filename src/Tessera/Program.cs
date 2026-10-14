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
    var loaded = SettingsLoader.Load(SettingsLoader.ReaderDatabase);
    if (loaded.IsFailure)
        return Fail(loaded.Error);

    await using var database = Database.Open(loaded.Value.Database, loaded.Value.Settings.Database);
    await TesseraService.Build(loaded.Value.Settings, database, Path.Combine(AppContext.BaseDirectory, "wwwroot")).RunAsync();
    return 0;
}

// Loads one file of places and prints how many places each layer received.
static async Task<int> Import(string path)
{
    var loaded = SettingsLoader.Load(SettingsLoader.PublisherDatabase);
    if (loaded.IsFailure)
        return Fail(loaded.Error);
    if (!File.Exists(path))
        return Fail([$"No file at {path}."]);

    await using var database = Database.Open(loaded.Value.Database);
    var imported = await Importer.Run(database, await File.ReadAllTextAsync(path), loaded.Value.Settings.Area);
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
