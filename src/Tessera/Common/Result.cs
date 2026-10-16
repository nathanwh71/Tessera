// A value that holds either an answer or the reason there is none (ADR 0004). A function returns one
// for a failure its caller expects, such as a database that cannot be reached, rather than throwing,
// so the caller must look before it uses the answer. F#, a functional language, has the same type.
// The ETL in tools/Tessera.Etl compiles this same file, so the whole repository has one Result.

namespace Common;

/// <summary>
/// Either a value or an error, never both: a <b><i>Result</i></b>. <see cref="Match{TOut}"/> is the
/// safe way to read one, since it makes the caller say what happens in both cases.
/// <see cref="Value"/> and <see cref="Error"/> suit code that has checked <see cref="IsFailure"/> first.
/// </summary>
/// <typeparam name="T">The kind of answer, such as a tile's bytes.</typeparam>
/// <typeparam name="TError">The kind of reason, such as a <c>DatabaseError</c> or a message.</typeparam>
public sealed record Result<T, TError>
{
    private readonly T? _value;
    private readonly TError? _error;

    // Private, so only Result.Success and Result.Failure make one, and every Result holds exactly one
    // of the two.
    private Result(bool isSuccess, T? value, TError? error) => (IsSuccess, _value, _error) = (isSuccess, value, error);

    internal static Result<T, TError> Success(T value) => new(true, value, default);

    internal static Result<T, TError> Failure(TError error) => new(false, default, error);

    /// <summary>True when this holds an answer.</summary>
    public bool IsSuccess { get; }

    /// <summary>True when this holds a reason instead.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The answer. Reading it from a failure is a fault in the code, so it throws.</summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"A failed Result holds no value; it failed with: {_error}");

    /// <summary>The reason. Reading it from a success is a fault in the code, so it throws.</summary>
    public TError Error => IsFailure ? _error! : throw new InvalidOperationException("A successful Result holds no error.");

    /// <summary>
    /// Changes the answer, if there is one, and passes a reason on untouched, so a chain of steps
    /// stops at the first failure without an <c>if</c> at each step.
    /// </summary>
    public Result<TOther, TError> Map<TOther>(Func<T, TOther> change) =>
        IsSuccess ? Result<TOther, TError>.Success(change(_value!)) : Result<TOther, TError>.Failure(_error!);

    /// <summary>
    /// Turns either case into one value: <paramref name="onValue"/> for an answer,
    /// <paramref name="onError"/> for a reason. The caller cannot forget the failure.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onValue, Func<TError, TOut> onError) =>
        IsSuccess ? onValue(_value!) : onError(_error!);

    /// <summary>The answer, or <paramref name="fallback"/> when there is none.</summary>
    public T GetValueOrDefault(T fallback) => IsSuccess ? _value! : fallback;
}

/// <summary>
/// Makes a <see cref="Result{T, TError}"/>: <c>Result.Success&lt;T, TError&gt;(value)</c> or
/// <c>Result.Failure&lt;T, TError&gt;(error)</c>.
/// </summary>
public static class Result
{
    public static Result<T, TError> Success<T, TError>(T value) => Result<T, TError>.Success(value);

    public static Result<T, TError> Failure<T, TError>(TError error) => Result<T, TError>.Failure(error);
}
