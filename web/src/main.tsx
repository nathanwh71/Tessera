// main.tsx: the first code the browser runs, which starts the page (US-23). It reads the area the
// service wrote into the page, then draws the App component of the app module with React, the
// library that draws a page from values. App runs the Elmish loop, named after the Elm language, in
// which one update function makes each new model, the value that holds everything the page shows,
// from a message that says what happened. Nothing imports this file; index.html loads it.

import type { Map as MapLibreMap } from "maplibre-gl";
import { StrictMode } from "react";
import { App, mount } from "./app";
import { layerFromPath } from "./layers";
import { createMapHandle } from "./map";
import { areaOf } from "./shared";
import "./styles.css";

declare global {
	interface Window {
		/**
		 * Set by the browser tests before the page loads, so they can reach the map of MapLibre, the
		 * library that draws it (e2e/).
		 */
		__tesseraTest?: boolean;
		__tesseraMap?: MapLibreMap;
	}
}

async function start(root: HTMLElement): Promise<void> {
	// The cluster images, one mark for several places close together, draw their numbers in Roboto,
	// the page's font, on a canvas, a bitmap the page paints on, which waits for no font; so the page
	// waits for the fonts first.
	await Promise.allSettled([document.fonts.load('500 14px "Roboto"'), document.fonts.load('24px "Material Symbols Rounded"')]);
	// The service writes the area into the page as a block of JSON, the text form of a value that
	// programs exchange, outside the part of the page React draws; and the map needs the area before
	// React starts, so this one line reads the page itself. The block never runs, so the page's
	// security policy, the rule that blocks scripts written into the page, lets it through, and the
	// map learns its area with no request of its own. A page with no block, as under `vite` alone,
	// the development server that serves the page without the service, or with one the map cannot
	// read, opens on Macau.
	const area = areaOf(document.getElementById("tessera-area")?.textContent ?? "");
	document.title = `Map of ${area.name}`;
	const openLayer = layerFromPath(location.pathname);
	const onMapReady = window.__tesseraTest === true ? (map: MapLibreMap) => (window.__tesseraMap = map) : undefined;
	mount(
		root,
		<StrictMode>
			<App
				area={area}
				origin={location.origin}
				map={createMapHandle()}
				{...(openLayer === undefined ? {} : { openLayer })}
				{...(onMapReady === undefined ? {} : { onMapReady })}
			/>
		</StrictMode>,
	);
}

const root = document.getElementById("root");
if (root !== null) void start(root);
