// map/labels.ts: the words a screen reader, the program that reads a page aloud to a visitor who
// cannot see it, says for what the canvas, the bitmap the map is drawn on, shows (UI-09, UI-10,
// UI-19). Pure functions of the map module, which read only their inputs and change nothing outside
// themselves; targets.ts and the controls call them.

import { MAX_ZOOM } from "../shared";
import { metresPerPixel } from "./scale";
import { CLUSTER_RADIUS_PX } from "./style";

/**
 * How many of a cluster's places its label names; the rest it counts. A cluster is one mark that
 * stands for several places close together.
 */
export const NAMED_IN_CLUSTER = 10;

/**
 * A cluster's label (UI-10, criterion 5): its number, the names of its first places, and that zooming
 * in shows them, such as '3 places: A, B, C. Zoom in to see them.'
 */
export function clusterLabel(total: number, names: readonly string[]): string {
	const named = names.slice(0, NAMED_IN_CLUSTER);
	const rest = total - named.length;
	const list = named.length === 0 ? "" : `: ${named.join(", ")}${rest > 0 ? `, and ${rest} more` : ""}`;
	return `${total} places${list}. Zoom in to see them.`;
}

/** A pin's label: its name and its type, as the prototype's 'Harbour Market, Market'. */
export function pinLabel(name: string, type: string | null): string {
	return type === null ? name : `${name}, ${type}`;
}

/** A zoom level as the zoom buttons say it: whole levels plainly, others to one decimal place. */
export function levelText(level: number): string {
	const tenths = Math.round(level * 10) / 10;
	return Number.isInteger(tenths) ? String(tenths) : tenths.toFixed(1);
}

/** UI-09, criterion 3: 'Zoom in, now at level 15 of 22'; the level shows nowhere else. */
export function zoomLabel(direction: "in" | "out", level: number): string {
	return `Zoom ${direction}, now at level ${levelText(level)} of ${MAX_ZOOM}`;
}

/**
 * The ground distance under which two pins merge into a cluster at a level and latitude (UI-10,
 * criterion 1): the cluster radius in pixels times the metres each pixel spans. It halves at each
 * level up, since each level doubles the scale.
 */
export function mergeDistanceMetres(level: number, latitude: number): number {
	return CLUSTER_RADIUS_PX * metresPerPixel(level, latitude);
}

/**
 * What the page says in place of the map when the browser cannot draw it (US-23, criterion 9): what
 * went wrong, and two things the visitor can do about it.
 */
export const CANNOT_DRAW = "This browser cannot draw the map. Try another browser, or turn on hardware acceleration in this browser's settings.";
