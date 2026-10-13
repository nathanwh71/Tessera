using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tessera.Kernel;

namespace Tessera.Settings;

/// <summary>
/// Turns the environment and the text of tessera.json into settings, or into a list of every
/// problem it found (US-00 c4, ADR 0003).
/// </summary>
/// <remarks>
/// These functions are pure: they read only their parameters and touch no file, so a test can hand
/// them any environment it likes. <see cref="SettingsLoader"/> does the reading from disk.
/// They report every problem at once, not the first alone, so one failed start tells the maintainer
/// everything to fix.
/// </remarks>
public static class SettingsReader
{
    public static Result<ServiceSettings, ImmutableArray<SettingsError>> ReadService(
        IReadOnlyDictionary<string, string> environment, string configName, Option<string> configText)
    {
        var document = Document(environment, configName, configText);
        var area = document.Bind(ReadArea);
        var map = document.Bind(json => Flag(json, "Map.Preview").Map(preview => new MapSettings(preview)));
        var tiles = document.Bind(ReadTiles);
        var port = Required(environment, EnvironmentNames.HttpPort).Bind(text => Port(EnvironmentNames.HttpPort, text));
        var database = Database(environment, EnvironmentNames.ReaderDatabase);

        var errors = Errors(document.Map(_ => 0), area, map, tiles, port, database);
        return errors.IsEmpty
            ? Result.Ok<ServiceSettings, ImmutableArray<SettingsError>>(
                new ServiceSettings(Value(area), Value(map), Value(tiles), Value(port), Value(database)))
            : Result.Refuse<ServiceSettings, ImmutableArray<SettingsError>>(errors);
    }

    public static Result<ImportSettings, ImmutableArray<SettingsError>> ReadImport(
        IReadOnlyDictionary<string, string> environment, string configName, Option<string> configText)
    {
        var box = Document(environment, configName, configText).Bind(json => ReadArea(json).Map(area => area.Box));
        var database = Database(environment, EnvironmentNames.PublisherDatabase);

        var errors = Errors(box, database);
        return errors.IsEmpty
            ? Result.Ok<ImportSettings, ImmutableArray<SettingsError>>(new ImportSettings(Value(box), Value(database)))
            : Result.Refuse<ImportSettings, ImmutableArray<SettingsError>>(errors);
    }

    /// <summary>
    /// Reads <c>postgresql://user:password@host:port/database</c>. A password may hold any
    /// character, so URL rules apply: a character such as '@' arrives "percent-encoded" as %40.
    /// </summary>
    public static Result<DatabaseAddress, SettingsError> ParseDatabaseUrl(string name, string text)
    {
        var refusal = new SettingsError(name, "expected postgresql://user:password@host:port/database");
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme is not ("postgresql" or "postgres"))
            return Result.Refuse<DatabaseAddress, SettingsError>(refusal);

        var userInfo = uri.UserInfo.Split(':', 2);
        var database = uri.AbsolutePath.TrimStart('/');
        return userInfo.Length == 2 && userInfo[0].Length > 0 && uri.Host.Length > 0 && database.Length > 0
            ? Result.Ok<DatabaseAddress, SettingsError>(new DatabaseAddress(
                uri.Host,
                uri.IsDefaultPort || uri.Port < 0 ? 5432 : uri.Port,
                Uri.UnescapeDataString(database),
                Uri.UnescapeDataString(userInfo[0]),
                Uri.UnescapeDataString(userInfo[1])))
            : Result.Refuse<DatabaseAddress, SettingsError>(refusal);
    }

    private static Result<JsonObject, SettingsError> Document(
        IReadOnlyDictionary<string, string> environment, string configName, Option<string> configText) =>
        configText
            .OrRefuse(new SettingsError(configName, "file not found"))
            .Bind(text => Parse(configName, text))
            .Map(json => Override(json, environment));

    private static Result<JsonObject, SettingsError> Parse(string configName, string text)
    {
        try
        {
            return JsonNode.Parse(text) is JsonObject json
                ? Result.Ok<JsonObject, SettingsError>(json)
                : Result.Refuse<JsonObject, SettingsError>(new SettingsError(configName, "expected a JSON object"));
        }
        catch (JsonException error)
        {
            // System.Text.Json reports bad JSON by throwing; this is the one place that catches it.
            return Result.Refuse<JsonObject, SettingsError>(new SettingsError(configName, error.Message));
        }
    }

    /// <summary>
    /// Copies the file's settings and lays each <c>TESSERA_Section__Key</c> variable over the key it
    /// names. The copy keeps the parsed file unchanged.
    /// </summary>
    private static JsonObject Override(JsonObject file, IReadOnlyDictionary<string, string> environment) =>
        environment
            .Where(variable => variable.Key.StartsWith(EnvironmentNames.OverridePrefix, StringComparison.Ordinal)
                               && variable.Key.Contains("__", StringComparison.Ordinal))
            .OrderBy(variable => variable.Key, StringComparer.Ordinal)
            .Aggregate((JsonObject)file.DeepClone(), (json, variable) =>
                Set(json, variable.Key[EnvironmentNames.OverridePrefix.Length..].Split("__"), variable.Value));

    // JsonObject is mutable, so Override hands this a fresh copy and nothing outside sees the change.
    private static JsonObject Set(JsonObject json, string[] path, string value)
    {
        var parent = path[..^1].Aggregate(json, (node, key) =>
            node[key] as JsonObject ?? (JsonObject)(node[key] = new JsonObject()));
        parent[path[^1]] = value;
        return json;
    }

    private static Result<Area, SettingsError> ReadArea(JsonObject json) =>
        from name in Text(json, "Area.Name")
        from west in Number(json, "Area.Box.West", -180, 180)
        from south in Number(json, "Area.Box.South", -90, 90)
        from east in Number(json, "Area.Box.East", -180, 180)
        from north in Number(json, "Area.Box.North", -90, 90)
        from box in west < east && south < north
            ? Result.Ok<AreaBox, SettingsError>(new AreaBox(west, south, east, north))
            : Result.Refuse<AreaBox, SettingsError>(new SettingsError("Area.Box", "West must lie below East, and South below North"))
        from longitude in Number(json, "Area.Home.Longitude", -180, 180)
        from latitude in Number(json, "Area.Home.Latitude", -90, 90)
        from zoom in Number(json, "Area.Home.Zoom", 0, 22)
        select new Area(name, box, new HomeView(longitude, latitude, zoom));

    private static Result<TileSettings, SettingsError> ReadTiles(JsonObject json) =>
        from maxFeatures in Number(json, "Tiles.MaxFeatures", 1, 1_000_000)
        from buffer in Number(json, "Tiles.Buffer", 0, 4096)
        from whole in maxFeatures % 1 == 0 && buffer % 1 == 0
            ? Result.Ok<bool, SettingsError>(true)
            : Result.Refuse<bool, SettingsError>(new SettingsError("Tiles", "MaxFeatures and Buffer must be whole numbers"))
        select new TileSettings((int)maxFeatures, (int)buffer);

    private static Result<JsonNode, SettingsError> Node(JsonObject json, string path) =>
        path.Split('.')
            .Aggregate(Option.Some<JsonNode>(json), (node, key) => node.Bind(found =>
                found is JsonObject parent ? parent[key].ToOption() : Option.None<JsonNode>()))
            .OrRefuse(new SettingsError(path, "missing"));

    private static Result<string, SettingsError> Text(JsonObject json, string path) =>
        Node(json, path).Bind(node =>
            node is JsonValue value && value.TryGetValue<string>(out var text) && text.Length > 0
                ? Result.Ok<string, SettingsError>(text)
                : Result.Refuse<string, SettingsError>(new SettingsError(path, "expected text")));

    // A number may arrive as a JSON number from the file or as text from an override variable.
    private static Result<double, SettingsError> Number(JsonObject json, string path, double lowest, double highest) =>
        Node(json, path).Bind(node =>
            node is JsonValue value
            && (value.TryGetValue<double>(out var number)
                || (value.TryGetValue<string>(out var text)
                    && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)))
            && number >= lowest && number <= highest
                ? Result.Ok<double, SettingsError>(number)
                : Result.Refuse<double, SettingsError>(new SettingsError(path, $"expected a number from {lowest} to {highest}")));

    private static Result<bool, SettingsError> Flag(JsonObject json, string path) =>
        Node(json, path).Bind(node =>
            node is JsonValue value
            && (value.TryGetValue<bool>(out var flag)
                || (value.TryGetValue<string>(out var text) && bool.TryParse(text, out flag)))
                ? Result.Ok<bool, SettingsError>(flag)
                : Result.Refuse<bool, SettingsError>(new SettingsError(path, "expected true or false")));

    private static Result<string, SettingsError> Required(IReadOnlyDictionary<string, string> environment, string name) =>
        environment.GetValueOrDefault(name).ToOption()
            .Bind(value => value.Length > 0 ? Option.Some(value) : Option.None<string>())
            .OrRefuse(new SettingsError(name, "missing or empty"));

    private static Result<int, SettingsError> Port(string name, string text) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var port) && port is >= 1 and <= 65535
            ? Result.Ok<int, SettingsError>(port)
            : Result.Refuse<int, SettingsError>(new SettingsError(name, "expected a port from 1 to 65535"));

    private static Result<DatabaseAddress, SettingsError> Database(IReadOnlyDictionary<string, string> environment, string name) =>
        Required(environment, name).Bind(text => ParseDatabaseUrl(name, text));

    private static ImmutableArray<SettingsError> Errors<T1, T2>(Result<T1, SettingsError> a, Result<T2, SettingsError> b) =>
        [.. Error(a), .. Error(b)];

    private static ImmutableArray<SettingsError> Errors<T1, T2, T3, T4, T5, T6>(
        Result<T1, SettingsError> a, Result<T2, SettingsError> b, Result<T3, SettingsError> c,
        Result<T4, SettingsError> d, Result<T5, SettingsError> e, Result<T6, SettingsError> f) =>
        // The file's own problem, if any, appears once: area, map and tiles repeat it, so keep distinct names.
        [.. new[] { Error(a), Error(b), Error(c), Error(d), Error(e), Error(f) }.SelectMany(error => error).Distinct()];

    private static IEnumerable<SettingsError> Error<T>(Result<T, SettingsError> result) =>
        result.Match(_ => [], error => new[] { error });

    // Called only once Errors has found none, so every result here is Ok.
    private static T Value<T>(Result<T, SettingsError> result) =>
        result.Match(value => value, _ => throw new UnreachableException());
}
