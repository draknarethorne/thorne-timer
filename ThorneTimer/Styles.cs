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
