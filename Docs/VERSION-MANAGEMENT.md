# Version Management

**Maintainer:** Draknaré Thorne
**Repository:** [draknarethorne/thorne-timer](https://github.com/draknarethorne/thorne-timer)

---

## Overview

This project follows **Semantic Versioning (SemVer)** adapted for a .NET Framework desktop application. The release workflow automatically injects version numbers into the built executable from git tags — you don't manually edit version strings for releases.

---

## Version Format

```
MAJOR.MINOR.PATCH.BUILD
```

**Examples:** `0.5.0.0`, `1.0.0.0`, `1.2.3.0`

### What Each Number Means

- **MAJOR** (e.g., `1.0.0.0`): Significant redesign, breaking changes to timer database schema, or major feature overhauls
- **MINOR** (e.g., `0.5.0.0`): New features, new timer styles, new overlay modes, or significant enhancements
- **PATCH** (e.g., `0.5.1.0`): Bug fixes, small tweaks, or documentation updates
- **BUILD** (e.g., `0.5.0.0`): Always `0` for releases; reserved for CI differentiation if needed

### Git Tag Format

Tags use 3-part SemVer with a `v` prefix: `v0.5.0`, `v1.0.0`, `v1.2.3`

The release workflow automatically expands to 4-part for AssemblyInfo: `v0.5.0` → `0.5.0.0`

---

## Version Sources of Truth

### 1. Git Tags (Release Markers) — Primary

**Format:** `vMAJOR.MINOR.PATCH` (e.g., `v0.5.0`)

**Purpose:**
- Trigger the automated release workflow
- Mark specific commits as releases
- Create downloadable release packages on GitHub

**Rules:**
- Always use annotated tags (`git tag -a`)
- Always include a descriptive message
- Pre-release tags use a hyphen suffix: `v0.5.0-beta`, `v0.5.0-rc.1`

### 2. AssemblyInfo.cs (Development Reference)

**Location:** `ThorneTimer/Properties/AssemblyInfo.cs`

**Purpose:**
- Shows the current development version in the built executable
- Updated manually during development to reflect what you're working toward
- **Overwritten automatically** by the release workflow at build time

**Contains:**
```csharp
[assembly: AssemblyVersion("0.5.0.0")]
[assembly: AssemblyFileVersion("0.5.0.0")]
```

> 💡 You can update AssemblyInfo.cs during development to keep the debug build version current, but the release workflow will inject the correct version from the git tag regardless.

### 3. ThorneTimer.csproj ApplicationVersion

**Location:** `ThorneTimer/ThorneTimer.csproj`

**Purpose:**
- ClickOnce deployment version (legacy; kept in sync for consistency)
- Update alongside AssemblyInfo.cs during development

### 4. README.md Version History (Changelog)

**Purpose:**
- Human-readable changelog visible on the repository landing page
- Historical record of what shipped in each version
- Updated as part of release preparation

---

## Release Workflow

### How It Works

The GitHub Actions release workflow (`.github/workflows/release.yml`) triggers when you push a version tag:

1. **Extracts version** from the tag (`v0.5.0` → `0.5.0.0`)
2. **Injects version** into `AssemblyInfo.cs` (overwrites whatever is there)
3. **Builds** the Release configuration
4. **Signs** the executable (if signing certificate is configured)
5. **Packages** EXE, DLLs, native binaries, Sounds, and Data into a ZIP
6. **Creates** a GitHub Release with installation instructions and auto-generated changelog
7. **Detects pre-release** tags (any tag containing `-`) and marks accordingly

### Step-by-Step Release Process

See [releases/PUBLISHING.md](releases/PUBLISHING.md) for the complete guide.

**Quick version:**

```bash
# 1. Update README.md Version History
# 2. Update AssemblyInfo.cs and .csproj (optional but recommended)
# 3. Commit release prep
git add README.md ThorneTimer/Properties/AssemblyInfo.cs
git commit -m "chore(release): prepare v0.5.0"
git push origin main

# 4. Create and push annotated tag
git tag -a v0.5.0 -m "Release v0.5.0: Per-character state, auto-switch, timer styles"
git push origin v0.5.0

# 5. Monitor GitHub Actions → verify release on Releases page
```

---

## Release Scenarios

### Scenario 1: Feature Release (Minor Version)

**Context:** Completed mini view overlays and per-character timer state

```bash
# Current: v0.4.0 → Target: v0.5.0

# 1. Update README.md Version History (add v0.5.0 entry)
# 2. Update AssemblyInfo.cs to 0.5.0.0
# 3. Commit, push, tag
git add README.md ThorneTimer/Properties/AssemblyInfo.cs
git commit -m "chore(release): prepare v0.5.0"
git push origin main
git tag -a v0.5.0 -m "Release v0.5.0: Per-character state, auto-switch, timer styles, mini views"
git push origin v0.5.0
```

### Scenario 2: Bug Fix (Patch Version)

**Context:** Fixed character switch timer leak

```bash
# Current: v0.5.0 → Target: v0.5.1

# 1. Update README.md (brief entry focused on fix)
# 2. Commit, push, tag
git add README.md
git commit -m "chore(release): prepare v0.5.1"
git push origin main
git tag -a v0.5.1 -m "Release v0.5.1: Fix character switch timer leak"
git push origin v0.5.1
```

### Scenario 3: Pre-Release

**Context:** Testing a release candidate before final release

```bash
# Tag with hyphen suffix — auto-marked as pre-release
git tag -a v0.6.0-rc.1 -m "Release candidate: v0.6.0-rc.1"
git push origin v0.6.0-rc.1
```

---

## Version History Archive

Historic versions and their significance:

- **v0.5.0** (June 2025): Per-character state, auto-switch, timer styles, mini views, scope system
- **v0.1.0 – v0.4.0** (2025): Core timer engine, log parsing, audio alerts, CI/CD pipeline, code signing

---

## Best Practices

### DO:
✅ Use annotated tags (`git tag -a`) with descriptive messages
✅ Update README.md Version History before tagging
✅ Test before tagging — run the app, verify core functionality
✅ Follow SemVer guidelines consistently
✅ Write descriptive commit messages (they appear in auto-generated changelogs)
✅ Keep AssemblyInfo.cs roughly current during development

### DON'T:
❌ Use lightweight tags (they don't trigger the workflow reliably)
❌ Tag without testing
❌ Push tags before pushing commits
❌ Delete tags from remote (causes workflow issues and broken release links)
❌ Reuse version numbers
❌ Manually edit AssemblyInfo.cs *for releases* (the workflow handles it)

---

## Troubleshooting

### Issue: "Tag already exists"

```bash
# Delete local tag
git tag -d v0.5.0

# Delete remote tag (use with caution!)
git push origin :refs/tags/v0.5.0

# Recreate tag
git tag -a v0.5.0 -m "Release v0.5.0: Description"
git push origin v0.5.0
```

### Issue: "Release workflow didn't trigger"

**Possible causes:**
1. Tag doesn't start with `v`
2. Tag pushed before commits were pushed
3. Workflow YAML has syntax errors

**Solution:**
```bash
# Verify tag format
git tag -l

# Check that the tagged commit is on the remote
git log --oneline -1 v0.5.0

# Re-push tag if needed
git push origin v0.5.0

# Check GitHub Actions tab for errors
```

### Issue: "Version in EXE doesn't match tag"

The release workflow overwrites AssemblyInfo.cs at build time. If the built version is wrong:
1. Check the workflow logs for the "Update AssemblyInfo.cs with version" step
2. Verify the tag format parses correctly (`v0.5.0` → `0.5.0.0`)

---

## References

- **Semantic Versioning:** https://semver.org/
- **Git Tagging:** https://git-scm.com/book/en/v2/Git-Basics-Tagging
- **GitHub Releases:** https://docs.github.com/en/repositories/releasing-projects-on-github
- **Release Workflow:** [`.github/workflows/release.yml`](../.github/workflows/release.yml)

---

**Last Updated:** June 2025
**Maintained By:** Draknaré Thorne
