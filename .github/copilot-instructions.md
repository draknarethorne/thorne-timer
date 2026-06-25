# Copilot Instructions for Thorne Timer

## Project Overview

**Thorne Timer** is a Windows desktop application (C# WinForms, .NET Framework 4.8) that monitors EverQuest log files and triggers overlay timers for in-game events. It replaces the legacy GINA timer application.

- **Repository**: [draknarethorne/thorne-timer](https://github.com/draknarethorne/thorne-timer)
- **Solution**: `Thorne-Timer.sln` → `ThorneTimer/ThorneTimer.csproj`
- **Database**: SQLite via `System.Data.SQLite` + Entity Framework 6
- **Build**: Visual Studio 2022 / MSBuild, targets .NET Framework 4.8

## Architecture

| Component | File(s) | Responsibility |
|-----------|---------|---------------|
| Entry point | `Program.cs` | Application bootstrap |
| Main form | `FormMain.cs`, `FormMain.Designer.cs` | Primary UI shell, timer grid, log monitoring wiring |
| Log monitoring | `LogMonitor.cs` | Multi-file polling; tracks `selectedCharacterID` (UI) vs `IsActive` (file growth) |
| Timer engine | `TimerRuntime.cs`, `TimerPlus.cs`, `Timers.cs` | Timer state, countdown, character-state save/restore |
| Mini views | `MiniView.cs`, `MiniViews.cs` | Always-on-top overlay windows; per-view colors and `EmptyBehavior` |
| Styles tab | `StylesController.cs`, `StylesRepository.cs` | First-class style entity with Add/Delete/Rename + color picker |
| Views tab | `ViewsController.cs`, `ViewsRepository.cs` | Per-view colors, `ShowWarning`, `EmptyBehavior`, dynamic style filter |
| Categories tab | `CategoriesController.cs`, `CategoriesRepository.cs` | Reference Add/Delete grid pattern |
| Database | `Database.cs` | SQLite schema/migration, shared CRUD helpers (`isTableExist`, `isFieldExist`, `EnsureViewExists`) |
| Data models | `Categories.cs`, `Characters.cs`, `StyleData.cs` | Domain model classes |
| Diagnostics | `ThorneLog.cs`, `ThorneArchive.cs` | INI-driven file logger, tiered log retention |
| Utilities | `ComboBoxItem.cs`, `SortableBindingList.cs` | UI helpers |
| About dialog | `FormAbout.cs` | Version and credits |

## Critical Coding Standards

### SQL — Always Parameterized
Every SQL query MUST use parameterized commands. Never concatenate user input into SQL strings.

```csharp
// ✅ Correct
cmd.CommandText = "SELECT * FROM timers WHERE Name = @name";
cmd.Parameters.AddWithValue("@name", name);

// ❌ Never do this
cmd.CommandText = "SELECT * FROM timers WHERE Name = '" + name + "'";
```

### Threading — Invoke for UI Updates
All UI updates from background threads (timer ticks, file watcher callbacks) MUST use `Invoke` or `BeginInvoke`.

```csharp
if (InvokeRequired)
    Invoke(new Action(() => UpdateLabel(text)));
else
    UpdateLabel(text);
```

### Resources — Always Dispose
- Wrap `SQLiteConnection`, `SQLiteCommand`, `SQLiteDataReader` in `using` blocks
- Unsubscribe event handlers when forms close
- Dispose mini view windows properly

### Naming Conventions
- **PascalCase**: Public members, methods, properties, classes
- **camelCase**: Private fields, local variables, parameters
- **Prefix**: Private fields may use underscore prefix (`_fieldName`)

## Database

- File extension: `.tdb` (Thorne Database / "tome" files)
- Core tables: `timers`, `categories`, `characters`, `classes`, `styles`, `miniviews`, `settings`
  - `styles` — first-class style entity (`ID`, `Name` UNIQUE, `ForeColor`, `BackColor`, `SortOrder`); seeded with Normal/Buff/Pet/Ping/Spawn/Lockout/Character on first run
  - `miniviews` — per-view configuration (`Name`, `StyleFilter`, `ActiveYn`, `PositionX/Y`, `SortOrder`, `ForeColor`, `BackColor`, `ShowWarning`, `EmptyBehavior`)
  - `settings` — still holds legacy color columns (`MiniViewNormFore/BuffFore/PingFore`) consumed only by the one-shot `StylesRepository.MigrateUserColorsFromLegacyViews` upgrade path
- All schema changes require migration support in `Database.cs` (uses `isTableExist` / `isFieldExist` to be idempotent)
- Startup migrations are **one-shot**: once a table exists, defaults are not re-seeded — user deletions and edits stick
- Connection string uses relative path to `.tdb` file

## Build & Release

- **Build**: `msbuild Thorne-Timer.sln /p:Configuration=Release`
- **Release workflow**: `.github/workflows/release.yml` — triggers on `v*` tags
- **Version**: Update `Properties/AssemblyInfo.cs` (AssemblyVersion + AssemblyFileVersion)
- **Packages**: NuGet via `packages.config` (not PackageReference)

## File Organization

```
Thorne-Timer/
├── .github/
│   ├── agents/          # Copilot agent definitions
│   └── workflows/       # GitHub Actions (release.yml)
├── ThorneTimer/         # C# project source
│   ├── Properties/      # AssemblyInfo, Resources, Settings
│   ├── Resources/       # Embedded resources (icons, images)
│   ├── Sounds/          # Audio files for timer alerts
│   └── *.cs             # Source files
├── packages/            # NuGet packages (git-ignored)
└── Thorne-Timer.sln     # Solution file
```

## Testing Guidelines

- No formal test framework currently — validation is manual + code review
- Use `get_errors` after edits to verify compilation
- Test SQL changes against a copy of the `.tdb` file
- Verify cross-thread safety for any timer or file watcher code

## Documentation

Internal design/spec docs live in `ThorneTimer/Docs/` (see `ThorneTimer/Docs/STATUS.md`
for the index). When adding or changing documentation, follow these rules:

### 1. Register new docs in the project (MANUAL step — easy to forget)

`ThorneTimer.csproj` uses **classic `<None Include="...">` items**, NOT a glob.
New files do **not** appear automatically. After creating a doc you MUST:

- Add `<None Include="Docs\your-new-doc.md" />` to the existing
  `<ItemGroup>` that holds the other `Docs\*.md` entries in `ThorneTimer.csproj`.
- The IDE locks the `.csproj` while the solution is open, so edit it from the
  **terminal** (e.g. a PowerShell text insert) rather than the file-edit tool.
- Also add the doc to the table in `ThorneTimer/Docs/STATUS.md` so it is discoverable.

A doc that is not in the `.csproj` will not show in Solution Explorer's Docs folder.

### 2. Write docs as ASCII-safe Markdown (avoid "funky characters")

Repo Markdown is stored as **UTF-8 without a BOM**. Some viewers (Solution Explorer
preview, editors defaulting to the system codepage) then misread multi-byte UTF-8 as
Windows-1252, so decorative Unicode shows up as garbled glyphs (e.g. an em dash `—`
appears as `â€"`). To stay safe and portable, prefer plain ASCII:

| Avoid (Unicode) | Use (ASCII) |
|---|---|
| em dash `—`, en dash `–` | ` - ` (spaced hyphen) |
| ellipsis `…` | `...` |
| arrows `→` `⇒` | `->` `=>` |
| math `×` `≈` `≤` `Σ` | `x` `~=` `<=` `sum` |
| box-drawing `│ ─ ┌ ┐ └ ┘ ├ ┼` | `| - + ` for ASCII diagrams |
| middot `·` separators | `/` or `,` |
| play/UI glyphs `▶ ▸ ▾` | `>` / `(play)` / `v` |

Acceptable non-ASCII (intentional, also safe in Latin-1 or used consistently):
status emoji in headers (`📐`, `✅`, `🗄️`), the section sign `§`, and accented names
(e.g. `Draknaré`).

### 3. Tooling note (how to write docs without corrupting bytes)

When creating/rewriting a doc from the terminal, write it with an explicit
**UTF-8-without-BOM** encoder, not bare `Set-Content -Encoding UTF8` (which adds a
BOM the repo does not use):

```powershell
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
```

After writing, verify: confirm `BOM=False`, scan for mojibake markers
(`Ã`, `â€`, `Â`), and list any chars `> U+007F` to catch stray decorative Unicode
before committing.

## Key Patterns

- **MVP-style separation**: Business logic in model classes, UI in forms
- **DataGridView binding**: Use `SortableBindingList<T>` for grid data sources
- **Timer styles**: Timers support multiple display styles (bar, countdown, stopwatch)
- **Character switching**: Users can switch active character; timers filter by character context
- **Mini view lifecycle**: Created/destroyed dynamically, always-on-top overlay windows
- **Feature-specific logic**: Prefer moving feature-specific logic into support classes/controllers/repositories instead of adding more logic directly to `FormMain` when practical.
- **Tab Management**: For Thorne Timer WinForms tabs, use the hybrid Designer + Controller + Repository pattern: keep base UI controls in the designer for discoverability, move tab behavior into a dedicated controller (e.g. `StylesController`, `ViewsController`, `CategoriesController`), and put SQLite CRUD into a typed repository (e.g. `StylesRepository`, `ViewsRepository`). `Database.cs` stays the home of shared schema, migrations, and helpers.
- **Style colors**: A style's `ForeColor` is the canonical style color. The main timer grid lightens it for the row tint; mini views use it directly for timer text. When changing style semantics, update both `StylesRepository`/`StylesController` and the main grid row painter.
- **Migrations**: Make startup migrations idempotent and one-shot. Never re-seed defaults into an existing table — it will undo user deletions.
- **Mini-view rendering (Classic vs. Thorne)**: Mini views support two interchangeable renderers behind the `IThorneMiniView` interface — **Classic** (`MiniView.cs`, the original `TableLayoutPanel` fixed-layout renderer, kept as the permanent fallback) and **Thorne** (`ThorneView.cs`, the layered-window custom-paint skin engine). `MiniViews.cs` orchestrates both through the interface only (no concrete-type branching); a factory selects the renderer per view based on the `miniviews.RenderEngine` column (`0=Classic`, `1=Thorne`), with a global override that forces Classic as a kill-switch. Engine changes are applied by tear-down/recreate via `RefreshMiniViews`, never a live hot-swap. `RenderEngine` defaults to Classic so existing `.tdb` files are unaffected. Note the two **Classic/Thorne axes are distinct**: `RenderEngine` picks the *painter*, while the per-view `TimePlacement` (Left/Right) only picks the *time-slot side* within the Thorne layout. See `ThorneTimer/Docs/styles-and-views-enhancements.md`.

## Copilot Configuration

- Prefer the latest AI models (e.g., Claude Opus 4.8, Claude Sonnet 4.6) when configuring Copilot custom agents or selecting models.
