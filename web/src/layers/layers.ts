// layers/layers.ts: the nine layers of places, named in the map's own code (US-23, criterion 7).
//
// The map loads no list of layers from the service. It knows the nine layers of US-24 from this file,
// and builds each tile's address, `/{layer}/{z}/{x}/{y}.pbf`, on the page's own origin: the scheme,
// host and port the page itself came from. So the page needs only the tile API.

import type { GroupKey } from "../design";

/** One of the nine layers: its id, `group.name`, the group it belongs to, and its names in words. */
export interface PlaceLayer {
	readonly id: LayerId;
	readonly group: GroupKey;
	/** The layer's title, such as 'Cafés'. */
	readonly title: string;
	/** One of its places, such as 'Café', for a pin's label. */
	readonly one: string;
}

/** The nine layers, in the map's order: the order in which names win the space between layers (UI-17, criterion 2). */
export const LAYERS = [
	{ id: "food.cafes", group: "food", title: "Cafés", one: "Café" },
	{ id: "food.restaurants", group: "food", title: "Restaurants", one: "Restaurant" },
	{ id: "food.markets", group: "food", title: "Markets", one: "Market" },
	{ id: "outdoors.parks", group: "outdoors", title: "Parks", one: "Park" },
	{ id: "outdoors.playgrounds", group: "outdoors", title: "Playgrounds", one: "Playground" },
	{ id: "culture.museums", group: "culture", title: "Museums", one: "Museum" },
	{ id: "culture.libraries", group: "culture", title: "Libraries", one: "Library" },
	{ id: "transport.trains", group: "transport", title: "Train stations", one: "Train station" },
	{ id: "transport.buses", group: "transport", title: "Bus stops", one: "Bus stop" },
] as const satisfies readonly { id: string; group: GroupKey; title: string; one: string }[];

/** A layer's id: one of the nine, such as `food.cafes`. */
export type LayerId = (typeof LAYERS)[number]["id"];

/** Every layer's id, in the map's order. */
export const LAYER_IDS: readonly LayerId[] = LAYERS.map((l) => l.id);

/** True for text that names one of the nine layers. */
export function isLayerId(text: string): text is LayerId {
	return LAYER_IDS.some((id) => id === text);
}

/** The layer of an id, or undefined for text that names none of the nine. */
export function layerOf(id: string): PlaceLayer | undefined {
	return LAYERS.find((l) => l.id === id);
}

/** The group of a layer, such as `food` for `food.cafes`, or undefined for text that names none of the nine. */
export function groupOf(id: string): GroupKey | undefined {
	return layerOf(id)?.group;
}

/**
 * The address MapLibre fetches a layer's tiles from: `/{layer}/{z}/{x}/{y}.pbf` on the page's own
 * origin. MapLibre fills in `{z}`, `{x}` and `{y}` for each tile, so they stay as written.
 */
export function tileUrl(origin: string, id: LayerId): string {
	return `${origin.replace(/\/+$/, "")}/${id}/{z}/{x}/{y}.pbf`;
}

/**
 * The layer a page's path names, such as `food.cafes` in `/food.cafes.html` (US-23, criterion 2), or
 * undefined for `/` and for any path that names none of the nine.
 */
export function layerFromPath(pathname: string): LayerId | undefined {
	const match = /^\/([^/]+)\.html$/.exec(pathname);
	const name = match?.[1];
	return name !== undefined && isLayerId(name) ? name : undefined;
}
