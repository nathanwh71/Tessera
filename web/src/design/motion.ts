// design/motion.ts: how long things move, for code that animates outside CSS, the page's styles
// (UI-15, criteria 7 and 8). Pure functions, which read only their inputs, and numbers of the design
// module; the map's handle and its landing use them, and styles.css holds the same times for what
// CSS animates.

/**
 * The durations of Material, Google's design system, in milliseconds, as the design sets them:
 * 300 ms is the ceiling for any movement.
 */
export const MOTION = { short: 150, exit: 200, medium: 300 } as const;

/**
 * True when the visitor has asked their operating system to reduce motion, which the browser passes
 * on.
 */
export function prefersReducedMotion(): boolean {
	// Tests run in Node, which has no matchMedia; there the answer is no.
	return typeof matchMedia === "function" && matchMedia("(prefers-reduced-motion: reduce)").matches;
}

/**
 * How long a movement of the map lasts: Material's medium duration, or none for a visitor who asks
 * for less motion.
 */
export function movementMs(reduced: boolean): number {
	return reduced ? 0 : MOTION.medium;
}

/**
 * Material's emphasised easing, the curve `cubic-bezier(0.2, 0, 0, 1)` that starts fast and settles
 * slowly, as a function of time from 0 to 1. MapLibre, the library that draws the map, takes a
 * function for its camera rather than a CSS curve. The curve gives x and y from a parameter u, so
 * this finds the u whose x is t by Newton's method, a guess improved by the slope a few times, then
 * returns that u's y.
 */
export function emphasised(t: number): number {
	const [x1, y1, x2, y2] = [0.2, 0, 0, 1];
	const bezier = (u: number, a: number, b: number): number => 3 * a * u * (1 - u) ** 2 + 3 * b * u ** 2 * (1 - u) + u ** 3;
	const slope = (u: number, a: number, b: number): number => 3 * a * (1 - u) ** 2 + 6 * (b - a) * u * (1 - u) + 3 * (1 - b) * u ** 2;
	const solve = (u: number, step: number): number => {
		const dx = bezier(u, x1, x2) - t;
		const d = slope(u, x1, x2);
		return step === 0 || Math.abs(dx) < 1e-6 || d === 0 ? u : solve(u - dx / d, step - 1);
	};
	return t <= 0 ? 0 : t >= 1 ? 1 : bezier(solve(t, 8), y1, y2);
}

/**
 * The gap between one pin's drop and the next, in milliseconds, as the prototype spaces them (UI-15,
 * criterion 6).
 */
export const DROP_STEP_MS = 30;

/**
 * The latest any pin starts to drop, in milliseconds, so a crowded view still settles soon after it
 * opens.
 */
export const DROP_SPREAD_MS = 600;

/**
 * UI-15, criterion 6: as the page opens, each pin drops into place over 300 ms, one after another.
 * Returns each pin's delay in milliseconds, in the order given: 30 ms apart, closer together when
 * there are so many pins that the last would start after 600 ms. A visitor who asks for less motion
 * gets no drop at all, so no delays (criterion 8).
 */
export function dropDelays(count: number, reduced: boolean): readonly number[] {
	if (reduced || count <= 0) return [];
	const step = count <= 1 ? 0 : Math.min(DROP_STEP_MS, DROP_SPREAD_MS / (count - 1));
	return Array.from({ length: count }, (_, i) => Math.round(i * step));
}

/** How long the whole drop lasts, in milliseconds, from the first pin's start to the last pin's rest. */
export function dropMs(delays: readonly number[]): number {
	return delays.length === 0 ? 0 : (delays.at(-1) ?? 0) + MOTION.medium;
}
