// Merges the three test suites' coverage records into one report, and fails when the tests cover
// less of the service than the floors in scripts/lib/coverage.ts.
//
//   node scripts/coverage.ts
//
// npm test and the GitHub build run it after the three suites, each run with the options
// coverageArguments gives, which write their records to coverage/. Open coverage/report/index.html
// to see each line the tests miss.
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { $, echo } from 'zx';
import { coverageFolder, floors, readSummary, reportArguments, shortfalls } from './lib/coverage.ts';

// ReportGenerator, pinned in dotnet-tools.json, merges the records; restore fetches it once.
await $`dotnet tool restore`;
await $`dotnet reportgenerator ${reportArguments(coverageFolder)}`;

const found = readSummary(readFileSync(join(coverageFolder, 'report', 'Summary.json'), 'utf8'));
if (typeof found === 'string') {
  echo(found);
  process.exit(1);
}
echo(`Coverage: ${found.lines} per cent of lines (floor ${floors.lines}), ${found.branches} per cent of branches (floor ${floors.branches}).`);
const short = shortfalls(found, floors);
short.forEach(sentence => echo(sentence));
if (short.length > 0) process.exit(1);
