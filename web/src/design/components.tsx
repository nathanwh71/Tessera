// design/components.tsx: the small parts the map's controls build from, drawn after Material 3,
// Google's design system (UI-15). They are React components: React is the library that draws the
// page from values, and a component is a function that turns part of the model, the one object that
// holds everything the page shows, into what you see. The design module holds them, and the map's
// MapControls.tsx and MapCanvas.tsx use them.
//
// Every control carries the `state-layer` class, which styles.css turns into Material's 8 per cent
// hover and 12 per cent press tint.

import type { MaterialSymbol } from "material-symbols";
import type { ReactNode } from "react";
import { PIN_PATH, SHAPE_PATHS } from "./shapes";
import { GROUP_LOOKS, type GroupKey } from "./theme";

/**
 * Props, the inputs a component receives, for an icon from Material Symbols, Google's icon font, in
 * its rounded style (UI-15, criterion 2).
 */
export interface IconProps {
	readonly name: MaterialSymbol;
	readonly size?: 18 | 24;
	readonly className?: string;
}

/**
 * An icon from Material Symbols, rounded. A screen reader, the program that reads a page aloud, skips
 * it, so the control that holds it must carry the words.
 */
export function Icon({ name, size = 24, className = "" }: IconProps): ReactNode {
	return (
		<span aria-hidden="true" className={`icon ${className}`} style={{ fontSize: `${size}px` }}>
			{name}
		</span>
	);
}

/**
 * A pin as an SVG drawing, the text form of a drawing a web page can show, 30 by 40 pixels: the
 * group's colour with its shape in white (UI-12). The map's own pins are images that MapLibre, the
 * library that draws the map, draws; this one draws the same paths for the pins that drop in as the
 * page opens (UI-15, criterion 6).
 */
export function PinShape({ group }: { readonly group: GroupKey }): ReactNode {
	const look = GROUP_LOOKS[group];
	return (
		<svg width="30" height="40" viewBox="0 0 24 32" aria-hidden="true" className="block">
			<path d={PIN_PATH} style={{ fill: `var(${look.colour})` }} />
			<path d={SHAPE_PATHS[look.shape]} className="fill-surface-lowest" />
		</svg>
	);
}
