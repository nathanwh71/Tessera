// The waits the scripts share: for the database in its container, and for the service to say UP.
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
