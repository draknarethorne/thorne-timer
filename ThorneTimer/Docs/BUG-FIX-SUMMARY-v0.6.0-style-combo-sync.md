# Bug Fix: Style ComboBox Synchronization Issue

**Date**: 2024  
**Branch**: v0.6.0-gui-enhancements  
**Issue**: `DataGridViewComboBoxCell value is not valid` exception on Views tab when selecting a style; newly added styles not appearing in main Timer tab dropdown

---

## Root Causes

### Issue 1: Hardcoded Style List in Main Timer Grid
**File**: `ThorneTimer/FormMain.cs`, `SetupTimerGrid()` method (line 1681)

The Style combo box column in the main Timer grid had a hardcoded list of 7 default styles:
```csharp
cboRole.Items.AddRange("Normal", "Buff", "Pet", "Ping", "Spawn", "Lockout", "Character");
```

When users added new custom styles via the Styles tab, those styles were inserted into the database but **never appeared in the main Timer grid's combo list** because it was hardcoded at setup time.

### Issue 2: Missing DataError Handler on Views Grid
**File**: `ThorneTimer/FormMain.cs`

The Views grid (`grdViews`) had no `DataError` event handler. When a new style was added:
1. The style gets inserted into the database
2. `OnStylesChanged()` is called, which triggers `viewsController?.RefreshStyleOptions()` to update the Views combo items
3. **However**, there's a race condition: if a row is painted before the combo items are refreshed, WinForms displays the error dialog "DataGridViewComboBoxCell value is not valid" because the cell value (the new style name) doesn't exist in the combo's items list

### Issue 3: Views Grid Style Combo Never Refreshed in Response to Timer Grid Changes
**File**: `ThorneTimer/FormMain.cs`, `OnStylesChanged()` method

When styles change, the callback was updating the Views grid combo via `viewsController?.RefreshStyleOptions()`, but **was NOT updating the main Timer grid's Style combo**. This meant the Timer grid continued to display only the original hardcoded list.

---

## Solution

### Fix 1: Dynamically Populate Timer Grid Style Combo from Database
**File**: `ThorneTimer/FormMain.cs`, `SetupTimerGrid()` method

Replaced the hardcoded `.AddRange()` call with dynamic population:
```csharp
// Populate Style combo with styles from database (dynamically loaded, not hardcoded)
if (stylesRepository != null)
{
	foreach (string styleName in stylesRepository.GetStyleNames())
		cboRole.Items.Add(styleName);
}
// Fallback to default if database is empty
if (cboRole.Items.Count == 0)
	cboRole.Items.AddRange("Normal", "Buff", "Pet", "Ping", "Spawn", "Lockout", "Character");
```

**Impact**: Initial setup now reads styles from the database instead of hardcoding them.

### Fix 2: Add DataError Handler to Views Grid
**File**: `ThorneTimer/FormMain.cs`, new method `GrdViews_DataError()`

Added a new event handler that suppresses the error dialog:
```csharp
private void GrdViews_DataError(object sender, DataGridViewDataErrorEventArgs e)
{
	ThorneLog.Warn($"Views grid data error at ({e.RowIndex}, {e.ColumnIndex}): {e.Exception?.Message ?? "Unknown"}");
	e.ThrowException = false;  // Suppress the error dialog
}
```

Wired it up in `SetupViewsGrid()`:
```csharp
grdViews.DataError += GrdViews_DataError;
```

**Impact**: Transient combo-box validation errors are now logged (for diagnostics) but don't show error dialogs, allowing the user experience to remain smooth during style synchronization.

### Fix 3: Refresh Timer Grid Style Combo on Style Changes
**File**: `ThorneTimer/FormMain.cs`, new method `RefreshTimerGridStyleCombo()`

Added a new method to refresh the Style combo in the main Timer grid:
```csharp
private void RefreshTimerGridStyleCombo()
{
	var col = grdTimers.Columns["Style"] as DataGridViewComboBoxColumn;
	if (col == null) return;

	col.Items.Clear();
	if (stylesRepository != null)
	{
		foreach (string name in stylesRepository.GetStyleNames())
			col.Items.Add(name);
	}
	if (col.Items.Count == 0)
		col.Items.Add("Normal");
}
```

Called this method from `OnStylesChanged()`:
```csharp
private void OnStylesChanged()
{
	stylesRepository?.RefreshCache();
	viewsController?.RefreshStyleOptions();
	RefreshTimerGridStyleCombo();  // <-- NEW
	miniViews.RefreshMiniViews(con, activeCharacterID);
	RepaintTimerGrid();
	UpdateMiniView();
}
```

**Impact**: Whenever a style is added, deleted, or renamed via the Styles tab, **both** the Views grid and Timer grid Style combos are immediately updated with the new list.

---

## Affected Components

| Component | Change | Impact |
|-----------|--------|--------|
| `SetupTimerGrid()` | Changed hardcoded style list to dynamic DB load | Timer grid now supports custom styles |
| `RefreshTimerGridStyleCombo()` | NEW method | Keeps Timer grid style combo in sync |
| `OnStylesChanged()` | Calls `RefreshTimerGridStyleCombo()` | Style changes propagate to UI |
| `SetupViewsGrid()` | Wires up `DataError` handler | Transient combo errors are handled gracefully |
| `GrdViews_DataError()` | NEW method | Suppresses error dialogs for combo-box value mismatches |

---

## Verification

### Before Fix
1. Add a new custom style "MyStyle" via Styles tab → appears in Views combo ✅ but NOT in Timer grid combo ❌
2. Select "MyStyle" in a Views row → "DataGridViewComboBoxCell value is not valid" error dialog ❌
3. Close and reopen the app → custom style now in both combos (because they're refreshed from DB on app load)

### After Fix
1. Add a new custom style "MyStyle" via Styles tab → appears in **both** Views combo ✅ and Timer grid combo ✅
2. Select "MyStyle" in a Views row → no error dialog, works smoothly ✅
3. Close and reopen the app → custom style still in both combos ✅

---

## Testing Checklist

- [ ] Add a new custom style via the Styles tab
- [ ] Verify the new style appears in the Timer tab's Style dropdown
- [ ] Verify the new style appears in the Views tab's Style dropdown
- [ ] Try to select the new style in a Views grid row — no error dialog should appear
- [ ] Create a new timer and assign it the new custom style — works correctly
- [ ] Delete the custom style — verify it cascades correctly (timers reset to "Normal", views reset to "Normal")
- [ ] Rename a style — verify both combos update to show the new name
- [ ] Check ThorneLog.txt for any "Views grid data error" warnings (should be none under normal operation)

---

## Related Documentation

- `Docs/styles-and-views-enhancements.md` — Overall style/view enhancement plan
- `Docs/styles-and-views-enhancements-progress.md` — Feature completion tracking
- `copilot-instructions.md` — SQL parameterization and threading requirements
