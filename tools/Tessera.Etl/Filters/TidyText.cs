using System.Collections.Immutable;

namespace Tessera.Etl.Filters;

/// <summary>
/// tidyText: text is put into one Unicode form, runs of spaces become one, ends are trimmed, and
/// empty text becomes null. With fields, it tidies those columns alone.
/// </summary>
public static class TidyText
{
    public const string Name = "tidyText";

    public sealed record Settings(ImmutableArray<string>? Fields = null);

    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "text is put into one Unicode form, runs of spaces become one, ends are trimmed, and empty text becomes null; `fields` limits it to some columns",
        FilterKind.NothingToCheck, (settings, _) => Create(settings.Fields));

    public static Filter Create(ImmutableArray<string>? fields = null) => rows =>
        Judge.Changes(rows, Name, row =>
        {
            var changed = new List<string>();
            var values = row.Values;
            foreach (var (column, value) in row.Values)
            {
                if (value is not string text || (fields is { IsDefaultOrEmpty: false } only && !only.Contains(column))) continue;
                var clean = Judge.TidyText(text);
                if (clean == text) continue;
                changed.Add(column);
                values = values.SetItem(column, clean);
            }
            return changed.Count == 0 ? null : (row with { Values = values }, $"tidied {string.Join(", ", changed.Order(StringComparer.Ordinal))}");
        });
}
