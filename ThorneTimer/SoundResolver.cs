using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ThorneTimer
{
    /// <summary>
    /// Resolves WAV file references to full paths with subdirectory search fallback.
    /// Handles backward compatibility when sounds move from root Sounds/ into subcategories.
    ///
    /// Resolution order:
    ///   1. Exact relative path under Sounds/ (e.g., "Alerts/ding.wav" → Sounds/Alerts/ding.wav)
    ///   2. Filename in Sounds/ root (e.g., "ding.wav" → Sounds/ding.wav)
    ///   3. Recursive subdirectory search (e.g., "ding.wav" found at Sounds/Alerts/ding.wav)
    /// </summary>
    static class SoundResolver
    {
        private static string _soundsRoot;
        private static Dictionary<string, string> _cache;

        /// <summary>
        /// Gets the root Sounds directory path (Application.StartupPath\Sounds).
        /// </summary>
        public static string SoundsRoot
        {
            get
            {
                if (_soundsRoot == null)
                    _soundsRoot = Path.Combine(Application.StartupPath, "Sounds");
                return _soundsRoot;
            }
        }

        /// <summary>
        /// Clear the cached file lookups (call after sounds directory changes).
        /// </summary>
        public static void ClearCache()
        {
            _cache = null;
        }

        /// <summary>
        /// Resolve a WAVFile reference to its full disk path.
        /// Returns null if the file cannot be found.
        /// </summary>
        public static string Resolve(string wavFile)
        {
            if (string.IsNullOrEmpty(wavFile))
                return null;

            string root = SoundsRoot;
            if (!Directory.Exists(root))
            {
                ThorneLog.Warn($"SoundResolver: Sounds directory not found: {root}");
                return null;
            }

            // Tier 1: Exact relative path (handles "Alerts/ding.wav" or "ding.wav" in root)
            string exactPath = Path.Combine(root, wavFile);
            if (File.Exists(exactPath))
            {
                ThorneLog.Debug($"SoundResolver: Tier 1 (exact) → {exactPath}");
                return exactPath;
            }

            // Tier 2: Filename-only lookup in root (strip any subdirectory from reference)
            string filename = Path.GetFileName(wavFile);
            if (filename != wavFile)
            {
                string rootPath = Path.Combine(root, filename);
                if (File.Exists(rootPath))
                {
                    ThorneLog.Debug($"SoundResolver: Tier 2 (root fallback) → {rootPath}");
                    return rootPath;
                }
            }

            // Tier 3: Recursive subdirectory search (case-insensitive)
            string found = SearchSubdirectories(root, filename);
            if (found != null)
            {
                ThorneLog.Debug($"SoundResolver: Tier 3 (subdirectory search) → {found}");
                return found;
            }

            ThorneLog.Warn($"SoundResolver: File not found in any tier: \"{wavFile}\"");
            return null;
        }

        /// <summary>
        /// Search subdirectories of the Sounds root for a filename.
        /// Uses a lazy-built cache for fast repeated lookups.
        /// </summary>
        private static string SearchSubdirectories(string root, string filename)
        {
            // Build/refresh cache on first miss
            if (_cache == null)
            {
                _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    foreach (string file in Directory.GetFiles(root, "*.wav", SearchOption.AllDirectories))
                    {
                        string name = Path.GetFileName(file);
                        // First match wins (root files are already handled above)
                        if (!_cache.ContainsKey(name))
                            _cache[name] = file;
                    }
                }
                catch (Exception ex)
                {
                    ThorneLog.Warn($"SoundResolver: Error scanning directories: {ex.Message}");
                }
            }

            string result;
            _cache.TryGetValue(filename, out result);
            return result;
        }

        /// <summary>
        /// Get the relative path from the Sounds root for storage in the database.
        /// If the file is under Sounds/, returns the relative portion (e.g., "Alerts/ding.wav").
        /// Otherwise returns just the filename.
        /// </summary>
        public static string GetRelativePath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return "";

            string root = SoundsRoot + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return fullPath.Substring(root.Length);

            return Path.GetFileName(fullPath);
        }

        /// <summary>
        /// Get all available sound files organized by subdirectory.
        /// Returns a flat list of relative paths from the Sounds root.
        /// </summary>
        public static List<string> GetAllSounds()
        {
            var sounds = new List<string>();
            string root = SoundsRoot;

            if (!Directory.Exists(root))
                return sounds;

            try
            {
                foreach (string file in Directory.GetFiles(root, "*.wav", SearchOption.AllDirectories))
                {
                    sounds.Add(file.Substring(root.Length + 1));
                }
            }
            catch { }

            sounds.Sort(StringComparer.OrdinalIgnoreCase);
            return sounds;
        }
    }
}
