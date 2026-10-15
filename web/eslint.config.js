// eslint.config.js: the settings of ESLint, the tool that reads the code for mistakes and broken
// rules without running it, beside the compiler's own checks (ADR 0004, the functional style).
// `npm test` runs it over every file of the map.
//
// typescript-eslint reads types through the TypeScript compiler itself, 6.0.3, the version the
// scripts use too (ADR 0002).

import js from "@eslint/js";
import reactHooks from "eslint-plugin-react-hooks";
import globals from "globals";
import tseslint from "typescript-eslint";

export default tseslint.config(
	// Built files, installed packages and test reports are made by tools, not written by hand.
	{ ignores: ["dist", "node_modules", "test-results", "playwright-report"] },
	js.configs.recommended,
	...tseslint.configs.recommendedTypeChecked,
	{
		languageOptions: {
			// The page runs in a browser, so names such as `window` and `document` exist.
			globals: { ...globals.browser },
			parserOptions: { project: ["./tsconfig.json", "./tsconfig.node.json"], tsconfigRootDir: import.meta.dirname },
		},
		plugins: { "react-hooks": reactHooks },
		rules: {
			// React's rules of hooks: a hook, a function such as `useMemo` that keeps a value between
			// draws, must run in the same order on every draw, or React mixes up their values.
			...reactHooks.configs.recommended.rules,
			// ADR 0004, immutability: values never change once made.
			"prefer-const": "error",
			// A canvas, a bitmap the page paints on, is drawn on by setting its context's properties, so
			// it alone may change (design/canvas.ts).
			"no-param-reassign": ["error", { props: true, ignorePropertyModificationsFor: ["context"] }],
			// ADR 0002, rule 4: TypeScript's own syntax that compiles to code stays out.
			"no-restricted-syntax": [
				"error",
				{ selector: "TSEnumDeclaration", message: "Use a union of strings or an `as const` object (ADR 0002, rule 4)." },
				{ selector: "TSModuleDeclaration[kind='namespace']", message: "Use modules, not namespaces (ADR 0002, rule 4)." },
			],
			// An import used only as a type says so with `type`, so the build drops it and a reader
			// sees at once that it brings no code.
			"@typescript-eslint/consistent-type-imports": ["error", { fixStyle: "inline-type-imports" }],
			// A name that starts with '_' is left unused on purpose, such as an argument a callback must
			// accept; every other unused name is a mistake.
			"@typescript-eslint/no-unused-vars": ["error", { argsIgnorePattern: "^_", varsIgnorePattern: "^_", destructuredArrayIgnorePattern: "^_" }],
			// A switch over a union, a type that is one of a fixed set of shapes, must name every
			// case, as F#'s exhaustive `match` does (ADR 0004), so a case no switch handles fails.
			"@typescript-eslint/switch-exhaustiveness-check": "error",
		},
	},
	{
		files: ["**/*.test.ts", "**/*.test.tsx", "e2e/**", "*.config.ts", "eslint.config.js"],
		// Tests and settings files also run in Node, the JavaScript runtime outside the browser, so
		// names such as `process` exist there.
		languageOptions: { globals: { ...globals.node } },
		// Fake ports, the functions a test gives the update in place of the map, are async functions
		// that resolve at once, as the real ones would after a request to the service.
		rules: { "@typescript-eslint/require-await": "off" },
	},
);
