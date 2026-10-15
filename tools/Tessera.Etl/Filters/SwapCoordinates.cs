using NetTopologySuite.Geometries;

namespace Tessera.Etl.Filters;

/// <summary>
/// swapCoordinates: a common slip writes latitude where longitude belongs. A shape outside the
/// area whose swapped copy falls inside it is swapped back; anything else is left alone.
/// </summary>
public static class SwapCoordinates
{
    public const string Name = "swapCoordinates";

    public sealed record Settings;

    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "a shape outside the area that falls inside it with longitude and latitude swapped is swapped back",
        FilterKind.NeedsArea, (_, area) => Create(area!));

    public static Filter Create(Area area) => rows =>
    {
        var box = Judge.Box(area);
        return Judge.Changes(rows, Name, row =>
        {
            if (row.Shape is null || row.Shape.IsEmpty || Judge.Inside(row.Shape, box)) return null;
            var turned = row.Shape.Copy();
            turned.Apply(new SwapAxes());
            return Judge.Inside(turned, box) ? (row with { Shape = turned }, "swapped longitude and latitude") : null;
        });
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
