// map/map.ts: the map's model and its pure update (US-23, UI-09, UI-10, UI-19). This is the heart of
// the map module, in the Elmish pattern named after the Elm language; app/App.tsx hands it to the
// page as the page's one model and update, and every test of the map's rules runs it with values
// alone.
//
// The model is the one object that holds everything the page shows. A message is a value that says
// what happened, such as a click. The update function is pure: it reads only its inputs and changes
// nothing outside itself (ADR 0004). From the old model and a message it returns the new model, and
// may add commands, pieces of work to do afterwards, such as moving the map through a port, a
// function that reaches the outside world.

import { type Cmd, type InitResult, type Message as AnyMessage, type UpdateMap, type UpdateReturnType, cmd } from "react-elmish";
import { LAYER_IDS, type LayerId } from "../layers";
import { type Area, type Bounds, type Level, type LngLat, MAX_ZOOM, MIN_ZOOM, type Problem, type View, clampZoom, level } from "../shared";
import { OPEN_CLUSTER_LEVEL } from "./levels";

/**
 * A ***layer*** opened alone, at /{layer}.html, is framed no closer than level 18, so a few places
 * never fill the screen.
 */
export const FRAME_MAX_LEVEL = level(18);

/** The scale bar's units (UI-19, criterion 4). */
export type Units = "metric" | "imperial";

/**
 * The map itself, behind its ***ports***, kept apart so the ***update*** stays pure and the tests can
 * swap in a fake. map-handle.ts carries them out through MapLibre, the library that draws the map.
 */
export interface Ports {
	/**
	 * Frames a box so it fills the view, coming no closer than the level given: how /{layer}.html
	 * shows its layer in view (US-23, criterion 2).
	 */
	readonly fitBounds: (bounds: Bounds, maxLevel: Level) => void;
	/**
	 * Moves to a view, a centre and a level: how a zoom button or a cluster's click moves the map
	 * (UI-09, criterion 1; UI-10, criterion 3).
	 */
	readonly jumpTo: (view: View) => void;
}

/** What the map's handlers receive: its ports, the area, and the layer the page's address names. */
export interface Props {
	/** The way out to the map, passed in so the update never touches MapLibre (ADR 0004). */
	readonly ports: Ports;
	/** The area the office maps, which gives the home view and the box to frame (US-23, US-24). */
	readonly area: Area;
	/** The layer named by /{layer}.html, opened alone and in view (US-23, criterion 2). */
	readonly openLayer?: LayerId;
}

/**
 * The map's ***model***: everything the map's part of the page shows, in one read-only value, so each
 * screen follows from it alone and the tests can check it with values (US-23, criterion 3).
 */
export interface Model {
	/** UI-09: where the map is now. */
	readonly view: View;
	/** The layers the map shows: all nine at `/`, one at `/{layer}.html` (US-23, criteria 1 and 2). */
	readonly visible: readonly LayerId[];
	readonly units: Units; // UI-19
	readonly pointer: LngLat | null; // UI-19
	/**
	 * The words of the last problem a request for a ***tile***, a square piece of the map, met, shown
	 * over the map (US-23, criterion 8).
	 */
	readonly notice: string | null;
	/** False until the pins have dropped into place as the page opens (UI-15, criterion 6). */
	readonly landed: boolean;
	/**
	 * False while the browser cannot draw the map, because it has no WebGL, its way to draw with the
	 * graphics card, or has taken it away (US-23, criterion 9).
	 */
	readonly drawable: boolean;
}

/**
 * The map's ***messages***, values that say what happened. A closed set of named cases, so the update
 * must answer each one and the compiler flags one it misses (ADR 0004).
 */
export type Message =
	| { name: "zoomIn" }
	| { name: "zoomOut" }
	| { name: "viewChanged"; view: View }
	| { name: "toggleUnits" }
	| { name: "pointerMoved"; position: LngLat | null }
	| { name: "clusterOpened"; center: LngLat; expansionLevel: Level }
	| { name: "tileFailed"; problem: Problem }
	| { name: "pinsLanded" }
	| { name: "cannotDraw" }
	| { name: "canDrawAgain" };

/**
 * Constructors, the functions that make each message. The app and the tests build messages only
 * through these, so a misspelt name or a missing field fails to compile rather than at run time.
 */
export const Msg = {
	/** A press of '+' or '−', or of a zoom key (UI-09, criterion 1). */
	zoomIn: (): Message => ({ name: "zoomIn" }),
	zoomOut: (): Message => ({ name: "zoomOut" }),
	/** The map moved, by the visitor or a port, so the model keeps the view the buttons read (UI-09). */
	viewChanged: (view: View): Message => ({ name: "viewChanged", view }),
	/** A click on the scale bar (UI-19, criterion 4). */
	toggleUnits: (): Message => ({ name: "toggleUnits" }),
	/** The pointer moved over the map, or left it; the readout follows (UI-19, criteria 7 and 8). */
	pointerMoved: (position: LngLat | null): Message => ({ name: "pointerMoved", position }),
	/**
	 * A ***cluster***, one mark for several places close together, was clicked; `expansionLevel` is the
	 * level at which it breaks apart.
	 */
	clusterOpened: (center: LngLat, expansionLevel: Level): Message => ({ name: "clusterOpened", center, expansionLevel }),
	/**
	 * A tile request answered with a ***problem document***, the service's form of an error in JSON,
	 * the text form of a value that programs exchange (US-07, criterion 5).
	 */
	tileFailed: (problem: Problem): Message => ({ name: "tileFailed", problem }),
	/** The dropping pins have landed, so the map's own pins show (UI-15, criterion 6). */
	pinsLanded: (): Message => ({ name: "pinsLanded" }),
	/** The browser cannot draw the map, or can again (US-23, criterion 9). */
	cannotDraw: (): Message => ({ name: "cannotDraw" }),
	canDrawAgain: (): Message => ({ name: "canDrawAgain" }),
};

/**
 * A command that frames a box; it calls a port and sends no message back, since the map's own move
 * events report where it ends up.
 */
const frame = <M extends AnyMessage>(props: Props, bounds: Bounds): Cmd<M> => cmd.ofNone(props.ports.fitBounds, bounds, FRAME_MAX_LEVEL);
/**
 * A command that moves the map to a view. Like `frame`, it sends nothing back: the map reports its
 * new view through viewChanged.
 */
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
		drawable: true,
	};
	return props.openLayer === undefined ? [model] : [model, frame(props, props.area.bounds)];
}

/** False at level 22, where '+' turns grey (UI-09, criterion 2). */
export const canZoomIn = (model: Model): boolean => model.view.zoom < MAX_ZOOM;
/** False at level 0, where '−' turns grey (UI-09, criterion 2). */
export const canZoomOut = (model: Model): boolean => model.view.zoom > MIN_ZOOM;

/**
 * The words to show for a problem document: its `detail`, or its `title` where it has none (US-23,
 * criterion 8).
 */
export const noticeOf = (problem: Problem): string => problem.detail ?? problem.title;

/** One level in or out, as far as the map allows: the buttons and the keys share it (UI-09). */
function zoomBy(step: 1 | -1, model: Model, props: Props): UpdateReturnType<Model, Message> {
	const allowed = step > 0 ? canZoomIn(model) : canZoomOut(model);
	return allowed ? [{}, jump(props, { ...model.view, zoom: clampZoom(model.view.zoom + step) })] : [{}];
}

/**
 * The update function, one handler, a function for one message, per message name. Each returns the
 * fields that change, and a command where one is due. It moves the map through its ports and never
 * touches MapLibre itself.
 */
export const update: UpdateMap<Props, Model, Message> = {
	// UI-09, criterion 1: one whole level about the centre of the view, which doubles or halves the
	// scale.
	zoomIn: (_msg, model, props) => zoomBy(1, model, props),
	zoomOut: (_msg, model, props) => zoomBy(-1, model, props),

	// UI-09, criterion 1: the level stops at 0 and at 22. The update clamps it itself rather than
	// trust the sender, so the model never holds a level outside the range, whoever sent the view.
	viewChanged({ view }) {
		return [{ view: { ...view, zoom: clampZoom(view.zoom) } }];
	},

	// UI-19, criterion 4: a click switches between metric and imperial.
	toggleUnits(_msg, model) {
		return [{ units: model.units === "metric" ? "imperial" : "metric" }];
	},

	// UI-19, criteria 7 and 8: the readout follows the pointer, and null hides it.
	pointerMoved({ position }) {
		return [{ pointer: position }];
	},

	// UI-10, criterion 3: zoom no further than the cluster needs to break apart, and at least to
	// level 16.
	clusterOpened({ center, expansionLevel }, _model, props) {
		return [{}, jump(props, { center, zoom: clampZoom(Math.max(OPEN_CLUSTER_LEVEL, expansionLevel)) })];
	},

	// US-23, criterion 8: the page shows the problem's words over the map.
	tileFailed({ problem }) {
		return [{ notice: noticeOf(problem) }];
	},

	// UI-15, criterion 6: the map's own pins and names show once the drop is over.
	pinsLanded() {
		return [{ landed: true }];
	},
	// US-23, criterion 9: words take the controls' place while the browser cannot draw, and the
	// controls come back once it can.
	cannotDraw() {
		return [{ drawable: false }];
	},
	canDrawAgain() {
		return [{ drawable: true }];
	},
};
