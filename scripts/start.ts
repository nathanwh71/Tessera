// Starts Tessera on one machine: the database in Docker, the places loaded once, then the service.
//
//   npm start
//
// Settings come from the environment, or from a file named .env beside package.json:
//   TESSERA_DB_PASSWORD          the database owner's password, for this script alone
//   TESSERA_READER_PASSWORD      tessera_reader's password, which the service signs in with
//   TESSERA_PUBLISHER_PASSWORD   tessera_publisher's password, which the import signs in with
//   TESSERA_HTTP_PORT            the port the service answers on: 80 in the office (US-35)
//   TESSERA_DB_PORT              optional; the database's port on this machine, 5432 by default
//
// Each step's decision comes from a pure function in lib/plan.ts; this file only runs commands.
import { resolve } from 'node:path';
import { $, sleep } from 'zx';
import { countQuery, databaseUrl, decideImport, parseCounts, readStartSettings, runArguments } from './lib/plan.ts';

const container = 'tessera-db';
const volume = 'tessera-data';

const checked = readStartSettings(process.env);
if (!checked.ok) {
  checked.problems.forEach(problem => console.error(problem));
  process.exit(1);
}
const settings = checked.value;

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

// On a first start the image runs its set-up scripts on a private server, which answers on no
// network port; so the database is ready only once it answers on 127.0.0.1.
for (let attempt = 1; ; attempt++) {
  const ready = await $({ nothrow: true })`docker exec ${container} pg_isready --host 127.0.0.1 --username postgres --dbname tessera`;
  if (ready.exitCode === 0) break;
  if (attempt === 60) {
    console.error('The database did not start within a minute; read "docker logs tessera-db".');
    process.exit(1);
  }
  await sleep(1000);
}

const counts = parseCounts((await $`docker exec ${container} psql --username postgres --dbname tessera --tuples-only --no-align --command ${countQuery()}`).stdout);
const decision = decideImport(counts);
if (decision.kind === 'refuse') {
  console.error(decision.reason);
  process.exit(1);
}
if (decision.kind === 'import') {
  console.log('The nine tables are empty: importing the places.');
  // dotnet run starts the program in the project's own folder, so the seed's path must be whole.
  await $({ env: { ...process.env, TESSERA_PUBLISHER_DATABASE_URL: databaseUrl('tessera_publisher', settings.publisherPassword, settings.databasePort) } })`dotnet run --project src/Tessera.Service --configuration Release -- import ${resolve('data/macau-pois.geojson')}`;
  console.log('Imported. The service shows the places once the publisher grants them: npm run publish.');
}

console.log(`Starting the service on port ${settings.httpPort}.`);
await $({ env: { ...process.env, DATABASE_URL: databaseUrl('tessera_reader', settings.readerPassword, settings.databasePort) }, stdio: 'inherit' })`dotnet run --project src/Tessera.Service --configuration Release`;
