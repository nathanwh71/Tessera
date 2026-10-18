// @vitest-environment jsdom
// map/controls.test.tsx: the zoom buttons, the compass, the scale bar and the coordinates, drawn in
// jsdom, a pretend browser that runs inside the test runner with no screen (UI-09, UI-19). The tests
// check what each control says to a screen reader, the program that reads a page aloud, what a click
// sends, and that an accessibility scan finds no fault.
/// <reference types="node" />
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { MACAU } from "../shared";
import { violations } from "../testing/axe";
import { Coordinates, MapControls, ScaleBar } from ".";

// Macau's latitude, where the stories give the scale bar's lengths (UI-19, criterion 1).
const LAT = MACAU.home.center.lat;

/**
 * The colour stops of the scale bar's background, read from the `.scale-sections` rule in
 * styles.css, such as `{ fill: "var(--color-on-surface)", from: 0, to: 25 }`. jsdom draws nothing
 * and reads no stylesheet, so the test reads the rule as text: the gradient is what draws the
 * sections.
 */
function sections(): { fill: string; from: number; to: number }[] {
	const styles = readFileSync(join(dirname(fileURLToPath(import.meta.url)), "..", "styles.css"), "utf8");
	const gradient = /\.scale-sections\s*\{[^}]*?linear-gradient\(\s*90deg,([^;]*)\);/.exec(styles)?.[1] ?? "";
	return [...gradient.matchAll(/(\S+)\s+(\d+)%?\s+(\d+)%/g)].map(([, fill = "", from = "", to = ""]) => ({ fill, from: Number(from), to: Number(to) }));
}

/**
 * Draws the controls at a level, with each button greyed as the app would grey it at 0 and 22, and
 * returns the fake handlers so a test can check what a press sent.
 */
function controls(level: number) {
	const handlers = { onZoomIn: vi.fn(), onZoomOut: vi.fn() };
	const view = render(<MapControls level={level} canZoomIn={level < 22} canZoomOut={level > 0} {...handlers} />);
	return { ...view, ...handlers };
}

describe("UI-09 the zoom buttons", () => {
	it("states the current level in each label, and nowhere on screen", async () => {
		const { container } = controls(15);
		expect(screen.getByRole("button", { name: "Zoom in, now at level 15 of 22" })).toBeTruthy();
		expect(screen.getByRole("button", { name: "Zoom out, now at level 15 of 22" })).toBeTruthy();
		expect(container.textContent).not.toMatch(/15/);
		expect(await violations(container)).toEqual([]);
	});

	it("turns the button grey at the end of the range, and it does nothing there", async () => {
		const user = userEvent.setup();
		const { onZoomIn, onZoomOut } = controls(22);
		const zoomIn = screen.getByRole("button", { name: /^Zoom in/ });
		expect(zoomIn.getAttribute("aria-disabled")).toBe("true");
		await user.click(zoomIn);
		expect(onZoomIn).not.toHaveBeenCalled();
		await user.click(screen.getByRole("button", { name: /^Zoom out/ }));
		expect(onZoomOut).toHaveBeenCalled();
	});
});

describe("UI-19 the compass", () => {
	it("names north for screen readers, as a sign rather than a button", () => {
		controls(15);
		expect(screen.getByRole("img", { name: "Compass: north is up" })).toBeTruthy();
		expect(screen.queryByRole("button", { name: /compass/i })).toBeNull();
	});
});

describe("UI-19 the scale bar and coordinates", () => {
	it("shows '0', the halfway figure and the full length, and names its length and the other units", async () => {
		const { container } = render(<ScaleBar level={15} latitude={LAT} units="metric" onToggleUnits={vi.fn()} />);
		const bar = screen.getByRole("button", { name: "Scale bar: 500 metres. Switch to imperial units" });
		expect(bar.textContent).toBe("0250500 m");
		expect(await violations(container)).toEqual([]);
	});

	// UI-19 c2 (FR-50): four sections, each a quarter of the bar, filled and empty in turn from the
	// left. The bar's border outlines the empty ones, and the bar is exactly as wide as the row of
	// figures, so '0', the halfway figure and the full length mark its start, middle and end.
	it("draws four sections, filled and empty in turn, under '0', the halfway figure and the full length", () => {
		const filled = "var(--color-on-surface)";
		expect(sections()).toEqual([
			{ fill: filled, from: 0, to: 25 },
			{ fill: "transparent", from: 25, to: 50 },
			{ fill: filled, from: 50, to: 75 },
			{ fill: "transparent", from: 75, to: 100 },
		]);

		const { container } = render(<ScaleBar level={15} latitude={LAT} units="metric" onToggleUnits={vi.fn()} />);
		const bar = container.querySelector<HTMLElement>(".scale-sections");
		const figures = bar?.nextElementSibling as HTMLElement | null | undefined;
		expect(bar?.className).toMatch(/\bborder-on-surface\b/);
		expect(bar?.style.width).toMatch(/^[1-9]\d*px$/);
		expect(figures?.style.width).toBe(bar?.style.width);
		expect([...(figures?.children ?? [])].map((f) => [f.className.match(/\b(left-0|left-1\/2|right-0)\b/)?.[0], f.textContent])).toEqual([
			["left-0", "0"],
			["left-1/2", "250"],
			["right-0", "500 m"],
		]);
	});

	it("switches between metric and imperial on a click", async () => {
		const user = userEvent.setup();
		const onToggleUnits = vi.fn();
		render(<ScaleBar level={18} latitude={LAT} units="imperial" onToggleUnits={onToggleUnits} />);
		await user.click(screen.getByRole("button", { name: /^Scale bar: 200 feet. Switch to metric units$/ }));
		expect(onToggleUnits).toHaveBeenCalled();
	});

	it("shows the point under the pointer, silent to screen readers, and goes when the pointer leaves", () => {
		const { rerender } = render(<Coordinates position={{ lng: 113.5512, lat: 22.1894 }} />);
		const readout = screen.getByTestId("coordinates");
		expect(readout.textContent).toBe("22.19° N, 113.55° E");
		expect(readout.getAttribute("aria-hidden")).toBe("true");
		rerender(<Coordinates position={null} />);
		expect(screen.queryByTestId("coordinates")).toBeNull();
	});
});
