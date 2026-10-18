// Tests of the coverage check's pure parts; node --test runs them, with no .NET and no Docker.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { coverageArguments, floors, readSummary, reportArguments, shortfalls } from './coverage.ts';

const summary = (counts: Record<string, number>): string => JSON.stringify({ summary: counts });

test('asks each suite for a Cobertura record named after it', () => {
  assert.deepEqual(coverageArguments('UnitTests', '/repo/coverage'), [
    '--coverage', '--coverage-output-format', 'cobertura', '--results-directory', '/repo/coverage', '--coverage-output', 'UnitTests.cobertura.xml',
  ]);
});

test('merges every record in the folder into one report', () => {
  const [reports, target] = reportArguments('coverage');
  assert.equal(reports, '-reports:coverage/*.cobertura.xml');
  assert.equal(target, '-targetdir:coverage/report');
});

test('works the shares out from the counts, cutting rather than rounding', () => {
  const text = summary({ coveredlines: 502, coverablelines: 524, coveredbranches: 207, totalbranches: 240 });
  assert.deepEqual(readSummary(text), { lines: 95.8, branches: 86.2 });
  // 9,399 of 10,000 lines is 93.99 per cent: it stays 93.9, below a floor of 94.
  assert.deepEqual(readSummary(summary({ coveredlines: 9399, coverablelines: 10000, coveredbranches: 1, totalbranches: 1 })), { lines: 93.9, branches: 100 });
  assert.deepEqual(readSummary(summary({ coveredlines: 0, coverablelines: 0, coveredbranches: 0, totalbranches: 0 })), { lines: 100, branches: 100 });
});

test('says what is wrong with a summary it cannot read', () => {
  assert.equal(readSummary('not json'), 'Summary.json is not JSON.');
  assert.match(String(readSummary(summary({ coveredlines: 1 }))), /lacks the counts/);
});

test('passes at the floors and fails below them, naming each share that falls short', () => {
  assert.deepEqual(shortfalls(floors, floors), []);
  assert.deepEqual(shortfalls({ lines: 93.9, branches: 90 }, floors), ['The tests cover 93.9 per cent of the lines, below the floor of 94.']);
  assert.equal(shortfalls({ lines: 50, branches: 50 }, floors).length, 2);
});
