using System;
using System.IO;
using System.Windows.Forms;

namespace ThorneTimer
{
    /// <summary>
    /// Manages the "Open Recent" menu and the persisted MRU list stored
    /// in <see cref="Properties.Settings"/>.  Owns add / move-to-top /
    /// trim-to-cap behavior and rebuilds the menu items on demand.
    ///
    /// FormMain wires this up once at startup and after every Open / New
    /// /Save As, then calls <see cref="Refresh"/>.  When a recent item is
    /// clicked, the manager invokes the supplied open-database callback.
    /// </summary>
    internal class RecentDatabasesManager
    {
        private const int MaxRecentCount = 10;

        private readonly ToolStripMenuItem menu;
        private readonly Action<string> openDatabase;

        public RecentDatabasesManager(ToolStripMenuItem openRecentMenu, Action<string> openDatabase)
        {
            this.menu = openRecentMenu ?? throw new ArgumentNullException(nameof(openRecentMenu));
            this.openDatabase = openDatabase ?? throw new ArgumentNullException(nameof(openDatabase));
        }

        /// <summary>
        /// Adds the given path to the front of the MRU list (moving it if
        /// it was already present) and trims the list to the maximum count.
        /// </summary>
        public void Add(string dbPath)
        {
            if (string.IsNullOrEmpty(dbPath)) return;

            var recent = Properties.Settings.Default.RecentDatabases;
            if (recent == null)
            {
                recent = new System.Collections.Specialized.StringCollection();
                Properties.Settings.Default.RecentDatabases = recent;
            }

            // Remove if already present (so it moves to top)
            for (int i = recent.Count - 1; i >= 0; i--)
            {
                if (string.Equals(recent[i], dbPath, StringComparison.OrdinalIgnoreCase))
                {
                    recent.RemoveAt(i);
                }
            }

            recent.Insert(0, dbPath);

            while (recent.Count > MaxRecentCount)
            {
                recent.RemoveAt(recent.Count - 1);
            }

            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Rebuilds the Open Recent menu from the current MRU list.
        /// Disables the menu when no entries exist.  Missing files show
        /// a friendly "Tome Not Found" message instead of opening.
        /// </summary>
        public void Refresh()
        {
            menu.DropDownItems.Clear();

            var recent = Properties.Settings.Default.RecentDatabases;
            if (recent == null || recent.Count == 0)
            {
                menu.Enabled = false;
                return;
            }

            menu.Enabled = true;
            foreach (string path in recent)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                var capturedPath = path;
                var item = new ToolStripMenuItem(capturedPath);
                item.Click += (s, ev) =>
                {
                    if (File.Exists(capturedPath))
                    {
                        openDatabase(capturedPath);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Tome not found:\n" + capturedPath,
                            "Tome Not Found",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                };
                menu.DropDownItems.Add(item);
            }
        }
    }
}
