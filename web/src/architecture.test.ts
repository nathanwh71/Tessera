/// <reference types="node" />
// architecture.test.ts: the walls of the modular monolith, checked on every test run (ADR 0001). A
// modular monolith is one program built from modules, folders of code with a public face, that may
// depend on each other in one direction alone. This file reads every source file under src/ as text,
// under vitest, the test runner, and fails when a file imports what its module may not.
//
// Each module may import only the modules listed for it, and only through their index.ts.
// The list is a one-way order, so no two modules can depend on each other.

import { readFileSync, readdirSync } from "node:fs";
import { dirname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

/** The src/ folder, found from this file's own place so the test runs from any working folder. */
const SRC = dirname(fileURLToPath(import.meta.url));

const MAY_IMPORT = {
	shared: [],
	design: ["shared"],
	layers: ["shared", "design"],
	map: ["shared", "design", "layers"],
	app: ["shared", "design", "layers", "map"],
	testing: ["shared"],
} as const satisfies Record<string, readonly string[]>;

type ModuleName = keyof typeof MAY_IMPORT;
/** True for a folder named in MAY_IMPORT; the compiler then lets the name index the list. */
const isModule = (name: string): name is ModuleName => Object.hasOwn(MAY_IMPORT, name);

/** Every TypeScript file in a module, with the relative imports it makes. */
function sources(module: string): { file: string; imports: string[] }[] {
	const dir = join(SRC, module);
	return readdirSync(dir, { recursive: true, encoding: "utf8" })
		.filter((f) => /\.tsx?$/.test(f))
		.map((f) => {
			const text = readFileSync(join(dir, f), "utf8");
			// Only imports that start with '.' reach another folder of src/; an import by bare name,
			// such as "react", is a package and crosses no wall between modules.
			const imports = [...text.matchAll(/\bfrom\s+"(\.[^"]*)"/g)].map((m) => m[1] ?? "");
			return { file: join(dir, f), imports };
		});
}

/** Every folder of code under src/; `assets` holds files the page ships as they are, such as fonts. */
const MODULES = readdirSync(SRC, { withFileTypes: true })
	.filter((d) => d.isDirectory() && d.name !== "assets")
	.map((d) => d.name);

describe("the modular monolith", () => {
	it("names every folder in its list of modules", () => {
		expect(MODULES.filter((m) => !isModule(m))).toEqual([]);
	});

	for (const module of MODULES.filter(isModule)) {
		const list = MAY_IMPORT[module].join(", ");
		it(`lets ${module} import ${list === "" ? "no other module" : `only ${list}, through their index.ts`}`, () => {
			const isTest = (file: string) => /\.test\.tsx?$/.test(file);
			const breaches = sources(module).flatMap(({ file, imports }) =>
				imports
					.map((spec) => relative(SRC, resolve(dirname(file), spec)).split(sep))
					.filter(([target]) => target !== module)
					.filter(([target, ...rest]) => {
						// A path to no module, such as ../main.tsx, reaches past every public face, so it
						// breaks the wall too.
						if (target === undefined || !isModule(target)) return true;
						if (target === "testing") return !isTest(file); // testing/ serves tests alone
						const allowed: readonly string[] = MAY_IMPORT[module];
						const throughIndex = rest.length === 0 || (rest.length === 1 && rest[0] === "index");
						return !allowed.includes(target) || !throughIndex;
					})
					.map((parts) => `${relative(SRC, file)} imports ${parts.join("/")}`),
			);
			expect(breaches).toEqual([]);
		});
	}
});

/**
 * Every file that defines a module's `update`, the function that makes the new model, the value that
 * holds everything the page shows, from the old one and a message that says what happened: the
 * functional core, where every decision lives (ADR 0004).
 */
function updateFiles(): { file: string; text: string }[] {
	return MODULES.flatMap((module) =>
		readdirSync(join(SRC, module), { recursive: true, encoding: "utf8" })
			.filter((f) => /\.ts$/.test(f) && !/\.test\.ts$/.test(f))
			.map((f) => ({ file: join(module, f), text: readFileSync(join(SRC, module, f), "utf8") }))
			.filter(({ text }) => /\bexport const update\b/.test(text)),
	);
}

describe("the functional core", () => {
	it("finds the page's one update in the map module, which owns every message", () => {
		expect(updateFiles().map((f) => f.file)).toEqual([join("map", "map.ts")]);
	});

	it("keeps React, the DOM and MapLibre out of every update file (ADR 0004, a pure core)", () => {
		const forbidden = /from\s+"(react|react-dom|react-dom\/client|maplibre-gl)"|\b(document|window|navigator|localStorage|fetch)\b/;
		const breaches = updateFiles()
			// Comments go first, since a comment may name `window` or `document` without using them.
			.filter(({ text }) => forbidden.test(text.replaceAll(/\/\/.*$|\/\*[\s\S]*?\*\//gm, "")))
			.map(({ file }) => file);
		expect(breaches).toEqual([]);
	});
});

/**
 * Every file of code the page ships under src/, with its text, comments stripped. Tests and the
 * testing module are left out: they may hold state and listen to events, since they drive the page
 * rather than make it.
 */
function codeFiles(): { file: string; text: string }[] {
	return readdirSync(SRC, { recursive: true, encoding: "utf8" })
		.filter((f) => /\.tsx?$/.test(f) && !/\.test\.tsx?$/.test(f) && !f.startsWith("testing"))
		.map((f) => ({ file: f, text: readFileSync(join(SRC, f), "utf8").replaceAll(/\/\/.*$|\/\*[\s\S]*?\*\//gm, "") }));
}

describe("the thin edge", () => {
	// Changing state and event wiring stay at the edge, so decisions live in pure code. Among the files
	// the page ships, only maplibre.ts and map-handle.ts, the two that drive MapLibre, the library that
	// draws the map, may declare a `let`, a variable the code can change; only maplibre.ts may register
	// a listener with `.on("…")` or `.once("…")`.
	const MAY_HOLD_STATE = [join("map", "maplibre.ts"), join("map", "map-handle.ts")];
	const MAY_LISTEN = [join("map", "maplibre.ts")];

	it("keeps `let` out of every file but the two that drive MapLibre", () => {
		const breaches = codeFiles().filter(({ file, text }) => /\blet\s/.test(text) && !MAY_HOLD_STATE.includes(file)).map((f) => f.file);
		expect(breaches).toEqual([]);
	});

	it("registers MapLibre's events in one file alone", () => {
		const breaches = codeFiles().filter(({ file, text }) => /\.(on|once)\(\s*"/.test(text) && !MAY_LISTEN.includes(file)).map((f) => f.file);
		expect(breaches).toEqual([]);
	});
});
