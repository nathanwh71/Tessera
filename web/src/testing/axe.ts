// testing/axe.ts: an accessibility scan with axe, a library that checks a page against the web's
// accessibility rules, for the component tests that run in jsdom, a pretend browser inside the test
// runner with no screen. It proves that every control can be reached and named without a mouse. The
// testing module holds the fakes and helpers for tests alone; the page and control tests call this.

import axe from "axe-core";

/**
 * Rules jsdom cannot judge, switched off here and left to the browser tests (e2e/), which run them
 * all:
 * - `color-contrast` needs the colours the browser computes from the stylesheet, which jsdom does
 *   not load; design.test.ts checks every text pair of the theme instead.
 * - `region` asks that all content sit in a landmark, a named region of the page such as its main
 *   part, which a component shown alone cannot do; the page test and the browser tests scan the
 *   whole page with it on.
 */
const JSDOM_BLIND = { "color-contrast": { enabled: false }, region: { enabled: false } } as const;

/** Each violation as its rule and the elements that break it, so a failing test names them. */
export async function violations(root: Element, options: { region?: boolean } = {}): Promise<string[]> {
	const rules = options.region === true ? { "color-contrast": JSDOM_BLIND["color-contrast"] } : JSDOM_BLIND;
	const result = await axe.run(root, { rules });
	return result.violations.map((v) => `${v.id}: ${v.nodes.map((n) => n.target.join(" ")).join(", ")}`);
}
