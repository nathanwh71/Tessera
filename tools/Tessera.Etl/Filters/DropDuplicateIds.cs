namespace Tessera.Etl.Filters;

/// <summary>
/// dropDuplicateIds: the first row with a value keeps it; each later row with the same value fails.
/// An id must name one place alone, or two places would claim one key. A row with no value passes,
/// since requireFields is the <b><i>filter</i></b> that asks for one.
/// </summary>
public static class DropDuplicateIds
{
    /// <summary>
    /// The name a <b><i>pipeline file</i></b> uses; the log and the tests name the filter by it
    /// too, so one constant keeps all three alike.
    /// </summary>
    public const string Name = "dropDuplicateIds";

    /// <summary>
    /// The settings a pipeline file may give it. Field has no default, since only the publisher
    /// knows which column holds the id; a failing row is dropped unless the file says stop.
    /// </summary>
    public sealed record Settings(string Field, OnFail OnFail = OnFail.Drop);

    /// <summary>The kind Filters/All.cs lists, so a pipeline file may name this filter.</summary>
    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "a row that repeats an earlier row's `field` fails",
        (settings, _) => string.IsNullOrWhiteSpace(settings.Field) ? ["needs a field, such as \"id\"."] : [],
        (settings, _) => Create(settings.Field, settings.OnFail));

    /// <summary>
    /// Builds the filter from plain values, so a test can make one without a pipeline file. Each
    /// run starts a fresh list of ids, since one table's ids say nothing about another's.
    /// </summary>
    public static Handler<Cleaning> Create(string field, OnFail onFail = OnFail.Drop) =>
        Steps.Filter(Name, onFail, rows =>
        {
            var first = new Dictionary<string, int>(StringComparer.Ordinal);
            return [.. rows.Select(row =>
            {
                if (Judge.Text(row.Value(field)) is not { } id) return Verdict.Keep(row);
                if (first.TryGetValue(id, out var earlier)) return Verdict.Fail(row, $"it repeats {field} '{id}' of row {earlier}");
                first[id] = row.Number;
                return Verdict.Keep(row);
            })];
        });
}
