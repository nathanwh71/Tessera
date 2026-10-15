namespace Tessera.Etl;

/// <summary>
/// Joins <b><i>filters</i></b> into one, as F#'s <c>&gt;&gt;</c> joins functions:
/// <c>a &gt;&gt; b</c> runs <c>a</c>, then <c>b</c> on the rows <c>a</c> kept. A stop ends the
/// chain at once, and the logs join in order. <see cref="Keep"/> starts a chain, so a list of
/// filters folds into one: <c>filters.Aggregate(Chain.Keep, (a, b) =&gt; a &gt;&gt; b)</c>.
/// </summary>
public static class Chain
{
    /// <summary>A filter that keeps every row and logs nothing: the start of any chain.</summary>
    public static readonly Filter Keep = rows => new Outcome(rows, [], null);

    // C# 14's extension members add an operator to a type declared elsewhere, here the Filter
    // delegate. `>>` is C#'s shift operator, reused to mean "then", as in F#.
    extension(Filter)
    {
        /// <summary>Runs <paramref name="first"/>, then <paramref name="next"/> on the rows it kept.</summary>
        public static Filter operator >>(Filter first, Filter next) => rows =>
        {
            var before = first(rows);
            if (before.Stop is not null) return before;
            var after = next(before.Rows);
            return after with { Log = before.Log.AddRange(after.Log) };
        };
    }
}
