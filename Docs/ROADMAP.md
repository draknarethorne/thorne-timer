# Thorne Timer Roadmap

**Repository:** [draknarethorne/thorne-timer](https://github.com/draknarethorne/thorne-timer)
**Maintainer:** Draknaré Thorne

---

## Current Status

**Latest Release:** v0.6.0 (shipped)
**Active Branch:** `v0.7.0-dev`
**v0.7.0 Focus:** Smarter Timer Authoring — wildcards, keyword test button, capture-group templates — plus small reliability/data-safety fixes. The Timer Maintenance Dialog is **deferred to post-1.0** (promote on adoption).

---

## ✅ Shipped

### Core Timer Engine — v0.1.0 – v0.4.0

The foundation: a working timer application with log parsing and overlay windows.

- Core timer engine with start/end keyword matching and countdown
- Real-time EQ log file parsing via `LogMonitor`
- Text-to-speech and WAV audio alerts with per-timer configuration
- SQLite database (tome `.tdb`) for timer and settings persistence
- Always-on-top overlay windows for timer display
- CI/CD pipeline — GitHub Actions build and release workflows
- Code signing support in release workflow
- Auto-version injection from git tags into `AssemblyInfo.cs`

### Per-Character State & Timer Styles — v0.5.0

Multi-character support and the four-style mini view system.

- **Timer Styles** — Normal, Buff, Pet, Ping with style-driven routing to mini views
- **Mini View Overlays** — four always-on-top compact overlay windows with real-time countdown
- **Per-Character Timer State** — save/restore active timers across character switches
- **Auto Character Switching** — detect character changes via log file monitoring
- **Class System** — 16 EQ class seed data, ClassID on timers/characters, class filtering
- **Scope System** — World vs Character scope replacing legacy categories
- **DependsOn Chaining** — DependsOnTimer + DependsOnDelay for timer sequences
- **Compact View** — streamlined main grid mode with reduced columns
- **Row Painting** — style-driven colors across grid and mini views
- **Column Persistence** — grid column widths saved per view mode
- **Parameterized SQL** — all database operations use parameterized queries
- **Window Management** — size/position persistence, screen bounds safety, min-size enforcement

### GUI Enhancements & Performance — v0.6.0 ✅ **SHIPPED**

Quality of life improvements, critical bug fixes, per-view color configuration, and a hybrid Designer + Controller + Repository pattern for the configuration tabs.

**Shipped in v0.6.0 (released from `main`):**
- **Styles tab** — first-class entity with Add/Delete/Rename, color picker, drives both grid row tint and mini-view appearance
- **New default styles** — Pet (lavender), Spawn (cyan), Lockout (DodgerBlue), Character (white) joining Normal/Buff/Ping
- **Per-View Color Configuration** — every view has its own `ForeColor`, `BackColor`, `ShowWarning`, and `EmptyBehavior` columns in the `miniviews` table
- **Views tab CRUD** — Add/Delete views, Style filter dropdown is now dynamic (reflects current styles), color cells with `ColorDialog` picker, Example preview column
- **Categories CRUD** — Add/Delete categories with the same hybrid pattern
- **Hybrid Designer + Controller + Repository pattern** — `StylesController`, `ViewsController`, `CategoriesController` configure designer-backed grids; `StylesRepository`, `ViewsRepository`, `CategoriesRepository` own SQLite CRUD
- **One-shot startup migration** — `StylesRepository.EnsureSchema` creates the table, seeds defaults, and migrates legacy `MiniViewNormFore/BuffFore/PingFore` from `settings` once; deletions and edits are sticky thereafter
- **`(None)` character** — manual pause without camping; main form, log monitor, and timer runtime all handle the no-active-character state cleanly
- **Camp-out Auto-Pause** — detects `/camp` with 10-second inactivity threshold, switches active character to `(None)`
- **Auto-Switch Bug Fixes** — suppress the OLD character (not NEW) on manual switch, proper re-enable logic
- **Voice System** — all English voice support (en-GB, en-AU, en-CA, etc.), comprehensive logging
- **Mini View Refinements** — hidden from Alt-Tab task switcher via `WS_EX_TOOLWINDOW`
- **Grid Performance** — O(n²) → O(n) dictionary optimization in `SyncRuntimeToGrid` (~98% faster with 130+ timers)

**Also delivered in the v0.6.0 final polish:**
- Per-style time formats (Classic / Long / Adaptive Compact / Full Compact)
- Pipe-separated multi-keyword (OR) matching for timer and category keywords
- **Controller/repository extraction** — `TimersController` + `TimersRepository` and `CharactersController` + `CharactersRepository` extracted from `FormMain`, joining the Styles/Views/Categories pattern. This was the stated maintenance-dialog prerequisite — it is now done.
- **Timer Duplicate + dependent-chain authoring** — one-click clone and Roman-numeral chain extension live in `TimersController`
- **`db_meta` version-stamp table** — records `CreatedByVersion`, `LastWrittenByVersion`, `SchemaVersion`, `LastWrittenAtUtc`; stamped on every `Database.Open()`
- **Tome Information dialog** (`FormTomeInfo` + `TomeStatisticsRepository`) — read-only summary of the active tome (catalog counts, version provenance) cross-cutting the `timers`, `characters`, `categories`, `styles`, `miniviews`, `classes`, `timer_runtime_state`, and `db_meta` tables
- **Tiered tome backup** (`Database.BackupDatabase`) reusing the `ThorneArchive` retention policy
- Per-cell tooltips across every configuration grid
- Newcomer-focused README (recipes, FAQ, SmartScreen guidance, screenshots, Mermaid flow)

---

## 🎯 Audience & Guiding Principles

Thorne Timer is built primarily for **solo and small-group players** (not raid-coordination tools). Optimization priorities derive from that audience:

keywords and durations. Several releases below attack this wound from different angles.
2. **Configuration data lives in the right place** — per-character / per-timer / per-style data lives in the `.tdb` tome; preferences, hotkeys, recent files, transient runtime state can live in INI.
3. **Overlays should feel native to the EQ UI** — borders, padding, fonts, and color should be skinnable to match the player's chosen EQ UI (Vert, Drakah's, Project Quarm defaults, etc.) rather than forcing a single Thorne Timer look.
4. **The core feature is timers — feeds and synthesis are additive output modes, not a chat-client substitute.** Captured events should be *transformed* (extract item + price), not echoed verbatim. If the user wants raw log, Notepad++ tail mode already exists.
5. **Statistics earn their place by riding on existing infrastructure.** Personal play-improvement metrics (survivability, fizzle rate, deaths, XP/hr) are in scope; raid DPS dashboards, combat replay, and cross-player analytics are not — GamParse and friends already do that well.

---

## 🔄 Next

### Smarter Timer Authoring — v0.7.0 🎯 **PRIORITY**

The single biggest player pain: **adding and fine-tuning timers takes too long.** This is the
highest-value gameplay work — it makes every timer you'll ever create faster and more accurate.
This release makes authoring feel fast and forgiving.

> **Already shipped (foundation laid in v0.6.0):** pipe-delimited multi-keyword (OR) matching and the one-click **Duplicate** button both landed early in `TimersController`. The remaining authoring work builds on top of them.

**Keyword power features (the core fine-tuning):**
- **Wildcards in keywords** — `*` glob support (compiled to `Regex` and cached per timer); `^` / `$` as an opt-in full-regex escape hatch for power users. This is the headline ask: stop fighting exact-match keywords for spells/messages that have small variations.
- **Capture groups → speech / display templates** — new `SpeechTemplate` and `DisplayNameTemplate` columns. With keyword `"* tells you, '*'"` and template `"{0} says {1}"`, pings can actually speak meaningful content instead of generic alerts.
- **Cooldown / throttling per timer** — new `MinTriggerIntervalSeconds` column to suppress ping spam in noisy zones (busy auction channel, etc.).
- **Keyword conflict detection** — warn when two timers would match the same line, so a new timer doesn't silently shadow an existing one (issue #33).

**Authoring UX (close the feedback loop):**
- **Test / preview keyword button** — small dialog in the timer editor where you paste a log line (or pick from the active log file's tail) and see ✅ Match / ❌ No match plus capture group preview. Eliminates the "alt-tab into the game to trigger it" loop.
- **DependsOn picker** — replace the free-text column with a `DataGridViewComboBoxColumn` bound to the in-memory timer list, sorted by Name, refreshed when the collection changes (same pattern as v0.6.0's dynamic Style dropdown).
- **Right-click context menu on the Timers grid** — Start / Stop / Reset / Duplicate / Test / Toggle Active / Jump to last trigger.
- **Search / filter box above the Timers grid** — type-to-filter by Name / Category / Style. Essential at 130+ timers.
- **Visual "just fired" indicator** — brief row-background flash when a timer starts or expires, so users can correlate sound alerts with which timer caused them.

**Diagnostics:**
- **Per-timer trigger history (ring buffer)** — in-memory last-N triggers per timer with timestamp and matched line, viewable via right-click → "Trigger history". Makes "did my timer actually fire?" answerable without re-reading the log.
- **Per-timer fire-rate stats** — triggers/hour, last-fired timestamp, and average interval between fires, surfaced alongside the trigger history. Reveals "this timer never actually completes its cooldown" or "this buff drops 4s early consistently" without any new parsing — it's just aggregation on data you already have. Foundation for the Personal Play Statistics release.
- **Capture-group `aggregate` modifier** — when a capture group is marked `aggregate=true` (e.g. `{damage:aggregate}`), the runtime rolls min / max / avg / count into per-timer stats. No new parsing code; statistics fall out of writing timers. This is the architectural unlock for the Personal Play Statistics release.

**Reliability & data-safety riding along (the 1.0 quality bar):**
Small, high-trust fixes shipped opportunistically alongside the authoring work — none require the deferred maintenance dialog:
- **Auto-switch pause respects "peek" mode** — paused auto-switch no longer snaps back when the still-logged-in character's log grows (issue #6).
- **`(auto)` status indicator is always accurate** — re-enabling auto-switch reliably restores the indicator (issue #7).
- **Compact/full toggle preserves window position** — toggling view modes no longer throws the window off-screen (issue #26).
- **Periodic auto-save of timer state** — crash / power-loss protection by flushing runtime state on a timer, building on the existing `TimerStateRepository` (issue #23).
- **Auto-archive `.tdb` on detected upgrade** — finish the data-safety add-on (relocated from the deferred maintenance-dialog work): snapshot the tome before migrations when the stamped `LastWrittenByVersion` is older than the running build. The `db_meta` stamp and `BackupDatabase` plumbing already shipped in v0.6.0; only the upgrade-detection trigger remains.

### Spell Library & Templates — v0.8.0 📚 **NEW**

Direct attack on the "new spell → new timer" pain. Eliminate the most repetitive part of authoring entirely.

> **Scope:** this is an *accelerator*, not a 1.0 requirement — the authoring release already makes hand-authoring fast, and Thorne Timer stays fully usable without any bundled spell data. A PDQ/Project Quarm `.sql` dump is already on hand as a seed source, so the work is mostly *import + UI*. Ship the smallest useful slice first (a searchable "Add timer from spell" dialog — issue #27) and grow packs from there.

**Spell library:**
- **Bundled EQ spell database** — JSON / SQLite snapshot of spell data (name, duration, target type, recast, level by class). Shipped with the app; refreshable. **Candidate seed source already in hand:** a PDQ/Project Quarm `.sql` dump (see issue #27 — "seed spells table from .sql dump"). Other sources (Lucy, Allakhazam exports) where licensing permits, or community-contributed JSON.
- **"Add timer from spell" dialog** — pick spell from a searchable, class-filtered list → keywords, duration, recast, suggested style auto-populated. The whole timer is one click + a few tweaks instead of starting from a blank row. **This is the high-value core of the phase.**
- **Spell-cast auto-detection (optional)** — when a `You begin casting <Spell>` or `<You|Someone> feel(s) the <Buff>` line is seen for a spell with no timer yet, surface a non-intrusive prompt or notification: "Add timer for <Spell>?" Click yes → pre-filled dialog. Click no → suppressed per-character.

**Templates:**
- **Timer template / pack export & import** — share timer sets as portable JSON (or `.ttpack` files). Critical for community sharing: "Druid Vert's leveling pack", "Necro charm/snare combo", "Velious overland spawn timers".
- **Class starter packs** — ship curated packs per EQ class (utility spells, common buffs, pet management). New player imports their class pack and has a sensible default setup in seconds.
- **Zone spawn packs** — pre-built spawn timer sets for popular hunting zones, leveraging DependsOn chains. Solves the "new zone setup" pain.

### Mini View Skinning & Theming — v0.9.0 🎨 **NEW**

Make overlays feel like part of the EQ UI rather than alien windows on top of it.

**Per-style typography (already partly scoped):**
- Add `FontFamily`, `FontSize`, `Bold`, `Italic` columns to the `styles` table
- Settings to choose between bundled fonts (a curated set that match common EQ skins) or any installed system font

**Theme / skin system:**
- **Skin definition file format** (likely JSON, stored under `Skins/` next to `Sounds/`) — declares border style, border color, padding, corner radius, background pattern/image, title bar style, header font
- **Bundled skins** matching popular EQ UI mods (Vert, Drakah's, Project Quarm default, Velious-era classic, Luclin-era) plus a "Thorne Timer" original
- **Per-view skin selection** — each view in the Views tab picks its skin independently; great for multi-boxers running different UIs per character
- **Skin editor / preview** — small dialog to preview a skin against sample timers before applying

**Layout polish:**
- **Per-view padding / spacing** — currently fixed; let dense raid users go tight, casual users go loose
- **Per-view background opacity** — already partial; expose to UI
- **Optional title bar / drag handle** styling that matches the chosen skin

### Feed Views & Log Synthesis — v0.10.0 📜 **NEW**

Introduces a second view type alongside countdown timers: a **scrolling, transformed event feed**. The killer example: vendor sale prices appear as `Rusty Short Sword — 1p 2g` next to the merchant window, optionally spoken as "one platinum two gold".

**Why it lands here:** it builds directly on the authoring release's capture groups (parsing) and the theming release's readable styling, and it reinforces the "timers are core, feeds are additive" boundary.

**New view type:**
- **`ViewType` column on `miniviews`** — `Timers` (current behavior) or `Feed` (new). Existing views default to `Timers`; backwards compatible.
- **Feed renderer** — scrolling, timestamped lines; newest-top or newest-bottom (per view); configurable max line count; pause-on-hover; click-to-copy.
- **Per-line color** inherits from the originating timer's style — the v0.6.0 style system carries straight through.
- **Auto-fade / shrink on idle** — feed compresses when no new activity, expands when busy. Optional.
- **Skin support** — feeds use the same skin system from the theming release; bundled skins ship a "feed" variant tuned for readability over transparency.

**Log synthesis pipeline (generalizes the authoring release):**
- **Multiple output targets per timer** — a single keyword match can trigger any combination of: countdown timer, feed line, speech, sound. Each target has its own template.
- **Templated output with capture groups** — `FeedTemplate`, `SpeechTemplate`, `DisplayNameTemplate` columns. Named groups (`{item}`, `{price}`, `{npc}`) preferred over positional (`{0}`, `{1}`) for clarity.
- **Speech sanitizer** — user-configurable substitutions (INI-stored) so `1p 2g 3s 4c` becomes "one platinum two gold three silver four copper" instead of "one P two G three S four C". Ships with a default EQ-aware substitution set.
- **Per-target throttling** — `MinFeedIntervalSeconds`, `MinSpeechIntervalSeconds`, separate from the authoring release's per-timer throttle, so a busy timer can still flash visually but only speak the first match per window.
- **"Speak only if feed was idle"** option — in noisy zones, the visual feed shows everything but speech stays quiet until there's a gap.

**Persistence model:**
- Feed contents are **runtime state only by default** — not saved to `.tdb`. Behaves like a chat window.
- Optional "save last N lines on close" toggle (per-view); if enabled, written to `Logs/feeds/{viewname}.log`, not the tome.

**Hard scope boundaries (so this doesn't become a chat client):**
- ❌ No full raw-log mirroring — every feed line must come from an explicit timer / keyword match
- ❌ No combat log roll-ups, DPS parsing, or fight summaries
- ❌ No two-way chat / sending text to the game
- ✅ Yes: curated, transformed streams (vendor prices, faction hits, named-mob sightings, group invites, tells)

### Directional Speech & Ping Refactor — v0.11.0

Centralize the Ping timer execution model and eliminate hardcoded branch points. **Pushed later** because it's internal cleanup with no user-visible benefit — the authoring, spell-library, and theming releases deliver more user value first.

- Refactor `StartTimer` / `StopTimer` / `ResetTimer` to handle Ping via the directional speech pattern
- Eliminate `|| PingTimer()` escape hatches throughout the codebase
- Centralize Ping lifecycle management
- See [TD-011](../ThorneTimer/Docs/active-views/technical-debt.md) for the full 19-step plan

---

## 🧭 Post-1.0 / Promote-on-Adoption

Larger structural features that are **deliberately deferred until after 1.0** and only promoted
into a numbered release if user adoption grows enough to justify the effort and the disruption.
The current app is finely tuned for a solo/small-group player; these change *how the app is
shaped*, so they wait until there's a base of users asking for them.

### Timer Maintenance Dialog (formerly Phase C) ⏸️ **DEFERRED**

Separate the gameplay view from timer maintenance by splitting the single grid into a read-only
runtime dashboard plus a dedicated editing dialog.

> **Why deferred:** the existing single-grid + auto-switch workflow already works well and is
> finely tuned in daily play. The dual-grid split is a *UX refinement for power users with many
> timers/characters*, not a correctness, stability, or data-safety gap — so it does not gate 1.0.
> The prerequisite refactor is already done (see below), which keeps the door open to pick this up
> cheaply whenever adoption warrants it.

**Already in place (shipped in v0.6.0):**
- ✅ `TimersController` + `TimersRepository` and `CharactersController` + `CharactersRepository` extracted from `FormMain`
- ✅ `LogMonitor` exposes `isActive` + `GetActiveCharacterID()` so the main form can lock to the actively logging character without snapshot/restore gymnastics

**Remaining work when promoted (issues #16–#21):**
- **Read-only timer view** — main form grid becomes non-editable, always shows the active character
- **Timer maintenance dialog** (`Edit > Timers...`) — host the existing `TimersController` against a separate grid for add/edit/delete across all characters (#16)
- **Dual-grid architecture** — main form grid (active gameplay) + dialog grid (frozen maintenance)
- **Always-show-active mode** — main form automatically follows the actively logging character (#21)
- **Frozen timer display in dialog** — maintenance grid loads timers with `isActive=false` (no countdown)
- **Background preservation** — active character's timers keep running while editing others in the dialog
- **Companion management dialogs** — Characters (#17), Categories (#18), Classes (#19), Views (#20) follow the same dialog pattern once the Timers dialog proves it out

**Promotion trigger:** revisit when there's meaningful external adoption (multiple active users
managing large timer sets) or when a concrete user request makes the single-grid workflow a pain
point rather than a preference.

---

## 📋 Planned

### Zones, Groups & Conditions — v0.12.0

Context-aware timer activation — useful for solo/small-group exploration play.

- **Zone-aware timers** — parse `You have entered <Zone>` from log; `Zone` column on timers (nullable, single or pipe-delimited); auto-activate / deactivate as the player zones
- **Timer groups** — `GroupName` column for bulk-enable / disable / edit; can be combined with zones (e.g. "Solusek's Eye spawns" group auto-activates in that zone)
- **Conditional / chained timers beyond DependsOn**:
  - "Start timer Y only if timer X is currently running"
  - "Reset all timers in group Z when keyword fires" (great for wipes: `You have been slain` resets combat group)
- **Per-timer warning threshold override** — global warning is the default; per-timer override for "warn at last 30s" or "warn at last 10%"
- **Class profiles (optional)** — class-specific timer profiles per EQ class, building on the spell library

### Power-User & Quality-of-Life — v1.0.0

The "everything we deferred because it was a nice-to-have" release that pushes us to 1.0.

- **Global hotkeys** (low-level keyboard hook) — toggle mini-views, pause/resume all, force-switch character, "mute all sounds for N seconds" panic button
- **Quick mute button** in toolbar — doorbell / phone / boss-walked-in scenarios
- **Character online/offline time tracking** — adjust Character+ scope timers for time elapsed while logged out (server cooldowns that progress offline)
- **Better online state detection** — distinguish character inactive (manual switch) vs. logged out (camp/disconnect)
- **Timer database backup / restore tooling** — built-in `.tdb` snapshot and restore, with auto-snapshots before destructive ops. *(Partially shipped: `Database.BackupDatabase` with tiered pruning and the `db_meta` version stamps landed in v0.6.0; a `Restore` UI and the pre-destructive auto-snapshot trigger remain.)*
- **Performance profiling diagnostics** — built-in panel showing log-poll latency, grid sync time, mini-view paint time
- **Accessibility** — high contrast mode, screen-reader labels, larger text mode (overlaps with the theming release)
- **User documentation / help system** — in-app help dialog or `Help → Topics` menu pointing at bundled markdown / online guide

### Personal Play Statistics — v1.1.0 📊 **NEW**

The **first post-1.0 release.** Statistics earn their place only because the foundation was built opportunistically across the authoring, spell-library, theming, feed, and zones work. This release is the dedicated focus that ties them together into a coherent self-improvement tool.

**Why this comes after 1.0:** Thorne Timer is a timer app first. Stats arrive only after the core authoring + feed + theming + zones story is mature. The architecture unlock (capture groups + the `aggregate` modifier from the authoring release) means most of the parsing is already free by the time we get here — this release is mostly *presentation* of data already being collected.

**Personal play metrics (in scope — "am I playing well?"):**
- **Combat survival** — hits taken, hits/min, max single hit, heals received, net survivability
- **Casting health** — fizzle rate per spell, interrupt rate, cast count, pet death count
- **Progression** — XP messages per hour (proxy for XP/hr), loot drops by item, currency earned per session
- **Session summary** — time logged in per character, time per zone, camp count, character switches
- **Death recap** — last N log lines before death with damage source highlighted; written to `Logs/deaths/` for post-mortem review

**New view type:**
- **`ViewType=Stats` (third view type alongside `Timers` and `Feed`)** — dashboard renderer with cards, simple bar / line charts, top-N tables. Same skin system as the other view types.
- **Optional graph view** — basic charts over a session window; not real-time tick-by-tick (that's a perf trap), but periodic refresh (every few seconds)
- **CSV export** — raw session data for users who want their own analysis

**Architecture (avoids parsing slowdown):**
- Stats accumulate in **in-memory dictionaries** keyed by timer ID / spell name / zone, not SQLite-per-match
- **Periodic flush to SQLite** — every 60s and on session end / character switch / camp
- **Lazy computation** — derived metrics (kills/hour, fizzle %) computed only when the stats view is open
- **UI refresh on a timer (250–500 ms)**, never on every match event
- **Optional "performance mode" toggle** — disables stats collection entirely for users who want minimum overhead
- **Bounded history** — ring buffers and aggregation windows; never unbounded event logs

**Hard scope boundaries (so this doesn't become GamParse):**
- ❌ No raid DPS parsing or fight summaries
- ❌ No combat log replay / timeline UI
- ❌ No cross-player aggregation / network sync
- ❌ No upload to external raid-logging services
- ❌ No real-time per-tick damage graphs during fights
- ✅ Yes to personal play-improvement metrics that fall out of data Thorne Timer is already capturing

---

## 💡 Backlog / Captured Ideas

Items captured from design discussion but not yet slotted to a version. Kept here so they don't get lost.

**Storage architecture:**
- **Move transient / preference state from `.tdb` to INI** — recent tomes, window position, mute-until timestamp, hotkey bindings, "last selected character". Per-character / per-timer / per-style data stays in the tome.

**Performance & code health:**
- **Virtual-mode `DataGridView`** for 200+ timer datasets (authoring-phase enhancement)
- **Incremental grid updates** — only refresh changed rows, not the whole grid
- **Async log polling with `Task` + `CancellationToken`** + adaptive backoff (10 ms active, 250 ms idle)
- **Move startup work off the UI thread** — show form immediately with a loading overlay; bind grid after `Task.Run` load

**Tabs / configuration UX:**
- **`SettingsController` + repository pair** to finish the controller/repository extraction across all tabs

**Sharing & community:**
- **Timer pack registry / index** (much later) — if community packs take off, a curated `https://thorne-timer.dev/packs` index browsable from inside the app

**Things deliberately deferred / declined:**
- ❌ ML / AI-driven timer detection — EQ log lines are deterministic; regex is the right tool
- ❌ Migration to .NET 8 / EF Core in the near term — WinForms designer story on modern .NET is still rough; current Designer + Controller + Repository pattern is working
- ❌ Plugin system — speculative, big effort, defer until 100+ users ask for it
- ❌ Normalizing `miniviews` color columns into a separate table — schema is small; join would be pure overhead
- ❌ Raid-mode coordination features (shared raid timers, pull timers, etc.) — audience is solo / small-group; revisit only if user base shifts

---

## Quality & Polish (Any Version)

Ongoing improvements that can ship with any release:

### ✅ Completed in v0.6.0
- [x] **Voice system improvements** — All English voices, Alt-Tab hiding for mini views, comprehensive logging
- [x] **Auto-pause logic fixes** — Camp-out detection, manual pause via "(None)" character, proper character state management
- [x] **Auto-switch suppression bug fixes** — Suppress correct (OLD) character, proper re-enable logic on NEW character activity
- [x] **Grid performance optimizations** — O(n²) → O(n) dictionary lookups in `SyncRuntimeToGrid` (~98% faster with 130+ timers)
- [x] **Character-scope timer pausing** — Character-scope timers now properly pause when viewing inactive characters (only run when actively logging)

### 📷 Documentation & Onboarding
- [ ] Screenshots for README.md (main grid, mini views, configuration)
- [ ] In-app help / documentation system
- [ ] Class starter pack walkthrough / first-run experience

---

## Timeline

| Release | Version | Highlights | Status |
|---------|---------|------------|--------|
| Core Engine | v0.1.0 – v0.4.0 | Foundation | ✅ Shipped |
| Per-Character & Styles | v0.5.0 | Multi-character | ✅ Shipped |
| GUI & Performance | v0.6.0 | Styles / Views CRUD, perf | ✅ Shipped |
| **Smarter Authoring** | **v0.7.0** | **Wildcards, capture groups, keyword test button, conflict detection (+ reliability fixes)** | **🎯 Next** |
| **Spell Library & Templates** | **v0.8.0** | **Seed `spells` from PDQ `.sql`, "add from spell", import/export packs** | **🎯 Planned** |
| **Skinning & Theming** | **v0.9.0** | **EQ-UI-matching overlays, per-style fonts** | **🎯 Planned** |
| **Feed Views & Log Synthesis** | **v0.10.0** | **Scrolling feed view type, transformed event streams, speech sanitizer** | **📜 Planned** |
| Ping Refactor | v0.11.0 | Internal cleanup | 📋 Planned |
| Zones, Groups, Conditions | v0.12.0 | Context-aware activation | 📋 Planned |
| Power-User & QoL | v1.0.0 | Hotkeys, mute, backups, a11y, in-app help | 📋 Future |
| **Personal Play Statistics** | **v1.1.0** | **Survival, fizzle rate, deaths, XP/hr; stats view type; death recap** | **📊 Post-1.0** |
| Timer Maintenance Dialog | Post-1.0 | Read-only grid + `Edit > Timers...` dialog | ⏸️ Deferred (promote on adoption) |

> Version numbers are targets and may shift as development progresses.
> **Theme grouping:** v0.7.0–v0.9.0 form a coherent "fast, forgiving authoring" arc — make timer authoring fast (wildcards, test button), then optionally seed it from spell data, then make the overlays look native. **v0.10.0** introduces the second display modality (scrolling feeds) and the log-synthesis pipeline; v0.11.0–v1.0.0 shift to internal cleanup, context awareness, and power-user polish that caps the **1.0** release. **v1.1.0** is the first post-1.0 release, adding personal-play statistics on top of the capture-group infrastructure built in the authoring release. The **Timer Maintenance Dialog** is intentionally **off the numbered track** — it is promoted only if adoption justifies the dual-grid restructure.

---

**Last Updated:** v0.7.0 planning pass (`v0.7.0-dev` branch) — re-sequenced for player value; maintenance dialog deferred post-1.0
**Maintained By:** Draknaré Thorne
