namespace Tessera.Etl.Filters;

/// <summary>insideArea: a shape that does not touch the area's box fails.</summary>
public static class InsideArea
{
    public const string Name = "insideArea";

    public sealed record Settings(OnFail OnFail = OnFail.Drop);

    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "a shape that does not touch the area's box fails",
        FilterKind.NeedsArea, (settings, area) => Create(area!, settings.OnFail));

    public static Filter Create(Area area, OnFail onFail = OnFail.Drop) => rows =>
    {
        var box = Judge.Box(area);
        return Judge.Rows(rows, Name, onFail, row => Judge.Inside(row.Shape, box) ? null : "its shape lies outside the area");
    };
}
