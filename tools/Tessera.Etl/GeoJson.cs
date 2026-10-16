using System.Collections.Immutable;
using System.Text.Json;
using CSharpFunctionalExtensions;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;

namespace Tessera.Etl;

/// <summary>
/// Reads GeoJSON, the text form of places GDAL extracts each table to, into the rows the
/// <b><i>filters</i></b> clean; TableSql then writes the cleaned rows as SQL.
/// </summary>
public static class GeoJson
{
    // NetTopologySuite's converter turns a GeoJSON geometry into a shape. These options
    // stand apart from the pipeline file's strict ones: GDAL writes the GeoJSON, so no key in it
    // can be a publisher's typing mistake.
    private static readonly JsonSerializerOptions Shapes = new() { Converters = { new GeoJsonConverterFactory() } };

    /// <summary>
    /// The rows of a FeatureCollection, numbered from 1 in the file's order, and its columns in the
    /// order they first appear, so the cleaned file keeps them in that order. GDAL writes the file,
    /// so it is well formed unless GDAL fails part-way; then the reason comes back instead of the
    /// rows, and the run stops with its log written, rather than crashing.
    /// </summary>
    public static Result<(ImmutableArray<Row> Rows, ImmutableArray<string> Columns), string> Read(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var rows = ImmutableArray.CreateBuilder<Row>();
            var columns = new List<string>();
            var number = 0;
            foreach (var feature in document.RootElement.GetProperty("features").EnumerateArray())
            {
                number++;
                // The converter gives null when it reads no shape; that becomes none here.
                var shape = feature.TryGetProperty("geometry", out var geometry) && geometry.ValueKind == JsonValueKind.Object
                    ? geometry.Deserialize<Geometry>(Shapes).AsMaybe()
                    : Maybe<Geometry>.None;
                var values = ImmutableDictionary.CreateBuilder<string, object?>(StringComparer.Ordinal);
                // GDAL writes a text field named id as the feature's own id, outside its properties;
                // it comes back here as the id column, first, as it stood in the source.
                if (feature.TryGetProperty("id", out var id))
                {
                    if (!columns.Contains("id")) columns.Insert(0, "id");
                    values["id"] = Value(id);
                }
                if (feature.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in properties.EnumerateObject())
                    {
                        if (!columns.Contains(property.Name)) columns.Add(property.Name);
                        values[property.Name] = Value(property.Value);
                    }
                }
                rows.Add(new Row(number, shape, values.ToImmutable()));
            }
            return Result.Success<(ImmutableArray<Row>, ImmutableArray<string>), string>((rows.ToImmutable(), [.. columns]));
        }
        // The three ways a file that is not a FeatureCollection fails: text that is not JSON, a
        // missing key such as features, and a value of the wrong kind. Any other exception is a fault
        // in this code, and escapes.
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return Result.Failure<(ImmutableArray<Row>, ImmutableArray<string>), string>(error.Message);
        }
    }

    // A JSON value as the plain .NET value a filter compares: text, a whole number, a number with a
    // fraction, true or false, or null. A list or an object stays as its JSON text. Null stays null,
    // not none, since it is a value the written GeoJSON must carry back (see Row).
    private static object? Value(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        // Boxed apart: 'a ? long : double' would make the whole number a double too.
        JsonValueKind.Number => value.TryGetInt64(out var whole) ? (object)whole : value.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => value.GetRawText(),
    };

    /// <summary>
    /// The columns to write, taken from the kept rows themselves, so a filter that adds a column
    /// reaches the script and one that removes a column leaves no empty one behind. The source's
    /// columns come first, in their order; new ones follow, by the first row that holds them, and
    /// by name within that row, since a row keeps no order of its own among its values. A
    /// column no kept row holds is left out; an empty source column stays, since each row still
    /// holds its key, with null.
    /// </summary>
    public static ImmutableArray<string> Columns(ImmutableArray<Row> rows, ImmutableArray<string> sourceColumns)
    {
        var held = rows.SelectMany(row => row.Values.Keys).ToHashSet(StringComparer.Ordinal);
        var added = rows.SelectMany(row => row.Values.Keys.Order(StringComparer.Ordinal))
            .Where(column => !sourceColumns.Contains(column))
            .Distinct();
        return [.. sourceColumns.Where(held.Contains), .. added];
    }
}
