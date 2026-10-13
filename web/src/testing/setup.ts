// testing/setup.ts: runs before every test file. Component tests render into jsdom; this unmounts them after each test.

import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";

afterEach(() => {
	if (typeof document !== "undefined") cleanup();
});
