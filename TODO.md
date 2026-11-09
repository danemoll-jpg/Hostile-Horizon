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
Current Objective — Fix 8.2: relaxed hands, cockpit fixes, docking pilots, menus in VR
---------------------------------------------------------------------------

Fix 8.1 is delivered as 0.8.1 (status under "Previous deliveries"). Dan
(2026-11-08, Quest 3): "Other than this it all looks great." Round 9 (missions
and more progression) comes after this fix.

0. **Commit first** (the repo as it is, with this text; leave the untracked
   `Data/screenshots/fix81/vr_5/` files as they are or commit them with it).
1. **Relaxed hands.** Dan: "Hands keep moving all over the place. They should
   just be relaxed on the throttle and flight stick." Find what moves them
   (IK targets jittering, following the head / torso too much, finger
   animation, the controls' own motion amplified). The hands rest still and
   relaxed on the grips; they only move with the stick and throttle
   themselves (small, smooth), the index finger only on fire. No drifting,
   shaking or reaching. Check: a minute of flying recorded as hand-position
   logs plus pictures; hand movement should match the control movement and
   nothing else.
2. **Throttle level with the stick.** Dan: "Can we move the throttle more
   forward so it is even with the flight stick." Move the throttle (and the
   left hand) forward to the stick's depth, so both hands sit at the same
   distance from the body, like the real HOTAS on a desk. Update `ShipLayout`.
3. **Pilots stay aboard after docking (bug).** Dan: "When docking the
   mothership, the rescued pilots count as being rescued and show as 0 on board
   but they remain on board." Find why: the count / pay is reset at docking but
   the pilot objects (cabin models, the cabin camera, cabin chatter, maybe the
   cabin list used by the Sleeper or the next flight) aren't cleared. Fix: at
   docking, the rescued pilots leave the ship (seen walking off in the bay if
   simple, otherwise simply gone), the cabin is empty on the next launch, and
   nothing from them carries over (voices, cabin camera, Sleeper logic).
   Check with the `[Play]` log and a cabin-camera picture after re-launch.
4. **Menus in VR must not clip into the cockpit.** Dan: "Pulled up the menu to
   look at the journal and it clipped through the cockpit and I couldn't read
   it." The VR menu board must always be fully visible and readable: placed in
   the free space in front of the seat (inside the canopy, in front of the
   dash, not intersecting it), or drawn on top of the cockpit geometry (its own
   render order, no depth test against the cockpit) so nothing cuts through
   it; sized and distanced for comfortable reading; recentred to where the
   player is looking when opened. Check every menu page in VR (both eyes):
   pause, Settings, the Field Journal with the 3D alien, hangar, star map.
5. **Field Journal navigation.** Dan: "If an alien was selected, I couldn't go
   left or right to change. I had to go back a level and select there." On an
   alien's page, **left / right (stick, hat, D-pad, arrows) goes to the
   previous / next alien** (skipping unmet ones or showing them as
   "Unknown"), the 3D view keeps working (rotate / zoom on the other inputs),
   and the page shows small "◀ Mimic | Knocker ▶" hints. Back still returns to
   the list.
6. **The white console on the right needs texture.** A plain white Meshy
   surface: give it real detail: panel lines, worn paint, labels / small
   switches, a material that isn't flat white (the art pipeline if a new model
   is needed: STOP for Dan; otherwise texture it in Blender / with materials).
   Check other cockpit surfaces for the same flat-white problem.
7. **A jagged piece at the top left of the windshield.** Remove it: find the
   stray Meshy geometry (or the canopy cut) at the windshield's top left and
   clean it in Blender; check the whole canopy frame from the eye for any other
   jagged bits.
8. **Check (muted, no focus):** the items above, VR pictures in both eyes where
   it applies (mock runtime), before / after.
9. **Publish as 0.8.2**, update `TODO.md` (per-item status, the causes found
   for 1 and 3) and `CLAUDE.md`, commit and push.

**For Dan to check (Quest 3):** hands calm on the controls; the throttle level
with the stick; docking empties the cabin; the Journal readable in VR and left
/ right between aliens; the right console; the windshield's top left.

---------------------------------------------------------------
Next Steps — queued rounds (Do Not Start Yet)
---------------------------------------------------------------------------

- **Round 9 — Missions and more progression:** mission objectives (recover a
  flight recorder, knock out a turret cluster, timed rescues, no-damage runs),
  recovering your own wreck yourself, insurance, best run per planet.
- **Enemies more of a threat** (DEFERRED by Dan 2026-11-07 until he's used to
  them; not decided against): turrets that hit a moving ship sometimes on
  Normal, saucers that hunt, more fire. Round 5's tuning note
  (`turretLead`, `turretAimError`, `boltHitRadius`) applies.
- **Free Flight time trial** (DECIDED yes): gates, timer, best time per seed.
  After Round 9 unless Dan moves it.

---------------------------------------------------------------------------
Previous deliveries
---------------------------------------------------------------------------

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

**Round 7 — the music, aliens up close, a [Play] log, no slow burn (Part A); Volcanic, Crystal Forest, Ocean Islands (Part B)
(2026-11-08). Published: 0.7.0 (`game-700`, Latest) + `soundtrack-700` (Dan's three new tracks).** Exe built 2026-11-08 04:57. Part A
committed alone (`f04ca14`). Evidence `Data/screenshots/round7/`: `partA_run3/` (`--only round7a`: model report, music loops and
flights, the five creatures before / after), `partA_sleeper/` (Sleeper + Knocker again after the last fix), `partA_round6a/` (Round 6's
event comparison), `partA_campaign/` (the old full-campaign check: `[Play]` lines), `partB_run1/` (`--only round7b`), `partB_free/`
(`round7bfree`, after the Volcanic look fix), `partB_ocean/` (the skim), `partB_deck/` (`round7bperf -steamDeck 1`, 1280 x 800),
`launcher*/`. Every run headless, muted (output 0.000 RMS), no window, Below Normal.
- **0 Commit first — done** (`659f7df`).
- **1 Music — done. Cause (data):** 0.6.3 went quiet whenever any pilot on foot was within 700 m and the ship under 250 m up, and
  whenever landed. Flying low along the route trail and on past pilots, the old rule silenced the flight track for **55 % (Canyons),
  36 % (Forest), 32 % (City), 33 % (Ice) of the low flying**; the loops themselves were fine (all seven tracks crossfade back into their
  loop, three passes each, ~20 min of play). Now it drops out only for the pickup: a pilot running / knocking / boarding, or landed /
  hovering within `pickupQuietRange` 250 m of the route's pilot. Same flights: **0 % quiet**, heard 100 %; at each pickup quiet 100 %
  (31-33 s), back at full volume 8 s after lift-off. Every change is a `[Play] music ...` line with the reason.
- **2 Aliens up close — done (the canopy staging not: tuning limit). Cause (data):** the meshes are opaque and closed (0.01-0.07 % open
  edges, opaque Lit materials), so not transparency or holes: 0.6.3's strike brought the body to **0.02-0.20 m from the eye** (Knocker
  0.03, Stalker 0.02, Hollow 0.12, Mimic 0.20): the camera was in or against the skin and saw the inside of the shell through the near
  clip. In flight the creature also trailed the eye (it chased a camera position a frame behind the moving ship). Now the strike uses the
  ship's eye point, the face stops 0.65 m away and no part comes nearer than 0.42 m: measured **face 0.54-0.77 m, nearest part
  0.37-0.48 m** for all five, at 47-66 m/s; up close the full-detail mesh (`forceMeshLod` 0), a cold face light, two-sided creature
  materials. Pictures `r7a_<type>_strike_before/after_*`, `_cabin_cam_*` (solid in the cabin camera, 0.75-1.48 m). **Canopy:** at 0.6.3's
  place 10-42 % of the body stands inside the glass bubble (hidden by the sill; it reads solid and close, `partA_sleeper/r7a_knocker_canopy_
  after.png`). Try 1 (pushed out along the glass): 0 % inside but face 2.5-2.7 m away and partly out of view; try 2 (leaning over the
  top): face above the view, 4 % inside. **Kept at 0.6.3's place** (full detail, light and two-sided material added); `CanopyStaging` holds
  try 2 for later. Dan decides.
- **3 [Play] log — done.** `Core/PlayLog.cs`: `[Play] <time> ...` lines in `Player.log` in players' builds: arrival (planet, seed,
  biome, aliens waiting), launch, pilots found (alien or not, tell shown) / running / at the airlock / boarded / killed, every alien event
  (type, door, canopy, strike kind and time), every music change with the reason, every computer / Control line, hull heat, shard breaks,
  docking (the debrief), deaths. 506 lines in the Part A run, 102 in Part B's.
- **4 The alien warning — done.** The slow burn is gone (`strikeInstantWeight` .55, `strikeTakeoffWeight` .45); the Sleeper alone with
  you bursts too. Nothing changes until the creature is in view (at most 0.6 s): then burst / lunge sounds, sting, music cut, controls
  taken; 0.35 s later lights, displays, alarm, INTRUDER. Measured for all five: **nothing the player could notice before it was in view**
  (in view 0.07-0.33 s after the strike). Round 6's comparison again: alien and real pilot boarding **identical** to the strike (18 / 18,
  20 / 20 events).
- **5 Check — done** (`--only round7a`, ~45 min). Note: the old `--only campaign` check (Round 2's) crashed its scripted pilot into a
  canyon wall after the first pickup and then stopped on its own bug; Round 7 changed no flying code; not chased further.
- **6 Commit Part A — done** (`f04ca14`).
- **B1 Three biomes — done.** `Biome` Volcanic / Crystal / Ocean; `ForSeed` picks from all seven (2 000 seeds: 267-313 each); same seed
  = same ground (checked); first planet still Canyons; Free Flight's Planet type lists all seven. Pads, pilots, turrets, saucers, radar,
  route trail, recall / tow, crash levels: checked on each (Campaign landing + pickup on all three; turrets on rock, never on the liquid).
- **B2 Volcanic — done.** Basalt canyons, lava rivers / lakes (20 % of the ground), glowing crust, walls lit orange from below, cooled
  shelves for pads (9 m over the lava), glowing vents with smoke plumes, ash + embers, dark red sky (brightened after the first pictures
  were too dark to read). Lava = a crash at Bounce (checked; every level but Ghost); 18 m over it: HULL HEAT + "Hull heat. Climb." +
  heat haze (rising glowing motes; no screen distortion: deliberate, VR rule 4 / no screen effects).
- **B3 Crystal Forest — done.** Spires 10-200 m (499 in 2.6 km square, 150 over 100 m), tilted, glowing inside in three cold tints with
  glassy facets (an opaque shader: Fresnel + inner glow, not real refraction); 5 125 shards; glowing moss on dark ground; pale violet sky.
  A shard flown through breaks (debris, `crystal_shatter`, no hit); a spire is a hit at Bounce (checked).
- **B4 Ocean Islands — done** (fliers code-made). Sea 54 % of the area, islands with beaches and cliffs, sea stacks, 16 arches in 4 km
  square; moving sea (waves, Fresnel sky, sun glint, whitecaps), surf / wet rock / sand at the waterline, sea mist, spray; 7 harmless
  gull-like fliers. A skim at 230 km/h touched the water twice: spray, no damage; a dive at Lasting = a crash ("the sea"). Pads on islands
  (94 m up in the check); turrets on cliff tops.
- **B5 Look, music, voices — done.** Sky / light / fog / materials / particles per biome; Dan's `flight_volcanic` (loop 103-152 s),
  `flight_crystal` (132-184 s), `flight_ocean` (108-168 s) shipped as `soundtrack-700`; new lines (ElevenLabs): `ctl_brief_<biome>`,
  `ctl_launch_<biome>` (Control names the world at launch), `cpu_hull_heat`, 36 `p<n>_mayday_<biome>`; sounds `crystal_shatter`,
  `water_splash`.
- **B6 Performance — done.** PC 1080p flying low: Volcanic 674 fps (1 % low 274), Crystal 526 (177), Ocean 474 (182). Deck setting
  1280 x 800: Volcanic 959 (348), Crystal 728 (220), Ocean 298 (50, one 1.3 s frame: not seen on the PC run; first-load hitch?).
- **B7 Check — done** (`--only round7b`, `round7bfree`, `round7bperf`).
- **Publish — done:** `soundtrack-700` then `game-700` (Latest); the live catalog points at both. **The fresh `starter-200` install
  check could not run:** from this session the launcher's requests to GitHub fail ("Update service unavailable"; PowerShell gets 504 on
  the same URL), so it was not seen updating to 700 here.

**Decisions made:** the canopy stays where 0.6.3 had it (two tries failed; numbers above); heat shimmer is rising particles, not a screen
effect; the crystal look is faked (no refraction); the Sleeper's "comes for you" uses the jump-scare burst; turrets / pilots never on the
lava or sea.
**For Dan to check:** fly low past pilots: does the music stay? Let an alien in: solid, at arm's length, scary? Look at the canopy alien
(it still stands partly inside the glass: keep it?). Fly the three new worlds: lava (and HULL HEAT), skim the sea, fly between the stacks
and under an arch, break a crystal shard. After a session, the `[Play]` lines in `Player.log` are for the planning chat. On the Deck:
the Ocean frame rate. Please start the launcher once: it should update to 0.7.0 (not verified from here).

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
