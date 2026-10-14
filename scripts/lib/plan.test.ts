// Tests of the scripts' pure decisions; node --test runs them, with no Docker and no database.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { databaseUrl, decideImport, nginxArguments, parseCounts, placeTables, readDatabasePlace, readStartSettings } from './plan.ts';

test('imports when all nine tables are missing', () => {
  assert.deepEqual(decideImport(new Map()), { kind: 'import' });
});

test('imports when all nine tables are empty', () => {
  assert.deepEqual(decideImport(new Map(placeTables.map(table => [table, 0]))), { kind: 'import' });
});

test('leaves the places alone when all nine tables hold rows', () => {
  assert.deepEqual(decideImport(new Map(placeTables.map(table => [table, 100]))), { kind: 'skip' });
});

test('stops when some tables are full and some are empty', () => {
  const counts = new Map(placeTables.map((table, index) => [table, index === 0 ? 0 : 100]));
  assert.equal(decideImport(counts).kind, 'refuse');
});

test('reads psql output into counts', () => {
  assert.deepEqual(parseCounts('food.cafes|100\nfood.markets|-1\n'), new Map([['food.cafes', 100], ['food.markets', -1]]));
});

test('names every missing variable at once', () => {
  const result = readStartSettings({});
  assert.equal(result.ok, false);
  assert.equal(result.ok ? 0 : result.error.length, 4);
});

test('runs nginx on the machine\'s network, with both ports filled in', () => {
  const args = nginxArguments('tessera-nginx', '80', '7800', '/srv/nginx.conf.template');
  assert.ok(args.includes('host'));
  assert.ok(args.includes('TESSERA_HTTPPORT=80'));
  assert.ok(args.includes('TESSERA_SERVICE_PORT=7800'));
  assert.ok(args.includes('/srv/nginx.conf.template:/etc/nginx/templates/default.conf.template:ro'));
});

test('puts the service on port 7800 unless told otherwise', () => {
  const result = readStartSettings({ TESSERA_DB_PASSWORD: 'a', TESSERA_READER_PASSWORD: 'b', TESSERA_PUBLISHER_PASSWORD: 'c', TESSERA_HTTPPORT: '80' });
  assert.equal(result.ok ? result.value.servicePort : '', '7800');
});

test('percent-encodes a password that holds an @', () => {
  assert.equal(databaseUrl('tessera_reader', 's@cret', { host: '127.0.0.1', port: '5432', name: 'tessera' }), 'postgresql://tessera_reader:s%40cret@127.0.0.1:5432/tessera');
});

test('places the database on this machine unless told otherwise', () => {
  assert.deepEqual(readDatabasePlace({}), { host: '127.0.0.1', port: '5432', name: 'tessera' });
  assert.deepEqual(readDatabasePlace({ TESSERA_DB_HOST: 'db.office', TESSERA_DB_PORT: '5433', TESSERA_DB_NAME: 'places' }), { host: 'db.office', port: '5433', name: 'places' });
});
