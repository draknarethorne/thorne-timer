using System;
using System.Drawing;

namespace ThorneTimer
{
    public class StyleData
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public int ForeColor { get; set; }
        public int BackColor { get; set; }
        public int SortOrder { get; set; }

        /// <summary>
        /// Per-style time display format (see <see cref="ThorneTimer.TimeFormat"/>).
        /// Stored as the underlying <c>int</c> on the <c>styles.TimeFormat</c> column.
        /// Defaults to <see cref="ThorneTimer.TimeFormat.Classic"/> so existing
        /// databases render unchanged.
        /// </summary>
        public TimeFormat TimeFormat { get; set; } = TimeFormat.Classic;

        public Color ForeColorValue
        {
            get { return Color.FromArgb(ForeColor); }
        }

        public Color BackColorValue
        {
            get { return Color.FromArgb(BackColor); }
        }
    }
}
