/// <reference types="node" />
// build.test.ts: builds the page as `npm run build` does, into a folder of its own, and checks how
// the code is split into files. MapLibre and React change only when a version changes, so each sits
// in a file of its own; a deploy that changes the page's own code then changes one small file, and a
// returning visitor fetches that file alone, keeping the rest from the browser's cache.

import { mkdtempSync, readdirSync, rmSync, statSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { build } from "vite";
import { afterAll, beforeAll, describe, expect, it } from "vitest";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..");
const OUT = mkdtempSync(join(tmpdir(), "tessera-build-"));

/** The page's script files, by the name before the hash, with their size in bytes. */
const scripts = (): Map<string, number> =>
	new Map(
		readdirSync(join(OUT, "assets"))
			// MapLibre's worker, the script that cuts tiles away from the page, was a file of its own already;
			// Rolldown's runtime, its 80 bytes that join the files, changes only with Rolldown.
			.filter((f) => f.endsWith(".js") && !f.startsWith("maplibre-gl-worker") && !f.startsWith("rolldown-runtime"))
			.map((f) => [f.replace(/-[\w-]{8}\.js$/, ""), statSync(join(OUT, "assets", f)).size] as const),
	);

describe("the page's files, split for the cache", () => {
	// A full build takes several seconds, far past the test runner's usual 5-second limit, so the
	// build may take up to a minute.
	beforeAll(async () => {
		await build({ root: ROOT, logLevel: "silent", build: { outDir: OUT, emptyOutDir: true } });
	}, 60_000);
	afterAll(() => rmSync(OUT, { recursive: true, force: true }));

	it("puts MapLibre, React and the page's own code in three files", () => {
		expect([...scripts().keys()].toSorted()).toEqual(["index", "maplibre", "react"]);
	});

	it("keeps the page's own code small, so a deploy re-sends little", () => {
		// 250 kB leaves the page's own code room to grow, while a deploy re-sends at most a quarter of
		// MapLibre's 1 MB; vite.config.ts relies on this limit.
		expect(scripts().get("index")).toBeLessThan(250_000);
	});
});
