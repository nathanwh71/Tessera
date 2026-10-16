// @vitest-environment jsdom
// map/canvas-hooks.test.tsx: the landing's timers, run in jsdom, a pretend browser inside the test
// runner (UI-15, criterion 6). The browser's animation frames are faked with a queue the test empties
// by hand, so it can stop the landing at any moment between two frames.
import { act, renderHook } from "@testing-library/react";
import type { RefObject } from "react";
import { afterEach, expect, it, vi } from "vitest";
import { type CanvasInputs, useLanding } from "./canvas-hooks";

vi.mock("maplibre-gl", async () => (await import("../testing/fake-maplibre")).fakeMapLibre);

/** The frames the hook has asked for and not yet had, by the number each request returned. */
const frames = new Map<number, FrameRequestCallback>();
const counter = { last: 0 };

/** Runs the frames waiting now; a frame one of them asks for waits for the next call. */
function nextFrame(): void {
	const due = [...frames];
	frames.clear();
	act(() => due.forEach(([, run]) => run(0)));
}

afterEach(() => {
	frames.clear();
	vi.unstubAllGlobals();
});

it("cancels the second frame of the fall when the landing ends between the two", () => {
	vi.stubGlobal("requestAnimationFrame", (run: FrameRequestCallback) => {
		counter.last += 1;
		frames.set(counter.last, run);
		return counter.last;
	});
	vi.stubGlobal("cancelAnimationFrame", (id: number) => frames.delete(id));
	const latest = { current: { landed: false, onLanded: vi.fn() } } as unknown as RefObject<CanvasInputs>;
	const { result } = renderHook(() => useLanding(latest));

	act(() => result.current.advance({ name: "placesFed" }));
	act(() => result.current.advance({ name: "mapIdle", pins: [{ key: "a", x: 10, y: 10, group: "food" }], reduced: false }));
	expect(result.current.landing.stage).toBe("dropping");

	nextFrame();
	act(() => result.current.advance({ name: "visitorMoved" }));

	expect(result.current.landing.stage).toBe("landed");
	expect(frames.size).toBe(0);
	expect(result.current.falling).toBe(false);
});
