# Character-Scope Timer Pausing (v0.6.0 Simplified Architecture)

> **Last Updated:** 2025-01-XX  
> **Version:** v0.6.0  
> **Status:** ✅ Current implementation (simple, stable, working)  

---

## Overview

Character-scope timer pausing ensures timers only run when their character is **actively logging** (log file growing). When viewing a different character in the UI (browsing mode), that character's timers display their frozen saved state without counting down.

---

## Problem Statement

When manually switching to view a different character in the UI (e.g., Character A is actively playing, user switches dropdown to view Character B's timers), we need to:

1. **Keep Character A's timers running** in the background (actively logging)
2. **Show Character B's timers frozen** at their saved state (not logging)
3. **Auto-detect if last active character is still online** on app startup

---

## Current Architecture (Simple & Working)

### Core Principle: Single-Character Timer Execution

**Only ONE character's Character-scope timers run at a time** — the character whose log file is actively growing (detected by `LogMonitor`). Other characters' timers are saved to the database and displayed frozen when viewing.

### Key Components

#### 1. LogMonitor: Source of Truth for "Actively Logging"

**File:** `ThorneTimer/LogMonitor.cs`

```csharp
// Tracks which character's log file is actively growing
private readonly List<CharacterFileState> fileStates;

public long GetActiveCharacterID()
{
    // Returns character ID where IsActive=true (file growth detected)
    // Returns 0 if no character actively logging
    var active = fileStates.FirstOrDefault(f => f.IsActive);
    return active?.CharacterID ?? 0;
}

public long GetSelectedCharacterID()
{
    // Returns character ID currently selected in UI dropdown
    return selectedCharacterID;
}
```

**IsActive Flag:**
- Set to `true` ONLY by file growth detection in `PollLoop()`
- Never set by UI events
- Cleared when character camps out or app switches characters

#### 2. TimerRuntime: Respects `isActive` Flag

**File:** `ThorneTimer/TimerRuntime.cs`

```csharp
public void RestoreCharacterState(Dictionary<long, TimerState> savedStates, bool isActive = true)
{
    foreach (var ts in timerStates)
    {
        var saved = savedStates.TryGetValue(ts.TimerID, out var s) ? s : null;
        
        if (saved != null && saved.IsRunning)
        {
            // Character-scope timers only run when character is actively logging
            if (ts.Scope == "Character" && !isActive)
            {
                // Not actively logging → keep frozen at saved state
                ts.Remaining = saved.Remaining;
                ts.ButtonState = saved.ButtonState;
                continue;  // Skip starting timer
            }
            
            // Character+ and World always run (or adjust offline time)
            // ... restart timer logic
        }
    }
}
```

**Key Method: `SaveCharacterState()`**
```csharp
public Dictionary<long, TimerState> SaveCharacterState()
{
    // Stops all Character/Character+ timers
    // Freezes their state (Remaining, Count, ButtonState)
    // Returns dictionary for database persistence
}
```

#### 3. FormMain: Coordinates Character Switches

**File:** `ThorneTimer/FormMain.cs`

**Determine if Character is Actively Logging:**
```csharp
private Dictionary<long, TimerState> LoadTimerRuntime()
{
    long currentCharID = 0;
    long.TryParse(activeCharacterID, out currentCharID);
    
    // Is this character the one actively logging?
    bool isActive = logMonitor.IsRunning && 
                    logMonitor.GetActiveCharacterID() == currentCharID;
    
    timerRuntime.RestoreCharacterState(savedStates, isActive);
    // If isActive=false, Character-scope timers stay frozen
}
```

**Manual Character Switch:**
```csharp
private void tscActiveCharacter_SelectedIndexChanged(object sender, EventArgs e)
{
    // 1. Save outgoing character's state
    var outgoingStates = timerRuntime.SaveCharacterState();
    Database.SaveTimerStates(con, outgoingStates, activeCharacterID);
    
    // 2. Change displayed character
    activeCharacterID = newCharacterID;
    
    // 3. Load new character with correct isActive flag
    LoadTimerRuntime();  // Calls RestoreCharacterState(isActive)
    
    // 4. Show browsing indicator if viewing != logging
    long loggingCharID = logMonitor.GetActiveCharacterID();
    if (loggingCharID > 0 && loggingCharID != newCharID)
    {
        lblBrowsingIndicator.Visible = true;
        lblBrowsingIndicator.Text = "⚠ Browsing Mode — ...";
    }
}
```

---

## Scenarios

### Scenario 1: App Startup (Character Offline)

```
1. FormMain_Load() checks if last active character's log modified within 5 minutes
2. If NOT recently modified:
   ✅ Set activeCharacterID = "0" (None)
   ✅ Update dropdown to "(None)"
   ✅ Status: "Watching: (no active character)"
3. If recently modified:
   ✅ Keep last active character
   ✅ Start watching normally
```

**Implementation:**
```csharp
if (!IsCharacterLogActive(lastActiveCharID, thresholdMinutes: 5))
{
    activeCharacterID = "0";
    Database.SetSetting(con, "ActiveCharacterID", activeCharacterID);
    // Update dropdown to "(None)"
}
```

### Scenario 2: Manual Switch (Browsing Mode)

```
User playing Character A, switches dropdown to view Character B

1. Save Character A's timer state to database
2. Change activeCharacterID = B
3. LoadTimerRuntime():
   - currentCharID = B
   - loggingCharID = A (still logging!)
   - isActive = false (B not logging)
   - RestoreCharacterState(savedStates, isActive=FALSE)
   - B's Character-scope timers display frozen
4. Show browsing indicator
5. A's timers continue running (LogMonitor still detecting A's file growth)
```

### Scenario 3: Auto-Switch

```
User logs out of Character A, logs into Character B

1. LogMonitor detects B's log file growing
2. Fires CharacterSwitched event (A → B)
3. FormMain.OnCharacterSwitched():
   - Save A's state (stops A's timers)
   - Change activeCharacterID = B
   - LoadTimerRuntime():
     - currentCharID = B
     - loggingCharID = B (file growth)
     - isActive = TRUE
     - B's timers start running
4. No browsing indicator (viewing == logging)
```

### Scenario 4: Camp-Out

```
User camps out of Character A

1. LogMonitor detects camp text + 10-second inactivity
2. Fires CharacterCampedOut event
3. FormMain.OnCharacterCampedOut():
   - Save A's state
   - Set activeCharacterID = "0" (None)
   - Update dropdown to "(None)"
   - LoadTimerRuntime() with no active character
   - All Character-scope timers stop
   - World/Character+ continue
4. Status: "Watching: (no active character)"
```

---

## Timer Scope Behavior

| Scope | Behavior |
|-------|----------|
| **World** | Always running, shared across all characters |
| **Character+** | Always running, per-character, adjusts for offline time (server cooldowns) |
| **Character** | ✅ **Only runs when character is actively logging** (`isActive=true`) <br/> ✋ **Frozen when viewing inactive character** (`isActive=false`) |

---

## Database Persistence

**Table:** `timer_runtime_state`

**Columns:**
- `CharacterID` — Which character this state belongs to
- `TimerID` — Which timer
- `Remaining` — Time remaining (e.g., "00:54:22")
- `ButtonState` — Button text ("Start", "Stop", "Buff", "Ping")
- `Count` — Execution count
- `LastSaved` — Timestamp

**When Persisted:**
- On character switch (manual or auto)
- On camp-out
- On app exit
- On timer state transitions (start, stop, expire)

**Key Methods:**
```csharp
// ThorneTimer/Database.cs
public static void SaveTimerStates(
    SQLiteConnection con,
    Dictionary<long, TimerState> states,
    string characterID)
{
    // Deletes old states for character
    // Inserts current running states
    // Used for persistence across sessions
}

public static Dictionary<long, TimerState> LoadTimerStates(
    SQLiteConnection con,
    string characterID)
{
    // Loads saved states for character from timer_runtime_state table
    // Returns dictionary of TimerID → TimerState
}
```

---

## Auto-Detect Inactive Character on Startup

**Problem:** App should not automatically resume watching a character that logged out before the app was stopped.

**Solution:** Check log file's last modification time on startup.

**Implementation:**
```csharp
private bool IsCharacterLogActive(long characterID, int thresholdMinutes = 5)
{
    var character = Database.GetCharacter(con, characterID.ToString());
    if (string.IsNullOrEmpty(character.LogFile) || !File.Exists(character.LogFile))
        return false;

    DateTime lastWrite = File.GetLastWriteTimeUtc(character.LogFile);
    double minutesSinceWrite = (DateTime.UtcNow - lastWrite).TotalMinutes;
    return minutesSinceWrite <= thresholdMinutes;
}
```

**Startup Check (FormMain_Load):**
```csharp
if (Properties.Settings.Default.ParseLog)
{
    StartLog();

    long lastActiveCharID = 0;
    long.TryParse(activeCharacterID, out lastActiveCharID);
    
    // Auto-detect if character is still online
    if (lastActiveCharID > 0 && !IsCharacterLogActive(lastActiveCharID, thresholdMinutes: 5))
    {
        activeCharacterID = "0";
        Database.SetSetting(con, "ActiveCharacterID", activeCharacterID);
        // Update dropdown to "(None)"
        logMonitor.SetActiveCharacter(0);
        statusParsing.Text = "Watching: (no active character)";
    }
}
```

---

## Architecture Notes

### What Was Reverted (NOT Current)

**Previous Attempt (REMOVED in v0.6.0 revert):**
- `CharacterID` field on every `TimerPlus` instance
- `CaptureRunningTimerSnapshot()` / `RestoreRunningTimerSnapshot()` methods
- Complex background preservation of multiple characters' timers
- **Result:** Timer bleed bugs, NullReferenceExceptions, cross-character corruption
- **Status:** ❌ **FULLY REVERTED** — do not attempt this approach again

**Current Architecture (v0.6.0):**
- Single-character timer execution (only actively logging character runs)
- `isActive` flag controls whether Character-scope timers run
- `LogMonitor.GetActiveCharacterID()` is source of truth
- Simple `SaveCharacterState()` → switch → `RestoreCharacterState(isActive)` pattern
- **Result:** ✅ Clean, stable, no timer bleed

### Why This Works

1. **Single character focus** — Only one character's timers tick at a time
2. **LogMonitor authority** — File growth detection is authoritative for "actively logging"
3. **Simple persistence** — Save on exit, restore on entry, no complex tracking
4. **Frozen display** — Inactive characters show last saved state without running

### Key Principle

> **The UI can display any character, but only the actively logging character's Character-scope timers actually run.**

This separation enables:
- ✅ Browsing mode (view one character while another plays)
- ✅ Clean character switching
- ✅ No timer bleed
- ✅ Predictable, testable behavior

---

## Known Deferred Issue

### Edge Case: Character-Scope Timer Persistence During Browsing

**Symptom:** During browsing mode (viewing Character B while Character A logs), Character B's frozen timers may still have `TimerPlus` objects ticking in the background.

**Impact:** Minimal — timers display correctly frozen, persistence works, no crashes

**User Decision:** "It's fine, but probably something to dig into as we will continue to improve"

**Future Fix:** Phase C (v0.7.0) — Gameplay vs. Edit Mode separation will naturally eliminate this edge case by removing manual character browsing from the main form.

---

## Related Documentation

- [auto-character-switching.md](auto-character-switching.md) — LogMonitor design and auto-switch logic
- [camp-out-auto-pause.md](camp-out-auto-pause.md) — Camp-out detection and "(None)" character state
- [SESSION-HANDOFF-v0.6.0-logmonitor-fix.md](../../SESSION-HANDOFF-v0.6.0-logmonitor-fix.md) — Complete session context and LogMonitor selected vs. logging fix

---

**Version:** v0.6.0  
**Build Status:** ✅ Compiles successfully  
**Application Status:** ✅ Runs correctly  
**Known Issues:** 1 deferred edge case (accepted by user)
