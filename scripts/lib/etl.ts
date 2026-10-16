// Pure parts of npm run etl, which hands a pipeline file to the Python pipeline in etl/ (ADR 0009). Each function reads only its parameters, so node --test checks it
// with no Python and no GDAL (ADR 0002, rule 1).
import { err, ok, type Result } from '../../web/src/shared/result.ts';

/** Where the pipeline lives when TESSERA_ETL_PYTHON names no other folder. */
export const etlProject = 'etl';

/** Reads the command line: one pipeline file, ending in .json. */
export function readEtlArguments(args: readonly string[]): Result<string, string> {
  const [file, ...rest] = args;
  if (file === undefined || rest.length > 0) return err('Usage: npm run etl -- PIPELINE.json');
  if (!file.endsWith('.json')) return err(`${file}: a pipeline file is JSON, such as data/parks.pipeline.json.`);
  return ok(file);
}

/**
 * What uv, Python's project tool, needs to run the pipeline on the file. uv installs the pipeline's
 * own packages first if they have changed, so a fresh clone needs no step of its own.
 */
export const uvArguments = (project: string, file: string): readonly string[] =>
  ['run', '--project', project, 'tessera-etl', file];

/** What the maintainer does once the pipeline has written a script (README, 'Adding places'). */
export const nextSteps: readonly string[] = [
  '',
  'Next:',
  '  1. Read the log and the script, then commit both.',
  '  2. Run npm start, which runs the new script once.',
  '  3. Run npm run publish, so the service serves the new tables.',
];
