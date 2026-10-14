// map/index.ts: the map module's public face, the list of what other modules may import. The app
// module draws the map's parts through it, and runs the map's model, the value that holds everything
// the map shows, and its update, the function that makes the new model from the old and a message;
// the module's own tests import through it too (ADR 0001).
export { formatPosition } from "./coordinates";
export { CLUSTER_PREFIX, PIN_PREFIX, TOOLTIP_IMAGE, pinImage, readImageName } from "./images";
export type { ImageRequest } from "./images";
export { CANNOT_DRAW, clusterLabel, levelText, mergeDistanceMetres, pinLabel, zoomLabel } from "./labels";
export type { MapZoom } from "./levels";
export { LAST_CLUSTER_LEVEL, NAMES_FROM_LEVEL, OPEN_CLUSTER_LEVEL, levelOf, mapZoom, toLevel, toMapZoom } from "./levels";
export type { Drop, Landing, LandingEvent, PinInView } from "./landing";
export { START, step } from "./landing";
export type { Message, Model, Ports, Props, Units } from "./map";
export { FRAME_MAX_LEVEL, Msg, canZoomIn, canZoomOut, init, noticeOf, update } from "./map";
export type { LoadedLayer, PlaceCollection, PlaceFeature, PlaceProperties, SourceFeature } from "./places";
export { fingerprint, placeCollection } from "./places";
export type { ScaleReading } from "./scale";
export { SCALE_MAX_PX, metresPerPixel, roundLengthBelow, scaleBar } from "./scale";
export type { FontFiles, PaintSetting, StyleInput } from "./style";
export { CLUSTER_RADIUS_PX, LAYER, NAME_FONT, SOURCE, TILE_MAX_LEVEL, buildStyle, landingPaint } from "./style";
export type { ClusterNames, Target } from "./targets";
export { nameClusters, openCluster, targetsInView } from "./targets";
export { tileProblem } from "./tiles";
export type { MapCanvasProps } from "./MapCanvas";
export { MapCanvas } from "./MapCanvas";
export type { MapControlsProps } from "./MapControls";
export { Compass, MapControls } from "./MapControls";
export type { MapHandle } from "./map-handle";
export { createMapHandle } from "./map-handle";
export type { ScaleBarProps } from "./ScaleBar";
export { Coordinates, ScaleBar } from "./ScaleBar";
