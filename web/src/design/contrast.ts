// design/contrast.ts: the contrast ratio of WCAG 2.2, the web's accessibility standard, for the checks of UI-12 and UI-15.

/** Reads `#RRGGBB`, a colour as six hex digits, into its red, green and blue from 0 to 255; null for any other text. */
export function parseHex(colour: string): readonly [number, number, number] | null {
	const match = /^#([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})$/i.exec(colour.trim());
	return match === null ? null : [parseInt(match[1] ?? "", 16), parseInt(match[2] ?? "", 16), parseInt(match[3] ?? "", 16)];
}

/** WCAG's relative luminance, how bright a colour looks from 0 (black) to 1 (white), of a colour in sRGB, the web's colour space. */
function luminance([r, g, b]: readonly [number, number, number]): number {
	const linear = (c: number): number => {
		const s = c / 255;
		return s <= 0.04045 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
	};
	return 0.2126 * linear(r) + 0.7152 * linear(g) + 0.0722 * linear(b);
}

/**
 * The contrast ratio of two `#RRGGBB` colours, from 1 (the same colour) to 21 (black on white), in
 * either order. Throws for any other text, since only tests call it, over the theme's own values.
 */
export function contrastRatio(a: string, b: string): number {
	const [ca, cb] = [parseHex(a), parseHex(b)];
	if (ca === null || cb === null) throw new Error(`'${a}' and '${b}' must both be #RRGGBB`);
	const [hi, lo] = [luminance(ca), luminance(cb)].toSorted((x, y) => y - x) as [number, number];
	return (hi + 0.05) / (lo + 0.05);
}
