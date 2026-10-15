using System.Collections.Immutable;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Operation.Valid;

namespace Tessera.Etl.Filters;

/// <summary>
/// repairShapes: NetTopologySuite's GeometryFixer rebuilds an invalid shape into a valid one, such
/// as an area whose edge crosses itself into two areas. A shape that comes out empty fails.
/// </summary>
public static class RepairShapes
{
    public const string Name = "repairShapes";

    public sealed record Settings(OnFail OnFail = OnFail.Drop);

    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "an invalid shape, such as an area whose edge crosses itself, is repaired; one that cannot be repaired fails",
        FilterKind.NothingToCheck, (settings, _) => Create(settings.OnFail));

    public static Filter Create(OnFail onFail = OnFail.Drop) => rows =>
    {
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
