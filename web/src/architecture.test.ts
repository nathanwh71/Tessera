/// <reference types="node" />
// architecture.test.ts: the modular monolith's walls, checked on every test run.
//
// Each module may import only the modules listed for it, and only through their index.ts.
// The list is a one-way order, so no two modules can depend on each other.

import { readFileSync, readdirSync } from "node:fs";
import { dirname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

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
const isModule = (name: string): name is ModuleName => Object.hasOwn(MAY_IMPORT, name);

/** Every TypeScript file in a module, with the relative imports it makes. */
function sources(module: string): { file: string; imports: string[] }[] {
	const dir = join(SRC, module);
	return readdirSync(dir, { recursive: true, encoding: "utf8" })
		.filter((f) => /\.tsx?$/.test(f))
		.map((f) => {
			const text = readFileSync(join(dir, f), "utf8");
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
						if (target === undefined || !isModule(target)) return true;
						if (target === "testing") return !isTest(file); // fixtures are for tests alone
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

/** Every file that defines a module's `update`: its functional core. */
function updateFiles(): { file: string; text: string }[] {
	return MODULES.flatMap((module) =>
		readdirSync(join(SRC, module), { recursive: true, encoding: "utf8" })
			.filter((f) => /\.ts$/.test(f) && !/\.test\.ts$/.test(f))
			.map((f) => ({ file: join(module, f), text: readFileSync(join(SRC, module, f), "utf8") }))
			.filter(({ text }) => /\bexport const update\b/.test(text)),
	);
}

describe("the functional core", () => {
	it("finds an update in every module that owns messages", () => {
		expect(updateFiles().map((f) => f.file).toSorted()).toEqual([join("app", "app.ts"), join("map", "map.ts")]);
	});

	it("keeps React, the DOM and MapLibre out of every update file (ADR 0004, a pure core)", () => {
		const forbidden = /from\s+"(react|react-dom|react-dom\/client|maplibre-gl)"|\b(document|window|navigator|localStorage|fetch)\b/;
		const breaches = updateFiles()
			.filter(({ text }) => forbidden.test(text.replaceAll(/\/\/.*$|\/\*[\s\S]*?\*\//gm, "")))
			.map(({ file }) => file);
		expect(breaches).toEqual([]);
	});
});

/** Every file of code under src/, with its text, comments stripped. */
function codeFiles(): { file: string; text: string }[] {
	return readdirSync(SRC, { recursive: true, encoding: "utf8" })
		.filter((f) => /\.tsx?$/.test(f) && !/\.test\.tsx?$/.test(f) && !f.startsWith("testing"))
		.map((f) => ({ file: f, text: readFileSync(join(SRC, f), "utf8").replaceAll(/\/\/.*$|\/\*[\s\S]*?\*\//gm, "") }));
}

describe("the thin edge", () => {
	// The code that talks to MapLibre keeps its changing state and its event wiring in two files, so
	// a component holds no `let` and registers no listener of its own: decisions live in pure code.
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
