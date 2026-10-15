using System.Collections.Immutable;

namespace Tessera.Etl.Filters;

/// <summary>
/// tidyText: text is put into one Unicode form, runs of spaces become one, ends are trimmed, and
/// empty text becomes null. Two names that look the same then are the same, so the later
/// <b><i>filters</i></b> that compare names, and the map's labels, see one name, not two. With
/// fields, it tidies those columns alone.
/// </summary>
public static class TidyText
{
    /// <summary>
    /// The name a <b><i>pipeline file</i></b> uses; the log and the tests name the filter by it
    /// too, so one constant keeps all three alike.
    /// </summary>
    public const string Name = "tidyText";

    /// <summary>
    /// The settings a pipeline file may give it. Fields is null by default, meaning every text
    /// column, since most data needs tidying throughout; no onFail, since it fails no row.
    /// </summary>
    public sealed record Settings(ImmutableArray<string>? Fields = null);

    /// <summary>The kind Filters/All.cs lists, so a pipeline file may name this filter.</summary>
    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "text is put into one Unicode form, runs of spaces become one, ends are trimmed, and empty text becomes null; `fields` limits it to some columns",
        FilterKind.NothingToCheck, (settings, _) => Create(settings.Fields));

    /// <summary>
    /// Builds the filter from plain values, so a test can make one without a pipeline file.
    /// </summary>
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
            // A row keeps its values in no order, so the names are sorted: the same row then
            // gives the same log line on every run.
            return changed.Count == 0 ? null : (row with { Values = values }, $"tidied {string.Join(", ", changed.Order(StringComparer.Ordinal))}");
        });
}
