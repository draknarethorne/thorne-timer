# Camp-Out Auto-Pause Feature (v0.6.0)

## Overview

The camp-out auto-pause feature automatically stops Character-scope timers when a character camps out or is manually set to "(None)". World-scope and Character+ timers continue running since they represent server-side state.

---

## Problem Statement

When a character camps out (`/camp`), their timers should stop running since the character is no longer in-game. Without auto-pause, Character-scope timers continue counting down even though the character is logged out.

---

## Solution

### Camp-Out Detection Pattern

```
1. Detect "It will take about 5 more seconds to prepare your camp." in log
2. Start inactivity timer (default 10 seconds, configurable via CampInactivityThresholdSeconds)
3. If "You abandon your preparations to camp." appears → cancel camp-out
4. If inactivity threshold reached with no new log activity → fire CharacterCampedOut event
```

### Inactivity Fallback (ungraceful exit)

The camp pattern only covers a *graceful* `/camp`. If the client crashes, is
closed with Alt-F4, or goes link-dead / disconnects, **no camp warning is ever
written** - the log simply stops growing. A separate fallback handles this:

```
1. Track LastActivityUtc on the actively logging character (updated on every read)
2. If no new log bytes arrive for InactivityTimeoutSeconds (default 300s / 5 min)
   AND the character is not already camping → fire the same CharacterCampedOut
3. Set InactivityTimeoutSeconds = 0 to disable the fallback
```

The window is deliberately long (5 minutes) because real play almost always
emits *some* log noise, so it only elapses when the client is genuinely gone.
It reuses the same `CharacterCampedOut` auto-pause path as camp-out, so the
outcome is identical: the active character drops to `(None)` and Character-scope
timers stop while World / Character+ timers keep running. The fallback is
suppressed while a camp is in progress so the two paths never compete.

Configured from the `InactivityTimeoutSeconds` setting (seconds; `0` disables);
falls back to the LogMonitor default when unset.

### Configuration (ThorneTimer.ini)

These thresholds are tunable without recompiling or editing the tome. They live in
the `[Monitoring]` section of `ThorneTimer.ini`, which sits next to
`ThorneTimer.exe` (the build copies it to the output folder; resolved via
`ThorneArchive.GetIniPath()`). Times are in **seconds**; set a timeout to `0` to
disable just that trigger.

```ini
[Monitoring]
; Quiet period after a "camp" warning before the camp-out is committed.
CampInactivityThresholdSeconds=10

; Silent-log fallback for crashes / Alt-F4 / link-dead (no camp warning written).
InactivityTimeoutSeconds=300

; Min bytes a non-active log must grow before an auto character switch (>= 1).
SwitchThresholdBytes=10
```

Precedence: built-in defaults (10 / 300 / 10) -> the database
`InactivityTimeoutSeconds` setting (if present) -> `ThorneTimer.ini` `[Monitoring]`
keys, which win. Loading is done by `LogMonitor.LoadIniSettings()`, called from
`FormMain` after the database-restored values are applied, so absent INI keys leave
those values untouched. For quick testing, lower the timeouts (e.g. `3` and `15`)
so the auto-pause fires without waiting the full production windows.

The user-facing reference for all of these (plus `[Logging]` and `[Backups]`) is
[`Docs/configuration.md`](../../Docs/configuration.md).

### Character State Model

| Active Character | Character Timers | World Timers | Character+ Timers |
|-----------------|------------------|--------------|-------------------|
| Valid ID        | ✅ Run           | ✅ Run       | ✅ Run            |
| `0` (None)      | ❌ Stopped       | ✅ Run       | ✅ Run            |

### Manual Pause

Users can manually set active character to `(None)` in the dropdown to stop all Character-scope timers without camping. This is useful for:

- Taking a break without logging out
- Monitoring World timers only
- Testing timer configurations

---

## Implementation Details

### LogMonitor Changes

**New CharacterFileState Fields:**
```csharp
public class CharacterFileState
{
    public DateTime LastActivityUtc { get; set; }
    public bool CampingOut { get; set; }
    public DateTime CampStartUtc { get; set; }
}
```

**Configurable Timeout:**
```csharp
public int CampInactivityThresholdSeconds { get; set; } = 10;
```

**New Event:**
```csharp
public event EventHandler<CharacterSwitchedEventArgs> CharacterCampedOut;
```

**Pattern Detection in ReadNewContent:**
```csharp
// Update activity timestamp
state.LastActivityUtc = DateTime.UtcNow;

// Camp-out pattern detection
if (text.Contains("It will take about 5 more seconds to prepare your camp."))
{
    state.CampingOut = true;
    state.CampStartUtc = DateTime.UtcNow;
}
else if (text.Contains("You abandon your preparations to camp."))
{
    state.CampingOut = false;
    state.CampStartUtc = DateTime.MinValue;
}
```

**Timeout Check in PollLoop:**
```csharp
// Check for camp-out timeout on active character
CheckCampOutTimeout(snapshot);
```

**CheckCampOutTimeout Method:**
```csharp
private void CheckCampOutTimeout(List<CharacterFileState> snapshot)
{
    var activeState = snapshot.FirstOrDefault(s => s.IsActive);
    if (activeState == null) return;
    if (!activeState.CampingOut) return;

    double secondsSinceCampStart = (DateTime.UtcNow - activeState.CampStartUtc).TotalSeconds;
    if (secondsSinceCampStart >= CampInactivityThresholdSeconds)
    {
        activeState.CampingOut = false;
        activeState.CampStartUtc = DateTime.MinValue;

        CharacterCampedOut?.Invoke(this, new CharacterSwitchedEventArgs
        {
            OldCharacterID = activeState.CharacterID,
            NewCharacterID = 0,
            NewCharacterName = ""
        });
    }
}
```

### FormMain Changes

**Wire Event:**
```csharp
logMonitor.CharacterCampedOut += OnCharacterCampedOut;
```

**Event Handler:**
```csharp
private void OnCharacterCampedOut(object sender, CharacterSwitchedEventArgs e)
{
    ThorneLog.Separator("CHARACTER CAMP-OUT (auto)");
    ThorneLog.Info($"Camp-out detected for charID={e.OldCharacterID}");

    // Save outgoing character's timer state
    var outgoingStates = timerRuntime.SaveCharacterState();
    Database.SaveTimerStates(con, outgoingStates, activeCharacterID);

    // Set active character to "None" (0)
    activeCharacterID = "0";
    Database.SetSetting(con, "ActiveCharacterID", activeCharacterID);
    
    // Update dropdown to "(None)"
    tscActiveCharacter.SelectedIndexChanged -= tscActiveCharacter_SelectedIndexChanged;
    foreach (ComboBoxItem item in (List<ComboBoxItem>)tscActiveCharacter.ComboBox.DataSource)
    {
        if (Convert.ToInt64(item.Value) == 0)
        {
            tscActiveCharacter.SelectedItem = item;
            break;
        }
    }
    tscActiveCharacter.SelectedIndexChanged += tscActiveCharacter_SelectedIndexChanged;

    // Tell LogMonitor there's no active character
    logMonitor.SetActiveCharacter(0);

    // Reload timers — stops all Character-scope timers
    LoadTimerRuntime();

    statusParsing.Text = "Watching: (no active character)";
    UpdateMiniView();
}
```

**Character Dropdown Includes "(None)":**
```csharp
private void SetupActiveCharacters()
{
    var characters = Database.GetActiveCharacters(con);
    
    // Add "None" option at the beginning
    characters.Insert(0, new ComboBoxItem { Value = 0, Text = "(None)" });
    
    tscActiveCharacter.ComboBox.DataSource = characters;
    // ... restore selection
}
```

---

## Auto-Switch Suppression Bug Fixes (v0.6.0)

### Bug 1: Suppressing Wrong Character

**Problem:** When manually switching from Character A to Character B, the code suppressed Character B (the NEW character) instead of Character A (the OLD character). This meant Character B's log activity would be ignored, but Character A's log activity would trigger an auto-switch back to A.

**Original Code (WRONG):**
```csharp
// Tell LogMonitor which character is now active
long newCharID = 0;
long.TryParse(activeCharacterID, out newCharID);
logMonitor.SetActiveCharacter(newCharID);

// ❌ BUG: Suppresses NEW character
if (logMonitor.AutoSwitchEnabled)
{
    autoSwitchSuppressed = true;
    logMonitor.SuppressedAutoSwitchCharacterID = newCharID;  // WRONG!
}
```

**Fixed Code (CORRECT):**
```csharp
// Capture OLD character ID before changing activeCharacterID
long oldCharID = 0;
long.TryParse(activeCharacterID, out oldCharID);

// Save outgoing character's timer state
var outgoingStates = timerRuntime.SaveCharacterState();
Database.SaveTimerStates(con, outgoingStates, activeCharacterID);

// Update activeCharacterID
activeCharacterID = (tscActiveCharacter.SelectedItem as ComboBoxItem).Value.ToString();

// Tell LogMonitor which character is now active
long newCharID = 0;
long.TryParse(activeCharacterID, out newCharID);
logMonitor.SetActiveCharacter(newCharID);

// ✅ FIX: Suppress OLD character (not NEW)
if (logMonitor.AutoSwitchEnabled && oldCharID > 0)
{
    autoSwitchSuppressed = true;
    logMonitor.SuppressedAutoSwitchCharacterID = oldCharID;  // CORRECT!
}
```

### Bug 2: Clearing Suppression on Wrong Character Activity

**Problem:** When suppression was active, ANY log activity would clear it — even activity from the suppressed (OLD) character. This meant if you manually switched from A to B, and A's log kept generating activity (combat scrolling, buff expiring), the suppression would clear immediately and auto-switch would yank you back to A.

**Original Code (WRONG):**
```csharp
private void OnLogChunkReceived(object sender, LogChunkReceivedEventArgs e)
{
    // ❌ BUG: Clears suppression on ANY activity
    if (autoSwitchSuppressed)
    {
        autoSwitchSuppressed = false;
        logMonitor.SuppressedAutoSwitchCharacterID = 0;
        // ... update status bar
    }
    
    timerRuntime.ProcessLogText(e.Text);
}
```

**Fixed Code (CORRECT):**
```csharp
private void OnLogChunkReceived(object sender, LogChunkReceivedEventArgs e)
{
    // ✅ FIX: Only clear suppression if activity is from NEW (active) character
    if (autoSwitchSuppressed)
    {
        long currentCharID = 0;
        long.TryParse(activeCharacterID, out currentCharID);
        
        // Only clear if active character is NOT the suppressed one
        if (currentCharID > 0 && currentCharID != logMonitor.SuppressedAutoSwitchCharacterID)
        {
            autoSwitchSuppressed = false;
            logMonitor.SuppressedAutoSwitchCharacterID = 0;
            this.BeginInvoke(new Action(() =>
            {
                if (tsbStartStopWatching.Text == stopWatchingText && logMonitor.FilePath != null)
                    statusParsing.Text = "Watching: " + Path.GetFileName(logMonitor.FilePath);
            }));
        }
    }
    
    timerRuntime.ProcessLogText(e.Text);
}
```

---

## Status Bar Updates

The status bar now reflects the current character state:

- **Active Character:** `"Watching: eqlog_CharName_server.txt"`
- **Auto-Switch Paused:** `"Watching: eqlog_CharName_server.txt (auto-switch paused)"`
- **No Active Character:** `"Watching: (no active character)"`
- **Auto-Switch Triggered:** `"Watching: eqlog_CharName_server.txt (auto)"`

---

## Testing Scenarios

### Camp-Out Detection
1. Start timer on active character
2. Type `/camp` in EQ
3. Wait for camp warning to appear in log
4. Do NOT type `/camp` again (no "abandon" message)
5. After 10 seconds of log inactivity, character should auto-pause
6. Dropdown should show "(None)"
7. Status bar should show "Watching: (no active character)"
8. Character timer should stop
9. World/Character+ timers should continue

### Manual Pause
1. Start timer on active character
2. Select "(None)" from character dropdown
3. Character timer should stop immediately
4. World/Character+ timers should continue

### Auto-Switch After Manual Switch
1. Character A is active and logging activity
2. Manually switch to Character B
3. Character A's log continues generating activity (combat, buffs)
4. Auto-switch should NOT switch back to A (suppression working)
5. Character B generates any log activity
6. Auto-switch suppression should clear
7. If Character C logs in, auto-switch should work normally

---

## Bug Fixes

### v0.7.0 - Camp-out not always firing

**Symptom:** Camp-out auto-switch to "(None)" was intermittent - sometimes a
`/camp` would not pause Character-scope timers even after the inactivity
threshold elapsed.

Two independent root causes were found and fixed in `LogMonitor.cs`:

1. **Chunk-boundary split (primary, intermittent).** The log is read in
   1024-byte ASCII chunks and the 55-character camp warning was matched with a
   per-chunk `text.Contains(...)`. When the warning line straddled a 1024-byte
   read boundary, `Contains` returned false on both halves, so `CampingOut` was
   never set and the timeout never started. Whether it triggered depended on byte
   alignment, hence the "not always" behavior. Fixed by routing all camp-pattern
   detection through `ScanForCampPatterns`, which prepends a retained trailing
   fragment (`CharacterFileState.CampScanTail`, one char shorter than the longest
   pattern) so a message spanning two reads is still matched. Verified with a
   standalone repro where the warning lands at bytes 1007..1062: old per-chunk
   `Contains` missed it; the tail-carry path matched it.

2. **Active-vs-selected mismatch (browsing mode).** Camp detection previously ran
   only inside `ReadNewContent`, which is called only for the *selected* (viewed)
   character. The timeout, however, checks the *actively logging* character
   (`IsActive`). When the user browsed a different character than the one logging,
   the active file's new bytes were only size-tracked, never scanned, so its
   camp-out was never detected. Fixed by adding `ScanActiveFileForCamp`: when the
   active character differs from the selected one, its new bytes are read for
   pattern scanning only (no `LogChunkReceived`, so timers still follow the
   selected character).

Diagnostic `Info` logging was added for camp warning / abandon / confirm
transitions to make future issues visible in the log.

### v0.7.0 - Inactivity fallback for ungraceful exits

**Gap:** Auto-pause only fired on a graceful `/camp` (the camp warning line). If
the client crashed, was closed with Alt-F4, or went link-dead / disconnected, no
camp warning was ever logged and the active character stayed "active" with its
Character-scope timers running indefinitely. The pre-existing `LastActivityUtc`
field was maintained on every read but never actually consumed.

**Fix:** `CheckCampOutTimeout` now has a second path. When the active character
is not camping and its log has produced no new bytes for
`InactivityTimeoutSeconds` (default 300s; `0` disables), it fires the same
`CharacterCampedOut` auto-pause via the shared `FireAutoPause` helper. Because
`IsActive` is only ever set by file growth - which is immediately followed by a
read that refreshes `LastActivityUtc` - a freshly-active character cannot
false-fire. The camp path still takes precedence (the fallback is skipped while
`CampingOut` is true), so the two never compete.

---

## Future Enhancements

### Additional Patterns
- **Disconnect / Linkdead Detection:** now covered generically by the inactivity
  fallback (see "Inactivity Fallback" above) - a silent log triggers auto-pause
  regardless of the specific cause. Explicit `"You have been disconnected."` /
  `"LOADING, PLEASE WAIT..."` matching could still be added for a *faster* pause
  than the inactivity window, if desired.
- **Zoning Detection:** Detect long zone times and pause temporarily

### UI Improvements
- **Settings Dialog:** In-app controls for `CampInactivityThresholdSeconds` and
  `InactivityTimeoutSeconds`. (Both are already editable today via the
  `[Monitoring]` section of `ThorneTimer.ini` - see "Configuration" above; a UI
  would just make tuning more discoverable.)
- **Visual Indicators:** Show camp-out countdown in status bar
- **Resume Notification:** Toast when character auto-resumes from "(None)"

### Smart Resume
- **Auto-Resume on Login:** Detect character login and automatically switch from "(None)" to the logging-in character
- **Last Active Memory:** Remember which character was active before camp-out and offer to resume

---

**Last Updated:** v0.7.0 (camp-out detection fixes + inactivity fallback + [Monitoring] ini tuning)  
**Status:** ✅ Implemented  
**Version:** v0.6.0 (feature) / v0.7.0 (boundary + browsing-mode fixes, inactivity fallback, ini config)
