# Thorne Timer Roadmap

**Repository:** [draknarethorne/thorne-timer](https://github.com/draknarethorne/thorne-timer)
**Maintainer:** Draknaré Thorne

---

## Current Status

**Latest Release:** v0.6.0 (in testing)
**Active Branch:** `v0.6.0-gui-enhancements`

---

## ✅ Shipped

### Phase A — Core Timer Engine (v0.1.0 – v0.4.0)

The foundation: a working timer application with log parsing and overlay windows.

- Core timer engine with start/end keyword matching and countdown
- Real-time EQ log file parsing via `LogMonitor`
- Text-to-speech and WAV audio alerts with per-timer configuration
- SQLite database (tome `.tdb`) for timer and settings persistence
- Always-on-top overlay windows for timer display
- CI/CD pipeline — GitHub Actions build and release workflows
- Code signing support in release workflow
- Auto-version injection from git tags into `AssemblyInfo.cs`

### Phase D — Per-Character State & Timer Styles (v0.5.0)

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

### Phase D++ — GUI Enhancements & Performance (v0.6.0) 🔄 **IN TESTING**

Quality of life improvements, critical bug fixes, per-view color configuration, and a hybrid Designer + Controller + Repository pattern for the configuration tabs.

**Shipped to branch `v0.6.0-gui-enhancements` (testing/polish in progress):**
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

**Remaining for v0.6.0 ship:**
- Bug fixes from user testing
- Additional refactoring of timer / character grid setup out of `FormMain` (mirror the Styles/Views/Categories controller+repository pattern)
- Possible UX polish (Timer "Duplicate" button, easier `DependsOn` picker)

**Why the GUI Pivot:** User hit color configuration limitations during gameplay—need for multiple views of same style with different colors became urgent. Existing global color approach didn't scale. This work also lays the per-view foundation for future properties (font size, opacity, thresholds).

---

## 🎯 Audience & Guiding Principles

Thorne Timer is built primarily for **solo and small-group players** (not raid-coordination tools). Optimization priorities derive from that audience:

1. **Timer authoring pain is the #1 user pain** — adding timers when leveling new spells, setting up spawn timers in a new zone, figuring out keywords and durations. Several phases below attack this wound from different angles.
2. **Configuration data lives in the right place** — per-character / per-timer / per-style data lives in the `.tdb` tome; preferences, hotkeys, recent files, transient runtime state can live in INI.
3. **Overlays should feel native to the EQ UI** — borders, padding, fonts, and color should be skinnable to match the player's chosen EQ UI (Vert, Drakah's, Project Quarm defaults, etc.) rather than forcing a single Thorne Timer look.
4. **The core feature is timers — feeds and synthesis are additive output modes, not a chat-client substitute.** Captured events should be *transformed* (extract item + price), not echoed verbatim. If the user wants raw log, Notepad++ tail mode already exists.
5. **Statistics earn their place by riding on existing infrastructure.** Personal play-improvement metrics (survivability, fizzle rate, deaths, XP/hr) are in scope; raid DPS dashboards, combat replay, and cross-player analytics are not — GamParse and friends already do that well.

---

## 🔄 Next

### Phase C — Timer Maintenance Dialog (v0.7.0) 🎯 **PRIORITY**

Separate gameplay view from timer maintenance, eliminating complexity in the main form.

**Core Vision:**
- **Main Form (Gameplay)** — Read-only grid locked to actively logging character, auto-switch enabled, mini-views active
- **Timer Maintenance Dialog** — Full CRUD on any character's timers without affecting active gameplay

**Key Features:**
- **Read-only timer view** — Main form grid becomes non-editable, always shows active character
- **Timer maintenance dialog** — Separate dialog for add/edit/delete timers across all characters
- **Dual-grid architecture** — Main form grid (active gameplay) + dialog grid (frozen maintenance)
- **Always-show-active mode** — Main form automatically follows actively logging character
- **No manual character browsing in main form** — Eliminates current dropdown complexity
- **Frozen timer display in dialog** — Maintenance grid loads timers with `isActive=false` (no countdown)
- **Background preservation** — Active character's timers continue running while editing others in dialog
- **Prerequisite refactor** — Extract `TimersController` + `TimersRepository` and `CharactersController` + `CharactersRepository` from `FormMain`, mirroring the v0.6.0 Styles/Views/Categories pattern. The maintenance dialog reuses the same controller against a different grid host.

**Why Priority:** The v0.6.0 controller/repository refactor plus the `isActive` flag and `GetActiveCharacterID()` on `LogMonitor` give us the right building blocks: configuration tabs already separate edit grids from runtime, and the main form can be locked to the actively logging character without snapshot/restore gymnastics. Completing Phase C also unblocks every feature in Phase G below (because `TimersController` is where Duplicate, DependsOn picker, right-click menus, etc. naturally live).

### Phase G — Smarter Timer Authoring (v0.8.0) ✨ **NEW**

The single biggest user pain identified: **adding new timers takes too long**. This phase makes authoring feel fast and forgiving.

**Keyword power features:**
- **Multiple start / end keywords per timer (OR-matching)** — pipe-delimited (`|`) values in the existing `StartKeyword` / `EndKeyword` columns; no schema change, fully backwards compatible
- **Wildcards in keywords** — `*` glob support (compiled to `Regex` and cached per timer); `^` / `$` as an opt-in full-regex escape hatch for power users
- **Capture groups → speech / display templates** — new `SpeechTemplate` and `DisplayNameTemplate` columns. With keyword `"* tells you, '*'"` and template `"{0} says {1}"`, pings can actually speak meaningful content instead of generic alerts
- **Cooldown / throttling per timer** — new `MinTriggerIntervalSeconds` column to suppress ping spam in noisy zones (busy auction channel, etc.)

**Authoring UX:**
- **Test / preview keyword button** — small dialog in the timer editor where you paste a log line (or pick from the active log file's tail) and see ✅ Match / ❌ No match plus capture group preview. Eliminates the "alt-tab into the game to trigger it" loop.
- **Duplicate button** — clone an existing timer in one click (`Name = "Copy of X"`), opens directly in edit mode. Pairs with multi-keyword for the common "I have four variants of one spell" case.
- **DependsOn picker** — replace the free-text column with a `DataGridViewComboBoxColumn` bound to the in-memory timer list, sorted by Name, refreshed when the collection changes (same pattern as v0.6.0's dynamic Style dropdown).
- **Right-click context menu on the Timers grid** — Start / Stop / Reset / Duplicate / Test / Toggle Active / Jump to last trigger.
- **Search / filter box above the Timers grid** — type-to-filter by Name / Category / Style. Essential at 130+ timers.
- **Visual "just fired" indicator** — brief row-background flash when a timer starts or expires, so users can correlate sound alerts with which timer caused them.

**Diagnostics:**
- **Per-timer trigger history (ring buffer)** — in-memory last-N triggers per timer with timestamp and matched line, viewable via right-click → "Trigger history". Makes "did my timer actually fire?" answerable without re-reading the log.
- **Per-timer fire-rate stats** — alongside trigger history, surface triggers/hour, last-fired timestamp, average interval between fires. Surfaces "this timer never actually completes its cooldown" or "this buff drops 4s early consistently" without any new parsing — it's just aggregation on data you already have. Foundation for Phase K.
- **Capture-group `aggregate` modifier** — when a capture group is marked `aggregate=true` (e.g. `{damage:aggregate}`), the runtime automatically rolls min / max / avg / count into per-timer stats. No new parsing code; statistics fall out of writing timers. This is the architectural unlock for Phase K.

### Phase H — Spell Library & Templates (v0.9.0) 📚 **NEW**

Direct attack on the "new spell → new timer" pain. Eliminate the most repetitive part of authoring entirely.

**Spell library:**
- **Bundled EQ spell database** — JSON / SQLite snapshot of spell data (name, duration, target type, recast, level by class). Shipped with the app; refreshable. Sources: existing EQ databases (Lucy, Allakhazam exports) where licensing permits, or community-contributed JSON.
- **"Add timer from spell" dialog** — pick spell from a searchable, class-filtered list → keywords, duration, recast, suggested style auto-populated. The whole timer is one click + a few tweaks instead of starting from a blank row.
- **Spell-cast auto-detection (optional)** — when a `You begin casting <Spell>` or `<You|Someone> feel(s) the <Buff>` line is seen for a spell with no timer yet, surface a non-intrusive prompt or notification: "Add timer for <Spell>?" Click yes → pre-filled dialog. Click no → suppressed per-character.

**Templates:**
- **Timer template / pack export & import** — share timer sets as portable JSON (or `.ttpack` files). Critical for community sharing: "Druid Vert's leveling pack", "Necro charm/snare combo", "Velious overland spawn timers".
- **Class starter packs** — ship curated packs per EQ class (utility spells, common buffs, pet management). New player imports their class pack and has a sensible default setup in seconds.
- **Zone spawn packs** — pre-built spawn timer sets for popular hunting zones, leveraging DependsOn chains. Solves the "new zone setup" pain.

### Phase I — Mini View Skinning & Theming (v0.10.0) 🎨 **NEW**

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

### Phase J — Feed Views & Log Synthesis (v0.11.0) 📜 **NEW**

Introduces a second view type alongside countdown timers: a **scrolling, transformed event feed**. Generalizes the speech-template work from Phase G into a full capture → transform → display → (optionally) speak pipeline. The killer example: vendor sale prices appear as `Rusty Short Sword — 1p 2g` next to the merchant window, optionally spoken as "one platinum two gold".

**Why this fits here:** depends on Phase G capture groups (parsing), Phase I skinning (readable styling). Strengthens the "timers are core, feeds are additive" boundary.

**New view type:**
- **`ViewType` column on `miniviews`** — `Timers` (current behavior) or `Feed` (new). Existing views default to `Timers`; backwards compatible.
- **Feed renderer** — scrolling, timestamped lines; newest-top or newest-bottom (per view); configurable max line count; pause-on-hover; click-to-copy.
- **Per-line color** inherits from the originating timer's style — the v0.6.0 style system carries straight through.
- **Auto-fade / shrink on idle** — feed compresses when no new activity, expands when busy. Optional.
- **Skin support** — feeds use the same Phase I skin system; bundled skins ship a "feed" variant tuned for readability over transparency.

**Log synthesis pipeline (generalizes Phase G):**
- **Multiple output targets per timer** — a single keyword match can trigger any combination of: countdown timer, feed line, speech, sound. Each target has its own template.
- **Templated output with capture groups** — `FeedTemplate`, `SpeechTemplate`, `DisplayNameTemplate` columns. Named groups (`{item}`, `{price}`, `{npc}`) preferred over positional (`{0}`, `{1}`) for clarity.
- **Speech sanitizer** — user-configurable substitutions (INI-stored) so `1p 2g 3s 4c` becomes "one platinum two gold three silver four copper" instead of "one P two G three S four C". Ships with a default EQ-aware substitution set.
- **Per-target throttling** — `MinFeedIntervalSeconds`, `MinSpeechIntervalSeconds`, separate from Phase G's per-timer throttle, so a busy timer can still flash visually but only speak the first match per window.
- **"Speak only if feed was idle"** option — in noisy zones, the visual feed shows everything but speech stays quiet until there's a gap.

**Persistence model:**
- Feed contents are **runtime state only by default** — not saved to `.tdb`. Behaves like a chat window.
- Optional "save last N lines on close" toggle (per-view); if enabled, written to `Logs/feeds/{viewname}.log`, not the tome.

**Hard scope boundaries (so this doesn't become a chat client):**
- ❌ No full raw-log mirroring — every feed line must come from an explicit timer / keyword match
- ❌ No combat log roll-ups, DPS parsing, or fight summaries
- ❌ No two-way chat / sending text to the game
- ✅ Yes: curated, transformed streams (vendor prices, faction hits, named-mob sightings, group invites, tells)

### Phase B — Directional Speech & Ping Refactor (v0.12.0)

Centralize the Ping timer execution model and eliminate hardcoded branch points. **Pushed later** because it's internal cleanup with no user-visible benefit — Phases G, H, I deliver more user value first.

- Refactor `StartTimer` / `StopTimer` / `ResetTimer` to handle Ping via directional speech pattern
- Eliminate `|| PingTimer()` escape hatches throughout the codebase
- Centralize Ping lifecycle management
- See [TD-011](../ThorneTimer/Docs/active-views/technical-debt.md) for the full 19-step plan

---

## 📋 Planned

### Phase E — Zones, Groups & Conditions (v0.13.0)

Context-aware timer activation — useful for solo/small-group exploration play.

- **Zone-aware timers** — parse `You have entered <Zone>` from log; `Zone` column on timers (nullable, single or pipe-delimited); auto-activate / deactivate as the player zones
- **Timer groups** — `GroupName` column for bulk-enable / disable / edit; can be combined with zones (e.g. "Solusek's Eye spawns" group auto-activates in that zone)
- **Conditional / chained timers beyond DependsOn**:
  - "Start timer Y only if timer X is currently running"
  - "Reset all timers in group Z when keyword fires" (great for wipes: `You have been slain` resets combat group)
- **Per-timer warning threshold override** — global warning is the default; per-timer override for "warn at last 30s" or "warn at last 10%"
- **Class profiles (optional)** — class-specific timer profiles per EQ class, building on Phase H's spell library

### Phase F — Power-User & Quality-of-Life (v1.0.0)

The "everything we deferred because it was a nice-to-have" release that pushes us to 1.0.

- **Global hotkeys** (low-level keyboard hook) — toggle mini-views, pause/resume all, force-switch character, "mute all sounds for N seconds" panic button
- **Quick mute button** in toolbar — doorbell / phone / boss-walked-in scenarios
- **Character online/offline time tracking** — adjust Character+ scope timers for time elapsed while logged out (server cooldowns that progress offline)
- **Better online state detection** — distinguish character inactive (manual switch) vs. logged out (camp/disconnect)
- **Timer database backup / restore tooling** — built-in `.tdb` snapshot and restore, with auto-snapshots before destructive ops
- **Performance profiling diagnostics** — built-in panel showing log-poll latency, grid sync time, mini-view paint time
- **Accessibility** — high contrast mode, screen-reader labels, larger text mode (overlaps with Phase I theming)
- **User documentation / help system** — in-app help dialog or `Help → Topics` menu pointing at bundled markdown / online guide

### Phase K — Personal Play Statistics (v1.1.0) 📊 **NEW**

The **first post-1.0 release.** Statistics earn their place only because the foundation was built opportunistically across G / H / I / J / E. Phase K is the dedicated focus that ties them together into a coherent self-improvement tool.

**Why this comes after 1.0:** Thorne Timer is a timer app first. Stats arrive only after the core authoring + feed + theming + zones story is mature. The architecture unlock (Phase G capture groups + `aggregate` modifier) means most of the parsing is already free by the time we get here — Phase K is mostly *presentation* of data already being collected.

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
- **Virtual-mode `DataGridView`** for 200+ timer datasets (Phase G+ enhancement)
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

| Phase | Version | Theme | Status |
|-------|---------|-------|--------|
| Phase A — Core Engine | v0.1.0 – v0.4.0 | Foundation | ✅ Shipped |
| Phase D — Per-Character & Styles | v0.5.0 | Multi-character | ✅ Shipped |
| Phase D++ — GUI & Performance | v0.6.0 | Styles / Views CRUD, perf | 🔄 In Testing |
| **Phase C — Maintenance Dialog** | **v0.7.0** | **Separate play from edit** | **🎯 Next** |
| **Phase G — Smarter Authoring** | **v0.8.0** | **Multi-keyword, wildcards, capture groups, test button** | **🎯 Planned** |
| **Phase H — Spell Library & Templates** | **v0.9.0** | **Spell DB, "add from spell", import/export packs** | **🎯 Planned** |
| **Phase I — Skinning & Theming** | **v0.10.0** | **EQ-UI-matching overlays, per-style fonts** | **🎯 Planned** |
| **Phase J — Feed Views & Log Synthesis** | **v0.11.0** | **Scrolling feed view type, transformed event streams, speech sanitizer** | **📜 Planned** |
| Phase B — Ping Refactor | v0.12.0 | Internal cleanup | 📋 Planned |
| Phase E — Zones, Groups, Conditions | v0.13.0 | Context-aware activation | 📋 Planned |
| Phase F — Power-User & QoL | v1.0.0 | Hotkeys, mute, backups, a11y | 📋 Future |
| **Phase K — Personal Play Statistics** | **v1.1.0** | **Survival, fizzle rate, deaths, XP/hr; stats view type; death recap** | **📊 Post-1.0** |

> Version numbers are targets and may shift as development progresses.
> **Theme grouping:** v0.7.0–v0.10.0 form a coherent "authoring & presentation" arc — separate editing from play, then make editing fast, then auto-populate from spell data, then make the overlays look native. **v0.11.0 (Phase J)** introduces the second display modality (scrolling feeds) and the log-synthesis pipeline; v0.12.0–v1.0.0 shift to internal cleanup, context awareness, and power-user polish. **v1.1.0 (Phase K)** is the first post-1.0 release, adding personal-play statistics on top of the capture-group infrastructure built in Phase G.

---

**Last Updated:** v0.6.0 testing cycle (`v0.6.0-gui-enhancements` branch)
**Maintained By:** Draknaré Thorne
