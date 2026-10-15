// Tests of npm run inspect's pure parts (ADR 0009); node --test runs them, with no GDAL and no Docker.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { nextScript, plain, readInspectArguments, readLayers, readValues, starter, summary } from './inspect.ts';

// What GDAL 3.8's `ogrinfo -json -so -al` writes for a geodatabase of cafés and bus routes, cut down.
const described = JSON.stringify({
  layers: [
    { name: 'Cafes', featureCount: 7, fields: [{ name: 'id' }, { name: 'Name' }],
      geometryFields: [{ type: 'PointZ', coordinateSystem: { projjson: { id: { authority: 'EPSG', code: 4979 } } } }] },
    { name: 'Bus Routes 2026', featureCount: 1200, fields: [{ name: 'code' }],
      geometryFields: [{ type: 'MultiLineString', coordinateSystem: null }] },
  ],
});

test('reads each layer\'s rows, shape, system and fields', () => {
  const [cafes, routes] = readLayers(described);
  assert.deepEqual(cafes, { name: 'Cafes', count: 7, shape: 'PointZ', system: 'EPSG:4979', fields: ['id', 'Name'] });
  assert.equal(routes?.system, undefined);
  assert.match(summary('x.gdb', readLayers(described)), /1,200 rows of MultiLineString, in an unknown system/);
});

test('reads a source, and a source split by a field', () => {
  assert.deepEqual(readInspectArguments(['data/parks.gdb']), { ok: true, value: { source: 'data/parks.gdb', split: undefined } });
  assert.deepEqual(readInspectArguments(['p.geojson', '--split', 'layer']), { ok: true, value: { source: 'p.geojson', split: 'layer' } });
  assert.equal(readInspectArguments(['p.geojson', '--split']).ok, false);
  assert.equal(readInspectArguments([]).ok, false);
});

test('makes plain table names and the next script\'s name', () => {
  assert.deepEqual(['Bus Routes 2026', '2026 Routes', '!!'].map(plain), ['bus_routes_2026', '_2026_routes', 'places']);
  assert.equal(nextScript(['0001-macau-places.sql', '0002-parks.sql'], 'data/Macau.gdb/'), '0003-macau.sql');
  assert.equal(nextScript([], 'parks.geojson'), '0001-parks.sql');
});

test('reads the values GDAL lists as CSV, quotes and all', () => {
  assert.deepEqual(readValues('layer\ncafes\n"O\'Brien""s"\nbuses\n\n'), ['O\'Brien"s', 'buses', 'cafes']);
});

test('writes one table for each layer, with the filters its shapes need', () => {
  const file = JSON.parse(starter('data/macau.gdb', 'db/seed/0002-macau.sql', readLayers(described))) as {
    tables: { table: string; layer?: string; lines?: boolean; filters: { filter: string }[] }[];
  };
  assert.deepEqual(file.tables.map(table => [table.table, table.layer, table.lines]), [['places.cafes', 'Cafes', undefined], ['places.bus_routes_2026', 'Bus Routes 2026', true]]);
  assert.ok(file.tables[1]?.filters.some(step => step.filter === 'repairShapes'));
  assert.ok(file.tables[0]?.filters.some(step => step.filter === 'requireFields'));
});

test('splits one layer into a table for each value, each picked by a where', () => {
  const one = readLayers(described).slice(0, 1);
  const file = JSON.parse(starter('p.geojson', 'db/seed/0002-p.sql', one, { field: 'kind', values: ['cafe', 'O\'Brien\'s'] })) as {
    tables: { table: string; layer?: string; where?: string }[];
  };
  assert.deepEqual(file.tables.map(table => [table.table, table.where, table.layer]), [
    ['places.cafe', '"kind" = \'cafe\'', undefined],
    ['places.o_brien_s', '"kind" = \'O\'\'Brien\'\'s\'', undefined],
  ]);
});
