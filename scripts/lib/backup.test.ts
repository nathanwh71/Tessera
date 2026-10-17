// Tests of the backup's pure decisions; node --test runs them, with no Docker and no database.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { backupName, backupTaskSteps, backupsToRemove, defaultKeep, isBackupName, readKeep } from './backup.ts';

test('names a backup after the minute it starts, in UTC', () => {
  assert.equal(backupName(new Date('2026-10-17T02:00:59Z')), 'tessera-2026-10-17T0200Z.dump');
  assert.ok(isBackupName(backupName(new Date())));
});

test('sorts backups by age when it sorts their names', () => {
  const names = ['2026-10-17T02:00Z', '2026-10-09T23:59Z', '2027-01-01T00:00Z'].map(moment => backupName(new Date(moment)));
  assert.deepEqual(names.toSorted(), [names[1], names[0], names[2]]);
});

test('removes the oldest backups beyond the number to keep, and no other file', () => {
  const names = ['tessera-2026-10-15T0200Z.dump', 'notes.txt', 'tessera-2026-10-17T0200Z.dump', 'tessera-2026-10-16T0200Z.dump', 'tessera-2026-10-14T0200Z.dump.part'];
  assert.deepEqual(backupsToRemove(names, 2), ['tessera-2026-10-15T0200Z.dump']);
  assert.deepEqual(backupsToRemove(names, 3), []);
  assert.deepEqual(backupsToRemove(names, 10), []);
});

test('keeps two weeks by default, and refuses a number below 1', () => {
  assert.equal(readKeep(undefined), defaultKeep);
  assert.equal(readKeep(''), defaultKeep);
  assert.equal(readKeep('30'), 30);
  for (const bad of ['0', '-1', 'ten', '1.5']) assert.match(String(readKeep(bad)), /TESSERA_BACKUP_KEEP/, bad);
});

test('registers a nightly task that runs the backup script', () => {
  const steps = backupTaskSteps({ distro: 'Ubuntu', repo: '/home/office/tessera', node: '/usr/bin/node', path: '/usr/bin' }).join('\n');
  assert.match(steps, /scripts\/backup\.ts/);
  assert.match(steps, /-Daily -At '02:00'/);
  assert.match(steps, /'Tessera backup'/);
});
