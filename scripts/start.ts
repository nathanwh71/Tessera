// Starts Tessera on one machine: the database in Docker, the places loaded once, the service, and
// Nginx in front of it (ADR 0008).
//
//   npm start
//
// Settings come from the environment, or from a file named .env beside package.json:
//   TESSERA_DB_PASSWORD          the database owner's password, for this script alone
//   TESSERA_READER_PASSWORD      tessera_reader's password, which the service signs in with
//   TESSERA_PUBLISHER_PASSWORD   tessera_publisher's password, which the import signs in with
//   TESSERA_HTTPPORT             the port Nginx answers the office on: 80 in the office (US-35)
//   TESSERA_SERVICE_PORT         optional; the port the service answers Nginx on, 7800 by default
//   TESSERA_DB_HOST              optional; where the service finds the database, 127.0.0.1 by default
//   TESSERA_DB_PORT              optional; the database's port, 5432 by default
//   TESSERA_DB_NAME              optional; the database's name, tessera by default
//
// Each step's decision comes from a pure function in lib/plan.ts; this file only runs commands.
import { resolve } from 'node:path';
import { $ } from 'zx';
import { waitForDatabase, waitForHealth } from './lib/database.ts';
import { countQuery, databaseUrl, decideImport, nginxArguments, parseCounts, readStartSettings, runArguments } from './lib/plan.ts';

const container = 'tessera-db';
const volume = 'tessera-data';
const nginx = 'tessera-nginx';

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

const counts = parseCounts((await $`docker exec ${container} psql --username postgres --dbname ${settings.database.name} --tuples-only --no-align --command ${countQuery()}`).stdout);
const decision = decideImport(counts);
if (decision.kind === 'refuse') {
  console.error(decision.reason);
  process.exit(1);
}
if (decision.kind === 'import') {
  console.log('The nine tables are empty: importing the places.');
  // dotnet run starts the program in the project's own folder, so the seed's path must be whole.
  await $({ env: { ...process.env, TESSERA_PUBLISHER_DATABASE_URL: databaseUrl('tessera_publisher', settings.publisherPassword, settings.database) } })`dotnet run --project src/Tessera --configuration Release -- import ${resolve('data/macau-pois.geojson')}`;
  console.log('Imported. The service shows the places once the publisher grants them: npm run publish.');
}

console.log(`Starting the service on port ${settings.servicePort}, behind Nginx on port ${settings.httpPort}.`);
const service = $({ env: { ...process.env, DATABASE_URL: databaseUrl('tessera_reader', settings.readerPassword, settings.database), TESSERA_HTTPPORT: settings.servicePort }, stdio: 'inherit' })`dotnet run --project src/Tessera --configuration Release`;

// A container left from an earlier start may hold old ports, so Nginx starts afresh each time.
await $({ nothrow: true, quiet: true })`docker rm --force ${nginx}`;
await $`docker run ${nginxArguments(nginx, settings.httpPort, settings.servicePort, resolve('nginx/default.conf.template'))}`;

// The service answers at once, but serves tiles only once its list of layers loads, so the script
// says 'ready' only when /health, asked through Nginx, says UP (N-25).
const health = await waitForHealth(`http://127.0.0.1:${settings.httpPort}/health`, 60);
console.log(health === 'up'
  ? `Tessera is ready: http://127.0.0.1:${settings.httpPort}/`
  : 'The service has not said UP within a minute; it keeps trying. Read its log above.');
await service;
