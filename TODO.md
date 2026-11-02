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
Current Objective — Fix 2.4: flying feel, landing, pause / reset, deeper canyons
---------------------------------------------------------------------------

Fix 2.3 (getting home) is done and published as 0.2.3 (status under "Previous
deliveries"). Round 3 (Aliens) comes after this fix.

Dan (2026-11-02, playing 0.2.3, gamepad / Steam Deck; HOTAS still untried):
- "I just feel it is too clunky to fly right now."
- "I hit X and I don't understand why it isn't landing but then I go flying by
  the pilot I am trying to reach." (Cause, as the planning chat reads the code:
  X starts a brake at `hoverBrake` 12 m/s², so from 300+ km/h it takes about
  10 s and most of a kilometre to stop, with nothing showing that it's braking
  or where it will stop.)
- "There has to be some sort of menu so I can pause and quit." (A pause menu
  exists on Start / Esc, but Dan didn't find it, and it can't open during the
  automatic parts: docking, tow, recall, mid-air rescue.)
- "There needs to be a reset button in case you get up too far in the air or
  something. This happened to me when I had to do work and couldn't pause."
- "Valleys should be deep and more compact, not lots of open space."

0. **Commit first** (the repo as it is, with this text).
1. **Pause anywhere, and easy to find.**
   - Start / Esc pauses in every state of play, including docking, tow, recall,
     the mid-air rescue and launch (time stops; resume continues them).
   - **View / Back (the Deck's ⧉ button) also opens the pause menu**, and on the
     HOTAS one button Dan picks (default: a base button; see the axis display).
   - **Auto-pause when the game loses focus** (alt-tab, the Steam overlay, the
     Deck's Steam / ... buttons) in players' builds. Test runs keep their
     run-in-background behaviour.
   - Show "☰ / Esc: Pause" small in a corner of the HUD for the first minute of
     each flight.
   - Pause menu (Campaign and Free Flight): Resume, **Reset position** (item 2),
     Recall (Campaign), Settings, Quit to title (Campaign: saves), Quit game.
2. **Reset position.** From the pause menu: puts the ship back in a steady
   hover about 60 m above open, landable ground near where it was (Free Flight:
   or back at the start), engines on, same cabin, same energy, no charge. For
   "got too high / lost / stuck". Not available while a pilot is running to the
   ship or at the airlock.
3. **Landing that does what you expect.**
   - Much harder braking when X is pressed: from top speed to a hover in about
     3 s (air brakes; tuning file), with a brake sound and a "BRAKING" /
     "HOVER" state on the displays.
   - A **stop-point marker**: while flying near a pilot, show on the ground
     where you'd come to a hover if you pressed X now, so you can time it.
   - Radar cue when a pilot is close: "Slow down — press X" at the distance
     where the brake would stop you near them.
   - In hover: descend faster high up (e.g. 30 m/s above 150 m over the
     ground), slowing down near the ground, so setting down from altitude
     doesn't take ages. A height-over-ground readout in big type in hover.
   - Make the hover → settled → landed steps obvious on the displays (gear
     down, "TOUCHDOWN", "LANDED — engines off (A) so the pilot comes").
4. **Less clunky flying (Dan's main complaint; first pass).** The ship should
   feel quick and direct on a gamepad and the Deck:
   - faster, more direct pitch and roll response (less lag and smoothing),
     yaw that actually helps in tight canyons;
   - the wing-levelling and bank-turn assists made lighter, with a
     **Flight assist** setting: Full / Light (new default) / Off;
   - **Stick sensitivity** setting (and a response curve so small moves stay
     fine).
   Code reports the before / after numbers (response times, turn rates).
   This will need Dan's play to tune: keep values in the tuning file.
5. **Deeper, more compact canyons.** Narrower canyons with taller, steeper
   walls (about twice as deep as now, roughly 300-700 m), packed closer
   together with more branching, and much less open basin and flat plain. Keep
   small flat landing spots at every pilot's site (and a few others), since you
   now land in narrow places. Same seed still = same world (the worlds change
   once with this fix; existing saved planets regenerate, which is fine at this
   stage). Keep 60+ fps on Dan's PC and the Deck setting's frame rate.
6. **Check (muted, no focus, as the testing rules say):** pause works in
   flight and during docking / tow / recall; View/Back opens it; focus loss
   pauses a players' build (simulate); Reset position from high up and from a
   canyon; braking from top speed stops within the target and the stop-point
   marker matches where it stops (within 50 m); hover descent from 1000 m time;
   flight response numbers before / after; pictures from the cockpit flying a
   new canyon low, and a height map of the new terrain next to the old one.
7. **Publish as 0.2.4**, update `TODO.md` (per-item status) and `CLAUDE.md`
   (controls, new settings, tuning), commit and push.

**For Dan to check:** does flying still feel clunky (and if so, what exactly:
too slow to turn, too twitchy, drifts, levels itself)? Try Flight assist Light
and Off. Can you land next to a pilot first try using the stop-point marker?
Pause with ☰ and ⧉; Reset position. Do the canyons feel deep and tight enough?

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

**Fix 2.3 — getting home (2026-11-02). Published: 0.2.3 (`game-203`, Latest).**
Exe built 2026-11-02 14:27 (`BUILD_INFO.txt`). Evidence `Data/screenshots/fix2.3/`
(`run1`-`run3`: `exe_check.txt` + pictures; `launcher/`). Every check headless
(`-batchmode`, no window) and muted: measured output 0.000 RMS at start and end;
no step played sound; no window opened. New check: `python Tools/run_check.py <dir> --only home`.

0. **Commit first — done.** "Fix 2.3 planning" (d36c2d7).
1. **A clear way home — done.** Cockpit left display (top line: the lower lines sit below the
   flat view) and HUD: `HOME <bearing>° <km> · DOCK ↑ <m>`; the mothership is always on the
   radar; Advice says "Climb to dock (↑ 450 m)" whenever docking is allowed; a blinking blue
   sky marker under the mothership, same size on screen at any distance. Check: the readout
   counted 953 -> 850 -> 700 -> ... -> 100 -> 0 m, docking started at 1001 m (`h02`, `h03b`).
2. **Energy-to-get-home warnings — done.** Needed = height to climb / 30 m/s x full-throttle
   drain + 3 %. RETURN NOW within 8 % of it, CAN'T REACH below it (display, HUD, Advice with
   the recall keys, a new falling two-tone warning sound, repeated every 10 s / 6 s); 1 % of
   hysteresis; the 15 % beep stays. Check over the canyon floor (44 m): need 13.2 %, RETURN NOW
   at 21.2 %, CAN'T REACH at 13.2 % (`h03_return_now`, `h04_cant_reach`). Low energy also
   lets you dock with no pilots aboard (as below 25 %).
3. **Rescue in the air — done.** At 0 energy in the air the mothership comes (faster than
   station keeping) and its beam catches the ship within 600 m level distance, live, then the
   normal docking. Check: caught at 36-38 m above ground (not landed), docked, pilot paid
   +500, tow -1500 cr; camera continuous (run 1: fastest 73 m/s; runs 2-3 show a "JUMP" only
   because the check presses the camera button for the chase picture `h05b`).
4. **Recall — done.** Hold 1 s: gamepad View, keyboard H, HOTAS button 7 (`Recall` action),
   fee 900 cr at the dock (tow 1500). A short press only shows "Keep holding…". Check: 0.4 s
   did nothing; from the air (801 m up) and from the ground both docked, -900 cr each, camera
   continuous; refused while a pilot runs to the ship ("Not now: a pilot is coming").
5. **Pilots bring fuel — done.** 3-15 % per boarding, `System.Random` (not the seed), never
   above 100 %, message "Pilot 3 aboard (1 / 6), brought 9 % energy". Check, same planet and
   pilots in three runs: pilot 3 9 / 5 / 5 %, pilot 2 5 / 13 / 7 %, pilot 1 13 / 11 / 11 %.
6. **Checks — done** (above; `-exeCheckOnly home`, `Checks/ExeCheck.Home.cs`). Fixed on the
   way: the HUD flashed "OUT OF ENERGY" / "ENERGY LOW" during the beam lift (now the lift's
   own prompt); `run_check.py` crashed printing "↑" to the console (now UTF-8).
7. **Publish — done.** 0.2.3 = build 203, tag `game-203`, Latest. Check: the `starter-200`
   ZIP extracted to `Builds/StarterCheck`, `--auto-check` against GitHub: "Updating to game
   0.2.3", installed `versions/203`, started, exit 0 (`launcher/`). TODO / CLAUDE updated, pushed. Clean-up:
   disk use 1.9 GB -> 1.6 GB (incl. the 0.2 GB starter test install).

**New tuning (`CampaignTuning.json`):** recallFee 900, recallHoldSeconds 1, homeClimbRate 30,
homeMargin 3, returnNowWarning 8, beamReach 600, pilotFuelMin 3, pilotFuelMax 15.

**For Dan to check:** with a pilot aboard and low energy, is it clear how to get home, and do
the warnings come early enough (30 m/s is a cautious climb)? Run dry in the air: does the
beam catch you? Try the recall (hold View on the Deck / H / HOTAS 7: is button 7 a good
one on your HOTAS?). Is the sky marker big enough? Does pilot fuel help enough?

**Fix 2.1 — full screen on the Steam Deck (2026-11-02). Published: 0.2.1
(`game-201`), then 0.2.2 (`game-202`, Latest) with Dan's 1080 default.** Exe built 2026-11-02 13:08 (`BUILD_INFO.txt`). Evidence
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
   performance target is 60 fps at 1080p. **Dan decided: "Default to 1920 x
   1080" -> 0.2.2:** the default full-screen size is the display's own, at most
   1080 tall, same shape (`ScreenSetting.DefaultSize`): 4K -> 1920 x 1080
   stretched to fill, Deck 1280 x 800 unchanged; Resolution offers "Default
   (1920 x 1080)" first and the display's own 3840 x 2160 among the rest.
   Check (headless, muted, exe built 13:42): Deck 1280 x 800 -> 1280 x 800,
   simulated 4K and this PC -> 1920 x 1080 FullScreenWindow; six screens at 1280
   x 800 nothing cut off (`exe_check_screen_v022.txt`, `v022_s3_display.jpg`).
   Not re-run in a visible window (one OK, used once).
4. **Publish — done.** 0.2.1 = build 201, tag `game-201`, Latest:
   https://github.com/danemoll-jpg/Hostile-Horizon-releases/releases/tag/game-201 .
   Check: the `starter-200` ZIP extracted to an empty folder, its launcher run
   with `--auto-check` against the real GitHub catalog (hidden, game in batch
   mode): "Updating to game 0.2.1", "Game activated 0.2.1", started
   `versions/201`, ready handshake, exit 0 (`launcher/`). Found on the way: the
   first try, from a 144-character folder, failed with "Cannot create extracted
   file" (Windows' 260-character path limit inside Unity's data folders) and
   started the old version; from a short folder it worked. Not changed (launcher
   code, not this fix); keep installs in short paths. 0.2.2 = build 202, tag
   `game-202`, Latest; the same starter check updated to 0.2.2 and started
   `versions/202` (`launcher-202/`).
5. **TODO.md / CLAUDE.md — done.** Committed and pushed. Clean-up: disk use 1.7 GB -> 1.5 GB.

**For Dan to check:** start the game on the Deck from the same Steam shortcut:
it updates to 0.2.1 and fills the screen. If not: what do Settings > Display
Screen and Resolution show? On the PC (0.2.2): full screen at 1920 x 1080
stretched over the 4K screen; is the frame rate back to normal, and is it sharp
enough?

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
  `/button9` and `/button10` menu. Fix 2.3: `/button7` recall (hold 1 s).
- The launcher self-check needs the game to close itself in its hidden batch run
  (LauncherBridge does it when `-racerTestSave` is given).
- Blender FBX files import facing backwards: turn the model 180 degrees on load,
  and put anything placed in ship space (cockpit displays) on the ship, not on the
  turned model.
