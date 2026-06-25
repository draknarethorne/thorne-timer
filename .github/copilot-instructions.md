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

### 2. Write docs as ASCII-safe where it matters

Repo Markdown is stored as **UTF-8 without a BOM**. The one character that reliably
breaks is the **section sign** `§`: in cross-references like `§5.4` it garbles in
viewers that misread the encoding, so always spell out the word `Section ` instead
(e.g. `§5` -> `Section 5`). Also avoid leftover **mojibake** (the garbled multi-
character runs left when a UTF-8 file was once saved as Windows-1252) and a UTF-8
**BOM**. Those three - section sign, mojibake, BOM - are the only must-fixes.

| Always fix | Use instead |
|---|---|
| section sign `§` (before a number) | the word `Section ` (e.g. `Section 5`) |
| mojibake (garbled Latin-1 <-> UTF-8 runs) | repair the encoding (tool below) |
| UTF-8 BOM | UTF-8 without a BOM |

**Decorative punctuation renders fine - keep it if you like.** Em/en dashes, arrows,
ellipsis, math glyphs, middot, and smart quotes all display correctly in UTF-8-aware
viewers (VS, VS Code, GitHub), so there is **no requirement** to flatten them to
ASCII. If you specifically want ASCII-only output, the tool's `--aggressive` mode
converts them.

Keep as-is (intentional, render fine, never "fix" these):
- **Emoji / pictographs** anywhere (headers, tables, prose).
- **ASCII-art diagrams** - box-drawing / block elements and geometric glyphs used
  inside diagram mockups (stored as clean UTF-8).
- **Accented letters** in names, e.g. `Draknaré`, and any other-language letters.
- **Decorative punctuation** (dashes, arrows, smart quotes, ellipsis, math) - see above.

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

### 4. Automated check/fix tool (`bin/fix_markdown.py`)

Instead of hand-scanning, use the repo's stdlib-only Python tool. By **default** it
fixes only what actually breaks: the section sign `§` -> `Section ` and classic
Latin-1 <-> UTF-8 mojibake; it reports BOMs and **preserves line endings**
(CRLF/LF untouched). It leaves alone all emoji / pictographs, accented names,
ASCII-art diagram glyphs (box-drawing / block elements `U+2500`..`U+259F` plus
curated geometric shapes), AND decorative punctuation (dashes, arrows, smart
quotes, ellipsis, math) - those render fine. Anything else non-ASCII is surfaced
as an informational "review" note. Pass `--aggressive` to also flatten decorative
punctuation to ASCII for strictly ASCII-only output. It is registered as the `bin`
Solution Items folder; see `bin/README.md`.

```powershell
python bin/fix_markdown.py                      # check repo Docs (report only, exit 1 if dirty)
python bin/fix_markdown.py --fix                # fix section sign + mojibake (UTF-8, no BOM)
python bin/fix_markdown.py --fix --aggressive   # also flatten dashes/arrows/quotes to ASCII
python bin/fix_markdown.py --check ThorneTimer/Docs Docs
```

Run `--check` after editing docs (good pre-commit gate); use `--fix` to repair.

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
