// playwright.config.ts: browser tests against a running service (e2e/).

import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
	testDir: "e2e",
	// The map draws with WebGL in software here, so tests run one after another and wait for the map.
	workers: 1,
	timeout: 60_000,
	expect: { timeout: 15_000, toHaveScreenshot: { maxDiffPixelRatio: 0.01 } },
	// In the automatic build, the github reporter also writes each failure as an annotation, which
	// shows on the run's page and through GitHub's API without the full log.
	reporter: process.env["CI"] === undefined ? [["list"]] : [["list"], ["github"]],
	// Approved images live beside the tests, named after their story (UI-17, test 3).
	snapshotPathTemplate: "{testDir}/__screenshots__/{arg}{ext}",
	use: {
		...devices["Desktop Chrome"],
		baseURL: process.env["TESSERA_BASE_URL"],
		viewport: { width: 1280, height: 800 },
		deviceScaleFactor: 1,
		launchOptions: {
			args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"],
			// A machine with Chromium already in place, such as a build container, names it here.
			...(process.env["CHROMIUM_PATH"] === undefined ? {} : { executablePath: process.env["CHROMIUM_PATH"] }),
		},
		trace: "retain-on-failure",
	},
});
