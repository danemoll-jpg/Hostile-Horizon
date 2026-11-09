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
Current Objective — Round 10: game fixes (Part A, 0.9.1) + the promo video (Part B)
---------------------------------------------------------------------------

Round 9 is delivered as 0.9.0 (status under "Previous deliveries"). Dan
(2026-11-09): "let's go ahead with the promo round". The plan below was decided
with Dan on 2026-11-08; **the draft narration is approved** ("sounds good").
Part A is a game update (0.9.1); Part B makes video files.

0. **Commit first** (the repo as it is, with this text).

**Part A first — game fixes (Fix 9.1, Dan 2026-11-09), before any capture,**
so the video shows them. Publish them as **0.9.1** when done (a normal game
release), then go on with the promo items below.

A1. **Aliens rarer.** Dan: "I seem to be hitting them like 50 % of the time."
    Now: share 20 % + 5 % a tier, max 35 %, and **at least one alien on every
    planet after the first** (`max(1, round(share x pilots))`): with 3-4 pilots
    that is 25-33 % or more of all pickups. New (Normal): share **10 %**, +2.5 %
    a tier, max **20 %**, rolled per pilot from the seed (so **some planets have
    none**, no minimum of one); the difficulty presets scale it (Easy lower,
    Hard / Nightmare higher, Custom's slider). Report the measured share over
    400 sample planets at tiers 0-4 for each preset. The Sleeper and the other
    types stay in the mix.
A2. **Dock through the bay, not the roof.** Dan: "Docking to the mothership
    should come in through the bay and not the roof." The automatic approach
    (and the tow / recall / mid-air rescue lifts) must fly in through the bay's
    opening (the opening cut in Round 9 A), lined up behind / below it, never
    through the hull or the roof. Launch leaves through the same opening (or the
    floor doors if that's the bay's design, but never through solid hull).
    Check: pictures along the approach from the chase view and the cockpit;
    the path never passes through hull geometry (log the closest distance to the
    hull mesh).
A3. **Mothership textures.** Dan: "The mothership seems to be missing its
    textures." Find why (Meshy textures not imported / not assigned after the
    bay cut in Round 9, a material lost in the Blender re-export, the hull drawn
    with a plain material, LOD materials). Restore the textured look on the
    hull, wall modules and the openings' new trim. Check: pictures of the
    mothership outside and in the bay, before / after.
A4. **VR recenter that really recenters.** Dan: "It needs to recenter to the way
    I am facing. If I take off the headset for a second, it faces a different
    direction and I can't get it to correct." Recenter must set the view so the
    direction the player's head is facing **now** becomes straight ahead in the
    cockpit (yaw only; keep the horizon level), and move the head back to the
    eye point. Handle the runtime's tracking-origin changes (the headset taken
    off / put back on, Virtual Desktop / SteamVR recentering, the Quest's own
    reset): when the origin changes, recenter automatically. The Recenter
    control: a **HOTAS button** Dan can reach (assignable, default a base
    button), the gamepad (hold ⧉ View + ☰ Menu, or as Code proposes), the
    keyboard, and in the pause menu; it works in flight, landed, docked and in
    menus. Check with the mock runtime: head turned 90° / 180°, taking the
    "headset off" (pose lost and back with a different origin), then recenter:
    straight ahead again, both eyes.
A5. **Publish 0.9.1**, update `TODO.md` (status for A1-A4, the causes for A3)
    and commit, then continue with the promo items.

**Part A status (Fix 9.1, 2026-11-09).** Evidence `Data/screenshots/fix91/` (`flat/` = `-exeCheckOnly fix91`, `vr/` = `fix91 -vr`
on the mock runtime, `home/` = the `home` check again). Run: `bash local/fix91_runs.sh flat|vr <name>`.
- **A1 DONE.** Cause: `Aliens.Assign` forced `max(1, round(share x pilots))`: never a planet without one, 25-33 %+ of pickups.
  Now each pilot is rolled from the seed at `Aliens.Share(tier)` = min(`alienShareMax` .20, `alienShareStart` .10 +
  `alienSharePerTier` .025 x tier) x the difficulty's level (.6 / 1 / 1.25 / 1.5). Measured over 400 planets a tier (`flat/exe_check.txt`):
  Easy 6.4-11.7 %, **Normal 10.8 % (tier 0) .. 20.0 % (tier 4)** (0.9.0: 33.3 / 25 / 40 / 33 / 29 %), Hard 13-25 %, Nightmare 16-29 %;
  planets with none on Normal 72 % (tier 0) .. 19 % (tier 4) (0.9.0: 0 %); all five types appear. Planets saved by 0.9.0: their
  still-waiting pilots are rolled again once (`PlanetState.alienRoll`).
- **A2 DONE.** Cause (logged): 0.9.0's approach reached its lining-up point (350 m off the bow, 25 m under the bay) at up to 200 m/s
  with no braking; its momentum carried the ship ~100 m up (seen at 1608 m, the mothership at 1500) through the hull, then down
  through the roof into the bay: 28-48 crossings of the hull's surface from 3 of 5 starts; the lift from above went straight down
  through it. Now (`GameShell.SternEntry`, `RoundTheHull`): round the hull in a lane beside it (only the corners needed, ~10 m/s round
  each), lined up 260 m behind the stern, **in through the stern opening** at ~20 m/s slowing to a walking pace, turning round to face
  out under the cradle; lifts (tow / recall / the beam) go round the hull to a lane 30 m under the keel, then up through the floor
  opening; the launch drops clear of the keel before moving off. Measured against the hull's mesh (120 k triangles): **0 crossings
  from all 5 docking starts, both lifts and the launch, at least 4.9 m from the hull** (`flat/exe_check.txt`); pictures
  `flat/a2_chase_*`, `a2_cockpit_*`. Docking takes 19-23 s (0.9.0: 15-22 s). For Dan: in the Chase view the camera (30 m behind) is
  inside the hull for the last seconds in the bay (the cockpit view is clean).
- **A3 DONE.** Cause: `Mothership.cs` (unchanged since Round 2) never loaded the Meshy materials (`Resources/Hard/*.mat`, as the ship
  and enemies do since Round 8): the hull, wall modules, cradle and pod were all drawn plain grey. Now the `mx_` slots get their
  Meshy material (every slot filled), the code-built lining / trim / liners take the hull's / bay wall's textures, tinted; the floor
  doors stay plain. Before: `Data/screenshots/round9/partA/exe2/r9a_approach_stern.png`, `r9a_docking_2_near.png`; after:
  `flat/a3_stern.png`, `a3_side`, `a3_below`, `a3_bow`, `a3_bay_inside`.
- **A4 DONE (numbers on the mock runtime; for Dan in the Quest).** Cause: 0.9.0 applied the recentre only in the cockpit view while
  it followed the ship; on the title / menus (the rig placed by other code) a recentre changed nothing, and nothing recentred after
  the headset came off (measured: 90° / 180° stayed off on the title; headset off and back at another yaw stayed 125° off everywhere).
  Now (`CameraRig.PlaceVr`, `VrPlacer`): every placement is an eye pose and the recentre (yaw only, head back to the eye point) is
  applied after it, in every state; **in VR, holding Pause 1 s recentres** (HOTAS OPTIONS by default, or whatever HOTAS setup gives
  Pause; the pad's ☰ Menu; Esc), a tap still pauses (on release); the right-stick click, Home and the pause-menu row ("Recenter VR
  view (or hold ... 1 s)") as before; automatic recentre when the runtime reports an origin change, when tracking comes back after
  0.25 s, or when the head pose jumps > 45° at > 1500°/s (a reset the runtime did not report). Measured (`vr/exe_check.txt`): title,
  flight and hangar: head turned 90° / 180° and moved 24 cm -> after the hold 0° and 0 cm; headset off 1.2 s, back on turned 305° ->
  0° by itself (the mock never reports lost tracking: the jump detector did it). Both eyes drawn in every picture; the batch-mode
  eye textures on the mock do not follow head turns at all (the "turned 90°" picture is straight too), so they cannot show the turn.
- Regression: the `home` check (Fix 2.3's) again: part (a), a pickup and the climb to dock, docks through the stern as designed;
  its later parts fail because turrets (since Round 5) shoot down its slow, low check pilot; a Nov 7 build (0.8.x, 0.9.0's docking)
  fails the same check earlier and worse (`home_before/`, run with the new `run_check.py --exe`): the check is stale, not a regression.
- **A5 DONE.** Exe built 2026-11-09 04:25; published **0.9.1 (`game-901`, Latest; soundtrack stays `soundtrack-700`)**. Not yet played
  by Dan. **For Dan to check:** docking (through the stern, cockpit and chase), the mothership's look, aliens' frequency over a few
  planets, and in the Quest: hold OPTIONS 1 s to recentre (title, flight, hangar), and take the headset off and put it back on.

**Part B — the promo video**

1. **Two cuts** from the same footage and narration:
   - **Trailer:** 60-90 s, 16:9, 1920 x 1080 (4K if the PC renders it in
     reasonable time), 60 fps, H.264 MP4, for Steam / YouTube.
   - **Vertical:** 30 s, 9:16, 1080 x 1920, 60 fps, H.264 MP4, for phones /
     social: its own framing (reframed or separately captured shots), not a
     letterboxed trailer; text bigger.
   Output: `Promo/HostileHorizon_Trailer.mp4`, `Promo/HostileHorizon_Vertical.mp4`
   (git-ignored if large; their capture scripts / edit lists in git).
2. **Narration by Control** — the in-game Control voice (ElevenLabs, the same
   voice id), as a radio briefing (the game's radio filter, lighter). The
   approved script:
   "This is Control. The colony fleet went down over the frontier. Hundreds of
   pilots, scattered across worlds that want them dead. ... You fly in low.
   Below the guns, through the canyons, through the forests, between the
   towers of dead cities. ... Find them. Land. Open the airlock. ... Just make
   sure it's really them. ... Fly it with a HOTAS. Step into the cockpit in VR.
   ... Hostile Horizon."
   Ending, over black: "Rescue Seven, respond. ... Rescue Seven? ... We've lost
   contact."
   The vertical cut uses a shortened version (Code trims it; same voice
   takes). Subtitles burned in on the vertical cut (phones play muted).
3. **Footage: real game, captured from the built exe** with scripted cinematic
   camera paths (a capture mode, e.g. `-promoCapture <shot> <dir>`), rendered
   **frame by frame at a fixed time step** (no dropped frames, full quality,
   higher settings than play if the PC allows), **no visible window, muted**
   (the testing rules; sound is added in the edit). Shot list (Code may add
   more good-looking shots):
   - opening: the mothership over a planet; the ship drops out of the bay;
   - the canyon run low and fast (chase and cockpit views);
   - a **biome montage**: Giant Forest, Dead City, Ice, Volcanic (lava),
     Crystal Forest, Ocean Islands (arches);
   - the **cockpit with the pilot's hands on the HOTAS stick and throttle**,
     moving with the flying;
   - **VR**: a cockpit look-around shot that reads as VR (head turning,
     looking down through the floor window), labelled on screen;
   - turrets firing, a saucer pass, the ship firing / shield;
   - the radar and route trail; braking; a landing with the belly camera;
   - a pilot running to the ship and the **three knocks**;
   - docking in the mothership;
   - the **ending**: landed, the knock, the airlock opens... an **alien bursts
     out at the camera**, hard cut to black, Control's last lines over black,
     then the title card.
   Never show a full alien before the ending (only hints: a shadow, a figure
   at a distance).
4. **Edit (ffmpeg):** cut to the music and the narration; **score** with Dan's
   Suno `title_theme` (the trailer), a flight track under the montage if it
   fits; ElevenLabs / in-game sound effects (engine, wind, shots, knocks, the
   alien shriek); the narration ducks the music. **Title cards** from Dan's
   artwork (`SourceArt/Title/SteamDeck/`: the logo, the wide / portrait art),
   short big on-screen text ("Rescue them.", "Don't let it in."). **Closing
   card:** the logo, "HOTAS · VR · Steam Deck". No store link or price (none
   decided yet; Dan can ask later).
5. **STOP for Dan: rough cut.** Low-res previews of both cuts
   (`Promo/rough/`), with the narration and music, plus a one-line list of the
   shots. Wait for Dan's notes before the final render.
6. **Final render** of both cuts after Dan's notes; check them (length, no
   black frames except the ending, audio levels: narration clear over music,
   loudness about -14 LUFS, no clipping; text readable on a phone). Any
   audible playback for checking follows the testing rule (brief, reported) —
   prefer measured levels.
7. **Update `TODO.md`** (per-item status, the file paths, how to re-render)
   and `CLAUDE.md` (the capture mode and the edit commands), clean up, commit
   and push. Tell Dan where the two videos are.

**For Dan to check:** the rough cut (step 5), then the two final videos.

---------------------------------------------------------------
Next Steps — queued rounds (Do Not Start Yet)
---------------------------------------------------------------------------

Nothing queued after Round 10. Other ideas, not decided: native Quest, more biomes, more alien types.

---------------------------------------------------------------------------
Previous deliveries
---------------------------------------------------------------------------

**Round 9 — Fix 8.3 (A), side missions (B), recover your own wreck (C), insurance (D), best runs (E), time trial (F), tougher enemies on
Hard / Nightmare (G) (2026-11-09). Published: 0.9.0 (`game-900`, Latest; soundtrack stays `soundtrack-700`) + `starter-900` (not Latest).**
Exe built 2026-11-09 (final after the G tuning). Commits: plan `7127eb4`, Part A `2bc6e8a`, Parts B-G `dd8e17c` (B-E share the campaign
files: one commit), G tuning + final checks after it. Evidence `Data/screenshots/round9/`: `partA/` (Blender `before_*` / `after_mother`,
`box_find/`, `exe2/`), `partB/run4`, `partCDE/run3`, `partF/run2`, `partG/run3` (earlier runs beside them), `launcher/`,
`update_from_github/`. New check `-exeCheckOnly round9` (`round9a`, `round9b`, `round9c` = C+D+E, `round9f`, `round9g`;
`Checks/ExeCheck.Round9.cs`). Every run headless, muted (output 0.000 RMS), no window, Below Normal, one at a time.
- **A0 Commit first — done** (`7127eb4`). **A1 Mothership openings — done.** Cause: the bay and floor shaft were cut by deleting whole
  faces by their centres (saw-tooth edges). `mothership.py` now slices the hull exactly along the openings' planes, then deletes; both
  openings lined: a plated lip from the stern skin to the bay liners, a shaft lining from the belly to under the doors, trim, edge lights.
  Check: Blender before / after, exe pictures from the approach, below, during the recall beam (3 stages) and inside the bay
  (`partA/exe2/r9a_sheet.png`). No other cuts on the mothership.
- **A2 The grey box — done. It was the passenger cabin** (`Campaign/Cabin.cs`, built in code in Round 3 for the old hull: 2.9 m wide,
  1.56 m tall, back wall at z -3.55): the Round 8 Meshy hull is slimmer and only ~1.4 m tall inside, so the cabin's floor, back wall and
  lower sides poked out under the tail (Blender test: 90-172 of 216 sample points outside the hull). The cabin is needed (passengers,
  hatch, cabin camera), so it is now drawn only for cameras inside the hull (cockpit, VR head, cabin camera). Check: chase / close chase /
  landing view / rear / below / side, before (0.8.2's cabin) and after (`partA/exe2/r9a_before_* / r9a_after_*`); the cockpit still sees
  the hatch. No other leftover parts seen.
- **2b Steam artwork — done.** Dan's originals (`SourceArt/Title/SteamDeck/`; `SourceArt/Title/Dan/` deleted) ->
  `python Tools/make_steam_art.py` -> `SourceArt/Title/Steam/`: Portrait 600 x 900 (also the launcher's Cover), Wide 920 x 430, **Hero
  1920 x 620** (no crop drops the title without cutting the ship: the title is painted out of the sky, the ship whole), **Logo
  transparent** (background un-mixed into alpha, clean edges; `logo_over_hero.png`), Icon. Shipped in the game folder (`SteamArt/`), the
  starter ZIP (beside the launcher) and **copied up beside the launcher by the 0.9.0 game on its first start** (`Core/SteamArt.cs`), so
  an existing install gets them with the update. README-LAUNCHER.md: a table of which file goes where in Steam. Checks: the launcher with
  the portrait cover (`launcher/auto-5.png`: whole, not stretched); the starter ZIP lists all six beside the launcher; an older install
  updated from GitHub to 0.9.0 has all six in its top folder ("6 copied", `update_from_github/`).
- **B1 Side missions — done.** 1-3 per planet from its seed (8 seeds: 3, 1, 2, 2, 2, 1, 3, 1), in the hangar (one line), the star map, at
  launch by Control, on the cockpit list / HUD and the radar. Check (all kinds forced on one planet): recorder pulled aboard after an 8 s
  hold (a 3 s hold and let go: not), nest 3 / 3 turrets on the high ground seeing the valley, destroyed; timed pilot picked up with 455 s
  left; clean run home without a scratch: all four paid at the dock (3450 -> 9200 cr). Second planet: the timed clock run out -> the
  pilot lost; a 6 % hit -> clean run failed. **B2 — done** (rewards x pay, at the dock; failing costs only the reward). **B3 — done**: 13
  new Control lines (ElevenLabs; Docs/VOICES.md), not heard by Code (muted).
- **C1 Recover your own wreck — done.** Third choice in the regeneration bay; the wreck (your ship, burnt) on the radar (WRECK), the route
  trail and the displays; land, engines off, hold the airlock 12 s; back at the next dock at 60 % hull, upgrades kept, loaner returned.
  Check: the loaner lost on the way (wreck stays where it was, same choices again), then the salvage (a saucer was waiting: ambush chance
  .4), own ship back.
- **D1 Insurance — done.** Hangar row (4 % of the ship's value: 240 cr; select again to cancel); insured wreck's recovery 600 cr instead
  of 3000 (20 %); on the hangar, the death page and the regeneration bay; pilots never covered.
- **E1 Best runs — done.** Per planet: fastest clear (flight time), most credits, fewest hull hits, cleared without dying; on the star
  map with the planet of the day's picked out; career totals (minutes flown, hull hits, clean clears, missions, salvages, insured flights).
- **F1-F3 Time trial — done.** Free Flight > Time trial / Ghost (On). Course from the seed and planet type (14 km, a gate every 550 m,
  radius 24 m, a miss +5 s), timer, splits, best per seed + planet type in `trials.json` with its ghost. Check (Ocean Islands, seed 4711,
  Bounce): 26 gates, same course twice, the ghost of run 1 shown in run 2, best 4:43.8 kept over the slower 4:54.4. The check's simple
  pilot missed 9 gates by flying low over hills (29-188 m off): for Dan to judge the course at speed.
- **G1-G3 Harder enemies — done, tuning limit reached** (2 tries). Easy / Normal unchanged (Round 5's lead .9, error 14, radius 3.5; 650 m
  crossing: Normal 0-1 hits in 57-58 shots). Hard: full intercept lead, error 13 (try 1: 14), saucers search for you: **1 in 6.6-7.2**
  (target 1 in 6). Nightmare: error 12.5 (try 1: 14), saucers in pairs: **1 in 3.4** (try 1: 1 in 4.5-5.5; target 1 in 4). Turret / saucer
  counts and fire rate scale as before (tier 3: 12 / 16 / 20 turrets). Warnings, dodgeable bolts and Hard's covered-pad share unchanged.
  Numbers: `turretLeadLevels`, `turretInterceptLevels`, `turretAimErrorLevels` (14, 14, 13, 12.5), `boltHitRadiusLevels`,
  `saucerHuntLevels`, `saucerPairLevels`, `saucerHuntRadius` 600, `saucerHuntEvery` 15.
- **End — done.** Published 0.9.0 + starter-900; CLAUDE.md updated. Clean-up: 11.5 GB -> 9.2 GB.

**Tuning added (CampaignTuning.json):** missions 1-3; rewards recorder 700, nest 1000, timed 800, clean 900; recorder hold 8 s within 70 m;
nest 3-5 turrets within 380 m; timed 540 s; salvage hold 12 s within 70 m, back at 40 % damage, ambush .4 at 1100 m; insurance 4 %,
insured recovery 20 %; trial 14 km, legs 1.1-1.9 km, gates 550 m apart (first 350 m), radius 24 m, miss +5 s; the G tables above.

**Steam artwork on the Deck (for Dan):** start the launcher once (it updates to 0.9.0 and starts the game: the game puts the six
`HostileHorizon-*.png` files beside the launcher; the launcher shows the portrait from its next start). Then in Desktop Mode, Steam
Library: right-click the game -> Manage -> Set custom artwork -> `HostileHorizon-Portrait.png`; the game's tile in the Recent games shelf ->
Manage -> Set custom artwork -> `HostileHorizon-Wide.png`; on the game's page right-click the empty banner -> Set custom background ->
`HostileHorizon-Hero.png`, and Set custom logo -> `HostileHorizon-Logo.png` (Adjust logo position if wanted); Properties -> the icon
square -> `HostileHorizon-Icon.png`.

**For Dan to check:** the mothership openings while docking; the ship's rear in the chase view; the launcher's cover; the Steam artwork
on the Deck; the side missions (Control's lines by ear); recovering your own wreck; insurance; the star map's best runs; a time trial with
the ghost (are the gates well placed at speed?); Hard and Nightmare enemies (hit rates just off target: your call).

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
