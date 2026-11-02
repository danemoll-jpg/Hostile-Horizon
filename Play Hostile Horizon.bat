@echo off
rem Starts the newest local build. Build it first: in Unity, Hostile Horizon > Build Executable (or the batch command in CLAUDE.md).
rem Ported from Chastain Rush's "Play Chastain Rush.bat". To start through the launcher instead: "Play Hostile Horizon (launcher).bat".
setlocal
set "GAME=%~dp0Builds\HostileHorizon"
if not exist "%GAME%\HostileHorizon.exe" (
  echo Hostile Horizon has not been built yet. In Unity: Hostile Horizon ^> Build Executable.
  pause
  exit /b 1
)
start "" /D "%GAME%" "%GAME%\HostileHorizon.exe"
