// shared/json.ts: the first step of reading any JSON the service sends. JSON is the text form of a
// value that programs exchange: an object of named fields, a list, text, a number, true, false or
// null. The shared kernel holds these steps, and the readers of the area and of a problem document
// use them before they trust a field.

import * as v from "valibot";
import { type Result, err, ok } from "./result";

/** Reads text as JSON, or says it is not JSON. */
export function parseJson(text: string): Result<unknown> {
	try {
		return ok(JSON.parse(text) as unknown);
	} catch {
		return err("not JSON");
	}
}

/**
 * Reads a value by a schema, a description of the shape a value must have, written with Valibot, a
 * small library of such descriptions. The answer is the map's own `Result`: the value as the schema
 * reads it, unknown fields left out, or the schema's sentence for the first thing that is wrong.
 * Each reader then states the shape it wants, and the checks need no `if` of their own.
 */
export const parseWith =
	<S extends v.GenericSchema>(schema: S) =>
	(value: unknown): Result<v.InferOutput<S>> => {
		const read = v.safeParse(schema, value);
		return read.success ? ok(read.output) : err(read.issues[0].message);
	};
