// @vitest-environment jsdom
// app/mount.test.tsx: checks that a failure nothing else catches leaves words on the page, not a blank
// window (US-23, criterion 9). It mounts a page that throws as it draws, and reads what is left.
import { describe, expect, it, vi } from "vitest";
import { PAGE_FAILED, mount } from "./mount";

function Broken(): never {
	throw new Error("a fault no part of the page caught");
}

describe("US-23 criterion 9: a page that fails says so", () => {
	it("replaces a page that throws with words that say what to do", async () => {
		// React's `act`, the test helper that runs its work at once, throws such an error back at the
		// test; the browser has no `act`, so the page here runs as React runs it in the browser.
		const wasAct: unknown = Reflect.get(globalThis, "IS_REACT_ACT_ENVIRONMENT");
		Reflect.set(globalThis, "IS_REACT_ACT_ENVIRONMENT", false);
		const quiet = vi.spyOn(console, "error").mockImplementation(() => undefined);
		const element = document.createElement("div");
		mount(element, <Broken />);
		await vi.waitFor(() => expect(element.querySelector("[role=alert]")?.textContent).toBe(PAGE_FAILED));
		quiet.mockRestore();
		Reflect.set(globalThis, "IS_REACT_ACT_ENVIRONMENT", wasAct);
	});
});
