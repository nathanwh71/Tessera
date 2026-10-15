// map/style.ts: the map's style, the description of what MapLibre, the library that draws the map,
// draws (US-23, UI-10, UI-11, UI-15, UI-17). One pure function of the map module builds it from the
// model, the one object that holds everything the page shows: it reads only its inputs and changes
// nothing outside itself (ADR 0004). MapCanvas.tsx builds a style each time the layers that are on
// change, and hands it to MapLibre. A style holds sources, the data to draw from, and layers, which
// say how to draw it; a symbol layer draws an icon or a name at each point.
//
// The stack, from the bottom: a plain background, with nothing drawn beneath the places (UI-15,
// criterion 9); one invisible layer for each layer that is on, which only makes MapLibre load its
// tiles, the square pieces of the map, for places.ts; the names; the pins; and the clusters, each one
// mark for several places close together.
//
// MapLibre places symbols from the top layer down, and each placed icon or name claims its space. So
// clusters and pins claim theirs first, always shown; and the names come last, each taking a side only
// where no icon or earlier name stands (UI-17, criteria 1 to 4). Names and pins sit in separate layers
// for that reason: in one layer a later pin, drawn regardless, could cover an earlier name.

import type {
	ExpressionSpecification,
	LayerSpecification,
	SourceSpecification,
	StyleSpecification,
	SymbolLayerSpecification,
	VariableAnchorOffsetCollectionSpecification,
} from "maplibre-gl";
import { GROUP_KEYS, type GroupKey, IMAGE_MARGIN, PIN_SIZE, type Theme } from "../design";
import { type LayerId, tileUrl } from "../layers";
import { level } from "../shared";
import { CLUSTER_PREFIX, PIN_PREFIX, TOOLTIP_IMAGE } from "./images";
import { LAST_CLUSTER_LEVEL, NAMES_FROM_LEVEL, toMapZoom } from "./levels";

/** The Roboto font files for the names, served from the page's own site (UI-15, criterion 2). */
export interface FontFiles {
	readonly latin: string;
	readonly latinExt: string;
}

/**
 * Everything the style depends on, passed in whole, so buildStyle reads nothing else and the same
 * input always gives the same style (ADR 0004).
 */
export interface StyleInput {
	/**
	 * The page's own origin, the scheme, host and port it came from, such as
	 * `http://tessera.office.lan`, on which the ***tile*** addresses are built (US-23, criterion 7).
	 */
	readonly origin: string;
	/**
	 * The ***layers*** that are on; each gets a tile source and a feed layer, and no other layer's
	 * tiles are asked for (US-23, criteria 1 and 2).
	 */
	readonly visible: readonly LayerId[];
	/**
	 * False while the pins drop in as the page opens: the map's own pins and names wait unseen until
	 * they land (UI-15, criterion 6).
	 */
	readonly landed: boolean;
	/**
	 * The page's colours, read from its style sheet, so the map's background and names match the
	 * rest of the page (UI-15, criterion 1).
	 */
	readonly theme: Theme;
	/** The font files' addresses, which the build gives, so the style names files that exist. */
	readonly fonts: FontFiles;
}

/**
 * The ids of the style's sources: the clustered places, and one tile source per layer that is on.
 * Every tile source's id starts `tiles:`, which maplibre.ts relies on to tell tile events and errors
 * from all others.
 */
export const SOURCE = {
	places: "places",
	tiles: (layerId: LayerId): string => `tiles:${layerId}`,
} as const;

/**
 * The ids of the style's layers, named once here, so the code that queries or repaints a layer
 * cannot misspell it.
 */
export const LAYER = {
	background: "background",
	feed: (layerId: LayerId): string => `feed:${layerId}`,
	names: "names",
	pins: "pins",
	clusters: "clusters",
} as const;

/**
 * The highest level at which the map asks the service for new tiles; closer in, MapLibre stretches the
 * tiles it already has. MapLibre's tiles are 512 pixels wide, so at level 16 it asks for tiles of the
 * service's grid level 15, each about 1.1 km wide over Macau. Their grid of 4,096 units puts a place
 * within 30 cm of its true spot, finer than a pin can show, so closer tiles would cost requests and
 * show nothing new.
 */
export const TILE_MAX_LEVEL = level(16);

/** The font name the names use; `font-faces` in the style maps it to the files above. */
export const NAME_FONT = "Roboto Regular";

/**
 * The size of a name, in pixels: the label medium of Material, Google's design system, as the
 * prototype's tooltips.
 */
const NAME_SIZE = 12;

/**
 * The tooltip's padding around a name, in pixels, as the prototype's 4 by 8 (top and bottom, then
 * sides). A tooltip is the small dark box that holds a name (UI-11).
 */
const TOOLTIP_PADDING = { block: 4, inline: 8 } as const;

/**
 * The space MapLibre keeps around each name's text, in pixels. MapLibre tests a fitted tooltip's box
 * where the name would sit before its anchor moves it, which is on the pin itself, so the tooltip
 * claims no space of its own; the text claims it instead, padded by at least the tooltip's widest
 * padding so that two tooltips never touch. One pixel more leaves a hairline between them.
 */
const NAME_PADDING = TOOLTIP_PADDING.inline + 1;

/**
 * Where a name may sit, in the order tried (UI-17, criterion 3): right of the pin, left, above, below.
 * Each pair is a MapLibre anchor (the side of the text that touches the place) and an offset in ems,
 * where one em is the text's size. Each offset puts the padded text clear of the pin's whole image,
 * shadow margin included. A name beside the pin sits at the height of its head, 25 pixels above the tip.
 */
function nameAnchors(): VariableAnchorOffsetCollectionSpecification {
	// Pixels to ems, to two decimals: finer steps than a hundredth of an em move nothing on screen.
	const em = (px: number): number => Math.round((px * 100) / NAME_SIZE) / 100;
	// The text's own padding, and one pixel more, so a hairline parts the tooltip from the pin.
	const clear = NAME_PADDING + 1;
	const halfWidth = PIN_SIZE.width / 2 + IMAGE_MARGIN;
	const top = PIN_SIZE.height + IMAGE_MARGIN; // the image's top edge, above the tip
	const bottom = IMAGE_MARGIN; // the image's bottom edge, below the tip
	const head = 25;
	return [
		"left", [em(halfWidth + clear), -em(head)],
		"right", [-em(halfWidth + clear), -em(head)],
		"bottom", [0, -em(top + clear)],
		"top", [0, em(bottom + clear)],
	];
}

/**
 * The pixels between a pin image's bottom edge and its tip, which `icon-offset` takes back
 * (design/canvas.ts).
 */
const PIN_TIP_GAP = IMAGE_MARGIN;

/**
 * The ***cluster***'s radius in pixels: two places closer than this on screen merge into one cluster
 * (UI-10, criterion 1).
 */
export const CLUSTER_RADIUS_PX = 48;

/**
 * MapLibre counts each group's places in a cluster through these properties: for each group, add 1
 * for a place in it, else 0. The counts name the cluster's image, which rings the cluster in its
 * groups' colours in proportion (UI-10, criterion 1).
 */
const CLUSTER_PROPERTIES = Object.fromEntries(GROUP_KEYS.map((g) => [g, ["+", ["case", ["==", ["get", "group"], g], 1, 0]]]));

/**
 * A source with no places, made afresh each time, since the types of GeoJSON, the form of points and
 * shapes in JSON, the text form of a value that programs exchange, allow a caller to change the
 * object.
 */
const empty = (): GeoJSON.FeatureCollection => ({ type: "FeatureCollection", features: [] });

/**
 * The style's sources: a vector tile source for each layer that is on, which no layer draws but
 * which loads the places, and the clustered GeoJSON source that places.ts fills, since MapLibre
 * clusters GeoJSON alone (ADR 0005).
 */
function sources(input: StyleInput): Record<string, SourceSpecification> {
	const tiles = input.visible.map((id): [string, SourceSpecification] => [
		SOURCE.tiles(id),
		{ type: "vector", tiles: [tileUrl(input.origin, id)], minzoom: 0, maxzoom: toMapZoom(TILE_MAX_LEVEL) },
	]);
	return {
		...Object.fromEntries(tiles),
		[SOURCE.places]: {
			type: "geojson",
			data: empty(),
			cluster: true,
			clusterMaxZoom: toMapZoom(LAST_CLUSTER_LEVEL),
			clusterRadius: CLUSTER_RADIUS_PX,
			clusterProperties: CLUSTER_PROPERTIES,
		},
	};
}

/** An invisible layer whose only work is to make MapLibre load the layer's tiles (see places.ts). */
function feedLayer(id: LayerId): LayerSpecification {
	return {
		id: LAYER.feed(id),
		type: "circle",
		source: SOURCE.tiles(id),
		"source-layer": id,
		paint: { "circle-radius": 0, "circle-opacity": 0, "circle-stroke-opacity": 0 },
	};
}

/**
 * True for a single place: MapLibre gives only a cluster a `point_count`, so names and pins skip
 * clusters (UI-11, criterion 3).
 */
const notCluster: ExpressionSpecification = ["!", ["has", "point_count"]];
/** Each place's rank in the fixed order, which decides whose name wins (UI-17, criterion 2). */
const sortKey: ExpressionSpecification = ["get", "sort"];

/**
 * The cluster's image name as a style expression, a small formula MapLibre works out for each
 * cluster: `cluster:` and its four groups' counts, such as `cluster:3-0-1-0`.
 */
function clusterImage(): ExpressionSpecification {
	const count = (g: GroupKey): ExpressionSpecification => ["to-string", ["get", g]];
	const [food, outdoors, culture, transport] = GROUP_KEYS.map(count);
	// There are always four groups, but the compiler cannot know it, so each takes "" as a default.
	return ["concat", CLUSTER_PREFIX, food ?? "", "-", outdoors ?? "", "-", culture ?? "", "-", transport ?? ""];
}

/**
 * A name in a plain tooltip (UI-11, criterion 1): light text on a dark box that MapLibre stretches
 * around it. The box and the text are placed together, so a name with no room leaves no empty box.
 */
function nameLayout(): NonNullable<SymbolLayerSpecification["layout"]> {
	return {
		"text-field": ["get", "name"],
		"text-font": ["literal", [NAME_FONT]],
		"text-size": NAME_SIZE,
		// Material's label medium spaces its letters 0.5 pixels apart, about 0.04 em at 12 pixels.
		"text-letter-spacing": 0.04,
		// A long name wraps at 20 ems, 240 pixels, rather than run across its neighbours' pins.
		"text-max-width": 20,
		"text-variable-anchor-offset": nameAnchors(),
		"text-justify": "auto",
		"icon-image": TOOLTIP_IMAGE,
		"icon-text-fit": "both",
		"icon-text-fit-padding": [TOOLTIP_PADDING.block, TOOLTIP_PADDING.inline, TOOLTIP_PADDING.block, TOOLTIP_PADDING.inline],
		// The box always draws with its text and claims no space; the padded text claims it (see
		// NAME_PADDING).
		"icon-allow-overlap": true,
		"icon-ignore-placement": true,
		"text-padding": NAME_PADDING,
		// UI-17, criterion 2: the fixed order, kept across tiles by sorting on the sort key, not on
		// screen position.
		"symbol-sort-key": sortKey,
		"symbol-z-order": "source",
	};
}

/**
 * A paint setting: a layer, one of its paint properties, and the value. Kept as plain data, so the
 * style and useStyle apply the very same list and cannot disagree.
 */
export type PaintSetting = readonly [layer: string, property: "icon-opacity" | "text-opacity", value: number];

/**
 * The paint that shows or hides the map's own pins and names while the dropping pins land (UI-15,
 * criterion 6). The map applies these alone when the pins land, rather than a new style, since a new
 * style would empty the places' source and make every pin blink.
 */
export function landingPaint(landed: boolean): readonly PaintSetting[] {
	const shown = landed ? 1 : 0;
	return [
		[LAYER.names, "text-opacity", shown],
		[LAYER.names, "icon-opacity", shown],
		[LAYER.pins, "icon-opacity", shown],
	];
}

/**
 * One layer's share of the landing paint, as a style's `paint` object, so a new style starts with
 * the pins shown or hidden as they are now.
 */
const paintOf = (layer: string, landed: boolean): Record<string, number> =>
	Object.fromEntries(landingPaint(landed).filter(([l]) => l === layer).map(([, property, value]) => [property, value]));

/**
 * The three symbol layers that draw the places, in the order the file's opening comment explains:
 * names, then pins, then clusters on top, so icons claim their space before any name.
 */
function placeLayers(theme: Theme, landed: boolean): LayerSpecification[] {
	return [
		{
			id: LAYER.names,
			type: "symbol",
			source: SOURCE.places,
			filter: notCluster,
			// UI-11, criterion 2: below level 16 no name shows.
			minzoom: toMapZoom(NAMES_FROM_LEVEL),
			layout: nameLayout(),
			paint: { "text-color": theme.inverseOnSurface, ...paintOf(LAYER.names, landed) },
		},
		{
			id: LAYER.pins,
			type: "symbol",
			source: SOURCE.places,
			filter: notCluster,
			layout: {
				"icon-image": ["concat", PIN_PREFIX, ["get", "group"]],
				"icon-anchor": "bottom",
				"icon-offset": [0, PIN_TIP_GAP],
				// UI-17, criteria 4 and 5: every pin shows; its name gives way instead.
				"icon-allow-overlap": true,
				// The image's shadow margin already keeps names clear of the pin.
				"icon-padding": 0,
				"symbol-sort-key": sortKey,
				"symbol-z-order": "source",
			},
			paint: paintOf(LAYER.pins, landed),
		},
		{
			id: LAYER.clusters,
			type: "symbol",
			source: SOURCE.places,
			filter: ["has", "point_count"],
			// No landing paint: only single pins drop in, so clusters show at once (UI-15, criterion
			// 6). Every cluster shows, and its image holds its own margin, as a pin's does.
			layout: { "icon-image": clusterImage(), "icon-allow-overlap": true, "icon-padding": 0 },
		},
	];
}

/**
 * The style for the ***model***'s state, built by a ***pure function***: the same input always gives
 * the same style, so the tests check it as a value (ADR 0004).
 */
export function buildStyle(input: StyleInput): StyleSpecification {
	return {
		version: 8,
		// The names draw from the self-hosted Roboto, so the style needs no `glyphs` server. The ranges
		// come from Fontsource's for each file, so the browser fetches the extended file only for a name
		// that needs it.
		"font-faces": {
			[NAME_FONT]: [
				{ url: input.fonts.latin, "unicode-range": ["U+0000-00FF", "U+0131", "U+0152-0153", "U+02BB-02BC", "U+02C6", "U+02DA", "U+02DC", "U+2000-206F", "U+20AC", "U+2122", "U+2212", "U+FEFF", "U+FFFD"] },
				{ url: input.fonts.latinExt, "unicode-range": ["U+0100-02BA", "U+02BD-02C5", "U+02C7-02CC", "U+02CE-02D7", "U+02DD-02FF", "U+1E00-1E9F", "U+1EF2-1EFF", "U+2C60-2C7F", "U+A720-A7FF"] },
			],
		},
		sources: sources(input),
		layers: [
			{ id: LAYER.background, type: "background", paint: { "background-color": input.theme.surface } },
			...input.visible.map(feedLayer),
			...placeLayers(input.theme, input.landed),
		],
	};
}
