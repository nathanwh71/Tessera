import { createUpdateArgsFactory, execCmd, getUpdateFn, initAndExecCmd } from "react-elmish/testing";
import { describe, expect, it, vi } from "vitest";
import { LAYER_IDS } from "../layers";
import { MACAU, level } from "../shared";
import { FRAME_MAX_LEVEL, type Message, type Model, Msg, type Props, init, noticeOf, tileProblem, update } from ".";

const props = (openLayer?: Props["openLayer"]): Props => ({
	ports: { fitBounds: vi.fn(), jumpTo: vi.fn() },
	area: MACAU,
	...(openLayer === undefined ? {} : { openLayer }),
});
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
