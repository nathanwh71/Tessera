using System.Collections.Immutable;

namespace Tessera.Etl.Filters;

/// <summary>requireFields: a row missing any of the fields, or holding only spaces there, fails.</summary>
public static class RequireFields
{
    public const string Name = "requireFields";

    public sealed record Settings(ImmutableArray<string> Fields, OnFail OnFail = OnFail.Drop);

    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "a row missing any of `fields`, or holding only spaces there, fails",
        (settings, _) => settings.Fields.IsDefaultOrEmpty ? ["needs fields, such as [\"name\"]."] : [],
        (settings, _) => Create(settings.Fields, settings.OnFail));

    public static Filter Create(ImmutableArray<string> fields, OnFail onFail = OnFail.Drop) => rows =>
        Judge.Rows(rows, Name, onFail, row =>
            fields.FirstOrDefault(field => Judge.IsBlank(row.Value(field))) is { } missing ? $"{missing} is missing" : null);
}
