# Session Handoff: v0.6.0 LogMonitor Selected vs. Logging Fix

**Date:** 2025-01-XX  
**Branch:** `v0.6.0-gui-enhancements`  
**Build Status:** ✅ Compiles successfully  
**Application Status:** ✅ Runs without crashes  

---

## Executive Summary

This session completed critical fixes to `LogMonitor.cs` that separated **UI character selection** (what the user is viewing) from **actively logging character** (which log file is actually growing). This distinction is essential for proper timer behavior and browsing mode.

### What Was Fixed

**Bug:** Manually selecting a character in the dropdown incorrectly treated them as "actively logging" without validating file activity. This caused Character-scope timers to start running for characters that weren't actually playing.

**Solution:** Added `selectedCharacterID` field to `LogMonitor` to track UI state separately from file activity. The `IsActive` flag is now set **exclusively** by file growth detection, never by UI events.

### Build & Test Status

- ✅ Build succeeds
- ✅ Application starts without crashes
- ✅ Auto-switch works correctly
- ✅ Manual character selection no longer incorrectly activates timers
- ✅ Browsing mode (viewing one character while another logs) works correctly
- ⏸️ **Known deferred issue:** Possible edge case with Character-scope timer persistence during browsing mode (user accepted deferral for future gameplay/edit mode separation)

---

## Critical Context: What Was Reverted

### The Complex Architecture (REMOVED)

A previous attempt implemented complex background character tracking with:
- `CharacterID` field on every `TimerPlus` instance
- `CaptureRunningTimerSnapshot()` / `RestoreRunningTimerSnapshot()` methods
- Complex background timer preservation during character switches
- Attempting to keep multiple characters' timers running simultaneously

**This was fully reverted** due to timer bleed bugs (timers persisting across character switches, wrong character IDs on timers, NullReferenceExceptions).

### The Simple Architecture (CURRENT v0.6.0)

Current working architecture is **simple and single-character focused**:

```
┌─────────────────────────────────────────────────────────┐
│ LogMonitor                                              │
│ ─────────────────────────────────────────────────────── │
│                                                         │
│  selectedCharacterID  ←─ UI dropdown selection         │
│       ↓                                                 │
│  FilePath = character's log file (for reading content)  │
│                                                         │
│  IsActive flag per file ←─ Set ONLY by file growth     │
│       ↓                                                 │
│  GetActiveCharacterID() returns char with IsActive=true │
│                                                         │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ TimerRuntime                                            │
│ ─────────────────────────────────────────────────────── │
│                                                         │
│  World timers: Always running                           │
│  Character+ timers: Always running (offline adjustment) │
│  Character timers: Run only when character IS ACTIVE    │
│                                                         │
│  SaveCharacterState() → stops timers, saves to DB       │
│  RestoreCharacterState(states, isActive) → restarts     │
│     if isActive=true                                    │
│                                                         │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ FormMain                                                │
│ ─────────────────────────────────────────────────────── │
│                                                         │
│  activeCharacterID = currently DISPLAYED character      │
│                                                         │
│  LoadTimerRuntime():                                    │
│    isActive = (logMonitor.IsRunning &&                  │
│                logMonitor.GetActiveCharacterID() == ID) │
│    RestoreCharacterState(savedStates, isActive)        │
│                                                         │
│  Manual Switch:                                         │
│    1. SaveCharacterState() for OLD character            │
│    2. Change activeCharacterID                          │
│    3. LoadTimerRuntime() for NEW character              │
│    4. Show browsing indicator if NEW != logging         │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Key Principle:** Only ONE character's Character-scope timers run at a time (the actively logging character). UI can display a different character's frozen timer state without affecting the active character.

---

## What Changed in This Session

### Files Modified

#### 1. `ThorneTimer/LogMonitor.cs` (6 replacements)

**Added:**
```csharp
private long selectedCharacterID;  // UI selection - controls which file to read
```

**Modified `GetActiveCharacterID()` docstring:**
```csharp
/// <summary>
/// Returns the character ID of the currently actively logging character
/// (the character whose log file is actively growing), or 0 if none.
/// This is NOT necessarily the same as the selected character in the UI.
/// Use GetSelectedCharacterID() for UI state.
/// </summary>
```

**Added new method:**
```csharp
/// <summary>
/// Returns the character ID currently selected in the UI dropdown.
/// This controls which character's timer data is displayed, but does
/// NOT indicate which character is actively logging.
/// </summary>
public long GetSelectedCharacterID()
{
    lock (stateLock)
    {
        return selectedCharacterID;
    }
}
```

**Modified `Start()` signature:**
```csharp
public void Start(List<CharacterFileState> characters, long selectedCharacterID)
{
    // All states initialized with IsActive=false
    // selectedCharacterID stored for file path lookup
}
```

**Modified `SetActiveCharacter()` to ONLY update UI state:**
```csharp
public void SetActiveCharacter(long characterID)
{
    lock (stateLock)
    {
        selectedCharacterID = characterID;  // UI state only
        
        if (fileStates != null)
        {
            var state = fileStates.FirstOrDefault(f => f.CharacterID == characterID);
            if (state != null)
            {
                FilePath = state.FilePath;  // Update read path
            }
        }
        
        // ❌ NO LONGER SETS IsActive FLAG
        // IsActive is set ONLY by file growth detection in PollLoop
    }
}
```

**Modified `PollLoop()` to read from selected character:**
```csharp
// Read content from the SELECTED character (enables browsing mode)
var selectedState = snapshot.FirstOrDefault(s => s.CharacterID == selectedCharacterID);
if (selectedState != null && new FileInfo(selectedState.FilePath).Length > selectedState.LastReadPosition)
{
    ReadNewContent(selectedState);  // Read from SELECTED character
}

// Auto-switch: detect file growth on OTHER characters
foreach (var state in snapshot)
{
    if (state.CharacterID != selectedCharacterID && ...)
    {
        // File growth detected → set IsActive, update selectedCharacterID
        state.IsActive = true;
        selectedCharacterID = state.CharacterID;  // UI follows logging
        // Fire CharacterSwitched event
    }
}
```

### No Changes Needed

- ✅ `ThorneTimer/FormMain.cs` - Already used `logMonitor.GetActiveCharacterID()` correctly
- ✅ `ThorneTimer/TimerRuntime.cs` - No changes needed, already worked correctly

---

## How It Works Now

### Scenario 1: Application Startup (Character Offline)

```
1. FormMain_Load() checks if last active character's log modified within 5 minutes
2. If NOT recently modified:
   - Set activeCharacterID = "0" (None)
   - Update dropdown to "(None)"
   - LogMonitor.SetActiveCharacter(0)
   - Status bar: "Watching: (no active character)"
3. If recently modified:
   - Keep last active character
   - Start watching normally
```

### Scenario 2: Manual Character Switch (Dropdown)

```
User playing Character A, switches dropdown to view Character B

1. FormMain saves Character A's state
   - SaveCharacterState() → DB
   
2. FormMain changes activeCharacterID = B
   - Database.SetSetting("ActiveCharacterID", B)
   
3. FormMain calls LogMonitor.SetActiveCharacter(B)
   - selectedCharacterID = B
   - FilePath = B's log file
   - ❌ Does NOT set IsActive flag
   
4. FormMain calls LoadTimerRuntime():
   - currentCharID = B
   - loggingCharID = LogMonitor.GetActiveCharacterID() = A (still logging!)
   - isActive = (loggingCharID == currentCharID) = false
   - RestoreCharacterState(savedStates, isActive=FALSE)
   - Character B's timers display frozen at saved state
   
5. Browsing indicator shows:
   "⚠ Browsing Mode — Character A is actively logging. 
   Character-scope timers for Character B are paused."
   
6. Character A's timers continue running in background
   - LogMonitor still detecting file growth on A
   - TimerPlus objects for A still ticking
   - State persisted on transitions
```

### Scenario 3: Auto-Switch (File Growth)

```
User playing Character A, logs in to Character B (EverQuest window)

1. LogMonitor PollLoop detects B's log file growing
   - Sets B's IsActive = true
   - Clears A's IsActive = false
   - Updates selectedCharacterID = B (UI follows)
   - Fires CharacterSwitched event
   
2. FormMain.OnCharacterSwitched():
   - Saves A's state (stops A's Character-scope timers)
   - Changes activeCharacterID = B
   - Calls LoadTimerRuntime():
     - loggingCharID = B (file growth)
     - currentCharID = B (just switched)
     - isActive = true
     - Restores B's timers with isActive=TRUE
     - B's Character-scope timers start running
   
3. Status bar: "Watching: eqlog_CharacterB.txt (auto)"
4. No browsing indicator (viewing == logging)
```

### Scenario 4: Camp-Out

```
User camps out of Character A

1. LogMonitor detects "prepare your camp" in log
2. Starts 10-second inactivity timer
3. If no new log activity for 10 seconds:
   - Clears A's IsActive flag
   - Fires CharacterCampedOut event
   
4. FormMain.OnCharacterCampedOut():
   - Saves A's state
   - Sets activeCharacterID = "0" (None)
   - Updates dropdown to "(None)"
   - LogMonitor.SetActiveCharacter(0)
   - LoadTimerRuntime() with no active character
   - All Character-scope timers stop
   - World/Character+ continue running
   
5. Status bar: "Watching: (no active character)"
```

---

## Architecture Principles

### 🔑 Key Principles (DO)

1. **LogMonitor is source of truth for "actively logging"**
   - `IsActive` flag set ONLY by file growth detection
   - Never set by UI events

2. **UI selection is separate from logging state**
   - `selectedCharacterID` = what user is viewing
   - `GetActiveCharacterID()` = which character is actually logging
   - These can differ (browsing mode)

3. **Single-character timer execution**
   - Only ONE character's Character-scope timers run at a time
   - The actively logging character
   - Other characters' timers frozen at saved state

4. **Persistence on transitions**
   - `SaveCharacterState()` / `RestoreCharacterState()` pattern
   - Database persistence via `SaveTimerStates()` / `LoadTimerStates()`
   - Happens on switches, exits, meaningful state changes

5. **World/Character+ always running**
   - World scope: global, shared across all characters
   - Character+ scope: per-character but continues offline

### ❌ Anti-Patterns (DON'T)

1. **Don't conflate UI state with logging state**
   - Never assume selected character is actively logging
   - Always query `LogMonitor.GetActiveCharacterID()` for truth

2. **Don't attempt background character tracking**
   - Was tried, was reverted due to bugs
   - Architecture is single-character focused by design

3. **Don't set IsActive from UI events**
   - Only file growth detection sets IsActive
   - `SetActiveCharacter()` is for UI state only

4. **Don't skip persistence**
   - Always save before switching
   - Always restore with correct isActive flag

---

## Known Issues & Deferred Work

### ⏸️ Deferred: Character-Scope Timer Persistence Edge Case

**Symptom:** During browsing mode (viewing Character B while Character A logs), Character B's frozen timers may still have `TimerPlus` objects ticking in the background.

**Why It Happens:**
- `RestoreCharacterState(states, isActive=false)` displays frozen state but doesn't explicitly STOP any pre-existing running timers for Character B
- If Character B had timers running from a previous session, those `TimerPlus` objects may still exist
- Late-arriving tick events from those timers could fire during browsing

**Current Impact:** Minimal - timers display correctly frozen, persistence works, no crashes

**User Decision:** "It's fine, but probably something to dig into as we will continue to improve"

**Future Fix:** When implementing **gameplay vs. edit mode separation** (Phase C - v0.7.0):
- Gameplay mode: No character dropdown, fully auto-sensed, read-only grid
- Edit mode: Stop watching first, character selector appears, full editing capability
- This architectural change will naturally eliminate the edge case

**For Now:** Accept this minor edge case. Do NOT attempt complex background tracking again.

---

## Future Vision: Gameplay vs. Edit Modes (Phase C)

User's long-term vision for separating concerns:

### Gameplay Mode (Main Form)
- **No character dropdown** - always shows actively logging character
- **Auto-switch enabled** - follows log file activity automatically
- **Read-only grid** - can't accidentally edit during play
- **Mini views active** - overlay timers visible
- **Clean, focused gameplay** - no UI clutter

### Edit Mode (Maintenance Dialog)
- **Stop watching first** - no active log parsing
- **Character selector** - choose any character to edit
- **Full CRUD** - add/edit/delete timers
- **Timers frozen** - display saved state, no countdown
- **No interference** - editing doesn't affect gameplay

**Why This Matters:**
- Eliminates need for browsing mode complexity
- Clear separation: playing vs. configuring
- v0.6.0 infrastructure (`isActive` flag, `GetActiveCharacterID()`) enables this architecture
- Phase C (v0.7.0) is next priority after current v0.6.0 testing completes

---

## Testing Checklist

### ✅ Completed Testing
- [x] Application starts without crashes
- [x] Correctly selects "(None)" when no characters logged recently (5-minute threshold)
- [x] Manual character selection does NOT incorrectly treat selected as actively logging
- [x] Auto-switch triggers correctly on file growth
- [x] Browsing indicator appears when selected ≠ logging
- [x] Camp-out detection works
- [x] Build succeeds

### 📋 Suggested Additional Testing
- [ ] Multi-character switching during gameplay (A→B→C)
- [ ] Manual switch while character camps out
- [ ] Auto-switch while viewing different character
- [ ] World timer behavior across character switches
- [ ] Character+ timer offline time adjustment
- [ ] Timer persistence across app restarts
- [ ] Mini view display during browsing mode
- [ ] Late-arriving tick events during browsing (edge case)

---

## Code Reference

### Key Methods

**LogMonitor.cs:**
```csharp
// Line ~28: Field
private long selectedCharacterID;

// Line ~110: Get actively logging character
public long GetActiveCharacterID()

// Line ~122: Get UI-selected character
public long GetSelectedCharacterID()

// Line ~135: Start monitoring with selected character
public void Start(List<CharacterFileState> characters, long selectedCharacterID)

// Line ~226: Update UI selection only
public void SetActiveCharacter(long characterID)

// Line ~269: Poll loop - reads from selected, detects growth on others
private void PollLoop(CancellationToken token)

// Line ~378: Camp-out timeout check
private void CheckCampOutTimeout(List<CharacterFileState> snapshot)
```

**FormMain.cs:**
```csharp
// Line 3024: LoadTimerRuntime - determines isActive correctly
bool isActive = logMonitor.IsRunning && logMonitor.GetActiveCharacterID() == currentCharID;

// Line 3457: Manual character switch handler
private void tscActiveCharacter_SelectedIndexChanged(object sender, EventArgs e)

// Line 3528: Browsing mode indicator logic
if (loggingCharID > 0 && loggingCharID != newCharID) { /* show warning */ }

// Line 3633: Auto-switch event handler
private void OnCharacterSwitched(object sender, CharacterSwitchedEventArgs e)

// Line 3708: Camp-out event handler
private void OnCharacterCampedOut(object sender, CharacterSwitchedEventArgs e)
```

**TimerRuntime.cs:**
```csharp
// No changes in this session - already correct

// Key methods for reference:
public void SaveCharacterState()
public void RestoreCharacterState(Dictionary<long, TimerState> savedStates, bool isActive = true)
public void RestoreWorldTimersOnStartup(Dictionary<long, TimerState> savedStates)
```

---

## Documentation Status

### ✅ Accurate Documentation
- `README.md` - Accurate for v0.6.0
- `Docs/README.md` - Documentation index is current
- `Docs/ROADMAP.md` - Reflects completed v0.6.0, Phase C priority
- `ThorneTimer/Docs/auto-character-switching.md` - Historical doc, accurate
- `ThorneTimer/Docs/camp-out-auto-pause.md` - Accurate for v0.6.0

### ⚠️ Needs Major Updates
- **`ThorneTimer/Docs/character-scope-timer-pausing.md`**
  - **CRITICAL:** Documents the REVERTED complex snapshot/restore architecture
  - Needs complete rewrite to document simple v0.6.0 approach
  - Remove all references to `CaptureRunningTimerSnapshot()` / `RestoreRunningTimerSnapshot()`
  - Remove references to background character timer preservation
  - Document simple SaveCharacterState → LoadTimerRuntime → RestoreCharacterState(isActive) pattern

- **`ThorneTimer/Docs/roadmap-phase-c-priority.md`**
  - References snapshot/restore as enabling Phase C
  - Needs update to note those methods were reverted
  - Should emphasize `isActive` flag and `GetActiveCharacterID()` as Phase C foundation instead

- **`ThorneTimer/Docs/architecture-redesign.md`**
  - Add note about complex character tracking attempt and revert
  - Document lessons learned: keep it simple, single-character focus

- **`ThorneTimer/Docs/active-views/` (entire directory)**
  - All files are future planning, not current implementation
  - Add clear "STATUS: FUTURE PLANNING" headers
  - Not blocking, but misleading if someone assumes it's current

---

## Next Steps

### Immediate (Before v0.6.0 Release)
1. ✅ **Testing** - Complete testing checklist above
2. ⚠️ **Documentation Cleanup** - Update/remove incorrect docs listed above
3. 📦 **Release Prep** - Follow `Docs/releases/PUBLISHING.md` process

### Phase C (v0.7.0) - Next Priority
1. **Timer Maintenance Dialog** - Separate dialog for add/edit/delete timers
2. **Read-only Main Form Grid** - Lock to actively logging character
3. **Always-Show-Active Mode** - Main form follows `GetActiveCharacterID()`
4. **Remove Character Dropdown** - Gameplay mode has no manual switching
5. **Leverage v0.6.0 Work** - `isActive` flag, `GetActiveCharacterID()`, persistence pattern all carry forward

### Future (v0.8.0+)
- Phase B: Ping refactor (directional speech, eliminate special cases)
- Phase E: Class profiles, zone awareness
- Phase F: Advanced automation

---

## Conversation History Notes

This session exceeded token budget (~124k tokens used) and needs to start fresh. The revert work was inherited from a previous session's 12-step plan that successfully removed the complex snapshot/restore architecture. This session focused exclusively on the LogMonitor selected vs. logging fix.

---

## How to Use This Document

**For the next agent:**

1. **Read Architecture Principles section** - Understand DO/DON'T patterns
2. **Review "How It Works Now" scenarios** - See the four main flows
3. **Check Code Reference section** - Know where the logic lives
4. **Note deferred work** - Don't try to fix the edge case yet
5. **Follow Future Vision** - Phase C is the path forward

**For testing:**
- Use Testing Checklist section
- Focus on character switch scenarios
- Verify browsing mode behavior

**For documentation:**
- Follow Documentation Status section
- Priority: Fix `character-scope-timer-pausing.md`
- Add "FUTURE PLANNING" markers to `active-views/`

**For Phase C work:**
- User wants gameplay/edit separation
- Remove character dropdown from main form
- Lock main form to `GetActiveCharacterID()`
- Create maintenance dialog with frozen timer display

---

**End of Session Handoff**

Build Status: ✅ Compiles successfully  
Application Status: ✅ Runs correctly  
Known Issues: 1 deferred edge case (accepted by user)  
Documentation: Needs cleanup (see Documentation Status section)  
Next Priority: Phase C (v0.7.0) - Gameplay/Edit Mode Separation
