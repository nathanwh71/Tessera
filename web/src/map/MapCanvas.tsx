// map/MapCanvas.tsx: the map that MapLibre, the map-drawing library, draws (US-23, UI-09 to UI-11,
// UI-15, UI-17). It is a component: a function that turns part of the model, the one object that holds
// everything the page shows, into what you see.
//
// MapLibre draws on a canvas, a bitmap that screen readers and the keyboard cannot reach. So over each
// pin in view sits an invisible picture with its name, and over each cluster an invisible button that
// opens it. They let the mouse through to the map, so dragging the map works as on any web map.
//
// The component is a thin edge. The decisions about the pins' landing live in landing.ts, a pure
// state machine; the wiring of MapLibre's events lives in maplibre.ts; and what MapLibre has drawn is
// read back in targets.ts. What stays here is React's own work: state, effects and what to draw.

import { Map as MapLibreMap } from "maplibre-gl";
import "maplibre-gl/dist/maplibre-gl.css";
import { type ReactNode, useEffect, useEffectEvent, useMemo, useRef, useState } from "react";
import { PinShape, type Theme, prefersReducedMotion, readTheme } from "../design";
import type { LayerId } from "../layers";
import type { Level, LngLat, Problem, View } from "../shared";
import { type Landing, type LandingEvent, START, step } from "./landing";
import { levelOf, toMapZoom } from "./levels";
import type { MapHandle } from "./map-handle";
import { FONT_FILES, attachEvents, baseOptions, feedPlaces, provideImages, watchTileErrors } from "./maplibre";
import { buildStyle, landingPaint } from "./style";
import { type ClusterNames, type Target, nameClusters, openCluster, targetsInView } from "./targets";

/** Props for the map, the inputs the component receives. Every event leaves through a callback, which the app turns into a message. */
export interface MapCanvasProps {
	readonly visible: readonly LayerId[];
	/** False until the pins have dropped in (UI-15, criterion 6). */
	readonly landed: boolean;
	/** The page's own origin, on which the tile addresses are built (US-23, criterion 7). */
	readonly origin: string;
	/** Where the map opens; later moves come through the handle's ports. */
	readonly initialView: View;
	readonly handle: MapHandle;
	readonly onViewChange: (view: View) => void;
	readonly onPointer: (position: LngLat | null) => void;
	readonly onClusterOpen: (center: LngLat, expansionLevel: Level) => void;
	readonly onTileFailed: (problem: Problem) => void;
	readonly onLanded: () => void;
	/** Called once MapLibre has started, so browser tests can reach the map (see main.tsx). */
	readonly onReady?: (map: MapLibreMap) => void;
}

/** The pin's head sits 25 pixels above its tip, which marks the place (design/canvas.ts). */
const PIN_HEAD = 25;

/** The longest the page waits, once the map has loaded, for its first places before it shows the map's own pins regardless. */
const LANDING_TIMEOUT_MS = 8000;

/** The pins among the targets in view, as the landing drops them. */
const pinsOf = (targets: readonly Target[]) =>
	targets.flatMap((t) => (t.clusterId === null && t.group !== null ? [{ key: t.key, x: t.x, y: t.y, group: t.group }] : []));

/** The map of the area, with the places of the layers that are on. */
export function MapCanvas(props: MapCanvasProps): ReactNode {
	const { visible, landed, origin, initialView, handle, onReady } = props;
	const container = useRef<HTMLDivElement>(null);
	const mapRef = useRef<MapLibreMap | null>(null);
	const feedRef = useRef<{ refresh(): void; stop(): void } | null>(null);
	const clusterNames = useRef<ClusterNames>(new Map());

	const [theme, setTheme] = useState<Theme | null>(null);
	// True once MapLibre has loaded its first style, after which it can diff every new one: apply only what changed.
	const [loaded, setLoaded] = useState(false);
	const [targets, setTargets] = useState<readonly Target[]>([]);
	const [landing, setLanding] = useState<Landing>(START);
	// True a frame after the drops are drawn, so the browser sees each pin start above its place and fall.
	const [falling, setFalling] = useState(false);
	const advance = (event: LandingEvent): void => setLanding((at) => step(at, event));

	// The style changes only with the layers that are on. The pins' landing changes paint alone, which
	// the effect below applies after every style, so this one leaves the pins unseen.
	const style = useMemo(
		() => (theme === null ? null : buildStyle({ origin, visible, landed: false, theme, fonts: FONT_FILES })),
		[theme, origin, visible],
	);

	// MapLibre's events call these long after the effect that registers them ran; useEffectEvent makes
	// each read the latest props instead of the ones from that first run.
	const firstStyle = useEffectEvent((pageTheme: Theme) => buildStyle({ origin, visible, landed, theme: pageTheme, fonts: FONT_FILES }));
	const firstView = useEffectEvent(() => initialView);
	const feedState = useEffectEvent(() => ({ visible }));
	const viewChanged = useEffectEvent((view: View) => props.onViewChange(view));
	const pointerMoved = useEffectEvent((position: LngLat | null) => props.onPointer(position));
	const clusterOpened = useEffectEvent((center: LngLat, level: Level) => props.onClusterOpen(center, level));
	const tileFailed = useEffectEvent((problem: Problem) => props.onTileFailed(problem));
	const ready = useEffectEvent((map: MapLibreMap) => onReady?.(map));

	// Start MapLibre once, in an effect, code React runs after it draws; the effects below keep the map
	// in step with the model. The function returned at the end runs when the component leaves the page.
	useEffect(() => {
		const element = container.current;
		if (element === null) return;
		const pageTheme = readTheme();
		const view = firstView();
		const reduced = prefersReducedMotion();
		const map = new MapLibreMap({
			...baseOptions(reduced),
			container: element,
			style: firstStyle(pageTheme),
			center: [view.center.lng, view.center.lat],
			zoom: toMapZoom(view.zoom),
		});
		map.touchZoomRotate.disableRotation();
		map.keyboard.disableRotation();
		provideImages(map, pageTheme);
		watchTileErrors(map, tileFailed);
		mapRef.current = map;
		feedRef.current = feedPlaces(map, feedState, () => advance({ name: "placesFed" }));

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
				viewChanged({ center: { lng: c.lng, lat: c.lat }, zoom: levelOf(map.getZoom()) });
				// One refresh a frame, the browser's next redraw, however many move events come in it.
				if (timers.frame === 0) timers.frame = requestAnimationFrame(() => ((timers.frame = 0), refreshTargets()));
			},
			onIdle: () => {
				const inView = refreshTargets();
				nameClusters(map, names, refreshTargets);
				advance({ name: "mapIdle", pins: pinsOf(inView), reduced });
			},
			onVisitorMove: () => advance({ name: "visitorMoved" }),
			onPlacesChanged: () => names.clear(),
			onPointer: pointerMoved,
			onClusterClick: (id, point) => openCluster(map, id, point, clusterOpened),
			onLoad: () => {
				setLoaded(true);
				ready(map);
				timers.giveUp = window.setTimeout(() => advance({ name: "timedOut" }), LANDING_TIMEOUT_MS);
			},
		});
		handle.attach(map);
		setTheme(pageTheme);
		return () => {
			stopEvents();
			handle.detach();
			feedRef.current?.stop();
			window.clearTimeout(timers.giveUp);
			if (timers.frame !== 0) cancelAnimationFrame(timers.frame);
			map.remove();
			mapRef.current = null;
		};
	}, [handle]);

	// The landing's stages become effects: a drop starts its timer and its fall, and landing tells the model.
	const land = useEffectEvent(() => {
		if (!landed) props.onLanded();
	});
	useEffect(() => {
		if (landing.stage === "landed") {
			land();
			return;
		}
		if (landing.stage !== "dropping") return;
		const frame = requestAnimationFrame(() => requestAnimationFrame(() => setFalling(true)));
		const timer = window.setTimeout(() => advance({ name: "dropsDone" }), landing.ms);
		return () => {
			cancelAnimationFrame(frame);
			window.clearTimeout(timer);
		};
	}, [landing]);

	// MapLibre can only diff a style once its first has loaded; before that it would rebuild from scratch.
	// A new style empties the places' source, so the feed fills it again.
	const applied = useRef<typeof style>(null);
	useEffect(() => {
		const map = mapRef.current;
		if (map === null || style === null || !loaded) return;
		if (applied.current === null) {
			// The map started with this style; there is nothing to change.
			applied.current = style;
			return;
		}
		if (applied.current === style) return;
		applied.current = style;
		map.setStyle(style, { diff: true });
		feedRef.current?.refresh();
	}, [style, loaded]);

	// UI-15, criterion 6: when the dropping pins land, the map's own pins and names show; and after any
	// new style, they show again at once if they have landed.
	useEffect(() => {
		const map = mapRef.current;
		if (map === null || style === null || !loaded) return;
		for (const [layer, property, value] of landingPaint(landed)) {
			if (map.getLayer(layer) !== undefined) map.setPaintProperty(layer, property, value);
		}
	}, [landed, loaded, style]);

	return (
		<div className="absolute inset-0">
			{/* MapLibre sets its container to position: relative, so the container must take its size from width and height. */}
			<div ref={container} className="h-full w-full" />
			{!landed && landing.stage === "dropping" && (
				<div aria-hidden="true" className="pointer-events-none absolute inset-0 overflow-hidden">
					{landing.drops.map((d) => (
						<div
							key={d.key}
							className={`pin-drop absolute ${falling ? "pin-drop-landed" : ""}`}
							style={{ left: d.x, top: d.y, transitionDelay: `${d.delay}ms` }}
						>
							<PinShape group={d.group} />
						</div>
					))}
				</div>
			)}
			<div role="group" aria-label="Places on the map" className="pointer-events-none absolute inset-0 overflow-hidden">
				{targets.map((t) =>
					t.clusterId === null ? (
						<span
							key={t.key}
							role="img"
							aria-label={t.label}
							className="absolute size-11 -translate-x-1/2 -translate-y-1/2"
							style={{ left: t.x, top: t.y - PIN_HEAD }}
						/>
					) : (
						<button
							key={t.key}
							type="button"
							aria-label={t.label}
							onClick={() => mapRef.current !== null && t.clusterId !== null && openCluster(mapRef.current, t.clusterId, t.point, props.onClusterOpen)}
							className="pointer-events-none absolute size-12 -translate-x-1/2 -translate-y-1/2 rounded-full border-0 bg-transparent p-0"
							style={{ left: t.x, top: t.y }}
						/>
					),
				)}
			</div>
		</div>
	);
}
