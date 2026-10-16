using System.Collections.Immutable;

namespace Tessera.Etl.Filters;

/// <summary>
/// requireFields: a row missing any of the fields, or holding only spaces there, fails. A place
/// with no name has no label on the map and cannot be told from its neighbours.
/// </summary>
public static class RequireFields
{
    /// <summary>
    /// The name a <b><i>pipeline file</i></b> uses; the log and the tests name the
    /// <b><i>filter</i></b> by it too, so one constant keeps all three alike.
    /// </summary>
    public const string Name = "requireFields";

    /// <summary>
    /// The settings a pipeline file may give it. Fields has no default, since only the publisher
    /// knows which columns a place cannot lack; a failing row is dropped unless the file says stop.
    /// </summary>
    public sealed record Settings(ImmutableArray<string> Fields, OnFail OnFail = OnFail.Drop);

    /// <summary>The kind Filters/All.cs lists, so a pipeline file may name this filter.</summary>
    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "a row missing any of `fields`, or holding only spaces there, fails",
        (settings, _) => settings.Fields.IsDefaultOrEmpty ? ["needs fields, such as [\"name\"]."] : [],
        (settings, _) => Create(settings.Fields, settings.OnFail));

    /// <summary>
    /// Builds the filter from plain values, so a test can make one without a pipeline file.
    /// </summary>
    public static Handler<Cleaning> Create(ImmutableArray<string> fields, OnFail onFail = OnFail.Drop) =>
        Steps.Filter(Name, onFail, Steps.EachRow(row =>
            fields.FirstOrDefault(field => Judge.IsBlank(row.Value(field))) is { } missing
                ? Verdict.Fail(row, $"{missing} is missing")
                : Verdict.Keep(row)));
}
