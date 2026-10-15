// Tests of the start-up task's pure parts; node --test runs them, with no Windows and no WSL2.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { dockerSteps, heading, largestLog, linuxPath, needsRoll, registerSteps, taskArgument } from './boot.ts';

// A made-up machine, with Node.js under nvm as many installs have it, so each test reads plain paths.
const place = { distro: 'Ubuntu-24.04', repo: '/home/ana/tessera', node: '/home/ana/.nvm/versions/node/v26.11.1/bin/node', path: '/usr/bin:/bin' };

test('keeps the Linux folders, once each, and drops those from Windows and npm', () => {
  const path = '/home/ana/tessera/node_modules/.bin:/usr/local/bin:/usr/bin:/mnt/c/Program Files/dotnet:/mnt/c/WINDOWS:/usr/bin:relative';
  assert.equal(linuxPath(path), '/usr/local/bin:/usr/bin');
});

test('opens the Ubuntu in the repository and runs boot.ts with no shell between', () => {
  assert.equal(taskArgument(place),
    '-d Ubuntu-24.04 --cd /home/ana/tessera --exec /usr/bin/env PATH=/usr/bin:/bin /home/ana/.nvm/versions/node/v26.11.1/bin/node scripts/boot.ts');
  assert.match(taskArgument({ ...place, repo: '/home/ana/my maps' }), / --cd "\/home\/ana\/my maps" /);
});

test('registers a task that starts with Windows, runs for ever and tries again', () => {
  const steps = registerSteps({ ...place, repo: "/home/o'neil/tessera" }).join('\n');
  assert.match(steps, /New-ScheduledTaskTrigger -AtStartup/);
  assert.match(steps, /-ExecutionTimeLimit \(\[TimeSpan\]::Zero\)/);
  assert.match(steps, /-RestartCount 3 /);
  assert.match(steps, /-Password \$account\.GetNetworkCredential\(\)\.Password/);
  // A single quote in the argument doubles, so PowerShell keeps the text whole.
  assert.match(steps, /--cd \/home\/o''neil\/tessera /);
  assert.match(steps, /tail -f \/home\/o'neil\/tessera\/start\.log/);
});

test('asks for Docker to start with Ubuntu, unless it already does', () => {
  assert.deepEqual(dockerSteps('enabled\n'), []);
  for (const state of ['disabled\n', '']) assert.match(dockerSteps(state).join('\n'), /systemctl enable docker/);
});

test('moves a large log aside, and marks each start with its time', () => {
  assert.equal(needsRoll(largestLog), false);
  assert.equal(needsRoll(largestLog + 1), true);
  assert.equal(heading(new Date(Date.UTC(2026, 9, 15, 8, 55))), '\n=== Started by the start-up task at 2026-10-15T08:55:00.000Z ===\n');
});
