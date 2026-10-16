using CSharpFunctionalExtensions;
using ExhaustiveMatching;

namespace Tessera.Etl;

/// <summary>
/// Why one table could not become SQL. Each table ends in a script or in one of these, and any one
/// of them stops the run with no script written.
/// </summary>
/// <remarks>
/// A <b><i>union</i></b>, as F# would write
/// <c>NotExtracted of TablePlan * string | Unreadable of TablePlan * string | Stopped of Outcome * string | Refused of Outcome * string</c>.
/// A plain string could not say whether the rows were cleaned; these cases can, so a stop or a
/// refusal still puts its table's counts in the log, and the two earlier failures do not.
/// </remarks>
[Closed(typeof(NotExtracted), typeof(Unreadable), typeof(Stopped), typeof(Refused))]
public abstract record Failure
{
    /// <summary>GDAL could not copy the table out of the source.</summary>
    public sealed record NotExtracted(TablePlan Table, string Why) : Failure;

    /// <summary>GDAL wrote a file that is not a FeatureCollection, perhaps having failed part-way.</summary>
    public sealed record Unreadable(TablePlan Table, string Why) : Failure;

    /// <summary>A filter or the stop rules ended the run; <c>Why</c> already names the table.</summary>
    public sealed record Stopped(Outcome Outcome, string Why) : Failure;

    /// <summary>The cleaned rows cannot be written as SQL; <c>Why</c> already names the table.</summary>
    public sealed record Refused(Outcome Outcome, string Why) : Failure;

    /// <summary>The line the program prints and the log ends with.</summary>
    public string Message => this switch
    {
        NotExtracted failure => $"{failure.Table.Table}: extraction failed. {failure.Why}",
        Unreadable failure => $"{failure.Table.Table}: GDAL's GeoJSON could not be read. {failure.Why}",
        Stopped failure => $"stopped. {failure.Why} No script written.",
        Refused failure => failure.Why,
        _ => throw ExhaustiveMatch.Failed(this),
    };

    /// <summary>The table's counts and log lines, when its rows were cleaned before it failed.</summary>
    public Maybe<Outcome> Cleaned => this switch
    {
        NotExtracted _ => Maybe<Outcome>.None,
        Unreadable _ => Maybe<Outcome>.None,
        Stopped failure => failure.Outcome,
        Refused failure => failure.Outcome,
        _ => throw ExhaustiveMatch.Failed(this),
    };
}
