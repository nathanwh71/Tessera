// e2e/globals.d.ts: the types of what main.tsx puts on the window, the browser's own object for the
// page, for the browser tests. A .d.ts file declares types alone and holds no code; the compiler
// reads it so the tests in e2e/ can name the map.
import type { Map as MapLibreMap } from "maplibre-gl";

declare global {
	interface Window {
		__tesseraTest?: boolean;
		__tesseraMap?: MapLibreMap;
	}
}
