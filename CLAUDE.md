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
  sound effects and short music/stings. All paid licenses. **Meshy**
  (meshy.ai, paid license + API; Dan, 2026-11-02) for generated 3D models,
  finished in Blender; **Mixamo** (mixamo.com) for rigging / character animation.
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
  (borderless) by default at the display's own resolution capped at 1080 tall
  (Dan: 4K ran 13.6 fps; 1920 x 1080 there, 1280 x 800 on the Deck), applied before the
  first scene; Settings > Display (Screen, Resolution); saved in `display.json`
  beside `settings.json`. Test runs never apply or save it.
- **Campaign tuning file (Round 2):** `Assets/Resources/Tuning/CampaignTuning.json`
  (built into the game; `Core/Tuning.cs` holds the same defaults). All Campaign
  numbers: starting credits, pilot pay, clear bonus, fuel / repair prices, free
  minimum fill, tow fee, recovery fee (base + share of ship value), exhaust kill
  range and penalty, cabin size, pilots per planet tier and their spread, pickup
  range, run speed, energy drains, hover speeds, touchdown limits, mothership
  altitude, docking height. Fix 2.4 adds the flight-feel and landing numbers: `pitchRate` 95,
  `rollRate` 170, `yawRate` 55, `bankTurnFull/Light` 38 / 26, `levelRateFull/Light` 22 / 8,
  `stickCurve` .35, `airBrake` 42 m/s², `hoverVerticalAccel` 15, `hoverSinkHigh` 30 (above
  `hoverSinkHighAbove` 150 m), `hoverSinkMid` 14 (`hoverSinkMidAbove` 60 m), `hoverSinkLow` 3
  (below `hoverSinkLowBelow` 8 m), `stopMarkerRange` 2500, `brakeCueLead` 150, `resetHeight` 60.
  Fix 2.5 changes / adds: `pitchRate` 82, `rollRate` 150, `yawRate` 44, `stickCurve` .6,
  `gamepadDeadzone` .12 (pad / Deck sticks, on top of the Input System's own 12.5 %),
  `brakeCueLead` 40 (to the pilot's pad), `pilotMaxClimb` 30 m, `launchEnergyWarning` 50 %,
  lift / docking speeds `motherComeSpeed/Accel` 420 / 36, `beamLiftSpeed/Accel` 320 / 100,
  `dockApproachSpeed/Accel` 200 / 45, `dockFinalSpeed/Accel` 150 / 45, `bayRiseFrom` 25 m,
  `bayRiseSpeed` 25, `bayRiseBrake` 12, `towBeamDelay` .5 s, `fastForward` 4.
  Fix 2.3 adds: `recallFee` 900, `recallHoldSeconds` 1,
  `homeClimbRate` 30 (m/s assumed for the climb to dock), `homeMargin` 3 %,
  `returnNowWarning` 8 %, `beamReach` 600 m, `pilotFuelMin` / `pilotFuelMax` 3 / 15 %.
- **Recall (Fix 2.3):** the `Recall` input action, **held 1 s**: gamepad View
  (Select; the Deck's View button), keyboard **H**, HOTAS **button 7**. The
  mothership comes and its beam lifts you to the dock (in the air or landed),
  `recallFee` at the dock; refused while a pilot is running / knocking / boarding.
  At 0 energy in the air the same beam catches the ship (tow fee). Code:
  `GameShell.Campaign` (`calling`, `BeamLift`, `HomeWarnings`, `SkyMarker`).
- **Pause / reset (Fix 2.4):** pause anywhere in play, including launch, docking,
  tow, recall and the beam (time stops; resume carries on): gamepad Start or a
  **tap** of View (held 1 s it is still the recall), keyboard Esc, HOTAS button 9
  (Settings > Controls and HOTAS > "HOTAS pause button", any of 1-16) or 10. A
  player's build auto-pauses on focus loss (`OnApplicationFocus` / `OnApplicationPause`;
  check runs don't). "≡ / Esc: Pause" shows on the HUD and the cockpit display for the
  first minute of a flight. Pause menu: Resume, **Reset position** (hover 60 m over
  the nearest landing pad, same cabin / energy, free; greyed out while a pilot runs /
  knocks / boards and during the automatic parts), Free Flight: Restart; Campaign:
  Recall (900 cr); Settings, quit. Code: `GameShell.Pause / Resume / ResetPosition`.
- **Landing (Fix 2.4):** X / G / HOTAS 4 from flight = air brakes (top speed to hover
  speed in 2.5 s, stopped in about 4 s, brake sound), BRAKING / HOVER (height in big
  type) / TOUCHDOWN / LANDED states on the HUD and the cockpit display; near a waiting
  pilot (2.5 km) an amber ring + post on the ground marks where the ship would stop
  (`ShipController.StopPoint`), and "SLOW DOWN — PRESS X" on the radar when braking
  now stops you near the pilot. Hover descent: 30 m/s full down above 150 m, 14 at
  60 m, 3 m/s (a safe touchdown) below 8 m.
  Fix 2.5: the cue and the amber ring aim at the waiting pilot's pad (green ring within
  900 m), the amber ring also shows in hover. **Belly camera** (`Cameras/BellyCam.cs`):
  on by itself in landing mode, D-pad down / V / HOTAS 8 toggles it; on the cockpit's
  centre display and as a HUD picture-in-picture; landed with a pilot coming it is the
  airlock camera. **Glass floor** at the feet and glass in the lower canopy (front of the
  side consoles) in `ship.py`. **Landing view** (5th in the camera cycle): high over the ship.
- **Pickup (Fix 2.5):** a pilot comes when landed with engines off within `pickupRange`
  160 m level and `pilotMaxClimb` 30 m of height (else the advice line says they can't
  reach you); they run over the ground round the hull to the door (left side). At the
  door: "PILOT AT AIRLOCK — press B to open" (the button for the device last used:
  `Controls.LastDevice`, `AirlockKey`), the lit AIRLOCK button over the centre display,
  KNOCK KNOCK KNOCK on the displays with each knock (a brighter knock sound).
- **Fast-forward (Fix 2.5, Dan's ask):** hold A / Space / HOTAS trigger (the UI Submit action)
  during an automatic part (launch, docking, tow, recall, the beam): it plays live at
  `fastForward` x (no cut, VR rule 5); flight is always back at normal speed. Lift from the
  ground into the bay about 10 s (0.2.4: 54 s), docking 19.5 s (0.2.4: 40-50 s), ~5 s with A held.
- **Launch warning (Fix 2.5):** Launch below `launchEnergyWarning` asks "Refuel and
  launch" (default) / "Launch anyway"; the hangar shows a refuel line below 100 %.
- **Flight feel (Fix 2.4, Fix 2.5):** Settings > **Flight assist** Full (0.2.3's bank turn 38
  and levelling 22 °/s) / **Light** (default; Fix 2.5: stick centred = the ship holds its
  attitude: the bank turn (26) only while pulling, no levelling) / Off; **Stick sensitivity**
  50-150 % (100); a response curve on the sticks (tuning `stickCurve`). Rates pitch 82 /
  roll 150 / yaw 44 °/s (0.2.3: 70 / 130 / 32; 0.2.4: 95 / 170 / 55). Fix 2.5: hover heading
  follows the stick at once (only the lean eases); a wall hit pushes the ship away and turns
  the nose only to run along the rock (0.2.4 mirrored it off, a spin). `ShipController.Moved`
  sums the rotation by source for the checks. Settings has at most 12 rows: the HOTAS
  rows moved to Settings > Controls and HOTAS (with the Axis display).
- **Radar and route trail (Fix 2.6):** the radar zooms by itself (1 / 2 / 3 / 6 / 9 km at the rim), range rings, every blip
  with its distance, NEAREST on the nearest pilot (`UI/Radar.cs`). Route trail (`Campaign/RouteTrail.cs`): A* on canyon openness
  (30 m grid, off the main thread), chevrons every 100 m (fade 300-700 m ahead) at 35 m over the floor (raised where the canyon is
  under ~70 m wide), the route on the radar; target cycling `RouteTarget` (gamepad D-pad right, keyboard T, HOTAS button 11);
  Settings > Route trail (On).
- **Aliens (Round 3):** `Campaign/Aliens.cs` (types, seeded assignment, journal hints), `DownedPilot.cs` (tells), `Core/GameShell.Aliens.cs`
  (exhaust reveal + bounty, canopy climb / break-in, the horror sequence, the Sleeper, security at the dock, Field Journal page),
  `Campaign/Creature.cs`, `Campaign/Cabin.cs` (cabin, hatch, lamps, passengers, cabin camera: `CabinCam` gamepad D-pad up, keyboard N,
  HOTAS button 12 toggles the HUD picture-in-picture). Pilots wait at the airlock ahead of the left wing (ship space x -2.45, z +0.8);
  the airlock camera looks at them from ahead of the wing (42°). Round 3 tuning: `alienShareStart` .20, `alienSharePerTier` .05,
  `alienShareMax` .35, `tellReliability` .75, `falseTellChance` 0, `alienBounty` 200, `canopyClimbAfter` 25, `breakInAfter` 10,
  `sleeperTurnMin/Max` 40 / 120, `sleeperPilotsWin` 2. Career save adds `journal`, `stats.aliensKilled`, planets' `alien` / `quirk`.
- **Planets (Fix 3.2):** the home system has as many planets as you want. Hangar: **Jump to a new planet** (free, tuning `jumpCost` 0;
  the next one from the career's seed + `planetsMade`, `Career.NextPlanetSeed`, seeds 1 - 19 999 999, never a date) and **Planets** (today's
  "planet of the day" first, then every planet visited, newest first; pick one and the mothership jumps there, doors shut, no cut).
  Pilots on today's planet pay +`dayBonus` 50 %. A cleared planet makes the jump the hangar's default row. Continue arrives at a new
  day's planet of the day (once), else where you left off (`currentSeed`). Code: `Core/GameShell.Planets.cs`, `GameShell.Campaign`
  (`Arrive`, `Jump`), `Core/Career.cs`. Hangar rows: Launch, Jump, Planets, Repair, Refuel, Recover, Field Journal, Back to title
  (checks pick rows by label: `ExeCheck.Row`).
- **Terrain (Fix 2.4):** `CanyonField`: three canyon systems at closer spacing, floors
  about 50 m wide, walls 300-600 m in five ledges; landing pads (one per 520 m cell where
  it falls on a canyon floor, 45 m flat clearing): pilots, the start and Reset use them.
- **Exe self-check** (plays from the title through the input actions with a
  virtual gamepad; screenshots, fps, views, pause / settings, typed seed, every
  crash level, determinism, a height map, sound level, world-space menu):
  `Builds\HostileHorizon\HostileHorizon.exe -exeCheck <dir> [-exeCheckOnly perf|campaign|home] [-steamDeck 1] -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -logFile <dir>\Player.log`
  writes `<dir>/exe_check.txt` and pictures, then quits (settings are defaults,
  never saved). `Checks/ExeCheck.cs`. `-exeCheckOnly campaign` (Round 2,
  `Checks/ExeCheck.Campaign.cs`, about 15 min) plays a whole career: new career,
  launch, radar, landing, pickup, docking, exhaust kill, out of energy + tow,
  crash with a pilot aboard, regeneration, loaner, recovery, quit + Continue, and
  logs the camera's top speed in every live sequence (a cut shows as a jump).
  `-exeCheckOnly fix24` (Fix 2.4, `Checks/ExeCheck.Fix24.cs`, about 12 min): flight
  response numbers with 0.2.3's handling and each Flight assist, brake from top speed
  and descent from 1000 m (0.2.3's and new) against the predicted stop point, pause
  (Start, View tap, simulated focus loss, during launch / docking / recall / tow),
  Reset position, the stop ring / cue / landing states at a pilot, new-canyon pictures
  and fps, the height map (white = 750 m).
  `-exeCheckOnly fix25` (Fix 2.5, `Checks/ExeCheck.Fix25.cs`, about 25 min): the
  uncommanded-movement list (simple stick inputs per Flight assist, hover, wall hits, what
  turned the ship by source), three pickups played as a player (brake at the cue, set
  straight down, engines off, the pilot's run, the knock, B), belly / airlock camera,
  glass floor and landing view pictures, the launch warning, lift and docking times (new
  and with 0.2.4's numbers).
  `-exeCheckOnly fix26` (Fix 2.6, `Checks/ExeCheck.Fix26.cs`, about 8 min): radar labels / zoom, the route trail from launch and from
  low down (path report, the check's pilot following it, chevrons in view, wall touches), re-plan after leaving it, target cycling, fps
  with the trail on and off. `-exeCheckOnly aliens` (Round 3, `Checks/ExeCheck.Aliens.cs`, about 35 min): same seed = same aliens,
  first planet clean, each outside type with its tell shown / hidden (airlock-camera pictures), engines kills (bounty, journal), a real
  pilot killed, canopy climb + thrown off + break-in, the horror sequence (pictures at its beats, camera continuity, looking round),
  the Sleeper three ways, the Field Journal, the sounds that would have played. `-exeCheckOnly fix31` (Fix 3.1, about 15 min): the aliens
  check without the "tell hidden" visits, plus a model report (triangles, Mesh LOD levels, clips), close-ups of each creature and both
  visors (a check-only camera), where the horror creature is at each beat, frame rates with the models and the code-made figures;
  `fix31h` = the airlock horror only; `fix31perf` = the Field Journal with all five + the frame rates.
  `-exeCheckOnly fix32` (Fix 3.2, `Checks/ExeCheck.Fix32.cs`, about 5 min): clear today's planet, the day bonus, jump, a pilot on the
  new planet, the Planets list, back and forth (waiting pilots kept), quit + Continue, the same system seed's second planet twice.
  `-exeCheckOnly home` (Fix 2.3, `Checks/ExeCheck.Home.cs`, about 9 min): pilot
  fuel, the DOCK readout on the climb, RETURN NOW / CAN'T REACH, the mid-air beam
  catch, the recall from the air and the ground. A
  check keeps running unfocused (`runInBackground`, Input System `IgnoreFocus`);
  a player's build pauses as before. Frame rates from a batch-mode check are
  measured rendering into the off-screen view (no present to a window). Round 2 launchers:
  `local/round2/build.ps1 -Name <log>`; **start every check with**
  `python Tools/run_check.py <dir> [--only all|perf|campaign|home|fix24|fix25|fix26|aliens|fix31|fix31h|fix31perf|fix32|mute|screen] [--extra "-steamDeck 1"] [--size 1280x800]`
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
- **Creature model (Round 3):** `F:lenderlender.exe --background --factory-startup --python Tools/Blender/creature.py`
  writes `SourceArt/Blender/Creature.blend` and `Assets/Resources/Models/Creature.fbx` (one group per body segment; the joints are
  mirrored in `Assets/Scripts/Campaign/Creature.cs`, which poses it in code). `ship.py` (Round 3) cuts the cockpit hatch (bulkhead,
  left of the seat); the cabin behind it is built in code (`Campaign/Cabin.cs`).
- **Characters (Fix 3.1) — Dan's art pipeline (standing):** Code writes image prompts (`Docs/MODELS-NEEDED.md`) -> Dan has the
  concepts drawn (`SourceArt/Concepts/<name>.png`) -> `python Tools/meshy_generate.py [--only a,b] [--force]` (Meshy image-to-3D; key from
  `MESHY_API_KEY`, process or Windows user environment, never printed or written) into `SourceArt/Meshy/<name>/` -> `blender --background
  --factory-startup --python Tools/Blender/meshy_cleanup.py -- <names>` (metres, feet at 0, renders, `<name>_mixamo.fbx`) -> Dan rigs in
  Mixamo and downloads the listed clips into `SourceArt/Mixamo/<name>/` (rigged model = `idle.fbx` With Skin; shared creature clips in
  `creature/`, each once) -> `python Tools/import_characters.py` -> `Assets/Resources/Characters/` (models, textures, `Clips/<set>_<clip>.fbx`,
  `Journal/` pictures). `Assets/Editor/CharacterImport.cs`: Humanoid, loops, Unity Mesh LOD (4 levels), the `.mat` assets (made by
  PlayerBuild). Runtime `Campaign/CharacterRig.cs` (clips by name: own set, then `creature`, then any). Models: pilot, mimic, hollow,
  knocker, stalker, sleeper; `pilot_hollow` = the Hollow's dead-visor disguise. Code-made figures are the fallback if a model is missing.
  Characters always animate (culled animators left the creature undrawn at the eye).
- **Sounds (Round 3):** `python Tools/make_sounds.py [--only name,name] [--force]` makes the scare sounds with ElevenLabs (key from
  `ELEVENLABS_API_KEY`, never printed) into `Assets/Resources/Sounds/<name>.mp3`; prompts in `Docs/SOUNDS.md`. `Sfx.Clip(name)` loads
  them (a code placeholder if one is missing). Making a sound plays nothing.
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
**Fix 3.2 (always another planet to fly: Jump to a new planet from the hangar, the Planets list, planet of the day +50 %, a cleared
planet offers the jump first) is built, checked in the exe (headless, muted, `--only fix32`) and published as 0.3.2 (`game-302`,
Latest); a starter-200 install updates to it from GitHub. Not yet played by Dan.** Details in `TODO.md` (Fix 3.2 delivery).
**Next: Round 4 (Enemies + difficulty)**, to be written by the planning chat.
**Fix 3.1 (real character models: the pilot and the five aliens' own creatures from Dan's concept art -> Meshy -> Mixamo,
animated; the Field Journal shows each type) is built, checked in the exe (headless, muted, `--only fix31`, `fix31perf` at both
settings) and published as 0.3.1 (`game-301`, Latest); a starter-200 install updates to it from GitHub. Not yet played by Dan.** The
tells still work; landed with the cabin full: 423 fps (old figures 434), Deck setting 456. Substitute clips (knock = "Opening", no bite /
pound / Sleeper lunge) and what Dan should look at are in `TODO.md` (Fix 3.1 delivery).
**Fix 2.6 + Round 3 (radar distances, route trail; aliens: five types with tells, canopy break-in, the horror sequence, the
Sleeper, cabin camera, Field Journal, ElevenLabs sounds) is built, checked in the exe (headless, muted, `--only fix26` and
`--only aliens`) and published as 0.3.0 (`game-300`); a starter-200 install updates to it from GitHub. Not yet played
or heard by Dan.** Open for Dan: the check's pilot still touched walls following the trail (2 in the last run; tuning limit
reached), the Mimic's shadow tell can't show on pads in shade, frame rates weren't measurable (other projects' builds loaded
the PC). Numbers in `TODO.md` (Fix 2.6 + Round 3 delivery).
**Fix 2.5 (tight control, belly camera / glass floor / landing view, the pickup fixed,
launch warning, quicker lift, hold-A fast-forward) is built, checked in the exe (headless,
muted, `--only fix25`) and published as 0.2.5 (`game-205`, Latest); a starter-200 install
updates to it from GitHub. Not yet played by Dan.** Numbers and the uncommanded-movement /
pickup findings in `TODO.md` (Fix 2.5 delivery). On Light the ship now holds its attitude with
the stick centred: bank and pull to turn.
**Fix 2.4 (flying feel, landing, pause / reset, deeper canyons) is built, checked in
the exe (headless, muted, `--only fix24` twice, `--only all`, Deck-setting perf) and
published as 0.2.4 (`game-204`, Latest); a starter-200 install updates to it from
GitHub. Not yet played by Dan.** Quicker turning with Flight assist Full / Light
(default) / Off and Stick sensitivity; air brakes (450 km/h to hover speed in 2.5 s)
with a stop-point ring and a SLOW DOWN cue near pilots; hover descent 30 m/s high up,
3 m/s at touchdown; pause anywhere (Start / Esc / a tap of View / HOTAS 9 or 10,
auto-pause on focus loss) with Reset position; deeper (to about 600 m), tighter
canyons with landing pads. Numbers in `TODO.md` (Fix 2.4 delivery).
**Fix 2.3 (getting home) is built, checked in the exe (headless, muted, three runs
of `--only home`) and published as 0.2.3 (`game-203`, Latest); a starter-200
install updates to it from GitHub. Not yet played by Dan.** It adds the way home
on the displays (HOME bearing / km and DOCK ↑ metres, mothership always on the
radar, a sky marker), RETURN NOW / CAN'T REACH warnings with their own sound, the
beam catching a ship that runs dry in the air, the recall (hold View / H / HOTAS
7, 900 cr) and pilots bringing 3-15 % energy (random, not seeded). Fix 2.1 / 2.2
(full screen: the display's own size at most 1080 tall; 1920 x 1080 on Dan's 4K,
1280 x 800 on the Deck) published as 0.2.1 / 0.2.2; not yet seen by Dan on the
Deck. Round 2 (Campaign core; see `TODO.md`) is published as 0.2.0
(`build-200`) with the starter package (`starter-200`); not yet played by Dan.
Round 1 (Free Flight) was played
by Dan on PC ("ran well"); HOTAS and Steam Deck still for him to check. The
Campaign: an ongoing career saved in `career.json` (Continue / New career), a home
system and today's planet named from the seed of the day, seeded downed pilots
with wrecks and beacons, a radar (cockpit + HUD), helicopter-style landing mode
with gear, engines on / off, the pickup (run, knock, airlock), exhaust kills,
energy, the mothership tow and recall, the Blender mothership with live launch and live
docking, credits (pay, clear bonus, fuel, repair, tow, recall, recovery), the hangar
page, crash explosions with the burning wreck kept (all modes, no fade), death ->
a live camera flight to the regeneration pod -> recovery or the loaner. Numbers in
`Assets/Resources/Tuning/CampaignTuning.json`. Test runs follow Dan's rules
(muted, no window): `Core/TestRun.cs`, `Tools/run_check.py`. About 370-440 fps at
1080p on the 1660 Ti. Code layout: `Assets/Scripts/` Core (GameShell +
GameShell.Campaign, settings, screen setting, career, tuning, names, seeds,
TestRun), Terrain, Flight (ShipController, ShipExtras), Campaign (Mothership,
DownedPilot, PlanetSites), Effects (Explosion), Cameras, Input, UI (incl. Radar),
Audio (incl. Sfx), Checks. Design decisions (credits, ongoing campaign,
planets, the five alien types) are at the top of `TODO.md`.
