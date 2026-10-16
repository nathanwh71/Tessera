using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;

namespace Tessera.Etl;

/// <summary>
/// The <b><i>pipeline file</i></b>: where the places come from, which <b><i>seed script</i></b> to
/// write, the area they must sit in, when to stop, and one entry for each table to make. A
/// publisher describes a run in this file rather than in code, so new data needs no new program
/// (US-24, US-36). An example is data/macau.pipeline.json.
/// </summary>
public sealed record Pipeline(
    string Source,
    string Output,
    ImmutableArray<TablePlan> Tables,
    Area? Area = null,
    StopWhen? StopWhen = null);

/// <summary>
/// One table: its name as <b><i>schema</i></b>.table, a schema being a named folder of tables; the
/// <b><i>layer</i></b> to read from the source, such as a feature class of a
/// <b><i>geodatabase</i></b>; an optional filter on the source's rows; whether its shapes are lines
/// or areas, which may mix single and multiple parts; and its cleaning <b><i>filters</i></b>, run
/// in the order given. Each filter is an object that names it, with its own settings beside the
/// name, such as <c>{ "filter": "insideArea", "onFail": "stop" }</c>.
/// </summary>
public sealed record TablePlan(
    string Table,
    string? Layer = null,
    string? Where = null,
    bool Lines = false,
    ImmutableArray<JsonObject> Filters = default,
    StopWhen? StopWhen = null);

/// <summary>
/// A table ready to clean: its plan, its filters' names in order, and the one chain of steps they
/// make, with the stop rules last (see Pipes.cs and Steps.cs). The steps are built while the file
/// is read, so a bad setting is found before GDAL runs. The names stay beside the chain, since the
/// log counts each filter by name.
/// </summary>
public sealed record CleaningPlan(TablePlan Table, ImmutableArray<string> Filters, Handler<Cleaning> Chain);

/// <summary>
/// A pipeline file read and checked, with each table's filters built and chained. A run starts
/// only from a Plan, so it never starts from a file with a fault in it (US-36, criterion 5).
/// </summary>
public sealed record Plan(Pipeline Pipeline, ImmutableArray<CleaningPlan> Tables);

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

/// <summary>
/// Reads and checks a pipeline file before any work starts, so a mistake in it costs a second,
/// not a half-finished run.
/// </summary>
public static class PipelineFile
{
    /// <summary>
    /// The <b><i>stop rules</i></b> when the file sets none: stop when more than 10 % of a table's
    /// rows are dropped, or when none remain. Stopping is the default, so a publisher who accepts
    /// more loss must say so in the file (US-36, criterion 3).
    /// </summary>
    public static readonly StopWhen Defaults = new(DroppedOverPercent: 10, RowsUnder: 1);

    // camelCase in the file, as JSON usually is. A key the records lack stops the read, so a
    // misspelt setting cannot pass unseen and leave a filter at its default; so does a setting a
    // record needs, such as requireFields' fields, when the file leaves it out. The JSON reader
    // knows no Maybe, so these records, which it fills, keep null for a setting the file leaves out
    // (ADR 0004).
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        PropertyNameCaseInsensitive = false,
        // Comments may stand in the file, so a publisher can note why a table has its filters.
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    // A plain lower-case name needs no quotes in SQL and reads the same in a tile's address.
    private static readonly Regex PlainName = new("^[a-z_][a-z0-9_]*$");

    // DbUp runs scripts in the order of their names, so a four-digit number leads each name.
    private static readonly Regex ScriptName = new(@"(^|/)\d{4}-[a-z0-9-]+\.sql$");

    /// <summary>
    /// Reads and checks a pipeline file's text, and builds each table's chain of filters. Every
    /// fault is named at once, so one failed run shows everything to fix.
    /// <paramref name="areaFromSettings"/> stands in when the file names no area of its own.
    /// </summary>
    public static Result<Plan, ImmutableArray<string>> Read(string json, Maybe<Area> areaFromSettings)
    {
        Maybe<Pipeline> read;
        try
        {
            // The reader gives null for the JSON text 'null'; that becomes none here.
            read = JsonSerializer.Deserialize<Pipeline>(json, Options).AsMaybe();
        }
        catch (JsonException error)
        {
            return Result.Failure<Plan, ImmutableArray<string>>([$"The pipeline file is not valid: {error.Message}"]);
        }
        if (!read.TryGetValue(out var file) || file.Tables.IsDefault)
            return Result.Failure<Plan, ImmutableArray<string>>(["The pipeline file needs source, output and tables."]);
        // The reader lets a null stand in a list, as in "tables": [null]; every check below reads each
        // table's fields, so a null is refused here, before it reaches them.
        if (file.Tables.Any(table => table is null))
            return Result.Failure<Plan, ImmutableArray<string>>(["tables: each entry must be an object that names its table."]);

        var pipeline = file with { Area = file.Area ?? areaFromSettings.GetValueOrDefault() };
        var tables = pipeline.Tables.Select(table => Build(table, pipeline)).ToImmutableArray();
        var problems = Problems(pipeline)
            .Concat(tables.Where(table => table.IsFailure).SelectMany(table => table.Error))
            .ToImmutableArray();
        return problems.IsEmpty
            ? Result.Success<Plan, ImmutableArray<string>>(new Plan(pipeline, [.. tables.Select(table => table.Value)]))
            : Result.Failure<Plan, ImmutableArray<string>>(problems);
    }

    /// <summary>
    /// Builds a table's filters from the file, in order, and joins them with <c>&gt;&gt;</c>
    /// (Pipes.cs), with the stop rules last. Each object names its filter in "filter"; the rest are
    /// its settings. The table's own stop rules win over the file's, and the file's over the
    /// defaults, so one table may allow more loss than the rest.
    /// </summary>
    private static Result<CleaningPlan, ImmutableArray<string>> Build(TablePlan table, Pipeline pipeline)
    {
        var area = pipeline.Area.AsMaybe();
        var problems = ImmutableArray.CreateBuilder<string>();
        var names = ImmutableArray.CreateBuilder<string>();
        var steps = ImmutableArray.CreateBuilder<Handler<Cleaning>>();
        foreach (var step in table.Filters.IsDefault ? [] : table.Filters)
        {
            // The indexer gives null for a missing key; that becomes none here.
            var name = step["filter"].AsMaybe()
                .Where(node => node.GetValueKind() == JsonValueKind.String)
                .Map(node => node.GetValue<string>());
            if (!name.Bind(Filters.All.Find).TryGetValue(out var kind))
            {
                problems.Add($"{table.Table}: '{name.GetValueOrDefault("")}' is no filter; the filters are {string.Join(", ", Filters.All.Kinds.Select(known => known.Name))}.");
                continue;
            }
            // A copy: a JsonObject changes in place, and taking out "filter" must leave the plan
            // as the file wrote it.
            var settings = step.DeepClone().AsObject();
            settings.Remove("filter");
            var built = kind.Build(settings, area);
            if (built.IsFailure)
            {
                problems.AddRange(built.Error.Select(problem => $"{table.Table}: {problem}"));
                continue;
            }
            names.Add(kind.Name);
            steps.Add(built.Value);
        }
        var chain = Pipes.All(steps) >> Steps.StopWhen(table.StopWhen ?? pipeline.StopWhen ?? Defaults);
        return problems.Count == 0
            ? Result.Success<CleaningPlan, ImmutableArray<string>>(new CleaningPlan(table, names.ToImmutable(), chain))
            : Result.Failure<CleaningPlan, ImmutableArray<string>>(problems.ToImmutable());
    }

    /// <summary>
    /// The faults in the file as a whole, beyond its filters. Each rule guards a later step: GDAL
    /// needs a source, DbUp runs scripts by numbered name (ADR 0009), the area box must not be
    /// empty, and each table must have one plain name of its own, so that SQL and a tile's address
    /// read it unquoted.
    /// </summary>
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
            // The reader refuses a null table name, so Table always holds text.
            var parts = table.Table.Split('.');
            if (parts.Length != 2 || !parts.All(PlainName.IsMatch))
                yield return $"{table.Table}: write the table as schema.table, in lower-case letters, digits and _.";
        }
    }
}
