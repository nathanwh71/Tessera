// shared/area.ts: the place the map serves, a setting of the service (`Area` in tessera.json) that the
// office sets to Macau. The service writes it into the page itself, so the map needs no extra request.

import { type Bounds, type View, level, readBounds, readLngLat } from "./geo";
import { isRecord, parseJson } from "./json";
import { pipe } from "./pipe";
import { type Result, defaultWith, err, flatMap, ok } from "./result";

/** The area as the page carries it, in the `tessera-area` script block the service writes. */
export interface Area {
	readonly name: string;
	/** West, south, east, north, in degrees of WGS 84, the longitude and latitude system GPS uses. */
	readonly bounds: Bounds;
	/** Where the map opens (US-23, criterion 1). */
	readonly home: View;
}

/** Macau's settings, as the service sends them; the page falls back on them when it carries no area, as under `vite` alone. */
export const MACAU = {
	name: "Macau",
	bounds: [113.52, 22.1, 113.61, 22.22],
	home: { center: { lng: 113.55, lat: 22.19 }, zoom: level(15) },
} as const satisfies Area;

/**
 * Reads the area the page carries, and returns it or the reason it was refused.
 *
 * Accepts `{name, bounds: [w, s, e, n], home: {center: {lng, lat}, zoom}}`; refuses a missing name,
 * a box whose west is not less than its east or whose south is not less than its north, and a home
 * zoom level outside 0 to 22.
 */
export function parseArea(json: unknown): Result<Area> {
	if (!isRecord(json)) return err("the area is not a JSON object");
	const name = json["name"];
	const bounds = readBounds(json["bounds"]);
	const home = json["home"];
	if (typeof name !== "string" || name.trim() === "") return err("the area has no name");
	if (bounds === null) return err("the area's bounds are not [west, south, east, north]");
	if (!isRecord(home)) return err("the area has no home view");
	const center = readLngLat(home["center"]);
	const zoom = home["zoom"];
	if (center === null) return err("the area's home view has no centre");
	if (typeof zoom !== "number" || zoom < 0 || zoom > 22) return err("the area's home level lies outside 0 to 22");
	return ok({ name, bounds, home: { center, zoom: level(zoom) } });
}

/** The area in the text of the page's JSON block, or Macau when the text holds none the map can read. */
export const areaOf = (text: string): Area => pipe(text, parseJson, flatMap(parseArea), defaultWith((): Area => MACAU));
