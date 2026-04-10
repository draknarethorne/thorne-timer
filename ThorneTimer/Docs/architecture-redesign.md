# Thorne Timer — Architecture Redesign

## 1. Current State Assessment

The main form (`FormMain.cs`, ~2,244 lines) still manages timer runtime, settings, character/category/view CRUD, log parsing, voice synthesis, and mini view coordination. Five tabs remain, though significant runtime logic has been extracted to `TimerRuntime.cs`. Entity editing is still inline via `DataGridView` grids.

**Current main form layout (as of v0.5.0):**

```
┌────────────────────────────────────────────────────────────────────┐
│ Menu: File | View | Watch | Help                                   │
├────────────────────────────────────────────────────────────────────┤
│ Toolbar: [Character ▼] | [▶ Watch] | [⊞ Views] | [⇄ Auto Switch] │
│          [👁 All Classes] | [◫ Compact View]                       │
├────────────────────────────────────────────────────────────────────┤
│ Tabs: Timers | Characters | Categories | Views | Settings          │
│ ┌────────────────────────────────────────────────────────────────┐ │
│ │ Timer grid: Active, Name, Count, Category, Style, Class,      │ │
│ │   Scope, StartKeyword, EndKeyword, Play, Sound, Speech,       │ │
│ │   Duration, Remaining, Case, Loop, DependsOn, Delay,          │ │
│ │   Start/Stop   (22 columns total, 9 hidden in compact mode)   │ │
│ │                                                                │ │
│ │ Row painting: style-driven colors (lightened style color for   │ │
│ │   running timers, pink for inactive, accent on Remaining cell) │ │
│ └────────────────────────────────────────────────────────────────┘ │
├────────────────────────────────────────────────────────────────────┤
│ Status: [Tome path] | [Watching: file] | [X/Y  Active: A  Run: R] │
└────────────────────────────────────────────────────────────────────┘
```

**Menu structure (current):**

```
File                View                Watch              Help
├─ New Tome...      ├─ Compact View     ├─ Start/Stop      ├─ Tome Info...
├─ Open Tome...     └─ Mini Views       │  Watching        ├─ ──────────────
├─ Save Tome As...                      ├─ Auto-Switch     └─ About
├─ ──────────────                       │  Character
├─ Open Recent  ▸                       └─ Show All
├─ ──────────────                          Classes
└─ Exit
```

**Toolbar (current):**

| Button | Type | Purpose |
|--------|------|---------|
| `tscActiveCharacter` | ComboBox | Active character selection |
| `tsbStartStopWatching` | Toggle | Start/stop log file watching |
| `tsbMiniViews` | Toggle | Show/hide floating mini views |
| `tsbAutoSwitch` | Toggle | Auto-switch character on log activity |
| `tsbShowAllClasses` | Toggle | Show all timers vs. filter by active character's class |
| `tsbCompactView` | Toggle | Compact view (hides config columns, narrows window) |

**Window dimensions:** ClientSize=1400×700, MinimumSize=800×550. Compact view saves/restores per-mode widths via `CompactWidth`/`FullWidth` DB settings.

**What's been resolved since initial assessment:**

- ✅ Timer runtime decoupled from grid → `TimerRuntime.cs` with `TimerState` model
- ✅ Classes entity exists → `classes` table, `ClassID` on timers/characters, grid filtering
- ✅ Timer identification stable → `TimerPlus.TimerID` replaces `RowIndex`
- ✅ Mini views use `TimerRuntime.GetMiniViewData()` instead of walking grid rows
- ✅ All SQL parameterized (no injection)
- ✅ `DependsOnTimer`/`DependsOnDelay` extracted from EndKeyword hack

**Remaining pain points:**

- Settings tab is still a flat collection of controls in two group boxes
- All entity editing is still inline — easy to accidentally modify data
- Characters, Categories, Views tabs still clutter the main form
- Styles are not a first-class entity — colors are hardcoded per type in settings table
- Category auto-activation still reads from `grdCategories.Rows` (deferred to Phase E)
- Ping timer execution model has 6 hardcoded special cases (see Section 9)

---

## 2. Target Architecture — "The Final Product"

### 2.1 Main Form — Pure Runtime Dashboard

The main form becomes a **runtime-only timer dashboard**. No tabs. No entity management. The full window is dedicated to monitoring and controlling timers for the active character. All setup — creating, editing, and deleting timers — happens through dialogs opened from the Edit menu, consistent with every other entity.

```
┌────────────────────────────────────────────────────────────────────┐
│ Menu: File | Edit | View | Watch | Help                            │
├────────────────────────────────────────────────────────────────────┤
│ Toolbar: [Character ▼] | [▶ Watch] | [⊞ Views] | [⇄ Auto Switch] │
│          [👁 All Classes] | [◫ Compact View]                      │
├────────────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────┬────────┬──────────┬────────┬──────┬─────┬──────────┐ │
│  │Act. │ Name   │ Category │ Style  │ Dur. │ Rem │ Start/   │ │
│  │     │        │          │        │      │     │ Stop     │ │
│  ├─────┼────────┼──────────┼────────┼──────┼─────┼──────────┤ │
│  │ ☑   │ Torpor │ DoTs     │ Normal │00:18 │     │ [Start]  │ │
│  │ ☑   │ KEI    │ Buffs    │ Buff   │01:00 │54:22│ [Buff]   │ │
│  │ ☑   │ Pet HP │ Pets     │ Pet    │00:06 │     │ [Start]  │ │
│  │ ☐   │ Snare  │ Utility  │ Normal │00:30 │     │ [Start]  │ │
│  └─────┴────────┴──────────┴────────┴──────┴─────┴──────────┘ │
│                                                               │
│  [Reset Count]                               [Stop All]       │
│                                                               │
├───────────────────────────────────────────────────────────────┤
│ Status: [Tome path] | [Watching: file] | [Timer stats]       │
└───────────────────────────────────────────────────────────────┘
```

**Key differences from today:**

- **No tabs** — the timer grid owns the full form
- **Pure runtime focus** — the main form is for *running* the app, not configuring it
- **Read-only grid** — cells are not editable; data cannot be accidentally changed
- **Active checkbox** — still interactive inline (quick toggle while playing)
- **Start/Stop button** — still interactive inline (quick trigger while playing)
- **Double-click** — opens `FormEditTimer` for the selected timer as a convenience shortcut
- **No Add/Delete/Edit buttons** — all timer CRUD goes through `Edit > Timers...` (keeps the main form clean and consistent with how every other entity is managed)
- **Stop All / Reset Count** — remain on the main form as they are *runtime operations*, not CRUD
- **Filtering** — when a Character is selected, only timers matching that character's Class (or Global timers) are shown

### 2.2 Menu Restructure

```
File                Edit                  View               Watch              Help
├─ New Tome...      ├─ Timers...          ├─ Compact View     ├─ Start/Stop      ├─ Tome Info...
├─ Open Tome...     ├─ Characters...      └─ Mini Views       │  Watching        ├─ ─────────────
├─ Save Tome As...  ├─ Categories...                         ├─ Auto-Switch     └─ About
├─ ─────────────    ├─ Classes...                            │  Character
├─ Open Recent  ▸   ├─ Views...                              └─ Show All
├─ ─────────────    ├─ Styles...                                Classes
└─ Exit             ├─ ─────────────
                    └─ Settings...
```

**Rationale:**

- **Edit** is a new top-level menu, the standard Windows location for managing application entities and preferences
- **Timers...** leads the Edit menu — it's the primary entity and the most commonly managed one
- `Settings...` goes under Edit (standard Windows convention — think Visual Studio's `Tools > Options`, or simpler apps that put it under `Edit > Preferences`)
- Each entity (Timers, Characters, Categories, Classes, Views, Styles) opens a **management dialog** — fully consistent pattern
- **View** menu keeps `Mini Views` toggle — could later add `Toolbar`, `Status Bar` toggles
- **Watch** stays as-is

### 2.3 New Entities

#### Styles (database table: `styles`)

A Style is a named visual theme that can be associated with a View. Instead of hardcoded Normal/Warn/Ping/Buff colors, every aspect becomes configurable per style.

```
┌─────────────────────────────────────────────────────────────┐
│                    Manage Styles                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────┬──────────┬──────────┬─────────┬───────────┐   │
│  │ Name     │ Fore     │ Back     │ Opacity │ Font Size │   │
│  ├──────────┼──────────┼──────────┼─────────┼───────────┤   │
│  │ Normal   │ ■ Black  │ ■ White  │ 100     │ 8         │   │
│  │ Warning  │ ■ White  │ ■ Red    │ 100     │ 8         │   │
│  │ Buff     │ ■ Orange │ ■ Black  │ 100     │ 8         │   │
│  │ Pet      │ ■ Orange │ ■ Black  │ 100     │ 8         │   │
│  │ Ping     │ ■ LGreen │ ■ Black  │ 100     │ 8         │   │
│  │ EQ Dark  │ ■ Gold   │ ■ DkBlue│ 80      │ 10        │   │
│  └──────────┴──────────┴──────────┴─────────┴───────────┘   │
│                                                             │
│  [Add]  [Delete]                            [Close]         │
│                                                             │
│  ── Style Details ──────────────────────────────────────     │
│  Name:     [EQ Dark          ]                              │
│  Fore:     [■] ← click to pick      Opacity: [===80===]    │
│  Back:     [■] ← click to pick      Font:    [===10===]    │
│                                                             │
│  Warn Fore: [■]   Warn Back: [■]    Warn Time: [00:30]     │
│  Show Warning: [☑]   (uncheck to suppress warning colors)   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Schema:**

```sql
CREATE TABLE styles (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    ForeColor INTEGER,        -- ARGB int
    BackColor INTEGER,        -- ARGB int
    WarnForeColor INTEGER,
    WarnBackColor INTEGER,
    WarnTime TEXT DEFAULT '00:30',
    ShowWarning INTEGER DEFAULT 1,  -- 0 = never show warning colors (e.g. Ping)
    Opacity INTEGER DEFAULT 100,
    FontSize INTEGER DEFAULT 8
)
```

Seed rows: `Normal`, `Buff`, `Pet`, `Ping` (migrated from current `settings` table values). The `Ping` style seeds with `ShowWarning = 0`. Users can add custom styles like "EQ Dark Theme" or "Raid Mode".

**Design notes:**

- `ShowWarning` replaces the current hardcoded `if (type != Ping)` check in `MiniView.cs`. Each style decides whether warning colors apply when the timer nears expiry. This makes Ping's "no warning" behavior a style attribute rather than a code branch.
- The old `PingForeColor`, `PingBackColor`, `PingTime`, and `ShowPing` settings are no longer per-style fields — they were artifacts of Ping being a special case. In the new model, Ping is just a style with its own `ForeColor`/`BackColor` and `ShowWarning = 0`.
- The `ShowPing` visibility toggle (whether Ping timers appear at all) moves to the View level — a Ping view can be activated/deactivated like any other view.

**Impact on Views:** The `miniviews` table's existing `StyleFilter` column becomes a foreign key to `styles.Name`. When a View is linked to a Style, it inherits that style's colors, opacity, and font size. This replaces the current global color settings entirely.

#### Classes (database table: `classes`)

A Class represents an EverQuest character class. Characters are linked to a Class, and Timers can be scoped to a Class (or left as Global).

```
┌──────────────────────────────────────────────┐
│              Manage Classes                  │
├──────────────────────────────────────────────┤
│                                              │
│  ┌────────────────────┐                      │
│  │ Bard               │                      │
│  │ Beastlord          │                      │
│  │ Berserker          │                      │
│  │ Cleric             │                      │
│  │ Druid              │                      │
│  │ Enchanter          │                      │
│  │ Magician           │                      │
│  │ Monk               │                      │
│  │ Necromancer        │                      │
│  │ Paladin            │                      │
│  │ Ranger             │                      │
│  │ Rogue              │                      │
│  │ Shadow Knight      │                      │
│  │ Shaman             │                      │
│  │ Warrior            │                      │
│  │ Wizard             │                      │
│  └────────────────────┘                      │
│                                              │
│  [Add]  [Delete]  [Rename]      [Close]      │
│                                              │
└──────────────────────────────────────────────┘
```

**Schema:**

```sql
CREATE TABLE classes (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL
)
```

Seed rows: all 16 EQ classes.

**Impact on Characters:** `characters` table gets a `ClassID INTEGER` column (FK to `classes.ID`). The character management dialog gains a Class dropdown.

**Impact on Timers:** `timers` table gets a `ClassID INTEGER` column (nullable — `NULL` = Global timer). When a timer has a ClassID, it only shows when the active character's class matches. Global timers always show.

### 2.4 Categories — Zone Automation

Categories serve a **dual purpose** that is important to preserve:

1. **Organizational grouping** — timers belong to a named category (DoTs, Buffs, Pets, etc.) for display and management
2. **Zone-based auto-activation** — categories can have `StartKeyword` and `EndKeyword` fields that the log parser watches for. When a keyword fires, all timers in that category are automatically activated or deactivated.

**Example — zone-based activation:**

| Category | Start Keyword | End Keyword | Auto Stop |
|----------|--------------|-------------|-----------|
| North Karana | You have entered North Karana | You have entered | ☑ |
| East Commonlands | You have entered East Commonlands | You have entered | ☑ |
| Raid Buffs | | | ☐ |

When the log parser sees `"You have entered North Karana"`, it activates all timers in the "North Karana" category. When it later sees `"You have entered East Commonlands"`, the AutoStop on NK fires (because "You have entered" matches the EndKeyword), deactivating those timers, and the EC category activates its timers.

**Key distinction from Classes:**

- **Classes** filter the *visible set* of timers on the main form (a Necromancer character sees Necromancer + Global timers)
- **Categories** manage *automatic activation/deactivation* of timers based on log events (entering/leaving zones, etc.)

Both features work together: a timer can be scoped to a Class *and* belong to a Category. The Class controls visibility; the Category controls when it turns on/off.

### 2.5 Internal Architecture — TimerRuntime

**The core architectural change.** Today, all timer runtime state lives inside `DataGridView` cells — the grid *is* the model. This creates tight coupling:

| Current Problem | Detail |
|----------------|--------|
| `ProcessLogText` reads from grid cells | `grdTimers.Rows[r].Cells["StartKeyword"].Value` — log parsing depends on a UI control |
| `TimerPlus.RowIndex` identifies timers by position | If rows are sorted, filtered, or removed, the index becomes wrong |
| `TimerElapsed` writes directly to grid cells | `cell.Value = e.GetTimeRemaining()` — timer tick updates are UI operations |
| `UpdateMiniTimers` iterates grid rows | Mini view data is extracted by walking `grdTimers.Rows` |
| `ActivateCategoryTimers` reads from categories grid | `grdCategories.Rows[r].Cells["StartKeyword"]` — same coupling for categories |

**Solution: `TimerRuntime` class** — an in-memory model that owns all timer and category data independently of any grid. The grid becomes a read-only *view* into this collection.

```
┌─────────────────────────────────────────────────────────┐
│                    TimerRuntime                          │
│                                                         │
│  ┌──────────────────────────────────────────────────┐   │
│  │ List<TimerState> Timers                          │   │
│  │                                                  │   │
│  │  TimerState:                                     │   │
│  │    DB fields: ID, Name, CategoryID, ClassID,     │   │
│  │      StartKeyword, EndKeyword, WAVFile, Speech,  │   │
│  │      Duration, ActiveYn, CaseYn, EndlessYn,      │   │
│  │      Style                                       │   │
│  │    Runtime: Remaining, ButtonState, Count,        │   │
│  │      RunningTimer (TimerPlus ref)                │   │
│  └──────────────────────────────────────────────────┘   │
│                                                         │
│  ┌──────────────────────────────────────────────────┐   │
│  │ List<CategoryState> Categories                   │   │
│  │                                                  │   │
│  │  CategoryState:                                  │   │
│  │    DB fields: ID, Name, StartKeyword,            │   │
│  │      EndKeyword, AutoStop                        │   │
│  └──────────────────────────────────────────────────┘   │
│                                                         │
│  Methods:                                               │
│    LoadTimers(con)      ── populate from database        │
│    LoadCategories(con)  ── populate from database        │
│    ProcessLogText(chunk) ── check keywords, start/stop  │
│    StartTimer(timerID)                                  │
│    StopTimer(timerID)                                   │
│    StopAllTimers()                                      │
│    SetActive(timerID, bool)                              │
│    GetVisibleTimers(classID) ── filtered for grid        │
│    GetMiniViewData(styleFilter) ── filtered for views   │
│    ResetCounts()                                        │
│    SaveCharacterState(con, charID)                       │
│    RestoreCharacterState(con, charID)                    │
│    SyncTimerFieldsFromGrid(timers)                       │
│                                                         │
│  Events:                                                │
│    TimerStateChanged    ── grid refreshes                │
│    TimerSoundRequested  ── FormMain plays WAV/speech    │
│                                                         │
└─────────────────────────────────────────────────────────┘
         │                           │
         ▼                           ▼
  ┌──────────────┐           ┌──────────────┐
  │  Main Form   │           │  MiniViews   │
  │  (grid view) │           │  (forms)     │
  │  read-only   │           │  read-only   │
  └──────────────┘           └──────────────┘
```

**How `TimerRuntime` changes the flow:**

| Operation | Before (grid-coupled) | After (TimerRuntime) |
|-----------|----------------------|---------------------|
| Log parsing | Iterate `grdTimers.Rows`, read cell values | Iterate `TimerRuntime.Timers`, read object properties |
| Timer tick | Write to `grdTimers.Rows[rowIndex].Cells["Remaining"]` | Update `TimerState.Remaining`, fire `TimerStateChanged` |
| Timer identity | `TimerPlus.RowIndex` (fragile) | `TimerPlus.TimerID` → matches `TimerState.ID` (stable) |
| Mini view update | Walk `grdTimers.Rows`, extract name/remaining/style | Call `GetMiniViewData("Buff")` — returns filtered list |
| Category activation | Walk `grdCategories.Rows`, check keywords | Walk `TimerRuntime.Categories`, check keywords |
| Start/stop | Modify `DataGridViewButtonCell.Value` | Set `TimerState.ButtonState`, fire event, grid refreshes |

**`TimerState` — the unified timer object:**

```csharp
class TimerState
{
    // ── Database fields (loaded from DB, saved back on changes) ──
    public long ID;
    public string Name;
    public long CategoryID;
    public long? ClassID;          // null = Global (all classes)
    public string StartKeyword;
    public string EndKeyword;
    public string WAVFile;
    public string Speech;
    public string Duration;        // "00:01:30" format
    public long ActiveYn;
    public long CaseYn;
    public long EndlessYn;
    public string Style;           // "Normal", "Buff", "Pet", "Ping"
    public string Scope;           // "Character" or "World" (default)
    public long DependsOnTimer;    // ID of timer that triggers this one
    public long DependsOnDelay;    // seconds to wait after trigger

    // ── Runtime state (not persisted — or saved to timer_runtime_state) ──
    public string Remaining;       // "00:00:42" — updated by timer tick
    public string ButtonState;     // "Start", "Stop", "Buff", "Pet", "Ping"
    public int Count;              // trigger count for current session
    public TimerPlus RunningTimer; // null when stopped
    public bool IsRunning;         // convenience: RunningTimer != null
    public bool IsActive;          // mirrors ActiveYn for runtime toggling
}
```

**What stays in `FormMain`:**

- Grid setup and refresh (subscribes to `TimerRuntime.TimerStateChanged`)
- Voice synthesis (subscribes to `TimerRuntime.TimerSoundRequested`)
- Menu and toolbar handling (compact view, auto-switch, show-all-classes toggles)
- Mini view coordination (calls `TimerRuntime.GetMiniViewData()`)
- Character switching (auto and manual — calls `TimerRuntime.GetVisibleTimers(classID)`)
- Row painting (style-driven colors via `ApplyTimerRowColor`)
- Compact view toggling (column visibility, window width save/restore)
- Column width persistence (`SaveColumnWidths`/`LoadColumnWidths`)

**What moves out of `FormMain` into `TimerRuntime`:**

- `ProcessLogText` logic (keyword matching, timer triggering, category activation)
- `StartRowTimer` / `StopRowTimer` / `TriggerRowTimer` logic
- `TimerElapsed` / `TimerExpired` handlers
- `ActivateCategoryTimers` logic
- Timer count tracking
- All `TimerPlus` lifecycle management

---

## 3. Entity Dialogs

Each entity gets a **standalone management dialog** opened from the Edit menu. All dialogs follow the same consistent pattern: a read-only grid/list at the top, detail editing below, Add/Delete/Close buttons.

### Timers Dialog (`Edit > Timers...`)

The Timers dialog replaces the current Timers tab for all CRUD operations. This makes timer management fully consistent with how every other entity is managed.

```
┌──────────────────────────────────────────────────────────┐
│                   Manage Timers                          │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  ┌─────┬──────────┬──────────┬────────┬────────┬──────┐  │
│  │Act. │ Name     │ Category │ Class  │ Style  │ Dur. │  │
│  ├─────┼──────────┼──────────┼────────┼────────┼──────┤  │
│  │ ☑   │ Torpor   │ DoTs     │ Global │ Normal │00:18 │  │
│  │ ☑   │ KEI      │ Buffs    │ Global │ Buff   │01:00 │  │
│  │ ☑   │ Pet HP   │ Pets     │ Global │ Pet    │00:06 │  │
│  │ ☐   │ Life Tap │ DoTs     │ Necro  │ Normal │00:12 │  │
│  │ ☑   │ Mend     │ Utility  │ Monk   │ Normal │06:00 │  │
│  └─────┴──────────┴──────────┴────────┴────────┴──────┘  │
│                                                          │
│  [Add]  [Edit]  [Delete]                     [Close]     │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

**Behavior:**

- Grid shows **all** timers (no class filtering — you're managing the full set)
- Read-only grid — no inline editing
- **Add** opens `FormEditTimer` with empty fields
- **Edit** (or double-click) opens `FormEditTimer` for the selected timer
- **Delete** removes the selected timer after confirmation
- **Close** returns to the main form; `TimerRuntime` is refreshed with any changes
- This dialog is not modal — it could stay open alongside the main form if desired, though modal is simpler initially

### Timer Editor Dialog (opened from Manage Timers or main form double-click)

```
┌──────────────────────────────────────────────────────┐
│               Edit Timer                             │
├──────────────────────────────────────────────────────┤
│                                                      │
│  Name:           [Torpor                       ]     │
│  Category:       [DoTs                     ▼] [+]   │
│  Class:          [Global (All Classes)     ▼]        │
│  Style:          [Normal                   ▼] [+]   │
│                                                      │
│  Start Keyword:  [Your target begins to rot  ]       │
│  End Keyword:    [                           ]       │
│  Duration:       [00:00:18                   ]       │
│                                                      │
│  ── Sound / Speech ──────────────────────────────    │
│  Start WAV:      [                    ] [Browse]     │
│  Start Speech:   [                           ]       │
│  End WAV:        [spell_dot.wav       ] [Browse]     │
│  End Speech:     [Torpor fading               ]      │
│                                                      │
│  [☑] Active    [☐] Case Sensitive    [☐] Loop        │
│                                                      │
│                           [Save]    [Cancel]         │
└──────────────────────────────────────────────────────┘
```

**Sound/Speech directionality:** Timers have two sound trigger points — **start** (when the timer begins counting) and **end** (when the timer expires). Each can independently have a WAV file and/or speech text. This replaces the current single `Speech` + `WAVFile` fields and eliminates the Ping special-case execution model (see Section 10).

**Examples:**

| Timer | Start Speech | End Speech | Behavior |
|-------|-------------|------------|----------|
| Torpor (Normal) | *(empty)* | Torpor fading | Speaks on expiry — standard timer |
| Darkness (Ping) | Darkness | *(empty)* | Speaks immediately — alert/ping |
| KEI (Buff) | Buff cast | Rebuff now | Speaks on both — new capability |

**Migration:** Existing `Speech`/`WAVFile` map to `EndSpeech`/`EndWAV` for all styles except Ping, which maps to `StartSpeech`/`StartWAV` (matching current behavior).

**Cross-dialog shortcuts:** The `[+]` buttons next to Category and Style dropdowns open their respective management dialog inline, so you can add a new category or style without leaving the timer editor. This keeps the workflow smooth — you don't have to close, go to Edit menu, create the entity, then come back.

### Characters Dialog

```
┌──────────────────────────────────────────────────────┐
│               Manage Characters                      │
├──────────────────────────────────────────────────────┤
│                                                      │
│  ┌────────────┬─────────────┬────────────────────┐   │
│  │ Name       │ Class       │ Log File           │   │
│  ├────────────┼─────────────┼────────────────────┤   │
│  │ Thorne     │ Necromancer │ eqlog_Thorne.txt   │   │
│  │ Aelwynn    │ Cleric      │ eqlog_Aelwynn.txt  │   │
│  └────────────┴─────────────┴────────────────────┘   │
│                                                      │
│  [Add]  [Delete]                        [Close]      │
│                                                      │
│  ── Character Details ─────────────────────────      │
│  Name:     [Thorne               ]                   │
│  Class:    [Necromancer      ▼]                       │
│  Log File: [C:\EQ\eqlog_Thorne.txt      ] [Browse]   │
│                                                      │
│                                  [Save]  [Cancel]    │
└──────────────────────────────────────────────────────┘
```

### Categories Dialog

```
┌──────────────────────────────────────────────────────┐
│               Manage Categories                      │
├──────────────────────────────────────────────────────┤
│                                                      │
│  ┌────────────┬───────────────┬──────────┬──────┐    │
│  │ Name       │ Start Keyword │ End Kwd  │ Auto │    │
│  ├────────────┼───────────────┼──────────┼──────┤    │
│  │ N. Karana  │ entered North │ entered  │  ☑   │    │
│  │ DoTs       │               │          │  ☐   │    │
│  │ Buffs      │               │          │  ☐   │    │
│  │ Pets       │               │          │  ☐   │    │
│  └────────────┴───────────────┴──────────┴──────┘    │
│                                                      │
│  [Add]  [Delete]                        [Close]      │
│                                                      │
│  ── Category Details ──────────────────────────      │
│  Name:          [N. Karana               ]            │
│  Start Keyword: [entered North Karana    ]            │
│  End Keyword:   [entered                 ]            │
│  Auto Stop:     [☑]                                   │
│                                                      │
│                                  [Save]  [Cancel]    │
└──────────────────────────────────────────────────────┘
```

### Views Dialog

```
┌──────────────────────────────────────────────────────┐
│               Manage Views                           │
├──────────────────────────────────────────────────────┤
│                                                      │
│  ┌───────────────┬──────────────┬────────┐           │
│  │ Name          │ Style        │ Active │           │
│  ├───────────────┼──────────────┼────────┤           │
│  │ Normal Timers │ Normal       │  ☑     │           │
│  │ Buff Timers   │ Buff         │  ☑     │           │
│  │ Pet Window    │ Pet          │  ☑     │           │
│  │ Ping Alerts   │ Ping         │  ☑     │           │
│  │ Raid Mode     │ EQ Dark      │  ☐     │           │
│  └───────────────┴──────────────┴────────┘           │
│                                                      │
│  [Add]  [Delete]                        [Close]      │
│                                                      │
│  ── View Details ──────────────────────────────      │
│  Name:   [Normal Timers          ]                   │
│  Style:  [Normal             ▼]   (from styles tbl)  │
│  Active: [☑]                                         │
│                                                      │
│                                  [Save]  [Cancel]    │
└──────────────────────────────────────────────────────┘
```

### Settings Dialog

```
┌──────────────────────────────────────────────────────┐
│               Settings                               │
├──────────────────────────────────────────────────────┤
│                                                      │
│  ── Voice Options ──────────────────────────────     │
│  Active Voice: [Microsoft David ▼]                   │
│  Volume:       [========70========]  [Test Voice]    │
│  Rate:         [=====-2===========]                  │
│  [☑] Voice Enabled                                   │
│                                                      │
│  ── General ────────────────────────────────────     │
│  (future: auto-save interval, startup behavior,     │
│   notification preferences, etc.)                    │
│                                                      │
│                              [OK]    [Cancel]        │
└──────────────────────────────────────────────────────┘
```

**What moved:** The current MiniView color/opacity/font settings move **into the Styles entity**. The Settings dialog retains only voice settings and future general/application-level preferences. This is a cleaner separation: visual appearance → Styles, application behavior → Settings.

---

## 4. Data Flow — How It All Connects

```
                    ┌──────────┐
                    │  Classes │  (Bard, Cleric, Necromancer...)
                    └─────┬────┘
                          │ ClassID
            ┌─────────────┼─────────────┐
            ▼             ▼             │
      ┌───────────┐  ┌──────────┐      │
      │Characters │  │ Timers   │      │
      │           │  │          │      │
      │ ClassID ──┘  │ ClassID ─┘      │
      │ LogFile      │ Style ──────────┤
      └───────┬───┘  │ CategoryID ─┐   │
              │      └──────┬──────┘   │
   Active     │             │          │
   Character  │      ┌──────┘          │
   selection  │      ▼                 │
              │  ┌────────────┐        │
              │  │ Categories │        │
              │  │ StartKwd   │        │
              │  │ EndKwd     │        │
              │  │ AutoStop   │        │
              │  └──────┬─────┘        │
              │         │ auto         │
              │         │ activate     │
              │         ▼              │
              │  ┌─────────────────┐   │
              │  │  TimerRuntime   │   │
              │  │                 │   │
              │  │ List<TimerState>│───┤── timer data + runtime state
              │  │ List<CatState> │   │
              │  │                 │   │
              │  │ ProcessLogText()│   │
              │  │ GetVisible()   │   │
              │  │ GetMiniData()  │   │
              │  └───┬───────┬────┘   │
              │      │       │        │
              │      ▼       ▼        │
              │  ┌──────┐ ┌───────┐   │
              │  │ Grid │ │ Voice │   │
              │  │(view)│ │(sound)│   │
              │  └──────┘ └───────┘   │
              │                       │
              │  ┌────────────┐   ┌────────────┐
              └─▶│   Views    │──▶│   Styles   │
                 │ StyleFilter│   │ ForeColor  │
                 │ ActiveYn   │   │ BackColor  │
                 └──────┬─────┘   │ WarnFore   │
                        │         │ WarnBack   │
                        ▼         │ ShowWarning│
                 ┌────────────┐   │ Opacity    │
                 │ Mini Views │◀──│ FontSize   │
                 │  (forms)   │   └────────────┘
                 └────────────┘
```

**Timer runtime flow:**

1. User selects Active Character (e.g., "Thorne" — Class: Necromancer)
2. `TimerRuntime.GetVisibleTimers(necromancerClassID)` returns: all timers where `ClassID IS NULL` (Global) **OR** `ClassID = Necromancer's ClassID`
3. Grid displays the filtered list (read-only)
4. Log parser calls `TimerRuntime.ProcessLogText(chunk)`:
   - Checks category keywords → auto-activates/deactivates timers
   - Checks timer keywords → starts/stops matching timers
   - Fires `TimerStateChanged` → grid and mini views refresh
   - Fires `TimerSoundRequested` → FormMain plays WAV/speech
5. Mini Views call `TimerRuntime.GetMiniViewData("Buff")` → filtered running-timer list for that style

---

## 5. File/Class Organization

### New files to create

| File | Purpose | Status |
|------|---------|--------|
| `TimerRuntime.cs` | In-memory timer model: `TimerState`, `CategoryState`, runtime logic, events | ✅ Created |
| `LogMonitor.cs` | Multi-file log polling, `CharacterSwitched` event | ✅ Created |
| `FormSettings.cs` / `.Designer.cs` | Settings dialog (voice options + general prefs) | Phase A |
| `FormManageTimers.cs` / `.Designer.cs` | Timer management dialog (grid + Add/Edit/Delete) | Phase E |
| `FormEditTimer.cs` / `.Designer.cs` | Single-timer editor dialog (all fields in a form layout) | Phase E |
| `FormManageCharacters.cs` / `.Designer.cs` | Character management dialog | Phase E |
| `FormManageCategories.cs` / `.Designer.cs` | Category management dialog | Phase E |
| `FormManageClasses.cs` / `.Designer.cs` | Class management dialog | Phase E |
| `FormManageViews.cs` / `.Designer.cs` | View management dialog | Phase E |
| `FormManageStyles.cs` / `.Designer.cs` | Style management dialog | Phase B |
| `Styles.cs` | `Styles.GridData` model class | Phase B |

### Files already modified (Phase D and QOL work)

| File | Changes Already Made |
|------|---------------------|
| `TimerRuntime.cs` | ✅ `TimerState`/`CategoryState` classes, `LoadTimers()`/`LoadCategories()`, `ProcessLogText()`, `StartTimer()`/`StopTimer()`, `SaveCharacterState()`/`RestoreCharacterState()`, `GetMiniViewData()` (includes Ping), `SyncTimerFieldsFromGrid()`, `GetVisibleTimers()`, events |
| `FormMain.cs` | ✅ Uses `TimerRuntime` for all operations, compact view (`ApplyCompactView` + width save/restore), style-driven row painting (`ApplyTimerRowColor`), status bar (visible/total/active/running), grid column ordering (`ResetTimersGridColumns`), auto-switch handling, `SyncTimerFieldsFromGrid` call from `SaveDataTimers` |
| `FormMain.Designer.cs` | ✅ All sizes 1400×700, toolbar buttons (`tsbAutoSwitch`, `tsbShowAllClasses`, `tsbCompactView`), View menu items (Compact View, Mini Views), Watch menu items (Auto-Switch, Show All Classes) |
| `Database.cs` | ✅ `classes` table + seed data, `ClassID` columns on timers/characters, `Scope` column, `DependsOnTimer`/`DependsOnDelay` columns, all SQL parameterized, `CompactWidth`/`FullWidth` settings, `grid_columns` table, `timer_runtime_state` table, `miniviews` table with full CRUD, `MigrateCategoryScopesToCharacter` (with Style='Pet' for Pet categories) |
| `MiniViews.cs` | ✅ `UpdateMiniTimers(List<MiniTimerData>)` overload using runtime data, `RefreshMiniViews()`, `CreateMiniViews()` from DB, `SaveViewPositions()`, style-based routing via `StyleFilter` |
| `MiniView.cs` | ✅ Warning color skip for Ping timers |
| `TimerPlus.cs` | ✅ `TimerID` replaces `RowIndex` |
| `Timers.cs` | ✅ `ClassID` in `GridData`, button state constants (`btnPing`, `btnPet`, `btnBuff`) |
| `Characters.cs` | ✅ `ClassID` in `GridData` |
| `LogMonitor.cs` | ✅ Multi-file polling, `CharacterSwitched` event, `AutoSwitchEnabled`, `SetActiveCharacter()` |

### Files still to modify (Phases A, B, E)

| File | Remaining Changes |
|------|-------------------|
| `FormMain.cs` | Remove tabs, remove entity grids, remove settings controls, add Edit menu, make timer grid read-only |
| `FormMain.Designer.cs` | Remove TabControl and child controls, simplify to single timer grid + runtime buttons |
| `Database.cs` | Add `styles` table, CRUD for new entities |
| `MiniViews.cs` | Load style colors from `styles` table instead of `mv*` settings fields |

### Estimated FormMain.cs reduction

- **Original:** ~2,483 lines (pre-TimerRuntime)
- **Current:** ~2,244 lines (after extracting runtime logic to TimerRuntime.cs, but with new QOL features added)
- **After all phases:** ~500–700 lines (grid setup, event subscriptions, menu/toolbar handlers, character switching, mini view coordination)
- **~1,500+ lines** still to move into dialog forms and the `Database.cs` CRUD layer

---

## 6. Phased Implementation Plan

### Phase A: Settings Dialog (first)

**Status: Not started**

**Goal:** Remove the Settings tab entirely; open a modal Settings dialog from `Edit > Settings...`

1. Create `FormSettings` with Voice Options group (moved from Settings tab)
2. Keep MiniView color/opacity/font settings on the Settings tab temporarily (they'll move to Styles in Phase B)
3. Add `Edit` menu to `MenuStrip` with `Settings...` item
4. Wire `Edit > Settings...` to open `FormSettings` as a modal dialog
5. On dialog OK, save voice settings to DB and update `FormMain` fields
6. Remove the Voice group from the Settings tab
7. Move MiniView settings into a temporary "Appearance" section of the Settings dialog (until Styles exists)
8. Remove the Settings tab entirely
9. Remove all Settings tab controls from `FormMain.Designer.cs`

**Risk:** Low. Voice settings are independent. MiniView settings can move to Settings dialog as an interim step.

### Phase B: Styles Entity + Directional Speech

**Status: Not started**

**Goal:** Replace hardcoded color/appearance settings with a dynamic `styles` table; replace single Speech/WAVFile with directional Start/End pairs to eliminate Ping special-casing

**Styles table:**

1. Create `styles` table schema with `ShowWarning` attribute (see Section 2.3)
2. Seed default styles: Normal, Buff, Pet, Ping (Ping seeds with `ShowWarning = 0`)
3. Migrate current `settings` table `mv*` values into seed rows
4. Create `Styles.cs` model class
5. Create `FormManageStyles` dialog
6. Add `Edit > Styles...` menu item
7. Update `MiniViews.cs` to load appearance from `styles` table instead of `mv*` fields
8. Update `MiniView.cs` to use style's `ShowWarning` instead of hardcoded Ping check
9. Update `miniviews.StyleFilter` to reference styles by name
10. Remove MiniView appearance section from Settings dialog (it now lives in Styles)
11. Remove `mv*` color/opacity/font fields from `MiniViews.cs`
12. Keep `settings` table columns for backward compat but stop reading them

**Directional speech (can be Phase B or deferred to Phase E with Timer Editor):**

13. Add `StartSpeech`, `StartWAV` columns to `timers` table; rename `Speech` → `EndSpeech`, `WAVFile` → `EndWAV`
14. Migrate: Ping timers → `StartSpeech`/`StartWAV`; all others → `EndSpeech`/`EndWAV`
15. Simplify `StartTimerInternal` — fire start sound if `StartSpeech`/`StartWAV` populated (no Ping check)
16. Simplify `OnTimerExpired` — fire end sound if `EndSpeech`/`EndWAV` populated (no Ping check)
17. Evaluate whether `btnPing` ButtonState can merge with `btnStop` (Ping visual differences handled by Style)
18. Remove `Timers.PingTimer()`, `Timers.TimerRunning()` Ping exclusion, `ShowMiniTimer()` Ping branch
19. Update grid columns, `SyncTimerFieldsFromGrid`, `SaveTimer`, `LoadTimers` for new field names

**See Section 9 for detailed analysis of current Ping technical debt and resolution approach.**

### Phase C: Classes Entity

**Status: Partially complete (infrastructure + filtering)**

**Goal:** Introduce character classes and wire filtering

1. ✅ Create `classes` table schema and seed 16 default EQ classes
2. ~~Create `Classes.cs` model class~~ — deferred; `GetGridClasses()` in Database.cs covers current needs
3. ~~Create `FormManageClasses` dialog~~ — deferred to Phase E
4. ~~Add `Edit > Classes...` menu item~~ — deferred to Phase E
5. ✅ Add `ClassID` column to `characters` table (migration)
6. ✅ Add `ClassID` column to `timers` table (migration, nullable = Global)
7. ✅ Timer grid filtering: `ShowAllClasses` toolbar toggle filters grid rows by active character's ClassID. Global (ClassID=0) timers always shown.
8. ✅ `GetVisibleTimers(classID)` in TimerRuntime for programmatic class filtering
9. ❌ Update mini view timer routing to respect the same filter — not implemented

### Phase D: TimerRuntime — The Model Layer

**Status: ✅ Complete**

**Goal:** Decouple timer runtime logic from the grid; introduce `TimerRuntime` as the central in-memory model

1. ✅ Create `TimerRuntime.cs` with `TimerState` and `CategoryState` classes
2. ✅ Add `LoadTimers()` and `LoadCategories()` methods that populate from DB
3. ✅ Move keyword-matching logic from `ProcessLogText` into `TimerRuntime.ProcessLogText()`
4. ✅ Move `ActivateCategoryTimers` logic into `TimerRuntime`
5. ✅ Move `StartRowTimer` / `StopRowTimer` / `TriggerRowTimer` logic into `TimerRuntime.StartTimer()` / `StopTimer()`
6. ✅ Change `TimerPlus.RowIndex` to `TimerPlus.TimerID` (use `TimerState.ID` for stable identity)
7. ✅ Move `TimerElapsed` / `TimerExpired` handling into `TimerRuntime` (update `TimerState.Remaining`, fire events)
8. ✅ Add `TimerStateChanged` event — `FormMain` subscribes to refresh the grid
9. ✅ Add `TimerSoundRequested` event — `FormMain` subscribes to play WAV/speech
10. ✅ Update `MiniViews.UpdateMiniTimers` to call `TimerRuntime.GetMiniViewData()` instead of walking grid rows
11. ✅ Update `FormMain` to use `TimerRuntime` for all operations (remove direct grid-cell manipulation)
12. ~~Remove `grdCategories` dependency from `FormMain`~~ — deferred; categories tab still exists pending Phase E

**Additional Phase D work completed (from auto-character-switching.md Phases D+ and D++):**

- ✅ `LogMonitor` extended to multi-file polling with `CharacterFileState` tracking
- ✅ `CharacterSwitched` event with 10-byte debounce threshold
- ✅ `AutoSwitchEnabled` toggle (persisted DB setting, Watch menu item)
- ✅ `SetActiveCharacter()` for live switching without monitor restart
- ✅ `SaveCharacterState()` / `RestoreCharacterState()` — scope-aware timer persistence
- ✅ `Scope` field (Character/World) on timers — DB column, grid dropdown, runtime behavior
- ✅ `timer_runtime_state` table with full CRUD
- ✅ `DependsOnTimer` / `DependsOnDelay` refactored from EndKeyword hack to proper fields
- ✅ All SQL parameterized (no SQL injection)
- ✅ Grid bug fixes (ResetTimersGridColumns, btnAddTimer defaults, ActiveYn checkbox sync)

**QOL / GUI preparation work completed (incremental, post-Phase D):**

- ✅ **Compact view**: `tsbCompactView` toolbar button + View > Compact View menu item. Hides 9 config-only columns (StartKeyword, EndKeyword, WAVFile, Speech, CaseYn, EndlessYn, DependsOnTimer, DependsOnDelay, WAV). Window width saves/restores per mode via `CompactWidth`/`FullWidth` DB settings. `ApplyCompactView(compact, initializing)` parameter prevents startup overwrite.
- ✅ **Style-driven row painting**: `ApplyTimerRowColor()` paints running timer rows with lightened style colors (Normal=green, Buff=orange, Pet=orange, Ping=lime), deeper accent on Remaining cell. Inactive timers paint pink. Uses `GetStyleColor()` + `LightenColor()` helpers.
- ✅ **Status bar redesign**: Format `Timers: X/Y   Active: A   Running: R` where X=visible, Y=total. `RepaintTimerGrid()` counts only visible rows for accurate compact/filtered stats.
- ✅ **Grid column ordering**: `ResetTimersGridColumns()` establishes canonical display order + sort modes for all 22 columns. Called after grid setup and column width restoration.
- ✅ **Column width persistence**: `grid_columns` table stores per-grid column widths. `SaveColumnWidths()`/`LoadColumnWidths()` persist on exit and restore on startup.
- ✅ **Window dimensions**: ClientSize=1400×700, MinimumSize=800×550. `DefaultFullViewWidth=1400`, `DefaultCompactViewWidth=800`. Name column FillWeight=60 for proportional scaling.
- ✅ **Mini view refresh on view toggle**: `RefreshMiniViews()` saves positions, destroys, recreates from DB when user activates/deactivates a view. `ValidateRowViews` calls it after saving.
- ✅ **`SyncTimerFieldsFromGrid()`**: Syncs grid edits (Name, Style, Keywords, Duration, etc.) back to `TimerRuntime` without disturbing runtime state. Called from `SaveDataTimers()` after DB save.
- ✅ **Ping timers in mini views**: `GetMiniViewData()` includes `ButtonState == btnPing` alongside `IsRunning`.
- ✅ **Warning color skip for Ping**: `MiniView.cs` skips warning coloration for `ColorType.Ping` (interim fix; resolves with `ShowWarning` style attribute in Phase B).
- ✅ **Migration: Style='Pet' for (Pet) categories**: `MigrateCategoryScopesToCharacter()` detects `(Pet)` in category names and includes `Style='Pet'` in the UPDATE, complementing the `#`-prefix EndKeyword detection.
- ✅ **Toolbar buttons**: `tsbAutoSwitch` and `tsbShowAllClasses` with GDI+ 16×16 icons, CheckOnClick behavior, synced with menu items. `tsbCompactView` toggle. All always visible regardless of active tab.
- ✅ **Show All Classes**: `tsbShowAllClasses` toolbar + Watch > Show All Classes menu. When unchecked, grid filters to active character's ClassID (or Global ClassID=0). Both click handlers call `RepaintTimerGrid()` after `RefreshTimerGridDataSource()` to update status bar.

**Risk:** ~~Medium-high~~ Completed. Needs integration testing.

### Phase D+ and D++: Auto Character Switching + Timer Persistence

**Status: ✅ Complete** — see `auto-character-switching.md` Section 11 for detailed status

### Phase E: Entity Dialogs — Move Everything to Dialogs

**Status: Not started**

**Goal:** Remove all remaining tabs; manage Timers, Characters, Categories, and Views through dialogs

1. Create `FormManageTimers` with read-only grid and Add/Edit/Delete buttons
2. Create `FormEditTimer` dialog (all timer fields, Category/Style [+] shortcuts)
3. Add `Edit > Timers...` menu item
4. Remove Timers tab buttons (Add/Delete/Reset Count from tab) — keep Stop All and Reset Count on main form as runtime controls
5. Create `FormManageCharacters` with detail editing panel (Name, Class dropdown, LogFile browse)
6. Add `Edit > Characters...` menu item
7. Remove Characters tab and all related FormMain grid code
8. Create `FormManageCategories` with detail editing panel
9. Add `Edit > Categories...` menu item
10. Remove Categories tab and all related FormMain grid code
11. Create `FormManageViews` with detail editing panel (Name, Style dropdown, ActiveYn)
12. Add `Edit > Views...` menu item
13. Remove Views tab and all related FormMain grid code
14. Remove `TabControl` entirely — timer grid becomes the main content area
15. Make timer grid fully read-only (except Active checkbox and Start/Stop button)
16. Wire double-click on main form timer row to open `FormEditTimer`
17. After dialog close, call `TimerRuntime.LoadTimers()` to refresh

### Phase F: Polish and Finalization

**Status: Not started**

**Goal:** Final cleanup, UX polish, and consistency pass

1. Keyboard shortcuts for all Edit menu items
2. Tab order consistency across all dialogs
3. Consistent dialog sizing and positioning (center on parent)
4. Cross-dialog [+] buttons in `FormEditTimer` for quick Category/Style creation
5. Main form column width persistence (already exists — verify it still works post-refactor)
6. Ensure `TimerRuntime` correctly handles database switches (New Tome, Open Tome)
7. Final `FormMain.cs` cleanup — extract any remaining helpers
8. Update version, release notes, README

---

## 7. Database Migration Strategy

Each phase adds migrations using the existing `isFieldExist` / `isTableExist` pattern in `Database.Connection()`:

```csharp
// ✅ Phase C: Classes table + FK columns (already implemented)
if (!isTableExist(con, "classes"))
{
    // CREATE TABLE classes(...)
    // INSERT 16 default EQ classes
}
if (!isFieldExist(con, "characters", "ClassID"))
{
    // ALTER TABLE characters ADD ClassID INTEGER
}
if (!isFieldExist(con, "timers", "ClassID"))
{
    // ALTER TABLE timers ADD ClassID INTEGER
}

// ✅ Phase D: Scope, DependsOn, Style (already implemented)
if (!isFieldExist(con, "timers", "Scope"))
{
    // ALTER TABLE timers ADD Scope TEXT DEFAULT 'World'
}
if (!isFieldExist(con, "timers", "DependsOnTimer"))
{
    // ALTER TABLE timers ADD DependsOnTimer INTEGER DEFAULT 0
    // ALTER TABLE timers ADD DependsOnDelay INTEGER DEFAULT 0
}
// Style column migration: @→Buff, #→Pet, Duration=0→Ping
// timer_runtime_state table, grid_columns table, miniviews table

// Phase B: Styles table (future)
if (!isTableExist(con, "styles"))
{
    // CREATE TABLE styles(...)
    // INSERT default rows migrated from settings values
}
```

Old columns in `settings` table (`MiniViewNormFore`, `MiniViewWarnBack`, etc.) stay in the schema (SQLite can't `DROP COLUMN`) but stop being read after Styles migration populates the `styles` table.

---

## 8. Summary — What Changes Where

| What | Current Location | Future Location | Status |
|------|-----------------|----------------|--------|
| Voice settings | Settings tab (FormMain) | `Edit > Settings...` (FormSettings) | Phase A |
| MiniView colors/opacity/font | Settings tab (FormMain) | `Edit > Styles...` (FormManageStyles) → per-style in `styles` table | Phase B |
| Warning/Ping times | Settings tab (FormMain) | `Edit > Styles...` (FormManageStyles) → per-style `ShowWarning` | Phase B |
| Speech/WAV directionality | Single `Speech`/`WAVFile` + Ping hardcoding | `StartSpeech`/`EndSpeech` + `StartWAV`/`EndWAV` per timer | Phase B/E |
| Timer CRUD | Timers tab (FormMain), inline grid editing | `Edit > Timers...` (FormManageTimers) + FormEditTimer | Phase E |
| Timer runtime display | ~~Timers tab grid (read-write)~~ → uses `TimerRuntime` as model | Main form grid (read-only) | ✅ Model done, grid still read-write |
| Timer runtime logic | ~~FormMain.cs reading grid cells~~ → `TimerRuntime.cs` | `TimerRuntime.cs` operating on `TimerState` objects | ✅ Complete |
| Category auto-activation | FormMain.cs reading `grdCategories` cells | `TimerRuntime.cs` reading `CategoryState` objects | Partial (runtime done, grid coupling remains) |
| Character CRUD | Characters tab (FormMain) | `Edit > Characters...` (FormManageCharacters) | Phase E |
| Category CRUD | Categories tab (FormMain) | `Edit > Categories...` (FormManageCategories) | Phase E |
| View CRUD | Views tab (FormMain) | `Edit > Views...` (FormManageViews) | Phase E |
| Class CRUD | ~~doesn't exist~~ → `classes` table + grid column | `Edit > Classes...` (FormManageClasses) | ✅ Infrastructure done, dialog Phase E |
| Timer filtering | ~~Show all timers~~ → `ShowAllClasses` toggle | Filter by Active Character's Class | ✅ Complete |
| Mini view data | ~~Walk `grdTimers.Rows`~~ → `TimerRuntime.GetMiniViewData()` | Same | ✅ Complete |
| Compact view | *(didn't exist)* | Toolbar toggle + window sizing | ✅ Complete |
| Row painting | *(didn't exist)* | Style-driven colors for running/inactive timers | ✅ Complete |
| Column persistence | *(didn't exist)* | `grid_columns` table save/restore | ✅ Complete |
| Auto character switching | Manual dropdown only | Multi-file polling + `CharacterSwitched` event | ✅ Complete |
| Timer state persistence | Lost on switch | `timer_runtime_state` table + save/restore | ✅ Complete |

---

## 9. Ping Execution Model — Technical Debt

### Current State (v0.5.0)

Ping timers have a fundamentally different execution model from Normal/Buff/Pet timers, implemented as hardcoded special cases throughout the codebase:

| Branch Point | Location | What It Does |
|-------------|----------|-------------|
| Speech on start | `TimerRuntime.StartTimerInternal()` | `if (Ping) FireSoundRequested(ts)` — plays speech immediately when timer starts |
| Skip speech on expiry | `TimerRuntime.OnTimerExpired()` | `if (type != Ping)` — suppresses end-of-timer speech for Ping |
| Not "running" | `Timers.TimerRunning()` | Returns false for `btnPing` — Ping timers excluded from running-timer checks |
| Mini view visibility | `MiniViews.ShowMiniTimer()` | Separate `Timers.PingTimer() && ShowPing()` check alongside `TimerRunning()` |
| Mini view data | `TimerRuntime.GetMiniViewData()` | Separate `ts.ButtonState == btnPing` check alongside `ts.IsRunning` |
| Warning colors | `MiniView.cs` LoadData | `if (type != Ping)` — skips warning coloration for Ping timers |

**The underlying concept:** The only behavioral difference is *when* speech fires — **on start** (Ping) vs. **on expiry** (everything else). This is currently encoded as a style distinction, but it's really a timer attribute.

### Resolution — Directional Speech (Phase B/E)

Replace the single `Speech` and `WAVFile` fields with directional pairs:

```sql
ALTER TABLE timers ADD StartSpeech TEXT DEFAULT '';
ALTER TABLE timers ADD StartWAV TEXT DEFAULT '';
ALTER TABLE timers RENAME COLUMN Speech TO EndSpeech;   -- or migrate with new columns
ALTER TABLE timers RENAME COLUMN WAVFile TO EndWAV;
```

**Migration:**
- For timers where `Style != 'Ping'`: `Speech` → `EndSpeech`, `WAVFile` → `EndWAV` (current behavior preserved)
- For timers where `Style == 'Ping'`: `Speech` → `StartSpeech`, `WAVFile` → `StartWAV` (matches current Ping-fires-on-start behavior)

**Code simplification:** After migration, the timer engine becomes style-agnostic for sound:

```csharp
// StartTimerInternal — no more if(Ping) check
if (!string.IsNullOrEmpty(ts.StartSpeech) || !string.IsNullOrEmpty(ts.StartWAV))
    FireSoundRequested(ts.StartSpeech, ts.StartWAV);

// OnTimerExpired — no more if(type != Ping) check
if (!string.IsNullOrEmpty(ts.EndSpeech) || !string.IsNullOrEmpty(ts.EndWAV))
    FireSoundRequested(ts.EndSpeech, ts.EndWAV);
```

**Warning colors** become a style attribute (`ShowWarning` on the `styles` table) rather than a hardcoded Ping exclusion.

**"Running" state** simplification: With `ShowWarning` on the style and speech directionality on the timer, there's less reason for Ping to have a separate `btnPing` ButtonState. Ping could use the same `btnStop` state as Normal timers, with the visual differences handled entirely by the Style. This would eliminate the `TimerRunning()` / `PingTimer()` / `ShowMiniTimer()` branching. (Evaluate during Phase B — may require careful testing of the countdown display behavior.)

### Interim Fixes Already Applied

- ✅ `GetMiniViewData()` includes Ping timers (`|| ts.ButtonState == btnPing`) — commit `807093b`
- ✅ `SyncTimerFieldsFromGrid()` syncs Style changes to runtime — commit `807093b`
- ✅ Warning colors skipped for Ping in mini view — commit `da28949`
- ✅ Migration sets `Style='Pet'` for `(Pet)` categories — commit `703aad5`

These are correct interim fixes. They'll dissolve naturally when Styles (Phase B) and directional speech (Phase B/E) are implemented.

---

## 10. New Files — Complete Inventory

### `TimerRuntime.cs` — Timer Model Layer (✅ Created)

**Already implemented.** The following have been moved from `FormMain.cs`:

| Method/Logic | Status | Notes |
|-------------|--------|-------|
| `ProcessLogText` keyword matching | ✅ Moved | Category keyword checking + timer keyword checking |
| `ActivateCategoryTimers` | ✅ Moved | Walks `CategoryState` list, toggles timer ActiveYn |
| Timer trigger with dependency checking | ✅ Moved | `StartTimerInternal` handles `DependsOnTimer`/`DependsOnDelay` |
| Start/Stop timer lifecycle | ✅ Moved | Creates `TimerPlus`, sets button state |
| `TimerElapsed` / `TimerExpired` handlers | ✅ Moved | Updates `TimerState.Remaining`, fires events |
| Timer count tracking | ✅ Moved | Count incremented on trigger |
| `SaveCharacterState()` / `RestoreCharacterState()` | ✅ New | Scope-aware timer persistence for character switching |
| `SyncTimerFieldsFromGrid()` | ✅ New | Syncs grid edits back to `TimerState` without disturbing runtime |

**Also includes:**

- `TimerState` class (DB fields + runtime state, including `Scope`, `DependsOnTimer`, `DependsOnDelay`)
- `CategoryState` class (loaded from categories table)
- `GetVisibleTimers(long? classID)` — returns filtered list for grid display
- `GetMiniViewData(string styleFilter)` — returns data for a specific mini view (includes Ping)
- `TimerStateChanged` event
- `TimerSoundRequested` event

### `FormSettings.cs` / `.Designer.cs` — Settings Dialog

**What moves here from `FormMain`:**

- Voice Options group: `cboActiveVoice`, `tbVolume`, `tbVoiceRate`, `btnTestVolume`, `chkVoiceEnabled` + labels
- Temporarily: MiniView appearance group (until Styles entity exists in Phase B)
- Event handlers: `cboActiveVoice_SelectedIndexChanged`, `tbVolume_Scroll`, `tbVoiceRate_Scroll`, `btnTestVolume_Click`, `chkVoiceEnabled_Click`
- Database read/write for `settings` table voice columns

### `FormManageTimers.cs` / `.Designer.cs` — Timer Management Dialog

**What moves here from `FormMain`:**

- Timer grid setup (simplified — no Start/Stop button column, no Remaining column, since this is for configuration not runtime)
- `btnAddTimer_Click` → opens `FormEditTimer`
- `btnDeleteTimer_Click` → delete with confirmation
- Database calls: `Database.GetTimers()`, `Database.DeleteTimer()`

### `FormEditTimer.cs` / `.Designer.cs` — Timer Editor Dialog

**New code (not a direct move — new form):**

- All timer fields in a clean form layout
- Category dropdown (populated from DB + "Add New..." option)
- Class dropdown (populated from DB, "Global" default)
- Style dropdown (populated from DB + "Add New..." option)
- WAV file browse button
- Save/Cancel with validation
- Database calls: `Database.SaveTimer()`

### `FormManageCharacters.cs` / `.Designer.cs` — Character Management Dialog

**What moves here from `FormMain`:**

- Character grid setup: `SetupCharacterGrid()`
- `btnAddCharacter_Click`, `btnDeleteCharacter_Click`
- Detail panel (new): Name, Class dropdown, LogFile browse
- Database calls: `Database.GetCharacters()`, `Database.SaveCharacter()`, `Database.DeleteCharacter()`

### `FormManageCategories.cs` / `.Designer.cs` — Category Management Dialog

**What moves here from `FormMain`:**

- Category grid setup: `SetupCategoriesGrid()`
- `btnAddCategory_Click`, `btnDeleteCategory_Click`
- Detail panel (new): Name, StartKeyword, EndKeyword, AutoStop
- Database calls: `Database.GetCategories()`, `Database.SaveCategory()`, `Database.DeleteCategory()`

### `FormManageClasses.cs` / `.Designer.cs` — Class Management Dialog

**New code:**

- Simple list/grid of class names
- Add, Delete, Rename buttons
- Database calls: new CRUD methods in `Database.cs`

### `FormManageViews.cs` / `.Designer.cs` — View Management Dialog

**What moves here from `FormMain`:**

- View grid setup: `SetupViewsGrid()`
- `btnAddView_Click`, `btnDeleteView_Click`
- Detail panel (new): Name, Style dropdown (from styles table), ActiveYn
- Database calls: `Database.GetViews()`, `Database.SaveView()`, `Database.DeleteView()`

### `FormManageStyles.cs` / `.Designer.cs` — Style Management Dialog

**New code:**

- Style grid showing Name, ForeColor, BackColor, Opacity, FontSize
- Detail panel: all color pickers, opacity/font sliders, ShowWarning toggle, warn colors/time
- Database calls: new CRUD methods in `Database.cs`

### `Styles.cs` — Style Model

```csharp
class Styles
{
    public class GridData
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public int ForeColor { get; set; }
        public int BackColor { get; set; }
        public int WarnForeColor { get; set; }
        public int WarnBackColor { get; set; }
        public string WarnTime { get; set; }
        public long ShowWarning { get; set; }  // 0 = suppress warning colors (e.g. Ping)
        public long Opacity { get; set; }
        public long FontSize { get; set; }
    }
}
```

### `Classes.cs` — Class Model

```csharp
class Classes
{
    public class GridData
    {
        public long ID { get; set; }
        public string Name { get; set; }
    }
}
```

---

## 10. Future Vision — Beyond Timers

This section captures planned directions that extend the app's log-parsing foundation
into broader QOL utility. These are **not** part of the current phased plan but should
inform architectural decisions so they don't paint us into a corner.

### 10.1 Watcher Style — Merchant Window & Text Display

**Problem:** The TAKP/P2002 merchant window does not display buy/sell prices when an
item is selected. The prices only appear in the chat log, which scrolls quickly and is
hard to read during a transaction.

**Solution:** A new timer `Style` called **Watcher** (or **Merchant**) that:

- Uses `StartKeyword` and `EndKeyword` to define a text region in the log
- Captures all log text between those delimiters
- Displays the captured text in a dedicated MiniView window (new style template)
- The MiniView sits near the in-game merchant window for easy reference

**Architecture impact:**

- `Style` field on timers already exists — add `"Watcher"` / `"Merchant"` as new style values
- `TimerRuntime.ProcessLogText()` needs a capture-mode path: when a Watcher timer's
  `StartKeyword` is matched, begin buffering lines until `EndKeyword` is seen
- New event (e.g., `WatcherTextCaptured`) to push the buffered text to the UI
- MiniView needs a text-display template (multi-line label or textbox) vs. the current
  countdown-bar style
- Consider: should the display persist until the next capture, or auto-clear after a timeout?

**Future extensions:**

- Speech synthesis: read the captured merchant text aloud via the existing voice engine
- Display keywords: instead of end-keyword termination, show N lines after the start keyword,
  or show lines matching a display-filter pattern within the capture region
- Multiple simultaneous Watchers for different log patterns (merchant, NPC speech, etc.)

### 10.2 Session Metrics — Damage, Fizzles, Resists

**Problem:** The existing `Count` column on timers is useful for tracking keyword
frequency (EXP gains, roots, resists per session), but the user wants more structured
metrics without bloating the app into a full log parser/analytics tool.

**Planned metrics:**

- **Fizzle count** — track spell fizzles per session
- **Resist count** — track resist messages (already partially covered by keyword timers)
- **EXP count** — already working via keyword timer with Count column
- **Damage output** — basic DPS or total damage per session (future consideration)

**Architecture impact:**

- Counts already work via `TimerState.Count` — no schema change needed for basic metrics
- A "Metrics" or "Session" panel on the main form could aggregate counts from multiple
  timers (sum fizzles + resists + EXP into a dashboard row)
- `TimerRuntime` could expose a `GetSessionMetrics()` method that aggregates counts
  by category or tag
- For damage tracking, `ProcessLogText` would need a regex path to extract numeric
  values from damage lines — this is a larger effort and may warrant its own subsystem

**Design principle:** Keep it real-time and lightweight. This is a HUD overlay tool,
not a log analysis workbench. Show what matters *now*, not historical trends.

### 10.3 Icon-Only Toolbar Mode

A future setting to toggle between `ImageAndText` and `Image` display styles on all
toolbar buttons. This would reclaim horizontal space as more toolbar items are added.

- Add a `ToolbarDisplayStyle` setting (persisted to DB)
- On change, iterate `toolStrip.Items` and set each button's `DisplayStyle`
- Could be a `View > Toolbar > Icons Only / Icons and Text` submenu

---

## 11. Design Principles

1. **Main form = runtime dashboard.** The main form is for *running* the app — watching, triggering, monitoring. Everything else is accessed via menus/dialogs.
2. **No accidental edits.** All grids are read-only. Editing happens in dedicated forms with explicit Save/Cancel.
3. **Every entity has a dialog.** Timers, Characters, Categories, Classes, Views, Styles — all managed through a consistent dialog pattern (list + detail + Add/Edit/Delete/Close).
4. **TimerRuntime is the model.** No UI control should own data. `TimerRuntime` holds the canonical timer and category state; the grid and mini views are read-only projections.
5. **Styles are first-class.** Visual appearance is fully configurable and reusable across views.
6. **Classes enable filtering.** Timers are scoped to a class or global; the active character drives what's visible.
7. **Categories enable automation.** Zone keywords automatically activate/deactivate timer groups. This is distinct from class-based filtering.
8. **Phased delivery.** Each phase is independently shippable — the app remains fully functional after every phase.
9. **Backward compatible database.** Old columns stay; new tables are created via migration; old databases open without data loss.
10. **Stable timer identity.** Timers are identified by database ID, never by grid row position.
