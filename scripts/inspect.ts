// Describes a source of places, and prints a starting pipeline file for npm run etl (ADR 0009):
//
//   npm run inspect -- data/parks.gdb
//   npm run inspect -- data/macau-pois.geojson --split layer
//
// GDAL's ogrinfo lists each layer: its rows, its shape, its coordinate system and its fields.
// --split FIELD makes one table for each value of FIELD, as the Macau places need for 'layer'.
// The script uses the machine's own GDAL if it has one, and GDAL's Docker image if not.
// Each decision comes from a pure function in lib/inspect.ts; this file only runs commands.
import { existsSync, readdirSync } from 'node:fs';
import { basename, dirname, resolve } from 'node:path';
import { $ } from 'zx';
import { gdalImage, nextScript, readInspectArguments, readLayers, readValues, starter, summary } from './lib/inspect.ts';

const read = readInspectArguments(process.argv.slice(2));
if (!read.ok) {
  console.error(read.error);
  process.exit(2);
}
const { source, split } = read.value;
if (!existsSync(source)) {
  console.error(`${source}: no such file or folder`);
  process.exit(2);
}

// Docker sees the source's folder as /data, read-only, so the image reads the same file.
const local = (await $({ nothrow: true, quiet: true })`which ogrinfo`).exitCode === 0;
const folder = resolve(dirname(source));
const path = local ? resolve(source) : `/data/${basename(source)}`;
const gdal = (program: string, args: readonly string[]) => local
  ? $({ quiet: true })`${program} ${args}`
  : $({ quiet: true })`docker run --rm --volume ${folder}:/data:ro ${gdalImage} ${program} ${args}`;

const layers = readLayers((await gdal('ogrinfo', ['-json', '-so', '-al', path])).stdout);
console.log(summary(source, layers));

let values: readonly string[] | undefined;
if (split !== undefined) {
  const layer = layers[0];
  if (layers.length !== 1 || layer === undefined) {
    console.error('--split needs a source with one layer.');
    process.exit(2);
  }
  if (!layer.fields.includes(split)) {
    console.error(`--split: ${layer.name} has no field '${split}'.`);
    process.exit(2);
  }
  // OGR SQL, GDAL's own small SQL, lists each value of the field once.
  values = readValues((await gdal('ogr2ogr', ['-f', 'CSV', '/vsistdout/', path, '-sql', `SELECT DISTINCT "${split}" FROM "${layer.name}"`])).stdout);
}

const existing = existsSync('db/seed') ? readdirSync('db/seed').filter(name => name.endsWith('.sql')) : [];
const output = `db/seed/${nextScript(existing, source)}`;
console.log('\nA starting pipeline file. Save it beside the data, such as data/NAME.pipeline.json,');
console.log('then set each table\'s schema and name, and its filters, before npm run etl:\n');
console.log(starter(source, output, layers, split === undefined || values === undefined ? undefined : { field: split, values }));
