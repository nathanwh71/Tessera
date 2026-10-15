// map/MapControls.tsx: the compass and the zoom buttons at the top right (UI-09, UI-19). They are
// React components: React is the library that draws the page from values, and a component is a
// function that turns part of the model, the one object that holds everything the page shows, into
// what you see. App.tsx places them and turns each press into a message for the update function.

import type { ReactNode } from "react";
import { Icon } from "../design";
import { zoomLabel } from "./labels";

/**
 * Props for the controls: the inputs the component receives. They are plain values and callbacks, so
 * the controls hold no state and the ***model*** stays the one source of truth (ADR 0004).
 */
export interface MapControlsProps {
	/**
	 * The current web-map zoom level, 0 for the whole world and 22 at its closest, which only the
	 * buttons' labels say (UI-09, criterion 3).
	 */
	readonly level: number;
	/** False at either end of the range, where that button turns grey (UI-09, criterion 2). */
	readonly canZoomIn: boolean;
	readonly canZoomOut: boolean;
	/** Called on a press, so the app can send the ***message*** that moves the map one level (UI-09). */
	readonly onZoomIn: () => void;
	readonly onZoomOut: () => void;
}

/**
 * UI-19, criteria 5 and 6: a compass that points north, with a red needle and a red 'N'. The map never
 * turns, so it is a sign, not a button.
 */
export function Compass(): ReactNode {
	return (
		<div role="img" aria-label="Compass: north is up" className="mr-1 flex size-12 items-center justify-center rounded-full bg-surface-low shadow-level-2">
			{/* A 40-pixel face inside the 48-pixel disc, drawn on a grid of 40 units: the ring of
			    radius 16 leaves room for the ticks at east, south and west, and the 'N' sits above the
			    needle in place of the north tick, so north reads first. */}
			<svg width="40" height="40" viewBox="0 0 40 40" aria-hidden="true">
				<circle cx="20" cy="20" r="16" className="fill-none stroke-outline-variant" strokeWidth="1" />
				<path d="M36 20h-3M20 36v-3M4 20h3" className="fill-none stroke-on-surface-variant" strokeWidth="1.5" strokeLinecap="round" />
				<text x="20" y="10.2" textAnchor="middle" className="fill-north" style={{ font: "700 7.5px var(--font-sans)" }}>
					N
				</text>
				<path d="M20 12.5L23.6 20h-7.2z" className="fill-north" />
				<path d="M20 27.5L23.6 20h-7.2z" className="fill-on-surface-variant" />
				<circle cx="20" cy="20" r="1.6" className="fill-surface-lowest" />
			</svg>
		</div>
	);
}

/**
 * One zoom button, '+' or '−'. Both share this one component, so they cannot drift apart in size,
 * label or behaviour at the end of the range (UI-09, criteria 2 and 3).
 */
function ZoomButton({ direction, level, enabled, onPress }: { direction: "in" | "out"; level: number; enabled: boolean; onPress: () => void }): ReactNode {
	return (
		<button
			type="button"
			aria-label={zoomLabel(direction, level)}
			// UI-09, criterion 2: grey at the end of the range, yet still in the Tab order, the order the
			// Tab key visits controls in, so a screen reader, the program that reads a page aloud, still
			// says its label.
			aria-disabled={!enabled}
			onClick={() => enabled && onPress()}
			className="state-layer map-button"
		>
			<Icon name={direction === "in" ? "add" : "remove"} />
		</button>
	);
}

/** The compass above two small buttons that move one whole level (UI-09, criterion 1). */
export function MapControls({ level, canZoomIn, canZoomOut, onZoomIn, onZoomOut }: MapControlsProps): ReactNode {
	return (
		<>
			<Compass />
			<div className="flex flex-col gap-2 pr-1.5">
				<ZoomButton direction="in" level={level} enabled={canZoomIn} onPress={onZoomIn} />
				<ZoomButton direction="out" level={level} enabled={canZoomOut} onPress={onZoomOut} />
			</div>
		</>
	);
}
