// Turns a file of places into a SQL script that 'Tessera seed' loads with DbUp (ADR 0009, US-24).
//
//   npm run convert -- SOURCE OUTPUT schema.table[:WHERE] ...
//
// SOURCE is any file GDAL reads: GeoJSON, a shapefile, a GeoPackage, CSV and more. Each
// schema.table becomes one table; WHERE, if given, picks its rows. For example, the Macau seed:
//
//   npm run convert -- data/macau-pois.geojson db/seed/0001-macau-places.sql \
//     "food.cafes:layer = 'cafes'" "food.restaurants:layer = 'restaurants'" ...
//
// GDAL's ogr2ogr does the work. The script uses the machine's own ogr2ogr if it has one, and GDAL's
// Docker image if not.
import { existsSync } from 'node:fs';
import { writeFile } from 'node:fs/promises';
import { basename, dirname, resolve } from 'node:path';
import { $ } from 'zx';
import { gdalImage, ogrArguments, readConvertArguments, seedScript, type Part } from './lib/convert.ts';

const read = readConvertArguments(process.argv.slice(2));
if (!read.ok) {
  read.error.forEach(problem => console.error(problem));
  process.exit(1);
}
const plan = read.value;

if (!existsSync(plan.source)) {
  console.error(`${plan.source}: no such file`);
  process.exit(1);
}
// DbUp runs a script once, known by its name, and never again; a changed script would change nothing.
if (existsSync(plan.output)) {
  console.error(`${plan.output} already exists. Give new data a new script, with the next number.`);
  process.exit(1);
}

const local = (await $({ nothrow: true, quiet: true })`which ogr2ogr`).exitCode === 0;
const parts: Part[] = [];
for (const target of plan.targets) {
  // Docker sees the source's folder as /data, read-only; ogr2ogr writes the SQL to its output.
  const run = local
    ? $({ quiet: true })`ogr2ogr ${ogrArguments(resolve(plan.source), target)}`
    : $({ quiet: true })`docker run --rm --volume ${resolve(dirname(plan.source))}:/data:ro ${gdalImage} ogr2ogr ${ogrArguments(`/data/${basename(plan.source)}`, target)}`;
  parts.push({ target, sql: (await run).stdout });
}

const script = seedScript(plan.source, parts);
if (!script.ok) {
  script.error.forEach(problem => console.error(problem));
  process.exit(1);
}
await writeFile(plan.output, script.value);
console.log(`Wrote ${plan.output}. Commit it; 'Tessera seed' runs it once (npm start runs the seed).`);
