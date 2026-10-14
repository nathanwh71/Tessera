// playwright.config.ts: the settings of Playwright, the tool that drives a real browser through the
// tests in e2e/, against a running service named by TESSERA_BASE_URL. `npm run e2e` reads this file;
// the unit tests under src/ do not.

import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
	testDir: "e2e",
	// `npm run measure` sets TESSERA_MEASURE and runs the measurements in place of the tests
	// (scripts/measure.ts); the tests never run them.
	testMatch: process.env["TESSERA_MEASURE"] === undefined ? "**/*.spec.ts" : "**/*.measure.ts",
	// The map draws with WebGL, the browser's 3D drawing, in software here, with no graphics card, so
	// tests run one after another and wait for the map.
	workers: 1,
	timeout: 60_000,
	expect: { timeout: 15_000, toHaveScreenshot: { maxDiffPixelRatio: 0.01 } },
	reporter: [["list"]],
	// Approved images live beside the tests, named after their story (UI-17, test 3).
	snapshotPathTemplate: "{testDir}/__screenshots__/{arg}{ext}",
	use: {
		...devices["Desktop Chrome"],
		baseURL: process.env["TESSERA_BASE_URL"],
		viewport: { width: 1280, height: 800 },
		deviceScaleFactor: 1,
		launchOptions: {
			args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"],
			// A machine with Chromium, the open-source core of the Chrome browser, already in place, such
			// as a build container, names it here.
			...(process.env["CHROMIUM_PATH"] === undefined ? {} : { executablePath: process.env["CHROMIUM_PATH"] }),
		},
		trace: "retain-on-failure",
	},
});
