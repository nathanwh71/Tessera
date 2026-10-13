/// <reference types="vitest/config" />
// vite.config.ts: the build, the dev server and the unit tests.

import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

// The map asks for tiles on its own origin (US-23, criterion 7). Under `vite` alone, the dev server
// passes each layer's tiles on to a running service named by TESSERA_BASE_URL.
const service = process.env["TESSERA_BASE_URL"];
const groups = ["food", "outdoors", "culture", "transport"];

export default defineConfig({
	// The service serves the page at / and at /{layer}.html, and its assets from /assets/ (US-23).
	base: "/",
	plugins: [react(), tailwindcss()],
	build: {
		outDir: "dist",
		assetsDir: "assets",
		// Fonts and icons stay files under /assets/, never inlined, so the office needs no CDN.
		assetsInlineLimit: 0,
		chunkSizeWarningLimit: 1500,
		rollupOptions: {
			output: {
				// MapLibre's worker ships as .mjs; many servers send that ending with no JavaScript type, which a module worker refuses.
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
