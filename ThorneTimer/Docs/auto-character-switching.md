# Auto Character Switching — Feature Design

## 1. Problem Statement

Today, switching between EverQuest characters requires manually changing the active character in the Thorne Timer dropdown. When you log out of one character and into another, the log parser continues watching the old character's log file until you alt-tab to Thorne Timer and change the selection. This is easy to forget, especially when switching characters frequently.

**Current flow (manual):**

```
1. Playing as Thorne → Thorne Timer watching eqlog_Thorne_server.txt ✓
2. Log out of Thorne, log into Aelwynn
3. Aelwynn's log file (eqlog_Aelwynn_server.txt) starts growing
4. Thorne Timer is still watching Thorne's log file ✗
5. *** You have to alt-tab and manually switch ***
6. Change dropdown to Aelwynn → parser restarts on correct file ✓
```

**Desired flow (automatic):**

```
1. Playing as Thorne → Thorne Timer watching eqlog_Thorne_server.txt ✓
2. Log out of Thorne, log into Aelwynn
3. Thorne Timer detects Aelwynn's log file is now changing
4. Automatically switches active character to Aelwynn ✓
5. Parser now watching eqlog_Aelwynn_server.txt ✓
```

### Scope

- **In scope:** Auto-detect which *already-registered* character's log file is actively being written to, and switch parsing to that file
- **In scope:** Investigate timer state preservation/restoration when switching characters
- **Out of scope:** Auto-discovering new characters or log files — characters must be added to the database first (they need Class assignment and other configuration)
- **Out of scope:** Multi-boxing (simultaneously parsing multiple log files for multiple characters) — this is a separate, more complex feature

### Constraints

- Characters are already defined in the `characters` table with their `LogFile` paths
- Log files may live in different directories (different game installs/servers)
- Only files for registered characters should be monitored
- The feature should be optional (some users may prefer manual control)

---

## 2. Current Architecture

### How parsing works today

```csharp
// FormMain.cs — single-threaded polling loop
private void ParseLog()
{
    Characters.GridData character = Database.GetCharacter(con, activeCharacterID);
    string filePath = character.LogFile;

    // Seek to end of file (skip existing content)
    var lastReadLength = new FileInfo(filePath).Length;

    while (true)
    {
        var fileSize = new FileInfo(filePath).Length;
        if (fileSize > lastReadLength)
        {
            // Read new bytes, call ProcessLogText(chunk)
        }
        Thread.Sleep(100);  // poll every 100ms
    }
}
```

**Key characteristics:**

| Aspect | Current Behavior |
|--------|-----------------|
| Files watched | Exactly one (active character's LogFile) |
| Detection method | Polling loop: `FileInfo.Length` every 100ms |
| Thread model | One `System.Threading.Thread`, started/stopped manually |
| Thread shutdown | `Thread.Abort()` (abrupt, no graceful cleanup) |
| Character switch | Manual: dropdown change → `StopLog()` → `StartLog()` |
| Timer state on switch | Lost — running timers stop, no state preservation |
| File location | Single path from `characters.LogFile` column |

---

## 3. Detection Approaches

There are three viable approaches for detecting which log file is active. Each has trade-offs.

### Approach A: FileSystemWatcher (event-driven)

.NET's `FileSystemWatcher` raises events when files change. We would create one watcher per character's log file directory.

```
┌──────────────────────────────────────────────────────────┐
│                  FileSystemWatcher Pool                   │
│                                                          │
│  Watcher 1: C:\EQ\Logs\                                 │
│    Filter: eqlog_Thorne_server.txt                       │
│    Filter: eqlog_Aelwynn_server.txt                      │
│                                                          │
│  Watcher 2: D:\EQ-TLP\Logs\                             │
│    Filter: eqlog_Draknar_tlp.txt                         │
│                                                          │
│  On Change → identify which character → switch if needed │
└──────────────────────────────────────────────────────────┘
```

**How it works:**

1. On "Start Watching", load all characters from the database
2. Group characters by directory (multiple characters may share a Logs folder)
3. Create one `FileSystemWatcher` per unique directory
4. Subscribe to `Changed` events, filter by filename
5. When a non-active character's file changes → trigger auto-switch
6. The actual content reading still uses the existing polling approach (or a separate reader)

**Pros:**
- Event-driven — no wasted CPU checking files that aren't changing
- OS-level notifications — very responsive (sub-second detection)
- Natural grouping — one watcher per directory handles multiple characters in the same folder
- `FileSystemWatcher` is a well-established .NET class (available since .NET 1.1)

**Cons:**
- `FileSystemWatcher` has a known buffer overflow issue — if the OS generates too many notifications too fast, some events can be lost (mitigated by setting `InternalBufferSize`)
- Less reliable on network drives (not relevant for local EQ installs)
- Requires careful disposal when stopping/switching databases
- `NotifyFilter` and event deduplication add complexity (a single log write can fire multiple `Changed` events)

**Mitigation for buffer overflow:** Set `InternalBufferSize = 32768` (32KB) and use `NotifyFilters.LastWrite | NotifyFilters.Size`. For our use case (a handful of files changing at human speed), buffer overflow is extremely unlikely.

### Approach B: Multi-file polling (extend current approach)

Extend the existing polling loop to check *all* character log files, not just the active one.

```csharp
// Conceptual — poll all character files
while (true)
{
    foreach (var character in allCharacters)
    {
        var fileSize = new FileInfo(character.LogFile).Length;
        if (fileSize > character.LastReadLength)
        {
            if (character.ID != activeCharacterID)
            {
                // Different character's file is growing → switch!
                SwitchToCharacter(character);
            }
            // Read new content from this file
            ReadAndProcess(character);
        }
    }
    Thread.Sleep(100);
}
```

**Pros:**
- Simplest implementation — minimal change from current code
- No new APIs to learn
- No event deduplication issues
- Works identically on all file systems

**Cons:**
- Polls every file every 100ms, even if only one is changing (minor CPU waste, but negligible for <20 files)
- Detection latency is up to 100ms (perfectly fine for this use case)
- Slightly less elegant than event-driven, but more predictable

### Approach C: Hybrid (FileSystemWatcher for detection, polling for reading)

Use `FileSystemWatcher` only to detect *which* file is changing, then switch the existing polling reader to that file.

**Pros:**
- Clean separation: detection vs. reading
- Leverages existing, proven reading code
- FileSystemWatcher only needs to answer "did this file change?" — no content reading

**Cons:**
- Complexity of two mechanisms
- Not much benefit over Approach B given the small number of files

### Recommendation

**Approach B (multi-file polling)** is the best fit for Thorne Timer:

1. **It's the smallest change** from the current architecture — the polling loop already exists and works reliably
2. **It's the most predictable** — no event deduplication, no buffer overflow edge cases
3. **Performance is not a concern** — checking `FileInfo.Length` on 5-10 files every 100ms is trivial
4. **It aligns with `TimerRuntime`** — the planned model layer (from the architecture redesign) is the natural home for multi-file awareness

`FileSystemWatcher` is a good technology, but it solves a problem we don't have (thousands of files, or needing instant reaction time). For a handful of known log files polled at human-interaction speed, simple polling is better engineering.

---

## 4. Design — Multi-File Log Monitor

### 4.1 Core Concept: `LogMonitor` class

A new class that replaces the current `ParseLog()` polling loop. Instead of watching one file, it watches all registered character log files and detects which one is active.

```
┌──────────────────────────────────────────────────────────────┐
│                       LogMonitor                             │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐    │
│  │ List<CharacterFileState>                              │    │
│  │                                                      │    │
│  │  CharacterFileState:                                 │    │
│  │    CharacterID: 1                                    │    │
│  │    CharacterName: "Thorne"                           │    │
│  │    FilePath: "C:\EQ\Logs\eqlog_Thorne_server.txt"    │    │
│  │    LastFileSize: 1,234,567                           │    │
│  │    LastActivity: 2025-01-15 14:32:01                 │    │
│  │    IsActive: true  ←── currently being parsed        │    │
│  │                                                      │    │
│  │  CharacterFileState:                                 │    │
│  │    CharacterID: 2                                    │    │
│  │    CharacterName: "Aelwynn"                          │    │
│  │    FilePath: "C:\EQ\Logs\eqlog_Aelwynn_server.txt"   │    │
│  │    LastFileSize: 987,654                             │    │
│  │    LastActivity: 2025-01-15 13:10:45                 │    │
│  │    IsActive: false                                   │    │
│  └──────────────────────────────────────────────────────┘    │
│                                                              │
│  Poll Loop (100ms):                                          │
│    for each CharacterFileState:                              │
│      check FileInfo.Length vs LastFileSize                    │
│      if file grew:                                           │
│        if not active character → fire CharacterSwitched      │
│        read new bytes → fire LogChunkReceived                │
│                                                              │
│  Events:                                                     │
│    LogChunkReceived(characterID, text)                        │
│    CharacterSwitched(oldCharacterID, newCharacterID)          │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### 4.2 `CharacterFileState` — Per-file tracking

```csharp
class CharacterFileState
{
    public long CharacterID;
    public string CharacterName;
    public string FilePath;
    public long LastFileSize;        // bytes read so far
    public DateTime LastActivity;    // last time this file grew
    public bool IsActive;            // currently the "focused" character
}
```

### 4.3 Detection logic

The core question: **how do we decide when to switch?**

**Simple rule:** If a non-active character's log file grows, switch to that character immediately.

This works because:
- EQ only writes to one character's log file at a time (per game instance)
- When you log into a new character, the old file stops growing and the new one starts
- There's typically a gap (character select screen) where neither file changes — the switch happens on the first write to the new file

**Edge cases:**

| Scenario | Behavior |
|----------|----------|
| Log into Character B while A is active | B's file grows → auto-switch to B |
| Both files grow simultaneously (multi-boxing) | First detected change wins; user should use manual mode for multi-boxing |
| File grows due to external edit (not EQ) | False positive — unlikely in practice, but could add a cooldown or minimum-bytes threshold |
| Character's log file doesn't exist yet | Skip that file; start monitoring when it appears |
| Character's log file path is invalid | Skip with warning; don't crash the monitor |

**Debounce / confirmation:** To avoid false switches from transient file changes (OS flush, antivirus scan), we could require the file to grow by at least N bytes (e.g., 10 bytes — one short log line) before triggering a switch. EQ log lines are typically 40-200+ characters, so even a 10-byte threshold eliminates noise.

### 4.4 Integration with `TimerRuntime`

From the architecture redesign, `TimerRuntime` will be the central model layer. `LogMonitor` feeds into it:

```
┌──────────────┐     LogChunkReceived      ┌────────────────┐
│  LogMonitor  │ ────────────────────────▶  │  TimerRuntime  │
│              │                            │                │
│  polls all   │     CharacterSwitched      │  ProcessLog()  │
│  char files  │ ────────────────────────▶  │  SwitchChar()  │
└──────────────┘                            └────────────────┘
                                                    │
                                              events│
                                         ┌──────────┼──────────┐
                                         ▼          ▼          ▼
                                      Grid      MiniViews    Voice
```

**On `CharacterSwitched`:**

1. `TimerRuntime` saves the current character's timer state (see Section 5)
2. `FormMain` updates the character dropdown (UI reflects the switch)
3. `TimerRuntime` loads/restores the new character's timer state
4. Mini views refresh with the new character's running timers
5. Status bar updates: `"Watching: eqlog_Aelwynn_server.txt (auto)"`

### 4.5 Reading multiple files

Even though only one character is "active" at a time, `LogMonitor` reads new content from *whichever file is growing*. This means:

- If you switch from Thorne to Aelwynn, any final log lines written to Thorne's file (e.g., "You have been disconnected") are still processed
- The monitor doesn't need to seek backwards — it just reads forward from `LastFileSize` for each file independently
- Each file has its own read position that persists for the session

### 4.6 Settings

| Setting | Type | Default | Purpose |
|---------|------|---------|---------|
| Auto-Switch Enabled | bool | true | Master toggle — some users may prefer manual |
| Switch Threshold | int | 10 | Minimum bytes of growth before triggering switch (debounce) |

These live in the `settings` table (database), consistent with other app settings.

---

## 5. Timer State Preservation on Character Switch

### 5.1 How EQ Timers Actually Work

In EverQuest, different timers have fundamentally different lifetimes based on what they're tracking. The timer state preservation strategy must reflect these real-world mechanics, not impose an artificial one-size-fits-all model.

**Per-character timers** — tied to a specific character being online:

| Example | What It Tracks | On Log Off | On Log Back In |
|---------|---------------|------------|----------------|
| KEI (buff) | A spell cast *on* the character | Pauses (buff is frozen while offline) | Resumes where it left off |
| Torpor (DoT) | A damage-over-time spell | Pauses | Resumes |
| Mend (ability) | A cooldown specific to this character | Pauses | Resumes |
| Clarity (buff) | Mana regen buff on the character | Pauses | Resumes |

These timers are *frozen* when the character logs off because the game server pauses them too. The buff timer in-game resumes from where it left off when the character returns.

**World/NPC timers** — tied to the game world, not any character:

| Example | What It Tracks | On Character Switch | On App Close |
|---------|---------------|--------------------|----|
| NPC respawn | Time until a named mob respawns | Keeps running (server clock) | Should keep running (real-time) but lost today |
| Debuff on NPC | A spell cast on a mob you're fighting | Keeps running while you're in-zone | Stops (you've left) |
| Raid lockout | Cooldown before re-entering a raid | Keeps running (server clock) | Should keep running |

These timers track things happening *in the world* regardless of which character you're playing. They should continue counting down when you switch characters. When you close the app entirely, whether to save them depends on the scenario — a 6-day raid lockout is worth persisting; a 30-second NPC debuff is not.

**Key insight:** The distinction isn't really "global vs. class-specific" — it's **"tied to the character's online state" vs. "tied to the world clock."** A per-character buff timer on a Necromancer and a per-character buff timer on a Cleric both behave the same way: they pause when that character is offline.

### 5.2 Proposed Timer Scope Model

Add a `Scope` property to each timer definition:

| Scope | Meaning | On Character Switch | On App Close |
|-------|---------|--------------------|----|
| `Character` | Tied to the character being online. Buffs, cooldowns, personal effects. | **Pause** — save remaining time, resume when character returns | **Save to DB** — the character is offline; the timer will resume next session |
| `World` | Tied to the game world clock. NPC respawns, raid lockouts, world events. | **Keep running** — world clock doesn't stop | **Optionally save** — long timers (lockouts) worth persisting; short ones can expire |

This is simpler than the four options in the original design because it maps directly to how EQ actually works. The user sets the Scope when creating the timer — they already know whether a timer is "my buff" or "the NPC's respawn."

```
┌──────────────────────────────────────────────────────────┐
│           Timer Scope Behavior Matrix                    │
│                                                          │
│  Event              Character Scope    World Scope       │
│  ─────────────────  ────────────────   ──────────────    │
│  Character logs off  PAUSE timer       KEEP RUNNING      │
│  Character logs in   RESUME timer      (still running)   │
│  Switch characters   PAUSE for old     KEEP RUNNING      │
│                      RESUME for new                      │
│  Close app           SAVE to DB        SAVE if long      │
│  Open app            RESTORE from DB   RESTORE if saved  │
│  Reset Counts        Clear counts      Clear counts      │
└──────────────────────────────────────────────────────────┘
```

**Example scenario:**

```
Playing as Thorne (Necromancer):
  - KEI buff:      Character scope, 32:00 remaining, running
  - Torpor DoT:    Character scope, 00:12 remaining, running
  - Ghoul respawn: World scope, 14:30 remaining, running
  - Raid lockout:  World scope, 05:42:00 remaining, running

→ Switch to Aelwynn (Cleric):

  Thorne's state saved:
    KEI:    PAUSED at 32:00
    Torpor: PAUSED at 00:12
  World timers continue:
    Ghoul respawn: 14:29... 14:28... (still counting)
    Raid lockout:  05:41:59... (still counting)
  Aelwynn's state restored:
    Virtue buff: RESUMED at 48:22 (where it was when Aelwynn last logged off)

→ Switch back to Thorne 5 minutes later:

  Aelwynn's state saved:
    Virtue: PAUSED at 43:22 (5 min elapsed while active)
  Thorne's state restored:
    KEI:    RESUMED at 32:00 (paused, no time elapsed)
    Torpor: RESUMED at 00:12 (paused)
  World timers still running:
    Ghoul respawn: 09:30 (5 min elapsed)
    Raid lockout:  05:37:00 (5 min elapsed)
```

### 5.3 Where Scope Fits in the Data Model

The `Scope` property lives on the timer *definition* in the database — it's set when the timer is created/edited and doesn't change at runtime:

```sql
-- In the timers table (Phase D migration):
ALTER TABLE timers ADD Scope TEXT DEFAULT 'World'
-- Values: 'Character', 'World'
```

In the Timer Editor dialog (from the architecture redesign):

```
┌──────────────────────────────────────────────────────┐
│               Edit Timer                             │
├──────────────────────────────────────────────────────┤
│                                                      │
│  Name:     [KEI                          ]           │
│  Scope:    [World                    ▼]               │
│  Category: [Buffs                   ▼]               │
│  ...                                                 │
└──────────────────────────────────────────────────────┘
```

**Defaults:**
- All new timers default to `World` scope — this is the safer default because:
  - Existing timers (pre-scope) all behaved as world timers (kept running regardless)
  - Most timers track world events (respawns, lockouts) or general purposes
  - Users who know they want per-character behavior (buffs, cooldowns) can change to `Character`
- Migration of existing timers also defaults to `World` to preserve pre-scope behavior
- User can change to `Character` for buff/cooldown timers that should pause on character switch

### 5.4 Per-Character Runtime State — `CharacterTimerState`

`TimerRuntime` maintains a runtime state object per character. This goes beyond the simple "snapshot" concept — it's a first-class association between a character and their timer state.

```csharp
class CharacterTimerState
{
    public long CharacterID;
    public string CharacterName;
    public List<TimerRuntimeEntry> Entries;   // per-timer runtime data
    public DateTime? LastActiveTime;          // when this character was last active
}

class TimerRuntimeEntry
{
    public long TimerID;              // FK to timers table
    public string Remaining;          // "00:32:00" — frozen value
    public string ButtonState;        // "Stop", "Buff", "Pet", "Ping"
    public int Count;                 // trigger count
    public DateTime? StartedAt;       // when the timer was started (for calculating elapsed on restore)
    public bool WasRunning;           // was this timer actively counting down?
}
```

**In `TimerRuntime`:**

```csharp
// Active state — the character currently being parsed
CharacterTimerState activeCharacterState;

// Saved states — one per character that has been active this session
Dictionary<long, CharacterTimerState> savedCharacterStates;

// World timers — these run independently of any character
List<TimerRuntimeEntry> worldTimerEntries;
```

### 5.5 App Close / App Open — Persistent State

**Today:** All runtime state (remaining time, counts, running/stopped) is lost when the app closes. Counts live only in grid cells; remaining time lives in `TimerPlus` objects. Nothing persists.

**Target:** Per-character timer state persists to the database so it survives app restarts.

New table:

```sql
CREATE TABLE timer_runtime_state (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    TimerID INTEGER NOT NULL,
    CharacterID INTEGER,          -- NULL for World-scope timers
    Remaining TEXT,               -- "00:32:00"
    ButtonState TEXT,             -- "Buff", "Start", etc.
    Count INTEGER DEFAULT 0,
    StartedAt TEXT,               -- ISO 8601 datetime (for World timers to calc elapsed)
    UNIQUE(TimerID, CharacterID)  -- one entry per timer per character
)
```

**Save triggers:**
- On character switch (auto or manual) — save outgoing character's state
- On app close — save active character's state + world timer state
- Periodically (every 60 seconds?) — background save as crash protection

**Load triggers:**
- On app start — restore last active character's state + world timer state
- On character switch — restore incoming character's saved state

**App close behavior:**

```
User closes app:
  1. Per-character timers (Character scope):
     → Save remaining time, count, state to timer_runtime_state
     → Next session, when this character becomes active, restore
  2. World timers (short-lived, < threshold):
     → Discard — a 30-second respawn timer is meaningless tomorrow
  3. World timers (long-lived, > threshold):
     → Save with StartedAt timestamp
     → Next session, calculate elapsed = now - StartedAt
     → If timer hasn't expired, resume with adjusted remaining
     → If timer has expired, mark as completed
  4. Optional: If world timers are running, show brief prompt:
     "Save world timers for next session? [Save] [Discard]"
     (only if there are meaningful long-running world timers)
```

**World timer threshold:** A configurable value (default: 5 minutes). World timers with less than this remaining at app close are discarded. World timers with more are saved with their StartedAt timestamp so elapsed real time can be calculated on restart.

### 5.6 Count and Statistics Persistence

**Today's problem:** The `Count` column in the timer grid is purely in-memory — it's a `DataGridViewCell.Value` that's never saved anywhere. Reset Count zeros them out. Counts are lost on:
- App close/restart
- Character switch
- Database reload (`ReloadFromDatabase`)

**Target:** Counts persist per character in `timer_runtime_state`. This enables:
- "How many times did Torpor fire this session?" — survives character switching
- "How many times did KEI land across all my sessions on Thorne?" — survives app restart

**Future expansion:** The `timer_runtime_state` table (or a companion `timer_statistics` table) could later track:
- Total trigger count (all time)
- Session trigger count (reset on new session)
- Last triggered timestamp
- Custom data fields (see Section 10 — Watcher Vision)

---

## 6. Thread Model Improvements

The current `Thread.Abort()` approach is a known anti-pattern. The move to `LogMonitor` is an opportunity to clean this up.

### Current (problematic)

```csharp
tParseLog = new Thread(new ThreadStart(ParseLog));
tParseLog.Start();
// ...
tParseLog.Abort();  // ← Abrupt; can leave resources in bad state
```

### Proposed: Cancellation Token

```csharp
class LogMonitor
{
    private CancellationTokenSource cts;
    private Thread monitorThread;

    public void Start(List<Characters.GridData> characters)
    {
        cts = new CancellationTokenSource();
        monitorThread = new Thread(() => PollLoop(cts.Token));
        monitorThread.IsBackground = true;
        monitorThread.Start();
    }

    public void Stop()
    {
        cts.Cancel();
        monitorThread.Join(timeout: 2000);  // graceful shutdown
    }

    private void PollLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            foreach (var state in characterFileStates)
            {
                if (token.IsCancellationRequested) break;
                CheckFile(state);
            }
            Thread.Sleep(100);
        }
    }
}
```

**Benefits:**
- Graceful shutdown — no `ThreadAbortException`
- `IsBackground = true` — thread doesn't prevent app exit
- Clean resource cleanup possible in the cancellation path

---

## 7. UX Considerations

### 7.1 Visual feedback on auto-switch

When an auto-switch occurs, the user should know:

- **Character dropdown** updates to the new character automatically
- **Status bar** shows: `"Watching: eqlog_Aelwynn_server.txt (auto-switched)"`
- **Optional toast/notification** — a brief non-modal indication (could be a status bar flash, or a small tray notification)
- **No modal dialog** — switching should be seamless, not interrupt gameplay

### 7.2 Manual override

- The character dropdown still works for manual switching
- If the user manually selects a character, auto-switch remains active — the next detected file change will switch again
- A "Lock to Character" option could disable auto-switch temporarily (pin the current character)

### 7.3 Settings UI

In the Settings dialog (from the architecture redesign):

```
── Auto-Switch ──────────────────────────────
[☑] Auto-switch active character when a
    different log file starts updating
```

Simple checkbox. The threshold (minimum bytes) is an advanced setting that doesn't need UI initially.

---

## 8. Implementation Phases

This feature spans multiple phases of the architecture redesign. The timer scope model (Character vs. World) replaces the earlier class-based approach — it can be implemented as soon as `TimerRuntime` exists, without waiting for the Class system.

### Phase D (TimerRuntime) — Foundation

`LogMonitor` and the Scope model are natural companions to `TimerRuntime`:

1. Extract current `ParseLog()` into `LogMonitor` class
2. Replace `Thread.Abort()` with `CancellationToken`
3. `LogMonitor` fires `LogChunkReceived` → `TimerRuntime.ProcessLogText()`
4. Single-file monitoring (same as today, but cleaner architecture)
5. Add `Scope` column to `timers` table (`ALTER TABLE timers ADD Scope TEXT DEFAULT 'Character'`)
6. Add `Scope` dropdown to `FormEditTimer` (default based on Style: Buff/Pet → Character)
7. Create `timer_runtime_state` table for persistent state
8. Implement `CharacterTimerState` and `TimerRuntimeEntry` in `TimerRuntime`
9. Wire up count persistence — counts survive character switch and app restart

### Phase D+ — Multi-file monitoring + auto-switch

After `LogMonitor` exists as a class:

1. Extend `LogMonitor` to accept `List<Characters.GridData>` instead of a single character
2. Add `CharacterFileState` tracking for each character's log file
3. Add auto-switch detection logic (file growth on non-active character)
4. Fire `CharacterSwitched` event
5. `FormMain` subscribes: updates dropdown, status bar
6. Add "Auto-Switch Enabled" setting to database + Settings dialog

### Phase D++ — Timer state preservation on switch

With `TimerRuntime`, `CharacterTimerState`, and `CharacterSwitched` all in place:

1. On `CharacterSwitched`: save Character-scope timer state for outgoing character
2. On `CharacterSwitched`: restore Character-scope timer state for incoming character
3. World-scope timers continue running uninterrupted through switches
4. Save active state to `timer_runtime_state` on app close
5. Restore state from `timer_runtime_state` on app start
6. Add periodic background save (every 60s) as crash protection
7. Handle world timer threshold — discard short-lived world timers on close, persist long-lived ones with `StartedAt`

### Phase F — Polish

1. UX refinement: auto-switch visual feedback, status bar messages
2. Settings UI: world timer threshold, save prompt preferences
3. Edge case handling: log file appearance, multi-boxing warnings
4. Optional: per-timer `OnSwitch` override (if simpler scope model proves insufficient)

### Summary timeline

```
Phase D:   TimerRuntime + LogMonitor + Scope column + timer_runtime_state table
Phase D+:  Multi-file polling + auto-switch detection
Phase D++: Timer state preservation (pause/resume per Character scope)
Phase F:   Polish (UX, settings, edge cases, future Watcher hooks)
```

**Key change from earlier design:** Timer state preservation no longer depends on the Class system (Phase C). The Character vs. World scope model is independent — it only needs `TimerRuntime` (Phase D) and `CharacterSwitched` (Phase D+) to work.

---

## 9. Risks and Open Questions

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| False positive switch (non-EQ file write) | Low | Medium | Byte threshold (10+ bytes), file path validation |
| Multi-boxing confusion (two EQ instances) | Medium | Low | Document limitation; recommend manual mode for multi-boxing |
| Timer state restoration bugs | Medium | Medium | Start with scope model (simpler than class-based); thorough testing |
| Thread safety on character switch | Medium | High | Use `Invoke()` for UI updates; lock `TimerRuntime` state during switch |
| Data loss on crash (no periodic save) | Medium | Medium | Background save every 60s to `timer_runtime_state`; crash protection |
| Stale world timer state after long app absence | Low | Low | Calculate elapsed from `StartedAt` on restore; expire if overdue |
| Scope defaults wrong for edge-case timers | Low | Low | User can override scope per timer; sensible defaults per Style |

### Open Questions

1. **Multi-boxing support (future):** Should `LogMonitor` ever support parsing multiple files simultaneously for true multi-boxing? This would require multiple `TimerRuntime` instances and a way to show timers from multiple characters. Significant complexity — defer to a future investigation.

2. **Timer sound behavior on switch:** When a Character-scope timer is paused during a switch, it should not fire sounds/voice. World-scope timers that expire during a switch should fire sounds since they represent real-world events. This needs careful testing around the transition moment.

3. **Category auto-activation across characters:** If Character A's category "North Karana" auto-activated timers, and you switch to Character B (who is also in NK), should those timers remain active? Categories are currently global — this is probably fine. Category state may eventually need per-character awareness too.

4. **Log file appearance:** If a character is registered but their log file doesn't exist yet (first time logging in), `LogMonitor` should silently skip that file and start monitoring it when it appears. This requires periodic re-checks for file existence.

5. **Per-timer switch behavior:** The Scope model (Character vs. World) covers the majority of cases. A per-timer `OnSwitch` override (`KeepRunning`, `Pause`, `Stop`) could be added later if edge cases arise — e.g., a Character-scope timer that should keep running, or a World-scope timer that should pause. Defer until the simpler scope model proves insufficient.

6. **Log off detection:** EQ doesn't write a definitive "logged out" message — the log file simply stops growing. The auto-switch detection (file growth on a different character's log) serves as an implicit log-off signal. But what about "log off without logging into another character"? The app can't know for certain. A configurable inactivity timeout (e.g., 5 minutes of no log writes) could be used to trigger a "character went offline" state, pausing Character-scope timers even without switching.

7. **App close with active timers — prompt or auto-save?** When the user closes the app with running timers, should it silently save, prompt, or discard? Recommendation: silently save Character-scope timers (they'll resume next session), prompt only if there are long-running World-scope timers worth preserving. This avoids annoying the user on routine app close while catching the rare "I have a 6-hour raid lockout running" case.

8. **Count semantics — session vs. lifetime:** The `Count` field in `timer_runtime_state` could mean "this session" or "all time." For now, treat it as a running total that persists across sessions. A separate "session count" (reset when the character becomes active) could be added later. The "Reset Count" button should reset both.

---

## 10. Future Vision — From Timer to Watcher

The architecture being built for timer state persistence and log parsing opens the door to a much broader capability. Today, Thorne Timer watches for keywords and counts down timers. But the log file contains a wealth of structured data that could be parsed, categorized, and persisted for analysis.

### 10.1 The Watcher Concept

A **Watcher** is a generalization of a Timer. Where a Timer says "start counting down when you see this keyword," a Watcher says "when you see this pattern, extract data and do something with it."

```
Timer (today):    keyword match → start countdown → alert on expiry
Watcher (future): pattern match → extract data → store/analyze/alert
```

Timers are a subset of Watchers — a Watcher whose action is "start a countdown."

### 10.2 What Could Be Watched

EQ log files contain rich, structured data. Examples of what pattern-based Watchers could track:

| Category | Pattern Example | Data Extracted | Use Case |
|----------|----------------|----------------|----------|
| **Damage** | `"You hit a gnoll for 142 points of damage"` | Target, amount, type | DPS tracking per character |
| **Healing** | `"Aelwynn heals you for 380 points"` | Source, amount | HPS tracking, healer analysis |
| **Commerce** | `"Thorne tells you, 'I'll buy that for 50pp'"` | Item, price, buyer/seller | Price tracking, trade history |
| **Experience** | `"You gain experience!!"` | Timestamp, zone | XP rate tracking, leveling speed |
| **Loot** | `"--Thorne has looted a Bone Chips--"` | Character, item | Loot history, drop rates |
| **Deaths** | `"You have been slain by a sand giant"` | Killer, zone | Death tracking, danger zones |
| **Zone** | `"You have entered North Karana"` | Zone name | Time-in-zone tracking, travel history |
| **Chat** | `"Thorne tells the guild, '...'"` | Channel, speaker, message | Chat logging, search |

### 10.3 Data Model Evolution

The `timer_runtime_state` table is the first step toward a broader `watcher_data` model:

```
Phase 1 (now):    timer_runtime_state — counts, remaining time, per character
Phase 2 (future): timer_statistics    — aggregated stats (total triggers, last triggered, averages)
Phase 3 (future): watcher_data        — arbitrary key/value data extracted by pattern Watchers
Phase 4 (future): watcher_history      — time-series log of all Watcher events (for charts, analysis)
```

Each phase builds on the persistence infrastructure being designed now. The `CharacterTimerState` concept generalizes to `CharacterWatcherState` — all per-character data, not just timer countdowns.

### 10.4 How This Informs Current Design

Even though Watcher features are far in the future, several current design decisions are shaped by this vision:

1. **`timer_runtime_state` uses `TimerID` + `CharacterID` as the key** — this naturally extends to any per-character, per-watcher data storage.

2. **Counts persist across sessions** — this is the first "statistic" we track. The infrastructure for persisting counts is the same infrastructure that would persist damage totals, loot counts, or XP events.

3. **`Scope` (Character vs. World)** — Watchers would have the same distinction. Damage done by *your character* is Character-scope. A guild event or world event is World-scope.

4. **`ProcessLogText()` already does pattern matching** — the current keyword-based matching is a simple version of what a Watcher engine would do. The refactoring into `TimerRuntime` (decoupled from the grid) makes it possible to later add a `WatcherRuntime` that handles more complex patterns.

5. **Log line parsing infrastructure** — `LogMonitor` already reads log chunks and delivers them for processing. A Watcher engine would receive the same `LogChunkReceived` events and parse different patterns from the same text.

### 10.5 Not for Now

This section is intentionally visionary. None of this is planned for implementation in the near term. The current focus is:

1. ✅ Architecture redesign (TimerRuntime, entity dialogs, grid decoupling)
2. ✅ Auto character switching (LogMonitor, multi-file polling)
3. ✅ Timer state persistence (Scope model, timer_runtime_state table)

The Watcher concept lives here as a north star — ensuring that architectural decisions made today don't foreclose on these possibilities tomorrow. When the time comes, the path from Timer to Watcher should feel like a natural extension, not a rewrite.

---

## 11. Implementation Status

*Last updated: Session of initial implementation*

### What's Built

| Component | Status | Notes |
|-----------|--------|-------|
| `LogMonitor` — multi-file polling | ✅ Done | `Start(List<CharacterFileState>, activeCharacterID)` polls all character files, 10-byte switch threshold |
| `LogMonitor` — `CharacterSwitched` event | ✅ Done | Fires when non-active file grows past threshold |
| `LogMonitor` — `AutoSwitchEnabled` toggle | ✅ Done | Property checked before firing `CharacterSwitched`; defaults to `true` |
| `LogMonitor` — `SetActiveCharacter()` | ✅ Done | Updates active character without restarting the monitor |
| `LogMonitor` — single-file backward compat | ✅ Done | `Start(string)` still works for legacy mode |
| `TimerRuntime` — `SaveCharacterState()` | ✅ Done | Freezes Character-scope running timers, captures remaining time, leaves World-scope running |
| `TimerRuntime` — `RestoreCharacterState()` | ✅ Done | Restarts Character-scope timers from saved data (rebuilds `TimerPlus` with remaining time) |
| `FormMain` — auto-switch event handler | ✅ Done | `OnCharacterSwitched`: saves outgoing state → DB, updates dropdown, reloads timers, restores incoming state, refreshes mini views |
| `FormMain` — manual switch with persistence | ✅ Done | `tscActiveCharacter_SelectedIndexChanged` saves/restores state on manual character change |
| `FormMain` — `StartLog()` multi-file mode | ✅ Done | Loads all characters, builds `CharacterFileState` list |
| `FormMain` — auto-switch menu toggle | ✅ Done | Watch > Auto-Switch Character (checkable, persisted to DB `AutoSwitchEnabled` setting) |
| `FormMain` — `LoadTimerRuntime()` full restore | ✅ Done | Calls `RestoreCharacterState()` to restore counts + running timers |
| `FormMain` — `FormClosing` full save | ✅ Done | Calls `SaveCharacterState()` before saving to DB |
| Timer Scope column (`Character`/`World`) | ✅ Done | ComboBox dropdown in timer grid, persisted to DB, flows to `TimerState.Scope` |
| `timer_runtime_state` table | ✅ Done | Schema + CRUD (`SaveTimerStates`, `LoadTimerStates`, `ClearTimerStates`) |
| World-scope timer survival | ✅ Verified | World-scope `TimerPlus` instances survive `LoadTimers()` reload — `OnTimerElapsed`/`OnTimerExpired` use ID-based lookup |

### Current Behavior on Character Switch

**Character-scope timers (Scope = "Character"):**
- Outgoing character: running timers are **stopped**, remaining time + count + button state saved to `timer_runtime_state`
- Incoming character: if saved state exists with a running button state, timer is **restarted** with the saved remaining time
- When a character is not active, their Character-scope timers are not running and not visible in mini views

**World-scope timers (Scope = "World"):**
- Continue running through character switches — `SaveCharacterState()` skips them entirely
- `LoadTimers()` preserves their `RunningTimer` entries and `TimerPlus` objects via ID matching
- Always visible in mini views and grid while running, regardless of which character is active
- On app close: `SaveCharacterState()` freezes only Character-scope, then `StopAllTimers()` stops World-scope. They're saved with current state but not restarted on next app open (see remaining work)

**Mini views:**
- `GetMiniViewData()` returns all timers where `IsRunning == true` — this includes both Character-scope (newly restored) and World-scope (still running)
- After a switch, outgoing character's Character-scope timers disappear from mini views (they're stopped)
- Incoming character's restored timers appear in mini views (they're restarted)
- World-scope timers remain visible throughout

**Main form grid:**
- All timer *definitions* are always visible (grid shows all timers from the DB, not per-character)
- Button state, remaining time, and count columns update to reflect the current character's state after a switch
- `SyncRuntimeToGrid()` is called after every switch to repaint

### Remaining Work

| Item | Priority | Description |
|------|----------|-------------|
| **World-scope timer restart on app open** | Medium | Currently World-scope timers are saved on app close but not restarted on next open. Need to use `StartedAt` timestamp to calculate elapsed real time and resume if not expired. |
| **Periodic background save** | Medium | Save timer state every 60s as crash protection (Section 5.5 of this doc). Not yet implemented. |
| **World timer close threshold** | Low | Short-lived World timers (< 5 min remaining) should be discarded on app close rather than persisted (Section 5.5). Not yet implemented. |
| **Log file appearance monitoring** | Low | If a character's log file doesn't exist at `Start()` time, it's skipped. Should periodically re-check for newly created files (Section 4.3). |
| **Status bar "(auto)" indicator** | ✅ Done | Status bar shows `"(auto)"` suffix on auto-switch. |
| **Per-character ActiveYn persistence** | ✅ Done | `timer_runtime_state.ActiveYn` column stores per-character activation preferences. Saved/restored on character switch. Each character remembers which timers they had active. |
| **Class-based timer grid filtering** | ✅ Done | `ShowAllClasses` toolbar toggle. When unchecked, grid shows only timers matching active character's ClassID (or Global ClassID=0). Persisted to DB. |
| **Auto-Switch toolbar button** | ✅ Done | `tsbAutoSwitch` CheckOnClick button on toolbar, synced with Watch menu item. Always visible regardless of active tab. |
| **Per-timer mini view visibility** | Future | Currently all running timers show in mini views. A future feature could let users choose which timers (or which characters' timers) appear in mini views — useful for World-scope timers from multiple characters. |
| **Inactivity timeout** | Future | Detect "character logged off without switching" via inactivity timeout on the log file (Section 9, Open Question 6). Would pause Character-scope timers after N minutes of no log writes. |
| **Multi-boxing** | Out of scope | Intentionally not supported. Auto-switch is designed for single-instance play with character switches. Multi-boxing users should disable auto-switch. |

### Design Decisions Made

1. **Scope over Class for switch behavior**: Timer behavior on switch is determined by `Scope` (Character/World), not by `ClassID`. Class filtering is a separate visual concern.

2. **Auto-switch is opt-out, not opt-in**: Defaults to enabled because it's the primary value proposition of this feature. Can be toggled via toolbar button or Watch menu.

3. **No restart-on-reconnect for monitor**: When a manual character switch happens while watching, the `LogMonitor` is NOT restarted — `SetActiveCharacter()` updates which file's content is read without stopping/starting the poll thread. This is more efficient and avoids losing tracking state for other files.

4. **Timer definitions are global, activation state is per-character**: Timer definitions (name, keywords, duration, ClassID, Scope) are shared across all characters. Per-character state includes ActiveYn (which timers are enabled), counts, remaining time, and running/stopped status. This means editing a timer definition affects all characters, but each character independently controls which timers they use.

5. **World-scope timers in `timer_runtime_state` use `activeCharacterID`**: Currently, World-scope timer state is saved alongside Character-scope state under the active character's ID. This is a simplification — ideally World-scope timers would use `NULL` CharacterID. This works for now because World-scope timers survive switches in-memory; the DB state is only used for app restart. Could be refined later.

6. **Default Scope is World, not Character**: All six default paths (CREATE TABLE, ALTER TABLE migration, GetTimers fallback, TimerState constructor, btnAddTimer_Click, LoadTimers null-coalesce) use `"World"`. Rationale: existing timers predate scope and behaved as world timers (kept running regardless of character). Most timers track world events. Users who specifically want per-character behavior (buffs, cooldowns that pause on logout) can change to `Character`. This is the safer default for both migration compatibility and new timer creation.

7. **Per-character ActiveYn replaces "lock" concept**: Instead of a separate lock field to prevent auto-activation, each character's ActiveYn preferences are independently saved and restored. A timer that is deactivated for one character stays deactivated for that character regardless of ClassID matching. First-time load with no saved preferences defaults to the timer definition's ActiveYn value. This eliminates the need for bulk auto-activation/deactivation on switch — the user's previous choices for that character are simply restored.

8. **ClassID=0 covers cross-class shared timers**: Common buff timers (POTG, AEGO) that any class can receive use ClassID=0 (Global) + Scope=Character. Each character independently tracks their countdown and activation state. Multi-class (but not all-class) timer associations deferred — ClassID=0 handles the common cases, and the user can keep such timers visible and manually toggle them per-character.

9. **Toolbar for runtime controls, menu for configuration**: Auto-Switch and Show All Classes are toolbar buttons because they're runtime mode switches toggled while playing. They remain useful post-Phase E (entity dialogs) as permanent toolbar controls. Settings that are "set once" remain in menus/dialogs.
