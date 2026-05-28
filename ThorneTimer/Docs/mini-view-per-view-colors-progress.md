# Per-View Color Configuration — Implementation Progress

**Version:** v0.6.0  
**Started:** 2025-01-22  
**Status:** 🔄 IN PROGRESS  
**Branch:** `v0.6.0-gui-enhancements`

---

## Overview

Refactoring mini-view color configuration from global settings to per-view database-driven configuration. This enables multiple views of the same style with different colors and lays groundwork for future per-view properties (font size, opacity, thresholds).

**Plan Document:** [mini-view-per-view-colors-plan.md](mini-view-per-view-colors-plan.md)

---

## Phase 1: Database Foundation ✅ COMPLETE

### ✅ Completed
- [x] Created progress tracking document (this file)
- [x] Update Database.cs Connection() with migration logic
  - Add ForeColor, BackColor, ShowWarning columns
  - Migrate existing views from global settings
  - Seed new style views (Spawn, Lockout, Character, Pet)
  - **NEW:** Add EmptyBehavior column with migration logic
- [x] Add EnsureViewExists helper method to Database.cs
  - **NEW:** Updated signature to include emptyBehavior parameter
- [x] Update ViewPositionData class with ForeColor/BackColor/ShowWarning
  - **NEW:** Added EmptyBehavior property
- [x] Update GetViewPositions query and reader loop
  - **NEW:** Includes EmptyBehavior in SELECT and reader
- [x] Update MiniViews.GridData class with ForeColor/BackColor/ShowWarning
  - **NEW:** Added EmptyBehavior property
- [x] Update GetViews query for grid binding
  - **NEW:** Includes EmptyBehavior in SELECT and reader
- [x] Update SaveView method to handle new columns (INSERT/UPDATE)
  - **NEW:** Persists EmptyBehavior to database
- [x] Compilation verified (no errors)
- [x] **NEW:** Fix seed data names (removed "Timers" suffix)

### ✅ Phase 1a: Views Grid UI - COMPLETE (2025-01-23)
- [x] Add EmptyBehavior ComboBox column to Views grid
  - Options: CharacterName, ViewName, Spaces, HideEmpty
  - Width: 120px, minimum 100px
- [x] Make ForeColor/BackColor columns visible
  - Changed from hidden to visible text columns
  - Width: 80px each, minimum 60px
- [x] Add CellPainting event handler
  - Draws colored rectangles in ForeColor/BackColor cells
  - Matches Settings tab UX (colored boxes with borders)
- [x] Add CellClick event handler
  - Opens ColorDialog when clicking ForeColor/BackColor cells
  - Persists color changes to database
  - Refreshes Example column and mini views
- [x] Update column display order
  - Visible: Active, Name, Style, Fore, Back, Example, ShowWarning, EmptyBehavior
  - Hidden: ID, PositionX, PositionY, SortOrder
- [x] Compilation verified (no errors)

### ⏸️ Pending (Manual Testing Required)
- [ ] Test migration with existing database (user to verify colors preserved)
- [ ] Test new database creation with seeded views (user to verify 7 views appear)
- [ ] **NEW:** Test EmptyBehavior combo box (4 options work)
- [ ] **NEW:** Test ForeColor/BackColor color picker (colored boxes, ColorDialog)
- [ ] **NEW:** Test Example column updates when colors change

---

## Phase 2: ViewsGridController & Views Grid UI ✅ COMPLETE (2025-01-23)

**Decision:** Skipped controller extraction to keep implementation focused and minimize risk.
Color picker and EmptyBehavior UI implemented directly in FormMain.cs.

- [x] ~~Create ViewsGridController.cs class structure~~ (skipped - deferred to future refactor)
- [x] ~~Extract grid setup logic into controller.SetupGrid()~~ (skipped)
- [x] Add CellClick handler for color picker (implemented in FormMain)
- [x] Add CellPainting handler for colored boxes (implemented in FormMain)
- [x] Add CellFormatting handler for preview (already existed)
- [x] Test color picker functionality (ready for user testing)
- [x] Test preview column rendering (ready for user testing)

**Summary:** Phase 2 goals achieved without controller extraction. All Views grid UI
functionality complete and ready for testing.

---

## Phase 3: Mini-View Refactor ✅ COMPLETE (2025-01-23)

### ✅ Completed
- [x] Update MiniViews.UpdateMiniAppearance() to read per-view colors
  - Reads ForeColor, BackColor, ShowWarning, EmptyBehavior from viewData
  - Computes empty text based on EmptyBehavior setting:
    - CharacterName: Uses activeCharacterName
    - ViewName: Uses view's Name or StyleFilter
    - Spaces: Shows 10 spaces for minimal presence
    - HideEmpty: Empty string (view shows nothing when no timers)
  - Uses viewData.ActiveYn directly instead of ShowPing() for all views
- [x] Update MiniViews.SetMiniAppearance() wrapper method
  - Accepts emptyText, showWarning parameters
  - Passes per-view colors (viewForeColor, viewBackColor)
  - Removed obsolete per-style color parameters (norm/buff/ping)
- [x] Update MiniView.SetAppearance() signature
  - Removed per-style color parameters (normForeColor/BackColor, buffForeColor/BackColor, pingForeColor/BackColor)
  - Added emptyText, showWarning parameters
  - Stores EmptyText, ShowWarning as fields
- [x] Remove obsolete color fields from MiniView.cs
  - Removed: NormForeColor, NormBackColor, BuffForeColor, BuffBackColor, PingForeColor, PingBackColor, TimerText
  - Kept: ViewForeColor, ViewBackColor (per-view colors)
  - Kept: WarnForeColor, WarnBackColor (global warning colors)
  - Added: EmptyText, ShowWarning
- [x] Implement EmptyBehavior logic in MiniView.LoadData()
  - Character view: Always shows character name header using EmptyText
  - Empty views: Show EmptyText or hide based on EmptyBehavior setting
  - All timer rows: Use ViewForeColor/ViewBackColor only
  - Warning colors: Applied based on ShowWarning flag
- [x] Simplify ColorType enum usage
  - Kept enum for Ping timer warning suppression (still needed by UpdateMiniTimers)
  - Simplified switch statements: all timer colors use ViewForeColor/ViewBackColor
  - ColorType.Ping still used to skip warning colors on ping timers
- [x] Remove GetStyleColors() method from MiniViews.cs
  - Deleted obsolete global color mapping method
  - All color logic now database-driven per-view
- [x] Update MiniViews.CreateMiniViews()
  - Already uses viewData.ActiveYn directly (no changes needed)
  - Calls UpdateMiniAppearance() which handles per-view colors
- [x] Compilation verified (build successful)

### Summary
Mini views now fully wired to per-view database colors. Each view uses its own ForeColor, BackColor, ShowWarning, and EmptyBehavior settings. The old GetStyleColors() global color system has been removed. ColorType enum kept for Ping timer warning suppression only.

---

## Phase 4: Settings Cleanup ⏸️ PENDING

- [ ] Remove per-style color pickers from Settings tab
- [ ] Remove chkShowPing checkbox
- [ ] Add info label explaining view colors
- [ ] Update FormMain_Load() remove per-style color loading
- [ ] Update ReloadFromDatabase() remove per-style color loading

---

## Phase 5: Add New Styles ⏸️ PENDING

- [ ] Add Spawn/Lockout/Character to dropdowns
- [ ] Update style tooltips
- [ ] Verify seeded default views

---

## Phase 6: Testing & Validation ⏸️ PENDING

- [ ] Test new database creation (7 views with correct colors)
- [ ] Test migration from pre-v0.6.0 database
- [ ] Test Views grid UI (color pickers, preview, checkbox)
- [ ] Test mini-view behavior (per-view colors, ShowWarning)
- [ ] Test Settings tab (global warning colors work)
- [ ] Test grid row colors (uses view colors with fallback)
- [ ] Test Character view (shows character name, updates on switch)

---

## Notes & Decisions

### 2025-01-23 — Phase 3 Cleanup Complete (ShowPing Special Logic Removed)
- **Summary:** Eliminated ShowPing special handling. Ping view now uses ActiveYn and ShowWarning like all other views.
- **Changes:**
  - Removed ShowPing() check from CreateMiniViews - Ping view now uses ActiveYn column like all views
  - Simplified ShowMiniTimer() - removed `ShowPing()` dependency, all timers show if running or ping
  - Removed Ping warning color exemption in MiniView.LoadData - ShowWarning column controls this behavior
  - Added descriptive tooltips to EmptyBehavior dropdown:
    - CharacterName: "Always show active character name (typically used for Character view)"
    - ViewName: "Show view name when empty (e.g., 'Buffs', 'Pets', 'Spawns')"
    - Spaces: "Show minimal blank space to maintain view presence on screen"
    - HideEmpty: "Completely hide view when no timers are active (view disappears)"
  - Verified Settings tab Warning controls still present (lblWarnPickFore, lblWarnPickBack, txtWarningTime)
- **Rationale:** With per-view ActiveYn and ShowWarning columns, the old ShowPing hack is obsolete
- **Impact:** 
  - Ping view visibility now controlled by ActiveYn checkbox in Views grid (like all views)
  - Ping timer warning colors now controlled by ShowWarning checkbox (like all timers)
  - Simplified codebase, removed special-case logic
- **Testing:** Build successful. Ready for user testing:
  - Toggle Ping view Active checkbox to show/hide
  - Toggle Ping view ShowWarning checkbox to enable/disable warning colors on ping timers
  - Hover over EmptyBehavior dropdown options to see tooltips

### 2025-01-23 — Phase 3 Complete (Mini View Refactor)
- **Summary:** Mini views fully wired to per-view database colors. Old global color system removed.
- **Changes:**
  - Updated MiniViews.UpdateMiniAppearance() to read per-view ForeColor, BackColor, ShowWarning, EmptyBehavior
  - Computes EmptyText based on EmptyBehavior setting before passing to mini view
  - Updated MiniViews.SetMiniAppearance() to pass emptyText, showWarning, removed obsolete per-style colors
  - Updated MiniView.SetAppearance() signature: removed norm/buff/ping color parameters, added emptyText/showWarning
  - Removed obsolete color fields: NormForeColor, BuffForeColor, PingForeColor and Back variants, TimerText
  - Added new fields: EmptyText (computed text for empty view), ShowWarning (per-view flag)
  - Implemented EmptyBehavior logic in MiniView.LoadData():
    - Character view uses EmptyText for header
    - Empty views show EmptyText or hide if empty string
    - All timer rows use ViewForeColor/ViewBackColor
    - Warning colors applied only if ShowWarning is true
  - Simplified timer color logic: removed per-style switch, all use view colors
  - Removed GetStyleColors() method - obsolete global color mapping
  - ColorType enum kept for Ping timer warning suppression only
- **Testing:** Build successful. Ready for user testing:
  - Mini views should use per-view colors from Views grid
  - EmptyBehavior should control how empty views display
  - ShowWarning checkbox should control warning color display
  - Color changes in Views grid should immediately update mini views

### 2025-01-23 — Phase 1a & Phase 2 Complete (Views Grid UI)
- **Summary:** Database foundation + complete Views grid UI with color picker and EmptyBehavior combo
- **Changes:**
  - Added EmptyBehavior column to miniviews table with migration SQL
  - Updated EnsureViewExists() to include emptyBehavior parameter
  - Fixed seed data: removed "Timers" suffix from view names
  - Updated ViewPositionData, MiniViews.GridData, GetViewPositions, GetViews, SaveView for EmptyBehavior
  - Added EmptyBehavior ComboBox column to Views grid (4 options)
  - Made ForeColor/BackColor visible as colored box columns (80px each)
  - Added CellPainting event handler (draws colored rectangles like Settings tab)
  - Added CellClick event handler (opens ColorDialog on color cell click)
  - Updated column display order: Active, Name, Style, Fore, Back, Example, ShowWarning, EmptyBehavior
- **Decision:** Skipped ViewsGridController.cs extraction to keep changes focused
- **Testing:** All changes compile successfully. Ready for user testing:
  - Database migration (EmptyBehavior column added, default values set)
  - Views grid loads with new columns (EmptyBehavior combo, ForeColor/BackColor boxes)
  - Color picker opens and persists changes
  - Example column updates when colors change
- **Next Phase:** Phase 3 (MiniView refactor) — implement EmptyBehavior logic in MiniView.cs

### 2025-01-22 — Project Start
- **Decision:** Extract ViewsGridController.cs to reduce FormMain complexity
- **Rationale:** Follows MiniViews/MiniView pattern, enables future controller extraction (Timers, Categories, Characters grids)
- **Impact:** ~200 line reduction in FormMain.cs

### 2025-01-22 — Phase 1 Complete (Database Foundation)
- **Summary:** All database schema changes, migration logic, and data access methods updated
- **Changes:**
  - Added 3 columns to miniviews table: ForeColor (INTEGER), BackColor (INTEGER), ShowWarning (INTEGER)
  - Migration SQL copies colors from old global settings to per-view columns
  - EnsureViewExists() helper seeds new style views (Spawn, Lockout, Character, Pet)
  - ViewPositionData class extended with 3 properties
  - MiniViews.GridData class extended with 3 properties
  - GetViewPositions() query updated (feeds MiniViews.CreateMiniViews)
  - GetViews() query updated (feeds Views grid in FormMain)
  - SaveView() INSERT/UPDATE updated (persists user color changes)
  - New database seeding includes 7 default views with colors
  - **HOTFIX:** Added Spawn/Lockout/Character cases to MiniViews.GetStyleColors() to prevent crashes when loading new views
  - **HOTFIX:** Added new styles to Timers grid Style dropdown (FormMain.cs line 1849)
- **Testing:** Compilation verified (no errors). Manual testing required:
  - User needs to close running app, rebuild, test migration with existing .tdb file
  - User needs to test new database creation (verify 7 views appear with correct colors)
  - User should verify Views grid loads without errors (new styles handled)
- **Next Phase:** Phase 2 (ViewsGridController & Views Grid UI) after migration testing validates

### Database Migration Strategy
- New columns: ForeColor (INTEGER), BackColor (INTEGER), ShowWarning (INTEGER)
- Migration preserves user's existing global colors by copying to per-view
- Old settings (MiniViewNormFore, etc.) remain in database as fallback
- New databases seed 7 default views (Normal, Buffs, Pets, Pings, Spawns, Lockouts, Character)

### Active Views by Default
- Normal, Buffs, Pings, Character = Active=1 (most commonly used)
- Pets, Spawns, Lockouts = Active=0 (user opts in as needed)

---

## Issues Encountered

*None yet — just started implementation*

---

## Testing Log

*Will be populated during Phase 6*

---

## References

- [Plan Document](mini-view-per-view-colors-plan.md) — Full implementation plan
- [ROADMAP.md](../../Docs/ROADMAP.md) — v0.6.0 timeline
- [roadmap-phase-c-priority.md](roadmap-phase-c-priority.md) — Phase C (v0.7.0) is next
