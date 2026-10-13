// Pure decisions for the scripts (ADR 0002, rule 1). Each function reads only its parameters and
// returns a value, so node --test can check it without Docker, a database or a shell.

/** The nine layers' tables, as schema.table: the ids the stories use (US-24). */
export const placeTables: readonly string[] = [
  'food.cafes', 'food.restaurants', 'food.markets',
  'outdoors.parks', 'outdoors.playgrounds',
  'culture.museums', 'culture.libraries',
  'transport.trains', 'transport.buses',
];

/** A result: a value, or the reasons it could not be made. */
export type Checked<T> =
  | { readonly ok: true; readonly value: T }
  | { readonly ok: false; readonly problems: readonly string[] };

/** The settings the start-up script needs, all from the environment (US-00 c4). */
export type StartSettings = {
  readonly ownerPassword: string;
  readonly readerPassword: string;
  readonly publisherPassword: string;
  readonly httpPort: string;
  readonly databasePort: string;
};

/**
 * Reads the start-up settings, naming every missing variable at once, so one failed start tells
 * the maintainer everything to fix.
 */
export function readStartSettings(environment: Readonly<Record<string, string | undefined>>): Checked<StartSettings> {
  const required = ['TESSERA_DB_PASSWORD', 'TESSERA_READER_PASSWORD', 'TESSERA_PUBLISHER_PASSWORD', 'TESSERA_HTTP_PORT'] as const;
  const missing = required.filter(name => !environment[name]);
  if (missing.length > 0) {
    return { ok: false, problems: missing.map(name => `${name}: missing or empty`) };
  }
  return {
    ok: true,
    value: {
      ownerPassword: environment['TESSERA_DB_PASSWORD'] ?? '',
      readerPassword: environment['TESSERA_READER_PASSWORD'] ?? '',
      publisherPassword: environment['TESSERA_PUBLISHER_PASSWORD'] ?? '',
      httpPort: environment['TESSERA_HTTP_PORT'] ?? '',
      // The database answers on this machine alone, so its port may keep PostgreSQL's usual 5432.
      databasePort: environment['TESSERA_DB_PORT'] || '5432',
    },
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
    '--publish', `127.0.0.1:${settings.databasePort}:5432`,
    '--volume', `${volume}:/var/lib/postgresql`,
    '--env', 'POSTGRES_DB=tessera',
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
export function databaseUrl(user: string, password: string, port: string): string {
  return `postgresql://${encodeURIComponent(user)}:${encodeURIComponent(password)}@127.0.0.1:${port}/tessera`;
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
