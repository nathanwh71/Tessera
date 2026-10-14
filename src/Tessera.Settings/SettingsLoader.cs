using System.Collections;
using System.Collections.Immutable;
using Tessera.Kernel;

namespace Tessera.Settings;

/// <summary>
/// The edge of the Settings module: it reads the real environment and the real tessera.json, then
/// hands both to <see cref="SettingsReader"/>, which is pure, touching nothing outside its own
/// parameters. The program's start-up, Program.cs in the Service module, calls it once before it
/// serves tiles or runs an import (US-00, ADR 0003).
/// </summary>
/// <remarks>
/// Keeping the disk and the operating system here, at the edge, lets tests check every settings rule
/// by handing <see cref="SettingsReader"/> made-up values (ADR 0004).
/// </remarks>
public static class SettingsLoader
{
    public const string ConfigFileName = "tessera.json";

    /// <summary>The settings the service needs to start, or every problem found with them.</summary>
    public static Result<ServiceSettings, ImmutableArray<SettingsError>> LoadService()
    {
        var environment = Environment();
        var path = ConfigPath(environment);
        return SettingsReader.ReadService(environment, path, ReadFile(path));
    }

    /// <summary>The settings the import needs, or every problem found with them.</summary>
    public static Result<ImportSettings, ImmutableArray<SettingsError>> LoadImport()
    {
        var environment = Environment();
        var path = ConfigPath(environment);
        return SettingsReader.ReadImport(environment, path, ReadFile(path));
    }

    /// <summary>
    /// The file TESSERA_CONFIG names, or tessera.json beside the program. The service reads no
    /// appsettings.json, the file most programs on .NET, the platform C# runs on, read by default, so
    /// one file holds every setting (ADR 0003).
    /// </summary>
    public static string ConfigPath(IReadOnlyDictionary<string, string> environment) =>
        environment.GetValueOrDefault(EnvironmentNames.Config) is { Length: > 0 } path
            ? path
            : Path.Combine(AppContext.BaseDirectory, ConfigFileName);

    // A dictionary, a table that finds a value by its name, of every environment variable. A variable
    // name is matched letter for letter, so 'Path' and 'PATH' stay apart, as on Linux.
    private static ImmutableDictionary<string, string> Environment() =>
        System.Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .ToImmutableDictionary(entry => (string)entry.Key, entry => entry.Value as string ?? "", StringComparer.Ordinal);

    // A missing file is no fault: SettingsReader then gives every key its default.
    private static Option<string> ReadFile(string path) =>
        File.Exists(path) ? Option.Some(File.ReadAllText(path)) : Option.None<string>();
}
