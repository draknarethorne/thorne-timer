# Thorne Timer — Architecture Redesign

## 1. Current State Assessment

The main form (`FormMain.cs`, ~2,483 lines) is currently doing *everything*: timer runtime logic, settings management, character/category/view CRUD, log parsing, voice synthesis, mini view management. Five tabs compete for attention and entity management uses inline-editable `DataGridView` grids where accidental edits are easy to make.

**Current main form layout:**

```
┌──────────────────────────────────────────────────────────┐
│ Menu: File | View | Watch | Help                         │
├──────────────────────────────────────────────────────────┤
│ Toolbar: [Character ▼] | [▶ Start Watching] | [⊞ Views] │
├──────────────────────────────────────────────────────────┤
│ Tabs: Timers | Characters | Categories | Views | Settings│
│ ┌──────────────────────────────────────────────────────┐ │
│ │                                                      │ │
│ │  (grid + buttons per tab)                            │ │
│ │  Settings tab: Voice Options group + MiniView group  │ │
│ │                                                      │ │
│ └──────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────┤
│ Status: [Tome path] | [Watching: file] | [Timer stats]  │
└──────────────────────────────────────────────────────────┘
```

**Pain points:**

- Settings tab is a flat collection of 30+ controls jammed into two group boxes
- All entity editing is inline — easy to accidentally modify data
- Characters, Categories, Views tabs clutter the main form when you're focused on timers
- No concept of "Styles" as a first-class entity — colors are hardcoded per type
- No concept of "Classes" — no way to filter timers by character class
- `FormMain.cs` is a monolith that's hard to maintain
- **Timer runtime state is coupled directly to grid cells** — `ProcessLogText` reads `StartKeyword`/`EndKeyword` from `DataGridViewCell` objects, `TimerElapsed`/`TimerExpired` write remaining time back into cells, `TimerPlus.RowIndex` identifies timers by grid row position (breaks on sort/filter), and `UpdateMiniTimers` iterates grid rows to build mini view data. There is no separate data model.
- **Category auto-activation reads from the categories grid** — `ProcessLogText` iterates `grdCategories.Rows` to check zone keywords, tightly coupling log parsing to a UI control that should just be data

---

## 2. Target Architecture — "The Final Product"

### 2.1 Main Form — Pure Runtime Dashboard

The main form becomes a **runtime-only timer dashboard**. No tabs. No entity management. The full window is dedicated to monitoring and controlling timers for the active character. All setup — creating, editing, and deleting timers — happens through dialogs opened from the Edit menu, consistent with every other entity.

```
┌───────────────────────────────────────────────────────────────┐
│ Menu: File | Edit | View | Watch | Help                       │
├───────────────────────────────────────────────────────────────┤
│ Toolbar: [Character ▼] | [▶ Start Watching] | [⊞ Views]      │
├───────────────────────────────────────────────────────────────┤
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
File                Edit                  View               Watch         Help
├─ New Tome...      ├─ Timers...          ├─ Mini Views      ├─ Start/     ├─ About
├─ Open Tome...     ├─ Characters...      │                  │  Stop
├─ Save Tome As...  ├─ Categories...      │                  │  Watching
├─ ─────────────    ├─ Classes...         │                  │
├─ Open Recent  ▸   ├─ Views...           │                  │
├─ ─────────────    ├─ Styles...          │                  │
└─ Exit             ├─ ─────────────      │                  │
                    └─ Settings...        │                  │
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
│  Ping Fore: [■]   Ping Back: [■]    Ping Time: [00:30]     │
│  Show Ping: [☑]                                             │
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
    PingForeColor INTEGER,
    PingBackColor INTEGER,
    PingTime TEXT DEFAULT '00:30',
    ShowPing INTEGER DEFAULT 1,
    Opacity INTEGER DEFAULT 100,
    FontSize INTEGER DEFAULT 8
)
```

Seed rows: `Normal`, `Buff`, `Pet`, `Ping` (migrated from current `settings` table values). Users can add custom styles like "EQ Dark Theme" or "Raid Mode".

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

    // ── Runtime state (not persisted) ──
    public string Remaining;       // "00:00:42" — updated by timer tick
    public string ButtonState;     // "Start", "Stop", "Buff", "Pet", "Ping"
    public int Count;              // trigger count for current session
    public TimerPlus RunningTimer; // null when stopped
}
```

**What stays in `FormMain`:**

- Grid setup and refresh (subscribes to `TimerRuntime.TimerStateChanged`)
- Voice synthesis (subscribes to `TimerRuntime.TimerSoundRequested`)
- Menu and toolbar handling
- Mini view coordination (calls `TimerRuntime.GetMiniViewData()`)
- Character switching (calls `TimerRuntime.GetVisibleTimers(classID)`)

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
│  Sound File:     [spell_dot.wav       ] [Browse]     │
│  Speech:         [Torpor fading               ]      │
│                                                      │
│  [☑] Active    [☐] Case Sensitive    [☐] Loop        │
│                                                      │
│                           [Save]    [Cancel]         │
└──────────────────────────────────────────────────────┘
```

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
                 └──────┬─────┘   │ Opacity    │
                        │         │ FontSize   │
                        ▼         │ WarnFore   │
                 ┌────────────┐   │ WarnBack   │
                 │ Mini Views │◀──│ PingFore   │
                 │  (forms)   │   │ ...        │
                 └────────────┘   └────────────┘
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

| File | Purpose |
|------|---------|
| `TimerRuntime.cs` | In-memory timer model: `TimerState`, `CategoryState`, runtime logic, events |
| `FormSettings.cs` / `.Designer.cs` | Settings dialog (voice options + general prefs) |
| `FormManageTimers.cs` / `.Designer.cs` | Timer management dialog (grid + Add/Edit/Delete) |
| `FormEditTimer.cs` / `.Designer.cs` | Single-timer editor dialog (all fields in a form layout) |
| `FormManageCharacters.cs` / `.Designer.cs` | Character management dialog |
| `FormManageCategories.cs` / `.Designer.cs` | Category management dialog |
| `FormManageClasses.cs` / `.Designer.cs` | Class management dialog |
| `FormManageViews.cs` / `.Designer.cs` | View management dialog |
| `FormManageStyles.cs` / `.Designer.cs` | Style management dialog |
| `Styles.cs` | `Styles.GridData` model class |
| `Classes.cs` | `Classes.GridData` model class |

### Files to modify

| File | Changes |
|------|---------|
| `FormMain.cs` | Remove tabs, remove all entity grids, remove settings controls, add Edit menu, replace grid-coupled runtime with `TimerRuntime` subscription, make timer grid read-only |
| `FormMain.Designer.cs` | Remove TabControl and all child controls, simplify to single timer grid + runtime buttons |
| `Database.cs` | Add `styles` and `classes` tables, schema migrations, CRUD methods for new entities, add ClassID columns to timers/characters |
| `MiniViews.cs` | Replace grid-walking with `TimerRuntime.GetMiniViewData()`, load style colors from `styles` table instead of `mv*` fields |
| `TimerPlus.cs` | Change `RowIndex` to `TimerID` (stable identity instead of fragile grid position) |
| `Timers.cs` | Add `ClassID` to `GridData` |
| `Characters.cs` | Add `ClassID` to `GridData` |

### Estimated FormMain.cs reduction

- **Current:** ~2,483 lines
- **After all phases:** ~500–700 lines (grid setup, event subscriptions, menu/toolbar handlers, character switching, mini view coordination)
- **~1,800 lines** move into `TimerRuntime.cs`, dialog forms, and the `Database.cs` CRUD layer

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

### Phase B: Styles Entity

**Status: Not started**

**Goal:** Replace hardcoded color/appearance settings with a dynamic `styles` table

1. Create `styles` table schema and `Database` migration
2. Seed default styles from current `settings` table values
3. Create `Styles.cs` model class
4. Create `FormManageStyles` dialog
5. Add `Edit > Styles...` menu item
6. Update `MiniViews.cs` to load appearance from `styles` table instead of `mv*` fields
7. Update `miniviews.StyleFilter` to reference styles by name
8. Remove MiniView appearance section from Settings dialog (it now lives in Styles)
9. Remove `mv*` color/opacity/font fields from `MiniViews.cs`
10. Keep `settings` table columns for backward compat but stop reading them

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
// Phase B: Styles table
if (!isTableExist(con, "styles"))
{
    // CREATE TABLE styles(...)
    // INSERT default rows migrated from settings values
}

// Phase C: Classes table + FK columns
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
```

Old columns in `settings` table (`MiniViewNormFore`, `MiniViewWarnBack`, etc.) stay in the schema (SQLite can't `DROP COLUMN`) but stop being read after Styles migration populates the `styles` table.

---

## 8. Summary — What Changes Where

| What | Current Location | Future Location |
|------|-----------------|----------------|
| Voice settings | Settings tab (FormMain) | `Edit > Settings...` (FormSettings) |
| MiniView colors/opacity/font | Settings tab (FormMain) | `Edit > Styles...` (FormManageStyles) → per-style in `styles` table |
| Warning/Ping times | Settings tab (FormMain) | `Edit > Styles...` (FormManageStyles) → per-style |
| Timer CRUD | Timers tab (FormMain), inline grid editing | `Edit > Timers...` (FormManageTimers) + FormEditTimer |
| Timer runtime display | Timers tab grid (read-write) | Main form grid (read-only), `TimerRuntime` as model |
| Timer runtime logic | FormMain.cs methods reading grid cells | `TimerRuntime.cs` operating on `TimerState` objects |
| Category auto-activation | FormMain.cs reading `grdCategories` cells | `TimerRuntime.cs` reading `CategoryState` objects |
| Character CRUD | Characters tab (FormMain) | `Edit > Characters...` (FormManageCharacters) |
| Category CRUD | Categories tab (FormMain) | `Edit > Categories...` (FormManageCategories) |
| View CRUD | Views tab (FormMain) | `Edit > Views...` (FormManageViews) |
| Class CRUD | *(doesn't exist yet)* | `Edit > Classes...` (FormManageClasses) |
| Timer filtering | Show all timers | Filter by Active Character's Class via `TimerRuntime` |
| Mini view data | Walk `grdTimers.Rows` in `MiniViews.cs` | `TimerRuntime.GetMiniViewData(styleFilter)` |

---

## 9. New Files — Complete Inventory

### `TimerRuntime.cs` — Timer Model Layer

**What moves here from `FormMain.cs`:**

| Method/Logic | Current Location | Notes |
|-------------|-----------------|-------|
| `ProcessLogText` keyword matching | `FormMain.ProcessLogText()` lines 1903-1986 | Category keyword checking + timer keyword checking |
| `ActivateCategoryTimers` | `FormMain.ActivateCategoryTimers()` | Walks categories, toggles timer ActiveYn |
| `TriggerRowTimer` | `FormMain.TriggerRowTimer()` | Dependency checking (EndKeyword tags) |
| `StartRowTimer` | `FormMain.StartRowTimer()` | Creates TimerPlus, sets button state |
| `StopRowTimer` | `FormMain.StopRowTimer()` | Stops TimerPlus, resets state |
| `StartTimer` | `FormMain.StartTimer()` | TimerPlus lifecycle (create, subscribe, start) |
| `StopTimer` | `FormMain.StopTimer()` | TimerPlus lifecycle (stop, dispose) |
| `TimerElapsed` handler | `FormMain.TimerElapsed()` | Updates remaining time |
| `TimerExpired` handler | `FormMain.TimerExpired()` | Handles timer completion, looping |
| Timer count tracking | Scattered across FormMain | Count incremented on trigger |
| `List<TimerPlus> timers` field | `FormMain` member | Active timer references |

**New code:**

- `TimerState` class (DB fields + runtime state)
- `CategoryState` class (loaded from categories table)
- `GetVisibleTimers(long? classID)` — returns filtered list for grid display
- `GetMiniViewData(string styleFilter)` — returns data for a specific mini view
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
- Detail panel: all color pickers, opacity/font sliders, warn/ping settings
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
        public int PingForeColor { get; set; }
        public int PingBackColor { get; set; }
        public string PingTime { get; set; }
        public long ShowPing { get; set; }
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
