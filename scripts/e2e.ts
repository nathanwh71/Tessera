// Runs the browser tests (US-23, UI-09 to UI-17): Playwright drives a real browser against the
// real service, which holds the office's places.
//
//   npm run e2e
//   npm run e2e -- --grep-invert "approved image"   (every test but the screenshot comparisons)
//
// It needs nothing set beforehand. It starts a database of its own in Docker, on a free port and
// with passwords made for this run alone; loads and publishes the places; starts the built service;
// runs Playwright; then stops the service and removes the database, whether the tests pass or fail.
// So it runs the same on a laptop and in the automatic build, and never touches `npm start`'s
// database. Build first, with `npm run build`.
import { randomBytes } from 'node:crypto';
import { createServer } from 'node:net';
import { resolve } from 'node:path';
import { $, type ProcessPromise } from 'zx';
import { waitForDatabase, waitForHealth } from './lib/database.ts';

const container = 'tessera-e2e';
const name = 'tessera';
// A password made for this run: the database lives only as long as the tests.
const secret = () => randomBytes(12).toString('hex');
const owner = secret();
const reader = secret();
const publisher = secret();

/** A port no program holds now: the operating system picks it when asked for port 0. */
function freePort(): Promise<number> {
  return new Promise((done, fail) => {
    const probe = createServer();
    probe.once('error', fail);
    probe.listen(0, '127.0.0.1', () => {
      const address = probe.address();
      probe.close(() => (typeof address === 'object' && address !== null ? done(address.port) : fail(new Error('no port'))));
    });
  });
}

const databasePort = await freePort();
const servicePort = await freePort();
const url = (user: string, password: string) => `postgresql://${user}:${password}@127.0.0.1:${databasePort}/${name}`;
const service = resolve('src/Tessera.Service/bin/Release/net10.0/Tessera.Service.dll');

await $`docker build --quiet --tag ${container} db`;
// A database left behind by a run that was killed would hold the name; remove it first.
await $({ nothrow: true, quiet: true })`docker rm --force ${container}`;
await $`docker run --detach --name ${container} --publish 127.0.0.1:${databasePort}:5432 --env POSTGRES_DB=${name} --env POSTGRES_PASSWORD=${owner} --env TESSERA_READER_PASSWORD=${reader} --env TESSERA_PUBLISHER_PASSWORD=${publisher} ${container}`;
let running: ProcessPromise | undefined;
try {
  const notReady = await waitForDatabase(container, name);
  if (notReady !== undefined) throw new Error(notReady);

  // The import and the grant, as tessera_publisher (US-24): the service's role only ever reads.
  await $({ env: { ...process.env, TESSERA_PUBLISHER_DATABASE_URL: url('tessera_publisher', publisher) } })`dotnet ${service} import ${resolve('data/macau-pois.geojson')}`;
  const grant = 'GRANT USAGE ON SCHEMA food, outdoors, culture, transport TO tessera_reader; GRANT SELECT ON ALL TABLES IN SCHEMA food, outdoors, culture, transport TO tessera_reader;';
  await $({ env: { ...process.env, PGPASSWORD: publisher } })`docker exec --env PGPASSWORD ${container} psql --quiet --host 127.0.0.1 --username tessera_publisher --dbname ${name} --command ${grant}`;

  // The service runs in the background; `nothrow` keeps its stop, below, from counting as a failure.
  running = $({
    nothrow: true,
    quiet: true,
    env: { ...process.env, DATABASE_URL: url('tessera_reader', reader), TESSERA_HTTPPORT: String(servicePort), TESSERA_CONFIG: resolve('tessera.json') },
  })`dotnet ${service}`;
  const base = `http://127.0.0.1:${servicePort}`;
  if ((await waitForHealth(`${base}/health`, 60)) !== 'up') throw new Error('The service did not say UP within a minute.');

  // `inherit` shows Playwright's own report, test by test, here and in the automatic build's log.
  // Arguments after `--` pass on to Playwright, such as `--grep-invert` to leave some tests out.
  await $({ cwd: 'web', stdio: 'inherit', env: { ...process.env, TESSERA_BASE_URL: base } })`npx playwright test ${process.argv.slice(2)}`;
} finally {
  await running?.kill();
  await $({ nothrow: true, quiet: true })`docker rm --force ${container}`;
}
