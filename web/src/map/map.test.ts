// map/map.test.ts: the map's model and update (US-23, UI-09, UI-15, UI-19), tested with values
// alone under vitest, the test runner. The update is the function that makes the new model, the
// value that holds everything the page shows, from the old one and a message that says what
// happened. The ports, the functions through which it moves the map, are fakes made by `vi.fn()`,
// so each test reads what the update asked of the map.

import { createUpdateArgsFactory, execCmd, getUpdateFn, initAndExecCmd } from "react-elmish/testing";
import { describe, expect, it, vi } from "vitest";
import { LAYER_IDS } from "../layers";
import { MACAU, level } from "../shared";
import { FRAME_MAX_LEVEL, type Message, type Model, Msg, type Props, init, noticeOf, tileProblem, update } from ".";

/**
 * Fresh props for each test, with fake ***ports*** from vi.fn() that record each move the
 * ***update*** asks for; `openLayer` stands for the /{layer}.html address (US-23, criterion 2).
 */
const props = (openLayer?: Props["openLayer"]): Props => ({
	ports: { fitBounds: vi.fn(), jumpTo: vi.fn() },
	area: MACAU,
	...(openLayer === undefined ? {} : { openLayer }),
});
// react-elmish's helpers: `updateFn` runs one message through the update, and `args` builds its
// arguments from a fresh model, changed only where a test says, so no test leaks into the next.
const updateFn = getUpdateFn(update);
const args = createUpdateArgsFactory<Props, Model, Message>(() => init(props())[0], () => props());

describe("US-23 open the map of the area", () => {
	it("opens on the area's home view with the nine layers on, and asks for no list of layers", async () => {
		const p = props();
		const [model, messages] = await initAndExecCmd(init, p);
		expect(model.view).toEqual(MACAU.home);
		expect(model.visible).toEqual(LAYER_IDS);
		expect(messages).toEqual([]);
		expect(p.ports.fitBounds).not.toHaveBeenCalled();
		expect(p.ports.jumpTo).not.toHaveBeenCalled();
	});

	it("opens /food.cafes.html with the cafés alone, framing the area that holds them", async () => {
		const p = props("food.cafes");
		const [model] = await initAndExecCmd(init, p);
		expect(model.visible).toEqual(["food.cafes"]);
		expect(p.ports.fitBounds).toHaveBeenCalledWith(MACAU.bounds, FRAME_MAX_LEVEL);
	});

	it("shows a problem document's detail, or its title where it has none", () => {
		const detail = { type: "about:blank", title: "Not Found", status: 404, detail: "No layer 'food.cafes' is published." };
		expect(updateFn(...args(Msg.tileFailed(detail)))[0]).toEqual({ notice: "No layer 'food.cafes' is published." });
		expect(noticeOf({ type: "about:blank", title: "Service Unavailable", status: 503 })).toBe("Service Unavailable");
	});

	it("leaves a failed tile's detail in the model, read from the answer's body", () => {
		const body = JSON.stringify({ type: "about:blank", title: "Service Unavailable", status: 503, detail: "The list of layers has not loaded yet." });
		const [model] = updateFn(...args(Msg.tileFailed(tileProblem(503, "Service Unavailable", body))));
		expect(model.notice).toBe("The list of layers has not loaded yet.");
	});

	it("makes words from the status alone when the body is no problem document", () => {
		expect(tileProblem(502, "Bad Gateway", "<html>proxy</html>")).toEqual({ type: "about:blank", title: "Bad Gateway", status: 502 });
		expect(tileProblem(500, "", "")).toEqual({ type: "about:blank", title: "HTTP 500", status: 500 });
	});
});

describe("UI-09 levels", () => {
	it("doubles and halves the scale one level at a time, through the map's port", async () => {
		const p = props();
		const [, command] = updateFn(...args(Msg.zoomIn(), { view: { ...MACAU.home, zoom: level(16.4) } }, p));
		await execCmd(command);
		expect(p.ports.jumpTo).toHaveBeenCalledWith({ ...MACAU.home, zoom: 17.4 });
		const [, out] = updateFn(...args(Msg.zoomOut(), { view: { ...MACAU.home, zoom: level(15) } }, p));
		await execCmd(out);
		expect(p.ports.jumpTo).toHaveBeenLastCalledWith({ ...MACAU.home, zoom: 14 });
	});

	it("keeps the level within 0 to 22", () => {
		expect(updateFn(...args(Msg.zoomIn(), { view: { ...MACAU.home, zoom: level(22) } }))[1]).toBeUndefined();
		expect(updateFn(...args(Msg.zoomOut(), { view: { ...MACAU.home, zoom: level(0) } }))[1]).toBeUndefined();
		expect(updateFn(...args(Msg.viewChanged({ ...MACAU.home, zoom: level(24) })))[0].view?.zoom).toBe(22);
	});
});

describe("UI-19 units and coordinates", () => {
	it("switches between metric and imperial on a click", () => {
		expect(updateFn(...args(Msg.toggleUnits()))[0]).toEqual({ units: "imperial" });
	});

	it("follows the pointer, and forgets it when the pointer leaves", () => {
		const at = { lng: 113.55, lat: 22.19 };
		expect(updateFn(...args(Msg.pointerMoved(at)))[0]).toEqual({ pointer: at });
		expect(updateFn(...args(Msg.pointerMoved(null)))[0]).toEqual({ pointer: null });
	});
});

describe("UI-15 the pins drop in once", () => {
	it("opens with the pins still to land, and records their landing", () => {
		expect(init(props())[0].landed).toBe(false);
		expect(updateFn(...args(Msg.pinsLanded()))[0]).toEqual({ landed: true });
	});
});

describe("US-23 criterion 9: a browser that cannot draw the map", () => {
	it("opens able to draw, records that it cannot, and records that it can again", () => {
		expect(init(props())[0].drawable).toBe(true);
		expect(updateFn(...args(Msg.cannotDraw()))[0]).toEqual({ drawable: false });
		expect(updateFn(...args(Msg.canDrawAgain()))[0]).toEqual({ drawable: true });
	});
});
