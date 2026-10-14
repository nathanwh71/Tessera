// map/readouts.test.ts: what the map reads out in words and numbers (UI-09, UI-19): zoom levels and
// the zoom of MapLibre, the library that draws the map; the scale bar; and the point under the
// pointer. Pure functions, which read only their inputs, tested with values alone under vitest, the
// test runner.

import { describe, expect, it } from "vitest";
import { MACAU, level } from "../shared";
import { type MapZoom, formatPosition, levelOf, levelText, mapZoom, metresPerPixel, roundLengthBelow, scaleBar, toLevel, toMapZoom, zoomLabel } from ".";

const LAT = MACAU.home.center.lat; // 22.19° N

describe("UI-09 levels", () => {
	it("doubles and halves the scale one level at a time", () => {
		const at15 = metresPerPixel(15, LAT);
		expect(metresPerPixel(16, LAT)).toBeCloseTo(at15 / 2, 9);
		expect(metresPerPixel(14, LAT)).toBeCloseTo(at15 * 2, 9);
	});

	it("speaks web-map levels, one above MapLibre's zoom, which draws the world 512 pixels wide", () => {
		expect(toMapZoom(level(15))).toBe(14);
		expect(toLevel(mapZoom(14))).toBe(15);
		expect(toLevel(toMapZoom(level(16.4)))).toBeCloseTo(16.4, 9);
	});

	it("keeps the two scales apart, as F#'s units of measure would: the compiler refuses one for the other", () => {
		// The two are branded types, numbers with a mark the compiler alone sees (ADR 0004).
		// @ts-expect-error a level cannot stand where MapLibre's zoom belongs
		const wrong: MapZoom = level(15);
		// @ts-expect-error nor can a plain number
		toMapZoom(15);
		expect(wrong).toBe(15);
	});

	it("reads MapLibre's zoom as a level within 0 to 22", () => {
		expect(levelOf(14)).toBe(15);
		expect(levelOf(30)).toBe(22);
		expect(levelOf(-3)).toBe(0);
	});

	it("says the level only in the zoom buttons' labels, to one decimal place between levels", () => {
		expect(zoomLabel("in", 15)).toBe("Zoom in, now at level 15 of 22");
		expect(zoomLabel("out", 16.44)).toBe("Zoom out, now at level 16.4 of 22");
		expect(levelText(17.96)).toBe("18");
	});
});

describe("UI-19 the scale bar", () => {
	it("chooses 500 m at level 15 and 50 m at level 18", () => {
		const at15 = scaleBar(15, LAT, "metric");
		const at18 = scaleBar(18, LAT, "metric");
		expect([at15.half, at15.full]).toEqual(["250", "500 m"]);
		expect([at18.half, at18.full]).toEqual(["25", "50 m"]);
		for (const bar of [at15, at18]) expect(bar.widthPx).toBeLessThanOrEqual(140);
		expect(at15.widthPx).toBe(Math.round(500 / metresPerPixel(15, LAT)));
	});

	it("takes the longest length of 1, 2 or 5 of a unit that fits", () => {
		expect([0.9, 1, 1.99, 2, 4.99, 5, 9.99, 730].map(roundLengthBelow)).toEqual([0.5, 1, 1, 2, 2, 5, 5, 500]);
		expect(scaleBar(12, LAT, "metric").full).toBe("2 km");
		expect(scaleBar(12, LAT, "metric").half).toBe("1");
	});

	it("switches between metric and imperial on a click, and names the units it will switch to", () => {
		const metric = scaleBar(15, LAT, "metric");
		const imperial = scaleBar(15, LAT, "imperial");
		expect(metric.label).toBe("Scale bar: 500 metres. Switch to imperial units");
		expect([imperial.half, imperial.full]).toEqual(["1,000", "2,000 ft"]);
		expect(imperial.label).toBe("Scale bar: 2,000 feet. Switch to metric units");
		expect(scaleBar(11, LAT, "imperial").full).toBe("5 mi");
		expect(scaleBar(13, LAT, "imperial").label).toBe("Scale bar: 1 mile. Switch to metric units");
	});
});

describe("UI-19 coordinates", () => {
	it("writes the point under the pointer as 22.19° N, 113.55° E, latitude first, to two decimal places", () => {
		expect(formatPosition({ lng: 113.5512, lat: 22.1894 })).toBe("22.19° N, 113.55° E");
	});

	it("names the southern and western hemispheres, and takes a value that rounds to zero as north or east", () => {
		expect(formatPosition({ lng: -0.1276, lat: -33.8688 })).toBe("33.87° S, 0.13° W");
		expect(formatPosition({ lng: -0.001, lat: -0.004 })).toBe("0.00° N, 0.00° E");
	});
});
