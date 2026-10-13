// map/MapControls.tsx: the compass and the zoom buttons at the top right (UI-09, UI-19).

import type { ReactNode } from "react";
import { Icon } from "../design";
import { zoomLabel } from "./labels";

/** Props for the controls: the inputs the component, a function that turns part of the model into what you see, receives. */
export interface MapControlsProps {
	/** The current web-map zoom level, 0 for the whole world and 22 at its closest, which only the buttons' labels say (UI-09, criterion 3). */
	readonly level: number;
	readonly canZoomIn: boolean;
	readonly canZoomOut: boolean;
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

function ZoomButton({ direction, level, enabled, onPress }: { direction: "in" | "out"; level: number; enabled: boolean; onPress: () => void }): ReactNode {
	return (
		<button
			type="button"
			aria-label={zoomLabel(direction, level)}
			// UI-09, criterion 2: grey at the end of the range, yet still in the Tab order, so a screen reader still says its label.
			aria-disabled={!enabled}
			onClick={() => enabled && onPress()}
			className="state-layer flex size-11 cursor-pointer items-center justify-center rounded-medium border-0 bg-surface-high text-on-surface-variant shadow-level-2"
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
