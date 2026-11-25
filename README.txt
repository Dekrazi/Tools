TOOLS 5.9  -  standalone Windows technician toolbox
====================================================
(see README.md for the GitHub description and CHANGELOG.md / CHANGELOG.txt for the history)

BUILD (one time, nothing to install)
  1. Unzip the folder anywhere.
  2. Double-click build.bat  ->  PCRepairToolkit.exe appears in the same folder (rename it as you like).
     It uses the C# compiler already inside Windows (.NET Framework 4.x).
  3. If the build prints errors, copy the error lines and send them to Claude.
  The result is ONE standalone exe (catalog, icon and manifest are inside it). Copy only that file to other PCs;
  it needs nothing else and leaves nothing behind.

NOTHING IS LEFT ON THE PC
  No settings, no log. It always starts in English with the cyan color; the EN / PL buttons and the Color menu
  (green, magenta, cyan, red, white) change language and color for the current run only. The only files it ever creates
  are the ones you ask for (reports, support bundle, profile backup) and the two Workstation Setup backups.

USE
  * Left tree: START, DIAGNOSTICS, REPAIR, MAINTENANCE and KNOWLEDGE BASE groups (34 entries: 19 with commands, 15 pages).
  * Ctrl+F searches every command; a "Search results" group gathers all matches in one list. Esc clears.
  * Enter / double-click runs the selected command; hover a row for the full description and the code.
  * Quick menu: the most used checks and repairs. Recent: the last 10 commands of this session.
  * Console (Ctrl+2): live output with errors in red, Stop, Send reply, Run again (F5), Find (Ctrl+F, F3),
    Copy output, Copy for ticket (PC / user / date header), Save result, Clear.
  * "Restore point first" (toolbar): when ON, a System Restore point is requested before Caution / High-risk commands that
    change the system (read-only and cleanup commands skip it; needs administrator rights).
  * Knowledge base pages: Using Tools, Troubleshooting playbooks (click a step to jump to the command), Hardware bench
    runbook, BIOS keys / Safe Mode / recovery, Windows 11 setup without a Microsoft account, Event IDs, Windows error codes,
    Ports and IP ranges, Official support sites, Tool licences, Hardware reference, POST beeps, Recommended tools,
    Shortcuts, Changelog.

WORKSTATION SETUP (Start group)
  Prepares every new Windows 11 PC the same way. Run the numbered steps in order; "Preview setup" changes nothing.
    1. Machine settings (admin)  - Search / Widgets policies; High Performance plan (named for the Windows language; display
                                   15 min AC / 10 min battery, sleep never AC / 1 h battery); System Protection on with 5%;
                                   no automatic restart after a crash + event logged; brightness-by-content off;
                                   Network discovery and File and printer sharing on.
    2. User settings             - dark mode, wallpaper, Explorer This PC + Compact view, taskbar Search / Widgets / Task View
                                   hidden, Start menu without recent items and All apps as a list. Runs for the SIGNED-IN user
                                   and refuses to run as another account.
    3. Windows Update            - turns on the update options and clicks "Check for updates" in the Windows Update window.
    4. Optional updates          - selects and installs the optional updates in the Settings page (run after step 3 finished).
    5. App download searches     - chrome, Adobe Reader, the graphics driver of the detected GPU, 7-Zip.
    6. Installed apps page.
  Restore user settings / Restore machine settings (admin) put the first saved values back.
  Backups: %LOCALAPPDATA%\ToolsSetupBackup\user.json and %ProgramData%\ToolsSetupBackup\machine.json.
  Steps 3 and 4 click the Windows Settings window (English and Polish labels); a Windows update can change that page.

COMMAND LINE (no window)
  Tools.exe --run "Command title" [--param value] [--out file]
  Tools.exe --list [--out file]
  The title is the English command title (a unique part of it is enough). Output goes to --out, or to stdout when
  redirected. Exit code = the command's own code; 2 unknown / ambiguous title, 3 needs a window, 4 bad --param,
  5 needs administrator (start from an elevated prompt; no UAC prompt in this mode).

SECURITY NOTES
  * The elevated helper accepts only a category id + title of an embedded admin command (plus one validated parameter,
    max 260 characters, no quotes / backtick / control characters; a $ is escaped).
  * SmartScreen / antivirus may warn about a self-built program that can run admin commands. Building it yourself from this
    source is the safe route: every command is in Catalog.xml.
  * "Show saved Wi-Fi password" and "Show BitLocker recovery passwords" reveal secrets - use only on your own or authorised PCs.

FILES
  Program.cs     main window, navigation, search, toolbar, themes menu
  Workspace.cs   colors / themes, controls, the console
  Runner.cs      PowerShell runner, UAC helper, GUI launcher, command-line mode
  Model.cs       catalog model + XML loader      Loc.cs  EN / PL interface strings      Navigation.cs  tab bar
  Catalog.xml    all commands, pages and translations (embedded in the exe)
  app.manifest, app.ico, build.bat
