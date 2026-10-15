// map/map-handle.ts: the map's ports, the functions through which the update function moves the map,
// as MapLibre, the library that draws the map, carries them out (US-23, UI-09, UI-10). main.tsx makes
// one handle and hands it to the page; the update calls its ports, and useMapLibre, in
// canvas-hooks.ts, attaches the map to it once MapLibre has started. It is one of the two files the
// architecture test lets hold changing state.
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

/**
 * The ***ports*** for the app, and the two calls the map view makes as MapLibre starts and stops.
 * The ports are fixed, so the update can hold them from the start; `attach` and `detach` swap the
 * map behind them.
 */
export interface MapHandle {
	readonly ports: Ports;
	attach(map: MapLibreMap): void;
	detach(): void;
}

/** One move of the map, kept as a function so it can wait until a map exists to run on. */
type Move = (map: MapLibreMap) => void;

/**
 * A handle with no map yet; `attach` gives it one, and any move asked for before then runs at that
 * moment.
 */
export function createMapHandle(): MapHandle {
	let current: MapLibreMap | null = null;
	let waiting: Move | null = null;

	// Runs a move now, or keeps it until a map attaches. Only the latest is kept, since a newer move
	// makes an older one pointless.
	const run = (move: Move): void => {
		if (current === null) waiting = move;
		else move(current);
	};

	// Every move lasts 0.3 seconds with Material's emphasised easing, or none at all for a visitor
	// who asks for less motion (UI-15, criteria 7 and 8). The page reads that wish itself and sets
	// the duration to match, so a move with a duration is marked essential: MapLibre then leaves it
	// alone rather than apply the same wish a second time by its own rules.
	const motion = () => {
		const duration = movementMs(prefersReducedMotion());
		return { duration, easing: emphasised, essential: duration > 0 };
	};

	return {
		ports: {
			// The four numbers are copied into a fresh array, since MapLibre's type takes an array it may
			// change and the area's bounds are read-only.
			fitBounds: (bounds: Bounds, maxLevel: Level) =>
				run((map) => map.fitBounds([bounds[0], bounds[1], bounds[2], bounds[3]], { ...motion(), maxZoom: toMapZoom(maxLevel), padding: FRAME_PADDING })),
			// The port keeps the update's name for a move to a view, but eases there rather than jumps:
			// every movement the visitor sees follows the same 0.3-second easing (UI-15, criterion 7).
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
