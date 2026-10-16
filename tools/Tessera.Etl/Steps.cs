using System.Collections.Immutable;
using System.Globalization;
using CSharpFunctionalExtensions;

namespace Tessera.Etl;

/// <summary>
/// The two kinds of step a table's chain is made of: <b><i>filters</i></b>, built from a judge by
/// <see cref="Filter"/>, and the <b><i>stop rules</i></b>, which <see cref="StopWhen"/> puts last.
/// Each is a <b><i>pure function</i></b>: it reads only the context and its settings, so the tests
/// check every rule with no files (ADR 0004).
/// </summary>
public static class Steps
{
    /// <summary>
    /// Turns a judge, a function that gives one <see cref="Verdict"/> for each row in order, into a
    /// step. Kept and mended rows pass on to the next step; a failed row is dropped and logged. With
    /// <see cref="OnFail.Stop"/>, the first failed row ends the run instead: the step returns the
    /// context with the reason, and never calls the steps after it (US-36, criteria 1 and 2).
    /// </summary>
    public static Handler<Cleaning> Filter(string name, OnFail onFail, Func<ImmutableArray<Row>, ImmutableArray<Verdict>> judge) =>
        next => cleaning =>
        {
            var kept = ImmutableArray.CreateBuilder<Row>();
            var log = ImmutableArray.CreateBuilder<LogLine>();
            foreach (var verdict in judge(cleaning.Rows))
            {
                var number = verdict.Row.Number;
                switch (verdict.Act)
                {
                    case Act.Dropped when onFail == OnFail.Stop:
                        // A stop hands back the rows it was given, unjudged: the run ends and writes
                        // no script, so nothing uses them as kept rows, and the log says 'nothing kept'.
                        log.Add(new LogLine(number, name, Act.Stopped, verdict.Detail));
                        return cleaning with
                        {
                            Log = cleaning.Log.AddRange(log),
                            Stop = $"{name} stopped the run at row {number}: {verdict.Detail}.",
                        };
                    case Act.Dropped:
                        log.Add(new LogLine(number, name, Act.Dropped, verdict.Detail));
                        break;
                    case Act.Fixed:
                        log.Add(new LogLine(number, name, Act.Fixed, verdict.Detail));
                        kept.Add(verdict.Row);
                        break;
                    default:
                        kept.Add(verdict.Row);
                        break;
                }
            }
            return next(cleaning with { Rows = kept.ToImmutable(), Log = cleaning.Log.AddRange(log) });
        };

    /// <summary>
    /// A judge of one row at a time, for the filters that need no other row to decide; only the
    /// two duplicate filters must see the rows before each one.
    /// </summary>
    public static Func<ImmutableArray<Row>, ImmutableArray<Verdict>> EachRow(Func<Row, Verdict> judge) =>
        rows => [.. rows.Select(judge)];

    /// <summary>
    /// The stop rules, the last step of every table's chain: the run stops when fewer than
    /// <c>RowsUnder</c> rows remain, or when more than <c>DroppedOverPercent</c> of the rows read
    /// were dropped (US-36, criterion 3). A chain that a filter stopped never reaches this step.
    /// </summary>
    public static Handler<Cleaning> StopWhen(StopWhen rules) => next => cleaning =>
    {
        var percent = cleaning.Read == 0 ? 0 : 100.0 * cleaning.Dropped / cleaning.Read;
        var broken =
            rules.RowsUnder.TryGetValue(out var least) && cleaning.Rows.Length < least
                ? Maybe.From($"only {cleaning.Rows.Length} rows remain, fewer than the {least} required")
            : rules.DroppedOverPercent.TryGetValue(out var most) && percent > most
                ? Maybe.From($"{cleaning.Dropped} of {cleaning.Read} rows were dropped ({percent.ToString("0.#", CultureInfo.InvariantCulture)} %), more than the {most.ToString(CultureInfo.InvariantCulture)} % allowed")
            : Maybe<string>.None;
        return broken.Match(
            reason => cleaning with { Stop = $"{cleaning.Table.Table}: {reason}." },
            () => next(cleaning));
    };
}
