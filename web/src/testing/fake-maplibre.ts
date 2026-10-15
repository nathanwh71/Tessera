// testing/fake-maplibre.ts: a stand-in for MapLibre, the library that draws the map, in jsdom, the
// pretend browser the tests run in, which has no WebGL, the browser's 3D drawing. The page tests mock
// the "maplibre-gl" module with it, that is, swap it in for the real one. It records what the page
// asks of the map, lets a test fire the map's events, and draws nothing. The testing module holds
// such fakes for tests alone.

type Listener = (...args: unknown[]) => void;

/** The fake map: every method the page calls, each doing as little as keeps the page running. */
export class FakeMap {
	/** Every fake map made, newest last; the page makes its own, and a test reaches it here. */
	static readonly made: FakeMap[] = [];
	/** True makes the next map fail to start, as MapLibre does in a browser with no WebGL; it resets itself. */
	static failNext = false;
	readonly options: Record<string, unknown>;
	readonly calls: { readonly name: string; readonly args: readonly unknown[] }[] = [];
	/**
	 * The page turns rotation off by touch and by keyboard, since the map never turns (UI-19,
	 * criterion 6); these stand-ins let it.
	 */
	readonly touchZoomRotate = { disableRotation: (): void => undefined };
	readonly keyboard = { disableRotation: (): void => undefined };
	readonly #container: HTMLElement;
	readonly #canvas = document.createElement("canvas");

	constructor(options: Record<string, unknown>) {
		if (FakeMap.failNext) {
			FakeMap.failNext = false;
			throw new Error("Failed to initialize WebGL");
		}
		this.options = options;
		const container = options["container"];
		this.#container = container instanceof HTMLElement ? container : document.createElement("div");
		FakeMap.made.push(this);
	}

	#record(name: string, args: readonly unknown[]): this {
		this.calls.push({ name, args });
		return this;
	}

	readonly #listeners = new Map<string, Listener[]>();

	/** Answers as MapLibre does: a subscription whose `unsubscribe` takes the listener back off. */
	on(event: string, ...rest: unknown[]): { unsubscribe(): void } {
		const listener = rest.at(-1);
		// A listener for one layer of the style, such as a click on the clusters layer, takes the
		// layer's id before it; the fake keeps it under the event alone.
		if (typeof listener === "function") this.#listeners.set(event, [...(this.#listeners.get(event) ?? []), listener as Listener]);
		return { unsubscribe: () => this.off(event, listener) };
	}

	/** Calls every listener of an event, as MapLibre would when it happens. */
	fire(event: string, payload: unknown = {}): void {
		for (const listener of this.#listeners.get(event) ?? []) listener(payload);
	}
	off(event: string, ...rest: unknown[]): this {
		const listener = rest.at(-1);
		this.#listeners.set(event, (this.#listeners.get(event) ?? []).filter((l) => l !== listener));
		return this;
	}
	/**
	 * The page waits once for 'load' alone (map/maplibre.ts), so that event fires at once, after the
	 * current step; the fake never loads anything else to wait for.
	 */
	once(event: string, ...rest: unknown[]): this {
		const listener = rest.at(-1);
		if (event === "load" && typeof listener === "function") queueMicrotask(() => (listener as Listener)());
		return this;
	}
	setMissingStyleImageResolver(): this {
		return this;
	}
	setStyle(...args: unknown[]): this {
		return this.#record("setStyle", args);
	}
	isStyleLoaded(): boolean {
		return true;
	}
	fitBounds(...args: unknown[]): this {
		return this.#record("fitBounds", args);
	}
	easeTo(...args: unknown[]): this {
		return this.#record("easeTo", args);
	}
	jumpTo(...args: unknown[]): this {
		return this.#record("jumpTo", args);
	}
	setPadding(): this {
		return this;
	}
	getSource(): undefined {
		return undefined;
	}
	getLayer(): undefined {
		return undefined;
	}
	querySourceFeatures(): [] {
		return [];
	}
	queryRenderedFeatures(): [] {
		return [];
	}
	/** Macau's home view, level 15 as MapLibre's zoom 14, the view the page opens on. */
	getCenter(): { lng: number; lat: number } {
		return { lng: 113.55, lat: 22.19 };
	}
	getZoom(): number {
		return 14;
	}
	getContainer(): HTMLElement {
		return this.#container;
	}
	getCanvas(): HTMLCanvasElement {
		return this.#canvas;
	}
	project(): { x: number; y: number } {
		return { x: 0, y: 0 };
	}
	/**
	 * A removed map forgets what it was asked, as the real one drops its state, so a test reads only
	 * the calls of the map still on the page.
	 */
	remove(): void {
		this.calls.length = 0;
	}
}

/**
 * The module the page imports as "maplibre-gl", faked. `setWorkerUrl` does nothing, since the fake
 * starts no worker, the background script that cuts ***tiles***.
 */
export const fakeMapLibre = { Map: FakeMap, setWorkerUrl: (): void => undefined };
