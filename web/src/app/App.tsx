// app/App.tsx: the page. It holds one model, the one object that holds everything the page shows, and
// one update function, which turns the old model and a message into the new model (US-23).

import type { Map as MapLibreMap } from "maplibre-gl";
import { type ReactNode, useMemo } from "react";
import { useElmish } from "react-elmish";
import type { LayerId } from "../layers";
import * as MapView from "../map";
import type { Area } from "../shared";
import { type Message, type Model, Msg, type Props, init, update } from "./app";

/** Props for the page: the inputs a component receives. */
export interface AppProps {
	readonly area: Area;
	/** The page's own origin, on which every tile address is built (US-23, criterion 7). */
	readonly origin: string;
	/** The layer the address names, if any (US-23, criterion 2). */
	readonly openLayer?: LayerId;
	readonly map: MapView.MapHandle;
	/** Called once MapLibre, the library that draws the map, has started; main.tsx passes it to browser tests alone. */
	readonly onMapReady?: (map: MapLibreMap) => void;
}

/**
 * The map of the area. It fills the window, with the compass and zoom buttons at the top right, and
 * the scale bar and the coordinates at the bottom left. Every click sends a message, a value that says
 * what happened, to `dispatch`, and the update function makes the next model from it.
 */
export function App({ area, origin, openLayer, map: handle, onMapReady }: AppProps): ReactNode {
	const props = useMemo<Props>(() => ({ ports: handle.ports, area, ...(openLayer === undefined ? {} : { openLayer }) }), [handle, area, openLayer]);
	const [model, dispatch] = useElmish<Props, Model, Message>({ name: "Tessera", props, init, update });

	return (
		<main className="relative h-full w-full overflow-hidden bg-surface font-sans text-on-surface">
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
					{...(onMapReady === undefined ? {} : { onReady: onMapReady })}
				/>
			</div>
			{model.notice !== null && (
				<p role="alert" className="absolute top-4 left-4 z-30 m-0 max-w-[min(480px,calc(100%-112px))] rounded-medium bg-surface-low px-4 py-3 text-body-medium text-on-surface shadow-level-1">
					{model.notice}
				</p>
			)}
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
		</main>
	);
}
