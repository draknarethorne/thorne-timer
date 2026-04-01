# Active Views Design

> **Status:** Planning  
> **Last Updated:** 2026-03-27  
> **Author:** Draknaré Thorne

---

## Executive Summary

Transform the current **hardcoded 4-view system** (Normal, Pet, Buff, Ping) into a **user-configurable view system** where each view can be linked to categories, individual timers, or timer types. This enables flexible timer organization and supports multiple user workflows including multi-boxing and raid scenarios.

---

## Current State Analysis

### The Problem: Hardcoded Views

In `MiniViews.cs` (lines 33-36), four view instances are statically defined:

```csharp
private MiniView miniView = null;   // Normal timers
private MiniView petView = null;    // Pet timers  
private MiniView buffView = null;   // Buff timers
private MiniView pingView = null;   // Ping timers
```

These are created with **fixed relative positions** in `CreateMiniViews()`:

```csharp
miniView = CreateMiniView(character.MiniViewX, character.MiniViewY);
petView = CreateMiniView(character.MiniViewX + 200, character.MiniViewY);
buffView = CreateMiniView(character.MiniViewX + 400, character.MiniViewY);
pingView = CreateMiniView(character.MiniViewX + 1000, character.MiniViewY);
```

Timer routing in `UpdateMiniTimers()` is **type-based, not configurable**:

```csharp
if (Timers.PetTimer(...)) petData.Add(md);
else if (Timers.BuffTimer(...)) buffData.Add(md);
else if (Timers.PingTimer(...)) pingData.Add(md);
else miniData.Add(md);
```

### Current Database Schema

| Table | Status | Notes |
|-------|--------|-------|
| `timers` | ✅ Solid | Timer definitions with keywords, duration, sounds |
| `characters` | ✅ Works | Includes single MiniViewX/Y position |
| `categories` | ✅ Works | Timer groupings with start/end keywords |
| `settings` | ✅ Works | Global app settings including view appearance |
| `miniviews` | ⚠️ **Incomplete** | Only has `ID` and `Name` — not connected to views |

### Code Organization Issues

- `FormMain.cs` is ~1000+ lines and growing — contains ALL timer logic, log parsing, UI setup
- `MiniViews.cs` manages hardcoded views but lacks abstraction for dynamic views
- No separation between view definition (data) and view rendering (UI)

---

## Desired State

1. **User-defined views** — Create, name, position, and configure any number of overlay windows
2. **Flexible filtering** — Views can show timers by category, by type, or by explicit selection
3. **Per-character views** — Each character can have their own view layout
4. **Global views** — Some views can be shared across all characters
5. **Preserved legacy behavior** — Auto-create the 4 default views on migration

---

## Data Model

### View Definition

```
ViewDefinition
├── ID (int)                         — Primary key
├── Name (string)                    — "Spell Timers", "Pet Buffs", etc.
├── CharacterID (int, nullable)      — NULL = global, set = character-specific
├── FilterType (string)              — 'Category', 'Type', 'Manual', 'All'
├── FilterValue (string)             — CategoryID, Type name, or comma-separated timer IDs
├── PositionX (int)                  — Window X position
├── PositionY (int)                  — Window Y position
├── Width (int)                      — 0 = auto-size
├── Height (int)                     — 0 = auto-size
├── SortOrder (int)                  — Display/creation order
├── IsVisible (bool)                 — Show/hide toggle
├── ColorScheme (string)             — 'Normal', 'Pet', 'Buff', 'Ping', 'Custom'
└── CustomColors (string, nullable)  — JSON for custom fore/back colors
```

### Filter Types Explained

| FilterType | FilterValue Example | Behavior |
|------------|---------------------|----------|
| `Category` | `3` | Show all timers in category ID 3 |
| `Type` | `Pet` | Show timers with StartStop = "Pet" (legacy) |
| `Manual` | `1,5,12,24` | Show specific timer IDs |
| `All` | (empty) | Show all active timers |

### Schema Migration SQL

```sql
-- Drop and recreate miniviews with full schema
DROP TABLE IF EXISTS miniviews;

CREATE TABLE miniviews (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    CharacterID INTEGER,
    FilterType TEXT DEFAULT 'Category',
    FilterValue TEXT,
    PositionX INTEGER DEFAULT 100,
    PositionY INTEGER DEFAULT 100,
    Width INTEGER DEFAULT 0,
    Height INTEGER DEFAULT 0,
    SortOrder INTEGER DEFAULT 0,
    IsVisible INTEGER DEFAULT 1,
    ColorScheme TEXT DEFAULT 'Normal',
    CustomColors TEXT,
    FOREIGN KEY (CharacterID) REFERENCES characters(ID) ON DELETE CASCADE
);

-- Junction table for manual timer selection (future use)
CREATE TABLE IF NOT EXISTS view_timers (
    ViewID INTEGER NOT NULL,
    TimerID INTEGER NOT NULL,
    PRIMARY KEY (ViewID, TimerID),
    FOREIGN KEY (ViewID) REFERENCES miniviews(ID) ON DELETE CASCADE,
    FOREIGN KEY (TimerID) REFERENCES timers(ID) ON DELETE CASCADE
);

-- Create legacy views for migration
INSERT INTO miniviews (Name, FilterType, FilterValue, PositionX, PositionY, ColorScheme, SortOrder)
VALUES 
    ('Normal Timers', 'Type', 'Normal', 100, 100, 'Normal', 1),
    ('Pet Timers', 'Type', 'Pet', 300, 100, 'Pet', 2),
    ('Buff Timers', 'Type', 'Buff', 500, 100, 'Buff', 3),
    ('Ping Timers', 'Type', 'Ping', 700, 100, 'Ping', 4);
```

---

## Architecture

### New Classes

```
ViewManager.cs (new)
├── LoadViews(characterId) → List<ViewDefinition>
├── CreateView(ViewDefinition) → int (new ID)
├── UpdateView(ViewDefinition)
├── DeleteView(viewId)
├── GetTimersForView(viewId, allTimers) → List<TimerData>
├── SaveViewPositions()
└── Events: ViewCreated, ViewDeleted, ViewUpdated

ViewDefinition.cs (new)
├── Properties matching database columns
├── GetFilteredTimers(allTimers) → List<TimerData>
└── Clone() for editing

MiniViewWindow.cs (rename from MiniView.cs)
├── ViewDefinition Definition { get; set; }
├── LoadData(List<MiniData>)
├── ApplyColorScheme(scheme)
└── (existing rendering logic)
```

### Class Relationships

```
FormMain
    └── ViewManager (new)
            ├── Dictionary<int, MiniViewWindow> _activeWindows
            ├── List<ViewDefinition> _definitions
            └── Database (existing)
                    └── miniviews table (expanded)
```

### Data Flow

```
1. FormMain.Load()
   └── ViewManager.LoadViews(characterId)
       └── Database.GetViews() → List<ViewDefinition>
           └── For each: CreateMiniViewWindow(definition)

2. Timer Tick / Log Event
   └── ViewManager.UpdateAllViews(grdTimers)
       └── For each view:
           ├── GetTimersForView() → filtered timers
           └── MiniViewWindow.LoadData(filtered)

3. User drags/repositions window
   └── MiniViewWindow.LocationChanged
       └── ViewManager.SaveViewPosition(viewId, x, y)
           └── Database.UpdateViewPosition()
```

---

## Implementation Phases

### Phase 1: Foundation (Current Sprint)

**Goal:** Replace hardcoded views with database-driven views while maintaining existing behavior.

| Task | Effort | Notes |
|------|--------|-------|
| Create `ViewDefinition.cs` | 1hr | Data model class |
| Expand `miniviews` schema | 1hr | Migration in `Database.Connection()` |
| Create `ViewManager.cs` | 3hr | Core view management logic |
| Update `MiniViews.cs` → use ViewManager | 2hr | Bridge to new system |
| Auto-create legacy views on migration | 1hr | Preserve user experience |
| Test: views load, position, filter | 2hr | Regression testing |

**Deliverable:** Views are loaded from database, positioned correctly, filter by type (legacy behavior).

### Phase 2: UI Integration

**Goal:** Add Views tab to main form for user configuration.

| Task | Effort | Notes |
|------|--------|-------|
| Add `tabViews` tab to FormMain | 1hr | Already exists but basic |
| Create views grid with full columns | 2hr | Name, Filter, Position, Visible |
| Add/Delete/Edit view functionality | 2hr | Grid inline editing |
| "Save Position" button per view | 1hr | Captures current window location |
| Category dropdown for filter | 1hr | Links to existing categories |

**Deliverable:** Users can create, edit, delete views from the UI.

### Phase 3: Per-Character Views

**Goal:** Allow each character to have their own view layout.

| Task | Effort | Notes |
|------|--------|-------|
| Add CharacterID filter to view queries | 1hr | NULL = global |
| Load views when character changes | 1hr | Destroy old, create new |
| "Copy views from character" feature | 2hr | Quality of life |
| Global vs character toggle in UI | 1hr | Checkbox or dropdown |

**Deliverable:** Switching characters loads that character's view layout.

### Phase 4: Advanced Features

**Goal:** Manual timer selection, custom colors, view templates.

| Task | Effort | Notes |
|------|--------|-------|
| Manual timer picker dialog | 3hr | Multi-select from timer list |
| Custom color picker per view | 2hr | Override scheme colors |
| View templates (save/load) | 2hr | Export/import layouts |
| View presets (raid, solo, boxing) | 2hr | Quick-switch configurations |

---

## Migration Strategy

### First Run After Update

1. Check if `miniviews` table has new columns
2. If not, run schema migration
3. Check if any views exist
4. If no views, auto-create 4 legacy views:
   - Normal Timers (Type=Normal, ColorScheme=Normal)
   - Pet Timers (Type=Pet, ColorScheme=Pet)
   - Buff Timers (Type=Buff, ColorScheme=Buff)
   - Ping Timers (Type=Ping, ColorScheme=Ping)
5. If character has saved MiniViewX/Y, apply to first view

### Backward Compatibility

- Existing `character.MiniViewX/Y` still used as default position for first view
- Color settings in `settings` table become defaults for "Normal" color scheme
- No data loss — only additive changes

---

## Open Questions (Resolved)

| Question | Decision |
|----------|----------|
| Views filter by category or individual timers? | **Both** — FilterType determines behavior |
| Per-character or global? | **Both** — CharacterID=NULL means global |
| Dialog or inline editing? | **Inline first** — consistent with existing UI |
| How to handle color schemes? | **Named schemes** + custom override option |

---

## Related Documents

- [CODEBASE-ANALYSIS.md](./CODEBASE-ANALYSIS.md) — Full code review and technical debt
- [SCHEMA-MIGRATION.md](./SCHEMA-MIGRATION.md) — Database migration details

---

## Revision History

| Date | Changes |
|------|---------|
| 2026-03-27 | Major rewrite: added data model, schema, phases, migration strategy |
| 2026-03-XX | Initial design document created |

---

*This document is the authoritative design reference for the Active Views feature.*
