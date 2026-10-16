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
			// MapLibre's worker, the script that prepares tiles on a second thread, ships as a file
			// of its own (map/maplibre.ts). Rolldown, the part of Vite that joins modules into
			// files, adds 80 bytes of its own code to link them, which change only with Rolldown.
			// Neither belongs to the split this file checks.
			.filter((f) => f.endsWith(".js") && !f.startsWith("maplibre-gl-worker") && !f.startsWith("rolldown-runtime"))
			.map((f) => [f.replace(/-[\w-]{8}\.js$/, ""), statSync(join(OUT, "assets", f)).size] as const),
	);

describe("the page's files, split for the cache", () => {
	// A full build can outlast the 10 seconds the test runner allows a setup step by default, so this
	// one may take up to a minute.
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
