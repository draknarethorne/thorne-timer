# Timer Output Authoring & Grid Simplification - Design

> **Status:** 📐 Design / Spec - pre-implementation
> **Version:** v0.7.0 ("Smarter Timer Authoring")
> **Branch:** `v0.7.0-dev`
> **Date:** 2026-06-25
> **Author:** Draknaré Thorne / GitHub Copilot
>
> This document is the **durable specification** for how a timer's *output* (speech,
> sound, display label, and the new capture templates) is authored, and how we shrink
> the already-wide timers grid. It is the companion to
> [`keyword-power-features.md`](keyword-power-features.md): that doc owns *matching +
> capture*; this doc owns *where the user sets up what happens on a match* and the
> column-maintenance story. If chat context is lost, this is the source of truth for
> the agreed UI/column model. Adjust **before** implementation and update as phases land.

---

## 1. Problem statement

The timers grid is already large - ~18 visible columns:

`Active / Name / Category / Start Keyword / End Keyword / Sound / ... / Speech / Duration / Remaining / Case / Loop / Style / Class / Scope / Depends On / Depends Delay / Start/Stop / Count`

Adding raw `SpeechTemplate` and `DisplayNameTemplate` columns (as the first draft of
`keyword-power-features.md` Section 5 proposed) would push it to ~20 and dump more free-text
into a grid that is already hard to scan. The user's goals:

1. **Reduce duplicate / raw-text columns** in the grid.
2. **Reuse the existing `Speech` column** to hold either a literal *or* a template.
3. Explore a **Templates tab** of reusable, named presets a timer can *pick*.
4. Be able to set **both** a speech template and a display template on one timer.
5. Possibly repurpose the existing **`...` button** from "pick a sound" into an
   **Advanced output** dialog - the first step toward moving speech/sound/output
   fields off the grid entirely.

All five are reasonable. This doc evaluates them against the real schema/UI and
recommends a phased path.

---

## 2. Ground truth (what exists today)

From `Database.cs` and `FormMain.SetupTimerGrid()`:

| Grid column (`Name`) | Header | DB column | Type today | Notes |
|---|---|---|---|---|
| `WAVFile` | Sound | `timers.WAVFile` | TEXT (relative path) | Free-text path |
| `WAV` | Play / `...` | - (button) | `DataGridViewButtonColumn` | Opens `FindWAVFile()` sound picker |
| `Speech` | Speech | `timers.Speech` | TEXT | Literal spoken phrase |
| `StartKeyword` / `EndKeyword` | Start/End Keyword | `timers.StartKeyword` / `EndKeyword` | TEXT | Match triggers |

Key existing facts the design leans on:

- The DB column is **`Speech`** (not `SpeechText`) and **`WAVFile`**. Naming in
  `keyword-power-features.md` must be corrected to match.
- The **`...` button is its own column (`WAV`)**; `grdTimers_CellClick` routes a
  click on it to `FindWAVFile(rowIndex)`. Repurposing/extending it is low-risk and
  localized.
- A **Compact view already exists** that hides exactly the "advanced" columns
  (`StartKeyword, EndKeyword, Speech, WAVFile, WAV, CaseYn, EndlessYn,
  DependsOnTimer, DependsOnDelay`). This is strong precedent: the app already
  treats those as secondary, collapsible fields.

---

## 3. The reuse question: one `Speech` column, literal *or* template

**Recommendation: yes - reuse `Speech`; do not add a `SpeechTemplate` column.**

The renderer already decides what string to speak. Whether `Speech` holds a literal
or a template is a *rendering-time* distinction, not a *storage* distinction:

- If the matched keyword produced **captures** and `Speech` contains placeholders
  (`{1}`, `{item}`), resolve the template against the captures.
- Otherwise speak `Speech` verbatim (today's behavior).

So `Speech` becomes "what to say (literal, or a template if it contains
placeholders)". No second column, no migration for speech. The same logic applies to
the **display label**: rather than a new `DisplayNameTemplate` column, the display
template is an *advanced* field (see Section 5) that defaults to the timer `Name`.

**Why not auto-detect on every column?** Detection (does this string contain
placeholders?) must be a **load/edit-time classification**, never per-match - exactly
the rule already locked in `keyword-power-features.md` Section 3/Section 4. A template flag is
computed once when the timer loads or is edited.

---

## 4. The Templates-tab question: reusable named presets

**Recommendation: defer the full preset library; design the columns so it can be
added later without a breaking change.**

A "Templates" tab (define `Vendor Sale -> "{item} - {1}p {2}g"` once, then pick it on
many timers) is genuinely useful and fits the app's existing tab+controller+repository
pattern (`StylesController`/`StylesRepository`, etc.). But it adds real scope:

- A new `templates` table (`ID`, `Name` UNIQUE, `Kind` = Speech|Display|Feed,
  `Body`, `SortOrder`).
- A controller/repository + grid (Add/Delete/Rename/edit body), mirroring
  `CategoriesController`.
- A **picker** on each timer (combo of template names) *plus* the ability to still
  type an inline one-off - i.e. "Custom..." vs. a named preset.

That is a feature in its own right. The pragmatic path:

- **Phase 1 (this release):** inline templates only, authored in the **Advanced
  output dialog** (Section 5). The `Speech`/display fields accept literal-or-template text
  directly. No `templates` table yet.
- **Phase 2 (later):** introduce the `templates` table + tab, and add a "Use
  preset" picker in the Advanced dialog. Inline text becomes the "Custom..." option.
  Because templates are just strings resolved at render time, a preset is simply a
  *named source* for that string - **no change to the matching/resolution engine**.

This keeps the first release shippable while leaving a clean seam. The same preset
mechanism could later serve sounds, keyword snippets, etc. - but we should prove it
on output templates first rather than build a generic system up front (**avoid
speculative generality**).

---

## 5. The `...` button -> Advanced Output dialog

**Recommendation: yes. This is the highest-value move and it directly shrinks the grid.**

Today the `...` (`WAV`) button only picks a sound. Repurpose it into an **Advanced
Output** dialog that owns every "what happens when this timer fires" field. The grid
then shows a *summary*, not the raw text.

### 5.1 What moves into the dialog

| Field | Source | In dialog as |
|---|---|---|
| Sound file | `timers.WAVFile` | File picker + Test (>) button |
| Speech (literal/template) | `timers.Speech` | Multiline text + "insert capture" helper + Test |
| Display label template | *advanced* (default = `Name`) | Multiline text + live preview |
| (later) preset pickers | `templates` table | Combos with "Custom..." |

`StartKeyword` / `EndKeyword` stay on the grid (they are *matching*, not *output*, and
authors scan them constantly). `Case`/`Loop`/`Depends*` are out of scope for this
dialog - they are timer *behavior*, and already collapsible via Compact view.

### 5.2 Grid after the change

Remove `Sound` (`WAVFile`) and `Speech` free-text columns from the grid; keep the
button column but relabel it and route it to the dialog:

- `WAV` button column -> rename to **`Output`** (header `Output`, text `...`), route
  `grdTimers_CellClick` -> `OpenAdvancedOutputDialog(rowIndex)` instead of
  `FindWAVFile`.
- Optional read-only **summary** column (e.g. `[sound] > "say {item}..."`) so the grid still
  communicates at a glance without being editable free-text.

Net column change: **-2 free-text columns, +0/+1 read-only summary**, and a button
that now does much more. This is a concrete reduction, not just a reshuffle.

### 5.3 Why a dialog (vs. more columns)

- The grid stops being the editor for long free-text -> easier to scan.
- Output fields gain room for **multiline text, inline Test buttons, capture-insert
  helpers, and live preview** that a grid cell cannot offer.
- It is the **first slice of a future full "Timer Setup" dialog** - once the pattern
  exists, `Case`/`Loop`/`Scope`/`Depends*` can migrate off the grid too, behind the
  same dialog, shrinking the grid further over time.

### 5.4 Discoverability

Repurposing `...` is good, but the icon currently reads as "pick a file". Mitigate:

- Relabel the column header to **`Output`** and keep the `...` glyph (or use a small
  gear/cog icon).
- Tooltip: "Advanced output: sound, speech, and display".
- Double-clicking the (now-removed) Speech area or a context-menu item
  **"Advanced output..."** opens the same dialog, so it is reachable more than one way.

---

## 6. Recommended end state

```
Timers grid (leaner):
  Active / Name / Category / Start Keyword / End Keyword /
  [Output ...] / (opt) Output summary / Duration / Remaining /
  Case / Loop / Style / Class / Scope / Depends On / Depends Delay /
  Start/Stop / Count

Advanced Output dialog (the "..." button):
  +-- Output for "<timer name>" ------------------------------+
  | Sound:   [ Sounds\spawn.wav        ] [Browse] [> Test]    |
  | Speech:  [ {item} for sale: {1}p {2}g            ] [> ]    |
  |          (literal, or a template using {1}/{item})        |
  | Display: [ {item} - {1}p {2}g                    ]        |
  |          Preview: Bronze Dagger - 1p 2g                   |
  | (Phase 2) Preset: [ Vendor Sale v ]  [Custom...]          |
  |                                   [ OK ]  [ Cancel ]      |
  +-----------------------------------------------------------+
```

---

## 7. Schema impact

- **`timers.Speech`** - unchanged column; semantics widen to "literal or template".
  No migration.
- **`timers.WAVFile`** - unchanged; just edited via dialog instead of inline.
- **Display label template** - needs storage. Two options:
  - (a) reuse a single new nullable `timers.DisplayName` TEXT (NULL = use `Name`), or
  - (b) fold into a small JSON `timers.OutputConfig` if more output fields are
    coming. **Recommendation: (a)** one nullable `DisplayName` column now; revisit a
    consolidated blob only if the dialog grows many more fields (**YAGNI** until then).
- **(Phase 2) `templates`** - new table; additive, idempotent migration.

All migrations follow the existing **idempotent, one-shot** rule (`isFieldExist`),
never re-seeding.

---

## 8. Corrections this forces in `keyword-power-features.md`

- Section 5 currently names new columns `timers.SpeechTemplate` and
  `timers.DisplayNameTemplate`. Replace with: **reuse `timers.Speech`** for speech
  (literal-or-template) and **one nullable `timers.DisplayName`** for the label.
- Section 5.4's table should reference `Speech` / `DisplayName`, not the `*Template`
  columns.
- The capture-resolution mechanics in Section 5.2/Section 5.5 are unchanged - only the *storage*
  and *authoring surface* change.

---

## 9. Phasing

| Phase | Scope | Ships |
|---|---|---|
| **1a** | Advanced Output dialog: Sound + Speech (literal/template) + Display, inline text, Test/preview; repurpose `...` button; remove `Sound`/`Speech` grid columns; add `DisplayName` column to DB | This release |
| **1b** | Optional read-only Output summary column in grid | This release / fast-follow |
| **2** | `templates` table + Templates tab + preset pickers ("Custom..." vs named) | Later |
| **3** | Extend the dialog into a full "Timer Setup" dialog; migrate `Case`/`Loop`/`Scope`/`Depends*` off the grid | Later |

Each phase is independently shippable and reversible (the dialog is additive; the
grid columns can be restored by config if needed).

---

## 10. Decisions & open questions

**Recommended (for confirmation):**

- ✅ **Reuse `Speech`** for literal-or-template; no `SpeechTemplate` column.
- ✅ **Repurpose the `...` button** into an Advanced Output dialog (highest-value,
  directly shrinks the grid).
- ✅ **One nullable `DisplayName`** column for the label template; default NULL = `Name`.
- ✅ **Defer the Templates tab** to Phase 2 behind a clean seam (inline templates first).

**Open:**

- Keep a **read-only Output summary** column, or remove output from the grid entirely?
- Should the dialog also expose **`Case`** (case-sensitive) since it pairs with
  matching, or leave it on the grid?
- Phase 2 preset **scope**: per-tome only, or shareable/exportable preset packs?
