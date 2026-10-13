// Tests of the scripts' pure decisions; node --test runs them, with no Docker and no database.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { databaseUrl, decideImport, parseCounts, placeTables, portProblem, readStartSettings } from './plan.ts';

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
  assert.equal(result.ok ? 0 : result.problems.length, 4);
});

test('names the fix when port 80 lies below the unprivileged limit', () => {
  assert.match(portProblem('80', 1024) ?? '', /ip_unprivileged_port_start=80/);
});

test('lets a port at or above the limit through', () => {
  assert.equal(portProblem('80', 80), undefined);
  assert.equal(portProblem('8080', 1024), undefined);
});

test('percent-encodes a password that holds an @', () => {
  assert.equal(databaseUrl('tessera_reader', 's@cret', '5432'), 'postgresql://tessera_reader:s%40cret@127.0.0.1:5432/tessera');
});
