// shared/index.ts: the shared kernel's public face, the list of what other modules may import. The
// kernel holds types and pure functions, which read only their inputs and change nothing outside
// themselves, that every module may use; it imports no module (ADR 0001).
export type { Area } from "./area";
export { MACAU, areaOf, parseArea } from "./area";
export type { Bounds, Level, LngLat, View } from "./geo";
export { BoundsSchema, LngLatSchema, MAX_ZOOM, MIN_ZOOM, clampZoom, level, readBounds } from "./geo";
export { parseJson, parseWith } from "./json";
export { flow, pipe } from "./pipe";
export type { Problem } from "./problem";
export { readProblem } from "./problem";
export type { Result } from "./result";
export { defaultWith, err, flatMap, fold, map, ok } from "./result";
