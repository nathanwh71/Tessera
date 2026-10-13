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
        var document = Document(environment, configName, configText).MapError(One);
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
                Document(environment, configName, configText).Bind(json => ReadArea(json).Map(area => area.Box)),
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
    /// The keys the service reads, spelt as pg_tileserv spells them where it has the same setting
    /// (ADR 0003, as amended for N-21). An override must name one of these.
    /// </summary>
    public static readonly ImmutableArray<string> Keys =
    [
        "HttpPort", "ShowPreview",
        "DbPoolMaxConns", "DbPoolMaxConnLifeTime", "DbTimeout", "DbPoolConnectionTimeout",
        "MaxFeaturesPerTile", "DefaultBuffer", "CacheTTL", "TileCacheMegabytes", "CatalogueRefresh",
        "Area.Name", "Area.Box.West", "Area.Box.South", "Area.Box.East", "Area.Box.North",
        "Area.Home.Longitude", "Area.Home.Latitude", "Area.Home.Zoom",
    ];

    /// <summary>
    /// Copies the file's settings and lays each override over the key it names: <c>TESSERA_</c>, then
    /// the key in any case, as pg_tileserv's <c>TS_HTTPPORT</c> overrides <c>HttpPort</c>. "__" steps
    /// down a level, so <c>TESSERA_AREA__HOME__ZOOM</c> overrides <c>Area.Home.Zoom</c>. A variable that
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

    // A browser may keep a tile for a year at most, the longest HTTP's max-age means anything.
    private const int SecondsInAYear = 365 * 24 * 60 * 60;

    private static Result<TileSettings, SettingsError> ReadTiles(JsonObject json) =>
        from maxFeatures in WholeOr(json, "MaxFeaturesPerTile", 10_000, 1, 1_000_000)
        from buffer in WholeOr(json, "DefaultBuffer", 64, 0, 4096)
        from cacheSeconds in WholeOr(json, "CacheTTL", 60, 0, SecondsInAYear)
        from cacheMegabytes in WholeOr(json, "TileCacheMegabytes", 64, 1, 65_536)
        select new TileSettings(maxFeatures, buffer, cacheSeconds, cacheMegabytes);

    // The port has no default: the environment, or the file, must name it (US-00 c4). The shipped
    // file leaves it out, so the office names port 80 in TESSERA_HTTPPORT.
    private static Result<HttpSettings, SettingsError> ReadHttp(JsonObject json) =>
        Find(json, "HttpPort").Match(
            _ => Whole(json, "HttpPort", 1, 65_535).Map(port => new HttpSettings(port)),
            () => Result.Refuse<HttpSettings, SettingsError>(new SettingsError("HttpPort", $"missing; set {EnvironmentNames.HttpPort}")));

    private static Result<DbSettings, ImmutableArray<SettingsError>> ReadDb(JsonObject json, IReadOnlyDictionary<string, string> environment) =>
        Result.Collect(
                Database(environment, EnvironmentNames.ReaderDatabase),
                WholeOr(json, "DbPoolMaxConns", 4, 1, 1000),
                DurationOr(json, "DbPoolMaxConnLifeTime", TimeSpan.FromHours(1)),
                WholeOr(json, "DbTimeout", 10, 1, 3600),
                WholeOr(json, "DbPoolConnectionTimeout", 3, 1, 600))
            .Map(found => new DbSettings(found.Item1, found.Item2, found.Item3, TimeSpan.FromSeconds(found.Item4), TimeSpan.FromSeconds(found.Item5)));

    private static ImmutableArray<SettingsError> One(SettingsError error) => [error];

    private static Result<CatalogueSettings, SettingsError> ReadCatalogue(JsonObject json) =>
        WholeOr(json, "CatalogueRefresh", 30, 0, 86_400).Map(seconds => new CatalogueSettings(TimeSpan.FromSeconds(seconds)));

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

    /// <summary>A whole number within a range.</summary>
    private static Result<int, SettingsError> Whole(JsonObject json, string path, int lowest, int highest) =>
        Number(json, path, lowest, highest).Bind(number => number % 1 == 0
            ? Result.Ok<int, SettingsError>((int)number)
            : Result.Refuse<int, SettingsError>(new SettingsError(path, $"expected a whole number from {lowest} to {highest}")));

    /// <summary>A whole number the file may leave out, as pg_tileserv's keys may be: the fallback then stands.</summary>
    private static Result<int, SettingsError> WholeOr(JsonObject json, string path, int fallback, int lowest, int highest) =>
        Find(json, path).Match(_ => Whole(json, path, lowest, highest), () => Result.Ok<int, SettingsError>(fallback));

    /// <summary>
    /// A length of time in pg_tileserv's form, such as <c>1h</c>, <c>30m</c> or <c>1h30m</c>: hours,
    /// minutes and seconds, each a whole number, in that order. It must lie from a second to a day.
    /// </summary>
    private static Result<TimeSpan, SettingsError> DurationOr(JsonObject json, string path, TimeSpan fallback) =>
        Find(json, path).Match(
            _ => Text(json, path).Bind(text => ParseDuration(text) is { } time && time >= TimeSpan.FromSeconds(1) && time <= TimeSpan.FromDays(1)
                ? Result.Ok<TimeSpan, SettingsError>(time)
                : Result.Refuse<TimeSpan, SettingsError>(new SettingsError(path, "expected a time such as 1h, 30m or 90s, from 1s to 24h"))),
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
