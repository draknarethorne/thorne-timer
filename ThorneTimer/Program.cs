using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThorneTimer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ThorneLog.Separator("APPLICATION START");
            ThorneLog.Info($"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            ThorneLog.Info($"Runtime: {Environment.Version}");
            ThorneLog.Info($"OS: {Environment.OSVersion}");
            ThorneLog.Info($"User: {Environment.UserName}");
            ThorneLog.Info($"Working Dir: {Environment.CurrentDirectory}");

            // Load backup retention policy from [Backups] in ThorneTimer.ini
            RetentionPolicy backupPolicy = RetentionPolicy.BackupDefaults;
            try
            {
                string iniPath = ThorneArchive.GetIniPath();
                var backupIni = ThorneArchive.ParseIniSection(iniPath, "Backups");
                if (backupIni.Count > 0)
                {
                    // Check Enabled flag — if false, skip backup entirely
                    if (backupIni.TryGetValue("Enabled", out string enabledVal)
                        && enabledVal.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        ThorneLog.Info("Database backup disabled via ThorneTimer.ini");
                    }
                    else
                    {
                        backupPolicy = ThorneArchive.ReadRetentionPolicy(
                            backupIni, RetentionPolicy.BackupDefaults);
                    }
                }
            }
            catch { }

            // Create a backup of the database before doing anything else
            string dbPath = Properties.Settings.Default.DatabasePath;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
                dbPath = Database.GetDefaultDatabasePath();

            if (File.Exists(dbPath))
            {
                string backupPath = Database.BackupDatabase(dbPath, backupPolicy);
                if (backupPath != null)
                    ThorneLog.Info($"Database backup created: {backupPath}");
                else
                    ThorneLog.Warn("Database backup failed or skipped");
            }
            else
            {
                ThorneLog.Info("No existing database to backup (new install)");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ThorneLog.Info("Launching main form...");
            Application.Run(new FormMain());

            ThorneLog.Separator("APPLICATION EXIT");
            ThorneLog.Info("Application shutdown complete");
        }
    }
}
