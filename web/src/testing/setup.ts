// testing/setup.ts: runs before every test file, as vite.config.ts asks. Component tests draw into
// jsdom, the pretend browser the tests run in; this removes what they drew after each test, so no
// test sees another's page.

import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";

afterEach(() => {
	if (typeof document !== "undefined") cleanup();
});
