// shared/problem.ts: an error answer from the service, read into a problem document (US-07). The
// service answers each request over HTTP, the web's rules for a request and its answer, and an
// answer that fails carries a problem document: an error in JSON, the text form of a value that
// programs exchange, in the shape RFC 9457, the internet standard for error answers, defines. The
// shared kernel, the module every other module may use, holds the reader; map/tiles.ts calls it
// for a tile request that failed.
//
// A problem travels as a value, in a message such as `tileFailed`, never as an exception to throw.

import * as v from "valibot";
import { parseWith } from "./json";
import { type Result, map } from "./result";

/** An error answer from the service, a problem document as RFC 9457 defines it. */
export interface Problem {
	readonly type: string; // 'about:blank' when the status, the answer's three-digit result, says it all
	readonly title: string;
	readonly status: number;
	readonly detail?: string;
	readonly instance?: string;
}

/** A field of text, or nothing when it is missing, blank or of another JSON type. */
const text = v.fallback(v.optional(v.pipe(v.string(), v.check((s) => s.trim() !== ""))), undefined);

/**
 * The fields a reader takes from a problem document; a field of the wrong JSON type reads as missing.
 * Every field may be missing, so a list would pass as an object of none; the first step refuses one.
 */
const ProblemFields = v.pipe(
	v.custom<object>((json) => typeof json === "object" && json !== null && !Array.isArray(json), "not a problem document"),
	v.object({ type: text, title: text, status: v.fallback(v.optional(v.number()), undefined), detail: text, instance: text }),
);

/**
 * Reads a problem document, or refuses anything else. RFC 9457 lets a missing type mean
 * 'about:blank', and asks a reader to ignore fields of the wrong JSON type.
 */
export const readProblem = (json: unknown, httpStatus: number): Result<Problem> =>
	map((fields: v.InferOutput<typeof ProblemFields>): Problem => {
		const status = fields.status ?? httpStatus;
		return {
			type: fields.type ?? "about:blank",
			title: fields.title ?? `HTTP ${status}`,
			status,
			...(fields.detail === undefined ? {} : { detail: fields.detail }),
			...(fields.instance === undefined ? {} : { instance: fields.instance }),
		};
	})(parseWith(ProblemFields)(json));
