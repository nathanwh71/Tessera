// map/clusters.test.ts: the clusters of UI-10, each one mark for several places close together,
// checked with values alone under vitest, the test runner. Supercluster, the library MapLibre
// clusters with, shows which pins merge at each level (ADR 0005); the map's update, the function that
// makes the new model, the value that holds everything the map shows, from the old one and a message
// that says what happened, shows where a click on a cluster takes the map.

import { Supercluster } from "@maplibre/geojson-vt";
import { createUpdateArgsFactory, execCmd, getUpdateFn } from "react-elmish/testing";
import { describe, expect, it, vi } from "vitest";
import { type Level, MACAU, level } from "../shared";
import {
	CLUSTER_RADIUS_PX,
	LAST_CLUSTER_LEVEL,
	type Message,
	type Model,
	Msg,
	type Props,
	clusterLabel,
	init,
	mergeDistanceMetres,
	toMapZoom,
	update,
} from ".";

// Macau's latitude, since the merge distance depends on latitude and the stories measure it there.
const LAT = MACAU.home.center.lat;
// Along the equator, on the sphere of Web Mercator, the flat projection every web map uses.
const METRES_PER_DEGREE = (2 * Math.PI * 6378137) / 360;

// The points Supercluster takes, named from its own type so the test cannot drift from the library.
type Points = Parameters<Supercluster["load"]>[0];

/**
 * Two places at Macau's centre, the second the given ground distance due east. Due east keeps both
 * on one latitude, where the merge distance holds, so the test measures that distance alone.
 */
function pair(metres: number): Points {
	const { lng, lat } = MACAU.home.center;
	const east = metres / (METRES_PER_DEGREE * Math.cos((lat * Math.PI) / 180));
	return [lng, lng + east].map((x, i) => ({ type: "Feature", properties: { id: `p${i}` }, geometry: { type: "Point", coordinates: [x, lat] } }));
}

/**
 * Groups places into ***clusters*** as MapLibre does for the places source, the data it draws the
 * places from: the same library, radius and last level. MapLibre measures the radius against
 * ***tiles***, square pieces of the map, 512 pixels wide, so the test does too.
 */
function clustersAt(at: Level, features: Points): number {
	const index = new Supercluster({ radius: CLUSTER_RADIUS_PX, extent: 512, maxZoom: toMapZoom(LAST_CLUSTER_LEVEL) });
	index.load(features);
	// The whole world, cut at latitude 85 north and south, where Web Mercator ends.
	return index.getClusters([-180, -85, 180, 85], toMapZoom(at)).length;
}

describe("UI-10 clusters", () => {
	it("merges pins that sit closer than the distance for the level", () => {
		const distance = mergeDistanceMetres(14, LAT);
		expect(clustersAt(level(14), pair(distance * 0.8))).toBe(1);
		expect(clustersAt(level(14), pair(distance * 1.2))).toBe(2);
	});

	it("halves that distance at each level up", () => {
		expect(mergeDistanceMetres(15, LAT)).toBeCloseTo(mergeDistanceMetres(14, LAT) / 2, 9);
		const distance = mergeDistanceMetres(14, LAT);
		expect(clustersAt(level(15), pair(distance * 0.8))).toBe(2);
		expect(clustersAt(level(15), pair(distance * 0.4))).toBe(1);
	});

	it("keeps every place apart from level 16 (UI-17, criterion 5)", () => {
		expect(clustersAt(level(16), pair(1))).toBe(2);
	});

	it("UI-10 c4: names a cluster's places and says that zooming in shows them", () => {
		expect(clusterLabel(3, ["A", "B", "C"])).toBe("3 places: A, B, C. Zoom in to see them.");
		const many = Array.from({ length: 12 }, (_, i) => `P${i}`);
		expect(clusterLabel(40, many)).toBe("40 places: P0, P1, P2, P3, P4, P5, P6, P7, P8, P9, and 30 more. Zoom in to see them.");
	});
});

describe("UI-10 opening a cluster", () => {
	// Fake ports from vi.fn() record each move the update asks for; react-elmish's helpers build
	// each call's arguments from a fresh model, so no test leaks into the next.
	const props = (): Props => ({ ports: { fitBounds: vi.fn(), jumpTo: vi.fn() }, area: MACAU });
	const updateFn = getUpdateFn(update);
	const args = createUpdateArgsFactory<Props, Model, Message>(() => init(props())[0], props);
	// Any point in Macau serves: the tests check the zoom, and the centre passes through unchanged.
	const center = { lng: 113.54, lat: 22.2 };

	it("zooms at least to level 16", async () => {
		const p = props();
		await execCmd(updateFn(...args(Msg.clusterOpened(center, level(15.2)), {}, p))[1]);
		expect(p.ports.jumpTo).toHaveBeenCalledWith({ center, zoom: 16 });
	});

	it("zooms no further than the cluster needs to open", async () => {
		const p = props();
		await execCmd(updateFn(...args(Msg.clusterOpened(center, level(19)), {}, p))[1]);
		expect(p.ports.jumpTo).toHaveBeenCalledWith({ center, zoom: 19 });
	});
});
