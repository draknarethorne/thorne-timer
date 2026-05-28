using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThorneTimer
{
    public partial class MiniView : Form
    {
        // Hide from Alt-Tab/task switcher by setting WS_EX_TOOLWINDOW
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW
                return cp;
            }
        }
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        public MiniView()
        {
            InitializeComponent();
        }

        public class MiniData
        {
            public enum ColorType
            {
                Normal,
                Pet,
                Buff,
                Ping
            }

            public string Name { get; set; }
            public string Remaining { get; set; }
            public ColorType TheColor { get; set; }
        }

        TableLayoutPanel tlpMain;
        double FormOpacity = 1.0f;
        float FontSize = 8;

        Color WarnForeColor = Color.White;
        Color WarnBackColor = Color.Red;
        string WarnTime = "00:00:30";

        Color ViewForeColor = Color.Yellow;
        Color ViewBackColor = Color.Black;

        string EmptyText = "No Timers";     // v0.6.0: Computed text to display when view is empty
        bool IsCharacterView = false;       // v0.6.0: Flag to always show character name header
        bool ShowWarning = true;            // v0.6.0: Per-view warning color control


        public void SetAppearance(int opacity, float fontSize, 
                                  Color warnForeColor, Color warnBackColor, String warnTime, 
                                  Color viewForeColor, Color viewBackColor,
                                  String emptyText,
                                  bool isCharacterView, bool showWarning)
        {
            FormOpacity = (double)opacity / 100.0f;
            FontSize = fontSize;

            WarnForeColor = warnForeColor;
            WarnBackColor = warnBackColor;
            WarnTime = "00:" + warnTime;

            ViewForeColor = viewForeColor;
            ViewBackColor = viewBackColor;

            EmptyText = emptyText;
            IsCharacterView = isCharacterView;
            ShowWarning = showWarning;

            this.BackColor = viewBackColor;
        }

        private void Control_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        public void LoadData(List<MiniData> data)
        {
            if (InvokeRequired)
            {
                try
                {
                    this.Invoke(new Action<List<MiniData>>(LoadData), new object[] { data });
                }
                catch { }

                return;
            }

            this.Opacity = FormOpacity;

            // Special handling for Character view: always show character name header
            if (IsCharacterView)
            {
                tlpMain = new TableLayoutPanel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    ColumnCount = 2,
                    RowCount = 0,
                    Margin = new Padding(0),
                    BackColor = Color.Transparent
                };

                for (int x = 0; x < tlpMain.ColumnCount; x++)
                {
                    tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                }

                // v0.6.0: Always show character name for Character view
                tlpMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                Label lblCharHeader = new Label() 
                { 
                    Text = EmptyText,  // Character name passed via SetAppearance
                    AutoSize = true, 
                    BackColor = ViewBackColor, 
                    ForeColor = ViewForeColor 
                };
                lblCharHeader.Font = new Font("Arial", FontSize, FontStyle.Bold);
                lblCharHeader.MouseDown += Control_MouseDown;
                lblCharHeader.Margin = new Padding(0);
                lblCharHeader.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;

                // Span character name across both columns
                tlpMain.Controls.Add(lblCharHeader, 0, tlpMain.RowCount);
                tlpMain.SetColumnSpan(lblCharHeader, 2);
                tlpMain.RowCount++;

                // Add separator line if there are timers
                if (data.Count > 0)
                {
                    tlpMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    Label lblSeparator = new Label() 
                    { 
                        Text = "─────────────",
                        AutoSize = true, 
                        BackColor = ViewBackColor, 
                        ForeColor = ViewForeColor 
                    };
                    lblSeparator.Font = new Font("Arial", FontSize - 1, FontStyle.Regular);
                    lblSeparator.MouseDown += Control_MouseDown;
                    lblSeparator.Margin = new Padding(0);
                    lblSeparator.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
                    tlpMain.Controls.Add(lblSeparator, 0, tlpMain.RowCount);
                    tlpMain.SetColumnSpan(lblSeparator, 2);
                    tlpMain.RowCount++;
                }

                // Add timer rows if any
                foreach (MiniData md in data)
                {
                    tlpMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                    Label lblName = new Label() { Text = md.Name, AutoSize = true };
                    lblName.Font = new Font("Arial", FontSize, FontStyle.Bold);
                    lblName.Margin = new Padding(0);
                    lblName.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
                    lblName.MouseDown += Control_MouseDown;
                    tlpMain.Controls.Add(lblName, 0, tlpMain.RowCount);

                    Label lblRemaining = new Label() { Text = md.Remaining, AutoSize = true };
                    lblRemaining.Font = new Font("Arial", FontSize, FontStyle.Bold);
                    lblRemaining.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
                    lblRemaining.Margin = new Padding(0);
                    lblRemaining.MouseDown += Control_MouseDown;
                    tlpMain.Controls.Add(lblRemaining, 1, tlpMain.RowCount);

                    // v0.6.0: All timers use per-view colors
                    lblName.BackColor = ViewBackColor;
                    lblName.ForeColor = ViewForeColor;
                    lblRemaining.BackColor = ViewBackColor;
                    lblRemaining.ForeColor = ViewForeColor;

                    // v0.6.0: Apply warning colors if enabled and timer is expiring
                    // ShowWarning column now controls warning display (no Ping exemption)
                    if (ShowWarning 
                        && TimerPlus.GetMilliseconds(md.Remaining) <= TimerPlus.GetMilliseconds(WarnTime))
                    {
                        lblRemaining.BackColor = WarnBackColor;
                        lblRemaining.ForeColor = WarnForeColor;
                    }

                    tlpMain.RowCount++;
                }
            }
            // Normal view handling (not Character style)
            else if (data.Count == 0)
            {
                // v0.6.0: Handle EmptyText for empty views
                // If EmptyText is empty string (HideEmpty behavior), don't display anything
                if (string.IsNullOrEmpty(EmptyText))
                {
                    tlpMain = new TableLayoutPanel
                    {
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        ColumnCount = 1,
                        RowCount = 0,
                        Margin = new Padding(0),
                        BackColor = Color.Transparent
                    };
                }
                else
                {
                    tlpMain = new TableLayoutPanel
                    {
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        ColumnCount = 1,
                        RowCount = 0,
                        Margin = new Padding(0),
                        BackColor = Color.Transparent
                    };

                    for (int x = 0; x < tlpMain.ColumnCount; x++)
                    {
                        tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    }

                    tlpMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                    Label lblNoActive = new Label() { Text = EmptyText, AutoSize = true, BackColor = ViewBackColor, ForeColor = ViewForeColor };
                    lblNoActive.Font = new Font("Arial", FontSize, FontStyle.Bold);
                    lblNoActive.MouseDown += Control_MouseDown;
                    lblNoActive.Margin = new Padding(0);
                    lblNoActive.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
                    tlpMain.Controls.Add(lblNoActive, 0, tlpMain.RowCount);

                    tlpMain.RowCount++;
                }
            }
            else
            {
                tlpMain = new TableLayoutPanel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    ColumnCount = 2,
                    RowCount = 0,
                    Margin = new Padding(0)
                };

                for (int x = 0; x < tlpMain.ColumnCount; x++)
                {
                    tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                }

                foreach (MiniData md in data)
                {
                    tlpMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                    Label lblName = new Label() { Text = md.Name, AutoSize = true };
                    lblName.Font = new Font("Arial", FontSize, FontStyle.Bold);
                    lblName.Margin = new Padding(0);
                    lblName.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
                    lblName.MouseDown += Control_MouseDown;
                    tlpMain.Controls.Add(lblName, 0, tlpMain.RowCount);

                    Label lblRemaining = new Label() { Text = md.Remaining, AutoSize = true };
                    lblRemaining.Font = new Font("Arial", FontSize, FontStyle.Bold);
                    lblRemaining.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
                    lblRemaining.Margin = new Padding(0);
                    lblRemaining.MouseDown += Control_MouseDown;
                    tlpMain.Controls.Add(lblRemaining, 1, tlpMain.RowCount);

                    // v0.6.0: All timers use per-view colors
                    lblName.BackColor = ViewBackColor;
                    lblName.ForeColor = ViewForeColor;
                    lblRemaining.BackColor = ViewBackColor;
                    lblRemaining.ForeColor = ViewForeColor;

                    // v0.6.0: Apply warning colors if enabled and timer is expiring
                    // ShowWarning column now controls warning display (no Ping exemption)
                    if (ShowWarning 
                        && TimerPlus.GetMilliseconds(md.Remaining) <= TimerPlus.GetMilliseconds(WarnTime))
                    {
                        lblRemaining.BackColor = WarnBackColor;
                        lblRemaining.ForeColor = WarnForeColor;
                    }

                    tlpMain.RowCount++;
                }
            }
            tlpMain.MouseDown += Control_MouseDown;
            Controls.Add(tlpMain);

            foreach (Control c in Controls)
            {
                if ((string)c.Tag == "TLP")
                    Controls.Remove(c);
            }
            tlpMain.Tag = "TLP";
        }

        private void MiniView_Load(object sender, EventArgs e)
        {
            this.MouseDown += Control_MouseDown;
        }
    }
}
