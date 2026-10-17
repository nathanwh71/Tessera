// Backs up the database: every table, every grant and DbUp's record of the seed scripts, in one
// file. It then reads the file back, to prove it whole, and deletes the oldest backups beyond the
// number to keep.
//
//   npm run backup
//
// Settings come from the environment, or from .env beside package.json:
//   TESSERA_BACKUP_FOLDER   optional; where the backups go, backups/ by default. A folder on a second
//                           disk, such as /mnt/d/tessera-backups, outlives this machine's own disk.
//   TESSERA_BACKUP_KEEP     optional; how many backups to keep, 14 by default
//   TESSERA_DB_NAME         optional; the database's name, tessera by default
//
// pg_dump runs inside the database's container, as the owner, postgres, which the container lets in
// without a password; the service may go on serving while it runs, since pg_dump reads one moment of
// the database and blocks no reader. npm run restore puts a backup back.
import { existsSync } from 'node:fs';
import { mkdir, readdir, rename, rm, stat } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { backupName, backupsToRemove, defaultFolder, readKeep } from './lib/backup.ts';
import { runToFile, runWithFile, waitForDatabase } from './lib/database.ts';
import { readDatabasePlace } from './lib/plan.ts';

// The nightly task starts this script directly, with no npm to read .env for it.
if (existsSync('.env')) process.loadEnvFile('.env');

const container = 'tessera-db';
const database = readDatabasePlace(process.env);
const folder = resolve(process.env['TESSERA_BACKUP_FOLDER'] || defaultFolder);
const keep = readKeep(process.env['TESSERA_BACKUP_KEEP']);
if (typeof keep === 'string') {
  console.error(keep);
  process.exit(1);
}

const notReady = await waitForDatabase(container, database.name);
if (notReady !== undefined) {
  console.error(notReady);
  process.exit(1);
}

await mkdir(folder, { recursive: true });
const file = join(folder, backupName(new Date()));
// The backup is written under a second name first and renamed once whole, so a backup cut short,
// by a full disk or a power cut, never passes for a good one.
const partial = `${file}.part`;

// --format custom: one compressed file, from which pg_restore can put back the whole database or
// a single table.
const dumped = await runToFile(container, partial, ['pg_dump', '--username', 'postgres', '--dbname', database.name, '--format', 'custom']);
if (dumped !== 0) {
  await rm(partial, { force: true });
  console.error('pg_dump failed; no backup was kept. Read its message above.');
  process.exit(1);
}

// pg_restore --list reads the whole file and lists what it holds; a damaged file fails here,
// tonight, rather than on the day it is needed.
const check = await runWithFile(container, partial, ['pg_restore', '--list']);
const listed = check.code === 0 ? check.output.split('\n').filter(line => line.includes(' TABLE DATA ')).length : 0;
if (listed === 0) {
  await rm(partial, { force: true });
  console.error('The backup could not be read back, or held no tables; it was deleted.');
  process.exit(1);
}
await rename(partial, file);

const removed = backupsToRemove(await readdir(folder), keep);
for (const name of removed) await rm(join(folder, name));

const megabytes = ((await stat(file)).size / 1024 / 1024).toFixed(1);
console.log(`Backed up ${listed} tables to ${file} (${megabytes} MB); kept the newest ${keep}, deleted ${removed.length}.`);
