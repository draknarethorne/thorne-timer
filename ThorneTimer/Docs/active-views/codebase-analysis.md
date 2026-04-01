# Thorne Timer — Codebase Analysis

> **Date:** 2026-03-27  
> **Purpose:** Comprehensive code review to inform architecture decisions

---

## Project Overview

| Aspect | Details |
|--------|---------|
| **Framework** | .NET Framework 4.8, WinForms |
| **Database** | SQLite via System.Data.SQLite + Entity Framework 6 |
| **Solution** | `Thorne-Timer.sln` with single project `ThorneTimer` |
| **Lines of Code** | ~3,500 (estimated across all .cs files) |

---

## File Structure Analysis

### Core Application Files

| File | Lines | Responsibility | Health |
|------|-------|----------------|--------|
| `FormMain.cs` | ~1000+ | **Everything** — UI setup, event handling, log parsing, timer management, grid operations, settings | ⚠️ **Too large** |
| `FormMain.Designer.cs` | ~900 | Auto-generated designer code | ✅ Normal |
| `Database.cs` | ~820 | SQLite operations, schema migrations, CRUD for all entities | ⚠️ **Mixed patterns** |
| `MiniViews.cs` | ~280 | Container for 4 hardcoded overlay windows | ⚠️ **Needs refactor** |
| `MiniView.cs` | ~250 | Individual overlay window rendering | ✅ Focused |
| `Timers.cs` | ~60 | Timer state constants and helper methods | ✅ Clean |
| `TimerPlus.cs` | ~100 | Extended timer with elapsed tracking | ✅ Clean |
| `Characters.cs` | ~30 | Character data model (GridData class) | ✅ Clean |
| `Categories.cs` | ~30 | Category data model (GridData class) | ✅ Clean |
| `SortableBindingList.cs` | ~100 | Generic sortable list for DataGridView | ✅ Clean |
| `ComboBoxItem.cs` | ~20 | Helper for combobox value/text pairs | ✅ Clean |

### Support Files

| File | Purpose |
|------|---------|
| `Program.cs` | Application entry point |
| `FormAbout.cs` | About dialog |
| `Properties/AssemblyInfo.cs` | Assembly metadata |
| `Properties/Resources.resx` | Embedded resources |
| `Properties/Settings.settings` | User settings persistence |

---

## Architecture Issues

### 1. FormMain.cs is a God Class

**Problem:** `FormMain.cs` handles:
- All UI initialization and event wiring
- Timer grid setup and operations
- Character grid setup and operations
- Category grid setup and operations
- Log file parsing (runs on background thread)
- Timer triggering and countdown logic
- Mini view coordination
- Settings management
- Voice/sound playback

**Impact:** 
- Hard to understand and modify
- Testing is nearly impossible
- All changes risk regressions

**Recommendation:** Extract into focused classes:
```
FormMain.cs          → UI wiring only
LogParser.cs         → Log file watching and parsing
TimerGridManager.cs  → Timer DataGridView operations
ViewManager.cs       → Mini view lifecycle management
SettingsManager.cs   → Settings load/save
SoundManager.cs      → WAV playback and TTS
```

### 2. Mixed Database Patterns

**Problem:** `Database.cs` uses:
- ✅ Parameterized queries in some places (good)
- ⚠️ String concatenation in others (SQL injection risk)

**Examples of vulnerable code:**
```csharp
// In SaveTimer():
sql += "Name = '" + Convert.ToString(Name.Value) + "',";

// In SaveCategory():
sql += "Name = '" + Convert.ToString(Name.Value) + "', ";

// In SetSetting():
CommandText = "UPDATE settings SET " + column + " = '" + value + "'"
```

**Recommendation:** Standardize on parameterized queries everywhere.

### 3. Hardcoded View System

**Problem:** Four views are hardcoded in `MiniViews.cs`:
```csharp
private MiniView miniView = null;
private MiniView petView = null;
private MiniView buffView = null;
private MiniView pingView = null;
```

**Impact:**
- Users cannot create custom views
- Cannot add/remove views without code changes
- Position offsets are magic numbers

**Solution:** See [active-views-design.md](./active-views-design.md)

### 4. Schema Migration is Fragile

**Problem:** `Database.Connection()` handles migrations inline with many `if (!isFieldExist())` checks spanning ~200 lines.

**Impact:**
- Hard to track what version a database is at
- No rollback capability
- Migration logic is procedural and repetitive

**Recommendation:** Implement a version table and migration runner:
```sql
CREATE TABLE schema_version (version INTEGER);
```
```csharp
void RunMigrations() {
    int currentVersion = GetSchemaVersion();
    if (currentVersion < 1) RunMigration1();
    if (currentVersion < 2) RunMigration2();
    // etc.
}
```

---

## Data Flow Analysis

### Timer Lifecycle

```
1. User adds timer in grdTimers
   └── ValidateRowTimers() → SaveDataTimers() → Database.SaveTimer()

2. Log parser detects StartKeyword
   └── TriggerRowTimer() → creates TimerPlus, starts countdown

3. TimerPlus.Elapsed event fires
   └── Updates grdTimers.Remaining cell, plays sound/speech

4. Timer reaches 00:00:00 or EndKeyword detected
   └── StopRowTimer() → clears Remaining, resets button
```

### Mini View Update Cycle

```
1. Any timer change (start, tick, stop)
   └── UpdateMiniView() called
       └── miniViews.UpdateMiniTimers(grdTimers)

2. UpdateMiniTimers scans all grid rows
   └── Filters by StartStop button value (Start/Stop/Pet/Buff/Ping)
   └── Builds 4 separate List<MiniData>
   └── Calls LoadData() on each hardcoded view
```

### Log Parsing Thread

```
1. btnStartStopLog_Click()
   └── Creates Thread(ParseLog)
   
2. ParseLog() runs continuously
   └── Opens FileStream with FileShare.ReadWrite
   └── Seeks to end of file
   └── Loops: ReadLine() → ProcessLogEntry()
   
3. ProcessLogEntry()
   └── Checks each timer's StartKeyword/EndKeyword
   └── Triggers or stops timers via Invoke()
```

---

## Database Schema

### Current Tables

```sql
-- Core timer data
CREATE TABLE timers (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT,
    CategoryID INTEGER,
    StartKeyword TEXT,
    EndKeyword TEXT,
    WAVFile TEXT,
    Speech TEXT,
    Duration TEXT,          -- Format: "HH:MM:SS"
    ActiveYn INTEGER,       -- 0/1 boolean
    CaseYn INTEGER,         -- Case-sensitive matching
    EndlessYn INTEGER       -- Loop timer
);

-- Character profiles
CREATE TABLE characters (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT,
    LogFile TEXT,           -- Full path to eqlog_CharName_server.txt
    MiniViewX INTEGER,      -- Single position (legacy)
    MiniViewY INTEGER
);

-- Timer groupings
CREATE TABLE categories (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT,
    StartKeyword TEXT,      -- Category-level trigger
    EndKeyword TEXT,
    AutoStop INTEGER        -- Stop all category timers on end
);

-- Global settings
CREATE TABLE settings (
    ID INTEGER PRIMARY KEY, -- Always 1
    ActiveCharacterID TEXT,
    ActiveVoice TEXT,
    MiniViewFontSize INTEGER,
    MiniViewOpacity INTEGER,
    MiniViewWarnFore INTEGER,
    MiniViewWarnBack INTEGER,
    MiniViewWarnTime TEXT,
    MiniViewNormFore INTEGER,
    MiniViewNormBack INTEGER,
    MiniViewShowPing INTEGER,
    MiniViewPingFore INTEGER,
    MiniViewPingBack INTEGER,
    MiniViewPingTime TEXT,
    MiniViewBuffFore INTEGER,
    MiniViewBuffBack INTEGER,
    VoiceVolume INTEGER,
    VoiceRate INTEGER,
    VoiceEnabled INTEGER
);

-- Views (currently incomplete)
CREATE TABLE miniviews (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT
    -- Missing: position, filter, character link, etc.
);
```

---

## Technical Debt Inventory

### High Priority

| Issue | Location | Impact | Effort |
|-------|----------|--------|--------|
| Hardcoded 4 views | `MiniViews.cs` | Blocks user customization | High |
| SQL injection vulnerabilities | `Database.cs` | Security risk | Medium |
| God class FormMain | `FormMain.cs` | Maintainability | High |

### Medium Priority

| Issue | Location | Impact | Effort |
|-------|----------|--------|--------|
| Fragile schema migrations | `Database.cs` | Upgrade failures | Medium |
| No unit tests | Entire project | Regression risk | High |
| Magic numbers in positions | `MiniViews.cs` | Confusing code | Low |
| Inconsistent error handling | Throughout | Silent failures | Medium |

### Low Priority

| Issue | Location | Impact | Effort |
|-------|----------|--------|--------|
| Mixed naming conventions | Throughout | Readability | Low |
| Unused code commented out | `FormMain.cs` | Clutter | Low |
| No logging/diagnostics | Throughout | Debugging difficulty | Medium |

---

## Recommendations Summary

### Immediate (Before Active Views)

1. **Parameterize all SQL** — Fix injection vulnerabilities
2. **Add schema versioning** — Prepare for Active Views migration

### Short-Term (With Active Views)

3. **Create ViewManager class** — Extract view logic from MiniViews.cs
4. **Create ViewDefinition model** — Data class for view configuration
5. **Expand miniviews schema** — Per design document

### Medium-Term (Post Active Views)

6. **Extract LogParser class** — Separate concern from FormMain
7. **Extract TimerGridManager** — Grid operations in one place
8. **Add basic error logging** — File-based diagnostics

### Long-Term

9. **Add unit tests** — Start with Database and ViewManager
10. **Consider MVP pattern** — Separate UI from logic properly
11. **Evaluate .NET Core migration** — Future-proofing

---

## Related Documents

- [active-views-design.md](./active-views-design.md) — Feature design
- [SCHEMA-MIGRATION.md](./SCHEMA-MIGRATION.md) — Database migration plan
- [TECHNICAL-DEBT.md](./TECHNICAL-DEBT.md) — Detailed debt tracking

---

*This analysis should be updated as refactoring progresses.*
