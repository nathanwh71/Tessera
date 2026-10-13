// map/tiles.ts: what a failed tile request says, read into a problem document (US-23, criterion 8).

import { type Problem, defaultWith, flatMap, parseJson, pipe, readProblem } from "../shared";

/** A problem made from the status alone, for a body that is no problem document. */
const problemOfStatus = (status: number, statusText: string): Problem => ({
	type: "about:blank",
	title: statusText.trim() === "" ? `HTTP ${status}` : statusText,
	status,
});

/**
 * The problem a failed tile request reports. The service answers every error with a problem document,
 * the JSON form RFC 9457 sets; `body` is the answer's text. A body that is not one, such as a proxy's
 * HTML page, gives a problem made from the status alone, so the page still has words to show.
 */
export const tileProblem = (status: number, statusText: string, body: string): Problem =>
	pipe(
		parseJson(body),
		flatMap((json) => readProblem(json, status)),
		defaultWith(() => problemOfStatus(status, statusText)),
	);
