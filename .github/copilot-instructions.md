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
| Main form | `FormMain.cs` | Primary UI, timer grid, log monitoring |
| Mini views | `MiniView.cs`, `MiniViews.cs` | Overlay windows (compact/full timer display) |
| Timer engine | `TimerPlus.cs`, `Timers.cs` | Timer creation, countdown, event routing |
| Database | `Database.cs` | SQLite operations, CRUD for all entities |
| Data models | `Categories.cs`, `Characters.cs` | Domain model classes |
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
- Tables: `timers`, `categories`, `characters`, `settings`
- All schema changes require migration support in `Database.cs`
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

## Key Patterns

- **MVP-style separation**: Business logic in model classes, UI in forms
- **DataGridView binding**: Use `SortableBindingList<T>` for grid data sources
- **Timer styles**: Timers support multiple display styles (bar, countdown, stopwatch)
- **Character switching**: Users can switch active character; timers filter by character context
- **Mini view lifecycle**: Created/destroyed dynamically, always-on-top overlay windows
- **Feature-specific logic**: Prefer moving feature-specific logic into support classes/controllers/repositories instead of adding more logic directly to `FormMain` when practical.
- **Tab Management**: For Thorne Timer WinForms tabs, use a hybrid pattern: keep base UI controls in the designer for discoverability, and move tab behavior and data access into dedicated controllers and repositories while maintaining the Database as the core schema and shared access.
