// Pure parts of the coverage check: where each test suite writes its record, the floors, and the
// verdict. Code coverage is the share of the service's code that the tests run: its lines, and its
// branches, the two ways out of each `if` or `?:`. A part no test runs can break unseen, so the
// build fails when either share drops below its floor. Each function reads only its parameters, so
// node --test checks it with no .NET and no Docker (ADR 0002, rule 1).
// Forward slashes on every system, which ReportGenerator reads on Windows too.
import { join } from 'node:path/posix';

/** The folder each suite writes its record to, and ReportGenerator its report; Git ignores it. */
export const coverageFolder = 'coverage';

/** The three test suites, each a program of its own, in the order npm test runs them (ADR 0010). */
export const suites = ['ArchitectureTests', 'UnitTests', 'IntegrationTests'] as const;

/**
 * The lowest share, in per cent, that the three suites together may cover. On 18 October 2026,
 * after the day's fixes, they covered 96.1 per cent of the lines (648 of 674) and 85.5 per cent
 * of the branches (267 of 312). Each floor sits below, so a timing that varies from run to run
 * cannot fail the build, while a new part with no tests does. JTessera, the Java build, asks 85 and 70.
 * Raise a floor when the figures rise; never lower one to pass a change.
 */
export const floors = { lines: 94, branches: 84 } as const;

/** A share of the service's lines and branches, in per cent. */
export interface Coverage {
  readonly lines: number;
  readonly branches: number;
}

/**
 * What a suite's test program needs after `dotnet run ... --`, to record the lines it runs as a
 * Cobertura file, the XML format ReportGenerator reads, such as coverage/UnitTests.cobertura.xml.
 * The Microsoft.Testing.Extensions.CodeCoverage package gives the program these options.
 */
export function coverageArguments(suite: string, folder: string): readonly string[] {
  return ['--coverage', '--coverage-output-format', 'cobertura', '--results-directory', folder, '--coverage-output', `${suite}.cobertura.xml`];
}

/**
 * What ReportGenerator needs to merge the three records into one: a page to read, in
 * coverage/report/index.html, and Summary.json, from which `readSummary` takes the figures. A line
 * counts as covered when any suite runs it.
 */
export function reportArguments(folder: string): readonly string[] {
  return [`-reports:${join(folder, '*.cobertura.xml')}`, `-targetdir:${join(folder, 'report')}`, '-reporttypes:Html;JsonSummary', '-verbosity:Warning'];
}

/**
 * The share `covered` makes of `total`, in per cent, cut, not rounded, to one decimal place, so a
 * share just under a floor never rounds up to pass it; nothing to cover counts as 100.
 */
const share = (covered: number, total: number): number => (total === 0 ? 100 : Math.floor((covered / total) * 1000) / 10);

/**
 * Reads the figures from ReportGenerator's Summary.json, from its counts rather than its rounded
 * shares, or says what is wrong with the text.
 */
export function readSummary(text: string): Coverage | string {
  try {
    const { summary } = JSON.parse(text) as { summary?: Record<string, unknown> };
    const count = (name: string): number | undefined => (typeof summary?.[name] === 'number' ? summary[name] : undefined);
    const [lines, coverable, branches, total] = ['coveredlines', 'coverablelines', 'coveredbranches', 'totalbranches'].map(count);
    if (lines === undefined || coverable === undefined || branches === undefined || total === undefined) {
      return 'Summary.json lacks the counts of lines and branches.';
    }
    return { lines: share(lines, coverable), branches: share(branches, total) };
  } catch {
    return 'Summary.json is not JSON.';
  }
}

/** One sentence for each share below its floor; none when both reach theirs. */
export function shortfalls(found: Coverage, floor: Coverage): readonly string[] {
  return (['lines', 'branches'] as const)
    .filter(part => found[part] < floor[part])
    .map(part => `The tests cover ${found[part]} per cent of the ${part}, below the floor of ${floor[part]}.`);
}
