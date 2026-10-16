// map/landing.ts: the pins' landing as the page opens (UI-15, criterion 6), as a pure state machine
// of the map module. useLanding, in canvas-hooks.ts, sends each event here, and MapCanvas.tsx draws
// the stage it gets back.
//
// A state machine is a value that says which stage something is at, and a function that moves it on
// when an event happens. This one reads only its inputs and changes nothing outside itself
// (ADR 0004), so every path through the landing is tested with values alone, including the two a
// browser test rarely reaches: a visitor who moves the map before the first places arrive, and a
// map whose places never come. useLanding keeps the timers, and MapCanvas.tsx the drawing.

import { type GroupKey, dropDelays, dropMs } from "../design";

/**
 * A pin in view, as the map reports it: where its tip sits on screen and its group, which gives it
 * its colour and shape.
 */
export interface PinInView {
	/** Made from the place's id, so it stays the same across draws and React keeps each pin in place. */
	readonly key: string;
	readonly x: number;
	readonly y: number;
	readonly group: GroupKey;
}

/** A pin dropping in: where it lands and when it starts, in milliseconds after the first. */
export interface Drop extends PinInView {
	readonly delay: number;
}

/**
 * The stages of the landing. A closed set of named cases: a `switch` must handle each, and the pins
 * cannot be both dropping and landed (ADR 0004).
 */
export type Landing =
	/**
	 * The first places have not come yet (`fed` false), or have come and the map has not yet come to
	 * rest.
	 */
	| { readonly stage: "waiting"; readonly fed: boolean }
	/** The pins in view are dropping; `ms` is how long the whole show takes. */
	| { readonly stage: "dropping"; readonly drops: readonly Drop[]; readonly ms: number }
	/** The map's own pins show; nothing moves this stage on. */
	| { readonly stage: "landed" };

/**
 * What can happen during the landing: the events that move the ***state machine*** on. Like the
 * stages, a closed set, so `step` must answer each one (ADR 0004).
 */
export type LandingEvent =
	/** The clustered source, the data the map draws the places from, gained its first places. */
	| { readonly name: "placesFed" }
	/**
	 * The map came to rest, with these pins in view; `reduced` is the visitor's wish for less motion
	 * (criterion 8).
	 */
	| { readonly name: "mapIdle"; readonly pins: readonly PinInView[]; readonly reduced: boolean }
	/** The visitor moved the map, so the show is not wanted. */
	| { readonly name: "visitorMoved" }
	/** The last drop came to rest. */
	| { readonly name: "dropsDone" }
	/** The wait for the first places ran out. */
	| { readonly name: "timedOut" };

/** Before the page has its places: every page load starts here, so the drop waits for them. */
export const START: Landing = { stage: "waiting", fed: false };

/** The one landed stage, shared, for the reason `step` gives. */
const LANDED: Landing = { stage: "landed" };

/**
 * The stage after an event. An event that means nothing at the current stage returns that stage
 * unchanged, the same object, so React sees no change and skips a redraw.
 */
export function step(landing: Landing, event: LandingEvent): Landing {
	switch (event.name) {
		case "visitorMoved":
		case "timedOut":
			return landing.stage === "landed" ? landing : LANDED;
		case "placesFed":
			return landing.stage === "waiting" && !landing.fed ? { stage: "waiting", fed: true } : landing;
		case "mapIdle":
			return landing.stage === "waiting" && landing.fed ? drop(event.pins, event.reduced) : landing;
		case "dropsDone":
			return landing.stage === "dropping" ? LANDED : landing;
	}
}

/**
 * The drop for the pins in view: none, and so landed at once, when there is nothing to drop or the
 * visitor asks for less motion.
 */
function drop(pins: readonly PinInView[], reduced: boolean): Landing {
	const delays = dropDelays(pins.length, reduced);
	if (delays.length === 0) return LANDED;
	return { stage: "dropping", drops: pins.map((pin, i) => ({ ...pin, delay: delays[i] ?? 0 })), ms: dropMs(delays) };
}
