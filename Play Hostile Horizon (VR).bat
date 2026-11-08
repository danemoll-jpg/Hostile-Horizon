@echo off
rem Round 8 (C2): starts the newest local build in VR (-vr): Quest 3 over Virtual Desktop, or SteamVR. Start Virtual Desktop or
rem SteamVR first. With no headset or OpenXR runtime found, the game says so and starts on the flat screen.
setlocal
set "GAME=%~dp0Builds\HostileHorizon"
if not exist "%GAME%\HostileHorizon.exe" (
  echo Hostile Horizon has not been built yet. In Unity: Hostile Horizon ^> Build Executable.
  pause
  exit /b 1
)
start "" /D "%GAME%" "%GAME%\HostileHorizon.exe" -vr
