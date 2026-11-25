using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Forms;
namespace RepairToolkit
{
    public class WorkspacePage : Panel
    {
        public WorkspacePage() { Dock = DockStyle.Fill; }
    }

    public class WorkspaceTabs : UserControl
    {
        FlowLayoutPanel header;
        Panel host;
        WorkspacePage selected;
        public event EventHandler SelectedIndexChanged;
        public PageCollection TabPages { get; private set; }

        public WorkspaceTabs()
        {
            BackColor = Skin.Bg;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = new Padding(0), Padding = new Padding(0) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(layout);
            header = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Skin.Surface, Padding = new Padding(10, 5, 0, 0), Margin = new Padding(0), WrapContents = false, AutoScroll = true };
            host = new Panel { Dock = DockStyle.Fill, BackColor = Skin.Bg, Margin = new Padding(0) };
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(host, 0, 1);
            TabPages = new PageCollection(this);
        }

        public WorkspacePage SelectedTab
        {
            get { return selected; }
            set
            {
                if (value == null || selected == value) return;
                selected = value;
                foreach (WorkspacePage page in TabPages) page.Visible = page == value;
                value.BringToFront();
                foreach (Control c in header.Controls) c.Invalidate();
                var handler = SelectedIndexChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        public int SelectedIndex
        {
            get { return TabPages.IndexOf(selected); }
            set { if (value >= 0 && value < TabPages.Count) SelectedTab = TabPages[value]; }
        }

        void Add(WorkspacePage page)
        {
            host.Controls.Add(page);
            var button = new NavButton(this, page) { Size = new Size(190, 36), Margin = new Padding(0, 0, 4, 0) };
            button.Click += (s, e) => SelectedTab = page;
            header.Controls.Add(button);
            page.Visible = false;
            if (selected == null) SelectedTab = page;
        }

        public class PageCollection : Collection<WorkspacePage>
        {
            WorkspaceTabs owner;
            public PageCollection(WorkspaceTabs owner) { this.owner = owner; }
            protected override void InsertItem(int index, WorkspacePage page)
            {
                base.InsertItem(index, page);
                owner.Add(page);
            }
        }

        class NavButton : Button
        {
            WorkspaceTabs owner;
            WorkspacePage page;

            public NavButton(WorkspaceTabs owner, WorkspacePage page)
            {
                this.owner = owner;
                this.page = page;
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                Cursor = Cursors.Hand;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                bool active = owner.SelectedTab == page;
                using (var brush = new SolidBrush(active ? Skin.Panel : Skin.Surface))
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                using (var font = new Font("Segoe UI", 9, active ? FontStyle.Bold : FontStyle.Regular))
                    TextRenderer.DrawText(e.Graphics, page.Text, font, ClientRectangle, active ? Skin.Accent : Skin.Muted,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                if (active)
                    using (var pen = new Pen(Skin.Accent, 2))
                        e.Graphics.DrawLine(pen, 8, Height - 2, Width - 8, Height - 2);
            }
        }
    }
}
