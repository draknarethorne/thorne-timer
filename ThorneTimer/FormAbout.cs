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

            // Prefer the informational version (e.g. "0.6.0-beta2"), which can
            // carry a pre-release suffix that AssemblyVersion cannot.  Fall back
            // to the numeric assembly version (drop revision unless non-zero).
            string versionText = GetInformationalVersion(asm);
            if (string.IsNullOrEmpty(versionText))
            {
                versionText = appVersion.Revision > 0
                    ? appVersion.ToString()
                    : $"{appVersion.Major}.{appVersion.Minor}.{appVersion.Build}";
            }
            labelVersion.Text = "Version " + versionText;

            // Copyright from assembly attribute
            var copyrightAttr = (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(asm, typeof(AssemblyCopyrightAttribute));
            if (copyrightAttr != null)
                labelCopyright.Text = copyrightAttr.Copyright;

            // Feature highlights — concise summary aligned with the README
            // Version History.  Keep in sync when adding major features.
            labelFeatures.Text =
                "\u2022  Always-on-top timer overlays driven by user-editable Styles & Views\n" +
                "\u2022  Real-time log parsing with start/end keyword matching and dependencies\n" +
                "\u2022  Text-to-speech and WAV sound alerts per timer (200+ built-in sounds)\n" +
                "\u2022  Multi-character tracking with automatic character switching and camp-out detection\n" +
                "\u2022  World / Character / Character+ scopes and class-based timer filtering\n" +
                "\u2022  Portable Tome system \u2014 all data in a single .tdb file with auto-migration\n" +
                "\u2022  Tome Information dialog with sortable usage and breakdown lists";

            // Runtime info.  Environment.Version on .NET Framework reports the
            // CLR build (4.0.30319.x), which is misleading — the framework
            // moniker is what users actually recognize.  Detect the installed
            // .NET Framework version from the Release key in the registry.
            labelFramework.Text = "Runtime:  .NET Framework " + GetNetFrameworkVersion();

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

        /// <summary>
        /// Returns the AssemblyInformationalVersion (e.g. "0.6.0-beta2"),
        /// which can carry a pre-release suffix.  Returns null if the attribute
        /// is missing or empty so callers can fall back to the numeric version.
        /// </summary>
        private static string GetInformationalVersion(Assembly asm)
        {
            var attr = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(
                asm, typeof(AssemblyInformationalVersionAttribute));
            string value = attr?.InformationalVersion;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// Resolves the installed .NET Framework 4.x version from the
        /// HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full Release key
        /// as documented at
        /// https://learn.microsoft.com/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
        /// Falls back to the CLR version if the registry lookup fails.
        /// </summary>
        private static string GetNetFrameworkVersion()
        {
            try
            {
                using (var key = Microsoft.Win32.RegistryKey
                    .OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry32)
                    .OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                {
                    if (key != null)
                    {
                        var releaseObj = key.GetValue("Release");
                        if (releaseObj is int release)
                        {
                            // Mapping from Release DWORD → marketing version.
                            if (release >= 533320) return "4.8.1 or later";
                            if (release >= 528040) return "4.8";
                            if (release >= 461808) return "4.7.2";
                            if (release >= 461308) return "4.7.1";
                            if (release >= 460798) return "4.7";
                            if (release >= 394802) return "4.6.2";
                            if (release >= 394254) return "4.6.1";
                            if (release >= 393295) return "4.6";
                            if (release >= 379893) return "4.5.2";
                            if (release >= 378675) return "4.5.1";
                            if (release >= 378389) return "4.5";
                        }
                    }
                }
            }
            catch
            {
                // Fall through to CLR version.
            }
            return Environment.Version.ToString();
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
