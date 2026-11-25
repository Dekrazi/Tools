# Changelog

All notable changes to **Tools** (formerly PC Repair Toolkit). Newest first. The same history is built into the app (Knowledge Base > Changelog).

## 5.9

### Workstation Setup

- Machine settings (admin): the **High Performance** plan is created when missing, named for the Windows language ("Wysoka wydajność" / "High performance") with the standard description (no more "Created by Tools"), tuned for performance, and set to display off 15 min on AC / 10 min on battery and sleep never on AC / after 1 hour on battery.
- Machine settings (admin): System Protection turned on with up to 5% disk space for restore points; Startup and Recovery set to no automatic restart and an event written to the system log; "Change brightness based on content" turned off (Windows 11 22H2+, only where the display supports it); Network discovery and File and printer sharing turned on (Domain and Private profiles, language-independent rule groups).
- User settings: Start menu without recent items (recently added apps and recently opened items) and with All apps shown as a list.
- Step 3 is now "Windows Update: turn on options and check for updates": turns on "Receive updates for other Microsoft products", "Get the latest updates as soon as they are available" and automatic optional updates, opens Settings > Windows Update and clicks Check for updates (UsoClient as a fallback). Windows does the downloading and installing in its own window.
- New step 4 "Windows Update: install optional updates": opens Advanced options > Optional updates, expands the groups, ticks every update and clicks Download & install (English and Polish labels).
- App searches: Chrome is searched as just "chrome"; steps renumbered (5 searches, 6 Installed apps).
- Restore machine settings now also restores the display/sleep timeouts on battery and switches the firewall rules it enabled off again. System Protection size and the created power plan are not undone.
- Setup steps print details under each [ OK ] / [FAIL] line.

### Start group and PC overview

- New first group "Start" with "Start here" (PC overview on one screen: computer, user, domain, Windows, uptime, CPU, RAM, disk space, IP, admin status, pending restart, battery) and Workstation Setup.

### Knowledge Base (10 new pages, English and Polish)

- Using Tools; Troubleshooting playbooks (symptom by symptom, **clickable steps that jump to the command**); Hardware bench runbook (condensed from the PC Hardware Troubleshooting Runbook); BIOS keys, Safe Mode and recovery; Windows 11 setup without a Microsoft account (OOBE\BYPASSNRO, start ms-cxh:localonly, registry value, other ways, with the build-dependence caveat); Event ID cheat sheet; Windows error codes (update, general, more stop codes); Ports and IP ranges; Official support and driver sites; Tool licences (commercial use).

### Interface

- Cyan is the default color (the Color menu still switches; nothing is stored).
- Group headers (Start, Diagnostics, Repair, Maintenance, Knowledge Base) are drawn as accent bands with a left bar and a divider; categories are indented and the selected one has an accent mark.
- Searching shows a "Search results" group first: every matching command from all categories in one list with the category in the last column.
- One "Quick" menu with two sections (system info; repair and network) replaces "Quick PowerShell" and "Quick CMD" (everything ran in PowerShell anyway).
- Tooltips on Run, Copy command and Recent.

### Recommended Tools

- Links added for MemTest86, OCCT, Prime95 and 3DMark; Cinebench added (free from Maxon; commercial-use terms not verified).

### Bugs found and fixed

- Taskbar Widgets reported success although the registry write was denied: the read of the missing value returned 0 and the errors were not terminating. All setup writes and reads now use -ErrorAction Stop; the Widgets step falls back to reg.exe and then to the Settings page, and reports which method worked.
- The UI-automation helpers of steps 3 and 4 used the Windows automation types before loading them (found in review; would have failed on first use).
- Step details were printed above the [ OK ] line, which made them look like they belonged to the previous step.
- The "Show saved Wi-Fi password" description named an entry that does not exist.
- Re-checked: bracket and quote balance of every PowerShell snippet, single double-quoted placeholder per prompt, C# 5 syntax, English/Polish parity of every page (same blocks, same links), and that every [[link]] points to a real command.

### Notes

- Build with `build.bat` and report any compiler message. Try the Workstation Setup steps (especially 3 and 4, which click the Settings window) on a spare PC first.

## 5.8

### New: Workstation Setup (Maintenance group; replaces Setup-Workstation-Automatic.ps1 and its .bat launchers)

- Preview setup (no changes): the plan plus the current state of every setting; no admin needed, writes nothing.
- 1. Machine settings (admin)      - one chained run: backup, Search + Widgets policies, High Performance plan (created if missing), display 10 min / no sleep on AC, Microsoft Update + optional updates, Microsoft Update service registration. Numbered [ OK ] / [FAIL] lines and a closing summary.
- 2. User settings (signed-in user) - one chained run: backup, dark mode, wallpaper, Explorer This PC + Compact view, Search / Task View / Widgets hidden (registry, reg.exe, then the Settings-page fallback), clean Explorer restart, read-back verification.
- 3. Install Windows and Microsoft updates (admin) - separate on purpose (long, may need a restart).
- 4. Open app download searches    - Chrome, Adobe Reader, graphics driver for the DETECTED GPU (NVIDIA / AMD / Intel, none for Basic Display; one tab per vendor on hybrid laptops), 7-Zip.
- 5. Open Installed apps           - plain window opener, kept separate so no other window covers it.
- Restore user settings / Restore machine settings (admin).
- Backups: user.json in %LOCALAPPDATA%\ToolsSetupBackup, machine.json in %ProgramData%\ToolsSetupBackup (first backup is kept on re-runs). These are the only files the section creates; the old setup.log is replaced by the console + Save result.
- "Restore point first" applies to steps 1 and 3 (the user step and the restores are excluded).

### Bugs found in the original script, fixed in the port

- Preview demanded administrator rights (the admin check ran before the preview block) and created the backup folder and log although it "changes nothing". The new preview needs no admin and writes nothing.
- Display / sleep timeouts were parsed from the English text "Current AC Power Setting Index", so on Polish Windows they were never backed up and could not be restored. They are now read from the hex values, independent of the language.
- The Widgets fallback searched for the Polish word with a literal "z with dot"; a .ps1 saved without a BOM breaks that character in Windows PowerShell 5.1. The character is now built from its code point.
- Explorer was restarted from the elevated process with a fixed 2-second wait, which could leave a second, elevated Explorer. The port waits for Windows to respawn the shell and starts Explorer only if it did not come back. The same fix is applied to Quick Fixes: Restart Windows Explorer and Rebuild icon and thumbnail cache.
- The final report vanished when the script relaunched itself elevated in a window that closed at the end. Output now stays in the console.
- Restore wrote the old wallpaper path but never redrew it. Restore now refreshes the wallpaper.
- The "Errors" counter was always 0 (nothing logged errors); failed steps are now counted and set the exit code.
- Failed updates were retried on every one of the 3 passes; they are now skipped after the first failure.
- Backups were mixed (user and machine in one file under one profile); they are now separate, and the user step refuses to run when the app runs as a different account than the signed-in user.
- Variable $matches shadowed PowerShell's automatic variable; renamed.
- Not ported: the cleanup of a Task View policy created by an older version of the script (only relevant on PCs prepared with that old version).

### New: colors

- Top bar > Color: green, magenta, cyan, red, white. Backgrounds, selection, buttons, tabs and borders follow the theme; success (green), caution (amber), error / danger (red) and info colors stay fixed so their meaning never changes.
- Changing color (or language) rebuilds the window but now keeps the console text, the last command (Run again / F5), the active tab and the selected category. The color resets to green at the next start (nothing is stored on the PC).
- Minimum window width is now 1200 px (the top bar has one more button); smaller screens are still handled.

### Still true

- One standalone exe; nothing is written to the PC except files you explicitly create and the two setup backups.

## 5.7

### New: Motherboard & BIOS (the BIOS / UEFI category, renamed and extended; sits after CPU & Temperatures)

- Motherboard details (moved here from System Information): manufacturer, model, version, serial, chassis type.
- CPU socket, Memory slots (slots / used / free, maximum capacity, per-slot module, DDR type), Expansion slots (PCIe usage).
- Chipset and platform drivers: vendor system drivers with version and date + a clickable search link "board model + chipset driver download" (manufacturer stripped, like the BIOS and GPU links).
- Onboard devices and their drivers: network, audio, Bluetooth, storage, USB (Microsoft generic drivers hidden).
- Virtualization status: firmware setting, SLAT, hypervisor running (with a note about the False-while-Hyper-V-runs quirk).
- Restart into UEFI setup (shutdown /r /fw, 10 seconds, confirmation first).
- Kept: BIOS version info (with search link), Boot mode and Secure Boot, TPM status.
- 11 commands, all with Polish titles and descriptions. 193 commands in 22 categories in total.

### Bug Fixes (found in the review of 5.6)

- Console buttons: eight buttons in one row overflowed at the smallest window size (a scrollbar covered them). They are now in two rows (reply row and action row).
- Prompts rejected paths containing $ (admin shares such as \\pc\c$). A $ is now allowed and is escaped before it is placed in the PowerShell string, so it can never start a variable or sub-expression. Other restrictions are unchanged.
- "Restore point first" created a restore point even for commands that only read or clean up (show Wi-Fi password, BitLocker keys, Defender scan, Temp cleanup, spooler / Explorer / audio restarts, backups, disk checks, ...). Those 23 commands are now excluded; system-changing ones (network resets, firewall reset, SFC/DISM repairs, Windows Update reset, DLL / app re-registration, power plans, ...) still get one. The elevated helper enforces the same rule.
- Removed a dead start-up log call.
- Re-checked: brackets / quotes of every PowerShell snippet, single double-quoted prompt placeholders, C# 5 syntax, EN / PL coverage (strings, titles, descriptions, reference pages).

### Still true

- One standalone exe; nothing is written to the PC except files you explicitly create.

## 5.6

### New categories

- Quick Fixes: restart Explorer, rebuild icon + thumbnail cache, restart audio services, restart / rebuild Windows Search, re-register Microsoft Store, re-register all apps (Start menu fix), Outlook reset navigation pane / safe mode.
- Domain & Policy: domain + Entra ID join status, applied Group Policies, Group Policy HTML report, forced refresh, secure channel check, time sync, domain controller, current user and groups.
- Changelog (Knowledge Base): the history of every version, EN + PL, built into the exe; the running version is read from the exe and shown at the top.

### New commands

- Security: BitLocker status, key protector IDs, recovery passwords (deliberate, with confirmation), expiring / expired certificates.
- Windows Repair: Full Windows repair (SFC, DISM, SFC) in one go; Windows Update reset (all steps) replaces the three manual steps; Create restore point, List restore points; Activation expiry, product key from BIOS, Office activation status.
- Network: Ping / Trace route / DNS lookup / TCP port test now ask for the host (prefilled with the old targets); Proxy settings, VPN connections, Mapped drives and SMB connections, Test a network path (UNC), Find devices on the local network.
- Events: Find events by ID (7 days), Errors and warnings in the last N hours, Last crash summary (stop code + minidumps).
- CPU: Top 15 by live CPU % (3-second sample).  Battery: Battery wear %, What is blocking sleep.
- System Information: Save a support bundle to a folder (system report, systeminfo, ipconfig, errors, updates, dxdiag, msinfo32).
- Disk: Back up Desktop and Documents to a chosen folder (robocopy).
APP
- Console: errors (stderr) in red; Run again (button + F5); Find in output (Ctrl+F, F3); Copy for ticket (header with PC, user, date).
- Toolbar: "Restore point first" switch (asks Windows for a restore point before every Caution / High-risk command; skipped with a visible note when the command would run without admin rights) and a "Recent" list of the last 10 commands.
- Command-line mode: Tools.exe --run "title" [--param v] [--out file] and --list, no window, exit codes documented in README.
- Every command has a Polish description (rows, tooltip, confirmation dialog, search); the command output stays English.
- Prompt defaults can use %DESKTOP% (the real Desktop folder). Folder / path prompts accept up to 260 characters.
- Standalone: still one exe built by build.bat; it needs no other file and writes nothing to the PC (except files you explicitly create, such as the support bundle or a report).

### Small fixes

- "ACPI thermal zone" no longer recommends HWiNFO64 (not free for commercial use); "List power plans" notes that the active plan is starred.
- Windows Update reset no longer ends with a red exit code because one service was already running.

### Not changed / skipped on purpose

- No custom-catalog file (the exe stays self-contained), no remote execution, no local user / password management, nothing that downloads software.

## 5.5

### Requested changes

- Recommended Tools: FurMark now carries a license line ("NOT free for commercial use - companies need the paid Geeks3D PRO Pack license") in English and Polish. Added MediCat USB (medicatusb.com; its description warns that it bundles third-party tools with their own licenses) and Sysinternals Live (live.sysinternals.com, run the tools straight from the web).
- System Information: removed "Quick system summary", "CPU details" (the Quick PowerShell menu now uses "Full CPU information" from CPU & Temperatures) and "Full system report" (the top-bar button still works; the command lives in a hidden category).
- Starts in English (5.3/5.4 followed the Windows language and started in Polish on a Polish PC).
- Printers: "List installed printers" and "Default printer" now print a clickable search link "printer model + driver download". The model is taken from the driver name (the printer name when the driver is generic), \\server\ prefixes and "(Copy 1)" / "PCL 6" suffixes are removed, duplicates are merged, PDF / XPS / OneNote / fax printers are skipped.

### Duplicates removed or merged

- Same tool twice: msinfo32 (BIOS + Drivers -> one "System Information (GUI)" in System Information), Device Manager (GPU copy removed, Drivers keeps it).
- Covered by another entry: "Recent system events" (Event Logs), "Identify motherboard model and revision" (Motherboard information), "Check Secure Boot state" (Boot mode and Secure Boot), "Find devices with problems" (Devices with errors), "Third-party driver store (pnputil)" (Third-party drivers), "Registry Run keys" x2 (Startup programs), "Open Task Manager (Startup apps tab)" (Startup applications), "Active power plan" (List power plans marks it), "WinSAT CPU only" / "WinSAT memory only" (WinSAT full benchmark), "Physical disks" (merged into "Physical disks and health", which now also shows the bus type).
- Moved to where they belong: "Startup programs" -> Boot & Startup, "List power plans" -> Battery & Power.
- 165 -> 148 visible commands. Kept on purpose: the individual DISM steps, the separate TCP/IP and Winsock resets next to the combined reset, and both IP views (ipconfig /all and Get-NetIPConfiguration).

### Bug fixes

- The status bar still said "History and event log updated" after every command although those tabs are gone; now "Operation finished".
- Category counts in the left tree used a different filter than the list when searching in Polish.
- Removed 52 unused interface strings left over from the removed tabs.
- Category descriptions of System Information and BIOS / UEFI no longer promise removed items.
- Review of all source files and every PowerShell snippet (brackets/quotes balance, C# 5 syntax only): no further problems found.

### Not changed

- Recommended Tools has no top-10 cut (your list of additions replaces that idea).
- Licenses: HWiNFO64, WizTree, OCCT and Recuva are also free for personal use only; only FurMark got the note you asked for.

## 5.4

- Pinned category removed completely (it did not survive a restart, and the tool stores nothing on the PC). Removed the Pin button, the right-click Pin item, the star marker, the pinned="true" catalog attribute and all related strings. The left tree is back to the 19 normal categories.
- BIOS version info: the search link is now "model name + BIOS download" (was "BIOS update"). Any manufacturer text at the start of the model name (e.g. "HP EliteDesk ..." or "ASUSTeK ...") is stripped before searching.
- GPU and driver details: after the adapter details it now prints a clickable search link per real adapter, "GPU name + driver download". Virtual adapters (Microsoft Basic Display, Remote Display, Hyper-V, ...) are skipped. Shown in both languages' UI; command output stays English.

### Not changed

- Recommended Tools still has the full list; cutting it to a top 10 is waiting for your answer about work PCs.

## 5.3

### Requested changes

- Window title is now "Tools" (also the message-box captions and the file description). New "T" icon (dark tile, green letter) in the title bar, taskbar and on the exe (app.ico, built in).
- BIOS / UEFI compacted: "BIOS version and release date" and "Open a web search for a newer BIOS" removed; "BIOS version + link..." renamed "BIOS version info" / "Informacje o wersji BIOS". It shows manufacturer, version, release date and the search link (model name only).
- System Information: "Windows version" removed (OS and computer details has it).
- New Pinned group at the top of the left tree. Pin / Unpin with the star button, the right-click menu, pinned rows show a star. Pins last until the app is closed (also across the EN/PL switch). Permanent pins are baked in with pinned="true" in Catalog.xml.
- Nothing is stored on the PC any more: no language file, no log. Polish Windows starts in Polish, others in English; EN/PL switches for the current run.
- "Find large folders" is now a built-in command: asks for a folder (default: system drive) and lists the 15 largest sub-folders with sizes. No more link.
- SMART details, Bootable memory test, GPU driver clean reinstall and Boot repair reference no longer open web pages; they print short guidance text in the console.
- Defender quick/full scan check that Defender is on before scanning. If another antivirus has taken over (error 0x800106ba) they say so, name the registered antivirus and finish with exit code 1. Defender status also shows the running mode and registered antivirus.
- "Open Windows Security" opens the real app (windowsdefender:). Windows Update, Troubleshooters, Recovery settings, Startup applications, Printer settings and Network reset are now plain "Open" actions: no console, no misleading "No output" message, and they work while another command runs.
- "Startup impact" is now "Startup programs" / "Programy startowe": Name, Enabled (from Task Manager's approval keys), Scope (Current user / All users / Other user) and Command; Local Service / Network Service leftovers (the OneDriveSetup lines) are hidden with a note. The duplicate "Startup command inventory" in Security was removed.
- Recommended Tools: FurMark added to stress testing; HWMonitor, WizTree, Recuva and DDU added.
- "Shortcuts & Tools" is now "Shortcuts" / "Skróty" (Win+R commands and keyboard shortcuts only).
- All of the above in both languages.

### Bug fixes

- Errors in the console showed raw "#< CLIXML <Objs ...>" text. PowerShell's error envelope is now decoded to the message plus "Error ID: ...", without the "At line / + ~~~" noise.
- Folder prompt accepts paths up to 260 characters (was 64) and ending in a backslash (C:\).
- Session pins no longer duplicate commands in the Quick menus.

### Known limits

- Command code, descriptions and guidance text are English only.
- Keeping the exe name PCRepairToolkit.exe; rename the file freely.

## 5.2

### Requested changes

- BIOS web search (both the link in "BIOS version + link..." and "Open a web search for a newer BIOS") now uses only the model name (PC Model / board Product), without the manufacturer.
- Tabs "03 Windows event log", "04 App history" and "05 Notes" removed. Only "01 Tool catalog" and "02 Operations console" remain (Ctrl+1 / Ctrl+2). The Event Logs category on the left is the way to read events; its description now points to the Event Viewer entry.
- Notes and tips removed everywhere (button, page and the tip blocks of command categories). Pure reference pages (Hardware, POST beeps, Recommended tools, Shortcuts) stay.
- Catalog tab: bottom detail pane removed. A slim toolbar now holds Run / Copy command / Open page plus a one-line summary of the selected entry. The full description and the command are in the row tooltip. Right-click a row for Run / Copy command; Ctrl+C copies the command, Enter runs.
- "Open user Temp folder" and "Open Windows Temp folder" replaced by "Clear user Temp folder" and "Clear Windows Temp folder" (confirmation first, reports MB freed, Windows one asks for admin). The Disk category had identical delete commands, so those two were removed.
- "PC Assembly & Repair Checklist" category removed (19 categories, 169 commands).
- Everything done in both languages (EN + PL titles, categories, UI strings).

### Bug fixes / cleanup

- Command output is no longer written to disk. Before, the last 12000 characters of every command's output were logged, which included Wi-Fi passwords from "Show saved Wi-Fi password". The log is now a plain one-line-per-event text file (%LOCALAPPDATA%\PCRepairToolkit\Logs, kept 30 days, no output, no viewer).
- Searching text that appears only in a category's Polish description showed "no commands match"; the filter now matches EN and PL consistently.
- Searching no longer matches hidden tip text in command categories.
- Event Logs category text no longer refers to the removed tab.
- Hardware Reference, POST Beeps and Recommended Tools had no Polish version (PL mode showed English). They are now fully translated.
- NativeEvents.cs (event reader) removed from the build because nothing uses it any more.
- Removed unused dialogs/strings/history code.

## 5.1

### Base

- Kept the tabbed 5.0 look/architecture (catalog, console, event log, history, notes).
- Compared against the standalone v5 (single-file) edition and ported what 5.0 was missing.

### Bug fixes

- GUI tools (winver, msinfo32, Device Manager, Disk Management, dxdiag, msconfig, Task Manager, Resource Monitor, Event Viewer, Services, System Restore, Reliability Monitor, mdsched, wsreset, cleanmgr, sigverif, Power Options, ...) were run through the PowerShell console pipe, which waits until the window is closed. The console stayed "Running", Run was disabled and the app could not be closed. They now open normally (new launch="true" catalog attribute, 24 entries) and keep working while another command runs.
- Notepad / Explorer entries (hosts file, boot log, SFC log, Temp folders) now use Start-Process, so they no longer block the console. The boot log entry explains how to enable logging if the file is missing.
- Hardcoded C: / C:\Windows replaced by %SystemDrive% / %SystemRoot% (CHKDSK, fsutil, WinSAT, Windows Update reset, temp cleanup). Titles no longer say "C:".
- Wi-Fi profile prompt rejected common names (e.g. "Marek's WiFi", brackets). New validation allows them and only blocks characters that could break out of the quoted PowerShell string (" ` $ control chars, curly quotes, trailing backslash). Elevated helper uses the same check.
- Elevated helper now also refuses launch-only and copy-only entries.
- Run button was disabled for ALL entries while a command ran; now only for entries that need the console.
- Event log reader opened publisher metadata once per event (very slow on 2000 rows); now cached per source and released at the end. Events without a message template now show their raw event data.
- Event time parsing no longer throws on odd timestamps.
- Event status line said "last 1 days"; now shows the chosen range ("24 hours", "7 days", ...).
- Clicking a group header ("DIAGNOSTICS", ...) in the left tree no longer drops the selection.
- Window grew on every EN/PL switch on scaled displays; bounds are now restored after DPI scaling and clamped to the screen (min size lowered so it fits 1080p at 150%).
- Defender status no longer asks for UAC (Get-MpComputerStatus works as a normal user).
- Battery report / status, Wi-Fi, adapter and VRAM commands print a clear message instead of an error on PCs without that hardware (desktop, no Wi-Fi, no network).
- Empty "No output" results and failed commands now explain themselves.
- Old history logs are deleted after 30 days.
- app.manifest lists all supported Windows versions and is DPI aware.

### Ported from the standalone edition

- Clickable links in the console (BIOS "find a newer one" search link).
- New BIOS commands: version + search link, open web search for a newer BIOS, boot mode (UEFI/Legacy) + Secure Boot.
- Friendlier TPM and Secure Boot checks.
- Quick system summary (Quick PowerShell menu), readable uptime.
- Full system report: each section is wrapped (one failing section no longer spoils the rest) + Printers section.
- Printers: default printer, drivers, ports, spooler status, clear queue + restart, auto-start spooler, test page, Devices and Printers, troubleshooting notes (EN/PL).
- WinSAT full benchmark prints a readable score table.
- Pending-reboot check counts queued file renames.
- Elapsed timer in the console state line while a command runs.
- Enter key runs the selected command.
- Extra hotkeys: Ctrl+1..5 switch tabs.
- Run commands table gained control printers / printmanagement.msc.

### Ui polish

- High-DPI support, double-buffered grids (no flicker), notes/reference pages show bold labels ("Key: value", "Label - text"), long tree entries get ellipsis.
- Windows Terminal missing -> clear message.

### Known limits

- Command code, descriptions and reference tables are English only (titles/UI/notes are translated).

## 5.0

- Base: tabbed catalog with risk levels, console with a UAC helper over a named pipe, native event-log reader, English and Polish.
