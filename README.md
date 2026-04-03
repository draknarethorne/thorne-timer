<p align="center">
  <img src="ThorneTimer/Resources/ThorneTimerLogo.png" alt="Thorne Timer" width="400"/>
</p>

<h1 align="center">Thorne Timer</h1>

<p align="center">
  <strong>A tactical overlay timer and log event notification system for Project Quarm</strong>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#screenshots">Screenshots</a> •
  <a href="#getting-started">Getting Started</a> •
  <a href="#how-it-works">How It Works</a> •
  <a href="#roadmap">Roadmap</a> •
  <a href="#building-from-source">Building</a> •
  <a href="#related-projects">Related Projects</a>
</p>

---

## About

**Thorne Timer** is a companion desktop application designed to enhance your gameplay experience on [Project Quarm](https://www.projectquarm.com/) — a PoP-era EverQuest server built on [The Al'Kabor Project (TAKP)](https://www.takproject.net/) foundation.

It watches your EverQuest log files in real-time and gives you always-on-top timer overlays, voice alerts, and event-driven notifications — the tactical information edge that UI files alone simply cannot provide.

### Why Thorne Timer?

The native TAKP UI files can only go so far. While [**Thorne-UI**](https://github.com/draknarethorne/thorne-ui) transforms your in-game interface with enhanced windows, better layouts, and quality-of-life improvements, there are things you simply *cannot* do with XML UI files alone:

- **Always-visible timers** that persist across zone transitions and game restarts
- **Log-triggered automation** that responds to game events in real-time
- **Voice alerts** and customizable text-to-speech notifications
- **Multi-character tracking** across boxing sessions
- **Separate overlay windows** for different timer types so you see what matters

Thorne Timer fills that gap — it's the overlay companion that lives *outside* the game, watching your log files and giving you the information you need, when you need it.

> 🎯 This is a community side project — built by a player, for players. Feedback, ideas, and contributions are always welcome!

---

## Features

### 🎯 Timer Overlays (Mini Views)
Compact, always-on-top windows that float over your game client. Four distinct view types keep your screen organized:

- **Normal** — Standard countdown timers for respawns, cooldowns, and custom events
- **Buff** — Track buff durations so you never let a key buff drop
- **Pet** — Dedicated pet-related timers for summoners and beastlords
- **Ping** — Instant visual notifications for log events (tells, auction alerts, custom triggers)

Each view remembers its position independently — arrange them once and they stay put.

### ⏱️ Real-Time Log Parsing
Point Thorne Timer at your EQ log file and it starts working immediately:

- **Start keywords** trigger timers when specific text appears in your log
- **End keywords** stop timers early when conditions are met
- **Case-sensitive matching** for precision when you need it
- **Endless mode** for timers that restart automatically

### 🔊 Voice & Sound Alerts
Never miss a timer expiration again:

- **Text-to-speech** — Configure spoken alerts for any timer
- **WAV file playback** — Use custom sound effects
- **Adjustable volume and speech rate** — Tune it to your preference
- **Per-timer audio** — Different sounds for different events

### 📊 Timer Styles
Each timer has a **Style** (Normal, Buff, Pet, or Ping) that determines which overlay window displays it. This lets you:

- Separate combat timers from buff tracking
- Keep ping notifications in their own corner
- Organize your screen the way you play

### 🗂️ Tome System
Your data lives in a **Tome** (`.tdb` file) — a portable SQLite database that stores everything:

- All your timers, characters, categories, and settings
- Automatically migrated from older versions (EQTimer → ThorneTimer)
- Create multiple tomes for different setups (raiding, soloing, tradeskills)
- Recent tomes menu for quick switching

### 👥 Multi-Character Support
- Maintain separate characters with their own log file paths
- Switch active character on the fly
- Each character's mini view positions are remembered

### 🖥️ Smart Window Management
- **Column width persistence** — resize grid columns once, they're saved to your tome
- **Screen bounds safety** — windows that end up offscreen (monitor changes, etc.) are automatically repositioned to the nearest visible screen
- **Window state persistence** — the app remembers its size, position, and state between sessions

---

## Screenshots

<!-- 
  TODO: Add screenshots showing:
  - Main timer grid with timers configured
  - Mini view overlays floating over the EQ client
  - Timer configuration (Style, keywords, duration)
  - Multiple mini views arranged on screen
-->

*Screenshots coming soon — check back after the next release!*

---

## Getting Started

### Download & Install

1. Download the latest release from the [**Releases**](https://github.com/draknarethorne/thorne-timer/releases) page
2. Extract `ThorneTimer-vX.X.X.zip` to a folder of your choice
3. Run **`ThorneTimer.exe`**

> 🔄 **Upgrading?** Just extract over your existing folder. Your tome will be migrated automatically — all timers, characters, and settings are preserved.

### Requirements
- Windows with .NET Framework 4.8 (included in Windows 10 1903+)
- EverQuest with logging enabled (`/log on` in game)

### Quick Setup
1. **Add a character** — give it a name and browse to your EQ log file (e.g., `eqlog_Draknaré_project1999.txt`)
2. **Select your character** from the dropdown and click **Start Parsing**
3. **Create timers** — set start keywords that match text in your log, set a duration, and optionally add voice/sound alerts
4. **Show Mini Views** — click the mini view button to see your overlay windows
5. **Play!** — timers trigger automatically as events appear in your log

---

## How It Works

```
EverQuest Log File
       │
       ▼
 ┌─────────────┐     ┌──────────────┐     ┌─────────────────┐
 │  Log Parser  │────▶│ Timer Engine │────▶│  Mini View      │
 │ (real-time)  │     │  (matching)  │     │  Overlays       │
 └─────────────┘     └──────────────┘     │  ┌───────────┐  │
                            │              │  │  Normal   │  │
                            ▼              │  │  Buff     │  │
                     ┌──────────────┐     │  │  Pet      │  │
                     │ Voice/Sound  │     │  │  Ping     │  │
                     │   Alerts     │     │  └───────────┘  │
                     └──────────────┘     └─────────────────┘
```

Thorne Timer reads your EQ log file line by line as new entries appear. When a line matches a timer's **start keyword**, the timer begins counting down. When it matches an **end keyword** (or the duration expires), the timer fires its alert. The overlay windows update in real-time so you always know what's active.

---

## Roadmap

Thorne Timer is evolving into a **tactical HUD** for serious multi-boxing and raid gameplay:

| Phase | Description |
|-------|-------------|
| **Current** | Timer styles, mini view overlays, per-timer settings, column persistence, UI polish |
| **Next** | Per-character timer collections, style-to-view linking, configuration dialogs |
| **Planned** | Class-specific timer profiles, zone-aware timers, global (cross-character) timers |
| **Future** | Full spell/ability management per class with smart timer automation |

The goal: the **ultimate timer and notification system** tailored to how *you* play.

---

## Building from Source

### Prerequisites

- **Visual Studio 2019/2022/2025+** with the **.NET desktop development** workload
- **.NET SDK** — for `dotnet restore` ([download](https://dotnet.microsoft.com/download))
- **Git** — for cloning the repo

### Quick Start

```bash
# Clone the repository
git clone https://github.com/draknarethorne/thorne-timer.git
cd thorne-timer

# Restore NuGet packages
dotnet restore "Thorne-Timer.sln"

# Build (from Developer Command Prompt or with MSBuild on PATH)
msbuild "Thorne-Timer.sln" /t:Rebuild /p:Configuration=Debug /p:Platform="Any CPU"
```

### Visual Studio Setup

1. Open `Thorne-Timer.sln` in Visual Studio
2. Go to **Build → Configuration Manager** and ensure Platform is set to `Any CPU`
3. Right-click **ThorneTimer** in Solution Explorer → **Set as Startup Project**
4. Press **F5** to build and run

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `BaseOutputPath/OutputPath property is not set` | Ensure Platform is `Any CPU` (with space) in Configuration Manager. Run `dotnet restore`. |
| Missing NuGet/targets errors | Run `dotnet restore "Thorne-Timer.sln"` |
| Old icon showing in Windows | Clear Windows icon cache: delete files in `%localappdata%\Microsoft\Windows\Explorer\iconcache*` and restart Explorer |

---

## Contributing

This is a community side project built to help fellow Project Quarm players. Contributions, ideas, and feedback are all welcome!

- 🐛 **Found a bug?** [Open an issue](https://github.com/draknarethorne/thorne-timer/issues)
- 💡 **Have an idea?** Start a discussion or submit a feature request
- 🛠️ **Want to contribute?** Fork the repo and submit a pull request

---

## CI/CD

This project uses GitHub Actions for continuous integration and release automation.

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| **Build** | Push to `main`/`working-on-views`, PRs | Compiles Debug & Release, uploads artifacts |
| **Release** | Push `v*` tags | Builds, packages, creates GitHub Release with auto-generated changelog |

### Creating a Release

```bash
# Merge your work to main
git checkout main
git merge working-on-views

# Tag a new version
git tag v1.0.0
git push origin main --tags
```

The release workflow will automatically:
1. **Extract version from tag** (`v0.1.0` → `0.1.0.0`)
2. **Inject into AssemblyInfo.cs** — the built EXE has the correct version embedded
3. Build the Release configuration
4. Package all required files into a ZIP
5. Create a GitHub Release with download links and **auto-generated changelog** from commits since the previous tag

**Versioning:** Use semantic versioning (e.g., `v0.1.0`, `v1.0.0`, `v2.0.0-beta`). Pre-release tags containing `-` are automatically marked as pre-release.

> 💡 **Note:** You don't need to manually update `AssemblyInfo.cs` before releases — the workflow handles it automatically!

---

## License

*License information coming soon.*

---

<p align="center">
  <sub>Built with ☕ for the Project Quarm community by Draknaré Thorne</sub><br/>
  <sub>⚔️ See you in Norrath</sub>
</p>
