// e2e/cannot-draw.spec.ts: the map in a browser that cannot draw it (US-23, criterion 9). The browser
// starts with WebGL, its way to draw with the graphics card, turned off, as some offices set their
// browsers. Playwright starts a separate browser for a file with its own launch settings, so this test
// has a file of its own.

import { expect, test } from "@playwright/test";

test.use({
	launchOptions: {
		args: ["--disable-webgl", "--disable-webgl2", "--disable-3d-apis"],
		// A file's own launch settings replace those of playwright.config.ts, so this file names a
		// machine's own Chromium, such as a build container's, as that one does.
		...(process.env["CHROMIUM_PATH"] === undefined ? {} : { executablePath: process.env["CHROMIUM_PATH"] }),
	},
});

test("US-23 criterion 9: says so in words, with no zoom buttons over a blank map", async ({ page }) => {
	await page.goto("/");
	await expect(page.getByRole("alert")).toHaveText(/^This browser cannot draw the map\./);
	await expect(page.getByRole("button", { name: /Zoom in/ })).toHaveCount(0);
});
