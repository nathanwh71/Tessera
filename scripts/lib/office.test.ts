// Tests of the office steps (US-35); node --test runs them, with no Windows and no WSL2.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { firewallSteps, networkingSteps } from './office.ts';

test('asks nothing more of a machine already in mirrored mode', () => {
  assert.deepEqual(networkingSteps('mirrored\n'), []);
});

test('tells a machine in NAT mode, or an older WSL, how to switch', () => {
  for (const mode of ['nat', '']) {
    const steps = networkingSteps(mode).join('\n');
    assert.match(steps, /networkingMode=mirrored/);
    assert.match(steps, /wsl --shutdown/);
  }
});

// A port proxy would hide each office machine's address from Nginx, so no step may set one up.
test('opens the one port in both firewalls, and never through a port proxy', () => {
  const steps = firewallSteps('80').join('\n');
  assert.match(steps, /New-NetFirewallRule .*-LocalPort 80 /);
  assert.match(steps, /New-NetFirewallHyperVRule .*-LocalPorts 80$/m);
  assert.doesNotMatch(steps, /portproxy/);
});
