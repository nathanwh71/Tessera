namespace Tessera.Etl.Filters;

/// <summary>dropEmptyShapes: a row with no shape, or an empty one, fails.</summary>
public static class DropEmptyShapes
{
    public const string Name = "dropEmptyShapes";

    /// <summary>The settings a pipeline file may give it.</summary>
    public sealed record Settings(OnFail OnFail = OnFail.Drop);

    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "a row with no shape, or an empty one, fails",
        FilterKind.NothingToCheck, (settings, _) => Create(settings.OnFail));

    public static Filter Create(OnFail onFail = OnFail.Drop) => rows =>
        Judge.Rows(rows, Name, onFail, row => row.Shape is null or { IsEmpty: true } ? "it has no shape" : null);
}
