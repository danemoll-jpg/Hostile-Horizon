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
  7. **Stereo shader rule (Fix 8.1).** VR renders single-pass instanced: every shader of the game (and every new one) must be
     single-pass-instanced safe: `UNITY_VERTEX_INPUT_INSTANCE_ID` in the vertex input, `UNITY_VERTEX_OUTPUT_STEREO` in the output,
     `UNITY_SETUP_INSTANCE_ID` + `UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO` in the vertex function, `UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX`
     in the fragment function, in every pass that draws to the camera (forward, depth); screen / depth sampling stereo-aware. Without it the
     right eye shows the clear colour (0.8.0). **VR checks compare both real eyes** (`-exeCheckOnly fix81 -vr`: the eye texture's two
     slices, pixel-compared), never a flat camera per eye.
- **Input devices:** Thrustmaster T.Flight HOTAS 4 (stick: pitch/roll, twist:
  yaw, detachable throttle lever: absolute throttle, hat: look / camera,
  buttons), any gamepad (Xbox layout, Steam Deck built-in controls), keyboard +
  mouse as a fallback. All through one Input Actions asset with a binding set
  per device. HOTAS axes need deadzones and a calibration/invert option.
  **HOTAS buttons are named by the symbol printed on them** (Dan's is the PS4
  T.Flight HOTAS 4: R2 trigger, R1, R3, L1-L3, □ ✕ ○ △, SHARE, OPTIONS), never
  "button 6"; prompts name only the buttons of the device in use (Fix 6.1).
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
  (Select; the Deck's View button), keyboard **H**, HOTAS **hold SHARE** (Fix 6.1; was button 7). The
  mothership comes and its beam lifts you to the dock (in the air or landed),
  `recallFee` at the dock; refused while a pilot is running / knocking / boarding.
  At 0 energy in the air the same beam catches the ship (tow fee). Code:
  `GameShell.Campaign` (`calling`, `BeamLift`, `HomeWarnings`, `SkyMarker`).
- **Pause / reset (Fix 2.4):** pause anywhere in play, including launch, docking,
  tow, recall and the beam (time stops; resume carries on): gamepad Start or a
  **tap** of View (held 1 s it is still the recall), keyboard Esc, HOTAS OPTIONS
  (Fix 6.1; any button in Settings > Controls > HOTAS setup). A
  player's build auto-pauses on focus loss (`OnApplicationFocus` / `OnApplicationPause`;
  check runs don't). "≡ / Esc: Pause" shows on the HUD and the cockpit display for the
  first minute of a flight. Pause menu: Resume, **Reset position** (hover 60 m over
  the nearest landing pad, same cabin / energy, free; greyed out while a pilot runs /
  knocks / boards and during the automatic parts), Free Flight: Restart; Campaign:
  Recall (900 cr); Settings, quit. Code: `GameShell.Pause / Resume / ResetPosition`.
- **Landing (Fix 2.4):** X / G / HOTAS □ from flight = air brakes (top speed to hover
  speed in 2.5 s, stopped in about 4 s, brake sound), BRAKING / HOVER (height in big
  type) / TOUCHDOWN / LANDED states on the HUD and the cockpit display; near a waiting
  pilot (2.5 km) an amber ring + post on the ground marks where the ship would stop
  (`ShipController.StopPoint`), and "SLOW DOWN — PRESS X" on the radar when braking
  now stops you near the pilot. Hover descent: 30 m/s full down above 150 m, 14 at
  60 m, 3 m/s (a safe touchdown) below 8 m.
  Fix 2.5: the cue and the amber ring aim at the waiting pilot's pad (green ring within
  900 m), the amber ring also shows in hover. **Belly camera** (`Cameras/BellyCam.cs`):
  on by itself in landing mode, D-pad down / V / HOTAS L1 toggles it; on the cockpit's
  centre display and as a HUD picture-in-picture; landed with a pilot coming it is the
  airlock camera. **Glass floor** at the feet and glass in the lower canopy (front of the
  side consoles) in `ship.py`. **Landing view** (5th in the camera cycle): high over the ship.
- **Pickup (Fix 2.5):** a pilot comes when landed with engines off within `pickupRange`
  160 m level and `pilotMaxClimb` 30 m of height (else the advice line says they can't
  reach you); they run over the ground round the hull to the door (left side). At the
  door: "PILOT AT AIRLOCK — press B to open" (the button for the device last used:
  `Controls.LastDevice`, `AirlockKey`), the lit AIRLOCK button over the centre display,
  KNOCK KNOCK KNOCK on the displays with each knock (a brighter knock sound).
- **Fast-forward (Fix 2.5, Dan's ask):** hold A / Space / HOTAS R1, the trigger (the UI Submit action)
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
  rows moved to Settings > Controls > HOTAS setup (Fix 6.1; with the live axis display).
- **Radar and route trail (Fix 2.6):** the radar zooms by itself (1 / 2 / 3 / 6 / 9 km at the rim), range rings, every blip
  with its distance, NEAREST on the nearest pilot (`UI/Radar.cs`). Route trail (`Campaign/RouteTrail.cs`): A* on canyon openness
  (30 m grid, off the main thread), chevrons every 100 m (fade 300-700 m ahead) at 35 m over the floor (raised where the canyon is
  under ~70 m wide), the route on the radar; target cycling `RouteTarget` (gamepad D-pad right, keyboard T, HOTAS L2);
  Settings > Route trail (On).
- **Aliens (Round 3):** `Campaign/Aliens.cs` (types, seeded assignment, journal hints), `DownedPilot.cs` (tells), `Core/GameShell.Aliens.cs`
  (exhaust reveal + bounty, canopy climb / break-in, the horror sequence, the Sleeper, security at the dock, Field Journal page),
  `Campaign/Creature.cs`, `Campaign/Cabin.cs` (cabin, hatch, lamps, passengers, cabin camera: `CabinCam` gamepad D-pad up, keyboard N,
  HOTAS L3 toggles the HUD picture-in-picture). Pilots wait at the airlock ahead of the left wing (ship space x -2.45, z +0.8);
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
- **Biomes (Round 4):** every planet is one biome from its seed (`Biomes.ForSeed`; a career's first planet is Canyons; saved as
  `PlanetState.biome`): **Canyons**, **Giant Forest**, **Dead City**, **Ice** — all in `Terrain/CanyonField.cs` (one height function per
  biome; `CanyonT` = openness for the route trail). Forest trees one per 64 m cell (`TreeAt`), city street grid in the height field
  (rooftop pads: `Pad.roof`, `PadSize` 26 m), ice crevasses; solid props (trunks, sky bridges, snow bridges) collide from the seed
  (`SolidPush`, used by `ShipController.Collide`), the forest crown is soft (`InCanopy`: leaves + rustle). Drawn by `Terrain/BiomeProps.cs`
  (GPU instancing, `Resources/Materials/Prop.mat` made by PlayerBuild, 2 LODs; Deck: trees to 60 % of the view, detailed to 420 m).
  Look per biome: `WorldLook.LookFor / ApplyBiome` (+ `Rock.shader` `_Mode`, `Sky.shader` `_Overcast`, `Prop.shader`, `Shaft.shader`);
  air and blizzards: `Effects/BiomeWeather.cs`. Free Flight: **Planet type** row (By seed / one biome; `GameSettings.freeBiome`); its rows
  are Seed, Seed number, Planet type, Crash level, Start flight, Back. Round 7 added three more (below).
- **Music (Round 4):** `Audio/Music.cs` (state -> track, 4 s crossfades, tension layer, silence near pilots and in the scares, ducks
  under voices, stingers). Files: **`BundleMusic/`** (git-ignored; Dan's Suno tracks `title_theme hangar planet_select flight_canyons
  flight_forest flight_city flight_ice`, + Dan's `tension_low_energy` (120 s, loops 65.7-107.3 s) and `regeneration` (30 s one-shot), Fix 4.1) -> `python Tools/music_loops.py` writes the
  loop points `BundleMusic/music.json` -> the build copies the folder to `Builds/HostileHorizon/Music/`; releases ship it as the launcher's
  **soundtrack** component (`Prepare-ComponentUpdate.py soundtrack --source BundleMusic`); the game reads `-racerManagedMusic` first.
  `python Tools/make_music.py [--only] [--force]`: the two music pieces via the ElevenLabs music API (Dan's key
  refuses it, so Dan made them himself; don't overwrite his files) and the stingers `Resources/Sounds/sting_*`. Docs/MUSIC.md.
  A soundtrack update: `Prepare-ComponentUpdate.py soundtrack --source BundleMusic --previous-catalog <latest release's catalog>
  --previous-manifest Builds/LauncherRelease-S<last>/assets/soundtrack-manifest.json --out Builds/LauncherRelease-S<build> ...` (tag
  `soundtrack-<build>`), published before the game update, whose `--previous-catalog` is then the soundtrack release's catalog.
- **Voices (Round 4):** `Audio/Voices.cs` (queue with priorities, radio filter + clicks + crackle, door voice through the hull, cabin,
  subtitles), lines `Assets/Resources/Voices/<id>.mp3` + `lines.json` from `python Tools/make_voices.py [--only id|prefix_] [--force]`
  (ElevenLabs TTS, key never printed); Docs/VOICES.md lists every line. Control = Daniel, computer = River, 12 pilot voices (callsign =
  voice). Wired in `Core/GameShell.Audio.cs` (also the music state and the biome world). Mimic voice sign: `GameShell.MimicVoiceSign`.
  Settings > **Sound**: Master, Music (85 %), Effects, **Engine** (engine hum + wind, Fix 6.3), Voices, **Test voices** (Control, the
  computer, a pilot; also from the pause menu), Subtitles (On). Fix 6.3: every line ducks engine + wind (`voiceDuckEngine` .6), music
  (to x`voiceDuckMusic` .6) and effects (`voiceDuckEffects` .25) (`Voices.DuckAmount`); the computer plays at 100 % of Voices with a chime
  before its 10 warnings (`Voices.Warnings`); its 18 lines are EQ'd, compressed and levelled to -16.5 LUFS by
  `python Tools/process_computer_voice.py` (originals kept in `SourceArt/Voices/computer_raw/`: after `make_voices.py --force --only cpu_`
  delete those and run it again). Mix tuning `engineMix` .55, `windMix` .5 (0.6.2: 1 / 1): music +4.6 dB over engine + wind at cruise.
- **Self-righting (Round 5 Part A):** Flight assist Full / Light with Settings > **Auto-level** On (default): pitch and roll centred
  (any stick output past the deadzones counts as steering) for `rightDelay` .25 s, the ship rolls wings-level the short way (from upside
  down it keeps rolling the way it started) and eases its nose to the horizon (6° climb below 25 m), heading kept; the first stick input
  stops it. Rates: roll 120 / pitch 60 °/s at most, ease-out 3/s; Settings > **Righting speed** Slow x.65 / Normal / Fast x1.5; Full x1.3.
  **Level-out button** (gamepad left-stick click, keyboard L, HOTAS L1 by default: Settings > Controls > HOTAS setup): x2, overrides pitch / roll until level (also on Flight assist Off). Landing mode rights the body at most `hoverRightRate`
  140 °/s. `ShipController.RightStep`; `Moved` sources 5 (self-righting) and 6 (level-out). Graphics quality and Steam Deck moved to
  Settings > Display (12 rows at most). Check `-exeCheckOnly round5a` (`Checks/ExeCheck.Round5A.cs`, about 8 min).
- **Enemies and difficulty (Round 5 Part B):** Campaign only. `Campaign/Enemies.cs` (placement from the seed + difficulty, turrets, saucers,
  pooled bolts, `ShipController.ExtraSolid` for turrets / stumps), `Core/GameShell.Enemies.cs` (gun, shield, hits, warnings, bounties, the
  difficulty page), `Effects/Fx.cs` (pooled sparks / blasts), `Core/Difficulty.cs` (`DifficultySetting` per career: `Career.difficulty`;
  a new career copies `GameSettings.difficulty`; the crash level is part of it now, `campaignCrashLevel` unused). Models
  `Tools/Blender/enemies.py` -> `Models/Turret.fbx`, `Models/Saucer.fbx` (parts built round their pivots, turned half a turn on load).
  Turrets on the high ground (forest: on giant stumps above the canopy), track / lock / fire only with line of sight (`Enemies.Sees`: the
  ground, and tree crowns in the forest); saucers from a career's second planet on (patrol, chase on sight or when you land / hover within
  3 km, firing passes, kept above the rock and the forest). On Easy / Normal no turret sees a pilot's pad; Hard / Nightmare cover a share
  (`coveredPadShare`). Destroyed enemies stay destroyed per planet (`PlanetState.enemiesDown`). **Controls:** gun = held A / Space / left
  mouse / HOTAS R1, the trigger (A still starts / stops the engines on the ground; Fix 6.3: the gun works in Free Flight too); shield toggle =
  D-pad left / Z / HOTAS R3; level out = left-stick click / L / HOTAS L1 (Fix 6.1 layout, Fix 6.3 names). Difficulty: Campaign page "Difficulty (new career)" + "Difficulty options"; hangar "Difficulty: X". Presets
  Easy / Normal / Hard / Nightmare / Custom; all numbers in the tuning file (tables `aggressionLevels`, `alienShareLevels`, `tellLevels`,
  `falseTellLevels`, `payLevels`, `coveredPadShare`, `preset*`; enemy, gun, shield and bounty numbers). Sounds `make_sounds.py`
  (gun_fire, turret_fire, saucer_fire, bolt_hit_*, shield_*, enemy_explosion, saucer_hum, lock_warning); voices `cpu_locked_on`,
  `cpu_saucer`, `cpu_shield_up/down`, `cpu_target_down`, `ctl_hot`, `ctl_hot_saucers`. Check `-exeCheckOnly round5` (`Checks/ExeCheck.Round5.cs`,
  about 20 min; `round5perf` = the per-biome fight frame rates only).
- **Round 6:** (A) **jump scare** (`Core/GameShell.Strike.cs`): an alien let in boards exactly like a pilot (message, chime, seat, music,
  control), then strikes, seeded per alien: instant (`strikeInstant*`, after the airlock shuts), on take-off (`strikeTakeoff*`, or a turn over
  `strikeHardTurn`), slow burn (`strikeSlow*`, the old long sequence); music cut / sting on the strike, INTRUDER after it; it strikes before
  docking / the beam. Canopy leap (`canopyLeap*`), red herrings for every boarding (`herring*`). `Core/Trace.cs` = the check-only event
  log for the alien-vs-pilot comparison. (B) enemies on the radar unspotted within `radarTurretRange` / `radarSaucerRange` (labelled with
  distance), world brackets within `bracketRange` (`UI/TargetBrackets.cs`), blinking warning lights, muzzle flashes, bolt trails
  (`beaconScale`, `flashScale`, `boltGlowScale`); accuracy / fire rate / aggression unchanged ("more of a threat" DEFERRED by Dan). (C) the
  **upgrade shop** (hangar > Upgrades; `Core/Upgrades.cs`, `GameShell.Upgrades.cs`): Engine, Hull, Shields, Energy, Radar, Cabin (6 -> 8 -> 10),
  Gun; prices `upg*` arrays, effects `upg*Step`; levels saved as keys in `ShipState.upgrades` (the loaner has none); ship value = 6000 + the
  prices paid. (D) the **star map** (hangar "Star map · Planets", `GameShell.Planets.cs`; drawn in 3D by `UI/ViewStage.cs`, a stage at y
  -30 000 on layer 30 shown as `MenuPanel.Page.View`), coordinates (typed seed), distress calls on cleared planets (`distress*`,
  `Career.CheckDistress`, real time; `Career.CheckHoursAhead` for checks; Control's `ctl_distress` at the dock). (E) the **Field Journal
  viewer** (`GameShell.Journal.cs`): a page per kind, the 3D model turned (Look / right-mouse drag / hat) and zoomed (ThrottleChange /
  wheel), animations, lore, record (`JournalEntry.killed / killedYou / firstPlanet`); silhouette for unmet kinds; from the hangar and the
  pause menu. Hangar rows: Launch, Jump, Star map, Upgrades, Repair, Refuel, Recover, Field Journal, Difficulty, Back. Check
  `-exeCheckOnly round6` (or `round6a` .. `round6e`; `Checks/ExeCheck.Round6.cs`, `ExeCheck.Round6CDE.cs`; about 45 min in all).
- **Round 7 Part A:** **music** (`GameShell.Audio.cs` `MusicTick` / `PickupQuiet`): the flight track drops out only for the pickup (a pilot
  running / knocking / boarding, or landed / hovering within `pickupQuietRange` 250 m of the route's pilot, else the nearest waiting one);
  0.6.3's rule (any pilot on foot within 700 m with the ship under 250 m up, or landed) is kept as `OldMusicQuiet` for the check only.
  **`[Play]` log** (`Core/PlayLog.cs`): game events in `Player.log` in players' builds (`[Play] <time> ...`, one line each, no stack
  trace): arrival (planet, seed, biome, aliens waiting), launch, pilots found / running / at the airlock / boarded / killed, every alien
  event (`AlienEvent`), every music change with its reason, every computer / Control line, hull heat, shard breaks, docking (the debrief),
  deaths. **Creatures up close** (`GameShell.Aliens.cs`, `Strike.cs`, `Creature.cs`): the eye is the ship's eye point
  (`ShipLayout.Eye`, rigid with the ship; the camera is a frame behind during coroutines); the strike's face stops `StrikeFace` .65 m from
  the eye, no part nearer than `StrikeGap` .42 m (`KeepClear`, from the posed mesh: `Creature.Surface` bakes it); the canopy stays at 0.6.3's
  place (two restaging tries failed, tuning limit; `CanopyStaging` / `NewCanopyFace` / `KeepOutOfCanopy` hold try 2, off; the bubble
  `CanopyC` / `CanopyR` = ship.py's); up close full detail (`Renderer.forceMeshLod` 0) and a cold face light; creature materials two-sided (`_Cull` 0, made by
  `CharacterImport.MakeMaterials`). `CheckOldStaging` = 0.6.3's staging for "before" pictures. **Strike** (`Strike.cs`): Instant / On
  take-off only (`strikeInstantWeight` .55, `strikeTakeoffWeight` .45; the slow burn is gone; the Sleeper alone with you bursts too):
  nothing changes until the creature is in view (`InView`, at most .6 s), then the burst / lunge sounds, sting, music cut, controls taken;
  .35 s later lights / displays / alarm / INTRUDER. Check `-exeCheckOnly round7a` (= `round7music` + `round7aliens`, `Checks/ExeCheck.Round7.cs`).
- **Biomes (Round 7 Part B):** **Volcanic**, **Crystal Forest**, **Ocean Islands** (`Biome` 4-6, keys `volcanic crystal ocean`; `ForSeed` now
  picks from all seven; saved planets keep theirs). `CanyonField`: `VolcanicHeight` (the canyons in basalt, floors under the lava level 0 m
  in rivers / lakes, vents `VentAt` per 256 m cell), `CrystalHeight` + `CrystalAt` (a spire per 64 m cell, 10-200 m, tilted, solid via
  `SolidPush`) + `ShardsAt` (thin shards: `ShardHit` breaks them, no hit; `Broken` per visit), `OceanHeight` (`IslandM` islands with beaches
  and cliffs, sea floor -38 m, `StackAt` sea stacks, `ArchAt` arches = two stacks + a slab `Box` kind 2). **Liquid** (`Liquid`, `HasLiquid`,
  `IsLava`, `OverLiquid`, `GroundHeight`): `SurfaceHeight` returns the liquid's surface where it is higher, so flying, landing, the trail,
  pilots and enemies all treat it as the surface. Pads: volcanic shelves raised to 9 m over the lava; ocean pads on islands (the flattest
  spot of the cell). Never a pilot / turret on the liquid (`PlanetSites`, `Enemies.Spaced`). **Ship** (`ShipController`): lava = a crash at
  every crash level but Ghost (`CrashCause` "lava"); the sea: a skim (spray `OnWater`, no landing on water) or, more than `waterHardSpeed`
  12 m/s down, a hit x `waterHitScale` 3 as on rock; a shard: `OnShard`. **Look:** `Terrain/Liquid.cs` + `Shaders/Liquid.shader` (one grid
  following the viewer: the sea with waves / Fresnel / glint / whitecaps, or lava with a drifting crust and glowing cracks), `Rock.shader`
  `_Mode` 4 / 5 / 6 (+ `_Hot`: the lava's light on the walls, glowing moss, sand / wet rock / surf at the waterline), `Prop.shader` `_Crystal`
  (glassy, glowing inside, three cold tints), `WorldLook.LookFor` (dark red / pale violet / sea skies), `BiomeProps` (crystals instanced in
  two LODs like the trees, vent glows, arches), `BiomeWeather` (ash + embers, smoke plumes over the 6 nearest vents, heat haze, crystal motes,
  sea mist, spray, shard debris), `Effects/Fliers.cs` (7 harmless gull-like fliers, Ocean only). **Hull heat** (`GameShell.LiquidTick`):
  under `lavaHeatHeight` 30 m over lava: HULL HEAT on the displays + `cpu_hull_heat` (every 20 s), warning only. **Sound / voices:**
  `crystal_shatter`, `water_splash` (`make_sounds.py`); `ctl_brief_<biome>` (arrival), `ctl_launch_<biome>` (Control names the world at
  launch), `cpu_hull_heat`, `p<n>_mayday_<biome>`; music `flight_volcanic / crystal / ocean` (Dan's Suno tracks, soundtrack 700). Free
  Flight's Planet type lists all seven. Check `-exeCheckOnly round7b` / `round7bperf` (`Checks/ExeCheck.Round7B.cs`).
- **Round 8:** (A) the strike: `GameShell.StageAtFace` (`StrikeStaging` 2; 0 = Round 7's for checks): face `StrikeFaceNear` .45 m out, body down past
  the left shoulder (canopy break-in: up over the dash), neck / head turned after the animation (`Creature.LookAt`, `FaceAt`, in the
  parent's space); the flat HUD hides at the strike. Check `-exeCheckOnly round8a` (pixel maps of what hides the creature: `_covered.png`).
  (B1) **Hard-surface models (Dan's art pipeline, no Mixamo):** concepts `SourceArt/Concepts/<name>.png` -> `meshy_generate.py --only <names>`
  (hard-surface models only by name) -> `blender ... --python Tools/Blender/hard_cleanup.py -- <names>` (size, turn `YAW`, renders) ->
  `python Tools/import_hard.py` (textures to `Resources/Hard/`; materials by `Editor/HardImport.cs` at build; slot `mx_<name>`; ship and
  mothership two-sided) -> `ship.py` / `mothership.py` / `enemies.py` (helpers `meshykit.py`; look with `render_ship.py -- <dir> [Mothership]`).
  Ship: Meshy hull at 0.8 (10 m long, 9.2 m wings, belly -1.24) fitted round `ShipLayout.Eye` (unchanged), dark canopy faces = glass,
  dash screens at `ShipLayout.ScreenCentre / Left / Right` (side screens scale .00046), new `Contacts`, airlock `ShipExtras.DoorAt`
  (-1.66, .95, .81; the door shows only while open). Turret pivots: head 2.40, gun (0, 1.07, 1.53), muzzle 3.1 m, eye 3.47 m.
  (B2) sounds `make_sounds.py` (engine_idle / engine_cruise loops in `ShipAudio`, at the old hum's level; gear_*, airlock_cycle, dock_clamp,
  jump_drive, explosion, regen_bay, amb_<biome key>) levelled by `python Tools/level_sounds.py` (raws in `SourceArt/Sounds_raw/`);
  `GameShell.AmbienceTick`, tuning `ambienceMix` .5. (B4) credits: People / Made with / Licences. Check `-exeCheckOnly round8b` (models,
  sound mix, credits, turret / saucer pictures; with `-steamDeck 1 --size 1280x800` for the Deck).
  (C) **VR:** OpenXR 1.18.0 + XR Management 4.6.1, set up by `Editor/XrSetup.cs` before each build (Windows, NOT started with the game,
  single-pass instanced). `Core/VrMode.cs`: VR only for `-vr` or Settings > Display > **VR mode** (Off by default), never on the Deck;
  no runtime -> a message and flat. `CameraRig.EnableVr` (TrackedPoseDriver, Recenter: gamepad right-stick click / Home / pause menu;
  cockpit only, no look control), menus on a board (`MenuPanel.PlaceInVr`, 1.4 m), the flat overlay off and a **VR strip** under the
  canopy frame (`FlightDisplays`), **Comfort** vignette / fade for the jump / horizon line all Off (`Effects/Comfort.cs`), VR quality
  preset (`WorldLook`: view 2200, shadow 150 / 1 cascade, MSAA 2, `VrRenderScale` .9). `Play Hostile Horizon (VR).bat`; the launcher
  passes `-vr` / `--vr`. **Check:** `-exeCheckOnly round8c` three ways: flat; `--extra=-vr --env XR_RUNTIME_JSON=C:/nonexistent/x.json`
  (never run `-vr` in a check without `XR_RUNTIME_JSON`: it would open Dan's real runtime); the mock runtime: `--extra=-vr --env
  "XR_RUNTIME_JSON=<repo>\Library\PackageCache\com.unity.xr.openxr@<hash>\Runtime\MockRuntime\unity-mock-runtime.json" --env
  "PATH=<that folder>\windows\x64;..."` (`local/round8_runs2.sh` has the exact line). `run_check.py`: `--env KEY=VALUE`; an `--extra`
  starting with `-` needs `--extra=...`.
- **Fix 8.1:** every shader single-pass-instanced safe (rule 7 above). **Your body** `Flight/PilotBody.cs` (`GameShell.Body`): the Fix 3.1
  pilot posed in code each frame (no head: shrunk at the neck; legs to the floor; two-bone IK hands on the stick / throttle, fingers curl,
  the index pulls while firing). `ship.py`: the Meshy seat mirrored (stick right, throttle left), stick / throttle cut out as groups
  `Stick` / `Throttle` (pivots from their bounds in `PilotBody.Pivot`; the throttle moved 15 cm forward); the stick tilts 18° (pitch /
  roll), twists 15° (yaw), the lever swings ±22°. Floor window: `WIN_C` (0, 3.02), 0.42 x 0.70 m, frame, bolts (slot `chrome`), 30 mm glass
  (slot `window`: `Shaders/Window.shader`), a lining widening to a clean cut in the belly (`WIN_FLARE`). Check `-exeCheckOnly fix81`
  (flat) / `fix81 -vr` (mock runtime: the real eye texture, both slices compared; `local/fix81_runs.sh`); `CameraRig.CheckLook` (checks).
- **Fix 8.2:** `PilotBody` poses every bone from the rest pose each frame, the body stays in the seat (it does not follow the head), the
  drawn stick / lever are smoothed (`Smooth` 18/s); 0.8.1's pose is `CheckOldPose` (checks). Throttle at the stick's depth
  (`ShipLayout.StickPivot / ThrottlePivot`, z 2.535). Docking: `DebriefAtDock` empties the cabin (figures, camera) at the dock
  (`GameShell.CheckOldDock` = 0.8.1's). VR menu board: 1.0 m out (`MenuPanel.VrDistance`, `VrScale` .0005), opened where you look, near-solid
  backdrop, drawn over everything (`Resources/Shaders/UIOnTop.shader`; `CheckVrBoard` 1 / 2 = 0.8.1's board for checks). Field Journal: a
  page-wide left / right (`MenuPanel.Page.Side`) = previous / next kind; HOTAS twist turns the creature. `ship.py`: one material per Meshy
  model (the second console was a second, white slot) and the game fills every material slot; faces wholly on a canopy pane and nearly as
  dark (lum < .13) become glass (the windshield sliver). Check `-exeCheckOnly fix82` (flat) / `fix82 -vr` (mock runtime; `local/fix82_runs.sh`).
- **Round 9:** (A) **Fix 8.3:** `mothership.py` bisects the Meshy hull along the bay's and the floor shaft's planes before deleting
  the faces inside (clean straight cuts), then lines both openings (plated lip from the stern skin to the bay liners at z -26, the shaft
  lining from the belly to under the doors, trim, edge lights). The "grey box" at the ship's rear was the code-built cabin (`Cabin.cs`,
  Round 3's size) poking out of the slimmer Round 8 hull: it is now drawn only for a camera inside the hull (`Cabin.InsideHull`: cockpit,
  VR head, cabin camera; `Cabin.CheckShowOutside` = 0.8.2's for checks). **Steam artwork:** Dan's originals in
  `SourceArt/Title/SteamDeck/` -> `python Tools/make_steam_art.py` -> `SourceArt/Title/Steam/HostileHorizon-{Portrait,Cover,Wide,Hero,Logo,Icon}.png`
  (Cover = the portrait: the launcher's cover; Hero 1920 x 620 with the title painted out; Logo transparent); the build copies them to
  `Builds/HostileHorizon/SteamArt/`; the game (`Core/SteamArt.cs`) copies missing / changed ones up beside the launcher
  (`<launcher>/versions/<id>/` -> `<launcher>/`) at start, so an existing install gets them with a game update (exe checks skip it unless
  `-steamArtRoot <dir>`); the starter package ships them (`Package-LauncherStarter.py --art`). README-LAUNCHER.md: where each goes in Steam.
  (B) **Side missions** (`Campaign/Missions.cs`, `Core/GameShell.Missions.cs`): 1-3 per planet from its seed (`PlanetState.missions`, made on
  the first arrival after Round 9): flight recorder (land at the crash site, engines off, hold the airlock button `missionRecorderHold` 8 s),
  turret nest (3-5 turrets round a valley: `Enemies.Setup(... nest)`, `Enemy.Nest`), timed rescue (`missionTimedSeconds` 540 s of flight time
  on the planet; zero = the pilot lost), clean run (cleared with no hull damage); on the hangar (one line), the star map, the displays
  (`FlightDisplays.Objectives`) and the radar (`RadarContact.mark` / `tag`); paid at the dock (`mission*Reward` x pay); Control `ctl_obj_*`.
  (C) **Recover your own wreck** (`Core/GameShell.Salvage.cs`): the regeneration bay's third row; the wreck (`Career.wreckSeed/X/Z`) is your
  ship's model burnt dark with an orange post, on the radar (WRECK) and the route trail's auto target; land, engines off, hold the airlock
  button `salvageHold` 12 s; back at the next dock with `salvageHullDamage` 40 %; `salvageAmbushChance` .4 that a saucer waits. (D)
  **Insurance**: hangar row, `insuranceShareOfShipValue` 4 % of the ship's value for one flight; an insured wreck's recovery x
  `insuredRecoveryShare` .2 (`Career.wreckInsured`). (E) **Best runs**: `PlanetState.run*` / `best*` (fastest clear in flight time, most
  credits, fewest hull hits, cleared without dying), on the star map (the planet of the day's first), career totals in `CareerStats`.
  (F) **Time trial** (`Core/GameShell.Trial.cs`): Free Flight > Time trial / Ghost (rows after Start flight: older checks count rows); a course
  from the seed and planet type (legs between landing pads ahead, each `RouteTrail.Plan`, `trialLength` 14 km, a gate every 550 m, radius
  24 m, a miss +5 s); splits, `trials.json` beside settings.json (best, splits, ghost at 10 Hz per seed / planet type); the ghost = the ship's
  model in `GlowAdd`. (G) **Harder enemies on Hard / Nightmare only**: tuning tables by aggression level (`turretLeadLevels`,
  `turretInterceptLevels`, `turretAimErrorLevels`, `boltHitRadiusLevels`, `saucerHuntLevels`, `saucerPairLevels`; `Difficulty.TurretLead` ...);
  Low / Normal = Round 5's numbers. Check `-exeCheckOnly round9` (or `round9a`, `round9b`, `round9c` = C+D+E, `round9f`, `round9g`;
  `Checks/ExeCheck.Round9.cs`); `round9a` with `--extra=-steamArtRoot <dir>` checks the art copy.
- **Fix 9.1 (Round 10 Part A):** (A1) aliens rolled **per pilot** from the seed at `Aliens.Share(tier)` = min(`alienShareMax` .20,
  `alienShareStart` .10 + `alienSharePerTier` .025 x tier) x the difficulty level (no minimum of one; some planets have none); 0.9.0
  planets' waiting pilots rolled again once (`PlanetState.alienRoll`). (A2) **Docking in through the stern opening**
  (`GameShell.SternEntry`): round the hull (`RoundTheHull`, `RouteRound`: a lane beside it, string-pulled against `Mothership.HullBox` /
  `Blocked`, ~10 m/s round each corner), lined up 260 m behind the stern, in at ~20 m/s to a walking pace, turning to face out under the
  cradle; lifts (tow / recall / beam) go round to a lane 30 m under the keel, then up through the floor opening; the launch drops
  clear of the keel before moving off. `GameShell.CheckOldDockPath` = 0.9.0's paths (checks); `CheckDock(at, rot, tow)` (checks).
  (A3) `Mothership.Create` loads the Meshy materials (`Hard/<name>.mat`, every slot), the code-built lining / trim / liners take the
  hull's / bay wall's textures; `Mothership.fbx` is readable (the check measures paths against its triangles). (A4) **VR recentre
  in every state:** `CameraRig.PlaceVr` (run by `VrPlacer`, execution order 32000) turns whatever eye pose the code set (cockpit, title,
  hangar, scripted parts) into the tracking space's pose; **hold Pause 1 s in VR = recentre** (`Controls.RecenterHold`; HOTAS OPTIONS /
  the button HOTAS setup gives Pause, pad ☰ Menu, Esc; a tap pauses on release), plus right-stick click / Home / pause-menu row;
  automatic recentre on `trackingOriginUpdated`, tracking back after .25 s, or a head jump > 45° at > 1500°/s. `CameraRig.CheckOldVr` =
  0.9.0's (checks). Check `-exeCheckOnly fix91` (flat: A1-A3) / `fix91 -vr` (mock runtime: A4, the mock head turned through
  `mock_runtime.dll`'s `MockRuntime_SetReferenceSpace` / `SetView`); `bash local/fix91_runs.sh flat|vr <name>`. `run_check.py --exe <path>`
  runs a check on another build (absolute path).
- **Promo video (Round 10 Part B):** (1) **capture** real footage from the built exe, headless and muted, frame by frame at 1/60 s
  (`Checks/ExeCheck.Promo.cs`: `-exeCheckOnly promo:<group>`, `Promo.Active`; higher quality in `WorldLook`, `-promoDay` / `-promoSeed`):
  `python Tools/promo_capture.py free|launch|combat|ending_<type> [--size 3840x2160]` -> `Promo/footage/<group>/<shot>.mp4` + `.events.txt`
  (git-ignored, ~10 GB at 4K; needs a local build with the capture code: the published 0.9.1 was built before it, gameplay identical).
  (2) **narration** by Control: `python Tools/promo_voice.py [--only n10] [--force]` -> `SourceArt/Promo/voice/` (n01-n10; e01-e03 unused).
  (3) **edit** from the edit lists `Promo/edit_trailer.json` / `Promo/edit_vertical.json` (shots, cards, text, narration, music,
  sfx, `game_voice` = the game's own lines with its computer chime / radio filter):
  `python Tools/promo_edit.py Promo/edit_trailer.json [--rough | --4k] [--sheet]` -> `Promo/HostileHorizon_Trailer[_4K].mp4`
  (`--rough`: half size in `Promo/rough/`); same for `edit_vertical.json` (subtitles burned in). -14 LUFS two-pass, a true-peak limiter.
  ffmpeg 9 here: `-/filter_complex <file>`; its `amix` stops when the last-listed input ends, so the tool lists inputs by end time.
- **Controls and the HOTAS (Fix 6.1):** `Input/Controls.cs`: the HOTAS stick has its own `HotasStick` action, calibrated in code
  (`GameSettings.hotasCal` = x min / centre / max, y min / centre / max; centred on the stick's first report and its reach learnt as it
  moves, or Settings > Controls > HOTAS setup > Calibrate stick: Dan's stick reads -0.50 left / right when centred). `Controls.Btn(Act,
  device)` names a button for the device in use (`LastDevice`, switched only by a clear push); pad: Xbox names / colours, ☰ Menu, View;
  HOTAS: printed symbols (`HotasSymbols`, `hotasSymbolButtons` from "Identify your buttons"). HOTAS layout (`HotasActs`, assignable in
  HOTAS setup, `hotasButtons`), by button number: 1 (the trigger, printed **R1**: Fix 6.3, Dan) fire / select / hold fast-forward (Fire and Submit follow R1 as identified: `Controls.FireButton`),
  ✕ engines, ○ airlock / menu back, □ land / brakes, △ view, hold SHARE recall, OPTIONS pause, 3 (**R3**, the back of the stick's head) shield, 2 (**L1**, the blue
  button on the stick head) level out, 10 (R2, assumed; sent nothing in Dan's log) belly camera, 9 (L2) route target, 4 (L3) cabin camera,
  hat look / menus, twist and the rocker yaw (the HOTAS puts both on rz), lever throttle.
  **Throttle lever (Fix 6.2):** Dan's T.Flight HOTAS 4 (PC mode, PID b67c) reports the lever as **z** (0 = fully forward), plus slider,
  dial, rx, ry that never move. Auto starts on z and switches by itself to any other axis moved end to end (never stick / twist), saved as
  `throttleAxisFound`; HOTAS setup > Find throttle lever does the same on request and learns the direction (`leverForwardIsMin`: the lever
  is left fully forward); **no Throttle axis / Invert lever options** (Fix 6.3, Dan; 0.6.2's keys are dropped); a big live Throttle %
  over the setup picture; the ship's throttle = the lever from the first frame of flight (`ShipController.LeverTakesThrottle`; the pad's
  triggers take over while used). **Resets (Fix 6.2):** `GameSettings.ResetHotas / ResetFlight / ResetSound / ResetDisplay / ResetAll`
  behind Yes / No pages (`GameShell.Confirm`); never touch `career.json`. Calibration: asked first, then three steps (let go; each edge with
  four markers; the trigger); ○ / B / Esc cancels. The HOTAS picture: a front view + four close-ups (`ControlsSheet.DrawHotas`, positions in
  `SymbolAt`). Log a real HOTAS's HID axes and buttons: `python Tools/hotas_hid_log.py <seconds> --out <file>` (read-only; no window, no
  sound). Check `-exeCheckOnly fix62` (about 3 min; `fix62pics` with `--size 1280x800`). Menus: `MenuPanel.NavReader` (dominant axis + lock); pages with more rows scroll (`Page.RowsShown`). **Fix 6.3:** on the HOTAS the stick
  only moves up / down in menus (0.6.2's sideways stick on HOTAS setup's rows changed Dan's throttle axis and button jobs); values change with
  the hat's left / right or R1 on a row; button jobs only by "press a button". HOTAS setup is a live **Test view** (every axis and held
  button with its job, held buttons lit). **Record my input (20 s)** (Controls and HOTAS setup; `Input/InputRecorder.cs`): every control of
  every device, Unity's HID descriptor and each change with the game action it feeds, to `hotas_input_log.txt` beside `settings.json`; the
  menu ignores buttons meanwhile. Check `-exeCheckOnly fix63` (about 5 min): a HID joystick built from Dan's descriptor, the cause replayed,
  menus, the trigger in Free Flight / Campaign, the recorder, the mix. Settings > Controls:
  `Core/GameShell.ControlsPages.cs`, picture and list `UI/ControlsSheet.cs` (drawn in code). Check `-exeCheckOnly fix61` (about 1 min;
  `--extra=-hotasTrace <csv>` plays a recorded stick, `Data/screenshots/fix61/dan_updown_trace.csv`), `fix61pics` (pictures; use
  `--size 1280x800`), `input` (a real joystick's layout: a batch-mode run sees none, so log a real HOTAS with Windows' joystick calls).
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
  `-exeCheckOnly round4` (Round 4, `Checks/ExeCheck.Round4.cs`, about 12 min): biomes from the seed, each biome in Free Flight
  (pictures, fps, a trunk / bridge hit, the canopy, a blizzard), the Campaign in Forest / City (a Mimic first, then a rooftop) / Ice
  (trail, landing, pickup, docking), the music / voice / stinger logs; `round4perf` = the Free Flight part only.
  `-exeCheckOnly music` (Fix 4.1, `Checks/ExeCheck.Music.cs`, 1 min): the tension layer's loop and the regeneration one-shot.
  `-exeCheckOnly fix32` (Fix 3.2, `Checks/ExeCheck.Fix32.cs`, about 5 min): clear today's planet, the day bonus, jump, a pilot on the
  new planet, the Planets list, back and forth (waiting pilots kept), quit + Continue, the same system seed's second planet twice.
  `-exeCheckOnly home` (Fix 2.3, `Checks/ExeCheck.Home.cs`, about 9 min): pilot
  fuel, the DOCK readout on the climb, RETURN NOW / CAN'T REACH, the mid-air beam
  catch, the recall from the air and the ground. A
  check keeps running unfocused (`runInBackground`, Input System `IgnoreFocus`);
  a player's build pauses as before. Frame rates from a batch-mode check are
  measured rendering into the off-screen view (no present to a window). Round 2 launchers:
  `local/round2/build.ps1 -Name <log>`; **start every check with**
  `python Tools/run_check.py <dir> [--only all|perf|campaign|home|fix24|fix25|fix26|aliens|fix31|fix31h|fix31perf|fix32|round4|round4perf|music|round5a|round5|round5perf|round6|round6a|round6b|round6c|round6d|round6e|input|fix61|fix61pics|fix62|fix62pics|fix63|round7a|round7music|round7aliens|round7b|round7bperf|round8a|round8b|round8c|fix81|fix82|round9|round9a|round9b|round9c|round9f|round9g|mute|screen] [--extra "-steamDeck 1"] [--size 1280x800]`
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
**Fix 9.1 (Round 10 Part A: aliens rarer, rolled per pilot (Normal 10.8 % at tier 0 .. 20 % at tier 4, was 33 % with one on every
planet); docking round the mothership and in through the stern opening (0.9.0's approach overshot at 200 m/s and went through the hull
and down through the roof: now 0 crossings of the hull from every start); the mothership's Meshy textures (never loaded before); VR
recentre in every state, hold Pause 1 s, automatic after the headset comes back) is built, checked in the exe (headless, muted, `fix91`
flat and on the mock VR runtime) and published as 0.9.1 (`game-901` Latest). Not yet played by Dan.** Details in `TODO.md` (Part A
status). **Round 10 Part B (the promo video) is done:** `Promo/HostileHorizon_Trailer.mp4` (89.4 s, 1080p; `_4K` beside it) and
`Promo/HostileHorizon_Vertical.mp4` (30.1 s, 9:16), real exe footage, Control's narration, the game's own strike as the ending, after
Dan's notes on two rough cuts. **Next:** nothing queued (TODO.md).
**Round 9 (A: the mothership's openings cut cleanly and lined; the grey box at the ship's rear was the code-built cabin poking out of
the Round 8 hull, now drawn only from inside; Dan's Steam artwork in the game, the starter and copied beside the launcher by the game,
the portrait as the launcher's cover. B: side missions (recorder, turret nest, timed rescue, clean run); C: recover your own wreck in
the loaner; D: insurance; E: best runs per planet and career totals; F: the Free Flight time trial with splits, best times and a ghost;
G: tougher enemies on Hard / Nightmare only (Hard 1 in 6.6-7.2 hits, Nightmare 1 in 3.4 at 650 m: tuning limit, for Dan); Easy / Normal
unchanged) is built, checked in the exe (headless, muted, `round9a` .. `round9g`), and published as 0.9.0 (`game-900` Latest) +
`starter-900` (not Latest); an older install updated from GitHub got all six artwork files. Not yet played by Dan.** Details and the
Deck artwork steps in `TODO.md` (Round 9 delivery). **Next: Round 10 (promo video)**, queued in `TODO.md`.
**Fix 8.2 (hands still on the stick and throttle: causes: chest / neck turns piled up every frame and the torso followed the head;
hands now within 0.6 mm of their place on the controls over 60 s, 0.8.1's slid 149-249 mm; the throttle level with the stick; rescued
pilots leave the ship at the dock (the cabin was never redrawn after the debrief); VR menus 1.0 m out, drawn over the cockpit (0.8.1's
board was 98 % hidden looking down), opened where you look; Journal left / right between kinds; the right console textured (a second,
unfilled material slot); the windshield sliver made glass) is built, checked in the exe (headless, muted, `fix82` flat and on the mock
VR runtime) and published as 0.8.2 (`game-802` Latest). Not yet seen by Dan.** Details in `TODO.md` (Fix 8.2 delivery). Round 9 followed (above).
**Fix 8.1 (VR: the right eye showed blue: none of the 9 custom shaders was single-pass-instanced safe; all are now, and the VR check
compares the two real eyes (old shaders: 27 / 32 pairs flagged, now 0 / 32); your pilot's body and gloved hands in the seat, the seat
mirrored so the stick is on the right and the throttle on the left like Dan's HOTAS, both moving with the input; a clean rounded floor
window with frame, bolts and thick glass in place of the ragged hole) is built, checked in the exe (headless, muted, `fix81` flat and on
the mock VR runtime) and published as 0.8.1 (`game-801`). Dan in the Quest: "all looks great" apart from the hands, the menus and the cockpit bits fixed in
Fix 8.2.** Details in `TODO.md` (Fix 8.1 delivery).
**Round 8 (A: the creature at the strike in front of the screens, the HUD off for it (cause: Round 7's staging put its body behind the
screens and inside the dash, 34-59 % hidden; now 0-14 %, residuals for Dan); B: Dan's ship / cockpit / mothership / bay / turret /
saucer via Meshy, 16 ElevenLabs sounds levelled into the Fix 6.3 mix, a credits page, Deck-setting numbers; C: VR through OpenXR,
off unless asked, checked on the mock runtime) is built, checked in the exe (headless, muted: `round8a`, `round8b`, `round8b` Deck,
`round4perf` / `round7bperf` Deck, `round8c` x 3) and published as 0.8.0 (`game-800`; soundtrack stays `soundtrack-700`). Dan in the Quest: scenery only in the
left eye (fixed in Fix 8.1).** Details in `TODO.md` (Round 8 delivery). Fix 8.1 followed (above).
**Round 7 (Part A: the flight music drops out only for the pickup (cause: 0.6.3's rule silenced it for 32-55 % of low flying); aliens
up close solid (cause: the strike put the eye 0.02-0.20 m from the body; now face 0.54-0.77 m, nothing nearer than 0.37-0.48 m; the
canopy kept at 0.6.3's place after two failed restaging tries, for Dan); a `[Play]` event log in `Player.log`; the slow burn removed,
nothing before the creature is in view. Part B: Volcanic, Crystal Forest, Ocean Islands with lava / sea, spires and shards, stacks and
arches, Dan's three Suno tracks) is built, checked in the exe (headless, muted, `--only round7a`, `round7b`, `round7bfree`,
`round7bperf` with the Deck setting, `round6a` again) and published as 0.7.0 (`game-700` Latest) + `soundtrack-700`. The fresh
starter-200 install check could not reach GitHub from this session (not verified). Not yet played by Dan.** Details and numbers in
`TODO.md` (Round 7 delivery). **Next: Round 8 (polish)**, to be written by the planning chat.
**Fix 6.3 (R1 is the trigger and fires, also in Free Flight; the cause of Dan's changed throttle axis and button jobs found: 0.6.2's
HOTAS setup rows stepped with left / right and the menus took left / right from the stick, so a hand leaning on it changed them (replayed:
exactly Dan's file); now the stick only scrolls and jobs change only by "press a button"; no Throttle axis / Invert lever options; a live
Test view and Record my input; an Engine volume, a new mix (music +4.6 dB over engine + wind at cruise, was -3.0), voices ducking the rest,
a levelled, louder computer with a warning chime, Test voices) is built, checked in the exe (headless, muted, `--only fix63`, `fix62` again)
and published as 0.6.3 (`game-603` Latest; soundtrack stays `soundtrack-401`); a starter-200 install updates to it from GitHub. Dan: "HOTAS is working as expected now."** Details in `TODO.md` (Fix 6.3 delivery). Round 7 followed (above).
**Fix 6.2 (the HOTAS throttle lever works: 0.6.1's Auto read the `slider` axis, which never moves on Dan's HOTAS, so the ship kept its 30 %
start; the lever is z, now found by itself or by Find throttle lever, with a live Throttle % and the ship following it from the first frame;
Reset HOTAS settings, Reset this page, Reset all settings, each asked first; calibration asked first and done in three plain steps; a new
HOTAS picture with close-ups; button names from Dan's own unit) is built, checked in the exe (headless, muted, `--only fix62`, `fix62pics`
at 1280 x 800) and published as 0.6.2 (`game-602` Latest; soundtrack stays `soundtrack-401`); a starter-200 install updates to it from
GitHub. Dan: the lever worked once its axis was set back.** Details and the cause in `TODO.md` (Fix 6.2 delivery). Fix 6.3 followed (above).
**Fix 6.1 (menus with the HOTAS no longer change the option you leave: Dan's stick reads half-left when centred, now calibrated, and
menus take the dominant axis with a lock; prompts name only the device in use's buttons, PlayStation symbols on the HOTAS, Xbox names and
colours on the pad; Settings > Controls per device with a code-drawn HOTAS / pad picture; HOTAS setup: calibrate, identify your buttons,
assign; the HOTAS layout moved onto its 12 buttons) is built, checked in the exe (headless, muted, `--only fix61` with Dan's recorded stick,
`fix61pics` at 1280 x 800) and published as 0.6.1 (`game-601` Latest; soundtrack stays `soundtrack-401`); a starter-200 install updates
to it from GitHub.** Details and the cause in `TODO.md` (Fix 6.1 delivery). Fix 6.2 followed (above).
**Round 6 (A: the alien attack as a jump scare: an alien boards exactly like a pilot, then strikes instantly, on take-off or as a slow burn;
canopy leap; red herrings. B: enemies easier to find. C: upgrade shop. D: full star map, coordinates, distress calls. E: Field Journal
viewer with 3D creatures) is built, checked in the exe (headless, muted, `--only round6a` .. `round6e` and `round6`) and published as
0.6.0 (`game-600` Latest; soundtrack stays `soundtrack-401`); a starter-200 install updates to it from GitHub. Not yet played by Dan.**
The event-log comparison: an alien boarding is identical to a real pilot boarding until the strike (16 / 16 and 20 / 20 events). Per-item
status, prices and tuning in `TODO.md` (Round 6 delivery). Fix 6.1 followed (above).
**Round 5 (Part A: the ship rights itself when the stick is released, a level-out button, Auto-level / Righting speed settings; Part B:
turrets, saucers, your gun and shield, hull damage from enemy fire, warnings, difficulty presets Easy / Normal / Hard / Nightmare / Custom)
is built, checked in the exe (headless, muted, `--only round5a`, `round5`, `round5perf` with the Deck setting, `round5presets`) and
published as 0.5.0 (`game-500` Latest; soundtrack stays `soundtrack-401`); a starter-200 install updates to it from GitHub. Not yet
played by Dan.** Open for Dan: turrets rarely hit a ship crossing at 650 m (tuning limit reached after two tries: `turretLead` .9,
`turretAimError` 14, `boltHitRadius` 3.5). Per-item status, numbers and the control layout in `TODO.md` (Round 5 delivery).
Round 6 followed (above).
**Fix 4.1 (Dan's own tension and regeneration music replace the stand-ins) is checked in the exe (headless, muted, `--only music`)
and published as 0.4.1 (`game-401` Latest + `soundtrack-401`); a starter-200 install updates to both from GitHub. Not yet heard by Dan.**
**Round 4 (planet biomes: Canyons, Giant Forest, Dead City, Ice; adaptive music with Dan's 7 Suno tracks as the launcher's soundtrack
component; voices: Control, the computer, 12 pilots, the aliens using them, radio sound, subtitles) is built, checked in the exe
(headless, muted, `--only round4`, `round4perf` with and without the Deck setting) and published as 0.4.0 (`game-400` Latest +
`soundtrack-400`); a starter-200 install updates to both from GitHub. Not yet played or heard by Dan.** Open: short hitches in forest / city in one run. Numbers and per-item status in `TODO.md`
(Round 4 delivery).
**Fix 3.2 (always another planet to fly: Jump to a new planet from the hangar, the Planets list, planet of the day +50 %, a cleared
planet offers the jump first) is built, checked in the exe (headless, muted, `--only fix32`) and published as 0.3.2 (`game-302`,
Latest); a starter-200 install updates to it from GitHub. Not yet played by Dan.** Details in `TODO.md` (Fix 3.2 delivery).
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
