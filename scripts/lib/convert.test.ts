// Tests of the converter's pure parts (ADR 0009); node --test runs them, with no GDAL and no Docker.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { ogrArguments, readConvertArguments, seedScript } from './convert.ts';

const cafes = { schema: 'food', table: 'cafes' };

test('reads a table with a filter, splitting at the first colon alone', () => {
  const read = readConvertArguments(['places.geojson', 'db/seed/0002-places.sql', "food.cafes:layer = 'a:b'", 'food.markets']);
  assert.deepEqual(read.ok ? read.value.targets : [], [{ schema: 'food', table: 'cafes', where: "layer = 'a:b'" }, { schema: 'food', table: 'markets' }]);
});

test('names every fault at once', () => {
  const read = readConvertArguments(['places.geojson', 'places.sql', 'Food.cafes', 'cafes']);
  assert.equal(read.ok ? 0 : read.error.length, 3);
});

test('asks for a source, a script and at least one table', () => {
  assert.equal(readConvertArguments(['places.geojson', 'db/seed/0002-places.sql']).ok, false);
});

// DbUp sends plain SQL, so the script must hold INSERT statements rather than COPY.
test('asks ogr2ogr for INSERT statements, a numbered key and longitude and latitude', () => {
  const args = ogrArguments('/data/places.geojson', { ...cafes, where: "layer = 'cafes'" });
  assert.deepEqual(args.slice(0, 3), ['--config', 'PG_USE_COPY', 'NO']);
  assert.ok(args.includes('FID=fid'));
  assert.ok(args.includes('EPSG:4326'));
  assert.deepEqual(args.slice(-2), ['-where', "layer = 'cafes'"]);
});

// GDAL's BEGIN and COMMIT would end DbUp's transaction early; a place's own text keeps its lines.
test('drops GDAL\'s own transaction and keeps everything else', () => {
  const sql = ['DROP TABLE IF EXISTS "food"."cafes" CASCADE;', 'BEGIN;', "INSERT INTO \"food\".\"cafes\" VALUES ('COMMIT;", "COMMIT;", "');", 'COMMIT;', ''].join('\n');
  const script = seedScript('places.geojson', [{ target: cafes, sql }]);
  const lines = script.ok ? script.value.split('\n') : [];

  assert.equal(lines.filter(line => line === 'BEGIN;').length, 0);
  assert.equal(lines.filter(line => line === 'COMMIT;').length, 1);
  assert.ok(lines.includes('CREATE SCHEMA IF NOT EXISTS "food";'));
  assert.ok(lines.includes('ANALYZE "food"."cafes";'));
});

test('refuses a table the filter left empty', () => {
  const script = seedScript('places.geojson', [{ target: cafes, sql: 'BEGIN;\nCOMMIT;\n' }]);
  assert.deepEqual(script.ok ? [] : script.error, ['food.cafes: the source gave no rows; check the filter']);
});
