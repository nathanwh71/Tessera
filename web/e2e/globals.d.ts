// e2e/globals.d.ts: what main.tsx puts on the window for the browser tests.
import type { Map as MapLibreMap } from "maplibre-gl";

declare global {
	interface Window {
		__tesseraTest?: boolean;
		__tesseraMap?: MapLibreMap;
	}
}
