using System.Collections.Immutable;
using CSharpFunctionalExtensions;

namespace Tessera.Etl;

/// <summary>A table cleaned and written: its counts and log lines, and the SQL that makes and fills it.</summary>
public sealed record Loaded(Outcome Outcome, ImmutableArray<string> Sql);

/// <summary>
/// The work done on each table between GDAL's extract and the script. It reads no file and runs
/// no program, so it is a <b><i>pure function</i></b>: the same text in gives the same result out.
/// Program.cs does the reading and writing around it, and the tests call it with GeoJSON they write
/// themselves (ADR 0004).
/// </summary>
public static class Tables
{
    /// <summary>
    /// Reads the rows from <paramref name="geoJson"/>, runs the table's chain of filters and stop rules
    /// on them (the whole transform step), and writes them as SQL. It returns the first
    /// <see cref="Failure"/> met, and does no work after it.
    /// </summary>
    public static Result<Loaded, Failure> Load(CleaningPlan plan, string geoJson)
    {
        var read = GeoJson.Read(geoJson);
        if (read.IsFailure)
            return new Failure.Unreadable(plan.Table, read.Error);
        var (rows, columns) = read.Value;
        var done = plan.Chain.Run(Cleaning.Start(plan.Table, rows));
        if (done.Stop.TryGetValue(out var stop))
            return new Failure.Stopped(done.Outcome, stop);
        return TableSql.Statements(plan.Table, done.Rows, columns)
            .Map(sql => new Loaded(done.Outcome, sql))
            .MapError(Failure (why) => new Failure.Refused(done.Outcome, why));
    }
}
