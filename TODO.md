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
start and updates automatically. **Every round is published (DECIDED, Dan
2026-11-02):** Dan tests on the Steam Deck, especially while his PC is busy, so
each round ends with a release the launcher picks up.

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
  5. **Sleeper (Dan, 2026-11-02):** boards like a normal pilot and passes
     every check at the airlock. You think you rescued a pilot. Some time later
     in flight it turns:
     - **With other rescued pilots aboard:** it kills one of them (heard and
       glimpsed from the cockpit, not shown in full). If enough pilots are left
       aboard, they fight it off and kill it: the ship survives, you lose the
       pilot it killed. If too few are left, it gets through to the cockpit
       and kills you (the same horror sequence and death as any alien aboard).
     - **With no other pilots aboard:** it goes straight for the cockpit.
     - Starting number (tunable): the pilots win if at least 2 are left after
       the first kill. More pilots aboard = safer against it.
     - Its tell is subtle and only comes after boarding (e.g. cabin sounds that
       are off, a cabin-camera glimpse). A player who spots it in time can dock
       before it turns, or another answer Round 3 settles. Field Journal rule
       as for the others.
- **Field Journal (modern touch):** after you meet a type, a journal entry
  unlocks with a vague hint; after you meet it again, the hint gets clearer.
- **What you can do:** don't open the airlock; fire up engines and shields
  (kills whatever is at the hull, the original's answer); or take off. Doing
  that to a real pilot kills them: no rescue, a credit penalty. The choice must
  cost something.
- **If you ignore it:** an alien left at the hull too long climbs onto the
  canopy and breaks in.
- **If you let it in:** a full horror sequence (lights die, alarms, something
  inside the cabin, use of the space behind the player for VR), then you die
  (see "Ongoing campaign" below: you are regenerated, the ship is wrecked). Built for maximum scare; no scare-intensity setting (Dan: "make it
  scary as hell"). Avoid full-screen strobe flashes (photosensitivity) — the
  scare comes from darkness, sound and timing.

**Modes — DECIDED:**
1. **Campaign (the story):** an ongoing career (see below), planet after
   planet. Difficulty is
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
- **credits instead of score** (see below); career stats and a best run per
  planet replace the high-score table;
- ship upgrades bought with credits (engine, hull/shields, energy, radar
  range, cabin size);
- radar for downed pilots (bearing and range, more detail as you get closer);
- checkpoints: covered by the ongoing campaign (a planet keeps its state, so
  after a death you carry on where it stands);
- mission objectives beyond "rescue X pilots" (examples: recover a flight
  recorder, knock out a turret cluster, rescue everyone within a time limit,
  finish with no hull damage).

**Free Flight time trial — DECIDED yes (Dan accepted the default):** a later
round adds a timed run through gates in the canyon, best time per seed.

**Planning defaults — DECIDED (Dan, 2026-11-02: "the defaults are good"):**
shields/engines on a real pilot kills them; an ignored alien breaks in through
the canopy; the alien types and their tells; Field Journal; Campaign
difficulty presets + custom options.

**Landing — DECIDED: like a helicopter.** Slow to a hover, turn on the spot,
set straight down. **Cabin — DECIDED: 6 pilots at the start** (upgrades raise
it). **Pilots — DECIDED: the first planet has 3**, more on later planets.

**Crash — DECIDED: an explosion, and the wreck stays on screen.** No fade to
black, in every mode; the menu appears over the live scene.

**Credits — DECIDED (replaces score).** Earned by delivering pilots (plus a bonus
for clearing a planet). Spent on fuel, repairs, ship recovery and upgrades.

**Ongoing campaign — DECIDED (Dan, 2026-11-02: "sounds perfect").** The game never
ends like the original. You are a rescue-corps pilot whose mind is backed up on
the mothership.
- **Death:** you wake in the mothership's regeneration bay. You keep credits,
  upgrades and career stats. Pilots in your cabin die with you (no pay, lost
  for good on that planet).
- **Your ship** is a wreck on the planet: pay a **recovery** fee (repaired,
  upgrades kept, fee grows with the ship's value), or take a free **loaner**
  (basic, no upgrades) and pay the recovery later. Later: **recover it
  yourself** by flying back and landing at the wreck (free, risky), and optional
  **insurance** paid before a sortie.
- **Fuel:** docking refuels and charges for what you used. Out of energy on the
  planet: land, and the mothership **tows** you back for a big fee. If you
  can't afford fuel, the corps gives a free minimum fill: a career never gets
  stuck.

**Planets — DECIDED.** A new career generates a **home star system** with its
own name. The **seed of the day is a new planet** in it, with a name generated
from the seed (e.g. "Vessra-IV"); the same day gives the same planet. Planets
keep their state between visits (rescued / lost pilots stay that way; "cleared"
when none are left). Later: a **star map** to return to older planets, a typed
seed visits any planet, and planets get harder (more pilots, enemies, aliens) as
the career goes on.

---------------------------------------------------------------------------
Current Objective — Round 3: Aliens (to be written by the planning chat)
---------------------------------------------------------------------------

Fix 2.1 (full screen on the Steam Deck) is built, checked in the exe (headless,
muted) and published as 0.2.1 (status below, under "Previous deliveries"; its
specification, as the planning chat wrote it, is in `TODO-ARCHIVE.md`). Round 3
(Aliens) is next; the planning chat writes it here. Nothing to build until then.

---------------------------------------------------------------------------
Next Steps — queued rounds (Do Not Start Yet)
---------------------------------------------------------------------------

- **Round 3 — Aliens:** the five types (incl. the Sleeper's fight against
  rescued pilots in the cabin) and their tells, tell reliability and
  false tells by difficulty, the shields/engines answer, canopy break-in, the
  on-board horror sequence, Field Journal. Scare sound design (ElevenLabs).
- **Round 4 — Enemies + difficulty:** turrets on ridges, saucers, enemy fire,
  shields and hull, damage; Campaign difficulty presets + custom options.
- **Round 5 — Modern progression:** the upgrade shop (credits), mission
  objectives, the star map (return to older planets, typed seed), recovering
  your own wreck yourself, insurance, best run per planet.
- **Round 6 — Look, sound and first release:** art pass (ship model, cockpit,
  pilots and aliens from Blender), Suno soundtrack, ElevenLabs sound pass,
  Steam Deck tuning, credits.
- **Round 7+ — VR:** OpenXR, PC VR through Virtual Desktop / SteamVR first:
  head-tracked cockpit, world-space menus, comfort options (all OFF by
  default: no snap turn, no fades, no vignette; Dan wants realism), VR performance
  pass. Native Quest decided afterwards.
- **Free Flight time trial** (DECIDED yes): gates, timer, best time per seed.
  After Round 5 unless Dan moves it.

---------------------------------------------------------------------------
Previous deliveries
---------------------------------------------------------------------------

**Fix 2.1 — full screen on the Steam Deck (2026-11-02). Published: 0.2.1
(`game-201`, Latest).** Exe built 2026-11-02 13:08 (`BUILD_INFO.txt`). Evidence
`Data/screenshots/fix2.1/`. Every check headless (`-batchmode`, no window) and
muted: measured output 0.000 RMS; no step played sound; no window opened.

0. **Commit first — done.** "Fix 2.1 planning" (d4bfb66).
1. **ScreenSetting ported — done.** `Core/ScreenSetting.cs` (Chastain Rush's):
   before the first scene the game asks for a borderless full-screen window at
   `Display.main.systemWidth/Height`, overriding the size Unity remembers; choice
   saved in `display.json` beside `settings.json`. Test runs (`-mute`,
   `-exeCheck`, `-racerTestSave`) never apply or save it. Check: "check run
   applied nothing: Screen 1280 x 800 Windowed" (its `-screen-*` size stood).
2. **Settings > Display — done.** New "Display" row under Axis display: Screen
   (Full screen / Windowed) and Resolution (the display's own first, then its
   sizes from 1280 wide, largest first). Check: `s3_display.jpg`.
3. **Check — done (Deck simulated headless; PC in a real window, Dan's OK).** New `-exeCheckOnly screen`
   (`python Tools/run_check.py <dir> --only screen --size 1280x800 --extra
   "-steamDeck 1"`): the size code path for a 1280 x 800 desktop asks for 1280 x
   800 FullScreenWindow (fills the screen); title, settings, display, cockpit,
   chase, pause played at 1280 x 800: every image and text glyph measured inside
   the view, "nothing cut off" on all six (`s1`-`s6`, `exe_check_screen.txt`).
   Dan's PC: the same path for his display (3840 x 2160) asks for 3840 x 2160
   FullScreenWindow. **Real start on Dan's PC (visible, focused window, run once
   with his OK, about 30 s, muted: output 0.000 RMS):** new `-exeCheckOnly
   fullscreen` (applies the default setting, reads / saves no `display.json`):
   "window 3840 x 2160 FullScreenWindow; display 3840 x 2160 -> fills the
   screen", title and cockpit HUD nothing cut off (`f1_title.jpg`,
   `f2_cockpit.jpg`, `exe_check_pc_fullscreen.txt`). **But only 13.6 fps (1 %
   low 10.4) at 3840 x 2160** in the cockpit, at Below Normal priority; the
   performance target is 60 fps at 1080p. Not changed: Dan decides (e.g. a
   1920 x 1080 default on displays bigger than that, or a render scale). Player
   behaviour unchanged by the check mode, so not republished; the local exe
   (built 13:17) has the extra check mode.
4. **Publish — done.** 0.2.1 = build 201, tag `game-201`, Latest:
   https://github.com/danemoll-jpg/Hostile-Horizon-releases/releases/tag/game-201 .
   Check: the `starter-200` ZIP extracted to an empty folder, its launcher run
   with `--auto-check` against the real GitHub catalog (hidden, game in batch
   mode): "Updating to game 0.2.1", "Game activated 0.2.1", started
   `versions/201`, ready handshake, exit 0 (`launcher/`). Found on the way: the
   first try, from a 144-character folder, failed with "Cannot create extracted
   file" (Windows' 260-character path limit inside Unity's data folders) and
   started the old version; from a short folder it worked. Not changed (launcher
   code, not this fix); keep installs in short paths.
5. **TODO.md / CLAUDE.md — done.** Committed and pushed. Clean-up: disk use 1.7 GB -> 1.5 GB.

**For Dan to check:** start the game on the Deck from the same Steam shortcut:
it updates to 0.2.1 and fills the screen. If not: what do Settings > Display
Screen and Resolution show? On the PC: the game now opens full screen at 3840 x
2160 and measured 13.6 fps there; Settings > Display > Resolution > 1920 x 1080
until a default is decided.

**Round 2 — Campaign core (2026-11-02). Published: 0.2.0 (`build-200`, Latest)
and the starter package (`starter-200`).** Exe `Builds/HostileHorizon/HostileHorizon.exe`
(built 2026-11-02 07:33, `BUILD_INFO.txt`). Checked in the built exe from the
title through the game's own input actions (virtual gamepad), 1920x1080, GTX
1660 Ti: `Data/screenshots/round2/` (`exe_check_campaign.txt`,
`exe_check_free_flight.txt`, pictures). HOTAS: not checked (code can't hold it).
**Test rules (Dan, mid-round, now in CLAUDE.md Process):** the first checks were
audible and windowed; one window took focus (the first mute test, about 15 s).
Since then every check runs with `Tools/run_check.py`: `-batchmode` (no window,
the view drawn off-screen for screenshots) and `-mute` (master volume 0 before
the first scene, the listener's final mix zeroed): measured output 0.000 RMS in
every run (the game itself produced about 0.08 RMS). No step played sound.

0. **Commit first — done.** "Round 2 planning docs" committed and pushed first.
1. **Crash = explosion, wreck stays — done.** `Effects/Explosion.cs`: fireball,
   sparks, a light flash in the world (no screen strobe), 18 debris pieces that
   bounce and rest, black smoke, then flames, a smoke column, firelight and
   crackle on the charred wreck; boom sound. The view eases out of the cockpit and
   circles the wreck live; the menu comes up over it. Round 1's fade is removed.
   Check: Free Flight Instant crash and Campaign crash (396 km/h), both "page over
   the live scene, camera continuous" (`35`-`38`, `12_ship_lost_4`).
2. **Save file — done.** `Core/Career.cs`, `career.json` beside `settings.json`
   (LocalLow, inside the launcher's save backup). Saved on dock, death, quit to
   title, closing the game. Campaign menu: Continue / New career (confirm page if a
   career exists) / Crash level. Check: quit to title + Continue gave the same
   credits, stats, planet and ship.
3. **Home system + planets — done.** `Core/Names.cs`: a new career names its home
   system (e.g. "Vera"); today's planet is named from the seed of the day:
   20261102 = **Bryshra-IV** (twice the same), 20261103 = Zanir-IV. Planets keep
   rescued / lost pilots; cleared when none wait (bonus paid once).
4. **Downed pilots — done.** `Campaign/PlanetSites.cs`, `DownedPilot.cs`: seeded
   flat landing patches on canyon floors (bumps under 2 m over 32 m), pilot 30 m
   off, their wreck + blinking beacon beyond; 3 on the first planet, +1 per
   planet cleared, wider spread per tier. Check: same spots twice; today 2.2-2.9 km out.
5. **Radar — done.** `UI/Radar.cs`: far (>3 km) rim blip to 30°, mid to 10° /
   0.5 km, within 1 km the exact spot; on the cockpit's right display and the HUD,
   with a list; the mothership shows when you can dock. Check: "PILOT 3 020° 2.0
   km" -> "029° 460 m" -> "004° 50 m".
6. **Helicopter landing — done.** Land button (X / G / HOTAS 4): brake to a hover,
   stick moves and turns on the spot, throttle climbs / sinks, gear legs come down,
   settles if slow and flat; too fast / too steep / gear up = a hit at the crash
   level. Check: 3 landings at 2.4-2.5 m/s, 4 m from the spot, no hits. Fixed on
   the way: turning in hover shrank with frame rate.
7. **The pickup — done.** Landed + engines off (A / K / HOTAS 5): the pilot runs,
   knocks three times (sound at the hull) every 7 s; airlock (B / O / HOTAS 6)
   opens the door and they board. Engines started with a pilot within 25 m kill
   them. Check: knock 4 s after engines off, boarded, cabin 1; engines on at 21 m:
   pilot dead, lost on the planet, -400 cr.
8. **Energy + tow — done.** Drain in flight (more with throttle) and hover, none
   with engines off; warning + beep under 15 %; at 0 a slow powerless descent that
   eases to a safe touchdown; landed with none: the tow page; the mothership comes
   over, a beam lifts the ship into the bay, live. Check (energy set to 0.5 % over
   a landing spot, logged as a check shortcut): landed 2.5 m/s, tow live (top 45
   m/s), -1500 cr fee.
9. **Mothership + live docking — done.** `Tools/Blender/mothership.py` (2.7k
   triangles, open-stern bay, floor doors, cradle, regeneration pod),
   `Campaign/Mothership.cs` (keeps station over you at 1500 m). Launch: doors open,
   the cradle lets go, the ship drops and its engines catch it. Docking (above 1000
   m with pilots aboard, or none left, or under 25 % energy): it flies itself round,
   under the bay and up into the cradle; you can look round (118° checked). Check:
   launch top 73 m/s, docking top 125 m/s, no frame jump.
10. **Credits — done.** Pilot pay, clear bonus, fuel charged at the dock (free fill
    to 35 % if you can't pay), repair in the hangar, tow fee; credits never go
    below 0. Check: 3000 -> 3322 (+500 pilot, -178 fuel for 30 %).
11. **Tuning file — done.** `Assets/Resources/Tuning/CampaignTuning.json` (see
    "Tuning (starting values)" below).
12. **Hangar — done.** A menu page over the live bay, seen from the cockpit
    (world-space-ready panel): credits, ship, hull, energy, today's planet and its
    pilots, career stats, last flight; Launch / Repair / Refuel / Recover your own
    ship (when flying the loaner) / Back to title.
13. **Death + regeneration — done.** Pilots aboard lost, death counted, ship
    wrecked; "Wake up": the view rises from the wreck and flies in through the open
    stern to the pod (live; top speed 221 m/s, no frame jump, moving with the
    carrier); Pay recovery (3000 cr) or Take the loaner
    (yellow, free): the ship rises in on the tow beam while you watch, then you
    step into the cockpit. Check: 1 pilot lost with the ship; loaner; recovery from
    the hangar (credits topped up by the check to afford it, logged): 3500 -> 500.
14. **Campaign crash level — done.** Campaign menu option, default Lasting damage
    (`settings.json` `campaignCrashLevel`).
15. **Publish — done.** 0.2.0 = build 200, Latest:
    https://github.com/danemoll-jpg/Hostile-Horizon-releases/releases/tag/build-200 .
    **Starter (download this):**
    https://github.com/danemoll-jpg/Hostile-Horizon-releases/releases/download/starter-200/HostileHorizon-Starter.zip
    (42 MB; launcher + the Round 1 game 0.1.0, so its first start must update).
    Check: that ZIP downloaded from GitHub into an empty folder, its launcher run
    against the real catalog: "Updating to game 0.2.0", downloaded 38 MiB,
    "Game activated 0.2.0", started `versions/200`, ready handshake, exit 0
    (`50_fresh_install_updating_from_github.jpg`, `fresh_install_*`). The releases
    repo was empty, so GitHub refused to tag: Code added a README as its first commit.
16. **End of round — done.** TODO / CLAUDE updated, disk cleaned (repo folder 1.92 GB -> 1.42 GB: check output removed after copying the evidence),
    committed and pushed.

Frame rate (1080p High, 1660 Ti, measured while the PC was otherwise idle,
windowed run 06:12): Free Flight low 439 fps (1 % low 214); Campaign with beacons +
mothership 373 fps (1 % low 157); hangar 435 fps. Final headless runs (07:35-07:50,
while another session's Chastain Rush check ran): Free Flight 416 fps (1 % low 57),
Campaign 159 fps with hitches up to 0.8 s, hangar 531 fps; Steam Deck setting at
1280x800: 1113 fps (1 % low 314). The typed-seed step can't type in a headless run
(no keyboard without a window); it passed windowed (1987).

**Tuning (starting values, Code's call):** starting credits 3000; pilot pay 500;
clear bonus 1000; fuel 6 cr per %; free fill to 35 %; repair 12 cr per %; tow
1500; recovery 1500 + 25 % of ship value (6000) = 3000; exhaust kill 25 m,
penalty 400; cabin 6; pilots 3 + 1 per tier (max 10), 900-3000 m out (+500 m per
tier), 700 m apart; pickup range 160 m; run 5.5 m/s; energy drain flight 0.10 +
0.22 x throttle %/s, hover 0.35 %/s (about 6 min of flight); warning 15 %; hover
18 m/s, climb / sink 14 m/s, turn 50°/s, brake 12 m/s²; safe touchdown 3.5 m/s,
slope 14°; mothership 1500 m, docking above 1000 m.

Decisions made: docking also starts with no pilots aboard if none are left or
energy is under 25 % (else you could be stuck up there); the regeneration pod is in
the bay, so every move to it is a real camera flight; quitting mid-flight puts the
ship back in the hangar with its energy and hull, and pilots in the cabin go back
to wait on the planet; the out-of-energy page has only "Call the tow" (no free way
out); the starter ZIP carries the Round 1 game (0.1.0) so its first start must
update from GitHub; landing mode also works in Free Flight. New: Steam poster
(`make_cover.py`). Known: one native Unity crash (access violation in the engine,
no game code on the stack, D3D12) while a check started a new career; 9 other
runs passed that point; not reproduced. Placeholder art and code-made sounds.

**For Dan to check (Round 2):**
- Steam Deck: install from the starter ZIP (steps in its README); the first start
  should update to 0.2.0 by itself.
- Landing like a helicopter: is it fair in tight canyons? Hover sink 14 m/s: too slow from 1500 m?
- Finding pilots with the radar; the pickup (knock, airlock — the pilot at the
  door is hidden by the hull from the cockpit; the knock is the cue).
- Docking and launch live: do they feel real? The trip to the regeneration pod.
- Credits for fuel, repairs, tow, recovery; the crash explosion; the planet name.
- HOTAS: buttons 4 / 5 / 6 are guesses for landing mode / engines / airlock.
- Continue after quitting keeps everything.

**Round 1 — project setup + Free Flight (2026-11-02). Local build only, not
published.** Exe `Builds/HostileHorizon/HostileHorizon.exe` (built 2026-11-02
03:34, see its `BUILD_INFO.txt`). Checked in the built exe from the title screen
through the game's own input actions (a virtual gamepad added to the Input
System), 1920x1080 on Dan's PC (GTX 1660 Ti): `Data/screenshots/round1/`
(`exe_check.txt` and pictures). HOTAS: not checked (code can't hold it).

0. **Commit first — done.** "Round 1 planning docs" committed and pushed before
   any change.
1. **Unity project — done.** Unity 6000.6.1f1, URP 17.6.0, Input System 1.20.0
   only (ProjectSettings, URP assets and package list copied from Chastain Rush;
   `com.unity.modules.screencapture` added for screenshots). Folders
   `Assets/Scripts/<System>/`, `Assets/Editor/`, `Tools/`, `Docs/`, `SourceArt/`;
   `.gitignore` from Chastain Rush minus its map lines. **Hostile Horizon > Build
   Executable** + batch command; `Play Hostile Horizon.bat`. Check: batch build
   succeeds (0 errors, 98 MB, about 35 s once imported).
2. **Seeded canyon terrain — done.** `Terrain/CanyonField.cs` (two warped noise
   fields give winding, forking canyons; wider basins; terraced walls 150-350 m;
   ridges), 256 m chunks streamed around the camera at 4/8/16/32 m detail with
   skirts (`TerrainStreamer.cs`). Look: own triplanar rock shader with sediment
   layers on the walls, dust on flats, darker canyon floors, sun shadows, height
   haze; teal-to-amber alien sky with sun and a banded gas giant. Textures are
   generated by the game (no third-party art). Checks: same seed twice gives
   identical heights and start, the next seed differs; restart starts at the same
   place; height map `00_heightmap.jpg`; **415-424 fps average, 1 % low about
   183 fps** flying low at 1080p High (target 60).
3. **Ship + flight model — done.** `Tools/Blender/ship.py` (Chastain Rush's kit)
   makes `Assets/Resources/Models/Ship.fbx`, 4.1k triangles, 1:1 cockpit (seat,
   consoles, dash, canopy). Arcade model: pitch / roll / yaw / throttle, 108-450
   km/h, no hovering (landing is Round 2), bank turns, wings level slowly when the
   stick is centred. Speed, altitude, height over ground, heading, throttle, hull,
   seed, crash level and view on three cockpit displays and on a screen HUD.
   Check: `03_cockpit.jpg`; RT held 2 s: throttle 30 to 100 %, 369 km/h.
4. **Cameras — done.** Cockpit (default), Chase, Close chase, Canyon (low,
   behind and below); one button cycles (Y / C / HOTAS button 3); look on hat /
   right stick / arrows / right mouse. Camera rig per the VR rule (code moves the
   rig root only). Check: four presses go round to Cockpit; pictures `04`-`07`.
5. **Input — done; HOTAS for Dan to check.** One asset
   (`Assets/Resources/Input/Controls.inputactions`) with HOTAS / Gamepad /
   Keyboard+mouse binding sets; deadzone (8 %), invert pitch, HOTAS throttle axis
   picker (Auto = slider if present, else z) and invert lever (on); Settings >
   Axis display shows every joystick axis, held buttons and what the ship reads.
   Check: menus, flight, view, look, throttle and pause all driven through the
   actions by a virtual gamepad; `10_axis_display.jpg`. HOTAS button numbers are
   guesses (see "For Dan to check").
6. **Free Flight + crash levels — done.** Checks (nose pushed into the ground):
   Ghost: through the rock (62 m under the surface), no hits; Bounce: 3 hits, no
   damage; Regenerating: one hit -32 %, healed to -18 % 4 s later; Lasting: -32 %
   stays -32 %; Instant: first hit ends it, SHIP LOST (fade, then Restart same
   seed / Return to title). Pictures `11_*`, `12_ship_lost_4.jpg`.
7. **Title menu + settings — done.** Title (Free Flight, Campaign "coming
   soon", Settings, Credits, Quit); Free Flight page (seed of the day / random /
   typed in, crash level, Start); Settings (invert pitch, deadzone, HOTAS throttle
   axis + invert, axis display, default view, graphics quality, Steam Deck,
   volume); pause menu. The same panel shown as a world-space board in
   `13_menu_world_space.jpg` (VR rule 3). Typed seed checked: D-pad digits plus a
   typed "5", B deletes it, result 1987. Credits page added (CLAUDE.md licensing
   rule).
8. **Steam Deck setting — done.** Chastain Rush's `SteamDeck.cs` ported (Auto /
   On / Off; Auto = the Deck's screen or GPU): view and shadow distance 60 %, no
   MSAA, one shadow cascade. Check `-steamDeck 1` at 1280x800: view 1920 m,
   `14_steam_deck_cockpit_1280x800.jpg` (frame rate measured on the 1660 Ti, not
   on a Deck).
9. **Launcher — done, not published.** Ported and renamed
   (`HostileHorizonLauncher.exe`, catalog `danemoll-jpg/Hostile-Horizon-releases`),
   new signing key made once (key ID `0c9c29ff...`, private key in
   `Builds/PublisherPrivate/`), built with VS 18. Self-check against a local test
   catalog (`Tools/launcher_selfcheck.py`): found build 2, downloaded, verified,
   installed, started the game, got its ready signal, both exited cleanly
   (`launcher_self_check.txt`, `15_launcher_self_check.jpg`). Added: in the
   self-check's hidden batch run the game closes itself after the ready signal.
   `Play Hostile Horizon (launcher).bat` starts a local launcher install of the
   newest build (`Tools/make_local_launcher.py`); the real catalog has no release
   yet (404), so the launcher says it couldn't check and starts the game.
10. **Placeholder audio — done (made in code, not ElevenLabs).** Engine hum
    (pitch with throttle), wind (with speed) and a thud on hits, synthesized at
    run time; no sound files this round. Check: output level 0.07-0.09 RMS while
    flying (silent = 0). Nobody has listened to it yet.
11. **End of round — done.** TODO.md / CLAUDE.md updated, disk cleaned,
    committed and pushed.

Decisions made: the scene is one GameShell that builds everything in code (no
hand-edited scenes or prefabs); ground contact is tested against the drawn
terrain (no physics engine); Blender models are turned 180 degrees on load (as
Chastain Rush does); flight assist = bank turns + slow wing levelling; two start
files (the exe, and through the launcher). Ported from Chastain Rush: project
settings, URP assets, package list, `.gitignore`, PlayerBuild, the Play bat,
SteamDeck.cs, LauncherBridge.cs, `Launcher/` with its vendor licences, the six
launcher tools, `cleanup_round.py`, README-LAUNCHER (rewritten), Blender `kit.py`.
Known: the full exe check ran on the build before last; the last build only
shortened one cockpit display's text and was checked with the short run
(`exe_check_final_build.txt`). The wavy strata "drips" and the plain ship are
placeholder art (Round 6).

**For Dan to check (Round 1):**
- HOTAS 4: in Settings > Axis display, do stick, twist, throttle lever and hat
  move the bars? Which button numbers show for the buttons you want for View,
  Menu, Select (trigger) and Back? If the lever works backwards, toggle "Invert
  throttle lever"; if it doesn't move the bar, try another "HOTAS throttle axis".
- Fly with a gamepad (LB / RB yaw, RT / LT throttle, Y view, Start menu).
- Does the canyon look "a little more realistic"? Is flying low through it fun?
- The five crash levels.
- Sound: engine hum and wind (placeholders): too loud or annoying?
- The launcher: `Play Hostile Horizon (launcher).bat` should show the launcher,
  say it couldn't check for updates, then start the game.

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
  Round 1 bindings (not yet tried on the real HOTAS): `<Joystick>/stick` pitch /
  roll, `<Joystick>/twist` and `/rz` yaw, throttle lever `<Joystick>/slider`
  (the Settings picker overrides it: slider, z, rz, rx, ry, dial), `/hat` look and
  menu navigation, `/trigger` select, `/button2` back, `/button3` view,
  `/button9` and `/button10` menu.
- The launcher self-check needs the game to close itself in its hidden batch run
  (LauncherBridge does it when `-racerTestSave` is given).
- Blender FBX files import facing backwards: turn the model 180 degrees on load,
  and put anything placed in ship space (cockpit displays) on the ship, not on the
  turned model.
