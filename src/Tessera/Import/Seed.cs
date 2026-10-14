using System.Collections.Immutable;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Tessera.Settings;

// The reader for the seed file, the GeoJSON file of Macau's places (US-24 c2 to c4). Seed turns the
// file's text into checked places, or names what is wrong; Importer then loads them.

namespace Tessera.Import;

/// <summary>Why the import refused a file; the message names the feature to blame, by id and place.</summary>
public sealed record ImportError(string Message)
{
    public override string ToString() => Message;
}

/// <summary>
/// A property's value as the file holds it: text, a number, true or false, or nothing. The private
/// constructor means these four are the only kinds.
/// </summary>
public abstract record PropertyValue
{
    private PropertyValue() { }

    public sealed record Text(string Value) : PropertyValue;

    public sealed record Number(double Value) : PropertyValue;

    public sealed record Flag(bool Value) : PropertyValue;

    public sealed record Missing : PropertyValue;
}

/// <summary>One checked place: a point in longitude and latitude, in one of the nine layers.</summary>
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
/// Reads and checks the GeoJSON seed: refuse bad data at the door and say exactly what is wrong, so
/// nothing half-right reaches the database. Every function here reads text, never a file, so a test
/// can hand it any text it likes.
/// </summary>
/// <remarks>
/// GeoJSON is JSON for maps: a FeatureCollection holds features, each with a geometry, here a point,
/// and properties. RFC 7946 says every position is longitude then latitude, in degrees of WGS 84.
/// </remarks>
public static class Seed
{
    /// <summary>The geometry column every table gets; a property may not take its name.</summary>
    public const string GeometryColumn = "geom";

    // The names a crs member may give for WGS 84. RFC 7946 dropped the member, so most files carry
    // none; a file that names any other system is refused (US-24 c2).
    private static readonly ImmutableHashSet<string> Wgs84Names =
    [
        "urn:ogc:def:crs:OGC:1.3:CRS84", "urn:ogc:def:crs:OGC::CRS84", "urn:ogc:def:crs:EPSG::4326", "EPSG:4326",
    ];

    /// <summary>
    /// Reads the file into checked places. Refuses the whole file, naming the first fault, if it is
    /// not a FeatureCollection, names another coordinate system, holds a feature that fails a check,
    /// or gives two features one id. Every place must lie inside <paramref name="box"/> (US-24 c4).
    /// </summary>
    public static Result<ImmutableArray<Place>, ImportError> Read(string json, AreaSettings box)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return Read(document.RootElement, box);
        }
        catch (JsonException error)
        {
            // System.Text.Json reports text that is not JSON by throwing, so the parse sits in a try.
            return Refuse<ImmutableArray<Place>>($"The file is not valid JSON: {error.Message}");
        }
    }

    private static Result<ImmutableArray<Place>, ImportError> Read(JsonElement root, AreaSettings box)
    {
        if (!IsText(Child(root, "type"), "FeatureCollection") || Child(root, "features") is not { ValueKind: JsonValueKind.Array } features)
            return Refuse<ImmutableArray<Place>>("The file must be a GeoJSON FeatureCollection with a list of features.");
        // A file with no crs passes, since RFC 7946 sets WGS 84 for every file.
        if (Child(root, "crs") is { } crs && !(Child(crs, "properties") is { } named && Text(named, "name") is { } name && Wgs84Names.Contains(name)))
            return Refuse<ImmutableArray<Place>>("The file names another coordinate system. Give positions as longitude and latitude in WGS 84, in degrees, as RFC 7946 sets.");

        return features.EnumerateArray()
            .Select((feature, index) => ReadFeature(feature, index, box))
            .Combine(errors => errors.First())
            .Map(places => places.ToImmutableArray())
            .Ensure(places => places.Select(place => place.Id).Distinct(StringComparer.Ordinal).Count() == places.Length,
                new ImportError("Two features share one id; each id must be unique."));
    }

    // The id may sit on the feature or among its properties; the feature's own wins. Each message
    // names the feature by its id and its number in the file, counting from 1, so it can be found.
    private static Result<Place, ImportError> ReadFeature(JsonElement feature, int index, AreaSettings box)
    {
        var properties = Child(feature, "properties") is { ValueKind: JsonValueKind.Object } found ? found : (JsonElement?)null;
        var id = Text(feature, "id") ?? (properties is { } holder ? Text(holder, "id") : null);
        var name = id is null ? $"Feature number {index + 1} in the file" : $"Feature '{id}' (number {index + 1} in the file)";

        if (id is null)
            return Refuse<Place>($"{name} has no id.");
        if (properties is not { } props)
            return Refuse<Place>($"{name} has no properties.");
        if (Point(feature) is not { } position)
            return Refuse<Place>($"{name} is not a point.");
        if (Text(props, "layer") is not { } layer || !Layers.GroupOfLayer.TryGetValue(layer, out var group))
            return Refuse<Place>($"{name} names no layer of the nine.");
        if (Text(props, "group") != group)
            return Refuse<Place>($"{name} gives a group other than '{group}', the group of layer '{layer}'.");
        // Swapped longitude and latitude, or metres in place of degrees, land far outside the box.
        if (!box.Contains(position.Longitude, position.Latitude))
            return Refuse<Place>($"{name} lies outside the area's box at [{position.Longitude}, {position.Latitude}]. Check that positions are longitude then latitude, in degrees.");

        return Values(props, name).Map(values => new Place(id, group, layer, position.Longitude, position.Latitude, values));
    }

    // A point's coordinates are at least two numbers, longitude first; a third, a height, is ignored.
    private static (double Longitude, double Latitude)? Point(JsonElement feature) =>
        Child(feature, "geometry") is { } geometry && IsText(Child(geometry, "type"), "Point")
        && Child(geometry, "coordinates") is { ValueKind: JsonValueKind.Array } coordinates && coordinates.GetArrayLength() >= 2
        && Number(coordinates[0]) is { } longitude && Number(coordinates[1]) is { } latitude
            ? (longitude, latitude)
            : null;

    // Every property but the id becomes a column. A property with no name, or the geometry column's
    // name, or one name twice, has no column it can take, so the file is refused rather than a value
    // vanishing without a word.
    private static Result<ImmutableSortedDictionary<string, PropertyValue>, ImportError> Values(JsonElement properties, string feature)
    {
        var members = properties.EnumerateObject().Where(property => property.Name != "id").ToImmutableArray();
        return members.Any(property => property.Name.Length == 0)
            ? Refuse<ImmutableSortedDictionary<string, PropertyValue>>($"{feature} has a property with no name. Give it a name, or remove it.")
            : members.Any(property => property.Name == GeometryColumn)
            ? Refuse<ImmutableSortedDictionary<string, PropertyValue>>($"{feature} has a property named '{GeometryColumn}', the name of the geometry column.")
            : members.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() != members.Length
            ? Refuse<ImmutableSortedDictionary<string, PropertyValue>>($"{feature} names one property twice.")
            : members.ToImmutableSortedDictionary(property => property.Name, property => Value(property.Value), StringComparer.Ordinal);
    }

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

    // System.Text.Json throws if asked for a member of something that is not an object, so every
    // lookup comes through here.
    private static JsonElement? Child(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : null;

    private static bool IsText(JsonElement? element, string expected) =>
        element is { ValueKind: JsonValueKind.String } found && found.ValueEquals(expected);

    private static double? Number(JsonElement element) =>
        element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var number) ? number : null;

    // Empty text counts as nothing, so a feature with "id": "" has no id.
    private static string? Text(JsonElement element, string name) =>
        Child(element, name) is { ValueKind: JsonValueKind.String } value && value.GetString() is { Length: > 0 } text ? text : null;

    private static Result<T, ImportError> Refuse<T>(string message) => Result.Failure<T, ImportError>(new ImportError(message));
}
