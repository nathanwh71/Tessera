import { describe, expect, it } from "vitest";
import * as shared from ".";
import {
	MACAU,
	type Result,
	areaOf,
	clampZoom,
	defaultWith,
	err,
	flatMap,
	flow,
	fold,
	map,
	ok,
	parseArea,
	parseJson,
	pipe,
	readBounds,
	readProblem,
} from ".";

describe("RFC 9457 problem documents", () => {
	it("reads a problem document, taking a missing type as about:blank", () => {
		expect(readProblem({ title: "Not Found", status: 404, detail: "No layer." }, 404)).toEqual(
			ok({ type: "about:blank", title: "Not Found", status: 404, detail: "No layer." }),
		);
	});

	it("ignores members of the wrong JSON type, and falls back to the HTTP status", () => {
		expect(readProblem({ type: 7, title: ["x"], status: "404", extra: true }, 404)).toEqual(
			ok({ type: "about:blank", title: "HTTP 404", status: 404 }),
		);
	});

	it("refuses anything that is not a JSON object, and says why", () => {
		for (const json of ["Not Found", [], null]) expect(readProblem(json, 404)).toEqual(err("not a problem document"));
	});

	it("carries a problem as a value, never as an exception to throw", () => {
		expect(Object.keys(shared)).not.toContain("ProblemError");
		expect(Object.keys(shared)).not.toContain("describeFailure");
	});
});

describe("geometry", () => {
	it("keeps a level within 0 to 22", () => {
		expect([-1, 16.4, 24].map(clampZoom)).toEqual([0, 16.4, 22]);
	});
});

describe("results, as F#'s Result module has them", () => {
	const double = (n: number): number => n * 2;
	const half = (n: number): Result<number> => (n % 2 === 0 ? ok(n / 2) : err(`${n} is odd`));

	it("reads JSON text into a value, or says the text is no JSON", () => {
		expect(parseJson('{"a":1}')).toEqual(ok({ a: 1 }));
		expect(parseJson("<html>proxy</html>")).toEqual(err("not JSON"));
		expect(parseJson("")).toEqual(err("not JSON"));
	});

	it("takes the result last, so each function can stand in a pipe", () => {
		expect(map(double)(ok(3))).toEqual(ok(6));
		expect(map(double)(err("no"))).toEqual(err("no"));
		expect(flatMap(half)(ok(4))).toEqual(ok(2));
		expect(flatMap(half)(ok(3))).toEqual(err("3 is odd"));
		expect(fold(String, (e: string) => `refused: ${e}`)(err("no"))).toBe("refused: no");
		expect(defaultWith(() => 0)(err("no"))).toBe(0);
		expect(defaultWith(() => 0)(ok(5))).toBe(5);
	});
});

describe("pipe and flow, F#'s |> and >>", () => {
	const add1 = (n: number): number => n + 1;
	const double = (n: number): number => n * 2;

	it("pipe runs a value through each step, in the order written", () => {
		expect(pipe(3)).toBe(3);
		expect(pipe(3, add1, double)).toBe(8);
		expect(pipe(3, double, add1, String)).toBe("7");
	});

	it("flow joins steps into one function that runs them in the order written", () => {
		expect(flow(add1, double)(3)).toBe(8);
		expect(flow(double, add1, String)(3)).toBe("7");
	});
});

describe("US-23 the area comes with the page", () => {
	const body = {
		name: "Macau",
		bounds: [113.52, 22.1, 113.61, 22.22],
		home: { center: { lng: 113.55, lat: 22.19 }, zoom: 15 },
	};

	it("reads the area the service sends", () => {
		expect(parseArea(body)).toEqual({ ok: true, value: MACAU });
	});

	it("refuses an area with no name, a box turned inside out, or a home level outside 0 to 22", () => {
		expect(parseArea({ ...body, name: " " }).ok).toBe(false);
		expect(parseArea({ ...body, bounds: [113.61, 22.1, 113.52, 22.22] }).ok).toBe(false);
		expect(parseArea({ ...body, home: { ...body.home, zoom: 23 } }).ok).toBe(false);
		expect(parseArea({ ...body, home: { zoom: 15 } }).ok).toBe(false);
		expect(parseArea([body]).ok).toBe(false);
	});

	it("reads the page's text into an area, or opens on Macau when the text holds none it can read", () => {
		expect(areaOf(JSON.stringify({ ...body, name: "Taipa" }))).toEqual({ ...MACAU, name: "Taipa" });
		expect(areaOf("")).toEqual(MACAU);
		expect(areaOf("<not json>")).toEqual(MACAU);
		expect(areaOf(JSON.stringify({ ...body, name: "" }))).toEqual(MACAU);
	});

	it("reads a box only as four numbers in degrees, west before east and south before north", () => {
		expect(readBounds([1, 2, 3, 4])).toEqual([1, 2, 3, 4]);
		for (const value of [[1, 2, 3], [1, 2, "3", 4], [1, 2, 3, 95], [3, 2, 1, 4], null]) expect(readBounds(value)).toBeNull();
	});
});
