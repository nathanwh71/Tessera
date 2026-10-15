using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Tessera.Etl;

/// <summary>What happened to one table: the rows read, each filter's outcome in turn, and why it stopped, if it did.</summary>
public sealed record TableReport(
    TablePlan Table,
    int Read,
    ImmutableArray<(string Filter, Outcome Outcome)> Steps,
    ImmutableArray<Row> Kept,
    string? Stop)
{
    public int Dropped => Read - Kept.Length;
}

/// <summary>
/// The transform step for one table, and the log of a whole run. Both are pure: the program reads
/// the files and runs GDAL; these only decide (ADR 0004).
/// </summary>
public static class Cleaning
{
    /// <summary>
    /// Runs the table's filters in the order the pipeline file gives them, each on the rows the one
    /// before it kept. The first filter that stops ends the table. Then the table's stop rules,
    /// or the pipeline's, or the defaults, judge what is left.
    /// </summary>
    public static TableReport Clean(TablePlan table, ImmutableArray<Row> rows, Area? area, StopWhen? pipelineRules)
    {
        var steps = ImmutableArray.CreateBuilder<(string, Outcome)>();
        var current = rows;
        foreach (var step in table.Filters.IsDefault ? [] : table.Filters)
        {
            var outcome = Filters.For(step, area)(current);
            steps.Add((step.Filter, outcome));
            if (outcome.Stop is { } stop) return new TableReport(table, rows.Length, steps.ToImmutable(), outcome.Rows, stop);
            current = outcome.Rows;
        }

        var rules = table.StopWhen ?? pipelineRules ?? PipelineFile.Defaults;
        var dropped = rows.Length - current.Length;
        var percent = rows.Length == 0 ? 0 : 100.0 * dropped / rows.Length;
        string? broken =
            rules.RowsUnder is { } least && current.Length < least
                ? $"only {current.Length} rows remain, fewer than the {least} required"
            : rules.DroppedOverPercent is { } most && percent > most
                ? $"{dropped} of {rows.Length} rows were dropped ({percent.ToString("0.#", CultureInfo.InvariantCulture)} %), more than the {most.ToString(CultureInfo.InvariantCulture)} % allowed"
            : null;
        return new TableReport(table, rows.Length, steps.ToImmutable(), current, broken is null ? null : $"{table.Table}: {broken}.");
    }

    /// <summary>
    /// The log of a run, as plain text: the source, then for each table the rows read, a count for
    /// each filter, and one line for each row a filter fixed, dropped or stopped at; then the result.
    /// </summary>
    public static string Log(string pipelineFile, Pipeline pipeline, ImmutableArray<TableReport> reports, string result, DateTimeOffset when)
    {
        var text = new StringBuilder();
        text.AppendLine($"Tessera ETL log, {when.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)}");
        text.AppendLine($"Pipeline: {pipelineFile}");
        text.AppendLine($"Source:   {pipeline.Source}");
        text.AppendLine($"Output:   {pipeline.Output}");
        foreach (var report in reports)
        {
            text.AppendLine();
            text.AppendLine($"== {report.Table.Table}{(report.Table.Layer is { } layer ? $", from {layer}" : "")}{(report.Table.Where is { } where ? $", where {where}" : "")}");
            text.AppendLine($"Read {report.Read} rows.");
            foreach (var (filter, outcome) in report.Steps)
                text.AppendLine($"  {filter}: kept {outcome.Rows.Length}, fixed {outcome.Fixed}, dropped {outcome.Dropped}{(outcome.Stop is null ? "" : ", stopped")}");
            text.AppendLine($"Kept {report.Kept.Length} of {report.Read}; dropped {report.Dropped}.");
            foreach (var line in report.Steps.SelectMany(step => step.Outcome.Log))
                text.AppendLine($"  {line}");
        }
        text.AppendLine();
        text.AppendLine($"Result: {result}");
        return text.ToString();
    }
}
