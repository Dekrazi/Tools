using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
[assembly: AssemblyTitle("Tools")]
[assembly: AssemblyVersion("5.9.0.0")]
[assembly: AssemblyDescription("Windows service commands and diagnostic tools")]
namespace RepairToolkit
{
    static class Program
    {
        // Used to rebuild the window (language switch) without losing position and category.
        public static bool Restart;
        public static Rectangle SavedBounds;
        public static FormWindowState SavedState;
        public static string SavedCategory;
        public static ExecutionPanel.ConsoleSnapshot SavedConsole;   // console text, last command and tab survive a window rebuild
        public static string SavedLastKey, SavedLastParam;
        public static int SavedTab;
        public static List<string> RecentKeys = new List<string>();   // commands run in this session (survives the EN/PL rebuild)

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 2 && args[0] == "--elevated-run") return CommandRunner.ElevatedHelper(args[1]);
                if (args.Length >= 1 && (args[0] == "--run" || args[0] == "--list")) return Cli.Run(args);   // command-line mode, no window
                Loc.Load();
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) =>
                {
                    Audit.Add("ERROR", "Unhandled UI error", e.Exception.ToString());
                    MessageBox.Show(e.Exception.Message, "Tools");
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    try { Audit.Add("ERROR", "Process error", e.ExceptionObject.ToString()); } catch (Exception) { }
                };
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                do
                {
                    Restart = false;
                    Application.Run(new MainWindow());
                } while (Restart);
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Loc.T("The application could not start.") + "\n" + ex.Message, "Tools");
                return 1;
            }
        }
    }

    public class MainWindow : Form
    {
        readonly Color bg = Skin.Bg, surface = Skin.Surface, text = Skin.Text, muted = Skin.Muted, accent = Skin.Accent;
        static readonly Font fHead = new Font("Segoe UI", 11, FontStyle.Bold), fBold = new Font("Segoe UI", 10, FontStyle.Bold),
            fNorm = new Font("Segoe UI", 10), fMono = new Font("Consolas", 10);
        List<Category> catalog;
        Category current;
        TreeView nav;
        TextBox search;
        ListView list;
        RichTextBox reference;
        Label heading, subtitle, info, status, lblAdmin;
        LinkLabel linkAdmin;
        Button copy, open, run, btnEn, btnPl;
        SplitContainer split;
        Panel content, toolbar;
        bool updating;
        Entry selected;
        WorkspaceTabs workspace;
        ExecutionPanel execution;
        WorkspacePage catalogTab, consoleTab;
        ContextMenuStrip menuQuick;
        readonly Dictionary<string, Entry> entryByTitle = new Dictionary<string, Entry>();
        readonly List<LinkSpan> links = new List<LinkSpan>();

        class LinkSpan { public int Start, Length; public Entry Target; }
        readonly bool isAdmin = CommandRunner.IsAdmin();
        Entry reportEntry;
        Button btnRp, btnRecent, btnTheme;
        bool restorePointFirst;
        Entry lastEntry;
        string lastParam, lastFind = "";

        public MainWindow()
        {
            catalog = Catalog.Load();
            reportEntry = catalog.SelectMany(c => c.Entries).FirstOrDefault(x => x.Special == "report");
            catalog.RemoveAll(c => c.Hidden);
            foreach (var c0 in catalog) foreach (var e0 in c0.Entries) if (!entryByTitle.ContainsKey(e0.Title)) entryByTitle[e0.Title] = e0;
            if (reportEntry != null && !entryByTitle.ContainsKey(reportEntry.Title)) entryByTitle[reportEntry.Title] = reportEntry;
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Tools";
            try { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }
            MinimumSize = new Size(1200, 700);
            Size = new Size(1320, 840);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = bg;
            ForeColor = text;
            Font = new Font("Segoe UI", 10);
            KeyPreview = true;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = bg, Margin = Padding.Empty, Padding = Padding.Empty };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            Controls.Add(root);

            // workspace + pages must exist before the top bar (quick menus) and status bar are wired
            split = new SplitContainer { Dock = DockStyle.Fill, BackColor = Skin.Line, FixedPanel = FixedPanel.Panel1, SplitterWidth = 3 };
            split.Panel1.BackColor = surface;
            split.Panel2.BackColor = bg;
            workspace = new WorkspaceTabs { Dock = DockStyle.Fill };
            split.Panel2.Controls.Add(workspace);
            catalogTab = Page(Loc.T("01   Tool catalog"));
            consoleTab = Page(Loc.T("02   Operations console"));
            execution = new ExecutionPanel();
            execution.RunAgainRequested += RunAgain;
            consoleTab.Controls.Add(execution);

            root.Controls.Add(BuildTopBar(), 0, 0);
            root.Controls.Add(split, 0, 1);
            root.Controls.Add(BuildStatusBar(), 0, 2);

            BuildNavTree();
            BuildCatalogTab();

            execution.BusyChanged += () =>
            {
                run.Enabled = selected != null && CanRun(selected);
                status.Text = execution.Busy ? Loc.T("OPERATION RUNNING \u2022 Output is in the Operations console tab.") : Loc.T("READY \u2022 Operation finished.");
            };

            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.F)
                {
                    if (workspace.SelectedTab == consoleTab) FindInConsole(true); else { search.Focus(); search.SelectAll(); }
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.F3 && workspace.SelectedTab == consoleTab) { FindInConsole(false); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.F5) { RunAgain(); e.SuppressKeyPress = true; }
                else if (e.Control && e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D2) { workspace.SelectedIndex = e.KeyCode - Keys.D1; e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Escape) { search.Clear(); e.SuppressKeyPress = true; }
            };
            Shown += (s, e) =>
            {
                FitToScreen();
                try
                {
                    float k = Math.Max(1f, CurrentAutoScaleDimensions.Width / 96f);
                    split.SplitterDistance = (int)(270 * k);
                }
                catch (Exception) { }
                BuildNav("");
                RestoreSession();
            };
            FormClosing += (s, e) =>
            {
                if (execution.Busy && e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    workspace.SelectedTab = consoleTab;
                    status.Text = Loc.T("An operation is still running. Stop it or wait for it to finish before closing.");
                }
            };
        }

        // Restores the window position after a language switch (done after DPI scaling, so it never grows) and keeps it on screen.
        void FitToScreen()
        {
            try
            {
                var wa = Screen.FromControl(this).WorkingArea;
                MinimumSize = new Size(Math.Min(MinimumSize.Width, wa.Width), Math.Min(MinimumSize.Height, wa.Height));
                if (Program.SavedBounds.Width > 0)
                {
                    Bounds = Program.SavedBounds;
                    if (Program.SavedState == FormWindowState.Maximized) WindowState = FormWindowState.Maximized;
                }
                if (WindowState != FormWindowState.Normal) return;
                int w = Math.Min(Width, wa.Width), h = Math.Min(Height, wa.Height);
                int x = Math.Max(wa.Left, Math.Min(Left, wa.Right - w)), y = Math.Max(wa.Top, Math.Min(Top, wa.Bottom - h));
                Bounds = new Rectangle(x, y, w, h);
            }
            catch (Exception) { }
        }

        WorkspacePage Page(string title)
        {
            var page = new WorkspacePage { Text = title, BackColor = bg, ForeColor = text, Padding = new Padding(0) };
            workspace.TabPages.Add(page);
            return page;
        }

        // ============================================================ top / status bars
        Control BuildTopBar()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = surface, Margin = Padding.Empty };
            var f = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = surface, Padding = new Padding(10, 10, 0, 0), Margin = Padding.Empty };

            var sl = new Label { Text = Loc.T("Search"), AutoSize = true, ForeColor = muted, Margin = new Padding(0, 6, 6, 0) };
            f.Controls.Add(sl);
            search = new TextBox { Width = 160, BackColor = bg, ForeColor = text, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 3, 14, 0) };
            search.TextChanged += (s, e) => BuildNav(Query);
            f.Controls.Add(search);

            menuQuick = BuildQuickMenu();
            var bq = Skin.Button(Loc.T("Quick") + "  \u25BE", true);
            bq.Click += (s, e) => menuQuick.Show(bq, new Point(0, bq.Height));
            f.Controls.Add(bq);

            var bp = Skin.Button(Loc.T("Open PowerShell"));
            bp.Click += (s, e) => OpenExternal("powershell.exe");
            f.Controls.Add(bp);
            var bc = Skin.Button(Loc.T("Open CMD"));
            bc.Click += (s, e) => OpenExternal("cmd.exe");
            f.Controls.Add(bc);
            var bt = Skin.Button(Loc.T("Open Terminal"));
            bt.Click += (s, e) => OpenExternal("wt.exe");
            f.Controls.Add(bt);
            var br = Skin.Button(Loc.T("Full system report"), true);
            br.Click += async (s, e) => await ExecuteEntry(reportEntry);
            f.Controls.Add(br);

            var r = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, BackColor = surface, Padding = new Padding(0, 10, 10, 0), Margin = Padding.Empty };
            btnTheme = Skin.Button(Loc.T("Color") + "  \u25BE");
            btnTheme.Margin = new Padding(0, 0, 10, 0);
            btnTheme.Click += (s, e) => ShowThemeMenu();
            r.Controls.Add(btnTheme);
            btnEn = Skin.Button("EN");
            btnEn.Margin = new Padding(0, 0, 4, 0);
            btnEn.Click += (s, e) => SetLang(false);
            r.Controls.Add(btnEn);
            btnPl = Skin.Button("PL");
            btnPl.Margin = new Padding(0);
            btnPl.Click += (s, e) => SetLang(true);
            r.Controls.Add(btnPl);
            StyleLang(btnEn, !Loc.Pl);
            StyleLang(btnPl, Loc.Pl);

            p.Controls.Add(f);   // fill first
            p.Controls.Add(r);
            return p;
        }

        void StyleLang(Button b, bool active)
        {
            b.ForeColor = active ? accent : text;
            b.FlatAppearance.BorderColor = active ? accent : Skin.Line;
            b.BackColor = active ? Skin.AccentBg : Skin.Panel;
        }

        Control BuildStatusBar()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = surface, Margin = Padding.Empty };
            status = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0), ForeColor = muted, Font = new Font("Segoe UI", 9) };
            status.Text = Loc.T("READY \u2022 Pick a command and click Run.");
            linkAdmin = new LinkLabel
            {
                Dock = DockStyle.Right, Width = 230, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9),
                LinkColor = Skin.Blue, ActiveLinkColor = text, VisitedLinkColor = Skin.Blue, BackColor = surface, Text = Loc.T("Restart as administrator"), Visible = !isAdmin
            };
            linkAdmin.LinkClicked += (s, e) => RestartAsAdmin();
            lblAdmin = new Label { Dock = DockStyle.Right, Width = 360, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 10, 0), Font = new Font("Segoe UI", 9) };
            lblAdmin.Text = isAdmin ? Loc.T("Running as Administrator") : Loc.T("Standard user - admin commands ask for UAC");
            lblAdmin.ForeColor = isAdmin ? Skin.Ok : Skin.Amber;
            p.Controls.Add(status);      // fill first
            p.Controls.Add(linkAdmin);
            p.Controls.Add(lblAdmin);
            return p;
        }

        // One menu, two sections: "ps" entries are read-only system information, "cmd" entries are repairs and network tools.
        ContextMenuStrip BuildQuickMenu()
        {
            var m = new ContextMenuStrip { BackColor = surface, ForeColor = text, ShowImageMargin = false };
            AddQuickSection(m, Loc.T("SYSTEM INFO"), "ps");
            m.Items.Add(new ToolStripSeparator());
            AddQuickSection(m, Loc.T("REPAIR AND NETWORK"), "cmd");
            return m;
        }

        void AddQuickSection(ContextMenuStrip m, string header, string kind)
        {
            m.Items.Add(new ToolStripLabel(header) { ForeColor = Skin.Accent, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) });
            foreach (var entry in catalog.SelectMany(c => c.Entries).Where(x => x.Quick == kind))
            {
                Entry en = entry;
                var mi = new ToolStripMenuItem(en.Name) { BackColor = surface, ForeColor = text, ToolTipText = Skin.OneLine(en.Code) };
                mi.Click += async (s, ev) => await ExecuteEntry(en);
                m.Items.Add(mi);
            }
        }

        // ============================================================ navigation tree
        void BuildNavTree()
        {
            nav = new TreeView
            {
                Dock = DockStyle.Fill, BackColor = surface, ForeColor = text, BorderStyle = BorderStyle.None, HideSelection = false, FullRowSelect = true,
                ShowLines = false, ShowPlusMinus = false, ShowRootLines = false, Indent = 12, ItemHeight = 32, Font = new Font("Segoe UI", 10)
            };
            split.Panel1.Controls.Add(nav);
            nav.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            nav.DrawNode += (s, e) =>
            {
                bool isGroup = !(e.Node.Tag is Category);
                bool sel = !isGroup && (e.State & TreeNodeStates.Selected) != 0;
                var full = new Rectangle(0, e.Bounds.Y, nav.ClientSize.Width, e.Bounds.Height);
                using (var brush = new SolidBrush(isGroup ? Skin.AccentBg : (sel ? Skin.Sel : surface))) e.Graphics.FillRectangle(brush, full);
                if (isGroup)
                {
                    using (var bar = new SolidBrush(Skin.Accent)) e.Graphics.FillRectangle(bar, 0, full.Y, 4, full.Height);
                    if (e.Node.Index > 0) using (var pen = new Pen(Skin.Line)) e.Graphics.DrawLine(pen, 0, full.Y, full.Width, full.Y);
                }
                else if (sel)
                {
                    using (var bar = new SolidBrush(Skin.Accent)) e.Graphics.FillRectangle(bar, 8, full.Y + 6, 3, full.Height - 12);
                }
                var b = full;
                b.X = isGroup ? 14 : 26;
                b.Width -= b.X;
                using (var font = new Font("Segoe UI", isGroup ? 9f : 10f, isGroup ? FontStyle.Bold : FontStyle.Regular))
                    TextRenderer.DrawText(e.Graphics, e.Node.Text, font, b, sel ? accent : (isGroup ? Skin.Accent : text),
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            };
            nav.AfterSelect += (s, e) => { if (!updating && e.Node != null && e.Node.Tag is Category) ShowCategory((Category)e.Node.Tag); };
            // Clicking a group header (not a category) selects nothing, so keep the previous category selected.
            nav.BeforeSelect += (s, e) => { if (!updating && e.Node != null && !(e.Node.Tag is Category)) e.Cancel = true; };
        }

        string Query { get { return search.Text.Trim(); } }
        static bool Match(string s, string q) { return s != null && s.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0; }

        bool CategoryMatches(Category c, string q)
        {
            return q.Length == 0 || c.Entries.Any(e => Match(e.SearchText, q)) ||
                   Match(c.Title + " " + c.TitlePl + " " + c.Description + " " + c.DescriptionPl + (c.Entries.Count == 0 ? " " + c.BlockText : ""), q);
        }

        string GroupLabel(string group)
        {
            return group == "Setup" ? Loc.T("START") : group == "Search" ? Loc.T("SEARCH RESULTS") : group == "Diagnostics" ? Loc.T("DIAGNOSTICS") : group == "Repair" ? Loc.T("REPAIR") : group == "Maintenance" ? Loc.T("MAINTENANCE") : Loc.T("KNOWLEDGE BASE");
        }

        void BuildNav(string q)
        {
            string previous = current != null ? current.Id : (Program.SavedCategory ?? "sysinfo");
            updating = true;
            nav.BeginUpdate();
            nav.Nodes.Clear();
            TreeNode chosen = null, first = null, resultsNode = null;
            if (q.Length > 0)
            {
                var results = new Category
                {
                    Id = "results", Title = "Search results", TitlePl = "Wyniki wyszukiwania", Group = "Search",
                    Description = "Every command that matches the search, from all categories.", DescriptionPl = "Każde polecenie pasujące do wyszukiwania, ze wszystkich kategorii."
                };
                foreach (var c in catalog) foreach (var en in c.Entries) if (Match(en.SearchText, q)) results.Entries.Add(en);
                if (results.Entries.Count > 0)
                {
                    var rg = nav.Nodes.Add(GroupLabel("Search"));
                    resultsNode = rg.Nodes.Add(results.Name + "  (" + results.Entries.Count + ")");
                    resultsNode.Tag = results;
                }
            }
            foreach (var group in catalog.GroupBy(c => c.Group))
            {
                var members = group.Where(c => CategoryMatches(c, q)).ToList();
                if (members.Count == 0) continue;
                var groupNode = nav.Nodes.Add(GroupLabel(group.Key));
                foreach (var c in members)
                {
                    bool catMatch = Match(c.Title + " " + c.TitlePl + " " + c.Description + " " + c.DescriptionPl, q);
                    int count = c.Entries.Count(e => q.Length == 0 || catMatch || Match(e.SearchText, q));
                    var n = groupNode.Nodes.Add(c.Name + (c.Entries.Count > 0 ? "  (" + count + ")" : ""));
                    n.Tag = c;
                    if (first == null) first = n;
                    if (c.Id == previous) chosen = n;
                }
            }
            nav.ExpandAll();
            nav.EndUpdate();
            updating = false;
            nav.SelectedNode = resultsNode ?? chosen ?? first;
            if (nav.SelectedNode == null)
            {
                heading.Text = Loc.T("No results");
                subtitle.Text = Loc.T("Change the search or press Esc to clear the filter.");
                list.Items.Clear();
                selected = null;
                list.Visible = false;
                toolbar.Visible = false;
                reference.Visible = false;
                status.Text = Loc.T("No results for: ") + q;
            }
        }

        // ============================================================ catalog tab
        void BuildCatalogTab()
        {
            var center = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(18, 12, 18, 8), BackColor = bg };
            center.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            center.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            center.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            center.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            catalogTab.Controls.Add(center);

            heading = new Label { Dock = DockStyle.Fill, ForeColor = text, Font = new Font("Segoe UI", 18, FontStyle.Bold) };
            subtitle = new Label { Dock = DockStyle.Fill, ForeColor = muted, Font = new Font("Segoe UI", 9) };
            center.Controls.Add(heading, 0, 0);
            center.Controls.Add(subtitle, 0, 1);

            // slim toolbar: Run / Copy command / Open page + one line about the selected entry (full text is in the row tooltip)
            toolbar = new Panel { Dock = DockStyle.Fill, BackColor = bg, Margin = Padding.Empty };
            info = new Label { Dock = DockStyle.Fill, ForeColor = muted, Font = new Font("Segoe UI", 9), AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false, BackColor = bg, Padding = new Padding(0, 6, 0, 0) };
            run = Skin.Button(Loc.T("\u25B6 Run"), true);
            run.Enabled = false;
            run.Click += async (s, e) => await ExecuteEntry(selected);
            copy = Skin.Button(Loc.T("Copy command"));
            copy.Enabled = false;
            copy.Click += (s, e) => Copy();
            open = Skin.Button(Loc.T("Open page"));
            open.Visible = false;
            open.Click += (s, e) => { if (selected != null) LaunchUrl(selected.Code); };
            buttons.Controls.Add(run);
            buttons.Controls.Add(copy);
            buttons.Controls.Add(open);
            btnRecent = Skin.Button(Loc.T("Recent") + "  \u25BE");
            btnRecent.Click += (s, e) => ShowRecent();
            btnRp = Skin.Button("");
            btnRp.Click += (s, e) => { restorePointFirst = !restorePointFirst; UpdateRpButton(); };
            new ToolTip().SetToolTip(btnRp, Loc.T("Creates a System Restore point before every Caution or High-risk command (administrator rights needed)."));
            UpdateRpButton();
            var tips = new ToolTip();
            tips.SetToolTip(run, Loc.T("Run the selected command (Enter or double-click)"));
            tips.SetToolTip(copy, Loc.T("Copy the command text (Ctrl+C)"));
            tips.SetToolTip(btnRecent, Loc.T("The last 10 commands run in this session"));
            var rightBar = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, BackColor = bg, Padding = new Padding(0, 6, 0, 0) };
            rightBar.Controls.Add(btnRp);
            rightBar.Controls.Add(btnRecent);
            toolbar.Controls.Add(info);      // fill first
            toolbar.Controls.Add(buttons);
            toolbar.Controls.Add(rightBar);
            center.Controls.Add(toolbar, 0, 2);

            content = new Panel { Dock = DockStyle.Fill, BackColor = bg };
            center.Controls.Add(content, 0, 3);

            list = Skin.Grid(Loc.T("Command / tool"), Loc.T("Risk"), Loc.T("Shell"));
            list.ShowItemToolTips = true;
            list.Columns[0].Width = 450;
            list.Columns[1].Width = 110;
            list.Columns[2].Width = 230;
            list.SelectedIndexChanged += (s, e) => { if (list.SelectedItems.Count > 0) ShowEntry((Entry)list.SelectedItems[0].Tag); };
            list.DoubleClick += async (s, e) => { if (selected != null) await ExecuteEntry(selected); };
            list.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && selected != null) { e.SuppressKeyPress = true; await ExecuteEntry(selected); }
                else if (e.Control && e.KeyCode == Keys.C) { e.SuppressKeyPress = true; Copy(); }
            };
            list.Resize += (s, e) =>
            {
                int width = Math.Max(500, list.ClientSize.Width - 22);
                list.Columns[0].Width = (int)(width * 0.52);
                list.Columns[1].Width = (int)(width * 0.14);
                list.Columns[2].Width = width - list.Columns[0].Width - list.Columns[1].Width;
            };
            var menu = new ContextMenuStrip { BackColor = surface, ForeColor = text, ShowImageMargin = false };
            menu.Items.Add(Loc.T("\u25B6 Run"), null, async (s, e) => { if (selected != null) await ExecuteEntry(selected); });
            menu.Items.Add(Loc.T("Copy command"), null, (s, e) => Copy());
            list.ContextMenuStrip = menu;
            content.Controls.Add(list);

            reference = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = surface, ForeColor = text, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10), DetectUrls = true, Visible = false };
            reference.LinkClicked += (s, e) => LaunchUrl(e.LinkText);
            reference.MouseMove += (s, e) => { reference.Cursor = LinkAt(e.Location) != null ? Cursors.Hand : Cursors.IBeam; };
            reference.MouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Left || reference.SelectionLength > 0) return;
                var l = LinkAt(e.Location);
                if (l != null) GoToEntry(l.Target);
            };
            content.Controls.Add(reference);
        }

        string RiskLabel(string risk)
        {
            return risk == "safe" ? Loc.T("Low") : risk == "caution" ? Loc.T("Caution") : risk == "danger" ? Loc.T("High") : Loc.T("Info");
        }

        Color RiskColor(string risk)
        {
            return risk == "danger" ? Skin.Red : risk == "caution" ? Skin.Amber : risk == "safe" ? Skin.Ok : Skin.Blue;
        }

        void ShowCategory(Category c)
        {
            current = c;
            workspace.SelectedTab = catalogTab;
            heading.Text = c.Name;
            subtitle.Text = c.Desc;
            list.BeginUpdate();
            list.Items.Clear();
            selected = null;
            copy.Enabled = false;
            run.Enabled = false;
            open.Visible = false;
            info.Text = "";
            bool titleMatch = Match(c.Title + " " + c.TitlePl + " " + c.Description + " " + c.DescriptionPl, Query);
            foreach (var e in c.Entries.Where(x => Query.Length == 0 || titleMatch || Match(x.SearchText, Query)))
            {
                var item = new ListViewItem(e.Name) { Tag = e };
                item.SubItems.Add(RiskLabel(e.Risk));
                item.SubItems.Add((c.Id == "results" ? e.Category.Name + "  \u2022  " : "") + e.Shell + (e.Admin ? " \u2022 admin" : ""));
                item.UseItemStyleForSubItems = false;
                item.SubItems[1].ForeColor = RiskColor(e.Risk);
                if (e.CopyOnly) item.SubItems[0].ForeColor = muted;
                item.ToolTipText = (e.Desc ?? "") + "\n\n" + Skin.OneLine(e.Code);
                list.Items.Add(item);
            }
            list.EndUpdate();
            bool showReference = c.Entries.Count == 0;
            reference.Visible = showReference;
            list.Visible = !showReference;
            toolbar.Visible = !showReference;
            if (showReference)
            {
                RenderBlocks(reference, c);
                reference.BringToFront();
            }
            else
            {
                list.BringToFront();
                if (list.Items.Count > 0) list.Items[0].Selected = true;
                else info.Text = Loc.T("No commands match the filter");
            }
            status.Text = Loc.F("{0} categories \u2022 {1} commands and links \u2022 {2}", catalog.Count, catalog.Sum(x => x.Entries.Count),
                Query.Length > 0 ? Loc.T("Filter: ") + Query : Loc.T("Ready - pick a command and click Run"));
        }

        void RenderBlocks(RichTextBox box, Category c)
        {
            box.Clear();
            links.Clear();
            if (c.Id == "changelog") Put(box, Loc.F("Version {0}", AppVersion()) + "\n", fHead, accent);
            foreach (var b in c.ActiveBlocks) AddText(box, b.Value, b.Key);
            box.SelectionStart = 0;
            box.SelectionLength = 0;
            box.ScrollToCaret();
        }

        static readonly Font fLinkNorm = new Font("Segoe UI", 10, FontStyle.Underline), fLinkBold = new Font("Segoe UI", 10, FontStyle.Bold | FontStyle.Underline);

        // Text may contain [[Command title]] markers; on the reference pages each becomes a link that jumps to that command.
        void Put(RichTextBox box, string value, Font font, Color color)
        {
            int pos = 0;
            while (pos < value.Length)
            {
                int a = value.IndexOf("[[", pos, StringComparison.Ordinal);
                int b = a < 0 ? -1 : value.IndexOf("]]", a + 2, StringComparison.Ordinal);
                if (a < 0 || b < 0) { PutRaw(box, value.Substring(pos), font, color); break; }
                if (a > pos) PutRaw(box, value.Substring(pos, a - pos), font, color);
                string key = value.Substring(a + 2, b - a - 2);
                Entry target;
                if (box == reference && entryByTitle.TryGetValue(key, out target))
                {
                    int start = box.TextLength;
                    PutRaw(box, target.Name, font.Bold ? fLinkBold : fLinkNorm, accent);
                    links.Add(new LinkSpan { Start = start, Length = box.TextLength - start, Target = target });
                }
                else PutRaw(box, key, font, color);
                pos = b + 2;
            }
        }

        LinkSpan LinkAt(Point pt)
        {
            if (links.Count == 0) return null;
            int i = reference.GetCharIndexFromPosition(pt);
            foreach (var l in links) if (i >= l.Start && i < l.Start + l.Length) return l;
            return null;
        }

        // Jumps to a command: clears the search, selects its category and its row.
        void GoToEntry(Entry target)
        {
            search.Clear();
            TreeNode node = null;
            foreach (TreeNode g in nav.Nodes) foreach (TreeNode n in g.Nodes) if (n.Tag == target.Category) node = n;
            if (node == null) return;
            nav.SelectedNode = node;
            foreach (ListViewItem it in list.Items)
                if (it.Tag == target) { it.Selected = true; it.EnsureVisible(); list.Focus(); break; }
        }

        static void PutRaw(RichTextBox box, string value, Font font, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;
            box.SelectionFont = font;
            box.SelectionColor = color;
            box.AppendText(value);
        }

        // "Label — description" lines and "Key: value" rows get a bold label so the reference pages are easy to scan.
        void AddText(RichTextBox box, string value, string kind)
        {
            if (kind == "heading")
            {
                if (box.TextLength > 0) Put(box, "\n", fNorm, text);
                Put(box, value + "\n\n", fHead, accent);
                return;
            }
            if (kind == "mono") { Put(box, value + "\n\n", fMono, text); return; }
            if (kind == "tip") { Put(box, value + "\n\n", fNorm, Skin.Amber); return; }
            if (kind == "text")
            {
                int i = value.IndexOf(" \u2014 ", StringComparison.Ordinal);
                if (i > 0)
                {
                    Put(box, value.Substring(0, i), fBold, text);
                    Put(box, "  \u2014  " + value.Substring(i + 3) + "\n", fNorm, muted);
                    return;
                }
                Put(box, value + "\n", fNorm, text);
                return;
            }
            // "row": several "Key: value" lines
            foreach (string line in value.Split('\n'))
            {
                int i = line.IndexOf(": ", StringComparison.Ordinal);
                if (i > 0 && i < 40 && line.IndexOf("://", StringComparison.Ordinal) < 0)
                {
                    Put(box, line.Substring(0, i + 1) + " ", fBold, muted);
                    Put(box, line.Substring(i + 2) + "\n", fNorm, text);
                }
                else Put(box, line + "\n", fNorm, text);
            }
            Put(box, "\n", fNorm, text);
        }

        // Entries that open a window, a web page or only copy text do not use the console, so they work while a command is running.
        static bool NeedsConsole(Entry e)
        {
            return !(e.CopyOnly || e.Launch || e.Shell == "Reference" || e.Shell == "Shortcut");
        }

        bool CanRun(Entry e) { return !execution.Busy || !NeedsConsole(e); }

        void ShowEntry(Entry e)
        {
            selected = e;
            info.Text = RiskLabel(e.Risk) + "  \u2022  " + e.Shell + (e.Admin ? "  \u2022  " + Loc.T("Administrator required") : "") + "  \u2014  " + (e.Desc ?? "");
            copy.Enabled = true;
            run.Enabled = CanRun(e);
            run.Text = e.CopyOnly ? Loc.T("Copy (WinRE only)")
                     : e.Shell == "Reference" ? Loc.T("Open page")
                     : e.Shell == "Shortcut" ? Loc.T("Run shortcut")
                     : e.Launch ? Loc.T("Open")
                     : (e.Admin && !isAdmin) ? Loc.T("\u25B6 Run as admin (UAC)") : Loc.T("\u25B6 Run");
            Uri url;
            open.Visible = Uri.TryCreate(e.Code, UriKind.Absolute, out url) && (url.Scheme == "https" || url.Scheme == "http");
        }

        // ============================================================ actions
        async Task ExecuteEntry(Entry e, string presetParam = null)
        {
            if (e == null) return;
            if (e.CopyOnly)
            {
                Skin.SetClipboard(this, e.Code);
                Audit.Add("INFO", e.Title, "Copied to clipboard (WinRE only): " + e.Code);
                status.Text = Loc.T("Copied: ") + Skin.OneLine(e.Code);
                MessageBox.Show(this,
                    Loc.T("This command only works from the Windows Recovery Environment (boot from a Windows USB, then Repair, then Command Prompt).\n\nIt has been copied to your clipboard."),
                    Loc.T("Copied"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (e.Shell == "Reference") { LaunchUrl(e.Code); return; }
            if (NeedsConsole(e) && execution.Busy) { workspace.SelectedTab = consoleTab; return; }
            if (e.Shell == "Shortcut")
            {
                try { CommandRunner.ResetGraphics(); Audit.Add("OK", e.Title, "Sent Win+Ctrl+Shift+B"); }
                catch (Exception ex) { Audit.Add("ERROR", e.Title, ex.Message); Skin.Error(this, ex); }
                return;
            }
            string param = null;
            if (e.Param != null)
            {
                param = presetParam ?? PromptText(Loc.Pl && e.ParamLabelPl != null ? e.ParamLabelPl : e.ParamLabel, CommandRunner.ExpandDefault(e.ParamDefault));
                if (param == null) return;
                if (!CommandRunner.ValidParam(param))
                {
                    MessageBox.Show(this, Loc.T("The value contains a character that is not allowed (quotes, backtick or a control character), is empty, or is longer than 260 characters."), Loc.T("Invalid input"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            string run1 = CommandRunner.Build(e, param);
            if (e.Risk == "caution" || e.Risk == "danger")
            {
                var answer = MessageBox.Show(this, e.Name + "\n\n" + e.Desc + "\n\n" + Skin.OneLine(run1) + "\n\n" + Loc.T("Run this operation?"),
                    Loc.T("Confirm operation"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes)
                {
                    Audit.Add("CANCELLED", e.Title, "Cancelled before start");
                    return;
                }
            }
            AddRecent(e);
            if (e.Launch) { StartGui(e); return; }
            bool rp = restorePointFirst && (e.Risk == "caution" || e.Risk == "danger") && !e.NoRp;
            string note = null;
            if (rp && !(isAdmin || e.Admin))
            {
                rp = false;
                note = Loc.T("Restore point skipped: this command runs without administrator rights, so Windows would refuse it. Use Restart as administrator.");
            }
            lastEntry = e;
            lastParam = param;
            workspace.SelectedTab = consoleTab;
            await execution.Start(e, run1, param, rp, note);
        }

        // ---------- session helpers: Run again, Recent, Find, restore-point switch ----------
        async void RunAgain()
        {
            if (lastEntry != null && !execution.Busy) await ExecuteEntry(lastEntry, lastParam);
        }

        void AddRecent(Entry e)
        {
            string key = e.Category.Id + "\n" + e.Title;
            Program.RecentKeys.Remove(key);
            Program.RecentKeys.Insert(0, key);
            if (Program.RecentKeys.Count > 10) Program.RecentKeys.RemoveRange(10, Program.RecentKeys.Count - 10);
        }

        void ShowRecent()
        {
            var m = new ContextMenuStrip { BackColor = surface, ForeColor = text, ShowImageMargin = false };
            var all = catalog.SelectMany(c => c.Entries).ToList();
            if (reportEntry != null) all.Add(reportEntry);
            int count = 0;
            foreach (string key in Program.RecentKeys)
            {
                Entry target = all.FirstOrDefault(x => x.Category.Id + "\n" + x.Title == key);
                if (target == null) continue;
                var mi = new ToolStripMenuItem(target.Name) { BackColor = surface, ForeColor = text, ToolTipText = Skin.OneLine(target.Code) };
                mi.Click += async (s, ev) => await ExecuteEntry(target);
                m.Items.Add(mi);
                count++;
            }
            if (count == 0) m.Items.Add(new ToolStripMenuItem(Loc.T("(none yet)")) { Enabled = false });
            m.Show(btnRecent, new Point(0, btnRecent.Height));
        }

        void FindInConsole(bool ask)
        {
            if (ask)
            {
                string q = PromptText(Loc.T("Find in output:"), lastFind);
                if (q == null) return;
                lastFind = q;
            }
            if (lastFind.Length == 0) return;
            if (!execution.Find(lastFind)) status.Text = Loc.T("Nothing found: ") + lastFind;
        }

        void UpdateRpButton()
        {
            btnRp.Text = restorePointFirst ? Loc.T("Restore point first: ON") : Loc.T("Restore point first: OFF");
            btnRp.ForeColor = restorePointFirst ? accent : text;
            btnRp.FlatAppearance.BorderColor = restorePointFirst ? accent : Skin.Line;
        }

        static string AppVersion()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            return v.Major + "." + v.Minor;
        }

        void StartGui(Entry e)
        {
            try
            {
                CommandRunner.Launch(e);
                Audit.Add("OK", e.Title, "Opened: " + e.Code);
                status.Text = Loc.T("Opened: ") + e.Name;
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223) { Audit.Add("CANCELLED", e.Title, "UAC approval was cancelled."); return; }
                Audit.Add("ERROR", e.Title, ex.Message);
                MessageBox.Show(this, Loc.F("Could not open {0}:\n{1}", e.Code, ex.Message), Loc.T("Launch failed"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Audit.Add("ERROR", e.Title, ex.Message);
                MessageBox.Show(this, Loc.F("Could not open {0}:\n{1}", e.Code, ex.Message), Loc.T("Launch failed"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        void Copy()
        {
            if (selected == null) return;
            Skin.SetClipboard(this, selected.Code);
            status.Text = Loc.T("Copied: ") + selected.Name;
        }

        void LaunchUrl(string value) { Skin.OpenUrl(this, value); }

        void OpenExternal(string exe)
        {
            try
            {
                var psi = new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };
                if (!isAdmin) psi.Verb = "runas";
                Process.Start(psi);
                Audit.Add("INFO", "Opened " + exe, isAdmin ? "Administrator" : "Elevated through UAC");
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223) return;   // UAC cancelled
                if (exe == "wt.exe" && (ex.NativeErrorCode == 2 || ex.NativeErrorCode == 1920))
                {
                    MessageBox.Show(this, Loc.T("Windows Terminal (wt.exe) is not installed on this PC."), Loc.T("Launch failed"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                MessageBox.Show(this, Loc.F("Could not open {0}:\n{1}", exe, ex.Message), Loc.T("Launch failed"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.F("Could not open {0}:\n{1}", exe, ex.Message), Loc.T("Launch failed"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        void RestartAsAdmin()
        {
            if (execution.Busy)
            {
                MessageBox.Show(this, Loc.T("An operation is still running. Stop it or wait for it to finish before closing."), Loc.T("Busy"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo(Application.ExecutablePath) { UseShellExecute = true, Verb = "runas" });
                Audit.Add("INFO", "Restart as administrator", "");
                Close();
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode != 1223) MessageBox.Show(this, ex.Message, Loc.T("Launch failed"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        void SetLang(bool polish)
        {
            if (Loc.Pl == polish) return;
            if (execution.Busy)
            {
                MessageBox.Show(this, Loc.T("An operation is still running. Stop it or wait for it to finish before closing."), Loc.T("Busy"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Loc.Pl = polish;
            PrepareRestart();
            Close();
        }

        // ---------- window rebuild (language / color): keep position, category, tab, console text and last command ----------
        void PrepareRestart()
        {
            Program.SavedBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            Program.SavedState = WindowState;
            Program.SavedCategory = current != null ? current.Id : null;
            Program.SavedTab = workspace.SelectedIndex;
            Program.SavedConsole = execution.Snapshot();
            Program.SavedLastKey = lastEntry != null ? lastEntry.Category.Id + "\n" + lastEntry.Title : null;
            Program.SavedLastParam = lastParam;
            Program.Restart = true;
        }

        void RestoreSession()
        {
            if (Program.SavedConsole != null) { execution.Restore(Program.SavedConsole); Program.SavedConsole = null; }
            if (Program.SavedLastKey != null)
            {
                string key = Program.SavedLastKey;
                lastEntry = catalog.SelectMany(c => c.Entries).FirstOrDefault(x => x.Category.Id + "\n" + x.Title == key);
                if (lastEntry == null && reportEntry != null && reportEntry.Category.Id + "\n" + reportEntry.Title == key) lastEntry = reportEntry;
                lastParam = Program.SavedLastParam;
                Program.SavedLastKey = null;
                Program.SavedLastParam = null;
            }
            if (Program.SavedTab > 0) workspace.SelectedIndex = Program.SavedTab;
            Program.SavedTab = 0;
        }

        void ShowThemeMenu()
        {
            var m = new ContextMenuStrip { BackColor = surface, ForeColor = text, ShowImageMargin = true };
            foreach (Theme t in Skin.Themes)
            {
                Theme th = t;
                var swatch = new Bitmap(16, 16);
                using (var g = Graphics.FromImage(swatch)) g.Clear(th.Accent);
                var mi = new ToolStripMenuItem(th.Label, swatch) { BackColor = surface, ForeColor = text, Checked = th == Skin.Current };
                mi.Click += (s, e) => SetTheme(th);
                m.Items.Add(mi);
            }
            m.Show(btnTheme, new Point(0, btnTheme.Height));
        }

        void SetTheme(Theme t)
        {
            if (t == Skin.Current) return;
            if (execution.Busy)
            {
                MessageBox.Show(this, Loc.T("An operation is still running. Stop it or wait for it to finish before closing."), Loc.T("Busy"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Skin.Apply(t);
            PrepareRestart();
            Close();
        }

        string PromptText(string label, string def)
        {
            using (var f = new Form())
            {
                f.Text = Loc.T("Input needed");
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.MaximizeBox = false;
                f.MinimizeBox = false;
                f.ShowInTaskbar = false;
                f.AutoScaleDimensions = new SizeF(96f, 96f);
                f.AutoScaleMode = AutoScaleMode.Dpi;
                f.ClientSize = new Size(480, 140);
                f.BackColor = surface;
                f.ForeColor = text;
                f.Font = new Font("Segoe UI", 9);
                var l = new Label { Left = 12, Top = 12, Width = 456, Height = 44, Text = label };
                var t = new TextBox { Left = 12, Top = 62, Width = 456, BackColor = bg, ForeColor = text, BorderStyle = BorderStyle.FixedSingle, Text = def ?? "" };
                var ok = Skin.Button(Loc.T("OK"), true);
                ok.AutoSize = false;
                ok.SetBounds(300, 100, 80, 30);
                ok.DialogResult = DialogResult.OK;
                var cancel = Skin.Button(Loc.T("Cancel"));
                cancel.AutoSize = false;
                cancel.SetBounds(388, 100, 80, 30);
                cancel.DialogResult = DialogResult.Cancel;
                f.Controls.Add(l);
                f.Controls.Add(t);
                f.Controls.Add(ok);
                f.Controls.Add(cancel);
                f.AcceptButton = ok;
                f.CancelButton = cancel;
                if (f.ShowDialog(this) != DialogResult.OK) return null;
                string v = t.Text.Trim();
                return v.Length == 0 ? null : v;
            }
        }
    }
}
