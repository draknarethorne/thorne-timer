# Documentation Status Index

> **Purpose:** Quick visibility into what is planned, implemented, in progress, or historical.
> 
> **Last Updated:** v0.7.0 planning pass (`v0.7.0-dev` branch) \u2014 reconciled with shipped v0.6.0 code
>
> **Reconciliation note:** v0.6.0 has shipped. The `TimersController`/`TimersRepository`
> and `CharactersController`/`CharactersRepository` extractions that earlier docs list as
> "in flight" are **done and merged**. The styles/views *time-format* slice shipped; the
> broader **skin-engine** spec (`styles-and-views-enhancements.md`) is still future work.

---

## Status Legend

- ✅ **Implemented** — behavior shipped in code on this branch
- 🔄 **In Progress** — partially implemented; additional work remains
- 📋 **Planned / Proposal** — design-ready, not yet implemented
- 🐞 **Bug Fix Summary** — historical implementation notes for resolved issues
- 🗄️ **Historical** — superseded or point-in-time; kept for context, not current guidance

> **Audience note:** End users should start with the repo [`README.md`](../../README.md)
> (install, security warning, working-with-timers, FAQ) and the [`Docs/ROADMAP.md`](../../Docs/ROADMAP.md).
> The documents below are **contributor/internal** design, progress, and history notes —
> useful for understanding *why* the code looks the way it does, not required to use the app.

---

## Active / Current Docs (`ThorneTimer/Docs`)

These reflect current behavior or active planning:

| Document | Type | Status | Notes |
|---|---|---|---|
| `auto-character-switching.md` | Feature Design | ✅ Implemented | Implemented behavior reference |
| `camp-out-auto-pause.md` | Feature Design | ✅ Implemented | Implemented behavior reference |
| `character-scope-timer-pausing.md` | Feature Design | ✅ Implemented | Implemented behavior reference |
| `multi-keyword-support-feature.md` | Feature Note | ✅ Implemented | Pipe-separated keyword matching shipped |
| `views-grid-completion-phase1.md` | Phase Summary | ✅ Implemented | Views grid phase completion |
| `keyword-power-features.md` | Feature Design | 📐 Design / Spec | v0.7.0 tiered keyword matching (literal→wildcard→regex), capture templates, perf/benchmark plan |
| `styles-and-views-enhancements-progress.md` | Progress Tracker | 🗄️ Historical | Time-format slice + `TimersController` extraction shipped in v0.6.0; skin-engine scope moved to ROADMAP Phase I |
| `styles-and-views-enhancements.md` | Feature Spec | 📋 Planned | Master spec for broader skin engine work (ROADMAP Phase I) |
| `architecture-redesign.md` | Architecture | 📋 Planned | Long-term MVP refactor roadmap |
| `roadmap-phase-c-priority.md` | Roadmap | 📋 Planned | Priority planning notes |
| `depends-on-chaining-enhancement.md` | Proposal | 📋 Planned | Dependency-chain optimization proposal |

## Historical / Reference Docs

Point-in-time notes kept for context — **not** current guidance:

| Document | Type | Status | Notes |
|---|---|---|---|
| `style-combo-sync-bugfix.md` | Bug Fix Summary | 🐞 Bug Fix Summary | Views/Timers style combo sync fix |
| `mini-view-per-view-colors-plan.md` | Plan | 🗄️ Historical | Planning artifact, slice shipped |
| `mini-view-per-view-colors-progress.md` | Progress Tracker | 🗄️ Historical | Prior slice completion notes |

---

## Active Views Subfolder (`ThorneTimer/Docs/active-views`)

| Document | Type | Status | Notes |
|---|---|---|---|
| `active-views-design.md` | Feature Spec | 📋 Planned | Design-level doc |
| `codebase-analysis.md` | Analysis | 📋 Planned/Historical | Design-support analysis |
| `schema-migration.md` | Migration Plan | 📋 Planned | Migration planning |
| `technical-debt.md` | Debt Tracker | 🔄 In Progress | Ongoing tracker |

---

## Usage Recommendation

For each feature area, keep this flow:

1. **Spec/Plan** (`<topic>-plan.md` or `<topic>-enhancement.md`)
2. **Progress** (`<topic>-progress.md`) while actively building
3. **Feature/Bug summary** when complete (`<topic>-feature.md` / `<topic>-bugfix.md`)
4. Update this `STATUS.md` entry in the same commit

This makes "planned vs implemented" obvious without re-reading every file.

---

## Naming Conventions

To keep this folder consistent and easy to scan:

- **Content docs** use **all-lowercase kebab-case** with a **type suffix**, not a screaming prefix:
  - Plan / spec: `<topic>-plan.md`, `<topic>-enhancement.md`, `<topic>-design.md`
  - Progress tracker: `<topic>-progress.md`
  - Completed feature note: `<topic>-feature.md`
  - Resolved bug write-up: `<topic>-bugfix.md`
  - Phase summary: `<topic>-phaseN.md`
- **Index / meta docs** are the only files in `UPPERCASE.md` — currently just `STATUS.md` here, plus `README.md` / `ROADMAP.md` / `VERSION-MANAGEMENT.md` in the repo-root `Docs/` folder.
- Pair a plan with its progress doc using the **same topic stem** (e.g. `mini-view-per-view-colors-plan.md` + `mini-view-per-view-colors-progress.md`) so they sort together.
- When a doc is superseded, move it under `Docs/archive/` rather than deleting it.
