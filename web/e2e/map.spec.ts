// e2e/map.spec.ts: the map in a real browser, against the running service with the 900 places of
// data/macau-pois.geojson loaded and published (US-24). Playwright, the tool that drives the browser,
// runs each test as playwright.config.ts sets out; map-page.ts holds the helpers. These tests prove
// what the unit tests cannot: what MapLibre, the library that draws the map, drew, and what the page
// asked the service for.

import AxeBuilder from "@axe-core/playwright";
import { type Page, expect, test } from "@playwright/test";
import { BUSIEST, drawn, onScreen, openMap, settle, view } from "./map-page";

/**
 * The nine ***layers*** of US-24, written out again rather than imported from src/layers, so the test
 * checks the page against the story and not against itself.
 */
const LAYERS = ["food.cafes", "food.restaurants", "food.markets", "outdoors.parks", "outdoors.playgrounds", "culture.museums", "culture.libraries", "transport.trains", "transport.buses"];
/**
 * A ***tile*** address, `/{layer}/{z}/{x}/{y}.pbf` (US-23, criterion 7), which tells a tile request
 * apart from a request for the page or its files.
 */
const TILE = /^\/(food|outdoors|culture|transport)\.[a-z]+\/\d+\/\d+\/\d+\.pbf$/;

/**
 * The faults axe, a library that checks a page against the web's accessibility rules, finds on the
 * page. Here, in a real browser, every rule runs, including the colour contrast and landmark rules
 * that jsdom cannot judge (src/testing/axe.ts).
 */
async function axe(page: Page): Promise<string[]> {
	const result = await new AxeBuilder({ page }).analyze();
	return result.violations.map((v) => `${v.id}: ${v.nodes.map((n) => n.target.join(" ")).join(", ")}`);
}

/**
 * Every request the page makes, and every error or warning the browser's console logs, while it
 * opens: the requests prove the page asks only its own origin for tiles and loads no list of layers
 * (US-23, criterion 7), and the console shows whether the security policy refused anything.
 */
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
		// The service's security policy, the rule that says which files a page may load, must let every
		// file of the page through; a refusal shows only as a console message.
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
		// Macau's bounds (shared/area.ts): the layer's page fits the whole area, so every café is in view.
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
		// A pin's tip marks its place, and its round head sits 25 pixels above the tip, so the click lands
		// on the pin itself.
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
				body: JSON.stringify({ type: "about:blank", title: "Service Unavailable", status: 503, detail: "The list of layers has not loaded yet." }),
			}),
		);
		// openMap would wait for the pins to land, which never happens while every café tile fails, so
		// the test opens the page by hand.
		await page.addInitScript(() => {
			window.__tesseraTest = true;
		});
		await page.goto("/");
		await expect(page.getByRole("alert")).toHaveText("The list of layers has not loaded yet.");
		expect(await axe(page)).toEqual([]);
	});
});

test.describe("N-18 a browser keeps a tile", () => {
	// N-18 test 3. Playwright sees every request the page makes, its web workers' included, where
	// MapLibre fetches tiles; a request the browser answers from its own cache counts too, so the
	// test is the stricter for it: the map must not ask again at all.
	test("asks once for a tile when the visitor pans away and back within the minute", async ({ page }) => {
		const asked = new Map<string, number>();
		page.on("request", (r) => {
			const path = new URL(r.url()).pathname;
			if (TILE.test(path)) asked.set(path, (asked.get(path) ?? 0) + 1);
		});

		await openMap(page);
		const home = [...asked.keys()];
		expect(home.length).toBeGreaterThan(0);
		// Coloane, far enough south that none of the home view's tiles stays in sight.
		await view(page, 15, { lng: 113.565, lat: 22.125 });
		expect(asked.size).toBeGreaterThan(home.length);
		await view(page, 15, { lng: 113.55, lat: 22.19 });

		expect(home.map((path) => asked.get(path))).toEqual(home.map(() => 1));
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
		// A zoom below the lowest level, over the world's top-left corner, tries to show the map's edge
		// (UI-09, criterion 5).
		await page.evaluate(() => window.__tesseraMap?.jumpTo({ zoom: -1, center: [-179, 84] }));
		await settle(page);
		const [west = 0, south = 0, east = 0, north = 0] = (await page.evaluate(() => window.__tesseraMap?.getBounds().toArray().flat())) ?? [];
		expect(west).toBeGreaterThanOrEqual(-180);
		expect(east).toBeLessThanOrEqual(180);
		// Web Mercator, the flat projection web maps use, ends at 85.06 degrees north and south, where the
		// world becomes a square; beyond it lies blank space.
		expect(south).toBeGreaterThanOrEqual(-85.06);
		expect(north).toBeLessThanOrEqual(85.06);
	});
});

test.describe("UI-10 clusters", () => {
	test("opens a cluster on a click, at least to level 16", async ({ page }) => {
		await openMap(page);
		// At level 13 over the middle of Macau the whole area is in view, and places this crowded
		// stay in clusters below level 16 (UI-17, criterion 5).
		await view(page, 13, { lng: 113.55, lat: 22.16 });
		const cluster = await page.evaluate(() => {
			const f = window.__tesseraMap?.queryRenderedFeatures({ layers: ["clusters"] })[0];
			return f?.geometry.type === "Point" ? { lng: f.geometry.coordinates[0] ?? 0, lat: f.geometry.coordinates[1] ?? 0 } : null;
		});
		expect(cluster).not.toBeNull();
		const at = await onScreen(page, cluster ?? BUSIEST);
		await page.mouse.click(at.x, at.y);
		await settle(page);
		// MapLibre's zoom runs one below the web-map level (src/map/levels.ts).
		const level = await page.evaluate(() => (window.__tesseraMap?.getZoom() ?? 0) + 1);
		expect(level).toBeGreaterThanOrEqual(16);
	});

	test("names a cluster's places for screen readers", async ({ page }) => {
		await openMap(page);
		// The same crowded view as above, sure to hold clusters.
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
		// Counts every dropping pin the page ever shows, however briefly: a MutationObserver hears each
		// change to the page and notes each pin as it appears.
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
		// MapLibre offers no public way to read its fade time, so the test reads the private field.
		const fade = await page.evaluate(() => (window.__tesseraMap as unknown as { _fadeDuration: number } | undefined)?._fadeDuration);
		expect(fade).toBe(0);
	});
});

test.describe("Keyboard and screen reader", () => {
	test("reaches every control with Tab: the map, the clusters in view, the zoom buttons, then the scale bar", async ({ page }) => {
		await openMap(page);
		const names: string[] = [];
		// 80 presses is far more than the controls in view, so the loop ends at the scale bar, or fails
		// rather than running forever if focus never reaches it.
		for (let i = 0; i < 80 && !(names.at(-1) ?? "").startsWith("Scale bar"); i += 1) {
			await page.keyboard.press("Tab");
			// The ARIA snapshot, what a screen reader sees of the focused control, holds its role and its
			// name in its first line: '- button "Zoom in"'.
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
	// UI-17, test 3: names start at level 16, where they crowd most, and the places spread apart at 18
	// and 20, so the three images cover names from crowded to roomy.
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
		// The text joins the bar's three figures, '0', '250' and '500 m' (UI-19, criterion 2).
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
