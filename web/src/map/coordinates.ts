// map/coordinates.ts: the point under the pointer, in words (UI-19, criterion 8). A pure function of
// the map module, which reads only its input; ScaleBar.tsx shows what it writes.

import type { LngLat } from "../shared";

/**
 * One coordinate to two decimal places with its hemisphere, N or S, E or W; a value that rounds to
 * zero takes the positive letter.
 */
function coordinate(value: number, positive: string, negative: string): string {
	const rounded = Math.abs(value).toFixed(2);
	return `${rounded}° ${value < 0 && rounded !== "0.00" ? negative : positive}`;
}

/** Writes a position latitude first, to two decimal places, such as '22.19° N, 113.55° E'. */
export function formatPosition({ lng, lat }: LngLat): string {
	return `${coordinate(lat, "N", "S")}, ${coordinate(lng, "E", "W")}`;
}
