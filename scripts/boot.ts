// Runs npm start for the start-up task, with everything it prints going to start.log beside
// package.json. Windows' Task Scheduler runs this file when the office machine switches on;
// npm run boot-task prints the steps that register the task. People run npm start instead.
//
// Each start opens with a line giving the time. A log over 10 MB moves aside to start.log.1 first,
// so the file cannot fill the disk. The script ends with npm start's own exit code, so Windows
// knows when a start failed and can try again.
import { spawn } from 'node:child_process';
import { appendFileSync, existsSync, openSync, renameSync, statSync } from 'node:fs';
import { resolve } from 'node:path';
import { heading, logName, mustMoveAside } from './lib/boot.ts';

const repo = resolve(import.meta.dirname, '..');
const log = resolve(repo, logName);
if (existsSync(log) && mustMoveAside(statSync(log).size)) renameSync(log, `${log}.1`);
appendFileSync(log, heading(new Date()));

// The same command as npm start, in package.json, with its output sent to the log.
const output = openSync(log, 'a');
const start = spawn(process.execPath, ['--env-file-if-exists=.env', 'scripts/start.ts'], {
  cwd: repo,
  stdio: ['ignore', output, output],
});

// When Windows ends the task, npm start stops both copies, as it does on Ctrl+C.
for (const signal of ['SIGINT', 'SIGTERM'] as const) process.on(signal, () => start.kill('SIGINT'));

start.on('exit', (code, signal) => {
  appendFileSync(log, `=== npm start ended with ${code === null ? `signal ${signal}` : `code ${code}`} ===\n`);
  process.exit(code ?? 1);
});
