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
Current Objective — Fix 2.5: tight control, seeing the ground, the pickup, launch warning, faster lift
---------------------------------------------------------------------------

Fix 2.4 is done and published as 0.2.4 (status under "Previous deliveries").
Round 3 (Aliens) comes after this fix.

Dan (2026-11-02, playing 0.2.4, gamepad / Steam Deck; HOTAS still untried):
- "It is too loose, I feel I don't really have control over it. It easily gets
  turned in directions I don't want."
- "When landing for a pilot, it is hard to get a good angle, there should be
  either a monitor or some way I can see what's going on (glass floorboards)."
- "Landed, the pilot came towards me, he didn't even knock and I didn't see a
  button for airlock."
- "The game should warn you if you are leaving the mothership and have not a
  lot of fuel."
- "When the mothership lifts you it should be much quicker."

0. **Commit first** (the repo as it is, with this text).
1. **Tight, predictable control (Dan's main complaint).** Fix 2.4 made the ship
   faster and the assists lighter; Dan now finds it loose and it turns where he
   doesn't want. Goal: the ship goes exactly where the stick points and nowhere
   else.
   a. **Find every source of rotation or drift the player didn't ask for, first,
      with data:** log, per frame, the stick input and each contribution to the
      ship's rotation and velocity (stick, assist bank-turn, assist levelling,
      collisions / wall bounces, ground effect, anything else). Fly the check's
      route with the stick centred and with simple inputs, and list what moved
      the ship without input and by how much. Report it.
   b. **Remove or tame those sources:** stick centred = the ship holds its
      attitude (no slow turning from the bank-turn assist on Light, no
      roll-over); the flight path follows the nose closely (little or no side
      slide / momentum carrying it the old way); rotation stops when the stick
      is released (no overshoot or wobble); wall bounces push the ship away
      from the wall, never spin it.
   c. **Calmer default rates and finer centre:** bring the default pitch / roll
      rates back down (about midway between 0.2.3 and 0.2.4), a larger centre
      deadzone on gamepad / Deck sticks (Deck sticks drift), and a stronger
      stick curve so small corrections are small. Keep Stick sensitivity and
      Flight assist settings (Light default).
   d. Report before / after numbers, including the "uncommanded movement" list
      from (a).
2. **See the ground when landing.**
   - A **belly camera** feed on a cockpit display that switches on by itself in
     hover (and can be called up any time): looking straight down with the
     stop-point / landing ring, the pad, the pilot and a height readout. Also a
     picture-in-picture on the flat-screen HUD while hovering.
   - **Glass floor panels** in the cockpit at the pilot's feet (and the lower
     canopy) so you can look down and see the ground, in flat-screen cockpit view
     (look down with the look control) and later in VR.
   - A **landing view** added to the camera cycle: high over the ship looking
     down at it, for flat-screen players.
3. **The pickup must be obvious and must work.**
   - **Find out why the pilot didn't knock** in Dan's run: reproduce on the new
     0.2.4 terrain (narrow pads, pilot path from the wreck to the door, the
     ship's landed position and heading relative to the pad). Fix the cause
     and report what it was. A pilot who can't reach the door must say so
     (Advice line) instead of silently stopping.
   - On-screen and in the cockpit, always: **"PILOT AT AIRLOCK — press B to
     open"** (the real binding for the device in use: gamepad / Deck, keyboard,
     HOTAS) the moment the pilot is at the door, plus a lit AIRLOCK button on the
     cockpit panel. While they run: "PILOT COMING — keep engines off".
   - The knock: a visible "KNOCK KNOCK KNOCK" on the displays as well as the
     sound; and the pilot visible on the belly camera / an airlock camera on the
     display when at the door (he's hidden by the hull from the cockpit).
   - Make sure players' builds are NOT muted (the test `-mute` flag only applies
     to test runs): confirm the knock sound plays in a players' build by logging
     its AudioSource playing (no unmuted run needed); if a real sound check is
     needed, follow the testing rule (brief, report it).
4. **Low-fuel warning when launching.** In the hangar, if energy is low (below
   a tuning value, starting 50 %), Launch asks first: "Energy 22 %. Refuel
   first? (cost N credits)" with **Refuel and launch** (default) / Launch
   anyway. Also a "Refuel" reminder on the hangar screen whenever energy is
   under 100 %.
5. **Much quicker mothership lift.** The tow, the recall lift and the mid-air
   beam rescue: about 3x faster than now, with the lift itself taking at most
   about 10 s from the ground into the bay. Still live and continuous (VR rule
   5), no jump. Shorten the docking approach the same way if it takes more
   than about 15 s. Tuning values in the file.
6. **Check (muted, no focus, as the testing rules say):** the uncommanded-
   movement list before / after; stick-centred hold test; a full pickup on
   at least three different pads of the new terrain (pilot knocks, prompt
   shows, airlock, boards), with cockpit pictures of the prompt and the belly
   camera; the launch warning; lift and docking times before / after.
7. **Publish as 0.2.5**, update `TODO.md` (per-item status) and `CLAUDE.md`,
   commit and push.

**For Dan to check:** does the ship go where you point it now? Land with the
belly camera / glass floor; does the pilot knock and is the airlock prompt
clear? Launch with low fuel; the lift speed. HOTAS when you get a chance.

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

**Fix 2.4 — flying feel, landing, pause / reset, deeper canyons (2026-11-02). Published: 0.2.4
(`game-204`, Latest).** Exe built 2026-11-02 17:20 (`BUILD_INFO.txt`). Evidence `Data/screenshots/fix2.4/`
(`run4/` = the published build, `run3/`, `all/`, `perf_deck/`, `before_023/` = 0.2.3's full check, `launcher/`,
`heightmap_old_vs_new.jpg`). Every check headless (`-batchmode`, no window) and muted: measured output 0.000 RMS
at start and end of every run; no step played sound; no window opened. New check: `python Tools/run_check.py <dir> --only fix24`.

0. **Commit first — done.** "Fix 2.4 planning" (3ef81de).
1. **Pause anywhere, easy to find — done.** Start / Esc / HOTAS 9 (Settings > Controls and HOTAS > HOTAS pause
   button, 1-16) / 10, and a tap of View (held 1 s it stays the recall), in flight and during launch, docking,
   recall lift, mid-air beam and tow (time stops; resume carries on). Auto-pause on focus loss in players' builds
   (check runs keep running). "≡ / Esc: Pause" on the HUD and cockpit display for the first minute (☰ isn't in the
   game's font; ≡ is). Pause menu: Resume, Reset position, Recall (Campaign) / Restart (Free Flight), Settings,
   quit. Check: paused during launch, docking, recall lift and tow: ship and mothership moved 0.00 m in 1.5 s, each
   resumed and finished; View tap pauses (Free Flight and landed in the Campaign), a 2 s hold recalls without
   pausing; simulated focus loss paused (`run4`, `c01`, `c07`-`c09`).
2. **Reset position — done.** Hover 60 m over the nearest landing pad, engines on, same cabin / energy, free;
   greyed out while a pilot runs / knocks / boards and during the automatic parts. Check: from 1802 m up -> 60 m
   over a pad, 0 m from its centre; from 35 m in a canyon -> a pad about 340 m away; refused while the pilot ran (`f04`).
3. **Landing that does what you expect — done.** X from flight = air brakes (`airBrake` 42 m/s²), brake sound,
   BRAKING / HOVER (GND in big type, sink rate) / TOUCHDOWN / LANDED — engines off (A) so the pilot comes, on the
   HUD and the cockpit's centre display; an amber ring + post on the ground where the ship would stop (shown with a
   waiting pilot within 2.5 km); "SLOW DOWN — PRESS X" on the radar (cockpit + HUD) and the advice line when
   braking now stops you near the pilot. Hover sink 30 m/s above 150 m, 14 at 60 m, 3 m/s below 8 m.
   **Before / after:** brake from 450 km/h: hover speed in 8.8 s -> **2.5 s**, stopped in 10.4 s / 651 m ->
   **4.1 s / 196 m**; predicted stop point 0.2-1.0 m from where it stopped (target 50 m). Descent from 1000 m:
   72 s and still 14 m/s at the ground (a hard landing) -> **43 s, touching down at 3.0 m/s** (safe 3.5).
   At a pilot: cue at 224 m (262 km/h), ring 0.1 m from the prediction, stopped 0.6 m from the ring, then set
   down 4 m from the pad centre; states seen: BRAKING -> HOVER -> TOUCHDOWN -> LANDED — engines off (A)… (`c02`-`c06`).
4. **Less clunky flying (first pass) — done; needs Dan's play.** Rates pitch 70 -> 95, roll 130 -> 170, yaw 32
   -> 55 °/s; Settings > Flight assist Full (0.2.3's bank turn 38 / levelling 22 °/s) / **Light** (default, 26 / 8)
   / Off; Settings > Stick sensitivity 50-150 % (100); stick curve .35 (small moves finer, full stick unchanged).
   The HOTAS rows moved to Settings > Controls and HOTAS (a page holds 12 rows). Measured (Ghost, high up, virtual pad,
   0.2.3 -> Light): roll to 60° 0.46 -> **0.35 s**; 90° turn banking and pulling 1.28 -> **1.08 s** (Full 1.02,
   Off 1.25); yaw on LB/RB 32 -> **55 °/s**; nose up 30° 0.43 -> **0.32 s**; 90° turn on a held 60° bank alone
   3.06 -> 4.33 s (Full 3.02, Off never: the assist is lighter by design); wings level from 45° 1.8 -> 5.0 s (Off
   never). No input smoothing existed or was added.
5. **Deeper, more compact canyons — done.** Three canyon systems at closer spacing (more branching), floors
   median about 50 m wide (was about 220 m), walls in five ledges; heights over 12 km -6..584 m (0.2.3: -8..341 m);
   open floor 16 % of the land (was 26 %, measured on a prototype). Landing pads: one per 520 m cell where it falls on
   a canyon floor (306 of 400 cells), a 45 m flat clearing; every pilot's site is on one (0 m from a pad centre),
   the start and Reset too. Same seed = same world (determinism check identical); saved planets regenerate.
   Frame rate flying low (1080p, 1660 Ti): 835 fps, 1 % low 212, worst frame 44 ms (0.2.3: 751 / 185 / 28);
   Steam Deck setting at 1280 x 800: 843 fps, 1 % low 178 (`perf_deck`). Pictures `f05`-`f07`,
   `heightmap_old_vs_new.jpg` (same seed and scale).
6. **Check — done** (above; `run4` is the published build, `run3` gave the same numbers). The full regression
   check (`all/`) passes with the new menu paths. Found on the way: the check's own timings must use the ship's
   capped time step (a slow frame on a busy PC skewed run 2's flight numbers); its pre-existing typed-seed line
   ("expected 1987") reads the same in 0.2.3 (the batch-mode keyboard digit isn't delivered; not a game bug).
   Not re-run this fix: `--only campaign` / `home` (Fix 2.3 features unchanged; the fix24 check covers the landing,
   boarding, docking, recall and tow on the new terrain).
7. **Publish — done.** 0.2.4 = build 204, tag `game-204`, Latest. Check: the `starter-200` ZIP extracted to
   `Builds/StarterCheck`, `--auto-check` against GitHub: "Updating to game 0.2.4", installed `versions/204`,
   started, exit 0 (`launcher/`). TODO / CLAUDE updated, pushed. Clean-up: disk use 2.1 GB -> 1.8 GB (incl. the 0.2 GB starter test install).

**New tuning (`CampaignTuning.json`):** pitchRate 95, rollRate 170, yawRate 55, bankTurnFull 38, levelRateFull 22,
bankTurnLight 26, levelRateLight 8, stickCurve .35, airBrake 42, hoverVerticalAccel 15, hoverSinkHigh 30 (above 150 m),
hoverSinkMid 14 (above 60 m), hoverSinkLow 3 (below 8 m), stopMarkerRange 2500, brakeCueLead 150, resetHeight 60.
**New settings:** Flight assist (Light), Stick sensitivity (100 %), HOTAS pause button (9).

**For Dan to check:** does flying still feel clunky, and if so what exactly (too slow to turn, too twitchy, drifts,
levels itself)? Try Flight assist Light, then Off and Full, and Stick sensitivity. Can you land next to a pilot first
try with the ring (brake when SLOW DOWN shows)? Pause with ☰ and a tap of ⧉; Reset position. Do the canyons feel deep
and tight enough (now perhaps too tight at speed)? HOTAS: which base button should pause?


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
