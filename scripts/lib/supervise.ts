// Keeps one copy of the service running (N-29). The effects arrive as parameters, so a test can
// drive the loop with fake copies, a fake clock and no waiting.
import { restartDelay } from './plan.ts';

/** What the loop needs from the world outside it. */
export type Supervision = {
  /** Starts the copy and resolves, with how it ended, when it stops. */
  readonly start: () => Promise<string>;
  /** True once the maintainer has asked everything to stop, as with Ctrl+C. */
  readonly stopping: () => boolean;
  /** Waits the given milliseconds. */
  readonly wait: (milliseconds: number) => Promise<void>;
  /** The time now, in milliseconds. */
  readonly now: () => number;
  /** Writes a line the maintainer reads. */
  readonly say: (line: string) => void;
};

/**
 * Runs the copy until told to stop. A copy that stops on its own starts again after restartDelay;
 * one that ran for a minute counts as healthy, so its next failure waits a second again.
 */
export async function keepRunning(name: string, world: Supervision): Promise<void> {
  let quickFailures = 0;
  while (!world.stopping()) {
    const began = world.now();
    const ended = await world.start();
    if (world.stopping()) return;
    quickFailures = world.now() - began > 60_000 ? 1 : quickFailures + 1;
    const wait = restartDelay(quickFailures);
    world.say(`The copy on ${name} stopped (${ended}); starting it again in ${wait / 1000} s.`);
    await world.wait(wait);
  }
}
