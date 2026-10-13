// Prints the steps that open port 80 on the Windows machine to the office (US-35). Run it inside
// Ubuntu, in WSL2:
//
//   npm run office-port
//
// WSL2 gives Ubuntu a private network, so the office sees the Windows machine alone. Windows must
// pass port 80 on to Ubuntu, and its firewall must let the office in. Both need an administrator,
// so this script prints the commands for one to run, rather than running them itself.
// The steps need a first run on the office machine; nothing here has run there yet (US-35).
import { $ } from 'zx';
import { officePortSteps } from './lib/office.ts';

const port = process.env['TESSERA_HTTP_PORT'] || '80';
const inWsl = (await $({ nothrow: true })`grep -qi microsoft /proc/version`).exitCode === 0;
if (!inWsl) {
  console.error('Run this inside Ubuntu on WSL2, on the office machine.');
  process.exit(1);
}

const address = (await $`hostname -I`).stdout.trim().split(' ')[0] ?? '';
console.log(officePortSteps(address, port).join('\n'));
