<p align="center">
  <img src="ThorneTimer/Resources/ThorneTimerLogo.png" alt="Thorne Timer" width="400"/>
</p>

<h1 align="center">Thorne Timer</h1>

<p align="center">
  <strong>A tactical overlay timer and log event notification system for Project Quarm</strong>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#roadmap">Roadmap</a> •
  <a href="#installation">Installation</a> •
  <a href="#building-from-source">Building</a> •
  <a href="#related-projects">Related Projects</a>
</p>

---

## About

**Thorne Timer** is a companion desktop application designed to enhance your gameplay experience on [Project Quarm](https://www.projectquarm.com/) — a PoP-era EverQuest server built on [The Al'Kabor Project (TAKP)](https://www.takproject.net/) foundation. It provides real-time timer overlays ("mini views") that float above your game client, giving you precise tracking of spells, abilities, respawns, and custom events.

### Why Thorne Timer?

The native TAKP UI files can only go so far. While [**Thorne-UI**](https://github.com/draknarethorne/thorne-ui) transforms your in-game interface with enhanced windows, better layouts, and quality-of-life improvements, there are things you simply *cannot* do with XML UI files alone:

- **Always-visible timers** that persist across zone transitions
- **Log-triggered automation** that responds to game events in real-time
- **Voice alerts** and text-to-speech notifications
- **Multi-character tracking** across boxing sessions

Thorne Timer fills that gap — it's the overlay companion that lives *outside* the game, watching your log files and giving you the tactical information edge you need.

---

## Features

- 🎯 **Mini View Overlays** — Compact, always-on-top timer windows that float over your game
- ⏱️ **Log Parsing** — Automatically triggers timers from in-game log events
- 🔊 **Voice & Sound Alerts** — Configurable audio notifications with text-to-speech support
- 🎨 **Customizable Appearance** — Adjustable colors, opacity, and font sizes for warning/normal/ping states
- 👥 **Multi-Character Support** — Track active characters and switch contexts on the fly
- 📁 **Category Organization** — Group timers by category for easy management

---

## Roadmap

Thorne Timer is evolving into a **tactical HUD** for serious multi-boxing and raid gameplay:

| Phase | Description |
|-------|-------------|
| **Current** | GUI refinements, icon/visual updates, stability improvements |
| **Next** | Per-character timer collections — each character maintains their own set of timers |
| **Planned** | Global timers (all characters), class-specific timers, and category/zone-aware timers |
| **Future** | Full architecture rework to support spells, abilities, and actions per class with smart timer management |

The goal: the **ultimate timer and notification system** tailored to how *you* play.

---

## Related Projects

The **Thorne** suite is designed to work together for the ultimate Project Quarm experience:

| Project | Description |
|---------|-------------|
| [**Thorne-UI**](https://github.com/draknarethorne/thorne-ui) | Custom TAKP UI overhaul — anatomical equipment layouts, multi-color gauges, class-specific slot art, and quality-of-life improvements across 60+ XML files |
| **Thorne Timer** *(this repo)* | Overlay timer & log notification companion — the features you *can't* build with UI files alone |

> 💡 **Tip:** Use both together! Thorne-UI handles your in-game windows, Thorne Timer handles your external overlays and automation.

---

## Installation

### Pre-built Releases
> *Coming soon* — check the [Releases](https://github.com/draknarethorne/thorne-timer/releases) page.

### Building from Source
See [Building from Source](#building-from-source) below.

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

# Switch to the active development branch
git checkout active-views

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

Contributions, ideas, and feedback are welcome! Feel free to open an issue or submit a pull request.

---

## License

*License information coming soon.*

---

<p align="center">
  <sub>Built with ☕ for the Project Quarm community by Draknaré Thorne</sub>
</p>
