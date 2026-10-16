using System.Collections.Immutable;
using CSharpFunctionalExtensions;

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
    /// column, since most data needs tidying throughout; no onFail, since it fails no row. The JSON
    /// reader fills this record, so it keeps null for a setting the file leaves out (ADR 0004).
    /// </summary>
    public sealed record Settings(ImmutableArray<string>? Fields = null);

    /// <summary>The kind Filters/All.cs lists, so a pipeline file may name this filter.</summary>
    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "text is put into one Unicode form, runs of spaces become one, ends are trimmed, and empty text becomes null; `fields` limits it to some columns",
        FilterKind.NothingToCheck, (settings, _) => Create(settings.Fields is { } fields ? fields : Maybe<ImmutableArray<string>>.None));

    /// <summary>
    /// Builds the filter from plain values, so a test can make one without a pipeline file.
    /// </summary>
    public static Handler<Cleaning> Create(Maybe<ImmutableArray<string>> fields = default) =>
        // It never fails a row, so OnFail.Drop never comes into play.
        Steps.Filter(Name, OnFail.Drop, Steps.EachRow(row =>
        {
            var changed = new List<string>();
            var values = row.Values;
            foreach (var (column, value) in row.Values)
            {
                if (value is not string text || (fields.TryGetValue(out var only) && !only.IsDefaultOrEmpty && !only.Contains(column))) continue;
                var clean = Judge.TidyText(text);
                if (clean.TryGetValue(out var tidy) && tidy == text) continue;
                changed.Add(column);
                // Text that tidies to nothing becomes null, the value of an empty cell (see Row).
                values = values.SetItem(column, clean.HasValue ? clean.Value : null);
            }
            // A row keeps its values in no order, so the names are sorted: the same row then
            // gives the same log line on every run.
            return changed.Count == 0
                ? Verdict.Keep(row)
                : Verdict.Fix(row with { Values = values }, $"tidied {string.Join(", ", changed.Order(StringComparer.Ordinal))}");
        }));
}
