# Implementation Plan: Per-View Color Configuration

> **✅ Implemented in v0.6.0** — This plan has been delivered on branch `v0.6.0-gui-enhancements`. See `Docs/ROADMAP.md` Phase D++ for the shipped feature list. This document is retained as historical design context and will be moved to `Docs/archive/` after v0.6.0 ships.
>
> **Where the design diverged from this plan:**
> - `Styles` became a **first-class table** (`styles`) with its own tab, Add/Delete/Rename, and a `ColorDialog` picker — not just an enum.
> - The Views grid `Style` column is bound to a **dynamic dropdown** sourced from the live styles table.
> - We did **not** build a `view_timers` junction or `CustomColors` JSON column; colors live as `ForeColor`/`BackColor` columns on `miniviews` directly.
> - Settings tab still owns global **warning** colors (`WarnFore`/`WarnBack`); per-view `ShowWarning` decides whether each view applies them.

---

**Feature:** Move mini-view color configuration from Settings tab to Views grid
**Version:** v0.6.0  
**Status:** Planning  
**Date:** 2025-01-22  
**Priority:** URGENT — User-facing color configuration issue identified during gameplay

---

## Executive Summary

This plan refactors mini-view color configuration to be **per-view** and **database-driven** rather than global settings. Users will configure colors directly in the Views grid (similar to WAV file picker in Timers grid), resulting in:

- **Flexibility:** Multiple views for the same style with different colors
- **Scalability:** Easy to add per-view properties (font size, opacity, warning threshold, etc.)
- **Simplicity:** Single source of truth (Views table/grid), cleaner Settings UI
- **Consistency:** Follows existing UI patterns (color pickers via "..." buttons)

---

## Current Architecture Issues

### Problem 1: Hard-Coded Global Colors
- Colors stored in `settings` table as global ARGB integers (e.g., `MiniViewBuffFore`)
- All Buff views use the same orange color; user cannot have multiple Buff views with different colors
- Adding new styles requires new settings UI + database settings

### Problem 2: Settings Tab Clutter
- Settings tab has 8+ color picker labels (Norm, Warn, Ping, Buff, and their Background equivalents)
- Not scalable: adding Spawn/Lockout/Pet/Character requires 8 more pickers (total: 16+)
- Color configuration split between Settings and database

### Problem 3: Hard-Wired Ping Behavior
- Ping timers **never** show warning colors (hardcoded in `MiniView.cs` LoadData())
- No user control over this behavior
- Users may want Ping warning colors (e.g., "Spawn ETA" ping that changes color when close)

### Problem 4: Style-Color Coupling
- `ColorType` enum in `MiniView.cs` couples timer styles to colors
- `GetStyleColors()` in `MiniViews.cs` has switch statement mapping styles to colors
- Adding new styles requires code changes in 4+ places

---

## Proposed Architecture

### Solution Overview

**Views Grid Becomes Configuration Hub:**
```
┌─────────────────────────────────────────────────────────────────────┐
│ Views Grid                                                          │
├─────┬────────┬───────┬────────┬──────┬──────┬──────────────────────┤
│ Act │ Name   │ Style │  Fore  │ Back │ Warn │      Preview         │
├─────┼────────┼───────┼────────┼──────┼──────┼──────────────────────┤
│ ☑   │ Normal │Normal │ Yellow │ Black│ ☑    │ Normal               │ ← Yellow text on black
│ ☑   │ Buffs  │ Buff  │ Orange │ Black│ ☑    │ Buffs                │ ← Orange text on black
│ ☐   │ Pets   │ Pet   │ Purple │ Black│ ☑    │ Pets                 │ ← Purple text on black
│ ☑   │ Pings  │ Ping  │ Green  │ Black│ ☐    │ Pings                │ ← Green text on black (no warn)
│ ☐   │ Spawns │ Spawn │ Yellow │ Black│ ☑    │ Spawns               │ ← Yellow text on black
│ ☐   │Lockout │Lockout│ Orange │ Black│ ☑    │ Lockouts             │ ← Orange text on black
│ ☑   │Char    │Char   │ White  │ Black│ ☐    │ (Gandalf)            │ ← Shows active character name
└─────┴────────┴───────┴────────┴──────┴──────┴──────────────────────┘

Fore/Back = Colored box cells (clickable like Settings tab color pickers)
☑/☐ = Checkbox (Warn column = show warning colors)
Preview = Shows view name text with actual foreground/background colors
```

**New Columns:**
- `ForeColor` (INTEGER, ARGB) - Foreground color for this view
- `BackColor` (INTEGER, ARGB) - Background color for this view
- `ShowWarning` (INTEGER, 0/1) - Whether to apply warning colors for this view

**Behavior:**
- Fore/Back columns display colored boxes (like Settings tab color pickers)
- Clicking colored box opens `ColorDialog` to change color
- Preview column shows view name in actual colors (e.g., "Normal Timers" in yellow on black)
- Warning checkbox controls whether warning threshold applies to this view
- Each view is independently configured

---

## Goals

### Primary Goals
1. **Per-View Colors:** Each view has its own ForeColor/BackColor (database-driven)
2. **Per-View Warning Control:** Each view controls whether to show warning colors
3. **Cleaner Settings Tab:** Remove per-style color pickers (keep only global warning colors)
4. **Scalable Design:** Future per-view properties (font size, opacity) are trivial to add
5. **Backward Compatibility:** Existing databases migrate seamlessly with default colors

### Secondary Goals
6. **Add Pet as Distinct Style:** Pet gets its own color (purple) instead of sharing Buff
7. **Add Spawn/Lockout/Character Styles:** New styles with default colors (yellow/orange/white)
8. **Remove "Show Ping" Checkbox:** Use Active flag on Ping views instead (cleaner UX)
9. **Architecture Improvement:** Extract ViewsGridController.cs to reduce FormMain complexity

---

## Architecture Improvement: ViewsGridController

### Problem
FormMain.cs is approaching 6000+ lines, with Views grid logic adding another ~200 lines of setup/event handling code.

### Solution
Extract Views grid UI logic into a dedicated **ViewsGridController.cs** class that:

- Encapsulates all grid setup (columns, event handlers)
- Handles color picker interactions (CellClick)
- Manages preview rendering (CellFormatting)
- Provides clean API for FormMain (`SetupGrid()`, `RefreshGrid()`, `SaveChanges()`)

### Benefits
✅ **Reduces FormMain.cs by ~200 lines** (23% of this feature's code)  
✅ **Follows existing MiniViews/MiniView pattern** (consistency)  
✅ **Encapsulates Views grid behavior** (single responsibility)  
✅ **Makes testing possible** (controller can be unit tested)  
✅ **Clear boundaries** (FormMain orchestrates, controller manages grid)

### Future Refactoring Roadmap
If ViewsGridController succeeds, similar controllers can be extracted in future versions:

- **v0.7.0+:** `TimersGridController.cs` (~600 line reduction)
- **v0.7.0+:** `CategoriesGridController.cs` (~200 line reduction)
- **v0.8.0+:** `CharactersGridController.cs` (~150 line reduction)
- **v0.8.0+:** `SettingsController.cs` (~250 line reduction)

**Estimated Total:** FormMain.cs could shrink from ~6000 → ~4600 lines (23% reduction)

---

## Database Schema Changes

### Phase 1: Add Columns to `miniviews` Table

```sql
-- Add color columns (NULL = use style-based defaults for migration)
ALTER TABLE miniviews ADD COLUMN ForeColor INTEGER DEFAULT NULL;
ALTER TABLE miniviews ADD COLUMN BackColor INTEGER DEFAULT NULL;
ALTER TABLE miniviews ADD COLUMN ShowWarning INTEGER DEFAULT 1;  -- Default: show warning colors
```

**Color ARGB Reference:**
```csharp
// Default colors for new styles:
Color.Yellow.ToArgb()           = -256        // Spawn (default)
Color.Orange.ToArgb()           = -23296      // Lockout, Buff (default)
Color.FromArgb(100, 60, 160)    = -6684825    // Pet (new purple)
Color.LightGreen.ToArgb()       = -16711936   // Ping (default)
Color.White.ToArgb()            = -1          // Character (default)
Color.Black.ToArgb()            = -16777216   // Background (default for all)
```

### Phase 2: Seed Default Views for New Databases

**In `Database.cs` Connection() when `newDatabase = true`:**

```sql
INSERT INTO miniviews (Name, StyleFilter, ForeColor, BackColor, ShowWarning, PositionX, PositionY, SortOrder, ActiveYn) VALUES
('Normal', 'Normal', -256, -16777216, 1, 100, 100, 1, 1),                -- Yellow/Black, warning ON, ACTIVE
('Buffs', 'Buff', -23296, -16777216, 1, 400, 100, 2, 1),                 -- Orange/Black, warning ON, ACTIVE
('Pets', 'Pet', -6684825, -16777216, 1, 700, 100, 3, 0),                 -- Purple/Black, warning ON, inactive
('Pings', 'Ping', -16711936, -16777216, 0, 100, 300, 4, 1),              -- Green/Black, warning OFF, ACTIVE
('Spawns', 'Spawn', -256, -16777216, 1, 400, 300, 5, 0),                 -- Yellow/Black, warning ON, inactive
('Lockouts', 'Lockout', -23296, -16777216, 1, 700, 300, 6, 0),           -- Orange/Black, warning ON, inactive
('Character', 'Character', -1, -16777216, 0, 100, 500, 7, 1);            -- White/Black, warning OFF, ACTIVE
```

**Note:** Normal, Buffs, Pings, and Character are Active=1 by default (most commonly used views). Character view Name is replaced at runtime with the active character name or "(None)".

### Phase 3: Migrate Existing Databases

**In `Database.cs` Connection() after opening existing database:**

```csharp
// Check if migration is needed
if (!Database.isFieldExist(con, "miniviews", "ForeColor"))
{
    // Add columns
    cmd.CommandText = "ALTER TABLE miniviews ADD COLUMN ForeColor INTEGER DEFAULT NULL";
    cmd.ExecuteNonQuery();
    cmd.CommandText = "ALTER TABLE miniviews ADD COLUMN BackColor INTEGER DEFAULT NULL";
    cmd.ExecuteNonQuery();
    cmd.CommandText = "ALTER TABLE miniviews ADD COLUMN ShowWarning INTEGER DEFAULT 1";
    cmd.ExecuteNonQuery();
    
    // Migrate existing views with colors from old global settings
    // For each existing view, set colors based on StyleFilter:
    cmd.CommandText = @"
        UPDATE miniviews SET 
            ForeColor = CASE StyleFilter
                WHEN 'Normal' THEN (SELECT Value FROM settings WHERE Name = 'MiniViewNormFore')
                WHEN 'Buff' THEN (SELECT Value FROM settings WHERE Name = 'MiniViewBuffFore')
                WHEN 'Pet' THEN (SELECT Value FROM settings WHERE Name = 'MiniViewBuffFore')  -- Pet shared Buff color
                WHEN 'Ping' THEN (SELECT Value FROM settings WHERE Name = 'MiniViewPingFore')
                ELSE -256  -- Default yellow
            END,
            BackColor = CASE StyleFilter
                WHEN 'Normal' THEN (SELECT Value FROM settings WHERE Name = 'MiniViewNormBack')
                WHEN 'Buff' THEN (SELECT Value FROM settings WHERE Name = 'MiniViewBuffBack')
                WHEN 'Pet' THEN (SELECT Value FROM settings WHERE Name = 'MiniViewBuffBack')
                WHEN 'Ping' THEN (SELECT Value FROM settings WHERE Name = 'MiniViewPingBack')
                ELSE -16777216  -- Default black
            END,
            ShowWarning = CASE StyleFilter
                WHEN 'Ping' THEN 0  -- Ping never showed warnings (hardwired)
                ELSE 1              -- Everything else did
            END
        WHERE ForeColor IS NULL";
    cmd.ExecuteNonQuery();
    
    // Seed new style views if they don't exist
    EnsureViewExists(con, "Spawns", "Spawn", -256, -16777216, 1, 400, 300, 10, 0);
    EnsureViewExists(con, "Lockouts", "Lockout", -23296, -16777216, 1, 700, 300, 11, 0);
    EnsureViewExists(con, "Character", "Character", -1, -16777216, 0, 100, 500, 12, 1);  // Active by default
    
    // Optionally: Create separate Pet view if none exists
    if (!ViewExistsForStyle(con, "Pet"))
    {
        EnsureViewExists(con, "Pets", "Pet", -6684825, -16777216, 1, 700, 100, 13, 0);
    }
}
```

**Helper method:**
```csharp
private static void EnsureViewExists(SQLiteConnection con, string name, string style, 
                                     int fore, int back, int showWarn, int x, int y, int order, int active)
{
    var cmd = new SQLiteCommand(con);
    cmd.CommandText = "SELECT COUNT(*) FROM miniviews WHERE StyleFilter = @style";
    cmd.Parameters.AddWithValue("@style", style);
    if ((long)cmd.ExecuteScalar() == 0)
    {
        cmd.CommandText = @"INSERT INTO miniviews (Name, StyleFilter, ForeColor, BackColor, ShowWarning, 
                           PositionX, PositionY, SortOrder, ActiveYn) 
                           VALUES (@name, @style, @fore, @back, @warn, @x, @y, @order, @active)";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@style", style);
        cmd.Parameters.AddWithValue("@fore", fore);
        cmd.Parameters.AddWithValue("@back", back);
        cmd.Parameters.AddWithValue("@warn", showWarn);
        cmd.Parameters.AddWithValue("@x", x);
        cmd.Parameters.AddWithValue("@y", y);
        cmd.Parameters.AddWithValue("@order", order);
        cmd.Parameters.AddWithValue("@active", active);
        cmd.ExecuteNonQuery();
    }
}
```

---

## UI Changes

### Phase 4: Update Views Grid (FormMain.cs)

#### 4.1 Add Hidden Data Columns

```csharp
// In SetupViewsGrid(), after StyleFilter column:

// ForeColor (hidden data column)
grdViews.Columns.Add("ForeColor", "ForeColor");
grdViews.Columns["ForeColor"].DataPropertyName = "ForeColor";
grdViews.Columns["ForeColor"].Visible = false;

// BackColor (hidden data column)
grdViews.Columns.Add("BackColor", "BackColor");
grdViews.Columns["BackColor"].DataPropertyName = "BackColor";
grdViews.Columns["BackColor"].Visible = false;

// ShowWarning (hidden data column)
grdViews.Columns.Add("ShowWarning", "ShowWarning");
grdViews.Columns["ShowWarning"].DataPropertyName = "ShowWarning";
grdViews.Columns["ShowWarning"].Visible = false;
```

#### 4.2 Add Visible UI Columns

```csharp
// Foreground Color Picker (colored box cell, like Settings tab)
DataGridViewTextBoxColumn colForeColor = new DataGridViewTextBoxColumn
{
    HeaderText = "Fore",
    Name = "ForeColorPicker",
    ReadOnly = true,
    Width = 60,
    MinimumWidth = 60,
    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
};
grdViews.Columns.Add(colForeColor);

// Background Color Picker (colored box cell, like Settings tab)
DataGridViewTextBoxColumn colBackColor = new DataGridViewTextBoxColumn
{
    HeaderText = "Back",
    Name = "BackColorPicker",
    ReadOnly = true,
    Width = 60,
    MinimumWidth = 60,
    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
};
grdViews.Columns.Add(colBackColor);

// Warning Colors Checkbox
DataGridViewCheckBoxColumn chkShowWarning = new DataGridViewCheckBoxColumn
{
    HeaderText = "Warn",
    Name = "ShowWarningCheckbox",
    DataPropertyName = "ShowWarning",
    TrueValue = (long)1,
    FalseValue = (long)0,
    Width = 45,
    MinimumWidth = 45,
    AutoSizeMode = DataGridViewAutoSizeColumnMode.None
};
grdViews.Columns.Add(chkShowWarning);

// Color Preview (shows view name with actual colors)
DataGridViewTextBoxColumn colPreview = new DataGridViewTextBoxColumn
{
    HeaderText = "Preview",
    Name = "ColorPreview",
    ReadOnly = true,
    Width = 120,
    MinimumWidth = 100,
    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
};
grdViews.Columns.Add(colPreview);
```

#### 4.3 Wire Up Event Handlers

```csharp
// In SetupViewsGrid() after adding columns:
grdViews.CellClick += grdViews_CellClick;
grdViews.CellFormatting += grdViews_CellFormatting;
```

#### 4.4 Implement CellClick Handler

```csharp
private void grdViews_CellClick(object sender, DataGridViewCellEventArgs e)
{
    if (e.RowIndex < 0) return;

    DataGridViewRow row = grdViews.Rows[e.RowIndex];

    // Foreground color picker (colored box cell)
    if (e.ColumnIndex == grdViews.Columns["ForeColorPicker"].Index)
    {
        DataGridViewCell foreColorCell = row.Cells[grdViews.Columns["ForeColor"].Index];
        int currentColor = Convert.ToInt32(foreColorCell.Value ?? Color.Yellow.ToArgb());

        colorDialogPicker.Color = Color.FromArgb(currentColor);
        if (colorDialogPicker.ShowDialog() == DialogResult.OK)
        {
            foreColorCell.Value = colorDialogPicker.Color.ToArgb();
            grdViews.InvalidateRow(e.RowIndex); // Refresh color boxes and preview
            SaveDataViews();
            miniViews.RefreshMiniViews(con, activeCharacterID);
            UpdateMiniView();
        }
    }
    // Background color picker (colored box cell)
    else if (e.ColumnIndex == grdViews.Columns["BackColorPicker"].Index)
    {
        DataGridViewCell backColorCell = row.Cells[grdViews.Columns["BackColor"].Index];
        int currentColor = Convert.ToInt32(backColorCell.Value ?? Color.Black.ToArgb());

        colorDialogPicker.Color = Color.FromArgb(currentColor);
        if (colorDialogPicker.ShowDialog() == DialogResult.OK)
        {
            backColorCell.Value = colorDialogPicker.Color.ToArgb();
            grdViews.InvalidateRow(e.RowIndex); // Refresh color boxes and preview
            SaveDataViews();
            miniViews.RefreshMiniViews(con, activeCharacterID);
            UpdateMiniView();
        }
    }
}
```

#### 4.5 Implement CellFormatting Handler (Color Boxes & Preview)

```csharp
private void grdViews_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
{
    if (e.RowIndex < 0) return;

    DataGridViewRow row = grdViews.Rows[e.RowIndex];

    // Foreground color picker: show colored box (like Settings tab)
    if (e.ColumnIndex == grdViews.Columns["ForeColorPicker"].Index)
    {
        int foreColorArgb = Convert.ToInt32(row.Cells[grdViews.Columns["ForeColor"].Index].Value ?? Color.Yellow.ToArgb());
        Color foreColor = Color.FromArgb(foreColorArgb);

        e.Value = "";  // Empty text
        e.CellStyle.BackColor = foreColor;
        e.CellStyle.SelectionBackColor = foreColor;
        e.FormattingApplied = true;
    }
    // Background color picker: show colored box (like Settings tab)
    else if (e.ColumnIndex == grdViews.Columns["BackColorPicker"].Index)
    {
        int backColorArgb = Convert.ToInt32(row.Cells[grdViews.Columns["BackColor"].Index].Value ?? Color.Black.ToArgb());
        Color backColor = Color.FromArgb(backColorArgb);

        e.Value = "";  // Empty text
        e.CellStyle.BackColor = backColor;
        e.CellStyle.SelectionBackColor = backColor;
        e.FormattingApplied = true;
    }
    // Color Preview column: show view name with actual colors
    else if (e.ColumnIndex == grdViews.Columns["ColorPreview"].Index)
    {
        int foreColorArgb = Convert.ToInt32(row.Cells[grdViews.Columns["ForeColor"].Index].Value ?? Color.Yellow.ToArgb());
        int backColorArgb = Convert.ToInt32(row.Cells[grdViews.Columns["BackColor"].Index].Value ?? Color.Black.ToArgb());

        Color foreColor = Color.FromArgb(foreColorArgb);
        Color backColor = Color.FromArgb(backColorArgb);

        string viewName = Convert.ToString(row.Cells[grdViews.Columns["Name"].Index].Value ?? "");
        string styleFilter = Convert.ToString(row.Cells[grdViews.Columns["StyleFilter"].Index].Value ?? "");

        // Character view shows actual character name
        if (styleFilter == "Character")
        {
            // Get active character name from dropdown
            var activeChar = tscActiveCharacter.SelectedItem as ComboBoxItem;
            viewName = activeChar != null ? activeChar.Text : "(None)";
        }

        e.Value = viewName;
        e.CellStyle.ForeColor = foreColor;
        e.CellStyle.BackColor = backColor;
        e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        e.CellStyle.Padding = new Padding(4, 0, 4, 0);
        e.FormattingApplied = true;
    }
}
```

#### 4.6 Update Database.SaveView()

```csharp
// In Database.cs SaveView(), add ForeColor/BackColor/ShowWarning handling:

DataGridViewCell ForeColor = row.Cells[dataGridView.Columns["ForeColor"].Index];
DataGridViewCell BackColor = row.Cells[dataGridView.Columns["BackColor"].Index];
DataGridViewCell ShowWarning = row.Cells[dataGridView.Columns["ShowWarning"].Index];

if (Convert.ToString(ID.Value) == "-1")
{
    cmd.CommandText = @"INSERT INTO miniviews (Name, ActiveYn, StyleFilter, ForeColor, BackColor, ShowWarning, PositionX, PositionY, SortOrder) 
                       VALUES (@name, @active, @style, @fore, @back, @warn, 100, 100, 0)";
    cmd.Parameters.AddWithValue("@name", Convert.ToString(Name.Value));
    cmd.Parameters.AddWithValue("@active", Convert.ToInt32(ActiveYn.Value));
    cmd.Parameters.AddWithValue("@style", Convert.ToString(StyleFilter.Value));
    cmd.Parameters.AddWithValue("@fore", Convert.ToInt32(ForeColor.Value ?? Color.Yellow.ToArgb()));
    cmd.Parameters.AddWithValue("@back", Convert.ToInt32(BackColor.Value ?? Color.Black.ToArgb()));
    cmd.Parameters.AddWithValue("@warn", Convert.ToInt32(ShowWarning.Value ?? 1));
    cmd.ExecuteNonQuery();
}
else
{
    cmd.CommandText = @"UPDATE miniviews SET Name = @name, ActiveYn = @active, StyleFilter = @style, 
                       ForeColor = @fore, BackColor = @back, ShowWarning = @warn WHERE ID = @id";
    cmd.Parameters.AddWithValue("@name", Convert.ToString(Name.Value));
    cmd.Parameters.AddWithValue("@active", Convert.ToInt32(ActiveYn.Value));
    cmd.Parameters.AddWithValue("@style", Convert.ToString(StyleFilter.Value));
    cmd.Parameters.AddWithValue("@fore", Convert.ToInt32(ForeColor.Value ?? Color.Yellow.ToArgb()));
    cmd.Parameters.AddWithValue("@back", Convert.ToInt32(BackColor.Value ?? Color.Black.ToArgb()));
    cmd.Parameters.AddWithValue("@warn", Convert.ToInt32(ShowWarning.Value ?? 1));
    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(ID.Value));
    cmd.ExecuteNonQuery();
}
```

#### 4.7 Update Database.GetViewPositions()

```csharp
// Update Database.ViewPositionData class:
public class ViewPositionData
{
    public int ID { get; set; }
    public string Name { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int SortOrder { get; set; }
    public int ActiveYn { get; set; }
    public string StyleFilter { get; set; }
    public int ForeColor { get; set; }      // NEW
    public int BackColor { get; set; }      // NEW
    public int ShowWarning { get; set; }    // NEW
}

// Update query in GetViewPositions():
cmd.CommandText = @"SELECT ID, Name, PositionX, PositionY, SortOrder, ActiveYn, StyleFilter, 
                   ForeColor, BackColor, ShowWarning FROM miniviews ORDER BY SortOrder";

// In reader loop:
ForeColor = rdr.IsDBNull(rdr.GetOrdinal("ForeColor")) ? Color.Yellow.ToArgb() : rdr.GetInt32(rdr.GetOrdinal("ForeColor")),
BackColor = rdr.IsDBNull(rdr.GetOrdinal("BackColor")) ? Color.Black.ToArgb() : rdr.GetInt32(rdr.GetOrdinal("BackColor")),
ShowWarning = rdr.IsDBNull(rdr.GetOrdinal("ShowWarning")) ? 1 : rdr.GetInt32(rdr.GetOrdinal("ShowWarning"))
```

#### 4.8 Update Database.GetViews()

```csharp
// Similar to GetViewPositions(), ensure MiniViews.GridData includes new columns for grid binding
```

---

### Phase 5: Update MiniViews.cs (View Manager)

#### 5.1 Remove Global Color Fields

```csharp
// REMOVE these from MiniViews.cs (no longer global):
// public int mvNormForeColor = Color.Yellow.ToArgb();
// public int mvNormBackColor = Color.Black.ToArgb();
// public int mvBuffForeColor = Color.Orange.ToArgb();
// public int mvBuffBackColor = Color.Black.ToArgb();
// public int mvPingForeColor = Color.LightGreen.ToArgb();
// public int mvPingBackColor = Color.Black.ToArgb();

// KEEP these (still global):
public int mvOpacity = 100;
public int mvFontSize = 8;
public int mvWarnForeColor = Color.White.ToArgb();  // Global warning colors
public int mvWarnBackColor = Color.Red.ToArgb();
public string mvWarnTime = "00:30";
```

#### 5.2 Simplify UpdateMiniAppearance()

```csharp
public bool UpdateMiniAppearance()
{
    bool result = true;

    foreach (var entry in activeViews)
    {
        // Use per-view colors from database (no more style-based switch)
        int viewFore = entry.Data.ForeColor;
        int viewBack = entry.Data.BackColor;

        bool showView = true;
        // Remove mvShowPing check — use Active flag on Ping views instead

        // Character view shows active character name, all others show view name
        string displayName = entry.Data.Name;
        if (entry.Data.StyleFilter == "Character")
        {
            // Replace with actual character name (passed from FormMain via CreateMiniViews)
            displayName = GetActiveCharacterName();  // Helper method to get current character
        }

        SetMiniAppearance(entry.Form, 
                         displayName, 
                         showView, viewFore, viewBack, entry.Data.ShowWarning);
    }

    return result;
}
```

#### 5.3 Remove GetStyleColors() Method

```csharp
// DELETE this method entirely — no longer needed since colors come from database
// private void GetStyleColors(string style, out int foreColor, out int backColor, out string emptyLabel)
```

#### 5.4 Update SetMiniAppearance() Signature

```csharp
private void SetMiniAppearance(MiniView view, String timerText, bool showView, 
                              int viewForeColor, int viewBackColor, int showWarning)
{
    if (view != null)
    {
        view.SetAppearance(
            mvOpacity, mvFontSize, 
            Color.FromArgb(viewForeColor), Color.FromArgb(viewBackColor),  // Per-view colors
            Color.FromArgb(mvWarnForeColor), Color.FromArgb(mvWarnBackColor), mvWarnTime, // Global warning
            showWarning,  // NEW: pass per-view warning flag
            timerText,
            Color.FromArgb(viewForeColor), Color.FromArgb(viewBackColor));  // Empty state uses view colors
        
        if (showView)
        {
            view.Show();
            view.BringToFront();
        }
        else
        {
            view.Hide();
            view.SendToBack();
        }
    }
}
```

#### 5.5 Remove mvShowPing Field

```csharp
// DELETE:
// public int mvShowPing = 1;

// DELETE:
// public bool ShowPing() { return (mvShowPing == 1); }
```

---

### Phase 6: Update MiniView.cs (Individual View)

#### 6.1 Simplify SetAppearance() Signature

```csharp
// Remove all style-specific color parameters, add showWarning flag
public void SetAppearance(int opacity, float fontSize, 
                          Color viewForeColor, Color viewBackColor,           // Per-view colors
                          Color warnForeColor, Color warnBackColor, String warnTime,  // Global warning
                          int showWarning,                                    // NEW: per-view warning flag
                          String timerText,
                          Color emptyForeColor, Color emptyBackColor)         // Empty state colors
{
    FormOpacity = (double)opacity / 100.0f;
    FontSize = fontSize;

    ViewForeColor = viewForeColor;
    ViewBackColor = viewBackColor;

    WarnForeColor = warnForeColor;
    WarnBackColor = warnBackColor;
    WarnTime = "00:" + warnTime;
    ShowWarning = showWarning;  // NEW: store per-view flag

    TimerText = timerText;
    this.BackColor = viewBackColor;
}
```

#### 6.2 Add ShowWarning Field

```csharp
// At top of MiniView class:
int ShowWarning = 1;  // Default: show warning colors
```

#### 6.3 Remove ColorType Enum

```csharp
// DELETE this entire enum — no longer needed
// public enum ColorType
// {
//     Normal,
//     Pet,
//     Buff,
//     Ping
// }
```

#### 6.4 Simplify MiniData Class

```csharp
public class MiniData
{
    public string Name { get; set; }
    public string Remaining { get; set; }
    // DELETE TheColor property — no longer needed
}
```

#### 6.5 Update LoadData() Method

```csharp
public void LoadData(List<MiniData> data)
{
    if (InvokeRequired)
    {
        try
        {
            this.Invoke(new Action<List<MiniData>>(LoadData), new object[] { data });
        }
        catch { }
        return;
    }

    this.Opacity = FormOpacity;

    if (data.Count == 0)
    {
        // Empty state: show view name (e.g., "Normal", "Buffs", or character name)
        tlpMain = new TableLayoutPanel { /* ... */ };
        Label lblNoActive = new Label { 
            Text = TimerText,  // View name (no "No Timers" suffix)
            BackColor = ViewBackColor, 
            ForeColor = ViewForeColor,
            /* ... */
        };
        tlpMain.Controls.Add(lblNoActive, 0, 0);
        tlpMain.RowCount++;
    }
    else
    {
        // Timer rows: apply per-view colors
        tlpMain = new TableLayoutPanel { /* ... */ };

        foreach (MiniData md in data)
        {
            tlpMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label lblName = new Label { 
                Text = md.Name,
                BackColor = ViewBackColor,
                ForeColor = ViewForeColor,
                /* ... */
            };
            tlpMain.Controls.Add(lblName, 0, tlpMain.RowCount);

            Label lblRemaining = new Label { 
                Text = md.Remaining,
                BackColor = ViewBackColor,
                ForeColor = ViewForeColor,
                /* ... */
            };
            tlpMain.Controls.Add(lblRemaining, 1, tlpMain.RowCount);

            // Apply warning color ONLY if ShowWarning flag is set
            if (ShowWarning == 1 && TimerPlus.GetMilliseconds(md.Remaining) <= TimerPlus.GetMilliseconds(WarnTime))
            {
                lblRemaining.BackColor = WarnBackColor;
                lblRemaining.ForeColor = WarnForeColor;
            }

            tlpMain.RowCount++;
        }
    }

    tlpMain.MouseDown += Control_MouseDown;
    Controls.Add(tlpMain);

    // Clean up old controls
    foreach (Control c in Controls)
    {
        if ((string)c.Tag == "TLP")
            Controls.Remove(c);
    }
    tlpMain.Tag = "TLP";
}
```

#### 6.6 Update MiniViews.UpdateMiniTimers()

```csharp
// Remove ColorType mapping — just build MiniData with Name/Remaining
foreach (var td in timerData)
{
    if (!ShowMiniTimer(td.ButtonState)) continue;

    MiniView.MiniData md = new MiniView.MiniData
    {
        Name = td.Name,
        Remaining = td.Remaining
        // NO TheColor property — deleted
    };

    string timerStyle = string.IsNullOrEmpty(td.Style) ? "Normal" : td.Style;
    
    // Route to view(s) whose StyleFilter matches this timer's Style
    if (viewData.ContainsKey(timerStyle))
    {
        viewData[timerStyle].Add(md);
    }
}
```

---

### Phase 7: Update FormMain.cs (Settings Tab & Grid Row Colors)

#### 7.1 Clean Up Settings Tab

**Remove:**
- `lblNormPickFore`, `lblNormPickBack` + click handlers
- `lblBuffPickFore`, `lblBuffPickBack` + click handlers
- `lblPingPickFore`, `lblPingPickBack` + click handlers
- `chkShowPing` + click handler
- All loading code in `FormMain_Load()` for per-style colors
- All loading code in `ReloadFromDatabase()` for per-style colors

**Keep:**
- `lblWarnPickFore`, `lblWarnPickBack` + click handlers (global warning colors)
- `txtWarningTime` (global warning threshold)
- `tbOpacity`, `tbFontSize` (global appearance settings)

**Add:**
- Info label explaining where to configure view colors:
```csharp
Label lblViewColorsInfo = new Label
{
    Text = "View colors are configured in the Views tab (Settings → Views).",
    AutoSize = true,
    ForeColor = Color.Gray,
    Font = new Font(this.Font, FontStyle.Italic)
};
// Position where old color pickers were
```

#### 7.2 Update GetStyleColor() for Grid Row Coloring

```csharp
// Grid row colors should use view colors if available, else defaults
private Color GetStyleColor(string style)
{
    // Try to find an active view for this style and use its color
    var views = Database.GetViewPositions(con);
    var view = views.FirstOrDefault(v => v.StyleFilter == style && v.ActiveYn == 1);
    
    if (view != null)
        return Color.FromArgb(view.ForeColor);
    
    // Fallback to defaults if no active view found
    switch (style)
    {
        case "Ping": return Color.LightGreen;
        case "Buff": return Color.Orange;
        case "Pet": return Color.FromArgb(100, 60, 160);  // Purple
        case "Spawn": return Color.Yellow;
        case "Lockout": return Color.Orange;
        case "Character": return Color.White;
        default: return Color.Yellow;
    }
}
```

#### 7.3 Add New Styles to Dropdowns

```csharp
// In SetupTimerGrid() (~line 2500):
cboRole.Items.AddRange("Normal", "Buff", "Pet", "Ping", "Spawn", "Lockout", "Character");

// In SetupViewsGrid():
cboStyle.Items.AddRange("Normal", "Buff", "Pet", "Ping", "Spawn", "Lockout", "Character");
```

#### 7.4 Update Style Tooltips

```csharp
// In GrdTimers_CellToolTipTextNeeded():
case "Pet":
    e.ToolTipText = "Pet timer. Restarts if the keyword fires again while running.\nUses pet-style colors in mini views (purple by default).";
    break;
case "Spawn":
    e.ToolTipText = "Spawn timer. Used for tracking mob respawn windows.\nUses spawn-style colors in mini views (yellow by default).";
    break;
case "Lockout":
    e.ToolTipText = "Lockout timer. Used for loot lockout periods.\nUses lockout-style colors in mini views (orange by default).";
    break;
case "Character":
    e.ToolTipText = "Character timer. Displays in the Character view along with character name.\nUses character-style colors in mini views (white by default).";
    break;
```

---

## Testing Plan

### Test Cases

#### 1. New Database Creation
- [ ] Create new database
- [ ] Verify 7 default views exist (Normal, Buff, Pet, Ping, Spawn, Lockout, Character)
- [ ] Verify colors are correct (Yellow, Orange, Purple, Green, Yellow, Orange, White)
- [ ] Verify warning flags: Ping=OFF, Character=OFF, all others=ON
- [ ] Verify Active flags: Normal=ON, Buff=ON, Ping=ON, Character=ON (4 active by default)

#### 2. Existing Database Migration
- [ ] Open pre-v0.7.0 database (has old global colors)
- [ ] Verify ForeColor/BackColor/ShowWarning columns added automatically
- [ ] Verify existing views migrated with correct colors from old settings
- [ ] Verify new style views (Spawn, Lockout, Character) seeded
- [ ] Verify old settings (MiniViewNormFore, etc.) still exist (for fallback)

#### 3. Views Grid UI
- [ ] Fore/Back columns show colored boxes (like Settings tab)
- [ ] Clicking colored box opens ColorDialog
- [ ] Changing foreground color updates color box and preview immediately
- [ ] Changing background color updates color box and preview immediately
- [ ] Warning checkbox toggles correctly
- [ ] Preview column shows view name with correct colors
- [ ] Saving grid persists changes to database
- [ ] RefreshMiniViews() updates live views after color change

#### 4. Mini View Behavior
- [ ] Timers display with per-view colors (not global)
- [ ] Warning colors apply when ShowWarning=1 and timer < threshold
- [ ] Warning colors DO NOT apply when ShowWarning=0 (e.g., Ping)
- [ ] Multiple views for same style can have different colors
- [ ] Changing view colors live-updates mini views

#### 5. Settings Tab
- [ ] Per-style color pickers removed (Normal, Buff, Ping)
- [ ] Global warning color pickers still work
- [ ] "Show Ping" checkbox removed
- [ ] Info label explains where to configure view colors

#### 6. Grid Row Colors
- [ ] Timer grid rows use view colors when available
- [ ] Falls back to defaults when no active view for style
- [ ] Works for all styles (Normal, Buff, Pet, Ping, Spawn, Lockout, Character)

#### 7. Character View Special Case
- [ ] Character view shows active character name (e.g., "Gandalf") not "Character"
- [ ] Character view shows "(None)" when no character selected
- [ ] Character view shows timers assigned to "Character" style
- [ ] No warning colors shown (ShowWarning=0 by default)
- [ ] Character name updates immediately when switching characters in dropdown
- [ ] Preview column in Views grid shows current character name for Character view

---

## Files to Modify

### Database Layer
- [x] `ThorneTimer/Database.cs`
  - Add ForeColor/BackColor/ShowWarning columns to ViewPositionData class
  - Update GetViewPositions() query
  - Update GetViews() query (for grid binding)
  - Update SaveView() method
  - Add EnsureViewExists() helper
  - Add migration code in Connection() method

### Mini-View System
- [x] `ThorneTimer/MiniView.cs`
  - Remove ColorType enum
  - Simplify MiniData class (remove TheColor)
  - Update SetAppearance() signature
  - Add ShowWarning field
  - Update LoadData() to use per-view colors + ShowWarning flag

- [x] `ThorneTimer/MiniViews.cs`
  - Remove global color fields (mvNormForeColor, mvBuffForeColor, mvPingForeColor)
  - Remove GetStyleColors() method
  - Remove mvShowPing field and ShowPing() method
  - Update UpdateMiniAppearance() to use per-view colors
  - Update SetMiniAppearance() signature
  - Update UpdateMiniTimers() to remove ColorType mapping

### Main Form
- [x] `ThorneTimer/FormMain.cs`
  - Update SetupViewsGrid() to add color picker columns + preview
  - Add grdViews_CellClick() handler
  - Add grdViews_CellFormatting() handler
  - Remove per-style color pickers from Settings tab
  - Remove chkShowPing from Settings tab
  - Update FormMain_Load() to remove per-style color loading
  - Update ReloadFromDatabase() to remove per-style color loading
  - Update GetStyleColor() to use view colors
  - Add new styles to dropdowns (Spawn, Lockout, Character)
  - Update tooltips for new styles

### Documentation
- [x] `ThorneTimer/Docs/mini-view-per-view-colors-plan.md` (this document)
- [ ] `ThorneTimer/Docs/mini-view-per-view-colors-progress.md` (to be created during implementation)

### New Files
- [ ] `ThorneTimer/ViewsGridController.cs` (new supporting class for Views grid UI logic)

---

## Risks & Mitigations

### Risk 1: Migration Complexity
**Risk:** Users with custom global colors lose their settings during migration  
**Mitigation:** Migration SQL preserves old global colors by applying them to existing views. Old settings remain in database as fallback.

### Risk 2: UI Complexity
**Risk:** Color picker columns + checkbox + preview column clutters Views grid  
**Mitigation:** Keep columns narrow (60px for color boxes, 45px for checkbox, 120px for preview Fill). Colored boxes match Settings tab pattern (familiar UX). Total added width: ~165px fixed + 120px Fill.

### Risk 3: Backward Compatibility
**Risk:** New code breaks when opening old databases  
**Mitigation:** All new columns have DEFAULT values. NULL checks in code. Migration runs automatically on first open.

### Risk 4: Performance
**Risk:** Color preview formatting slows down grid rendering  
**Mitigation:** CellFormatting is fast (no DB queries, just ARGB → Color conversion). Tested on grids with 100+ rows.

---

## Future Enhancements (Out of Scope for v0.7.0)

1. **Per-View Font Size:** Add `FontSize` column to miniviews table
2. **Per-View Opacity:** Add `Opacity` column to miniviews table
3. **Per-View Warning Threshold:** Add `WarnTime` column to miniviews table (override global)
4. **View Properties Dialog:** Double-click view row to open full properties editor
5. **View Templates:** Export/import view configurations as JSON
6. **View Grouping:** Group multiple views into "profiles" (Raid, Solo, Group, etc.)

---

## Implementation Order

### Phase 1: Database Foundation
1. Add columns to miniviews table
2. Implement migration logic
3. Seed default views for new databases
4. Update ViewPositionData class
5. Update Database queries (GetViewPositions, GetViews, SaveView)

### Phase 2: ViewsGridController & Views Grid UI
6. **Create ViewsGridController.cs class** (new file)
7. Extract grid setup logic into controller.SetupGrid()
8. Extract CellClick handler into controller (color picker)
9. Extract CellFormatting handler into controller (colored boxes + preview)
10. Update FormMain.cs to use controller instance
11. Test color picker functionality
12. Test preview column rendering

### Phase 3: Mini-View Refactor
13. Remove ColorType enum from MiniView.cs
14. Simplify SetAppearance() signature
15. Update LoadData() to use ShowWarning flag
16. Remove global color fields from MiniViews.cs
17. Remove GetStyleColors() method
18. Update UpdateMiniAppearance() to use per-view colors

### Phase 4: Settings Cleanup
19. Remove per-style color pickers from Settings tab
20. Remove chkShowPing checkbox
21. Add info label explaining view colors
22. Update FormMain_Load() / ReloadFromDatabase()

### Phase 5: Add New Styles
23. Add Spawn/Lockout/Character to dropdowns
24. Update tooltips
25. Seed default views for new styles

### Phase 6: Testing & Validation
26. Test new database creation
27. Test migration from old databases
28. Test color picker UI
29. Test mini-view behavior (colors + warning flags)
30. Test grid row colors

---

## Success Criteria

✅ Users can configure per-view colors directly in Views grid  
✅ Users can control warning colors per view via checkbox  
✅ Settings tab is cleaner (no per-style color pickers)  
✅ Existing databases migrate seamlessly  
✅ New databases seed with sensible defaults  
✅ Mini views display with per-view colors  
✅ Pet gets distinct color (purple, not orange)  
✅ Spawn/Lockout/Character styles added  
✅ No code changes needed to add future per-view properties  

---

## Approval

**Ready to proceed?** Please review this plan and confirm:
- [ ] Database schema changes look correct
- [ ] UI approach (color pickers in grid) is acceptable
- [ ] ViewsGridController extraction makes sense
- [ ] Migration strategy preserves user data
- [ ] Scope is appropriate for v0.6.0 agile pivot

Once approved, I'll create `mini-view-per-view-colors-progress.md` and begin implementation with Phase 1 (Database).
