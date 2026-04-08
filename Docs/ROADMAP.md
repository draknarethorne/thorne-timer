# Thorne Timer Roadmap

**Repository:** [draknarethorne/thorne-timer](https://github.com/draknarethorne/thorne-timer)
**Maintainer:** Draknaré Thorne

---

## Current Status

**Latest Release:** v0.5.0
**Active Branch:** `miniview-enhancements`

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

---

## 🔄 Next

### Phase B — Directional Speech & Ping Refactor

Centralize the Ping timer execution model and eliminate hardcoded branch points.

- Refactor `StartTimer`/`StopTimer`/`ResetTimer` to handle Ping via directional speech pattern
- Eliminate `|| PingTimer()` escape hatches throughout the codebase
- Centralize Ping lifecycle management
- See [TD-011](../ThorneTimer/Docs/active-views/technical-debt.md) for the full 19-step plan

### Phase C — Custom Views & Configuration

User-defined views and UI configuration dialogs.

- Custom user-defined overlay views (beyond the four fixed defaults)
- Style-to-view linking — assign which styles appear in which views
- Per-character timer collections and visibility
- Configuration dialogs for timer settings, view management, and preferences
- Timer import/export for sharing configurations

---

## 📋 Planned

### Phase E — Class Profiles & Zone Awareness

Intelligence features that adapt to gameplay context.

- Class-specific timer profiles — pre-built timer sets per EQ class
- Zone-aware timers — timers that activate/deactivate based on current zone
- Global (cross-character) timers — shared timers visible across all characters
- Timer templates and profile sharing

### Phase F — Advanced Automation

Full spell/ability management with smart automation.

- Spell and ability management per class
- Smart timer automation (auto-create timers from detected spells)
- Raid-oriented features (raid timer coordination, pull timers)
- Advanced log parsing (combat metrics, DPS tracking integration points)

---

## Quality & Polish (Any Version)

Ongoing improvements that can ship with any release:

- [ ] Screenshots for README.md (main grid, mini views, configuration)
- [ ] Performance profiling and optimization pass
- [ ] Accessibility improvements (high contrast, screen reader support)
- [ ] User documentation / help system
- [ ] Timer database backup/restore tooling

---

## Timeline

| Phase | Version | Status |
|-------|---------|--------|
| Phase A — Core Engine | v0.1.0 – v0.4.0 | ✅ Shipped |
| Phase D — Per-Character & Styles | v0.5.0 | ✅ Shipped |
| Phase B — Ping Refactor | v0.6.0 | 🔄 Next |
| Phase C — Custom Views | v0.7.0 | 📋 Planned |
| Phase E — Class Profiles | v0.8.0 | 📋 Planned |
| Phase F — Advanced Automation | v1.0.0 | 📋 Future |

> Version numbers are targets and may shift as development progresses.

---

**Last Updated:** June 2025
**Maintained By:** Draknaré Thorne
