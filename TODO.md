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
Current Objective — Round 8: the creature in front of the screens (A), polish (B), VR (C)
---------------------------------------------------------------------------

Round 7 is delivered as 0.7.0 (status under "Previous deliveries"). Dan
(2026-11-08): the alien in the cockpit "is not transparent [now]; however they
appear behind the screens instead of in front." Then: polish (queued as
Round 8). One publish at the end: **0.8.0**; Part B runs in stages with STOPs
for Dan (his art pipeline).

**Part A — the creature in front of the screens**

A0. **Commit first** (the repo as it is, with this text).
A1. Find out with pictures and data what draws over the creature during the
    strike / canopy: the flat-screen HUD overlay (a screen-space canvas, always
    on top), the cockpit's world-space displays (their UI shader's ZTest /
    render queue), the belly / cabin camera feeds, the glass, or the creature
    really being placed behind the dash. Report the cause.
A2. Fix so **the creature is always drawn in front of anything between it and
    the player's eyes:** world-space displays z-test against it like solid
    objects; at the strike the flat HUD overlay, subtitles and prompts hide
    (they come back for the death screen); the creature is placed between the
    player and the dash, never inside it. Same on the canopy (outside the glass,
    in front of the sky, the displays inside the cockpit stay in front of it
    only where they really are nearer).
A3. **Check:** cockpit pictures of each creature at the strike and on the
    canopy, with the HUD on and off, before / after, that Code looks at and
    describes. Commit Part A.

**Part B — polish**

B1. **Art pipeline (Dan's standing way, `Docs/MODELS-NEEDED.md`; STOP for Dan
    at each step):** new models for the **ship's exterior**, the **cockpit
    interior** (detailed, believable, built for VR: switches, panels, seat,
    canopy frame; the displays, glass floor and cameras keep working), the
    **mothership** and its **hangar bay**, the **turret** and the **saucer**.
    Code writes the concept-image prompts (STOP), Dan makes the images, Code
    makes the models with the Meshy API and cleans them up in Blender (LODs,
    collision shapes kept simple; the cockpit's layout must match `ShipLayout`
    or update it) and shows renders (STOP), then puts them in the game. No
    Mixamo needed (no characters), unless Code finds something that needs
    rigging.
B2. **Sound pass:** every ship and world sound checked and improved where weak
    (engine at idle / cruise / boost, landing gear, airlock, hover, docking,
    the jump, the regeneration bay, biome ambiences: forest, city, ice, lava,
    crystal, ocean), ElevenLabs where new; levels balanced with the Round 6.3
    mix. Muted checks with logged levels; any audible step brief and reported.
B3. **Steam Deck tuning:** frame rate in every biome on the Deck setting at
    1280 x 800 (target 40+ fps, 1 % lows reported), lighter settings where it
    misses; text sizes readable on the Deck; the controls pages fit.
B4. **Credits:** a proper credits screen (Dan; the tools: Unity, Blender, Meshy,
    Mixamo, Suno, ElevenLabs; third-party licences from `Launcher/vendor` and
    any CC0 assets; Code / the planning chat as tools).
B5. **Check:** pictures of every new model in game (cockpit view, chase view,
    hangar), frame rate before / after, the Deck setting's numbers.

**Part C — VR** (Dan, 2026-11-08: "let's go ahead and wrap VR into this
round"). PC VR first: Quest 3 through **Virtual Desktop** (and SteamVR), via
OpenXR. Native Quest is decided later. The VR-ready rules in `CLAUDE.md` were
followed from Round 1, so this should be wiring, not rebuilding. Best done after
Part B's new cockpit is in, so the cockpit is checked once in VR.

C1. **OpenXR:** add Unity's OpenXR plugin + XR Plugin Management (versions that
    fit Unity 6000.6.1f1), PC Standalone only. The same exe plays flat or VR.
C2. **Starting in VR, never by accident:** a **VR mode** setting (Off by default)
    and **`-vr`** on the command line; `Play Hostile Horizon (VR).bat` in the
    repo root; the launcher passes `-vr` through when asked. With VR on and no
    headset / runtime found, say so and start flat (never hang, never force
    SteamVR open when VR is off). The Steam Deck build is unaffected (VR off).
C3. **Seated cockpit, head-tracked:** the camera rig's head follows the
    headset at 1:1 inside the cockpit (seated reference, the eyes at the seat's
    eye point); **Recenter** (a button on HOTAS / gamepad, and in the pause
    menu); the player's head can lean and look through the canopy, the glass
    floor and back at the cabin hatch. Only the cockpit view in VR (the chase
    / landing views are flat-screen only); the belly / cabin cameras stay on the
    cockpit displays.
C4. **No screen overlay in VR:** everything the flat HUD shows is already on
    the cockpit displays (VR rule 2); subtitles, prompts and warnings move to a
    world-space strip under the canopy frame; menus (title, pause, hangar, star
    map, Field Journal with the rotatable 3D alien, settings) show on a
    world-space panel in front of the seat, driven by HOTAS / gamepad /
    keyboard as now. Motion controllers are optional this round: if they're
    easy, a laser pointer for menus; not required.
C5. **Comfort and realism (Dan's rule, VR rule 5):** realism by default: no
    vignette, no fades, no snap anything, the docking, tow, jump and horror all
    live. A **Comfort** settings group exists with every option **Off by
    default** (vignette when turning hard, fade instead of the jump, a
    horizon line). The ship's self-righting and Auto-level apply as on the flat
    screen.
C6. **The scares in VR:** the canopy creature and the strike staged for real
    stereo depth (the 0.42 m minimum distance from Round 7 kept; the creature
    solid and lit; the cabin hatch behind the left shoulder so turning your head
    to the noise works). Spatial sound for the creature, the knocks and the
    cabin.
C7. **Performance:** the 1660 Ti must hold the headset's rate through Virtual
    Desktop (72 Hz target, 90 if it can): single-pass instanced rendering, a
    **VR quality** preset (render scale, shadows, view distance, effects, the
    densest biomes thinned), no expensive full-screen effects; report frame
    times per biome. Text in the cockpit and on panels readable in the headset
    (sizes in metres, not pixels).
C8. **Check (muted, no focus; Code can't wear the headset):** with OpenXR's
    **mock runtime**: VR starts, the head moves the view 1:1, Recenter, menus
    on the panel, no overlay, the strike and canopy staged in both eyes'
    pictures (Code looks at the left / right images), frame times with the VR
    quality preset in every biome; VR off by default and with no runtime starts
    flat. **For Dan to check** (only he has the headset): everything above in
    the Quest 3 through Virtual Desktop.

**End of the round:** publish **0.8.0**, update `TODO.md` (per-item status for
A1-A3, B1-B5 and C1-C8: done / not done / deferred and why; the cause from A1) and
`CLAUDE.md`, clean up, commit and push.

**For Dan to check:** the creature at the strike: in front of everything? The
new ship, cockpit, mothership, turrets and saucers. The sound. The Deck. Then
VR: `Play Hostile Horizon (VR).bat` with Virtual Desktop running on the Quest 3:
the cockpit, the menus, the scares, the frame rate.

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

**Fix 6.3 — HOTAS trigger / R1 / R3, stray setting changes, an input recorder, the sound mix (2026-11-08). Published: 0.6.3
(`game-603`, Latest; soundtrack stays `soundtrack-401`).** Exe built 2026-11-08 01:47. Evidence `Data/screenshots/fix63/`: `run7/` (the
check `--only fix63`: `exe_check.txt`, pictures, the recorder's `hotas_input_log.txt`), `fix62_regression/` (`--only fix62` again: all OK),
`launcher/`. Every run headless, muted (output 0.000 RMS), no window, Below Normal. The check's HOTAS is a HID joystick built from Dan's own
HID descriptor, so Unity's HID support names its controls exactly as for his unit (`trigger`, `button2`..`button12`, `stick`, `z`, `hat`...).
- **0 Commit first — done** (`9d75f72`).
- **1 R1 is the trigger — done.** `DefaultSymbolButtons` now R1 = button 1, R3 = button 3 (the back of the stick's head); the picture
  (R1 on the trigger, R3 on the head), Identify, every prompt. Settings still on 0.6.2's default names get the new ones once (`version` 4).
- **2 What changed Dan's settings — found and fixed. Cause:** on 0.6.2's HOTAS setup page the Throttle axis, Invert lever and all 11
  button-job rows changed with **left / right**, and the menus took left / right from the **stick** too. A hand resting on the stick leans it
  sideways while scrolling. Replaying that through 0.6.2's own menu rules (two left steps on Throttle axis, two on Invert lever, a held lean on
  Level out (8 steps), one left on Camera view, one right on Shield) gives **exactly** Dan's 00:26 file: throttle axis `ry` (5), invert
  true, jobs `[2,5,8,4,6,7,9,12,11,10,3]`. The other suspects don't: loading 0.6.1 / 0.6.2 / Dan's file never changes jobs, Identify only
  changes names, Assign one job, Calibrate the calibration, Find the lever, the resets give the defaults. Fixed by 3 and 6b.
- **3 No accidental changes — done.** On the HOTAS the stick only moves up / down in menus; values change only with the hat's left / right
  or a press of R1 (select) on a row. Twist, rocker and lever never touch menus. Button jobs have no left / right at all (only "press a
  button"). Check: the same leaning-stick trace with twist and lever moving, live on HOTAS setup: selection moved 15 times, **settings
  unchanged**; stick held right 1 s on Flight assist: no change; hat right / R1 / hat left: Light / Off / Full; hat down moves a row.
- **4 The trigger fires — done.** Fire and Submit's HOTAS bindings follow R1 as identified (`Controls.FireButton`; Unity names HID button 1
  `trigger`). Free Flight has the gun now (no enemies; shots spark on the rock and cost energy). Check: HID button 1 held 1 s = 7 shots in
  Free Flight and 7 in the Campaign.
- **5 Input recorder — done.** Settings > Controls and HOTAS setup: **Record my input (20 s)** writes every control of every device (the
  device list with Unity's HID descriptor, then each change: time, device, path, value, the game actions it feeds) to
  `hotas_input_log.txt` beside `settings.json` and says where. Buttons don't act on the menu while it records. Check: 1 507 changes,
  `trigger pressed -> Flight/Fire, UI/Submit`, `button3 pressed -> Flight/Shield`.
- **6 Test view — done.** HOTAS setup always shows the live Test readout (stick, twist / rocker, lever, hat, each with its job; held
  buttons with their job) and lights every held button on the picture. Holding R1 on a row also selects it, so the page says to use Record
  my input to try buttons freely. Pictures `fix63_test_view_R1_held.png`, `_R3_held.png`, `fix63_controls_hotas.png`.
- **6b No throttle-axis option — done.** Throttle axis and Invert lever are gone from every page; 0.6.2's saved values are ignored and
  dropped. The lever is found by itself (z, else the axis moved end to end), forward = full; Find throttle lever learns the direction
  (you leave the lever fully forward) and Reset HOTAS settings stay.
- **6c Sound mix — done (2 tuning tries; numbers for Dan).** Settings > Sound > **Engine** (engine hum + wind, 100 %). Defaults: music 70
  -> 85 % (0.6.2's untouched 70 becomes 85 once), tuning `engineMix` .55, `windMix` .5 (was 1 / 1). At cruise (383 km/h, 80 %): engine +
  wind -5.9 dB, music +1.7 dB: music vs engine + wind **-3.0 dB -> +4.6 dB** (try 1, windMix .6: +4.5). The music's level comes from the
  track files (-16 dB RMS, all four flight tracks): a muted run doesn't render the music, so it can't be metered live.
- **6d Audible computer — done.** Every voice line ducks engine + wind (-8.6 dB measured), music (x.6, -4.4 dB) and effects (x.75).
  The 18 computer lines: EQ + light compression + levelled (`Tools/process_computer_voice.py`; originals kept in
  `SourceArt/Voices/computer_raw/`): **-31.1 .. -24.5 LUFS (6.6 LU apart) -> -16.7 .. -16.4 LUFS**, played at 100 % of Voices (was 90 %).
  A two-tone chime before its 10 warnings. **Test voices** (Settings > Sound) plays Control, the computer and a pilot, also from the pause
  menu (check: all three logged, muted). In Free Flight the computer can say only `cpu_pull_up` and `cpu_whiteout` (Ice blizzards); every
  other line is Campaign only (energy, return / can't reach, hull, gear, airlock, pilot aboard, cabin full, intruder, engines off, locked
  on, saucer, shield up / down, target down).
- **7 Check — done** (`-exeCheckOnly fix63`, about 5 min). **8 Publish — done:** 0.6.3 = build 603, `game-603` Latest; a fresh `starter-200`
  install updated to `versions/603`, exit 0. Clean-up: disk see the round report.

**Decisions made:** the gun works in Free Flight (no enemies there); the Test view shows all the time on HOTAS setup; buttons are tried
freely under Record my input (R1 is select on the page itself); the music meter uses the files' level.
**For Dan to check:** the Throttle axis / Invert lever options are gone and the lever just works. Scroll Settings with the stick: nothing
changes unless you use the hat. Pull R1 in Free Flight: does the gun fire? Can you hear the music over the engine now (try the Engine
slider), and the computer (Settings > Sound > Test voices)? Then **Record my input** in HOTAS setup and move everything, so the planning
chat can read `hotas_input_log.txt`.

**Fix 6.2 — HOTAS reset to defaults, the throttle lever, clear calibration, an accurate HOTAS picture (2026-11-07). Published: 0.6.2
(`game-602`, Latest; soundtrack stays `soundtrack-401`).** Exe built 2026-11-07 21:36. Evidence `Data/screenshots/fix62/`: `hid_descriptor.txt`
and `hid_lever_log.txt` (Dan's HOTAS logged through Windows' HID calls while he moved it: `python Tools/hotas_hid_log.py`), `run3/` (the
check at 1080p), `deck/` (pictures at 1280 x 800), `launcher/`. Every run headless, muted (output 0.000 RMS), no window, Below Normal.
- **0 Commit first — done** (`749447f`).
- **1 Reset to defaults — done.** HOTAS setup > **Reset HOTAS settings** (calibration, throttle axis and the lever found, invert lever, button
  names, button jobs, deadzone); Settings, Sound and Display > **Reset this page to defaults**; Settings > **Reset all settings**. Each asks
  "Reset ...? No / Yes", No selected first. The Controls page has no settings of its own (its reset is HOTAS setup's). Check: No changes
  nothing; each Yes resets only its own page; `career.json` byte-identical before / after.
- **2 The throttle lever — done. Cause, from the log:** in PC mode the T.Flight HOTAS 4 reports **z = the lever** (0 fully forward, 255 fully
  back), rz = the twist **and** the rocker (the HOTAS itself puts both on one axis), and four axes that never move: slider and dial (always
  the middle), rx and ry. 0.6.1's Auto took `slider` whenever a device had one. So the game read an axis that never moves. The ship only
  took the lever's value when it changed, so the throttle stayed at its 30 % start. Not Dan's settings, not the pad's triggers. Fixed:
  Auto starts on z and switches to any other axis moved end to end (never the stick or the twist), and saves it; **Find throttle lever**
  in HOTAS setup; a big live **Throttle: 64 %** readout and bar over the picture; the lever is read without the axis' own deadzone; the
  ship's throttle = the lever from the first frame of flight and all the time (the pad's triggers take over while used, until the lever
  moves). Check (a virtual HOTAS with Dan's eight axes): a lever on z / slider / dial / rx / ry is picked by Auto every time and reads
  100 / 50 / 0 %; 0.6.1's binding with the lever moving reads 50 50 50 50 50 %; Find picked z in 2.7 s with the twist and stick moving;
  lever 0 / 50 / 100 % = ship 0 / 50 / 100 % on the first frame of flight; lever steps 0-100 % = ship 0 25 50 75 100 %. **PC / PS4
  switch:** Dan's is on PC (Windows sees "T.Flight Hotas 4", PID b67c). How the PS4 position looks to the PC was not logged: it needs the
  switch moved during a log.
- **3 Calibration — done.** It asks first ("Calibrate the stick? Takes about 10 seconds", No / Yes), then: Step 1 of 3 let go; Step 2 of 3
  push to each edge (or full circles) with the stick drawn from above, four markers that turn green and a live dot; Step 3 of 3 press the
  trigger. The trigger before all four edges names the missing ones. ○ / B / Esc cancels at any step and keeps the old calibration
  (checked). Idioms removed: "right round", "look round", "banks the ship round".
- **4 HOTAS picture — done** (our own drawing, `UI/ControlsSheet.cs`). A front view (throttle left, stick right, the base with SHARE / PS /
  OPTIONS between) plus four close-ups: the throttle's side (□ ✕ ○ △ top to bottom, rocker below), its back (R2 high, L2 low), the stick
  head (hat, L1 = the blue one, L3) and the stick from the side (R1, the trigger). Each control has a leader line and label; PS is greyed
  with no label. Identify lights each symbol in its place. **Button names now follow Dan's labels:** trigger = R3 (button 1), L1 2, L3 4,
  L2 9. R2 sent nothing in the log and R1 wasn't found, so 10 and 3 are assumed. 0.6.1 had guessed the trigger was R2; settings still on
  0.6.1's defaults get the new names once. Compared with the poster and Dan's photo, control by control: throttle left / stick right, the
  four face buttons' order and the rocker, SHARE / PS / OPTIONS, hat / L1 / L3 on the head all match. R1 at the back of the head and R2 / L2
  on the throttle's back come from the poster and Dan's words; the photo doesn't show them.
- **5 Check — done** (`-exeCheckOnly fix62`, `Checks/ExeCheck.Fix62.cs`, about 3 min; `fix62pics` at 1280 x 800): every line OK. Readable at
  1080p; at 1280 x 800 the picture's labels are small (~13 px) but clear. Not re-run: `--only fix61` (its calibration step now meets the
  new question first).
- **6 Publish — done.** 0.6.2 = build 602, `game-602` Latest; a fresh `starter-200` install updated to `versions/602`, exit 0.
  Clean-up: 3.9 GB -> 3.9 GB.

**For Dan to check:** HOTAS setup: Reset HOTAS settings, then move the lever: does Throttle % follow it (Auto should already be on
"z"), and does the ship speed up and slow down with it? Calibrate: are the steps clear? Does the picture match your HOTAS? Run
Identify your buttons once: R2 sent nothing in today's log, so see whether "Held" shows R2 when you press it, and find where R1 is.

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
