// map/places.ts: the places MapLibre, the library that draws the map, has loaded, joined into one list
// for the clustered source (UI-10, UI-17, ADR 0005). A source is the data a map layer draws from; a
// cluster is one mark that stands for several places close together. A pure function of the map
// module, which reads only its inputs; maplibre.ts calls it each time the loaded tiles change.
//
// MapLibre clusters only a GeoJSON source (GeoJSON is the form of points and shapes in JSON, the text
// form of a value that programs exchange), and the service sends vector tiles, square pieces of the
// map holding points rather than pictures (ADR 0005). So the map reads the features, the points with
// their properties, of the tiles it has loaded for each layer that is on, and feeds them here. A
// point near a tile's edge arrives in each tile whose margin holds it, which is why each id is kept
// once.

import * as v from "valibot";
import type { GroupKey } from "../design";
import { type LayerId, layerOf } from "../layers";

/**
 * A property value as a ***vector tile*** carries it: text, a number, true or false, or null. Only
 * these pass to the clustered source, so each place there holds plain values alone.
 */
export type PropertyValue = string | number | boolean | null;

/** A feature as `map.querySourceFeatures` returns it, reduced to what this module reads. */
export interface SourceFeature {
	readonly properties: Readonly<Record<string, unknown>>;
	readonly geometry: { readonly type: string; readonly coordinates?: unknown };
}

/**
 * The features loaded for one ***layer*** that is on, kept with its id, since the id gives each place
 * its group and its rank in the map's order.
 */
export interface LoadedLayer {
	readonly layerId: LayerId;
	readonly features: readonly SourceFeature[];
}

/**
 * A place's properties on the clustered source: the tile's own, plus its layer, its group and its
 * rank in the fixed order.
 */
export type PlaceProperties = Readonly<Record<string, PropertyValue>> & {
	readonly id: string;
	readonly name: string;
	readonly layerId: LayerId;
	readonly group: GroupKey;
	/** One of the layer's places in words, such as 'Café', for the pin's label. */
	readonly one: string;
	/**
	 * UI-17, criterion 2: a lower number wins the space for its name; layers in the map's order
	 * first, then ids within a layer.
	 */
	readonly sort: number;
};

/**
 * One place on the clustered source, the source MapLibre groups into ***clusters***. Always a point,
 * since MapLibre clusters points alone.
 */
export interface PlaceFeature {
	readonly type: "Feature";
	readonly geometry: { readonly type: "Point"; readonly coordinates: readonly [number, number] };
	readonly properties: PlaceProperties;
}

/** The places for the clustered source, as GeoJSON, the one form MapLibre clusters (ADR 0005). */
export interface PlaceCollection {
	readonly type: "FeatureCollection";
	readonly features: readonly PlaceFeature[];
}

/**
 * The step between layers in a sort key, larger than any layer's count of places, so no two layers'
 * keys overlap.
 */
const LAYER_STEP = 1_000_000;

/**
 * True for a plain value. A feature's properties arrive typed as unknown, so valuesOf keeps only
 * these and passes nothing else to the clustered source.
 */
const isValue = (v: unknown): v is PropertyValue =>
	v === null || typeof v === "string" || typeof v === "number" || typeof v === "boolean";

/**
 * A point's shape: the type "Point" and two numbers, longitude then latitude; a third, a height, is
 * left out, since a pin marks only where a place stands on the ground.
 */
const PointGeometry = v.object({ type: v.literal("Point"), coordinates: v.tuple([v.number(), v.number()]) });

/** The point a feature stands at, in degrees, or null for any other shape: a line, an area, or none. */
export function pointOf(feature: { readonly geometry: unknown }): [number, number] | null {
	const read = v.safeParse(PointGeometry, feature.geometry);
	return read.success ? read.output.coordinates : null;
}

/**
 * A property as text: a vector tile may carry an id as a number, yet the code compares and joins ids
 * as text, so a number becomes text and anything else the empty text, which marks it missing.
 */
const textOf = (v: unknown): string => (typeof v === "string" ? v : typeof v === "number" ? String(v) : "");

/**
 * The tile's properties that are plain values, with the place's id and name as text, so each place
 * can be kept once by its id and labelled by its name.
 */
function valuesOf(feature: SourceFeature): Readonly<Record<string, PropertyValue>> & { id: string; name: string } {
	const values = Object.fromEntries(Object.entries(feature.properties).filter(([, v]) => isValue(v)));
	return { ...values, id: textOf(feature.properties["id"]), name: textOf(feature.properties["name"]) };
}

/**
 * Orders two places by id, comparing the text code by code rather than by the browser's language, so
 * every browser ranks the same ids the same way and the same name wins (UI-17, criterion 2).
 */
const byId = (a: { values: { id: string } }, b: { values: { id: string } }): number =>
	a.values.id < b.values.id ? -1 : a.values.id > b.values.id ? 1 : 0;

/**
 * Joins the loaded layers into one list for the clustered source.
 *
 * Takes the layers in the map's order. Keeps each place once, by its `id`, and drops features that are
 * not points or carry no id. Each place gains its layer, its group and a sort key that ranks layers in
 * order, then ids within a layer, so the same name wins every time (UI-17, criterion 2).
 */
export function placeCollection(loaded: readonly LoadedLayer[]): PlaceCollection {
	const features = loaded.flatMap(({ layerId, features: raw }, layerIndex) => {
		const layer = layerOf(layerId);
		// A layer the page does not name has no group to colour its pins, so its places are left out.
		if (layer === undefined) return [];
		const unique = new Map(
			raw.flatMap((f) => {
				const point = pointOf(f);
				const values = valuesOf(f);
				return point === null || values.id === "" ? [] : [[values.id, { point, values }] as const];
			}),
		);
		return [...unique.values()].toSorted(byId).map(
			({ point, values }, rank): PlaceFeature => ({
				type: "Feature",
				geometry: { type: "Point", coordinates: point },
				properties: { ...values, layerId, group: layer.group, one: layer.one, sort: layerIndex * LAYER_STEP + rank },
			}),
		);
	});
	return { type: "FeatureCollection", features };
}

/**
 * A fingerprint of a list's places, their ids joined into one string, so the map resets its source
 * only when they change (ADR 0005, step 5).
 */
export function fingerprint(collection: PlaceCollection): string {
	return collection.features.map((f) => f.properties.id).join("|");
}
