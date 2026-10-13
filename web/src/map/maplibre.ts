// map/maplibre.ts: what every map drawn by MapLibre, the map-drawing library, shares: its start-up
// options, its images and the feed of loaded places into the clustered source (a source is the data a
// map layer draws from). This is the shell of side effects around the pure functions of style.ts and
// places.ts; it holds no rule of its own.

import latinFont from "@fontsource/roboto/files/roboto-latin-400-normal.woff2?url";
import latinExtFont from "@fontsource/roboto/files/roboto-latin-ext-400-normal.woff2?url";
import { type GeoJSONSource, type MapOptions, type Map as MapLibreMap, setWorkerUrl } from "maplibre-gl";
import workerUrl from "maplibre-gl/dist/maplibre-gl-worker.mjs?url";
import { GROUP_LOOKS, IMAGE_PIXEL_RATIO, type Theme, drawCluster, drawPin, drawTooltip, movementMs } from "../design";
import type { LayerId } from "../layers";
import { type LngLat, MAX_ZOOM, MIN_ZOOM, type Problem } from "../shared";
import { readImageName } from "./images";
import { toMapZoom } from "./levels";
import { type LoadedLayer, fingerprint, placeCollection } from "./places";
import { type FontFiles, LAYER, SOURCE } from "./style";
import { pointOf } from "./targets";
import { tileProblem } from "./tiles";

// MapLibre runs its heavy work in a worker, a script on a second thread, and looks for it beside its own
// module, which a bundle moves; the build ships the worker as an asset of its own under /assets/, and
// this tells MapLibre where.
setWorkerUrl(workerUrl);

/** The Roboto font files the names draw with, served from the page's own site. */
export const FONT_FILES: FontFiles = { latin: latinFont, latinExt: latinExtFont };

/**
 * The whole world in Web Mercator, the flat projection every web map uses, which cuts off near the
 * poles: the map may not pan past it (UI-09, criterion 5). The east and west edges stop a
 * ten-thousandth of a degree short of 180°, since MapLibre 6.13 fails to start with bounds that span
 * exactly 360°.
 */
export const WORLD: [[number, number], [number, number]] = [
	[-179.9999, -85.051129],
	[179.9999, 85.051129],
];

/**
 * Chinese names draw from the visitor's own fonts, since a CJK (Chinese, Japanese and Korean) font file
 * is too large to ship; these cover Traditional Chinese on the office's Windows machines, then macOS
 * and Linux.
 */
const IDEOGRAPH_FONTS = '"Microsoft JhengHei", "PingFang TC", "Noto Sans CJK TC", sans-serif';

/**
 * The options every map starts with: zoom levels 0 to 22, the world as its limit, no copies of the
 * world beside it, no turning or tilting (the compass is a sign, UI-19 criterion 6), and symbols (pins
 * and names) that fade within 300 ms, or at once for a visitor who asks for less motion (UI-15). The
 * places are the office's own and no base map is drawn, so there is nothing to credit and no
 * attribution button.
 */
export function baseOptions(reducedMotion: boolean): Partial<MapOptions> {
	return {
		minZoom: toMapZoom(MIN_ZOOM),
		maxZoom: toMapZoom(MAX_ZOOM),
		maxBounds: WORLD,
		renderWorldCopies: false,
		dragRotate: false,
		pitchWithRotate: false,
		touchPitch: false,
		maxPitch: 0,
		attributionControl: false,
		fadeDuration: movementMs(reducedMotion),
		localIdeographFontFamily: IDEOGRAPH_FONTS,
	};
}

/** Draws each image the style, MapLibre's description of what to draw, names when MapLibre first needs it (see images.ts). */
export function provideImages(map: MapLibreMap, theme: Theme): void {
	map.setMissingStyleImageResolver((name) => {
		const request = readImageName(name);
		if (request === null || map.hasImage(name)) return;
		switch (request.kind) {
			case "pin":
				map.addImage(name, drawPin(theme, request.group, GROUP_LOOKS[request.group].shape).image, { pixelRatio: IMAGE_PIXEL_RATIO });
				return;
			case "cluster":
				map.addImage(name, drawCluster(theme, request.counts), { pixelRatio: IMAGE_PIXEL_RATIO });
				return;
			case "tooltip": {
				const { image, stretch } = drawTooltip(theme);
				map.addImage(name, image, { pixelRatio: IMAGE_PIXEL_RATIO, ...stretch });
				return;
			}
			default:
				request satisfies never;
		}
	});
}

/** What the feed reads afresh each time it runs: the layers that are on. */
export interface FeedState {
	readonly visible: readonly LayerId[];
}

/**
 * Keeps the clustered source in step with the tiles, the square pieces of the map, that MapLibre has
 * loaded for the layers that are on (see places.ts). Runs when a tile source finishes loading, when the
 * map comes to rest, and when the caller calls the returned `refresh`. Sets the source's data only when
 * its places change, since each new set makes MapLibre cluster again; `refresh` sets it regardless,
 * since a new style empties it. `onFed` hears each time the source gains places.
 */
export function feedPlaces(map: MapLibreMap, state: () => FeedState, onFed: () => void = () => undefined): { refresh(): void; stop(): void } {
	let last = "";
	let frame = 0;

	const run = (): void => {
		frame = 0;
		const source = map.getSource<GeoJSONSource>(SOURCE.places);
		if (source === undefined) return;
		const { visible } = state();
		const loaded: LoadedLayer[] = visible
			.filter((id) => map.getSource(SOURCE.tiles(id)) !== undefined)
			.map((id) => ({ layerId: id, features: map.querySourceFeatures(SOURCE.tiles(id), { sourceLayer: id }) }));
		const collection = placeCollection(loaded);
		const print = fingerprint(collection);
		if (print === last) return;
		last = print;
		if (collection.features.length > 0) onFed();
		void source.setData({ type: "FeatureCollection", features: collection.features.map((f) => ({ ...f, geometry: { ...f.geometry, coordinates: [...f.geometry.coordinates] } })) });
	};

	// One run per frame, the browser's next redraw of the screen, however many tiles arrive in it.
	const schedule = (): void => {
		if (frame === 0) frame = requestAnimationFrame(run);
	};
	const onData = (e: { sourceId?: string; isSourceLoaded?: boolean }): void => {
		if (e.sourceId?.startsWith("tiles:") === true && e.isSourceLoaded === true) schedule();
	};

	map.on("sourcedata", onData);
	map.on("idle", schedule);
	return {
		refresh() {
			last = "";
			schedule();
		},
		stop() {
			map.off("sourcedata", onData);
			map.off("idle", schedule);
			if (frame !== 0) cancelAnimationFrame(frame);
		},
	};
}

/** A failed request as MapLibre reports it: an AJAXError, which carries the answer's status and body. */
interface FailedRequest {
	readonly status: number;
	readonly statusText: string;
	readonly body: Blob;
}

function isFailedRequest(error: unknown): error is FailedRequest {
	if (typeof error !== "object" || error === null) return false;
	const e = error as Record<string, unknown>;
	return typeof e["status"] === "number" && typeof e["statusText"] === "string" && e["body"] instanceof Blob;
}

/**
 * Reports each tile request that the service answers with an error, as the problem document its body
 * holds (US-23, criterion 8). Errors of anything but a tile source, such as a missing image, are not
 * the service's and are left to MapLibre.
 */
export function watchTileErrors(map: MapLibreMap, report: (problem: Problem) => void): void {
	map.on("error", (event: { error?: unknown; sourceId?: string }) => {
		const { error, sourceId } = event;
		if (sourceId?.startsWith("tiles:") !== true || !isFailedRequest(error)) return;
		void error.body.text().then(
			(text) => report(tileProblem(error.status, error.statusText, text)),
			() => report(tileProblem(error.status, error.statusText, "")),
		);
	});
}

/** What the page wants to hear from the map. Each is called with values; none sees MapLibre. */
export interface MapEvents {
	/** The map moved, by the visitor or by a port. */
	readonly onMove: () => void;
	/** The map came to rest. */
	readonly onIdle: () => void;
	/** The visitor, not a port, started to move the map. */
	readonly onVisitorMove: () => void;
	/** The clustered source holds a new set of places, so MapLibre has renumbered its clusters. */
	readonly onPlacesChanged: () => void;
	/** The pointer sits here, or has left the map. */
	readonly onPointer: (position: LngLat | null) => void;
	/** The visitor clicked a cluster: its id, and its point in longitude and latitude. */
	readonly onClusterClick: (clusterId: number, point: readonly [number, number]) => void;
	/** MapLibre has loaded its first style. */
	readonly onLoad: () => void;
}

/**
 * Wires MapLibre's events to the page's handlers, in one place, and returns a function that unwires
 * them. Each handler receives plain values, so the page's own code never reads a MapLibre event; and
 * a test fires these events at a fake map to check what the page hears.
 */
export function attachEvents(map: MapLibreMap, events: MapEvents): () => void {
	// Only an event with an `originalEvent`, the browser's own pointer or key event, is the visitor's;
	// a move a port asked for has none.
	const onMoveStart = (e: { originalEvent?: unknown }): void => {
		if (e.originalEvent !== undefined) events.onVisitorMove();
	};
	const onSourceData = (e: { sourceId?: string; sourceDataType?: string }): void => {
		if (e.sourceId === SOURCE.places && e.sourceDataType === "content") events.onPlacesChanged();
	};
	const onMouseMove = (e: { lngLat: { lng: number; lat: number } }): void => events.onPointer({ lng: e.lngLat.lng, lat: e.lngLat.lat });
	const onMouseOut = (): void => events.onPointer(null);
	// A pin is a sign, not a button: a click opens nothing (US-23, criterion 6). Only a cluster answers.
	const onClusterClick = (e: { features?: readonly { geometry: { type: string; coordinates?: unknown }; properties: Record<string, unknown> }[] }): void => {
		const feature = e.features?.[0];
		const point = feature === undefined ? null : pointOf(feature as Parameters<typeof pointOf>[0]);
		const id: unknown = feature?.properties["cluster_id"];
		if (point !== null && typeof id === "number") events.onClusterClick(id, point);
	};
	const cursor = (value: string) => (): void => {
		const canvas = map.getCanvas();
		canvas.style.cursor = value;
	};
	const pointer = cursor("pointer");
	const plain = cursor("");

	map.on("move", events.onMove);
	map.on("idle", events.onIdle);
	map.on("movestart", onMoveStart);
	map.on("sourcedata", onSourceData);
	map.on("mousemove", onMouseMove);
	map.on("mouseout", onMouseOut);
	map.on("click", LAYER.clusters, onClusterClick);
	map.on("mouseenter", LAYER.clusters, pointer);
	map.on("mouseleave", LAYER.clusters, plain);
	map.once("load", events.onLoad);
	return () => {
		map.off("move", events.onMove);
		map.off("idle", events.onIdle);
		map.off("movestart", onMoveStart);
		map.off("sourcedata", onSourceData);
		map.off("mousemove", onMouseMove);
		map.off("mouseout", onMouseOut);
		map.off("click", LAYER.clusters, onClusterClick);
		map.off("mouseenter", LAYER.clusters, pointer);
		map.off("mouseleave", LAYER.clusters, plain);
	};
}
