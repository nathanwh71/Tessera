// map/levels.ts: web-map zoom levels (UI-09) and the zoom of MapLibre, the map-drawing library, which
// are not the same number.
//
// The stories count levels as every web map does: level 0 shows the world in one tile (a square piece
// of the map) 256 pixels wide, and each level up doubles the scale, so the scale bar reads 500 m at
// level 15 (UI-19). MapLibre draws the world 512 pixels wide at its zoom 0, so its zoom shows the same
// scale one number lower. Everything the visitor sees, and the model, speaks in levels; only the map's
// view code and its style speak MapLibre's zoom, through these functions. Each scale has a type of its
// own, `Level` and `MapZoom`, so the compiler refuses one where the other belongs (ADR 0004).

import { type Level, clampZoom, flow, level } from "../shared";

/** MapLibre's zoom: one number below the web-map level for the same scale. A brand, for the compiler alone. */
export type MapZoom = number & { readonly unit: "mapZoom" };

/** Marks a number MapLibre hands over as MapLibre's zoom. */
export const mapZoom = (n: number): MapZoom => n as MapZoom;

/** MapLibre's zoom for a web-map level. */
export function toMapZoom(webLevel: Level): MapZoom {
	return mapZoom(webLevel - 1);
}

/** The web-map level for MapLibre's zoom. */
export function toLevel(zoom: MapZoom): Level {
	return level(zoom + 1);
}

/** A number MapLibre hands over as its zoom, read as a level within 0 to 22: the one way such a number becomes a level. */
export const levelOf = flow(mapZoom, toLevel, clampZoom);

/** UI-11, criterion 1: names show from level 16. */
export const NAMES_FROM_LEVEL = level(16);

/** UI-17, criterion 6: places stay in clusters, one mark for several places close together, below level 16, so the last level that clusters is 15. */
export const LAST_CLUSTER_LEVEL = level(15);

/** UI-10, criterion 3: a click on a cluster zooms at least to level 16. */
export const OPEN_CLUSTER_LEVEL = level(16);
