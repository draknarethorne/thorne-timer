using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ThorneTimer
{
    /// <summary>
    /// Represents the runtime state of a single timer.
    /// Decoupled from the grid — identified by TimerID, not row index.
    /// </summary>
    internal class TimerState
    {
        public long TimerID { get; set; }
        public string Name { get; set; }
        public long CategoryID { get; set; }
        public string StartKeyword { get; set; }
        public string EndKeyword { get; set; }
        public string WAVFile { get; set; }
        public string Speech { get; set; }
        public string Duration { get; set; }
        public string Remaining { get; set; }
        public long ActiveYn { get; set; }
        public long CaseYn { get; set; }
        public long EndlessYn { get; set; }
        public string Style { get; set; }
        public string Scope { get; set; }
        public string DependsOnTimer { get; set; }
        public long DependsOnDelay { get; set; }
        public long ClassID { get; set; }

        // Runtime-only state
        public string ButtonState { get; set; }
        public int Count { get; set; }
        public DateTime? SavedAtUtc { get; set; }

        public TimerState()
        {
            ButtonState = Timers.btnStart;
            Remaining = "";
            Count = 0;
            Scope = "World";
        }

        public bool IsActive => ActiveYn == 1;
        public bool IsRunning => Timers.TimerRunning(ButtonState);
        public bool IsStopped => Timers.TimerStopped(ButtonState);
    }

    /// <summary>
    /// Tracks a running TimerPlus instance keyed by TimerID.
    /// </summary>
    internal class RunningTimer
    {
        public long TimerID { get; set; }
        public TimerPlus Timer { get; set; }
    }

    /// <summary>
    /// Event args for timer state changes (remaining, button, color, count).
    /// </summary>
    internal class TimerStateChangedEventArgs
    {
        public long TimerID { get; set; }
        public string Remaining { get; set; }
        public string ButtonState { get; set; }
        public int Count { get; set; }
        public TimerPlus.TimerType TheType { get; set; }
        public bool Expired { get; set; }

        /// <summary>
        /// True when the timer underwent a meaningful state transition
        /// (start, stop, expire, keyword-stop, deactivate-stop, offline-expire).
        /// False for periodic ticks and restore-restart (UI sync only).
        /// Used by the UI to persist state immediately on transitions.
        /// </summary>
        public bool IsTransition { get; set; }
    }

    /// <summary>
    /// Event args for sound/speech requests.
    /// </summary>
    internal class TimerSoundRequestedEventArgs
    {
        public string WAVFile { get; set; }
        public string Speech { get; set; }
    }

    /// <summary>
    /// Data record for mini view consumption — no grid dependency.
    /// </summary>
    internal class MiniTimerData
    {
        public string Name { get; set; }
        public string Remaining { get; set; }
        public string Style { get; set; }
        public string ButtonState { get; set; }
    }

    /// <summary>
    /// Category runtime state for log processing.
    /// </summary>
    internal class CategoryState
    {
        public long CategoryID { get; set; }
        public string Name { get; set; }
        public string StartKeyword { get; set; }
        public string EndKeyword { get; set; }
        public long AutoStop { get; set; }
    }

    /// <summary>
    /// Central model layer for timer management. Owns all runtime timer state
    /// and operates independently of the grid. The grid subscribes to events
    /// to reflect state changes in the UI.
    /// </summary>
    internal class TimerRuntime
    {
        // All timer definitions loaded from the database
        private readonly List<TimerState> timerStates = new List<TimerState>();

        // Currently running TimerPlus instances, keyed by TimerID
        private readonly List<RunningTimer> runningTimers = new List<RunningTimer>();

        // Category definitions for log processing
        private readonly List<CategoryState> categoryStates = new List<CategoryState>();

        // Lock for thread safety between poll thread and UI thread
        private readonly object syncLock = new object();

        // Events
        public event EventHandler<TimerStateChangedEventArgs> TimerStateChanged;
        public event EventHandler<TimerSoundRequestedEventArgs> TimerSoundRequested;
        public event EventHandler CategoryTimersActivated;

        /// <summary>
        /// Load timer states from a GridData list (from Database.GetTimers).
        /// Preserves runtime state (counts, running timers) for timers that still exist.
        /// </summary>
        public void LoadTimers(SortableBindingList<Timers.GridData> gridData)
        {
            lock (syncLock)
            {
                // Snapshot current runtime state for preservation
                var previousStates = new Dictionary<long, TimerState>();
                foreach (var ts in timerStates)
                {
                    previousStates[ts.TimerID] = ts;
                }

                timerStates.Clear();

                foreach (var gd in gridData)
                {
                    var ts = new TimerState
                    {
                        TimerID = gd.ID,
                        Name = gd.Name,
                        CategoryID = gd.CategoryID,
                        StartKeyword = gd.StartKeyword ?? "",
                        EndKeyword = gd.EndKeyword ?? "",
                        WAVFile = gd.WAVFile ?? "",
                        Speech = gd.Speech ?? "",
                        Duration = gd.Duration ?? "00:00:00",
                        Remaining = gd.Remaining ?? "",
                        CaseYn = gd.CaseYn,
                        EndlessYn = gd.EndlessYn,
                        Style = gd.Style ?? "Normal",
                        Scope = gd.Scope ?? "World",
                        DependsOnTimer = gd.DependsOnTimer ?? "",
                        DependsOnDelay = gd.DependsOnDelay,
                        ClassID = gd.ClassID
                    };

                    // For World-scope timers, the global timers.ActiveYn is
                    // authoritative — all characters share the same setting.
                    // For Character / Character+ timers, ActiveYn is per-character
                    // and comes from timer_runtime_state.  Default to 0 here;
                    // RestoreCharacterState will set the correct per-character value.
                    if (string.IsNullOrEmpty(ts.Scope) || ts.Scope == "World")
                        ts.ActiveYn = gd.ActiveYn;
                    else
                        ts.ActiveYn = 0;

                    // Restore runtime state if this timer was already tracked
                    if (previousStates.TryGetValue(gd.ID, out TimerState prev))
                    {
                        ts.Count = prev.Count;

                        // Only preserve running state (ButtonState/Remaining) if
                        // the timer actually has a live TimerPlus in runningTimers.
                        // This prevents stale "was running" markers from leaking
                        // across character switches (SaveCharacterState stops the
                        // TimerPlus but leaves markers on TimerState for DB save).
                        bool actuallyRunning = runningTimers.Any(rt => rt.TimerID == gd.ID);
                        if (actuallyRunning)
                        {
                            ts.ButtonState = prev.ButtonState;
                            ts.Remaining = prev.Remaining;
                        }

                        ThorneLog.Debug($"LoadTimers TID={gd.ID} \"{gd.Name}\" Scope={ts.Scope} hasPrev=true actuallyRunning={actuallyRunning} Btn={ts.ButtonState} Rem={ts.Remaining} Act={ts.ActiveYn} Count={ts.Count}");
                    }
                    else
                    {
                        ThorneLog.Debug($"LoadTimers TID={gd.ID} \"{gd.Name}\" Scope={ts.Scope} hasPrev=false Btn={ts.ButtonState} Rem={ts.Remaining} Act={ts.ActiveYn}");
                    }

                    timerStates.Add(ts);
                }

                // Clean up running timers for IDs that no longer exist
                var validIDs = new HashSet<long>(timerStates.Select(t => t.TimerID));
                for (int i = runningTimers.Count - 1; i >= 0; i--)
                {
                    if (!validIDs.Contains(runningTimers[i].TimerID))
                    {
                        runningTimers[i].Timer.Stop();
                        runningTimers[i].Timer.Dispose();
                        runningTimers.RemoveAt(i);
                    }
                }

                ThorneLog.Info($"LoadTimers complete: {timerStates.Count} timers, {runningTimers.Count} running");
                ThorneLog.DumpTimerStates("LoadTimers-end", timerStates);
            }
        }

        /// <summary>
        /// Load category states from the database.
        /// </summary>
        public void LoadCategories(List<Categories.GridData> categories)
        {
            lock (syncLock)
            {
                categoryStates.Clear();
                foreach (var c in categories)
                {
                    categoryStates.Add(new CategoryState
                    {
                        CategoryID = c.ID,
                        Name = c.Name ?? "",
                        StartKeyword = c.StartKeyword ?? "",
                        EndKeyword = c.EndKeyword ?? "",
                        AutoStop = c.AutoStop
                    });
                }
            }
        }

        /// <summary>
        /// Process a chunk of log text — matches keywords against all active timers.
        /// Called from the log monitor thread; fires events back to UI.
        /// </summary>
        public void ProcessLogText(string chunk)
        {
            lock (syncLock)
            {
                // Process Categories
                foreach (var cat in categoryStates)
                {
                    if (cat.StartKeyword.Length > 0 && chunk.Contains(cat.StartKeyword))
                    {
                        ActivateCategoryTimers(cat.CategoryID, true);
                    }
                    else if (cat.EndKeyword.Length > 0 && chunk.Contains(cat.EndKeyword))
                    {
                        if (cat.AutoStop == 1)
                        {
                            ActivateCategoryTimers(cat.CategoryID, false);
                        }
                    }
                }

                // Process Active Timers
                foreach (var ts in timerStates)
                {
                    if (!ts.IsActive) continue;

                    bool containsStart;
                    bool containsEnd;

                    if (ts.CaseYn != 0)
                    {
                        containsStart = chunk.IndexOf(ts.StartKeyword, StringComparison.Ordinal) >= 0;
                        containsEnd = chunk.IndexOf(ts.EndKeyword, StringComparison.Ordinal) >= 0;
                    }
                    else
                    {
                        containsStart = chunk.IndexOf(ts.StartKeyword, StringComparison.OrdinalIgnoreCase) >= 0;
                        containsEnd = chunk.IndexOf(ts.EndKeyword, StringComparison.OrdinalIgnoreCase) >= 0;
                    }

                    if (containsStart && ts.StartKeyword.Length > 0)
                    {
                        if (ts.IsStopped)
                        {
                            TriggerTimer(ts);
                        }
                        else if (ts.IsRunning)
                        {
                            // Buff and Pet timers reset when their start keyword fires again
                            if (ts.Style == "Buff" || ts.Style == "Pet")
                            {
                                StopTimerInternal(ts, false);
                                StartTimerInternal(ts);
                            }
                        }
                    }

                    if (containsEnd && ts.EndKeyword.Length > 0)
                    {
                        if (ts.IsRunning)
                        {
                            StopTimerInternal(ts, false);
                            FireStateChanged(ts, false, isTransition: true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Trigger a timer — checks dependencies before starting.
        /// </summary>
        private void TriggerTimer(TimerState ts)
        {
            if (string.IsNullOrEmpty(ts.DependsOnTimer))
            {
                // No dependency, just start
                StartTimerInternal(ts);
            }
            else
            {
                double delayMS = ts.DependsOnDelay * 1000.0;
                if (CheckDependentTimer(ts.DependsOnTimer, delayMS))
                {
                    StartTimerInternal(ts);
                }
            }
        }

        /// <summary>
        /// Manually start a timer by ID (from UI button click).
        /// </summary>
        public void StartTimer(long timerID)
        {
            lock (syncLock)
            {
                var ts = timerStates.FirstOrDefault(t => t.TimerID == timerID);
                if (ts != null && ts.IsStopped)
                {
                    TriggerTimer(ts);
                }
            }
        }

        /// <summary>
        /// Manually stop a timer by ID (from UI button click).
        /// </summary>
        public void StopTimer(long timerID)
        {
            lock (syncLock)
            {
                var ts = timerStates.FirstOrDefault(t => t.TimerID == timerID);
                if (ts != null && ts.IsRunning)
                {
                    StopTimerInternal(ts, false);
                    FireStateChanged(ts, false, isTransition: true);
                }
            }
        }

        /// <summary>
        /// Start the timer countdown for a TimerState.
        /// </summary>
        private void StartTimerInternal(TimerState ts)
        {
            string durationText = ts.Duration ?? "";

            // Validate duration format
            if (!ValidDuration(durationText)) return;

            // Increment count
            ts.Count++;

            // Determine timer type and button state from Style
            TimerPlus.TimerType timerType;
            string pingDuration = durationText;

            switch (ts.Style)
            {
                case "Ping":
                    timerType = TimerPlus.TimerType.Ping;
                    if (TimerPlus.GetMilliseconds(pingDuration) == 0) return;
                    ts.ButtonState = Timers.btnPing;
                    break;
                case "Buff":
                    timerType = TimerPlus.TimerType.Buff;
                    if (TimerPlus.GetMilliseconds(durationText) == 0) return;
                    ts.ButtonState = Timers.btnBuff;
                    break;
                case "Pet":
                    timerType = TimerPlus.TimerType.Pet;
                    if (TimerPlus.GetMilliseconds(durationText) == 0) return;
                    ts.ButtonState = Timers.btnPet;
                    break;
                default:
                    timerType = TimerPlus.TimerType.Normal;
                    if (TimerPlus.GetMilliseconds(durationText) == 0) return;
                    ts.ButtonState = Timers.btnStop;
                    break;
            }

            // Create and start the TimerPlus
            string effectiveDuration = (timerType == TimerPlus.TimerType.Ping) ? pingDuration : durationText;
            TimerPlus tp = new TimerPlus
            {
                TimerID = ts.TimerID,
                Interval = 1000,
                ElapsedTime = 0,
                DurationTime = TimerPlus.GetMilliseconds(effectiveDuration)
            };
            tp.TimerElapsed += OnTimerElapsed;
            tp.TimerExpired += OnTimerExpired;
            tp.TheType = timerType;

            runningTimers.Add(new RunningTimer { TimerID = ts.TimerID, Timer = tp });

            ts.Remaining = tp.GetTimeRemaining();
            tp.Start();

            // Fire sound for Ping timers on start
            if (timerType == TimerPlus.TimerType.Ping)
            {
                FireSoundRequested(ts);
            }

            FireStateChanged(ts, false, isTransition: true);
        }

        /// <summary>
        /// Stop a timer internally.
        /// </summary>
        private void StopTimerInternal(TimerState ts, bool resetYn)
        {
            for (int i = runningTimers.Count - 1; i >= 0; i--)
            {
                if (runningTimers[i].TimerID == ts.TimerID)
                {
                    var rt = runningTimers[i];
                    if (resetYn)
                    {
                        rt.Timer.Stop();
                        rt.Timer.ElapsedTime = 0;
                        rt.Timer.Start();
                        break; // reset only applies to one instance
                    }
                    else
                    {
                        rt.Timer.Stop();
                        rt.Timer.TimerElapsed -= OnTimerElapsed;
                        rt.Timer.TimerExpired -= OnTimerExpired;
                        rt.Timer.Dispose();
                        runningTimers.RemoveAt(i);
                        // Don't break � clean up ALL instances for this TimerID
                    }
                }
            }

            if (!resetYn)
            {
                ts.ButtonState = Timers.btnStart;
                ts.Remaining = "";
            }
        }

        /// <summary>
        /// Called every second by TimerPlus.
        /// </summary>
        private void OnTimerElapsed(object sender, TimerPlus e)
        {
            lock (syncLock)
            {
                var ts = timerStates.FirstOrDefault(t => t.TimerID == e.TimerID);
                if (ts == null) return;

                ts.Remaining = e.GetTimeRemaining();
                FireStateChanged(ts, false);
            }
        }

        /// <summary>
        /// Called when a timer reaches zero.
        /// </summary>
        private void OnTimerExpired(object sender, TimerPlus e)
        {
            lock (syncLock)
            {
                var ts = timerStates.FirstOrDefault(t => t.TimerID == e.TimerID);
                if (ts == null) return;

                if (ts.EndlessYn == 0)
                {
                    // One-shot: stop and clean up
                    StopTimerInternal(ts, false);
                }
                else
                {
                    // Endless/loop: reset and keep running
                    StopTimerInternal(ts, true);
                    ts.Remaining = "";
                }

                // Play sounds on expiry (except Ping, which plays on start)
                if (e.TheType != TimerPlus.TimerType.Ping)
                {
                    FireSoundRequested(ts);
                }

                FireStateChanged(ts, true, isTransition: true);
            }
        }

        /// <summary>
        /// Stop all running timers (used on app close, database switch, etc.).
        /// </summary>
        public void StopAllTimers()
        {
            lock (syncLock)
            {
                foreach (var rt in runningTimers)
                {
                    rt.Timer.Stop();
                    rt.Timer.TimerElapsed -= OnTimerElapsed;
                    rt.Timer.TimerExpired -= OnTimerExpired;
                    rt.Timer.Dispose();
                }
                runningTimers.Clear();

                foreach (var ts in timerStates)
                {
                    if (ts.IsRunning)
                    {
                        ts.ButtonState = Timers.btnStart;
                        ts.Remaining = "";
                        FireStateChanged(ts, false);
                    }
                }
            }
        }

        /// <summary>
        /// Activate or deactivate all timers in a category.
        /// </summary>
        public void ActivateCategoryTimers(long categoryID, bool activate)
        {
            lock (syncLock)
            {
                foreach (var ts in timerStates)
                {
                    if (ts.CategoryID == categoryID)
                    {
                        ts.ActiveYn = activate ? 1 : 0;

                        if (!activate && ts.IsRunning)
                        {
                            StopTimerInternal(ts, false);
                            FireStateChanged(ts, false, isTransition: true);
                        }
                    }
                }
            }

            CategoryTimersActivated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Update the ActiveYn for a specific timer (from grid checkbox change).
        /// </summary>
        public void SetTimerActive(long timerID, bool active)
        {
            lock (syncLock)
            {
                var ts = timerStates.FirstOrDefault(t => t.TimerID == timerID);
                if (ts != null)
                {
                    ts.ActiveYn = active ? 1 : 0;
                }
            }
        }

        /// <summary>
        /// Add a single new timer to the runtime (used when the user adds
        /// a timer via the grid without reloading the full timer list).
        /// </summary>
        public void AddTimerState(Timers.GridData gd)
        {
            lock (syncLock)
            {
                var ts = new TimerState
                {
                    TimerID = gd.ID,
                    Name = gd.Name ?? "",
                    CategoryID = gd.CategoryID,
                    StartKeyword = gd.StartKeyword ?? "",
                    EndKeyword = gd.EndKeyword ?? "",
                    WAVFile = gd.WAVFile ?? "",
                    Speech = gd.Speech ?? "",
                    Duration = gd.Duration ?? "00:00:00",
                    Remaining = "",
                    CaseYn = gd.CaseYn,
                    EndlessYn = gd.EndlessYn,
                    Style = gd.Style ?? "Normal",
                    Scope = gd.Scope ?? "World",
                    DependsOnTimer = gd.DependsOnTimer ?? "",
                    DependsOnDelay = gd.DependsOnDelay,
                    ClassID = gd.ClassID,
                    ActiveYn = gd.ActiveYn
                };
                timerStates.Add(ts);
            }
        }

        /// <summary>
        /// Remove a single timer from the runtime (used when the user
        /// deletes a timer via the grid without reloading the full list).
        /// </summary>
        public void RemoveTimerState(long timerID)
        {
            lock (syncLock)
            {
                var ts = timerStates.FirstOrDefault(t => t.TimerID == timerID);
                if (ts != null)
                    timerStates.Remove(ts);
            }
        }

        /// <summary>
        /// Sync editable fields (Name, Style, Keywords, etc.) from the grid
        /// back to TimerRuntime without disturbing runtime state (ButtonState,
        /// Count, Remaining, running timers).
        /// </summary>
        public void SyncTimerFieldsFromGrid(DataGridView grid)
        {
            lock (syncLock)
            {
                foreach (DataGridViewRow row in grid.Rows)
                {
                    long rowID = Convert.ToInt64(row.Cells[grid.Columns["ID"].Index].Value);
                    var ts = timerStates.FirstOrDefault(t => t.TimerID == rowID);
                    if (ts == null) continue;

                    ts.Name = Convert.ToString(row.Cells[grid.Columns["Name"].Index].Value) ?? "";
                    ts.Style = Convert.ToString(row.Cells[grid.Columns["Style"].Index].Value) ?? "Normal";
                    ts.StartKeyword = Convert.ToString(row.Cells[grid.Columns["StartKeyword"].Index].Value) ?? "";
                    ts.EndKeyword = Convert.ToString(row.Cells[grid.Columns["EndKeyword"].Index].Value) ?? "";
                    ts.WAVFile = Convert.ToString(row.Cells[grid.Columns["WAVFile"].Index].Value) ?? "";
                    ts.Speech = Convert.ToString(row.Cells[grid.Columns["Speech"].Index].Value) ?? "";
                    ts.Duration = Convert.ToString(row.Cells[grid.Columns["Duration"].Index].Value) ?? "00:00:00";
                    ts.CaseYn = Convert.ToInt64(row.Cells[grid.Columns["CaseYn"].Index].Value ?? 0);
                    ts.EndlessYn = Convert.ToInt64(row.Cells[grid.Columns["EndlessYn"].Index].Value ?? 0);
                    ts.DependsOnTimer = Convert.ToString(row.Cells[grid.Columns["DependsOnTimer"].Index].Value) ?? "";

                    object delayVal = row.Cells[grid.Columns["DependsOnDelay"].Index].Value;
                    long delay;
                    long.TryParse(Convert.ToString(delayVal), out delay);
                    ts.DependsOnDelay = delay;

                    // Sync fields that affect per-character state persistence
                    ts.ActiveYn = Convert.ToInt64(row.Cells[grid.Columns["ActiveYn"].Index].Value ?? 0);
                    ts.Scope = Convert.ToString(row.Cells[grid.Columns["Scope"].Index].Value) ?? "World";

                    object classVal = row.Cells[grid.Columns["ClassID"].Index].Value;
                    ts.ClassID = (classVal == null || classVal == DBNull.Value) ? 0 : Convert.ToInt64(classVal);
                }
            }
        }

        /// <summary>
        /// Reset all timer counts.
        /// </summary>
        public void ResetCounts()
        {
            lock (syncLock)
            {
                foreach (var ts in timerStates)
                {
                    ts.Count = 0;
                }
            }
        }

        /// <summary>
        /// Get data for mini views — no grid dependency.
        /// </summary>
        public List<MiniTimerData> GetMiniViewData()
        {
            lock (syncLock)
            {
                var data = new List<MiniTimerData>();
                foreach (var ts in timerStates)
                {
                    if (ts.IsRunning || ts.ButtonState == Timers.btnPing)
                    {
                        data.Add(new MiniTimerData
                        {
                            Name = ts.Name,
                            Remaining = ts.Remaining,
                            Style = ts.Style ?? "Normal",
                            ButtonState = ts.ButtonState
                        });
                    }
                }
                return data;
            }
        }

        /// <summary>
        /// When true, GetVisibleTimers returns all timers regardless of ClassID.
        /// When false, only timers matching the specified class (or Global) are returned.
        /// </summary>
        public bool ShowAllClasses { get; set; } = true;

        /// <summary>
        /// When true, only active timers (ActiveYn == 1) are shown in the grid.
        /// When false, all timers are shown regardless of Active status.
        /// </summary>
        public bool ShowActiveOnly { get; set; } = false;

        /// <summary>
        /// Get a snapshot of all timer states (for grid refresh, persistence, etc.).
        /// </summary>
        public List<TimerState> GetAllStates()
        {
            lock (syncLock)
            {
                return timerStates.ToList();
            }
        }

        /// <summary>
        /// Get timer states filtered by class. Returns all timers where ClassID is 0 (Global)
        /// or matches the specified classID. If ShowAllClasses is true, returns all timers.
        /// </summary>
        public List<TimerState> GetVisibleTimers(long classID)
        {
            lock (syncLock)
            {
                if (ShowAllClasses || classID <= 0)
                {
                    return timerStates.ToList();
                }
                return timerStates.Where(t => t.ClassID == 0 || t.ClassID == classID).ToList();
            }
        }

        /// <summary>
        /// Get a specific timer state by ID.
        /// </summary>
        public TimerState GetState(long timerID)
        {
            lock (syncLock)
            {
                return timerStates.FirstOrDefault(t => t.TimerID == timerID);
            }
        }

        /// <summary>
        /// Get the count of currently running timers.
        /// </summary>
        public int RunningTimerCount
        {
            get
            {
                lock (syncLock)
                {
                    return runningTimers.Count;
                }
            }
        }

        // --- Dependency checking ---

        private bool CheckDependentTimer(string dependentName, double delayMS)
        {
            foreach (var ts in timerStates)
            {
                if (ts.IsRunning && ts.Name == dependentName)
                {
                    if (ValidDuration(ts.Duration))
                    {
                        double remainingMS = TimerPlus.GetMilliseconds(ts.Remaining ?? "00:00:00");
                        double durationMS = TimerPlus.GetMilliseconds(ts.Duration);
                        double elapsedMS = durationMS - remainingMS;

                        if (elapsedMS > delayMS)
                        {
                            if (string.IsNullOrEmpty(ts.DependsOnTimer))
                            {
                                return true;
                            }
                            else
                            {
                                // Walk the dependency chain
                                double chainDelayMS = ts.DependsOnDelay * 1000.0;
                                return CheckDependentTimer(ts.DependsOnTimer, chainDelayMS);
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool ValidDuration(string durationText)
        {
            if (string.IsNullOrEmpty(durationText)) return false;

            // Check for DD HH:MM:SS or DDd HH:MM:SS (space separates days from time)
            int spaceIdx = durationText.IndexOf(' ');
            if (spaceIdx > 0)
            {
                string dayPart = durationText.Substring(0, spaceIdx).TrimEnd('d');
                if (dayPart.Length == 0 || !int.TryParse(dayPart, out _))
                    return false;

                string[] parts = durationText.Substring(spaceIdx + 1).Split(':');
                if (parts.Length != 3) return false;

                foreach (string p in parts)
                {
                    if (p.Length != 2 || !int.TryParse(p, out _)) return false;
                }
                return true;
            }

            // HH:MM:SS
            string[] timeParts = durationText.Split(':');
            if (timeParts.Length != 3) return false;

            foreach (string p in timeParts)
            {
                if (p.Length != 2 || !int.TryParse(p, out _)) return false;
            }

            return true;
        }

        // --- Event firing helpers ---

        private void FireStateChanged(TimerState ts, bool expired, bool isTransition = false)
        {
            TimerStateChanged?.Invoke(this, new TimerStateChangedEventArgs
            {
                TimerID = ts.TimerID,
                Remaining = ts.Remaining,
                ButtonState = ts.ButtonState,
                Count = ts.Count,
                TheType = GetTimerType(ts.Style),
                Expired = expired,
                IsTransition = isTransition
            });
        }

        private void FireSoundRequested(TimerState ts)
        {
            if ((ts.WAVFile != null && ts.WAVFile.Length > 0) || (ts.Speech != null && ts.Speech.Length > 0))
            {
                TimerSoundRequested?.Invoke(this, new TimerSoundRequestedEventArgs
                {
                    WAVFile = ts.WAVFile ?? "",
                    Speech = ts.Speech ?? ""
                });
            }
        }

        private TimerPlus.TimerType GetTimerType(string style)
        {
            switch (style)
            {
                case "Ping": return TimerPlus.TimerType.Ping;
                case "Buff": return TimerPlus.TimerType.Buff;
                case "Pet": return TimerPlus.TimerType.Pet;
                default: return TimerPlus.TimerType.Normal;
            }
        }

        // --- Character switch: scope-aware save / restore ---

        /// <summary>
        /// Saves the outgoing character's timer state. Character and Character+
        /// scope running timers are stopped (remaining time frozen in TimerState).
        /// World-scope timers keep running untouched.
        /// Returns a snapshot list suitable for Database.SaveTimerStates.
        /// </summary>
        public List<TimerState> SaveCharacterState()
        {
            lock (syncLock)
            {
                ThorneLog.Info($"SaveCharacterState: timerStates={timerStates.Count} runningTimers={runningTimers.Count}");
                ThorneLog.DumpTimerStates("SaveCharacterState-before", timerStates);

                // Stop Character / Character+ scope running timers and freeze
                // their state for DB persistence so RestoreCharacterState can
                // restart them.  Include Ping timers — they have a live
                // TimerPlus even though TimerRunning() excludes them.
                foreach (var ts in timerStates)
                {
                    if ((ts.Scope == "Character" || ts.Scope == "Character+")
                        && (ts.IsRunning || Timers.PingTimer(ts.ButtonState)))
                    {
                        // Capture current remaining time BEFORE StopTimerInternal clears it
                        string remaining = "";
                        var rt = runningTimers.FirstOrDefault(r => r.TimerID == ts.TimerID);
                        if (rt != null)
                        {
                            remaining = rt.Timer.GetTimeRemaining();
                        }
                        StopTimerInternal(ts, false);
                        // Restore the frozen remaining and mark with the style button
                        // state so the DB snapshot knows this timer was running.
                        // Note: in-memory state retains these markers, but LoadTimers
                        // will only preserve state for timers still in runningTimers,
                        // so these stale markers won't leak to the next character.
                        ts.Remaining = remaining;
                        ts.ButtonState = GetStyleButtonState(ts.Style);

                            ThorneLog.Debug($"  FROZEN TID={ts.TimerID} \"{ts.Name}\" Scope={ts.Scope} Btn={ts.ButtonState} Rem={ts.Remaining} Act={ts.ActiveYn}");
                            }
                        }

                        ThorneLog.DumpTimerStates("SaveCharacterState-after", timerStates);
                        return timerStates.ToList();
            }
        }

        /// <summary>
        /// Restores an incoming character's timer state from previously saved data.
        /// Restores per-character ActiveYn preferences for all timers.
        /// Character-scope timers are restarted ONLY if isActive=true (character is actively logging).
        /// Character+ scope timers are restarted with remaining adjusted for
        /// elapsed offline time (server-tracked cooldowns).
        /// World-scope timers are left alone (still running).
        /// </summary>
        /// <param name="savedStates">Previously saved timer states for this character</param>
        /// <param name="isActive">True if this character is actively logging (LogMonitor active), false if just viewing</param>
        public void RestoreCharacterState(Dictionary<long, TimerState> savedStates, bool isActive = true)
        {
            lock (syncLock)
            {
                ThorneLog.Info($"RestoreCharacterState: timerStates={timerStates.Count} savedStates={savedStates.Count} isActive={isActive}");
                ThorneLog.DumpSavedStates("RestoreCharacterState-input", savedStates);

                foreach (var ts in timerStates)
                {
                    // World-scope timers keep running across character switches —
                    // never touch their state here.  World restore happens only
                    // once at app startup via RestoreWorldTimersOnStartup.
                    if (ts.Scope == "World")
                    {
                        ThorneLog.Debug($"  RESTORE TID={ts.TimerID} \"{ts.Name}\" Scope=World: SKIPPED (world)");
                        continue;
                    }

                    if (!savedStates.TryGetValue(ts.TimerID, out TimerState saved))
                    {
                        // Character / Character+ timers with no per-character saved
                        // state default to inactive.  The global timers.ActiveYn must
                        // not leak to characters that never explicitly activated them.
                        if (ts.Scope == "Character" || ts.Scope == "Character+")
                        {
                            ts.ActiveYn = 0;
                        }
                        ThorneLog.Debug($"  RESTORE TID={ts.TimerID} \"{ts.Name}\" Scope={ts.Scope}: NO saved state -> ActiveYn={ts.ActiveYn}");
                        continue;
                    }

                    // Always restore count and per-character ActiveYn preference
                    ts.Count = saved.Count;
                    ts.ActiveYn = saved.ActiveYn;

                    // Was this timer running when the character was last active?
                    // Include Ping timers — they have style-based ButtonState markers too.
                    bool wasRunning = Timers.TimerRunning(saved.ButtonState) || Timers.PingTimer(saved.ButtonState);
                    bool hasRemaining = !string.IsNullOrEmpty(saved.Remaining);

                    ThorneLog.Debug($"  RESTORE TID={ts.TimerID} \"{ts.Name}\" Scope={ts.Scope} savedBtn={saved.ButtonState} savedRem={saved.Remaining} savedAct={saved.ActiveYn} wasRunning={wasRunning} hasRemaining={hasRemaining}");

                    if (wasRunning && hasRemaining)
                    {
                        // Character-scope timers should only run when the character is actively logging.
                        // If we're just viewing this character (not actively logging), keep timers frozen.
                        if (ts.Scope == "Character" && !isActive)
                        {
                            ThorneLog.Debug($"  RESTORE TID={ts.TimerID} \"{ts.Name}\" Scope=Character: SKIPPED (character not active), frozen at {saved.Remaining}");
                            ts.Remaining = saved.Remaining;
                            ts.ButtonState = saved.ButtonState;
                            continue;
                        }

                        if (!ValidDuration(saved.Remaining)) continue;

                        string effectiveRemaining = saved.Remaining;

                        // Character+ timers continue on the server while offline.
                        // Subtract elapsed time since the state was saved.
                        if (ts.Scope == "Character+" && saved.SavedAtUtc.HasValue)
                        {
                            double savedRemainingMS = TimerPlus.GetMilliseconds(saved.Remaining);
                            double elapsedOfflineMS = (DateTime.UtcNow - saved.SavedAtUtc.Value).TotalMilliseconds;
                            double adjustedMS = savedRemainingMS - elapsedOfflineMS;

                            ThorneLog.Debug($"  CHAR+ TID={ts.TimerID} \"{ts.Name}\": savedRem={saved.Remaining} elapsedOffline={TimeSpan.FromMilliseconds(elapsedOfflineMS):hh\\:mm\\:ss} adjustedMS={adjustedMS:F0}");

                            if (adjustedMS <= 0)
                            {
                                // Timer expired while offline — mark as stopped
                                ts.ButtonState = Timers.btnStart;
                                ts.Remaining = "00:00:00";
                                ts.Count = Math.Max(0, saved.Count - 1);
                                ThorneLog.Debug($"  CHAR+ TID={ts.TimerID} \"{ts.Name}\": EXPIRED offline");
                                FireStateChanged(ts, false, isTransition: true);
                                continue;
                            }

                            TimeSpan adjusted = TimeSpan.FromMilliseconds(adjustedMS);
                            if (adjusted.Days > 0)
                                effectiveRemaining = string.Format("{0}d {1:00}:{2:00}:{3:00}", adjusted.Days, adjusted.Hours, adjusted.Minutes, adjusted.Seconds);
                            else
                                effectiveRemaining = string.Format("{0:00}:{1:00}:{2:00}", adjusted.Hours, adjusted.Minutes, adjusted.Seconds);
                        }
                        else if (ts.Scope == "Character+" && !saved.SavedAtUtc.HasValue)
                        {
                            ThorneLog.Warn($"  CHAR+ TID={ts.TimerID} \"{ts.Name}\": no SavedAtUtc, cannot compute offline elapsed, using raw remaining={saved.Remaining}");
                        }

                        if (TimerPlus.GetMilliseconds(effectiveRemaining) <= 0)
                        {
                            ThorneLog.Debug($"  RESTORE TID={ts.TimerID} \"{ts.Name}\": effectiveRemaining={effectiveRemaining} resolved to 0ms, skipping restart");
                            continue;
                        }

                        // Restart the timer with the (possibly adjusted) remaining time
                        ThorneLog.Debug($"  RESTORE TID={ts.TimerID} \"{ts.Name}\" Scope={ts.Scope}: RESTARTING with remaining={effectiveRemaining} (saved={saved.Remaining})");
                        RestartTimerFromRemaining(ts, effectiveRemaining, saved.ButtonState);
                    }
                }

                ThorneLog.Info($"RestoreCharacterState complete: {runningTimers.Count} running");
                ThorneLog.DumpTimerStates("RestoreCharacterState-end", timerStates);
            }
        }

        /// <summary>
        /// Restores World-scope timers that were running when the app last closed.
        /// Computes elapsed offline time from SavedAtUtc and adjusts remaining.
        /// Call once during app startup, after LoadTimers + RestoreCharacterState.
        /// </summary>
        public void RestoreWorldTimersOnStartup(Dictionary<long, TimerState> savedStates)
        {
            lock (syncLock)
            {
                ThorneLog.Info($"RestoreWorldTimersOnStartup: timerStates={timerStates.Count} savedStates={savedStates.Count}");
                ThorneLog.DumpSavedStates("RestoreWorldTimers-input", savedStates);

                foreach (var ts in timerStates)
                {
                    if (ts.Scope != "World") continue;
                    if (!savedStates.TryGetValue(ts.TimerID, out TimerState saved))
                    {
                        ThorneLog.Debug($"  WORLD TID={ts.TimerID} \"{ts.Name}\": no saved state");
                        continue;
                    }

                    // Restore count
                    ts.Count = saved.Count;

                    bool wasRunning = Timers.TimerRunning(saved.ButtonState) || Timers.PingTimer(saved.ButtonState);
                    if (!wasRunning)
                    {
                        ThorneLog.Debug($"  WORLD TID={ts.TimerID} \"{ts.Name}\": saved but not running (Btn={saved.ButtonState})");
                        continue;
                    }
                    if (string.IsNullOrEmpty(saved.Remaining))
                    {
                        ThorneLog.Debug($"  WORLD TID={ts.TimerID} \"{ts.Name}\": running but no remaining");
                        continue;
                    }
                    if (!ValidDuration(saved.Remaining))
                    {
                        ThorneLog.Warn($"  WORLD TID={ts.TimerID} \"{ts.Name}\": invalid remaining={saved.Remaining}");
                        continue;
                    }

                    string effectiveRemaining = saved.Remaining;

                    // Adjust for elapsed offline time
                    if (saved.SavedAtUtc.HasValue)
                    {
                        double savedRemainingMS = TimerPlus.GetMilliseconds(saved.Remaining);
                        double elapsedOfflineMS = (DateTime.UtcNow - saved.SavedAtUtc.Value).TotalMilliseconds;
                        double adjustedMS = savedRemainingMS - elapsedOfflineMS;

                        ThorneLog.Debug($"  WORLD TID={ts.TimerID} \"{ts.Name}\": savedRem={saved.Remaining} elapsedOffline={TimeSpan.FromMilliseconds(elapsedOfflineMS):hh\\:mm\\:ss} adjustedMS={adjustedMS:F0}");

                        if (adjustedMS <= 0)
                        {
                            ts.ButtonState = Timers.btnStart;
                            ts.Remaining = "00:00:00";
                            ts.Count = Math.Max(0, saved.Count - 1);
                            ThorneLog.Debug($"  WORLD TID={ts.TimerID} \"{ts.Name}\": EXPIRED offline");
                            FireStateChanged(ts, false, isTransition: true);
                            continue;
                        }

                        TimeSpan adjusted = TimeSpan.FromMilliseconds(adjustedMS);
                        if (adjusted.Days > 0)
                            effectiveRemaining = string.Format("{0}d {1:00}:{2:00}:{3:00}", adjusted.Days, adjusted.Hours, adjusted.Minutes, adjusted.Seconds);
                        else
                            effectiveRemaining = string.Format("{0:00}:{1:00}:{2:00}", adjusted.Hours, adjusted.Minutes, adjusted.Seconds);
                    }

                    if (TimerPlus.GetMilliseconds(effectiveRemaining) <= 0) continue;

                    ThorneLog.Debug($"  WORLD TID={ts.TimerID} \"{ts.Name}\": RESTARTING with remaining={effectiveRemaining}");
                    RestartTimerFromRemaining(ts, effectiveRemaining, saved.ButtonState);
                }

                ThorneLog.Info($"RestoreWorldTimersOnStartup complete: {runningTimers.Count} running");
                ThorneLog.DumpTimerStates("RestoreWorldTimers-end", timerStates);
            }
        }

        /// <summary>
        /// Restarts a timer with a specific remaining time and button state.
        /// Used when restoring Character/Character+ scope timers after a
        /// character switch, or World timers after app restart.
        /// </summary>
        private void RestartTimerFromRemaining(TimerState ts, string remaining, string buttonState)
        {
            // Defense: stop any existing TimerPlus for this timer ID first.
            // Prevents duplicate running instances if RestoreCharacterState
            // is called more than once (e.g. during initialization).
            for (int i = runningTimers.Count - 1; i >= 0; i--)
            {
                if (runningTimers[i].TimerID == ts.TimerID)
                {
                    ThorneLog.Debug($"RestartTimerFromRemaining TID={ts.TimerID}: stopping existing TimerPlus before restart");
                    runningTimers[i].Timer.Stop();
                    runningTimers[i].Timer.Dispose();
                    runningTimers.RemoveAt(i);
                }
            }

            ts.ButtonState = buttonState;
            ts.Remaining = remaining;

            TimerPlus.TimerType timerType = GetTimerType(ts.Style);
            double remainingMS = TimerPlus.GetMilliseconds(remaining);

            TimerPlus tp = new TimerPlus
            {
                TimerID = ts.TimerID,
                Interval = 1000,
                ElapsedTime = 0,
                DurationTime = remainingMS
            };
            tp.TimerElapsed += OnTimerElapsed;
            tp.TimerExpired += OnTimerExpired;
            tp.TheType = timerType;

            runningTimers.Add(new RunningTimer { TimerID = ts.TimerID, Timer = tp });
            tp.Start();

            FireStateChanged(ts, false);
        }

        /// <summary>
        /// Returns the button state string for a given timer style.
        /// </summary>
        private string GetStyleButtonState(string style)
        {
            switch (style)
            {
                case "Ping": return Timers.btnPing;
                case "Buff": return Timers.btnBuff;
                case "Pet": return Timers.btnPet;
                default: return Timers.btnStop;
            }
        }
    }
}
