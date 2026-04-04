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

---

## 2. Target Architecture — "The Final Product"

### 2.1 Main Form — Timer-Focused

The main form becomes a **timer dashboard**. No more tabs. The full window is dedicated to showing and controlling timers filtered by the active character.

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
│  [Delete] [Reset Count]              [Stop All] [Add] [Edit] │
│                                                               │
├───────────────────────────────────────────────────────────────┤
│ Status: [Tome path] | [Watching: file] | [Timer stats]       │
└───────────────────────────────────────────────────────────────┘
```

**Key differences from today:**

- **No tabs** — the timer grid owns the full form
- **Read-only by default** — the grid shows data but you can't type into cells
- **Edit button** — opens a dedicated **Timer Editor dialog** for the selected row
- **Active/Enable toggle** and **Start/Stop** still work inline (checkbox click and button click)
- **Filtering** — when a Character is selected, only timers matching that character's Class (or Global timers) are shown

### 2.2 Menu Restructure

```
File                Edit                  View               Watch         Help
├─ New Tome...      ├─ Characters...      ├─ Mini Views      ├─ Start/     ├─ About
├─ Open Tome...     ├─ Categories...      │                  │  Stop
├─ Save Tome As...  ├─ Classes...         │                  │  Watching
├─ ─────────────    ├─ Views...           │                  │
├─ Open Recent  ▸   ├─ Styles...          │                  │
├─ ─────────────    ├─ ─────────────      │                  │
└─ Exit             └─ Settings...        │                  │
```

**Rationale:**

- **Edit** is a new top-level menu, the standard Windows location for managing application entities and preferences
- `Settings...` goes under Edit (standard Windows convention — think Visual Studio's `Tools > Options`, or simpler apps that put it under `Edit > Preferences`)
- Each entity (Characters, Categories, Classes, Views, Styles) opens a **management dialog**
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

### 2.4 Redesigned Entity Dialogs

Each entity gets a **standalone management dialog** opened from the Edit menu. All dialogs follow the same pattern: a grid/list at the top, detail editing below, Add/Delete/Close buttons.

#### Characters Dialog

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

#### Categories Dialog

```
┌──────────────────────────────────────────────────────┐
│               Manage Categories                      │
├──────────────────────────────────────────────────────┤
│                                                      │
│  ┌────────────┬───────────────┬──────────┬──────┐    │
│  │ Name       │ Start Keyword │ End Kwd  │ Auto │    │
│  ├────────────┼───────────────┼──────────┼──────┤    │
│  │ DoTs       │ begins to rot │          │  ☐   │    │
│  │ Buffs      │               │          │  ☐   │    │
│  │ Pets       │               │          │  ☐   │    │
│  └────────────┴───────────────┴──────────┴──────┘    │
│                                                      │
│  [Add]  [Delete]                        [Close]      │
│                                                      │
│  ── Category Details ──────────────────────────      │
│  Name:          [DoTs                    ]            │
│  Start Keyword: [begins to rot           ]            │
│  End Keyword:   [                        ]            │
│  Auto Stop:     [☐]                                   │
│                                                      │
│                                  [Save]  [Cancel]    │
└──────────────────────────────────────────────────────┘
```

#### Views Dialog

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

#### Timer Editor Dialog (opened from main form Edit button or double-click)

```
┌──────────────────────────────────────────────────────┐
│               Edit Timer                             │
├──────────────────────────────────────────────────────┤
│                                                      │
│  Name:           [Torpor                       ]     │
│  Category:       [DoTs                     ▼]        │
│  Class:          [Global (All Classes)     ▼]        │
│  Style:          [Normal                   ▼]        │
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

#### Settings Dialog

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

## 3. Data Flow — How It All Connects

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
              │      └─────────────┤───┘
   Active     │                    │
   Character  │           ┌───────┘
   selection  │           ▼
              │    ┌────────────┐
              │    │ Categories │
              │    └────────────┘
              │
              │    ┌────────────┐     ┌────────────┐
              └───▶│   Views    │────▶│   Styles   │
                   │ StyleFilter│     │ ForeColor  │
                   │ ActiveYn   │     │ BackColor  │
                   └──────┬─────┘     │ Opacity    │
                          │           │ FontSize   │
                          ▼           │ WarnFore   │
                   ┌────────────┐     │ WarnBack   │
                   │ Mini Views │◀────│ PingFore   │
                   │  (forms)   │     │ ...        │
                   └────────────┘     └────────────┘
```

**Timer filtering logic:**

1. User selects Active Character (e.g., "Thorne" — Class: Necromancer)
2. Timer grid shows: all timers where `ClassID IS NULL` (Global) **OR** `ClassID = Necromancer's ClassID`
3. Mini Views show only running timers from the filtered set, routed by Style → View's StyleFilter

---

## 4. File/Class Organization

### New files to create

| File | Purpose |
|------|---------|
| `FormSettings.cs` / `.Designer.cs` | Settings dialog (voice options + general prefs) |
| `FormManageCharacters.cs` / `.Designer.cs` | Character management dialog |
| `FormManageCategories.cs` / `.Designer.cs` | Category management dialog |
| `FormManageClasses.cs` / `.Designer.cs` | Class management dialog |
| `FormManageViews.cs` / `.Designer.cs` | View management dialog |
| `FormManageStyles.cs` / `.Designer.cs` | Style management dialog |
| `FormEditTimer.cs` / `.Designer.cs` | Single-timer editor dialog |
| `Styles.cs` | `Styles.GridData` model class |
| `Classes.cs` | `Classes.GridData` model class |

### Files to modify

| File | Changes |
|------|---------|
| `FormMain.cs` | Remove tabs, remove settings controls, remove entity grids, add Edit menu, make timer grid read-only, add Edit button, add ClassID-based filtering |
| `FormMain.Designer.cs` | Remove all tab/settings controls, simplify to single timer grid |
| `Database.cs` | Add `styles` and `classes` tables, schema migrations, CRUD methods for new entities, add ClassID columns to timers/characters |
| `MiniViews.cs` | Load style colors from `styles` table instead of hardcoded `mv*` fields |
| `Characters.cs` | Add `ClassID` to `GridData` |
| `Timers.cs` | Add `ClassID` to `GridData` |

### Estimated FormMain.cs reduction

- **Current:** ~2,483 lines
- **After all phases:** ~800–1,000 lines (timer grid setup, log parsing, mini view coordination, toolbar/menu handlers)
- **~1,500 lines** move into separate dialog forms and the `Database.cs` CRUD layer

---

## 5. Phased Implementation Plan

### Phase A: Settings Dialog (first)

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

**Goal:** Introduce character classes and wire filtering

1. Create `classes` table schema and seed 16 default EQ classes
2. Create `Classes.cs` model class
3. Create `FormManageClasses` dialog
4. Add `Edit > Classes...` menu item
5. Add `ClassID` column to `characters` table (migration)
6. Add `ClassID` column to `timers` table (migration, nullable = Global)
7. Update timer grid filtering: when Active Character is selected, show only matching-class + global timers
8. Update `SetupTimerGrid` to apply class filter
9. Update mini view timer routing to respect the same filter

### Phase D: Move Characters, Categories, Views to Dialogs

**Goal:** Remove the Characters, Categories, and Views tabs; manage via dialogs

1. Create `FormManageCharacters` with detail editing panel (Name, Class dropdown, LogFile browse)
2. Add `Edit > Characters...` menu item
3. Remove Characters tab and all related FormMain grid code
4. Create `FormManageCategories` with detail editing panel
5. Add `Edit > Categories...` menu item
6. Remove Categories tab and all related FormMain grid code
7. Create `FormManageViews` with detail editing panel (Name, Style dropdown from styles table, ActiveYn)
8. Add `Edit > Views...` menu item
9. Remove Views tab and all related FormMain grid code
10. Remove `TabControl` entirely — timer grid becomes the main content

### Phase E: Timer Redesign

**Goal:** Make the timer grid read-only with an edit dialog, finalize the main form

1. Create `FormEditTimer` dialog (all timer fields in a clean form layout)
2. Add `[Edit]` button to main form (next to Add/Delete)
3. Wire double-click on timer row to open `FormEditTimer`
4. Make timer grid read-only (except Active checkbox and Start/Stop button)
5. Add Class dropdown to `FormEditTimer` (Global + all classes)
6. Refactor `ProcessLogText` to work with the simplified grid
7. Clean up `FormMain.cs` — extract remaining helpers into service classes if needed
8. Final polish: keyboard shortcuts, tab order, consistent dialog sizing

---

## 6. Database Migration Strategy

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

## 7. Summary of What Changes Where

| What | Current Location | Future Location |
|------|-----------------|----------------|
| Voice settings | Settings tab (FormMain) | `Edit > Settings...` (FormSettings) |
| MiniView colors/opacity/font | Settings tab (FormMain) | `Edit > Styles...` (FormManageStyles) → per-style in `styles` table |
| Warning/Ping times | Settings tab (FormMain) | `Edit > Styles...` (FormManageStyles) → per-style |
| Character CRUD | Characters tab (FormMain) | `Edit > Characters...` (FormManageCharacters) |
| Category CRUD | Categories tab (FormMain) | `Edit > Categories...` (FormManageCategories) |
| View CRUD | Views tab (FormMain) | `Edit > Views...` (FormManageViews) |
| Class CRUD | *(doesn't exist yet)* | `Edit > Classes...` (FormManageClasses) |
| Timer editing | Inline grid editing | `[Edit]` button → FormEditTimer dialog |
| Timer display | Timers tab | Full main form (no tabs) |
| Timer filtering | Show all timers | Filter by Active Character's Class |

---

## 8. Design Principles

1. **Main form = timer dashboard.** Everything else is accessed via menus/dialogs.
2. **No accidental edits.** Grids are read-only; editing happens in dedicated forms with Save/Cancel.
3. **Each entity owns its dialog.** Consistent pattern: list at top, detail editor below, Add/Delete/Close buttons.
4. **Styles are first-class.** Visual appearance is fully configurable and reusable across views.
5. **Classes enable filtering.** Timers are scoped to a class or global; the active character drives what's visible.
6. **Phased delivery.** Each phase is independently shippable — the app remains fully functional after every phase.
7. **Backward compatible database.** Old columns stay; new tables are created via migration; old databases open without data loss.
