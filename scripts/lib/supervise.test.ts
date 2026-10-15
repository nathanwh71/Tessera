// Tests of the restart loop (N-29 test 4): fake copies that stop when told, a fake clock, no waiting.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { keepRunning } from './supervise.ts';

// A world whose copies each run for the given milliseconds of fake time, then stop; it asks the loop
// to stop once every run has been used. It records each wait and each line.
function world(runs: readonly number[]) {
  let clock = 0;
  let started = 0;
  const waits: number[] = [];
  const lines: string[] = [];
  return {
    waits,
    lines,
    started: () => started,
    world: {
      start: async () => {
        clock += runs[started] ?? 0;
        started++;
        return 'code 1';
      },
      stopping: () => started >= runs.length,
      wait: async (milliseconds: number) => {
        waits.push(milliseconds);
        clock += milliseconds;
      },
      now: () => clock,
      say: (line: string) => lines.push(line),
    },
  };
}

test('starts a copy again after each stop, doubling the wait after quick failures', async () => {
  const fake = world([100, 100, 100, 100, 100]);
  await keepRunning('port 7800', fake.world);

  assert.equal(fake.started(), 5);
  assert.deepEqual(fake.waits, [1000, 2000, 4000, 8000]);
  assert.equal(fake.lines[0], 'The copy on port 7800 stopped (code 1); starting it again in 1 s.');
});

test('waits a second again after a copy that ran for over a minute, then doubles afresh', async () => {
  const fake = world([100, 100, 90_000, 100, 100]);
  await keepRunning('port 7801', fake.world);

  assert.deepEqual(fake.waits, [1000, 2000, 1000, 2000]);
});

test('starts nothing again once told to stop', async () => {
  const fake = world([100]);
  await keepRunning('port 7800', fake.world);

  assert.equal(fake.started(), 1);
  assert.deepEqual(fake.waits, []);
});
