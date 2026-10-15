// Tests of the scripts' pure decisions; node --test runs them, with no Docker and no database.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { databaseUrl, isOfficeNetwork, nginxArguments, readDatabasePlace, readStartSettings, restartDelay } from './plan.ts';

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
