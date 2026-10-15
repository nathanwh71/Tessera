// app/app.test.ts: checks that the page has one model, the value that holds everything it shows, and
// one pure update, which makes the new model from the old and a message without changing the old
// (US-23, criterion 3). It runs under vitest, the test runner, with values alone: the ports, the
// functions that would move the map, are fakes made by `vi.fn()`.

import { describe, expect, it, vi } from "vitest";
import { MACAU } from "../shared";
import { Msg, init, update } from ".";

describe("US-23 c3 one model, one pure update", () => {
	it("starts from one model and changes it only through the update's handlers", () => {
		const [model] = init({ ports: { fitBounds: vi.fn(), jumpTo: vi.fn() }, area: MACAU });
		const handler = update.toggleUnits;
		// The last argument carries helpers a handler may use to send further messages; toggleUnits
		// uses none, so an empty stand-in serves.
		const [change] = handler({ name: "toggleUnits" }, model, { ports: { fitBounds: vi.fn(), jumpTo: vi.fn() }, area: MACAU }, {} as never);
		expect(change).toEqual({ units: "imperial" });
		// ADR 0004, immutability: the update returned a change and left the old model as it was.
		expect(model.units).toBe("metric");
		expect(Msg.toggleUnits()).toEqual({ name: "toggleUnits" });
	});
});
