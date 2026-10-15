// map/images.ts: the names of the images the style asks for, and how to read them back (UI-10 to
// UI-12). Pure functions of the map module, which read only their inputs and change nothing outside
// themselves: style.ts writes the names, and maplibre.ts reads each one back to know what to draw.
//
// The style, the description of what MapLibre, the library that draws the map, draws, names an image
// for each group's pin and each mix of groups in a cluster, one mark that stands for several places
// close together. MapLibre asks for a name it lacks, and the map view draws it then. Drawing on
// demand keeps the number of cluster images to the mixes that appear on screen.

import { GROUP_KEYS, type GroupKey } from "../design";

/**
 * What an image name asks the canvas, the bitmap the page paints images on, to draw. A closed set of
 * named cases, so maplibre.ts must handle each kind and the compiler flags one it misses (ADR 0004).
 */
export type ImageRequest =
	| { readonly kind: "pin"; readonly group: GroupKey }
	| { readonly kind: "cluster"; readonly counts: Readonly<Record<GroupKey, number>> }
	| { readonly kind: "tooltip" };

/** The tooltip: the dark box behind every name, stretched to fit it (UI-11, criterion 1). */
export const TOOLTIP_IMAGE = "tooltip";

/**
 * The prefix of every pin image; the group follows, such as `pin:food`. The prefix tells a pin's
 * name from a ***cluster***'s when readImageName reads it back.
 */
export const PIN_PREFIX = "pin:";

/**
 * The image name of a group's pin, such as `pin:food`. index.ts exports it, so code outside the map
 * ***module*** can name a pin's image without knowing the prefix.
 */
export function pinImage(group: GroupKey): string {
	return `${PIN_PREFIX}${group}`;
}

/**
 * The prefix of every cluster image; the counts of the four groups follow, in the map's order, joined
 * by '-', such as `cluster:3-0-1-0`. The counts sit in the name so each mix of groups gets an image
 * of its own, ringed in their colours in proportion (UI-10, criterion 1).
 */
export const CLUSTER_PREFIX = "cluster:";

/** True for one of the four groups; a name with any other text after `pin:` draws nothing. */
const isGroup = (text: string): text is GroupKey => GROUP_KEYS.some((g) => g === text);

/**
 * Reads an image name back into what to draw; null for a name this module never makes, so a stray
 * name draws nothing.
 */
export function readImageName(name: string): ImageRequest | null {
	if (name === TOOLTIP_IMAGE) return { kind: "tooltip" };
	if (name.startsWith(CLUSTER_PREFIX)) {
		const counts = name.slice(CLUSTER_PREFIX.length).split("-").map(Number);
		const valid = counts.length === GROUP_KEYS.length && counts.every((c) => Number.isInteger(c) && c >= 0);
		// `valid` has checked there are four, but the compiler cannot know it, so each takes 0 as a
		// default to stay a number.
		const [food = 0, outdoors = 0, culture = 0, transport = 0] = counts;
		return valid ? { kind: "cluster", counts: { food, outdoors, culture, transport } } : null;
	}
	if (!name.startsWith(PIN_PREFIX)) return null;
	const group = name.slice(PIN_PREFIX.length);
	return isGroup(group) ? { kind: "pin", group } : null;
}
