using System;
using System.Drawing;
using System.Windows.Forms;

namespace ThorneTimer
{
    /// <summary>
    /// Manages window position / size persistence and multi-monitor
    /// visibility for FormMain.  Owns the save/restore round-trip via
    /// <see cref="Properties.Settings"/>, the one-time size nudge on
    /// version upgrade, and the offscreen clamp for moved or removed
    /// monitors.
    ///
    /// Compact-vs-full width tracking remains in FormMain because it is
    /// tightly coupled to the toolbar toggle and tab layout state.
    /// </summary>
    internal class WindowPositionManager
    {
        private readonly Form form;

        public WindowPositionManager(Form form)
        {
            this.form = form ?? throw new ArgumentNullException(nameof(form));
        }

        /// <summary>
        /// Restores window state, location, and size from persisted settings.
        /// When <paramref name="needsSizeNudge"/> is true, the saved size is
        /// bumped up to satisfy the minimum default — used as a one-time
        /// upgrade nudge so existing users see the improved default layout.
        /// </summary>
        public void Restore(bool needsSizeNudge, int defaultFullViewWidth, bool isCompactView)
        {
            if (!Properties.Settings.Default.HasSetDefaults) return;

            form.WindowState = Properties.Settings.Default.WindowState;
            Point loc = Properties.Settings.Default.Location;
            Size sz = Properties.Settings.Default.Size;

            if (needsSizeNudge)
            {
                // Always nudge height; only nudge width for full-view users
                // so compact-view users keep their narrower window.
                sz = new Size(
                    isCompactView ? sz.Width : Math.Max(sz.Width, defaultFullViewWidth),
                    Math.Max(sz.Height, 700));
            }

            form.Location = EnsureVisibleOnScreen(loc, sz);
            form.Size = sz;
        }

        /// <summary>
        /// Saves the current window state, location, and size to settings.
        /// Uses <see cref="Form.RestoreBounds"/> when minimized/maximized so
        /// the restore target is preserved through close/reopen cycles.
        /// </summary>
        public void Save()
        {
            Properties.Settings.Default.WindowState = form.WindowState;

            if (form.WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.Location = form.Location;
                Properties.Settings.Default.Size = form.Size;
            }
            else
            {
                Properties.Settings.Default.Location = form.RestoreBounds.Location;
                Properties.Settings.Default.Size = form.RestoreBounds.Size;
            }

            Properties.Settings.Default.HasSetDefaults = true;
        }

        /// <summary>
        /// Returns true if the given rectangle is at least partially visible
        /// on any connected monitor.  Returns false only when 100% of the
        /// window would be offscreen.
        /// </summary>
        public static bool IsVisibleOnAnyScreen(Rectangle rect)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(rect))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Ensures a window position is visible on at least one monitor.
        /// If the window is entirely offscreen, clamps it to 10 pixels
        /// inside the nearest screen's working area.
        /// </summary>
        public static Point EnsureVisibleOnScreen(Point location, Size size)
        {
            Rectangle windowRect = new Rectangle(location, size);
            if (IsVisibleOnAnyScreen(windowRect))
                return location;

            const int inset = 10;
            Screen nearest = Screen.FromPoint(location);
            Rectangle area = nearest.WorkingArea;
            int x = Math.Max(area.Left + inset, Math.Min(location.X, area.Right - size.Width - inset));
            int y = Math.Max(area.Top + inset, Math.Min(location.Y, area.Bottom - size.Height - inset));
            return new Point(x, y);
        }
    }
}
