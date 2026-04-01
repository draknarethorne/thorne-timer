# Technical Debt Tracker

> **Last Updated:** 2026-03-27  
> **Purpose:** Track and prioritize technical debt for systematic resolution

---

## Summary

| Priority | Count | Estimated Effort |
|----------|-------|------------------|
| 🔴 High | 3 | ~20 hrs |
| 🟡 Medium | 4 | ~15 hrs |
| 🟢 Low | 3 | ~5 hrs |

---

## High Priority 🔴

### TD-001: SQL Injection Vulnerabilities

**Location:** `Database.cs`  
**Risk:** Security — malicious input could corrupt or extract database  
**Status:** Open

**Examples:**
```csharp
// SaveTimer() - line ~380
sql += "Name = '" + Convert.ToString(Name.Value) + "',";

// SaveCategory() - line ~730
sql += "Name = '" + Convert.ToString(Name.Value) + "', ";

// SetSetting() - line ~350
CommandText = "UPDATE settings SET " + column + " = '" + value + "'"
```

**Fix:** Replace all string concatenation with parameterized queries:
```csharp
cmd.CommandText = "UPDATE timers SET Name = @name WHERE ID = @id";
cmd.Parameters.AddWithValue("@name", Name.Value);
cmd.Parameters.AddWithValue("@id", ID.Value);
```

**Effort:** 4 hours  
**Priority:** Should fix before any public release

---

### TD-002: God Class FormMain.cs

**Location:** `FormMain.cs` (~1000+ lines)  
**Risk:** Maintainability — changes are error-prone, testing impossible  
**Status:** Open

**Responsibilities currently in FormMain:**
1. UI initialization and event wiring
2. Timer grid setup and CRUD operations
3. Character grid setup and CRUD operations  
4. Category grid setup and CRUD operations
5. Log file parsing (background thread)
6. Timer triggering and countdown logic
7. Mini view coordination
8. Settings management
9. Voice/sound playback
10. Window position persistence

**Fix:** Extract into focused classes:

| New Class | Responsibility | Extract From |
|-----------|----------------|--------------|
| `LogParser.cs` | Log file watching, keyword matching | `ParseLog()`, `ProcessLogEntry()` |
| `TimerGridManager.cs` | Timer DataGridView operations | `SetupTimerGrid()`, `SaveDataTimers()`, etc. |
| `ViewManager.cs` | Mini view lifecycle | Already planned for Active Views |
| `SoundManager.cs` | WAV playback, TTS | Sound-related methods |

**Effort:** 10 hours  
**Priority:** Critical for Active Views work — do incrementally

---

### TD-003: Hardcoded Mini Views

**Location:** `MiniViews.cs` lines 33-36  
**Risk:** Feature limitation — users cannot customize views  
**Status:** In Progress (Active Views feature)

**Current State:**
```csharp
private MiniView miniView = null;
private MiniView petView = null;
private MiniView buffView = null;
private MiniView pingView = null;
```

**Fix:** See [active-views-design.md](./active-views-design.md)

**Effort:** 10 hours  
**Priority:** Primary feature work

---

## Medium Priority 🟡

### TD-004: Fragile Schema Migrations

**Location:** `Database.Connection()` (~200 lines of migration code)  
**Risk:** Reliability — upgrade failures, inconsistent state  
**Status:** Planned (part of Active Views)

**Current Pattern:**
```csharp
if (!isFieldExist(con, "settings", "ActiveVoice"))
{
    // Add column and update
}
if (!isFieldExist(con, "settings", "MiniViewFontSize"))
{
    // Add column and update
}
// ... repeated 15+ times
```

**Fix:** Implement schema versioning — see [SCHEMA-MIGRATION.md](./SCHEMA-MIGRATION.md)

**Effort:** 4 hours  
**Priority:** Required for Active Views

---

### TD-005: No Unit Tests

**Location:** Entire project  
**Risk:** Regression — changes may break existing functionality silently  
**Status:** Open

**Impact Areas:**
- Database operations (CRUD)
- Timer calculations
- Log parsing/keyword matching
- View filtering logic

**Fix:** Add test project with xUnit or MSTest:
1. Start with `Database.cs` tests (isolated, pure functions)
2. Add `TimerPlus.cs` tests (time calculations)
3. Add `ViewManager.cs` tests (when created)

**Effort:** 8 hours (initial setup + core tests)  
**Priority:** Important for sustainable development

---

### TD-006: Inconsistent Error Handling

**Location:** Throughout codebase  
**Risk:** Reliability — silent failures, confusing behavior  
**Status:** Open

**Examples:**
```csharp
// Silent catch blocks
catch { }

// Catch-all with no logging
catch (Exception) { continue; }

// No validation before operations
if (!int.TryParse(ID, out int idValue)) return;  // Silent failure
```

**Fix:** 
1. Add logging framework (NLog or Serilog)
2. Replace empty catches with logged warnings
3. Add user-facing error messages for common failures

**Effort:** 4 hours  
**Priority:** Improves debugging and user experience

---

### TD-007: Magic Numbers

**Location:** `MiniViews.cs`, `FormMain.cs`  
**Risk:** Readability — hard to understand intent  
**Status:** Open

**Examples:**
```csharp
// View positions
petView = CreateMiniView(character.MiniViewX + 200, character.MiniViewY);
buffView = CreateMiniView(character.MiniViewX + 400, character.MiniViewY);
pingView = CreateMiniView(character.MiniViewX + 1000, character.MiniViewY);

// Time calculations
if ((currTime.Subtract(lastTime).TotalMilliseconds) > 999)

// Color defaults
Color.White.ToArgb()  // Used in many places without constants
```

**Fix:** Extract to named constants:
```csharp
private const int VIEW_HORIZONTAL_SPACING = 200;
private const int UPDATE_THROTTLE_MS = 1000;
```

**Effort:** 2 hours  
**Priority:** Low effort, high readability gain

---

## Low Priority 🟢

### TD-008: Dead/Commented Code

**Location:** `FormMain.cs`  
**Risk:** Clutter — confusing for maintainers  
**Status:** Open

**Examples:**
```csharp
// grdTimers.CellValueChanged += ...
// grdTimers.CurrentCellDirtyStateChanged += ...

void grdTimers_CurrentCellDirtyStateChanged(object sender, EventArgs e)
{
    //if (grdTimers.IsCurrentCellDirty)
    //{
    //    // This fires the cell value changed handler below
    //    grdTimers.CommitEdit(DataGridViewDataErrorContexts.Commit);
    //}
}
```

**Fix:** Remove commented code or add TODO explaining why it's preserved

**Effort:** 1 hour  
**Priority:** Quick cleanup

---

### TD-009: Inconsistent Naming

**Location:** Throughout  
**Risk:** Readability  
**Status:** Open

**Examples:**
- `mvOpacity` vs `FormOpacity` (inconsistent prefix)
- `btnStartStopLog` vs `buttonStopAll` (inconsistent casing)
- `grdTimers` vs `dataGridView` parameter names

**Fix:** Establish naming conventions in `STANDARDS.md` and apply consistently

**Effort:** 2 hours  
**Priority:** Nice to have

---

### TD-010: No Logging/Diagnostics

**Location:** Entire project  
**Risk:** Debugging difficulty  
**Status:** Open

**Current State:** No logging framework, only `Debug.WriteLine` in a few places

**Fix:** 
1. Add NLog or Serilog
2. Log key events: startup, timer triggers, errors
3. Add log file rotation

**Effort:** 3 hours  
**Priority:** Helpful for support/debugging

---

## Resolution Plan

### Phase 1: Pre-Active-Views
- [ ] TD-001: Fix SQL injection (required for security)
- [ ] TD-004: Add schema versioning (required for migration)

### Phase 2: With Active Views  
- [ ] TD-003: Replace hardcoded views (primary feature)
- [ ] TD-002: Extract ViewManager (partial)

### Phase 3: Post-Active-Views
- [ ] TD-002: Extract remaining classes
- [ ] TD-005: Add unit tests
- [ ] TD-006: Add logging

### Phase 4: Polish
- [ ] TD-007: Replace magic numbers
- [ ] TD-008: Remove dead code
- [ ] TD-009: Standardize naming
- [ ] TD-010: Add diagnostics

---

## Tracking

| ID | Status | Resolved Date | Notes |
|----|--------|---------------|-------|
| TD-001 | Open | — | |
| TD-002 | Open | — | Partial with Active Views |
| TD-003 | In Progress | — | Active Views feature |
| TD-004 | Planned | — | |
| TD-005 | Open | — | |
| TD-006 | Open | — | |
| TD-007 | Open | — | |
| TD-008 | Open | — | |
| TD-009 | Open | — | |
| TD-010 | Open | — | |

---

*Update this document as debt is identified or resolved.*
