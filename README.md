# Tools

A single-file **Windows technician toolbox**: 203 ready-to-run diagnostic, repair, maintenance and setup commands in 19 categories, a built-in knowledge base, and a one-screen console that shows the output. It is written in C# (WinForms), built with the compiler that ships with Windows, and the result is **one standalone `.exe`** - no installer, no runtime to install, nothing left behind on the PC.

The interface is available in **English and Polish** (titles, descriptions, tooltips, knowledge base). Command output itself stays English.

## Highlights

- **203 commands**, each with a risk level, an admin flag, a description and the exact code that will run. Hover a row to read it, copy it, or run it.
- **Everything runs in one console** with live output, errors in red, *Stop*, *Send reply* (for Y/N prompts), *Run again* (F5), *Find* (Ctrl+F / F3), *Copy for ticket* and *Save result*.
- **UAC done safely** - the app starts as a normal user; commands that need administrator rights are run by a small elevated helper over a named pipe that accepts **only the embedded catalog entries** (category + title + one validated parameter). *Restart as administrator* removes the prompts.
- **Restore point first** switch - asks Windows for a System Restore point before commands that change the system.
- **Workstation Setup** - a numbered, repeatable procedure for preparing every new Windows 11 PC the same way, with backup and restore (see below).
- **Knowledge base** with clickable troubleshooting playbooks, event IDs, error codes, BIOS/boot keys, hardware bench runbook, ports and IP ranges, official support sites, tool licences, Windows 11 setup without a Microsoft account, and a changelog.
- **Command-line mode** for unattended use: `Tools.exe --run "Ping a host" --param 8.8.8.8 --out result.txt`.
- **Five color themes** (cyan by default, green, magenta, red, white) and a language switch; nothing is stored between runs.
- **The app itself never downloads anything.** The only web links are search links and official vendor pages that *you* open (commands you run may of course use the network, for example ping or Windows Update).

## Build

Requirements: Windows 10 or 11 (the Workstation Setup steps target Windows 11). The .NET Framework 4.x compiler (`csc.exe`) is part of Windows.

1. Download or clone this repository.
2. Double-click **`build.bat`**.
3. Copy **`PCRepairToolkit.exe`** wherever you like (rename it freely). It is self-contained: the catalog, icon and manifest are inside.

If the build prints errors, they are plain C# compiler messages - open an issue with the output.

## Use

| Key | Action |
|---|---|
| Ctrl+F | Search every command (results are gathered in one list); on the console tab, search the output |
| Enter / double-click | Run the selected command |
| F5 | Run the last command again |
| F3 | Next match in the console output |
| Ctrl+1 / Ctrl+2 | Switch between the catalog and the console |
| Ctrl+C (in the list) | Copy the command text |
| Esc | Clear the search |

Risk levels: **Low** only reads, **Info** is informational, **Caution** changes settings or deletes data, **High** can break things. Caution and High ask for confirmation.

## Categories

| Group | Category | Commands | What it covers |
|---|---|---:|---|
| Start | Start here | 1 | A one-screen overview of this PC and the first things to check. |
| Start | Workstation Setup | 9 | Prepare every new PC the same way. Run the numbered steps in order; Restore puts the saved settings back. Settings are saved to a backup file the first time (the only files this section creates). |
| Diagnostics | System Information | 8 | Identify Windows, uptime and installed software; open the system report or save a support bundle. |
| Diagnostics | CPU & Temperatures | 5 | Processor details, load and the temperature sensors Windows exposes natively. |
| Diagnostics | Motherboard & BIOS | 11 | Board model and revision, CPU socket, memory and expansion slots, chipset drivers, BIOS, boot mode, Secure Boot, TPM and virtualization. |
| Diagnostics | Disk & Storage | 12 | Inspect physical disks, volumes, free space and filesystem health. Back up important data before repairs. |
| Diagnostics | Memory (RAM) | 6 | Inspect memory modules and utilization; test only when the user can tolerate a restart or downtime. |
| Diagnostics | GPU & Display | 6 | Check graphics hardware, display mode and driver symptoms before reinstalling drivers. |
| Diagnostics | Battery & Power | 11 | Battery health, power plans and sleep/wake diagnostics for laptops and desktops. |
| Diagnostics | Event Logs | 9 | Quick queries for errors, warnings and crashes. For browsing and filtering open Event Viewer (last entry). |
| Repair | Quick Fixes | 9 | One-click fixes for common annoyances: Explorer, icons, audio, search, Store apps and Outlook. |
| Repair | Network Diagnostics | 25 | Test connectivity layer by layer. Avoid network resets until simpler checks identify a likely local stack problem. |
| Repair | Domain & Policy | 8 | Domain / Entra ID join, Group Policy, secure channel and time sync: the usual suspects when a company PC cannot log in or does not get its policies. |
| Repair | Windows Repair | 22 | Repair system components in a controlled order. These scans can take time and require an elevated terminal. |
| Repair | Boot & Startup | 15 | Start with diagnostics and Windows recovery. Boot repair commands are environment-specific and should not be run blindly. |
| Repair | Drivers & Devices | 7 | Identify device errors and export drivers before major changes. Do not install generic driver packs on managed computers. |
| Repair | Printers & Print Spooler | 14 | Check the selected printer, correct port and driver before reinstalling. Printing commands may require elevation. |
| Maintenance | Security & Malware | 17 | Check status before scanning. On company devices, follow endpoint protection policy and do not disable security controls to troubleshoot. |
| Maintenance | Cleanup & Performance | 8 | Prefer built-in cleanup and measurement over “debloat” scripts or blanket performance tweaks. |


Knowledge base pages:

- **Using Tools** — Keys, buttons, risk levels and the command line in one page.
- **Troubleshooting playbooks** — Symptom by symptom: which commands to run, in order. Click a name to jump to the command.
- **Hardware bench runbook** — No power, no picture, freezes and restarts: the bench procedure in short, condensed from the PC Hardware Troubleshooting Runbook.
- **BIOS keys, Safe Mode and recovery** — Which key opens the BIOS or boot menu, and how to reach recovery and Safe Mode.
- **Windows 11 setup without a Microsoft account** — OOBE\BYPASSNRO, start ms-cxh:localonly and other ways to create a local account during setup, with the caveats.
- **Event ID cheat sheet** — The event IDs worth knowing in the System, Application and Security logs.
- **Windows error codes** — Update and servicing codes, general error codes and more blue screen stop codes, with the usual cause.
- **Ports and IP ranges** — Common TCP / UDP ports, private and special IP ranges and subnet masks.
- **Official support and driver sites** — Where to get drivers and BIOS files: the vendors' own sites only.
- **Tool licences (commercial use)** — Which recommended tools are free for commercial use. Checked in November 2026; licences change.
- **Hardware Reference** — Compatibility and connector reminders. The motherboard manual and exact product specifications take precedence.
- **POST Beeps & Debug LEDs** — Use beep codes and board diagnostic LEDs as clues, not universal diagnoses.
- **Recommended Tools** — Download from official project/vendor sites; avoid repackaged installers and generic driver-pack sites.
- **Shortcuts** — Run commands and keyboard shortcuts.
- **Changelog** — What changed in each version of Tools. The running version is shown at the top.

## Workstation Setup

Prepares a new Windows 11 PC the same way every time. Run the numbered steps in order; *Preview setup* changes nothing and writes no file.

1. **Machine settings (admin)** - Search/Widgets policies; High Performance power plan (named for the Windows language; display off 15 min on AC / 10 min on battery, sleep never on AC / after 1 hour on battery); System Protection on with 5% disk space; no automatic restart after a crash with the event logged; "change brightness based on content" off; Network discovery and File and printer sharing on.
2. **User settings (signed-in user)** - dark mode, built-in wallpaper, File Explorer opens to This PC with Compact view, taskbar Search/Widgets/Task View hidden, Start menu without recent items and with *All apps* as a list. It refuses to run as a different account than the signed-in user, so the wrong profile can never be changed.
3. **Windows Update** - turns on the Microsoft-products, latest-updates and optional-updates options and clicks *Check for updates* in the Windows Update window.
4. **Optional updates** - selects and installs the optional updates in the Settings page.
5. **App download searches** - Chrome, Adobe Reader, the graphics driver for the detected GPU, 7-Zip (nothing is downloaded).
6. **Installed apps** - opens the Settings page.

*Restore user settings* and *Restore machine settings* put the first saved values back. The two backup files (`%LOCALAPPDATA%\ToolsSetupBackup\user.json` and `%ProgramData%\ToolsSetupBackup\machine.json`) are the only files the app creates on its own.

> Steps 3 and 4 drive the Windows Settings window through UI Automation (English and Polish labels). They can stop working after a Windows update changes that page.

## Command line

```
Tools.exe --run "Command title" [--param value] [--out file]
Tools.exe --list [--out file]
```

The title is the English command title (a unique part is enough). Output goes to `--out` or to stdout when redirected. Exit code is the command's own code, or **2** unknown/ambiguous title, **3** needs a window, **4** bad `--param`, **5** needs administrator (start from an elevated prompt; there is no UAC prompt in this mode).

## Design notes

- Commands are plain PowerShell stored in `Catalog.xml` and embedded in the exe - every command can be read before it runs.
- User input is only ever placed inside a double-quoted PowerShell string after rejecting quotes, backticks and control characters (a `$` is escaped).
- The elevated helper refuses anything that is not an embedded admin command.
- Nothing is written to the PC except files you create (reports, support bundle, profile backup) and the two Workstation Setup backups.

## Repository layout

| File | Purpose |
|---|---|
| `Program.cs` | main window, navigation, search, themes, toolbar |
| `Workspace.cs` | colors/themes, controls, the console |
| `Runner.cs` | PowerShell runner, UAC helper, GUI launcher, command-line mode |
| `Model.cs` | catalog model and XML loader |
| `Loc.cs` | English/Polish interface strings |
| `Navigation.cs` | tab bar |
| `Catalog.xml` | every command, knowledge-base page and translation (embedded in the exe) |
| `app.manifest`, `app.ico`, `build.bat` | manifest, icon, build script |

## Status and disclaimer

The code was developed with AI assistance. The PowerShell commands are syntax-checked, but not every command has been tested on every Windows build or edition. **Try new commands on a spare PC first**, especially the Caution/High ones and the Workstation Setup steps. Use at your own risk.

The *Recommended Tools* and *Tool licences* pages only link to the vendors' own sites; no third-party software is bundled. Licences change - check them before relying on them in a company.

## License

_Add a license file before publishing (for example MIT) and state it here._
