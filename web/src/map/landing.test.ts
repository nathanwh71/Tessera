import { describe, expect, it } from "vitest";
import { type Landing, type LandingEvent, START, step } from "./landing";

/** A pin in view, as the map reports it: where it lands and its group. */
const pin = (key: string) => ({ key, x: 10, y: 20, group: "food" as const });

const run = (events: readonly LandingEvent[], from: Landing = START): Landing => events.reduce(step, from);

describe("UI-15 the pins' landing, as a pure state machine", () => {
	it("waits for the first places before it drops anything", () => {
		expect(run([{ name: "mapIdle", pins: [pin("a")], reduced: false }])).toEqual(START);
	});

	it("drops the pins in view once the places are in and the map rests, each with its delay", () => {
		const landing = run([{ name: "placesFed" }, { name: "mapIdle", pins: [pin("a"), pin("b")], reduced: false }]);
		expect(landing.stage).toBe("dropping");
		if (landing.stage !== "dropping") return;
		expect(landing.drops.map((d) => d.key)).toEqual(["a", "b"]);
		expect(landing.drops.map((d) => d.delay)).toEqual([0, 30]);
		expect(landing.ms).toBeGreaterThan(30);
	});

	it("lands at once when no pin is in view, or when the visitor asks for less motion (criterion 8)", () => {
		expect(run([{ name: "placesFed" }, { name: "mapIdle", pins: [], reduced: false }])).toEqual({ stage: "landed" });
		expect(run([{ name: "placesFed" }, { name: "mapIdle", pins: [pin("a")], reduced: true }])).toEqual({ stage: "landed" });
	});

	it("lands when the drops finish, and ignores a second rest of the map while dropping", () => {
		const dropping = run([{ name: "placesFed" }, { name: "mapIdle", pins: [pin("a")], reduced: false }]);
		expect(step(dropping, { name: "mapIdle", pins: [pin("a"), pin("b")], reduced: false })).toBe(dropping);
		expect(step(dropping, { name: "dropsDone" })).toEqual({ stage: "landed" });
	});

	it("lands at once when the visitor moves the map, before or during the drop: a visitor who moves needs no show", () => {
		expect(run([{ name: "visitorMoved" }])).toEqual({ stage: "landed" });
		const dropping = run([{ name: "placesFed" }, { name: "mapIdle", pins: [pin("a")], reduced: false }]);
		expect(step(dropping, { name: "visitorMoved" })).toEqual({ stage: "landed" });
	});

	it("lands when the wait for the first places runs out, so a map with no places still shows its pins", () => {
		expect(run([{ name: "timedOut" }])).toEqual({ stage: "landed" });
	});

	it("stays landed whatever happens next", () => {
		const landed: Landing = { stage: "landed" };
		for (const event of [{ name: "placesFed" }, { name: "dropsDone" }, { name: "timedOut" }] as const) expect(step(landed, event)).toBe(landed);
		expect(step(landed, { name: "mapIdle", pins: [pin("a")], reduced: false })).toBe(landed);
	});
});
