// layers/layers.test.ts: the nine layers, their tile addresses and the layer a page's address names
// (US-23, US-24), checked with values alone under vitest, the test runner.

import { describe, expect, it } from "vitest";
import { LAYERS, LAYER_IDS, groupOf, layerFromPath, tileUrl } from ".";

describe("US-23 the map names the nine layers in its own code", () => {
	it("names the nine layers of US-24, in four groups, in the map's order", () => {
		expect(LAYER_IDS).toEqual([
			"food.cafes",
			"food.restaurants",
			"food.markets",
			"outdoors.parks",
			"outdoors.playgrounds",
			"culture.museums",
			"culture.libraries",
			"transport.trains",
			"transport.buses",
		]);
		expect(LAYERS.every((l) => l.id.startsWith(`${l.group}.`))).toBe(true);
		expect(groupOf("transport.buses")).toBe("transport");
		expect(groupOf("public.roads")).toBeUndefined();
	});

	it("builds each tile's address on the page's own origin", () => {
		expect(tileUrl("http://tessera.office.lan", "food.cafes")).toBe("http://tessera.office.lan/food.cafes/{z}/{x}/{y}.pbf");
		expect(tileUrl("http://localhost:7800/", "culture.museums")).toBe("http://localhost:7800/culture.museums/{z}/{x}/{y}.pbf");
	});

	it("reads the layer a page's path names, and only one of the nine", () => {
		expect(layerFromPath("/food.cafes.html")).toBe("food.cafes");
		expect(layerFromPath("/")).toBeUndefined();
		expect(layerFromPath("/public.roads.html")).toBeUndefined();
		expect(layerFromPath("/food.cafes.json")).toBeUndefined();
		expect(layerFromPath("/food.cafes/1/0/0.pbf")).toBeUndefined();
	});
});
