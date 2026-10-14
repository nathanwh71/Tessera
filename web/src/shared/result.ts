// shared/result.ts: a Result, the outcome of a step that may fail, carried as a value rather than as
// a thrown error. It lives in the shared kernel, the module every other module may use, and the
// readers of the area and of a failed tile build their chains on it. Its functions follow those of
// F#, a functional language, whose Result module has the same ones (ADR 0004).

/**
 * A value, or the reason there is none. The `ok` flag says which, so a caller must check before it
 * reads.
 */
export type Result<T, E = string> =
	| { readonly ok: true; readonly value: T }
	| { readonly ok: false; readonly error: E };

/** A success. */
export const ok = <T>(value: T): Result<T, never> => ({ ok: true, value });
/** A failure, and why. */
export const err = <E>(error: E): Result<never, E> => ({ ok: false, error });

// Each function below takes its step first and the result last, as F#'s do, so it can stand in a
// pipe, a chain of steps that runs in the order written (pipe.ts):
// `pipe(result, flatMap(next), defaultWith(fallback))` reads in the order the steps run.

/** Runs the next step only on success; the first failure skips every later step. F#'s `Result.bind`. */
export const flatMap =
	<T, U, E>(next: (value: T) => Result<U, E>) =>
	(r: Result<T, E>): Result<U, E> =>
		r.ok ? next(r.value) : r;

/** Changes a success's value; passes a failure on unchanged. */
export const map =
	<T, U>(f: (value: T) => U) =>
	<E>(r: Result<T, E>): Result<U, E> =>
		r.ok ? ok(f(r.value)) : r;

/** Ends the chain with one plain value: one function for success, one for failure. */
export const fold =
	<T, E, R>(onOk: (value: T) => R, onErr: (error: E) => R) =>
	(r: Result<T, E>): R =>
		r.ok ? onOk(r.value) : onErr(r.error);

/** The value, or the fallback's when the step failed. F#'s `defaultWith`. */
export const defaultWith =
	<T>(fallback: () => T) =>
	<E>(r: Result<T, E>): T =>
		r.ok ? r.value : fallback();
