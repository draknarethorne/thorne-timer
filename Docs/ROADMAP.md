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

### Phase D++ — GUI Enhancements & Performance (v0.6.0)

Quality of life improvements and critical bug fixes.

- **Voice System** — All English voice support (en-GB, en-AU, en-CA, etc.), comprehensive logging
- **Mini View Refinements** — Hidden from Alt-Tab task switcher via WS_EX_TOOLWINDOW
- **Camp-Out Auto-Pause** — Detects `/camp` with 10-second inactivity threshold, sets character to "(None)"
- **Manual Pause** — "(None)" character option for manual timer pause without camping
- **Auto-Switch Bug Fixes** — Suppress OLD character (not NEW) on manual switch, proper re-enable logic
- **Grid Performance** — O(n²) → O(n) dictionary optimization in SyncRuntimeToGrid (~98% faster with 130+ timers)
- **Character State Management** — Proper handling of "no active character" state across all operations

---

## 🔄 Next

### Phase C — Custom Views & Timer Maintenance Dialog (v0.7.0) 🎯 **PRIORITY**

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

**Why Priority:** Current v0.6.0 work (snapshot/restore, `isActive` flag, `GetActiveCharacterID()`) provides the foundation for this architecture. Completing Phase C will clean up main form complexity and deliver the intended user experience.

### Phase B — Directional Speech & Ping Refactor (v0.8.0)

Centralize the Ping timer execution model and eliminate hardcoded branch points.

- Refactor `StartTimer`/`StopTimer`/`ResetTimer` to handle Ping via directional speech pattern
- Eliminate `|| PingTimer()` escape hatches throughout the codebase
- Centralize Ping lifecycle management
- See [TD-011](../ThorneTimer/Docs/active-views/technical-debt.md) for the full 19-step plan

---

## 📋 Planned

### Phase E — Class Profiles & Zone Awareness (v0.9.0)

Intelligence features that adapt to gameplay context.

- Class-specific timer profiles — pre-built timer sets per EQ class
- Zone-aware timers — timers that activate/deactivate based on current zone
- Global (cross-character) timers — shared timers visible across all characters
- Timer templates and profile sharing

### Phase F — Advanced Automation (v1.0.0)

Full spell/ability management with smart automation.

- Spell and ability management per class
- Smart timer automation (auto-create timers from detected spells)
- Raid-oriented features (raid timer coordination, pull timers)
- Advanced log parsing (combat metrics, DPS tracking integration points)

---

## Quality & Polish (Any Version)

Ongoing improvements that can ship with any release:

### ✅ Completed in v0.6.0
- [x] **Voice system improvements** — All English voices, Alt-Tab hiding for mini views, comprehensive logging
- [x] **Auto-pause logic fixes** — Camp-out detection, manual pause via "(None)" character, proper character state management
- [x] **Auto-switch suppression bug fixes** — Suppress correct (OLD) character, proper re-enable logic on NEW character activity
- [x] **Grid performance optimizations** — O(n²) → O(n) dictionary lookups in SyncRuntimeToGrid (~98% faster with 130+ timers)
- [x] **Character-scope timer pausing** — Character-scope timers now properly pause when viewing inactive characters (only run when actively logging)

### 🔄 High Priority (v0.7.0 - Phase C)
- [ ] **Timer maintenance dialog** — Separate dialog for add/edit/delete timers without affecting gameplay
- [ ] **Read-only main form grid** — Lock main form to actively logging character, no manual browsing
- [ ] **Always-show-active mode** — Main form automatically follows LogMonitor active character
- [ ] **Dual-grid architecture** — Gameplay grid (running) + maintenance grid (frozen)

### 📋 Future Improvements
- [ ] **Virtual mode DataGridView** — For 200+ timer datasets, on-demand row loading (Phase C+ enhancement)
- [ ] **Incremental grid updates** — Only refresh changed timer rows, not entire grid (Phase C+ enhancement)
- [ ] **Character online/offline time tracking** — Track elapsed time when switching characters to adjust Character+ timers (server cooldowns that progress offline)
- [ ] **Online state detection** — Better distinguish between character inactive (manual switch) vs logged out (camp/disconnect)
- [ ] **Custom user-defined views** — Beyond the four fixed mini views, user-created overlays (Phase C extension)
- [ ] **Style-to-view linking** — Assign which timer styles appear in which views (Phase C extension)
- [ ] **Timer import/export** — Share timer configurations between users
- [ ] Screenshots for README.md (main grid, mini views, configuration)
- [ ] Accessibility improvements (high contrast, screen reader support)
- [ ] User documentation / help system
- [ ] Timer database backup/restore tooling
- [ ] Performance profiling tools (built-in diagnostics)

---

## Timeline

| Phase | Version | Status |
|-------|---------|--------|
| Phase A — Core Engine | v0.1.0 – v0.4.0 | ✅ Shipped |
| Phase D — Per-Character & Styles | v0.5.0 | ✅ Shipped |
| Phase D++ — GUI & Performance | v0.6.0 | ✅ Shipped (in testing) |
| **Phase C — Maintenance Dialog** | **v0.7.0** | **🎯 Next (Priority)** |
| Phase B — Ping Refactor | v0.8.0 | 📋 Planned |
| Phase E — Class Profiles | v0.9.0 | 📋 Planned |
| Phase F — Advanced Automation | v1.0.0 | 📋 Future |

> Version numbers are targets and may shift as development progresses.
> **Phase C prioritized** to clean up main form complexity and deliver intended dual-grid architecture.

---

**Last Updated:** June 2025
**Maintained By:** Draknaré Thorne
