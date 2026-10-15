using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;

namespace Tessera.Etl;

/// <summary>
/// The pipeline file: where the places come from, which script to write, the area they must sit in,
/// when to stop, and one entry for each table to make. An example is data/macau.pipeline.json.
/// </summary>
public sealed record Pipeline(
    string Source,
    string Output,
    Area? Area,
    StopWhen? StopWhen,
    ImmutableArray<TablePlan> Tables);

/// <summary>
/// One table: its name as schema.table; the layer to read from the source, such as a feature class
/// of a geodatabase; an optional filter on the source's rows; whether its shapes are lines or areas,
/// which may mix single and multiple parts; and its cleaning filters, run in the order given.
/// </summary>
public sealed record TablePlan(
    string Table,
    string? Layer,
    string? Where,
    bool Lines,
    ImmutableArray<FilterStep> Filters,
    StopWhen? StopWhen);

/// <summary>
/// One cleaning filter and its settings. <c>OnFail</c> says what to do with a row the filter
/// cannot pass: drop it, or stop the whole run. Only the settings a filter reads need a value.
/// </summary>
public sealed record FilterStep(
    string Filter,
    OnFail OnFail = OnFail.Drop,
    ImmutableArray<string>? Fields = null,
    string? Field = null,
    double? Metres = null);

/// <summary>What a filter does with a row it cannot pass: drop it, or stop the whole run.</summary>
public enum OnFail
{
    Drop,
    Stop,
}

/// <summary>
/// When a table stops the run, even though every filter passed: when more than
/// <c>DroppedOverPercent</c> of its rows were dropped, or fewer than <c>RowsUnder</c> remain.
/// </summary>
public sealed record StopWhen(double? DroppedOverPercent = null, int? RowsUnder = null);

/// <summary>The box every place should sit in, in degrees, as tessera.json's Area holds it.</summary>
public sealed record Area(double MinLongitude, double MinLatitude, double MaxLongitude, double MaxLatitude);

public static class PipelineFile
{
    /// <summary>Stop when more than 10 % of a table's rows are dropped, or when none remain.</summary>
    public static readonly StopWhen Defaults = new(DroppedOverPercent: 10, RowsUnder: 1);

    // camelCase in the file, as JSON usually is. A key the records lack stops the read, so a
    // misspelt setting cannot pass unseen and leave a filter at its default.
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    // A plain lower-case name needs no quotes in SQL and reads the same in a tile's address.
    private static readonly Regex PlainName = new("^[a-z_][a-z0-9_]*$");

    // DbUp runs scripts in the order of their names, so a four-digit number leads each name.
    private static readonly Regex ScriptName = new(@"(^|/)\d{4}-[a-z0-9-]+\.sql$");

    /// <summary>
    /// Reads and checks a pipeline file's text. Every fault is named at once, so one failed run
    /// shows everything to fix. <paramref name="areaFromSettings"/> stands in when the file names
    /// no area of its own.
    /// </summary>
    public static Result<Pipeline, ImmutableArray<string>> Read(string json, Area? areaFromSettings)
    {
        Pipeline? read;
        try
        {
            read = JsonSerializer.Deserialize<Pipeline>(json, Options);
        }
        catch (JsonException error)
        {
            return Result.Failure<Pipeline, ImmutableArray<string>>([$"The pipeline file is not valid: {error.Message}"]);
        }
        if (read is null || read.Tables.IsDefault)
            return Result.Failure<Pipeline, ImmutableArray<string>>(["The pipeline file needs source, output and tables."]);

        var pipeline = read with { Area = read.Area ?? areaFromSettings };
        var problems = Problems(pipeline).ToImmutableArray();
        return problems.IsEmpty
            ? Result.Success<Pipeline, ImmutableArray<string>>(pipeline)
            : Result.Failure<Pipeline, ImmutableArray<string>>(problems);
    }

    private static IEnumerable<string> Problems(Pipeline pipeline)
    {
        if (string.IsNullOrWhiteSpace(pipeline.Source))
            yield return "source: name the file to read, such as data/macau.gdb.";
        if (string.IsNullOrWhiteSpace(pipeline.Output) || !ScriptName.IsMatch(pipeline.Output))
            yield return $"output: '{pipeline.Output}' must be named NNNN-words.sql, such as db/seed/0002-parks.sql, so DbUp runs it in order.";
        if (pipeline.Tables.IsEmpty)
            yield return "tables: name at least one table.";
        if (pipeline.Area is { } area && (area.MinLongitude >= area.MaxLongitude || area.MinLatitude >= area.MaxLatitude))
            yield return "area: the box must run west to east and south to north.";

        foreach (var duplicate in pipeline.Tables.GroupBy(table => table.Table).Where(group => group.Count() > 1))
            yield return $"{duplicate.Key}: named more than once.";

        foreach (var table in pipeline.Tables)
        {
            var parts = (table.Table ?? "").Split('.');
            if (parts.Length != 2 || !parts.All(PlainName.IsMatch))
                yield return $"{table.Table}: write the table as schema.table, in lower-case letters, digits and _.";
            foreach (var problem in (table.Filters.IsDefault ? [] : table.Filters).SelectMany(step => Filters.Problems(step, pipeline.Area)))
                yield return $"{table.Table}: {problem}";
        }
    }
}
