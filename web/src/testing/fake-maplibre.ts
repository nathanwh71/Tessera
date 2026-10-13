// testing/fake-maplibre.ts: a stand-in for MapLibre in jsdom, which has no WebGL. Page tests mock
// "maplibre-gl" with it; it records what the page asks of the map, lets a test fire the map's events,
// and draws nothing.

type Listener = (...args: unknown[]) => void;

/** The fake map: every method the page calls, each doing as little as keeps the page running. */
export class FakeMap {
	static readonly made: FakeMap[] = [];
	readonly options: Record<string, unknown>;
	readonly calls: { readonly name: string; readonly args: readonly unknown[] }[] = [];
	readonly touchZoomRotate = { disableRotation: (): void => undefined };
	readonly keyboard = { disableRotation: (): void => undefined };
	readonly #container: HTMLElement;
	readonly #canvas = document.createElement("canvas");

	constructor(options: Record<string, unknown>) {
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

	on(event: string, ...rest: unknown[]): this {
		const listener = rest.at(-1);
		// A listener for a layer, such as a click on the clusters, takes the layer's id before it; the fake keeps it under the event alone.
		if (typeof listener === "function") this.#listeners.set(event, [...(this.#listeners.get(event) ?? []), listener as Listener]);
		return this;
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
	remove(): void {
		this.calls.length = 0;
	}
}

/** The module the page imports as "maplibre-gl", faked. */
export const fakeMapLibre = { Map: FakeMap, setWorkerUrl: (): void => undefined };
