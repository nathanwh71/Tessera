using System.Collections.Immutable;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Operation.Valid;

namespace Tessera.Etl.Filters;

/// <summary>
/// repairShapes: NetTopologySuite's GeometryFixer rebuilds an invalid shape into a valid one, such
/// as an area whose edge crosses itself into two areas. PostGIS may refuse or misdraw an invalid
/// shape when it cuts a <b><i>tile</i></b>, and its inside and outside are unclear. A shape that
/// comes out empty fails, since nothing is left to draw.
/// </summary>
public static class RepairShapes
{
    /// <summary>
    /// The name a <b><i>pipeline file</i></b> uses; the log and the tests name the
    /// <b><i>filter</i></b> by it too, so one constant keeps all three alike.
    /// </summary>
    public const string Name = "repairShapes";

    /// <summary>
    /// The settings a pipeline file may give it: only what to do with a shape beyond repair,
    /// which is dropped unless the file says stop.
    /// </summary>
    public sealed record Settings(OnFail OnFail = OnFail.Drop);

    /// <summary>The kind Filters/All.cs lists, so a pipeline file may name this filter.</summary>
    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "an invalid shape, such as an area whose edge crosses itself, is repaired; one that cannot be repaired fails",
        FilterKind.NothingToCheck, (settings, _) => Create(settings.OnFail));

    /// <summary>
    /// Builds the filter from plain values, so a test can make one without a pipeline file.
    /// </summary>
    public static Filter Create(OnFail onFail = OnFail.Drop) => rows =>
    {
        // Two passes, each with its own helper: the first repairs, so each repair logs as fixed;
        // the second fails the rows beyond repair, so they obey onFail, drop or stop.
        var beyondRepair = new HashSet<int>();
        var repaired = Judge.Changes(rows, Name, row =>
        {
            if (row.Shape is null || row.Shape.IsEmpty || row.Shape.IsValid) return null;
            var why = new IsValidOp(row.Shape).ValidationError?.Message ?? "invalid";
            var fixedShape = GeometryFixer.Fix(row.Shape);
            if (!fixedShape.IsEmpty) return (row with { Shape = fixedShape }, $"repaired a shape that was invalid ({why})");
            beyondRepair.Add(row.Number);
            return null;
        });
        var judged = Judge.Rows(repaired.Rows, Name, onFail, row => beyondRepair.Contains(row.Number) ? "its shape is invalid and cannot be repaired" : null);
        return judged with { Log = repaired.Log.AddRange(judged.Log) };
    };
}
