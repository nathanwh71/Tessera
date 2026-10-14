using System.Diagnostics;

namespace Tessera.Kernel;

/// <summary>
/// A value that may be missing: <see cref="Some"/> holds one, <see cref="None"/> holds nothing.
/// It lives in Kernel, the small module every other module shares; Settings, Sql, Catalogue, Import,
/// Tiles, Http and Map use it wherever a value may be absent.
/// </summary>
/// <remarks>
/// <para>
/// C# can mark a missing value as <c>null</c>, but a null travels unseen until some line of code trips
/// over it and the program crashes. An Option says 'this may be missing' in its type, so the compiler,
/// the program that turns C# into something the machine runs, makes every caller decide what to do
/// when it is (ADR 0004). The idea comes from F#, another programming language, where it has the same
/// name.
/// </para>
/// <para>
/// The <c>T</c> in <c>Option&lt;T&gt;</c> makes it generic, a type with a blank for another type:
/// <c>Option&lt;string&gt;</c> may hold text, <c>Option&lt;int&gt;</c> a whole number. It is a record,
/// a bundle of named values that cannot change once made. Its constructor, the code that makes a new
/// one, is private, so Some and None are the only two cases there can be.
/// </para>
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

    /// <summary>
    /// Runs <paramref name="some"/> on the value, or <paramref name="none"/> if there is none. Both are
    /// functions handed in as values, so the caller must say what happens in each case.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> some, Func<TOut> none) => this switch
    {
        Some(var value) => some(value),
        None => none(),
        // The private constructor rules out any third case, so this line can never run.
        _ => throw new UnreachableException(),
    };

    /// <summary>Changes the value if there is one; nothing stays nothing.</summary>
    public Option<TNext> Map<TNext>(Func<T, TNext> next) =>
        Match<Option<TNext>>(value => new Option<TNext>.Some(next(value)), () => Option<TNext>.None.Instance);

    /// <summary>Runs a next step that may itself find nothing; nothing at any step ends the chain.</summary>
    public Option<TNext> Bind<TNext>(Func<T, Option<TNext>> next) =>
        Match(next, () => Option<TNext>.None.Instance);

    /// <summary>The value, or <paramref name="fallback"/> if there is none.</summary>
    public T OrElse(T fallback) => Match(value => value, () => fallback);

    /// <summary>
    /// Turns a missing value into a refusal that says why, so a step that found nothing can join a
    /// chain of <see cref="Result{T,TError}"/> steps.
    /// </summary>
    public Result<T, TError> OrRefuse<TError>(TError error) =>
        Match<Result<T, TError>>(value => new Result<T, TError>.Ok(value), () => new Result<T, TError>.Refused(error));

    // Select and SelectMany let C#'s query form, 'from … in … select', chain steps one after another,
    // the way an F# computation expression does. C# looks for these two names, so they must stay.
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
    /// Libraries, code written by others that Tessera borrows, such as Dapper and System.Text.Json,
    /// hand back null for a missing value. The edge of a module turns it into an Option at once, so no
    /// null crosses into the code other modules call. The word <c>this</c> before the parameter lets a
    /// caller write <c>value.ToOption()</c>, as if the method belonged to the value.
    /// </remarks>
    public static Option<T> ToOption<T>(this T? value) where T : class =>
        value is null ? None<T>() : Some(value);

    // C# treats a missing object and a missing number differently, so plain values such as int need
    // this second version.
    /// <inheritdoc cref="ToOption{T}(T)"/>
    public static Option<T> ToOption<T>(this T? value) where T : struct =>
        value is { } present ? Some(present) : None<T>();
}
