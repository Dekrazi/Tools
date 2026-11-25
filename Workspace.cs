using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
namespace RepairToolkit
{
    // ======================================================================================
    //  Shared look: dark terminal-green palette
    // ======================================================================================
    public class BufferedListView : ListView
    {
        public BufferedListView() { DoubleBuffered = true; }
    }

    public class Theme
    {
        public string Id, Name, NamePl;
        public Color Accent, Bg, Surface, Panel, Line, Hover, Sel, Text, Muted, ConsoleBg, AccentDim, AccentBg, RowAlt;
        public string Label { get { return Loc.Pl ? NamePl : Name; } }
    }

    public static class Skin
    {
        public static Color Bg, Surface, Panel, Line, Hover, Sel, Text, Muted, Accent, AccentDim, AccentBg, RowAlt, ConsoleBg;

        // Semantic colors stay the same in every theme: Ok = success / safe, Blue = info, Amber = caution, Red = error / danger.
        public static readonly Color Ok = Color.FromArgb(57, 255, 126),
            Blue = Color.FromArgb(119, 176, 255),
            Amber = Color.FromArgb(245, 166, 35),
            Red = Color.FromArgb(255, 95, 95);

        public static readonly Theme[] Themes = new Theme[]
        {
            new Theme { Id = "green", Name = "Green", NamePl = "Zielony", Accent = Color.FromArgb(57, 255, 126), Bg = Color.FromArgb(13, 15, 14), Surface = Color.FromArgb(19, 22, 20), Panel = Color.FromArgb(24, 27, 25), Line = Color.FromArgb(37, 48, 41), Hover = Color.FromArgb(26, 44, 32), Sel = Color.FromArgb(22, 56, 35), Text = Color.FromArgb(200, 216, 204), Muted = Color.FromArgb(125, 145, 131), ConsoleBg = Color.FromArgb(8, 10, 9), AccentDim = Color.FromArgb(40, 120, 70), AccentBg = Color.FromArgb(13, 31, 18), RowAlt = Color.FromArgb(21, 25, 23) },
            new Theme { Id = "magenta", Name = "Magenta", NamePl = "Magenta", Accent = Color.FromArgb(255, 85, 230), Bg = Color.FromArgb(15, 13, 15), Surface = Color.FromArgb(22, 19, 22), Panel = Color.FromArgb(27, 24, 27), Line = Color.FromArgb(52, 37, 50), Hover = Color.FromArgb(46, 26, 44), Sel = Color.FromArgb(66, 24, 62), Text = Color.FromArgb(216, 200, 214), Muted = Color.FromArgb(150, 125, 148), ConsoleBg = Color.FromArgb(10, 8, 10), AccentDim = Color.FromArgb(140, 40, 125), AccentBg = Color.FromArgb(36, 13, 33), RowAlt = Color.FromArgb(25, 21, 25) },
            new Theme { Id = "cyan", Name = "Cyan", NamePl = "Cyjan", Accent = Color.FromArgb(64, 224, 255), Bg = Color.FromArgb(13, 14, 15), Surface = Color.FromArgb(19, 21, 22), Panel = Color.FromArgb(24, 26, 27), Line = Color.FromArgb(37, 47, 52), Hover = Color.FromArgb(26, 40, 46), Sel = Color.FromArgb(20, 52, 64), Text = Color.FromArgb(200, 212, 216), Muted = Color.FromArgb(125, 141, 150), ConsoleBg = Color.FromArgb(8, 9, 10), AccentDim = Color.FromArgb(30, 110, 130), AccentBg = Color.FromArgb(13, 28, 34), RowAlt = Color.FromArgb(21, 24, 25) },
            new Theme { Id = "red", Name = "Red", NamePl = "Czerwony", Accent = Color.FromArgb(255, 82, 82), Bg = Color.FromArgb(15, 13, 13), Surface = Color.FromArgb(22, 19, 19), Panel = Color.FromArgb(27, 24, 24), Line = Color.FromArgb(52, 37, 37), Hover = Color.FromArgb(46, 26, 26), Sel = Color.FromArgb(68, 24, 24), Text = Color.FromArgb(216, 200, 200), Muted = Color.FromArgb(150, 125, 125), ConsoleBg = Color.FromArgb(10, 8, 8), AccentDim = Color.FromArgb(140, 40, 40), AccentBg = Color.FromArgb(36, 13, 13), RowAlt = Color.FromArgb(25, 21, 21) },
            new Theme { Id = "white", Name = "White", NamePl = "Biały", Accent = Color.FromArgb(240, 240, 240), Bg = Color.FromArgb(14, 14, 14), Surface = Color.FromArgb(20, 20, 20), Panel = Color.FromArgb(26, 26, 26), Line = Color.FromArgb(48, 48, 48), Hover = Color.FromArgb(38, 38, 38), Sel = Color.FromArgb(56, 56, 56), Text = Color.FromArgb(210, 210, 210), Muted = Color.FromArgb(135, 135, 135), ConsoleBg = Color.FromArgb(9, 9, 9), AccentDim = Color.FromArgb(110, 110, 110), AccentBg = Color.FromArgb(34, 34, 34), RowAlt = Color.FromArgb(23, 23, 23) }
        };

        public static Theme Current;

        static Skin() { Apply(Themes[2]); }   // cyan

        // Takes effect for windows created afterwards (the app rebuilds its window when the color changes).
        public static void Apply(Theme t)
        {
            Current = t;
            Bg = t.Bg; Surface = t.Surface; Panel = t.Panel; Line = t.Line; Hover = t.Hover; Sel = t.Sel; Text = t.Text; Muted = t.Muted;
            Accent = t.Accent; AccentDim = t.AccentDim; AccentBg = t.AccentBg; RowAlt = t.RowAlt; ConsoleBg = t.ConsoleBg;
        }

        public static Button Button(string text, bool primary = false)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, 30),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = Panel,
                ForeColor = primary ? Accent : Text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(8, 2, 8, 2),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
            };
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = primary ? AccentDim : Line;
            b.FlatAppearance.MouseOverBackColor = Hover;
            return b;
        }

        public static Control Field(string label, Control control)
        {
            var panel = new System.Windows.Forms.Panel { Width = control.Width + 4, Height = 55, Margin = new Padding(0, 0, 7, 0) };
            var title = Label(label, 8, true);
            title.Dock = DockStyle.Top;
            title.Height = 20;
            title.ForeColor = Muted;
            control.Dock = DockStyle.None;
            control.Location = new Point(0, 23);
            panel.Controls.Add(control);
            panel.Controls.Add(title);
            return panel;
        }

        public static Label Label(string text, int size = 10, bool bold = false)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, ForeColor = Text, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), TextAlign = ContentAlignment.MiddleLeft };
        }

        public static TextBox TextBox(bool multi = false)
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = multi,
                ReadOnly = multi,
                ScrollBars = multi ? ScrollBars.Both : ScrollBars.None,
                WordWrap = false,
                BackColor = Bg,
                ForeColor = Text,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font(multi ? "Consolas" : "Segoe UI", 10)
            };
        }

        public static ListView Grid(params string[] columns)
        {
            var list = new BufferedListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, BackColor = Panel, ForeColor = Text, BorderStyle = BorderStyle.None, OwnerDraw = true };
            list.SmallImageList = new ImageList { ImageSize = new Size(1, 30) };
            foreach (string title in columns) list.Columns.Add(title, 150);
            list.DrawColumnHeader += (s, e) =>
            {
                using (var brush = new SolidBrush(Surface)) e.Graphics.FillRectangle(brush, e.Bounds);
                var bounds = e.Bounds;
                bounds.X += 8;
                TextRenderer.DrawText(e.Graphics, e.Header.Text, list.Font, bounds, Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            };
            list.DrawItem += (s, e) => { };
            list.DrawSubItem += (s, e) =>
            {
                Color back = e.Item.Selected ? Sel : (e.ItemIndex % 2 == 0 ? Panel : RowAlt);
                using (var brush = new SolidBrush(back)) e.Graphics.FillRectangle(brush, e.Bounds);
                var bounds = e.Bounds;
                bounds.X += 8;
                bounds.Width -= 10;
                Color color = e.SubItem.ForeColor;
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, list.Font, bounds, color.IsEmpty ? Text : color,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            };
            return list;
        }

        public static bool OpenUrl(Control owner, string value)
        {
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) || (uri.Scheme != "https" && uri.Scheme != "http")) return false;
            try
            {
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
                Audit.Add("INFO", "Opened web page", uri.AbsoluteUri);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, Loc.T("Cannot open the page.") + "\n" + ex.Message, Loc.T("Web page"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
        }

        public static void Error(Control owner, Exception ex)
        {
            MessageBox.Show(owner, ex.Message, "Tools", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void SetClipboard(Control owner, string text)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try { Clipboard.SetDataObject(text, true); return; }
                catch (Exception) { System.Threading.Thread.Sleep(80); }
            }
            MessageBox.Show(owner, Loc.T("Could not access the clipboard (another program may be using it)."), Loc.T("Clipboard"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static string OneLine(string s)
        {
            if (s == null) return "";
            string t = s.Replace("\r", " ").Replace("\n", " ");
            return t.Length > 240 ? t.Substring(0, 240) + "..." : t;
        }
    }

    // ======================================================================================
    //  Nothing is written to disk (the tool is meant to be carried between PCs and leave no trace).
    //  Kept as a no-op so the call sites stay simple.
    // ======================================================================================
    public static class Audit
    {
        public static void Add(string level, string title, string details = "") { }
    }

    // ======================================================================================
    //  Console tab: live output, stdin, stop, copy, save
    // ======================================================================================
    public class ExecutionPanel : UserControl
    {
        RichTextBox output;
        TextBox input;
        Label title, state;
        Button send, stop, copyOut, save, clear, runAgain, copyTicket;
        Color lastCol = Skin.Text;
        RunSession session;
        readonly StringBuilder curLine = new StringBuilder();
        int lineStart;
        bool pendingCR, sawOutput;
        string stateBase;
        DateTime runStarted;
        System.Windows.Forms.Timer clock;
        public bool Busy { get; private set; }
        public event Action BusyChanged;
        public event Action RunAgainRequested;

        public ExecutionPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = Skin.Bg;
            Padding = new Padding(22, 14, 22, 10);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            Controls.Add(layout);

            title = Skin.Label(Loc.T("Operations console"), 20, true);
            state = Skin.Label(Loc.T("Pick a command in the catalog and press Run."));
            state.ForeColor = Skin.Muted;

            output = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Skin.ConsoleBg,
                ForeColor = Skin.Text,
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.None,
                WordWrap = false,
                DetectUrls = true,
                HideSelection = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            output.LinkClicked += (s, e) => Skin.OpenUrl(this, e.LinkText);
            clock = new System.Windows.Forms.Timer { Interval = 1000 };
            clock.Tick += (s, e) =>
            {
                if (!Busy || stateBase == null) return;
                TimeSpan t = DateTime.Now - runStarted;
                state.Text = stateBase + " \u2022 " + ((int)t.TotalMinutes).ToString("00") + ":" + t.Seconds.ToString("00");
            };
            var menu = new ContextMenuStrip();
            menu.Items.Add(Loc.T("Copy"), null, delegate { if (output.SelectionLength > 0) output.Copy(); });
            menu.Items.Add(Loc.T("Select all"), null, delegate { output.SelectAll(); });
            output.ContextMenuStrip = menu;
            AppendColored(Loc.T("Command output appears here.") + "\n" + Loc.T("Administrator commands stay in this view too."), Skin.Muted);

            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(state, 0, 1);
            layout.Controls.Add(output, 0, 2);

            var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0), WrapContents = false };
            var bar2 = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0), WrapContents = false };
            input = Skin.TextBox();
            input.Dock = DockStyle.None;
            input.Width = 260;
            input.Margin = new Padding(0, 2, 6, 0);
            input.Enabled = false;
            send = Skin.Button(Loc.T("Send reply"));
            send.Enabled = false;
            stop = Skin.Button(Loc.T("Stop"));
            stop.Enabled = false;
            copyOut = Skin.Button(Loc.T("Copy output"));
            save = Skin.Button(Loc.T("Save result"));
            clear = Skin.Button(Loc.T("Clear"));
            runAgain = Skin.Button(Loc.T("Run again"));
            runAgain.Enabled = false;
            copyTicket = Skin.Button(Loc.T("Copy for ticket"));
            bar.Controls.Add(input);
            bar.Controls.Add(send);
            bar.Controls.Add(stop);
            bar.Controls.Add(runAgain);
            bar2.Controls.Add(copyOut);
            bar2.Controls.Add(copyTicket);
            bar2.Controls.Add(save);
            bar2.Controls.Add(clear);
            layout.Controls.Add(bar, 0, 3);
            layout.Controls.Add(bar2, 0, 4);
            var hint = Skin.Label(Loc.T("Replies go to the running command. Choose new commands in the catalog."), 9);
            hint.ForeColor = Skin.Muted;
            layout.Controls.Add(hint, 0, 5);

            send.Click += (s, e) => Send();
            input.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Send(); e.SuppressKeyPress = true; } };
            stop.Click += (s, e) => StopRun();
            copyOut.Click += (s, e) => { if (output.TextLength > 0) Skin.SetClipboard(this, output.Text); };
            save.Click += (s, e) => Save();
            clear.Click += (s, e) => ClearOutput();
            runAgain.Click += (s, e) => { var h = RunAgainRequested; if (h != null) h(); };
            copyTicket.Click += (s, e) => CopyForTicket();
        }

        // Header + output, ready to paste into a ticket.
        void CopyForTicket()
        {
            if (output.TextLength == 0) return;
            string header = "Computer: " + Environment.MachineName + "   User: " + Environment.UserDomainName + "\\" + Environment.UserName +
                            "   Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "   Command: " + title.Text;
            Skin.SetClipboard(this, header + "\r\n" + new string('-', 60) + "\r\n" + output.Text.Replace("\r\n", "\n").Replace("\n", "\r\n"));
            state.Text = Loc.T("Copied with a header for a ticket.");
        }

        // The console text is carried over when the window is rebuilt (language / color change).
        public class ConsoleSnapshot { public string Rtf, Title, State; public bool CanRunAgain; }

        public ConsoleSnapshot Snapshot()
        {
            return new ConsoleSnapshot { Rtf = output.Rtf, Title = title.Text, State = state.Text, CanRunAgain = runAgain.Enabled };
        }

        public void Restore(ConsoleSnapshot snap)
        {
            try { output.Rtf = snap.Rtf; } catch (Exception) { }
            title.Text = snap.Title;
            state.Text = snap.State;
            state.ForeColor = Skin.Muted;
            runAgain.Enabled = snap.CanRunAgain;
            curLine.Length = 0;
            pendingCR = false;
            lineStart = output.TextLength;
            lastCol = Skin.Text;
            output.SelectionStart = output.TextLength;
            output.ScrollToCaret();
        }

        // Select the next occurrence of the text in the output (wraps around).
        public bool Find(string text)
        {
            if (string.IsNullOrEmpty(text) || output.TextLength == 0) return false;
            int start = output.SelectionStart + output.SelectionLength;
            int i = output.Find(text, start, RichTextBoxFinds.None);
            if (i < 0 && start > 0) i = output.Find(text, 0, RichTextBoxFinds.None);
            if (i < 0) return false;
            output.Select(i, text.Length);
            output.ScrollToCaret();
            return true;
        }

        // ---------- output pane (handles carriage-return progress such as DISM's) ----------
        void Feed(string s) { FeedColor(s, Skin.Text); }

        // stdout is drawn in the normal colour, stderr in red; a half-written line is closed when the colour changes.
        void FeedColor(string s, Color col)
        {
            if (col != lastCol && (curLine.Length > 0 || pendingCR)) FeedCore("\n", lastCol);
            lastCol = col;
            FeedCore(s, col);
        }

        void FeedCore(string s, Color col)
        {
            if (output.TextLength > 4000000)
            {
                output.Select(0, 2000000);
                output.SelectedText = "";
                lineStart = Math.Max(0, output.TextLength - curLine.Length);
            }
            StringBuilder done = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch == '\0' || ch == '\uFEFF' || ch == '\uFFFD') continue;
                if (!char.IsWhiteSpace(ch)) sawOutput = true;
                if (ch == '\r') { pendingCR = true; continue; }
                if (pendingCR)
                {
                    pendingCR = false;
                    if (ch == '\n')
                    {
                        done.Append(curLine.ToString());
                        done.Append('\n');
                        curLine.Length = 0;
                        continue;
                    }
                    curLine.Length = 0;   // bare CR: the program is redrawing the same line
                }
                if (ch == '\n')
                {
                    done.Append(curLine.ToString());
                    done.Append('\n');
                    curLine.Length = 0;
                }
                else curLine.Append(ch);
            }
            if (lineStart > output.TextLength) lineStart = output.TextLength;
            output.SelectionStart = lineStart;
            output.SelectionLength = output.TextLength - lineStart;
            output.SelectionColor = col;
            output.SelectedText = done.ToString() + curLine.ToString();
            lineStart = Math.Max(0, output.TextLength - curLine.Length);
            output.SelectionStart = output.TextLength;
            output.ScrollToCaret();
        }

        void AppendColored(string text, Color col)
        {
            if (curLine.Length > 0 || pendingCR) FeedCore("\n", lastCol);
            lastCol = Skin.Text;
            output.SelectionStart = output.TextLength;
            output.SelectionLength = 0;
            output.SelectionColor = col;
            output.AppendText(text);
            output.SelectionColor = Skin.Text;
            lineStart = output.TextLength;
            curLine.Length = 0;
            pendingCR = false;
            output.SelectionStart = output.TextLength;
            output.ScrollToCaret();
        }

        void ClearOutput()
        {
            output.Clear();
            curLine.Length = 0;
            lineStart = 0;
            pendingCR = false;
            lastCol = Skin.Text;
        }

        void Append(string value)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action<string>(Append), value); return; }
            value = value.Replace("\0", "");
            Feed(value);
        }

        void AppendErr(string value)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action<string>(AppendErr), value); return; }
            value = value.Replace("\0", "");
            FeedColor(value, Skin.Red);
        }

        // ---------- running ----------
        public async Task Start(Entry e, string code, string param, bool restorePoint, string note)
        {
            if (Busy) throw new InvalidOperationException(Loc.T("Wait for the current operation to finish."));
            sawOutput = false;
            stateBase = null;
            Busy = true;
            Changed();
            title.Text = e.Name;
            state.Text = (e.Admin && !CommandRunner.IsAdmin()) ? Loc.T("Waiting for administrator approval (UAC)...") : Loc.T("Starting...");
            state.ForeColor = Skin.Amber;
            ClearOutput();
            AppendColored("> " + e.Name + "\n", Skin.Amber);
            if (e.Special != "report") AppendColored(Skin.OneLine(code) + "\n\n", Skin.Muted);
            else AppendColored("\n", Skin.Muted);
            if (restorePoint) AppendColored(Loc.T("A restore point is created first (Restore point first is ON).") + "\n\n", Skin.Muted);
            if (note != null) AppendColored(note + "\n\n", Skin.Amber);
            Audit.Add("START", e.Title, code);
            session = new RunSession();
            DateTime started = DateTime.Now;
            try
            {
                int exit = await session.Run(e, param, Append, AppendErr, delegate
                {
                    send.Enabled = true;
                    input.Enabled = true;
                    stop.Enabled = true;
                    stateBase = Loc.T("Running") + " \u2022 " + (e.Admin ? Loc.T("administrator") : Loc.T("current user"));
                    runStarted = DateTime.Now;
                    state.Text = stateBase + " \u2022 00:00";
                    clock.Start();
                }, restorePoint);
                double secs = (DateTime.Now - started).TotalSeconds;
                if (session.Stopped)
                {
                    state.Text = Loc.T("Stopped");
                    state.ForeColor = Skin.Amber;
                    AppendColored("\n" + Loc.T("[stopped]") + "\n", Skin.Amber);
                    Audit.Add("CANCELLED", e.Title, "Stopped by the user.");
                }
                else
                {
                    clock.Stop();
                    state.Text = Loc.F("Finished \u2022 exit code: {0}", exit);
                    state.ForeColor = exit == 0 ? Skin.Ok : Skin.Red;
                    if (!sawOutput)
                        AppendColored("\n" + (exit == 0 ? Loc.T("(No output - the command finished and found nothing to show.)") : Loc.T("(No output was produced.)")) + "\n", Skin.Amber);
                    if (exit != 0)
                        AppendColored(Loc.T("A non-zero exit code usually means the command reported a problem or found nothing - the text above explains why. Some tools also need Administrator rights.") + "\n", Skin.Muted);
                    AppendColored("\n" + Loc.F("[finished - exit code {0}, {1} s]", exit, secs.ToString("0.0")) + "\n", exit == 0 ? Skin.Ok : Skin.Red);
                    if (e.Special == "report") AppendColored(Loc.T("Use \"Save result\" below to keep this report.") + "\n", Skin.Muted);
                    Audit.Add(exit == 0 ? "OK" : "ERROR", e.Title, "Exit code: " + exit);
                }
            }
            catch (Win32Exception ex)
            {
                bool cancelled = ex.NativeErrorCode == 1223;
                state.Text = cancelled ? Loc.T("UAC approval was cancelled.") : Loc.T("Could not start the operation.");
                state.ForeColor = Skin.Amber;
                AppendColored("\n" + ex.Message + "\n", Skin.Red);
                Audit.Add(cancelled ? "CANCELLED" : "ERROR", e.Title, ex.Message);
            }
            catch (Exception ex)
            {
                state.Text = Loc.T("Operation error");
                state.ForeColor = Skin.Red;
                AppendColored("\n" + ex.Message + "\n", Skin.Red);
                Audit.Add("ERROR", e.Title, ex.ToString());
            }
            finally
            {
                clock.Stop();
                stateBase = null;
                session.Dispose();
                session = null;
                Busy = false;
                send.Enabled = false;
                input.Enabled = false;
                stop.Enabled = false;
                runAgain.Enabled = true;
                Changed();
            }
        }

        void Changed() { var handler = BusyChanged; if (handler != null) handler(); }

        void StopRun()
        {
            if (session == null) return;
            session.Stop();
            stateBase = null;
            state.Text = Loc.T("Stopping...");
            stop.Enabled = false;
        }

        void Send()
        {
            try
            {
                if (session != null) { session.Send(input.Text); input.Clear(); }
            }
            catch (Exception ex) { Skin.Error(this, ex); }
        }

        void Save()
        {
            if (output.TextLength == 0) { MessageBox.Show(this, Loc.T("There is no output to save yet."), Loc.T("Save"), MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = Loc.T("Text file (*.txt)|*.txt|All files (*.*)|*.*");
                dialog.FileName = "PC_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".txt";
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    File.WriteAllText(dialog.FileName, output.Text.Replace("\r\n", "\n").Replace("\n", "\r\n"), new UTF8Encoding(true));
                    Audit.Add("EXPORT", "Save result", dialog.FileName);
                    state.Text = Loc.T("Saved: ") + dialog.FileName;
                }
                catch (Exception ex) { Skin.Error(this, ex); }
            }
        }
    }
}
