using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using System.Windows.Forms;
using System.Data.SQLite;

namespace ThorneTimer
{
    class MiniViews
    {
        public int mvOpacity = 100;
        public int mvFontSize = 8;

        public int mvNormForeColor = Color.Yellow.ToArgb();
        public int mvNormBackColor = Color.Black.ToArgb();

        public int mvWarnForeColor = Color.White.ToArgb();
        public int mvWarnBackColor = Color.Red.ToArgb();
        public string mvWarnTime = "00:30";

        public int mvShowPing = 1;
        public int mvPingForeColor = Color.LightGreen.ToArgb();
        public int mvPingBackColor = Color.Black.ToArgb();
        public string mvPingTime = "00:30";

        public int mvBuffForeColor = Color.Orange.ToArgb();
        public int mvBuffBackColor = Color.Black.ToArgb();

        private MiniView miniView = null;
        private MiniView petView = null;
        private MiniView buffView = null;
        private MiniView pingView = null;

        private DateTime lastTime = DateTime.MinValue;

        public class GridData
        {
            public long ID { get; set; }
            public string Name { get; set; }
        }

        public bool MiniViewsActive()
        {
            return (bool)(miniView != null);
        }

        public bool MiniViewsHidden()
        {
            return (bool)(miniView == null);
        }

        public MiniView MV()
        {
            return miniView;
        }

        /// <summary>
        /// Gets the current positions of all views.
        /// Returns a dictionary keyed by ViewType (Normal, Pet, Buff, Ping).
        /// </summary>
        public Dictionary<string, Point> GetCurrentViewPositions()
        {
            Dictionary<string, Point> positions = new Dictionary<string, Point>();

            if (miniView != null)
                positions["Normal"] = miniView.Location;
            if (petView != null)
                positions["Pet"] = petView.Location;
            if (buffView != null)
                positions["Buff"] = buffView.Location;
            if (pingView != null)
                positions["Ping"] = pingView.Location;

            return positions;
        }

        public void AddView(SQLiteConnection con, DataGridView grdViews)
        {
            DataGridViewRow row = grdViews.CurrentRow;

            if (row == null)
            {
                List<MiniViews.GridData> data = Database.GetViews(con);

                GridData gd = new MiniViews.GridData
                {
                    ID = -1
                };
                data.Add(gd);

                grdViews.DataSource = data;

                grdViews.CurrentCell = grdViews.Rows[grdViews.Rows.Count - 1].Cells[grdViews.Columns["Name"].Index];
                grdViews.BeginEdit(true);
            }
        }

        public bool DeleteView(SQLiteConnection con, DataGridView grdViews)
        {
            bool result = false;

            if (grdViews.CurrentCell != null)
            {
                if (MessageBox.Show("Are you sure you want to delete this view?", "Delete View", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == System.Windows.Forms.DialogResult.Yes)
                {
                    DataGridViewCell idCell = grdViews.Rows[grdViews.CurrentCell.RowIndex].Cells[grdViews.Columns["ID"].Index];
                    Database.DeleteView(con, Convert.ToString(idCell.Value));

                    grdViews.DataSource = Database.GetViews(con);

                    result = true;
                }
            }

            return result;
        }

        private MiniView CreateMiniView(int x = 100, int y = 100, bool showView = true, string title = null)
        {
            MiniView view = new MiniView
            {
                StartPosition = FormStartPosition.Manual
            };
            if (!string.IsNullOrEmpty(title))
                view.Text = title;
            Point loc = FormMain.EnsureVisibleOnScreen(new Point(x, y), view.Size);
            view.Location = loc;

            if (showView)
            {
                view.Show();
                view.BringToFront();
            }
            else
            {
                view.Hide();
                view.SendToBack();
            }

            return view;
        }

        public bool CreateMiniViews(SQLiteConnection con, string activeCharacterID)
        {
            bool result = false;

            if (miniView == null)
            {
                // Load positions from database (falls back to defaults if not found)
                Dictionary<string, Database.ViewPositionData> positions = Database.GetViewPositions(con);

                // Get positions and names for each view type, with fallbacks
                int normalX = 100, normalY = 100;
                int petX = 300, petY = 100;
                int buffX = 500, buffY = 100;
                int pingX = 1100, pingY = 100;
                string normalName = "Normal Timers";
                string petName = "Pet Timers";
                string buffName = "Buff Timers";
                string pingName = "Ping Timers";

                if (positions.ContainsKey("Normal"))
                {
                    normalX = positions["Normal"].PositionX;
                    normalY = positions["Normal"].PositionY;
                    if (!string.IsNullOrEmpty(positions["Normal"].Name)) normalName = positions["Normal"].Name;
                }
                if (positions.ContainsKey("Pet"))
                {
                    petX = positions["Pet"].PositionX;
                    petY = positions["Pet"].PositionY;
                    if (!string.IsNullOrEmpty(positions["Pet"].Name)) petName = positions["Pet"].Name;
                }
                if (positions.ContainsKey("Buff"))
                {
                    buffX = positions["Buff"].PositionX;
                    buffY = positions["Buff"].PositionY;
                    if (!string.IsNullOrEmpty(positions["Buff"].Name)) buffName = positions["Buff"].Name;
                }
                if (positions.ContainsKey("Ping"))
                {
                    pingX = positions["Ping"].PositionX;
                    pingY = positions["Ping"].PositionY;
                    if (!string.IsNullOrEmpty(positions["Ping"].Name)) pingName = positions["Ping"].Name;
                }

                miniView = CreateMiniView(normalX, normalY, true, normalName);
                petView = CreateMiniView(petX, petY, true, petName);
                buffView = CreateMiniView(buffX, buffY, true, buffName);
                pingView = CreateMiniView(pingX, pingY, ShowPing(), pingName);

                UpdateMiniAppearance();

                result = true;
            }

            return result;
        }

        public bool UpdateMiniAppearance()
        {
            bool result = true;

            SetMiniAppearance(miniView, "Timers", true, mvNormForeColor, mvNormBackColor);
            SetMiniAppearance(petView, "Pet", true, mvBuffForeColor, mvBuffBackColor);
            SetMiniAppearance(buffView, "Buffs", true, mvBuffForeColor, mvBuffBackColor);
            SetMiniAppearance(pingView, "Ping", ShowPing(), mvPingForeColor, mvPingBackColor);

            return result;
        }

        public bool DestroyMiniViews()
        {
            bool result = false;

            if (miniView != null)
            {
                miniView = DestroyMiniView(miniView);
                buffView = DestroyMiniView(buffView);
                petView = DestroyMiniView(petView);
                pingView = DestroyMiniView(pingView);

                result = true;
            }

            return result;
        }

        private MiniView DestroyMiniView(MiniView view)
        {
            if (view != null)
            {
                view.Close();
                view.Dispose();
            }

            return null;
        }

        private void SetMiniAppearance(MiniView view, String timerText, bool showView, int viewForeColor, int viewBackColor)
        {
            if (view != null)
            {
                view.SetAppearance(mvOpacity, mvFontSize, Color.FromArgb(mvNormForeColor), Color.FromArgb(mvNormBackColor),
                             Color.FromArgb(mvWarnForeColor), Color.FromArgb(mvWarnBackColor), mvWarnTime,
                             Color.FromArgb(mvPingForeColor), Color.FromArgb(mvPingBackColor),
                             Color.FromArgb(mvBuffForeColor), Color.FromArgb(mvBuffBackColor),
                             timerText,
                             Color.FromArgb(viewForeColor), Color.FromArgb(viewBackColor));
                if (showView)
                {
                    view.Show();
                    view.BringToFront();
                }
                else
                {
                    view.Hide();
                    view.SendToBack();
                }
            }
        }

        private bool ShowMiniTimer(string btnString)
        {
            return (Timers.TimerRunning(btnString) || (Timers.PingTimer(btnString) && ShowPing()));
        }

        public bool ShowPing()
        {
            return (mvShowPing == 1);
        }

        public void UpdateMiniTimers(DataGridView grdTimers, bool bForce=true)
        {
            // Check current time vs. last time to prevent excessive updates to the mini view since it scans all rows in the grid every time
            // This could use more refactoring, but this works for now.
            DateTime currTime = DateTime.Now;
            if ((((currTime.Subtract(lastTime).TotalMilliseconds) > 999) || bForce) && (miniView != null))
            {
                lastTime = currTime;

                List<MiniView.MiniData> miniData = new List<MiniView.MiniData>();
                List<MiniView.MiniData> petData = new List<MiniView.MiniData>();
                List<MiniView.MiniData> buffData = new List<MiniView.MiniData>();
                List<MiniView.MiniData> pingData = new List<MiniView.MiniData>();

                for (int r = 0; r < grdTimers.Rows.Count; r++)
                {
                    DataGridViewRow row = grdTimers.Rows[r];

                    DataGridViewCell cellStartStop = row.Cells[grdTimers.Columns["StartStop"].Index];

                    if (ShowMiniTimer((string)cellStartStop.Value))
                    {
                        DataGridViewCell cellName = row.Cells[grdTimers.Columns["Name"].Index];
                        DataGridViewCell cellRemaining = row.Cells[grdTimers.Columns["Remaining"].Index];

                        MiniView.MiniData md = new MiniView.MiniData
                        {
                            Name = (string)cellName.Value,
                            Remaining = (string)cellRemaining.Value
                        };

                        if (Timers.PetTimer((string)cellStartStop.Value))
                        {
                            md.TheColor = MiniView.MiniData.ColorType.Pet;
                            petData.Add(md);
                        }
                        else if (Timers.BuffTimer((string)cellStartStop.Value))
                        {
                            md.TheColor = MiniView.MiniData.ColorType.Buff;
                            buffData.Add(md);
                        }
                        else if (Timers.PingTimer((string)cellStartStop.Value))
                        {
                            md.TheColor = MiniView.MiniData.ColorType.Ping;
                            pingData.Add(md);
                        }
                        else
                        {
                            md.TheColor = MiniView.MiniData.ColorType.Normal;
                            miniData.Add(md);
                        }
                    }
                }

                miniView.LoadData(miniData);
                petView.LoadData(petData);
                buffView.LoadData(buffData);
                pingView.LoadData(pingData);
            }
        }
    }
}
