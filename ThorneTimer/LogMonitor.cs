using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ThorneTimer
{
    /// <summary>
    /// Event args for log chunks read from the file.
    /// </summary>
    public class LogChunkReceivedEventArgs : EventArgs
    {
        public string Text { get; set; }
    }

    /// <summary>
    /// Event args for auto-detected character switch.
    /// </summary>
    public class CharacterSwitchedEventArgs : EventArgs
    {
        public long OldCharacterID { get; set; }
        public long NewCharacterID { get; set; }
        public string NewCharacterName { get; set; }
    }

    /// <summary>
    /// Per-file tracking state used by LogMonitor for multi-file polling.
    /// </summary>
    public class CharacterFileState
    {
        public long CharacterID { get; set; }
        public string CharacterName { get; set; }
        public string FilePath { get; set; }
        public long LastFileSize { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastActivityUtc { get; set; }
        public bool CampingOut { get; set; }
        public DateTime CampStartUtc { get; set; }

        // Trailing fragment from the previous read, prepended to the next chunk
        // for camp-pattern detection only, so a camp message that straddles a
        // 1024-byte read boundary is still matched. Not forwarded to the timer
        // engine (it is only used for Contains checks).
        public string CampScanTail { get; set; } = "";
    }

    /// <summary>
    /// Multi-file log monitor with CancellationToken-based polling.
    /// Watches all registered character log files, reads new content from
    /// the active character, and detects character switches when a non-active
    /// file starts growing.
    /// </summary>
    public class LogMonitor
    {
        private CancellationTokenSource cts;
        private Thread monitorThread;
        private List<CharacterFileState> fileStates;
        private readonly object stateLock = new object();
        private long selectedCharacterID; // UI selection - controls which file to read

        /// <summary>
        /// Minimum bytes a non-active file must grow before triggering a switch.
        /// Prevents false positives from OS flushes or antivirus scans. Tunable
        /// via the [Monitoring] section of ThorneTimer.ini; raise it if benign
        /// background writes cause spurious character switches.
        /// </summary>
        public int SwitchThresholdBytes { get; set; } = 10;

        /// <summary>
        /// When false, file growth on non-active characters is tracked but
        /// CharacterSwitched events are not fired. Content is still read
        /// from the active character's file.
        /// </summary>
        public bool AutoSwitchEnabled { get; set; } = true;

        /// <summary>
        /// When set to a positive character ID, auto-switch ignores growth
        /// from that specific character only.  Other characters (e.g. a
        /// brand-new login) still trigger a switch normally.  Set to 0 to
        /// clear the suppression.
        /// </summary>
        public long SuppressedAutoSwitchCharacterID { get; set; }

        /// <summary>
        /// Seconds of log inactivity after camp warning before triggering camp-out.
        /// Default is 10 seconds.
        /// </summary>
        public int CampInactivityThresholdSeconds { get; set; } = 10;

        /// <summary>
        /// Fallback for ungraceful exits (client crash, Alt-F4, link-dead /
        /// disconnect) where NO camp warning is ever written and the log simply
        /// goes silent. If the actively logging character produces no new log
        /// bytes for this many seconds, it is treated as gone and the same
        /// <see cref="CharacterCampedOut"/> auto-pause fires.
        ///
        /// This is intentionally much longer than the camp threshold because real
        /// play almost always emits *some* log noise; the window only elapses when
        /// the client is truly gone. Set to 0 to disable the fallback entirely.
        /// Default is 300 seconds (5 minutes).
        /// </summary>
        public int InactivityTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Loads the auto-pause thresholds from the [Monitoring] section of
        /// ThorneTimer.ini (next to the executable). INI values take priority
        /// over the built-in defaults; missing keys leave the current value
        /// untouched, so callers may layer this over database-restored values.
        ///
        /// Keys (seconds; 0 disables that timer):
        ///   CampInactivityThresholdSeconds  - quiet period after a camp warning
        ///   InactivityTimeoutSeconds        - silent-log fallback for crashes
        /// </summary>
        public void LoadIniSettings()
        {
            try
            {
                string iniPath = ThorneArchive.GetIniPath();
                var settings = ThorneArchive.ParseIniSection(iniPath, "Monitoring");
                if (settings.Count == 0) return;

                if (settings.TryGetValue("CampInactivityThresholdSeconds", out string campVal)
                    && int.TryParse(campVal, out int campSecs)
                    && campSecs >= 0)
                {
                    CampInactivityThresholdSeconds = campSecs;
                }

                if (settings.TryGetValue("InactivityTimeoutSeconds", out string inactiveVal)
                    && int.TryParse(inactiveVal, out int inactiveSecs)
                    && inactiveSecs >= 0)
                {
                    InactivityTimeoutSeconds = inactiveSecs;
                }

                // Must be at least 1; a 0 threshold would make every OS flush
                // look like a character switch.
                if (settings.TryGetValue("SwitchThresholdBytes", out string switchVal)
                    && int.TryParse(switchVal, out int switchBytes)
                    && switchBytes >= 1)
                {
                    SwitchThresholdBytes = switchBytes;
                }

                ThorneLog.Info(
                    $"Monitoring config loaded from ini: " +
                    $"CampInactivityThresholdSeconds={CampInactivityThresholdSeconds}, " +
                    $"InactivityTimeoutSeconds={InactivityTimeoutSeconds}, " +
                    $"SwitchThresholdBytes={SwitchThresholdBytes}");
            }
            catch (Exception ex)
            {
                ThorneLog.Warn($"Failed to load [Monitoring] from ini: {ex.Message}");
            }
        }

        /// <summary>
        /// Fired when new text is read from the active character's log file.
        /// </summary>
        public event EventHandler<LogChunkReceivedEventArgs> LogChunkReceived;

        /// <summary>
        /// Fired when a non-active character's log file starts growing,
        /// indicating the user has switched characters in-game.
        /// </summary>
        public event EventHandler<CharacterSwitchedEventArgs> CharacterSwitched;

        /// <summary>
        /// Fired when the active character camps out (after camp warning + inactivity threshold).
        /// </summary>
        public event EventHandler<CharacterSwitchedEventArgs> CharacterCampedOut;

        /// <summary>
        /// The file path of the currently active character being monitored.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Whether the monitor is currently running.
        /// </summary>
        public bool IsRunning => monitorThread != null && monitorThread.IsAlive;

        /// <summary>
        /// Get the character ID of the actively logging character (based on file growth).
        /// Returns 0 if not running or no character is actively logging.
        /// This is the authoritative source for "who is logging" state.
        /// </summary>
        public long GetActiveCharacterID()
        {
            lock (stateLock)
            {
                if (fileStates == null) return 0;
                var active = fileStates.FirstOrDefault(f => f.IsActive);
                return active?.CharacterID ?? 0;
            }
        }

        /// <summary>
        /// Get the character ID selected for viewing in the UI.
        /// This may differ from GetActiveCharacterID() during browsing mode.
        /// Returns 0 if no character is selected.
        /// </summary>
        public long GetSelectedCharacterID()
        {
            lock (stateLock)
            {
                return selectedCharacterID;
            }
        }

        // Camp-out detection patterns
        private const string CampWarningPattern = "It will take about 5 more seconds to prepare your camp.";
        private const string CampAbandonPattern = "You abandon your preparations to camp.";
        private const string DisconnectPattern = "You have been disconnected.";
        private const string LinkDeadPattern = "LOADING, PLEASE WAIT...";

        /// <summary>
        /// Scans a freshly read chunk for camp warning / abandon patterns and
        /// updates the camp state on <paramref name="state"/>. Handles the case
        /// where a camp message straddles a 1024-byte read boundary by prepending
        /// the trailing fragment kept from the previous read
        /// (<see cref="CharacterFileState.CampScanTail"/>) before matching, then
        /// retaining a new tail for the next read. This runs for the actively
        /// logging character regardless of which character is selected in the UI,
        /// so camp-out is detected even while browsing another character.
        /// </summary>
        private void ScanForCampPatterns(CharacterFileState state, string chunk)
        {
            if (state == null || string.IsNullOrEmpty(chunk)) return;

            // Prepend the residual tail so a pattern split across two reads is
            // still found. The combined string is used for matching only.
            string scan = state.CampScanTail.Length > 0
                ? state.CampScanTail + chunk
                : chunk;

            if (scan.IndexOf(CampWarningPattern, StringComparison.Ordinal) >= 0)
            {
                if (!state.CampingOut)
                    ThorneLog.Info($"Camp warning detected for charID={state.CharacterID} (active={state.IsActive}); threshold={CampInactivityThresholdSeconds}s");
                state.CampingOut = true;
                state.CampStartUtc = DateTime.UtcNow;
            }
            else if (scan.IndexOf(CampAbandonPattern, StringComparison.Ordinal) >= 0)
            {
                if (state.CampingOut)
                    ThorneLog.Info($"Camp abandoned for charID={state.CharacterID}; camp-out cancelled");
                state.CampingOut = false;
                state.CampStartUtc = DateTime.MinValue;
            }

            // Keep enough trailing text to bridge a boundary split on the next
            // read. The longest pattern is the camp warning; keep one char less
            // than its length so a pattern that begins at the very end of this
            // chunk can complete at the start of the next one.
            int keep = CampWarningPattern.Length - 1;
            state.CampScanTail = scan.Length <= keep
                ? scan
                : scan.Substring(scan.Length - keep);
        }

        /// <summary>
        /// Start monitoring multiple character log files.
        /// Content is read from the selected character's file and fired via LogChunkReceived.
        /// All files are checked for growth to detect character switches.
        /// IsActive flags are set by file growth detection, NOT by this method.
        /// </summary>
        public void Start(List<CharacterFileState> characters, long selectedCharacterID)
        {
            if (IsRunning) Stop();

            this.selectedCharacterID = selectedCharacterID;
            fileStates = new List<CharacterFileState>();
            foreach (var c in characters)
            {
                if (string.IsNullOrEmpty(c.FilePath)) continue;

                var state = new CharacterFileState
                {
                    CharacterID = c.CharacterID,
                    CharacterName = c.CharacterName,
                    FilePath = c.FilePath,
                    LastFileSize = 0,
                    IsActive = false, // Will be set by file growth detection
                    LastActivityUtc = DateTime.UtcNow,
                    CampingOut = false,
                    CampStartUtc = DateTime.MinValue
                };

                // Seed LastFileSize to current file length (skip existing content)
                try
                {
                    if (File.Exists(c.FilePath))
                    {
                        state.LastFileSize = new FileInfo(c.FilePath).Length;
                    }
                }
                catch { }

                fileStates.Add(state);

                // Set FilePath to selected character for content reading
                if (state.CharacterID == selectedCharacterID)
                {
                    FilePath = state.FilePath;
                }
            }

            cts = new CancellationTokenSource();
            monitorThread = new Thread(() => PollLoop(cts.Token))
            {
                IsBackground = true,
                Name = "LogMonitor"
            };
            monitorThread.Start();
        }

        /// <summary>
        /// Start monitoring a single log file (backward-compatible).
        /// No character switch detection in this mode.
        /// </summary>
        public void Start(string filePath)
        {
            if (IsRunning) Stop();

            FilePath = filePath;
            fileStates = null;

            cts = new CancellationTokenSource();
            monitorThread = new Thread(() => PollLoopSingle(filePath, cts.Token))
            {
                IsBackground = true,
                Name = "LogMonitor"
            };
            monitorThread.Start();
        }

        /// <summary>
        /// Gracefully stop monitoring.
        /// </summary>
        public void Stop()
        {
            if (cts != null)
            {
                cts.Cancel();
                if (monitorThread != null && monitorThread.IsAlive)
                {
                    monitorThread.Join(2000);
                }
                cts.Dispose();
                cts = null;
            }
            monitorThread = null;
            FilePath = null;
        }

        /// <summary>
        /// Update the selected character for UI viewing without restarting the monitor.
        /// Called when the user manually changes the character dropdown.
        /// This does NOT change which character is actively logging (IsActive flag) —
        /// that is determined exclusively by file growth detection.
        /// </summary>
        public void SetActiveCharacter(long characterID)
        {
            lock (stateLock)
            {
                selectedCharacterID = characterID;
                if (fileStates == null) return;

                // Update FilePath for content reading (browsing mode support)
                var selectedState = fileStates.FirstOrDefault(f => f.CharacterID == characterID);
                if (selectedState != null)
                {
                    FilePath = selectedState.FilePath;
                }
                else if (characterID == 0)
                {
                    FilePath = null; // "(None)" selected
                }
            }
        }

        /// <summary>
        /// Multi-file poll loop. Checks all character files for growth.
        /// Reads content from selected file (browsing mode support), detects switches on non-active files.
        /// </summary>
        private void PollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                List<CharacterFileState> snapshot;
                long currentSelectedID;
                lock (stateLock)
                {
                    if (fileStates == null) break;
                    snapshot = fileStates.ToList();
                    currentSelectedID = selectedCharacterID;
                }

                foreach (var state in snapshot)
                {
                    if (token.IsCancellationRequested) break;

                    try
                    {
                        if (!File.Exists(state.FilePath)) continue;

                        long currentSize = new FileInfo(state.FilePath).Length;
                        if (currentSize <= state.LastFileSize) continue;

                        long growth = currentSize - state.LastFileSize;

                        // Check for character switch (non-active file growing)
                        if (!state.IsActive)
                        {
                            // Non-active file is growing — potential character switch
                            if (growth >= SwitchThresholdBytes && AutoSwitchEnabled)
                            {
                                // If this specific character is suppressed (manual switch),
                                // track its size but don't trigger a switch.  Other
                                // characters (e.g. a brand-new login) still switch normally.
                                if (SuppressedAutoSwitchCharacterID > 0
                                    && state.CharacterID == SuppressedAutoSwitchCharacterID)
                                {
                                    state.LastFileSize = currentSize;
                                    continue;
                                }

                                // Find the old active character
                                long oldCharID = 0;
                                lock (stateLock)
                                {
                                    var oldActive = fileStates.FirstOrDefault(f => f.IsActive);
                                    if (oldActive != null)
                                    {
                                        oldCharID = oldActive.CharacterID;
                                        oldActive.IsActive = false;
                                    }
                                    state.IsActive = true;

                                    // Auto-switch also updates selected character and FilePath
                                    selectedCharacterID = state.CharacterID;
                                    FilePath = state.FilePath;
                                }

                                CharacterSwitched?.Invoke(this, new CharacterSwitchedEventArgs
                                {
                                    OldCharacterID = oldCharID,
                                    NewCharacterID = state.CharacterID,
                                    NewCharacterName = state.CharacterName
                                });
                            }
                        }

                        // Read content from selected character's file (enables browsing mode)
                        // This may be different from the actively logging character
                        if (state.CharacterID == currentSelectedID)
                        {
                            ReadNewContent(state, token);
                        }
                        else if (state.IsActive)
                        {
                            // The actively logging character is NOT the one selected for
                            // viewing (browsing mode). We still must detect its camp-out,
                            // so read its new bytes for pattern scanning only - without
                            // forwarding them to the timer engine (the selected character's
                            // content drives timers, not this one).
                            ScanActiveFileForCamp(state, token);
                        }
                        else
                        {
                            // Track file size for non-selected, non-active files
                            // without reading content.
                            state.LastFileSize = currentSize;
                        }
                    }
                    catch
                    {
                        // Swallow file access errors and retry on next poll
                    }
                }

                // Check for camp-out timeout on actively logging character
                CheckCampOutTimeout(snapshot);

                // Sleep in small increments to allow faster cancellation response
                for (int i = 0; i < 10 && !token.IsCancellationRequested; i++)
                {
                    Thread.Sleep(10);
                }
            }
        }

        /// <summary>
        /// Checks whether the actively logging character should be auto-paused,
        /// via either of two paths, and fires <see cref="CharacterCampedOut"/> if so:
        ///   1. Camp-out  - a camp warning was seen and the camp inactivity
        ///      threshold has since elapsed (the graceful /camp case).
        ///   2. Inactivity fallback - no camp warning at all, but the log has gone
        ///      completely silent for <see cref="InactivityTimeoutSeconds"/> (the
        ///      ungraceful crash / Alt-F4 / link-dead case). Disabled when that
        ///      value is 0.
        /// </summary>
        private void CheckCampOutTimeout(List<CharacterFileState> snapshot)
        {
            var activeState = snapshot.FirstOrDefault(s => s.IsActive);
            if (activeState == null) return;

            // Path 1: graceful camp-out (camp warning + short inactivity threshold).
            if (activeState.CampingOut)
            {
                double secondsSinceCampStart = (DateTime.UtcNow - activeState.CampStartUtc).TotalSeconds;
                if (secondsSinceCampStart >= CampInactivityThresholdSeconds)
                {
                    FireAutoPause(activeState,
                        $"Camp-out confirmed for charID={activeState.CharacterID} after {secondsSinceCampStart:F1}s inactivity");
                }
                return; // While camping, the inactivity fallback below does not apply.
            }

            // Path 2: ungraceful-exit fallback (no camp warning, log went silent).
            if (InactivityTimeoutSeconds > 0)
            {
                double secondsSinceActivity = (DateTime.UtcNow - activeState.LastActivityUtc).TotalSeconds;
                if (secondsSinceActivity >= InactivityTimeoutSeconds)
                {
                    FireAutoPause(activeState,
                        $"Inactivity fallback for charID={activeState.CharacterID}: no log activity for {secondsSinceActivity:F0}s (threshold {InactivityTimeoutSeconds}s); assuming client closed/disconnected");
                }
            }
        }

        /// <summary>
        /// Clears the active/camp state on a character and raises
        /// <see cref="CharacterCampedOut"/> so the host pauses Character-scope
        /// timers (active character -> "(None)"). Shared by the camp-out and
        /// inactivity-fallback paths.
        /// </summary>
        private void FireAutoPause(CharacterFileState activeState, string reason)
        {
            activeState.CampingOut = false;
            activeState.CampStartUtc = DateTime.MinValue;
            activeState.IsActive = false; // No longer actively logging
            activeState.CampScanTail = ""; // Reset boundary buffer

            ThorneLog.Info($"{reason}; firing CharacterCampedOut");

            CharacterCampedOut?.Invoke(this, new CharacterSwitchedEventArgs
            {
                OldCharacterID = activeState.CharacterID,
                NewCharacterID = 0,  // No actively logging character
                NewCharacterName = ""
            });
        }

        /// <summary>
        /// Reads new bytes from a file state and fires LogChunkReceived.
        /// Also detects camp-out patterns and updates activity timestamps.
        /// </summary>
        private void ReadNewContent(CharacterFileState state, CancellationToken token)
        {
            using (var fs = new FileStream(state.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(state.LastFileSize, SeekOrigin.Begin);
                var buffer = new byte[1024];

                while (!token.IsCancellationRequested)
                {
                    var bytesRead = fs.Read(buffer, 0, buffer.Length);
                    state.LastFileSize += bytesRead;

                    if (bytesRead == 0) break;

                    var text = ASCIIEncoding.ASCII.GetString(buffer, 0, bytesRead);

                    // Update activity timestamp
                    state.LastActivityUtc = DateTime.UtcNow;

                    // Camp-out pattern detection (boundary-safe; see ScanForCampPatterns)
                    ScanForCampPatterns(state, text);

                    LogChunkReceived?.Invoke(this, new LogChunkReceivedEventArgs { Text = text });
                }
            }
        }

        /// <summary>
        /// Reads new bytes from the actively logging character's file purely to
        /// detect camp-out patterns, WITHOUT firing LogChunkReceived. Used when the
        /// active character differs from the selected (viewed) character so camp-out
        /// is still detected during browsing mode. Advances LastFileSize and updates
        /// the activity timestamp, mirroring ReadNewContent minus the timer-engine
        /// dispatch.
        /// </summary>
        private void ScanActiveFileForCamp(CharacterFileState state, CancellationToken token)
        {
            using (var fs = new FileStream(state.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(state.LastFileSize, SeekOrigin.Begin);
                var buffer = new byte[1024];

                while (!token.IsCancellationRequested)
                {
                    var bytesRead = fs.Read(buffer, 0, buffer.Length);
                    state.LastFileSize += bytesRead;

                    if (bytesRead == 0) break;

                    var text = ASCIIEncoding.ASCII.GetString(buffer, 0, bytesRead);

                    // Update activity timestamp
                    state.LastActivityUtc = DateTime.UtcNow;

                    // Camp-out pattern detection (boundary-safe; see ScanForCampPatterns)
                    ScanForCampPatterns(state, text);
                }
            }
        }

        /// <summary>
        /// Single-file poll loop (backward-compatible mode).
        /// </summary>
        private void PollLoopSingle(string filePath, CancellationToken token)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            long lastReadLength = new FileInfo(filePath).Length;
            if (lastReadLength < 0) lastReadLength = 0;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var fileSize = new FileInfo(filePath).Length;
                    if (fileSize > lastReadLength)
                    {
                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            fs.Seek(lastReadLength, SeekOrigin.Begin);
                            var buffer = new byte[1024];

                            while (!token.IsCancellationRequested)
                            {
                                var bytesRead = fs.Read(buffer, 0, buffer.Length);
                                lastReadLength += bytesRead;

                                if (bytesRead == 0)
                                    break;

                                var text = ASCIIEncoding.ASCII.GetString(buffer, 0, bytesRead);
                                LogChunkReceived?.Invoke(this, new LogChunkReceivedEventArgs { Text = text });
                            }
                        }
                    }
                }
                catch
                {
                    // Swallow file access errors and retry on next poll
                }

                // Sleep in small increments to allow faster cancellation response
                for (int i = 0; i < 10 && !token.IsCancellationRequested; i++)
                {
                    Thread.Sleep(10);
                }
            }
        }
    }
}
