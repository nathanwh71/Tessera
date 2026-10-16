namespace Tessera.Etl;

// The kernel of the pipeline: Giraffe's two types, without the web. Giraffe, a web library for F#,
// builds a web server from handlers, each a step that may pass a request on to the rest. Here the
// same idea passes a table of places from one cleaning step to the next. The kernel knows nothing
// of places: the pipeline fills in TContext with its own Cleaning record.

/// <summary>
/// The rest of a chain, from here to its end: it takes the <b><i>context</i></b>, the value every
/// step reads and hands on, and returns the context as the chain left it. Giraffe calls this an
/// HttpFunc.
/// </summary>
public delegate TContext Next<TContext>(TContext context);

/// <summary>
/// One step of a chain, Giraffe's HttpHandler: given the rest of the chain, it returns a chain with
/// this step in front. A step may change the context and pass it on, or end the chain by returning
/// the context without calling <c>next</c>, as a stop does.
/// </summary>
public delegate Next<TContext> Handler<TContext>(Next<TContext> next);

/// <summary>
/// The ways to join and run <b><i>handlers</i></b>. Each is a few lines, since a handler is only a
/// function: joining two is calling one with the other.
/// </summary>
public static class Pipes
{
    // C# 14's extension members add an operator to a type declared elsewhere, here the Handler
    // delegate. `>>` is C#'s shift operator, reused to mean 'then', as F#'s >> and Giraffe's >=> do.
    extension<TContext>(Handler<TContext>)
    {
        /// <summary>
        /// Runs <paramref name="first"/>, then <paramref name="second"/>: the first is handed a
        /// chain that starts with the second, so whatever the first passes on reaches the second.
        /// </summary>
        public static Handler<TContext> operator >>(Handler<TContext> first, Handler<TContext> second) =>
            next => first(second(next));
    }

    /// <summary>
    /// The step that passes the context straight on. It starts a fold, so a list of steps joins
    /// into one chain, and an empty list gives a chain that changes nothing.
    /// </summary>
    public static Handler<TContext> Identity<TContext>() => next => next;

    /// <summary>Joins steps in the order given, as <c>a &gt;&gt; b &gt;&gt; c</c> would.</summary>
    public static Handler<TContext> All<TContext>(IEnumerable<Handler<TContext>> steps) =>
        steps.Aggregate(Identity<TContext>(), (chain, step) => chain >> step);

    /// <summary>
    /// A step that applies only when <paramref name="test"/> holds; otherwise the context passes
    /// straight on. A step stays simple when the choice of where it applies lives outside it.
    /// </summary>
    public static Handler<TContext> When<TContext>(Func<TContext, bool> test, Handler<TContext> step) =>
        next => context => test(context) ? step(next)(context) : next(context);

    /// <summary>
    /// Runs a chain on a context. The end of every chain hands the context back as it arrives, so
    /// the result is the context as the last step to run left it.
    /// </summary>
    public static TContext Run<TContext>(this Handler<TContext> chain, TContext context) =>
        chain(done => done)(context);
}
