# Thorne Timer Roadmap

**Repository:** [draknarethorne/thorne-timer](https://github.com/draknarethorne/thorne-timer)
**Maintainer:** Draknaré Thorne

---

## Current Status

**Latest Release:** v0.6.0 (shipped)
**Active Branch:** `v0.7.0-dev`
**Road to 1.0:** two releases away. **v0.7.0** finishes timer-authoring fine-tuning plus reliability/data-safety fixes; **v1.0.0** adds final polish (mute, hotkeys, restore UI, help) and ships. Everything else — spell library, theming, feeds, zones, stats — is **post-1.0, built as desired or as users ask**.

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

Thorne Timer is built primarily for **solo and small-group players** (not raid-coordination tools). Priorities derive from that audience:

1. **Timer authoring pain is the #1 user pain** — adding timers when leveling new spells, setting up spawn timers in a new zone, figuring out keywords and durations.
2. **Configuration data lives in the right place** — per-character / per-timer / per-style data lives in the `.tdb` tome; preferences, hotkeys, recent files, transient runtime state can live in INI.
3. **Overlays should feel native to the EQ UI** — borders, padding, fonts, and color should be skinnable to match the player's chosen EQ UI (Vert, Drakah's, Project Quarm defaults, etc.) rather than forcing a single Thorne Timer look.
4. **The core feature is timers — feeds and synthesis are additive output modes, not a chat-client substitute.** Captured events should be *transformed* (extract item + price), not echoed verbatim. If the user wants raw log, Notepad++ tail mode already exists.
5. **Statistics earn their place by riding on existing infrastructure.** Personal play-improvement metrics (survivability, fizzle rate, deaths, XP/hr) are in scope; raid DPS dashboards, combat replay, and cross-player analytics are not — GamParse and friends already do that well.

---

## 🔄 Road to 1.0

### Smarter Timer Authoring — v0.7.0 🎯 **PRIORITY**

Adding and fine-tuning timers is the single biggest player pain. This release makes authoring fast and forgiving — every timer becomes quicker to create and more accurate to match.

**Keyword power features:**
- **Wildcards in keywords** — `*` glob support (compiled to `Regex` and cached per timer); `^` / `$` as an opt-in full-regex escape hatch. Stops the fight with exact-match keywords for spells/messages that vary slightly.
- **Capture groups → speech / display templates** — new `SpeechTemplate` and `DisplayNameTemplate` columns. Keyword `"* tells you, '*'"` with template `"{0} says {1}"` lets pings speak meaningful content instead of a generic alert.
- **Cooldown / throttling per timer** — `MinTriggerIntervalSeconds` column to suppress ping spam in noisy zones.
- **Keyword conflict detection** — warn when two timers would match the same line, so a new timer doesn't silently shadow an existing one (issue #33).

**Authoring UX (close the feedback loop):**
- **Test / preview keyword button** — small dialog in the timer editor where you paste a log line (or pick from the active log file's tail) and see ✅ Match / ❌ No match plus capture group preview. Eliminates the "alt-tab into the game to trigger it" loop.
- **DependsOn picker** — replace the free-text column with a `DataGridViewComboBoxColumn` bound to the in-memory timer list, sorted by Name, refreshed when the collection changes (same pattern as v0.6.0's dynamic Style dropdown).
- **Right-click context menu on the Timers grid** — Start / Stop / Reset / Duplicate / Test / Toggle Active / Jump to last trigger.
- **Search / filter box above the Timers grid** — type-to-filter by Name / Category / Style. Essential at 130+ timers.
- **Visual "just fired" indicator** — brief row-background flash when a timer starts or expires, so users can correlate sound alerts with which timer caused them.

**Diagnostics:**
- **Per-timer trigger history (ring buffer)** — in-memory last-N triggers per timer with timestamp and matched line, viewable via right-click → "Trigger history". Answers "did my timer actually fire?" without re-reading the log.

**Reliability & data-safety (the 1.0 quality bar):**
- **Auto-switch pause respects "peek" mode** — paused auto-switch no longer snaps back when the still-logged-in character's log grows (issue #6).
- **`(auto)` status indicator is always accurate** — re-enabling auto-switch reliably restores the indicator (issue #7).
- **Compact/full toggle preserves window position** — toggling view modes no longer throws the window off-screen (issue #26).
- **Periodic auto-save of timer state** — crash / power-loss protection by flushing runtime state on a timer (issue #23).
- **Auto-archive `.tdb` on detected upgrade** — snapshot the tome before migrations when the stamped `LastWrittenByVersion` is older than the running build. The `db_meta` stamp and `BackupDatabase` plumbing already shipped in v0.6.0; only the upgrade-detection trigger remains.

### Final Polish & 1.0 — v1.0.0 🚀 **SHIP IT**

The small, high-trust quality-of-life items that make Thorne Timer feel finished, then tag 1.0.

- **Quick mute button** in toolbar — doorbell / phone / boss-walked-in scenarios
- **Global hotkeys** (low-level keyboard hook) — toggle mini-views, pause/resume all, force-switch character, "mute all for N seconds" panic button
- **Tome restore UI** — `Restore` dialog and a pre-destructive auto-snapshot trigger on top of the existing `Database.BackupDatabase` (backup + `db_meta` stamps already shipped in v0.6.0)
- **In-app help / onboarding** — `Help → Topics` pointing at bundled markdown / online guide; first-run experience
- **Accessibility basics** — high-contrast option, larger-text mode, screen-reader labels
- **README screenshots** — main grid, mini views, configuration

> Stretch items if they're quick: better online/offline state detection, and adjusting Character-scope timers for time elapsed while logged out. Neither gates the tag.

---

## 🧭 Post-1.0 — Build As Desired

Everything below is **past the 1.0 line**. None of it gates the 1.0 tag. These are unordered — each
ships as a minor release (1.1, 1.2, …) if and when it's fun to build or a user asks for it. They're
grouped by theme, not sequenced; pick whatever brings the most value at the time.

### Spell Library & Templates 📚

Direct attack on the "new spell → new timer" pain. Bundled spell data is **optional** — Thorne Timer stays fully usable without it. A PDQ/Project Quarm `.sql` dump is on hand as a seed source, so the work is mostly import + UI (issue #27).

- **Bundled EQ spell database** — JSON / SQLite snapshot of spell data (name, duration, target type, recast, level by class), shipped and refreshable. Seed source: a PDQ/Project Quarm `.sql` dump (issue #27), or community-contributed JSON where licensing permits.
- **"Add timer from spell" dialog** — pick a spell from a searchable, class-filtered list → keywords, duration, recast, and suggested style auto-populated. One click plus a few tweaks instead of a blank row.
- **Spell-cast auto-detection (optional)** — on a `You begin casting <Spell>` line for a spell with no timer, prompt "Add timer for <Spell>?" Yes → pre-filled dialog; No → suppressed per-character.
- **Timer template / pack export & import** — share timer sets as portable JSON (or `.ttpack` files): "Druid leveling pack", "Necro charm/snare combo", "Velious overland spawn timers".
- **Class starter packs** — curated packs per EQ class so a new player imports their class pack and has a sensible setup in seconds.
- **Zone spawn packs** — pre-built spawn timer sets for popular hunting zones, leveraging DependsOn chains.

### Mini View Skinning & Theming 🎨

Make overlays feel like part of the EQ UI rather than alien windows on top of it.

- **Per-style typography** — `FontFamily`, `FontSize`, `Bold`, `Italic` columns on `styles`; choose bundled fonts (matched to common EQ skins) or any installed system font.
- **Skin definition file format** (JSON under `Skins/`) — border style/color, padding, corner radius, background pattern/image, title-bar style, header font.
- **Bundled skins** matching popular EQ UI mods (Vert, Drakah's, Project Quarm default, Velious/Luclin-era) plus a "Thorne Timer" original.
- **Per-view skin selection** — each view picks its skin independently; great for multi-boxers running different UIs per character.
- **Skin editor / preview** — preview a skin against sample timers before applying.
- **Layout polish** — per-view padding/spacing, per-view background opacity exposed to the UI, optional skinned title bar / drag handle.

### Feed Views & Log Synthesis 📜

A second view type alongside countdown timers: a **scrolling, transformed event feed**. The killer example — vendor sale prices appear as `Rusty Short Sword — 1p 2g` next to the merchant window, optionally spoken as "one platinum two gold".

- **`ViewType` column on `miniviews`** — `Timers` (current) or `Feed` (new); existing views default to `Timers`, backwards compatible.
- **Feed renderer** — scrolling timestamped lines; newest-top or newest-bottom; configurable max lines; pause-on-hover; click-to-copy. Per-line color inherits from the originating timer's style.
- **Multiple output targets per timer** — one keyword match can drive any combination of countdown timer, feed line, speech, sound; each target has its own template.
- **Templated output with capture groups** — `FeedTemplate`, `SpeechTemplate`, `DisplayNameTemplate` columns; named groups (`{item}`, `{price}`) preferred over positional.
- **Speech sanitizer** — INI-stored substitutions so `1p 2g 3s 4c` speaks "one platinum two gold three silver four copper"; ships with an EQ-aware default set.
- **Per-target throttling** and a **"speak only if feed was idle"** option for noisy zones.
- **Persistence** — feed is runtime-only by default (like a chat window); optional "save last N lines on close" writes to `Logs/feeds/{viewname}.log`, never the tome.

**Scope boundaries (so this doesn't become a chat client):**
- ❌ No raw-log mirroring — every feed line comes from an explicit keyword match
- ❌ No combat roll-ups, DPS parsing, or two-way chat
- ✅ Yes: curated, transformed streams (vendor prices, faction hits, named sightings, tells)

### Zones, Groups & Conditions 🗺️

Context-aware timer activation for solo/small-group exploration play.

- **Zone-aware timers** — parse `You have entered <Zone>`; `Zone` column on timers (nullable, single or pipe-delimited); auto-activate / deactivate as the player zones.
- **Timer groups** — `GroupName` column for bulk enable / disable / edit; combinable with zones (e.g. a "Solusek's Eye spawns" group auto-activates in that zone).
- **Conditional / chained timers beyond DependsOn** — "start Y only if X is running", "reset all timers in group Z when keyword fires" (great for wipes: `You have been slain`).
- **Per-timer warning threshold override** — global warning is the default; per-timer "warn at last 30s" or "warn at last 10%".
- **Class profiles (optional)** — class-specific timer profiles, building on the spell library.

### Personal Play Statistics 📊

A self-improvement dashboard built mostly on data the timers already capture. The `aggregate` capture-group modifier means most stats fall out of writing timers — this is mostly *presentation*.

- **Personal play metrics** — combat survival (hits taken/min, max hit, heals), casting health (fizzle/interrupt rate, pet deaths), progression (XP/hr proxy, loot, currency), session summary (time per character/zone, camps, switches), and a **death recap** (last N lines before death, written to `Logs/deaths/`).
- **Per-timer fire-rate stats** — triggers/hour, last-fired, average interval; reveals "this timer never completes its cooldown" or "this buff drops 4s early".
- **`ViewType=Stats`** — third view type with cards, simple charts, top-N tables, CSV export; periodic refresh (250–500 ms), never per-match.
- **Architecture** — `aggregate` modifier rolls min/max/avg/count with no new parsing; in-memory dictionaries flushed to SQLite periodically; lazy computation; optional "performance mode" to disable collection; bounded ring buffers.

**Scope boundaries (so this doesn't become GamParse):**
- ❌ No raid DPS parsing, combat replay, cross-player aggregation, or upload to external services
- ✅ Yes: personal play-improvement metrics that fall out of data Thorne Timer already captures

### Directional Speech & Ping Refactor 🔧

Internal cleanup with no user-visible change — centralize the Ping execution model and remove hardcoded branch points.

- Refactor `StartTimer` / `StopTimer` / `ResetTimer` to handle Ping via the directional speech pattern
- Eliminate `|| PingTimer()` escape hatches; centralize Ping lifecycle management
- See [TD-011](../ThorneTimer/Docs/active-views/technical-debt.md) for the full 19-step plan

### Timer Maintenance Dialog (formerly Phase C) ⏸️

Split the single grid into a read-only runtime dashboard plus a dedicated editing dialog. A *UX refinement for power users with large timer/character sets* — promoted into a release only if adoption warrants the dual-grid restructure.

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
| **Final Polish & 1.0** | **v1.0.0** | **Quick mute, global hotkeys, restore UI, in-app help, accessibility, screenshots** | **🚀 Ship it** |
| Spell Library & Templates | post-1.0 | Bundled spells, "add from spell", import/export packs | 🧭 Build as desired |
| Skinning & Theming | post-1.0 | EQ-UI-matching overlays, per-style fonts | 🧭 Build as desired |
| Feed Views & Log Synthesis | post-1.0 | Scrolling feed view type, transformed streams, speech sanitizer | 🧭 Build as desired |
| Zones, Groups & Conditions | post-1.0 | Context-aware activation | 🧭 Build as desired |
| Personal Play Statistics | post-1.0 | Survival, fizzle rate, deaths, XP/hr; stats view type | 🧭 Build as desired |
| Ping Refactor | post-1.0 | Internal cleanup | 🧭 Build as desired |
| Timer Maintenance Dialog | post-1.0 | Read-only grid + `Edit > Timers...` dialog | ⏸️ Promote on adoption |

> Version numbers through v1.0.0 are the committed path; post-1.0 items are an **unordered pool** shipped as minor releases (1.1, 1.2, …) when they bring the most value.
> The **Timer Maintenance Dialog** is promoted into a release only if adoption justifies the dual-grid restructure.

---

**Last Updated:** v0.7.0 planning pass (`v0.7.0-dev` branch) — collapsed to 1.0; v0.7.0 is the last pre-1.0 release, everything beyond is a post-1.0 build-as-desired pool
**Maintained By:** Draknaré Thorne
