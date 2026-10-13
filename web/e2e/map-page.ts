// e2e/map-page.ts: opens the map and reads it, for the browser tests.

import { type Page, expect } from "@playwright/test";

/**
 * The busiest street of the sample places: the point with the most places within 200 metres, 21 of
 * them, measured over data/macau-pois.geojson (UI-17, test 3).
 */
export const BUSIEST = { lng: 113.54566, lat: 22.21188 } as const;

/** Opens the page with the map reachable from the test (main.tsx), and waits until the pins have landed and the map rests. */
export async function openMap(page: Page, path = "/"): Promise<void> {
	await page.addInitScript(() => {
		window.__tesseraTest = true;
	});
	await page.goto(path);
	await page.waitForFunction(() => window.__tesseraMap !== undefined);
	await landed(page);
	await settle(page);
}

/** Waits until the dropping pins have landed and the map's own pins show (UI-15, criterion 6). */
export async function landed(page: Page): Promise<void> {
	await expect
		.poll(() => page.evaluate(() => window.__tesseraMap?.getPaintProperty("pins", "icon-opacity")), { timeout: 15_000 })
		.toBe(1);
}

/** Waits until the map has drawn every tile and finished every movement, then one more frame for the overlay. */
export async function settle(page: Page): Promise<void> {
	await page.evaluate(
		() =>
			new Promise<void>((resolve) => {
				const map = window.__tesseraMap;
				if (map === undefined) return resolve();
				const done = (): void => void requestAnimationFrame(() => resolve());
				if (map.loaded() && !map.isMoving()) done();
				else map.once("idle", done);
			}),
	);
	// MapLibre fades symbols in over 300 ms after it rests (UI-15).
	await page.waitForTimeout(400);
}

/** Moves the map to a web-map level over a point, at once, and waits for it to settle. */
export async function view(page: Page, level: number, center: { lng: number; lat: number }): Promise<void> {
	// MapLibre's zoom runs one below the web-map level (src/map/levels.ts).
	await page.evaluate(({ zoom, lng, lat }) => window.__tesseraMap?.jumpTo({ zoom, center: [lng, lat] }), { zoom: level - 1, ...center });
	await settle(page);
}

/** The features MapLibre has drawn on the given layers of its style. */
export async function drawn(page: Page, layers: readonly string[]): Promise<{ id: string; layer: string; group: string }[]> {
	return page.evaluate((ids) => {
		const map = window.__tesseraMap;
		if (map === undefined) return [];
		const present = ids.filter((id) => map.getLayer(id) !== undefined);
		return map.queryRenderedFeatures({ layers: present }).map((f) => ({
			id: String(f.properties["id"] ?? f.properties["cluster_id"] ?? ""),
			layer: f.layer.id,
			group: String(f.properties["group"] ?? ""),
		}));
	}, layers);
}

/** Where a place sits on screen, in page pixels. */
export async function onScreen(page: Page, at: { lng: number; lat: number }): Promise<{ x: number; y: number }> {
	return page.evaluate(({ lng, lat }) => {
		const map = window.__tesseraMap;
		const p = map?.project([lng, lat]) ?? { x: 0, y: 0 };
		const box = map?.getContainer().getBoundingClientRect() ?? { left: 0, top: 0 };
		return { x: box.left + p.x, y: box.top + p.y };
	}, at);
}
