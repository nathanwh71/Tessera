namespace Tessera.Etl.Filters;

/// <summary>
/// insideArea: a shape that does not touch the area's box fails. The map shows one area alone, so
/// a place outside it is a mistake in the data, such as a wrong sign on a coordinate, and no
/// <b><i>tile</i></b> of the map would ever show it.
/// </summary>
public static class InsideArea
{
    /// <summary>
    /// The name a <b><i>pipeline file</i></b> uses; the log and the tests name the
    /// <b><i>filter</i></b> by it too, so one constant keeps all three alike.
    /// </summary>
    public const string Name = "insideArea";

    /// <summary>
    /// The settings a pipeline file may give it: only what to do with a failing row, which is
    /// dropped unless the file says stop. The box comes from the area, not from these settings.
    /// </summary>
    public sealed record Settings(OnFail OnFail = OnFail.Drop);

    /// <summary>
    /// The kind Filters/All.cs lists, so a pipeline file may name this filter. NeedsArea refuses
    /// the filter when there is no area, so <c>GetValueOrThrow</c> never meets none.
    /// </summary>
    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "a shape that does not touch the area's box fails",
        FilterKind.NeedsArea, (settings, area) => Create(area.GetValueOrThrow(), settings.OnFail));

    /// <summary>
    /// Builds the filter from plain values, so a test can make one without a pipeline file.
    /// </summary>
    public static Handler<Cleaning> Create(Area area, OnFail onFail = OnFail.Drop)
    {
        var box = Judge.Box(area);
        return Steps.Filter(Name, onFail, Steps.EachRow(row =>
            Judge.Inside(row.Shape, box) ? new Verdict.Keep(row) : new Verdict.Fail(row, "its shape lies outside the area")));
    }
}
