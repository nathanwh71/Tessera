using NetTopologySuite.Geometries;

namespace Tessera.Etl.Filters;

/// <summary>
/// swapCoordinates: a common slip writes latitude where longitude belongs. A shape outside the
/// area whose swapped copy falls inside it is swapped back; anything else is left alone.
/// </summary>
public static class SwapCoordinates
{
    /// <summary>
    /// The name a <b><i>pipeline file</i></b> uses; the log and the tests name the
    /// <b><i>filter</i></b> by it too, so one constant keeps all three alike.
    /// </summary>
    public const string Name = "swapCoordinates";

    /// <summary>
    /// No settings, not even onFail: this filter only mends rows and never fails one, so a
    /// shape it cannot mend passes on for insideArea to judge.
    /// </summary>
    public sealed record Settings;

    /// <summary>
    /// The kind Filters/All.cs lists, so a pipeline file may name this filter. NeedsArea refuses
    /// the filter when there is no area, so <c>GetValueOrThrow</c> never meets none.
    /// </summary>
    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "a shape outside the area that falls inside it with longitude and latitude swapped is swapped back",
        FilterKind.NeedsArea, (_, area) => Create(area.GetValueOrThrow()));

    /// <summary>
    /// Builds the filter from plain values, so a test can make one without a pipeline file.
    /// </summary>
    public static Handler<Cleaning> Create(Area area)
    {
        var box = Judge.Box(area);
        // It never fails a row, so OnFail.Drop never comes into play.
        return Steps.Filter(Name, OnFail.Drop, Steps.EachRow(row =>
        {
            if (!row.Shape.TryGetValue(out var shape) || shape.IsEmpty || Judge.Inside(shape, box)) return new Verdict.Keep(row);
            // Apply changes a shape in place, so it works on a copy: the row read must stay as it
            // was, since rows are never changed, only replaced (ADR 0004).
            var turned = shape.Copy();
            turned.Apply(new SwapAxes());
            return Judge.Inside(turned, box) ? new Verdict.Fix(row with { Shape = turned }, "swapped longitude and latitude") : new Verdict.Keep(row);
        }));
    }

    /// <summary>Swaps each point's two numbers, so (latitude, longitude) becomes (longitude, latitude).</summary>
    private sealed class SwapAxes : IEntireCoordinateSequenceFilter
    {
        /// <summary>
        /// Never done early: every part of the shape must turn, or the shape would tear.
        /// </summary>
        public bool Done => false;

        /// <summary>
        /// True, so NetTopologySuite forgets the box it kept for the shape; otherwise the Inside
        /// test just after, and insideArea later, would read the old, unswapped box.
        /// </summary>
        public bool GeometryChanged => true;

        /// <summary>
        /// The step NetTopologySuite calls once for each run of points in the shape.
        /// </summary>
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
