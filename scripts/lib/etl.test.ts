// Tests of npm run etl's pure parts; node --test runs them, with no .NET and no GDAL.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readEtlArguments, uvArguments } from './etl.ts';

test('reads one pipeline file, and refuses anything else', () => {
  assert.deepEqual(readEtlArguments(['data/parks.pipeline.json']), { ok: true, value: 'data/parks.pipeline.json' });
  assert.equal(readEtlArguments([]).ok, false);
  assert.equal(readEtlArguments(['a.json', 'b.json']).ok, false);
  // A source passed by mistake, in place of its pipeline file, gets a plain answer.
  const source = readEtlArguments(['data/parks.gdb']);
  assert.ok(!source.ok && source.error.includes('a pipeline file is JSON'));
});

test('runs the Python pipeline in its own project, with the file last', () => {
  assert.deepEqual(uvArguments('../tessera-etl-python', 'data/parks.pipeline.json'),
    ['run', '--project', '../tessera-etl-python', 'tessera-etl', 'data/parks.pipeline.json']);
});
