// Prints the steps that make Windows start Tessera when the office machine switches on. Run it
// inside Ubuntu, in WSL2, from the repository:
//
//   npm run boot-task
//
// The task runs scripts/boot.ts, which runs npm start and writes everything it prints to start.log.
// Registering a task that runs at start-up needs an administrator, so this script prints the
// PowerShell for one to run, as npm run office-port does, rather than running it itself.
import { resolve } from 'node:path';
import { $ } from 'zx';
import { dockerSteps, linuxPath, registerSteps } from './lib/boot.ts';

const distro = process.env['WSL_DISTRO_NAME'];
if (!distro) {
  console.error('Run this inside Ubuntu on WSL2, on the office machine.');
  process.exit(1);
}

// npm start needs Docker running before it; systemd starts Docker with Ubuntu once it is enabled.
const docker = dockerSteps((await $({ nothrow: true, quiet: true })`systemctl is-enabled docker`).stdout);
if (docker.length > 0) {
  console.log(docker.join('\n'));
  process.exit(1);
}

// The task finds Node.js and the other programs by the same folders this shell uses now.
const place = {
  distro,
  repo: resolve(import.meta.dirname, '..'),
  node: process.execPath,
  path: linuxPath(process.env['PATH'] ?? ''),
};
console.log(registerSteps(place).join('\n'));
