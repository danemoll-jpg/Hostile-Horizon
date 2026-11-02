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
Current Objective — Round 2: Campaign core (rescue loop, credits, ongoing career)
---------------------------------------------------------------------------

Round 1 reconciled by the planning chat (2026-11-02): all 12 items done as
reported. Dan played it on PC ("ran well"). Still for Dan to check: HOTAS 4,
Steam Deck. Placeholder sounds are made in code (ElevenLabs comes in Round 6).

**Goal:** Dan can start a Campaign career, fly today's planet, find downed
pilots on the radar, land like a helicopter, pick them up, climb to the
mothership and dock (live, no fade), get paid in credits, pay for fuel and
repairs, die, get regenerated and carry on. Everything saves. All pilots are
real this round (aliens are Round 3), no enemies (Round 4).

0. **Commit first.** Commit the repo as it is (it holds the planning chat's
   doc updates) before changing anything.
1. **Crash = explosion, wreck stays (all modes, Free Flight too).** A real
   explosion (fireball, debris, smoke, sound) where the ship hits; the burning
   wreck stays in the scene and the camera stays on it live. The Ship Lost /
   death menu appears over the live scene. Remove Round 1's fade to black.
2. **Save file (career).** One career save: credits, the ship (hull, energy,
   wrecked or not, upgrades list — empty for now), the home system, every
   planet visited and its state, career stats. Autosave on dock, death and quit.
   Keep it where the launcher's save backup covers it (check `LauncherBridge` /
   Chastain Rush's save location), so updates never touch it. Campaign menu:
   **Continue** / **New career** (New asks to confirm before replacing a save).
3. **Home system + planets with generated names.** A new career generates its
   home star system and its name once. Today's planet comes from the seed of
   the day and gets a name generated from that seed (e.g. "Vessra-IV"); the
   same day gives the same planet and name. A planet keeps its state between
   visits: pilots rescued or lost stay that way. A planet is "cleared" when no
   pilots are left to rescue. (Star map and older planets: Round 5.)
4. **Downed pilots.** Seeded: each pilot sits by their crashed ship, which
   sends a beacon, with landable ground within reach. The first planet has 3
   pilots; later planets have more and spread them wider (tier rises with
   planets cleared in the career). Numbers in the tuning file (item 11).
5. **Radar.** Bearing and distance to each beacon, more detail as you close in
   (rough direction far out, exact spot near). On a cockpit display and on the
   flat-screen HUD (VR rule 2). Rescued and lost pilots drop off it.
6. **Helicopter landing.** A landing mode (one button on HOTAS / gamepad /
   keyboard): the ship can slow to a hover, turn on the spot, go straight down
   and up, gear down, and settle on fairly flat ground. Landing too hard or on
   a slope does damage by the crash level. Take off again by climbing out of
   the hover and going back to flight mode. Round 1's minimum speed applies
   only outside landing mode.
7. **The pickup.** Landed + engines off: the pilot leaves their wreck, runs to
   the ship and knocks the three-knock code on the hull. Airlock button opens
   the airlock and the pilot boards (seen and heard from the cockpit; the cabin
   count goes up). Engines started while a pilot is within the exhaust range
   kills them: lost for good on that planet, credit penalty. Cabin holds 6.
8. **Energy.** Drains with flight (more at high throttle and in hover). Gauge
   in the cockpit and HUD, warning at 15 %. At 0 the ship can only come down;
   once landed, call a **mothership tow** for a big fee (the lift plays live).
9. **Mothership + docking, live.** A mothership high above the planet (Blender
   placeholder model). With pilots aboard, climbing above the docking height
   starts the approach: the ship flies itself into the bay, live and
   continuous, and the player can look around the whole time (CLAUDE.md VR
   rule 5: no fade, no cut). Docked = the hangar (item 12). Launching again
   plays the drop out of the bay live.
10. **Credits (no score).** Paid per pilot delivered, plus a bonus when a
    planet is cleared. At docking: pilots paid, fuel used is charged, hull
    damage repair is charged (repair can be skipped and paid later). If the
    player can't afford fuel, the corps gives a free minimum fill, so a career
    can never get stuck.
11. **Tuning file.** All numbers (pilot pay, clear bonus, starting credits,
    fuel price, repair price, tow fee, recovery fee, pilots per planet tier,
    exhaust kill range) in one JSON file Code names and lists in `CLAUDE.md`.
    Starting values are Code's call; report them.
12. **Hangar screen (between flights).** Inside the mothership, built to the
    world-space panel rule: credits, ship hull and energy, today's planet name
    and pilots left (rescued / lost / waiting), career stats (pilots rescued,
    pilots lost, credits earned, deaths, planets cleared). Buttons: Launch,
    Repair, Refuel, Back to title.
13. **Death and regeneration.** On death (crash at any crash level that ends a
    flight): the explosion (item 1), then pilots aboard are lost (no pay, lost
    for good on that planet). You wake in the mothership's regeneration bay and
    keep credits, upgrades and stats. Your ship is a wreck on the planet; choose:
    - **Pay recovery:** the ship is hauled back and repaired, upgrades kept;
      fee grows with the ship's value.
    - **Take the loaner:** a free basic ship with no upgrades; your own ship
      stays wrecked until you pay its recovery later from the hangar.
    (Recovering the wreck yourself: Round 5.)
14. **Campaign crash level.** A Campaign setting using the Free Flight scale;
    default **Lasting damage**.
15. **Publish the round.** After the round's checks pass, publish it as a
    release to `danemoll-jpg/Hostile-Horizon-releases` with the launcher tools
    (version 0.2.0). This is the first real release, so also publish the
    starter package (launcher + game, `Package-LauncherStarter.py`, with the
    rewritten `README-LAUNCHER.md`) as a downloadable asset Dan can install on
    the Steam Deck. Check: a launcher installed from that starter package finds
    and installs the release from GitHub (not a local test catalog). Give Dan
    the starter download link in the status.
16. **End of round.** Update `TODO.md` (per-item status: done / not done /
    deferred and why, never skip one silently; "For Dan to check") and
    `CLAUDE.md` (Commands, tuning file, save location, Where things stand).
    Clean up the disk, commit and push.

**For Dan to check (after Round 2):** install on the Steam Deck from the
starter package (steps in its README) and play through the launcher; landing feels like a helicopter and is
fair in tight canyons; finding pilots with the radar; the pickup (knock,
airlock); docking live (does it feel real?); credits feel right for fuel and
repairs; the crash explosion and the death / recovery / loaner choice; the
planet name; Continue after quitting keeps everything.

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
