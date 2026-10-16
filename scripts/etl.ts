// Cleans a file of places into the next seed script, with the Python pipeline in etl/ (ADR 0009,
// US-36):
//
//   npm run etl -- data/parks.pipeline.json
//
// The pipeline does the work. pyogrio, a Python library built on GDAL, the library that reads map
// files, reads the source; GDAL's own program, ogr2ogr, reads it instead in three cases
// (etl/src/tessera_etl/shell/extract.py). The pipeline then runs the filters and writes the script
// to db/seed and its log to db/seed-logs. This script runs the pipeline in etl/, or in the folder
// TESSERA_ETL_PYTHON names. It checks what it can first, so a mistake gets a plain
// answer, then runs the pipeline and ends with its exit code, the number a program hands back to
// whoever ran it: 0 when the pipeline wrote the script, 1 when it stopped or failed, and 2 when a
// check fails before any row is read, here or in the pipeline.
import { existsSync } from 'node:fs';
import { resolve } from 'node:path';
import { $ } from 'zx';
import { etlProject, nextSteps, readEtlArguments, uvArguments } from './lib/etl.ts';

const read = readEtlArguments(process.argv.slice(2));
if (!read.ok) {
  console.error(read.error);
  process.exit(2);
}
const file = read.value;
if (!existsSync(file)) {
  console.error(`${file}: no such file. npm run inspect -- SOURCE prints one to start from.`);
  process.exit(2);
}
const project = resolve(process.env.TESSERA_ETL_PYTHON ?? etlProject);
if (!existsSync(project)) {
  console.error(`${project}: no pipeline here. Run npm run etl from the repository's root, or set TESSERA_ETL_PYTHON.`);
  process.exit(2);
}
if ((await $({ nothrow: true, quiet: true })`uv --version`).exitCode !== 0) {
  console.error('The pipeline needs uv, Python\'s project tool, and uv is not installed.');
  process.exit(2);
}

const run = await $({ nothrow: true, stdio: 'inherit' })`uv ${uvArguments(project, file)}`;
if (run.exitCode === 0) console.log(nextSteps.join('\n'));
process.exit(run.exitCode ?? 1);
