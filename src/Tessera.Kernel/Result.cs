using System.Diagnostics;

namespace Tessera.Kernel;

/// <summary>
/// The outcome of a step that may refuse: <see cref="Ok"/> holds the value, <see cref="Refused"/>
/// holds the reason.
/// </summary>
/// <remarks>
/// A refusal is an expected outcome, such as a GeoJSON file in the wrong coordinate system, so it
/// comes back as a value rather than an exception (ADR 0004). An exception jumps out of the normal
/// flow and is easy to forget to catch; a Result sits in the type, so the compiler makes every
/// caller handle both cases. The private constructor means Ok and Refused are the only two cases.
/// </remarks>
public abstract record Result<T, TError>
{
    private Result() { }

    public sealed record Ok(T Value) : Result<T, TError>;

    public sealed record Refused(TError Error) : Result<T, TError>;

    /// <summary>Runs <paramref name="ok"/> on the value, or <paramref name="refused"/> on the reason.</summary>
    public TOut Match<TOut>(Func<T, TOut> ok, Func<TError, TOut> refused) => this switch
    {
        Ok(var value) => ok(value),
        Refused(var error) => refused(error),
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

    /// <summary>Changes the reason for a refusal, so steps with different errors can chain.</summary>
    public Result<T, TNextError> MapError<TNextError>(Func<TError, TNextError> next) =>
        Match<Result<T, TNextError>>(
            value => new Result<T, TNextError>.Ok(value),
            error => new Result<T, TNextError>.Refused(next(error)));

    /// <summary>Runs a next step that waits on the database or the network.</summary>
    public async Task<Result<TNext, TError>> BindAsync<TNext>(Func<T, Task<Result<TNext, TError>>> next) =>
        this switch
        {
            Ok(var value) => await next(value).ConfigureAwait(false),
            Refused(var error) => new Result<TNext, TError>.Refused(error),
            _ => throw new UnreachableException(),
        };

    // Select and SelectMany let C#'s query form, "from … in … select", chain steps the way an
    // F# computation expression does.
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
    /// Checking a list of 900 places is one step per place. This turns "a list of results" into
    /// "a result holding a list", the shape the next step wants. F# calls it traverse.
    /// </remarks>
    public static Result<System.Collections.Immutable.ImmutableArray<TOut>, TError> Traverse<TIn, TOut, TError>(
        this IEnumerable<TIn> items, Func<TIn, Result<TOut, TError>> step)
    {
        var values = System.Collections.Immutable.ImmutableArray.CreateBuilder<TOut>();
        foreach (var item in items)
        {
            switch (step(item))
            {
                case Result<TOut, TError>.Ok(var value):
                    values.Add(value);
                    break;
                case Result<TOut, TError>.Refused(var error):
                    return new Result<System.Collections.Immutable.ImmutableArray<TOut>, TError>.Refused(error);
            }
        }

        return new Result<System.Collections.Immutable.ImmutableArray<TOut>, TError>.Ok(values.ToImmutable());
    }

    /// <summary>
    /// <see cref="Traverse"/> for steps that wait, such as queries: each runs after the one before,
    /// and the first refusal stops the rest.
    /// </summary>
    public static async Task<Result<System.Collections.Immutable.ImmutableArray<TOut>, TError>> TraverseAsync<TIn, TOut, TError>(
        this IEnumerable<TIn> items, Func<TIn, Task<Result<TOut, TError>>> step)
    {
        var values = System.Collections.Immutable.ImmutableArray.CreateBuilder<TOut>();
        foreach (var item in items)
        {
            switch (await step(item).ConfigureAwait(false))
            {
                case Result<TOut, TError>.Ok(var value):
                    values.Add(value);
                    break;
                case Result<TOut, TError>.Refused(var error):
                    return new Result<System.Collections.Immutable.ImmutableArray<TOut>, TError>.Refused(error);
            }
        }

        return new Result<System.Collections.Immutable.ImmutableArray<TOut>, TError>.Ok(values.ToImmutable());
    }
}
