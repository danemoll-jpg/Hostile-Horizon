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
  5. **Realism first, comfort options OFF by default (Dan, 2026-11-02).** Dan
     is not prone to VR sickness and hates snap turn and fades: "I want it to
     feel more real". So by default: smooth continuous turning, no snap turn,
     no fade-to-black transitions, no vignette. Moments like docking with the
     mothership play out live and continuous (the ship flies in; you can still
     look around). Comfort options (snap turn, fades instead of camera moves,
     vignette) may be added later as settings, off unless the player turns
     them on. This applies to flat-screen play too: prefer live, continuous
     moments over fades and cuts.
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
- **Publish every round (Dan, 2026-11-02 — replaces "local build by default").**
  Dan tests on his Steam Deck, especially while his PC is busy, so every round
  ends with a release published to `danemoll-jpg/Hostile-Horizon-releases`
  that the launcher picks up (after the round's checks pass). Version
  `0.<round>.<fix>`. A follow-up fix to a round is published too.

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
- **Testing on Dan's PC — always muted, never steal focus (Dan, 2026-11-02;
  `collaboration-process-notes.md`, "Testing on Dan's PC").**
  1. Every test run (exe checks, editor play runs, launcher checks) is muted from
     the first frame by a command-line flag (`-mute`) that sets the master volume /
     `AudioListener.volume` to 0 before any sound starts; never rely on a saved
     setting or on muting after start-up. Every check reports the measured audio
     output level, and it must be 0. Unmute only for an actual sound check, as
     briefly as possible, then mute again; say which step played sound and for how
     long.
  2. Test windows never pop up or take keyboard / mouse focus. Prefer no window
     (batch mode, off-screen rendering for screenshots); otherwise start minimized /
     not activated with run-in-background on. If a test truly needs a visible or
     focused window, stop and ask Dan first and wait for his OK.
  Hostile Horizon: `Core/TestRun.cs` (muted for `-mute`, `-exeCheck`,
  `-racerTestSave`; a listener filter zeroes the final mix and measures it; a
  `-batchmode` check draws its view off-screen); start checks with
  `python Tools/run_check.py` (batch mode, `-mute`, Below Normal).
- **One heavy job at a time** (Unity batch build, exe check), started at Below
  Normal priority with fewer worker threads, so Dan's PC stays usable.
- **Build and publish every round:** the local build
  `Builds/HostileHorizon/HostileHorizon.exe` (started by `Play Hostile Horizon.bat`
  in the repo root), then the published release (see "Launcher" above).
- **Clean up the disk at the end of every round** (Chastain Rush's
  `Tools/cleanup_round.py`), and give disk use before/after in one line.

## Commands
All from the repo root. Unity: `C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe`
(below: `Unity.exe`). Run heavy jobs at Below Normal priority, one at a time.
- **Build the exe:** Unity menu **Hostile Horizon > Build Executable**, or batch
  `Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod HostileHorizon.PlayerBuild.BuildFromCommandLine -job-worker-count 3 -logFile <file>`.
  It remakes `Assets/Scenes/Main.unity` (one GameShell; everything else is built
  in code at start) and writes `Builds/HostileHorizon/HostileHorizon.exe` +
  `BUILD_INFO.txt` (give its build time at the end of a round).
- **Play:** `Play Hostile Horizon.bat` (the exe), or `Play Hostile Horizon (launcher).bat`
  (through the launcher: a local install of the newest build in
  `Builds/LauncherLocal/`, remade by `python Tools/make_local_launcher.py` when
  the build is newer). Settings and `Player.log`:
  `%USERPROFILE%\AppData\LocalLow\Hostile Horizon\Hostile Horizon\`.
- **Career save (Round 2):** `career.json` in that same folder (beside
  `settings.json`; the launcher backs the folder up before every update and never
  changes it). Saved on dock, death, quitting to the title and closing the game.
  `Assets/Scripts/Core/Career.cs`. A check run saves into `<check dir>/save`.
- **Screen (Fix 2.1):** `Core/ScreenSetting.cs` (Chastain Rush's): full screen
  (borderless) at the display's own resolution by default, applied before the
  first scene; Settings > Display (Screen, Resolution); saved in `display.json`
  beside `settings.json`. Test runs never apply or save it.
- **Campaign tuning file (Round 2):** `Assets/Resources/Tuning/CampaignTuning.json`
  (built into the game; `Core/Tuning.cs` holds the same defaults). All Campaign
  numbers: starting credits, pilot pay, clear bonus, fuel / repair prices, free
  minimum fill, tow fee, recovery fee (base + share of ship value), exhaust kill
  range and penalty, cabin size, pilots per planet tier and their spread, pickup
  range, run speed, energy drains, hover speeds, touchdown limits, mothership
  altitude, docking height.
- **Exe self-check** (plays from the title through the input actions with a
  virtual gamepad; screenshots, fps, views, pause / settings, typed seed, every
  crash level, determinism, a height map, sound level, world-space menu):
  `Builds\HostileHorizon\HostileHorizon.exe -exeCheck <dir> [-exeCheckOnly perf|campaign] [-steamDeck 1] -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -logFile <dir>\Player.log`
  writes `<dir>/exe_check.txt` and pictures, then quits (settings are defaults,
  never saved). `Checks/ExeCheck.cs`. `-exeCheckOnly campaign` (Round 2,
  `Checks/ExeCheck.Campaign.cs`, about 15 min) plays a whole career: new career,
  launch, radar, landing, pickup, docking, exhaust kill, out of energy + tow,
  crash with a pilot aboard, regeneration, loaner, recovery, quit + Continue, and
  logs the camera's top speed in every live sequence (a cut shows as a jump). A
  check keeps running unfocused (`runInBackground`, Input System `IgnoreFocus`);
  a player's build pauses as before. Frame rates from a batch-mode check are
  measured rendering into the off-screen view (no present to a window). Round 2 launchers:
  `local/round2/build.ps1 -Name <log>`; **start every check with**
  `python Tools/run_check.py <dir> [--only all|perf|campaign|mute|screen] [--extra "-steamDeck 1"] [--size 1280x800]`
  (`-batchmode -mute`, no window, Below Normal; `mute` = a 10 s run that reports
  the output level; `fullscreen` (Fix 2.1) = a real
  start with the default screen setting applied: it opens a visible, focused full-screen
  window, so only with Dan's OK and started by hand, not by run_check.py; `screen` (Fix 2.1) = the full-screen size path for a simulated
  1280 x 800 Deck and this display, then menus / HUD measured inside the view; run
  it with `--size 1280x800`).
- **Ship model:** `F:\blender\blender.exe --background --factory-startup --python Tools/Blender/ship.py`
  writes `SourceArt/Blender/Ship.blend` and `Assets/Resources/Models/Ship.fbx`
  (cockpit points mirrored in `Assets/Scripts/Flight/ShipLayout.cs`). Blender FBX
  models import facing back: the game turns them 180 degrees on load.
- **Mothership model (Round 2):** `F:lenderlender.exe --background --factory-startup --python Tools/Blender/mothership.py`
  writes `SourceArt/Blender/Mothership.blend` and `Assets/Resources/Models/Mothership.fbx`
  (bay, doors' opening, regeneration pod mirrored in `Assets/Scripts/Campaign/Mothership.cs`).
- **Icon and cover:** `python Tools/make_icon.py` (exe icon, launcher .ico);
  `python Tools/make_cover.py <menu-free title screenshot>` (launcher cover
  `SourceArt/Title/HostileHorizonCover.png` and, Round 2, the Steam library poster
  `SourceArt/Title/HostileHorizonPoster.png` for the starter package).
- **Launcher** (Chastain Rush's / Woodstock Rush's, renamed): build
  `powershell -File Tools/Build-Launcher.ps1 [-CoreOnly]` (VS 18; SDK read from
  Racer's `Builds/LauncherSDK`) gives `Builds/Launcher/HostileHorizonLauncher.exe`
  and `LauncherChecks.exe`. Signing key `Builds/PublisherPrivate/launcher-signing.pem`
  (made once by `python Tools/New-LauncherKey.py`, key ID `0c9c29ff...`; never
  delete, print or replace it). Self-check against a local test catalog:
  `python Tools/launcher_selfcheck.py` (two signed local releases, update 1 to 2,
  start, ready handshake; output `Builds/LauncherCheck/`).
- **Releases (every round; version `0.<round>.<fix>`, build `<round>*100+<fix>`,
  e.g. 0.2.0 = build 200, tag `build-200`).** First release (Round 2):
  `python Tools/Prepare-LauncherRelease.py --game Builds/HostileHorizon --bundle-music <dir with placeholder.txt> --out Builds/LauncherRelease-200 --version 0.2.0 --build 200 --music-build 1 --notes "..."`,
  then `python Tools/Publish-LauncherRelease.py Builds/LauncherRelease-200 --publish --confirm-repository danemoll-jpg/Hostile-Horizon-releases`
  (gh: Racer's `Builds/PublisherTools`; a new draft may not be listed at once:
  re-run with `--resume-draft`). **Next rounds** publish the game alone:
  `python Tools/Prepare-ComponentUpdate.py game --source Builds/HostileHorizon --previous-catalog Builds/LauncherRelease-<last>/assets/update-catalog.json --previous-manifest Builds/LauncherRelease-<last>/assets/game-manifest.json --out Builds/LauncherRelease-<build> --build <build> --version 0.<round>.<fix> --notes "..."`
  and publish that draft the same way (keep the last release folder: it is the
  next one's "previous"; its tag is `game-<build>`, e.g. 0.2.1 = `game-201`). **Starter package** (launcher + an installed game; the
  launcher updates it on first start): `python Tools/Package-LauncherStarter.py --game <installed game folder> --game-manifest <its signed game-manifest.json> --music <soundtrack folder> --music-manifest <its soundtrack-manifest.json> --out Builds/LauncherStarter-<n> --zip-name HostileHorizon-Starter.zip --tag starter-<n> --title "..." --notes "..." --not-latest`,
  published with `Publish-LauncherRelease.py` the same way (`--not-latest`
  leaves Latest, and so every launcher's catalog, on the game release). Starter
  README `Docs/Starter/README-LAUNCHER.md`. Check a fresh install against GitHub:
  extract the published starter ZIP, then
  `HostileHorizonLauncher.exe --auto-check --root <extracted HostileHorizon> --test-save <dir> --evidence <dir>`
  (no `--catalog`: the real GitHub catalog; saves isolated). Extract it to a short
  folder (e.g. `Builds/StarterCheck`): from a 144-character path the update fails
  with "Cannot create extracted file" (Windows' 260-character path limit).
- **Clean-up at the end of a round:** `python Tools/cleanup_round.py [--dry-run]`.

## Where things stand
**Fix 2.1 (full screen on the Steam Deck) is built, checked in the exe (headless,
muted: the Deck's 1280 x 800 asks for a 1280 x 800 borderless full screen, menus
and HUD all inside the view) and published as 0.2.1 (`game-201`, Latest); a
starter-200 install updates to it from GitHub. A real start on Dan's PC (with his
OK) fills his 3840 x 2160 display but runs at 13.6 fps there: default resolution
on big displays for Dan to decide. Not yet seen by Dan on the Deck.** Round 2 (Campaign core; see `TODO.md`) is published as 0.2.0
(`build-200`) with the starter package (`starter-200`); not yet played by Dan.
Round 1 (Free Flight) was played
by Dan on PC ("ran well"); HOTAS and Steam Deck still for him to check. The
Campaign: an ongoing career saved in `career.json` (Continue / New career), a home
system and today's planet named from the seed of the day, seeded downed pilots
with wrecks and beacons, a radar (cockpit + HUD), helicopter-style landing mode
with gear, engines on / off, the pickup (run, knock, airlock), exhaust kills,
energy and the mothership tow, the Blender mothership with live launch and live
docking, credits (pay, clear bonus, fuel, repair, tow, recovery), the hangar
page, crash explosions with the burning wreck kept (all modes, no fade), death ->
a live camera flight to the regeneration pod -> recovery or the loaner. Numbers in
`Assets/Resources/Tuning/CampaignTuning.json`. Test runs follow Dan's rules
(muted, no window): `Core/TestRun.cs`, `Tools/run_check.py`. About 370-440 fps at
1080p on the 1660 Ti. Code layout: `Assets/Scripts/` Core (GameShell +
GameShell.Campaign, settings, screen setting, career, tuning, names, seeds,
TestRun), Terrain, Flight (ShipController, ShipExtras), Campaign (Mothership,
DownedPilot, PlanetSites), Effects (Explosion), Cameras, Input, UI (incl. Radar),
Audio (incl. Sfx), Checks. **Next: Round 3 (Aliens)**, to be written into
`TODO.md` by the planning chat. Design decisions (credits, ongoing campaign,
planets, the five alien types) are at the top of `TODO.md`.
