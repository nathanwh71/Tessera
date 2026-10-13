using System.Collections;
using System.Collections.Immutable;
using Tessera.Kernel;

namespace Tessera.Settings;

/// <summary>
/// The edge of the Settings module: it reads the real environment and the real tessera.json, then
/// hands both to the pure <see cref="SettingsReader"/>.
/// </summary>
public static class SettingsLoader
{
    public const string ConfigFileName = "tessera.json";

    public static Result<ServiceSettings, ImmutableArray<SettingsError>> LoadService()
    {
        var environment = Environment();
        var path = ConfigPath(environment);
        return SettingsReader.ReadService(environment, path, ReadFile(path));
    }

    public static Result<ImportSettings, ImmutableArray<SettingsError>> LoadImport()
    {
        var environment = Environment();
        var path = ConfigPath(environment);
        return SettingsReader.ReadImport(environment, path, ReadFile(path));
    }

    /// <summary>
    /// The file TESSERA_CONFIG names, or tessera.json beside the program. The service reads no
    /// appsettings.json, so one file holds every setting (ADR 0003).
    /// </summary>
    public static string ConfigPath(IReadOnlyDictionary<string, string> environment) =>
        environment.GetValueOrDefault(EnvironmentNames.Config) is { Length: > 0 } path
            ? path
            : Path.Combine(AppContext.BaseDirectory, ConfigFileName);

    private static ImmutableDictionary<string, string> Environment() =>
        System.Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .ToImmutableDictionary(entry => (string)entry.Key, entry => entry.Value as string ?? "", StringComparer.Ordinal);

    private static Option<string> ReadFile(string path) =>
        File.Exists(path) ? Option.Some(File.ReadAllText(path)) : Option.None<string>();
}
