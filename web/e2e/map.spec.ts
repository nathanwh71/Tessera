// e2e/map.spec.ts: the map in a real browser, against the running service with the 900 places of
// data/macau-pois.geojson loaded and published (US-24).

import AxeBuilder from "@axe-core/playwright";
import { type Page, expect, test } from "@playwright/test";
import { BUSIEST, drawn, onScreen, openMap, settle, view } from "./map-page";

const LAYERS = ["food.cafes", "food.restaurants", "food.markets", "outdoors.parks", "outdoors.playgrounds", "culture.museums", "culture.libraries", "transport.trains", "transport.buses"];
const TILE = /^\/(food|outdoors|culture|transport)\.[a-z]+\/\d+\/\d+\/\d+\.pbf$/;

async function axe(page: Page): Promise<string[]> {
	const result = await new AxeBuilder({ page }).analyze();
	return result.violations.map((v) => `${v.id}: ${v.nodes.map((n) => n.target.join(" ")).join(", ")}`);
}

/** Every request the page makes, and every message the browser's console shows, while it opens. */
function watch(page: Page): { paths: string[]; origins: Set<string>; console: string[] } {
	const seen = { paths: [] as string[], origins: new Set<string>(), console: [] as string[] };
	page.on("request", (r) => {
		const url = new URL(r.url());
		if (url.protocol.startsWith("http")) {
			seen.origins.add(url.origin);
			seen.paths.push(url.pathname);
		}
	});
	page.on("console", (m) => {
		if (m.type() === "error" || m.type() === "warning") seen.console.push(m.text());
	});
	return seen;
}

test.describe("US-23 open the map of the area", () => {
	test("opens / on the area's home view with the nine layers, asking only for the page, its files and tiles", async ({ page, baseURL }) => {
		const seen = watch(page);
		await openMap(page);
		await expect(page.getByRole("button", { name: "Zoom in, now at level 15 of 22" })).toBeVisible();
		const centre = await page.evaluate(() => window.__tesseraMap?.getCenter());
		expect(centre?.lng).toBeCloseTo(113.55, 5);
		expect(centre?.lat).toBeCloseTo(22.19, 5);
		const features = await drawn(page, ["pins", "clusters"]);
		expect(features.length).toBeGreaterThan(0);
		expect([...seen.origins]).toEqual([new URL(baseURL ?? "").origin]);
		const others = seen.paths.filter((p) => p !== "/" && !p.startsWith("/assets/") && !TILE.test(p));
		expect(others).toEqual([]);
		const tiles = new Set(seen.paths.filter((p) => TILE.test(p)).map((p) => p.split("/")[1]));
		expect([...tiles].toSorted()).toEqual(LAYERS.toSorted());
		expect(seen.console.filter((m) => /Content Security Policy|Refused/.test(m))).toEqual([]);
	});

	test("fills the window with the map, on a plain background", async ({ page }) => {
		await openMap(page);
		const box = await page.locator(".maplibregl-canvas").boundingBox();
		expect(box).toMatchObject({ x: 0, y: 0, width: 1280, height: 800 });
		const layers = await page.evaluate(() => window.__tesseraMap?.getStyle().layers.map((l) => l.type));
		expect(new Set(layers)).toEqual(new Set(["background", "circle", "symbol"]));
	});

	test("opens /food.cafes.html with the cafés alone, all in view", async ({ page }) => {
		const seen = watch(page);
		await openMap(page, "/food.cafes.html");
		const bounds = await page.evaluate(() => window.__tesseraMap?.getBounds().toArray().flat());
		const [w = 0, s = 0, e = 0, n = 0] = bounds ?? [];
		expect([w <= 113.52, s <= 22.1, e >= 113.61, n >= 22.22]).toEqual([true, true, true, true]);
		await expect.poll(async () => (await drawn(page, ["pins", "clusters"])).length).toBeGreaterThan(0);
		await view(page, 17, BUSIEST);
		const pins = await drawn(page, ["pins"]);
		expect(pins.length).toBeGreaterThan(0);
		expect(new Set(pins.map((p) => p.group))).toEqual(new Set(["food"]));
		expect(new Set(seen.paths.filter((p) => TILE.test(p)).map((p) => p.split("/")[1]))).toEqual(new Set(["food.cafes"]));
	});

	test("treats a pin as a sign: a click on it opens nothing and moves nothing", async ({ page }) => {
		await openMap(page);
		await view(page, 18, BUSIEST);
		const before = await page.evaluate(() => [window.__tesseraMap?.getZoom(), window.__tesseraMap?.getCenter().toArray()]);
		const pin = await page.evaluate(() => {
			const f = window.__tesseraMap?.queryRenderedFeatures({ layers: ["pins"] })[0];
			return f?.geometry.type === "Point" ? { lng: f.geometry.coordinates[0] ?? 0, lat: f.geometry.coordinates[1] ?? 0 } : null;
		});
		expect(pin).not.toBeNull();
		const at = await onScreen(page, pin ?? BUSIEST);
		await page.mouse.click(at.x, at.y - 25);
		await settle(page);
		expect(await page.evaluate(() => [window.__tesseraMap?.getZoom(), window.__tesseraMap?.getCenter().toArray()])).toEqual(before);
		await expect(page.getByRole("dialog")).toHaveCount(0);
		await expect(page.getByRole("complementary")).toHaveCount(0);
	});

	test("shows a tile's problem document over the map", async ({ page }) => {
		await page.route(/\/food\.cafes\/\d+\/\d+\/\d+\.pbf$/, (route) =>
			route.fulfill({
				status: 503,
				contentType: "application/problem+json",
				body: JSON.stringify({ type: "about:blank", title: "Service Unavailable", status: 503, detail: "The list of layers is not ready yet." }),
			}),
		);
		await page.addInitScript(() => {
			window.__tesseraTest = true;
		});
		await page.goto("/");
		await expect(page.getByRole("alert")).toHaveText("The list of layers is not ready yet.");
		expect(await axe(page)).toEqual([]);
	});
});

test.describe("UI-09 levels", () => {
	test("moves one level per button, and says so in the labels", async ({ page }) => {
		await openMap(page);
		await page.getByRole("button", { name: "Zoom in, now at level 15 of 22" }).click();
		await settle(page);
		await expect(page.getByRole("button", { name: "Zoom in, now at level 16 of 22" })).toBeVisible();
		await page.getByRole("button", { name: "Zoom out, now at level 16 of 22" }).click();
		await settle(page);
		await expect(page.getByRole("button", { name: "Zoom out, now at level 15 of 22" })).toBeVisible();
	});

	test("keeps the edge of the map out of view", async ({ page }) => {
		await openMap(page);
		await page.evaluate(() => window.__tesseraMap?.jumpTo({ zoom: -1, center: [-179, 84] }));
		await settle(page);
		const [west = 0, south = 0, east = 0, north = 0] = (await page.evaluate(() => window.__tesseraMap?.getBounds().toArray().flat())) ?? [];
		expect(west).toBeGreaterThanOrEqual(-180);
		expect(east).toBeLessThanOrEqual(180);
		expect(south).toBeGreaterThanOrEqual(-85.06);
		expect(north).toBeLessThanOrEqual(85.06);
	});
});

test.describe("UI-10 clusters", () => {
	test("opens a cluster on a click, at least to level 16", async ({ page }) => {
		await openMap(page);
		await view(page, 13, { lng: 113.55, lat: 22.16 });
		const cluster = await page.evaluate(() => {
			const f = window.__tesseraMap?.queryRenderedFeatures({ layers: ["clusters"] })[0];
			return f?.geometry.type === "Point" ? { lng: f.geometry.coordinates[0] ?? 0, lat: f.geometry.coordinates[1] ?? 0 } : null;
		});
		expect(cluster).not.toBeNull();
		const at = await onScreen(page, cluster ?? BUSIEST);
		await page.mouse.click(at.x, at.y);
		await settle(page);
		const level = await page.evaluate(() => (window.__tesseraMap?.getZoom() ?? 0) + 1);
		expect(level).toBeGreaterThanOrEqual(16);
	});

	test("names a cluster's places for screen readers", async ({ page }) => {
		await openMap(page);
		await view(page, 13, { lng: 113.55, lat: 22.16 });
		await expect(page.getByRole("group", { name: "Places on the map" }).getByRole("button", { name: /^\d+ places: .+\. Zoom in to see them\.$/ }).first()).toBeAttached();
	});
});

test.describe("UI-11 names", () => {
	test("shows names from level 16, and none below", async ({ page }) => {
		await openMap(page);
		await view(page, 15, BUSIEST);
		expect(await drawn(page, ["names"])).toEqual([]);
		await view(page, 16, BUSIEST);
		expect((await drawn(page, ["names"])).length).toBeGreaterThan(0);
	});
});

test.describe("UI-15 calm", () => {
	test("drops the pins in as the page opens, then hands over to the map's own", async ({ page }) => {
		// Counts every dropping pin the page ever shows, however briefly.
		await page.addInitScript(() => {
			const seen = new Set<Element>();
			(window as unknown as { __drops: Set<Element> }).__drops = seen;
			new MutationObserver(() => document.querySelectorAll(".pin-drop").forEach((e) => seen.add(e))).observe(document, { childList: true, subtree: true });
		});
		await openMap(page);
		expect(await page.evaluate(() => (window as unknown as { __drops: Set<Element> }).__drops.size)).toBeGreaterThan(0);
		await expect(page.locator(".pin-drop")).toHaveCount(0);
	});

	test("draws the zoom icons from Material Symbols, one glyph each", async ({ page }) => {
		await openMap(page);
		const widths = await page.locator("button .icon").evaluateAll((icons) => icons.map((i) => i.getBoundingClientRect().width));
		expect(widths.map(Math.round)).toEqual([24, 24]);
	});

	test("stops all movement when the visitor asks for reduced motion", async ({ page }) => {
		await page.emulateMedia({ reducedMotion: "reduce" });
		await openMap(page);
		await expect(page.locator(".pin-drop")).toHaveCount(0);
		const fade = await page.evaluate(() => (window.__tesseraMap as unknown as { _fadeDuration: number } | undefined)?._fadeDuration);
		expect(fade).toBe(0);
	});
});

test.describe("Keyboard and screen reader", () => {
	test("reaches every control with Tab: the map, the clusters in view, the zoom buttons, then the scale bar", async ({ page }) => {
		await openMap(page);
		const names: string[] = [];
		for (let i = 0; i < 80 && !(names.at(-1) ?? "").startsWith("Scale bar"); i += 1) {
			await page.keyboard.press("Tab");
			// The ARIA snapshot's first line holds the role and the accessible name: '- button "Zoom in"'.
			const snapshot = await page.locator(":focus").ariaSnapshot();
			names.push(/"(.*)"/.exec(snapshot.split("\n")[0] ?? "")?.[1] ?? snapshot);
		}
		expect(names[0]).toBe("Map");
		expect(names.slice(-3)).toEqual(["Zoom in, now at level 15 of 22", "Zoom out, now at level 15 of 22", "Scale bar: 500 metres. Switch to imperial units"]);
		expect(names.slice(1, -3).every((n) => /^\d+ places: .+\. Zoom in to see them\.$/.test(n))).toBe(true);
	});

	test("passes an accessibility scan of the opening page", async ({ page }) => {
		await openMap(page);
		expect(await axe(page)).toEqual([]);
	});
});

test.describe("UI-17 names that never overlap", () => {
	for (const level of [16, 18, 20]) {
		test(`matches the approved image at level ${level} over the busiest street, collision boxes shown`, async ({ page }) => {
			await openMap(page);
			await page.evaluate(() => {
				if (window.__tesseraMap !== undefined) window.__tesseraMap.showCollisionBoxes = true;
			});
			await view(page, level, BUSIEST);
			await expect(page).toHaveScreenshot(`UI-17-level-${level}.png`);
		});
	}
});

test.describe("UI-19 distance, direction and position", () => {
	test("reads 500 m at level 15 and 50 m at level 18, and switches units on a click", async ({ page }) => {
		await openMap(page);
		const bar = page.getByRole("button", { name: /^Scale bar/ });
		await expect(bar).toHaveAccessibleName("Scale bar: 500 metres. Switch to imperial units");
		await expect(bar).toHaveText("0250500 m");
		await view(page, 18, { lng: 113.55, lat: 22.19 });
		await expect(bar).toHaveText("02550 m");
		await bar.click();
		await expect(bar).toHaveAccessibleName("Scale bar: 200 feet. Switch to metric units");
		await expect(page.getByRole("img", { name: "Compass: north is up" })).toBeVisible();
	});

	test("shows the point under the pointer while it is over the map, and not over a control", async ({ page }) => {
		await openMap(page);
		const centre = await onScreen(page, { lng: 113.55, lat: 22.19 });
		await page.mouse.move(centre.x, centre.y);
		await expect(page.getByTestId("coordinates")).toHaveText("22.19° N, 113.55° E");
		await page.getByRole("button", { name: /^Zoom in/ }).hover();
		await expect(page.getByTestId("coordinates")).toBeHidden();
	});
});
