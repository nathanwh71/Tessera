// The steps that open the service to the office on port 80 (US-35), as text; pure functions, so a
// test can read them without Windows.

/**
 * WSL2's mirrored networking gives Ubuntu the Windows machine's own address. Nginx then sees each
 * office machine's own address, which the office-only rule and the limits for each visitor need
 * (S-07, S-30); a Windows port proxy would show every request as coming from Windows itself. The
 * address also survives a restart of WSL2, so no step needs running again (US-35 c3).
 */
export const mirrored = 'mirrored';

/**
 * What to do about WSL2's networking mode, as `wslinfo --networking-mode` reports it. Nothing, when
 * it is mirrored; otherwise the two steps that switch it, which need Windows.
 */
export function networkingSteps(mode: string): readonly string[] {
  if (mode.trim() === mirrored) return [];
  return [
    `WSL2's networking mode is '${mode.trim() || 'unknown'}'; Tessera needs '${mirrored}'. In Windows:`,
    '  1. Add these two lines to %UserProfile%\\.wslconfig:',
    '       [wsl2]',
    `       networkingMode=${mirrored}`,
    '  2. In PowerShell, run "wsl --shutdown", then open Ubuntu again and rerun this script.',
    '',
  ];
}

/**
 * The firewall steps that let the office in on `port`, and the check from another office machine.
 * Mirrored networking passes traffic through Hyper-V's own firewall as well as Windows', so both
 * need a rule. Each rule opens the one port, and the Windows rule only on the office's own networks.
 */
export function firewallSteps(port: string): readonly string[] {
  return [
    `In PowerShell, as administrator, let the office in on port ${port}:`,
    `  New-NetFirewallRule -DisplayName "Tessera ${port}" -Direction Inbound -Protocol TCP -LocalPort ${port} -Action Allow -Profile Domain,Private`,
    `  New-NetFirewallHyperVRule -Name "Tessera${port}" -DisplayName "Tessera ${port}" -Direction Inbound -VMCreatorId "{40E0AC32-46A5-438A-A0B2-2B479E8F2E90}" -Protocol TCP -LocalPorts ${port}`,
    '',
    'Then, from another office machine, check that a tile answers 200:',
    `  curl -I http://<this machine's office name>:${port}/food.cafes/0/0/0.pbf`,
    'and here, that Nginx saw that machine\'s own address, not this one\'s:',
    '  docker logs --tail 5 tessera-nginx',
  ];
}
