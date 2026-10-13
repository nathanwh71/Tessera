// @vitest-environment jsdom
import type { Map as MapLibreMap } from "maplibre-gl";
import { describe, expect, it, vi } from "vitest";
import { FakeMap } from "../testing/fake-maplibre";
import { type MapEvents, attachEvents } from "./maplibre";

/** A fake map with every handler recorded, so a test fires MapLibre's events and reads what the page heard. */
function wired() {
	const map = new FakeMap({});
	const events: MapEvents = {
		onMove: vi.fn(),
		onIdle: vi.fn(),
		onVisitorMove: vi.fn(),
		onPlacesChanged: vi.fn(),
		onPointer: vi.fn(),
		onClusterClick: vi.fn(),
		onLoad: vi.fn(),
	};
	const stop = attachEvents(map as unknown as MapLibreMap, events);
	return { map, events, stop };
}

describe("the map's events, wired at the edge", () => {
	it("tells the visitor's own move apart from the map's: only an event with an originalEvent is the visitor's", () => {
		const { map, events } = wired();
		map.fire("movestart", {});
		expect(events.onVisitorMove).not.toHaveBeenCalled();
		map.fire("movestart", { originalEvent: new Event("pointerdown") });
		expect(events.onVisitorMove).toHaveBeenCalledTimes(1);
	});

	it("reports the pointer's position, and null when it leaves the map", () => {
		const { map, events } = wired();
		map.fire("mousemove", { lngLat: { lng: 113.5, lat: 22.2 } });
		expect(events.onPointer).toHaveBeenLastCalledWith({ lng: 113.5, lat: 22.2 });
		map.fire("mouseout");
		expect(events.onPointer).toHaveBeenLastCalledWith(null);
	});

	it("reports a click on a cluster with its id and point, and ignores a click with no cluster under it", () => {
		const { map, events } = wired();
		map.fire("click", { features: [] });
		expect(events.onClusterClick).not.toHaveBeenCalled();
		map.fire("click", { features: [{ geometry: { type: "Point", coordinates: [113.5, 22.2] }, properties: { cluster_id: 7 } }] });
		expect(events.onClusterClick).toHaveBeenCalledWith(7, [113.5, 22.2]);
	});

	it("reports that the places changed only for the places source's own content", () => {
		const { map, events } = wired();
		map.fire("sourcedata", { sourceId: "tiles:food.cafes", sourceDataType: "content" });
		map.fire("sourcedata", { sourceId: "places", sourceDataType: "metadata" });
		expect(events.onPlacesChanged).not.toHaveBeenCalled();
		map.fire("sourcedata", { sourceId: "places", sourceDataType: "content" });
		expect(events.onPlacesChanged).toHaveBeenCalledTimes(1);
	});

	it("passes move and idle on, and hears nothing after stop", () => {
		const { map, events, stop } = wired();
		map.fire("move");
		map.fire("idle");
		expect(events.onMove).toHaveBeenCalledTimes(1);
		expect(events.onIdle).toHaveBeenCalledTimes(1);
		stop();
		map.fire("idle");
		expect(events.onIdle).toHaveBeenCalledTimes(1);
	});
});
