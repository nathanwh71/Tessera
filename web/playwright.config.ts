// playwright.config.ts: the settings of Playwright, the tool that drives a real browser through the
// tests in e2e/, against a running service named by TESSERA_BASE_URL. `npm run e2e` reads this file;
// the unit tests under src/ do not.

import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
	testDir: "e2e",
	// `TESSERA_MEASURE=1 npm run e2e` runs the measurements in place of the tests, so a plain run
	// never waits on them; a measurement prints numbers rather than passing or failing.
	testMatch: process.env["TESSERA_MEASURE"] === undefined ? "**/*.spec.ts" : "**/*.measure.ts",
	// The map draws with WebGL, the browser's 3D drawing, in software here, with no graphics card, so
	// tests run one after another and wait for the map.
	workers: 1,
	timeout: 60_000,
	// A screenshot may differ from the approved image in 1 pixel in 100: software drawing smooths
	// edges a little differently from run to run, while a moved name changes far more.
	expect: { timeout: 15_000, toHaveScreenshot: { maxDiffPixelRatio: 0.01 } },
	// In the automatic build, the github reporter also writes each failure as an annotation, which
	// shows on the run's page and through GitHub's API without the full log.
	reporter: process.env["CI"] === undefined ? [["list"]] : [["list"], ["github"]],
	// Approved images live beside the tests, named after their story (UI-17, test 3).
	snapshotPathTemplate: "{testDir}/__screenshots__/{arg}{ext}",
	use: {
		...devices["Desktop Chrome"],
		baseURL: process.env["TESSERA_BASE_URL"],
		// One fixed window size and one device pixel per CSS pixel, so the approved images (UI-17) match
		// on every machine, and map.spec.ts can check the map fills 1280 by 800.
		viewport: { width: 1280, height: 800 },
		deviceScaleFactor: 1,
		launchOptions: {
			// SwiftShader is Chrome's software drawing: the processor does the work of a graphics card,
			// so the map draws on a machine with none, such as the automatic build's, and draws near
			// enough the same pixels on every machine for the approved images. `--use-angle` picks it.
			// Chrome offers WebGL through SwiftShader only with `--enable-unsafe-swiftshader`, since it
			// widens what a hostile web page could attack; these tests open Tessera's own page alone.
			args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"],
			// A machine with Chromium, the open-source core of the Chrome browser, already in place, such
			// as a build container, names it here.
			...(process.env["CHROMIUM_PATH"] === undefined ? {} : { executablePath: process.env["CHROMIUM_PATH"] }),
		},
		// A trace, a recording of each step with the page as it stood, is kept only for a failing test,
		// where it explains the failure, and not for the many that pass.
		trace: "retain-on-failure",
	},
});
