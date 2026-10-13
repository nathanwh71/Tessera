// The one step the scripts share that needs Docker: waiting for the database in its container.
import { $, sleep } from 'zx';

/**
 * Waits until the database answers on 127.0.0.1 inside its container, for up to a minute. On a
 * first start the image runs its set-up scripts on a private server, which answers on no network
 * port, so "running" is not yet "ready". The result is the reason it is not ready, or nothing.
 */
export async function waitForDatabase(container: string, name: string): Promise<string | undefined> {
  for (let attempt = 1; attempt <= 60; attempt++) {
    const ready = await $({ nothrow: true, quiet: true })`docker exec ${container} pg_isready --host 127.0.0.1 --username postgres --dbname ${name}`;
    if (ready.exitCode === 0) return undefined;
    await sleep(1000);
  }
  return `The database did not answer within a minute; read "docker logs ${container}".`;
}
