/// <reference types="node" />
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { DROP_SPREAD_MS, GROUP_KEYS, GROUP_LOOKS, MOTION, SHAPE_PATHS, contrastRatio, dropDelays, dropMs, emphasised, movementMs, themeFrom } from ".";

const STYLES = readFileSync(join(dirname(fileURLToPath(import.meta.url)), "..", "styles.css"), "utf8");

/** The theme's colours, as styles.css sets them. */
const TOKENS: ReadonlyMap<string, string> = new Map(
	[...STYLES.matchAll(/(--color-[a-z-]+):\s*(#[0-9a-fA-F]{6})\s*;/g)].map((m) => [m[1] ?? "", m[2] ?? ""]),
);

function token(name: string): string {
	const value = TOKENS.get(`--color-${name}`);
	if (value === undefined) throw new Error(`the theme has no --color-${name}`);
	return value;
}

describe("UI-12 shapes as well as colours", () => {
	it("gives each group its own shape", () => {
		expect(Object.fromEntries(GROUP_KEYS.map((g) => [g, GROUP_LOOKS[g].shape]))).toEqual({
			food: "circle",
			outdoors: "triangle",
			culture: "square",
			transport: "diamond",
		});
		expect(new Set(GROUP_KEYS.map((g) => SHAPE_PATHS[GROUP_LOOKS[g].shape])).size).toBe(4);
	});

	it("meets 4.5:1 for text in each group colour on white", () => {
		for (const g of GROUP_KEYS) {
			expect.soft(contrastRatio(token(`group-${g}`), token("surface-lowest")), g).toBeGreaterThanOrEqual(4.5);
		}
	});

	it("takes each group's colour from the theme, never from code", () => {
		const theme = themeFrom((variable) => TOKENS.get(variable) ?? "");
		expect(theme.groups).toEqual({ food: token("group-food"), outdoors: token("group-outdoors"), culture: token("group-culture"), transport: token("group-transport") });
	});
});

describe("UI-15 and UI-12 contrast", () => {
	// Every pair of text and background the page draws: [text, background].
	const PAIRS = [
		["on-surface", "surface"],
		["on-surface", "surface-low"],
		["on-surface", "surface-lowest"],
		["on-surface-variant", "surface-low"],
		["on-surface-variant", "surface-lowest"],
		["on-surface-variant", "surface-high"],
		["primary", "surface-low"],
		["primary", "surface-lowest"],
		["on-primary", "primary"],
		["on-secondary-container", "secondary-container"],
		["secondary-container", "on-secondary-container"],
		["surface-low", "on-surface-variant"],
		["inverse-on-surface", "inverse-surface"],
		["north", "surface-low"],
	] as const;

	it.each(PAIRS)("meets 4.5:1 for %s on %s", (text, background) => {
		expect(contrastRatio(token(text), token(background))).toBeGreaterThanOrEqual(4.5);
	});

	// A pair listed twice runs twice and hides a pair left out; the list once held a duplicate.
	it("lists each text pair once", () => {
		expect(new Set(PAIRS.map((pair) => pair.join(" on "))).size).toBe(PAIRS.length);
	});

	it("measures contrast as WCAG does", () => {
		expect(contrastRatio("#000000", "#FFFFFF")).toBeCloseTo(21, 5);
		expect(contrastRatio("#777777", "#FFFFFF")).toBeCloseTo(4.48, 2);
	});
});

describe("UI-15 motion", () => {
	it("moves for no longer than 300 ms, and not at all for a visitor who asks for less", () => {
		expect(Math.max(...Object.values(MOTION))).toBeLessThanOrEqual(300);
		expect(movementMs(false)).toBe(300);
		expect(movementMs(true)).toBe(0);
	});

	it("eases as Material's emphasised curve: fast away, slow to rest", () => {
		expect([emphasised(0), emphasised(1)]).toEqual([0, 1]);
		expect(emphasised(0.25)).toBeGreaterThan(0.5);
		const samples = Array.from({ length: 21 }, (_, i) => emphasised(i / 20));
		expect(samples.every((v, i) => i === 0 || v >= (samples[i - 1] ?? 0))).toBe(true);
	});
});

describe("UI-15 the pins drop in as the page opens", () => {
	it("drops each pin in 300 ms, one after another, 30 ms apart", () => {
		expect(dropDelays(4, false)).toEqual([0, 30, 60, 90]);
		expect(dropMs(dropDelays(4, false))).toBe(90 + 300);
	});

	it("brings the pins closer together in a crowded view, so the last starts by 600 ms", () => {
		const delays = dropDelays(101, false);
		expect(delays.at(-1)).toBe(DROP_SPREAD_MS);
		expect(delays.every((d, i) => i === 0 || d >= (delays[i - 1] ?? 0))).toBe(true);
	});

	it("drops nothing for a visitor who asks for reduced motion", () => {
		expect(dropDelays(10, true)).toEqual([]);
		expect(dropMs(dropDelays(10, true))).toBe(0);
	});
});
