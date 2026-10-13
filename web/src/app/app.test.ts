import { describe, expect, it, vi } from "vitest";
import { MACAU } from "../shared";
import { Msg, init, update } from ".";

describe("US-23 c3 one model, one pure update", () => {
	it("starts from one model and changes it only through the update's handlers", () => {
		const [model] = init({ ports: { fitBounds: vi.fn(), jumpTo: vi.fn() }, area: MACAU });
		const handler = update.toggleUnits;
		const [change] = handler({ name: "toggleUnits" }, model, { ports: { fitBounds: vi.fn(), jumpTo: vi.fn() }, area: MACAU }, {} as never);
		expect(change).toEqual({ units: "imperial" });
		// The update returned a change and left the old model as it was.
		expect(model.units).toBe("metric");
		expect(Msg.toggleUnits()).toEqual({ name: "toggleUnits" });
	});
});
