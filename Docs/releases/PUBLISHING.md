# Publishing a Release

This guide explains how to create and publish releases for Thorne Timer on GitHub.

---

## 🎯 Quick Answers

**Q: Do I need to build locally?**
**A: No! GitHub Actions builds, packages, signs, and publishes automatically when you push a tag.**

**Q: Where do users find releases?**
**A: https://github.com/draknarethorne/thorne-timer/releases** (or click "Releases" in the right sidebar)

**Q: What do I need to do?**
**A: Update README changelog → commit → push a git tag → everything else is automatic.**

---

## Overview

GitHub Releases provide a way to distribute Thorne Timer with:

- **Automatic build and packaging** — no local Release builds needed
- **Code signing** — executable is signed automatically (when certificate is configured)
- **Downloadable ZIP** — contains EXE, DLLs, native binaries, Sounds, Data
- **Auto-generated changelog** — from git commit history since the last tag
- **Version injection** — git tag version is embedded in the built EXE
- **Pre-release support** — tags with `-` suffix are auto-marked as pre-release

---

## What Gets Released

Each release includes:

1. **`ThorneTimer-v{VERSION}.zip`** — complete application package containing:
   - `ThorneTimer.exe` (version-stamped, optionally code-signed)
   - All dependency DLLs
   - `ThorneTimer.exe.config`
   - Native SQLite binaries (`x64/`, `x86/`)
   - `Sounds/` directory (default audio files)
   - `Data/` directory (seed data)
   - `README.md`
2. **Release notes** — static installation instructions + auto-generated commit changelog
3. **Version tag** — git tag marking the exact commit

---

## Creating a Release

### Method 1: Automated Release (Recommended)

**🎯 Everything happens automatically on GitHub's servers — no local builds needed.**

#### 1. Prepare the Release

Complete all code changes and verify the application works:

```bash
# Ensure everything is committed and pushed
git status
git push origin main
```

#### 2. Update Documentation

Update the README.md Version History section with the new version entry:

```markdown
**v0.5.0** (June 2025)

- ✅ Per-character timer state persistence
- ✅ Auto character switching via log file detection
- ✅ Timer Styles system (Normal, Buff, Pet, Ping)
- ... etc.
```

Optionally update `AssemblyInfo.cs` and `.csproj` ApplicationVersion to match:

```csharp
[assembly: AssemblyVersion("0.5.0.0")]
[assembly: AssemblyFileVersion("0.5.0.0")]
```

> 💡 The release workflow overwrites AssemblyInfo.cs at build time, so this step is for keeping your development builds current — not strictly required.

#### 3. Commit and Push Release Prep

```bash
git add README.md
git commit -m "chore(release): prepare v0.5.0"
git push origin main
```

#### 4. Create and Push the Version Tag

```bash
# Create an annotated tag with a descriptive message
git tag -a v0.5.0 -m "Release v0.5.0: Per-character state, auto-switch, timer styles, mini views"

# Push the tag (this triggers the release workflow)
git push origin v0.5.0
```

#### 5. Monitor and Verify

1. Go to **[GitHub Actions](https://github.com/draknarethorne/thorne-timer/actions)** and watch the workflow run (~2-3 minutes)
2. When complete, go to **[Releases](https://github.com/draknarethorne/thorne-timer/releases)** and verify:
   - Release name and tag are correct
   - ZIP download is attached
   - Release notes include installation instructions and changelog
   - Pre-release flag is correct (if applicable)

### Method 2: Manual Release (Fallback)

If the workflow fails and you need to release immediately:

```bash
# Build locally in Release configuration
msbuild Thorne-Timer.sln /p:Configuration=Release /p:Platform="Any CPU"

# Package manually
# Gather: ThorneTimer.exe, *.dll, *.config, x64/, x86/, Sounds/, Data/, README.md
# Create ZIP: ThorneTimer-v0.5.0.zip

# Upload through GitHub web interface
# Go to Releases → Draft a new release → Upload ZIP → Write notes → Publish
```

---

## Pre-Release Workflow

For release candidates or beta versions:

```bash
# Tag with a hyphen suffix — auto-detected as pre-release
git tag -a v0.6.0-rc.1 -m "Release candidate: v0.6.0-rc.1"
git push origin v0.6.0-rc.1
```

The workflow detects `contains(github.ref_name, '-')` and marks the release accordingly.

---

## Release Checklist

Use the [RELEASE-CHECKLIST-TEMPLATE.md](RELEASE-CHECKLIST-TEMPLATE.md) for a copy-paste checklist per version. Key items:

- [ ] All code changes committed and pushed
- [ ] Application tested (launch, create timer, verify mini views, character switch)
- [ ] README.md Version History updated with new entry
- [ ] Release prep committed (`chore(release): prepare vX.Y.Z`)
- [ ] Annotated tag created and pushed
- [ ] GitHub Actions workflow completed successfully
- [ ] Release page verified (ZIP, notes, pre-release flag)
- [ ] Download ZIP and verify it runs correctly

---

## Commit Message Conventions

Since the auto-generated changelog is built from commit messages, write clear, descriptive messages:

**Good:**
```
fix: Character switch no longer leaks timer state to wrong character
feat: Add Ping timer support to character switch save/restore
chore(release): prepare v0.5.0
```

**Avoid:**
```
fix stuff
wip
updates
```

The changelog groups commits by the messages, so each message should tell users what changed.

---

## Troubleshooting

### Release workflow didn't trigger

- Verify the tag starts with `v`: `git tag -l`
- Ensure commits were pushed *before* the tag: `git push origin main` then `git push origin v0.5.0`
- Check the Actions tab for error messages

### Missing ZIP file in release

- Check the workflow logs for the "Create release package" step
- Verify the build succeeded (check the "Build Release" step)
- Ensure the release-package directory structure is correct

### Changelog is empty or unhelpful

- The workflow uses GitHub's `generate_release_notes: true` which compares against the previous tag
- For the first release, it includes all commits
- Write descriptive commit messages for better changelogs

### Code signing failed

- The workflow checks `HAS_SIGNING_CERT` — if no certificate secret is configured, signing is silently skipped
- To enable: add `SIGNING_CERTIFICATE` (base64 PFX) and `PFX_PASSWORD` as repository secrets
- The executable still works without signing; users may see a Windows SmartScreen warning

---

## Best Practices

1. **Test before tagging** — run the application and verify core functionality works
2. **Clear commit messages** — they become the auto-generated changelog
3. **Update README first** — the Version History is the user-facing changelog
4. **One release per tag** — don't reuse or delete+recreate tags unless absolutely necessary
5. **Keep release notes clean** — the static body template handles installation; commits handle the changelog
6. **Announce to community** — share the release link after publishing

---

**Last Updated:** June 2025
**Workflow Version:** 1.0
