// shared/area.ts: the area the map serves, a setting of the service (`Area` in tessera.json) that
// the office sets to Macau (US-23). The service writes it into the page itself, so the map needs no
// extra request. The shared kernel holds the reader, and main.tsx calls `areaOf` once as the page
// starts.

import * as v from "valibot";
import { type Bounds, BoundsSchema, LngLatSchema, type View, level } from "./geo";
import { parseJson, parseWith } from "./json";
import { pipe } from "./pipe";
import { type Result, defaultWith, flatMap } from "./result";

/** The area as the page carries it, in the `tessera-area` script block the service writes. */
export interface Area {
	/** The area's name, such as 'Macau', which heads the page and its tab for a screen reader. */
	readonly name: string;
	/** West, south, east, north, in degrees of WGS 84, the longitude and latitude system GPS uses. */
	readonly bounds: Bounds;
	/** Where the map opens (US-23, criterion 1). */
	readonly home: View;
}

/**
 * Macau's settings, as the service sends them. The page falls back on them when it carries no area,
 * as under `vite` alone, the development server that serves the page without the service.
 */
export const MACAU = {
	name: "Macau",
	bounds: [113.52, 22.1, 113.61, 22.22],
	home: { center: { lng: 113.55, lat: 22.19 }, zoom: level(15) },
} as const satisfies Area;

/**
 * The area the page carries: `{name, bounds: [w, s, e, n], home: {center: {lng, lat}, zoom}}`. It
 * refuses a missing or blank name, a box whose west is not less than its east or whose south is not
 * less than its north, and a home zoom level outside 0 to 22.
 */
const AreaSchema = v.object(
	{
		name: v.message(v.pipe(v.string(), v.check((name) => name.trim() !== "")), "the area has no name"),
		bounds: v.message(BoundsSchema, "the area's bounds are not [west, south, east, north]"),
		home: v.object(
			{
				center: v.message(LngLatSchema, "the area's home view has no centre"),
				zoom: v.pipe(
					v.message(v.pipe(v.number(), v.minValue(0), v.maxValue(22)), "the area's home level lies outside 0 to 22"),
					v.transform(level),
				),
			},
			"the area has no home view",
		),
	},
	"the area is not a JSON object with a name, bounds and a home view",
);

/** Reads the area the page carries, and returns it or the reason it was refused. */
export const parseArea: (json: unknown) => Result<Area> = parseWith(AreaSchema);

/**
 * The area in the text of the page's block of JSON, the text form of a value that programs exchange,
 * or Macau when the text holds none the map can read. A pipe, a chain of steps run in the order
 * written (pipe.ts), joins the three steps.
 */
export const areaOf = (text: string): Area => pipe(text, parseJson, flatMap(parseArea), defaultWith((): Area => MACAU));
