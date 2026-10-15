using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Tessera.Etl;

/// <summary>
/// What happened to one table: its filters' names in order, the rows read, the chain's outcome,
/// and why the table stops the run, if it does.
/// </summary>
public sealed record TableReport(TablePlan Table, ImmutableArray<string> Filters, int Read, Outcome Outcome, string? Stop)
{
    public ImmutableArray<Row> Kept => Outcome.Rows;

    public int Dropped => Read - Kept.Length;
}

/// <summary>
/// The transform step for one table, and the log of a whole run. Both are pure: the program reads
/// the files and runs GDAL; these only decide (ADR 0004).
/// </summary>
public static class Cleaning
{
    /// <summary>
    /// Runs the table's chain of filters (see <see cref="Chain"/>), which ends at the first filter
    /// that stops. Then the table's stop rules, or the pipeline's, or the defaults, judge what is left.
    /// </summary>
    public static TableReport Clean(CleaningPlan plan, ImmutableArray<Row> rows, StopWhen? pipelineRules)
    {
        var outcome = plan.Chain(rows);
        if (outcome.Stop is { } stop) return new TableReport(plan.Table, plan.Filters, rows.Length, outcome, stop);

        var rules = plan.Table.StopWhen ?? pipelineRules ?? PipelineFile.Defaults;
        var dropped = rows.Length - outcome.Rows.Length;
        var percent = rows.Length == 0 ? 0 : 100.0 * dropped / rows.Length;
        string? broken =
            rules.RowsUnder is { } least && outcome.Rows.Length < least
                ? $"only {outcome.Rows.Length} rows remain, fewer than the {least} required"
            : rules.DroppedOverPercent is { } most && percent > most
                ? $"{dropped} of {rows.Length} rows were dropped ({percent.ToString("0.#", CultureInfo.InvariantCulture)} %), more than the {most.ToString(CultureInfo.InvariantCulture)} % allowed"
            : null;
        return new TableReport(plan.Table, plan.Filters, rows.Length, outcome, broken is null ? null : $"{plan.Table.Table}: {broken}.");
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
            // Each filter's counts come from the joined log, whose lines name their filter. The rows
            // kept after a filter are those read less every row dropped up to and including it.
            var keptSoFar = report.Read;
            foreach (var filter in report.Filters.Distinct())
            {
                var lines = report.Outcome.Log.Where(line => line.Filter == filter).ToList();
                var dropped = lines.Count(line => line.Act == Act.Dropped);
                keptSoFar -= dropped;
                var stopped = lines.Any(line => line.Act == Act.Stopped);
                text.AppendLine($"  {filter}: {(stopped ? "stopped" : $"kept {keptSoFar}")}, fixed {lines.Count(line => line.Act == Act.Fixed)}, dropped {dropped}");
                if (stopped) break;
            }
            text.AppendLine(report.Outcome.Stop is null ? $"Kept {report.Kept.Length} of {report.Read}; dropped {report.Dropped}." : "Stopped; nothing kept.");
            foreach (var line in report.Outcome.Log)
                text.AppendLine($"  {line}");
        }
        text.AppendLine();
        text.AppendLine($"Result: {result}");
        return text.ToString();
    }
}
