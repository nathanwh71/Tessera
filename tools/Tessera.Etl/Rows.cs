using System.Collections.Immutable;
using CSharpFunctionalExtensions;
using NetTopologySuite.Geometries;

namespace Tessera.Etl;

/// <summary>
/// One place read from the source: its number in the source, counting from 1, so the log can name
/// it; its shape, in longitude and latitude, or none; and its other values, by column name. A value
/// is a string, a whole number (long), a number with a fraction (double), true or false, or null.
/// Values keep null, not a <b><i>Maybe</i></b>: in GeoJSON a null is a value in its own right, the
/// mark of an empty cell, and it must reach the GeoJSON the pipeline writes as it came. tidyText,
/// too, writes null for text that tidies to nothing.
/// </summary>
public sealed record Row(int Number, Maybe<Geometry> Shape, ImmutableDictionary<string, object?> Values)
{
    /// <summary>
    /// The value in <paramref name="column"/>, or none when the row has none: a missing column
    /// and an empty one mean the same to a <b><i>filter</i></b>, a step that checks or mends rows.
    /// </summary>
    public Maybe<object> Value(string column) =>
        Values.TryGetValue(column, out var value) ? value.AsMaybe() : Maybe<object>.None;
}

/// <summary>What a filter did to one row. Kept rows leave no line in the log; the rest do.</summary>
public enum Act
{
    Kept,
    Fixed,
    Dropped,
    Stopped,
}

/// <summary>
/// A filter's word on one row: keep it, keep a mended copy, or fail it. A filter returns one
/// verdict for each row, in order, and Steps.Filter acts on them, so every filter drops, logs and
/// stops in the same way.
/// </summary>
public sealed record Verdict(Act Act, Row Row, string Detail)
{
    /// <summary>The row passes as it is.</summary>
    public static Verdict Keep(Row row) => new(Act.Kept, row, "");

    /// <summary>The row passes as <paramref name="mended"/>, and the log says what changed.</summary>
    public static Verdict Fix(Row mended, string did) => new(Act.Fixed, mended, did);

    /// <summary>The row cannot pass, for the reason given: it is dropped, or the run stops.</summary>
    public static Verdict Fail(Row row, string reason) => new(Act.Dropped, row, reason);
}

/// <summary>One line of the log: a row's number, what happened to it, and why.</summary>
public sealed record LogLine(int Row, string Filter, Act Act, string Detail)
{
    /// <summary>
    /// The line as the log file writes it. One fixed form lets a publisher search the log, and
    /// lets the tests match a line in full.
    /// </summary>
    public override string ToString() => $"Row {Row}: {Act.ToString().ToLowerInvariant()} by {Filter}: {Detail}";
}

/// <summary>
/// The <b><i>context</i></b> every step of a table's chain reads and hands on (see Pipes.cs): the
/// table, the rows read from the source, the rows still kept, a log line for each row a filter
/// fixed, dropped or stopped at, and, once a step stops the run, the reason. Read stays fixed, so
/// the stop rules can count what the filters dropped.
/// </summary>
public sealed record Cleaning(TablePlan Table, int Read, ImmutableArray<Row> Rows, ImmutableArray<LogLine> Log, Maybe<string> Stop)
{
    /// <summary>A table's rows as GDAL read them, before any step has run.</summary>
    public static Cleaning Start(TablePlan table, ImmutableArray<Row> rows) => new(table, rows.Length, rows, [], Maybe<string>.None);

    /// <summary>The rows lost, counted from the rows read, whichever filter dropped them.</summary>
    public int Dropped => Read - Rows.Length;
}
