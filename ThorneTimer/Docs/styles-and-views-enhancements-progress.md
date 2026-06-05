# Styles & Views Enhancements — Implementation Progress

> **Status: 🔄 IN PROGRESS — foundation slice complete, skin engine not yet started.**
> The **Time Formats** slice (plan §2) has fully landed and is in testing. An unplanned
> **interim correctness-hardening** pass (plan §13.7) plus a **`FormMain` → `TimersController`**
> extraction are currently **in flight (uncommitted)**. The marquee work — per-view appearance
> fields (§3.2), editor dialogs + live preview (§7.1–7.4), and the entire **Thorne** layered-window
> skin engine (§8) with the dual-renderer seam (§10) — has **not been started**.

**Version:** v0.6.0
**Plan document:** [styles-and-views-enhancements.md](styles-and-views-enhancements.md)
**Branch:** `v0.6.0-gui-enhancements`
**Last assessed:** 2026-06-04 (against commit `407134c` + uncommitted working tree)

---

## 1. TL;DR — How far are we?

**Roughly 25–30% complete by effort.** The foundational, low-risk slice is done; the
high-effort, high-visibility pieces are still entirely ahead.

| | |
|---|---|
| ✅ **Done & committed** | Time Formats (§2): `TimerTimeFormatter` + `TimeFormat` enum, `styles.TimeFormat` column + migration, Styles-tab dropdown with live `Example` preview, format-independent warning detection, grid Remaining column honoring per-style format. Spike deleted (harvested into Appendix A). Grid column-layout persistence pre-work (§7.5) shipped in beta2. |
| 🔄 **In flight (uncommitted)** | Interim time-parse hardening (§13.7): `TimerPlus.TryParseRemaining`, `NormalizeRemainingForStorage`, dependency-delay fix. Stale-layout guard for newly-added grid columns. Opportunistic `FormMain` → `TimersController` extraction (new file). |
| ❌ **Not started** | All per-view `miniviews` fields (§3.2). `ViewsRepository` field extensions (§5). `StylePreviewPanel` + `StyleEditorDialog` + `ViewEditorDialog` + grid rework (§7.1–7.3). Settings Font Family picker (§3.4 / §7.4). `IThorneMiniView` seam + `MiniViewFactory` (§10). The **entire `ThorneView` skin engine** (§8). |
| ⏸️ **Deferred (by design)** | Full numeric time-model refactor (§13) — explicitly out of scope for this feature branch. |

---

## 2. Completion snapshot by plan section

| Plan § | Area | Status | Notes |
|--------|------|--------|-------|
| §2 | Time Formats + `TimerTimeFormatter` | ✅ **DONE** | All four formats implemented; Classic is byte-for-byte identical to legacy output. |
| §3.1 | `StyleData` new fields | 🟡 **PARTIAL** | `TimeFormat` ✅ landed. `FontSize`, `FontBold`, `FontItalic`, `ShowIconSlot` ❌ not added. |
| §3.2 | `ViewData` new fields | ❌ **NOT STARTED** | None of `RenderEngine`, `BackgroundOpacity`, `TimePlacement`, `ShowHeader`, `HeaderText`, `RowSpacing`, `FixedWidth`, `LockPosition`, `ClickThrough` exist yet. `ViewData` unchanged. |
| §3.3 | `EmptyBehavior` / `PlaceholderText` unify | ❌ **NOT STARTED** | `EmptyBehavior` column exists (prior work); no `PlaceholderText`, no value unification. |
| §3.4 | `settings.FontFamily` | ❌ **NOT STARTED** | Column not added. |
| §4 | Schema migrations | 🟡 **PARTIAL** | Only `styles.TimeFormat` migration shipped (idempotent `ALTER` + fresh `CREATE`). `miniviews` / `settings` migrations not written. |
| §5 | Repository changes | 🟡 **PARTIAL** | `StylesRepository` extended for `TimeFormat` ✅. `ViewsRepository` field extensions ❌. |
| §6 | Rendering integration | ✅ **DONE** (for current scope) | `TimerPlus.GetTimeRemaining()` delegates to formatter; `TimerRuntime` resolves style→format; grid Remaining column honors it. |
| §7.1 | `StylePreviewPanel` | ❌ **NOT STARTED** | No user control created. |
| §7.2 | `StyleEditorDialog` / `ViewEditorDialog` | ❌ **NOT STARTED** | Neither dialog exists. |
| §7.3 | Grid rework (summary + Edit…) | ❌ **NOT STARTED** | Styles/Views grids still inline-edit. Styles tab does have the new inline Time Format column. |
| §7.4 | Settings tab Font Family picker | ❌ **NOT STARTED** | — |
| §7.5 | Grid column-layout persistence pre-work | ✅ **DONE** | Shipped in beta2 (`2255694`). Stale-layout-for-new-column follow-on is the in-flight `GridLayoutManager` change. |
| §8 | `ThorneView` skin engine | ❌ **NOT STARTED** | No `ThorneView.cs`. Layered-window plumbing exists only as Appendix A reference. |
| §10 | Dual-renderer seam (`IThorneMiniView` + factory) | ❌ **NOT STARTED** | No interface, no factory; `MiniViews.cs` still concretely typed to `MiniView`. |
| §11 | Color palette cross-check | 🟡 **MOSTLY DONE** | Default style colors aligned during the earlier per-view-colors work; final cross-check pending. |
| §12 | Acceptance / build verification | 🟡 **PARTIAL** | Time-format acceptance criteria pass; renderer/opacity/header criteria N/A until §8 lands. |
| §13 | Numeric time-model refactor | ⏸️ **DEFERRED** | §13.7 interim hardening in flight (see below); full refactor intentionally out of scope. |

Legend: ✅ done · 🟡 partial · 🔄 in flight (uncommitted) · ❌ not started · ⏸️ deferred

---

## 3. What landed (committed — `407134c`)

Commit: `feat(styles): per-style time formats with format-independent warnings`

- [x] **`TimerTimeFormatter.cs`** — new static formatter, single source of truth, with the
      `TimeFormat` enum (`Classic=0`, `Long=1`, `AdaptiveCompact=2`, `FullCompact=3`). Negative
      spans clamp to zero. `Classic` reproduces the legacy `GetTimeRemaining()` output exactly.
- [x] **`TimerPlus.GetTimeRemaining()`** delegates to the formatter; added a
      `GetTimeRemaining(TimeFormat)` overload for style-aware callers.
- [x] **`styles.TimeFormat` column** — idempotent `ALTER TABLE … ADD COLUMN … DEFAULT 0`,
      fresh-DB `CREATE` includes it, plus hydration + persistence in `StylesRepository`
      (parameterized `INSERT`/`UPDATE`/`SELECT`).
- [x] **`StyleData.TimeFormat`** property, defaults to `TimeFormat.Classic`.
- [x] **`TimerRuntime`** resolves each timer's style → `TimeFormat` (via a
      `StyleTimeFormatResolver` wired from `FormMain` → `StylesRepository`), so the **main grid
      Remaining column** matches the mini view and the Styles-tab preview.
- [x] **Styles tab "Time Format" dropdown column** + live read-only **`Example`** column that
      re-renders the `1h 23m 45s` sample on change (`StylesController`).
- [x] **Format-independent warning detection** — warning color is driven by raw remaining ms
      (`MiniTimerData.RemainingMs` → `MiniData.RemainingMs`), not by parsing the display string;
      fixes false warnings under lossy formats. String parse remains only as a fallback.
- [x] **Spike removed** — `Spike\LayeredMiniViewSpike.cs` deleted; reusable Win32 plumbing
      preserved verbatim in the plan's **Appendix A** so `ThorneView` can adopt it directly.

Earlier on the same branch (beta2, `2255694`):

- [x] **§7.5 grid column-layout persistence** — Fill-aware load (skip pixel `Width` for Fill
      columns), Styles grid widths now saved on close, Views grid save+load wired.

---

## 4. What is in flight (uncommitted working tree)

These changes are **not yet committed**. They represent the §13.7 interim hardening plus an
opportunistic refactor. Net: ~637 insertions / 225 deletions across 11 files + 1 new file.

- [x] **`TimerPlus.cs`** — `TryParseRemaining` tolerant parser covering **every**
      `TimerTimeFormatter` output (colon forms *and* unit-suffixed `d/h/m/s` forms);
      `GetMilliseconds` delegates to it; failures are now explicit (no silent `0`).
- [x] **`TimerStateRepository.cs`** — `NormalizeRemainingForStorage` normalizes the persisted
      `Remaining` to canonical **Classic** at the write boundary (AdaptiveCompact is lossy);
      live grid/mini view still render the per-style format.
- [x] **`TimerRuntime.cs`** — dependency check (`CheckDependentTimer`) now reads the live
      authoritative `TimerPlus.ElapsedTime` instead of re-parsing the display string (fixes the
      `DependsOnDelay`-ignored-for-non-Classic-styles chain bug).
- [x] **`GridLayoutManager.cs`** — stale-saved-layout guard: when a new column (e.g. Time
      Format) appears, fit it into the user's existing footprint by proportionally shrinking
      saved columns to their `MinimumWidth` floors instead of overflowing the grid
      (`ApplyWidthsWithFitForNewColumns`).
- [x] **`TimersController.cs` (NEW, untracked)** — extracts timer-maintenance domain logic out
      of `FormMain` (Add/Duplicate/Chain, Roman-numeral chain naming, duration validation +
      shorthand auto-format, grid CRUD on `RowValidating`) following the established
      Controller pattern. Wired into `FormMain` via delegate hooks; added to `.csproj`.
- [x] **`FormMain.cs` / `FormMain.Designer.cs`** — wires/owns `TimersController` (net code
      reduction in `FormMain`).
- [x] **`TimersRepository.cs`** — minor supporting changes.
- [x] **Doc** — added §13.7 "Interim hardening already landed" to the plan.

> ⚠️ **Action needed:** this is a meaningful, coherent chunk of work sitting uncommitted.
> Recommend committing it (e.g. `fix(timers): tolerant time parsing + dependency-delay fix; refactor(timers): extract TimersController`) before starting the next slice, so the working tree is clean for the §3.2/§8 work.

---

## 5. What has NOT been started

The bulk of the feature — everything that makes the "skin engine" visible to users:

### Data / schema (§3.2, §3.4, §4, §5)
- [ ] `miniviews` columns: `RenderEngine`, `BackgroundOpacity`, `TimePlacement`, `ShowHeader`,
      `HeaderText`, `RowSpacing`, `FixedWidth`, `LockPosition`, `ClickThrough` (+ `PlaceholderText`).
- [ ] `styles` columns: `FontSize`, `FontBold`, `FontItalic`, `ShowIconSlot`.
- [ ] `settings.FontFamily`.
- [ ] `ViewData` model + `ViewsRepository` `SELECT`/`INSERT`/`UPDATE` extensions.

### UI rework (§7.1–7.4)
- [ ] `StylePreviewPanel` reusable live-preview user control.
- [ ] `StyleEditorDialog` (colors, time format, font size/bold/italic, icon slot).
- [ ] `ViewEditorDialog` (engine, time placement, opacity, header, spacing, width, lock,
      click-through, empty behavior).
- [ ] Styles/Views grids → summary grid + "Edit…" button.
- [ ] Settings tab Font Family picker.

### Renderer + seam (§8, §10) — the largest remaining piece
- [ ] `IThorneMiniView` interface; make `MiniView` implement it (no behavior change).
- [ ] Retype `MiniViews.ViewEntry.Form` to the interface; route creation through a factory.
- [ ] `MiniViewFactory` + global force-Classic kill switch.
- [ ] `ThorneView.cs` layered-window skin engine: per-pixel ARGB push, `TIME | ICON | NAME`
      row layout, per-row invalidation, context menu, lock/click-through, header, empty-state.
- [ ] `SetAppearance(MiniViewAppearance)` on both renderers (§8.1).

### Deferred (§13) — out of scope for this branch
- [ ] Full numeric time-model refactor (`RemainingMs`/`TimeSpan` as source of truth, numeric
      persistence, retire runtime string parsing). The §13.7 interim fix removes the
      *correctness* risk now; the cleanup stays deferred to its own branch.

---

## 6. File inventory vs. plan

| File | Plan role | State |
|------|-----------|-------|
| `TimerTimeFormatter.cs` | §2 formatter | ✅ exists (committed) |
| `Styles.cs` (`StyleData`) | §3.1 | 🟡 `TimeFormat` only |
| `StylesRepository.cs` | §5 | 🟡 `TimeFormat` only |
| `StylesController.cs` | §7.3 | 🟡 inline Time Format column + Example preview |
| `TimerPlus.cs` | §6 / §13.7 | 🔄 formatter delegation committed; `TryParseRemaining` uncommitted |
| `TimerRuntime.cs` | §6 / §13.7 | 🔄 resolver committed; dependency fix uncommitted |
| `TimerStateRepository.cs` | §13.7 | 🔄 normalize-on-store uncommitted |
| `GridLayoutManager.cs` | §7.5 | 🔄 stale-layout guard uncommitted |
| `TimersController.cs` | (opportunistic) | 🔄 new, untracked |
| `Views.cs` (`ViewData`) | §3.2 | ❌ unchanged |
| `ViewsRepository.cs` | §5 | ❌ no new fields |
| `IThorneMiniView.cs` | §10 | ❌ does not exist |
| `MiniViewFactory.cs` | §10 | ❌ does not exist |
| `ThorneView.cs` / `.Designer.cs` | §8 | ❌ do not exist |
| `StylePreviewPanel.cs` | §7.1 | ❌ does not exist |
| `StyleEditorDialog.cs` | §7.2 | ❌ does not exist |
| `ViewEditorDialog.cs` | §7.2 | ❌ does not exist |
| `Spike\LayeredMiniViewSpike.cs` | harvested | ✅ deleted (as planned) |

---

## 7. Recommended resumption order

1. **Commit the in-flight work** (§13.7 hardening + `GridLayoutManager` guard + `TimersController`
   extraction) so the tree is clean. Build + `get_errors` clean first.
2. **Schema + repos for views (§3.2, §4, §5)** — add the `miniviews` columns (idempotent,
   one-shot), extend `ViewData` + `ViewsRepository`. Pure data layer, low risk, unblocks UI.
   Add the four `styles` font columns + `settings.FontFamily` in the same pass.
3. **Dual-renderer seam (§10.3 steps 1–2)** — extract `IThorneMiniView`, make `MiniView`
   implement it, retype `MiniViews`, add the factory. Verify still 100% Classic. **This is the
   safest order: the seam lands with zero behavior change before any new renderer exists.**
4. **`ThorneView` skin engine (§8)** — build against the interface using Appendix A plumbing,
   Classic untouched and default.
5. **UI rework (§7.1–7.4)** — `StylePreviewPanel`, then the two editor dialogs, then grid
   rework, then the Settings Font Family picker.
6. **Cutover (§10.3 step 5)** — only after dogfooding parity, flip the default; later remove
   `MiniView`.

---

## 8. Open decisions still outstanding

From plan §9 (most are resolved; these remain):

- **§9.6 — Default engine cutover.** Confirmed approach: keep **Classic** as the default
  `RenderEngine` until `ThorneView` reaches parity, then flip in a later step. No code impact
  yet (column doesn't exist). Revisit at resumption step 6.
- **Color palette final cross-check (§11).** Defaults were aligned in the per-view-colors work;
  a final pass against the `C:\Thorne-UI` semantic palette is still pending before release.

---

## 9. Impact analysis & testing notes

**Affected components (current + in-flight):** `TimerTimeFormatter`, `TimerPlus`, `TimerRuntime`,
`TimerStateRepository`, `StylesRepository`/`StylesController`, `GridLayoutManager`, `FormMain`,
`MiniView`/`MiniViews` (warning path), and the new `TimersController`.

**Testing guidance for what has landed / is in flight:**
- All four `TimeFormat`s render per the §2.1 table for sub-minute / multi-hour / multi-day spans.
- Warning threshold fires correctly under **every** format (the §2.5 regression) — verify
  `AdaptiveCompact` (`"1d 4h"`) and `FullCompact` (`"45s"`) no longer false-trigger.
- **Dependency chains** with a non-Classic style (e.g. Spawn = `FullCompact`) honor
  `DependsOnDelay` (the §13.7 bug — chain must stagger, not fire all at once).
- Existing `.tdb` **upgrades in place**: `styles.TimeFormat` back-fills to Classic; persisted
  `timer_runtime_state.Remaining` round-trips losslessly through `NormalizeRemainingForStorage`.
- Styles grid with the new **Time Format** column restores saved widths without overflowing
  (the `GridLayoutManager` stale-layout guard).
- `msbuild Thorne-Timer.sln /p:Configuration=Release` succeeds; `get_errors` clean.

**Docs to update going forward:** keep this file in lock-step with each slice; move it (and the
plan) to `Docs/archive/` once v0.6.0 ships, mirroring the `mini-view-per-view-colors-*` docs.
