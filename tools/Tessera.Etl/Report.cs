using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using CSharpFunctionalExtensions;

namespace Tessera.Etl;

/// <summary>
/// The log of a whole run, as plain text (US-36, criterion 4). A dropped row leaves no trace in the
/// script, so this log is the publisher's only account of what was left out and why. It is a
/// <b><i>pure function</i></b> of the run's results: the program writes the file.
/// </summary>
public static class Report
{
    /// <summary>
    /// The source, then for each table the rows read, a count for each <b><i>filter</i></b>, and
    /// one line for each row a filter fixed, dropped or stopped at; then the result. Each table
    /// comes as its plan, for the filters' names in order, and the context its chain ended with.
    /// </summary>
    public static string Log(string pipelineFile, Pipeline pipeline, ImmutableArray<(CleaningPlan Plan, Cleaning Done)> tables, string result, DateTimeOffset when)
    {
        var text = new StringBuilder();
        text.AppendLine($"Tessera ETL log, {when.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)}");
        text.AppendLine($"Pipeline: {pipelineFile}");
        text.AppendLine($"Source:   {pipeline.Source}");
        text.AppendLine($"Output:   {pipeline.Output}");
        foreach (var (plan, done) in tables)
        {
            var table = done.Table;
            text.AppendLine();
            text.AppendLine($"== {table.Table}{(table.Layer is { } layer ? $", from {layer}" : "")}{(table.Where is { } where ? $", where {where}" : "")}");
            text.AppendLine($"Read {done.Read} rows.");
            // Each filter's counts come from the joined log, whose lines name their filter. The rows
            // kept after a filter are those read less every row dropped up to and including it.
            // Distinct, since lines carry only a filter's name: a filter listed twice gets one
            // line, with the counts of both its runs.
            var keptSoFar = done.Read;
            foreach (var filter in plan.Filters.Distinct())
            {
                var lines = done.Log.Where(line => line.Filter == filter).ToList();
                var dropped = lines.Count(line => line.Act == Act.Dropped);
                keptSoFar -= dropped;
                var stopped = lines.Any(line => line.Act == Act.Stopped);
                text.AppendLine($"  {filter}: {(stopped ? "stopped" : $"kept {keptSoFar}")}, fixed {lines.Count(line => line.Act == Act.Fixed)}, dropped {dropped}");
                // The filters after a stop never ran, so they get no line of counts.
                if (stopped) break;
            }
            // A filter that stops hands back its rows unjudged, so none counts as kept. A table
            // stopped by its stop rules did judge every row, so its counts still stand.
            text.AppendLine(done.Log.Any(line => line.Act == Act.Stopped)
                ? "Stopped; nothing kept."
                : $"Kept {done.Rows.Length} of {done.Read}; dropped {done.Dropped}.");
            foreach (var line in done.Log)
                text.AppendLine($"  {line}");
        }
        text.AppendLine();
        text.AppendLine($"Result: {result}");
        return text.ToString();
    }
}
