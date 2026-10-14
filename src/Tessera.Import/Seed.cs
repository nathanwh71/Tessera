using System.Collections.Immutable;
using System.Text.Json;
using Tessera.Kernel;
using Tessera.Settings;

// The Import module's reader for the seed file, the GeoJSON file of Macau's places (US-24 c2 to c4).
// Seed turns the file's text into checked places, or names what is wrong with it; Importer, in this
// module, then loads the places into the database. Layers lists the four groups and nine layers.

namespace Tessera.Import;

/// <summary>
/// Why the import refused a file. The message names the feature, by id and place, where one is to blame.
/// </summary>
public sealed record ImportError(string Message)
{
    public override string ToString() => Message;
}

/// <summary>
/// A property's value as the GeoJSON file holds it: text, a number, true or false, or nothing. The
/// private constructor means these four are the only kinds.
/// </summary>
public abstract record PropertyValue
{
    private PropertyValue() { }

    public sealed record Text(string Value) : PropertyValue;

    public sealed record Number(double Value) : PropertyValue;

    public sealed record Flag(bool Value) : PropertyValue;

    public sealed record Missing : PropertyValue;
}

/// <summary>
/// One place from the file, checked: a point in longitude and latitude, in one of the nine layers.
/// </summary>
public sealed record Place(
    string Id, string Group, string Layer, double Longitude, double Latitude, ImmutableSortedDictionary<string, PropertyValue> Properties);

/// <summary>
/// The four groups and their nine layers (US-24). Each group becomes a schema, a named folder of
/// tables, and each layer a table in it, so <c>food.cafes</c> carries its group in its name.
/// </summary>
public static class Layers
{
    /// <summary>Each layer's group, by layer name.</summary>
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
/// Reads and checks the GeoJSON seed (US-24 c2 to c4). Every function here is pure, a function that
/// only computes its answer and touches nothing outside: it reads the file's text, not the file, so a
/// test can hand it any text it likes.
/// </summary>
/// <remarks>
/// GeoJSON is JSON for maps: a 'FeatureCollection' holds 'features', and each feature holds a
/// 'geometry', here a point, and 'properties', such as its name. RFC 7946, the GeoJSON standard,
/// says every position is longitude then latitude, in degrees of WGS 84, the system GPS uses.
/// <para>
/// These checks are input validation: refuse bad data at the door, and say exactly what is wrong,
/// so nothing half-right reaches the database.
/// </para>
/// </remarks>
public static class Seed
{
    /// <summary>The geometry column every table gets; a property may not take its name.</summary>
    public const string GeometryColumn = "geom";

    // The names a crs member, the part of a file that names its coordinate system, may give for
    // longitude and latitude in WGS 84. RFC 7946 dropped the member, so most files carry none; one
    // that names any other system is refused (US-24 c2).
    private static readonly ImmutableHashSet<string> Wgs84Names =
    [
        "urn:ogc:def:crs:OGC:1.3:CRS84", "urn:ogc:def:crs:OGC::CRS84", "urn:ogc:def:crs:EPSG::4326", "EPSG:4326",
    ];

    /// <summary>
    /// Reads the file's text into checked places. Refuses the whole file, naming the first fault, if
    /// it is not a FeatureCollection, names another coordinate system, holds a feature that fails a
    /// check, or gives two features one id.
    /// </summary>
    /// <param name="json">The whole text of the GeoJSON file.</param>
    /// <param name="box">The area's edges in degrees; every place must lie inside them (US-24 c4).</param>
    public static Result<ImmutableArray<Place>, ImportError> Read(string json, AreaBox box)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return Read(document.RootElement, box);
        }
        catch (JsonException error)
        {
            // System.Text.Json reports text that is not JSON by throwing an exception, an error that
            // jumps out of the normal flow, so the parse alone sits in a try.
            return Refuse<ImmutableArray<Place>>($"The file is not valid JSON: {error.Message}");
        }
    }

    // The 'from ... select' form runs each check in turn and stops at the first refusal.
    private static Result<ImmutableArray<Place>, ImportError> Read(JsonElement root, AreaBox box) =>
        from collection in IsText(Child(root, "type"), "FeatureCollection")
            ? Child(root, "features")
                .Bind(features => features.ValueKind == JsonValueKind.Array ? Option.Some(features) : Option.None<JsonElement>())
                .OrRefuse(new ImportError("The file must be a GeoJSON FeatureCollection with a list of features."))
            : Refuse<JsonElement>("The file must be a GeoJSON FeatureCollection with a list of features.")
        from _ in CheckCrs(root)
        from places in collection.EnumerateArray().Select((feature, index) => (feature, index))
            .Traverse(pair => ReadFeature(pair.feature, pair.index, box))
        from unique in places.Select(place => place.Id).Distinct(StringComparer.Ordinal).Count() == places.Length
            ? Result.Ok<ImmutableArray<Place>, ImportError>(places)
            : Refuse<ImmutableArray<Place>>("Two features share one id; each id must be unique.")
        select unique;

    // A file with no crs member passes, since RFC 7946 sets WGS 84 for every file.
    private static Result<bool, ImportError> CheckCrs(JsonElement root) =>
        Child(root, "crs").Match(
            crs => Text(Child(crs, "properties").OrElse(default), "name").Match(name => Wgs84Names.Contains(name), () => false),
            () => true)
            ? Result.Ok<bool, ImportError>(true)
            : Refuse<bool>("The file names another coordinate system. Give positions as longitude and latitude in WGS 84, in degrees, as RFC 7946 sets.");

    // The id may sit on the feature or among its properties; the feature's own wins. The name built
    // here gives each message the feature's id and its number in the file, counting from 1, so the
    // maintainer can find it.
    private static Result<Place, ImportError> ReadFeature(JsonElement feature, int index, AreaBox box)
    {
        var properties = Child(feature, "properties").Bind(found => found.ValueKind == JsonValueKind.Object ? Option.Some(found) : Option.None<JsonElement>());
        var id = Text(feature, "id").Match(Option.Some, () => properties.Bind(found => Text(found, "id")));
        var name = id.Match(text => $"Feature '{text}' (number {index + 1} in the file)", () => $"Feature number {index + 1} in the file");

        return
            from identity in id.OrRefuse(new ImportError($"{name} has no id."))
            from props in properties.OrRefuse(new ImportError($"{name} has no properties."))
            from position in Point(feature).OrRefuse(new ImportError($"{name} is not a point."))
            from layer in Text(props, "layer").Bind(layer => Layers.GroupOfLayer.ContainsKey(layer) ? Option.Some(layer) : Option.None<string>())
                .OrRefuse(new ImportError($"{name} names no layer of the nine."))
            from groupName in Text(props, "group").Bind(found => found == Layers.GroupOfLayer[layer] ? Option.Some(found) : Option.None<string>())
                .OrRefuse(new ImportError($"{name} gives a group other than '{Layers.GroupOfLayer[layer]}', the group of layer '{layer}'."))
            from inside in box.Contains(position.Longitude, position.Latitude)
                ? Result.Ok<bool, ImportError>(true)
                // Swapped longitude and latitude, or metres in place of degrees, land far outside the box
                // (US-24 c4).
                : Refuse<bool>($"{name} lies outside the area's box at [{position.Longitude}, {position.Latitude}]. Check that positions are longitude then latitude, in degrees.")
            from values in Values(props, name)
            select new Place(identity, groupName, layer, position.Longitude, position.Latitude, values);
    }

    // A point's coordinates are a list of at least two numbers, longitude first; any third number,
    // a height, is ignored.
    private static Option<(double Longitude, double Latitude)> Point(JsonElement feature) =>
        from geometry in Child(feature, "geometry")
        from coordinates in IsText(Child(geometry, "type"), "Point") ? Child(geometry, "coordinates") : Option.None<JsonElement>()
        from pair in coordinates.ValueKind == JsonValueKind.Array && coordinates.GetArrayLength() >= 2
            ? Option.Some((coordinates[0], coordinates[1]))
            : Option.None<(JsonElement, JsonElement)>()
        from longitude in Number(pair.Item1)
        from latitude in Number(pair.Item2)
        select (longitude, latitude);

    /// <summary>
    /// A member of a JSON object, or nothing. System.Text.Json throws if asked for a member of
    /// something that is not an object, such as a number or null, so every lookup comes through here.
    /// </summary>
    private static Option<JsonElement> Child(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            ? Option.Some(value)
            : Option.None<JsonElement>();

    private static bool IsText(Option<JsonElement> element, string expected) =>
        element.Match(found => found.ValueKind == JsonValueKind.String && found.ValueEquals(expected), () => false);

    private static Option<double> Number(JsonElement element) =>
        element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var number) ? Option.Some(number) : Option.None<double>();

    // Empty text counts as nothing, so a feature with "id": "" has no id.
    private static Option<string> Text(JsonElement element, string name) =>
        Child(element, name).Bind(value =>
            value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text ? Option.Some(text) : Option.None<string>());

    // Every property but the id becomes a column; the id is the primary key, the column whose values
    // name each row, instead. A property with an empty name is refused, naming the feature: no column
    // can take it, and a value that vanished without a word would be a fault no one could find.
    // JSON allows one name twice in an object, but a table cannot hold two columns of one name.
    private static Result<ImmutableSortedDictionary<string, PropertyValue>, ImportError> Values(JsonElement properties, string feature)
    {
        var members = properties.EnumerateObject().Where(property => property.Name != "id").ToImmutableArray();
        return members.Any(property => property.Name.Length == 0)
            ? Refuse<ImmutableSortedDictionary<string, PropertyValue>>($"{feature} has a property with no name. Give it a name, or remove it.")
            : members.Any(property => property.Name == GeometryColumn)
            ? Refuse<ImmutableSortedDictionary<string, PropertyValue>>($"{feature} has a property named '{GeometryColumn}', the name of the geometry column.")
            : members.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() != members.Length
                ? Refuse<ImmutableSortedDictionary<string, PropertyValue>>($"{feature} names one property twice.")
                : Result.Ok<ImmutableSortedDictionary<string, PropertyValue>, ImportError>(
                    members.ToImmutableSortedDictionary(property => property.Name, property => Value(property.Value), StringComparer.Ordinal));
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

    private static Result<T, ImportError> Refuse<T>(string message) => Result.Refuse<T, ImportError>(new ImportError(message));
}
