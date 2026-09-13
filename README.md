# WinGuard Agent

WinGuard Agent is a local-first, administrator-controlled parental-control service for Windows 11. It is intentionally designed **without** keylogging, password/cookie capture, webcam/microphone surveillance, private-message collection, hidden screenshots, or HTTPS decryption.

## Architecture

- **Windows Service / Worker Service:** persistent enforcement across logon/logoff.
- **SQLite:** `%ProgramData%\WinGuardAgent\winguard.db` stores policy, block rules, events, installed-app inventory, settings and agent state.
- **Process blocker:** low-frequency process scans terminate administrator-blocked executables and log only the process/user/reason event.
- **Installed-app inventory:** reads normal Windows uninstall registry entries; it never deletes software automatically.
- **DNS filter:** optional local UDP DNS forwarder on `127.0.0.1:53`. When the friendly block page is enabled, blocked IPv4/HTTP lookups are mapped to `127.0.0.1`; other blocked DNS queries are denied. It does not inspect TLS/HTTPS content.
- **Friendly block page:** loopback-only HTTP page with a text/ASCII cat and administrator-defined message such as `Go back to work 😼`. No external images, tracking, or browser-data collection.
- **Chrome policy manager:** writes machine Chrome Enterprise policies under `HKLM\SOFTWARE\Policies\Google\Chrome` for URL lists, extension controls, Safe Browsing and Safe Search.
- **Windows network hardening:** optional administrator-controlled DNS adapter configuration and machine proxy policy. Standard users cannot write the machine policy/configuration area.
- **ACL hardening:** `%ProgramData%\WinGuardAgent` gives full control only to SYSTEM/Administrators and read/execute to Users.

## Important scope notes

This baseline implements practical local enforcement without a kernel driver. Windows Firewall is used where appropriate for the local DNS service, while **domain** decisions are made from DNS names rather than static IP-only lists. For high-assurance all-browser filtering against applications that use hard-coded DNS, DNS-over-HTTPS, or their own network stack, a production version should add a signed Windows Filtering Platform (WFP) callout/driver or an approved enterprise filtering provider. This project deliberately does not MITM/decrypt HTTPS.

`dns-protection` is **off by default** because changing adapter DNS is a network-wide behavior that should be explicitly approved by the administrator. Chrome URL policies still work when DNS protection is off.

## Requirements

- Windows 11 x64
- Visual Studio 2022 with .NET 8 SDK, or .NET 8 SDK from the command line
- Administrator rights for install, uninstall and policy changes
- For MSI: WiX Toolset SDK 5 via NuGet (the `.wixproj` restores it)

The project pins `Microsoft.Data.Sqlite` 8.0.31 and `Microsoft.Extensions.Hosting.WindowsServices` 8.0.1 for the .NET 8 target.

## Build

```powershell
.\build.ps1
```

## Publish a self-contained x64 executable

```powershell
.\publish.ps1
```

Output:

```text
publish\WinGuardAgent.exe
publish\WinGuardAdmin.exe
```

Equivalent direct commands:

```powershell
dotnet publish .\WinGuardAgent\WinGuardAgent.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish
dotnet publish .\WinGuardAdmin\WinGuardAdmin.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish
```

## Install manually

Open **Windows Terminal / PowerShell as Administrator**:

```powershell
.\publish\WinGuardAgent.exe install
.\publish\WinGuardAgent.exe status
```

The installer command creates an auto-start service and sets Service Control Manager recovery to restart after crashes.

To remove the service:

```powershell
.\publish\WinGuardAgent.exe uninstall
```

Uninstall intentionally retains `%ProgramData%\WinGuardAgent` so policy/event history is not silently destroyed. An administrator can remove that directory afterward if desired.


## WinGuard Admin graphical dashboard

WinGuard now includes a WPF administrator frontend, `WinGuardAdmin.exe`, so normal parental-control management does not require Terminal. The dashboard provides:

- Windows service status/start/stop
- blocked and allowed website rules
- blocked applications
- category controls
- DNS/Chrome/SafeSearch/extension settings
- friendly blocked-page message editing
- recent WinGuard events

### Administrator-password protection

`WinGuardAdmin.exe` has a Windows `requireAdministrator` manifest and also has its own Windows-account unlock gate. On the first successful unlock, WinGuard stores only the administrator account's **Windows SID** in `%ProgramData%\WinGuardAgent\authorized-admin.sid`. It never stores the administrator password or a password hash.

Every later unlock calls the Windows `LogonUser` API to validate the **current Windows password** and confirms that:

1. the credentials belong to a Windows administrator, and
2. the account SID matches the administrator originally authorized for this WinGuard installation.

This means changing the Windows account password automatically changes the password required to unlock WinGuard. The actual account password is required; a Windows Hello PIN is not treated as the account password by this validation method. For a local account use a name such as `.\Parent`. For a Microsoft account, Windows commonly accepts `MicrosoftAccount\parent@example.com`.

Because the GUI performs privileged policy changes, Windows UAC also protects launch/elevation. A standard child account cannot open the management dashboard without administrator authorization.

Launch after publishing:

```powershell
.\publish\WinGuardAdmin.exe
```

## CLI examples

```powershell
WinGuardAgent.exe status
WinGuardAgent.exe block-domain example.com Custom
WinGuardAgent.exe allow-domain school.example.com
WinGuardAgent.exe unblock-domain example.com
WinGuardAgent.exe block-app example.exe Custom
WinGuardAgent.exe unblock-app example.exe
WinGuardAgent.exe import-blocklist .\blocklist.example.json
WinGuardAgent.exe events 100
WinGuardAgent.exe policy
WinGuardAgent.exe dns-protection on
WinGuardAgent.exe block-page on
WinGuardAgent.exe block-message "Go back to work 😼"
WinGuardAgent.exe dns-protection off
WinGuardAgent.exe category CloudGaming on
WinGuardAgent.exe category SocialMedia off
WinGuardAgent.exe extension-block abcdefghijklmnopqrstuvwxyzabcdef
WinGuardAgent.exe extension-allow abcdefghijklmnopqrstuvwxyzabcdef
```

All CLI commands except `help` require elevation.

### Categories

- `WebProxiesAnonymizers`
- `VpnTunneling`
- `AdultContent`
- `Gambling`
- `MalwarePhishing`
- `CloudGaming`
- `SocialMedia`
- `Custom`

Categories are policy-controlled and can be enabled/disabled independently. Domain entries remain configurable data, not hard-coded immutable restrictions. The included JSON blocklist importer accepts a source, version, generated timestamp, and entries; a future authenticated dashboard/updater can feed the same format after signature/transport validation.

## DNS protection

Enable it from an elevated terminal:

```powershell
WinGuardAgent.exe dns-protection on
WinGuardAgent.exe block-page on
WinGuardAgent.exe block-message "Go back to work 😼"
```

This points active adapters at the local WinGuard DNS listener. Disabling resets Windows DNS server addresses back to the adapter defaults (typically DHCP):

```powershell
WinGuardAgent.exe dns-protection off
```

If another local DNS program already owns UDP port 53, leave WinGuard DNS protection off or reconfigure the competing service.

## Friendly blocked-site page

WinGuard can show a local friendly page for blocked **plain HTTP** sites instead of a generic DNS failure. The default page says `Nice try 😹` and `Go back to work 😼`, shows the requested hostname and the matching policy category, and provides a Go Back button. The page is generated locally by the service and loads no external images/scripts.

```powershell
WinGuardAgent.exe block-page on
WinGuardAgent.exe block-message "Go back to work 😼"
```

The listener binds only to `127.0.0.1:80`. With DNS protection enabled, blocked A-record requests are answered with `127.0.0.1`, so ordinary HTTP navigation lands on the WinGuard page.

**HTTPS limitation:** WinGuard intentionally does not impersonate blocked HTTPS sites or install a TLS interception certificate. Doing that would require decrypting/intercepting secure traffic. Therefore HTTPS sites continue to use Chrome's administrator-blocked/security fallback rather than showing the local HTTP page. A future managed Chrome companion extension can provide a custom HTTPS navigation page without decrypting traffic.

## Chrome policies

The service applies machine-level Chrome Enterprise policy values. URL block/allow rules are regenerated from the SQLite domain table. Chrome normally reloads enterprise policy without reading browser data.

Extension lists are administrator-managed. To prevent all extensions except explicitly allowed IDs, set `blockUnauthorizedExtensions` to `true` in the stored config through a future admin UI/API (or extend the CLI), then add allowed IDs. The current CLI also supports direct `extension-block`/`extension-allow` list updates.

Check effective Chrome policy at `chrome://policy` while signed in locally. WinGuard never reads Chrome passwords, cookies, browsing form contents, or private messages.

## Application detection

The service inventories uninstall entries from 32-bit and 64-bit registry views. Fields stored are:

- application name
- publisher
- version
- install timestamp when the installer provides it
- install location/path when available
- first/last seen timestamps

A `new_application_detected` event is created the first time an AppKey appears. No automatic uninstall is performed.

## Security model

- Service runs as LocalSystem.
- Child account should remain a **standard Windows user**.
- ProgramData policy/database/config files are ACL-protected.
- Policy-changing CLI requires an elevated administrator token.
- No administrator password is stored.
- Machine Chrome policies and machine proxy policy require administrator/SYSTEM rights.
- Standard users normally cannot alter Windows Firewall or adapter DNS configuration.

For stronger tamper resistance in a production deployment, code-sign the executable/MSI, enable Windows Defender Application Control/AppLocker as appropriate, and protect the service/installer with normal enterprise software deployment controls. Do not rely on obscurity.

## Event types

The baseline emits these event names where the corresponding local signal exists, and reserves browser-only signals that need future browser/API feedback:

- `agent_started`
- `agent_stopped`
- `blocked_domain`
- `blocked_application`
- `blocked_extension` (reserved for future browser/API feedback)
- `policy_changed`
- `new_application_detected`
- `proxy_attempt_detected` when a blocked application is categorized `WebProxiesAnonymizers`
- `vpn_application_detected` when a blocked application is categorized `VpnTunneling`

No keystrokes or private message contents are collected.

## Future dashboard/API design

Keep the service local-first. Add a separate authenticated API host component rather than exposing SQLite directly:

```text
Phone
  ↓ HTTPS
WinGuard Cloud API
  ↓ mutually authenticated / device-bound connection
Windows 11 WinGuard Agent
  ↓
Local SQLite + Windows policies
```

Recommended future design:

1. Device enrollment creates an asymmetric device key in Windows CNG/TPM when available.
2. Cloud issues short-lived credentials; never store the parent password on the PC.
3. Agent opens an outbound TLS connection (WebSocket or long polling), avoiding inbound port exposure.
4. Every remote policy change is signed/authenticated, validated, versioned and written to the local policy repository.
5. Local policy remains authoritative if the cloud is unavailable.
6. Remote commands are limited to documented parental-control actions and produce auditable `policy_changed` events.

## Installer

`Installer/Package.wxs` is a WiX v5 per-machine installer definition. First publish the executable, then build:

```powershell
.\Installer\build-installer.ps1
```

The MSI installs both `WinGuardAgent.exe` and `WinGuardAdmin.exe` to Program Files, registers an auto-start LocalSystem service, starts it, and requires machine-level installation privileges. ProgramData/SQLite creation occurs on the service's first run.

## Troubleshooting

**Service will not start**

```powershell
sc.exe query WinGuardAgent
Get-Content "$env:ProgramData\WinGuardAgent\logs\agent-$(Get-Date -Format yyyyMMdd).log" -Tail 100
```

Also check Event Viewer → Windows Logs → Application.

**DNS filtering does not work**

- Confirm `WinGuardAgent.exe policy` shows `dnsProtectionEnabled: true`.
- Confirm the service was restarted after enabling it.
- Run `Get-NetUDPEndpoint -LocalPort 53` as Administrator and make sure WinGuard owns the local listener.
- Run `Get-DnsClientServerAddress` and confirm active adapters point to `127.0.0.1`.
- Another VPN/security product can override DNS; use administrator policy to decide which product controls DNS.

**Chrome rule does not appear**

- Run `chrome://policy` and click Reload policies.
- Confirm the rule exists under `HKLM\SOFTWARE\Policies\Google\Chrome`.
- Make sure Chrome is the standard desktop build that supports enterprise policies.

**Blocked application keeps returning**

WinGuard intentionally does not uninstall software. It terminates matching processes while the policy exists. Remove the software through Windows Settings if the administrator chooses to do so.

## Development notes

The code is separated into `Agent`, `Storage`, `Network`, `Apps`, `Chrome`, `Security`, and `Cli` namespaces so the future REST/WebSocket adapter can call the same repositories and policy engine without changing enforcement logic.

---

## Build in the browser with GitHub Actions

If you do not want to install Visual Studio or the .NET SDK locally, this project now includes:

```text
.github/workflows/build-windows.yml
```

Upload the project to a **private GitHub repository**. GitHub Actions will use a Windows cloud runner to publish the self-contained x64 executables and build the WiX MSI installer. The finished workflow artifact is named **WinGuard-Installer**.

See [`BUILD_ON_GITHUB.md`](BUILD_ON_GITHUB.md) for the browser-only steps.

## Enforcement hardening

WinGuard treats the LocalSystem Windows service as the enforcement owner. The controlled child account is expected to remain a standard Windows user. The service periodically verifies administrator-controlled proxy, DNS, and Chrome/Edge machine policies and restores them if they are changed, recording a `tamper_detected` event.

Website enforcement is layered: Chrome/Edge `URLBlocklist`/`URLAllowlist`, optional loopback DNS filtering, Windows networking/firewall integration, and process blocking as a secondary control. Chrome/Edge DNS-over-HTTPS is disabled by machine policy when WinGuard DNS protection is enabled so managed browsers continue using the Windows resolver.

WinGuard does not inspect or decrypt HTTPS traffic. WFP/Windows Firewall enforcement works at network/connection layers; domain classification remains in DNS/browser policy. A future production WFP callout driver should only be added if requirements cannot be met with the built-in filtering engine, and would require normal Windows driver signing/deployment practices.
