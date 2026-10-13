// The shared kernel: types and pure functions, which read only their inputs and change nothing outside themselves, that every module may use. It imports no module.
export type { Area } from "./area";
export { MACAU, areaOf, parseArea } from "./area";
export type { Bounds, Level, LngLat, View } from "./geo";
export { MAX_ZOOM, MIN_ZOOM, clampZoom, level, readBounds, readLngLat } from "./geo";
export { isFiniteNumber, isRecord, parseJson } from "./json";
export { flow, pipe } from "./pipe";
export type { Problem } from "./problem";
export { readProblem } from "./problem";
export type { Result } from "./result";
export { defaultWith, err, flatMap, fold, map, ok } from "./result";
