// @vitest-environment jsdom
import { act, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { LAYER_IDS, type LayerId } from "../layers";
import { createMapHandle } from "../map";
import { MACAU } from "../shared";
import { violations } from "../testing/axe";
import { FakeMap } from "../testing/fake-maplibre";
import { App } from ".";

vi.mock("maplibre-gl", async () => (await import("../testing/fake-maplibre")).fakeMapLibre);

function page(openLayer?: LayerId) {
	const handle = createMapHandle();
	const view = render(<App area={MACAU} origin="http://tessera.test" map={handle} {...(openLayer === undefined ? {} : { openLayer })} />);
	return { ...view, handle, map: () => FakeMap.made.at(-1) };
}

/** What sequential focus visits, in order: every control not disabled or hidden. */
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
		// Level 15 is MapLibre's zoom 14 (map/levels.ts).
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
		expect(frame?.args[1]).toMatchObject({ maxZoom: 17 });
	});

	it("shows a failed tile's problem detail as text over the map", async () => {
		const { map, container } = page();
		const body = new Blob([JSON.stringify({ type: "about:blank", title: "Not Found", status: 404, detail: "No layer 'food.cafes' is published." })]);
		act(() => map()?.fire("error", { sourceId: "tiles:food.cafes", error: { status: 404, statusText: "Not Found", url: "x", body } }));
		expect((await screen.findByRole("alert")).textContent).toBe("No layer 'food.cafes' is published.");
		expect(await violations(container, { region: true })).toEqual([]);
	});

	it("leaves other errors, such as a missing image, to MapLibre", async () => {
		const { map } = page();
		act(() => map()?.fire("error", { error: new Error("image 'x' missing") }));
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
