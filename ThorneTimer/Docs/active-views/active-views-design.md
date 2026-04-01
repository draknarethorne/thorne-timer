# Active Views Design

> **Status:** Planning  
> **Last Updated:** 2026-03-27  
> **Author:** Draknaré Thorne  
> **Branch:** `active-views`

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current State Analysis](#current-state-analysis)
3. [Desired State](#desired-state)
4. [Data Model](#data-model)
5. [Architecture](#architecture)
6. [Implementation Phases](#implementation-phases)
7. [Error Handling & Edge Cases](#error-handling--edge-cases)
8. [GUI Redesign Foundation](#gui-redesign-foundation)
9. [Migration Strategy](#migration-strategy)
10. [Testing Strategy](#testing-strategy)
11. [Related Documents](#related-documents)

---

## Executive Summary

Transform the current **hardcoded 4-view system** (Normal, Pet, Buff, Ping) into a **user-configurable view system** where each view can be linked to categories, individual timers, or timer types. This enables flexible timer organization and supports multiple user workflows including multi-boxing and raid scenarios.

### Design Goals

| Goal | Rationale |
|------|-----------|
| **Backward compatible** | Existing users should see no change on upgrade — legacy views auto-created |
| **Incrementally adoptable** | Each phase delivers value; no "big bang" rewrite required |
| **GUI-ready foundation** | Data model and ViewManager will support future GUI overhaul |
| **Testable in isolation** | ViewManager can be unit tested without UI dependencies |
| **Extensible** | Future features (templates, presets, sharing) have clear extension points |

### Key Decisions Made

| Decision | Choice | Why |
|----------|--------|-----|
| Storage | SQLite `miniviews` table | Consistent with existing data layer; supports schema versioning |
| Filter types | Enum-like string | Easy to add new types; readable in database |
| Color schemes | Named + custom | Preserves existing 4-color system while enabling customization |
| View ownership | CharacterID (nullable) | NULL = global view, set = character-specific |
| Position tracking | Per-view X/Y | Each view independently positioned (replaces single MiniViewX/Y) |

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

### C# Implementation

```csharp
/// <summary>
/// Represents a user-configurable timer overlay window definition.
/// This is the core data model that replaces hardcoded views.
/// </summary>
/// <remarks>
/// Design notes:
/// - FilterType uses string instead of enum for database compatibility and extensibility
/// - CustomColors is JSON to allow future expansion (gradients, themes, etc.)
/// - CharacterID=null means "global view" visible to all characters
/// </remarks>
public class ViewDefinition
{
    public int ID { get; set; }
    public string Name { get; set; } = "New View";

    /// <summary>
    /// Owner character. NULL = global (all characters see this view).
    /// </summary>
    public int? CharacterID { get; set; }

    /// <summary>
    /// How this view filters timers: Category, Type, Manual, or All.
    /// </summary>
    public string FilterType { get; set; } = "Category";

    /// <summary>
    /// Filter-specific value:
    /// - Category: category ID as string
    /// - Type: "Normal", "Pet", "Buff", or "Ping"  
    /// - Manual: comma-separated timer IDs
    /// - All: ignored (empty string)
    /// </summary>
    public string FilterValue { get; set; } = "";

    // Position & Size (0 = auto for width/height)
    public int PositionX { get; set; } = 100;
    public int PositionY { get; set; } = 100;
    public int Width { get; set; } = 0;
    public int Height { get; set; } = 0;

    public int SortOrder { get; set; } = 0;
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Named color scheme: Normal, Pet, Buff, Ping, Custom.
    /// Maps to existing settings table colors.
    /// </summary>
    public string ColorScheme { get; set; } = "Normal";

    /// <summary>
    /// JSON object for custom colors when ColorScheme = "Custom".
    /// Format: {"ForeColor":"#FFFFFF","BackColor":"#000000","WarningColor":"#FF0000"}
    /// </summary>
    public string CustomColors { get; set; }

    /// <summary>
    /// Creates a deep copy for editing without affecting the original.
    /// Used by UI when user edits a view but hasn't saved yet.
    /// </summary>
    public ViewDefinition Clone() => new ViewDefinition
    {
        ID = this.ID,
        Name = this.Name,
        CharacterID = this.CharacterID,
        FilterType = this.FilterType,
        FilterValue = this.FilterValue,
        PositionX = this.PositionX,
        PositionY = this.PositionY,
        Width = this.Width,
        Height = this.Height,
        SortOrder = this.SortOrder,
        IsVisible = this.IsVisible,
        ColorScheme = this.ColorScheme,
        CustomColors = this.CustomColors
    };

    /// <summary>
    /// Validates the view definition before save.
    /// Returns error message or null if valid.
    /// </summary>
    public string Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "View name is required.";
        if (Name.Length > 50)
            return "View name must be 50 characters or less.";
        if (!new[] { "Category", "Type", "Manual", "All" }.Contains(FilterType))
            return $"Invalid filter type: {FilterType}";
        return null; // Valid
    }
}
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

### New Classes Overview

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

### ViewManager Implementation

```csharp
/// <summary>
/// Central coordinator for all view operations.
/// Replaces the hardcoded 4-view logic in MiniViews.cs.
/// </summary>
/// <remarks>
/// Design principles:
/// - Single responsibility: manages view lifecycle only
/// - FormMain delegates all view operations here
/// - Events allow UI to respond without tight coupling
/// - Testable: can inject mock Database for unit tests
/// </remarks>
public class ViewManager : IDisposable
{
    private readonly Database _db;
    private readonly Dictionary<int, MiniViewWindow> _activeWindows = new Dictionary<int, MiniViewWindow>();
    private List<ViewDefinition> _definitions = new List<ViewDefinition>();
    private int? _currentCharacterId;

    // Events for UI binding (future GUI redesign will use these)
    public event EventHandler<ViewDefinition> ViewCreated;
    public event EventHandler<int> ViewDeleted;
    public event EventHandler<ViewDefinition> ViewUpdated;
    public event EventHandler ViewsReloaded;

    public ViewManager(Database db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Loads all views for a character (including global views).
    /// Call this when character selection changes.
    /// </summary>
    /// <param name="characterId">Character ID, or null for global-only</param>
    public void LoadViews(int? characterId)
    {
        // Close existing windows before loading new set
        CloseAllWindows();

        _currentCharacterId = characterId;

        // Load global views (CharacterID IS NULL) + character-specific views
        _definitions = _db.GetViews(characterId);

        // If no views exist (first run or migration), create legacy defaults
        if (_definitions.Count == 0)
        {
            CreateLegacyViews(characterId);
            _definitions = _db.GetViews(characterId);
        }

        // Create window for each visible view
        foreach (var def in _definitions.Where(d => d.IsVisible))
        {
            CreateWindow(def);
        }

        ViewsReloaded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Updates all view windows with current timer data.
    /// Call this from the main timer tick.
    /// </summary>
    /// <param name="allTimers">All active timers from grdTimers</param>
    public void UpdateAllViews(IEnumerable<TimerRowData> allTimers)
    {
        var timerList = allTimers.ToList();

        foreach (var kvp in _activeWindows)
        {
            var def = _definitions.FirstOrDefault(d => d.ID == kvp.Key);
            if (def == null) continue;

            var filtered = FilterTimers(def, timerList);
            kvp.Value.LoadData(filtered);
        }
    }

    /// <summary>
    /// Filters timers based on view's FilterType and FilterValue.
    /// This is the core routing logic that replaces hardcoded if/else chains.
    /// </summary>
    private List<MiniData> FilterTimers(ViewDefinition view, List<TimerRowData> allTimers)
    {
        IEnumerable<TimerRowData> filtered;

        switch (view.FilterType)
        {
            case "Category":
                // Filter by category ID
                if (int.TryParse(view.FilterValue, out int catId))
                    filtered = allTimers.Where(t => t.CategoryID == catId);
                else
                    filtered = Enumerable.Empty<TimerRowData>();
                break;

            case "Type":
                // Legacy type filtering: Normal, Pet, Buff, Ping
                filtered = allTimers.Where(t => 
                    GetTimerType(t.StartStop) == view.FilterValue);
                break;

            case "Manual":
                // Explicit timer IDs: "1,5,12,24"
                var ids = view.FilterValue
                    .Split(',')
                    .Select(s => int.TryParse(s.Trim(), out int id) ? id : -1)
                    .Where(id => id > 0)
                    .ToHashSet();
                filtered = allTimers.Where(t => ids.Contains(t.ID));
                break;

            case "All":
            default:
                filtered = allTimers;
                break;
        }

        return filtered.Select(t => new MiniData(t)).ToList();
    }

    /// <summary>
    /// Maps StartStop value to legacy type name.
    /// Bridges old Timers.PetTimer/BuffTimer/PingTimer logic.
    /// </summary>
    private string GetTimerType(string startStop)
    {
        // TODO: Refactor when Timers.cs constants are cleaned up
        if (startStop == Timers.btnPet) return "Pet";
        if (startStop == Timers.btnBuff) return "Buff";
        if (startStop == Timers.btnPing) return "Ping";
        return "Normal";
    }

    // ... Additional methods: CreateView, DeleteView, SaveViewPosition, etc.

    public void Dispose()
    {
        CloseAllWindows();
    }

    private void CloseAllWindows()
    {
        foreach (var window in _activeWindows.Values)
        {
            window.Close();
            window.Dispose();
        }
        _activeWindows.Clear();
    }
}
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

## Error Handling & Edge Cases

### Failure Scenarios

| Scenario | Handling | User Experience |
|----------|----------|-----------------|
| **Database unavailable** | Log error, fall back to hardcoded 4 views | App still works, views just aren't saved |
| **Schema migration fails** | Rollback transaction, log error, continue with old schema | No data loss, feature disabled until fixed |
| **View window creation fails** | Skip view, log error, continue with others | Partial functionality, error in logs |
| **Invalid FilterValue** | Treat as "All" filter | View shows all timers (safe default) |
| **Duplicate view names** | Allow (not unique constraint) | User can name views identically |
| **Character deleted** | CASCADE delete views | Views cleaned up automatically |
| **Timer deleted from Manual filter** | Ignore missing ID | View just shows fewer timers |

### Defensive Code Patterns

```csharp
/// <summary>
/// Safely loads views with fallback behavior.
/// Never throws — logs errors and degrades gracefully.
/// </summary>
public void LoadViewsSafe(int? characterId)
{
    try
    {
        LoadViews(characterId);
    }
    catch (SQLiteException ex)
    {
        // Database error — fall back to legacy behavior
        Logger.Error($"Failed to load views: {ex.Message}");
        CreateLegacyViewsInMemory();
    }
    catch (Exception ex)
    {
        // Unexpected error — log and continue with empty views
        Logger.Error($"Unexpected error loading views: {ex.Message}");
        _definitions.Clear();
        _activeWindows.Clear();
    }
}

/// <summary>
/// Validates view position is within screen bounds.
/// Resets to default if window would be off-screen.
/// </summary>
private void ValidatePosition(ViewDefinition view)
{
    var screenBounds = Screen.PrimaryScreen.WorkingArea;

    // Ensure at least 50px of window is visible
    const int minVisible = 50;

    if (view.PositionX < -screenBounds.Width + minVisible)
        view.PositionX = 100;
    if (view.PositionY < -screenBounds.Height + minVisible)
        view.PositionY = 100;
    if (view.PositionX > screenBounds.Width - minVisible)
        view.PositionX = screenBounds.Width - 300;
    if (view.PositionY > screenBounds.Height - minVisible)
        view.PositionY = screenBounds.Height - 200;
}
```

### Multi-Monitor Considerations

```csharp
/// <summary>
/// Handles window position restoration across monitor configuration changes.
/// Called when views are loaded to ensure windows are visible.
/// </summary>
private void EnsureWindowsVisible()
{
    var allScreens = Screen.AllScreens;

    foreach (var kvp in _activeWindows)
    {
        var window = kvp.Value;
        var windowRect = new Rectangle(window.Location, window.Size);

        // Check if window is on any active monitor
        bool isVisible = allScreens.Any(s => 
            s.WorkingArea.IntersectsWith(windowRect));

        if (!isVisible)
        {
            // Window is off-screen (monitor removed?) — move to primary
            window.Location = new Point(100, 100);
            SaveViewPosition(kvp.Key, 100, 100);
            Logger.Info($"Moved view '{window.Text}' to primary monitor (was off-screen)");
        }
    }
}
```

---

## GUI Redesign Foundation

> **Purpose:** This section documents how the Active Views architecture will support the planned GUI overhaul.

### Current GUI Limitations

| Issue | Impact | Solution Path |
|-------|--------|---------------|
| `FormMain.cs` monolith | Can't test UI logic in isolation | ViewManager extracts view logic |
| Hardcoded tab structure | Adding features requires modifying FormMain | Future: plugin/module architecture |
| Direct database calls in event handlers | Tight coupling, hard to mock | ViewManager abstracts data access |
| No MVVM/MVP pattern | Can't swap UI frameworks | Events on ViewManager enable binding |

### Architecture for Future GUI

```
┌─────────────────────────────────────────────────────────────────┐
│                         Future Architecture                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   ┌──────────────┐    Events     ┌────────────────────────────┐ │
│   │   WinForms   │◄─────────────►│      ViewManager           │ │
│   │  (current)   │               │  (Phase 1 deliverable)     │ │
│   └──────────────┘               │                            │ │
│         or                       │  • LoadViews()             │ │
│   ┌──────────────┐               │  • UpdateAllViews()        │ │
│   │    WPF       │◄─────────────►│  • CreateView()            │ │
│   │  (future?)   │               │  • DeleteView()            │ │
│   └──────────────┘               │  • Events for UI binding   │ │
│         or                       └────────────┬───────────────┘ │
│   ┌──────────────┐                            │                 │
│   │   MAUI       │◄───────────────────────────┤                 │
│   │  (future?)   │                            ▼                 │
│   └──────────────┘               ┌────────────────────────────┐ │
│                                  │      Database              │ │
│                                  │  (SQLite, unchanged)       │ │
│                                  └────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Event-Driven Design for UI Binding

```csharp
// ViewManager events enable reactive UI patterns
public class ViewManager
{
    // Fired when a view is created — UI can add to list/grid
    public event EventHandler<ViewDefinition> ViewCreated;

    // Fired when a view is deleted — UI can remove from list/grid
    public event EventHandler<int> ViewDeleted;

    // Fired when a view property changes — UI can refresh
    public event EventHandler<ViewDefinition> ViewUpdated;

    // Fired when views are reloaded (character change) — UI can rebind
    public event EventHandler ViewsReloaded;

    // Example: Phase 2 UI binding
    // grdViews.DataSource = viewManager.Definitions; // INotifyCollectionChanged
    // viewManager.ViewUpdated += (s, v) => grdViews.Refresh();
}
```

### UI Component Roadmap

| Phase | Component | Description |
|-------|-----------|-------------|
| **1** | ViewManager | Core logic, no UI changes yet |
| **2** | Views Tab | Grid-based editing in existing FormMain |
| **3** | View Editor Dialog | Modal dialog for advanced view config |
| **4** | Context Menus | Right-click on MiniView to edit inline |
| **Future** | Dockable Panels | Modernize to VS-style docking |
| **Future** | Settings Dialog | Unified settings with categories |

### Preparing for MVVM (Future)

While current WinForms architecture doesn't support full MVVM, we can prepare:

```csharp
/// <summary>
/// ViewModel-like wrapper for ViewDefinition.
/// Can be used directly with WinForms binding, or
/// adapted to INotifyPropertyChanged for WPF/MAUI later.
/// </summary>
public class ViewDefinitionViewModel
{
    private readonly ViewDefinition _model;
    private readonly ViewManager _manager;

    public ViewDefinitionViewModel(ViewDefinition model, ViewManager manager)
    {
        _model = model;
        _manager = manager;
    }

    // Properties delegate to model
    public string Name
    {
        get => _model.Name;
        set
        {
            if (_model.Name != value)
            {
                _model.Name = value;
                _manager.UpdateView(_model);
                // In WPF: PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
    }

    // Commands for UI actions
    public void Delete() => _manager.DeleteView(_model.ID);
    public void MoveUp() => _manager.ReorderView(_model.ID, -1);
    public void MoveDown() => _manager.ReorderView(_model.ID, +1);
}
```

---

## Testing Strategy

### Unit Tests (Phase 1)

```csharp
[TestClass]
public class ViewDefinitionTests
{
    [TestMethod]
    public void Validate_EmptyName_ReturnsError()
    {
        var view = new ViewDefinition { Name = "" };
        Assert.IsNotNull(view.Validate());
        Assert.IsTrue(view.Validate().Contains("name"));
    }

    [TestMethod]
    public void Validate_ValidView_ReturnsNull()
    {
        var view = new ViewDefinition 
        { 
            Name = "Test View",
            FilterType = "Category",
            FilterValue = "1"
        };
        Assert.IsNull(view.Validate());
    }

    [TestMethod]
    public void Clone_CreatesIndependentCopy()
    {
        var original = new ViewDefinition { Name = "Original", PositionX = 100 };
        var clone = original.Clone();

        clone.Name = "Modified";
        clone.PositionX = 200;

        Assert.AreEqual("Original", original.Name);
        Assert.AreEqual(100, original.PositionX);
    }
}

[TestClass]
public class ViewManagerTests
{
    private MockDatabase _mockDb;
    private ViewManager _manager;

    [TestInitialize]
    public void Setup()
    {
        _mockDb = new MockDatabase();
        _manager = new ViewManager(_mockDb);
    }

    [TestMethod]
    public void LoadViews_NoExistingViews_CreatesLegacyViews()
    {
        _mockDb.SetupEmptyViews();

        _manager.LoadViews(characterId: 1);

        Assert.AreEqual(4, _manager.Definitions.Count);
        Assert.IsTrue(_manager.Definitions.Any(v => v.Name == "Normal Timers"));
    }

    [TestMethod]
    public void FilterTimers_CategoryFilter_ReturnsOnlyMatchingCategory()
    {
        var view = new ViewDefinition 
        { 
            FilterType = "Category", 
            FilterValue = "3" 
        };
        var timers = new List<TimerRowData>
        {
            new TimerRowData { ID = 1, CategoryID = 3 },
            new TimerRowData { ID = 2, CategoryID = 5 },
            new TimerRowData { ID = 3, CategoryID = 3 }
        };

        var filtered = _manager.FilterTimers(view, timers);

        Assert.AreEqual(2, filtered.Count);
        Assert.IsTrue(filtered.All(t => t.CategoryID == 3));
    }
}
```

### Integration Tests (Phase 2)

| Test | Validates |
|------|-----------|
| Create view → appears in grid | UI binding works |
| Delete view → window closes | Cleanup occurs |
| Reposition window → save position | Position persists |
| Switch character → views reload | Character isolation works |
| Schema migration → legacy views exist | Migration doesn't break existing users |

### Manual Test Checklist

- [ ] Fresh install creates 4 default views
- [ ] Upgrade from previous version preserves positions
- [ ] Views filter timers correctly by category
- [ ] Views filter timers correctly by type (legacy)
- [ ] Window positions persist across app restart
- [ ] Window positions handle multi-monitor correctly
- [ ] Switching characters loads different view sets
- [ ] Global views appear for all characters
- [ ] Deleting a character cascades to views

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

- [codebase-analysis.md](./codebase-analysis.md) — Full code review and technical debt
- [schema-migration.md](./schema-migration.md) — Database migration details
- [technical-debt.md](./technical-debt.md) — Technical debt tracking

---

## Revision History

| Date | Changes |
|------|---------|
| 2026-03-27 | Added Error Handling, GUI Redesign Foundation, Testing Strategy sections |
| 2026-03-27 | Major rewrite: added data model, ViewManager implementation, phases, migration strategy |
| 2026-03-XX | Initial design document created |

---

## Appendix: Quick Reference

### File Changes Summary

| File | Change Type | Notes |
|------|-------------|-------|
| `ViewDefinition.cs` | **NEW** | Data model for views |
| `ViewManager.cs` | **NEW** | Core view management logic |
| `MiniView.cs` | **RENAME** | → `MiniViewWindow.cs` |
| `MiniViews.cs` | **MODIFY** | Delegates to ViewManager |
| `Database.cs` | **MODIFY** | Add view CRUD methods |
| `FormMain.cs` | **MODIFY** | Use ViewManager instead of direct MiniViews |

### Database Changes Summary

```sql
-- New schema for miniviews table
ALTER TABLE miniviews ADD COLUMN CharacterID INTEGER;
ALTER TABLE miniviews ADD COLUMN FilterType TEXT DEFAULT 'Category';
ALTER TABLE miniviews ADD COLUMN FilterValue TEXT;
ALTER TABLE miniviews ADD COLUMN PositionX INTEGER DEFAULT 100;
-- ... (see Schema Migration SQL above for complete)

-- New junction table
CREATE TABLE view_timers (ViewID, TimerID, PRIMARY KEY);
```

### Key Integration Points

```csharp
// FormMain.cs — key changes
private ViewManager _viewManager;

private void FormMain_Load(object sender, EventArgs e)
{
    _viewManager = new ViewManager(Database);
    _viewManager.ViewsReloaded += (s, e) => UpdateViewsGrid();
    _viewManager.LoadViews(CurrentCharacter?.ID);
}

private void tmrMain_Tick(object sender, EventArgs e)
{
    // ... existing timer logic ...
    _viewManager.UpdateAllViews(GetActiveTimers());
}

private void cboCharacter_SelectedIndexChanged(object sender, EventArgs e)
{
    // ... existing character change logic ...
    _viewManager.LoadViews(CurrentCharacter?.ID);
}
```

---

*This document is the authoritative design reference for the Active Views feature.*  
*Last generated: 2026-03-27*
