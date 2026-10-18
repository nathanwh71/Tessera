// Tests of the build's clear-out (US-23 c4): a real folder in the system's temporary space, so the
// test checks what the build does to files, with no .NET and no map build.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { mkdir, mkdtemp, readdir, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { emptyReleaseMap, releaseMap } from './build.ts';

test('empties the release\'s map folder, so no file of an older map ships', async () => {
  const root = await mkdtemp(join(tmpdir(), 'tessera-build-'));
  try {
    const old = join(releaseMap(root), 'assets');
    await mkdir(old, { recursive: true });
    await writeFile(join(old, 'index-old.js'), 'an older build of the map');
    await writeFile(join(releaseMap(root), 'index.html'), 'an older page');
    await emptyReleaseMap(root);
    assert.ok(!existsSync(releaseMap(root)) || (await readdir(releaseMap(root))).length === 0);
  } finally {
    await rm(root, { recursive: true, force: true });
  }
});

test('passes when no build has run yet, and leaves the rest of the release alone', async () => {
  const root = await mkdtemp(join(tmpdir(), 'tessera-build-'));
  try {
    await emptyReleaseMap(root);
    const program = join(releaseMap(root), '..', 'Tessera.dll');
    await mkdir(releaseMap(root), { recursive: true });
    await writeFile(program, 'the program');
    await emptyReleaseMap(root);
    assert.ok(existsSync(program));
  } finally {
    await rm(root, { recursive: true, force: true });
  }
});

test('names the folder the service serves the map from', () => {
  assert.equal(releaseMap('/repo'), join('/repo', 'src', 'Tessera', 'bin', 'Release', 'net10.0', 'wwwroot'));
});
