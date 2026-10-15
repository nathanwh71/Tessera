// Cleans a file of places into the next seed script, with the C# pipeline in tools/Tessera.Etl
// (ADR 0009, US-36):
//
//   npm run etl -- data/parks.pipeline.json
//
// The pipeline itself is C#, and does the work: it reads the source with GDAL, runs the filters,
// writes the script to db/seed and its log to db/seed-logs. This script checks what it can first,
// so a mistake gets a plain answer, then runs the pipeline and ends with its exit code: 0 when it
// wrote the script, 1 when it stopped or failed, 2 when the command or the pipeline file is wrong.
import { existsSync } from 'node:fs';
import { $ } from 'zx';
import { dotnetArguments, nextSteps, readEtlArguments } from './lib/etl.ts';

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
if ((await $({ nothrow: true, quiet: true })`dotnet --version`).exitCode !== 0) {
  console.error('The pipeline needs the .NET 10 SDK, and dotnet is not installed.');
  process.exit(2);
}

const run = await $({ nothrow: true, stdio: 'inherit' })`dotnet ${dotnetArguments(file)}`;
if (run.exitCode === 0) console.log(nextSteps.join('\n'));
process.exit(run.exitCode ?? 1);
