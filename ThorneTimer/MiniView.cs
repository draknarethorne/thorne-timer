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
        
        Color NormForeColor = Color.Yellow;
        Color NormBackColor = Color.Black;

        Color BuffForeColor = Color.Orange;
        Color BuffBackColor = Color.Black;
        
        Color WarnForeColor = Color.White;
        Color WarnBackColor = Color.Red;
        string WarnTime = "00:00:30";
        
        Color PingForeColor = Color.Green;
        Color PingBackColor = Color.Black;

        Color ViewForeColor = Color.Yellow;
        Color ViewBackColor = Color.Black;

        string TimerText = "No Timers";

        public void SetAppearance(int opacity, float fontSize, 
                                  Color normForeColor, Color normBackColor, 
                                  Color warnForeColor, Color warnBackColor, String warnTime, 
                                  Color pingForeColor, Color pingBackColor,
                                  Color buffForeColor, Color buffBackColor,
                                  String timerText,
                                  Color viewForeColor, Color viewBackColor)
        {
            FormOpacity = (double)opacity / 100.0f;
            FontSize = fontSize;

            NormForeColor = normForeColor;
            NormBackColor = normBackColor;

            WarnForeColor = warnForeColor;
            WarnBackColor = warnBackColor;
            WarnTime = "00:" + warnTime;

            PingForeColor = pingForeColor;
            PingBackColor = pingBackColor;

            BuffForeColor = buffForeColor;
            BuffBackColor = buffBackColor;

            ViewForeColor = viewForeColor;
            ViewBackColor = viewBackColor;

            TimerText = timerText;

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

            if (data.Count == 0)
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

                Label lblNoActive = new Label() { Text = TimerText, AutoSize = true, BackColor = ViewBackColor, ForeColor = ViewForeColor };
                lblNoActive.Font = new Font("Arial", FontSize, FontStyle.Bold);
                lblNoActive.MouseDown += Control_MouseDown;
                lblNoActive.Margin = new Padding(0);
                lblNoActive.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
                tlpMain.Controls.Add(lblNoActive, 0, tlpMain.RowCount);

                tlpMain.RowCount++;
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

                    switch (md.TheColor)
                    {
                        case MiniData.ColorType.Pet:
                            lblName.BackColor = BuffBackColor;
                            lblName.ForeColor = BuffForeColor;
                            lblRemaining.BackColor = BuffBackColor;
                            lblRemaining.ForeColor = BuffForeColor;
                            break;
                        case MiniData.ColorType.Buff:
                            lblName.BackColor = BuffBackColor;
                            lblName.ForeColor = BuffForeColor;
                            lblRemaining.BackColor = BuffBackColor;
                            lblRemaining.ForeColor = BuffForeColor;
                            break;
                        case MiniData.ColorType.Ping:
                            lblName.BackColor = PingBackColor;
                            lblName.ForeColor = PingForeColor;
                            lblRemaining.BackColor = PingBackColor;
                            lblRemaining.ForeColor = PingForeColor;
                            break;

                        default:
                            lblName.BackColor = NormBackColor;
                            lblName.ForeColor = NormForeColor;
                            lblRemaining.BackColor = NormBackColor;
                            lblRemaining.ForeColor = NormForeColor;
                            break;
                    }

                    if (md.TheColor != MiniData.ColorType.Ping
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
