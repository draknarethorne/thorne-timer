Thorne Timer - Setup and Development Notes
=========================================

This document lists the tools and steps required to restore, build and run the project on Windows.

Prerequisites
-------------
- Visual Studio (2019/2022/2026) with the ".NET desktop development" workload (required for MSBuild and .NET Framework 4.8 support).
- .NET SDK (for `dotnet restore` convenience). Install from https://dotnet.microsoft.com/download (any modern SDK works for `dotnet restore`).
- NuGet CLI (optional) — useful if you prefer `nuget restore`. Download `nuget.exe` from https://www.nuget.org/downloads or install with Chocolatey (`choco install nuget.commandline`).
- MSBuild / Visual Studio Build Tools — included with Visual Studio. If building from CI or CLI, install Visual Studio Build Tools.
- PowerShell 7 (optional) — nicer CLI scripting (`pwsh`).
- Git (Git for Windows) — for repo operations.

Quick install (Chocolatey examples)
----------------------------------
Run as Administrator in PowerShell if you use Chocolatey:

choco install dotnet --version=7.0.0  # example, install SDK
choco install nuget.commandline
choco install visualstudio2022buildtools --package-parameters "--add Microsoft.VisualStudio.Workload.MSBuildTools"
choco install powershell-core

Project setup steps
-------------------
1. Clone the repo and switch to the branch you want:

   git clone https://github.com/draknarethorne/thorne-timer.git
   cd thorne-timer
   git checkout active-views

2. Restore NuGet packages (pick one):

   dotnet restore "Thorne-Timer.sln"
   --or--
   nuget restore "Thorne-Timer.sln"

3. Build the solution from Developer Command Prompt or a shell with MSBuild on PATH:

   msbuild "Thorne-Timer.sln" /t:Rebuild /p:Configuration=Debug /p:Platform="Any CPU"

4. In Visual Studio: open the solution, open Build -> Configuration Manager and ensure the project Platform maps to "Any CPU" and that the project's Build checkbox is checked. If the project does not appear as expected, use the solution's Configuration Manager to add/match platforms.

5. Set the startup project: right-click `ThorneTimer` in Solution Explorer -> Set as Startup Project.

Common issues and fixes
-----------------------
- "BaseOutputPath/OutputPath property is not set" — MSBuild couldn't find a matching `<PropertyGroup Condition>` for the active Configuration|Platform. Fixes:
  - Ensure the solution configuration/platform is `Debug|Any CPU` (note the space) or that the project contains a matching condition for `AnyCPU` (no space). Use Configuration Manager to set Platform to "Any CPU".
  - Restore NuGet packages so imported targets exist.

- Missing NuGet/targets errors — run `dotnet restore` or install NuGet CLI and run `nuget restore`.

Security / git notes
--------------------
- Private signing keys (`*.pfx`) should not be committed unless intentionally shared. To stop tracking a key:

  git rm --cached ThorneTimer/ThorneTimer_TemporaryKey.pfx
  echo "ThorneTimer_TemporaryKey.pfx" >> .gitignore
  git add .gitignore
  git commit -m "Ignore private pfx key"

If you want, I can apply the README file into the repo (already committed) and optionally add a `.gitignore` change or perform the `git rm --cached` step for private keys if you confirm.

Useful commands
---------------
- Restore: `dotnet restore "Thorne-Timer.sln"`
- Rebuild: `msbuild "Thorne-Timer.sln" /t:Rebuild /p:Configuration=Debug /p:Platform="Any CPU"`
- Run tests (if any): use the Test Explorer in Visual Studio.

Contact
-------
If you want, I can add CI steps or a script to automate restore + build on your machine or in GitHub Actions.
