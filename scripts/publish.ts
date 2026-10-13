// Publishes the places: grants the four schemas and their nine tables to the service's role, as
// tessera_publisher (US-24 c5). The import never grants; publishing is the publisher's own act.
//
//   npm run publish
//
// The service builds its list of layers at start-up, so restart it afterwards to serve them.
import { $ } from 'zx';
import { waitForDatabase } from './lib/database.ts';
import { readDatabasePlace } from './lib/plan.ts';

const container = 'tessera-db';
const password = process.env['TESSERA_PUBLISHER_PASSWORD'];
if (!password) {
  console.error('TESSERA_PUBLISHER_PASSWORD: missing or empty');
  process.exit(1);
}
const database = readDatabasePlace(process.env);

// Run straight after npm start, the database may still be setting itself up.
const notReady = await waitForDatabase(container, database.name);
if (notReady !== undefined) {
  console.error(notReady);
  process.exit(1);
}

const grant = [
  'GRANT USAGE ON SCHEMA food, outdoors, culture, transport TO tessera_reader;',
  'GRANT SELECT ON ALL TABLES IN SCHEMA food, outdoors, culture, transport TO tessera_reader;',
].join(' ');

// "--env PGPASSWORD" with no value copies the variable from this script's own environment, so the
// password never appears on a command line, where any user's process list could show it.
await $({ env: { ...process.env, PGPASSWORD: password } })`docker exec --env PGPASSWORD ${container} psql --host 127.0.0.1 --username tessera_publisher --dbname ${database.name} --command ${grant}`;
console.log('Published the nine layers. Restart the service to serve them.');
