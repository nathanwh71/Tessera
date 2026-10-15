// app/App.tsx: the page, as a React component (US-23). React is the library that draws the page from
// values, and a component is a function that turns such values into what you see. This one runs the
// Elmish loop, named after the Elm language: it holds one model, the one object that holds everything
// the page shows, and one update function, which turns the old model and a message, a value that says
// what happened, into the new model. main.tsx draws it once as the page starts; the map module
// supplies every part it lays out.

import type { Map as MapLibreMap } from "maplibre-gl";
import { type ReactNode, useMemo } from "react";
import { useElmish } from "react-elmish";
import type { LayerId } from "../layers";
import * as MapView from "../map";
import type { Area } from "../shared";
import { type Message, type Model, Msg, type Props, init, update } from "../map";

/** Props for the page: the inputs a component receives. */
export interface AppProps {
	/**
	 * The area the service serves, read from the page as it starts (main.tsx): its name heads the
	 * page, and its home view is where the map opens (US-23, criterion 1).
	 */
	readonly area: Area;
	/**
	 * The page's own origin, the scheme, host and port the page came from, on which every tile
	 * address is built (US-23, criterion 7).
	 */
	readonly origin: string;
	/** The layer the address names, if any (US-23, criterion 2). */
	readonly openLayer?: LayerId;
	/** The map's ports, the functions through which the update moves the map (map/map-handle.ts). */
	readonly map: MapView.MapHandle;
	/**
	 * Called once MapLibre, the library that draws the map, has started; main.tsx passes it for the
	 * browser tests alone.
	 */
	readonly onMapReady?: (map: MapLibreMap) => void;
}

/**
 * The map of the area. It fills the window, with the compass and zoom buttons at the top right, and
 * the scale bar and the coordinates at the bottom left. Every click sends a message to `dispatch`,
 * the function that hands it to the update, and the update makes the next model from it.
 */
export function App({ area, origin, openLayer, map: handle, onMapReady }: AppProps): ReactNode {
	// Made again only when an input changes, so the Elmish loop and any effect that depends on its
	// props see one stable value, not a new object on every draw that React would take for a change.
	const props = useMemo<Props>(() => ({ ports: handle.ports, area, ...(openLayer === undefined ? {} : { openLayer }) }), [handle, area, openLayer]);
	const [model, dispatch] = useElmish<Props, Model, Message>({ name: "Tessera", props, init, update });

	return (
		<main className="relative h-full w-full overflow-hidden bg-surface font-sans text-on-surface">
			{/* Hidden from sight but read aloud: a screen reader's list of headings opens on the area. */}
			<h1 className="sr-only">Map of {area.name}</h1>
			<div className="absolute inset-0 z-0">
				<MapView.MapCanvas
					visible={model.visible}
					landed={model.landed}
					origin={origin}
					initialView={model.view}
					handle={handle}
					onViewChange={(view) => dispatch(Msg.viewChanged(view))}
					onPointer={(position) => dispatch(Msg.pointerMoved(position))}
					onClusterOpen={(center, level) => dispatch(Msg.clusterOpened(center, level))}
					onTileFailed={(problem) => dispatch(Msg.tileFailed(problem))}
					onLanded={() => dispatch(Msg.pinsLanded())}
					onCannotDraw={() => dispatch(Msg.cannotDraw())}
					onCanDrawAgain={() => dispatch(Msg.canDrawAgain())}
					{...(onMapReady === undefined ? {} : { onReady: onMapReady })}
				/>
			</div>
			{!model.drawable && (
				<p role="alert" className="card message">
					{MapView.CANNOT_DRAW}
				</p>
			)}
			{/*
			  A failed tile's notice shows only while the map can draw: when it cannot, its own message
			  explains the blank map, and a notice about one tile would mislead. The width leaves 112
			  pixels on the right for the column of controls.
			*/}
			{model.drawable && model.notice !== null && (
				<p role="alert" className="card absolute top-4 left-4 z-30 m-0 max-w-[min(480px,calc(100%-112px))] px-4 py-3 text-body-medium">
					{model.notice}
				</p>
			)}
			{/* The controls and readouts work the map, so they leave with it while it cannot draw. */}
			{model.drawable && (
				<>
					<div className="absolute top-4 right-4 z-30 flex flex-col items-end gap-3">
						<MapView.MapControls
							level={model.view.zoom}
							canZoomIn={MapView.canZoomIn(model)}
							canZoomOut={MapView.canZoomOut(model)}
							onZoomIn={() => dispatch(Msg.zoomIn())}
							onZoomOut={() => dispatch(Msg.zoomOut())}
						/>
					</div>
					<div className="absolute bottom-4 left-4 z-30 flex max-w-[calc(100%-32px)] items-stretch gap-2">
						<MapView.ScaleBar level={model.view.zoom} latitude={model.view.center.lat} units={model.units} onToggleUnits={() => dispatch(Msg.toggleUnits())} />
						<MapView.Coordinates position={model.pointer} />
					</div>
				</>
			)}
		</main>
	);
}
