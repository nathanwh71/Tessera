// The steps that open the service to the office on port 80 (US-35), as text; a pure function, so a
// test can read them without Windows.

/**
 * Two ways to let the office reach Ubuntu, as US-35 leaves open: WSL2's mirrored networking, which
 * gives Ubuntu the Windows machine's own address, or a Windows port forward to Ubuntu's private
 * address, which changes when WSL2 restarts. Mirrored networking needs no step after a restart
 * (US-35 c3), so it comes first.
 */
export function officePortSteps(ubuntuAddress: string, port: string): readonly string[] {
  return [
    `First, in Ubuntu, let an ordinary user open port ${port}, so the service can answer on it:`,
    `  echo "net.ipv4.ip_unprivileged_port_start=${port}" | sudo tee /etc/sysctl.d/90-tessera.conf && sudo sysctl --system`,
    '',
    'Option 1, mirrored networking (no step after a restart):',
    '  1. In Windows, add these two lines to %UserProfile%\\.wslconfig, then run "wsl --shutdown":',
    '       [wsl2]',
    '       networkingMode=mirrored',
    `  2. In PowerShell, as administrator, let the office in on port ${port}:`,
    `       New-NetFirewallRule -DisplayName "Tessera ${port}" -Direction Inbound -Protocol TCP -LocalPort ${port} -Action Allow -Profile Domain,Private`,
    '       Set-NetFirewallHyperVVMSetting -Name "{40E0AC32-46A5-438A-A0B2-2B479E8F2E90}" -DefaultInboundAction Allow',
    '',
    'Option 2, a port forward (repeat step 1 after every WSL2 restart):',
    `  1. In PowerShell, as administrator, forward port ${port} to Ubuntu at ${ubuntuAddress}:`,
    `       netsh interface portproxy add v4tov4 listenport=${port} listenaddress=0.0.0.0 connectport=${port} connectaddress=${ubuntuAddress}`,
    `  2. Let the office in on port ${port}:`,
    `       New-NetFirewallRule -DisplayName "Tessera ${port}" -Direction Inbound -Protocol TCP -LocalPort ${port} -Action Allow -Profile Domain,Private`,
    '',
    'Then, from another office machine, check a tile answers 200:',
    `  curl -I http://<this machine's office name>:${port}/food.cafes/0/0/0.pbf`,
  ];
}
