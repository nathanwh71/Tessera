// map/scale.ts: the scale bar's length and figures (UI-19, criteria 1, 2 and 4).

import type { Units } from "./map";

/** The equator's length in metres on the sphere of Web Mercator, the flat projection every web map uses (EPSG:3857, radius 6,378,137 m). */
const EQUATOR_METRES = 2 * Math.PI * 6378137;
const FEET_PER_METRE = 1 / 0.3048;
const FEET_PER_MILE = 5280;

/** UI-19, criterion 1: the bar is never longer than 140 pixels. */
export const SCALE_MAX_PX = 140;

/**
 * Metres on the ground for one screen pixel, at a web-map zoom level (level 0 is the world in 256
 * pixels, and each level up doubles the scale) and a latitude in degrees. Web Mercator stretches
 * distance by 1 / cos(latitude), more and more towards the poles, so the bar must be measured where
 * the reader looks.
 */
export function metresPerPixel(level: number, latitude: number): number {
	return (EQUATOR_METRES * Math.cos((latitude * Math.PI) / 180)) / (256 * 2 ** level);
}

/** The largest 1, 2 or 5 times a power of ten (1, 2, 5, 10, 20, 50 and so on) that is at most the value given. */
export function roundLengthBelow(value: number): number {
	const power = 10 ** Math.floor(Math.log10(value));
	const leading = value / power;
	return (leading >= 5 ? 5 : leading >= 2 ? 2 : 1) * power;
}

/** What the scale bar draws and says. */
export interface ScaleReading {
	/** The bar's length on screen, in pixels, at most 140. */
	readonly widthPx: number;
	/** The figure at the halfway mark, such as '250'. */
	readonly half: string;
	/** The figure at the end, with its unit, such as '500 m'. */
	readonly full: string;
	/** The button's label for screen readers: the length in words, and the units a click switches to. */
	readonly label: string;
}

type Unit = "m" | "km" | "ft" | "mi";

const UNIT_WORDS: Readonly<Record<Unit, readonly [one: string, many: string]>> = {
	m: ["metre", "metres"],
	km: ["kilometre", "kilometres"],
	ft: ["foot", "feet"],
	mi: ["mile", "miles"],
};

/** A figure with a comma every three digits and no more than three decimals: '1,000', '0.5'. */
function figure(value: number): string {
	return (Math.round(value * 1000) / 1000).toLocaleString("en-GB", { maximumFractionDigits: 3 });
}

/** The length to draw, in the unit to name, and how many metres it covers. */
function roundLength(span: number, units: Units): { length: number; unit: Unit; metres: number } {
	if (units === "metric") {
		const metres = roundLengthBelow(span);
		return metres >= 1000 ? { length: metres / 1000, unit: "km", metres } : { length: metres, unit: "m", metres };
	}
	const feet = span * FEET_PER_METRE;
	if (feet >= FEET_PER_MILE) {
		const miles = roundLengthBelow(feet / FEET_PER_MILE);
		return { length: miles, unit: "mi", metres: (miles * FEET_PER_MILE) / FEET_PER_METRE };
	}
	const rounded = roundLengthBelow(feet);
	return { length: rounded, unit: "ft", metres: rounded / FEET_PER_METRE };
}

/**
 * The scale bar for a zoom level and the latitude of the map's centre: the longest round length (1, 2
 * or 5 of a unit) that fits in 140 pixels, in metres and kilometres or in feet and miles.
 */
export function scaleBar(level: number, latitude: number, units: Units): ScaleReading {
	const perPixel = metresPerPixel(level, latitude);
	const { length, unit, metres } = roundLength(perPixel * SCALE_MAX_PX, units);
	const [one, many] = UNIT_WORDS[unit];
	const other = units === "metric" ? "imperial" : "metric";
	return {
		widthPx: Math.round(metres / perPixel),
		half: figure(length / 2),
		full: `${figure(length)} ${unit}`,
		label: `Scale bar: ${figure(length)} ${length === 1 ? one : many}. Switch to ${other} units`,
	};
}
