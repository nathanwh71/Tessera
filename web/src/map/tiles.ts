// map/tiles.ts: what a failed request for a tile, a square piece of the map, says, read into a
// problem document: the service's form of an error in JSON, the text form of a value that programs
// exchange (US-23, criterion 8). A pure function of the map module, which reads only its inputs and
// changes nothing outside itself; maplibre.ts calls it for each tile request the service refuses,
// and the update shows the words it returns over the map.

import { type Problem, defaultWith, flatMap, parseJson, pipe, readProblem } from "../shared";

/**
 * A problem made from the status alone, the answer's three-digit result, for a body that is no
 * problem document.
 */
const problemOfStatus = (status: number, statusText: string): Problem => ({
	type: "about:blank",
	title: statusText.trim() === "" ? `HTTP ${status}` : statusText,
	status,
});

/**
 * The problem a failed tile request reports. The service answers every error with a problem document
 * in the form RFC 9457, the internet standard for error answers, sets; `body` is the answer's text. A
 * body that is not one, such as the HTML page of a proxy, a server that stands between the browser
 * and the service, gives a problem made from the status alone, so the page still has words to show.
 * A pipe, a chain of steps run in the order written (shared/pipe.ts), joins the three steps.
 */
export const tileProblem = (status: number, statusText: string, body: string): Problem =>
	pipe(
		parseJson(body),
		flatMap((json) => readProblem(json, status)),
		defaultWith(() => problemOfStatus(status, statusText)),
	);
