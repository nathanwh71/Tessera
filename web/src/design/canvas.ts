// design/canvas.ts: pins, tooltips and clusters, each one mark for several places close together,
// drawn on a canvas, a bitmap the page can paint on, for the symbol layers of MapLibre, the library
// that draws the map (UI-10 to UI-12). A symbol layer draws an icon or a name at each point. The
// design module holds these drawings, and map/maplibre.ts calls each one the first time MapLibre asks
// for its image.
//
// MapLibre draws images, not SVG, the text form of a drawing that a web page can show. So the shapes
// in shapes.ts are painted here at twice the size and handed over with a pixel ratio of 2, which
// keeps them sharp on screens that pack two device pixels into each CSS pixel, the unit the page's
// styles measure in.

import { PIN_PATH, SHAPE_PATHS, type Shape } from "./shapes";
import { GROUP_KEYS, type GroupKey, type Theme } from "./theme";

/**
 * An image as `map.addImage` takes it: its size and its pixels, four bytes each (red, green, blue,
 * alpha, the last being how opaque the pixel is).
 */
export interface MapImage {
	readonly width: number;
	readonly height: number;
	readonly data: Uint8ClampedArray;
}

/** Device pixels per CSS pixel in every image; pass the same to `addImage` as `pixelRatio`. */
export const IMAGE_PIXEL_RATIO = 2;

/** Room around a pin or a cluster for its shadow, in CSS pixels; MapLibre counts it as part of the icon. */
export const IMAGE_MARGIN = 4;
/** A short name for the margin, since the drawings below use it many times. */
const MARGIN = IMAGE_MARGIN;

/** The pin's size on the map in CSS pixels: 30 by 40, as in the prototype. */
export const PIN_SIZE = { width: 30, height: 40 } as const;

/**
 * How far the centre of a pin's round head sits above its tip, in CSS pixels: 25. In the 24 by 32
 * box of `PIN_PATH` (shapes.ts) the head centres at y 12 and the tip at y 32, 20 units apart, and the
 * pin draws 30 / 24 = 1.25 times that box. A name beside a pin sits at this height, and a pin's
 * invisible picture centres here, so both line up with the head the eye sees.
 */
export const PIN_HEAD_ABOVE_TIP = ((32 - 12) * PIN_SIZE.width) / 24;

/** The cluster's outer diameter and its white disc, in CSS pixels, as in the prototype. */
const CLUSTER = { outer: 48, inner: 34 } as const;

/**
 * A blank canvas for an image of the given size in CSS pixels. The bitmap is twice as large, and the
 * scale lets every drawing below still measure in CSS pixels, as the prototype does.
 */
function canvasOf(width: number, height: number): CanvasRenderingContext2D {
	const canvas = document.createElement("canvas");
	canvas.width = width * IMAGE_PIXEL_RATIO;
	canvas.height = height * IMAGE_PIXEL_RATIO;
	const context = canvas.getContext("2d");
	if (context === null) throw new Error("this browser cannot draw on a canvas");
	context.scale(IMAGE_PIXEL_RATIO, IMAGE_PIXEL_RATIO);
	return context;
}

/** The canvas's pixels in the form `map.addImage` takes, since MapLibre cannot take a canvas. */
function imageOf(context: CanvasRenderingContext2D): MapImage {
	const { width, height } = context.canvas;
	return { width, height, data: context.getImageData(0, 0, width, height).data };
}

/**
 * Paints a soft shadow under a path, an outline to fill: the path filled in the theme's shadow
 * colour, blurred and faded. A blur filter stands in for the two-part shadow of Material, Google's
 * design system, which a canvas cannot draw.
 */
function shadow(context: CanvasRenderingContext2D, path: Path2D, theme: Theme, lift: 1 | 2 | 3): void {
	context.save();
	context.filter = `blur(${lift}px)`;
	// 30 per cent, the strength of Material's key shadow; the dropping pins use the same (styles.css).
	context.globalAlpha = 0.3;
	context.translate(0, lift);
	context.fillStyle = theme.shadow;
	context.fill(path);
	context.restore();
}

/**
 * A pin in the group's colour with its shape in white (UI-12), with a soft shadow. A pin is a sign,
 * not a button: it marks a place and opens nothing (US-23, criterion 6).
 *
 * Returns the image and the CSS pixels from the pin's tip to the image's bottom edge, which the
 * symbol layer's `icon-offset` takes back so the tip sits on the place.
 */
export function drawPin(theme: Theme, group: GroupKey, shape: Shape): { image: MapImage; tipGap: number } {
	// The pin's path is drawn in a 24 by 32 box (shapes.ts), so it grows to the pin's 30-pixel width.
	const scale = PIN_SIZE.width / 24;
	const [width, height] = [24 * scale + 2 * MARGIN, 32 * scale + 2 * MARGIN];
	const context = canvasOf(width, height);
	context.translate(MARGIN, MARGIN);
	context.scale(scale, scale);
	const pin = new Path2D(PIN_PATH);
	shadow(context, pin, theme, 1);
	context.fillStyle = theme.groups[group];
	context.fill(pin);
	context.fillStyle = theme.surfaceLowest;
	context.fill(new Path2D(SHAPE_PATHS[shape]));
	// The tip touches the bottom of the path's box, so only the margin lies below it.
	return { image: imageOf(context), tipGap: MARGIN };
}

/** How MapLibre may stretch an image around text, in device pixels, as `addImage` takes it. */
export interface StretchMetadata {
	readonly stretchX: [[number, number]];
	readonly stretchY: [[number, number]];
	readonly content: [number, number, number, number];
}

/**
 * Material's plain tooltip (UI-11), the small box that holds a name: dark, with 4-pixel rounded
 * corners. MapLibre stretches its middle to fit each name, keeping the corners whole.
 */
export function drawTooltip(theme: Theme): { image: MapImage; stretch: StretchMetadata } {
	// Material's 4-pixel corners (UI-15, criterion 4); 16 pixels leaves a middle between the corners
	// for MapLibre to stretch.
	const [size, corner] = [16, 4];
	const context = canvasOf(size, size);
	context.beginPath();
	context.roundRect(0, 0, size, size, corner);
	context.fillStyle = theme.inverseSurface;
	context.fill();
	const [from, to] = [corner * IMAGE_PIXEL_RATIO, (size - corner) * IMAGE_PIXEL_RATIO];
	return { image: imageOf(context), stretch: { stretchX: [[from, to]], stretchY: [[from, to]], content: [from, from, to, to] } };
}

/**
 * A cluster (UI-10): its number on a white disc, ringed in the colours of its groups in proportion to
 * their places, in the map's order, with Material's level-2 shadow. It draws once and never moves.
 */
export function drawCluster(theme: Theme, counts: Readonly<Record<GroupKey, number>>): MapImage {
	const size = CLUSTER.outer + 2 * MARGIN;
	const context = canvasOf(size, size);
	const centre = size / 2;
	const total = GROUP_KEYS.reduce((sum, g) => sum + counts[g], 0);
	const disc = new Path2D();
	disc.arc(centre, centre, CLUSTER.outer / 2, 0, 2 * Math.PI);
	shadow(context, disc, theme, 2);

	// Angles are in radians; the ring starts at twelve o'clock and runs clockwise, as the prototype's
	// conic gradient, a sweep of colour around a centre, does.
	const start = -Math.PI / 2;
	GROUP_KEYS.filter((g) => counts[g] > 0).reduce((from, g) => {
		// At least 1, so a cluster with no counts cannot divide by zero.
		const to = from + (2 * Math.PI * counts[g]) / Math.max(total, 1);
		context.beginPath();
		context.moveTo(centre, centre);
		context.arc(centre, centre, CLUSTER.outer / 2, from, to);
		context.closePath();
		context.fillStyle = theme.groups[g];
		context.fill();
		return to;
	}, start);

	context.beginPath();
	context.arc(centre, centre, CLUSTER.inner / 2, 0, 2 * Math.PI);
	context.fillStyle = theme.surfaceLowest;
	context.fill();

	context.fillStyle = theme.onSurface;
	context.font = '500 14px "Roboto"';
	context.textAlign = "center";
	context.textBaseline = "middle";
	// One pixel low: digits carry no descenders, so text centred by its box looks high to the eye.
	context.fillText(String(total), centre, centre + 1);
	return imageOf(context);
}
