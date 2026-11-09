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
Current Objective — Round 9: Fix 8.3 (A), missions (B), own wreck (C), insurance (D), best runs (E), time trial (F), harder enemies on Hard / Nightmare (G)
---------------------------------------------------------------------------

Fix 8.2 is delivered as 0.8.2 (status under "Previous deliveries"). Round 9
(missions and more progression) comes after this fix.

Dan (2026-11-08):
- "Both holes in the mothership have jaggies."
- "My ship has a black / grey box in the rear which I don't believe belongs
  there."

0. **Commit first** (the repo as it is, with this text; the untracked
   `Data/screenshots/fix82/` files with it).
1. **Clean mothership openings.** Both openings in the mothership's hull (the
   bay cut out of the Meshy hull, Round 8 B1) have jagged edges. Cut them
   cleanly in Blender (a smooth outline) and give each a proper frame / lip
   (hull plating turning inward, trim, lights along the edge) so they look
   built, not cut. Check the rest of the mothership from outside and from the
   bay for other jagged cuts. **Check:** pictures of both openings from the
   approach, during docking and from inside the bay, before / after.
2. **The grey box at the back of the ship.** Find what it is (a leftover
   collision / placeholder box, a Round 1-7 part such as the old airlock,
   cabin or engine block showing through the new Meshy hull, a camera / light
   helper, the cabin room poking out). Remove it or hide it inside the hull;
   if it is something the game needs (the cabin room, a collider), keep it
   invisible from outside. Check the whole ship from every chase / landing view
   for any other leftover old parts. **Check:** pictures of the ship's rear
   from the chase view, landing view and the mothership bay, before / after;
   say what the box was.
3. **Publish as 0.8.3**, update `TODO.md` (per-item status, what the box was)
   and `CLAUDE.md`, commit and push.

2b. **Dan's Steam library artwork and the launcher cover** (Dan, 2026-11-08:
   "I added these three which I want part of the file so that I can update
   Steam Deck. I also want the launcher to use the portrait version.")
   Dan's images are in **`SourceArt/Title/SteamDeck/`** (where he saved them):
   `hostile-horizon-portrait-600x900.png`, `hostile-horizon-landscape-920x430.png`,
   `hostile-horizon-logo.png` (opaque dark-teal background), plus the full-size
   masters and `generation-notes.txt`. Use these originals.
   `SourceArt/Title/Dan/` holds re-saved copies the planning chat made from the
   chat upload: **delete that folder** (duplicates).
   - **Launcher cover = the portrait** (`hostile-horizon-portrait-600x900.png`) in place
     of the old `HostileHorizonCover.png`; check it fits the launcher window
     without stretching or cropping the title.
   - **The starter package and the game folder ship all Steam artwork** beside
     the launcher, named for what Steam calls them: `HostileHorizon-Portrait.png`
     (library capsule / grid), `HostileHorizon-Wide.png` (wide capsule / header),
     `HostileHorizon-Hero.png` (the library's big banner: make it 1920 x 620
     from `SteamDeck/hostile-horizon-landscape-master.png` (full resolution),
     no title text if a clean crop allows, so the logo can sit on it),
     `HostileHorizon-Logo.png` (make a **transparent** version of Dan's logo by
     removing the dark-teal background with clean edges), and the existing
     icon. The old poster is replaced by the portrait.
   - Since existing installs update through the launcher, also put the artwork
     where an existing install gets it (the launcher's top folder on the next
     update), so Dan doesn't need a fresh install to get the files on the Deck.
   - **README-LAUNCHER.md** (Steam Deck section): how to set each one in Steam
     (Desktop Mode: right-click the game → Manage → Set custom artwork for the
     portrait; the hero, logo and wide image through the library page's
     right-click / Properties options as Steam names them), and which file goes
     where. Tell Dan the steps in the status too.
   - **Check:** the launcher's window with the new cover (picture); the
     transparent logo over the hero (picture); the files present in the starter
     ZIP and in an updated install's top folder.

**For Dan to check:** the mothership openings while docking; the ship's rear in
the chase view. The launcher's portrait cover; set the Steam artwork on the Deck
from the files beside the launcher.

**Dan (2026-11-08): fold Round 9 and the time trial into this round, and put the
harder enemies on the harder difficulty settings.** So this round is **Round 9**:
Part A = Fix 8.3 (items 0-2 above; commit), Parts B-G below (commit after each).
One publish at the end: **0.9.0** (item 3 above is replaced by the end of the
round below).

**Part B — side missions**
B1. Each planet gets 1-3 optional **objectives**, chosen by its seed (planet of
    the day included), listed in the hangar and on the star map, announced by
    Control at launch, tracked on a cockpit display and the HUD:
    - **Recover the flight recorder:** land at a marked crash site and hold the
      airlock button for about 8 s to pull it aboard (engines off, like a
      pickup; anything nearby can still come for you).
    - **Knock out the turret cluster:** 3-5 turrets guarding one valley.
    - **Timed rescue:** one pilot's life support runs out (a countdown shown
      on the radar); rescue and dock before it hits zero.
    - **Clean run:** clear the planet without hull damage.
B2. Rewards in credits (tuning), paid at docking; completion shown on the star
    map. Failing one costs nothing except the reward (the timed pilot is lost).
B3. Voice lines (Control briefings, completions) with ElevenLabs.

**Part C — recover your own wreck**
C1. After a death, a third choice next to Pay recovery / Take the loaner:
    **Recover it yourself** (free): you launch in the loaner; the wreck is
    marked on the radar and the route trail; land at it, engines off, hold the
    airlock button for about 12 s to salvage it. Danger as anywhere on the
    planet (aliens, turrets, saucers), plus a chance something is waiting at the
    wreck (tuning). Success: at the next dock your own ship (with its upgrades)
    is back and the loaner returned. Dying on the way: the wreck stays where it
    is (pay or try again).

**Part D — insurance**
D1. In the hangar before launch: **Insure this flight** for a fee (a share of the
    ship's value, tuning). If you die on that flight, recovery costs much less
    (tuning, starting 20 % of the normal fee). Shown in the hangar and on the
    death screen. Pilots aboard are never covered.

**Part E — best runs**
E1. Per planet: fastest clear, most credits earned, fewest hull hits, cleared
    without dying; shown on the star map, with the planet of the day's best
    picked out. Career stats get the totals.

**Part F — Free Flight time trial** (decided earlier)
F1. In Free Flight: **Time trial** mode: a course of glowing gates through the
    canyons / forest / city / ice / lava / crystals / islands, generated from
    the seed so the same seed is the same course; a timer, split times, missed
    gates cost time, a best time per seed and biome.
F2. A **ghost** of your best run you can race (a translucent ship), on by
    default, switchable.
F3. Course length about 2-3 minutes; the crash level setting applies.

**Part G — tougher enemies on the harder settings** (the deferred "more of a
threat", Dan: "put [it] on a harder mode")
G1. **Easy and Normal stay exactly as now.**
G2. **Hard:** turrets lead their shots and hit a ship crossing at mid range
    some of the time (target: about 1 shot in 6 at 650 m on a straight
    crossing); saucers actively hunt (they search for the ship, not just react
    when they see it); more turrets. **Nightmare:** more again (about 1 in 4,
    faster fire, more saucers that work in pairs). All numbers in the tuning
    file (`turretLead`, `turretAimError`, `boltHitRadius` per preset);
    Custom's aggression slider covers the same range.
G3. Fairness: the warnings (incoming-fire markers, LOCKED ON) and dodgeable
    bolts stay; pads stay out of sight on Hard where Round 5 said so.

**Check (muted, no focus, per the testing rules):** each part in the built exe:
a side mission of each kind completed and paid; the timed pilot lost when the
clock runs out; a wreck recovered yourself (and a death on the way); insurance
cutting the fee; best runs recorded; a time trial course run twice (same course,
ghost, best time); Hard / Nightmare hit rates measured against the targets
(two tuning attempts at most, then report); Normal unchanged (same numbers as
Round 5).

**End of the round:** publish **0.9.0**, update `TODO.md` (per-item status for
Fix 8.3 and B1-G3: done / not done / deferred and why; what the grey box was;
tuning values) and `CLAUDE.md`, clean up, commit and push.

**For Dan to check (Parts B-G):** the side missions; recovering your own wreck;
insurance; the star map's best runs; a time trial with the ghost; Hard and
Nightmare enemies.


---------------------------------------------------------------
Next Steps — queued rounds (Do Not Start Yet)
---------------------------------------------------------------------------

Nothing queued after Round 9 yet: Dan and the planning chat decide what comes next
(ideas so far, not decided: native Quest, more biomes, more alien types).

---------------------------------------------------------------------------
Previous deliveries
---------------------------------------------------------------------------

**Fix 8.2 — relaxed hands, cockpit fixes, docking pilots, menus in VR (2026-11-08). Published: 0.8.2 (`game-802`, Latest; soundtrack
stays `soundtrack-700`).** Exe built 2026-11-08 20:46. Evidence `Data/screenshots/fix82/`: `flat_3/` and `vr_3/` (final; `vr_2/` = the
same build bar a check-order fix), `flat_1`, `flat_2`, `vr_1` (earlier tries). New check `-exeCheckOnly fix82` (`Checks/ExeCheck.Fix82.cs`;
`local/fix82_runs.sh flat|vr <name>`). Every run headless, muted (output 0.000 RMS), no window, Below Normal, one at a time; VR on the mock
runtime with both real eyes compared (28 / 28 pairs drawn in both eyes).
- **0 Commit first — done** (`af04da7`, with the untracked `fix81/vr_5` files).
- **1 Relaxed hands — done. Causes found** (0.8.1's pose kept as `PilotBody.CheckOldPose`, the same flying logged every frame,
  `hands_old.csv` / `hands_new.csv`): (a) the chest and neck turns were added onto the previous frame's pose (the animator is off, nothing
  reset the bones), so any head turn twisted the chest further every frame, up to 180 degrees, swinging the shoulders up to 364-403 mm and
  the arms with them; the hands slid 149-249 mm on the stick / throttle; (b) the torso followed the head camera (VR: the headset), a frame
  late, switching to the seat past .35 m. (Fingers were measured steady in both: not a cause.) Fix: every bone starts each frame from the
  rest pose, the body stays in the seat, no chest / neck turn, the drawn stick and lever smoothed (~55 ms). **Check:** 60 s of flying (stick
  sweeps, throttle up / down, looking round, a hover): hands within 0.3-0.6 mm of their place on their controls while the grip itself moved
  40-46 mm; shoulders 0 mm; pictures `f82_hands_new_1..3`.
- **2 Throttle level with the stick — done.** `ship.py` puts the throttle's middle at the stick's depth (z 2.535; 0.8.1: 17.8 cm behind);
  `ShipLayout.StickPivot / ThrottlePivot`. Check: depth difference 0.0 cm, shoulder to hand 0.38 / 0.35 m (`f82_hands_rest`, `f82_throttle_left`).
- **3 Pilots aboard after docking — done. Cause:** `DebriefAtDock` cleared the cabin list but never redrew the cabin, so the delivered
  pilots sat on in the cabin (and the cabin camera stayed on) through the whole hangar; the next launch emptied it. Fix: at the dock they
  leave the ship (cabin redrawn, camera off, chatter reset; `[Play] docked: N pilots delivered (...): they leave the ship`). Check (0.8.1's as
  `GameShell.CheckOldDock`): old: hangar count 0 but 2 figures, camera on; new: 0 figures, camera off, also after re-launch
  (`f82_cabin_*_hangar / _relaunch.png`). Walking off in the bay not done (simply gone, as the item allows).
- **4 VR menus — done. Cause:** the board was 1.4 m out, beyond the dash (0.55 m) and among the canopy frame (1.2-1.6 m), depth-tested,
  so the cockpit cut through it: 3 % hidden looking ahead, **98 % looking down** (the board's backdrop painted one colour, pixels counted
  in both eyes). Now 1.0 m out (same 51 degrees wide), opened where you look (35 down / 25 up at most), near-solid backdrop, drawn over
  everything (`Shaders/UIOnTop.shader`, stereo-safe). Check: hangar, star map, pause, Settings, the Journal with its 3D alien, looking
  down: 0 % hidden (`vr_3/f82_vr_*_eyes.png`).
- **5 Field Journal left / right — done.** A page-wide `MenuPanel.Page.Side`: left / right on any row of a kind's page = previous / next
  kind (unmet = Unknown), "◀ Sleeper | Unknown ▶" under the title; turning moves to inputs that don't navigate (pad right stick, mouse
  drag, HOTAS twist), zoom unchanged; Back to the list. Check: D-pad right / left through the kinds, the view still turns 66 degrees with
  the right stick, 0 with the left; B returns to the list.
- **6 White right console — done, no new model.** Cause: both consoles were joined into one mesh with two Blender materials
  (`mx_cockpit_console.001`); the game filled only the first slot, so the right one kept the importer's plain white. `ship.py` reuses the
  material; the game fills every slot. Every other cockpit part has one textured slot (checked) (`f82_console_right`).
- **7 Jagged piece at the windshield's top left — done.** A long sliver of the left pane (lum .081, just over the .07 glass threshold) stayed
  hull; `ship.py` now makes glass of faces wholly on a pane and nearly as dark (4 faces). Canopy pictures from the eye: frame clean
  (`f82_ahead`, `f82_windshield_top_left`, `f82_canopy_*`). Residual seen (in 0.8.0 too): a small dark triangle in the ceiling grab-handle recess.
- **8 Check — done** (above). **9 Published 0.8.2 — done**; CLAUDE.md updated. Clean-up: 7.8 GB -> 7.8 GB.

**For Dan to check (Quest 3):** hands calm on the stick and throttle while you look round and lean; the throttle level with the stick;
dock with pilots and look back: cabin empty; the Journal readable in VR (and when looking down) and left / right between aliens; the right
console; the windshield's top left; the ceiling handle recess.

**Fix 8.1 — VR right eye, the pilot's body and hands, a real floor window (2026-11-08). Published: 0.8.1 (`game-801`, Latest;
soundtrack stays `soundtrack-700`).** Exe built 2026-11-08 19:25. Evidence `Data/screenshots/fix81/`: `flat_5/` and `vr_5/` (final),
`vr_before/` (the same check against Round 8's shaders); in git: the reports and key pictures (all pictures stay on disk). Every run headless, muted (output 0.000
RMS), no window, Below Normal, one at a time; `-exeCheckOnly fix81` (flat) and `fix81 -vr` on the mock runtime (`local/fix81_runs.sh`).
- **0 Commit first — done** (`baac2dd`).
- **1 Both eyes — done.** Cause confirmed: the 9 custom shaders had no stereo-instancing macros. All passes (forward + depth) now have
  them, plus the new `Window.shader`; none samples the screen or depth. Single-pass instanced kept. Render-texture feeds (belly / cabin
  camera, star map) are flat cameras: unaffected. **Check:** the eye pictures are now the real eye texture (both slices, copied right
  after the stereo head camera draws). Round 8's check drew each eye with a separate flat camera, so it could not see the bug; also a
  headless player draws no camera by itself, so Round 8's VR frame times (1-2 ms) did not include drawing the eyes: not valid.
  Old shaders: 27 / 32 pairs flagged (right eye 38 % scenery vs 75 % left in every biome, blue: `vr_before/*_eyes.png`); new: 0 / 32
  (7 biomes ahead + firing, lava, sea, hangar bay, launch, route trail, title, body, window, strike; correlation 0.78-0.94).
- **2 Body and hands — done (one residual for Dan).** `Flight/PilotBody.cs`: the Fix 3.1 pilot posed in code, no head (shrunk at the
  neck), legs to the floor beside the window, right hand on the stick, left on the throttle (two-bone IK, fingers curled), index pulls
  the trigger while firing, thumbs press for shield / level out (stick) and air brake (throttle); chest / neck turn a little with the head.
  Seat mirrored in `ship.py` (normals checked: 4865 outward faces before and after; the texture has no lettering): stick right, throttle
  left; both cut out of the Meshy seat (groups Stick / Throttle, no new models needed). Stick tilts 18° with pitch / roll, twists 15°
  with yaw (pad forward + right -> stick top forward + right, checked); lever swings ±22° with the throttle (the HOTAS lever when bound).
  The throttle moved 15 cm forward along the armrest (in the seat's own place the arm bent backwards). Cabin camera: the body is out of
  its view (same layer, would show). Strike: face 0.62 m, nearest part 0.40 m (unchanged). **Residual (tuning limit, two tries):**
  looking far back over the left shoulder, your shoulder is a lumpy shape 10 cm from the eye (`flat_5/f81_body_left_hand.png`).
- **3 Floor window — done.** Cause: Round 8 turned the belly faces whose centres fell in a box into glass. Now a clean cut along four
  planes, a 0.42 x 0.70 m rounded window between the feet (raised frame, 14 bolts, 30 mm tinted glass with sky reflection and scratches),
  and a lining that widens to the belly so the ground shows (`f81_window_flight.png`, `f81_window_hover.png`; in hover the nose gear shows).
- **4 Published 0.8.1 — done**; CLAUDE.md has the stereo shader rule. Clean-up: 7.3 GB -> 7.3 GB.
- Seen in passing (0.8.0 too, not changed): the right side console looks untextured white (`round8/partB_run1/r8b_cockpit_right.png`).

**For Dan to check (Quest 3):** scenery in both eyes; look down: body, hands on the stick and throttle moving with the HOTAS, the
trigger finger; the floor window in a hover; your left shoulder when looking back.

**Round 8 — the creature in front of the screens (A), new models, sound, credits, Deck numbers (B), VR (C) (2026-11-08). Published:
0.8.0 (`game-800`, Latest; soundtrack stays `soundtrack-700`).** Exe built 2026-11-08 18:14. Commits: Part A `b0127db`, models `e5ac84f` /
`1c33830`, B2-C `2ac7c9d`. Evidence `Data/screenshots/round8/`: `partA/` (+ `a_try1`, `a_try2`, `partA_run3`, `partA_run4`: logs of each
staging try), `partB_models/`, `partB_run1/`, `partB_deck/`, `partB_deck4/`, `partB_deck7/`, `partB_controls/`, `partC_flat/`,
`partC_noruntime/`, `partC_mock/`. Every run headless, muted (output 0.000 RMS), no window, Below Normal, one at a time (one overlap
by mistake mid-round: those results were thrown away and run again).
- **A0 — done** (`bf66b6d`). **A1 — done. Cause:** Round 7 turned the whole creature to look at you with its face 0.65 m out, so its body
  ran straight out from there, away from you: behind the screens (0.51-0.57 m) and inside the dash, **34-59 % of it hidden** (pixel maps:
  each frame drawn with and without creature / screens / cockpit / HUD); the screens themselves z-test correctly; the flat HUD stayed on.
- **A2 — done, residuals for Dan (tuning limit).** HUD / prompts / subtitles hide at the strike (back with the death page). Face 0.40-0.55 m
  out, body down past your left shoulder toward the hatch (canopy break-in: up over the dash), neck and head turned to you after the
  animation (`Creature.LookAt` / `FaceAt`). Still hidden: Mimic 13.5 % (shoulder, behind a screen), Knocker 11 %, Stalker 12 % (in the
  dash); Hollow, Sleeper, all break-ins 0-1 %. **A3 — done** (pictures + `_covered` maps per creature).
- **B1 — done.** Dan's 10 concepts -> Meshy -> Blender (`hard_cleanup.py`, `meshykit.py`, `render_ship.py`) -> game. Ship: Meshy hull at
  0.8 (10 m) round `ShipLayout.Eye`, its canopy panes = game glass, hull two-sided, belly window glass; Meshy seat / dash (live screens in
  its recesses: `ShipLayout.Screen*`) / two consoles; new `Contacts`, airlock door at the hull's painted door (z +0.8), engine glow at the
  pods. Mothership: Meshy hull, bay cut out, 7 wall modules, cradle, regeneration pod. Turret cut into base / head / gun (pivots measured:
  ring 2.40, gun (0, 1.07, 1.53), barrel tips 3.1 m), saucer with the game's rim lamps. For Dan: the hangar is dim, the floor window's
  edge is ragged.
- **B2 — done.** 16 ElevenLabs sounds (Docs/SOUNDS.md), levelled (`Tools/level_sounds.py`: one-shots -12, engines -16, ambiences -24 LUFS;
  raws in `SourceArt/Sounds_raw/`): engine idle / cruise loops (crossfaded and pitched by throttle, at the old hum's level: 0.0322 vs
  0.0313 RMS, music over engine + wind +11.1 dB vs +11.3), gear down / up, airlock, clamp, jump drive, explosion, regen bay, 7 biome
  ambiences (`AmbienceTick`, tuning `ambienceMix` .5). Not heard by Code (muted): **for Dan to listen**.
- **B3 — done (on this PC with the Deck setting at 1280 x 800, not on a Deck):** Canyons 1026 fps (1 % low 359), Forest 906 (314), City
  1096 (263), Ice 841 (287), Volcanic 874 (316), Crystal 723 (242), Ocean 578 (205); no lighter settings needed. Credits / menus /
  controls pages read at 1280 x 800 (`partB_deck`, `partB_controls`). Fixed on the way: `round4perf` broke at 7 biomes (array of 4).
- **B4 — done.** Credits page: People / Made with / Licences (JSON for Modern C++ and miniz, MIT; no CC0 assets).
- **B5 — done.** Pictures in game: cockpit (ahead, down, left, right, back), chase, close chase, landing view, hangar, launch, turret,
  saucer. Frame rate with the new models: cockpit 824 fps (1 % low 198), close chase 789 (192), 1080p; no same-flight 0.7.0 number.
- **C1 — done.** OpenXR 1.18.0 + XR Management 4.6.1; `Editor/XrSetup.cs` before every build: OpenXR for Windows, not started with the
  game, single-pass instanced. **C2 — done.** `Core/VrMode.cs`: only `-vr` or Settings > Display > VR mode (Off by default), never on the
  Deck; no runtime -> says so, flat, title in 2.3 s (checked); `Play Hostile Horizon (VR).bat`; the launcher passes `-vr` (source + build;
  installed launchers get it with the next starter package).
- **C3 — done, 1:1 not provable with the mock.** TrackedPoseDriver on the head, rig = tracking space, Recenter (right-stick click, Home,
  pause menu; auto at start): head on the eye point after it (0.000 m); cockpit view only. The mock's test API did not move Unity's head
  pose, so 1:1 head movement is **for Dan**.
- **C4 — done.** No screen overlay in VR; warning / prompt / subtitle on a strip under the canopy frame; every menu on a 1.34 m board
  1.4 m ahead (title, pause seen in both eyes). No motion controllers this round (optional).
- **C5 — done.** Display: Comfort vignette / fade for the jump / horizon line, all Off by default (`Effects/Comfort.cs`).
- **C6 — done.** Strike staged at the real head in VR (face 0.49 m, nearest 0.34 m in the mock run); creature, knocks, cabin are 3D sounds.
- **C7 — done.** VR quality preset: view 2200 m, shadows 150 m / 1 cascade, MSAA 2x, render scale 0.9. **Frame times (mock runtime,
  1512 x 1680 per eye, both eyes):** Canyons 1.0 ms (1 % 4.5), Forest 1.3 (5.1), City 1.0 (4.6), Ice 1.5 (6.0), Volcanic 1.3 (5.0),
  Crystal 1.6 (5.9), Ocean 1.9 (6.9) vs 13.9 ms (72 Hz) / 11.1 ms (90 Hz). Virtual Desktop's encode is not in these.
- **C8 — done:** flat (OpenXR never started), no runtime (flat), mock (all above, both eyes' pictures). **For Dan: everything in the Quest.**

**For Dan to check:** the strike (anything still behind the screens?); the new ship / cockpit / mothership / turret / saucer; the hangar's
light; the sounds (engine, gear, ambiences: levels by ear); the Deck. VR: `Play Hostile Horizon (VR).bat` with Virtual Desktop running:
the head following you, Recenter, menus, the strip, the scares, the frame rate.

---------------------------------------------------------------------------
Technical Notes / Blockers
---------------------------------------------------------------------------

- Reference project (read-only): `C:\Users\danmo\chastain_rush`. Its
  `CLAUDE.md` "Commands" section documents the launcher build, key and release
  scripts; its `Docs/Starter/README-LAUNCHER.md` is the starter README to
  adapt.
- Launcher build needs Visual Studio 18 and reads its SDK from Racer's
  `Builds/LauncherSDK` (as in Chastain Rush).
- The HOTAS 4 shows up to the Input System as a HID joystick (VID 044F, PID B67C in PC mode, red light). Fix 6.1, logged on Dan's
  unit: X / Y 16-bit with logical 0..1023 (X rests at 258 and reaches 0..686: calibrated in code), Z, Rx, Ry, Rz, slider, dial 8-bit, a hat
  (0..7, null = centre), 12 buttons. `<Joystick>/stick` -> the `HotasStick` action (calibrated, `Controls.HotasStick`); `/twist` and `/rz`
  yaw; the throttle lever `z` (found by itself, Fix 6.2 / 6.3; no option); `/hat` look and menus (left / right change values); the
  stick only moves up / down in menus (Fix 6.3); `/trigger` (HID button 1, printed R1) fire / select; the other
  buttons by symbol (`Controls.HotasActs`, `DefaultSymbolButtons`). A batch-mode game sees no real joystick: log a real one through
  Windows' joystick / HID calls.
- The launcher self-check needs the game to close itself in its hidden batch run
  (LauncherBridge does it when `-racerTestSave` is given).
- Blender FBX files import facing backwards: turn the model 180 degrees on load,
  and put anything placed in ship space (cockpit displays) on the ship, not on the
  turned model.
