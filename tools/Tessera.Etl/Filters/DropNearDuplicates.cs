using NetTopologySuite.Geometries;

namespace Tessera.Etl.Filters;

/// <summary>
/// dropNearDuplicates: two rows whose field matches, ignoring case and spaces, and whose shapes'
/// centres lie within the given metres, are one place entered twice; the first is kept.
/// </summary>
public static class DropNearDuplicates
{
    public const string Name = "dropNearDuplicates";

    public sealed record Settings(string Field, double Metres, OnFail OnFail = OnFail.Drop);

    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "a row whose `field` matches an earlier row's, within `metres` of it, fails",
        (settings, _) =>
        [
            .. string.IsNullOrWhiteSpace(settings.Field) ? ["needs a field, such as \"name\"."] : Array.Empty<string>(),
            .. settings.Metres > 0 ? Array.Empty<string>() : ["needs metres above 0, such as 25."],
        ],
        (settings, _) => Create(settings.Field, settings.Metres, settings.OnFail));

    public static Filter Create(string field, double metres, OnFail onFail = OnFail.Drop) => rows =>
    {
        var seen = new Dictionary<string, List<(int Number, Point Centre)>>(StringComparer.Ordinal);
        return Judge.Rows(rows, Name, onFail, row =>
        {
            if (Judge.Text(row.Value(field)) is not { } value || row.Shape is null || row.Shape.IsEmpty) return null;
            var key = (Judge.TidyText(value) ?? "").ToLowerInvariant();
            var centre = row.Shape.Centroid;
            if (!seen.TryGetValue(key, out var earlier)) seen[key] = earlier = [];
            foreach (var (number, other) in earlier)
            {
                var distance = Judge.Metres(centre, other);
                if (distance <= metres) return $"its {field} matches row {number}, {distance:0} m away";
            }
            earlier.Add((row.Number, centre));
            return null;
        });
    };
}
