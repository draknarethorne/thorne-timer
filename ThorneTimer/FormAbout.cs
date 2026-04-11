using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ThorneTimer
{
    public partial class FormAbout : Form
    {
        private const string GitHubUrl = "https://github.com/draknarethorne/thorne-timer";

        public FormAbout()
        {
            InitializeComponent();
        }

        private void FormAbout_Load(object sender, EventArgs e)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Version appVersion = asm.GetName().Version;

            // Version — show major.minor.patch (drop revision unless non-zero)
            string versionText = appVersion.Revision > 0
                ? appVersion.ToString()
                : $"{appVersion.Major}.{appVersion.Minor}.{appVersion.Build}";
            labelVersion.Text = "Version " + versionText;

            // Copyright from assembly attribute
            var copyrightAttr = (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(asm, typeof(AssemblyCopyrightAttribute));
            if (copyrightAttr != null)
                labelCopyright.Text = copyrightAttr.Copyright;

            // Feature highlights — concise summary pulled from README themes
            labelFeatures.Text =
                "\u2022  Always-on-top timer overlays (Normal, Buff, Pet, Ping)\n" +
                "\u2022  Real-time log parsing with start/end keyword matching\n" +
                "\u2022  Text-to-speech and WAV sound alerts per timer\n" +
                "\u2022  Multi-character tracking with automatic character switching\n" +
                "\u2022  Portable Tome system \u2014 all data in a single .tdb file\n" +
                "\u2022  Class-based timer filtering and compact view mode\n" +
                "\u2022  Category system with zone-aware auto-activation";

            // Runtime info
            labelFramework.Text = "Runtime:  .NET Framework " + Environment.Version.ToString();

            // SQLite version
            string sqliteVersion = GetSQLiteVersion();
            labelDatabase.Text = "Database:  SQLite" + (sqliteVersion != null ? " " + sqliteVersion : "");
        }

        private string GetSQLiteVersion()
        {
            try
            {
                using (var con = new System.Data.SQLite.SQLiteConnection("Data Source=:memory:"))
                {
                    con.Open();
                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = "SELECT sqlite_version()";
                        return cmd.ExecuteScalar()?.ToString();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private void linkGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = GitHubUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                // If the browser can't be opened, silently ignore
            }
        }

        private void ButtonOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
