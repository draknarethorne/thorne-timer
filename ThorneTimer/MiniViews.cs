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

        public StylesRepository Styles { get; set; }

        // v0.6.0: Track active character name for Character style mini view
        private string activeCharacterName = "All Characters";

        /// <summary>
        /// Pairs a database view record with its live MiniView form.
        /// </summary>
        private class ViewEntry
        {
            public Database.ViewPositionData Data { get; set; }
            public MiniView Form { get; set; }
        }

        private List<ViewEntry> activeViews = new List<ViewEntry>();

        private DateTime lastTime = DateTime.MinValue;

        public class GridData
        {
            public long ID { get; set; }
            public string Name { get; set; }
            public long ActiveYn { get; set; }
            public string StyleFilter { get; set; }
            public int PositionX { get; set; }
            public int PositionY { get; set; }
            public int SortOrder { get; set; }
            public int ForeColor { get; set; }      // v0.6.0: Per-view foreground color
            public int BackColor { get; set; }      // v0.6.0: Per-view background color
            public int ShowWarning { get; set; }    // v0.6.0: Per-view warning color control
            public string EmptyBehavior { get; set; }  // v0.6.0: Per-view empty display behavior
            public string Example { get; set; }     // v0.6.0: Read-only preview (populated by UI layer)
        }

        public bool MiniViewsActive()
        {
            return activeViews.Count > 0;
        }

        /// <summary>
        /// Saves current positions, destroys all mini views, and recreates
        /// them from the database.  Used when the user activates or
        /// deactivates a view while mini views are visible.
        /// </summary>
        public void RefreshMiniViews(SQLiteConnection con, string activeCharacterID)
        {
            if (activeViews.Count == 0) return;

            // Persist current positions before tearing down
            Dictionary<int, Point> positions = GetCurrentViewPositions();
            Database.SaveViewPositions(con, positions);

            DestroyMiniViews();
            CreateMiniViews(con, activeCharacterID);
        }

        public bool MiniViewsHidden()
        {
            return activeViews.Count == 0;
        }

        public MiniView MV()
        {
            return activeViews.Count > 0 ? activeViews[0].Form : null;
        }

        /// <summary>
        /// Gets the current positions of all views.
        /// Returns a dictionary keyed by database ID.
        /// </summary>
        public Dictionary<int, Point> GetCurrentViewPositions()
        {
            Dictionary<int, Point> positions = new Dictionary<int, Point>();

            foreach (var entry in activeViews)
            {
                if (entry.Form != null)
                    positions[entry.Data.ID] = entry.Form.Location;
            }

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

            if (activeViews.Count == 0)
            {
                // Get the active character name for the Character mini view
                // Use same approach as status bar for consistency
                long charID = 0;
                long.TryParse(activeCharacterID, out charID);
                if (charID == 0)
                {
                    activeCharacterName = "All Characters";
                }
                else
                {
                    var character = Database.GetCharacter(con, activeCharacterID);
                    activeCharacterName = string.IsNullOrEmpty(character.Name) ? "All Characters" : character.Name;
                }

                // Load view definitions from database
                List<Database.ViewPositionData> views = Database.GetViewPositions(con);

                foreach (var viewData in views)
                {
                    // Skip inactive views
                    if (viewData.ActiveYn != 1)
                        continue;

                    // v0.6.0: All views use ActiveYn (no special Ping handling)
                    bool showView = (viewData.ActiveYn == 1);

                    string title = string.IsNullOrEmpty(viewData.Name) ? viewData.StyleFilter + " Timers" : viewData.Name;

                    // Special handling for Character style: use character name as title
                    if (viewData.StyleFilter == "Character")
                    {
                        title = string.IsNullOrEmpty(viewData.Name) ? activeCharacterName : viewData.Name;
                    }

                    MiniView form = CreateMiniView(viewData.PositionX, viewData.PositionY, showView, title);

                    activeViews.Add(new ViewEntry { Data = viewData, Form = form });
                }

                UpdateMiniAppearance();

                result = true;
            }

            return result;
        }

        public bool UpdateMiniAppearance()
        {
            bool result = true;

            foreach (var entry in activeViews)
            {
                StyleData style = Styles?.GetStyle(entry.Data.StyleFilter);
                int viewFore = style == null ? Color.Black.ToArgb() : style.ForeColor;
                int viewBack = style == null ? Color.Yellow.ToArgb() : style.BackColor;
                bool showWarning = (entry.Data.ShowWarning != 0);
                string emptyBehaviorSetting = entry.Data.EmptyBehavior ?? "ViewName";
                bool showView = (entry.Data.ActiveYn != 0);

                // Compute empty text based on EmptyBehavior setting
                string emptyText;
                switch (emptyBehaviorSetting)
                {
                    case "CharacterName":
                        emptyText = activeCharacterName;
                        break;
                    case "ViewName":
                        emptyText = string.IsNullOrEmpty(entry.Data.Name) ? entry.Data.StyleFilter : entry.Data.Name;
                        break;
                    case "Spaces":
                        emptyText = "          ";  // 10 spaces for minimal presence
                        break;
                    case "HideEmpty":
                        emptyText = "";  // Empty (view will be hidden when no timers)
                        break;
                    default:
                        emptyText = entry.Data.Name ?? entry.Data.StyleFilter;
                        break;
                }

                bool isCharacterView = (entry.Data.StyleFilter == "Character");
                SetMiniAppearance(entry.Form, emptyText, showView, viewFore, viewBack, isCharacterView, showWarning);
            }

            return result;
        }

        public bool DestroyMiniViews()
        {
            bool result = false;

            if (activeViews.Count > 0)
            {
                foreach (var entry in activeViews)
                {
                    DestroyMiniView(entry.Form);
                }
                activeViews.Clear();

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

        private void SetMiniAppearance(MiniView view, String emptyText, bool showView, int viewForeColor, int viewBackColor, bool isCharacterView, bool showWarning)
        {
            if (view != null)
            {
                // v0.6.0: Pass per-view colors, emptyText, and settings; warn colors still global
                view.SetAppearance(mvOpacity, mvFontSize, 
                             Color.FromArgb(mvWarnForeColor), Color.FromArgb(mvWarnBackColor), mvWarnTime,
                             Color.FromArgb(viewForeColor), Color.FromArgb(viewBackColor),
                             emptyText, isCharacterView, showWarning);
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
            // v0.6.0: Simplified - no special Ping handling
            // All timer visibility now controlled by timer state only
            return Timers.TimerRunning(btnString) || Timers.PingTimer(btnString);
        }

        public bool ShowPing()
        {
            return (mvShowPing == 1);
        }

        /// <summary>
        /// Updates the active character name for the Character style mini view.
        /// Call this when the character changes without recreating mini views.
        /// Uses same approach as status bar for consistency.
        /// </summary>
        public void UpdateActiveCharacter(SQLiteConnection con, string activeCharacterID)
        {
            long charID = 0;
            long.TryParse(activeCharacterID, out charID);
            if (charID == 0)
            {
                activeCharacterName = "All Characters";
            }
            else
            {
                var character = Database.GetCharacter(con, activeCharacterID);
                activeCharacterName = string.IsNullOrEmpty(character.Name) ? "All Characters" : character.Name;
            }

            // Update mini view appearances to reflect the new character name
            if (activeViews.Count > 0)
            {
                UpdateMiniAppearance();
            }
        }

        public void UpdateMiniTimers(List<MiniTimerData> timerData, bool bForce=true)
        {
            // Check current time vs. last time to prevent excessive updates
            DateTime currTime = DateTime.Now;
            if ((((currTime.Subtract(lastTime).TotalMilliseconds) > 999) || bForce) && (activeViews.Count > 0))
            {
                lastTime = currTime;

                // Build a data list for each active view keyed by StyleFilter
                Dictionary<string, List<MiniView.MiniData>> viewData = new Dictionary<string, List<MiniView.MiniData>>();
                foreach (var entry in activeViews)
                {
                    if (!viewData.ContainsKey(entry.Data.StyleFilter))
                        viewData[entry.Data.StyleFilter] = new List<MiniView.MiniData>();
                }

                foreach (var td in timerData)
                {
                    if (!ShowMiniTimer(td.ButtonState)) continue;

                    string timerStyle = string.IsNullOrEmpty(td.Style) ? "Normal" : td.Style;

                    MiniView.MiniData.ColorType colorType;
                    switch (timerStyle)
                    {
                        case "Pet": colorType = MiniView.MiniData.ColorType.Pet; break;
                        case "Buff": colorType = MiniView.MiniData.ColorType.Buff; break;
                        case "Ping": colorType = MiniView.MiniData.ColorType.Ping; break;
                        default: colorType = MiniView.MiniData.ColorType.Normal; break;
                    }

                    MiniView.MiniData md = new MiniView.MiniData
                    {
                        Name = td.Name,
                        Remaining = td.Remaining,
                        TheColor = colorType
                    };

                    // Route to view(s) whose StyleFilter matches this timer's Style
                    if (viewData.ContainsKey(timerStyle))
                    {
                        viewData[timerStyle].Add(md);
                    }
                }

                // Push data to each view
                foreach (var entry in activeViews)
                {
                    if (viewData.ContainsKey(entry.Data.StyleFilter))
                        entry.Form.LoadData(viewData[entry.Data.StyleFilter]);
                    else
                        entry.Form.LoadData(new List<MiniView.MiniData>());
                }
            }
        }

        public void UpdateMiniTimers(DataGridView grdTimers, bool bForce=true)
        {
            // Check current time vs. last time to prevent excessive updates to the mini view since it scans all rows in the grid every time
            DateTime currTime = DateTime.Now;
            if ((((currTime.Subtract(lastTime).TotalMilliseconds) > 999) || bForce) && (activeViews.Count > 0))
            {
                lastTime = currTime;

                // Build a data list for each active view keyed by StyleFilter
                Dictionary<string, List<MiniView.MiniData>> viewData = new Dictionary<string, List<MiniView.MiniData>>();
                foreach (var entry in activeViews)
                {
                    if (!viewData.ContainsKey(entry.Data.StyleFilter))
                        viewData[entry.Data.StyleFilter] = new List<MiniView.MiniData>();
                }

                for (int r = 0; r < grdTimers.Rows.Count; r++)
                {
                    DataGridViewRow row = grdTimers.Rows[r];
                    DataGridViewCell cellStartStop = row.Cells[grdTimers.Columns["StartStop"].Index];

                    if (ShowMiniTimer((string)cellStartStop.Value))
                    {
                        DataGridViewCell cellName = row.Cells[grdTimers.Columns["Name"].Index];
                        DataGridViewCell cellRemaining = row.Cells[grdTimers.Columns["Remaining"].Index];
                        DataGridViewCell cellStyle = row.Cells[grdTimers.Columns["Style"].Index];
                        string timerStyle = Convert.ToString(cellStyle.Value);
                        if (string.IsNullOrEmpty(timerStyle)) timerStyle = "Normal";

                        MiniView.MiniData.ColorType colorType;
                        switch (timerStyle)
                        {
                            case "Pet": colorType = MiniView.MiniData.ColorType.Pet; break;
                            case "Buff": colorType = MiniView.MiniData.ColorType.Buff; break;
                            case "Ping": colorType = MiniView.MiniData.ColorType.Ping; break;
                            default: colorType = MiniView.MiniData.ColorType.Normal; break;
                        }

                        MiniView.MiniData md = new MiniView.MiniData
                        {
                            Name = (string)cellName.Value,
                            Remaining = (string)cellRemaining.Value,
                            TheColor = colorType
                        };

                        // Route to view(s) whose StyleFilter matches this timer's Style
                        if (viewData.ContainsKey(timerStyle))
                        {
                            viewData[timerStyle].Add(md);
                        }
                    }
                }

                // Push data to each view
                foreach (var entry in activeViews)
                {
                    if (viewData.ContainsKey(entry.Data.StyleFilter))
                        entry.Form.LoadData(viewData[entry.Data.StyleFilter]);
                    else
                        entry.Form.LoadData(new List<MiniView.MiniData>());
                }
            }
        }
    }
}
