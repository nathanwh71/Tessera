using System.Collections.Immutable;
using System.Diagnostics;

namespace Tessera.Kernel;

/// <summary>
/// The outcome of a step that may refuse: <see cref="Ok"/> holds the value, <see cref="Refused"/>
/// holds the reason. It lives in Kernel, the small module every other module shares; Settings, Sql,
/// Catalogue, Import and Tiles return it from any step that can fail.
/// </summary>
/// <remarks>
/// <para>
/// A refusal is an expected outcome, such as a GeoJSON file in the wrong coordinate system, so it
/// comes back as a value rather than an exception, C#'s way of jumping out of the normal flow when
/// something goes wrong (ADR 0004). An exception is easy to forget to catch; a Result sits in the
/// type, so the compiler, the program that turns C# into something the machine runs, makes every
/// caller handle both cases.
/// </para>
/// <para>
/// <c>T</c> and <c>TError</c> make the type generic, a type with blanks for other types: the type of
/// the value and the type of the reason. It is a record, a bundle of named values that cannot change
/// once made. Its constructor, the code that makes a new one, is private, so Ok and Refused are the
/// only two cases.
/// </para>
/// </remarks>
public abstract record Result<T, TError>
{
    private Result() { }

    public sealed record Ok(T Value) : Result<T, TError>;

    public sealed record Refused(TError Error) : Result<T, TError>;

    /// <summary>
    /// Runs <paramref name="ok"/> on the value, or <paramref name="refused"/> on the reason. Both are
    /// functions handed in as values, so the caller must say what happens in each case.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> ok, Func<TError, TOut> refused) => this switch
    {
        Ok(var value) => ok(value),
        Refused(var error) => refused(error),
        // The private constructor rules out any third case, so this line can never run.
        _ => throw new UnreachableException(),
    };

    /// <summary>Changes the value if the step succeeded; a refusal passes through untouched.</summary>
    public Result<TNext, TError> Map<TNext>(Func<T, TNext> next) =>
        Match<Result<TNext, TError>>(
            value => new Result<TNext, TError>.Ok(next(value)),
            error => new Result<TNext, TError>.Refused(error));

    /// <summary>Runs a next step that may itself refuse; the first refusal ends the chain.</summary>
    public Result<TNext, TError> Bind<TNext>(Func<T, Result<TNext, TError>> next) =>
        Match(next, error => new Result<TNext, TError>.Refused(error));

    /// <summary>Changes the reason for a refusal, so steps with different kinds of reason can chain.</summary>
    public Result<T, TNextError> MapError<TNextError>(Func<TError, TNextError> next) =>
        Match<Result<T, TNextError>>(
            value => new Result<T, TNextError>.Ok(value),
            error => new Result<T, TNextError>.Refused(next(error)));

    /// <summary>
    /// <see cref="Bind"/> for a next step that waits on the database or the network. The step returns a
    /// <c>Task</c>, a promise of a value that arrives later; <c>await</c> lets the program serve other
    /// requests while it waits.
    /// </summary>
    public async Task<Result<TNext, TError>> BindAsync<TNext>(Func<T, Task<Result<TNext, TError>>> next) =>
        this switch
        {
            Ok(var value) => await next(value).ConfigureAwait(false),
            Refused(var error) => new Result<TNext, TError>.Refused(error),
            _ => throw new UnreachableException(),
        };

    // Select and SelectMany let C#'s query form, 'from … in … select', chain steps one after another,
    // the way a computation expression does in F#, another programming language. C# looks for these
    // two names, so they must stay.
    public Result<TNext, TError> Select<TNext>(Func<T, TNext> next) => Map(next);

    public Result<TOut, TError> SelectMany<TNext, TOut>(
        Func<T, Result<TNext, TError>> next, Func<T, TNext, TOut> project) =>
        Bind(value => next(value).Map(nextValue => project(value, nextValue)));
}

/// <summary>Builds results, so a caller need not spell out both types.</summary>
public static class Result
{
    public static Result<T, TError> Ok<T, TError>(T value) => new Result<T, TError>.Ok(value);

    public static Result<T, TError> Refuse<T, TError>(TError error) => new Result<T, TError>.Refused(error);

    /// <summary>
    /// Runs each step in turn and keeps every value, or stops at the first refusal.
    /// </summary>
    /// <remarks>
    /// Checking a list of 900 places is one step per place. This turns 'a list of results' into
    /// 'a result holding a list', the shape the next step wants. F# calls it traverse. The list it
    /// returns is immutable: nothing can change it once made.
    /// </remarks>
    public static Result<ImmutableArray<TOut>, TError> Traverse<TIn, TOut, TError>(
        this IEnumerable<TIn> items, Func<TIn, Result<TOut, TError>> step)
    {
        var values = ImmutableArray.CreateBuilder<TOut>();
        foreach (var item in items)
        {
            switch (step(item))
            {
                case Result<TOut, TError>.Ok(var value):
                    values.Add(value);
                    break;
                case Result<TOut, TError>.Refused(var error):
                    return new Result<ImmutableArray<TOut>, TError>.Refused(error);
            }
        }

        return new Result<ImmutableArray<TOut>, TError>.Ok(values.ToImmutable());
    }

    /// <summary>
    /// Joins steps that ran side by side and keeps every refusal, where <see cref="Result{T,TError}.Bind"/>
    /// would stop at the first. A failed start then names every missing setting at once (US-00 c4).
    /// </summary>
    /// <returns>Ok with every value as a tuple, a group of values kept together; or every refusal.</returns>
    public static Result<(T1, T2), ImmutableArray<TError>> Collect<T1, T2, TError>(
        Result<T1, TError> first, Result<T2, TError> second)
    {
        var errors = ImmutableArray.CreateRange(Refusals(first).Concat(Refusals(second)));
        return errors.IsEmpty
            ? new Result<(T1, T2), ImmutableArray<TError>>.Ok((Value(first), Value(second)))
            : new Result<(T1, T2), ImmutableArray<TError>>.Refused(errors);
    }

    // C# has no way to take any number of differently typed results, so each count the settings
    // need, two, five and six, has its own version.
    public static Result<(T1, T2, T3, T4, T5), ImmutableArray<TError>> Collect<T1, T2, T3, T4, T5, TError>(
        Result<T1, TError> first, Result<T2, TError> second, Result<T3, TError> third, Result<T4, TError> fourth, Result<T5, TError> fifth)
    {
        var errors = ImmutableArray.CreateRange(
            Refusals(first).Concat(Refusals(second)).Concat(Refusals(third)).Concat(Refusals(fourth)).Concat(Refusals(fifth)));
        return errors.IsEmpty
            ? new Result<(T1, T2, T3, T4, T5), ImmutableArray<TError>>.Ok((Value(first), Value(second), Value(third), Value(fourth), Value(fifth)))
            : new Result<(T1, T2, T3, T4, T5), ImmutableArray<TError>>.Refused(errors);
    }

    public static Result<(T1, T2, T3, T4, T5, T6), ImmutableArray<TError>> Collect<T1, T2, T3, T4, T5, T6, TError>(
        Result<T1, TError> first, Result<T2, TError> second, Result<T3, TError> third, Result<T4, TError> fourth, Result<T5, TError> fifth, Result<T6, TError> sixth)
    {
        var errors = ImmutableArray.CreateRange(
            Refusals(first).Concat(Refusals(second)).Concat(Refusals(third)).Concat(Refusals(fourth)).Concat(Refusals(fifth)).Concat(Refusals(sixth)));
        return errors.IsEmpty
            ? new Result<(T1, T2, T3, T4, T5, T6), ImmutableArray<TError>>.Ok((Value(first), Value(second), Value(third), Value(fourth), Value(fifth), Value(sixth)))
            : new Result<(T1, T2, T3, T4, T5, T6), ImmutableArray<TError>>.Refused(errors);
    }

    private static IEnumerable<TError> Refusals<T, TError>(Result<T, TError> result) =>
        result.Match(_ => [], error => new[] { error });

    // Collect calls this only when it found no refusal, so the result is always Ok.
    private static T Value<T, TError>(Result<T, TError> result) =>
        result.Match(value => value, _ => throw new UnreachableException());

    /// <summary>
    /// <see cref="Traverse"/> for steps that wait, such as queries: each runs after the one before,
    /// and the first refusal stops the rest.
    /// </summary>
    public static async Task<Result<ImmutableArray<TOut>, TError>> TraverseAsync<TIn, TOut, TError>(
        this IEnumerable<TIn> items, Func<TIn, Task<Result<TOut, TError>>> step)
    {
        var values = ImmutableArray.CreateBuilder<TOut>();
        foreach (var item in items)
        {
            switch (await step(item).ConfigureAwait(false))
            {
                case Result<TOut, TError>.Ok(var value):
                    values.Add(value);
                    break;
                case Result<TOut, TError>.Refused(var error):
                    return new Result<ImmutableArray<TOut>, TError>.Refused(error);
            }
        }

        return new Result<ImmutableArray<TOut>, TError>.Ok(values.ToImmutable());
    }
}
