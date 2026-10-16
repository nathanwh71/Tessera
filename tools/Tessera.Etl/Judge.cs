using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NetTopologySuite.Geometries;

namespace Tessera.Etl;

/// <summary>
/// The small measures the <b><i>filters</i></b>' judges share: whether a shape touches the area,
/// whether a value is blank, a value as text, tidy text, and the distance between two points.
/// Sharing them keeps each judge to a few lines, and makes two filters measure alike.
/// </summary>
public static class Judge
{
    /// <summary>
    /// The area's box, as NetTopologySuite's Envelope, a box it can test against a shape's own box
    /// in four comparisons, before the slower test of the shape itself.
    /// </summary>
    public static Envelope Box(Area area) => new(area.MinLongitude, area.MaxLongitude, area.MinLatitude, area.MaxLatitude);

    /// <summary>True when any part of the shape touches the box: a road may run out of the area.</summary>
    public static bool Inside(Geometry? shape, Envelope box) =>
        shape is { IsEmpty: false } && box.Intersects(shape.EnvelopeInternal) && shape.Intersects(shape.Factory.ToGeometry(box));

    /// <summary>
    /// True for no value, or text of spaces alone: such a value shows nothing on the map, so
    /// requireFields treats it as missing.
    /// </summary>
    public static bool IsBlank(object? value) => value is null || value is string text && string.IsNullOrWhiteSpace(text);

    /// <summary>A value as text, for comparing ids and names; numbers keep their exact digits.</summary>
    public static string? Text(object? value) => value switch
    {
        null => null,
        string text => string.IsNullOrWhiteSpace(text) ? null : text,
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    // Any run of white space, tabs and line breaks included: each shows as a gap in a label.
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
