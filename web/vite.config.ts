/// <reference types="vitest/config" />
// vite.config.ts: the settings of Vite, the tool that builds the page's files, serves them while a
// developer works, and runs the unit tests through vitest, the test runner. `npm run build`,
// `npm run dev` and `npm test` all read this file.

import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

// The map asks for tiles, the square pieces of the map, on its own origin, the scheme, host and port
// the page came from (US-23, criterion 7). Under `vite` alone, the development server passes each
// layer's tile requests on to a running service named by TESSERA_BASE_URL.
const service = process.env["TESSERA_BASE_URL"];
const groups = ["food", "outdoors", "culture", "transport"];

export default defineConfig({
	// The service serves the page at / and at /{layer}.html, and its assets from /assets/ (US-23).
	base: "/",
	plugins: [react(), tailwindcss()],
	build: {
		outDir: "dist",
		assetsDir: "assets",
		// Fonts and icons stay files under /assets/, never written into the page's code, so the office
		// needs no CDN, a content delivery network that serves such files from the internet.
		assetsInlineLimit: 0,
		// MapLibre's own file is about 1 MB, which no setting shrinks; src/build.test.ts holds the page's
		// own code under 250 kB instead, so this warns only if MapLibre itself grows.
		chunkSizeWarningLimit: 1100,
		rollupOptions: {
			output: {
				// MapLibre and React change only with their versions, so each goes in a file of its own; a
				// deploy that changes Tessera's own code then changes one small file, and a returning
				// visitor keeps the other two from the cache (src/build.test.ts).
				codeSplitting: {
					groups: [
						{ name: "maplibre", test: /node_modules[\\/]maplibre-gl[\\/]/ },
						{ name: "react", test: /node_modules[\\/](react|react-dom|scheduler)[\\/]/ },
					],
				},
				// The worker of MapLibre, the library that draws the map, ships as .mjs; many servers send
				// that ending with no JavaScript type, which a module worker refuses.
				assetFileNames: (asset) => (asset.names.some((n) => n.endsWith(".mjs")) ? "assets/[name]-[hash].js" : "assets/[name]-[hash][extname]"),
			},
		},
	},
	server: service === undefined ? {} : { proxy: Object.fromEntries(groups.map((g) => [`^/${g}\\.[a-z]+/`, service])) },
	test: {
		include: ["src/**/*.test.{ts,tsx}"],
		environment: "node",
		setupFiles: ["src/testing/setup.ts"],
	},
});
