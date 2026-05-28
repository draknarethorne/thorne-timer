using System;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;

namespace ThorneTimer
{
    /// <summary>
    /// Manages the Mini View settings panel on the Settings tab:
    /// opacity, font size, warning colors, and warning time.
    /// Owns DB load/save and pushes changes into <see cref="MiniViews"/>.
    /// </summary>
    /// <remarks>
    /// This is a transitional home for these settings.  When the larger
    /// Views/Styles overhaul lands, warning colors and timing will move
    /// into per-view configuration and this manager will likely shrink
    /// (or fold into ViewsController).
    /// </remarks>
    internal class MiniViewSettingsManager
    {
        private readonly SQLiteConnection _con;
        private readonly MiniViews _miniViews;
        private readonly TrackBar _tbOpacity;
        private readonly TrackBar _tbFontSize;
        private readonly Label _lblWarnPickFore;
        private readonly Label _lblWarnPickBack;
        private readonly TextBox _txtWarningTime;
        private readonly ColorDialog _colorPicker;

        public MiniViewSettingsManager(
            SQLiteConnection con,
            MiniViews miniViews,
            TrackBar tbOpacity,
            TrackBar tbFontSize,
            Label lblWarnPickFore,
            Label lblWarnPickBack,
            TextBox txtWarningTime,
            ColorDialog colorPicker)
        {
            _con = con;
            _miniViews = miniViews;
            _tbOpacity = tbOpacity;
            _tbFontSize = tbFontSize;
            _lblWarnPickFore = lblWarnPickFore;
            _lblWarnPickBack = lblWarnPickBack;
            _txtWarningTime = txtWarningTime;
            _colorPicker = colorPicker;
        }

        /// <summary>Reads persisted values and syncs UI + <see cref="MiniViews"/>.</summary>
        public void LoadFromDatabase()
        {
            _tbOpacity.Value = Clamp(
                SafeParseInt(Database.GetSetting(_con, "MiniViewOpacity"), 100),
                _tbOpacity.Minimum, _tbOpacity.Maximum);
            _miniViews.mvOpacity = _tbOpacity.Value;

            _tbFontSize.Value = Clamp(
                SafeParseInt(Database.GetSetting(_con, "MiniViewFontSize"), 8),
                _tbFontSize.Minimum, _tbFontSize.Maximum);
            _miniViews.mvFontSize = _tbFontSize.Value;

            _miniViews.mvWarnForeColor =
                SafeParseInt(Database.GetSetting(_con, "MiniViewWarnFore"), Color.White.ToArgb());
            _lblWarnPickFore.BackColor = Color.FromArgb(_miniViews.mvWarnForeColor);

            _miniViews.mvWarnBackColor =
                SafeParseInt(Database.GetSetting(_con, "MiniViewWarnBack"), Color.Red.ToArgb());
            _lblWarnPickBack.BackColor = Color.FromArgb(_miniViews.mvWarnBackColor);

            _miniViews.mvWarnTime = Database.GetSetting(_con, "MiniViewWarnTime");
            _txtWarningTime.Text = _miniViews.mvWarnTime;

            _miniViews.UpdateMiniAppearance();
        }

        public void OnOpacityChanged()
        {
            _miniViews.mvOpacity = _tbOpacity.Value;
            Database.SetSetting(_con, "MiniViewOpacity", _miniViews.mvOpacity);
            _miniViews.UpdateMiniAppearance();
        }

        public void OnFontSizeChanged()
        {
            _miniViews.mvFontSize = _tbFontSize.Value;
            Database.SetSetting(_con, "MiniViewFontSize", _miniViews.mvFontSize);
            _miniViews.UpdateMiniAppearance();
        }

        public void PickWarningForeColor()
        {
            _colorPicker.Color = _lblWarnPickFore.BackColor;
            _colorPicker.ShowDialog();
            _lblWarnPickFore.BackColor = _colorPicker.Color;

            _miniViews.mvWarnForeColor = _lblWarnPickFore.BackColor.ToArgb();
            Database.SetSetting(_con, "MiniViewWarnFore", _miniViews.mvWarnForeColor);
            _miniViews.UpdateMiniAppearance();
        }

        public void PickWarningBackColor()
        {
            _colorPicker.Color = _lblWarnPickBack.BackColor;
            _colorPicker.ShowDialog();
            _lblWarnPickBack.BackColor = _colorPicker.Color;

            _miniViews.mvWarnBackColor = _lblWarnPickBack.BackColor.ToArgb();
            Database.SetSetting(_con, "MiniViewWarnBack", _miniViews.mvWarnBackColor);
            _miniViews.UpdateMiniAppearance();
        }

        /// <summary>
        /// Validates the warning time text and persists it.  Returns false
        /// if the text is not in MM:SS format; caller may show an error.
        /// </summary>
        public bool TryCommitWarningTime()
        {
            string text = _txtWarningTime.Text;
            if (!ValidTime(text))
                return false;

            _miniViews.mvWarnTime = text;
            Database.SetSetting(_con, "MiniViewWarnTime", _miniViews.mvWarnTime);
            _miniViews.UpdateMiniAppearance();
            return true;
        }

        private static bool ValidTime(string theTime)
        {
            if (string.IsNullOrEmpty(theTime) || theTime.Length != 5) return false;
            if (theTime.Substring(2, 1) != ":") return false;
            return int.TryParse(theTime.Substring(0, 2), out _)
                && int.TryParse(theTime.Substring(3, 2), out _);
        }

        private static int SafeParseInt(string value, int fallback)
        {
            return int.TryParse(value, out int n) ? n : fallback;
        }

        private static int Clamp(int v, int min, int max)
        {
            return Math.Max(min, Math.Min(max, v));
        }
    }
}
