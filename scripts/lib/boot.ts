// Pure parts of the start-up task, which starts Tessera when the office machine switches on. Each
// function reads only its parameters, so node --test checks it with no Windows and no WSL2
// (ADR 0002, rule 1).

/**
 * The task's name in Windows' Task Scheduler. One constant, since the steps that register, start
 * and remove the task must all name the same task.
 */
export const taskName = 'Tessera';

/** The log the task writes, beside package.json. */
export const logName = 'start.log';

/** A log larger than this moves aside to start.log.1 at the next start, so it cannot fill the disk. */
export const largestLog = 10 * 1024 * 1024;

export const mustMoveAside = (bytes: number): boolean => bytes > largestLog;

/** The line that opens each start in the log, so a reader can tell one start from the next. */
export const heading = (now: Date): string => `\n=== Started by the start-up task at ${now.toISOString()} ===\n`;

/**
 * The folders the task searches for programs: this shell's own, without the ones WSL2 adds from
 * Windows, under /mnt, and without npm's own for this one run. Windows folders have spaces in
 * their names and the service needs none of them.
 */
export function linuxPath(path: string): string {
  return path.split(':')
    .filter(folder => folder.startsWith('/') && !folder.startsWith('/mnt/') && !folder.includes(' ') && !folder.includes('node_modules'))
    // Each folder once: a folder listed twice only lengthens the line Windows must pass.
    .filter((folder, index, all) => all.indexOf(folder) === index)
    .join(':');
}

// Windows splits a command line at spaces, so a part with a space or a quote goes inside double quotes.
const windowsPart = (part: string): string => /[\s"]/.test(part) ? `"${part.replaceAll('"', '\\"')}"` : part;

/** Where the task finds what it needs: the Ubuntu, the repository, Node.js and the folders. */
export type BootPlace = {
  readonly distro: string;
  readonly repo: string;
  readonly node: string;
  readonly path: string;
};

/**
 * What the task passes to wsl.exe: open this Ubuntu in the repository, set the folders to search,
 * and run scripts/boot.ts with this Node.js. --exec runs the program with no shell between, so no
 * shell's rules about quotes apply.
 */
export function taskArgument(place: BootPlace): string {
  return ['-d', place.distro, '--cd', place.repo, '--exec', '/usr/bin/env', `PATH=${place.path}`, place.node, 'scripts/boot.ts']
    .map(windowsPart)
    .join(' ');
}

// PowerShell keeps text in single quotes as it is, except that a single quote doubles.
const quoted = (text: string): string => `'${text.replaceAll("'", "''")}'`;

/**
 * The PowerShell that registers the task. It runs at start-up, a minute after Windows, whether or
 * not anyone signs in; it may run for ever; and Windows starts it again, up to three times, a
 * minute apart, if it fails. Windows needs the account's password to run it with no one signed in.
 *
 * The minute's delay lets Windows bring up WSL2 and the network first, so Ubuntu and Docker start
 * on a machine that is ready for them. -StartWhenAvailable runs a start missed while the machine
 * was off or busy as soon as it can. The two battery settings keep a laptop on battery from
 * skipping or stopping the task, since Windows otherwise does both to save power.
 */
export function registerSteps(place: BootPlace): readonly string[] {
  return [
    'In PowerShell, run as administrator:',
    '',
    `  $action = New-ScheduledTaskAction -Execute 'C:\\Windows\\System32\\wsl.exe' -Argument ${quoted(taskArgument(place))}`,
    '  $trigger = New-ScheduledTaskTrigger -AtStartup',
    "  $trigger.Delay = 'PT1M'",
    '  $settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries',
    '  $account = Get-Credential "$env:USERDOMAIN\\$env:USERNAME"',
    `  Register-ScheduledTask -TaskName ${quoted(taskName)} -Action $action -Trigger $trigger -Settings $settings -User $account.UserName -Password $account.GetNetworkCredential().Password -Force`,
    '',
    `To try it now: Start-ScheduledTask -TaskName ${quoted(taskName)}; then, in Ubuntu: tail -f ${place.repo}/${logName}`,
    `To remove it:  Unregister-ScheduledTask -TaskName ${quoted(taskName)}`,
    '',
    `Run npm run boot-task again after moving the repository or changing Node.js: the task names both.`,
  ];
}

/**
 * What to do so Docker starts with Ubuntu, from `systemctl is-enabled docker`. Nothing, when it is
 * enabled; otherwise the steps that turn on systemd and enable Docker, since npm start needs it.
 */
export function dockerSteps(state: string): readonly string[] {
  if (state.trim() === 'enabled') return [];
  return [
    'Docker does not start with Ubuntu yet, and npm start needs it. In Ubuntu:',
    '  1. Make sure /etc/wsl.conf holds these two lines, which start systemd with Ubuntu:',
    '       [boot]',
    '       systemd=true',
    '  2. In PowerShell, run "wsl --shutdown", then open Ubuntu again.',
    '  3. Run "sudo systemctl enable docker", then rerun this script.',
  ];
}
