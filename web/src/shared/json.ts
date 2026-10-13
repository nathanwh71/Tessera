// shared/json.ts: the first step of reading any JSON the service sends.

import { type Result, err, ok } from "./result";

/** True for a JSON object, a set of named fields: not null, not an array (a list). */
export function isRecord(value: unknown): value is Readonly<Record<string, unknown>> {
	return typeof value === "object" && value !== null && !Array.isArray(value);
}

/** A finite number, neither infinity nor NaN (not a number), as JSON numbers from the service must be. */
export function isFiniteNumber(value: unknown): value is number {
	return typeof value === "number" && Number.isFinite(value);
}

/**
 * Reads JSON text into a value, or says the text is no JSON. `JSON.parse` throws on bad text; this
 * turns the throw into a value, once, here, so no other code needs a `try`.
 */
export function parseJson(text: string): Result<unknown> {
	try {
		return ok(JSON.parse(text) as unknown);
	} catch {
		return err("not JSON");
	}
}
