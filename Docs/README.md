# Thorne Timer Documentation

**Repository:** [draknarethorne/thorne-timer](https://github.com/draknarethorne/thorne-timer)
**Maintainer:** Draknaré Thorne

---

> 👤 **Just want to use the app?** Start with the main [**project README**](../README.md) — it covers
> install, the first-run security warning, working with timers (with real examples), and an FAQ.
> The documents in this folder are **contributor/maintainer** material (release process, roadmap, design history).

---

## 🚀 START HERE (contributors)

### **[VERSION-MANAGEMENT.md](VERSION-MANAGEMENT.md)** 📋
How we version, tag, and release Thorne Timer. Read this before creating any release.

### **[releases/PUBLISHING.md](releases/PUBLISHING.md)** 📦
Step-by-step guide for creating a GitHub Release — from prep to publish.

### **[ROADMAP.md](ROADMAP.md)** 🗺️
Detailed development roadmap with phase breakdowns and future plans.

### **[configuration.md](configuration.md)** ⚙️
User-facing configuration reference: every `ThorneTimer.ini` option (`[Logging]`,
`[Monitoring]`, `[Backups]`) and the per-tome Settings tab. Linked from the main README.

---

## Directory Structure

### `archive/` 🗄️
Historical design documents, superseded proposals, and past session-handoff notes — retained for context but no longer reflecting the current architecture. Useful for understanding why certain paths were chosen (or rejected).

### `images/` 🖼️
Screenshots referenced by the main [README](../README.md). See [`images/README.md`](images/README.md) for the expected file names and capture guidelines.

### `perf/` ⚡
Performance investigation and refactor write-ups (e.g. the grid-filter refactor).

### `releases/` 📦
Release process documentation, templates, and per-version notes.

**Start with** `releases/PUBLISHING.md` for the complete release workflow.

Includes:
- Publishing guide (step-by-step release process)
- Release checklist + notes templates (reusable per-version)
- `releases/notes/` — the per-version changelog entries

### `../ThorneTimer/Docs/` 🔧
Internal architecture and technical design documentation. See [`STATUS.md`](../ThorneTimer/Docs/STATUS.md) for an index of what is planned, in progress, implemented, or historical.

Includes:
- Architecture redesign notes and the long-term MVP roadmap
- Feature design docs (auto character switching, camp-out auto-pause, character-scope pausing)
- Styles & views enhancement spec + progress tracker
- Active-views design and technical-debt tracking

---

## Quick Links

- **[Configuration Guide](configuration.md)** ⚙️ INI options and per-tome Settings (user-facing)
- **[Version Management](VERSION-MANAGEMENT.md)** 📋 Versioning strategy and release workflow
- **[Release Publishing](releases/PUBLISHING.md)** 📦 How to create a release
- **[Roadmap](ROADMAP.md)** 🗺️ Development phases and future plans
- [Main Project README](../README.md)
- [Architecture Notes](../ThorneTimer/Docs/architecture-redesign.md)
- [Technical Debt](../ThorneTimer/Docs/active-views/technical-debt.md)

---

## Documentation Conventions

**This `Docs/` directory** contains contributor-facing and user-facing documentation:
- Release processes, version management, roadmaps
- Anything a contributor or user needs to understand the project

**`ThorneTimer/Docs/`** contains internal technical documentation:
- Architecture decisions and design documents
- Schema migrations and codebase analysis
- Implementation-specific technical debt tracking

Keep both directories current when making significant changes.

---

**Maintained by:** Draknaré Thorne
