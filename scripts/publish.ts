// Publishes the places: grants the four schemas and their nine tables to the service's role, as
// tessera_publisher (US-24 c5). The import never grants; publishing is the publisher's own act.
//
//   npm run publish
//
// The service builds its list of layers at start-up, so restart it afterwards to serve them.
import { $ } from 'zx';
import { readStartSettings } from './lib/plan.ts';

const checked = readStartSettings(process.env);
if (!checked.ok) {
  checked.problems.forEach(problem => console.error(problem));
  process.exit(1);
}

const grant = [
  'GRANT USAGE ON SCHEMA food, outdoors, culture, transport TO tessera_reader;',
  'GRANT SELECT ON ALL TABLES IN SCHEMA food, outdoors, culture, transport TO tessera_reader;',
].join(' ');

// PGPASSWORD hands psql the password without putting it on the command line, where others could read it.
await $`docker exec --env PGPASSWORD=${checked.value.publisherPassword} tessera-db psql --host 127.0.0.1 --username tessera_publisher --dbname tessera --command ${grant}`;
console.log('Published the nine layers. Restart the service to serve them.');
