// shared/pipe.ts: F#'s |> and >> for TypeScript, which has neither (ADR 0004).
//
// JavaScript's own pipe operator is still a proposal, and TypeScript cannot read it, so these two
// functions stand in for it. Each list of overloads, one line for each number of steps, lets the
// compiler check that every step takes what the step before it gives. The overloads are types alone,
// so they leave plain JavaScript behind (ADR 0002, rule 4).

/** Runs a value through each step in the order written, as F#'s `x |> f |> g` does. */
export function pipe<A>(a: A): A;
export function pipe<A, B>(a: A, ab: (a: A) => B): B;
export function pipe<A, B, C>(a: A, ab: (a: A) => B, bc: (b: B) => C): C;
export function pipe<A, B, C, D>(a: A, ab: (a: A) => B, bc: (b: B) => C, cd: (c: C) => D): D;
export function pipe<A, B, C, D, E>(a: A, ab: (a: A) => B, bc: (b: B) => C, cd: (c: C) => D, de: (d: D) => E): E;
export function pipe(a: unknown, ...steps: readonly ((x: unknown) => unknown)[]): unknown {
	return steps.reduce((value, step) => step(value), a);
}

/** Joins steps into one function that runs them in the order written, as F#'s `f >> g` does. */
export function flow<A, B>(ab: (a: A) => B): (a: A) => B;
export function flow<A, B, C>(ab: (a: A) => B, bc: (b: B) => C): (a: A) => C;
export function flow<A, B, C, D>(ab: (a: A) => B, bc: (b: B) => C, cd: (c: C) => D): (a: A) => D;
export function flow(...steps: readonly ((x: unknown) => unknown)[]): (a: unknown) => unknown {
	return (a) => steps.reduce((value, step) => step(value), a);
}
