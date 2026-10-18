// Runs every test (US-00 c3): the scripts', the map's, the service's borders and its two suites, the
// pipeline's, and the browser tests.
//
//   npm test
//
// The integration tests start the packaged database in Docker, so Docker must be running.
import { rmSync } from 'node:fs';
import { resolve } from 'node:path';
import { $ } from 'zx';
import { coverageArguments, coverageFolder, suites } from './lib/coverage.ts';

// The quick checks first, so a type error or a broken script fails in seconds: tsc checks the
// scripts' types, since Node only strips them (ADR 0002), and node --test runs their pure parts.
await $`npx tsc --noEmit`;
await $`node --test scripts/**/*.test.ts`;
// The pipeline's checks, in etl/: its types (pyright, in strict mode), its style (ruff), the rule
// that its pure code reads no file and runs no program (import-linter), then its tests (pytest).
// uv installs Python and the pipeline's packages first, when they have changed.
for (const check of [['pyright'], ['ruff', 'check', '.'], ['lint-imports'], ['pytest', '-q']]) {
  await $({ cwd: 'etl' })`uv run ${check}`;
}
// The map's own tools and tests live under web/; --no-audit and --no-fund drop two reports that
// change nothing.
await $({ cwd: 'web' })`npm ci --no-audit --no-fund`;
await $({ cwd: 'web' })`npm test`;
await $`node scripts/build.ts`;
// build.ts, the step before, built everything in Release, so --no-build runs that build. Each test
// project is a program of its own (global.json picks Microsoft.Testing.Platform), so dotnet run
// runs it.
// The architecture tests go first: a broken border between layers is a fault of design, and they
// find it in seconds (ADR 0010).
// Each suite records which of the service's lines it runs, in coverage/; the old records go first,
// so a suite that no longer runs cannot leave one behind. Then coverage.ts merges them and fails
// below the floors in scripts/lib/coverage.ts.
rmSync(coverageFolder, { recursive: true, force: true });
for (const suite of suites) {
  await $`dotnet run --no-build --configuration Release --project tests/Tessera.${suite} -- ${coverageArguments(suite, resolve(coverageFolder))}`;
}
await $`node scripts/coverage.ts`;
// Last, the browser tests: the slowest, against the real service and the places.
await $`node scripts/e2e.ts`;
