using System.Collections.Immutable;
using CSharpFunctionalExtensions;

namespace Tessera.Etl.Filters;

/// <summary>
/// Every <b><i>filter</i></b> a <b><i>pipeline file</i></b> may name. A new filter is one new file
/// beside these, declaring its Kind, and one more line here.
/// </summary>
public static class All
{
    /// <summary>
    /// The filters in the order US-36 lists them, which is also the order an error message lists
    /// them in when a pipeline file names one that does not exist.
    /// </summary>
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

    /// <summary>
    /// The filter a pipeline file names, or none. The match is exact, case included, as JSON keys
    /// are, so 'insidearea' is refused rather than quietly read as 'insideArea'.
    /// </summary>
    public static Maybe<FilterKind> Find(string name) => Kinds.TryFirst(kind => kind.Name == name);
}
