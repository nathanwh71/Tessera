// Builds Tessera: the map first, then the service with the map inside it (US-00 c1, US-23 c4).
//
//   npm run build
//
// The map's own tools live under web/; `npm ci` there installs them once, and `npm run build`
// type-checks the map and writes it to web/dist. The service's project copies web/dist beside the
// program as wwwroot/, so one `dotnet build` ships both.
import { resolve } from 'node:path';
import { $ } from 'zx';
import { emptyReleaseMap } from './lib/build.ts';

await $({ cwd: 'web' })`npm ci --no-audit --no-fund`;
await $({ cwd: 'web' })`npm run build`;
// The copy into wwwroot/ never deletes a file, so the old map goes first, or its files would ship
// beside the new ones (US-23 c4).
await emptyReleaseMap(resolve(import.meta.dirname, '..'));
await $`dotnet build --configuration Release`;
