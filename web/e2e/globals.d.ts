// e2e/globals.d.ts: the types of the two values the browser tests and main.tsx share on the window,
// the browser's own object for the page: the tests set __tesseraTest before the page loads, and
// main.tsx then puts the map on __tesseraMap, so a test can ask MapLibre what it drew. A .d.ts file
// declares types alone and holds no code; the compiler reads it so the tests in e2e/ can name the
// map.
import type { Map as MapLibreMap } from "maplibre-gl";

/**
 * The same two fields main.tsx declares. The tests in e2e/ compile on their own
 * (tsconfig.node.json), apart from src/, so they cannot see main.tsx's declaration and need this
 * copy.
 */
declare global {
	interface Window {
		__tesseraTest?: boolean;
		__tesseraMap?: MapLibreMap;
	}
}
