// Publishes the places: grants the four schemas and their nine tables to the service's role, as
// tessera_publisher (US-24 c5). The import never grants; publishing is the publisher's own act.
//
//   npm run publish
//
// The service builds its list of layers at start-up, so restart it afterwards to serve them.
import { $ } from 'zx';

const password = process.env['TESSERA_PUBLISHER_PASSWORD'];
if (!password) {
  console.error('TESSERA_PUBLISHER_PASSWORD: missing or empty');
  process.exit(1);
}

const grant = [
  'GRANT USAGE ON SCHEMA food, outdoors, culture, transport TO tessera_reader;',
  'GRANT SELECT ON ALL TABLES IN SCHEMA food, outdoors, culture, transport TO tessera_reader;',
].join(' ');

// "--env PGPASSWORD" with no value copies the variable from this script's own environment, so the
// password never appears on a command line, where any user's process list could show it.
await $({ env: { ...process.env, PGPASSWORD: password } })`docker exec --env PGPASSWORD tessera-db psql --host 127.0.0.1 --username tessera_publisher --dbname tessera --command ${grant}`;
console.log('Published the nine layers. Restart the service to serve them.');
