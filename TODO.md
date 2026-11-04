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
Current Objective — Round 4 (Enemies + difficulty): to be written by the planning chat
---------------------------------------------------------------------------

Fix 3.2 (always another planet to fly) is delivered as 0.3.2 (status under "Previous deliveries"; its specification is in
`TODO-ARCHIVE.md`). **Dan's art pipeline (DECIDED, his standing way of working):** Code writes the image prompts -> Dan has the
concept images drawn -> Code makes the 3D models with the Meshy API (Dan's key) -> Dan rigs them in Mixamo and downloads the
animations Code lists -> Code imports and wires them up; in stages with a STOP for Dan at each. How-to: `Docs/MODELS-NEEDED.md`.
Still open for Dan from 0.3.0: the route corridor width / height, and a second sign for the Mimic's tell in shade.
**Planets (Fix 3.2, planning chat default, Dan to confirm by playing):** the home system has as many planets as you want; the seed
of the day stays the featured "planet of the day" (pilots +50 %); Jump to a new planet from the hangar (free), a Planets list.

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
  objectives, the full star map (builds on Fix 3.2's planet list; typed seed;
  maybe new distress calls appearing on visited planets over time), recovering
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

**Fix 3.2 — always another planet to fly (2026-11-04). Published: 0.3.2 (`game-302`, Latest).** Exe built 2026-11-04 11:15
(`BUILD_INFO.txt`). Evidence `Data/screenshots/round3_2/final/` (`--only fix32`: `exe_check.txt` + pictures a0-a5), headless, muted
(output level 0.000 RMS from the first frame to the end), no window.
0. **Commit first — done** (`77aae7f` "Fix 3.2 planning").
1. **New planets on demand — done.** Hangar row 2 **Jump to a new planet (free)**: the next planet of the home system from the
   career's seed + a counter (`Career.NextPlanetSeed`, seeds 1 - 19 999 999 so never a date), own name, tier = planets cleared, as
   before. Works cleared or not (pilots left behind stay). The jump plays in the bay with the doors shut (no cut, no fade: about
   0.4 s while the new canyons load; doors opened 0.00). Tuning `jumpCost` 0. Check: Zanveth-IV (today) -> Vekir-VI (tier 1, 4 pilots,
   one Mimic), a different name and pilots (a3).
2. **Planet list — done.** Hangar **Planets**: New planet (names the next one), today's planet first (★ of the day, even before its
   first visit), then every planet visited, newest first (9 a page, "More planets…"), each with waiting / cleared and "◄ here"; the
   selected one in full on the right (biome "canyons", rescued / lost / waiting, visits). Pick one: the mothership jumps there; Launch
   from the hangar. Saved with the career (`currentSeed`, `planetsMade`, each planet's `biome`, `lastVisit`). Check (a4): the list,
   back to today's planet, back to Vekir-VI: its 3 waiting pilots still there (fates the same).
3. **Planet-of-the-day bonus — done.** Tuning `dayBonus` .5: 750 cr a pilot on today's planet; the hangar says so (and, elsewhere,
   which planet is today's). Check: 3 pilots on Zanveth-IV paid +2250 (750 of it the bonus); 1 on Vekir-VI paid 500. Continue: a new
   day's planet of the day comes first (once), otherwise you arrive where you left off.
4. **Cleared — done.** The debrief says "**Zanveth-IV cleared! +1000 credits**", the hangar "Cleared! Jump to a new planet
   (selected)" and the jump row is the default (a1), on every return to a cleared planet too.
5. **Check — done** (`Checks/ExeCheck.Fix32.cs`, `--only fix32`, about 5 min): one pilot per sortie picked up as a player does (the
   run, the knock, B; the check puts the ship 40 m over the pad and climbs straight up in landing mode), two more on today's planet
   put aboard by the check (logged shortcuts). Same system seed in a second career -> the same second planet (seed, name, tier,
   pilots, aliens, first pad: identical); quit + Continue: same planet, credits and fates. The first run's scripted climb flew into a
   canyon wall (check pilot, not the game); it also showed a hidden list row could be pressed by script: guarded.
   Older checks now pick hangar rows by label (the hangar has 2 new rows; Field Journal before Back to title).
6. **Publish — done.** 0.3.2 = build 302, tag `game-302`, Latest; the `starter-200` ZIP from GitHub, `--auto-check`: "Updating to game
   0.3.2", installed `versions/302`, started, ready, exit 0 (the game muted by the launcher's test-save flag). Clean-up: repo 3.3 GB ->
   2.9 GB (starter test install, the 0.3.1 release folder, the failed first check run removed).

**For Dan to check:** after clearing a planet, is jumping to a new one obvious and quick? Is the planet list clear? Is +50 % on the
planet of the day enough to make it special?

**Fix 3.1 — real character models (2026-11-04). Published: 0.3.1 (`game-301`, Latest).** Exe built 2026-11-04 07:15
(`BUILD_INFO.txt`). Evidence `Data/screenshots/round3_1/`: `final/` (`--only fix31`, the 06:58 build: every alien scene + close-ups),
`perf_pc/` + `perf_deck/` (`--only fix31perf`, the published 07:15 build, which differs from 06:58 only in the journal layout and check
code), `horror3/` (the culling bug found and fixed), `launcher/`. Every run headless (`-batchmode`, no window) and muted: output 0.000 RMS
at start and end of every run; no step played sound. New checks: `--only fix31` (about 15 min), `fix31h` (the airlock horror only),
`fix31perf` (journal + frame rates).

0. **Commit first — done.** "Fix 3.1 planning" (f1f2930); each stage committed on its own.
1. **Stage 1, prompts — done.** `Docs/MODELS-NEEDED.md`: six prompts, design notes, file names, the Mixamo list. The Sleeper's first
   prompt was refused by Dan's image tool (violence filter): rewritten with the seam closed.
2. **Stage 2, concept art (Dan) — done.** Six images in `SourceArt/Concepts/`, all usable (full body, A-pose, plain background).
3. **Stage 3, Meshy — done.** `Tools/meshy_generate.py` (key from `MESHY_API_KEY`, never written anywhere; 30 credits each, 180 in all),
   `Tools/Blender/meshy_cleanup.py` (metres, feet at 0, front, renders, `<name>_mixamo.fbx`). Pilot 18.7k triangles, creatures 25.9-29.2k.
   **LODs: not made in Blender** — Unity 6.6's own Mesh LOD on import instead (4 levels, keeps the skinning; reported per model).
4. **Stage 4, Mixamo (Dan) — done, with substitutes.** Dan: each clip downloaded once (one skeleton for all; Humanoid retargets it).
   No Mixamo knock: "Opening" (its first 2.5 s per set of knocks); flinch = Zombie Reaction Hit; pilot run / climb / death from the
   creature set; no bite (attack), no Knocker pound (attack), no Sleeper twitch / lunge (its tell stays the head turn; roar / attack).
5. **Stage 5, into the game — done.** `Tools/import_characters.py` -> `Assets/Resources/Characters/`; `Assets/Editor/CharacterImport.cs`
   (Humanoid, loops, Mesh LOD, materials as assets made at build time); `Campaign/CharacterRig.cs` (clips by name, crossfades). Pilots wave
   as the ship lands, look round, run, stop, knock, climb in, flinch + die; every type its own creature (reveal, canopy climb — the Mimic
   leaps, canopy, cabin crawl — the Stalker crouch-walks, hatch, lunge, death); seated pilots in the cabin; journal pictures. Tells:
   Hollow's visor a dead black (pilot visor pixels found in the texture: `pilot_mask_hollow`), real visor a mirror; the Mimic's shadow is
   the Mimic itself (shadow-only, 3.3 m behind); Stalker 420° round, off radar 10 s, side-steps crouched facing the ship; Knocker's
   knocks unchanged (3 / 4 at 0.34 s). Code-made figures stay as the fallback (`CharacterRig.Disabled` in checks). The Sleeper's seam
   can't open (no jaw bone in Mixamo's rig). **Fixed on the way:** Animator culling left the creature undrawn right in front of the eye
   (the lunge was empty): characters always animate.
6. **Check — done.** Pictures looked at: pilots at the airlock camera, the Hollow's visor beside a real one, each creature idle + roar up
   close (`m_*`), the canopy, three lunges (`b_04`, `h_07`, `s2_06`), the cabin camera (Sleeper's tell, the kill), the journal (`j2`).
   Frame rate landed (pilot coming, 4 in the cabin, cabin camera): models 423 fps / old figures 434 at 1080p; Deck setting 1280 x 800:
   456 / 444. Flying low: 310 (1080p), 321 (Deck setting). The PC was otherwise idle except Dan's open Chastain Rush editor.
7. **Publish — done.** 0.3.1 = build 301, tag `game-301`, Latest; the `starter-200` ZIP from GitHub, `--auto-check`: "Updating to game
   0.3.1", installed `versions/301`, started, ready, exit 0. Clean-up: disk use 3.0 GB -> 2.4 GB (starter test install, the 0.3.0 release
   folder and debug check runs removed); `SourceArt/` grew by ~0.45 GB (Meshy + Mixamo files; `.blend` and Meshy's raw FBX not in git).

**For Dan to check:** do the pilots look and move right (the knock is "Opening": good enough?); are the creatures scary up close (the
lunge comes side-on, an arm reaching past); can you still spot the tells (the real visor's reflection is soft; is the difference enough?).
Field Journal pictures. HOTAS / Deck as always.

**Fix 2.6 + Round 3 — radar distances and route trail; aliens (2026-11-04). Published: 0.3.0 (`game-300`).**
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
