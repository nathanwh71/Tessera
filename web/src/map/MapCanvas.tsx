// map/MapCanvas.tsx: the map that MapLibre, the library that draws the map, draws (US-23, UI-09 to
// UI-11, UI-15, UI-17). It is a React component: React is the library that draws the page from
// values, and a component is a function that turns part of the model, the one object that holds
// everything the page shows, into what you see. App.tsx places it under the controls and turns each
// of its callbacks, functions it calls when something happens, into a message, a value that says
// what happened.
//
// MapLibre draws on a canvas, a bitmap that the keyboard and a screen reader, the program that reads
// a page aloud, cannot reach. So over each pin in view sits an invisible picture with its name, and
// over each cluster, one mark for several places close together, an invisible button that opens it.
// They let the mouse through to the map, so dragging the map works as on any web map.
//
// The component is a thin edge. The decisions about the pins' landing live in landing.ts, a pure
// state machine; the wiring of MapLibre's events lives in maplibre.ts; what MapLibre has drawn is read
// back in targets.ts; and React's work of keeping the map in step lives in four named hooks in
// canvas-hooks.ts. What stays here is what to draw.

import type { Map as MapLibreMap } from "maplibre-gl";
import "maplibre-gl/dist/maplibre-gl.css";
import { type ReactNode, useMemo, useRef } from "react";
import { PinShape } from "../design";
import type { LayerId } from "../layers";
import type { Level, LngLat, Problem, View } from "../shared";
import { useLanding, useLatest, useMapLibre, useStyle } from "./canvas-hooks";
import type { MapHandle } from "./map-handle";
import { FONT_FILES } from "./maplibre";
import { buildStyle } from "./style";
import { openCluster } from "./targets";

/**
 * Props for the map, the inputs the component receives. Every event leaves through a callback, which
 * the app turns into a message.
 */
export interface MapCanvasProps {
	readonly visible: readonly LayerId[];
	/** False until the pins have dropped in (UI-15, criterion 6). */
	readonly landed: boolean;
	/**
	 * The page's own origin, the scheme, host and port the page came from, on which the tile
	 * addresses are built (US-23, criterion 7).
	 */
	readonly origin: string;
	/** Where the map opens; later moves come through the handle's ports, the functions that move the map. */
	readonly initialView: View;
	readonly handle: MapHandle;
	readonly onViewChange: (view: View) => void;
	readonly onPointer: (position: LngLat | null) => void;
	readonly onClusterOpen: (center: LngLat, expansionLevel: Level) => void;
	readonly onTileFailed: (problem: Problem) => void;
	readonly onLanded: () => void;
	/** The browser cannot draw the map, or has taken away its means to (US-23, criterion 9). */
	readonly onCannotDraw: () => void;
	/** The browser can draw the map again. */
	readonly onCanDrawAgain: () => void;
	/** Called once MapLibre has started, so browser tests can reach the map (see main.tsx). */
	readonly onReady?: (map: MapLibreMap) => void;
}

/** The pin's head sits 25 pixels above its tip, which marks the place (design/canvas.ts). */
const PIN_HEAD = 25;

/** The map of the area, with the places of the layers that are on. */
export function MapCanvas(props: MapCanvasProps): ReactNode {
	const { visible, landed, origin, handle } = props;
	const container = useRef<HTMLDivElement>(null);
	const latest = useLatest(props);
	const { landing, falling, advance } = useLanding(latest);
	const { mapRef, feedRef, theme, loaded, targets } = useMapLibre(container, handle, latest, advance);

	// The style changes only with the layers that are on. The pins' landing changes paint alone, which
	// useStyle applies after every style, so this one leaves the pins unseen.
	const style = useMemo(
		() => buildStyle({ origin, visible, landed: false, theme, fonts: FONT_FILES }),
		[theme, origin, visible],
	);
	useStyle(mapRef, feedRef, style, loaded, landed);

	return (
		<div className="absolute inset-0">
			{/* MapLibre sets its container to position: relative, so the container must take its size from
			    width and height rather than from its edges. */}
			<div ref={container} className="h-full w-full" />
			{!landed && landing.stage === "dropping" && (
				<div aria-hidden="true" className="overlay">
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
			<div role="group" aria-label="Places on the map" className="overlay">
				{targets.map((t) =>
					t.clusterId === null ? (
						<span
							key={t.key}
							role="img"
							aria-label={t.label}
							className="on-point size-11"
							style={{ left: t.x, top: t.y - PIN_HEAD }}
						/>
					) : (
						<button
							key={t.key}
							type="button"
							aria-label={t.label}
							onClick={() => mapRef.current !== null && t.clusterId !== null && openCluster(mapRef.current, t.clusterId, t.point, props.onClusterOpen)}
							className="on-point pointer-events-none size-12 rounded-full border-0 bg-transparent p-0"
							style={{ left: t.x, top: t.y }}
						/>
					),
				)}
			</div>
		</div>
	);
}
