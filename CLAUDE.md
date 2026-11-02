# Project Context & Rules — Hostile Horizon

A modern remake of the *idea* of Rescue on Fractalus! (Lucasfilm Games, 1984):
fly a rescue ship low through procedurally generated alien canyons, find
downed pilots, land, let them aboard — and sometimes the thing knocking on the
airlock is not a pilot. Same core loop, all-new name, art, sound and story.

**Always check `TODO.md` for the current round before starting work.** TODO.md
is the source of truth for what to build; this file is the standing context.

## Tech Stack
- **Unity 6000.6.1f1**, URP 17.6.0, Input System 1.20.0 (Input System only, no
  old Input Manager). Same versions as Chastain Rush / Woodstock Rush so code
  can be ported between them.
- **Blender** for models, scripted where possible (`Tools/Blender/*.py`, same
  pattern as Chastain Rush). **Suno** for long music tracks, **ElevenLabs** for
  sound effects and short music/stings. All paid licenses.
- **Platforms, in order:** Windows PC + Steam Deck (Proton) first → PC VR
  (Quest 3 over Virtual Desktop / SteamVR, via OpenXR) → maybe native Quest later.
- **Dan's PC:** Dell XPS 8930, i7-9700, GTX 1660 Ti 6 GB, 64 GB RAM, Win 11.
  Performance target: 60 fps at 1080p on that PC; 40+ fps on Steam Deck at its
  own "Steam Deck" setting; VR later needs 72–90 fps, so keep headroom.
- **Repos:** game code `C:\Users\danmo\Hostile-Horizon`
  (github.com/danemoll-jpg/Hostile-Horizon). Releases / update catalog:
  github.com/danemoll-jpg/Hostile-Horizon-releases.
- **Reference project (read-only):** `C:\Users\danmo\chastain_rush`. Copy from
  it, never edit it. Source of the launcher, LauncherBridge, SteamDeck setting,
  build/cleanup tools and process rules.

## Code Style & Architecture
- **Original assets only.** Use none of the 1984 game's names (Fractalus,
  Jaggi, Valkyrie, Lucasfilm), art, sounds or text. The mechanics are the
  homage; everything you see and hear is new.
- **Core loop stays the original's.** Modern touches are added around it, not
  in place of it. When unsure, the original's behaviour is the default.
- **VR-ready from day one (standing rule).** VR is not built yet, but nothing
  may block it:
  1. The player's view is a camera *rig* (a tracking-space root with a head
     camera under it). Flat-screen code moves the rig, never assumes it owns
     the camera's local pose. VR will drive the head pose.
  2. The cockpit is a real 3D cockpit at 1:1 scale (1 unit = 1 m), with
     instruments as in-world displays, not screen-space overlays. The
     flat-screen HUD may add a screen overlay, but every piece of information
     must also exist in the cockpit.
  3. Menus are built so they can be shown on a world-space panel (UI Toolkit or
     uGUI with a canvas that can switch to World Space).
  4. No effects that only work in screen space for gameplay-critical
     information (e.g. the alien tells must be visible in the 3D world).
  5. No forced camera moves the player can't stop during flight (comfort);
     cutscene-style moments happen while landed or are fades.
  6. Input goes through Input System actions, never direct device reads, so
     XR controllers can be added as another binding set.
- **Input devices:** Thrustmaster T.Flight HOTAS 4 (stick: pitch/roll, twist:
  yaw, detachable throttle lever: absolute throttle, hat: look / camera,
  buttons), any gamepad (Xbox layout, Steam Deck built-in controls), keyboard +
  mouse as a fallback. All through one Input Actions asset with a binding set
  per device. HOTAS axes need deadzones and a calibration/invert option.
  Code cannot hold the HOTAS: HOTAS behaviour is "for Dan to check".
- **Seeded everything.** Terrain, pilot placement, enemy placement and which
  pilots are aliens all come from one seed, so a seed always plays the same.
  Seed sources: seed of the day (from the local date), random, or typed in.
- **World scale is real:** 1 unit = 1 metre.
- **KISS.** The smallest change that does what the round asks. No unrequested
  features, systems or scenery.
- **Port before writing.** If Chastain Rush / Woodstock Rush already has a
  finished system (launcher, settings, Steam Deck mode, menus, build tools),
  copy it and adapt it. Bring only what the round needs and list what came with
  it in the round report.
- **Licensing / attribution:** keep a credits screen listing tools and any
  third-party code licences (e.g. the launcher's vendor/ JSON and miniz
  licences).

## Launcher / auto-update
Same launcher as Chastain Rush (from Woodstock Rush), renamed:
- `Launcher/` (C++), `Assets/Scripts/LauncherBridge.cs`, and in `Tools/`:
  `Build-Launcher.ps1`, `New-LauncherKey.py`, `Prepare-LauncherRelease.py`,
  `Prepare-ComponentUpdate.py`, `Package-LauncherStarter.py`,
  `Publish-LauncherRelease.py`, plus `Docs/Starter/README-LAUNCHER.md`
  rewritten for Hostile Horizon.
- Exe name `HostileHorizonLauncher.exe`; catalog repo
  `danemoll-jpg/Hostile-Horizon-releases`.
- Signing key: made **once** with `python Tools/New-LauncherKey.py` into
  `Builds/PublisherPrivate/` (git-ignored). Never delete, print or replace it.
- On every launch it checks for a newer version, installs it, then starts the
  game; B/Esc skips; menu for check / update / soundtrack / restore previous.
- **Local build by default.** Only build and publish a public release when the
  round says "publish" or Dan asks.

## Process (Dan's standing rules, carried from Chastain Rush)
- **Commit before starting a round** (a safety checkpoint), and commit + push at
  the end of it.
- **Planning chat writes the round.** Dan and the planning chat (Claude) agree on
  each round and the chat writes it into `TODO.md` / `CLAUDE.md`. Code builds it.
- **Code updates `TODO.md` and `CLAUDE.md` at the end of every round:** per-item
  status (done / not done / deferred, and why — never silently skip an item),
  what changed in standing facts, and "Where things stand" below.
- **Keep `TODO.md` small:** standing decisions, the current round, queued
  rounds, the last three "Previous delivery" sections, technical notes. Older
  deliveries move word for word to `TODO-ARCHIVE.md` (grep it, don't read it
  whole).
- **Short results:** about 20 lines per round — one line per item (what changed
  + its one check, links to evidence), decisions made, "For Dan to check".
  Details go in `Data/screenshots/round<N>/`.
- **Check it as Dan plays it:** in the built Windows exe, from where a player
  starts, with the game's real input path; screenshots from the player's view
  that Code actually looks at and describes. "Checked by script" alone is not done.
- **Targeted testing:** one check per change, where it changed. Dan plays the rest.
- **Tuning limit:** if a number the round asks for is not hit after two
  attempts, stop, report the numbers, Dan decides.
- **One heavy job at a time** (Unity batch build, exe check), started at Below
  Normal priority with fewer worker threads, so Dan's PC stays usable.
- **Local build every round:** `Builds/HostileHorizon/HostileHorizon.exe`, started by
  `Play Hostile Horizon.bat` in the repo root.
- **Clean up the disk at the end of every round** (port Chastain Rush's
  `Tools/cleanup_round.py`), and give disk use before/after in one line.

## Commands
<!-- Code fills this in as commands come to exist (Round 1: build exe, play,
launcher build, cleanup). Keep them exact and copy-pasteable. -->

## Where things stand
Nothing built yet. The repo holds only these planning docs. Round 1 (see
`TODO.md`) sets up the Unity project and builds Free Flight: seeded canyon
terrain, the flight model, cameras, HOTAS + gamepad + keyboard input, crash
levels, the title menu, the Steam Deck setting and the launcher (local build
only, no publish).
