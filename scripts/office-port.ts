// Prints the steps that open port 80 on the Windows machine to the office (US-35). Run it inside
// Ubuntu, in WSL2:
//
//   npm run office-port
//
// Tessera needs WSL2's mirrored networking, so Ubuntu shares the Windows machine's address and
// Nginx sees each office machine's own address (S-07, S-30). The script checks the mode, then
// prints the firewall steps. Both need Windows and an administrator, so it prints them for one to
// run, rather than running them itself.
import { $ } from 'zx';
import { firewallSteps, networkingSteps } from './lib/office.ts';

// The port Nginx answers on, from .env; 80 when unset, as US-35 asks.
const port = process.env['TESSERA_HTTPPORT'] || '80';
// WSL's Linux kernel names Microsoft in its version line; an ordinary Linux machine's does not.
const inWsl = (await $({ nothrow: true })`grep -qi microsoft /proc/version`).exitCode === 0;
if (!inWsl) {
  console.error('Run this inside Ubuntu on WSL2, on the office machine.');
  process.exit(1);
}

// wslinfo comes with WSL 2.0 and later; an older WSL has no mirrored networking, so 'unknown' fits.
const mode = (await $({ nothrow: true, quiet: true })`wslinfo --networking-mode`).stdout;
const switchFirst = networkingSteps(mode);
console.log([...switchFirst, ...(switchFirst.length > 0 ? ['Once it is mirrored:'] : []), ...firewallSteps(port)].join('\n'));
// A machine that must switch modes first ends with 1, so a caller can tell the job is not done.
process.exit(switchFirst.length > 0 ? 1 : 0);
