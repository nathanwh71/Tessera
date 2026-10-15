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
import { $, sleep, type ProcessPromise } from 'zx';
import { waitForDatabase, waitForHealth } from './lib/database.ts';
import { databaseUrl, nginxArguments, readStartSettings, restartDelay, runArguments } from './lib/plan.ts';

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
await $({ env: { ...process.env, TESSERA_PUBLISHER_DATABASE_URL: databaseUrl('tessera_publisher', settings.publisherPassword, settings.database) } })`dotnet ${program} seed`;

// N-29: each copy runs until told to stop. One that stops on its own starts again after
// restartDelay; a copy that ran for a minute counts as healthy, so its next failure waits a second.
let stopping = false;
const copiesNow = new Map<string, ProcessPromise>();
const reader = databaseUrl('tessera_reader', settings.readerPassword, settings.database);
async function keepRunning(port: string): Promise<void> {
  let quickFailures = 0;
  while (!stopping) {
    const began = Date.now();
    const copy = $({ nothrow: true, stdio: 'inherit', env: { ...process.env, DATABASE_URL: reader, Urls: `http://127.0.0.1:${port}` } })`dotnet ${program}`;
    copiesNow.set(port, copy);
    const ended = await copy;
    if (stopping) return;
    quickFailures = Date.now() - began > 60_000 ? 1 : quickFailures + 1;
    const wait = restartDelay(quickFailures);
    console.error(`The copy on port ${port} stopped (${ended.exitCode ?? ended.signal}); starting it again in ${wait / 1000} s.`);
    await sleep(wait);
  }
}

// Ctrl+C stops both copies and then this script; Nginx and the database keep running in Docker.
process.on('SIGINT', () => {
  stopping = true;
  for (const copy of copiesNow.values()) void copy.kill().catch(() => undefined);
});

console.log(`Starting two copies of the service, on ports ${settings.servicePorts.join(' and ')}, behind Nginx on port ${settings.httpPort}.`);
const copies = settings.servicePorts.map(keepRunning);

// A container left from an earlier start may hold old ports, so Nginx starts afresh each time.
await $({ nothrow: true, quiet: true })`docker rm --force ${nginx}`;
await $`docker run ${nginxArguments(nginx, settings.httpPort, settings.servicePorts, settings.officeNetwork, resolve('nginx/default.conf.template'))}`;

// The service answers at once, but serves tiles only once its list of layers loads, so the script
// says 'ready' only when /health, asked through Nginx, says UP (N-25).
const health = await waitForHealth(`http://127.0.0.1:${settings.httpPort}/health`, 60);
console.log(health === 'up'
  ? `Tessera is ready: http://127.0.0.1:${settings.httpPort}/`
  : 'The service has not said UP within a minute; it keeps trying. Read its log above.');
await Promise.all(copies);
