// shared/geo.ts: positions, views and boxes on the map, in degrees of WGS 84, the longitude and latitude system GPS uses.

import { isFiniteNumber, isRecord } from "./json";

/** A position in WGS 84 degrees: longitude east of Greenwich, then latitude north of the equator. */
export interface LngLat {
	readonly lng: number;
	readonly lat: number;
}

/**
 * A web-map zoom level (UI-09). It is a number with a brand, a mark that exists for the compiler alone,
 * as F#'s units of measure are: a plain number, or MapLibre's zoom, cannot stand where a level belongs.
 */
export type Level = number & { readonly unit: "level" };

/** Marks a number as a level, as F#'s `15<level>` would. */
export const level = (n: number): Level => n as Level;

/** Where the map looks: a centre and a web-map zoom level (UI-09), where level 0 shows the world in 256 pixels and each level up doubles the scale. */
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

/** Reads `[west, south, east, north]` in degrees; null for anything else, or for a box turned inside out (west past east, or south past north). */
export function readBounds(value: unknown): Bounds | null {
	if (!Array.isArray(value) || value.length !== 4 || !value.every(isFiniteNumber)) return null;
	const [west, south, east, north] = value as [number, number, number, number];
	const inRange = Math.abs(west) <= 180 && Math.abs(east) <= 180 && Math.abs(south) <= 90 && Math.abs(north) <= 90;
	return inRange && west < east && south < north ? [west, south, east, north] : null;
}

/** Reads `{lng, lat}` in degrees; null for anything else. */
export function readLngLat(value: unknown): LngLat | null {
	if (!isRecord(value)) return null;
	const { lng, lat } = value;
	return isFiniteNumber(lng) && isFiniteNumber(lat) && Math.abs(lng) <= 180 && Math.abs(lat) <= 90 ? { lng, lat } : null;
}
