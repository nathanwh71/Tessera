// Runs the browser tests (US-23, UI-09 to UI-17): Playwright drives a real browser against Nginx
// and the real service behind it, which holds the office's places. Then it checks what Nginx adds
// (lib/edge.ts).
//
//   npm run e2e
//   npm run e2e -- --grep-invert "approved image"   (every test but the screenshot comparisons)
//
// It needs nothing set beforehand. It starts a database of its own in Docker, on a free port and
// with passwords made for this run alone; loads and publishes the places; starts two copies of the
// built service and Nginx in front of them (N-29); runs Playwright; then stops both copies and
// removes both containers, whether the tests pass or fail.
// So it runs the same on a laptop and in the automatic build, and never touches `npm start`'s
// database. Build first, with `npm run build`.
import { randomBytes } from 'node:crypto';
import { createServer } from 'node:net';
import { resolve } from 'node:path';
import { $, type ProcessPromise } from 'zx';
import { waitForDatabase, waitForHealth } from './lib/database.ts';
import { checkEdge } from './lib/edge.ts';
import { databaseUrl, nginxArguments } from './lib/plan.ts';

/**
 * The containers' names differ from npm start's tessera-db and tessera-nginx, so a test run never
 * stops or removes the office's own; the database inside keeps npm start's name, tessera.
 */
const container = 'tessera-e2e';
const nginx = 'tessera-e2e-nginx';
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
const servicePorts: [string, string] = [String(await freePort()), String(await freePort())];
const nginxPort = await freePort();
// S-07: the office, here, is 127.0.0.4 to 127.0.0.7. Linux answers every 127.x.x.x address on
// this machine, so the checks can ask from inside the office and from outside it.
const officeNetwork = '127.0.0.4/30';
const url = (user: string, password: string) => databaseUrl(user, password, { host: '127.0.0.1', port: String(databasePort), name });
/** The program npm run build writes, in Release, so the tests meet the build the office runs. */
const service = resolve('src/Tessera/bin/Release/net10.0/Tessera.dll');

await $`docker build --quiet --tag ${container} db`;
// A database left behind by a run that was killed would hold the name; remove it first.
await $({ nothrow: true, quiet: true })`docker rm --force ${container} ${nginx}`;
await $`docker run --detach --name ${container} --publish 127.0.0.1:${databasePort}:5432 --env POSTGRES_DB=${name} --env POSTGRES_PASSWORD=${owner} --env TESSERA_READER_PASSWORD=${reader} --env TESSERA_PUBLISHER_PASSWORD=${publisher} ${container}`;
let copies: ProcessPromise[] = [];
try {
  const notReady = await waitForDatabase(container, name);
  if (notReady !== undefined) throw new Error(notReady);

  // The seed and the grant, as tessera_publisher (US-24, ADR 0009): the service's role only ever reads.
  await $({ env: { ...process.env, TESSERA_PUBLISHER_DATABASE_URL: url('tessera_publisher', publisher) } })`dotnet ${service} seed`;
  // A layer that takes two seconds to build any tile, for the checks of a crowd and of the cap on
  // requests at once (N-24, S-30). The pause runs once for each query, not once for each place.
  // The grant repeats scripts/publish.ts's, since that script names npm start's container,
  // tessera-db, and this run's database lives in tessera-e2e. A schema added to that grant must
  // join this one too.
  const slow = 'CREATE VIEW food.slow_cafes AS SELECT c.fid, c.geom, c.name FROM (SELECT pg_sleep(2)) AS pause CROSS JOIN food.cafes AS c;';
  const grant = `${slow} GRANT USAGE ON SCHEMA food, outdoors, culture, transport TO tessera_reader; GRANT SELECT ON ALL TABLES IN SCHEMA food, outdoors, culture, transport TO tessera_reader;`;
  await $({ env: { ...process.env, PGPASSWORD: publisher } })`docker exec --env PGPASSWORD ${container} psql --quiet --host 127.0.0.1 --username tessera_publisher --dbname ${name} --command ${grant}`;

  // The two copies run in the background; `nothrow` keeps their stops, below, from counting as failures.
  copies = servicePorts.map(port => $({
    nothrow: true,
    quiet: true,
    env: { ...process.env, DATABASE_URL: url('tessera_reader', reader), Urls: `http://127.0.0.1:${port}` },
  })`dotnet ${service}`);
  await $`docker run ${nginxArguments(nginx, String(nginxPort), servicePorts, officeNetwork, resolve('nginx/default.conf.template'))}`;
  const base = `http://127.0.0.1:${nginxPort}`;
  if ((await waitForHealth(`${base}/health`, 60)) !== 'up') throw new Error('The service did not say UP, through Nginx, within a minute.');

  // `inherit` shows Playwright's own report, test by test, here and in the automatic build's log.
  // Arguments after `--` pass on to Playwright, such as `--grep-invert` to leave some tests out.
  await $({ cwd: 'web', stdio: 'inherit', env: { ...process.env, TESSERA_BASE_URL: base } })`npx playwright test ${process.argv.slice(2)}`;
  // The checks stop each copy part-way; they wait for it to exit, so no answer comes from a copy
  // half-way through stopping.
  const stop = (copy: ProcessPromise | undefined) => async () => {
    await copy?.kill();
    await copy;
  };
  await checkEdge(base, stop(copies[0]), stop(copies[1]));
} finally {
  // The Nginx checks may have stopped the copies already; zx then refuses a second kill.
  for (const copy of copies) {
    try {
      await copy.kill();
    } catch {
      // Already stopped.
    }
  }
  await $({ nothrow: true, quiet: true })`docker rm --force ${container} ${nginx}`;
}
