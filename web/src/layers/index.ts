// The layers module's public face, the list of what other modules may import: the nine layers the map names in its own code.
export type { LayerId, PlaceLayer } from "./layers";
export { LAYERS, LAYER_IDS, groupOf, isLayerId, layerFromPath, layerOf, tileUrl } from "./layers";
