using System.Collections.Immutable;
using NetTopologySuite.Geometries;

namespace Tessera.Etl;

/// <summary>
/// One place read from the source: its number in the source, counting from 1, so the log can name
/// it; its shape, in longitude and latitude; and its other values, by column name. A value is a
/// string, a whole number (long), a number with a fraction (double), true or false, or null.
/// </summary>
public sealed record Row(int Number, Geometry? Shape, ImmutableDictionary<string, object?> Values)
{
    /// <summary>The value in <paramref name="column"/>, or null when the row has none.</summary>
    public object? Value(string column) => Values.TryGetValue(column, out var value) ? value : null;
}

/// <summary>What a filter did to one row.</summary>
public enum Act
{
    Fixed,
    Dropped,
    Stopped,
}

/// <summary>One line of the log: a row's number, what happened to it, and why.</summary>
public sealed record LogLine(int Row, string Filter, Act Act, string Detail)
{
    public override string ToString() => $"Row {Row}: {Act.ToString().ToLowerInvariant()} by {Filter}: {Detail}";
}

/// <summary>
/// What one filter hands on: the rows it kept, a log line for each row it fixed or dropped, and,
/// when it found a row it must not pass, the reason the whole run stops.
/// </summary>
public sealed record Outcome(ImmutableArray<Row> Rows, ImmutableArray<LogLine> Log, string? Stop)
{
    public int Fixed => Log.Count(line => line.Act == Act.Fixed);

    public int Dropped => Log.Count(line => line.Act == Act.Dropped);
}

/// <summary>
/// A filter takes every row of one table and returns an <see cref="Outcome"/>. Each is a pure
/// function: it reads only its rows and its settings, so a test can check it with no files (ADR 0004).
/// </summary>
public delegate Outcome Filter(ImmutableArray<Row> rows);
