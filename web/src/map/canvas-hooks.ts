// map/canvas-hooks.ts: the four pieces of React work behind MapCanvas.tsx, each a hook with a name
// that says what it does (US-23, UI-15). A hook is a function whose name starts with `use` that a
// component calls to keep values between draws or to run code after it draws. Splitting the work into
// named hooks lets a reader find each job without reading the others:
//
// - useLatest keeps the newest props where MapLibre's events can read them;
// - useLanding runs the pins' drop, from the pure state machine in landing.ts;
// - useMapLibre makes the map, wires its events, and removes it when the page leaves;
// - useStyle keeps the map's style and the pins' paint in step with the model.

import { Map as MapLibreMap, type MapOptions } from "maplibre-gl";
import { type RefObject, useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import { type Theme, prefersReducedMotion, readTheme } from "../design";
import type { LayerId } from "../layers";
import type { Level, LngLat, Problem, View } from "../shared";
import { type Landing, type LandingEvent, START, step } from "./landing";
import { levelOf, toMapZoom } from "./levels";
import type { MapHandle } from "./map-handle";
import { FONT_FILES, attachEvents, baseOptions, feedPlaces, provideImages, watchTileErrors } from "./maplibre";
import { buildStyle, landingPaint } from "./style";
import { type ClusterNames, type Target, nameClusters, openCluster, targetsInView } from "./targets";

/** What the map needs from its props; MapCanvas.tsx passes them all. */
export interface CanvasInputs {
	readonly visible: readonly LayerId[];
	readonly landed: boolean;
	readonly origin: string;
	readonly initialView: View;
	readonly onViewChange: (view: View) => void;
	readonly onPointer: (position: LngLat | null) => void;
	readonly onClusterOpen: (center: LngLat, expansionLevel: Level) => void;
	readonly onTileFailed: (problem: Problem) => void;
	readonly onLanded: () => void;
	readonly onCannotDraw: () => void;
	readonly onCanDrawAgain: () => void;
	readonly onReady?: (map: MapLibreMap) => void;
}

/** The places' feed: `refresh` fills the source again after a new style, `stop` ends the feed. */
interface Feed {
	refresh(): void;
	stop(): void;
}

/**
 * The longest the page waits, once the map has loaded, for its first places before it shows the
 * map's own pins regardless, in milliseconds.
 */
const LANDING_TIMEOUT_MS = 8000;

/** The pins among the targets in view, as the landing drops them. */
const pinsOf = (targets: readonly Target[]) =>
	targets.flatMap((t) => (t.clusterId === null && t.group !== null ? [{ key: t.key, x: t.x, y: t.y, group: t.group }] : []));

/**
 * The newest value, in a box that keeps its place between draws. MapLibre calls its events long
 * after the code that registered them ran; reading the box, rather than a value captured then, means
 * each event sees the props of the latest draw. The box is filled after each draw and before any
 * other effect runs, so every effect and event reads the same props the page shows.
 */
export function useLatest<T>(value: T): RefObject<T> {
	const box = useRef(value);
	useLayoutEffect(() => {
		box.current = value;
	});
	return box;
}

/** The pins' drop: the landing's state, whether the drops are falling, and how to move it on. */
export function useLanding(latest: RefObject<CanvasInputs>) {
	const [landing, setLanding] = useState<Landing>(START);
	// True a frame, one redraw of the screen, after the drops are drawn, so the browser sees each pin
	// start above its place and fall.
	const [falling, setFalling] = useState(false);
	const advance = useCallback((event: LandingEvent) => setLanding((at) => step(at, event)), []);

	// The landing's stages become effects: a drop starts its timer and its fall, and landing tells the
	// model, once.
	useEffect(() => {
		if (landing.stage === "landed") {
			if (!latest.current.landed) latest.current.onLanded();
			return;
		}
		if (landing.stage !== "dropping") return;
		const frame = requestAnimationFrame(() => requestAnimationFrame(() => setFalling(true)));
		const timer = window.setTimeout(() => advance({ name: "dropsDone" }), landing.ms);
		return () => {
			cancelAnimationFrame(frame);
			window.clearTimeout(timer);
		};
	}, [landing, advance, latest]);

	return { landing, falling, advance };
}

/** A new map, or null when the browser cannot draw one. */
function makeMap(options: MapOptions): MapLibreMap | null {
	try {
		return new MapLibreMap(options);
	} catch {
		return null;
	}
}

/**
 * MapLibre, made once into the container and removed when the page leaves. Its events turn into the
 * props' callbacks and the landing's events; it returns the map, the places' feed, the page's theme,
 * whether the first style has loaded, and the pins and clusters in view.
 */
export function useMapLibre(
	container: RefObject<HTMLDivElement | null>,
	handle: MapHandle,
	latest: RefObject<CanvasInputs>,
	advance: (event: LandingEvent) => void,
) {
	const mapRef = useRef<MapLibreMap | null>(null);
	const feedRef = useRef<Feed | null>(null);
	const clusterNames = useRef<ClusterNames>(new Map());
	// The page's colours, read once from its style sheet as the map is first drawn.
	const [theme] = useState<Theme>(readTheme);
	// True once MapLibre has loaded its first style, its description of what to draw; after that it
	// can diff every new one, and apply only what changed.
	const [loaded, setLoaded] = useState(false);
	const [targets, setTargets] = useState<readonly Target[]>([]);

	// The function returned at the end runs when the component leaves the page, and undoes everything
	// the effect set up.
	useEffect(() => {
		const element = container.current;
		if (element === null) return;
		const first = latest.current;
		const reduced = prefersReducedMotion();
		const map = makeMap({
			...baseOptions(reduced),
			container: element,
			style: buildStyle({ origin: first.origin, visible: first.visible, landed: first.landed, theme, fonts: FONT_FILES }),
			center: [first.initialView.center.lng, first.initialView.center.lat],
			zoom: toMapZoom(first.initialView.zoom),
		});
		// MapLibre throws when the browser has no WebGL; the page then says so in place of the map
		// (US-23, criterion 9), rather than losing the whole page to the error.
		if (map === null) {
			first.onCannotDraw();
			return;
		}
		map.touchZoomRotate.disableRotation();
		map.keyboard.disableRotation();
		provideImages(map, theme);
		watchTileErrors(map, (problem) => latest.current.onTileFailed(problem));
		mapRef.current = map;
		feedRef.current = feedPlaces(map, () => ({ visible: latest.current.visible }), () => advance({ name: "placesFed" }));

		const names = clusterNames.current;
		const refreshTargets = (): readonly Target[] => {
			const inView = targetsInView(map, names);
			setTargets(inView);
			return inView;
		};
		const timers = { frame: 0, giveUp: 0 };
		const stopEvents = attachEvents(map, {
			onMove: () => {
				const c = map.getCenter();
				latest.current.onViewChange({ center: { lng: c.lng, lat: c.lat }, zoom: levelOf(map.getZoom()) });
				// One refresh a frame, however many move events come in it.
				if (timers.frame === 0) timers.frame = requestAnimationFrame(() => ((timers.frame = 0), refreshTargets()));
			},
			onIdle: () => {
				const inView = refreshTargets();
				nameClusters(map, names, refreshTargets);
				advance({ name: "mapIdle", pins: pinsOf(inView), reduced });
			},
			onVisitorMove: () => advance({ name: "visitorMoved" }),
			onPlacesChanged: () => names.clear(),
			onPointer: (position) => latest.current.onPointer(position),
			onClusterClick: (id, point) => openCluster(map, id, point, (center, level) => latest.current.onClusterOpen(center, level)),
			onDrawingLost: () => latest.current.onCannotDraw(),
			onDrawingRestored: () => latest.current.onCanDrawAgain(),
			onLoad: () => {
				setLoaded(true);
				latest.current.onReady?.(map);
				timers.giveUp = window.setTimeout(() => advance({ name: "timedOut" }), LANDING_TIMEOUT_MS);
			},
		});
		handle.attach(map);
		return () => {
			stopEvents();
			handle.detach();
			feedRef.current?.stop();
			window.clearTimeout(timers.giveUp);
			if (timers.frame !== 0) cancelAnimationFrame(timers.frame);
			map.remove();
			mapRef.current = null;
		};
	}, [container, handle, latest, advance, theme]);

	return { mapRef, feedRef, theme, loaded, targets };
}

/**
 * The map's style and the pins' paint, kept in step with the model. MapLibre can diff a style only
 * once its first has loaded; before that it would rebuild from scratch. A new style empties the
 * places' source, the data the place layers draw from, so the feed fills it again (ADR 0005).
 */
export function useStyle(
	mapRef: RefObject<MapLibreMap | null>,
	feedRef: RefObject<Feed | null>,
	style: ReturnType<typeof buildStyle>,
	loaded: boolean,
	landed: boolean,
): void {
	const applied = useRef<typeof style>(null);
	useEffect(() => {
		const map = mapRef.current;
		if (map === null || !loaded) return;
		if (applied.current === null) {
			// The map started with this style; there is nothing to change.
			applied.current = style;
			return;
		}
		if (applied.current === style) return;
		applied.current = style;
		map.setStyle(style, { diff: true });
		feedRef.current?.refresh();
	}, [mapRef, feedRef, style, loaded]);

	// UI-15, criterion 6: when the dropping pins land, the map's own pins and names show; and after any
	// new style, they show again at once if they have landed.
	useEffect(() => {
		const map = mapRef.current;
		if (map === null || !loaded) return;
		for (const [layer, property, value] of landingPaint(landed)) {
			if (map.getLayer(layer) !== undefined) map.setPaintProperty(layer, property, value);
		}
	}, [mapRef, landed, loaded, style]);
}
