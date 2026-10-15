namespace Tessera.Etl.Filters;

/// <summary>
/// dropDuplicateIds: the first row with a value keeps it; each later row with the same value fails.
/// A row with no value passes, since requireFields is the filter that asks for one.
/// </summary>
public static class DropDuplicateIds
{
    public const string Name = "dropDuplicateIds";

    public sealed record Settings(string Field, OnFail OnFail = OnFail.Drop);

    public static readonly FilterKind Kind = FilterKind.Of<Settings>(Name,
        "a row that repeats an earlier row's `field` fails",
        (settings, _) => string.IsNullOrWhiteSpace(settings.Field) ? ["needs a field, such as \"id\"."] : [],
        (settings, _) => Create(settings.Field, settings.OnFail));

    public static Filter Create(string field, OnFail onFail = OnFail.Drop) => rows =>
    {
        var first = new Dictionary<string, int>(StringComparer.Ordinal);
        return Judge.Rows(rows, Name, onFail, row =>
        {
            if (Judge.Text(row.Value(field)) is not { } id) return null;
            if (first.TryGetValue(id, out var earlier)) return $"it repeats {field} '{id}' of row {earlier}";
            first[id] = row.Number;
            return null;
        });
    };
}
