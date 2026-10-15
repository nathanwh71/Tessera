// Tests of npm run etl's pure parts; node --test runs them, with no .NET and no GDAL.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { dotnetArguments, readEtlArguments } from './etl.ts';

test('reads one pipeline file, and refuses anything else', () => {
  assert.deepEqual(readEtlArguments(['data/parks.pipeline.json']), { ok: true, value: 'data/parks.pipeline.json' });
  assert.equal(readEtlArguments([]).ok, false);
  assert.equal(readEtlArguments(['a.json', 'b.json']).ok, false);
  // A source passed by mistake, in place of its pipeline file, gets a plain answer.
  const source = readEtlArguments(['data/parks.gdb']);
  assert.ok(!source.ok && source.error.includes('a pipeline file is JSON'));
});

test('runs the C# pipeline in Release, with the file after --', () => {
  assert.deepEqual(dotnetArguments('data/parks.pipeline.json'),
    ['run', '--project', 'tools/Tessera.Etl', '--configuration', 'Release', '--', 'data/parks.pipeline.json']);
});
