// main.tsx: starts the page. Reads the area the service wrote into the page, then hands it to
// React-Elmish, the library that runs the update loop (US-23).

import type { Map as MapLibreMap } from "maplibre-gl";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./app";
import { layerFromPath } from "./layers";
import { createMapHandle } from "./map";
import { areaOf } from "./shared";
import "./styles.css";

declare global {
	interface Window {
		/** Set by the browser tests before the page loads, so they can reach the map (e2e/). */
		__tesseraTest?: boolean;
		__tesseraMap?: MapLibreMap;
	}
}

async function start(root: HTMLElement): Promise<void> {
	// The cluster images draw their numbers in Roboto on a canvas, a bitmap the page paints on, which
	// waits for no font; so the page waits for the fonts first.
	await Promise.allSettled([document.fonts.load('500 14px "Roboto"'), document.fonts.load('24px "Material Symbols Rounded"')]);
	// The service writes the area into the page as a JSON block, outside the part of the page React
	// draws, and the map needs the area before React starts; so this one line reads the page itself.
	// The block never runs, so the page's security policy, which blocks inline scripts, lets it
	// through, and the map learns its area with no request of its own. A page with no block, as
	// under `vite` alone, or with one the map cannot read, opens on Macau.
	const area = areaOf(document.getElementById("tessera-area")?.textContent ?? "");
	document.title = `Map of ${area.name}`;
	const openLayer = layerFromPath(location.pathname);
	const onMapReady = window.__tesseraTest === true ? (map: MapLibreMap) => (window.__tesseraMap = map) : undefined;
	createRoot(root).render(
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
