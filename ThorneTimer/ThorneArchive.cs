using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ThorneTimer
{
    /// <summary>
    /// Tiered file-retention policy used by both logging and database
    /// backup cleanup.  Each tier keeps progressively fewer files as
    /// age increases, giving recent granularity while still keeping
    /// older snapshots for recovery.
    ///
    /// Tier 1 — Recent   (0 .. RecentDays):      keep MaxFilesPerDay per day
    /// Tier 2 — Daily    (RecentDays+1 .. RetentionDays): keep 1 per day
    /// Tier 3 — Monthly  (RetentionDays+1 .. MaxAgeDays): keep 1 per month
    /// Tier 4 — Expired  (older than MaxAgeDays):         delete all
    ///
    /// After tiered pruning, a hard MaxTotalFiles cap trims the oldest
    /// survivors if needed.
    /// </summary>
    class RetentionPolicy
    {
        /// <summary>Days considered "recent" — full per-day quota kept.</summary>
        public int RecentDays { get; set; }

        /// <summary>Max files to keep per calendar day in the recent tier.
        /// Set to 0 for unlimited.</summary>
        public int MaxFilesPerDay { get; set; }

        /// <summary>Days to keep at least 1-per-day beyond the recent tier.
        /// Files older than this but within MaxAgeDays thin to 1-per-month.</summary>
        public int RetentionDays { get; set; }

        /// <summary>Absolute maximum age in days.  Files older than this are
        /// always deleted.  Set to 0 to disable age-based deletion entirely.</summary>
        public int MaxAgeDays { get; set; }

        /// <summary>Hard cap on total files across all tiers.  After tiered
        /// cleanup, the oldest files are removed until under this limit.
        /// Set to 0 for unlimited.</summary>
        public int MaxTotalFiles { get; set; }

        /// <summary>Logging defaults — shorter retention, fewer files.</summary>
        public static RetentionPolicy LogDefaults => new RetentionPolicy
        {
            RecentDays    = 7,
            MaxFilesPerDay = 3,
            RetentionDays = 30,
            MaxAgeDays    = 90,
            MaxTotalFiles = 50
        };

        /// <summary>Backup defaults — longer retention, more generous limits.</summary>
        public static RetentionPolicy BackupDefaults => new RetentionPolicy
        {
            RecentDays    = 7,
            MaxFilesPerDay = 5,
            RetentionDays = 30,
            MaxAgeDays    = 365,
            MaxTotalFiles = 100
        };
    }

    /// <summary>
    /// Shared utilities for tiered file retention and INI parsing.
    /// Used by both <see cref="ThorneLog"/> and <see cref="Database"/>
    /// backup management to keep cleanup logic consistent.
    /// </summary>
    static class ThorneArchive
    {
        // ── INI helpers ─────────────────────────────────────────────

        /// <summary>
        /// Returns the path to ThorneTimer.ini next to the executable.
        /// </summary>
        public static string GetIniPath()
        {
            string exeDir = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            return Path.Combine(exeDir, "ThorneTimer.ini");
        }

        /// <summary>
        /// Minimal INI parser — returns key/value pairs for the given
        /// [section].  Keys are case-insensitive.  Lines starting with
        /// ; or # are comments.  Returns an empty dictionary if the
        /// file or section is not found.
        /// </summary>
        public static Dictionary<string, string> ParseIniSection(string path, string section)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path)) return result;

            bool inSection = false;
            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#')
                    continue;

                if (line[0] == '[')
                {
                    int close = line.IndexOf(']');
                    if (close > 1)
                    {
                        string name = line.Substring(1, close - 1).Trim();
                        inSection = name.Equals(section, StringComparison.OrdinalIgnoreCase);
                    }
                    continue;
                }

                if (!inSection) continue;

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                result[key] = val;
            }
            return result;
        }

        /// <summary>
        /// Reads a <see cref="RetentionPolicy"/> from an INI section,
        /// falling back to the supplied defaults for missing keys.
        /// </summary>
        public static RetentionPolicy ReadRetentionPolicy(
            Dictionary<string, string> ini, RetentionPolicy defaults)
        {
            var p = new RetentionPolicy
            {
                RecentDays    = defaults.RecentDays,
                MaxFilesPerDay = defaults.MaxFilesPerDay,
                RetentionDays = defaults.RetentionDays,
                MaxAgeDays    = defaults.MaxAgeDays,
                MaxTotalFiles = defaults.MaxTotalFiles
            };

            if (ini.TryGetValue("RecentDays", out string rd)
                && int.TryParse(rd, out int rdv) && rdv >= 0)
                p.RecentDays = rdv;

            if (ini.TryGetValue("MaxFilesPerDay", out string mfpd)
                && int.TryParse(mfpd, out int mfpdv) && mfpdv >= 0)
                p.MaxFilesPerDay = mfpdv;

            if (ini.TryGetValue("RetentionDays", out string ret)
                && int.TryParse(ret, out int retv) && retv >= 0)
                p.RetentionDays = retv;

            if (ini.TryGetValue("MaxAgeDays", out string mad)
                && int.TryParse(mad, out int madv) && madv >= 0)
                p.MaxAgeDays = madv;

            if (ini.TryGetValue("MaxTotalFiles", out string mtf)
                && int.TryParse(mtf, out int mtfv) && mtfv >= 0)
                p.MaxTotalFiles = mtfv;

            return p;
        }

        // ── Tiered pruning ──────────────────────────────────────────

        /// <summary>
        /// Applies tiered retention to files matching <paramref name="searchPattern"/>
        /// in <paramref name="directory"/>.
        ///
        /// Tier 1 (0 .. RecentDays):              keep MaxFilesPerDay per day
        /// Tier 2 (RecentDays+1 .. RetentionDays): keep 1 per day  (newest)
        /// Tier 3 (RetentionDays+1 .. MaxAgeDays): keep 1 per month (newest)
        /// Tier 4 (older than MaxAgeDays):         delete all
        ///
        /// Finally, if total surviving files exceed MaxTotalFiles the
        /// oldest are removed.
        /// </summary>
        public static void PruneFiles(string directory, string searchPattern, RetentionPolicy policy)
        {
            try
            {
                if (!Directory.Exists(directory)) return;

                var files = Directory.GetFiles(directory, searchPattern)
                    .Select(f => new FileInfo(f))
                    .OrderBy(f => f.CreationTime)
                    .ToList();

                if (files.Count == 0) return;

                DateTime now = DateTime.Now;
                DateTime recentCutoff    = now.AddDays(-policy.RecentDays);
                DateTime dailyCutoff     = now.AddDays(-policy.RetentionDays);
                DateTime maxAgeCutoff    = policy.MaxAgeDays > 0
                    ? now.AddDays(-policy.MaxAgeDays)
                    : DateTime.MinValue;

                // ── Tier 4: delete everything beyond MaxAgeDays ─────
                if (policy.MaxAgeDays > 0)
                {
                    for (int i = files.Count - 1; i >= 0; i--)
                    {
                        if (files[i].CreationTime < maxAgeCutoff)
                        {
                            TryDelete(files[i]);
                            files.RemoveAt(i);
                        }
                    }
                }

                // ── Tier 3: monthly (RetentionDays+1 .. MaxAgeDays) ─
                // Keep only the newest file per calendar month.
                var monthlyBand = files
                    .Where(f => f.CreationTime < dailyCutoff)
                    .GroupBy(f => new { f.CreationTime.Year, f.CreationTime.Month })
                    .ToList();
                foreach (var grp in monthlyBand)
                {
                    foreach (var fi in grp.OrderByDescending(f => f.CreationTime).Skip(1))
                    {
                        TryDelete(fi);
                        files.Remove(fi);
                    }
                }

                // ── Tier 2: daily (RecentDays+1 .. RetentionDays) ───
                // Keep only the newest file per calendar day.
                var dailyBand = files
                    .Where(f => f.CreationTime < recentCutoff && f.CreationTime >= dailyCutoff)
                    .GroupBy(f => f.CreationTime.Date)
                    .ToList();
                foreach (var grp in dailyBand)
                {
                    foreach (var fi in grp.OrderByDescending(f => f.CreationTime).Skip(1))
                    {
                        TryDelete(fi);
                        files.Remove(fi);
                    }
                }

                // ── Tier 1: recent (0 .. RecentDays) ────────────────
                // Keep up to MaxFilesPerDay per calendar day.
                if (policy.MaxFilesPerDay > 0)
                {
                    var recentBand = files
                        .Where(f => f.CreationTime >= recentCutoff)
                        .GroupBy(f => f.CreationTime.Date)
                        .ToList();
                    foreach (var grp in recentBand)
                    {
                        foreach (var fi in grp.OrderByDescending(f => f.CreationTime)
                                              .Skip(policy.MaxFilesPerDay))
                        {
                            TryDelete(fi);
                            files.Remove(fi);
                        }
                    }
                }

                // ── Hard cap ────────────────────────────────────────
                if (policy.MaxTotalFiles > 0 && files.Count > policy.MaxTotalFiles)
                {
                    int toRemove = files.Count - policy.MaxTotalFiles;
                    for (int i = 0; i < toRemove; i++)
                        TryDelete(files[i]);
                }
            }
            catch { }
        }

        private static void TryDelete(FileInfo fi)
        {
            try { fi.Delete(); } catch { }
        }
    }
}
