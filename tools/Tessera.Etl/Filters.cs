using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Operation.Valid;

namespace Tessera.Etl;

/// <summary>
/// The cleaning filters a pipeline file may name. Each turns a <see cref="FilterStep"/> into a
/// <see cref="Filter"/>: a pure function from one table's rows to an <see cref="Outcome"/>.
/// A new kind of filter is one more entry in <see cref="Known"/> and one function here.
/// </summary>
public static class Filters
{
    /// <summary>Every filter's name, and what it does, as the README lists them.</summary>
    public static readonly ImmutableSortedDictionary<string, string> Known = new Dictionary<string, string>
    {
        ["dropEmptyShapes"] = "a row with no shape, or an empty one, fails",
        ["repairShapes"] = "an invalid shape, such as an area whose edge crosses itself, is repaired; one that cannot be repaired fails",
        ["swapCoordinates"] = "a shape outside the area that falls inside it with longitude and latitude swapped is swapped back",
        ["insideArea"] = "a shape that does not touch the area's box fails",
        ["tidyText"] = "text is put into one Unicode form, runs of spaces become one, ends are trimmed, and empty text becomes null",
        ["requireFields"] = "a row missing any of `fields`, or holding only spaces there, fails",
        ["dropDuplicateIds"] = "a row that repeats an earlier row's `field` fails",
        ["dropNearDuplicates"] = "a row whose `field` matches an earlier row's, within `metres` of it, fails",
    }.ToImmutableSortedDictionary();

    /// <summary>What is wrong with a step's settings; nothing when it can run.</summary>
    public static IEnumerable<string> Problems(FilterStep step, Area? area)
    {
        if (!Known.ContainsKey(step.Filter ?? ""))
        {
            yield return $"'{step.Filter}' is no filter; the filters are {string.Join(", ", Known.Keys)}.";
            yield break;
        }
        if (step.Filter is "swapCoordinates" or "insideArea" && area is null)
            yield return $"{step.Filter} needs an area: give one in the pipeline file, or run beside tessera.json.";
        if (step.Filter == "requireFields" && (step.Fields is not { IsDefaultOrEmpty: false }))
            yield return "requireFields needs fields, such as [\"name\"].";
        if (step.Filter is "dropDuplicateIds" or "dropNearDuplicates" && string.IsNullOrWhiteSpace(step.Field))
            yield return $"{step.Filter} needs a field, such as \"id\".";
        if (step.Filter == "dropNearDuplicates" && step.Metres is not > 0)
            yield return "dropNearDuplicates needs metres above 0, such as 25.";
    }

    /// <summary>The filter a step names. Call <see cref="Problems"/> first: this trusts the settings.</summary>
    public static Filter For(FilterStep step, Area? area) => step.Filter switch
    {
        "dropEmptyShapes" => rows => Judge(rows, step, row => row.Shape is null or { IsEmpty: true } ? "it has no shape" : null),
        "repairShapes" => rows => Repair(rows, step),
        "swapCoordinates" => rows => Swap(rows, step.Filter, Box(area!)),
        "insideArea" => rows => Judge(rows, step, row => Inside(row.Shape, Box(area!)) ? null : "its shape lies outside the area"),
        "tidyText" => rows => Tidy(rows, step),
        "requireFields" => rows => Judge(rows, step, row => step.Fields!.Value.FirstOrDefault(field => IsBlank(row.Value(field))) is { } missing ? $"{missing} is missing" : null),
        "dropDuplicateIds" => rows => DuplicateIds(rows, step),
        "dropNearDuplicates" => rows => NearDuplicates(rows, step),
        _ => throw new ArgumentException($"'{step.Filter}' is no filter.", nameof(step)),
    };

    /// <summary>
    /// The shared shape of every filter that passes or fails a row. A row <paramref name="fault"/>
    /// names no fault for passes. A row it does name a fault for is dropped and logged, or, when the
    /// step says stop, ends the filter at once with the reason the run stops.
    /// </summary>
    private static Outcome Judge(ImmutableArray<Row> rows, FilterStep step, Func<Row, string?> fault)
    {
        var kept = ImmutableArray.CreateBuilder<Row>();
        var log = ImmutableArray.CreateBuilder<LogLine>();
        foreach (var row in rows)
        {
            if (fault(row) is not { } reason)
            {
                kept.Add(row);
                continue;
            }
            if (step.OnFail == OnFail.Stop)
            {
                log.Add(new LogLine(row.Number, step.Filter, Act.Stopped, reason));
                return new Outcome(rows, log.ToImmutable(), $"{step.Filter} stopped the run at row {row.Number}: {reason}.");
            }
            log.Add(new LogLine(row.Number, step.Filter, Act.Dropped, reason));
        }
        return new Outcome(kept.ToImmutable(), log.ToImmutable(), null);
    }

    // repairShapes: NetTopologySuite's GeometryFixer rebuilds an invalid shape into a valid one,
    // such as an area whose edge crosses itself into two areas. A shape that comes out empty fails.
    private static Outcome Repair(ImmutableArray<Row> rows, FilterStep step)
    {
        var fixes = new List<LogLine>();
        var beyondRepair = new HashSet<int>();
        var repaired = rows.Select(row =>
        {
            if (row.Shape is null || row.Shape.IsEmpty || row.Shape.IsValid) return row;
            var why = new IsValidOp(row.Shape).ValidationError?.Message ?? "invalid";
            var fixedShape = GeometryFixer.Fix(row.Shape);
            if (fixedShape.IsEmpty)
            {
                beyondRepair.Add(row.Number);
                return row;
            }
            fixes.Add(new LogLine(row.Number, step.Filter, Act.Fixed, $"repaired a shape that was invalid ({why})"));
            return row with { Shape = fixedShape };
        }).ToImmutableArray();
        var judged = Judge(repaired, step, row => beyondRepair.Contains(row.Number) ? "its shape is invalid and cannot be repaired" : null);
        return judged with { Log = [.. fixes, .. judged.Log] };
    }

    // swapCoordinates: a common slip writes latitude where longitude belongs. A shape outside the
    // area whose swapped copy falls inside it is swapped back; anything else is left alone.
    private static Outcome Swap(ImmutableArray<Row> rows, string name, Envelope box)
    {
        var log = ImmutableArray.CreateBuilder<LogLine>();
        var swapped = rows.Select(row =>
        {
            if (row.Shape is null || row.Shape.IsEmpty || Inside(row.Shape, box)) return row;
            var turned = row.Shape.Copy();
            turned.Apply(new SwapAxes());
            if (!Inside(turned, box)) return row;
            log.Add(new LogLine(row.Number, name, Act.Fixed, "swapped longitude and latitude"));
            return row with { Shape = turned };
        }).ToImmutableArray();
        return new Outcome(swapped, log.ToImmutable(), null);
    }

    // tidyText: NFC is the Unicode form most systems write, so 'é' typed two ways compares equal.
    private static Outcome Tidy(ImmutableArray<Row> rows, FilterStep step)
    {
        var log = ImmutableArray.CreateBuilder<LogLine>();
        var tidied = rows.Select(row =>
        {
            var changed = new List<string>();
            var values = row.Values;
            foreach (var (column, value) in row.Values)
            {
                if (value is not string text || (step.Fields is { IsDefaultOrEmpty: false } only && !only.Contains(column))) continue;
                var clean = TidyText(text);
                if (clean == text) continue;
                changed.Add(column);
                values = values.SetItem(column, clean);
            }
            if (changed.Count == 0) return row;
            log.Add(new LogLine(row.Number, step.Filter, Act.Fixed, $"tidied {string.Join(", ", changed.Order(StringComparer.Ordinal))}"));
            return row with { Values = values };
        }).ToImmutableArray();
        return new Outcome(tidied, log.ToImmutable(), null);
    }

    private static readonly Regex Spaces = new(@"\s+");

    /// <summary>One Unicode form, one space between words, no spaces at the ends; null when nothing is left.</summary>
    public static string? TidyText(string text)
    {
        var clean = Spaces.Replace(text.Normalize(NormalizationForm.FormC), " ").Trim();
        return clean.Length == 0 ? null : clean;
    }

    // dropDuplicateIds: the first row with a value keeps it; each later row with the same value fails.
    // A row with no value passes, since requireFields is the filter that asks for one.
    private static Outcome DuplicateIds(ImmutableArray<Row> rows, FilterStep step)
    {
        var first = new Dictionary<string, int>(StringComparer.Ordinal);
        return Judge(rows, step, row =>
        {
            if (Text(row.Value(step.Field!)) is not { } id) return null;
            if (first.TryGetValue(id, out var earlier)) return $"it repeats {step.Field} '{id}' of row {earlier}";
            first[id] = row.Number;
            return null;
        });
    }

    // dropNearDuplicates: two rows whose field matches, ignoring case and spaces, and whose shapes'
    // centres lie within the given metres, are one place entered twice; the first is kept.
    private static Outcome NearDuplicates(ImmutableArray<Row> rows, FilterStep step)
    {
        var seen = new Dictionary<string, List<(int Number, Point Centre)>>(StringComparer.Ordinal);
        return Judge(rows, step, row =>
        {
            if (Text(row.Value(step.Field!)) is not { } value || row.Shape is null || row.Shape.IsEmpty) return null;
            var key = (TidyText(value) ?? "").ToLowerInvariant();
            var centre = row.Shape.Centroid;
            if (!seen.TryGetValue(key, out var earlier)) seen[key] = earlier = [];
            foreach (var (number, other) in earlier)
            {
                var metres = Metres(centre, other);
                if (metres <= step.Metres) return $"its {step.Field} matches row {number}, {metres:0} m away";
            }
            earlier.Add((row.Number, centre));
            return null;
        });
    }

    /// <summary>
    /// The distance in metres between two points given in longitude and latitude, along the Earth's
    /// surface, by the haversine formula on a sphere of the Earth's mean radius. Over a few hundred
    /// metres it errs by well under a metre.
    /// </summary>
    public static double Metres(Point a, Point b)
    {
        const double radius = 6_371_008.8;
        static double Radians(double degrees) => degrees * Math.PI / 180;
        var dLat = Radians(b.Y - a.Y);
        var dLon = Radians(b.X - a.X);
        var h = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(Radians(a.Y)) * Math.Cos(Radians(b.Y)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * radius * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    private static Envelope Box(Area area) => new(area.MinLongitude, area.MaxLongitude, area.MinLatitude, area.MaxLatitude);

    // A shape counts as inside when any part of it touches the box: a road may run out of the area.
    private static bool Inside(Geometry? shape, Envelope box) =>
        shape is { IsEmpty: false } && box.Intersects(shape.EnvelopeInternal) && shape.Intersects(shape.Factory.ToGeometry(box));

    private static bool IsBlank(object? value) => value is null || value is string text && string.IsNullOrWhiteSpace(text);

    // A value as text, for comparing ids and names; numbers keep their exact digits.
    private static string? Text(object? value) => value switch
    {
        null => null,
        string text => string.IsNullOrWhiteSpace(text) ? null : text,
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    /// <summary>Swaps each point's two numbers, so (latitude, longitude) becomes (longitude, latitude).</summary>
    private sealed class SwapAxes : IEntireCoordinateSequenceFilter
    {
        public bool Done => false;

        public bool GeometryChanged => true;

        public void Filter(CoordinateSequence sequence)
        {
            for (var i = 0; i < sequence.Count; i++)
            {
                var x = sequence.GetX(i);
                sequence.SetX(i, sequence.GetY(i));
                sequence.SetY(i, x);
            }
        }
    }
}
