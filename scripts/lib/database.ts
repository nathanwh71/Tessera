// The waits the scripts share, for the database in its container and for the service to say UP,
// and the two ways a backup's file passes in or out of the database's container.
import { spawn } from 'node:child_process';
import { createWriteStream } from 'node:fs';
import { open } from 'node:fs/promises';
import { pipeline } from 'node:stream/promises';
import { $, sleep } from 'zx';

/**
 * Waits until the database answers on 127.0.0.1 inside its container, for up to a minute. On a
 * first start the image runs its set-up scripts on a private server, which answers on no network
 * port, so 'running' is not yet 'ready'. The result is the reason it is not ready, or nothing.
 */
export async function waitForDatabase(container: string, name: string): Promise<string | undefined> {
  for (let attempt = 1; attempt <= 60; attempt++) {
    const ready = await $({ nothrow: true, quiet: true })`docker exec ${container} pg_isready --host 127.0.0.1 --username postgres --dbname ${name}`;
    if (ready.exitCode === 0) return undefined;
    await sleep(1000);
  }
  return `The database did not answer within a minute; read "docker logs ${container}".`;
}

/**
 * Asks the service's /health once a second, for up to `seconds`, until it answers {"status":"UP"}
 * (N-25). A refused connection means the service is still starting, so it counts as 'not yet'.
 */
export async function waitForHealth(url: string, seconds: number): Promise<'up' | 'not yet'> {
  for (let attempt = 1; attempt <= seconds; attempt++) {
    const status = await fetch(url).then(answer => answer.ok ? answer.json() as Promise<{ status?: string }> : undefined, () => undefined);
    if (status?.status === 'UP') return 'up';
    await sleep(1000);
  }
  return 'not yet';
}

/**
 * Runs a program in the container with a file as its input, and returns its exit code and what it
 * printed. The file streams in, so a backup of any size passes without filling memory.
 */
export async function runWithFile(container: string, file: string, command: readonly string[]): Promise<{ readonly code: number | null; readonly output: string }> {
  const input = await open(file);
  try {
    const run = spawn('docker', ['exec', '--interactive', container, ...command], { stdio: [input.fd, 'pipe', 'inherit'] });
    let output = '';
    run.stdout?.setEncoding('utf8').on('data', (part: string) => { output += part; });
    const code = await new Promise<number | null>(done => run.on('close', done));
    return { code, output };
  } finally {
    await input.close();
  }
}

/**
 * Runs a program in the container and writes what it prints to a file, a stream at a time, so a
 * backup of any size passes without filling memory. The result is the program's exit code.
 */
export async function runToFile(container: string, file: string, command: readonly string[]): Promise<number | null> {
  const run = spawn('docker', ['exec', container, ...command], { stdio: ['ignore', 'pipe', 'inherit'] });
  const ended = new Promise<number | null>(done => run.on('close', done));
  await pipeline(run.stdout, createWriteStream(file));
  return ended;
}
