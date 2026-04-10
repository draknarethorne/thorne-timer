using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ThorneTimer
{
    public partial class FormTomeInfo : Form
    {
        private readonly SQLiteConnection _con;
        private readonly string _dbPath;

        public FormTomeInfo(SQLiteConnection con, string dbPath)
        {
            InitializeComponent();
            _con = con;
            _dbPath = dbPath;
        }

        private void FormTomeInfo_Load(object sender, EventArgs e)
        {
            // Tome file info
            labelTomeName.Text = Path.GetFileName(_dbPath);
            labelTomePath.Text = _dbPath;
            toolTipPath.SetToolTip(labelTomePath, _dbPath);

            try
            {
                var fileInfo = new FileInfo(_dbPath);
                labelFileSize.Text = FormatFileSize(fileInfo.Length);
                labelCreated.Text = fileInfo.CreationTime.ToString("yyyy-MM-dd  HH:mm");
                labelModified.Text = fileInfo.LastWriteTime.ToString("yyyy-MM-dd  HH:mm");
            }
            catch
            {
                labelFileSize.Text = "—";
                labelCreated.Text = "—";
                labelModified.Text = "—";
            }

            // Database statistics
            var stats = Database.GetTomeStatistics(_con);

            labelTimerCount.Text = stats.TimerCount.ToString();
            labelActiveTimerCount.Text = stats.ActiveTimerCount.ToString();
            labelCharacterCount.Text = stats.CharacterCount.ToString();
            labelCategoryCount.Text = stats.CategoryCount.ToString();
            labelViewCount.Text = stats.ViewCount.ToString();
            labelClassCount.Text = stats.ClassCount.ToString();

            // Timers by style breakdown
            string styleBreakdown = string.Join("    ",
                stats.TimersByStyle.OrderBy(kvp => kvp.Key)
                    .Select(kvp => kvp.Key + ": " + kvp.Value));
            labelStyleBreakdown.Text = styleBreakdown.Length > 0 ? styleBreakdown : "—";

            // Timers by scope breakdown
            string scopeBreakdown = string.Join("    ",
                stats.TimersByScope.OrderBy(kvp => kvp.Key)
                    .Select(kvp => kvp.Key + ": " + kvp.Value));
            labelScopeBreakdown.Text = scopeBreakdown.Length > 0 ? scopeBreakdown : "—";
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return bytes + " bytes";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            return (bytes / (1024.0 * 1024.0)).ToString("F1") + " MB";
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
