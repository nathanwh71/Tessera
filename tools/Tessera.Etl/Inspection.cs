using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Tessera.Etl;

/// <summary>One layer of a source, as GDAL's ogrinfo describes it.</summary>
public sealed record LayerInfo(string Name, long Count, string Shape, string? System, ImmutableArray<string> Fields)
{
    /// <summary>True for lines and areas, which a pipeline file marks <c>lines</c>.</summary>
    public bool Lines => Shape.Contains("Line", StringComparison.Ordinal) || Shape.Contains("Polygon", StringComparison.Ordinal);
}

/// <summary>
/// The inspect command's pure parts: reading ogrinfo's description of a source, printing it plainly,
/// and writing a starting pipeline file from it (ADR 0009).
/// </summary>
public static class Inspection
{
    /// <summary>The layers in the JSON that <c>ogrinfo -json -so -al</c> writes.</summary>
    public static ImmutableArray<LayerInfo> Read(string ogrinfoJson)
    {
        using var document = JsonDocument.Parse(ogrinfoJson);
        return [.. document.RootElement.GetProperty("layers").EnumerateArray().Select(layer =>
        {
            var shape = layer.TryGetProperty("geometryFields", out var shapes) && shapes.GetArrayLength() > 0 ? shapes[0] : default;
            return new LayerInfo(
                layer.GetProperty("name").GetString() ?? "",
                layer.TryGetProperty("featureCount", out var count) ? count.GetInt64() : 0,
                shape.ValueKind == JsonValueKind.Object ? shape.GetProperty("type").GetString() ?? "None" : "None",
                shape.ValueKind == JsonValueKind.Object ? SystemOf(shape) : null,
                [.. layer.GetProperty("fields").EnumerateArray().Select(field => field.GetProperty("name").GetString() ?? "")]);
        })];
    }

    // The coordinate system as its authority and code, such as EPSG:4326, when GDAL knows one.
    private static string? SystemOf(JsonElement shape) =>
        shape.TryGetProperty("coordinateSystem", out var system)
        && system.ValueKind == JsonValueKind.Object
        && system.TryGetProperty("projjson", out var projjson)
        && projjson.TryGetProperty("id", out var id)
            ? $"{id.GetProperty("authority").GetString()}:{id.GetProperty("code")}"
            : null;

    /// <summary>A plain description of the source: one block for each layer.</summary>
    public static string Summary(string source, ImmutableArray<LayerInfo> layers)
    {
        var text = new StringBuilder();
        text.AppendLine($"{source}: {layers.Length} layer{(layers.Length == 1 ? "" : "s")}");
        foreach (var layer in layers)
        {
            text.AppendLine();
            text.AppendLine($"{layer.Name}");
            text.AppendLine($"  {layer.Count.ToString("N0", CultureInfo.InvariantCulture)} rows of {layer.Shape}, in {layer.System ?? "an unknown system"}");
            text.AppendLine($"  Fields: {(layer.Fields.IsEmpty ? "none" : string.Join(", ", layer.Fields))}");
        }
        return text.ToString();
    }

    // A plain lower-case name, as a table needs: letters, digits and _, starting with a letter or _.
    private static readonly Regex NotPlain = new("[^a-z0-9_]+");

    /// <summary>A name made plain for a table, such as 'Bus Stops' to 'bus_stops'.</summary>
    public static string Plain(string name)
    {
        var plain = NotPlain.Replace(name.ToLowerInvariant(), "_").Trim('_');
        return plain.Length == 0 ? "places" : char.IsDigit(plain[0]) ? $"_{plain}" : plain;
    }

    /// <summary>
    /// A starting pipeline file: one table for each layer, in the schema <c>places</c>, with the
    /// filters most sources need. With <paramref name="split"/>, the one layer becomes one table for
    /// each value of a field, each picked by a <c>where</c>. The maintainer then edits the names,
    /// the schemas and the filters.
    /// </summary>
    public static string Starter(string source, string output, ImmutableArray<LayerInfo> layers, (string Field, ImmutableArray<string> Values)? split)
    {
        var tables = new JsonArray();
        foreach (var layer in layers)
        {
            var parts = split is { } by
                ? by.Values.Select(value => (Name: Plain(value), Where: (string?)$"{Quoted(by.Field)} = '{value.Replace("'", "''", StringComparison.Ordinal)}'"))
                : [(Name: Plain(layer.Name), Where: null)];
            foreach (var (name, where) in parts)
            {
                var table = new JsonObject { ["table"] = $"places.{name}" };
                if (layers.Length > 1 || split is null) table["layer"] = layer.Name;
                if (where is not null) table["where"] = where;
                if (layer.Lines) table["lines"] = true;
                table["filters"] = Filters(layer);
                tables.Add(table);
            }
        }
        var pipeline = new JsonObject { ["source"] = source, ["output"] = output, ["tables"] = tables };
        return pipeline.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    // OGR SQL quotes a field's name in double quotes, so a name with spaces or capitals still works.
    private static string Quoted(string field) => $"\"{field.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    // The filters most sources need. A name field, when there is one, is required.
    private static JsonArray Filters(LayerInfo layer)
    {
        var filters = new JsonArray
        {
            new JsonObject { ["filter"] = "dropEmptyShapes", ["onFail"] = "stop" },
        };
        if (layer.Lines) filters.Add(new JsonObject { ["filter"] = "repairShapes" });
        filters.Add(new JsonObject { ["filter"] = "swapCoordinates" });
        filters.Add(new JsonObject { ["filter"] = "insideArea" });
        filters.Add(new JsonObject { ["filter"] = "tidyText" });
        if (layer.Fields.FirstOrDefault(field => field.Equals("name", StringComparison.OrdinalIgnoreCase)) is { } name)
            filters.Add(new JsonObject { ["filter"] = "requireFields", ["fields"] = new JsonArray(name) });
        return filters;
    }

    /// <summary>The next script's name in <paramref name="existing"/>: the highest number plus one, and the source's name.</summary>
    public static string NextScript(IEnumerable<string> existing, string source)
    {
        var highest = existing
            .Select(Path.GetFileName)
            .Select(name => name is { Length: > 5 } && name[4] == '-' && int.TryParse(name.AsSpan(0, 4), CultureInfo.InvariantCulture, out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();
        var stem = Plain(Path.GetFileNameWithoutExtension(source.TrimEnd('/', '\\'))).Replace('_', '-');
        return $"{(highest + 1).ToString("0000", CultureInfo.InvariantCulture)}-{stem}.sql";
    }
}
