// map/targets.ts: what MapLibre, the library that draws the map, has drawn in view, read back for the
// page's invisible overlay (UI-10, UI-11). MapLibre draws on a canvas, a bitmap that the keyboard and
// a screen reader, the program that reads a page aloud, cannot reach; so the page lays a picture with
// its name over each pin and a button over each cluster, one mark for several places close together.
// useMapLibre, in canvas-hooks.ts, calls these functions each time the map moves or rests, and
// MapCanvas.tsx calls openCluster when a cluster's button is pressed; they ask MapLibre where each
// one is, and change nothing.

import type { GeoJSONSource, Map as MapLibreMap } from "maplibre-gl";
import { type GroupKey, GROUP_KEYS } from "../design";
import type { Level, LngLat } from "../shared";
import { clusterLabel, pinLabel } from "./labels";
import { levelOf } from "./levels";
import { pointOf } from "./places";
import { LAYER, SOURCE } from "./style";

/** A pin or ***cluster*** in view: where it sits on screen and what a screen reader says for it. */
export interface Target {
	/**
	 * `place:` and the place's id, or `cluster:` and the cluster's id: stable across draws, so React
	 * keeps each target's element, and unique, so a place drawn twice yields one target.
	 */
	readonly key: string;
	readonly x: number;
	readonly y: number;
	readonly label: string;
	/** The cluster's id, or null for a pin. */
	readonly clusterId: number | null;
	/** Longitude and latitude, kept so a press of a cluster's button can centre the map on it. */
	readonly point: readonly [number, number];
	/** The pin's group, so the landing drops a pin of the right colour; null for a cluster. */
	readonly group: GroupKey | null;
}

/**
 * The names a cluster's label speaks, by the cluster's id, fetched once for each cluster (UI-10,
 * criterion 4).
 */
export type ClusterNames = Map<number, readonly string[]>;

/**
 * A feature's properties arrive untyped from MapLibre, so these read them safely: text, or null for
 * none, so a pin's label leaves out a missing type; and a known group, or null.
 */
const textOf = (v: unknown): string | null => (typeof v === "string" && v !== "" ? v : null);
const groupOf = (v: unknown): GroupKey | null => GROUP_KEYS.find((g) => g === v) ?? null;

/** The pins and clusters drawn in view, in reading order: top to bottom, then left to right. */
export function targetsInView(map: MapLibreMap, clusterNames: ReadonlyMap<number, readonly string[]>): Target[] {
	// MapLibre reports an error and finds nothing when asked about a layer it lacks, and the layers
	// are missing until a style has loaded, so the query names only those present.
	const layers = [LAYER.clusters, LAYER.pins].filter((id) => map.getLayer(id) !== undefined);
	if (layers.length === 0) return [];
	const { clientWidth: width, clientHeight: height } = map.getContainer();
	// A place near a tile's edge is drawn in each tile that holds it, so the query may return it
	// twice; the keys seen so far keep one target for each.
	const seen = new Set<string>();
	return map
		.queryRenderedFeatures({ layers })
		.flatMap((feature): Target[] => {
			const point = pointOf(feature);
			const p = feature.properties;
			const clusterId = typeof p["cluster_id"] === "number" ? p["cluster_id"] : null;
			const key = clusterId === null ? `place:${String(p["id"])}` : `cluster:${clusterId}`;
			if (point === null || seen.has(key)) return [];
			seen.add(key);
			const at = map.project(point);
			// The query returns a symbol whose image reaches into view even when its point lies outside;
			// a target there would sit off the page, where the keyboard could reach a place unseen.
			if (at.x < 0 || at.x > width || at.y < 0 || at.y > height) return [];
			const label =
				clusterId === null
					? pinLabel(String(p["name"] ?? ""), textOf(p["one"]))
					: clusterLabel(Number(p["point_count"] ?? 0), clusterNames.get(clusterId) ?? []);
			return [{ key, x: at.x, y: at.y, label, clusterId, point, group: clusterId === null ? groupOf(p["group"]) : null }];
		})
		.toSorted((a, b) => a.y - b.y || a.x - b.x);
}

/**
 * Asks MapLibre the level at which a cluster breaks apart, then reports it, for a click on the canvas
 * and a press of its button alike.
 */
export function openCluster(map: MapLibreMap, clusterId: number, point: readonly [number, number], open: (center: LngLat, level: Level) => void): void {
	const source = map.getSource<GeoJSONSource>(SOURCE.places);
	void source?.getClusterExpansionZoom(clusterId).then((zoom) => open({ lng: point[0], lat: point[1] }, levelOf(zoom)));
}

/**
 * Fetches the first ten names in each cluster in view for its label (UI-10, criterion 4), once for
 * each cluster.
 */
export function nameClusters(map: MapLibreMap, names: ClusterNames, done: () => void): void {
	const source = map.getSource<GeoJSONSource>(SOURCE.places);
	if (source === undefined || map.getLayer(LAYER.clusters) === undefined) return;
	const ids = map
		.queryRenderedFeatures({ layers: [LAYER.clusters] })
		.map((f): unknown => f.properties["cluster_id"])
		.filter((id): id is number => typeof id === "number" && !names.has(id));
	if (ids.length === 0) return;
	// Asks for ten leaves from the first, the most the label names (NAMED_IN_CLUSTER in labels.ts);
	// the two numbers must change together. A cluster may vanish before its names arrive, when the
	// places change and MapLibre renumbers its clusters; the label then keeps its count alone until
	// the next rest, so a failure is ignored.
	void Promise.all(
		[...new Set(ids)].map((id) =>
			source.getClusterLeaves(id, 10, 0).then((leaves) => names.set(id, leaves.map((l) => String(l.properties?.["name"] ?? "")))),
		),
	).then(done, () => undefined);
}
