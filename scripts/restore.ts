// Puts a backup back: every table, every grant and DbUp's record, as they stood when the backup
// was taken. Whatever the database holds now in those tables is replaced.
//
//   npm run restore -- backups/tessera-2026-10-17T0200Z.dump
//
// It runs in one transaction, so a restore that fails part of the way changes nothing. The service
// may keep running: it reads its list of layers again within Catalogue.RefreshSeconds, and a tile
// asked for while the restore runs waits for it, or answers 503 (N-22). A new, empty database, as
// npm start makes on a new machine, takes a backup the same way.
import { existsSync } from 'node:fs';
import { runWithFile, waitForDatabase } from './lib/database.ts';
import { readDatabasePlace } from './lib/plan.ts';

if (existsSync('.env')) process.loadEnvFile('.env');

const container = 'tessera-db';
const database = readDatabasePlace(process.env);
const file = process.argv[2];
if (file === undefined || !existsSync(file)) {
  console.error('Name a backup to restore, such as: npm run restore -- backups/tessera-2026-10-17T0200Z.dump');
  process.exit(2);
}

const notReady = await waitForDatabase(container, database.name);
if (notReady !== undefined) {
  console.error(notReady);
  process.exit(1);
}

// --clean --if-exists drops each object the backup holds before it makes it again, so the restore
// replaces rather than adds; --single-transaction and --exit-on-error make it all or nothing.
// The roles are not in the backup: the database's first start makes them (db/initdb/20-roles.sql),
// so the grants in the backup find them waiting.
const restored = await runWithFile(container, file, ['pg_restore', '--username', 'postgres', '--dbname', database.name, '--clean', '--if-exists', '--single-transaction', '--exit-on-error']);
if (restored.code !== 0) {
  console.error('The restore failed and changed nothing. Read pg_restore\'s message above.');
  process.exit(1);
}
console.log(`Restored ${file}. The service serves its layers within Catalogue.RefreshSeconds (30 seconds by default).`);
