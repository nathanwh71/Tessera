// shared/problem.ts: error answers from the HTTP API, as RFC 9457, the internet standard for them, defines them (US-07).
//
// A problem travels as a value, in a message such as `tileFailed`, never as an exception to throw.

import { isRecord } from "./json";
import { type Result, err, ok } from "./result";

/** An error answer from the service, a 'problem document' as RFC 9457 defines it. */
export interface Problem {
	readonly type: string; // 'about:blank' when the status code says it all
	readonly title: string;
	readonly status: number;
	readonly detail?: string;
	readonly instance?: string;
}

const text = (v: unknown): string | undefined => (typeof v === "string" && v.trim() !== "" ? v : undefined);

/**
 * Reads a problem document, or refuses anything else. RFC 9457 lets a missing type mean
 * 'about:blank', and asks a reader to ignore fields of the wrong JSON type.
 */
export function readProblem(json: unknown, httpStatus: number): Result<Problem> {
	if (!isRecord(json)) return err("not a problem document");
	const status = typeof json["status"] === "number" ? json["status"] : httpStatus;
	const detail = text(json["detail"]);
	const instance = text(json["instance"]);
	return ok({
		type: text(json["type"]) ?? "about:blank",
		title: text(json["title"]) ?? `HTTP ${status}`,
		status,
		...(detail === undefined ? {} : { detail }),
		...(instance === undefined ? {} : { instance }),
	});
}
