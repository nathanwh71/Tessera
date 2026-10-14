// Pure decisions for the scripts (ADR 0002, rule 1). Each function reads only its parameters and
// returns a value, so node --test can check it without Docker, a database or a shell.
import { err, ok, type Result } from '../../web/src/shared/result.ts';

/** The settings the start-up script needs, all from the environment (US-00 c4). */
export type StartSettings = {
  readonly ownerPassword: string;
  readonly readerPassword: string;
  readonly publisherPassword: string;
  readonly httpPort: string;
  readonly servicePort: string;
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
 * the maintainer everything to fix. The result is the map's own Result type (ADR 0006), shared
 * so the scripts and the map say "failed" one way.
 */
export function readStartSettings(environment: Readonly<Record<string, string | undefined>>): Result<StartSettings, readonly string[]> {
  const required = ['TESSERA_DB_PASSWORD', 'TESSERA_READER_PASSWORD', 'TESSERA_PUBLISHER_PASSWORD', 'TESSERA_HTTPPORT'] as const;
  const missing = required.filter(name => !environment[name]);
  return missing.length > 0
    ? err(missing.map(name => `${name}: missing or empty`))
    : ok({
      ownerPassword: environment['TESSERA_DB_PASSWORD'] ?? '',
      readerPassword: environment['TESSERA_READER_PASSWORD'] ?? '',
      publisherPassword: environment['TESSERA_PUBLISHER_PASSWORD'] ?? '',
      httpPort: environment['TESSERA_HTTPPORT'] ?? '',
      servicePort: environment['TESSERA_SERVICE_PORT'] || '7800',
      database: readDatabasePlace(environment),
    });
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
 * The arguments for "docker run" that start the packaged database (US-01). The port binds to
 * 127.0.0.1, so only this machine reaches the database; the office reaches the service alone.
 * The volume keeps the data when the container goes. Each argument stays one word, however many
 * spaces a password holds, since the script passes the list as it is, not as one line of text.
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
 * A database URL for a role. encodeURIComponent writes each character a URL would misread, such
 * as '@' in a password, as '%' and two hexadecimal digits.
 */
export function databaseUrl(user: string, password: string, database: DatabasePlace): string {
  return `postgresql://${encodeURIComponent(user)}:${encodeURIComponent(password)}@${database.host}:${database.port}/${encodeURIComponent(database.name)}`;
}

/**
 * The arguments for "docker run" that start Nginx in front of the service (ADR 0008). Nginx shares
 * the machine's own network, so it reaches the service at 127.0.0.1 and opens the office port
 * itself; it runs as root inside its container, so port 80 needs no change to Ubuntu. The image
 * fills the two ports into the template when it starts.
 */
export function nginxArguments(container: string, httpPort: string, servicePort: string, template: string): readonly string[] {
  return [
    '--detach', '--name', container, '--restart', 'unless-stopped', '--network', 'host',
    '--env', `TESSERA_HTTPPORT=${httpPort}`,
    '--env', `TESSERA_SERVICE_PORT=${servicePort}`,
    '--volume', `${template}:/etc/nginx/templates/default.conf.template:ro`,
    nginxImage,
  ];
}

/** The Nginx image both the start-up script and the browser tests run. */
export const nginxImage = 'nginx:1.29-alpine';
