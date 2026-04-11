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

        /// <summary>
        /// Minimum bytes a non-active file must grow before triggering a switch.
        /// Prevents false positives from OS flushes or antivirus scans.
        /// </summary>
        private const int SwitchThresholdBytes = 10;

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
        /// Fired when new text is read from the active character's log file.
        /// </summary>
        public event EventHandler<LogChunkReceivedEventArgs> LogChunkReceived;

        /// <summary>
        /// Fired when a non-active character's log file starts growing,
        /// indicating the user has switched characters in-game.
        /// </summary>
        public event EventHandler<CharacterSwitchedEventArgs> CharacterSwitched;

        /// <summary>
        /// The file path of the currently active character being monitored.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Whether the monitor is currently running.
        /// </summary>
        public bool IsRunning => monitorThread != null && monitorThread.IsAlive;

        /// <summary>
        /// Start monitoring multiple character log files.
        /// Only the file belonging to activeCharacterID will have its content
        /// read and fired via LogChunkReceived. All files are checked for
        /// growth to detect character switches.
        /// </summary>
        public void Start(List<CharacterFileState> characters, long activeCharacterID)
        {
            if (IsRunning) Stop();

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
                    IsActive = c.CharacterID == activeCharacterID
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

                if (state.IsActive)
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
        /// Update the active character without restarting the monitor.
        /// Called after a character switch has been processed by the UI.
        /// </summary>
        public void SetActiveCharacter(long characterID)
        {
            lock (stateLock)
            {
                if (fileStates == null) return;
                foreach (var fs in fileStates)
                {
                    fs.IsActive = fs.CharacterID == characterID;
                    if (fs.IsActive)
                    {
                        FilePath = fs.FilePath;
                    }
                }
            }
        }

        /// <summary>
        /// Multi-file poll loop. Checks all character files for growth.
        /// Reads content from active file, detects switches on non-active files.
        /// </summary>
        private void PollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                List<CharacterFileState> snapshot;
                lock (stateLock)
                {
                    if (fileStates == null) break;
                    snapshot = fileStates.ToList();
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
                                    FilePath = state.FilePath;
                                }

                                CharacterSwitched?.Invoke(this, new CharacterSwitchedEventArgs
                                {
                                    OldCharacterID = oldCharID,
                                    NewCharacterID = state.CharacterID,
                                    NewCharacterName = state.CharacterName
                                });
                            }
                            else
                            {
                                // Below threshold — update size but don't switch
                                state.LastFileSize = currentSize;
                                continue;
                            }
                        }

                        // Read new content from the (now-)active file
                        ReadNewContent(state, token);
                    }
                    catch
                    {
                        // Swallow file access errors and retry on next poll
                    }
                }

                // Sleep in small increments to allow faster cancellation response
                for (int i = 0; i < 10 && !token.IsCancellationRequested; i++)
                {
                    Thread.Sleep(10);
                }
            }
        }

        /// <summary>
        /// Reads new bytes from a file state and fires LogChunkReceived.
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
                    LogChunkReceived?.Invoke(this, new LogChunkReceivedEventArgs { Text = text });
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
