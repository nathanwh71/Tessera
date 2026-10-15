// map/style.test.ts: the style, the description of what MapLibre, the library that draws the map,
// draws, checked as a value (US-23, UI-11, UI-12, UI-15, UI-17). `buildStyle` is pure, reading only
// its input, so each test builds a style and reads its sources, the data to draw from, and its
// layers, which say how to draw it. It runs under vitest, the test runner, with no browser.

import type { LayerSpecification, SymbolLayerSpecification } from "maplibre-gl";
import { describe, expect, it } from "vitest";
import { themeFrom } from "../design";
import { LAYER_IDS } from "../layers";
import { LAYER, NAMES_FROM_LEVEL, SOURCE, type SourceFeature, type StyleInput, TILE_MAX_LEVEL, buildStyle, placeCollection, readImageName, toMapZoom } from ".";

// The theme's colours as the names of their CSS variables, the named values the page's styles hold:
// the tests check where colours go, not what they are.
const THEME = themeFrom((variable) => `var(${variable})`);

/**
 * A style for the nine ***layers***, landed, with only the inputs a test names changed, so each test
 * states just what it is about.
 */
function style(changes: Partial<StyleInput> = {}) {
	return buildStyle({
		origin: "http://tessera.office.lan",
		visible: LAYER_IDS,
		landed: true,
		theme: THEME,
		fonts: { latin: "/assets/roboto-latin.woff2", latinExt: "/assets/roboto-latin-ext.woff2" },
		...changes,
	});
}

/** A style layer by its id, or undefined, so a test can check that a layer is missing too. */
const layerById = (layers: readonly LayerSpecification[], id: string): LayerSpecification | undefined => layers.find((l) => l.id === id);

/**
 * A symbol layer by its id; it throws when there is none, so a test fails with a clear message
 * rather than on a missing field later.
 */
function symbol(id: string, changes: Partial<StyleInput> = {}): SymbolLayerSpecification {
	const found = layerById(style(changes).layers, id);
	if (found?.type !== "symbol") throw new Error(`no symbol layer '${id}'`);
	return found;
}

/**
 * A café at one point with the given id, named after it; the tests that use it vary only the ids,
 * since the order of names rests on ids alone (UI-17, criterion 2).
 */
function feature(id: string): SourceFeature {
	return { properties: { id, name: id, type: "Café" }, geometry: { type: "Point", coordinates: [113.55, 22.19] } };
}

describe("US-23 the map's sources", () => {
	it("builds each layer's tile address on the page's own origin, one vector source for each of the nine", () => {
		const sources = style().sources;
		expect(LAYER_IDS.map((id) => sources[SOURCE.tiles(id)])).toEqual(
			LAYER_IDS.map((id) => ({
				type: "vector",
				tiles: [`http://tessera.office.lan/${id}/{z}/{x}/{y}.pbf`],
				minzoom: 0,
				maxzoom: toMapZoom(TILE_MAX_LEVEL),
			})),
		);
	});

	it("loads only the layer the page opened on, at /{layer}.html", () => {
		const sources = Object.keys(style({ visible: ["food.cafes"] }).sources);
		expect(sources.toSorted()).toEqual([SOURCE.places, SOURCE.tiles("food.cafes")].toSorted());
	});
});

describe("UI-15 a plain background", () => {
	it("draws nothing beneath the places but the plain background", () => {
		const layers = style().layers;
		expect(layers[0]).toEqual({ id: LAYER.background, type: "background", paint: { "background-color": "var(--color-surface)" } });
		expect(layers.filter((l) => !["background", "symbol"].includes(l.type)).every((l) => l.id.startsWith("feed:"))).toBe(true);
		// Names draw from the self-hosted Roboto, so the style asks no glyph server for anything.
		expect(style().glyphs).toBeUndefined();
	});

	it("loads the tiles of each layer that is on through a layer that draws nothing", () => {
		const feed = layerById(style().layers, LAYER.feed("food.cafes"));
		expect(feed).toMatchObject({ type: "circle", "source-layer": "food.cafes", paint: { "circle-radius": 0, "circle-opacity": 0 } });
		expect(layerById(style({ visible: ["food.cafes"] }).layers, LAYER.feed("culture.museums"))).toBeUndefined();
	});

	it("keeps the map's own pins and names unseen until the dropping pins land", () => {
		expect(symbol(LAYER.pins, { landed: false }).paint).toEqual({ "icon-opacity": 0 });
		expect(symbol(LAYER.names, { landed: false }).paint).toMatchObject({ "text-opacity": 0, "icon-opacity": 0 });
		expect(symbol(LAYER.pins).paint).toEqual({ "icon-opacity": 1 });
		// Clusters never wait.
		expect(symbol(LAYER.clusters, { landed: false }).paint).toBeUndefined();
	});
});

describe("UI-12 pins in their group's colour and shape", () => {
	it("draws each place with its group's pin", () => {
		expect(symbol(LAYER.pins).layout?.["icon-image"]).toEqual(["concat", "pin:", ["get", "group"]]);
		expect(readImageName("pin:transport")).toEqual({ kind: "pin", group: "transport" });
		expect(readImageName("pin:bakeries")).toBeNull();
		expect(readImageName("cluster:3-0-1-0")).toEqual({ kind: "cluster", counts: { food: 3, outdoors: 0, culture: 1, transport: 0 } });
	});
});

describe("UI-11 names", () => {
	it("shows names from level 16, and none inside a cluster", () => {
		const names = symbol(LAYER.names);
		expect(names.minzoom).toBe(toMapZoom(NAMES_FROM_LEVEL));
		expect(names.layout?.["text-field"]).toEqual(["get", "name"]);
		expect(names.filter).toEqual(["!", ["has", "point_count"]]);
	});

	it("writes names in light text on a dark tooltip, in the self-hosted Roboto", () => {
		expect(symbol(LAYER.names).paint).toMatchObject({ "text-color": "var(--color-inverse-on-surface)" });
		expect(symbol(LAYER.names).layout).toMatchObject({ "icon-image": "tooltip", "icon-text-fit": "both" });
		expect(readImageName("tooltip")).toEqual({ kind: "tooltip" });
		expect(style()["font-faces"]?.["Roboto Regular"]).toEqual([
			expect.objectContaining({ url: "/assets/roboto-latin.woff2" }),
			expect.objectContaining({ url: "/assets/roboto-latin-ext.woff2" }),
		]);
	});
});

describe("UI-17 names that never overlap", () => {
	it("gives the names four anchors, and lets a name give way while its pin stays", () => {
		const anchors = symbol(LAYER.names).layout?.["text-variable-anchor-offset"];
		// MapLibre names the side of the text that touches the place: 'left' puts the name on the right.
		expect(Array.isArray(anchors) && anchors.filter((a) => typeof a === "string")).toEqual(["left", "right", "bottom", "top"]);
		const pins = symbol(LAYER.pins);
		expect(pins.layout).toMatchObject({ "icon-allow-overlap": true });
		expect(pins.layout?.["text-field"]).toBeUndefined();
		// The name gives way alone: its own layer, never allowed to overlap, so it is optional while
		// its pin stays.
		expect(symbol(LAYER.names).layout?.["text-allow-overlap"]).toBeUndefined();
		expect(symbol(LAYER.names).layout).toMatchObject({ "icon-ignore-placement": true, "text-padding": 9 });
	});

	it("places every icon before any name, so no name covers an icon", () => {
		const ids = style().layers.map((l) => l.id);
		// MapLibre places symbols from the top layer down.
		expect(ids.slice(-3)).toEqual([LAYER.names, LAYER.pins, LAYER.clusters]);
	});

	it("orders names between layers by the map's order, then by id, whatever order the tiles bring", () => {
		const loaded = [
			{ layerId: "food.cafes", features: [feature("cafes-002"), feature("cafes-001"), feature("cafes-002")] },
			{ layerId: "outdoors.parks", features: [feature("parks-001")] },
		] as const;
		const places = placeCollection(loaded).features.map((f) => [f.properties.id, f.properties.sort, f.properties.one]);
		expect(places).toEqual([["cafes-001", 0, "Café"], ["cafes-002", 1, "Café"], ["parks-001", 1_000_000, "Park"]]);
		expect(symbol(LAYER.names).layout).toMatchObject({ "symbol-sort-key": ["get", "sort"], "symbol-z-order": "source" });
	});

	it("drops features that are not points or carry no id", () => {
		const odd: SourceFeature[] = [{ properties: { name: "no id" }, geometry: { type: "Point", coordinates: [113.55, 22.19] } }, { properties: { id: "line" }, geometry: { type: "LineString", coordinates: [] } }];
		expect(placeCollection([{ layerId: "food.cafes", features: odd }]).features).toEqual([]);
	});
});
