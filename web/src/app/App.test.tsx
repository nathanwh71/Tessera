// @vitest-environment jsdom
// app/App.test.tsx: the whole page drawn in jsdom, a pretend browser that runs inside the test runner
// with no screen, so the tests check what the page says to a screen reader, the program that reads a
// page aloud, and what it asks of the map (US-23). MapLibre, the library that draws the map, needs
// WebGL, the browser's 3D drawing, which jsdom lacks; so the tests swap in the fake map of
// testing/fake-maplibre.ts.
import { act, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { LAYER_IDS, type LayerId } from "../layers";
import { CANNOT_DRAW, createMapHandle } from "../map";
import { MACAU } from "../shared";
import { violations } from "../testing/axe";
import { FakeMap } from "../testing/fake-maplibre";
import { App } from ".";

vi.mock("maplibre-gl", async () => (await import("../testing/fake-maplibre")).fakeMapLibre);

/**
 * Draws the whole page over Macau, on a made-up origin so the tests can spot it in each ***tile***
 * address, and hands back the fake map the page made, so a test can read what the page asked of it.
 */
function page(openLayer?: LayerId) {
	const handle = createMapHandle();
	const view = render(<App area={MACAU} origin="http://tessera.test" map={handle} {...(openLayer === undefined ? {} : { openLayer })} />);
	return { ...view, handle, map: () => FakeMap.made.at(-1) };
}

/**
 * What the Tab key visits, in order: every control not disabled or hidden. jsdom cannot press Tab,
 * so this reads the same rule from the page: in document order, every element focus can reach.
 */
function tabOrder(root: HTMLElement): string[] {
	return [...root.querySelectorAll<HTMLElement>("button, a[href], input, [tabindex]")]
		.filter((e) => e.tabIndex >= 0 && !e.hasAttribute("disabled"))
		.map((e) => e.getAttribute("aria-label") ?? e.textContent ?? "");
}

describe("US-23 open the map of the area", () => {
	it("names the area for screen readers", () => {
		page();
		expect(screen.getByRole("heading", { level: 1 }).textContent).toBe("Map of Macau");
	});

	it("fills the window and opens on the area's home view, with the nine layers' tiles on the page's own origin", () => {
		const { map } = page();
		expect(map()?.options["center"]).toEqual([113.55, 22.19]);
		// Level 15 is MapLibre's zoom 14: its zoom runs one below the web-map level (map/levels.ts).
		expect(map()?.options["zoom"]).toBe(14);
		const style = map()?.options["style"] as { sources: Record<string, { tiles?: string[] }> };
		const tiles = Object.values(style.sources).flatMap((s) => s.tiles ?? []);
		expect(tiles).toEqual(LAYER_IDS.map((id) => `http://tessera.test/${id}/{z}/{x}/{y}.pbf`));
		expect(screen.getByRole("button", { name: "Zoom in, now at level 15 of 22" })).toBeTruthy();
	});

	it("opens /food.cafes.html with the cafés alone and in view", async () => {
		const { map } = page("food.cafes");
		const style = map()?.options["style"] as { sources: Record<string, { tiles?: string[] }> };
		expect(Object.values(style.sources).flatMap((s) => s.tiles ?? [])).toEqual(["http://tessera.test/food.cafes/{z}/{x}/{y}.pbf"]);
		await waitFor(() => expect(map()?.calls.some((c) => c.name === "fitBounds")).toBe(true));
		const frame = map()?.calls.find((c) => c.name === "fitBounds");
		expect(frame?.args[0]).toEqual([...MACAU.bounds]);
		// The map frames a layer no closer than level 18 (map/map.ts), MapLibre's zoom 17, so a few
		// cafés never fill the screen.
		expect(frame?.args[1]).toMatchObject({ maxZoom: 17 });
	});

	it("shows a failed tile's problem detail as text over the map", async () => {
		const { map, container } = page();
		const body = new Blob([JSON.stringify({ type: "about:blank", title: "Not Found", status: 404, detail: "No layer 'food.cafes' is published." })]);
		act(() => map()?.fire("error", { sourceId: "tiles:food.cafes", error: { status: 404, statusText: "Not Found", url: "x", body } }));
		expect((await screen.findByRole("alert")).textContent).toBe("No layer 'food.cafes' is published.");
		// The whole page has its landmarks, so the landmark rule runs too (testing/axe.ts).
		expect(await violations(container, { region: true })).toEqual([]);
	});

	it("leaves other errors, such as a missing image, to MapLibre", async () => {
		const { map } = page();
		act(() => map()?.fire("error", { error: new Error("image 'x' missing") }));
		// The page reads a failed tile's answer in a step that finishes later, so a wrong alert could
		// appear only after a moment; the test waits that moment before it looks.
		await new Promise((resolve) => setTimeout(resolve, 10));
		expect(screen.queryByRole("alert")).toBeNull();
	});
});

describe("Keyboard and screen reader", () => {
	it("reaches every control with Tab, in reading order", () => {
		const { container } = page();
		expect(tabOrder(container)).toEqual([
			"Zoom in, now at level 15 of 22",
			"Zoom out, now at level 15 of 22",
			"Scale bar: 500 metres. Switch to imperial units",
		]);
	});

	it("passes an accessibility scan of the whole page", async () => {
		const { container } = page();
		expect(await violations(container, { region: true })).toEqual([]);
	});
});

describe("US-23 criterion 9: a browser that cannot draw the map", () => {
	it("says so in words, in place of the controls, when MapLibre cannot start", () => {
		FakeMap.failNext = true;
		page();
		expect(screen.getByRole("alert").textContent).toBe(CANNOT_DRAW);
		expect(screen.queryByRole("button", { name: /Zoom in/ })).toBeNull();
	});

	it("says so when the browser takes the map's drawing away, and draws again once it gives it back", () => {
		const { map } = page();
		act(() => map()?.fire("webglcontextlost"));
		expect(screen.getByRole("alert").textContent).toBe(CANNOT_DRAW);
		act(() => map()?.fire("webglcontextrestored"));
		expect(screen.queryByRole("alert")).toBeNull();
		expect(screen.getByRole("button", { name: /Zoom in/ })).toBeTruthy();
	});
});
