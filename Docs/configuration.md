# Configuration Guide

Thorne Timer reads two kinds of configuration:

1. **`ThorneTimer.ini`** - a plain-text file next to `ThorneTimer.exe`. These are
   **global** settings that apply to **every** tome (`.tdb`) you open: diagnostic
   logging, database backups, and the auto-pause / character-switch monitor.
2. **The Settings tab (per-tome)** - preferences stored *inside* each `.tdb` file,
   so they travel with that tome: voice, overlay opacity, warning time, colors,
   and class filtering.

> **Rule of thumb:** if a setting is about *how the app runs on this PC*, it is in
> `ThorneTimer.ini`. If it is about *how this particular tome behaves*, it is on
> the Settings tab and saved in the `.tdb`.

This page documents the INI file. The INI itself is also fully commented, so you
can read every option and its default right inside the file.

---

## Where is `ThorneTimer.ini`?

It lives in the same folder as `ThorneTimer.exe` (your install folder). A fully
commented copy ships with every build, so you already have one. Open it in any
text editor (Notepad works), change a value, save, and restart Thorne Timer.

- Lines beginning with `;` or `#` are comments.
- Keys are case-insensitive; values are trimmed of surrounding whitespace.
- If the file is missing, or a key is absent, the built-in **default** is used.
- INI values take **priority** over values stored in a tome.

---

## Sections at a glance

| Section | Controls | Scope |
|---|---|---|
| `[Logging]` | Diagnostic log verbosity, file mode, retention | Global (all tomes) |
| `[Monitoring]` | Camp-out auto-pause, crash fallback, switch sensitivity | Global (all tomes) |
| `[Backups]` | Automatic tome backups at startup, retention | Global (all tomes) |

---

## `[Monitoring]` - auto-pause and character switching

Thorne can automatically switch the active character to `(None)` and pause its
timers when you camp out, so a respawn clock does not keep ticking after you have
logged off. Three knobs tune this behavior. Times are in **seconds**; set a
timeout to `0` to disable just that trigger.

> **Testing tip:** lower the two timeouts (for example `3` and `15`) so the
> auto-pause fires quickly instead of waiting the full production windows.

| Key | Default | Meaning |
|---|---|---|
| `CampInactivityThresholdSeconds` | `10` | Quiet period after a `camp` warning appears in the log before the camp-out is committed. Short, because the warning already confirms intent. `0` disables. |
| `InactivityTimeoutSeconds` | `300` | Fallback for ungraceful exits (client crash, Alt-F4, link-dead / disconnect) where no camp warning is ever written and the log just goes silent. After this many seconds with no new log bytes, the active character is treated as gone. Kept long because normal play almost always emits some log noise. `0` disables. |
| `SwitchThresholdBytes` | `10` | Minimum bytes a non-active character log must grow before an automatic character switch fires. Raise it if benign background writes (OS flushes, antivirus) cause spurious switches. Must be `1` or greater. |

```ini
[Monitoring]
CampInactivityThresholdSeconds=10
InactivityTimeoutSeconds=300
SwitchThresholdBytes=10
```

---

## `[Logging]` - diagnostic log files

Thorne writes diagnostic log files (handy when reporting a bug). This section
controls how chatty they are and how long they are kept.

| Key | Default | Meaning |
|---|---|---|
| `Enabled` | `true` | Master switch. `false` silences all logging without recompiling. |
| `MinLevel` | `Info` | Minimum severity recorded: `Debug`, `Info`, `Warn`, or `Error`. |
| `Mode` | `Session` | `Session` = one log file per launch; `Daily` = one file per calendar day. |
| `RecentDays` | `7` | Days considered "recent" - the recent tier keeps `MaxFilesPerDay` per day. |
| `MaxFilesPerDay` | `3` | Max log files kept per day during the recent tier. |
| `RetentionDays` | `30` | Days to keep at least one file per day; beyond this, files thin to one per month. |
| `MaxAgeDays` | `90` | Absolute max age; everything older is deleted. |
| `MaxTotalFiles` | `50` | Hard cap on total log files across all tiers. |

---

## `[Backups]` - automatic tome backups

At startup Thorne can back up your tome so you can recover from accidents. This
section controls whether it does and how many backups it keeps. Backups default
to a full year of retention so you can recover from long-ago issues.

| Key | Default | Meaning |
|---|---|---|
| `Enabled` | `true` | Master switch for automatic backups at startup. |
| `RecentDays` | `7` | Days considered "recent" - the recent tier keeps `MaxFilesPerDay` per day. |
| `MaxFilesPerDay` | `5` | Max backup files kept per day during the recent tier. |
| `RetentionDays` | `30` | Days to keep at least one backup per day; beyond this, backups thin to one per month. |
| `MaxAgeDays` | `365` | Absolute max age; everything older is deleted. |
| `MaxTotalFiles` | `100` | Hard cap on total backup files across all tiers. |

---

## Tiered retention (how `[Logging]` and `[Backups]` prune files)

Both sections prune old files at startup with the same tiered algorithm. It keeps
more detail for recent files and thins out over time:

```
Tier 1 - Recent  (0 .. RecentDays)                keep MaxFilesPerDay per day
Tier 2 - Daily   (RecentDays+1 .. RetentionDays)  keep 1 per day
Tier 3 - Monthly (RetentionDays+1 .. MaxAgeDays)  keep 1 per month
Tier 4 - Expired (older than MaxAgeDays)           delete all
```

After tiered cleanup, `MaxTotalFiles` enforces a hard cap on the total number of
surviving files. Set any value to `0` to disable that particular limit.

---

## Per-tome settings (the Settings tab)

These are **not** in the INI - they live inside each `.tdb` and are edited on the
in-app **Settings** tab:

- **Voice:** selected voice, speech rate, and volume (with a Test button).
- **Overlay opacity:** mini-view transparency.
- **Warning time / colors:** when a timer turns its warning color, and the
  fore/background colors for normal, warning, and ping styles.
- **Ping display time:** how long ping notifications stay on screen.
- **Class filtering:** show all classes vs. the active character's class.

Because these are stored in the tome, copying a `.tdb` to another machine carries
its preferences along with its timers.

---

## Quick reference: which file changes what?

| I want to change... | Where |
|---|---|
| How fast camp-out auto-pauses | `ThorneTimer.ini` -> `[Monitoring]` |
| Crash / disconnect fallback timeout | `ThorneTimer.ini` -> `[Monitoring]` |
| Spurious auto-switch sensitivity | `ThorneTimer.ini` -> `[Monitoring]` |
| Log verbosity / how many logs are kept | `ThorneTimer.ini` -> `[Logging]` |
| Whether/how many tome backups are kept | `ThorneTimer.ini` -> `[Backups]` |
| Voice, rate, volume | Settings tab (per tome) |
| Overlay opacity, warning time, colors | Settings tab (per tome) |
| Ping display duration | Settings tab (per tome) |
| Show all classes vs. active class | Settings tab / toolbar (per tome) |

---

**See also:** the [main README](../README.md) for install and getting-started, and
[`ROADMAP.md`](ROADMAP.md) for planned configuration work (more INI/Settings
coverage is part of v0.7.0 fine-tuning).
