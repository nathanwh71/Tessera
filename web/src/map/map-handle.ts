// map/map-handle.ts: the map's ports, the functions through which the update function moves the map,
// as MapLibre, the library that draws the map, carries them out (US-23, UI-09, UI-10). main.tsx makes
// one handle and hands it to the page; the update calls its ports, and MapCanvas.tsx attaches the map
// to it once MapLibre has started. It is one of the two files the architecture test lets hold
// changing state.
//
// The ports exist before the map does: /food.cafes.html asks to frame the area before MapLibre has
// started. So the handle keeps the latest request and carries it out once a map attaches.

import type { Map as MapLibreMap } from "maplibre-gl";
import { emphasised, movementMs, prefersReducedMotion } from "../design";
import type { Bounds, Level, View } from "../shared";
import { toMapZoom } from "./levels";
import type { Ports } from "./map";

/**
 * Space kept around a framed set of places, in pixels, so pins at the edge stay clear of the
 * screen's edge.
 */
const FRAME_PADDING = 64;

/** The ports for the app, and the two calls the map view makes as MapLibre starts and stops. */
export interface MapHandle {
	readonly ports: Ports;
	attach(map: MapLibreMap): void;
	detach(): void;
}

type Move = (map: MapLibreMap) => void;

/**
 * A handle with no map yet; `attach` gives it one, and any move asked for before then runs at that
 * moment.
 */
export function createMapHandle(): MapHandle {
	let current: MapLibreMap | null = null;
	let waiting: Move | null = null;

	const run = (move: Move): void => {
		if (current === null) waiting = move;
		else move(current);
	};

	const motion = () => {
		const duration = movementMs(prefersReducedMotion());
		return { duration, easing: emphasised, essential: duration > 0 };
	};

	return {
		ports: {
			fitBounds: (bounds: Bounds, maxLevel: Level) =>
				run((map) => map.fitBounds([bounds[0], bounds[1], bounds[2], bounds[3]], { ...motion(), maxZoom: toMapZoom(maxLevel), padding: FRAME_PADDING })),
			jumpTo: (view: View) => run((map) => map.easeTo({ ...motion(), center: [view.center.lng, view.center.lat], zoom: toMapZoom(view.zoom) })),
		},
		attach(map) {
			current = map;
			const pending = waiting;
			waiting = null;
			pending?.(map);
		},
		detach() {
			current = null;
		},
	};
}
