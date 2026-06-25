# Timer Grid Filter Refactor — Design Note

**Status:** Implemented on `v0.6.0-gui-enhancements`
**Author:** Performance investigation, May 2026
**Branch:** `v0.6.0-gui-enhancements`
**Related code:** `ThorneTimer/FormMain.cs` — `RefreshTimerGridDataSource`, `SyncRuntimeToGrid`, `RefreshGridAfterSort`

---

## TL;DR

Character switches and startup currently take **~2 seconds**. Profiling proved
**~90 %** of that cost is a single line:

```csharp
row.Visible = visible;
```

…executed inside a loop over all 118 timer rows in `RefreshTimerGridDataSource`.
Each setter triggers internal `DataGridView` work (≈14 ms × 118 rows ≈ 1.7 s).

The fix is to stop hiding rows one-by-one and instead **bind the grid to a
pre-filtered list**. One DataSource assignment ⇒ one layout pass.

Estimated impact: **~1700 ms → <50 ms** per character switch.

---

## What the data says

| Operation                            | Before |
| ------------------------------------ | -----: |
| `FormMain_Load TOTAL`                | 2754 ms |
| ↳ `RefreshGridAfterSort` (startup)   | 1803 ms |
| `CharacterSwitch TOTAL (manual)`     | ~2050 ms |
| ↳ `RefreshGridAfterSort`             | ~1850 ms |
| ↳↳ `visibility loop (118 rows)`      | ~1650 ms ⚠ |
| Everything else (DB, runtime, sync)  | ~150 ms |

Two mitigation attempts already shipped but **had no measurable effect**:

1. Caching column indices outside the loop.
2. Guarding the setter (`if (row.Visible != visible)`).
3. `CurrencyManager.SuspendBinding()` around the loop.

The cost is intrinsic to `DataGridView.Row.Visible` and **cannot** be reduced
without changing the mechanism. The above three tweaks remain in the code
because they are harmless and correct; they just don’t move the number.

---

## Current architecture (today)

```
SQLite (timers table)
   │
   ▼
TimersRepository.GetTimers()
   │  returns SortableBindingList<Timers.GridData>
   ▼
grdTimers.DataSource = <that list>          ← set once, in SetupTimerGrid
                                              (lines ~1705)

TimerRuntime  ──► holds per-timer runtime state (count, remaining, button…)

On every character switch / startup:
   SyncRuntimeToGrid()                ← walks grdTimers.Rows, copies state into cells
   RefreshTimerGridDataSource()       ← walks grdTimers.Rows, toggles row.Visible
                                        based on ClassID + ActiveYn filters
   UpdateSortGlyphs()
```

`grdTimers.DataSource` is **assigned exactly once** (line 1705). Filtering is
done by mutating row visibility in place. That is the hot path.

---

## Proposed architecture

```
SQLite (timers table)
   │
   ▼
TimersRepository.GetTimers()
   │  returns SortableBindingList<Timers.GridData>   ← full set
   ▼
_allTimers     (new private field — full unfiltered list)
   │
   │  RefreshTimerGridDataSource() applies filter
   ▼
_visibleTimers = new SortableBindingList<Timers.GridData>(
                    _allTimers.Where(passesFilter))
   ▼
grdTimers.DataSource = _visibleTimers      ← reassigned on filter change
```

### Key change

- **`RefreshTimerGridDataSource`** stops walking `grdTimers.Rows`. Instead it
  builds the filtered subset and assigns it to `DataSource` once.
- **`SyncRuntimeToGrid`** is unchanged in shape — it still walks
  `grdTimers.Rows` updating cells. After the swap there are simply fewer rows
  in the grid (only the visible ones), so it also gets faster.

### Why this is fast

WinForms `DataGridView` handles a `DataSource` swap as a single bulk operation:
one virtualized layout, one scrollbar recalc, one repaint. The 118 individual
`row.Visible` setters become **zero**.

---

## What needs to change (concrete checklist)

All edits are confined to **`ThorneTimer/FormMain.cs`**:

1. **Add private fields** to hold the full list and the current filter signature.
2. **`SetupTimerGrid`** (line ~1705): load into `_allTimers`, then assign
   `_visibleTimers` (initially equal to `_allTimers`) to `grdTimers.DataSource`.
3. **`RefreshTimerGridDataSource`**: replace the visibility loop with:
   - Compute filter signature; bail if unchanged (cheap short-circuit).
   - Capture current sort + selected timer ID.
   - Build new `SortableBindingList<>` from filtered slice.
   - Assign to `grdTimers.DataSource`.
   - Reapply sort via `SortableBindingList.ApplySort(...)`.
   - Restore selection if the timer is still visible.
4. **`SyncRuntimeToGrid`**: unchanged.
5. **`LoadTimerRuntime`** (character switch path): load into `_allTimers`
   instead of straight into `DataSource`; let
   `RefreshTimerGridDataSource` build the visible view.
6. **Anywhere that mutates `row.Visible` directly**: nothing else does today
   (already audited).

Estimated diff: **~80–120 lines added, ~50 removed**, all in one file.

---

## What does **NOT** change

- ✅ Public behavior of the timer grid (sorting, columns, paint, sort glyphs).
- ✅ Timer runtime engine (`TimerRuntime`, `TimerState`, `LogMonitor`).
- ✅ Database schema and repositories (`TimersRepository`, `TimerStateRepository`,
  `CategoriesRepository`, etc.).
- ✅ Mini views, styles, views, categories tabs.
- ✅ Character switching semantics (save outgoing, restore incoming).
- ✅ Persisted column widths, sort state, per-character ActiveYn preferences.
- ✅ `BeginGridUpdate`/`EndGridUpdate` helpers and `grdTimers.Visible = false`
  guards around bulk operations.

---

## Do we need to back any changes out? **No.**

The performance work shipped so far is purely additive and harmless:

| Change                                               | Keep? | Why |
|------------------------------------------------------|:-----:|-----|
| `ThorneLog.Time(label)` `PerfScope` helper           | ✅ | Useful indefinitely; zero overhead when disabled. |
| `PERF […]` log lines around startup/switch hot paths | ✅ | Lets us re-measure after the refactor. |
| Cached column indices in `RefreshTimerGridDataSource`| ✅ | Tiny correctness/clarity win, no cost. |
| `if (row.Visible != visible)` guard                  | 🟡 | Becomes dead code after the refactor (no more `row.Visible` writes). Safe to delete in the same commit. |
| `CurrencyManager.SuspendBinding/ResumeBinding`       | 🟡 | Same — surrounds a loop that no longer exists. Safe to delete. |

So the refactor is a forward step; nothing has to be reverted first. The two
🟡 items get cleaned up *as part of* the refactor commit because the code
they wrap is being replaced.

---

## Risks and how we mitigate them

| Risk | Likelihood | Mitigation |
|------|:----------:|------------|
| Sort order is lost when `DataSource` is reassigned. | High | Re-apply via `SortableBindingList.SortDescriptions` after the swap (mechanism already exists for refresh-sort). |
| Selected row is lost when `DataSource` is reassigned. | Medium | Capture selected `TimerID` before swap; re-select after if still visible. |
| Cell colors / `ActiveYn` checkbox state get reset because cell styles live on rows that are now new rows. | Medium | `SyncRuntimeToGrid` already reapplies all per-cell visuals; we keep calling it after the swap (it’s our pattern today). |
| Persisted column widths flicker because the grid rebuilds rows. | Low | Column widths are properties of `DataGridViewColumn`, not rows; unaffected by row replacement. |
| Mini views read from `grdTimers.Rows`. | Low | Verified: mini views read from `TimerRuntime`, not the grid. No coupling. |
| Per-character `ActiveYn` preference gets lost. | Low | `ActiveYn` lives on `TimerState` (`TimerRuntime`), not on the grid row. Filter only *reads* it. |

---

## New rows under the filtered data source

A reasonable concern: *“If a user clicks Add Timer and a new row hasn’t had
its filter columns (ClassID / ActiveYn) set yet, will it vanish before they
can edit it?”*

**Answer: No, because of how `btnAddTimer_Click` already initializes the
row.** The existing code (lines 3149–3210 in `FormMain.cs`) creates the
new `Timers.GridData` with explicit defaults:

```csharp
Timers.GridData gd = new Timers.GridData
{
    ID        = -1,
    ActiveYn  = 1,     // ← passes "Show Active Only"
    Style     = "Normal",
    Scope     = "World",
    ClassID   = 0,     // ← passes class filter ("all classes")
    Duration  = noTime
};
```

Both filter inputs (`ClassID = 0` and `ActiveYn = 1`) are filter-safe by
construction — the new row passes whatever combination of
`ShowAllClasses` / `ShowActiveOnly` is active.

### What the refactor adds to be safe

1. The new `Timers.GridData` is appended to **`_allTimers`** (the full list).
2. It is also appended to **`_visibleTimers`** (the filtered list bound to
   the grid) so the row appears in the grid immediately, without any filter
   recompute. This mirrors today’s behavior where `data.Add(gd)` makes the
   row appear instantly.
3. The user edits ClassID / ActiveYn / etc. and saves.
4. On the *next* filter-changing event (character switch, toggle), the
   filter is recomputed. If the user has set ClassID to a class that
   doesn’t match the active character and Show All Classes is off, the row
   filters out — **exactly as today**.

### What about row deletion?

Same pattern: the existing delete path removes from the bound list. The
refactor will route the removal through a helper that removes from both
`_allTimers` and `_visibleTimers` (if present). No new semantic change.

### Manual verification suggested

After the refactor, smoke-test:

- [ ] Add Timer → new row appears at cursor → edit Name, Duration → Save.
- [ ] Add Timer with Show Active Only ON → row appears (ActiveYn=1 by default).
- [ ] Set ClassID on a new row to a class that doesn’t match active character,
      with Show All Classes OFF → on next switch/toggle, row hides (matches
      current behavior).
- [ ] Delete a timer → row vanishes from grid and full list.
- [ ] Re-add same name → no duplicate, no ghost.

---

## Bundled-with: color polish (small, separate concern)

This commit is also a good moment to apply two small color tweaks the user
flagged while reviewing this design:

1. **Inactive row background**: `Color.LightPink` → `Color.Gainsboro`
   (light gray). The pink read too close to user-chosen red style colors
   (e.g. Lockout). Gray is neutral and reads clearly as “inactive” without
   competing with any style.
2. **Default style colors** (`StylesRepository.SeedDefaultStyles`): updated
   to the user’s curated palette from their working `.tdb`, so a fresh
   install starts with a usable palette out of the box. These defaults are
   only seeded into **new** databases — existing `.tdb` files are
   untouched (one-shot migration rule from `Database.cs`).

   | Style     | Old default                  | New default              |
   |-----------|------------------------------|--------------------------|
   | Normal    | Yellow `#FFFF00`             | Light cyan `#80FFFF`     |
   | Buff      | Orange `#FFA500`             | Amber `#FFB833`          |
   | Pet       | Light purple `#DCA0FF`       | Lighter lavender `#E4B9FF` |
   | Ping      | LightGreen `#90EE90`         | Neon green `#2BFF2B`     |
   | Spawn     | Cyan `#00FFFF`               | Yellow `#FFFF00`         |
   | Lockout   | DodgerBlue `#1E90FF`         | Salmon `#FF7D7D`         |
   | Character | White `#FFFFFF`              | White `#FFFFFF` (unchanged) |

   These two color changes ship in the same commit as the refactor (or
   immediately before it) so v0.6.0 stable presents a coherent visual
   identity.

---

## Rollout plan

1. **Branch:** continue on `v0.6.0-gui-enhancements` — no separate branch needed.
2. **Single commit:** `perf(grid): filter via DataSource swap to eliminate per-row Visible cost`.
3. **Validation:**
   - Build clean.
   - Run app → confirm grid renders, sort works, selection works, character
     switch is fast.
   - Check the `PERF` lines in the latest log; expect
     `RefreshGridAfterSort` to drop from ~1800 ms to <100 ms.
   - Toggle Show All Classes / Show Active Only / Group Sort and verify
     filter changes are instant.
   - Run through the same scenarios you tested for v0.6.0 beta (timers,
     mini views, manual + auto character switch).
4. **If anything regresses:** revert the single commit. Nothing else depends
   on this change.

---

## Decision

If you’re comfortable with the scope above, I’ll implement it as a single
focused commit. Estimated wall time to implement + verify: short. If
you’d rather ship v0.6.0 as-is and tackle this in v0.6.1, that’s also a
clean option — the perf instrumentation already in place will let us pick
up the work later without re-discovering the bottleneck.
