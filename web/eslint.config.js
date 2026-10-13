// eslint.config.js: the static checks beside the compiler (ADR 0004, the functional style).
//
// typescript-eslint reads types through the TypeScript compiler itself, 6.0.3, the version the
// scripts use too (ADR 0002).

import js from "@eslint/js";
import reactHooks from "eslint-plugin-react-hooks";
import globals from "globals";
import tseslint from "typescript-eslint";

export default tseslint.config(
	{ ignores: ["dist", "node_modules", "test-results", "playwright-report"] },
	js.configs.recommended,
	...tseslint.configs.recommendedTypeChecked,
	{
		languageOptions: {
			globals: { ...globals.browser },
			parserOptions: { project: ["./tsconfig.json", "./tsconfig.node.json"], tsconfigRootDir: import.meta.dirname },
		},
		plugins: { "react-hooks": reactHooks },
		rules: {
			...reactHooks.configs.recommended.rules,
			// ADR 0004, immutability: values never change once made.
			"prefer-const": "error",
			// A canvas context is drawn on by setting its properties, so it alone may change (design/canvas.ts).
			"no-param-reassign": ["error", { props: true, ignorePropertyModificationsFor: ["context"] }],
			// ADR 0002, rule 4: TypeScript's own syntax that compiles to code stays out.
			"no-restricted-syntax": [
				"error",
				{ selector: "TSEnumDeclaration", message: "Use a union of strings or an `as const` object (ADR 0002, rule 4)." },
				{ selector: "TSModuleDeclaration[kind='namespace']", message: "Use modules, not namespaces (ADR 0002, rule 4)." },
			],
			"@typescript-eslint/consistent-type-imports": ["error", { fixStyle: "inline-type-imports" }],
			"@typescript-eslint/no-unused-vars": ["error", { argsIgnorePattern: "^_", varsIgnorePattern: "^_", destructuredArrayIgnorePattern: "^_" }],
			"@typescript-eslint/switch-exhaustiveness-check": "error",
		},
	},
	{
		files: ["**/*.test.ts", "**/*.test.tsx", "e2e/**", "*.config.ts", "eslint.config.js"],
		languageOptions: { globals: { ...globals.node } },
		// Fake ports are async functions that resolve at once, as the real ones would after a fetch.
		rules: { "@typescript-eslint/require-await": "off" },
	},
);
