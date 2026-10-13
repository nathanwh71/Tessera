// @vitest-environment jsdom
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { MACAU } from "../shared";
import { violations } from "../testing/axe";
import { Coordinates, MapControls, ScaleBar } from ".";

const LAT = MACAU.home.center.lat;

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
