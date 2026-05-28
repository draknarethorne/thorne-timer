# Thorne Timer Documentation

**Repository:** [draknarethorne/thorne-timer](https://github.com/draknarethorne/thorne-timer)
**Maintainer:** Draknaré Thorne

---

## 🚀 START HERE

### **[VERSION-MANAGEMENT.md](VERSION-MANAGEMENT.md)** 📋
How we version, tag, and release Thorne Timer. Read this before creating any release.

### **[releases/PUBLISHING.md](releases/PUBLISHING.md)** 📦
Step-by-step guide for creating a GitHub Release — from prep to publish.

### **[ROADMAP.md](ROADMAP.md)** 🗺️
Detailed development roadmap with phase breakdowns and future plans.

---

## Directory Structure

### `archive/` 🗄️
Historical design documents and superseded proposals — retained for context but no longer reflect the current architecture. Useful for understanding why certain paths were chosen (or rejected).

### `releases/` 📦
Release process documentation, templates, and checklists.

**Start with** `releases/PUBLISHING.md` for the complete release workflow.

Includes:
- Publishing guide (step-by-step release process)
- Release checklist template (reusable per-version)
- Release notes template (for manual or enhanced notes)

### `../ThorneTimer/Docs/` 🔧
Internal architecture and technical design documentation.

Includes:
- Architecture redesign notes
- Auto character switching design
- Active views design (partially superseded by v0.6.0)
- Phase C maintenance dialog priority
- Per-view color implementation notes (banner-marked, archived after v0.6.0 release)
- Technical debt tracking

---

## Quick Links

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
