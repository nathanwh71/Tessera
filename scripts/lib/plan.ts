// Pure decisions for the scripts (ADR 0002, rule 1). Each function reads only its parameters and
// returns a value, so node --test can check it without Docker, a database or a shell.
import { err, ok, type Result } from '../../web/src/shared/result.ts';

/** The nine layers' tables, as schema.table: the ids the stories use (US-24). */
export const placeTables: readonly string[] = [
  'food.cafes', 'food.restaurants', 'food.markets',
  'outdoors.parks', 'outdoors.playgrounds',
  'culture.museums', 'culture.libraries',
  'transport.trains', 'transport.buses',
];

/** The settings the start-up script needs, all from the environment (US-00 c4). */
export type StartSettings = {
  readonly ownerPassword: string;
  readonly readerPassword: string;
  readonly publisherPassword: string;
  readonly httpPort: string;
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
 * Ubuntu lets an ordinary user open a port from ip_unprivileged_port_start upwards, 1024 by
 * default, so the service cannot answer on port 80 until that limit drops. This names the problem
 * and its fix before the service tries and fails (US-35).
 */
export function portProblem(port: string, unprivilegedStart: number): string | undefined {
  const number = Number.parseInt(port, 10);
  return number < unprivilegedStart
    ? `Port ${number} lies below ${unprivilegedStart}, the lowest port an ordinary user may open. Run once:\n` +
      `  echo "net.ipv4.ip_unprivileged_port_start=${number}" | sudo tee /etc/sysctl.d/90-tessera.conf && sudo sysctl --system`
    : undefined;
}

/** What to do with the places: load them, leave them, or stop because the tables disagree. */
export type ImportDecision =
  | { readonly kind: 'import' }
  | { readonly kind: 'skip' }
  | { readonly kind: 'refuse'; readonly reason: string };

/**
 * The start-up script imports only when the nine tables are empty: missing, or holding no rows
 * (ADR 0001). A count of -1 marks a missing table. Some full and some empty means a load went
 * wrong, and loading again by itself would hide it, so the script stops and says so.
 */
export function decideImport(rowsInTable: ReadonlyMap<string, number>): ImportDecision {
  const counts = placeTables.map(table => rowsInTable.get(table) ?? -1);
  const empty = counts.filter(count => count <= 0).length;
  return empty === placeTables.length ? { kind: 'import' }
    : empty === 0 ? { kind: 'skip' }
    : { kind: 'refuse', reason: `${empty} of the nine tables are empty and the rest are not; run the import by hand to replace them all.` };
}

/** Reads psql's "table|count" lines into a map; psql prints one line for each table. */
export function parseCounts(output: string): ReadonlyMap<string, number> {
  return new Map(output
    .split('\n')
    .map(line => line.trim().split('|'))
    .filter((parts): parts is [string, string] => parts.length === 2 && parts[0] !== '')
    .map(([table, count]) => [table, Number.parseInt(count, 10)] as const));
}

/**
 * The SQL that counts each table's rows, or gives -1 for a table that does not exist yet.
 * query_to_xml runs a query whose text is built at run time, and xpath reads the count out of it.
 */
export function countQuery(): string {
  const rows = placeTables.map(table =>
    `SELECT '${table}', CASE WHEN to_regclass('${table}') IS NULL THEN -1 ELSE ` +
    `(xpath('/row/c/text()', query_to_xml('SELECT count(*) AS c FROM ${table}', false, true, '')))[1]::text::int END`);
  return rows.join(' UNION ALL ');
}
