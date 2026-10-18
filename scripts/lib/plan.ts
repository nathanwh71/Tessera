// Pure decisions for the scripts (ADR 0002, rule 1). Each function reads only its parameters and
// returns a value, so node --test can check it without Docker, a database or a shell.
import { err, ok, type Result } from '../../web/src/shared/result.ts';

/** The settings the start-up script needs, all from the environment (US-00 c4). */
export type StartSettings = {
  readonly ownerPassword: string;
  readonly readerPassword: string;
  readonly publisherPassword: string;
  readonly httpPort: string;
  readonly servicePorts: readonly [string, string];
  readonly officeNetwork: string;
  readonly database: DatabasePlace;
};

/** Where the database answers: the host and port the service dials, and the database's name. */
export type DatabasePlace = {
  readonly host: string;
  readonly port: string;
  readonly name: string;
};

/**
 * Reads the start-up settings, naming every missing variable at once, so one failed start tells
 * the maintainer everything to fix. The result is the map's own ***Result*** type (ADR 0006), shared
 * so the scripts and the map say 'failed' one way.
 *
 * The first copy listens on 7800 unless told otherwise, the port US-35 and N-29 name for the
 * service, and the second on the next port. A port below 1024 needs root on Linux, and the second
 * copy needs room for port + 1, so the first must lie between 1024 and 65534.
 */
export function readStartSettings(environment: Readonly<Record<string, string | undefined>>): Result<StartSettings, readonly string[]> {
  const required = ['TESSERA_DB_PASSWORD', 'TESSERA_READER_PASSWORD', 'TESSERA_PUBLISHER_PASSWORD', 'TESSERA_HTTPPORT', 'TESSERA_OFFICE_NETWORK'] as const;
  const missing = required.filter(name => !environment[name]);
  const network = environment['TESSERA_OFFICE_NETWORK'] ?? '';
  const firstPort = environment['TESSERA_SERVICE_PORT'] || '7800';
  const problems = [
    ...missing.map(name => `${name}: missing or empty`),
    ...(network === '' || isOfficeNetwork(network) ? [] : [`TESSERA_OFFICE_NETWORK: '${network}' is not a network such as 192.168.10.0/24, with a prefix from 8 to 32`]),
    ...(/^\d+$/.test(firstPort) && Number(firstPort) >= 1024 && Number(firstPort) < 65535 ? [] : [`TESSERA_SERVICE_PORT: '${firstPort}' is not a port from 1024 to 65534`]),
  ];
  return problems.length > 0
    ? err(problems)
    : ok({
      ownerPassword: environment['TESSERA_DB_PASSWORD'] ?? '',
      readerPassword: environment['TESSERA_READER_PASSWORD'] ?? '',
      publisherPassword: environment['TESSERA_PUBLISHER_PASSWORD'] ?? '',
      httpPort: environment['TESSERA_HTTPPORT'] ?? '',
      servicePorts: [firstPort, String(Number(firstPort) + 1)],
      officeNetwork: network,
      database: readDatabasePlace(environment),
    });
}

/**
 * How long to wait before starting a copy of the service again (N-29). The first restart waits a
 * second; each quick failure after it doubles the wait, up to 30 seconds, so a copy that cannot
 * start, such as one with a broken setting, does not spin. A copy that ran for a minute starts the
 * count again at one.
 */
export function restartDelay(quickFailures: number): number {
  return Math.min(30_000, 1000 * 2 ** Math.max(0, quickFailures - 1));
}

/**
 * True for an IPv4 network written as an address, a slash and a prefix, such as 192.168.10.0/24:
 * the prefix counts the leading bits every office address shares. A prefix below 8 would let in
 * more than sixteen million addresses, so the check refuses it, and with it /0, which lets in all.
 */
export function isOfficeNetwork(text: string): boolean {
  const parts = /^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})\/(\d{1,2})$/.exec(text);
  if (parts === null) return false;
  const [, ...numbers] = parts.map(Number);
  const prefix = numbers[4] ?? -1;
  return numbers.slice(0, 4).every(octet => octet <= 255) && prefix >= 8 && prefix <= 32;
}

/**
 * The container answers on this machine alone, so the defaults are 127.0.0.1, PostgreSQL's usual
 * port 5432, and the name the image gives the database. Each may change, for a database elsewhere.
 */
export function readDatabasePlace(environment: Readonly<Record<string, string | undefined>>): DatabasePlace {
  return {
    host: environment['TESSERA_DB_HOST'] || '127.0.0.1',
    port: environment['TESSERA_DB_PORT'] || '5432',
    name: environment['TESSERA_DB_NAME'] || 'tessera',
  };
}

/**
 * The arguments for `docker run` that start the packaged database (US-01). The port binds to
 * 127.0.0.1, so only this machine reaches the database; the office reaches the service alone.
 * The volume keeps the data when the container goes. Each argument stays one word, however many
 * spaces a password holds, since the script passes the list as it is, not as one line of text.
 *
 * --restart unless-stopped brings the database back after a crash, a reboot or a restart of
 * Docker, unless someone stopped it on purpose. The volume mounts at /var/lib/postgresql, not
 * /var/lib/postgresql/data: from PostgreSQL 18 the image keeps each version's data in a folder of
 * its own below it, so a later upgrade finds the old data on the same volume.
 */
export function runArguments(settings: StartSettings, container: string, volume: string): readonly string[] {
  return [
    '--detach', '--name', container, '--restart', 'unless-stopped',
    '--publish', `127.0.0.1:${settings.database.port}:5432`,
    '--volume', `${volume}:/var/lib/postgresql`,
    '--env', `POSTGRES_DB=${settings.database.name}`,
    '--env', `POSTGRES_PASSWORD=${settings.ownerPassword}`,
    '--env', `TESSERA_READER_PASSWORD=${settings.readerPassword}`,
    '--env', `TESSERA_PUBLISHER_PASSWORD=${settings.publisherPassword}`,
    container,
  ];
}

/**
 * A database URL for a ***role***. encodeURIComponent writes each character a URL would misread, such
 * as '@' in a password, as '%' and two hexadecimal digits.
 */
export function databaseUrl(user: string, password: string, database: DatabasePlace): string {
  return `postgresql://${encodeURIComponent(user)}:${encodeURIComponent(password)}@${database.host}:${database.port}/${encodeURIComponent(database.name)}`;
}

/**
 * The arguments for `docker run` that start Nginx in front of the two copies of the service
 * (ADR 0008, N-29). Nginx shares the machine's own network, so it reaches the service at
 * 127.0.0.1 and opens the office port itself; it runs as root inside its container, so port 80
 * needs no change to Ubuntu. The image fills the ports and the office's network into the template
 * when it starts (S-07). --restart unless-stopped brings Nginx back after a crash, a reboot or a
 * restart of Docker; the copies behind it come back only when npm start runs again.
 */
export function nginxArguments(container: string, httpPort: string, servicePorts: readonly [string, string], officeNetwork: string, template: string): readonly string[] {
  return [
    '--detach', '--name', container, '--restart', 'unless-stopped', '--network', 'host',
    '--env', `TESSERA_HTTPPORT=${httpPort}`,
    '--env', `TESSERA_SERVICE_PORT=${servicePorts[0]}`,
    '--env', `TESSERA_SECOND_SERVICE_PORT=${servicePorts[1]}`,
    '--env', `TESSERA_OFFICE_NETWORK=${officeNetwork}`,
    '--volume', `${template}:/etc/nginx/templates/default.conf.template:ro`,
    nginxImage,
  ];
}

/**
 * The Nginx image both the start-up script and the browser tests run, so the tests check the Nginx
 * the office runs. Each builds it from nginx/Dockerfile, which names the Nginx release, before it
 * runs a container from it.
 */
export const nginxImage = 'tessera-nginx';

/**
 * The lines to show from what `Tessera seed` printed: the scripts it ran, or that it had none to
 * run (US-01, ADR 0009). zx keeps a command's output to itself unless asked, so start.ts prints
 * these lines, as it prints its other steps' messages. Blank lines drop out.
 */
export function seedReport(printed: string): readonly string[] {
  const lines = printed.split(/\r?\n/).filter(line => line.trim() !== '');
  return lines.length > 0 ? lines : ['The seed ran, and printed no report.'];
}

/**
 * Playwright's own Docker image for a version of @playwright/test: Ubuntu with the Chromium,
 * the fonts and the libraries that version was tested with. The approved images of UI-17 (test 3)
 * match within their tolerance there, since the image brings the same browser to every machine.
 */
export const playwrightImage = (version: string): string => `mcr.microsoft.com/playwright:v${version}-noble`;

/**
 * The command that runs the browser tests: Playwright on this machine, or, when `image` names one,
 * Playwright inside that image. The container shares the machine's network, so it reaches the
 * service on 127.0.0.1, and its memory for passing data between processes, which Chromium needs
 * more of than Docker gives by default. It runs as `user`, so the files it writes in the
 * repository stay the user's. HOME points at /tmp, since that user has no home in the image. `--env NAME` with no
 * value copies the variable from this script's environment, and leaves it out if it is not set.
 */
export function playwrightCommand(image: string | undefined, repo: string, user: string, args: readonly string[]): readonly string[] {
  const tests = ['npx', 'playwright', 'test', ...args];
  return image === undefined ? tests : [
    'docker', 'run', '--rm', '--network', 'host', '--ipc', 'host', '--user', user,
    '--env', 'HOME=/tmp', '--env', 'TESSERA_BASE_URL', '--env', 'TESSERA_MEASURE', '--env', 'CI',
    '--volume', `${repo}:/work`, '--workdir', '/work/web', image, ...tests,
  ];
}
