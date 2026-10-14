/// <reference types="node" />
// rules.test.ts: the page-wide rules of UI-15 that no single module owns, checked over the source
// text of every file under src/: one theme of colours, calm motion, and icons from one font. It runs
// under vitest, the test runner, beside the module tests, and reads the files rather than running
// them.

import { readFileSync, readdirSync } from "node:fs";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const SRC = dirname(fileURLToPath(import.meta.url));

/** Every source file the page ships: no tests, no fixtures. */
const FILES = readdirSync(SRC, { recursive: true, encoding: "utf8" })
	.filter((f) => /\.(ts|tsx|css)$/.test(f) && !/\.test\.tsx?$/.test(f) && !f.startsWith("testing"))
	.map((f) => ({ file: f, text: readFileSync(join(SRC, f), "utf8") }));

const STYLES = readFileSync(join(SRC, "styles.css"), "utf8");
const THEME = /@theme static \{[\s\S]*?\n\}/.exec(STYLES)?.[0] ?? "";

/** The text with its comments blanked out, line breaks kept, since a comment draws nothing. */
const withoutComments = (text: string): string =>
	text.replaceAll(/\/\*[\s\S]*?\*\//g, (c) => c.replaceAll(/[^\n]/g, " ")).replaceAll(/(?<![:"'])\/\/.*$/gm, "");

/** Each line of every file, numbered, with comments and the theme block of styles.css left out. */
const LINES = FILES.flatMap(({ file, text }) =>
	withoutComments(file === "styles.css" ? text.replace(THEME, "") : text)
		.split("\n")
		.map((line, i) => ({ where: `${file}:${i + 1}`, line })),
);

const NAMED_COLOURS = ["white", "black", "red", "green", "blue", "grey", "gray", "yellow", "orange", "purple", "pink", "brown"];

describe("UI-15 one theme of colours", () => {
	it("keeps the theme in styles.css", () => {
		expect(THEME).toContain("--color-primary: #1F5FA8");
		// Tailwind, the library whose class names set the styles, has lost its own palette, so no class
		// can bring a colour from outside the theme.
		expect(THEME).toContain("--color-*: initial;");
	});

	it("lists every colour written outside the theme's Material roles, and finds none", () => {
		const literal = new RegExp(
			[
				String.raw`#[0-9a-fA-F]{3,8}\b`,
				String.raw`\b(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch)\(`,
				String.raw`(?:["'\x60]|:\s*)(?:${NAMED_COLOURS.join("|")})\b`,
			].join("|"),
		);
		expect(LINES.filter(({ line }) => literal.test(line)).map(({ where, line }) => `${where}: ${line.trim()}`)).toEqual([]);
	});
});

describe("UI-15 calm motion", () => {
	/**
	 * Every duration written in the source: times in CSS, the page's styles, and Tailwind's duration
	 * and delay classes.
	 */
	const durations = LINES.flatMap(({ where, line }) => [
		...[...line.matchAll(/(?<![\w.-])(\d+(?:\.\d+)?)(ms|s)\b/g)].map((m) => ({ where, ms: Number(m[1]) * (m[2] === "s" ? 1000 : 1) })),
		...[...line.matchAll(/\b(?:duration|delay)-(\d+)\b/g)].map((m) => ({ where, ms: Number(m[1]) })),
	]);

	it("finds the durations it checks", () => {
		expect(durations.length).toBeGreaterThanOrEqual(4);
	});

	it("makes every transition and animation last 300 ms or less", () => {
		expect(durations.filter((d) => d.ms > 300)).toEqual([]);
	});

	it("lets nothing loop", () => {
		expect(LINES.filter(({ line }) => /\binfinite\b|@keyframes|animation-iteration-count/.test(line)).map((l) => l.where)).toEqual([]);
	});

	it("stops all movement when the visitor asks for reduced motion", () => {
		const rule = /@media \(prefers-reduced-motion: reduce\) \{([\s\S]*?)\n\}/.exec(STYLES)?.[1] ?? "";
		expect(rule).toMatch(/\*,\s*\*::before,\s*\*::after\s*\{/);
		expect(rule).toContain("animation: none !important;");
		expect(rule).toContain("transition: none !important;");
	});
});

describe("UI-15 icons", () => {
	const SYMBOLS = new Set(
		[...readFileSync(join(SRC, "..", "node_modules", "material-symbols", "index.d.ts"), "utf8").matchAll(/^\s*"([a-z0-9_]+)",?$/gm)].map((m) => m[1]),
	);
	const tsx = FILES.filter(({ file }) => file.endsWith(".tsx"));

	it("draws every icon from Material Symbols", () => {
		const names = tsx.flatMap(({ text }) => [...text.matchAll(/\b(?:name|icon)="([a-z0-9_]+)"/g)].map((m) => m[1] ?? ""));
		const ternaries = tsx.flatMap(({ text }) => [...text.matchAll(/\?\s*"([a-z_]+)"\s*:\s*"([a-z_]+)"/g)].flatMap((m) => [m[1] ?? "", m[2] ?? ""]));
		const used = [...names, ...ternaries.filter((n) => ["add", "remove"].includes(n))];
		expect(used).toEqual(expect.arrayContaining(["add", "remove"]));
		expect(used.filter((n) => !SYMBOLS.has(n))).toEqual([]);
	});

	it("draws no icon any other way: the only drawings are the group shapes and the compass", () => {
		const withSvg = tsx.filter(({ text }) => text.includes("<svg")).map(({ file }) => relative(".", file));
		expect(withSvg.toSorted()).toEqual([join("design", "components.tsx"), join("map", "MapControls.tsx")]);
		expect(FILES.filter(({ text }) => /<img\b|font-awesome|material-icons(?!-)/.test(text)).map((f) => f.file)).toEqual([]);
	});
});

describe("UI-15 named surfaces", () => {
	/**
	 * The class lists the page repeats, each named once in styles.css. A component that spells one out
	 * by hand would drift from the others the day someone changes one.
	 */
	const SHARED = {
		card: "rounded-medium bg-surface-low",
		"map-button": "rounded-medium border-0 bg-surface-high",
		overlay: "pointer-events-none absolute inset-0 overflow-hidden",
		"on-point": "-translate-x-1/2 -translate-y-1/2",
		message: "top-1/2 left-1/2",
	};

	it("names each shared surface once, in styles.css", () => {
		for (const name of Object.keys(SHARED)) expect(STYLES).toMatch(new RegExp(String.raw`\.${name} \{\s*@apply `));
	});

	it("finds no component that spells a shared surface out by hand", () => {
		const tsx = LINES.filter(({ where }) => where.includes(".tsx:"));
		expect(tsx.filter(({ line }) => Object.values(SHARED).some((classes) => line.includes(classes))).map((l) => l.where)).toEqual([]);
	});
});
