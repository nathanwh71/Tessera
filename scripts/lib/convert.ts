// Pure parts of the converter, which turns a file of places into a SQL script for DbUp (ADR 0009).
// Each function reads only its parameters, so node --test checks it with no GDAL and no Docker.
import { err, ok, type Result } from '../../web/src/shared/result.ts';

/** GDAL's own image, for a machine with no ogr2ogr of its own; the tag fixes the version. */
export const gdalImage = 'ghcr.io/osgeo/gdal:alpine-small-3.11.4';

/** One table to make: its schema, its name, and an optional filter that picks its rows from the source. */
export type Target = {
  readonly schema: string;
  readonly table: string;
  readonly where?: string;
};

/** What to convert: one source file, one script to write, and the tables the script makes. */
export type ConvertPlan = {
  readonly source: string;
  readonly output: string;
  readonly targets: readonly Target[];
};

// A plain lower-case name needs no quotes in SQL and reads the same in a tile's address.
const plainName = /^[a-z_][a-z0-9_]*$/;

// DbUp runs scripts in the order of their names, so a four-digit number leads each name.
const scriptName = /(^|\/)\d{4}-[a-z0-9-]+\.sql$/;

/**
 * Reads the command line: SOURCE OUTPUT TABLE..., where each TABLE is schema.table or
 * schema.table:WHERE. Every fault is named at once, so one failed run shows everything to fix.
 */
export function readConvertArguments(args: readonly string[]): Result<ConvertPlan, readonly string[]> {
  const [source, output, ...tables] = args;
  if (source === undefined || output === undefined || tables.length === 0)
    return err(['Usage: npm run convert -- SOURCE OUTPUT schema.table[:WHERE] ...']);

  const targets = tables.map(readTarget);
  const problems = [
    ...(scriptName.test(output) ? [] : [`${output}: name the script NNNN-words.sql, such as 0002-parks.sql, so DbUp runs it in order`]),
    ...targets.flatMap(target => typeof target === 'string' ? [target] : []),
  ];
  return problems.length > 0
    ? err(problems)
    : ok({ source, output, targets: targets.filter((target): target is Target => typeof target !== 'string') });
}

// 'food.cafes:layer = 'cafes'' splits at the first colon; the filter may hold colons of its own.
function readTarget(text: string): Target | string {
  const colon = text.indexOf(':');
  const name = colon < 0 ? text : text.slice(0, colon);
  const where = colon < 0 ? undefined : text.slice(colon + 1);
  const [schema, table, ...rest] = name.split('.');
  if (schema === undefined || table === undefined || rest.length > 0 || !plainName.test(schema) || !plainName.test(table))
    return `${name}: write the table as schema.table, in lower-case letters, digits and _`;
  return where === undefined ? { schema, table } : { schema, table, where };
}

/**
 * ogr2ogr's arguments for one table. PGDump writes SQL rather than loading a database. Each choice:
 * - PG_USE_COPY NO writes INSERT statements; DbUp sends a script as plain SQL and cannot feed COPY.
 * - CREATE_SCHEMA OFF, because the script makes each schema once, at its head.
 * - FID fid: GDAL numbers the rows 1, 2, 3 in the file's order, and the number is the primary
 *   key, which fixes the order of features in a tile (ADR 0006 rule 7).
 * - EPSG:4326 stores every place in longitude and latitude, whatever system the source uses.
 * - DROP_TABLE stays at GDAL's default, IF_EXISTS, so a new script replaces the table whole.
 */
export function ogrArguments(source: string, target: Target): readonly string[] {
  return [
    '--config', 'PG_USE_COPY', 'NO',
    '-f', 'PGDump', '/vsistdout/', source,
    '-nln', target.table,
    '-lco', `SCHEMA=${target.schema}`,
    '-lco', 'CREATE_SCHEMA=OFF',
    '-lco', 'GEOMETRY_NAME=geom',
    '-lco', 'FID=fid',
    '-t_srs', 'EPSG:4326',
    ...(target.where === undefined ? [] : ['-where', target.where]),
  ];
}

/** One table's SQL, as ogr2ogr wrote it. */
export type Part = {
  readonly target: Target;
  readonly sql: string;
};

/**
 * Joins the tables' SQL into one script. DbUp runs the script in one transaction, so all of it
 * takes effect or none does; GDAL's own BEGIN and COMMIT lines would end that transaction early,
 * so they go. A table the filter left empty is refused: most often the filter is mistyped.
 */
export function seedScript(source: string, parts: readonly Part[]): Result<string, readonly string[]> {
  const empty = parts.filter(part => !part.sql.includes('INSERT INTO'));
  if (empty.length > 0)
    return err(empty.map(part => `${part.target.schema}.${part.target.table}: the source gave no rows; check the filter`));

  const schemas = [...new Set(parts.map(part => part.target.schema))];
  return ok([
    `-- Made by 'npm run convert' from ${source}. DbUp runs this script once (ADR 0009).`,
    '-- Never change it after it has run: DbUp would not run it again. Write a new script instead.',
    ...schemas.map(schema => `CREATE SCHEMA IF NOT EXISTS "${schema}";`),
    ...parts.flatMap(part => [
      withoutTransaction(part.sql),
      // Fresh statistics let PostgreSQL plan the tile query well from the first tile.
      `ANALYZE "${part.target.schema}"."${part.target.table}";`,
    ]),
    '',
  ].join('\n'));
}

// GDAL writes one BEGIN line before the table and one COMMIT line at the end. Only those two go:
// a place's own text may hold the same words on a line of their own.
function withoutTransaction(sql: string): string {
  const lines = sql.trimEnd().split('\n');
  const begin = lines.indexOf('BEGIN;');
  const commit = lines.lastIndexOf('COMMIT;');
  return lines.filter((_, index) => index !== begin && index !== commit).join('\n');
}
