# Styles & Views Enhancements — v0.6.0 Mini-View Skin Engine

> **Status:** Design / Spec — pre-implementation
> **Version:** v0.6.0
> **Branch:** `v0.6.0-gui-enhancements`
> **Tracking plan:** `plan-c124891a-3bf9-4da8-98d6-22cfb1a9f0cb.md` ("v0.6.0 Mini-View Skin Engine")
> **Date:** 2026-06-03
>
> This document is the **detailed, durable specification** behind the tracked plan. If chat
> context is lost, this file is the source of truth for the agreed-upon behavior (time
> formats, new style/view fields, schema, and UI). Adjust this doc + the plan `.md`
> **before** implementation begins.

---

## 1. Goal

Replace the current `TableLayoutPanel`-based `MiniView` with a **layered-window, custom-paint
"skin engine"** and expand the **Styles** and **Views** tabs so users get rich, per-view
appearance control:

- Per-**style**: time format, font size/bold/italic, optional icon slot.
- Per-**view**: render engine (Classic/Thorne), time placement (Left/Right), background
  opacity, header on/off + text, row spacing, fixed width, lock position, click-through.
- Global: configurable font family (Settings tab).

The skin engine is proven out first by the throwaway spike at
`ThorneTimer\Spike\LayeredMiniViewSpike.cs` (per-pixel alpha via `UpdateLayeredWindow`,
drag via `WM_NCLBUTTONDOWN`, right-click `ContextMenuStrip`, per-row invalidation). Once the
production `MiniView` rewrite lands and is validated, the `Spike\` folder is deleted.

---

## 2. Time Formats (agreed design)

### 2.1 The format catalog

Four named formats. Samples shown for a **long** timer (1d 4h 5m 22s), a **mid** timer
(1h 23m 45s), and a **sub-minute** timer (45s) so the differences are obvious:

| Name              | Sample (long timer) | Sample (mid) | Sample (sub-minute) |
|-------------------|---------------------|--------------|---------------------|
| **Classic** (current, default) | `1d 04:05:22` | `01:23:45` | `00:00:45`     |
| **Long**          | `1d 4:05:22`        | `1:23:45`    | `0:45`              |
| **Adaptive Compact** | `1d 4h`          | `1h 23m`     | `45s`               |
| **Full Compact**  | `1d 4h 5m 22s`      | `1h 23m 45s` | `45s`               |

**Notes / exact semantics:**

- **Classic (current, default)** — matches today's `TimerPlus.GetTimeRemaining()` exactly. It
  **always shows full zero-padded `HH:MM:SS` digits** (hours, minutes, and seconds all padded
  to two places), so a running timer reads `01:23:01` or `00:00:32`:
  - With days: `"{Days}d {Hours:00}:{Minutes:00}:{Seconds:00}"` → `1d 04:05:22`.
  - Without days: `"{Hours:00}:{Minutes:00}:{Seconds:00}"` → `01:23:45`, `00:00:45`.
  - **Confirmed:** "Classic" is a true no-op rename of existing output — no padding change.
    It remains the **default** `TimeFormat` so existing `.tdb` files render unchanged.
- **Long** — the standard "stopwatch/video-player" convention: **collapse** to the
  most-significant non-zero unit, drop that leading unit's zero-padding, and keep every lower
  unit two-digit zero-padded. Units below the largest present unit are always shown:
  - `≥ 1 day`  → `"{Days}d {Hours}:{Minutes:00}:{Seconds:00}"` → `1d 4:05:22`.
  - `≥ 1 hour` → `"{Hours}:{Minutes:00}:{Seconds:00}"`         → `1:23:45`.
  - `< 1 hour` → `"{Minutes}:{Seconds:00}"`                    → `0:45`, `12:05`.
    (The hours slot collapses away under an hour — that's why `0:00:45` becomes `0:45`.)
  - **Render right-justified.** Because the field width changes as a timer crosses
    minute/hour/day boundaries, Long **must** be right-aligned in mini views and the grid so a
    stack of timers stays column-aligned on the right edge instead of drifting left. Classic's
    fixed width doesn't depend on this, but right-justify is the canonical alignment for both.
- **Adaptive Compact** — show only the **two most significant** non-zero units, largest first:
  - `≥ 1 day`  → `Dd Hh`         (e.g. `1d 4h`)
  - `≥ 1 hour` → `Hh Mm`         (e.g. `1h 23m`)
  - `≥ 1 min`  → `Mm Ss`         (e.g. `23m 45s`)
  - `< 1 min`  → `Ss`            (e.g. `45s`)
- **Full Compact** — every non-zero unit, largest→smallest, space-separated, suffix letters:
  - `1d 4h 5m 22s`, `1h 23m 45s`, `45s`. Zero-value leading/trailing units are omitted
    (so `1h 23m 45s` has no `0d`, and `45s` has no `0h 0m`).

### 2.2 Where time format applies

- The format is a **per-style** property (column on the `styles` table). Every timer rendered
  with that style — in **mini views** *and* in the **main grid Remaining column** — uses the
  style's chosen format, so what the user previews is exactly what they get everywhere.
- `TimePlacement` (separate **per-view** field, see §3.2) controls *which side* of the row the
  time slot sits on (Left/Right), independent of the *format string* itself.
- **Alignment:** the time slot is **right-justified** by default. This is essential for the
  variable-width formats (Long, Adaptive/Full Compact) so a vertical stack of timers stays
  aligned on the right edge instead of drifting left as units collapse — the original reason
  Classic's fixed `HH:MM:SS` width existed. The fixed time-slot column in the §8 row layout
  reserves enough width for the widest expected value and right-aligns the rendered text.

### 2.3 `TimerTimeFormatter` helper

New static helper class `TimerTimeFormatter` is the single source of truth for rendering.
`TimerPlus.GetTimeRemaining()` and the main grid Remaining column both call into it.

```csharp
public enum TimeFormat
{
    Classic = 0,          // 1d 04:05:22   (current behavior, default — always zero-padded)
    Long = 1,             // 1d 4:05:22 / 0:45  (collapses, right-justified)
    AdaptiveCompact = 2,  // 1d 4h         (two most-significant units)
    FullCompact = 3       // 1d 4h 5m 22s  (all non-zero units)
}

public static class TimerTimeFormatter
{
    // Canonical entry point. Negative spans clamp to zero.
    public static string Format(TimeSpan remaining, TimeFormat format);

    // Convenience for grid/db code that stores the enum as int.
    public static string Format(TimeSpan remaining, int format)
        => Format(remaining, (TimeFormat)format);
}
```

### 2.4 Styles tab "sample" affordance

- The Styles tab gets **one new column: "Time Format"** (dropdown bound to the `TimeFormat`
  names above).
- A **sample row / preview** re-renders a fixed sample `TimeSpan` (use **`1h 23m 45s`**) so
  users see exactly what they'll get the moment they change the dropdown. In the new
  `StyleEditorDialog`, the live `StylePreviewPanel` shows the same sample.

### 2.5 Implementation status (landed)

- ✅ `TimerTimeFormatter` + `TimeFormat` enum added (`Classic`/`Long`/`AdaptiveCompact`/`FullCompact`),
  with `Classic` reproducing the original `GetTimeRemaining()` output byte-for-byte.
- ✅ `TimerPlus.GetTimeRemaining()` delegates to the formatter; added `GetTimeRemaining(TimeFormat)`
  overload for style-aware callers.
- ✅ `styles.TimeFormat` column added (idempotent `ALTER TABLE` migration, fresh-DB `CREATE`,
  hydration + persistence in `StylesRepository`); `StyleData.TimeFormat` defaults to `Classic`.
- ✅ `TimerRuntime` resolves the timer's style → `TimeFormat` via `StyleTimeFormatResolver`
  (wired from `FormMain` to `StylesRepository`) so the **main grid Remaining column** honors it.
- ✅ Styles tab "Time Format" dropdown column added with live `Example` preview.
- ✅ **Warning detection is format-independent.** Mini-view warning colors are driven by the
  timer's **raw remaining milliseconds** (carried through `MiniTimerData.RemainingMs` →
  `MiniData.RemainingMs`), not by re-parsing the formatted display string. This fixes false
  warnings under lossy/compact formats (e.g. `AdaptiveCompact` `"1d 4h"`, `FullCompact` `"45s"`)
  that `TimerPlus.GetMilliseconds` cannot parse. `MiniView.IsWarning(md)` falls back to string
  parsing only when raw ms is unavailable (negative).

---

## 3. New / changed data fields

### 3.1 `StyleData` (`Styles.cs`) — `styles` table

| Field          | Type (C#) | DB column     | Default              | Meaning |
|----------------|-----------|---------------|----------------------|---------|
| *(existing)* ID, Name, ForeColor, BackColor, SortOrder | — | — | — | unchanged |
| `TimeFormat`   | `int`     | `TimeFormat`  | `0` (Classic)        | One of §2 `TimeFormat` enum. |
| `FontSize`     | `int`     | `FontSize`    | `0` (inherit/global) | Per-style point size; `0` = use the view/global default. |
| `FontBold`     | `int`     | `FontBold`    | `0`                  | Bool-as-int (0/1). |
| `FontItalic`   | `int`     | `FontItalic`  | `0`                  | Bool-as-int (0/1). |
| `ShowIconSlot` | `int`     | `ShowIconSlot`| `0`                  | Reserve the middle icon column for this style's rows. |

> `ForeColor` remains the **canonical style color** — main grid lightens it for the row tint,
> mini views use it directly for timer text. (See copilot-instructions "Style colors".)

### 3.2 `ViewData` (`Views.cs`) — `miniviews` table

| Field             | Type (C#) | DB column          | Default     | Meaning |
|-------------------|-----------|--------------------|-------------|---------|
| *(existing)* ID, Name, ActiveYn, StyleFilter, PositionX, PositionY, SortOrder, ShowWarning, EmptyBehavior | — | — | — | unchanged |
| `RenderEngine`    | `int`     | `RenderEngine`     | `0` (Classic) | Which renderer paints this view: `0=Classic` (`MiniView`), `1=Thorne` (`ThorneView`). Subject to the global force-Classic override (§10). |
| `BackgroundOpacity` | `int`   | `BackgroundOpacity`| `100`       | 0–100 %; drives per-pixel alpha of the panel background in the layered window. |
| `TimePlacement`   | `int`     | `TimePlacement`    | `0` (Left)  | Which side of the row the time slot occupies. `0=Left` (canonical / current behavior: time to the **left** of the name), `1=Right` (time to the **right** of the name). Time text is **right-justified within its slot**, name is **left-justified within its slot**, either way. **Thorne-engine only** — the Classic render engine always draws its fixed time-on-left layout and ignores this field. |
| `ShowHeader`      | `int`     | `ShowHeader`       | `0`         | Bool-as-int; draw a header bar above the rows. |
| `HeaderText`      | `string`  | `HeaderText`       | `""`        | Header label; falls back to view Name when empty and `ShowHeader=1`. |
| `RowSpacing`      | `int`     | `RowSpacing`       | `1`         | Pixels between rows. |
| `FixedWidth`      | `int`     | `FixedWidth`       | `0`         | `0` = auto-size to content; `>0` = fixed pixel width. |
| `LockPosition`    | `int`     | `LockPosition`     | `0`         | Bool-as-int; disables drag (no `WM_NCLBUTTONDOWN`). |
| `ClickThrough`    | `int`     | `ClickThrough`     | `0`         | Bool-as-int; `WS_EX_TRANSPARENT` so clicks pass through to the game. |

#### 3.2.1 Why `TimePlacement` is per-view (not per-style) — and how it relates to `RenderEngine`

- **Scope decision:** time placement is an *arrangement* concern about how a whole stack of
  rows lines up, which is what a **view** owns. Styles own *appearance* (color, font, time
  **format**); views own *layout* (placement, spacing, header, opacity, width). So
  `TimePlacement` lives on `miniviews`, **not** `styles`. This also avoids forcing two views
  that share a style into the same layout.
- **No config conflict:** `TimePlacement` is orthogonal to every other field — `TimeFormat`
  (style) decides *how the time string reads*; `TimePlacement` (view) decides *which side the
  time slot sits on*. It does not interact with `BackgroundOpacity`, `FixedWidth`,
  `ShowHeader`, `RowSpacing`, `LockPosition`, `ClickThrough`, or `RenderEngine` values.
- **Naming guardrail — keep the two axes distinct:**
  - **`RenderEngine`** = *which painter* draws the view: **Classic** (`MiniView`, the
    fixed-layout fallback) vs. **Thorne** (`ThorneView`, the new skin engine). This is the
    **kill-switch** (§10), preserved exactly as defined.
  - **`TimePlacement`** = *cosmetic time side* within the Thorne layout: **Left** vs.
    **Right**. Stored/enumerated as `Left`/`Right`, **not** "Classic/Thorne", so a single view
    doesn't carry two different "Classic/Thorne" toggles meaning different things. The UI may
    show a friendly label, but the field stays `TimePlacement`.
- **Engine dependency:** `TimePlacement` is a **Thorne-engine feature**. When
  `RenderEngine = Classic`, the old renderer draws its fixed time-on-left layout and ignores
  `TimePlacement`. Default `0 = Left` means a view upgraded onto the Thorne engine looks the
  same as it did under Classic until the user opts to flip it.

### 3.3 `EmptyBehavior` / placeholder unification (plan step 7)

- Today `EmptyBehavior` is a string on `miniviews` (`"ViewName"` default; the per-view
  `EmptyText` is computed in `MiniView`). The plan **unifies** empty-state handling so a view
  can show: the **view name**, **custom placeholder text**, or **nothing** (collapse/hide).
- Proposed values for `EmptyBehavior`: `"ViewName"` (default), `"Placeholder"`, `"Hidden"`.
- When `EmptyBehavior = "Placeholder"`, the text comes from a dedicated
  `PlaceholderText TEXT DEFAULT ''` column on `miniviews` (**decided**, §9.2). It is only
  consulted in the `"Placeholder"` branch; `"ViewName"` and `"Hidden"` ignore it.

### 3.4 `settings` table — global font family (plan step 8/23)

| Field        | DB column    | Default       | Meaning |
|--------------|--------------|---------------|---------|
| `FontFamily` | `FontFamily` | `"Segoe UI"`  | Global mini-view font family; per-style size/bold/italic layer on top. Exposed via a picker in the Settings tab "Mini Views" group. |

---

## 4. Schema migrations

All migrations live in `Database.cs` / `StylesRepository.EnsureSchema` / `ViewsRepository`
and **must follow the existing idempotent, one-shot pattern**:

- Use `Database.isTableExist` / `Database.isFieldExist` so re-running is safe.
- **Never re-seed defaults into an existing table** — user deletions and edits must stick.
- New columns are added with `ALTER TABLE ... ADD COLUMN ... DEFAULT <x>` guarded by
  `isFieldExist`, so existing `.tdb` files upgrade in place without losing data.

Migration steps (map to plan steps 5–8):

1. `styles`: add `TimeFormat, FontSize, FontBold, FontItalic, ShowIconSlot`.
2. `miniviews`: add `RenderEngine, TimePlacement, BackgroundOpacity, ShowHeader, HeaderText, RowSpacing, FixedWidth, LockPosition, ClickThrough` (+ `PlaceholderText` per §3.3 decision).
3. Unify `EmptyBehavior` values / add `PlaceholderText`.
4. `settings`: add `FontFamily`.

---

## 5. Repository changes

- **`StylesRepository`** (`GetStyles`, `GetStyle`, save/insert, `EnsureSchema`, `SeedDefaultStyles`):
  extend `SELECT`/`INSERT`/`UPDATE` column lists and the `StyleData` hydration to include the
  new fields. Keep all SQL **parameterized**. `GetRowBaseColor` / `ForeColor` semantics
  unchanged.
- **`ViewsRepository`** (`GetViews`, `SaveView`, `DeleteView`): extend `SELECT`/`INSERT`/
  `UPDATE` for the new per-view fields. Preserve existing null-coalescing defaults pattern.

---

## 6. Rendering integration

- **`TimerRuntime`** (plan step 11): use `TimerTimeFormatter` for any countdown text it owns.
- **`TimerPlus.GetTimeRemaining()`**: delegate to `TimerTimeFormatter.Format(span, format)`;
  default format keeps current **Classic** output so nothing regresses for users who don't
  touch the new column.
- **Main grid Remaining column** (plan step 12, `FormMain`): format via the timer's style's
  `TimeFormat` so the grid matches the mini view and the Styles-tab preview.

---

## 7. UI rework

### 7.1 Reusable preview (plan step 13)
- `StylePreviewPanel` user control: renders a representative row (and the §2.4 sample time)
  using a `StyleData` + optional `ViewData`, so both editor dialogs show a **live preview**.

### 7.2 Editor dialogs (plan steps 14–15)
- `StyleEditorDialog`: edit a single style (colors, **Time Format**, font
  size/bold/italic, icon slot) with live `StylePreviewPanel`.
- `ViewEditorDialog`: edit a single view (render engine, **time placement**, style filter,
  opacity, header, row spacing, fixed width, lock, click-through, empty behavior) with live
  preview.

### 7.3 Grid rework (plan steps 16–17)
- `StylesController` and `ViewsController` grids move from inline cell editing to a **summary
  grid + "Edit…" button** that opens the corresponding dialog. Keep Add/Delete/Rename.
- The Styles grid still surfaces the new **"Time Format"** column inline for quick scanning,
  with the sample preview.

### 7.4 Settings tab (plan step 23)
- Add a **Font Family** picker to the Settings tab "Mini Views" group, persisting to
  `settings.FontFamily`.

### 7.5 Grid column-layout persistence (pre-work fix — completed)

Done **before** the §7.3 grid rework as a prerequisite, since the rework touches the same
grids. Three defects in per-grid column persistence were fixed:

1. **Fill-mode corruption (the Characters/Categories misalignment).** All main-form grids use
   `AutoSizeColumnsMode = Fill`, where layout is driven by `FillWeight`, not pixel `Width`.
   `GridLayoutManager.LoadColumnWidths` restored `FillWeight` correctly, then assigned
   `col.Width`, which makes WinForms back-compute `FillWeight` against the grid's *current*
   client width. For tabs not yet shown at startup (Characters, Categories, Styles) that width
   is wrong, corrupting the restored proportions. **Fix:** skip the pixel-`Width` assignment
   for columns whose `InheritedAutoSizeMode == Fill`; non-Fill columns still get exact widths.
2. **Styles grid was load-only.** Widths were loaded but never saved on close. **Fix:** added
   `SaveColumnWidths("Styles", …)` to `FormMain_FormClosing`.
3. **Views grid wasn't persisted at all.** **Fix:** wired both save (close) and load (open +
   database switch) for `grdViews` under the `"Views"` grid key.

No `TabControl.SelectedIndexChanged` re-apply was needed: with the Fill-aware load, restored
`FillWeight`s fully govern layout when a tab is first shown. Only the startup tab (`grdTimers`)
needs the existing post-`Shown` `None→Fill` toggle (a documented WinForms quirk for Fill set
before initial layout) plus a pixel re-apply for any non-Fill columns.

---



> **This is additive, not a destructive rewrite.** See **§10 Dual-renderer transition
> strategy**. The old `TableLayoutPanel`-based renderer (`MiniView`) is the **Classic**
> engine and stays the default until the new **Thorne** engine reaches parity. The new
> layered-window renderer lives in a brand-new `ThorneView.cs`; both implement the shared
> `IThorneMiniView` interface so `MiniViews.cs` orchestrates them identically.

The new **Thorne** renderer (`ThorneView`) uses the spike's layered-window custom paint:

1. **Layered window** (`WS_EX_LAYERED | WS_EX_TOOLWINDOW`), per-pixel ARGB bitmap pushed via
   `UpdateLayeredWindow`, honoring `BackgroundOpacity`.
2. **3-column row layout** — canonical left-to-right order is
   `TIME (fixed slot)` | `ICON slot` | `NAME`, i.e. **time on the left of the name** (matches
   the current mini view). Per-slot alignment is the key to clean stacking:
   - **TIME** slot: fixed width (sized to the widest expected value), text **right-justified**
     so all times line up on the slot's right edge even as Long/Compact widths change.
   - **ICON** slot: fixed width, reserved only when `ShowIconSlot=1` (future).
   - **NAME** slot: flexible width, text **left-justified**.
   `TimePlacement` (a **per-view** field, §3.2) swaps the time slot to the right of the name
   (`1=Right`) for users who prefer it; `0=Left` is the default and matches current behavior.
   `ShowIconSlot` reserves the icon column.
3. **Per-row invalidation** on each timer tick (redraw only changed rows) — all UI updates
   from the tick thread go through `Invoke`/`BeginInvoke` per threading standards.
4. **Right-click context menu** (`ContextMenuStrip`) for view actions.
5. **`LockPosition`** disables drag; **`ClickThrough`** adds `WS_EX_TRANSPARENT`.
6. Header bar drawn when `ShowHeader=1` (text = `HeaderText` or view Name).
7. Empty-state rendering per §3.3 (`ViewName` / `Placeholder` / `Hidden`).

### 8.1 `SetAppearance` call site (plan step 24)
`IThorneMiniView.SetAppearance(MiniViewAppearance)` (see §10.1) receives the new per-view +
resolved per-style fields as a single options object; `MiniViews.SetMiniAppearance` builds it
and pushes it through to whichever engine the view is using. The Classic `MiniView` unpacks it
into its existing private fields (no visual change); the Thorne `ThorneView` reads the new
members.

### 8.2 Layered-window paint & threading notes

The layered-window model has three sharp edges the implementer must respect; capture them here
so they don't resurface as bugs:

1. **You cannot partially update a layered surface.** `UpdateLayeredWindow` replaces the
   So "per-row
   invalidation" (§8 item 3) is a **CPU optimization, not a GPU one**: keep a single persistent
   off-screen `Bitmap`/`Graphics` owned by the `ThorneView`, repaint only the rows whose text
   changed into that bitmap, then push the whole bitmap with one `UpdateLayeredWindow` call per
   tick. Do **not** allocate a new bitmap per tick (GC churn + flicker). (This is the "per-row
   invalidation" referenced in **§8 item 3**.)
2. **The bitmap is owned by, and pushed from, the UI thread.** Timer ticks arrive on a
   background thread; marshal to the UI thread via `Invoke`/`BeginInvoke` (per the threading
   standard) before touching the bitmap or calling `UpdateLayeredWindow`. The Classic renderer
   already does this in `LoadData`; mirror that guard in `ThorneView`.
3. **DPI / multi-monitor.** `FixedWidth`, `RowSpacing`, slot widths, and font sizes are
   authored in **logical pixels** and must be scaled by the view's current DPI before painting
   (use `DeviceDpi` / `Graphics.DpiX`, not a hard-coded 96). The bitmap pushed to
   `UpdateLayeredWindow` must be sized in **physical** pixels. Re-measure and rebuild the
   bitmap on `WM_DPICHANGED` (monitor-to-monitor drag) so the overlay stays crisp. The Classic
   `TableLayoutPanel` path gets this for free from WinForms; the custom-paint path does not.

---

## 9. Open decisions (resolve before/while implementing)

1. ~~**"Long" hours padding**~~ — **RESOLVED:** Long always shows full zero-padded
   `HH:MM:SS` (e.g. `01:23:01`, `00:00:32`); true no-op rename of current output. *(see §2.1)*
2. ~~**`PlaceholderText` storage**~~ — **RESOLVED:** add a dedicated
   `PlaceholderText TEXT DEFAULT ''` column on `miniviews` (clarity over reusing the empty-text
   path); only consulted when `EmptyBehavior = "Placeholder"`. *(see §3.3)*
3. ~~**`TimePlacement` "Inline" layout**~~ — **RESOLVED:** no inline mode. Canonical layout
   is `TIME | ICON | NAME` (time left of name, `TimePlacement=0`); time slot right-justified,
   name slot left-justified; `1=Right` puts time after the name. *(see §3.1 / §8)*
4. ~~**`FontSize = 0` inheritance**~~ — **RESOLVED:** `0` means **inherit** (fall back to the
   view/global font size), not a literal 0-pt size. The resolver substitutes the global size
   before building `MiniViewAppearance`, so renderers never see `0`. *(see §3.1)*
5. ~~**Spike validation**~~ — **RESOLVED:** the layered-window approach was validated by the
   spike; its reusable Win32 plumbing is preserved in **Appendix A** and the throwaway
   `Spike\` file has been **deleted** (so no proof-of-concept code ships). `ThorneView` builds
   on Appendix A. *(see §10.0 / §A)*
6. **Default engine cutover** — confirm we keep **Classic** as the default `RenderEngine`
   until `ThorneView` reaches parity, then flip the default in a later step. *(see §10)*

---

## 10. Dual-renderer transition strategy (Classic vs. Thorne)

> Naming intentionally mirrors the **Classic / Thorne** option naming already used in the
> `C:\Thorne-UI` (`thorne_drak`) EQ UI Options, so the timer app and the UI mod stay
> consistent for the user.

### 10.0 File inventory — what stays, what's new (at a glance)

`MiniView.cs` is **kept and left functionally intact**; `ThorneView.cs` is **new and
additive**. Nothing is rewritten in place; the only change to existing files is that
`MiniView` gains an interface and `MiniViews.cs` talks to that interface instead of the
concrete type.

| File | Status | Role | Notes |
|------|--------|------|-------|
| `MiniView.cs` + `MiniView.Designer.cs` | **Keep — unchanged behavior** | The **Classic** renderer (`TableLayoutPanel`, fixed time-on-left layout). | Only edit: add `: IThorneMiniView` and adapt method signatures to match the interface. No layout/visual changes. This is the permanent fallback until/unless we delete it post-cutover. |
| `IThorneMiniView.cs` | **New** | Shared contract both renderers implement. | The single seam that lets `MiniViews.cs` stay renderer-agnostic. |
| `ThorneView.cs` + `ThorneView.Designer.cs` | **New** | The **Thorne** renderer (layered-window skin engine, §8). | Built from the validated `Spike\` code. Implements `IThorneMiniView`. All new per-view/per-style appearance features live here. |
| `MiniViewFactory.cs` *(or a factory method on `MiniViews`)* | **New** | Picks Classic vs. Thorne per view at creation time. | Reads the resolved engine (§10.2). Single place that decides which concrete type to `new`. |
| `MiniViews.cs` | **Modify (minimal)** | Orchestration: create / update / destroy / position. | `ViewEntry.Form` retyped `MiniView` → `IThorneMiniView`; `CreateMiniView` routes through the factory. Lifecycle logic otherwise unchanged. |
| `Spike\LayeredMiniViewSpike.cs` | **Deleted (harvested)** | Throwaway proof-of-concept. | Validated the layered-window approach; reusable plumbing preserved in **Appendix A**. Removed so no spike code ships in the build. |

**One-line summary:** *Keep `MiniView` as Classic, add `ThorneView` as Thorne, put both
behind `IThorneMiniView`, and let a factory + `RenderEngine` setting choose per view — with a
global override that forces Classic as the kill-switch.*

### 10.1 Decision: interface seam, not a forked pipeline

Do **not** stand up a parallel `ThorneView.cs` with its own create/update/destroy pipeline —
that duplicates orchestration and forces every new per-view feature to be wired twice
(brittle). Instead, extract a thin interface that **both** renderers implement, and keep the
single orchestration pipeline in `MiniViews.cs`.

Today the only coupling is that `MiniViews.ViewEntry.Form` is concretely typed `MiniView`,
and the lifecycle funnels through `CreateMiniView` / `UpdateMiniAppearance` /
`SetMiniAppearance` / `DestroyMiniView`. Swapping the leaf type behind an interface is the
entire change.

The signatures below are **derived directly from the current `MiniView` surface** that
`MiniViews.cs` already touches (`Text`, `Location`, `Size`, `Show/Hide/BringToFront/
SendToBack/Close/Dispose`, `SetAppearance(...)`, `LoadData(List<MiniData>)`). Extracting the
interface is therefore a *rename of the variable type*, not new behavior. Two deliberate
upgrades are folded in so the seam doesn't have to change again when `ThorneView` lands:

- `SetAppearance` takes a single **`MiniViewAppearance`** options object instead of the
  current 10 positional params. The Classic adapter just unpacks it into today's fields; the
  Thorne renderer reads the new per-view/per-style members. This avoids a 15-arg method and
  lets us add fields later without touching the interface.
- The row DTO (`MiniView.MiniData`) is reused as-is so `LoadData` is unchanged. We only
  promote it out of the `MiniView` nested scope (or alias it) so `ThorneView` can share it.

```csharp
// New options object — supersedes the 10 positional SetAppearance args and carries the
// v0.6.0 per-view + resolved per-style fields. Defaults reproduce current behavior.
public sealed class MiniViewAppearance
{
    // --- existing (already passed today) ---
    public int Opacity { get; set; } = 100;          // 0..100
    public float FontSize { get; set; } = 8f;
    public string FontFamily { get; set; } = "Arial";
    public Color WarnForeColor { get; set; }
    public Color WarnBackColor { get; set; }
    public string WarnTime { get; set; }             // "mm:ss"
    public Color ViewForeColor { get; set; }
    public Color ViewBackColor { get; set; }
    public string EmptyText { get; set; }
    public bool IsCharacterView { get; set; }
    public bool ShowWarning { get; set; }

    // --- v0.6.0 resolved per-style (from StyleData) ---
    public TimeFormat TimeFormat { get; set; } = TimeFormat.Classic;
    public bool FontBold { get; set; } = true;
    public bool FontItalic { get; set; }
    public bool ShowIconSlot { get; set; }

    // --- v0.6.0 per-view (from ViewData) ---
    public TimePlacement TimePlacement { get; set; } = TimePlacement.Left;
    public int BackgroundOpacity { get; set; } = 100;
    public bool ShowHeader { get; set; }
    public string HeaderText { get; set; }
    public int RowSpacing { get; set; }
    public int FixedWidth { get; set; }              // 0 = auto-size
    public bool LockPosition { get; set; }
    public bool ClickThrough { get; set; }
    public EmptyBehavior EmptyBehavior { get; set; } = EmptyBehavior.ViewName;
}

public interface IThorneMiniView : IDisposable
{
    // window state the orchestrator already manipulates today
    Point Location { get; set; }
    Size Size { get; }
    string Text { get; set; }

    void Show();
    void Hide();
    void BringToFront();
    void SendToBack();
    void Close();

    // appearance + data — both engines honor identical calls
    void SetAppearance(MiniViewAppearance appearance);
    void LoadData(List<MiniView.MiniData> rows);   // reuses the existing row DTO
}
```

> **Mapping to today's code:** `SetMiniAppearance(...)` builds a `MiniViewAppearance` and
> calls `view.SetAppearance(appearance)`; `UpdateMiniTimers(...)` keeps calling
> `view.LoadData(rows)` unchanged. The Classic `MiniView.SetAppearance(MiniViewAppearance)`
> simply assigns the existing private fields from the object (1:1 with the current 10 args),
> so Classic output is byte-for-byte identical.

- **`MiniView`** (existing `TableLayoutPanel`) implements `IThorneMiniView` with ~zero
  behavior change → this is the **Classic** engine.
- **`ThorneView`** (new layered-window skin engine, §8) implements the same contract → the
  **Thorne** engine.
- `ViewEntry.Form` becomes `IThorneMiniView`; `CreateMiniView` becomes a **factory** that
  picks the concrete type.

### 10.2 Two toggles (they compose)

1. **Per-view** — add a `RenderEngine` column to `miniviews` (`0 = Classic`, `1 = Thorne`),
   default `0`. The factory reads `viewData.RenderEngine`, so views can migrate **one at a
   time** and be compared live side-by-side.
2. **Global override / kill switch** — a setting (Settings tab, or `settings.RenderEngine` /
   INI flag) that can force **Classic** for all views regardless of per-view value. This is
   the escape hatch if the layered window misbehaves on a given machine — revert without a
   rebuild.

Resolution rule: `effectiveEngine = (globalForceClassic) ? Classic : view.RenderEngine`.

### 10.2.1 Runtime selection & fallback (how the two coexist at run time)

This is the concrete answer to "how do we keep `MiniView` working while building
`ThorneView`, and how does the fallback kick in":

- **At view creation** (`CreateMiniViews` → factory), for each active view:
  1. Resolve the engine: `globalForceClassic ? Classic : view.RenderEngine`.
  2. Factory returns `new MiniView(...)` (Classic) or `new ThorneView(...)` (Thorne) **as an
     `IThorneMiniView`**. `MiniViews.cs` never sees the concrete type after this point.
- **During the session**, the per-tick update / appearance push call only interface members,
  so both engines are driven by the **same** code path. No `if (engine == …)` branching leaks
  into the orchestration or `FormMain`.
- **When the engine changes** (user flips a view's `RenderEngine`, or toggles the global
  kill-switch): treat it like the existing activate/deactivate flow — **tear down and
  recreate** via `RefreshMiniViews` (`SaveViewPositions` → `DestroyMiniViews` →
  `CreateMiniViews`). The new engine's window is constructed fresh at the saved position.
  (We do **not** hot-swap a live window's renderer.)
- **Kill-switch behavior:** setting the global override to "force Classic" and refreshing
  makes **every** view fall back to the proven `MiniView`, regardless of per-view
  `RenderEngine` — no rebuild, no data loss (per-view values are retained and honored again
  when the override is cleared).
- **Default safety:** `RenderEngine` defaults to `0 = Classic`, so an upgraded `.tdb` runs
  **100% on the existing renderer** until the user explicitly opts a view into Thorne. While
  `ThorneView` is still being built, the app behaves exactly as it does today.

### 10.3 Transition sequence (no broken in-between states)

1. Extract `IThorneMiniView`; make `MiniView` implement it (no behavior change). Build + verify.
2. Change `ViewEntry.Form` to the interface; route `CreateMiniView` through a factory.
   Build + verify — still 100% **Classic**.
3. Build `ThorneView` against the interface (the whole §8 work), Classic untouched & default.
4. Add the per-view `RenderEngine` column + global override so individual views can opt in.
5. Once `ThorneView` reaches parity and is dogfooded, flip the default; later remove `MiniView`.

### 10.4 Schema impact

- `miniviews`: add `RenderEngine INTEGER DEFAULT 0` (idempotent, one-shot — see §4).
- `settings`: optional `RenderEngine` global override (or reuse an existing settings flag).

---

## 11. Thorne-UI (`C:\Thorne-UI`) reusable assets

The companion EQ UI mod workspace (`thorne_drak`, VS Code workspace at `C:\Thorne-UI`)
contains assets worth reusing so the timer overlay visually matches the in-game UI.

### 11.1 Canonical semantic color palette

`C:\Thorne-UI\.docs\STANDARDS.md` → **"Color Palette & Text Styling"** defines named colors
with RGB + hex (and EQType bindings). These are the in-game UI's canonical colors; reusing
them keeps Thorne Timer styles consistent with the user's EQ UI. Highlights:

| Semantic            | Color Name     | RGB             | Hex       |
|---------------------|----------------|-----------------|-----------|
| Default text        | White          | 255, 255, 255   | `#FFFFFF` |
| Attributes          | Sky Blue       | 70, 180, 255    | `#46B4FF` |
| HP value            | Heated Blush   | 255, 100, 100   | `#FF6464` |
| Mana value          | Crystal Blue   | 100, 150, 255   | `#6496FF` |
| AC / ATK            | Amber          | 255, 185, 30    | `#FFB91E` |
| FIRE resist         | Fire Red       | 255, 113, 46    | `#FF712E` |
| COLD resist         | Frost Blue     | 15, 182, 240    | `#0FB6F0` |
| MAGIC resist        | Arcane Violet  | 255, 113, 255   | `#FF71FF` |
| DISEASE resist      | Plague Yellow  | 230, 230, 0     | `#E6E600` |
| POISON resist       | Venom Green    | 0, 220, 0       | `#00DC00` |
| Positive / XP       | Verdant        | 0, 205, 0       | `#00CD00` |

Gauge fill colors (HP `#FF0000`, Mana `#1E1EFF`, Pet `#C850C8`, XP `#DC9600`, casting
`#F000F0`, spell recast `#C800C8`, etc.) are also tabulated there with EQType IDs — useful
defaults when seeding/refreshing the **Styles** color set so a "Buff"/"Pet"/"Ping" style
matches the EQ overlay it's tracking.

> **Action:** when finalizing default style colors (`StylesRepository.SeedDefaultStyles`),
> cross-check against this palette so the timer overlay and the EQ UI agree. (Seeding stays
> one-shot per §4 — only affects new `.tdb` files.)

### 11.2 Other reference material in `C:\Thorne-UI`

- `.docs\technical\EQTYPES.md` — EQType numeric bindings (Gauge/Label/InvSlot data sources);
  context for which game values map to which colors.
- `.docs\technical\ZEAL-FEATURES.md` — Zeal-client features (relevant for spell/recast data
  awareness).
- `.docs\STANDARDS.md` — SIDL XML conventions, fonts (`Font 3` for titles), TGA texture
  rules, transparency behavior (parent `Style_Transparent` dims children — informs how we
  reason about layered-window alpha).
- `thorne_drak\*.tga` — gauge/spell/class icon textures (e.g. `spell_icons_thorne0*.tga`,
  class `*01.tga`) that could feed the §3.1 `ShowIconSlot` icon column later.
- SIDL files use **integer ARGB-style RGB triples**; Thorne Timer already stores colors as
  ARGB `int` (`Color.ToArgb()`), so palette values port directly.

> These are **reference**, not a build dependency. Nothing in `C:\Thorne-UI` is compiled into
> Thorne Timer; we copy values/conventions, not files (except possibly icon TGAs if/when the
> icon slot ships).

---

## 12. Acceptance / build verification (plan step 26)

- `msbuild Thorne-Timer.sln /p:Configuration=Release` succeeds; `get_errors` clean.
- Existing `.tdb` upgrades in place (no lost styles/views; user edits & deletions preserved).
- All four time formats render per the §2.1 table for long/mid/sub-minute spans.
- Styles tab "Time Format" column + sample preview re-render against the `1h 23m 45s` sample.
- Mini views honor opacity, header, row spacing, fixed width, lock, and click-through.
- **Classic** renderer remains default and unchanged; views can opt into **Thorne** per-view
  via `RenderEngine`, with a global override forcing Classic. (§10)
- Default style colors cross-checked against the `C:\Thorne-UI` palette. (§11)
- No throwaway/spike code ships in the build (the `Spike\` proof-of-concept has been
  harvested into Appendix A and removed; see §10.0). (§A)

---

## Appendix A — Harvested layered-window plumbing (from the deleted spike)

> The `Spike\LayeredMiniViewSpike.cs` proof-of-concept **validated** the layered-window +
> custom-paint approach (per-pixel alpha, drag via `WM_NCLBUTTONDOWN`, right-click menu,
> per-row repaint) and has been **deleted** so no throwaway code ships. The reusable Win32
> plumbing it proved out is preserved verbatim here so `ThorneView` (§8) can adopt it directly.

**Layered-window `CreateParams` + the ARGB-bitmap push.** The non-obvious detail is
`bmp.GetHbitmap(Color.FromArgb(0))` (premultiplied alpha) paired with `AC_SRC_ALPHA`; getting
either wrong yields a black box or no transparency.

```csharp
// Window style: layered + tool-window (hidden from Alt-Tab). Add WS_EX_TRANSPARENT
// only when the view's ClickThrough is set.
protected override CreateParams CreateParams
{
    get
    {
        const int WS_EX_LAYERED = 0x00080000;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        CreateParams cp = base.CreateParams;
        cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW;
        return cp;
    }
}

// Push a 32-bpp premultiplied-ARGB bitmap as the whole window surface.
// Per §8.2 there is no partial update — this replaces the entire window bitmap.
private void PushBitmap(Bitmap bmp)
{
    const int ULW_ALPHA = 0x00000002;
    const byte AC_SRC_OVER = 0x00;
    const byte AC_SRC_ALPHA = 0x01;

    IntPtr screenDc = GetDC(IntPtr.Zero);
    IntPtr memDc = CreateCompatibleDC(screenDc);
    IntPtr hBitmap = IntPtr.Zero, oldBitmap = IntPtr.Zero;
    try
    {
        hBitmap = bmp.GetHbitmap(Color.FromArgb(0)); // premultiplied alpha
        oldBitmap = SelectObject(memDc, hBitmap);

        Size size = new Size(bmp.Width, bmp.Height);
        Point src = new Point(0, 0);
        Point top = new Point(Left, Top);
        BLENDFUNCTION blend = new BLENDFUNCTION
        {
            BlendOp = AC_SRC_OVER,
            BlendFlags = 0,
            SourceConstantAlpha = 255,   // panel/row alpha comes from the bitmap itself
            AlphaFormat = AC_SRC_ALPHA
        };
        UpdateLayeredWindow(Handle, screenDc, ref top, ref size, memDc,
            ref src, 0, ref blend, ULW_ALPHA);
    }
    finally
    {
        SelectObject(memDc, oldBitmap);
        if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
        DeleteDC(memDc);
        ReleaseDC(IntPtr.Zero, screenDc);
    }
}
```

Required P/Invoke + struct (gdi32/user32):

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
private struct BLENDFUNCTION
{ public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }

[DllImport("user32.dll", SetLastError = true)]
private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
    ref Point pptDst, ref Size psize, IntPtr hdcSrc, ref Point pprSrc,
    int crKey, ref BLENDFUNCTION pblend, int dwFlags);
[DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
[DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
[DllImport("gdi32.dll")]  private static extern IntPtr CreateCompatibleDC(IntPtr hDC);
[DllImport("gdi32.dll")]  private static extern bool DeleteDC(IntPtr hdc);
[DllImport("gdi32.dll")]  private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
[DllImport("gdi32.dll")]  private static extern bool DeleteObject(IntPtr hObject);
```

**Drag-to-move** (matches existing `MiniView`): on left `MouseDown`, `ReleaseCapture()` then
`SendMessage(Handle, 0xA1 /*WM_NCLBUTTONDOWN*/, 0x2 /*HT_CAPTION*/, 0)`. Gate it on
`!LockPosition`.

**Row painting recipe the spike proved** (feeds §8 layout): build one full-window
`Format32bppArgb` bitmap per repaint with `SmoothingMode.AntiAlias` +
`TextRenderingHint.ClearTypeGridFit` + `CompositingMode.SourceOver`; fill the panel
background with the per-view background alpha, then per row draw the row-tint rectangle and
the `TIME` (right-justified `StringAlignment.Far`) / icon-slot / `NAME` (left-justified, with
`StringTrimming.EllipsisCharacter`) — i.e. the `TIME | ICON | NAME` layout, time right-aligned
in its fixed slot so stacked rows line up. Per §8.2, prefer a single persistent backing bitmap
in production rather than allocating one per tick.
```