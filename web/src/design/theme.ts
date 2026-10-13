// design/theme.ts: the colour roles of Material 3, Google's design system, and the group looks that code outside CSS needs, read from the theme.

import type { Shape } from "./shapes";

/** The four groups by their schema, the named set of database tables each comes from (US-24). */
export type GroupKey = "food" | "outdoors" | "culture" | "transport";

/** What marks a group wherever it appears (UI-12): a colour role, a named colour in the theme, and a shape. */
export interface GroupLook {
	readonly shape: Shape;
	/** The CSS variable in the theme that holds the group's colour. */
	readonly colour: `--color-group-${GroupKey}`;
}

/** UI-12, criterion 1: a circle for Food and drink, a triangle for Outdoors, a square for Culture, a diamond for Transport. */
export const GROUP_LOOKS: Readonly<Record<GroupKey, GroupLook>> = {
	food: { shape: "circle", colour: "--color-group-food" },
	outdoors: { shape: "triangle", colour: "--color-group-outdoors" },
	culture: { shape: "square", colour: "--color-group-culture" },
	transport: { shape: "diamond", colour: "--color-group-transport" },
};

/** The group keys in the map's order, which is also the order of a cluster's ring. */
export const GROUP_KEYS: readonly GroupKey[] = ["food", "outdoors", "culture", "transport"];

/**
 * The colours that MapLibre, the library that draws the map, and the canvas need as plain values, since
 * neither reads CSS variables. Each comes from the theme in `styles.css`, so the page holds every colour
 * in one place (UI-15, criterion 1).
 */
export interface Theme {
	readonly surface: string;
	readonly surfaceLowest: string;
	readonly onSurface: string;
	readonly inverseSurface: string;
	readonly inverseOnSurface: string;
	readonly shadow: string;
	readonly groups: Readonly<Record<GroupKey, string>>;
}

/** Builds the theme from a function that reads a CSS variable by name; `readTheme` passes the page's own, and tests pass a fake. */
export function themeFrom(read: (variable: string) => string): Theme {
	return {
		surface: read("--color-surface"),
		surfaceLowest: read("--color-surface-lowest"),
		onSurface: read("--color-on-surface"),
		inverseSurface: read("--color-inverse-surface"),
		inverseOnSurface: read("--color-inverse-on-surface"),
		shadow: read("--color-shadow"),
		groups: {
			food: read(GROUP_LOOKS.food.colour),
			outdoors: read(GROUP_LOOKS.outdoors.colour),
			culture: read(GROUP_LOOKS.culture.colour),
			transport: read(GROUP_LOOKS.transport.colour),
		},
	};
}

/** The theme as the page's stylesheet sets it on the root element, the `<html>` tag. */
export function readTheme(root: Element = document.documentElement): Theme {
	const style = getComputedStyle(root);
	return themeFrom((variable) => style.getPropertyValue(variable).trim());
}
