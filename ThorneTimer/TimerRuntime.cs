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
                        ActiveYn = gd.ActiveYn,
                        CaseYn = gd.CaseYn,
                        EndlessYn = gd.EndlessYn,
                        Style = gd.Style ?? "Normal",
                        Scope = gd.Scope ?? "World",
                        DependsOnTimer = gd.DependsOnTimer ?? "",
                        DependsOnDelay = gd.DependsOnDelay,
                        ClassID = gd.ClassID
                    };

                    // Restore runtime state if this timer was already tracked
                    if (previousStates.TryGetValue(gd.ID, out TimerState prev))
                    {
                        ts.ButtonState = prev.ButtonState;
                        ts.Count = prev.Count;
                        ts.Remaining = prev.Remaining;

                        // If it was running and still exists, keep it running
                        // The RunningTimer entry in runningTimers list still references the old state,
                        // but the TimerPlus object is still ticking. We'll leave it alone.
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
                            FireStateChanged(ts, false);
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
                    FireStateChanged(ts, false);
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

            FireStateChanged(ts, false);
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
                    }
                    else
                    {
                        rt.Timer.Stop();
                        rt.Timer.TimerElapsed -= OnTimerElapsed;
                        rt.Timer.TimerExpired -= OnTimerExpired;
                        rt.Timer.Dispose();
                        runningTimers.RemoveAt(i);
                    }
                    break;
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

                FireStateChanged(ts, true);
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
                            FireStateChanged(ts, false);
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
            if (durationText.Length != 8) return false;
            if (durationText.Substring(2, 1) != ":" || durationText.Substring(5, 1) != ":") return false;

            return int.TryParse(durationText.Substring(0, 2), out _)
                && int.TryParse(durationText.Substring(3, 2), out _)
                && int.TryParse(durationText.Substring(6, 2), out _);
        }

        // --- Event firing helpers ---

        private void FireStateChanged(TimerState ts, bool expired)
        {
            TimerStateChanged?.Invoke(this, new TimerStateChangedEventArgs
            {
                TimerID = ts.TimerID,
                Remaining = ts.Remaining,
                ButtonState = ts.ButtonState,
                Count = ts.Count,
                TheType = GetTimerType(ts.Style),
                Expired = expired
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
        /// Saves the outgoing character's timer state. Character-scope running
        /// timers are stopped (their remaining time is frozen in TimerState).
        /// World-scope timers keep running untouched.
        /// Returns a snapshot list suitable for Database.SaveTimerStates.
        /// </summary>
        public List<TimerState> SaveCharacterState()
        {
            lock (syncLock)
            {
                // Stop Character-scope running timers (freeze their remaining time)
                foreach (var ts in timerStates)
                {
                    if (ts.Scope == "Character" && ts.IsRunning)
                    {
                        // Capture current remaining time from the TimerPlus
                        var rt = runningTimers.FirstOrDefault(r => r.TimerID == ts.TimerID);
                        if (rt != null)
                        {
                            ts.Remaining = rt.Timer.GetTimeRemaining();
                        }
                        StopTimerInternal(ts, false);
                        // Mark with the style button state so we know it was running
                        ts.ButtonState = GetStyleButtonState(ts.Style);
                    }
                }

                return timerStates.ToList();
            }
        }

        /// <summary>
        /// Restores an incoming character's timer state from previously saved data.
        /// Restores per-character ActiveYn preferences for all timers.
        /// Character-scope timers that were running are restarted with their saved
        /// remaining time. World-scope timers are left alone (still running).
        /// </summary>
        public void RestoreCharacterState(Dictionary<long, TimerState> savedStates)
        {
            lock (syncLock)
            {
                foreach (var ts in timerStates)
                {
                    if (!savedStates.TryGetValue(ts.TimerID, out TimerState saved))
                        continue;

                    // Always restore count and per-character ActiveYn preference
                    ts.Count = saved.Count;
                    ts.ActiveYn = saved.ActiveYn;

                    // Only restore running state for Character-scope timers
                    if (ts.Scope != "Character") continue;

                    // Was this timer running when the character was last active?
                    if (Timers.TimerRunning(saved.ButtonState) && !string.IsNullOrEmpty(saved.Remaining))
                    {
                        if (!ValidDuration(saved.Remaining)) continue;
                        if (TimerPlus.GetMilliseconds(saved.Remaining) <= 0) continue;

                        // Restart the timer with the saved remaining time
                        RestartTimerFromRemaining(ts, saved.Remaining, saved.ButtonState);
                    }
                }
            }
        }

        /// <summary>
        /// Restarts a timer with a specific remaining time and button state.
        /// Used when restoring Character-scope timers after a character switch.
        /// </summary>
        private void RestartTimerFromRemaining(TimerState ts, string remaining, string buttonState)
        {
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
