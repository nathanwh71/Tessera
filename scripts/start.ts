// Starts Tessera on one machine: the database in Docker, any new seed scripts, two copies of the
// service, and Nginx in front of them (ADR 0008, ADR 0009, N-29). It keeps both copies running:
// one that stops is started again, while Nginx sends its requests to the other.
//
//   npm start
//
// Settings come from the environment, or from a file named .env beside package.json:
//   TESSERA_DB_PASSWORD          the database owner's password, for this script alone
//   TESSERA_READER_PASSWORD      tessera_reader's password, which the service signs in with
//   TESSERA_PUBLISHER_PASSWORD   tessera_publisher's password, which the seed signs in with
//   TESSERA_HTTPPORT             the port Nginx answers the office on: 80 in the office (US-35)
//   TESSERA_OFFICE_NETWORK       the office's network, such as 192.168.10.0/24; Nginx answers it alone (S-07)
//   TESSERA_SERVICE_PORT         optional; the first copy's port, 7800 by default; the second copy
//                                takes the next port, 7801
//   TESSERA_DB_HOST              optional; where the service finds the database, 127.0.0.1 by default
//   TESSERA_DB_PORT              optional; the database's port, 5432 by default
//   TESSERA_DB_NAME              optional; the database's name, tessera by default
//
// Each step's decision comes from a pure function in lib/plan.ts; this file only runs commands.
import { resolve } from 'node:path';
import { setTimeout as delay } from 'node:timers/promises';
import { $, type ProcessPromise } from 'zx';
import { waitForDatabase, waitForHealth } from './lib/database.ts';
import { databaseUrl, nginxArguments, nginxImage, readStartSettings, runArguments, seedReport } from './lib/plan.ts';
import { keepRunning } from './lib/supervise.ts';

// Fixed names, so each start finds the containers and the data an earlier start left, and
// npm run publish finds the database by the same name.
const container = 'tessera-db';
const volume = 'tessera-data';
const nginx = 'tessera-nginx';
// The program the build writes; both copies and the seed run it, so it builds once, not three times.
const program = resolve('src/Tessera/bin/Release/net10.0/Tessera.dll');

const read = readStartSettings(process.env);
if (!read.ok) {
  read.error.forEach(problem => console.error(problem));
  process.exit(1);
}
const settings = read.value;

// The map is built first, so the release carries it (US-23 c4); the service then builds with it inside.
await $`node scripts/build.ts`;

// The database image is db/Dockerfile: the same image the tests start (US-01).
await $`docker build --quiet --tag ${container} db`;

// The database container is kept, not made afresh: a running one is left alone, and a stopped one
// starts again with its data and passwords. docker run would refuse a second container of the
// same name, so it runs only on the first start.
const running = (await $`docker ps --quiet --filter name=^${container}$`).stdout.trim() !== '';
if (!running) {
  const exists = (await $`docker ps --all --quiet --filter name=^${container}$`).stdout.trim() !== '';
  if (exists) {
    await $`docker start ${container}`;
  } else {
    await $`docker run ${runArguments(settings, container, volume)}`;
  }
}

const notReady = await waitForDatabase(container, settings.database.name);
if (notReady !== undefined) {
  console.error(notReady);
  process.exit(1);
}

// DbUp runs each seed script it has not run before, and none twice, so this runs at every start.
// zx keeps what the seed prints to itself, so its report, the scripts it ran, is printed here.
const seeded = await $({ env: { ...process.env, TESSERA_PUBLISHER_DATABASE_URL: databaseUrl('tessera_publisher', settings.publisherPassword, settings.database) } })`dotnet ${program} seed`;
seedReport(seeded.stdout).forEach(line => console.log(line));

// N-29: each copy runs until told to stop; lib/supervise.ts starts one again when it stops.
let stopping = false;
// Ctrl+C aborts this, which cuts short any wait before a restart, so the script ends at once
// rather than up to 30 seconds later, when the wait would have run out.
const stopped = new AbortController();
// Waits before a restart, or less if Ctrl+C comes first; an aborted wait ends early.
const wait = (milliseconds: number): Promise<void> => delay(milliseconds, undefined, { signal: stopped.signal }).catch(() => undefined);
const copiesNow = new Map<string, ProcessPromise>();
const reader = databaseUrl('tessera_reader', settings.readerPassword, settings.database);
const runCopy = (port: string) => keepRunning(`port ${port}`, {
  start: async () => {
    const copy = $({ nothrow: true, stdio: 'inherit', env: { ...process.env, DATABASE_URL: reader, Urls: `http://127.0.0.1:${port}` } })`dotnet ${program}`;
    copiesNow.set(port, copy);
    const ended = await copy;
    return ended.exitCode === null ? `signal ${ended.signal}` : `code ${ended.exitCode}`;
  },
  stopping: () => stopping,
  wait,
  now: Date.now,
  say: line => console.error(line),
});

// Ctrl+C stops both copies and then this script; Nginx and the database keep running in Docker.
process.on('SIGINT', () => {
  stopping = true;
  stopped.abort();
  for (const copy of copiesNow.values()) stopCopy(copy);
});

/**
 * Stops one copy, whatever state it is in. A copy waiting out its restart delay has already ended,
 * and zx then throws at once rather than return a promise, an object that stands for a result still
 * to come, so the try catches that; a copy that ends while the signal is on its way makes the
 * kill fail later, and .catch drops that. Either way the copy is stopped, and an error here would
 * end the script before it stopped the other copy.
 */
function stopCopy(copy: ProcessPromise): void {
  try {
    void copy.kill().catch(() => undefined);
  } catch {
    // The copy had ended already: nothing to stop.
  }
}

console.log(`Starting two copies of the service, on ports ${settings.servicePorts.join(' and ')}, behind Nginx on port ${settings.httpPort}.`);
const copies = settings.servicePorts.map(runCopy);

// A container left from an earlier start may hold old ports, so Nginx starts afresh each time.
await $({ nothrow: true, quiet: true })`docker rm --force ${nginx}`;
await $`docker build --quiet --tag ${nginxImage} nginx`;
await $`docker run ${nginxArguments(nginx, settings.httpPort, settings.servicePorts, settings.officeNetwork, resolve('nginx/default.conf.template'))}`;

// The service answers at once, but serves tiles only once its list of layers loads, so the script
// says 'ready' only when /health, asked through Nginx, says UP (N-25).
const health = await waitForHealth(`http://127.0.0.1:${settings.httpPort}/health`, 60);
console.log(health === 'up'
  ? `Tessera is ready: http://127.0.0.1:${settings.httpPort}/`
  : 'The service has not said UP within a minute; it keeps trying. Read its log above.');
// The script stays alive while the copies run, so Ctrl+C here stops them both.
await Promise.all(copies);
