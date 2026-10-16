// Runs every test (US-00 c3): the scripts', the map's, the service's borders and its two suites, the
// pipeline's, and the browser tests.
//
//   npm test
//
// The integration tests start the packaged database in Docker, so Docker must be running.
import { $ } from 'zx';

// The quick checks first, so a type error or a broken script fails in seconds: tsc checks the
// scripts' types, since Node only strips them (ADR 0002), and node --test runs their pure parts.
await $`npx tsc --noEmit`;
await $`node --test scripts/**/*.test.ts`;
// The map's own tools and tests live under web/; --no-audit and --no-fund drop two reports that
// change nothing.
await $({ cwd: 'web' })`npm ci --no-audit --no-fund`;
await $({ cwd: 'web' })`npm test`;
await $`node scripts/build.ts`;
// build.ts has just built everything in Release, so --no-build runs that build. Each test project
// is a program of its own (global.json picks Microsoft.Testing.Platform), so dotnet run runs it.
// The architecture tests go first: a broken border between layers is a fault of design, and they
// find it in seconds (ADR 0010).
for (const suite of ['ArchitectureTests', 'UnitTests', 'IntegrationTests']) {
  await $`dotnet run --no-build --configuration Release --project tests/Tessera.${suite}`;
}
// Last, the browser tests: the slowest, against the real service and the places.
await $`node scripts/e2e.ts`;
