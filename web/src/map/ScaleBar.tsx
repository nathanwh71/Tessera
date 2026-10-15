// map/ScaleBar.tsx: the scale bar and the coordinates under the pointer, at the bottom left (UI-19).
// They are React components: React is the library that draws the page from values, and a component
// is a function that turns part of the model, the one object that holds everything the page shows,
// into what you see. App.tsx places them; scale.ts and coordinates.ts work out the words they show.

import type { ReactNode } from "react";
import type { LngLat } from "../shared";
import { formatPosition } from "./coordinates";
import type { Units } from "./map";
import { scaleBar } from "./scale";

/** Props for the scale bar: the inputs the component receives. */
export interface ScaleBarProps {
	/** The web-map zoom level, 0 for the whole world and 22 at its closest. */
	readonly level: number;
	/** The latitude of the map's centre, where the scale holds; it stretches further north and south. */
	readonly latitude: number;
	readonly units: Units;
	readonly onToggleUnits: () => void;
}

/**
 * A bar of four sections, filled and empty in turn, marked '0', the halfway figure and the full length
 * (UI-19, criteria 1 and 2). A click switches the units, and the label names the units it will switch to.
 */
export function ScaleBar({ level, latitude, units, onToggleUnits }: ScaleBarProps): ReactNode {
	const bar = scaleBar(level, latitude, units);
	const width = { width: `${bar.widthPx}px` };
	return (
		<button
			type="button"
			aria-label={bar.label}
			onClick={onToggleUnits}
			className="state-layer card flex min-h-11 cursor-pointer flex-col justify-center gap-[3px] border-0 pt-2 pr-3.5 pb-1.5 pl-3"
		>
			<span
				aria-hidden="true"
				className="scale-sections block h-2 rounded-[2px] border-[1.5px] border-solid border-on-surface transition-[width] duration-300 ease-emphasized"
				style={width}
			/>
			<span aria-hidden="true" className="relative block h-4 text-label-small text-on-surface-variant tabular-nums transition-[width] duration-300 ease-emphasized" style={width}>
				<span className="absolute left-0">0</span>
				<span className="absolute left-1/2 -translate-x-1/2">{bar.half}</span>
				<span className="absolute right-0 whitespace-nowrap text-on-surface">{bar.full}</span>
			</span>
		</button>
	);
}

/**
 * The point under the mouse pointer, latitude first, beside the scale bar (UI-19, criteria 7 to 9).
 * It shows only while the pointer is over the map, and a screen reader, the program that reads a page
 * aloud, skips it, since it follows the pointer alone and a screen reader has none.
 */
export function Coordinates({ position }: { readonly position: LngLat | null }): ReactNode {
	if (position === null) return null;
	return (
		<span aria-hidden="true" data-testid="coordinates" className="card flex items-center whitespace-nowrap px-3.5 text-label-medium tabular-nums">
			{formatPosition(position)}
		</span>
	);
}
