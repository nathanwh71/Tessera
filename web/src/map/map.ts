// map/map.ts: the map's model and its pure update (US-23, UI-09, UI-10, UI-19).
//
// The model is the one object that holds everything the page shows. A message is a value that says
// what happened, such as a click. The update function is pure: it reads only its inputs and changes
// nothing outside itself. From the old model and a message it returns the new model, and may add
// commands, pieces of work to do afterwards, such as moving the map through a port.

import { type Cmd, type InitResult, type Message as AnyMessage, type UpdateMap, cmd } from "react-elmish";
import { LAYER_IDS, type LayerId } from "../layers";
import { type Area, type Bounds, type Level, type LngLat, MAX_ZOOM, MIN_ZOOM, type Problem, type View, clampZoom, level } from "../shared";
import { OPEN_CLUSTER_LEVEL } from "./levels";

/** A layer opened alone, at /{layer}.html, is framed no closer than level 18, so a few places never fill the screen. */
export const FRAME_MAX_LEVEL = level(18);

/** The scale bar's units (UI-19, criterion 4). */
export type Units = "metric" | "imperial";

/**
 * The map itself, behind ports: functions that reach the outside world, kept apart so the update
 * stays pure and the tests can swap in a fake.
 */
export interface Ports {
	/** Frames a box so it fills the view, coming no closer than the level given. */
	readonly fitBounds: (bounds: Bounds, maxLevel: Level) => void;
	/** Moves to a view: a centre and a level. */
	readonly jumpTo: (view: View) => void;
}

/** What the map's handlers receive: its ports, the area, and the layer the page's address names. */
export interface Props {
	readonly ports: Ports;
	readonly area: Area;
	/** The layer named by /{layer}.html, opened alone and in view (US-23, criterion 2). */
	readonly openLayer?: LayerId;
}

/** The map's model. */
export interface Model {
	/** UI-09: where the map is now. */
	readonly view: View;
	/** The layers the map shows: all nine at `/`, one at `/{layer}.html` (US-23, criteria 1 and 2). */
	readonly visible: readonly LayerId[];
	readonly units: Units; // UI-19
	readonly pointer: LngLat | null; // UI-19
	/** The words of the last problem a tile request met, shown over the map (US-23, criterion 8). */
	readonly notice: string | null;
	/** False until the pins have dropped into place as the page opens (UI-15, criterion 6). */
	readonly landed: boolean;
}

/** The map's messages, values that say what happened. */
export type Message =
	| { name: "zoomIn" }
	| { name: "zoomOut" }
	| { name: "viewChanged"; view: View }
	| { name: "toggleUnits" }
	| { name: "pointerMoved"; position: LngLat | null }
	| { name: "clusterOpened"; center: LngLat; expansionLevel: Level }
	| { name: "tileFailed"; problem: Problem }
	| { name: "pinsLanded" };

/** Constructors, the functions that make each message. */
export const Msg = {
	zoomIn: (): Message => ({ name: "zoomIn" }),
	zoomOut: (): Message => ({ name: "zoomOut" }),
	viewChanged: (view: View): Message => ({ name: "viewChanged", view }),
	toggleUnits: (): Message => ({ name: "toggleUnits" }),
	pointerMoved: (position: LngLat | null): Message => ({ name: "pointerMoved", position }),
	/** A cluster, one mark for several places close together, was clicked; `expansionLevel` is the level at which it breaks apart. */
	clusterOpened: (center: LngLat, expansionLevel: Level): Message => ({ name: "clusterOpened", center, expansionLevel }),
	/** A tile request answered with a problem document (US-07, criterion 5). */
	tileFailed: (problem: Problem): Message => ({ name: "tileFailed", problem }),
	pinsLanded: (): Message => ({ name: "pinsLanded" }),
};

/** A command that frames a box; it calls a port and sends no message back. */
const frame = <M extends AnyMessage>(props: Props, bounds: Bounds): Cmd<M> => cmd.ofNone(props.ports.fitBounds, bounds, FRAME_MAX_LEVEL);
/** A command that moves the map to a view. */
const jump = <M extends AnyMessage>(props: Props, view: View): Cmd<M> => cmd.ofNone(props.ports.jumpTo, view);

/**
 * The first model and the first command (US-23, test 2). At `/` the map opens on the area's home view
 * with all nine layers on, and sends no request for a list of layers. At `/{layer}.html` it shows that
 * layer alone and frames the area's box, which holds every place the import let in (US-24).
 */
export function init(props: Props): InitResult<Model, Message> {
	const model: Model = {
		view: props.area.home,
		visible: props.openLayer === undefined ? LAYER_IDS : [props.openLayer],
		units: "metric",
		pointer: null,
		notice: null,
		landed: false,
	};
	return props.openLayer === undefined ? [model] : [model, frame(props, props.area.bounds)];
}

/** False at level 22, where '+' turns grey (UI-09, criterion 2). */
export const canZoomIn = (model: Model): boolean => model.view.zoom < MAX_ZOOM;
/** False at level 0, where '−' turns grey (UI-09, criterion 2). */
export const canZoomOut = (model: Model): boolean => model.view.zoom > MIN_ZOOM;

/** The words to show for a problem document: its `detail`, or its `title` where it has none (US-23, criterion 8). */
export const noticeOf = (problem: Problem): string => problem.detail ?? problem.title;

/** The update function, one handler per message name. It moves the map through its ports and never touches MapLibre itself. */
export const update: UpdateMap<Props, Model, Message> = {
	// UI-09, criterion 1: one whole level about the centre of the view, which doubles or halves the scale.
	zoomIn(_msg, model, props) {
		return canZoomIn(model) ? [{}, jump(props, { ...model.view, zoom: clampZoom(model.view.zoom + 1) })] : [{}];
	},

	zoomOut(_msg, model, props) {
		return canZoomOut(model) ? [{}, jump(props, { ...model.view, zoom: clampZoom(model.view.zoom - 1) })] : [{}];
	},

	viewChanged({ view }) {
		return [{ view: { ...view, zoom: clampZoom(view.zoom) } }];
	},

	toggleUnits(_msg, model) {
		return [{ units: model.units === "metric" ? "imperial" : "metric" }];
	},

	pointerMoved({ position }) {
		return [{ pointer: position }];
	},

	// UI-10, criterion 3: zoom no further than the cluster needs to break apart, and at least to level 16.
	clusterOpened({ center, expansionLevel }, _model, props) {
		return [{}, jump(props, { center, zoom: clampZoom(Math.max(OPEN_CLUSTER_LEVEL, expansionLevel)) })];
	},

	tileFailed({ problem }) {
		return [{ notice: noticeOf(problem) }];
	},

	pinsLanded() {
		return [{ landed: true }];
	},
};
