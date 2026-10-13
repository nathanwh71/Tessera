// design/shapes.ts: the four group shapes and the pin, as SVG path data, a string of drawing moves that both SVG and a canvas can draw.

/** The shape each group carries beside its colour (UI-12). */
export type Shape = "circle" | "triangle" | "square" | "diamond";

/**
 * Each shape in a 24 by 24 box, centred on (12, 12), as the prototype draws it. One string serves the
 * map's canvas pins and the pins that drop in as the page opens, so the two can never drift apart.
 */
export const SHAPE_PATHS: Readonly<Record<Shape, string>> = {
	circle: "M12 7.5a4.5 4.5 0 1 1 0 9a4.5 4.5 0 1 1 0-9z",
	triangle: "M12 6.8l5.2 9H6.8z",
	square: "M9 8h6a1 1 0 0 1 1 1v6a1 1 0 0 1-1 1H9a1 1 0 0 1-1-1V9a1 1 0 0 1 1-1z",
	diamond: "M12 6.2l5.8 5.8-5.8 5.8-5.8-5.8z",
};

/** The pin's outline in a 24 by 32 box; its tip at (12, 32) marks the place, and its round head centres on (12, 12). */
export const PIN_PATH = "M12 0C5.4 0 0 5.4 0 12c0 9 12 20 12 20s12-11 12-20C24 5.4 18.6 0 12 0z";
