# v[VERSION] Release Checklist

**Status:** Pending
**Release Date:** [DATE]
**Previous Release:** v[PREV_VERSION]

---

## ✅ Pre-Release

- [ ] All feature work committed and pushed to `main`
- [ ] Application tested:
  - [ ] Launch and create a timer
  - [ ] Verify mini view overlays display correctly
  - [ ] Test character switching (save/restore state)
  - [ ] Test audio alerts (TTS and WAV)
  - [ ] Verify timer styles route to correct mini views
- [ ] No known critical bugs or regressions

## 📝 Documentation

- [ ] Update `README.md` Version History (add v[VERSION] entry at top)
- [ ] Update `Docs/ROADMAP.md` if a milestone was completed
- [ ] Review and update `Docs/VERSION-MANAGEMENT.md` if the process changed

## 🏷️ Release Prep

- [ ] Commit documentation changes:
  ```bash
  git add README.md Docs/
  git commit -m "chore(release): prepare v[VERSION]"
  git push origin main
  ```
- [ ] Create annotated tag:
  ```bash
  git tag -a v[VERSION] -m "Release v[VERSION]: [DESCRIPTION]"
  git push origin v[VERSION]
  ```

## 🔄 Verification

- [ ] GitHub Actions workflow completed successfully ([Actions tab](https://github.com/draknarethorne/thorne-timer/actions))
- [ ] Release page shows correct version ([Releases](https://github.com/draknarethorne/thorne-timer/releases))
- [ ] ZIP file is attached and downloadable
- [ ] Release notes include installation instructions
- [ ] Auto-generated changelog looks reasonable
- [ ] Pre-release flag is correct (set only for `-rc`, `-beta`, etc.)

## 📦 Post-Release

- [ ] Download the ZIP and verify `ThorneTimer.exe` runs
- [ ] Check the EXE version (right-click → Properties → Details) matches the tag
- [ ] Announce to community (if applicable)

---

## 📌 Canonical Release Commands

```bash
# 1. Commit release prep
git add README.md Docs/
git commit -m "chore(release): prepare v[VERSION]"
git push origin main

# 2. Tag and push
git tag -a v[VERSION] -m "Release v[VERSION]: [DESCRIPTION]"
git push origin v[VERSION]

# 3. Verify at:
# https://github.com/draknarethorne/thorne-timer/actions
# https://github.com/draknarethorne/thorne-timer/releases
```

---

[← Back to Publishing Guide](PUBLISHING.md)
