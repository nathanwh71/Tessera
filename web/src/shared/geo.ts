// shared/geo.ts: positions, views and boxes on the map, in degrees of WGS 84, the longitude and
// latitude system GPS uses. The shared kernel holds them, and the area, the map's model and its
// controls all speak in them (UI-09, US-23).

import * as v from "valibot";

/** A position in WGS 84 degrees: longitude east of Greenwich, then latitude north of the equator. */
export interface LngLat {
	readonly lng: number;
	readonly lat: number;
}

/**
 * A web-map zoom level (UI-09): level 0 shows the world in 256 pixels, and each level up doubles the
 * scale. It is a branded type: a number with a brand, a mark that exists for the compiler alone, as
 * the units of measure of F#, a functional language, are. A plain number, or the zoom of MapLibre,
 * the library that draws the map, cannot stand where a level belongs (ADR 0004).
 */
export type Level = number & { readonly unit: "level" };

/** Marks a number as a level, as F#'s `15<level>` would. */
export const level = (n: number): Level => n as Level;

/** Where the map looks: a centre and a web-map zoom level (UI-09). */
export interface View {
	readonly center: LngLat;
	readonly zoom: Level;
}

/** A box on the map: west, south, east, north. */
export type Bounds = readonly [west: number, south: number, east: number, north: number];

/** The lowest web-map level (UI-09). */
export const MIN_ZOOM = level(0);
/** The highest web-map level (UI-09). */
export const MAX_ZOOM = level(22);

/** The level, held within 0 to 22: a value outside the range becomes the nearest end. */
export function clampZoom(zoom: number): Level {
	return level(Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, zoom)));
}

/** A number of degrees from `-most` to `most`, such as a longitude, from -180 to 180. */
const degrees = (most: number) => v.pipe(v.number(), v.finite(), v.minValue(-most), v.maxValue(most));

/** A position in degrees: a longitude from -180 to 180 and a latitude from -90 to 90. */
export const LngLatSchema = v.object({ lng: degrees(180), lat: degrees(90) });

/**
 * A box: exactly four numbers, `[west, south, east, north]`, in degrees, west before east and south
 * before north, so a box turned inside out is refused.
 */
export const BoundsSchema = v.pipe(
	v.strictTuple([degrees(180), degrees(90), degrees(180), degrees(90)]),
	v.check(([west, south, east, north]) => west < east && south < north),
);

/** Reads a box; null for anything else. */
export const readBounds = (value: unknown): Bounds | null => (v.is(BoundsSchema, value) ? value : null);
