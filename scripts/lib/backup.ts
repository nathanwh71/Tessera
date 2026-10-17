// Pure parts of the backup and the restore: the backups' names, which ones to keep, and the steps
// that make Windows take a backup each night. Each function reads only its parameters, so
// node --test checks it with no Docker and no database (ADR 0002, rule 1).
import { taskArgument, type BootPlace } from './boot.ts';

/** The folder the backups go in, beside package.json, unless TESSERA_BACKUP_FOLDER names another. */
export const defaultFolder = 'backups';

/** How many backups to keep, unless TESSERA_BACKUP_KEEP says otherwise: two weeks of nightly ones. */
export const defaultKeep = 14;

/** The task's name in Windows' Task Scheduler, so the steps that register and remove it agree. */
export const backupTaskName = 'Tessera backup';

/**
 * A backup's file name, from the moment it starts, in UTC: tessera-2026-10-17T1824Z.dump. The
 * digits run from the year down to the minute, so sorting the names by their letters sorts the
 * backups by age, oldest first.
 */
export function backupName(now: Date): string {
  const stamp = now.toISOString().slice(0, 16).replace(':', '');
  return `tessera-${stamp}Z.dump`;
}

/** True for a name backupName makes, so the clear-out never touches any other file in the folder. */
export const isBackupName = (name: string): boolean => /^tessera-\d{4}-\d{2}-\d{2}T\d{4}Z\.dump$/.test(name);

/** The backups to delete so that only the newest `keep` remain. Files of any other name stay. */
export function backupsToRemove(names: readonly string[], keep: number): readonly string[] {
  const backups = names.filter(isBackupName).toSorted();
  return backups.slice(0, Math.max(0, backups.length - keep));
}

/**
 * Reads TESSERA_BACKUP_KEEP: a whole number of at least 1, since a backup kept for no time at all
 * would be deleted the moment it was taken. Anything else is an error that names the variable.
 */
export function readKeep(text: string | undefined): number | string {
  if (text === undefined || text === '') return defaultKeep;
  return /^\d+$/.test(text) && Number(text) >= 1
    ? Number(text)
    : `TESSERA_BACKUP_KEEP: '${text}' is not a whole number of at least 1`;
}

/**
 * The PowerShell that registers a nightly backup: at 02:00, whether or not anyone signs in. The
 * task runs scripts/backup.ts in Ubuntu, as the start-up task runs scripts/boot.ts, and Windows
 * runs a backup missed while the machine was off as soon as it switches on.
 */
export function backupTaskSteps(place: BootPlace): readonly string[] {
  const quoted = (text: string): string => `'${text.replaceAll("'", "''")}'`;
  return [
    'In PowerShell, run as administrator:',
    '',
    `  $action = New-ScheduledTaskAction -Execute 'C:\\Windows\\System32\\wsl.exe' -Argument ${quoted(taskArgument(place, 'scripts/backup.ts'))}`,
    "  $trigger = New-ScheduledTaskTrigger -Daily -At '02:00'",
    '  $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries',
    '  $account = Get-Credential "$env:USERDOMAIN\\$env:USERNAME"',
    `  Register-ScheduledTask -TaskName ${quoted(backupTaskName)} -Action $action -Trigger $trigger -Settings $settings -User $account.UserName -Password $account.GetNetworkCredential().Password -Force`,
    '',
    `To try it now: Start-ScheduledTask -TaskName ${quoted(backupTaskName)}; then, in Ubuntu: ls ${place.repo}/${defaultFolder}`,
    `To remove it:  Unregister-ScheduledTask -TaskName ${quoted(backupTaskName)}`,
  ];
}
