// Runs every test (US-00 c3): the scripts', the map's, the service's three suites, and the
// browser tests.
//
//   npm test
//
// The integration tests start the packaged database in Docker, so Docker must be running.
import { $ } from 'zx';

await $`npx tsc --noEmit`;
await $`node --test scripts/**/*.test.ts`;
await $({ cwd: 'web' })`npm ci --no-audit --no-fund`;
await $({ cwd: 'web' })`npm test`;
await $`node scripts/build.ts`;
for (const suite of ['UnitTests', 'ArchitectureTests', 'IntegrationTests']) {
  await $`dotnet run --no-build --configuration Release --project tests/Tessera.${suite}`;
}
// Last, the browser tests: the slowest, against the real service and the places.
await $`node scripts/e2e.ts`;
