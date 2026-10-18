// Tests of the scripts' pure decisions; node --test runs them, with no Docker and no database.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { databaseUrl, isOfficeNetwork, nginxArguments, readDatabasePlace, readStartSettings, playwrightCommand, playwrightImage, restartDelay, seedReport } from './plan.ts';

test('names every missing variable at once', () => {
  const result = readStartSettings({});
  assert.equal(result.ok, false);
  assert.equal(result.ok ? 0 : result.error.length, 5);
});

test('runs nginx on the machine\'s network, with both ports filled in', () => {
  const args = nginxArguments('tessera-nginx', '80', ['7800', '7801'], '192.168.10.0/24', '/srv/nginx.conf.template');
  assert.ok(args.includes('host'));
  assert.ok(args.includes('TESSERA_HTTPPORT=80'));
  assert.ok(args.includes('TESSERA_SERVICE_PORT=7800'));
  assert.ok(args.includes('TESSERA_SECOND_SERVICE_PORT=7801'));
  assert.ok(args.includes('TESSERA_OFFICE_NETWORK=192.168.10.0/24'));
  assert.ok(args.includes('/srv/nginx.conf.template:/etc/nginx/templates/default.conf.template:ro'));
});

// Every required variable set, so each test below changes only the one setting it checks.
const office = { TESSERA_DB_PASSWORD: 'a', TESSERA_READER_PASSWORD: 'b', TESSERA_PUBLISHER_PASSWORD: 'c', TESSERA_HTTPPORT: '80', TESSERA_OFFICE_NETWORK: '192.168.10.0/24' };

// S-07: the office's network must be a real range, and never one that lets in the world.
test('accepts an office network, and refuses one too wide or misspelt', () => {
  for (const good of ['192.168.10.0/24', '10.0.0.0/8', '127.0.0.4/30', '192.168.10.7/32']) assert.ok(isOfficeNetwork(good), good);
  for (const bad of ['0.0.0.0/0', '10.0.0.0/7', '192.168.10.0', '192.168.10.0/33', '192.168.300.0/24', 'office']) assert.ok(!isOfficeNetwork(bad), bad);
  const read = readStartSettings({ ...office, TESSERA_OFFICE_NETWORK: '0.0.0.0/0' });
  assert.match(read.ok ? '' : read.error.join(), /TESSERA_OFFICE_NETWORK/);
});

// N-29: two copies, on the port given and the next one.
test('puts the two copies on ports 7800 and 7801 unless told otherwise', () => {
  const result = readStartSettings(office);
  assert.deepEqual(result.ok ? result.value.servicePorts : [], ['7800', '7801']);
  const moved = readStartSettings({ ...office, TESSERA_SERVICE_PORT: '8100' });
  assert.deepEqual(moved.ok ? moved.value.servicePorts : [], ['8100', '8101']);
  // 80 needs root, 65535 leaves no port for the second copy, and 'abc' is no port at all.
  for (const bad of ['80', '65535', 'abc']) assert.equal(readStartSettings({ ...office, TESSERA_SERVICE_PORT: bad }).ok, false, bad);
});

test('waits a second before a restart, doubling after each quick failure up to 30', () => {
  assert.deepEqual([0, 1, 2, 3, 4, 5, 6, 10].map(restartDelay), [1000, 1000, 2000, 4000, 8000, 16000, 30000, 30000]);
});

test('percent-encodes a password that holds an @', () => {
  assert.equal(databaseUrl('tessera_reader', 's@cret', { host: '127.0.0.1', port: '5432', name: 'tessera' }), 'postgresql://tessera_reader:s%40cret@127.0.0.1:5432/tessera');
});

test('places the database on this machine unless told otherwise', () => {
  assert.deepEqual(readDatabasePlace({}), { host: '127.0.0.1', port: '5432', name: 'tessera' });
  assert.deepEqual(readDatabasePlace({ TESSERA_DB_HOST: 'db.office', TESSERA_DB_PORT: '5433', TESSERA_DB_NAME: 'places' }), { host: 'db.office', port: '5433', name: 'places' });
});

// The seed's report, 'Ran 0002-parks.sql' or 'The seed is up to date.', reaches the screen
// (US-01, ADR 0009): the operator sees which scripts ran, and that a new table waits for its grant.
test('passes on the seed\'s own report, line by line', () => {
  assert.deepEqual(seedReport('Ran 0002-parks.sql\nA new table waits for its grant: npm run publish.\n'),
    ['Ran 0002-parks.sql', 'A new table waits for its grant: npm run publish.']);
  assert.deepEqual(seedReport('The seed is up to date.\r\n'), ['The seed is up to date.']);
});

test('says so when the seed printed nothing, rather than print a blank line', () => {
  assert.deepEqual(seedReport(''), ['The seed ran, and printed no report.']);
});

// UI-17 test 3: the approved images match only when Chromium draws them as it drew them, so the
// browser tests can run inside Playwright's own Docker image, the one named after its version.
test('names Playwright\'s image after the version the map pins', () => {
  assert.equal(playwrightImage('1.56.0'), 'mcr.microsoft.com/playwright:v1.56.0-noble');
});

test('runs Playwright on the machine, or inside its image when one is named', () => {
  assert.deepEqual(playwrightCommand(undefined, '/repo', '1001:1001', ['--grep', 'UI-17']),
    ['npx', 'playwright', 'test', '--grep', 'UI-17']);
  const docker = playwrightCommand('mcr.microsoft.com/playwright:v1.56.0-noble', '/repo', '1001:1001', ['--grep', 'UI-17']);
  assert.deepEqual(docker.slice(0, 2), ['docker', 'run']);
  assert.deepEqual(docker.slice(-6), ['mcr.microsoft.com/playwright:v1.56.0-noble', 'npx', 'playwright', 'test', '--grep', 'UI-17']);
  for (const part of ['--rm', 'host', '1001:1001', '/repo:/work', '/work/web', 'TESSERA_BASE_URL']) assert.ok(docker.includes(part), part);
});
