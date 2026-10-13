using System.Collections.Immutable;
using System.Text.Json;
using Tessera.Kernel;
using Tessera.Settings;

namespace Tessera.Import;

/// <summary>Why the import refused a file. The message names the feature, by id and place, where one is to blame.</summary>
public sealed record ImportError(string Message)
{
    public override string ToString() => Message;
}

/// <summary>A property's value as the GeoJSON file holds it: text, a number, true or false, or nothing.</summary>
public abstract record PropertyValue
{
    private PropertyValue() { }

    public sealed record Text(string Value) : PropertyValue;

    public sealed record Number(double Value) : PropertyValue;

    public sealed record Flag(bool Value) : PropertyValue;

    public sealed record Missing : PropertyValue;
}

/// <summary>One place from the file, checked: a point in longitude and latitude, in one of the nine layers.</summary>
public sealed record Place(
    string Id, string Group, string Layer, double Longitude, double Latitude, ImmutableSortedDictionary<string, PropertyValue> Properties);

/// <summary>
/// The four groups and their nine layers (US-24). Each group becomes a schema, a named folder of
/// tables, and each layer a table in it, so <c>food.cafes</c> carries its group in its name.
/// </summary>
public static class Layers
{
    public static readonly ImmutableSortedDictionary<string, string> GroupOfLayer = new Dictionary<string, string>
    {
        ["cafes"] = "food", ["restaurants"] = "food", ["markets"] = "food",
        ["parks"] = "outdoors", ["playgrounds"] = "outdoors",
        ["museums"] = "culture", ["libraries"] = "culture",
        ["trains"] = "transport", ["buses"] = "transport",
    }.ToImmutableSortedDictionary(StringComparer.Ordinal);

    public static readonly ImmutableSortedSet<string> Groups = [.. GroupOfLayer.Values];
}

/// <summary>
/// Reads and checks the GeoJSON seed (US-24 c2 to c4). Every function here is pure: it reads the
/// file's text, not the file, so a test can hand it any text it likes.
/// </summary>
/// <remarks>
/// GeoJSON is JSON for maps: a "FeatureCollection" holds "features", and each feature holds a
/// "geometry", here a point, and "properties", such as its name. RFC 7946, the GeoJSON standard,
/// says every position is longitude then latitude, in degrees of WGS 84, the system GPS uses.
/// These checks are input validation, the defensive design you meet at school: refuse bad data at
/// the door, and say exactly what is wrong, so nothing half-right reaches the database.
/// </remarks>
public static class Seed
{
    /// <summary>The geometry column every table gets; a property may not take its name.</summary>
    public const string GeometryColumn = "geom";

    // The names a crs member may give for longitude and latitude in WGS 84. RFC 7946 dropped the
    // member, so most files carry none; one that names any other system is refused (US-24 c2).
    private static readonly ImmutableHashSet<string> Wgs84Names =
    [
        "urn:ogc:def:crs:OGC:1.3:CRS84", "urn:ogc:def:crs:OGC::CRS84", "urn:ogc:def:crs:EPSG::4326", "EPSG:4326",
    ];

    public static Result<ImmutableArray<Place>, ImportError> Read(string json, AreaBox box)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return Read(document.RootElement, box);
        }
        catch (JsonException error)
        {
            // System.Text.Json reports bad JSON by throwing; this is the one place that catches it.
            return Refuse<ImmutableArray<Place>>($"The file is not valid JSON: {error.Message}");
        }
    }

    private static Result<ImmutableArray<Place>, ImportError> Read(JsonElement root, AreaBox box) =>
        from collection in root.ValueKind == JsonValueKind.Object
                           && root.TryGetProperty("type", out var type) && type.ValueEquals("FeatureCollection")
                           && root.TryGetProperty("features", out var features) && features.ValueKind == JsonValueKind.Array
            ? Result.Ok<JsonElement, ImportError>(features)
            : Refuse<JsonElement>("The file must be a GeoJSON FeatureCollection with a list of features.")
        from _ in CheckCrs(root)
        from places in collection.EnumerateArray().Select((feature, index) => (feature, index))
            .Traverse(pair => ReadFeature(pair.feature, pair.index, box))
        from unique in places.Select(place => place.Id).Distinct(StringComparer.Ordinal).Count() == places.Length
            ? Result.Ok<ImmutableArray<Place>, ImportError>(places)
            : Refuse<ImmutableArray<Place>>("Two features share one id; each id must be unique.")
        select unique;

    private static Result<bool, ImportError> CheckCrs(JsonElement root) =>
        !root.TryGetProperty("crs", out var crs)
        || (crs.TryGetProperty("properties", out var properties)
            && properties.TryGetProperty("name", out var name)
            && name.ValueKind == JsonValueKind.String
            && Wgs84Names.Contains(name.GetString() ?? ""))
            ? Result.Ok<bool, ImportError>(true)
            : Refuse<bool>("The file names another coordinate system. Give positions as longitude and latitude in WGS 84, in degrees, as RFC 7946 sets.");

    private static Result<Place, ImportError> ReadFeature(JsonElement feature, int index, AreaBox box)
    {
        var properties = feature.TryGetProperty("properties", out var found) && found.ValueKind == JsonValueKind.Object
            ? found
            : default;
        var id = Text(feature, "id").Match(text => Option.Some(text), () => properties.ValueKind == JsonValueKind.Object ? Text(properties, "id") : Option.None<string>());
        var name = id.Match(text => $"Feature '{text}' (number {index + 1} in the file)", () => $"Feature number {index + 1} in the file");

        return
            from identity in id.OrRefuse(new ImportError($"{name} has no id."))
            from props in properties.ValueKind == JsonValueKind.Object
                ? Result.Ok<JsonElement, ImportError>(properties)
                : Refuse<JsonElement>($"{name} has no properties.")
            from position in Point(feature).OrRefuse(new ImportError($"{name} is not a point."))
            from layer in Text(props, "layer").Bind(layer => Layers.GroupOfLayer.ContainsKey(layer) ? Option.Some(layer) : Option.None<string>())
                .OrRefuse(new ImportError($"{name} names no layer of the nine."))
            from groupName in Text(props, "group").Bind(found => found == Layers.GroupOfLayer[layer] ? Option.Some(found) : Option.None<string>())
                .OrRefuse(new ImportError($"{name} gives a group other than '{Layers.GroupOfLayer[layer]}', the group of layer '{layer}'."))
            from inside in box.Contains(position.Longitude, position.Latitude)
                ? Result.Ok<bool, ImportError>(true)
                // Swapped longitude and latitude, or metres in place of degrees, land far outside the box (US-24 c4).
                : Refuse<bool>($"{name} lies outside the area's box at [{position.Longitude}, {position.Latitude}]. Check that positions are longitude then latitude, in degrees.")
            from values in props.TryGetProperty(GeometryColumn, out _)
                ? Refuse<ImmutableSortedDictionary<string, PropertyValue>>($"{name} has a property named '{GeometryColumn}', the name of the geometry column.")
                : Result.Ok<ImmutableSortedDictionary<string, PropertyValue>, ImportError>(Values(props))
            select new Place(identity, groupName, layer, position.Longitude, position.Latitude, values);
    }

    private static Option<(double Longitude, double Latitude)> Point(JsonElement feature) =>
        feature.TryGetProperty("geometry", out var geometry)
        && geometry.ValueKind == JsonValueKind.Object
        && geometry.TryGetProperty("type", out var type) && type.ValueEquals("Point")
        && geometry.TryGetProperty("coordinates", out var coordinates)
        && coordinates.ValueKind == JsonValueKind.Array && coordinates.GetArrayLength() >= 2
        && coordinates[0].TryGetDouble(out var longitude) && coordinates[1].TryGetDouble(out var latitude)
            ? Option.Some((longitude, latitude))
            : Option.None<(double, double)>();

    private static Option<string> Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text
            ? Option.Some(text)
            : Option.None<string>();

    // Every property but the id becomes a column; the id is the primary key instead.
    private static ImmutableSortedDictionary<string, PropertyValue> Values(JsonElement properties) =>
        properties.EnumerateObject()
            .Where(property => property.Name != "id" && property.Name.Length > 0)
            .ToImmutableSortedDictionary(property => property.Name, property => Value(property.Value), StringComparer.Ordinal);

    private static PropertyValue Value(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => new PropertyValue.Text(value.GetString() ?? ""),
        JsonValueKind.Number => new PropertyValue.Number(value.GetDouble()),
        JsonValueKind.True => new PropertyValue.Flag(true),
        JsonValueKind.False => new PropertyValue.Flag(false),
        JsonValueKind.Null or JsonValueKind.Undefined => new PropertyValue.Missing(),
        // A list or an object has no column type of its own, so it is stored as its JSON text.
        _ => new PropertyValue.Text(value.GetRawText()),
    };

    private static Result<T, ImportError> Refuse<T>(string message) => Result.Refuse<T, ImportError>(new ImportError(message));
}
