// Prints the steps that make Windows back up the database each night at 02:00. Run it inside
// Ubuntu, in WSL2, from the repository:
//
//   npm run backup-task
//
// Registering a task needs an administrator, so this script prints the PowerShell for one to run,
// as npm run boot-task does, rather than running it itself.
import { resolve } from 'node:path';
import { backupTaskSteps } from './lib/backup.ts';
import { linuxPath } from './lib/boot.ts';

const distro = process.env['WSL_DISTRO_NAME'];
if (!distro) {
  console.error('Run this inside Ubuntu on WSL2, on the office machine.');
  process.exit(1);
}

// The task finds Node.js and Docker by the same folders this shell uses now.
console.log(backupTaskSteps({
  distro,
  repo: resolve(import.meta.dirname, '..'),
  node: process.execPath,
  path: linuxPath(process.env['PATH'] ?? ''),
}).join('\n'));
