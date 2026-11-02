@echo off
rem Starts the game through the launcher, as a player would (Round 1). Uses a local launcher install of the newest build
rem (Builds\LauncherLocal, made by Tools\make_local_launcher.py; remade here whenever the build is newer than it).
rem Nothing is published yet, so the launcher says it couldn't check for updates and starts the installed game.
setlocal
set "ROOT=%~dp0"
set "INST=%ROOT%Builds\LauncherLocal\HostileHorizon"
if not exist "%ROOT%Builds\HostileHorizon\HostileHorizon.exe" (
  echo Hostile Horizon has not been built yet. In Unity: Hostile Horizon ^> Build Executable.
  pause
  exit /b 1
)
set "REMAKE="
if not exist "%INST%\made.txt" set "REMAKE=1"
if exist "%INST%\made.txt" (
  for /f %%i in ('powershell -NoProfile -Command "if ((Get-Item '%ROOT%Builds\HostileHorizon\HostileHorizon_Data\globalgamemanagers').LastWriteTime -gt (Get-Item '%INST%\made.txt').LastWriteTime) {1} else {0}"') do if "%%i"=="1" set "REMAKE=1"
)
if defined REMAKE python "%ROOT%Tools\make_local_launcher.py" || (pause & exit /b 1)
start "" /D "%INST%" "%INST%\HostileHorizonLauncher.exe"
