using System.Collections.Immutable;

namespace Tessera.Etl.Filters;

/// <summary>
/// Every filter a pipeline file may name. A new filter is one new file beside these, declaring its
/// Kind, and one more line here.
/// </summary>
public static class All
{
    public static readonly ImmutableArray<FilterKind> Kinds =
    [
        DropEmptyShapes.Kind,
        RepairShapes.Kind,
        SwapCoordinates.Kind,
        InsideArea.Kind,
        TidyText.Kind,
        RequireFields.Kind,
        DropDuplicateIds.Kind,
        DropNearDuplicates.Kind,
    ];

    public static FilterKind? Find(string name) => Kinds.FirstOrDefault(kind => kind.Name == name);
}
