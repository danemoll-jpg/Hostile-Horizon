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
Current Objective — Round 7: fixes (music, aliens up close, play log, the alien warning) + three more biomes
---------------------------------------------------------------------------

Fix 6.3 is delivered as 0.6.3 (status under "Previous deliveries"). Dan
(2026-11-08): "HOTAS is working as expected now." Dan then asked for more
biomes and polish next: this round is **Part A** (the fixes, items 0-5, commit)
and **Part B** (three more biomes), one publish: **0.7.0**. Polish is Round 8;
missions move to Round 9.

**Part A — fixes**

Dan (2026-11-08, PC, 0.6.3):
- "I may be mistaken but I think the music disappears on planets after a bit."
- "Still getting a warning about the aliens." (Which warning is being asked;
  item 3 makes the next one visible in the log either way.)
- "When they appear the graphics aren't very good. They are too close and you
  can kind of see through their bodies."

0. **Commit first** (the repo as it is, with this text).
1. **Music that doesn't disappear.** Find out with data why the flight music goes
   away. Planning chat's reading of `GameShell.Audio.cs` `MusicTick`: the flight
   track goes silent whenever **any** pilot on foot is within 700 m and the ship
   is under 250 m above the ground (`NearWaitingPilot`), and while landed; with
   pilots about 700 m apart and the ship flying low in the canyons, that may be
   most of the time. Also check the loop / crossfade logic over a long flight
   (20+ min simulated, every biome track), and the tension layer taking over.
   Fix: the flight music only drops out for the pickup itself: when the ship is
   **landed or hovering for a pickup within about 250 m of the pilot it's
   going for** (the route target), and comes back as soon as you leave; never
   silent just for flying past a pilot. Log the reason whenever the music goes
   quiet (item 3).
2. **Aliens up close look right.**
   - **"See through their bodies":** find the cause (a transparent / alpha
     material from Meshy's textures, back faces culled on a one-sided mesh,
     inverted normals, the cockpit camera's near clip plane (.05 m) cutting into
     the model, the low LOD used close up, or the canopy glass shader's draw
     order). Fix it so the creature is solid from every angle, inside and
     outside the cockpit and through the canopy glass.
   - **"Too close":** the strike and the canopy pose put the creature's body
     through or right at the camera. Stage them so the player sees it: the
     canopy creature pressed against the outside of the glass at a readable
     distance (head and shoulders filling the view, not inside the camera);
     the cockpit strike stops about 0.5-0.8 m from the player's eyes, face lit,
     body visible behind it (VR rule 5 still: it comes at you, the camera
     isn't moved).
   - Up close it always uses the **full-detail** model and textures (no LOD
     swap), with a light that shows its shape (a rim / cockpit light on it).
   - Check with pictures from the cockpit for each of the five creatures: on
     the canopy, at the strike, in the cabin camera, that Code looks at and
     describes (solid? readable? scary?), before / after.
3. **A play log Dan's planning chat can read.** In players' builds, write game
   events (not every frame) to `Player.log` with a `[Play]` prefix: launch,
   planet / biome, pilots found / boarded / lost, every alien event (type,
   tell shown or not, door, canopy, strike type and time), every music change
   with the reason (track, quiet: why), every computer / Control voice line,
   docking, deaths. Short lines, so the planning chat can read what happened
   in Dan's session from his PC.
4. **The alien warning** — Dan: "the cockpit shuts down and the music
   starts", before the creature is on him. That is the **slow burn** strike
   (`GameShell.Strike.cs`: 25 %, `Horror(..., 3, ...)`: lights and displays
   die, cabin noises, hatch hits, before the attack), which still announces
   it. Dan wants the jump scare. **Remove the slow burn** (weights become
   Instant about 55 % / On take-off about 45 %, tuning). In every strike,
   the order is: the creature is on the player first (hatch bursts, lunge),
   and only then the sting, the cut of the music, the lights / displays going
   and the INTRUDER call. Nothing (no music, sting, sound, light, display,
   input or message change) before the creature is visible. Check that no
   tension / regeneration / other track can start before it. Prove with the
   `[Play]` log and the event comparison from Round 6.
5. **Check (muted, no focus):** item 1's music on a long low flight past pilots
   in each biome (logged: it keeps playing, drops only for the pickup); the
   creature pictures; the `[Play]` lines from a full campaign run.
6. Commit Part A (publishing happens at the end of the round).

**For Dan to check:** does the music stay with you while flying? Let an alien
in or wait for one on the canopy: does it look solid and readable now? If
anything strange happens, say so: the planning chat can read the `[Play]` log.


**Part B — three more biomes** (Dan, 2026-11-08: "I want more biomes")

B1. Add **Volcanic**, **Crystal Forest** and **Ocean Islands** to the biomes
    a planet's seed can pick (Round 4's rules: one biome per planet, same seed
    = same biome, the first planet of a career is Canyons; Free Flight's biome
    picker gets them). Same game rules everywhere: landing pads, pilots and
    wrecks, aliens, turrets and saucers placed to fit, radar, route trail,
    recall / tow, crash levels.
B2. **Volcanic:** black basalt canyons and cliffs, **lava rivers and lakes** in
    the valley floors (glowing, they light the canyon walls from below; flying
    into lava is a crash at any crash level except Ghost; low over it: heat
    shimmer and a hull-heat warning), ash falling, smoke plumes, glowing vents,
    a dark red sky. Pads on cooled rock shelves above the lava.
B3. **Crystal Forest:** huge glassy crystal spires and clusters (10-200 m),
    some translucent and glowing from inside in cold colours, refracting the
    light; fields of smaller crystals; dark ground with luminous fungus /
    moss; a pale violet sky. Crystals are solid (crash levels apply); thin
    crystal shards break when hit (debris, chime-like shatter), big spires
    don't. Pads in the clearings.
B4. **Ocean Islands:** open sea with **tall rocky islands, sea stacks and
    arches** to fly between and through, cliffs, surf breaking at their feet,
    a moving water surface with reflections, sea spray low down, gulls-like
    alien fliers (harmless, few). Hitting the water hard is a crash; skimming it
    throws spray. Pilots crashed on island tops, beaches and ledges (the pads);
    turrets on cliff tops; saucers over open water.
B5. Each gets its own sky, light, weather, fog, materials and particles, as in
    Round 4; its own Suno flight track (`flight_volcanic.mp3`,
    `flight_crystal.mp3`, `flight_ocean.mp3`; prompts in `Docs/MUSIC.md`;
    Dan's tracks are **already in `BundleMusic/`** (2026-11-08, put there by the
    planning chat: volcanic 3:07, crystal 3:34, ocean 3:10); loop them like the
    others and ship them in a soundtrack update); and new voice
    lines where useful (Control naming the biome at launch, the computer's
    "Hull heat" warning over lava).
B6. Performance as Round 4 (instancing, LOD, the Deck setting thins them);
    report frame rate per new biome on PC and the Deck setting.
B7. **Check (muted, no focus):** per new biome: cockpit pictures flying low, a
    landing at a pilot, a turret, the route trail; lava / water crash and heat
    warning; crystal shard break; the same seed = the same biome; frame rate.

**End of the round:** publish **0.7.0** (Part A = items 0-5 above, Part B), update
`TODO.md` (per-item status for Part A and B1-B7: done / not done / deferred and
why; the causes found) and `CLAUDE.md`, clean up, commit and push. Tell Dan if
the three Suno tracks aren't in yet.

**For Dan to check (Part B):** do the volcano, crystal and ocean planets feel
different and good to fly? Lava, the arches, the crystal spires.


---------------------------------------------------------------
Next Steps — queued rounds (Do Not Start Yet)
---------------------------------------------------------------------------

- **Round 8 — Polish (Dan, 2026-11-08: next after Round 7):** the ship, cockpit,
  mothership, turrets and saucers made through Dan's art pipeline (concept art
  -> Meshy -> Blender clean-up; cockpit detailed for VR), a sound pass, Steam
  Deck tuning, credits.
- **Round 9 — Missions and more progression:** mission objectives (recover a
  flight recorder, knock out a turret cluster, timed rescues, no-damage runs),
  recovering your own wreck yourself, insurance, best run per planet.
- **Enemies more of a threat** (DEFERRED by Dan 2026-11-07 until he's used to
  them; not decided against): turrets that hit a moving ship sometimes on
  Normal, saucers that hunt, more fire. Round 5's tuning note
  (`turretLead`, `turretAimError`, `boltHitRadius`) applies.
- **Round 10+ — VR:** OpenXR, PC VR through Virtual Desktop / SteamVR first:
  head-tracked cockpit, world-space menus, comfort options (all OFF by
  default: no snap turn, no fades, no vignette; Dan wants realism), VR performance
  pass. Native Quest decided afterwards.
- **Free Flight time trial** (DECIDED yes): gates, timer, best time per seed.
  After Round 9 unless Dan moves it.

---------------------------------------------------------------------------
Previous deliveries
---------------------------------------------------------------------------

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

**Fix 6.1 — menus with the HOTAS, and a clean controls page per device (2026-11-07). Published: 0.6.1 (`game-601`, Latest; soundtrack
stays `soundtrack-401`).** Exe built 2026-11-07 19:14. Evidence `Data/screenshots/fix61/` (`exe_check_1080.txt`, pictures at 1080p and
`deck_*` at 1280 x 800, Dan's HOTAS logs). Every run headless, muted (output 0.000 RMS), no window, Below Normal. Commit `167a346` planning.
- **0 Commit first — done** (`167a346`).
- **1 Menu navigation — done. Cause (logged on Dan's HOTAS through Windows' joystick / HID calls while Dan moved it; a batch-mode game sees
  no real joystick):** the stick's left / right reads **-0.50 when centred** (HID 258 of its declared 0..1023; it reaches 0 left but only
  686 = +0.34 right; forward / back is fine, 511 centred). The 0.6.0 menu fired Left at |x| > .55 whenever |y| < .55, so each time the
  stick sagged through the middle leaning a little left it crossed -0.55 and changed the row's value (Dan's 16 s up / down recording:
  2 value changes under the 0.6.0 rule). The same offset made the ship roll at -0.5 with the stick let go and (the action took the bigger
  input) hid small pad stick moves while the HOTAS was plugged in. Twist / throttle were not the cause (not bound to menus). Fixed: the
  HOTAS stick has its own action, **calibrated** (centre + each side's reach; centred on the stick's first report, reach learnt as it
  moves, or Settings > Controls > HOTAS setup > Calibrate stick); menus use the dominant axis with a lock (`MenuPanel.NavReader`: up / down
  |y| > .55 and > |x|; left / right only |x| > .6 and > 2|y|; after one, the other waits until both axes are under .25). Check: Dan's
  recording and a lean / diagonal trace: **0 value changes** (0.6.0 rule on the same input: 2 and 8); left / right still change values;
  the hat moves rows; twist and lever never navigate; HOTAS full right / left = roll +1 / -1, centred 0; a pad's 0.30 stick is no longer
  hidden.
- **2 Controls for the device in use — done.** `Controls.Btn(Act, device)` names every button per device; the device switches on a clear
  push only (1.5 s of HOTAS jitter left the pad as the device). Advice line, airlock / brake / engines / recall / belly / cabin / route
  prompts, the HUD and cockpit pause hint, menu footers, star map, Field Journal, seed entry. Pad: Xbox names and colours (A green, B red,
  X blue, Y yellow), LB / RB / LT / RT, D-pad arrows, ☰ Menu, **View** (the font draws ⧉ blank, so it says "View"). HOTAS: printed symbols.
- **3 Controls page — done.** Settings > Controls: Showing (the device used last; left / right shows another), HOTAS setup (when one is
  plugged in), Back; groups Flying / Landing and pickup / Combat / Views / Ship, action | button; a code-drawn picture of the HOTAS (throttle
  + stick, a label and line from every button, the hat, twist and lever; PS greyed "not used") or the pad; keyboard: the list only. The
  symbols are drawn larger than the font's own (it draws □ ○ △ at half height).
- **HOTAS button names and layout.** Dan's unit sends trigger 1, R3 2, R1 3, L3 4, □ 5, ✕ 6, ○ 7, △ 8, L1 10, SHARE 11, OPTIONS 12
  (logged; L2 not pressed, 9 assumed); 12 buttons + hat. 0.6.0 bound buttons 13-16, which it doesn't have (level out on 14 never worked).
  The new layout mirrors the pad: R2 fire / select, ✕ engines, ○ airlock / menu back, □ land / brakes, △ view, SHARE hold recall, OPTIONS
  pause, R1 shield, R3 level out, L1 belly camera, L2 route target, L3 cabin camera. Old settings (version 1) get it once.
- **4 HOTAS setup — done.** Calibrate stick, Identify your buttons (each symbol lit on the picture; 8 s skips one), Throttle axis, Invert
  lever, every button action ("press a button to assign"; a button already in use swaps), live stick (raw and as used) / twist / lever /
  hat / buttons held; no keyboard text. 8 rows on screen, the rest scroll. Check: identify with L1 / L2 swapped renamed them; Pause
  assigned to L3 (the cabin camera took OPTIONS); calibration with Dan's reach gave centre -0.46, and the centred stick reads 0.
- **5 Check — done** (`-exeCheckOnly fix61`, `Checks/ExeCheck.Fix61.cs`: a virtual HOTAS plays Dan's recorded trace; `fix61pics` for
  1280 x 800). Looked at: readable at 1080p; at 1280 x 800 everything fits and reads, smaller (list ~18 px, picture labels ~16 px).
- **6 Publish — done.** 0.6.1 = build 601, `game-601` Latest; a fresh `starter-200` install updated to `versions/601`, ready, exit 0.
  Clean-up: 3.8 GB -> 3.7 GB.

**For Dan to check:** with the HOTAS, move up and down the Settings: do values stay put? Run Settings > Controls > HOTAS setup >
Calibrate stick once (then the ship shouldn't roll with the stick let go), and Identify your buttons if any name on the picture is wrong
(L2 was guessed). Is the Controls page readable and does it show only the HOTAS? Do the prompts switch to Xbox buttons when you pick up
the controller? Thrustmaster's own fix for the off-centre stick may be to unplug / replug it without touching the stick; if it then reads
centred, run Calibrate stick again.

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
