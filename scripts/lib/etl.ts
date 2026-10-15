// Pure parts of npm run etl, which hands a pipeline file to the C# pipeline in tools/Tessera.Etl
// (ADR 0009). Each function reads only its parameters, so node --test checks it with no .NET and
// no GDAL (ADR 0002, rule 1).
import { err, ok, type Result } from '../../web/src/shared/result.ts';

/** The pipeline's project, beside the service's; Release, so it builds once for every run. */
export const etlProject = 'tools/Tessera.Etl';

/** Reads the command line: one pipeline file, ending in .json. */
export function readEtlArguments(args: readonly string[]): Result<string, string> {
  const [file, ...rest] = args;
  if (file === undefined || rest.length > 0) return err('Usage: npm run etl -- PIPELINE.json');
  if (!file.endsWith('.json')) return err(`${file}: a pipeline file is JSON, such as data/parks.pipeline.json.`);
  return ok(file);
}

/** What dotnet needs to build the pipeline, if it has changed, and run it on the file. */
export const dotnetArguments = (file: string): readonly string[] =>
  ['run', '--project', etlProject, '--configuration', 'Release', '--', file];

/** What the maintainer does once the pipeline has written a script (README, 'Adding places'). */
export const nextSteps: readonly string[] = [
  '',
  'Next:',
  '  1. Read the log and the script, then commit both.',
  '  2. Run npm start, which runs the new script once.',
  '  3. Run npm run publish, so the service serves the new tables.',
];
