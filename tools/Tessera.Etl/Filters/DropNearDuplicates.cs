using NetTopologySuite.Geometries;

namespace Tessera.Etl.Filters;

/// <summary>
/// dropNearDuplicates: two rows whose field matches, ignoring case and spaces, and whose shapes'
/// centres lie within the given metres, are one place entered twice; the first is kept. Neither
/// test alone is enough: a name repeats across a city, and a position repeats in a busy building.
/// </summary>
public static class DropNearDuplicates
{
    /// <summary>
    /// The name a <b><i>pipeline file</i></b> uses; the log and the tests name the
    /// <b><i>filter</i></b> by it too, so one constant keeps all three alike.
    /// </summary>
    public const string Name = "dropNearDuplicates";

    /// <summary>
    /// The settings a pipeline file may give it. Field and Metres have no default, since only the
    /// publisher knows which column names a place and how close counts as the same place; a
    /// failing row is dropped unless the file says stop.
    /// </summary>
    public sealed record Settings(string Field, double Metres, OnFail OnFail = OnFail.Drop);

    /// <summary>The kind Filters/All.cs lists, so a pipeline file may name this filter.</summary>
    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "a row whose `field` matches an earlier row's, within `metres` of it, fails",
        (settings, _) =>
        [
            .. string.IsNullOrWhiteSpace(settings.Field) ? ["needs a field, such as \"name\"."] : Array.Empty<string>(),
            .. settings.Metres > 0 ? Array.Empty<string>() : ["needs metres above 0, such as 25."],
        ],
        (settings, _) => Create(settings.Field, settings.Metres, settings.OnFail));

    /// <summary>
    /// Builds the filter from plain values, so a test can make one without a pipeline file.
    /// </summary>
    public static Handler<Cleaning> Create(string field, double metres, OnFail onFail = OnFail.Drop) =>
        Steps.Filter(Name, onFail, rows =>
        {
            // Each name keeps a list of earlier places, not one: two cafés of one chain may share a
            // name far apart, and both are real, so a third must be measured against each.
            var seen = new Dictionary<string, List<(int Number, Point Centre)>>(StringComparer.Ordinal);
            return [.. rows.Select(row =>
            {
                // A row with no name or no shape passes: requireFields and dropEmptyShapes judge those.
                if (Judge.Text(row.Value(field)) is not { } value || row.Shape is null || row.Shape.IsEmpty) return Verdict.Keep(row);
                var key = (Judge.TidyText(value) ?? "").ToLowerInvariant();
                var centre = row.Shape.Centroid;
                if (!seen.TryGetValue(key, out var earlier)) seen[key] = earlier = [];
                foreach (var (number, other) in earlier)
                {
                    var distance = Judge.Metres(centre, other);
                    if (distance <= metres) return Verdict.Fail(row, $"its {field} matches row {number}, {distance:0} m away");
                }
                earlier.Add((row.Number, centre));
                return Verdict.Keep(row);
            })];
        });
}
