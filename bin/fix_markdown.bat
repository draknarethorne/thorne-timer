@echo off
REM ---------------------------------------------------------------------------
REM fix_markdown.bat - convenience launcher for bin\fix_markdown.py
REM
REM   fix_markdown.bat                 -> check repo Docs (reports, no changes)
REM   fix_markdown.bat --fix           -> auto-fix in place
REM   fix_markdown.bat --fix --aggressive  -> also flatten dashes/arrows/quotes
REM   fix_markdown.bat --fix --strip-bom path\to\file.md
REM
REM Any arguments are passed straight through to the Python script.
REM Run from anywhere; paths default to the repo's Docs folders.
REM ---------------------------------------------------------------------------
setlocal
set "SCRIPT_DIR=%~dp0"

where python >nul 2>nul
if errorlevel 1 (
	echo [error] Python not found on PATH. Install Python 3 or add it to PATH.
	exit /b 2
)

python "%SCRIPT_DIR%fix_markdown.py" %*
exit /b %ERRORLEVEL%
