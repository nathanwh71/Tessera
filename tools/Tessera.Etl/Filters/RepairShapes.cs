using System.Collections.Immutable;
using CSharpFunctionalExtensions;
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
    public static Handler<Cleaning> Create(OnFail onFail = OnFail.Drop) =>
        Steps.Filter(Name, onFail, Steps.EachRow(row =>
        {
            if (!row.Shape.TryGetValue(out var shape) || shape.IsEmpty || shape.IsValid) return new Verdict.Keep(row);
            // NetTopologySuite gives null when it names no error; that becomes none here.
            var why = new IsValidOp(shape).ValidationError.AsMaybe().Map(error => error.Message).GetValueOrDefault("invalid");
            var fixedShape = GeometryFixer.Fix(shape);
            // A repair is a fix; a shape that comes out empty fails, so it obeys onFail, drop or stop.
            return fixedShape.IsEmpty
                ? new Verdict.Fail(row, "its shape is invalid and cannot be repaired")
                : new Verdict.Fix(row with { Shape = fixedShape }, $"repaired a shape that was invalid ({why})");
        }));
}
