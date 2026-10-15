// e2e/drag.measure.ts: measures a drag over the busiest street, with every layer on, at level 17,
// where the most pins and their invisible names sit in view. It is a measurement, not a test:
// `TESSERA_MEASURE=1 npm run e2e` runs it (playwright.config.ts), and it prints what it saw rather
// than passing or failing on a number, since the machine it runs on sets the numbers.
//
// It records two things while the pointer drags the map for about two seconds:
// - each frame's length, from one redraw of the screen to the next; a frame longer than 16.7 ms
//   misses the screen's 60 redraws a second, and the drag stutters;
// - for each long frame, one over 50 ms, the time each script file spent in it, from the browser's
//   long-animation-frame records. The page's code is split into files (vite.config.ts), so the time
//   in react-*.js and index-*.js is React's and the page's own work, and the time in maplibre-*.js is
//   MapLibre's. That split says whether the React Compiler could help.

import { expect, test } from "@playwright/test";
import { BUSIEST, openMap, view } from "./map-page";

/**
 * What the test reads back from the browser once the drag ends. It drops the `stop` flag of
 * `window.__seen`, since that flag only tells the recorder in the page when to quit.
 */
interface Seen {
	readonly frames: number[];
	readonly scripts: Record<string, number>;
	readonly longFrames: number;
	readonly longFrameMs: number;
}

/**
 * The recorder lives on `window`, the browser's own object for the page, because each
 * `page.evaluate` runs a fresh function in the browser: one call starts the recorder, a later one
 * reads it, and the window is the one place both can reach.
 */
declare global {
	interface Window {
		__seen?: { frames: number[]; scripts: Record<string, number>; longFrames: number; longFrameMs: number; stop: boolean };
	}
}

/** The middle frame length, which one stray slow frame cannot pull up as an average would. */
const median = (values: readonly number[]): number => values.toSorted((a, b) => a - b)[Math.floor(values.length / 2)] ?? 0;
/**
 * The frame length that the given share of frames stay within, a percentile: 0.95 gives the 95th,
 * which shows how bad the worst stutters are while ignoring the rare one-off.
 */
const share = (values: readonly number[], q: number): number => values.toSorted((a, b) => a - b)[Math.floor(values.length * q)] ?? 0;

test("a drag over the busiest street, with every layer on", async ({ page }) => {
	await openMap(page);
	await view(page, 17, BUSIEST);

	await page.evaluate(() => {
		const seen = { frames: [] as number[], scripts: {} as Record<string, number>, longFrames: 0, longFrameMs: 0, stop: false };
		window.__seen = seen;
		let last = performance.now();
		const tick = (now: number): void => {
			seen.frames.push(now - last);
			last = now;
			if (!seen.stop) requestAnimationFrame(tick);
		};
		requestAnimationFrame(tick);
		// `buffered: false` skips long frames from before this point, such as the page opening, so
		// only the drag counts.
		new PerformanceObserver((list) => {
			for (const entry of list.getEntries() as (PerformanceEntry & { scripts?: { sourceURL: string; duration: number }[] })[]) {
				seen.longFrames += 1;
				seen.longFrameMs += entry.duration;
				for (const script of entry.scripts ?? []) {
					// Vite names each built file with an 8-character hash of its content, such as
					// react-Bx3k9aQz.js; the hash goes, so the time adds up under the file's plain name.
					const file = (script.sourceURL.split("/").at(-1) ?? "other").replace(/-[\w-]{8}\.js$/, "") || "other";
					seen.scripts[file] = (seen.scripts[file] ?? 0) + script.duration;
				}
			}
		}).observe({ type: "long-animation-frame", buffered: false });
	});

	const box = await page.locator("canvas.maplibregl-canvas").boundingBox();
	if (box === null) throw new Error("the map has no canvas");
	const start = { x: box.x + box.width / 2, y: box.y + box.height / 2 };
	await page.mouse.move(start.x, start.y);
	await page.mouse.down();
	// About two seconds of steady dragging, one small step a frame, out and back.
	for (let i = 0; i < 120; i += 1) {
		const step = i < 60 ? i : 120 - i;
		await page.mouse.move(start.x + step * 4, start.y + step * 2);
		await page.waitForTimeout(16);
	}
	await page.mouse.up();

	const seen: Seen = await page.evaluate(() => {
		const s = window.__seen;
		if (s === undefined) return { frames: [], scripts: {}, longFrames: 0, longFrameMs: 0 };
		s.stop = true;
		// The first entry runs from the recorder's start, not from one redraw to the next, so it goes.
		return { frames: s.frames.slice(1), scripts: s.scripts, longFrames: s.longFrames, longFrameMs: s.longFrameMs };
	});
	// Ten frames or fewer means the recorder barely ran, and the numbers would mean nothing.
	expect(seen.frames.length).toBeGreaterThan(10);

	const slow = seen.frames.filter((f) => f > 16.7).length;
	const report = {
		frames: seen.frames.length,
		slowerThan16_7ms: slow,
		medianMs: Number(median(seen.frames).toFixed(1)),
		p95Ms: Number(share(seen.frames, 0.95).toFixed(1)),
		longFramesOver50ms: seen.longFrames,
		// The rest of a long frame, beyond its scripts, is the browser drawing: style, layout and the
		// map's WebGL.
		longFramesTotalMs: Math.round(seen.longFrameMs),
		scriptMsInLongFramesByFile: Object.fromEntries(Object.entries(seen.scripts).map(([file, ms]) => [file, Math.round(ms)])),
	};
	console.log(JSON.stringify(report, null, 2));
	await test.info().attach("drag.json", { body: JSON.stringify(report, null, 2), contentType: "application/json" });
});
