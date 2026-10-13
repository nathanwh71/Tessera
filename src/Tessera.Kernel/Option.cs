using System.Diagnostics;

namespace Tessera.Kernel;

/// <summary>
/// A value that may be missing: <see cref="Some"/> holds one, <see cref="None"/> holds nothing.
/// </summary>
/// <remarks>
/// C# can mark a missing value as <c>null</c>, but a null travels silently until something trips
/// over it. An Option says "this may be missing" in its type, so the compiler makes every caller
/// decide what to do when it is (ADR 0004). The idea comes from F#, where it has the same name.
/// The base record's constructor is private, so Some and None are the only two cases there can be.
/// </remarks>
public abstract record Option<T>
{
    private Option() { }

    public sealed record Some(T Value) : Option<T>;

    public sealed record None : Option<T>
    {
        // One None serves every caller: it holds nothing, so no two need differ.
        internal static readonly None Instance = new();
    }

    /// <summary>Runs <paramref name="some"/> on the value, or <paramref name="none"/> if there is none.</summary>
    public TOut Match<TOut>(Func<T, TOut> some, Func<TOut> none) => this switch
    {
        Some(var value) => some(value),
        None => none(),
        _ => throw new UnreachableException(),
    };

    /// <summary>Changes the value if there is one; nothing stays nothing.</summary>
    public Option<TNext> Map<TNext>(Func<T, TNext> next) =>
        Match<Option<TNext>>(value => new Option<TNext>.Some(next(value)), () => Option<TNext>.None.Instance);

    /// <summary>Runs a next step that may itself find nothing.</summary>
    public Option<TNext> Bind<TNext>(Func<T, Option<TNext>> next) =>
        Match(next, () => Option<TNext>.None.Instance);

    /// <summary>The value, or <paramref name="fallback"/> if there is none.</summary>
    public T OrElse(T fallback) => Match(value => value, () => fallback);

    /// <summary>Turns a missing value into a refusal that says why.</summary>
    public Result<T, TError> OrRefuse<TError>(TError error) =>
        Match<Result<T, TError>>(value => new Result<T, TError>.Ok(value), () => new Result<T, TError>.Refused(error));

    // Select and SelectMany let C#'s query form, "from … in … select", chain steps the way an
    // F# computation expression does.
    public Option<TNext> Select<TNext>(Func<T, TNext> next) => Map(next);

    public Option<TOut> SelectMany<TNext, TOut>(Func<T, Option<TNext>> next, Func<T, TNext, TOut> project) =>
        Bind(value => next(value).Map(nextValue => project(value, nextValue)));
}

/// <summary>Builds options, so a caller need not spell out the type.</summary>
public static class Option
{
    public static Option<T> Some<T>(T value) => new Option<T>.Some(value);

    public static Option<T> None<T>() => Option<T>.None.Instance;

    /// <summary>
    /// Turns a value that may be null into an Option, at the edge of a module (ADR 0004).
    /// </summary>
    /// <remarks>
    /// Libraries such as Dapper and System.Text.Json hand back null for a missing value. The edge of
    /// a module turns it into an Option at once, so no null crosses the module's public face.
    /// </remarks>
    public static Option<T> ToOption<T>(this T? value) where T : class =>
        value is null ? None<T>() : Some(value);

    /// <inheritdoc cref="ToOption{T}(T)"/>
    public static Option<T> ToOption<T>(this T? value) where T : struct =>
        value is { } present ? Some(present) : None<T>();
}
