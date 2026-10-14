using System.Collections.Immutable;
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
/// The file is the one both builds read, key for key (ADR 0006, rule 4): the Java build's keys,
/// forms and defaults, which are pg_tileserv's wherever pg_tileserv has the setting. So one
/// tessera.json starts either build, and a file one refuses, the other refuses too.
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
        var document = Document(environment, configName, configText).Bind(Known).MapError(One);
        return Result.Collect(
                document.Bind(json => ReadArea(json).MapError(One)),
                document.Bind(json => FlagOr(json, "ShowPreview", true).Map(preview => new MapSettings(preview)).MapError(One)),
                document.Bind(json => ReadTiles(json).MapError(One)),
                document.Bind(json => ReadHttp(json).MapError(One)),
                // DATABASE_URL lives in the environment, so its problem is named even when the file has one.
                ReadDb(document.Match(json => json, _ => new JsonObject()), environment),
                document.Bind(json => ReadCatalogue(json).MapError(One)))
            .Map(found => new ServiceSettings(found.Item1, found.Item2, found.Item3, found.Item4, found.Item5, found.Item6))
            // The file's own problem, if any, reaches every group alike, so it is named once.
            .MapError(errors => errors.SelectMany(group => group).Distinct().ToImmutableArray());
    }

    public static Result<ImportSettings, ImmutableArray<SettingsError>> ReadImport(
        IReadOnlyDictionary<string, string> environment, string configName, Option<string> configText) =>
        Result.Collect(
                Document(environment, configName, configText).Bind(Known).Bind(json => ReadArea(json).Map(area => area.Box)),
                Database(environment, EnvironmentNames.PublisherDatabase))
            .Map(found => new ImportSettings(found.Item1, found.Item2));

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

    // As in pg_tileserv, a missing file is no fault: every key then takes its default.
    private static Result<JsonObject, SettingsError> Document(
        IReadOnlyDictionary<string, string> environment, string configName, Option<string> configText) =>
        configText
            .Match(text => Parse(configName, text), () => Result.Ok<JsonObject, SettingsError>(new JsonObject()))
            .Map(json => Override(json, environment));

    /// <summary>
    /// Refuses a key the service does not read. A key it does not know is most often a misspelling
    /// of one it does, so the maintainer learns at start-up rather than wondering why a value had no effect.
    /// </summary>
    private static Result<JsonObject, SettingsError> Known(JsonObject json)
    {
        var unknown = json.Select(entry => entry.Key).Where(key => !TopKeys.Contains(key)).Order(StringComparer.Ordinal)
            .Concat((json["Area"] as JsonObject)?.Select(entry => entry.Key).Where(key => !AreaKeys.Contains(key)).Select(key => $"Area.{key}") ?? [])
            .ToList();
        return unknown.Count == 0
            ? Result.Ok<JsonObject, SettingsError>(json)
            : Result.Refuse<JsonObject, SettingsError>(new SettingsError(unknown[0], "not a setting Tessera knows"));
    }

    private static readonly ImmutableArray<string> TopKeys =
    [
        "HttpHost", "HttpPort", "HttpMaxConnections", "HttpAcceptCount",
        "DbPoolMaxConns", "DbPoolMaxConnLifeTime", "DbTimeout", "DbPoolConnectionTimeout",
        "DefaultResolution", "DefaultBuffer", "MaxFeaturesPerTile", "DefaultMinZoom", "DefaultMaxZoom", "CacheTTL", "TileCacheMegabytes",
        "ShowPreview", "CatalogueRefresh", "Area",
    ];

    private static readonly ImmutableArray<string> AreaKeys =
        ["Name", "MinLongitude", "MinLatitude", "MaxLongitude", "MaxLatitude", "HomeLongitude", "HomeLatitude", "HomeLevel"];

    private static Result<JsonObject, SettingsError> Parse(string configName, string text)
    {
        try
        {
            // Touching every key makes JsonObject read them all now, so a key written twice fails here.
            return JsonNode.Parse(text) is JsonObject json && Touch(json)
                ? Result.Ok<JsonObject, SettingsError>(json)
                : Result.Refuse<JsonObject, SettingsError>(new SettingsError(configName, "expected a JSON object"));
        }
        catch (Exception error) when (error is JsonException or ArgumentException)
        {
            // System.Text.Json reports text that is not JSON, or a key written twice, by throwing,
            // so the parse alone sits in a try.
            return Result.Refuse<JsonObject, SettingsError>(new SettingsError(configName, error.Message));
        }
    }

    private static bool Touch(JsonNode node) =>
        node is not JsonObject json || json.All(entry => entry.Value is null || Touch(entry.Value));

    /// <summary>
    /// Every key the service reads, spelt as pg_tileserv spells it where it has the same setting
    /// (ADR 0003, ADR 0006 rule 4). An override must name one of these.
    /// </summary>
    public static readonly ImmutableArray<string> Keys =
        [.. TopKeys.Where(key => key != "Area"), .. AreaKeys.Select(key => $"Area.{key}")];

    /// <summary>
    /// Copies the file's settings and lays each override over the key it names: <c>TESSERA_</c>, then
    /// the key in any case, as pg_tileserv's <c>TS_HTTPPORT</c> overrides <c>HttpPort</c>. "__" steps
    /// down a level, so <c>TESSERA_AREA__HOMELEVEL</c> overrides <c>Area.HomeLevel</c>. A variable that
    /// names no key, such as <c>TESSERA_DB_PASSWORD</c>, is left alone. The copy keeps the parsed
    /// file unchanged.
    /// </summary>
    private static JsonObject Override(JsonObject file, IReadOnlyDictionary<string, string> environment) =>
        environment
            .Where(variable => variable.Key.StartsWith(EnvironmentNames.OverridePrefix, StringComparison.OrdinalIgnoreCase))
            .Select(variable => (Key: KeyOf(variable.Key[EnvironmentNames.OverridePrefix.Length..]), variable.Value))
            .Where(variable => variable.Key is not null)
            .OrderBy(variable => variable.Key, StringComparer.Ordinal)
            .Aggregate((JsonObject)file.DeepClone(), (json, variable) => Set(json, variable.Key!.Split('.'), variable.Value));

    private static string? KeyOf(string name) =>
        Keys.FirstOrDefault(key => string.Equals(key.Replace(".", "__", StringComparison.Ordinal), name, StringComparison.OrdinalIgnoreCase));

    // JsonObject is mutable, so Override hands this a fresh copy and nothing outside sees the change.
    private static JsonObject Set(JsonObject json, string[] path, string value)
    {
        var parent = path[..^1].Aggregate(json, (node, key) =>
            node[key] as JsonObject ?? (JsonObject)(node[key] = new JsonObject()));
        parent[path[^1]] = value;
        return json;
    }

    // Macau, the office's area, when the file names none: the Java build's default.
    private static readonly Area Macau = new("Macau", new AreaBox(113.52, 22.10, 113.61, 22.22), new HomeView(113.55, 22.19, 15));

    // Web Mercator, the map's own projection, cannot show the poles: it stops near 85 degrees.
    private const double FarthestLatitude = 85.06;

    private static Result<Area, SettingsError> ReadArea(JsonObject json) =>
        from name in TextOr(json, "Area.Name", Macau.Name)
        from west in NumberOr(json, "Area.MinLongitude", Macau.Box.West, -180, 180)
        from south in NumberOr(json, "Area.MinLatitude", Macau.Box.South, -FarthestLatitude, FarthestLatitude)
        from east in NumberOr(json, "Area.MaxLongitude", Macau.Box.East, -180, 180)
        from north in NumberOr(json, "Area.MaxLatitude", Macau.Box.North, -FarthestLatitude, FarthestLatitude)
        from box in west < east && south < north
            ? Result.Ok<AreaBox, SettingsError>(new AreaBox(west, south, east, north))
            : Result.Refuse<AreaBox, SettingsError>(new SettingsError("Area", "a box runs west to east and south to north"))
        from longitude in NumberOr(json, "Area.HomeLongitude", Macau.Home.Longitude, -180, 180)
        from latitude in NumberOr(json, "Area.HomeLatitude", Macau.Home.Latitude, -FarthestLatitude, FarthestLatitude)
        from level in NumberOr(json, "Area.HomeLevel", Macau.Home.Zoom, 0, 22)
        select new Area(name, box, new HomeView(longitude, latitude, level));

    private static Result<TileSettings, SettingsError> ReadTiles(JsonObject json) =>
        from resolution in WholeOr(json, "DefaultResolution", 4096, 1, int.MaxValue)
        from buffer in WholeOr(json, "DefaultBuffer", 256, 0, int.MaxValue)
        from maxFeatures in WholeOr(json, "MaxFeaturesPerTile", 10_000, 1, int.MaxValue)
        from minZoom in WholeOr(json, "DefaultMinZoom", 0, 0, 30)
        from maxZoom in WholeOr(json, "DefaultMaxZoom", 22, 0, 30)
        from zooms in maxZoom >= minZoom
            ? Result.Ok<int, SettingsError>(maxZoom)
            : Result.Refuse<int, SettingsError>(new SettingsError("DefaultMaxZoom", "not below DefaultMinZoom"))
        // pg_tileserv sends no cache rule by default; the shipped file asks for a minute.
        from cacheSeconds in WholeOr(json, "CacheTTL", 0, 0, int.MaxValue)
        from cacheMegabytes in WholeOr(json, "TileCacheMegabytes", 64, 1, 65_536)
        select new TileSettings(maxFeatures, buffer, cacheSeconds, cacheMegabytes, resolution, minZoom, maxZoom);

    // pg_tileserv listens on every network card, on port 7800; the office overrides the port with
    // TESSERA_HTTPPORT, so no machine's port sits in the shipped file (US-00 c4).
    private static Result<HttpSettings, SettingsError> ReadHttp(JsonObject json) =>
        from host in TextOr(json, "HttpHost", "0.0.0.0")
        from address in System.Net.IPAddress.TryParse(host, out var parsed)
            ? Result.Ok<System.Net.IPAddress, SettingsError>(parsed)
            : Result.Refuse<System.Net.IPAddress, SettingsError>(new SettingsError("HttpHost", "expected an address such as 0.0.0.0"))
        from port in WholeOr(json, "HttpPort", 7800, 1, 65_535)
        from maxConnections in WholeOr(json, "HttpMaxConnections", 8192, 1, 1_000_000)
        from acceptCount in WholeOr(json, "HttpAcceptCount", 100, 1, 1_000_000)
        select new HttpSettings(address, port, maxConnections, acceptCount);

    private static Result<DbSettings, ImmutableArray<SettingsError>> ReadDb(JsonObject json, IReadOnlyDictionary<string, string> environment) =>
        Result.Collect(
                Database(environment, EnvironmentNames.ReaderDatabase),
                WholeOr(json, "DbPoolMaxConns", 4, 1, 1000),
                DurationOr(json, "DbPoolMaxConnLifeTime", TimeSpan.FromHours(1)),
                WholeOr(json, "DbTimeout", 10, 0, int.MaxValue),
                // Milliseconds, as the Java build's tuning guide measured it.
                WholeOr(json, "DbPoolConnectionTimeout", 3000, 250, 600_000))
            .Map(found => new DbSettings(found.Item1, found.Item2, found.Item3, TimeSpan.FromSeconds(found.Item4), TimeSpan.FromMilliseconds(found.Item5)));

    private static ImmutableArray<SettingsError> One(SettingsError error) => [error];

    private static Result<CatalogueSettings, SettingsError> ReadCatalogue(JsonObject json) =>
        WholeOr(json, "CatalogueRefresh", 30, 0, int.MaxValue).Map(seconds => new CatalogueSettings(TimeSpan.FromSeconds(seconds)));

    private static Option<JsonNode> Find(JsonObject json, string path) =>
        path.Split('.')
            .Aggregate(Option.Some<JsonNode>(json), (node, key) => node.Bind(found =>
                found is JsonObject parent ? parent[key].ToOption() : Option.None<JsonNode>()));

    private static Result<JsonNode, SettingsError> Node(JsonObject json, string path) =>
        Find(json, path).OrRefuse(new SettingsError(path, "missing"));

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

    private static Result<double, SettingsError> NumberOr(JsonObject json, string path, double fallback, double lowest, double highest) =>
        Find(json, path).Match(_ => Number(json, path, lowest, highest), () => Result.Ok<double, SettingsError>(fallback));

    private static Result<string, SettingsError> TextOr(JsonObject json, string path, string fallback) =>
        Find(json, path).Match(_ => Text(json, path), () => Result.Ok<string, SettingsError>(fallback));

    /// <summary>A whole number within a range.</summary>
    private static Result<int, SettingsError> Whole(JsonObject json, string path, int lowest, int highest) =>
        Number(json, path, lowest, highest).Bind(number => number % 1 == 0
            ? Result.Ok<int, SettingsError>((int)number)
            : Result.Refuse<int, SettingsError>(new SettingsError(path, $"expected a whole number from {lowest} to {highest}")));

    /// <summary>A whole number the file may leave out, as pg_tileserv's keys may be: the fallback then stands.</summary>
    private static Result<int, SettingsError> WholeOr(JsonObject json, string path, int fallback, int lowest, int highest) =>
        Find(json, path).Match(_ => Whole(json, path, lowest, highest), () => Result.Ok<int, SettingsError>(fallback));

    /// <summary>
    /// A length of time: a whole number of seconds, as the Java build first wrote it, or pg_tileserv's
    /// form, such as <c>1h</c>, <c>30m</c> or <c>1h30m</c> (ADR 0006 rule 4). Both builds read both.
    /// </summary>
    private static Result<TimeSpan, SettingsError> DurationOr(JsonObject json, string path, TimeSpan fallback) =>
        Find(json, path).Match(
            node => (node is JsonValue value && value.TryGetValue<string>(out var text) ? ParseDuration(text) : null) is { } time
                ? Result.Ok<TimeSpan, SettingsError>(time)
                : Whole(json, path, 0, int.MaxValue).Map(seconds => TimeSpan.FromSeconds(seconds))
                    .MapError(_ => new SettingsError(path, "expected a whole number of seconds, or a time such as 1h, 30m or 90s")),
            () => Result.Ok<TimeSpan, SettingsError>(fallback));

    public static TimeSpan? ParseDuration(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"^(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s)?$");
        int Part(int group) => match.Groups[group].Success ? int.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture) : 0;
        return match.Success && text.Length > 0 ? new TimeSpan(Part(1), Part(2), Part(3)) : null;
    }

    private static Result<bool, SettingsError> FlagOr(JsonObject json, string path, bool fallback) =>
        Find(json, path).Match(_ => Flag(json, path), () => Result.Ok<bool, SettingsError>(fallback));

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

    private static Result<DatabaseAddress, SettingsError> Database(IReadOnlyDictionary<string, string> environment, string name) =>
        Required(environment, name).Bind(text => ParseDatabaseUrl(name, text));
}
