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
Current Objective — Fix 3.1: real character models (concept art -> Meshy -> Mixamo)
---------------------------------------------------------------------------

Fix 2.6 + Round 3 are delivered as 0.3.0 (status under "Previous deliveries").
Round 4 (Enemies + difficulty) comes after this fix. Still open for Dan from
0.3.0: the route corridor width / height, and a second sign for the Mimic's
tell in shade.

Dan (2026-11-04): the code-made pilot and creature models won't be good
enough; use real models and animation. **Dan's art pipeline (DECIDED, his
standing way of working):** Code writes the image prompts -> Dan has the
concept images drawn -> Code generates the 3D models from them with the Meshy
API (Dan's key) -> Dan uploads them to Mixamo and picks the animations Code
lists -> Code imports and wires them up. This round runs in **stages with
stops for Dan**; Code must stop and say what it needs at each STOP.

**Models this fix:** (1) the **pilot** (rescue-corps flight suit, helmet with a
reflective visor — the Hollow's tell needs the reflection; it is also every
alien's disguise); (2) **five alien reveal forms**, one per type (Mimic,
Hollow, Knocker, Stalker, Sleeper), each with its own silhouette so the types
feel different and the Field Journal can show them. All original designs,
nothing from the 1984 game. Horrible, 2-2.5 m, wrong-jointed but built on a
humanoid skeleton so Mixamo can rig them.

0. **Commit first** (the repo as it is, with this text).
1. **Stage 1 — prompts (Code).** Write `Docs/MODELS-NEEDED.md`: for each model
   a ready-to-paste image prompt for the concept art (full body, **A-pose or
   T-pose, front view, plain light background, no cropping, no props**, so
   Meshy and Mixamo work), a short design note (what makes it scary, which
   tell or behaviour it must support), the file name Dan should save it as
   (`SourceArt/Concepts/<name>.png`), and the Mixamo animation list for stage 4
   (item 4). **STOP: tell Dan the prompts are ready.**
2. **Stage 2 — concept art (Dan).** Dan generates the images and saves them in
   `SourceArt/Concepts/`. Code checks each one is usable (full body, clean
   pose) and says if one needs redoing.
3. **Stage 3 — Meshy (Code).** Image-to-3D with the Meshy API.
   - Key: read from the environment variable `MESHY_API_KEY`; if it isn't
     set, ask Dan to set it. **Never write the key into the repo, a log, a
     report or the chat.**
   - Ask for textured (PBR) output in A/T-pose, FBX (or GLB converted in
     Blender), with triangle budgets: pilot about 15-20k, creatures about
     20-30k; make LODs (about 50 % and 20 %) in Blender for the Deck and VR.
   - Save to `SourceArt/Meshy/<name>/`. Clean up in Blender where needed
     (scale to metres, origin at the feet, facing +Z, symmetric pose).
   - Render a front / side / three-quarter picture of each and look at them.
   - **STOP: show Dan the renders and the files to upload to Mixamo** (the
     exact FBX paths). Redo any Dan rejects (new seed / adjusted prompt).
4. **Stage 4 — Mixamo (Dan).** Dan uploads each FBX, auto-rigs it, and
   downloads the listed animations (FBX for Unity, 30 fps; the first "with
   skin", the rest "without skin"; "In Place" ticked for locomotion) into
   `SourceArt/Mixamo/<name>/`. Code's list per model should name real Mixamo
   searches and say what each is for, at least:
   - **Pilot:** idle (waiting by the wreck, looking around), wave / signal,
     run, slowing to stop, knock on a door (or a punch / tap to adapt), climb
     up / step in, sitting idle (cabin), death / fall, flinch.
   - **Each creature:** idle (breathing, twitching), creepy walk, run /
     lurch, crawl, climb (for the canopy), attack / bite, scream / roar, death,
     plus anything its type needs (Stalker: circling / crouch walk; Knocker:
     pounding; Sleeper: a sitting idle and a sudden lunge).
   **STOP until Dan has downloaded them.**
5. **Stage 5 — into the game (Code).** Import as Humanoid; Animator
   controllers per model; replace the placeholder pilot and creature everywhere
   (pickup, canopy, break-in, horror sequence, cabin, Sleeper fight, security
   kill, Field Journal pictures) with each type's own creature. The alien tells
   must still work (the Mimic's shadow, the Hollow's dead visor, the Stalker's
   path, the Knocker's knock timing on the new knock animation). The disguise
   (pilot) -> reveal (creature) change happens live. LODs on; frame rate on
   Dan's PC and the Deck setting. Keep the code-made models as the fallback if
   a model file is missing.
6. **Check (muted, no focus):** cockpit and landing-view pictures that Code looks
   at and describes: a pilot running and knocking; each creature's reveal, on
   the canopy and in the horror sequence; the Sleeper in the cabin camera; the
   journal pictures; frame rate before / after.
7. **Publish as 0.3.1**, update `TODO.md` (per-item status) and `CLAUDE.md`
   (the art pipeline and folders), commit and push.

**For Dan to check:** do the pilots look and move right; are the creatures
scary up close; can you still spot the tells?

---------------------------------------------------------------------------
Next Steps — queued rounds (Do Not Start Yet)
---------------------------------------------------------------------------

- **Round 4 — Enemies + difficulty:** turrets on ridges, saucers, enemy fire,
  shields and hull, damage; Campaign difficulty presets + custom options.
- **Planet biomes (Dan, 2026-11-03) — placement OPEN (default: right after
  Round 4).** Each planet is one biome, chosen by its seed; the planet's name
  and the star map show it. Dan's ideas: **huge forests** (giant trees, you fly
  through and over the canopy, clearings to land in) and **large cities with
  skyscrapers** (ruined alien cities: the canyons are streets between towers).
  Planning chat's suggestions for Dan to pick from: ice (glacier crevasses,
  blizzards), volcanic (lava rivers, ash, glowing vents), alien fungal / crystal
  forest, ocean archipelago (sea stacks and arches), and the current desert
  canyons. Each biome changes the shapes, materials, sky, weather and where
  pilots can land; same rules everywhere (pads, route trail, radar). Needs a
  performance plan (instancing / LOD for trees and buildings) for the Deck and
  later VR.
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

**Fix 2.6 + Round 3 — radar distances and route trail; aliens (2026-11-04). Published: 0.3.0 (`game-300`, Latest).**
Exe built 2026-11-04 03:19 (`BUILD_INFO.txt`). Evidence `Data/screenshots/fix2.6/final/` and `Data/screenshots/round3/final/` (the
published build; `exe_check.txt` + pictures), `round3/launcher/`; earlier runs' logs in `fix2.6/run1-7`, `round3/run3`. Every run headless
(`-batchmode`, no window) and muted: measured output 0.000 RMS at start and end of every run; no step played sound; no window opened.
New checks: `--only fix26`, `--only aliens`. **Frame rates this round are not usable:** other projects' Unity builds (Chastain Rush, a
football game) ran on the PC the whole time (CPU 80-100 %); before they started the Campaign with the trail measured 227 fps (1 % low 74).

0. **Commit first — done.** "Fix 2.6 + Round 3 planning" (280a997). Part A committed on its own (2ddb323).
1. **Radar distances — done.** True-distance scope that zooms by itself (1 / 2 / 3 / 6 / 9 km at the rim: the smallest holding the
   nearest pilot), range rings with labels, every blip labelled ("P2 2.7 km", "MS 290 m"), the nearest pilot bigger with a halo and
   "NEAREST 2.2 km", the same on the cockpit display and the HUD (`a1_radar_cockpit`). Bearing still rough past 3 km (Round 2's rule).
2. **Route trail — done; following it without a touch: not reached (tuning limit).** A* over a 30 m grid on canyon openness, off the main
   thread (3-90 ms; up to 2.4 s with the PC loaded); chevrons 20 m wide every 100 m, bright to 300 m, fading out by 700 m, ending over the
   pad (green ring); the route on the radar as dots; height 35 m over the floor, raised where the canyon is narrower than ~70 m, gliding
   down from the ship (≤25°) and climbing early before ridges (≤30°). Re-plan: at once on a new target or 150 m off (387 m off → re-planned
   in 9 ms), every 4 s while drifting 60 m off. Target cycling D-pad right / T / HOTAS 11 (auto → pilots by distance → mothership → auto).
   Settings > Route trail (On). Check from Reset position at the start pad: route 3.2 km through 12 bends, never under 26 m over the
   ground; the check's pilot (160 km/h) followed it to 260 m from the pad with **2 wall touches** (runs: 2, -, 15, 11, 3, 4, 2), each when it
   strayed 13-63 m off the line; chevrons in the cockpit view at the three pictures 4 / 6, 0 / 6 (behind a rise), 6 / 7 (`a3-a5`). **Dan decides:** wider corridor / higher line?
3. **Check — done** (`fix26/final`), frame rate: see above.
B1. **Which pilots are aliens — done.** `Aliens.Assign` from the planet's seed (same seed = same list, checked on 3 seeds); none on a new
   career's first planet (checked); then max(1, round(share × pilots)), share 20 % + 5 % a tier (max 35 %); every type; 400 sample planets:
   33 % aliens at 6 pilots, tells shown 74 %, types 147-188 each. Planets saved before 0.3.0 get theirs on the next visit.
B2. **Models — done (placeholder art).** Aliens wear the pilot suit; the creature (`Tools/Blender/creature.py`, 2.2 m, 8000 triangles: reversed
   knees, arms past the knees, four-claw hands, ribs, an eyeless long skull with milky eye clusters, a jaw that splits) posed in code
   (`Campaign/Creature.cs`: crawl, canopy, lunge, thrash, dead; jerky head, breathing). Close up: `b_04_lunge`, `b1_canopy_pressed_to_the_glass`.
B3. **Tells — done.** Mimic: the figure casts no shadow, a taller long-armed shadow follows 3.3 m behind; Hollow: matte black visor, no
   glint (real visors: blue sheen + glint; `t2_hollow_tell_airlock_cam` vs `t2_hollow_hidden_airlock_cam`); Knocker: four knocks (or the
   wrong rhythm / from the nose, by pilot); Stalker: circles 420° at 18 m, off the radar 10 s, comes in from behind. Shown 75 % (seeded),
   false tells 0 %. **Fixed on the way:** pilots waited under the left wing (since 0.2.5): head and visor hidden from the airlock camera:
   they now wait ahead of the wing; the airlock camera moved and narrowed (42°) so the figure fills it; between knocks they look into it.
   **Mimic: not seen in any check picture**: the check's pad lies in the canyon wall's shade (no shadows at all there,
   `t1_mimic_tell_coming_landing_view`); the tell only shows on sunlit ground. **Dan decides:** add a second sign (e.g. its own lamp-cast shadow)?
B4. **At the door — done.** Engines: an alien shrieks, rears up as the creature and dies; +200 cr bounty; journal; a real pilot: -400 (as before).
B5. **Ignored → canopy → break-in — done.** Climbed 27.3 s after reaching the door (25 + the 2.4 s climb, live), pressed on the glass
   straight ahead (scrape, breath, taps); broke in 10.0 s later; engines while it is up there: thrown off, burnt, +200 cr.
B6. **Real pilots — done.** Never climb; keep knocking every 7 s, the Advice line keeps saying so.
B7. **Horror sequence — done.** Airlock shuts (0.8 s), lights die and every cockpit display goes dark (2.0), alarm, something moving in the
   cabin (3.4-9.4), three blows on the hatch behind the left shoulder, it gives way (13.2), it comes round the seat to your face, the jaw
   opens, lunge (17.4), death: "Killed by the thing you let aboard: a Mimic", the ship lost with it, regeneration bay, loaner / recovery.
   Canopy break-in: crack, glass gone, lunge (6.5 s). Live: camera never moved by the game (fastest 0-1 m/s), look works (turned 102-111°
   back to the hatch), no fade, no strobe (a slow red pulse), view left as the player had it.
B8. **Sleeper — done.** Seeded turn 40-120 s of flight (this one 89 s; the check shortened it). 3 others aboard: it killed one, the two
   left killed it (cabin 4 → 2); alone: it came for the cockpit (death in flight: the ship drifts to a hover); docked first: security in
   the bay, "Security killed a Sleeper in your cabin", the other pilot paid. Tell (75 %): wet clicking and a looping voice in the cabin, its
   head turned too far toward the cabin camera.
B9. **Cabin camera — done.** A cabin behind the cockpit (benches, lamps, passengers; the hatch cut in `ship.py`), its camera on a small
   cockpit display (left of the dash) and on the HUD with D-pad up / N / HOTAS 12; dark when the lights die.
B10. **Field Journal — done.** Hangar and pause menu (last row); entry + vague hint at the first reveal, clearer at the second (all five
   checked: `j1_field_journal`); saved in `career.json` (`journal`, `aliensKilled`).
B11. **Sounds — done with ElevenLabs** (key from `ELEVENLABS_API_KEY`): 18 sounds in `Assets/Resources/Sounds/`, prompts in `Docs/SOUNDS.md`,
   `python Tools/make_sounds.py [--only x --force]`; code placeholder if a file is missing. **Not heard by anyone yet** (muted rule):
   the check logs each one started (`sounds that would have played`).
B12. **Check — done** (`--only aliens`, about 35 min, every item above). Not re-run this round: `all` / `campaign` / `home` / `fix24` / `fix25`.
Publish: 0.3.0 = build 300, tag `game-300`, Latest; the `starter-200` ZIP from GitHub, `--auto-check`: "Updating to game 0.3.0",
installed `versions/300`, started, exit 0. Clean-up: disk use 2.1 GB -> 1.6 GB (starter test install, 0.2.1-0.2.5 release folders and check runs removed).

**New tuning (`CampaignTuning.json`):** alienShareStart .20, alienSharePerTier .05, alienShareMax .35, tellReliability .75,
falseTellChance 0, alienBounty 200, canopyClimbAfter 25, breakInAfter 10, sleeperTurnMin 40, sleeperTurnMax 120, sleeperPilotsWin 2.
**New settings / controls:** Settings > Route trail (On); RouteTarget D-pad right / T / HOTAS 11; CabinCam D-pad up / N / HOTAS 12.

**For Dan to check:** radar distances and the route trail (is it followable at speed? wider / higher?); the aliens: can you learn the
tells by playing, do they still fool you, is the canopy and the horror sequence scary, does the Sleeper's cabin fight work? **Listen to
the sounds** (never played on your PC by me). Frame rate on your PC and the Deck (not measurable this round).

**Fix 2.5 — tight control, seeing the ground, the pickup, launch warning, faster lift (2026-11-02). Published: 0.2.5
(`game-205`, Latest).** Exe built 2026-11-02 20:46 (`BUILD_INFO.txt`). Evidence `Data/screenshots/fix2.5/`: `before/` (0.2.4's
flight code with the new measuring, `--only fix25`), `final_d/` (the 20:29 build: everything below), `fastforward/` (20:46, the published
build: hold-A fast-forward), `final_c/` (lift times with 0.2.4's numbers), `launcher/`. Every run headless (`-batchmode`, no window) and
muted: measured output 0.000 RMS at start and end of every run; no step played sound; no window opened. New check: `--only fix25`.

0. **Commit first — done.** "Fix 2.5 planning" (9daeafb).
1. **Tight, predictable control — done; needs Dan's play.** Measured first (`ShipController.Moved`: rotation by source; Light, stick
   centred). **What moved the ship without input (0.2.4):** (1) the bank-turn assist: a bank left alone kept turning the ship: 15° bank
   +6° heading in 10 s, 30° +25°, 60° **+93°**; a 0.3 s roll then release **+66° in 5 s**; (2) past 80° of bank the wing levelling
   switched off, so the bank turn ran on forever: full roll with 20 % pitch leak **+77° in 3 s**; (3) wing levelling itself (8 °/s,
   uncommanded roll); (4) wall hits mirrored the flight path off the rock ×1.35: a 15° hit turned the nose **43.5°**, 35° → 51°;
   (5) hover: the body eased after the stick, so it kept turning **22.5°** after release; (6) braking into hover from a banked climb
   eases the body level (33° over 4 s: kept, it is the landing mode's level attitude). Not sources: level flight 0.0°, stick drift up
   to 12 % (eaten by the pad's own 12.5 % deadzone + the game's 8 %), no side slide in flight (the path is always the nose).
   **Fixes:** Light = the bank turn only while you pull, no levelling (stick centred holds the attitude); hover heading follows the stick
   at once (only the lean eases); a wall hit pushes the ship away and turns the nose only to run along the rock; rates 95/170/55 →
   **82/150/44 °/s** (midway to 0.2.3's 70/130/32), stick curve .35 → **.6**, pad / Deck sticks +12 % centre deadzone (`gamepadDeadzone`).
   Full and Off kept. **After (Light):** every stick-centred case **0.0°** (15/30/60° bank, roll release, diagonal leak); hover after
   release **0.0°**; 25 % roll for 1 s 9.2° → 2.5° of bank; wall hits: nose 31° at a 15° hit, 36.5° at 35° (= the rock's own line),
   pushed 20-46 m clear. On Light you now bank **and pull** to turn (Full turns by itself as before).
2. **See the ground when landing — done.** Belly camera (`Cameras/BellyCam.cs`, drawn by hand each frame): on by itself in landing
   mode, D-pad down / V / HOTAS 8 toggles it; fills the cockpit's centre display with height, sink rate and speed over it, and a
   picture-in-picture on the HUD (`p1-3_hover_belly_cam`); landed with a pilot coming it is the **airlock camera** under the left wing
   (`p1_at_door_cockpit`: the pilot at the door). The amber stop ring also shows in hover, a green ring marks the waiting pilot's pad.
   Glass floor at the feet and glass in the lower canopy (`ship.py`): first try showed grey — the fuselage's top skin runs across the
   cockpit at the sill, so the window is cut through the whole hull (`p1_glass_floor_look_down`: nose gear and ground below; the ground
   is dark there, in the ship's own shadow). Landing view = 5th in the camera cycle (`p1_landing_view`, `p1_at_door_landing_view`).
   Frame rate landed with the airlock camera on: 284-368 fps (1660 Ti, 1080p).
3. **The pickup — done.** **Cause, measured as a player lands:** the SLOW DOWN cue and ring stopped the ship **151 m short** of the
   pilot; in the 0.2.4 canyons that is on the canyon wall: the ship hovered **530 m above** the pilot, "too steep to land (14°)", and
   with Lasting damage the tries to set down there destroyed it. The pickup range was a 3-D 160 m, so a pilot on the floor below never
   came, with no message why. In range, the pilot ran a straight 3-D line (crawling up slopes) **through the hull** to the door on the
   left rear, where the cockpit can't see it; the knock was a 0.22 s, mostly **190 Hz** thud (small speakers like the Deck's hardly
   play it) and the only prompt was the advice line, replaced for 4 s by any passing message. Dan's exact run wasn't reproduced; these are
   what the check found. **Fixes:** cue and ring aim at the pilot's pad (`brakeCueLead` 150 → 40); a pilot comes within 160 m level and
   30 m of height (`pilotMaxClimb`), else the advice says "Pilot 2 can't reach you here (61 m away, 0 m down): land on their pad (green
   ring)" (`p3_cant_reach_advice`); they run over the ground round the nose or tail to the door; "PILOT AT AIRLOCK — press B to open"
   (the button of the device in use) / "PILOT COMING — keep engines off" on the HUD and in the cockpit, beating passing messages; a lit
   amber AIRLOCK strip on the centre display; KNOCK KNOCK KNOCK on the HUD and displays with each knock (`p1_knock_knock`); a brighter,
   louder knock. **After:** three pads, cue → stopped 40-41 m from the pad centre, set straight down (slope 0-9°), pilots ran 9.1 /
   12.8 / 11.0 s, knocked, B, aboard. Knock source logged playing at full level at the cockpit (4.3 m); players' builds aren't muted
   (`TestRun.Muted` only with `-mute` / `-exeCheck` / `-racerTestSave`).
4. **Low-fuel warning — done.** Launch below 50 % (`launchEnergyWarning`): "Energy 22 %. Refuel first? (cost 468 credits)" with
   **Refuel and launch** (default) / Launch anyway / Back; it refuelled to 100 % (-468 cr) and launched (`h02`); the hangar shows a
   refuel line under 100 % (`h01`).
5. **Much quicker lift — done (docking above target; Dan's fast-forward added).** Lift from the ground into the cradle: recall
   **54.4 → 10.0 s**, mid-air catch 46.6 → **9.1 s**, tow ~60 s (0.2.4 log) → **10.1 s**; the mothership comes 3x faster (tow: 6.4 s).
   Live and continuous (camera never turned or jumped; top 320 m/s). Docking ~40-50 s (0.2.4 log) → 22.0 s (attempt 1) → **19.5 s**
   (attempt 2): still over 15 s, tuning stopped there. **Dan (chat): "cant we just do a fast dock? Maybe button to skip"** → hold A /
   Space / HOTAS trigger in any automatic part: it plays live at 4x (`fastForward`), prompt "hold A to speed up"; launch 6 s → 2.1 s
   real, recall lift 10.1 → 2.5 s real, back to normal speed in flight (`fastforward/`). Tuning values in the file.
6. **Check — done** (`--only fix25`, list above). The check's own pilot flies its transit legs on Flight assist Full and between pilots
   is placed 1.5 km out (getting about isn't measured); its steering got firmer for the new deadzone. Not re-run: `all` / `campaign` /
   `home` / `fix24` (their steering was updated for Light; the fix25 run covers launch, landing, pickup, docking, recall, tow, mid-air).
7. **Publish — done.** 0.2.5 = build 205, tag `game-205`, Latest. The `starter-200` ZIP in `Builds/StarterCheck`, `--auto-check`
   against GitHub: "Updating to game 0.2.5", installed `versions/205`, started, exit 0 (`launcher/`). TODO / CLAUDE updated, pushed.
   Clean-up: disk use 2.1 GB -> 1.8 GB (intermediate check runs and the 0.2 GB starter test install removed).

**For Dan to check:** does the ship go where you point it (Light: bank and pull to turn)? Is 82/150/44 and the bigger pad deadzone
right, or too sluggish? Land with the belly camera / glass floor (look down with the right stick). Does the pilot knock and is the
airlock prompt clear; can you hear the knock on the Deck? Launch with low fuel; the lift; hold A during docking. HOTAS when you can
(button 8 = belly camera).

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
