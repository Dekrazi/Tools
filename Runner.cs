using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace RepairToolkit
{
    public static class CommandRunner
    {
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);

        // The value is placed inside a double-quoted PowerShell string, so quotes, backticks and control characters are refused;
        // a $ is allowed (admin shares such as \\pc\c$) and is escaped by Build so it can never start a variable or sub-expression.
        public static bool ValidParam(string v)
        {
            if (v == null || v.Length == 0 || v.Length > 260) return false;
            foreach (char c in v)
                if (c < ' ' || c == '"' || c == '`' || (c >= '\u2018' && c <= '\u201F')) return false;
            return true;
        }

        // GUI tools (Device Manager, msinfo32, ...) are started normally instead of through the console, so they never block it.
        public static void Launch(Entry e)
        {
            string file = e.Code.Trim(), args = "";
            int sp = file.IndexOf(' ');
            if (sp > 0) { args = file.Substring(sp + 1); file = file.Substring(0, sp); }
            var psi = new ProcessStartInfo(file, args) { UseShellExecute = true };
            if (e.Admin && !IsAdmin()) psi.Verb = "runas";
            Process.Start(psi);
        }

        // Optional first step of a risky command: ask Windows for a System Restore point (needs administrator rights).
        const string RestorePointSnippet = "try { Checkpoint-Computer -Description 'Tools restore point' -RestorePointType MODIFY_SETTINGS -ErrorAction Stop; 'Restore point requested (Windows skips it if one was created in the last 24 hours).' } catch { 'Restore point skipped: ' + $_.Exception.Message }\r\n";

        public static string Build(Entry e, string param, bool restorePoint = false)
        {
            string code = (e.Param != null && param != null) ? e.Code.Replace(e.Param, param.Replace("$", "`$")) : e.Code;
            return restorePoint ? RestorePointSnippet + code : code;
        }

        // Default text of a prompt: environment variables and %DESKTOP% (the real Desktop folder, also when redirected) are expanded.
        public static string ExpandDefault(string def)
        {
            if (string.IsNullOrEmpty(def)) return "";
            return Environment.ExpandEnvironmentVariables(def.Replace("%DESKTOP%", Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));
        }

        public static string Wrap(string command)
        {
            return "$ErrorActionPreference = 'Continue'; $ProgressPreference = 'SilentlyContinue'; $global:LASTEXITCODE = 0; " +
                   "try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }; " +
                   "try { & {\r\n" + command + "\r\n} | Out-String -Width 250 -Stream; " +
                   "if ($global:LASTEXITCODE -ne 0) { exit $global:LASTEXITCODE } } " +
                   "catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }";
        }

        public static string Encode(string script) { return Convert.ToBase64String(Encoding.Unicode.GetBytes(script)); }
        public static string Pack(string value) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(value)); }
        public static string Unpack(string value) { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }

        public static string PowerShellPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"); }
        }

        public static Process CreateProcess(string command)
        {
            var psi = new ProcessStartInfo(PowerShellPath, "-NoLogo -NoProfile -OutputFormat Text -EncodedCommand " + Encode(Wrap(command)));
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;
            psi.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new Process { StartInfo = psi };
        }

        // PowerShell wraps error text in a CLIXML envelope when started with -EncodedCommand and redirected streams.
        // Plain stderr is passed through live; an envelope is collected and shown as readable text (message + error id).
        public static async Task PumpErr(StreamReader reader, Action<string> append)
        {
            char[] buffer = new char[2048];
            int count;
            bool decided = false, xml = false;
            var held = new StringBuilder();
            while ((count = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                string chunk = new string(buffer, 0, count);
                if (decided) { if (xml) held.Append(chunk); else append(chunk); continue; }
                held.Append(chunk);
                string t = held.ToString().TrimStart();
                if (t.Length < 9 && "#< CLIXML".StartsWith(t, StringComparison.Ordinal)) continue;   // not enough text to decide yet
                decided = true;
                xml = t.StartsWith("#< CLIXML", StringComparison.Ordinal);
                if (!xml) { append(held.ToString()); held.Length = 0; }
            }
            if (xml) { string text = DecodeClixml(held.ToString()); if (text.Length > 0) append(text); }
            else if (!decided && held.Length > 0) append(held.ToString());
        }

        public static string DecodeClixml(string raw)
        {
            var sb = new StringBuilder();
            foreach (Match m in Regex.Matches(raw, "<S S=\"(?:Error|Warning)\">(.*?)</S>", RegexOptions.Singleline))
                sb.Append(m.Groups[1].Value);
            string text = System.Net.WebUtility.HtmlDecode(sb.ToString());
            text = Regex.Replace(text, "_x([0-9A-Fa-f]{4})_", mm => ((char)Convert.ToInt32(mm.Groups[1].Value, 16)).ToString());
            var lines = new List<string>();
            bool skipping = false;
            foreach (string line in text.Replace("\r", "").Split('\n'))
            {
                string t = line.Trim();
                if (t.StartsWith("At line:", StringComparison.Ordinal)) { skipping = true; continue; }
                if (t.StartsWith("+", StringComparison.Ordinal))
                {
                    skipping = true;
                    int i = t.IndexOf("FullyQualifiedErrorId : ", StringComparison.Ordinal);
                    if (i >= 0) lines.Add("Error ID: " + t.Substring(i + 24).Trim());
                    continue;
                }
                if (skipping && t.Length > 0 && line.StartsWith(" ", StringComparison.Ordinal)) continue;   // wrapped CategoryInfo line
                skipping = false;
                if (t.Length == 0) continue;
                lines.Add(line);
            }
            return lines.Count == 0 ? "" : string.Join("\n", lines.ToArray()) + "\n";
        }

        public static async Task Pump(StreamReader reader, Action<string> append)
        {
            char[] buffer = new char[2048];
            int count;
            while ((count = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                append(new string(buffer, 0, count));
        }

        public static bool IsAdmin()
        {
            try
            {
                using (var id = WindowsIdentity.GetCurrent())
                    return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception) { return false; }
        }

        public static void KillTree(int pid)
        {
            try
            {
                var k = new ProcessStartInfo("taskkill", "/T /F /PID " + pid);
                k.CreateNoWindow = true;
                k.UseShellExecute = false;
                using (var kp = Process.Start(k)) { kp.WaitForExit(4000); }
            }
            catch (Exception)
            {
                try { Process.GetProcessById(pid).Kill(); } catch (Exception) { }
            }
        }

        public static void ResetGraphics()
        {
            byte[] keys = { 0x5B, 0x11, 0x10, 0x42 };
            int pressed = 0;
            try
            {
                foreach (byte k in keys) { keybd_event(k, 0, 0, UIntPtr.Zero); pressed++; }
            }
            finally
            {
                for (int i = pressed - 1; i >= 0; i--) keybd_event(keys[i], 0, 2, UIntPtr.Zero);
            }
        }

        // Runs inside the elevated helper. Only an embedded catalog entry (looked up by category id + title) can be executed.
        public static int ElevatedHelper(string pipeName)
        {
            if (!pipeName.StartsWith("PCRepair-", StringComparison.Ordinal) || pipeName.Length != 41) return 2;
            try
            {
                using (var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    pipe.Connect(15000);
                    var read = new StreamReader(pipe, new UTF8Encoding(false), false, 4096, true);
                    var write = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true);
                    write.AutoFlush = true;
                    object sync = new object();
                    Action<string, string> send = (kind, value) => { lock (sync) write.WriteLine(kind + Pack(value)); };

                    string key = read.ReadLine();
                    if (key == null) return 2;
                    var decoded = Unpack(key).Split(new[] { '\n' }, 4);
                    if (decoded.Length < 2) return 2;
                    var entry = Catalog.Load().SelectMany(c => c.Entries).FirstOrDefault(e => e.Category.Id == decoded[0] && e.Title == decoded[1] && e.Admin && !e.Launch && !e.CopyOnly);
                    if (entry == null) return 2;
                    string param = (decoded.Length >= 3 && decoded[2].Length > 0) ? decoded[2] : null;
                    bool rp = decoded.Length == 4 && decoded[3] == "1" && (entry.Risk == "caution" || entry.Risk == "danger") && !entry.NoRp;
                    if (entry.Param != null && !ValidParam(param)) return 2;

                    using (var process = CreateProcess(Build(entry, param, rp)))
                    {
                        process.Start();
                        send("A", "");
                        Task.Run(async () =>
                        {
                            try
                            {
                                string line;
                                while ((line = await read.ReadLineAsync().ConfigureAwait(false)) != null)
                                {
                                    if (line.StartsWith("I"))
                                    {
                                        process.StandardInput.WriteLine(Unpack(line.Substring(1)));
                                        process.StandardInput.Flush();
                                    }
                                    else if (line.StartsWith("X"))
                                    {
                                        KillTree(process.Id);
                                    }
                                }
                            }
                            catch (Exception) { }
                            try { process.StandardInput.Close(); } catch (Exception) { }
                        });
                        Task.WaitAll(Pump(process.StandardOutput, s => send("O", s)), PumpErr(process.StandardError, s => send("E", s)));
                        process.WaitForExit();
                        send("R", process.ExitCode.ToString());
                        return process.ExitCode;
                    }
                }
            }
            catch (Exception) { return 3; }
        }
    }

    public sealed class RunSession : IDisposable
    {
        Process process, helper;
        NamedPipeServerStream pipe;
        StreamWriter writer;
        readonly object sync = new object();
        bool disposed;
        public bool Ready { get; private set; }
        public bool Stopped { get; private set; }

        public async Task<int> Run(Entry e, string param, Action<string> append, Action<string> appendErr, Action ready, bool restorePoint)
        {
            if (e.Admin && !CommandRunner.IsAdmin()) return await Elevated(e, param, append, appendErr, ready, restorePoint);
            process = CommandRunner.CreateProcess(CommandRunner.Build(e, param, restorePoint));
            process.Start();
            Ready = true;
            ready();
            await Task.WhenAll(CommandRunner.Pump(process.StandardOutput, append), CommandRunner.PumpErr(process.StandardError, appendErr));
            await Task.Run(() => process.WaitForExit());
            return process.ExitCode;
        }

        async Task<int> Elevated(Entry e, string param, Action<string> append, Action<string> appendErr, Action ready, bool restorePoint)
        {
            string name = "PCRepair-" + Guid.NewGuid().ToString("N");
            var security = new PipeSecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User, PipeAccessRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
            pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 8192, 8192, security);
            var connection = Task.Factory.FromAsync(pipe.BeginWaitForConnection, pipe.EndWaitForConnection, null);
            helper = await Task.Run(() => Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location, "--elevated-run " + name) { UseShellExecute = true, Verb = "runas" }));
            if (await Task.WhenAny(connection, Task.Delay(20000)) != connection) throw new IOException(Loc.T("Could not connect to the administrator process."));
            await connection;
            var read = new StreamReader(pipe, new UTF8Encoding(false), false, 4096, true);
            writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true);
            writer.AutoFlush = true;
            lock (sync) writer.WriteLine(CommandRunner.Pack(e.Category.Id + "\n" + e.Title + "\n" + (param ?? "") + "\n" + (restorePoint ? "1" : "0")));
            string line;
            while ((line = await read.ReadLineAsync()) != null)
            {
                if (line.Length == 0) continue;
                string value = CommandRunner.Unpack(line.Substring(1));
                switch (line[0])
                {
                    case 'A': Ready = true; ready(); break;
                    case 'O': append(value); break;
                    case 'E': appendErr(value); break;
                    case 'R': return int.Parse(value);
                }
            }
            throw new IOException(Loc.T("The administrator process closed the connection without a result."));
        }

        public void Send(string value)
        {
            if (!Ready || disposed) throw new InvalidOperationException(Loc.T("The command does not accept input yet."));
            if (writer != null) { lock (sync) writer.WriteLine("I" + CommandRunner.Pack(value)); }
            else { process.StandardInput.WriteLine(value); process.StandardInput.Flush(); }
        }

        public void Stop()
        {
            if (disposed || !Ready) return;
            Stopped = true;
            try
            {
                if (writer != null) { lock (sync) writer.WriteLine("X"); }
                else if (process != null) CommandRunner.KillTree(process.Id);
            }
            catch (Exception) { }
        }

        public void Dispose()
        {
            disposed = true;
            Ready = false;
            if (pipe != null) pipe.Dispose();
            if (process != null) process.Dispose();
            if (helper != null) helper.Dispose();
        }
    }

    // ======================================================================================
    //  Command-line mode (no window):  Tools.exe --run "Command title" [--param value] [--out file]
    //                                  Tools.exe --list [--out file]
    //  Output goes to --out, or to stdout when redirected. Exit code = the command's own exit code, or
    //  2 = unknown / ambiguous title, 3 = cannot run without a window, 4 = bad parameter, 5 = needs administrator.
    // ======================================================================================
    public static class Cli
    {
        public static int Run(string[] args)
        {
            string title = null, param = null, outFile = null;
            bool list = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--run" && i + 1 < args.Length) title = args[++i];
                else if (args[i] == "--param" && i + 1 < args.Length) param = args[++i];
                else if (args[i] == "--out" && i + 1 < args.Length) outFile = args[++i];
                else if (args[i] == "--list") list = true;
            }
            var sb = new StringBuilder();
            int code;
            try { code = Execute(title, param, list, sb); }
            catch (Exception ex) { sb.AppendLine(ex.Message); code = 1; }
            try
            {
                if (!string.IsNullOrEmpty(outFile)) File.WriteAllText(outFile, sb.ToString(), new UTF8Encoding(true));
                else { var o = Console.Out; o.Write(sb.ToString()); o.Flush(); }
            }
            catch (Exception) { }
            return code;
        }

        static bool Runnable(Entry e) { return !(e.CopyOnly || e.Launch || e.Shell == "Reference" || e.Shell == "Shortcut"); }

        static int Execute(string title, string param, bool list, StringBuilder sb)
        {
            var entries = Catalog.Load().SelectMany(c => c.Entries).ToList();
            if (list)
            {
                foreach (var x in entries)
                    if (Runnable(x)) sb.AppendLine(x.Title + (x.Admin ? "   [admin]" : "") + (x.Param != null ? "   [takes --param]" : ""));
                return 0;
            }
            if (string.IsNullOrEmpty(title))
            {
                sb.AppendLine("Usage: Tools.exe --run \"Command title\" [--param value] [--out file]   |   Tools.exe --list [--out file]");
                return 2;
            }
            var found = entries.Where(x => string.Equals(x.Title, title, StringComparison.OrdinalIgnoreCase)).ToList();
            if (found.Count == 0) found = entries.Where(x => x.Title.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if (found.Count == 0) { sb.AppendLine("No command matches: " + title); return 2; }
            if (found.Count > 1)
            {
                sb.AppendLine("More than one command matches \"" + title + "\":");
                foreach (var x in found) sb.AppendLine("  " + x.Title);
                return 2;
            }
            var e = found[0];
            if (!Runnable(e)) { sb.AppendLine("This entry opens a window or page, or only copies text, so it cannot run from the command line: " + e.Title); return 3; }
            if (e.Param != null)
            {
                if (param == null) param = CommandRunner.ExpandDefault(e.ParamDefault);
                if (!CommandRunner.ValidParam(param)) { sb.AppendLine("This command needs a valid --param value."); return 4; }
            }
            if (e.Admin && !CommandRunner.IsAdmin()) { sb.AppendLine("This command needs administrator rights: start Tools.exe from an elevated prompt."); return 5; }
            using (var p = CommandRunner.CreateProcess(CommandRunner.Build(e, param)))
            {
                p.Start();
                try { p.StandardInput.Close(); } catch (Exception) { }
                object gate = new object();
                Action<string> add = s => { lock (gate) sb.Append(s); };
                Task.WaitAll(CommandRunner.Pump(p.StandardOutput, add), CommandRunner.PumpErr(p.StandardError, add));
                p.WaitForExit();
                return p.ExitCode;
            }
        }
    }
}
