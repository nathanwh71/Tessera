namespace Tessera.Etl.Filters;

/// <summary>
/// dropEmptyShapes: a row with no shape, or an empty one, fails. A place with no shape has no
/// position, so no <b><i>tile</i></b> can draw it, and the other shape <b><i>filters</i></b> have
/// nothing to test.
/// </summary>
public static class DropEmptyShapes
{
    /// <summary>
    /// The name a <b><i>pipeline file</i></b> uses; the log and the tests name the filter by it
    /// too, so one constant keeps all three alike.
    /// </summary>
    public const string Name = "dropEmptyShapes";

    /// <summary>
    /// The settings a pipeline file may give it: only what to do with a failing row, which is
    /// dropped unless the file says stop.
    /// </summary>
    public sealed record Settings(OnFail OnFail = OnFail.Drop);

    /// <summary>The kind Filters/All.cs lists, so a pipeline file may name this filter.</summary>
    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "a row with no shape, or an empty one, fails",
        FilterKind.NothingToCheck, (settings, _) => Create(settings.OnFail));

    /// <summary>
    /// Builds the filter from plain values, so a test can make one without a pipeline file.
    /// </summary>
    public static Handler<Cleaning> Create(OnFail onFail = OnFail.Drop) =>
        Steps.Filter(Name, onFail, Steps.EachRow(row =>
            !row.Shape.TryGetValue(out var shape) || shape.IsEmpty ? Verdict.Fail(row, "it has no shape") : Verdict.Keep(row)));
}
