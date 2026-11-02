Active Development Plan — Hostile Horizon

A modern remake of the idea of Rescue on Fractalus! (1984) with its own name,
art, sound and story. Planned by Dan with the planning chat; built by Code.
Standing context and rules are in `CLAUDE.md`; process notes in
`collaboration-process-notes.md`.

---------------------------------------------------------------------------
Design decisions (all DECIDED with Dan, 2026-11-02 unless marked OPEN)
---------------------------------------------------------------------------

**Name — DECIDED:** Hostile Horizon. Take the original's idea, nothing else:
all-new assets, no original names, art, sound or text.

**Core loop — DECIDED: the same as the original.** You are sent to rescue your
people. Fly low through canyons on a hostile planet, find downed pilots by
their beacons, land near them, shut the engines down and wait. The pilot runs
to the ship and knocks; you open the airlock and they board. When the cabin is
full or the level's quota is met, climb to altitude and dock with the
mothership. Ship energy is a limited resource. Sometimes the "pilot" is an
alien, and if you let it aboard you lose the ship.

**Platforms — DECIDED:** PC and Steam Deck first, then VR (Quest 3 via Virtual
Desktop / SteamVR), maybe native Quest later. VR-ready architecture from day
one (rules in `CLAUDE.md`).

**Toolchain — DECIDED:** Unity, Blender, Suno (long songs), ElevenLabs (sound
effects, short songs). Paid licences.

**Launcher — DECIDED:** the Chastain Rush / Woodstock Rush launcher, renamed
`HostileHorizonLauncher.exe`, own signing key, catalog repo
`danemoll-jpg/Hostile-Horizon-releases`. Checks for the latest version on every
start and updates automatically. Local builds unless a round says "publish".

**Controls — DECIDED:** Thrustmaster T.Flight HOTAS 4 (confirmed by Dan), any
gamepad (incl. Steam Deck), keyboard + mouse fallback.

**Look — DECIDED: "a little more realistic".** Believable rock, depth and
atmosphere rather than stylized low-poly: triplanar rock and sediment materials,
layered canyon walls, haze/fog that gives depth, an alien sky. It must still hit
the frame-rate targets in `CLAUDE.md` (1660 Ti, Steam Deck, later VR), so
quality comes from materials, lighting and fog more than from polygon count.
Any third-party textures must be CC0 (licence noted in the credits).

**Cameras — DECIDED: several camera angles.** Cockpit (default, and the only
one in VR), chase, close chase and a low "canyon" cam. One button cycles them.

**Terrain — DECIDED: procedural, seeded.** Fixed "seed of the day" (from the
local date) so a run can be replayed, plus a random seed and a typed-in seed.
The seed decides terrain, pilots, enemies and which pilots are aliens.

**Aliens — DECIDED: "scary as hell", with variants.**
- Several alien types disguised as pilots, so you never know which one you get.
- Each type has its own **tell**, visible or audible in the 3D world (VR rule).
  The game never explains the tells up front: you learn them by being fooled
  or by catching one.
- **Sometimes they still fool you:** a type shows its tell only some of the time
  (how often depends on difficulty), and on harder settings real pilots can show
  a false, tell-like quirk.
- Starting set (names and tells can change after Dan sees them):
  1. **Mimic:** perfect suit, but its shadow is wrong (lags its body or has the
     wrong shape).
  2. **Hollow:** its visor reflects nothing (not your landing lights, not the sky).
  3. **Knocker:** wrong knock. Real pilots knock the standard three-knock code;
     it knocks wrong (extra knock, wrong rhythm), or knocks from the wrong place
     on the hull.
  4. **Stalker:** doesn't come straight to the airlock. It circles the ship,
     drops off the radar for a moment, or turns up from behind.
- **Field Journal (modern touch):** after you meet a type, a journal entry
  unlocks with a vague hint; after you meet it again, the hint gets clearer.
- **What you can do:** don't open the airlock; fire up engines and shields
  (kills whatever is at the hull, the original's answer); or take off. Doing
  that to a real pilot kills them: no rescue, a score penalty. The choice must
  cost something.
- **If you ignore it:** an alien left at the hull too long climbs onto the
  canopy and breaks in.
- **If you let it in:** a full horror sequence (lights die, alarms, something
  inside the cabin, use of the space behind the player for VR), then the ship
  is lost. Built for maximum scare; no scare-intensity setting (Dan: "make it
  scary as hell"). Avoid full-screen strobe flashes (photosensitivity) — the
  scare comes from darkness, sound and timing.

**Modes — DECIDED:**
1. **Campaign (the story):** rescue missions level after level. Difficulty is
   set by presets (Easy / Normal / Hard / Nightmare) plus custom options:
   - enemies: Off / Turrets / Turrets + Saucers, and how aggressive;
   - how reliably alien tells show, and whether real pilots show false tells;
   - crash level (same scale as Free Flight).
   The alien surprise stays on in every Campaign setting, enemies-off included.
2. **Free Flight:** no plot, no pilots, no enemies, no aliens. Just fly the
   valleys. Crash levels (DECIDED):
   - **Ghost:** pass through walls;
   - **Bounce:** bounce off, no damage;
   - **Regenerating damage:** damage heals over time;
   - **Lasting damage:** damage adds up, enough of it and you crash;
   - **Instant crash:** any hit ends the flight.

**Modern touches — DECIDED: all of them.**
- score and a high-score table (overall and per seed, including the seed of the day);
- ship upgrades between Campaign levels (engine, hull/shields, energy, radar
  range, cabin size);
- radar for downed pilots (bearing and range, more detail as you get closer);
- checkpoints (after a death, restart from the last rescue, not the level start);
- mission objectives beyond "rescue X pilots" (examples: recover a flight
  recorder, knock out a turret cluster, rescue everyone within a time limit,
  finish with no hull damage).

**OPEN — Free Flight time trial.** Should Free Flight also have a timed run
through a marked route (gates through the canyon) with a best time per seed?
Default: yes, as a later round, once Free Flight feels good. Dan to decide.

---------------------------------------------------------------------------
Current Objective — Round 1: project setup + Free Flight
---------------------------------------------------------------------------

**Goal:** a local Windows build Dan can play from the launcher: pick Free Flight,
fly a seeded canyon with the HOTAS, a gamepad or the keyboard, switch cameras,
choose a crash level. No pilots, enemies or aliens yet.

0. **Commit first.** Commit the planning docs as they are before changing
   anything ("Round 1 planning docs").
1. **Unity project:** Unity 6000.6.1f1, URP 17.6.0, Input System 1.20.0 only.
   Folders: `Assets/Scripts/<System>/`, `Assets/Editor/`, `Tools/`, `Docs/`,
   `SourceArt/`. `.gitignore` (start from Chastain Rush's, minus its map-data
   lines; `Builds/` and `Builds/PublisherPrivate/` ignored). Editor menu
   **Hostile Horizon > Build Executable** plus a batch command producing
   `Builds/HostileHorizon/HostileHorizon.exe`, and `Play Hostile Horizon.bat` in the root.
2. **Seeded canyon terrain:** chunked terrain meshes streamed around the
   player, generated from the seed (fractal/ridged noise shaped into a network
   of winding canyons with walls tall enough to fly between, open basins and
   ridges). Same seed = same world, every time. Seed sources: seed of the day,
   random, typed in. "A little more realistic" look: triplanar rock materials
   with layering on the walls, haze that gives depth, an alien sky and sun. Meet
   60 fps at 1080p on Dan's PC.
3. **Ship + flight model:** a placeholder ship made by a Blender script
   (`Tools/Blender/ship.py`) with a 1:1 3D cockpit. An arcade-leaning flight
   model in the spirit of the original: pitch, roll, yaw, throttle, a minimum
   forward speed when flying (landing comes in Round 2), easy to fly low through
   canyons. Speed and altitude shown in the cockpit instruments (and on a screen
   HUD in flat-screen mode).
4. **Cameras:** cockpit (default), chase, close chase, low canyon cam; one
   button cycles them. Built on the VR camera-rig rule in `CLAUDE.md`.
5. **Input:** one Input Actions asset with binding sets for the T.Flight HOTAS 4
   (stick pitch/roll, twist yaw, throttle lever absolute throttle, hat look,
   buttons for camera and menu), gamepad (Xbox layout / Steam Deck) and
   keyboard + mouse. Deadzones; invert pitch option; a simple axis display in
   Settings so Dan can see the HOTAS axes respond. HOTAS = "for Dan to check".
6. **Free Flight mode + crash levels:** all five levels (Ghost, Bounce,
   Regenerating damage, Lasting damage, Instant crash). After a crash: restart
   at the same seed, or return to the menu.
7. **Title menu + settings:** title screen (Hostile Horizon), Free Flight
   (seed + crash level picker), Campaign shown as "coming soon", Settings
   (controls, invert, camera default, graphics quality, volume), Quit. Menus
   usable with HOTAS buttons, gamepad and keyboard; built to the world-space
   panel rule.
8. **Steam Deck setting:** port `SteamDeck.cs` from Chastain Rush (lower view
   distance and effects; on by itself when running on the Deck).
9. **Launcher:** port the launcher and tools (see `CLAUDE.md`), rename to
   Hostile Horizon, make the signing key once, point the catalog at
   `danemoll-jpg/Hostile-Horizon-releases`, add `LauncherBridge.cs`. Build the
   launcher and run its self-check against a local test catalog. **Do not
   publish** a release this round.
10. **Placeholder audio:** engine hum and wind (ElevenLabs or simple
    placeholders). Music comes later (Suno).
11. **End of round:** update `TODO.md` (per-item status, "For Dan to check")
    and `CLAUDE.md` (Commands, Where things stand), clean up the disk, commit
    and push.

**For Dan to check (after Round 1):** fly with the HOTAS 4 (axes, twist,
throttle, buttons); fly with a gamepad; does the canyon look "a little more
realistic"; is flying low through canyons fun; the crash levels; the launcher
starts the game.

---------------------------------------------------------------------------
Next Steps — queued rounds (Do Not Start Yet)
---------------------------------------------------------------------------

- **Round 2 — Campaign core:** downed pilots with beacons, the radar, landing
  (slow, low, flat enough ground), engines off, pilot runs over and knocks,
  airlock, boarding, cabin capacity, energy, climb and dock with the
  mothership, level end, score. All pilots are real in this round.
- **Round 3 — Aliens:** the four types and their tells, tell reliability and
  false tells by difficulty, the shields/engines answer, canopy break-in, the
  on-board horror sequence, Field Journal. Scare sound design (ElevenLabs).
- **Round 4 — Enemies + difficulty:** turrets on ridges, saucers, enemy fire,
  shields and hull, damage; Campaign difficulty presets + custom options.
- **Round 5 — Modern progression:** upgrades between levels, mission
  objectives, checkpoints, high-score tables (overall + per seed, seed of the
  day).
- **Round 6 — Look, sound and first release:** art pass (ship model, cockpit,
  pilots and aliens from Blender), Suno soundtrack, ElevenLabs sound pass,
  Steam Deck tuning, credits. First "publish" through the launcher.
- **Round 7+ — VR:** OpenXR, PC VR through Virtual Desktop / SteamVR first:
  head-tracked cockpit, world-space menus, comfort options, VR performance
  pass. Native Quest decided afterwards.
- **OPEN (see above):** Free Flight time trial.

---------------------------------------------------------------------------
Previous deliveries
---------------------------------------------------------------------------

None yet.

---------------------------------------------------------------------------
Technical Notes / Blockers
---------------------------------------------------------------------------

- Reference project (read-only): `C:\Users\danmo\chastain_rush`. Its
  `CLAUDE.md` "Commands" section documents the launcher build, key and release
  scripts; its `Docs/Starter/README-LAUNCHER.md` is the starter README to
  adapt.
- Launcher build needs Visual Studio 18 and reads its SDK from Racer's
  `Builds/LauncherSDK` (as in Chastain Rush).
- The HOTAS 4 shows up to the Input System as a HID joystick; its axes may
  need a custom layout or explicit HID bindings. Record what worked here.
