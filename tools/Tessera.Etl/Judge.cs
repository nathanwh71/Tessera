using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NetTopologySuite.Geometries;

namespace Tessera.Etl;

/// <summary>The parts every filter shares: passing or failing rows, and the small measures they use.</summary>
public static class Judge
{
    /// <summary>
    /// The shape of every filter that passes or fails a row. A row <paramref name="fault"/> names no
    /// fault for passes. A row it names a fault for is dropped and logged, or, with
    /// <see cref="OnFail.Stop"/>, ends the filter at once with the reason the run stops.
    /// </summary>
    public static Outcome Rows(ImmutableArray<Row> rows, string filter, OnFail onFail, Func<Row, string?> fault)
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
            if (onFail == OnFail.Stop)
            {
                log.Add(new LogLine(row.Number, filter, Act.Stopped, reason));
                return new Outcome(rows, log.ToImmutable(), $"{filter} stopped the run at row {row.Number}: {reason}.");
            }
            log.Add(new LogLine(row.Number, filter, Act.Dropped, reason));
        }
        return new Outcome(kept.ToImmutable(), log.ToImmutable(), null);
    }

    /// <summary>
    /// The shape of every filter that changes rows and drops none. <paramref name="change"/> returns
    /// the changed row and what it did, or nothing for a row it leaves alone.
    /// </summary>
    public static Outcome Changes(ImmutableArray<Row> rows, string filter, Func<Row, (Row Row, string Did)?> change)
    {
        var log = ImmutableArray.CreateBuilder<LogLine>();
        var changed = rows.Select(row =>
        {
            if (change(row) is not { } done) return row;
            log.Add(new LogLine(row.Number, filter, Act.Fixed, done.Did));
            return done.Row;
        }).ToImmutableArray();
        return new Outcome(changed, log.ToImmutable(), null);
    }

    /// <summary>The area's box, as NetTopologySuite measures it.</summary>
    public static Envelope Box(Area area) => new(area.MinLongitude, area.MaxLongitude, area.MinLatitude, area.MaxLatitude);

    /// <summary>True when any part of the shape touches the box: a road may run out of the area.</summary>
    public static bool Inside(Geometry? shape, Envelope box) =>
        shape is { IsEmpty: false } && box.Intersects(shape.EnvelopeInternal) && shape.Intersects(shape.Factory.ToGeometry(box));

    public static bool IsBlank(object? value) => value is null || value is string text && string.IsNullOrWhiteSpace(text);

    /// <summary>A value as text, for comparing ids and names; numbers keep their exact digits.</summary>
    public static string? Text(object? value) => value switch
    {
        null => null,
        string text => string.IsNullOrWhiteSpace(text) ? null : text,
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    private static readonly Regex Spaces = new(@"\s+");

    /// <summary>
    /// One Unicode form, one space between words, no spaces at the ends; null when nothing is left.
    /// NFC is the form most systems write, so 'é' typed two ways compares equal.
    /// </summary>
    public static string? TidyText(string text)
    {
        var clean = Spaces.Replace(text.Normalize(NormalizationForm.FormC), " ").Trim();
        return clean.Length == 0 ? null : clean;
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
}
