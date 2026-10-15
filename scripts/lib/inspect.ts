// Pure parts of npm run inspect, which describes a source of places and prints a starting pipeline
// file for npm run etl (ADR 0009). Each function reads only its parameters, so node --test checks it
// with no GDAL and no Docker (ADR 0002, rule 1).
import { err, ok, type Result } from '../../web/src/shared/result.ts';

/** GDAL's own image, for a machine with no ogrinfo of its own; the tag fixes the version. */
export const gdalImage = 'ghcr.io/osgeo/gdal:alpine-small-3.11.4';

/** One ***layer*** of a source, as GDAL's ogrinfo describes it. */
export type LayerInfo = {
  readonly name: string;
  readonly count: number;
  readonly shape: string;
  readonly system: string | undefined;
  readonly fields: readonly string[];
};

/** What to inspect: the source, and the field to split its one layer by, if any. */
export type InspectPlan = {
  readonly source: string;
  readonly split: string | undefined;
};

/**
 * Reads the command line: SOURCE, then --split FIELD if wanted. Anything else gets the usage line,
 * so a mistyped command gets a plain answer before GDAL or Docker runs.
 */
export function readInspectArguments(args: readonly string[]): Result<InspectPlan, string> {
  if (args.length === 1 && args[0] !== undefined) return ok({ source: args[0], split: undefined });
  if (args.length === 3 && args[0] !== undefined && args[1] === '--split' && args[2]) return ok({ source: args[0], split: args[2] });
  return err('Usage: npm run inspect -- SOURCE [--split FIELD]');
}

/**
 * True for lines and areas, which a ***pipeline file*** marks `lines`. Either kind may mix shapes of
 * one part and of many, and `lines` stores them all as many-part shapes, so one column type holds
 * every row. Points need no such care.
 */
export const isLines = (layer: LayerInfo): boolean => layer.shape.includes('Line') || layer.shape.includes('Polygon');

/**
 * The parts of `ogrinfo -json -so -al`'s answer this script reads. Every field is optional, since
 * GDAL leaves out what a source lacks, such as a coordinate system, and the code must not assume it.
 */
type Described = {
  readonly layers?: readonly {
    readonly name?: string;
    readonly featureCount?: number;
    readonly fields?: readonly { readonly name?: string }[];
    readonly geometryFields?: readonly {
      readonly type?: string;
      readonly coordinateSystem?: { readonly projjson?: { readonly id?: { readonly authority?: string; readonly code?: number | string } } } | null;
    }[];
  }[];
};

/**
 * The layers in the JSON that `ogrinfo -json -so -al` writes, with a plain default for each missing
 * part, so the summary and the starting file never meet undefined.
 */
export function readLayers(json: string): readonly LayerInfo[] {
  const described = JSON.parse(json) as Described;
  return (described.layers ?? []).map(layer => {
    // The first shape column alone: each table the pipeline writes holds one, named geom.
    const shape = layer.geometryFields?.[0];
    const id = shape?.coordinateSystem?.projjson?.id;
    return {
      name: layer.name ?? '',
      count: layer.featureCount ?? 0,
      shape: shape?.type ?? 'None',
      system: id?.authority !== undefined && id.code !== undefined ? `${id.authority}:${id.code}` : undefined,
      fields: (layer.fields ?? []).map(field => field.name ?? ''),
    };
  });
}

/**
 * A plain description of the source: one block for each layer, so the maintainer sees what the
 * source holds before writing a pipeline file. 'en-GB' groups the thousands, as in 1,200.
 */
export function summary(source: string, layers: readonly LayerInfo[]): string {
  return [
    `${source}: ${layers.length} layer${layers.length === 1 ? '' : 's'}`,
    ...layers.flatMap(layer => [
      '',
      layer.name,
      `  ${layer.count.toLocaleString('en-GB')} rows of ${layer.shape}, in ${layer.system ?? 'an unknown system'}`,
      `  Fields: ${layer.fields.length === 0 ? 'none' : layer.fields.join(', ')}`,
    ]),
  ].join('\n');
}

/**
 * A name made plain for a table, such as 'Bus Stops' to 'bus_stops': lower-case letters, digits and
 * _. PostgreSQL then needs no quotes around it. A name may not start with a digit, so one that does
 * gains a leading _, and a name with nothing left becomes 'places'.
 */
export function plain(name: string): string {
  const made = name.toLowerCase().replace(/[^a-z0-9_]+/g, '_').replace(/^_+|_+$/g, '');
  return made === '' ? 'places' : /^\d/.test(made) ? `_${made}` : made;
}

/**
 * The next ***seed script***'s name: the highest number in `existing` plus one, then the source's
 * name. DbUp runs the scripts in the order of their names (ADR 0009), so four digits, padded with
 * zeros, keep '0010' after '0009'.
 */
export function nextScript(existing: readonly string[], source: string): string {
  const highest = Math.max(0, ...existing.map(name => /(?:^|\/)(\d{4})-[^/]*$/.exec(name)?.[1]).map(Number).filter(Number.isFinite));
  // A geodatabase is a folder, often typed with a trailing slash, so the slash goes before the
  // last part of the path gives the name.
  const stem = plain((source.replace(/[/\\]+$/, '').split(/[/\\]/).pop() ?? '').replace(/\.[^.]*$/, '')).replaceAll('_', '-');
  return `${String(highest + 1).padStart(4, '0')}-${stem}.sql`;
}

/** The values GDAL wrote as CSV for `SELECT DISTINCT field`: one a line after the heading, sorted. */
export function readValues(csv: string): readonly string[] {
  return csv.split('\n').slice(1).map(line => line.trim().replace(/^"(.*)"$/, '$1').replaceAll('""', '"')).filter(value => value !== '').toSorted();
}

// OGR SQL quotes a field's name in double quotes, so a name with spaces or capitals still works.
const quoted = (field: string): string => `"${field.replaceAll('"', '""')}"`;

/**
 * The ***filters*** most sources need, in the order they must run. dropEmptyShapes comes first and
 * stops the run, so the maintainer looks at the source before any row is lost, and may relax it to
 * a drop; the later filters need a shape to test. repairShapes mends lines and areas, which may cross themselves; a point
 * cannot. swapCoordinates mends a swapped place before insideArea judges where it lies. tidyText
 * then makes names alike, and a name field, when there is one, is required, since a place with no
 * name has no label on the map.
 */
function filters(layer: LayerInfo): readonly object[] {
  const name = layer.fields.find(field => field.toLowerCase() === 'name');
  return [
    { filter: 'dropEmptyShapes', onFail: 'stop' },
    ...(isLines(layer) ? [{ filter: 'repairShapes' }] : []),
    { filter: 'swapCoordinates' },
    { filter: 'insideArea' },
    { filter: 'tidyText' },
    ...(name === undefined ? [] : [{ filter: 'requireFields', fields: [name] }]),
  ];
}

/**
 * A starting pipeline file: one table for each layer, in the ***schema*** `places`, with the filters most
 * sources need. With `split`, the one layer becomes one table for each value of a field, each picked
 * by a `where`. The maintainer then edits the names, the schemas and the filters.
 */
export function starter(
  source: string,
  output: string,
  layers: readonly LayerInfo[],
  split?: { readonly field: string; readonly values: readonly string[] },
): string {
  const tables = layers.flatMap(layer => {
    const parts = split === undefined
      ? [{ name: plain(layer.name), where: undefined }]
      : split.values.map(value => ({ name: plain(value), where: `${quoted(split.field)} = '${value.replaceAll("'", "''")}'` }));
    return parts.map(part => ({
      table: `places.${part.name}`,
      // A split source has one layer, and GDAL reads a one-layer source whole when no layer is
      // named, so the file names the layer only when there is a choice.
      ...(split === undefined || layers.length > 1 ? { layer: layer.name } : {}),
      ...(part.where === undefined ? {} : { where: part.where }),
      ...(isLines(layer) ? { lines: true } : {}),
      filters: filters(layer),
    }));
  });
  return JSON.stringify({ source, output, tables }, null, 2);
}
