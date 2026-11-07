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
Current Objective — Fix 6.1: menus with the HOTAS, and a clean controls page per device
---------------------------------------------------------------------------

Round 6 is delivered as 0.6.0 (status under "Previous deliveries"). Round 7
(missions and more progression) comes after this fix.

Dan (2026-11-07, first time with the HOTAS, on PC):
- "When I am selecting options, I can go up and down on the joystick to select
  the option I want to change. Then I can move left or right to change the
  selection, but when I pull up or down it does move, but it also changes the
  option I just left, so I can't keep it set."
- "Please clean up the HOTAS controls. It looks like just a big wall of text and
  I can't read it. Since it is the HOTAS settings I don't need to see the
  keyboard buttons. Also if I am playing with an Xbox controller just show those
  options."

Likely cause of the first one (planning chat, from `UI/MenuPanel.cs` Update):
the stick's direction is picked per frame by a plain threshold (|y| > .55 else
|x| > .55), so a stick pushed up/down that also leans sideways, or that passes
through a sideways angle on its way back to centre, fires Left / Right on the
row. Also check the Navigate action's HOTAS bindings (twist or another axis
must not feed it). Confirm with logged input before fixing.

0. **Commit first** (the repo as it is, with this text).
1. **Menu navigation that doesn't change values by accident** (all devices,
   sticks and hats):
   - Log the raw Navigate input from the HOTAS stick / hat / twist while moving
     up and down, find what triggers the Left / Right, report it.
   - Dominant-axis only, with hysteresis: Left / Right only when the horizontal
     push clearly dominates (e.g. |x| > .6 and |x| > 2|y|); after an Up / Down
     move, no Left / Right until the stick has come back to centre (both axes
     under about .25), and the same the other way round.
   - The twist and the throttle never navigate menus.
   - The HOTAS hat works for menus as a d-pad (it's the cleanest on the HOTAS).
2. **Controls shown only for the device in use.** The game tracks the active
   device (last one used: HOTAS, Xbox-style gamepad / Steam Deck, keyboard +
   mouse) and every control hint (Advice line, prompts like "press B to open",
   the pause hint, menus' footer) shows only that device's buttons, switching
   as soon as another device is used. Gamepad hints use Xbox button names /
   colours (A green, B red, X blue, Y yellow), LB / RB / LT / RT, D-pad, ☰ / ⧉.
3. **A clean Controls page** (Settings > Controls), showing only the active
   device (with a small switch to view another device's layout if wanted):
   - grouped and short: **Flying** (pitch / roll, yaw, throttle, level out),
     **Landing and pickup** (land / hover, engines, airlock, belly camera),
     **Combat** (fire, shield), **Views** (camera, look, cabin camera, route
     target), **Ship** (recall, pause);
   - two columns (action | button), large readable text, no long sentences;
   - for the HOTAS: **a picture of the HOTAS with every mapping on it** (Dan,
     2026-11-07: "there are lots of buttons"): the stick and the throttle drawn
     clearly (Code draws it; a reference photo of Dan's own unit is in
     `SourceArt/Reference/hotas_dan.jpg`), with a label line from each button,
     axis, hat and the twist to what it does in the game. Dan's unit is the
     **PS4 version of the T.Flight HOTAS 4**: its buttons are printed with
     PlayStation symbols (**△ □ ✕ ○** on the right side of the throttle, plus
     L1 / L2 / R1 / R2 / L3 / R3, SHARE / OPTIONS / PS), so the picture, the
     Controls page, the HOTAS setup page and every in-game prompt name HOTAS
     buttons **by the symbol printed on them** (e.g. "press ○ to open the
     airlock"), never "button 6". Map the HID button numbers to the printed
     symbols; then **a short "identify your buttons" step** on the HOTAS setup
     page (press each highlighted button on the picture in turn) lets Dan
     confirm or correct the mapping once, saved in settings. Unused buttons are
     shown greyed on the picture;
   - for the gamepad: the same with an Xbox controller picture (fine for the
     Deck too).
4. **HOTAS setup separate from the list:** a **HOTAS setup** page (shown when a
   HOTAS is connected): axis display, throttle axis, invert lever, the
   assignable buttons (pause, level out, ...), one per row, each with its
   current button and "press a button to assign". No keyboard text on it.
5. **Check (muted, no focus):** a recorded HOTAS-like input (up / down with a
   sideways lean, and returning through a diagonal) moves the selection
   without changing any value; left / right still changes values; pictures of
   the Controls page for HOTAS, gamepad and keyboard, and the HOTAS setup page
   — Code looks at them: readable at 1080p and on the Deck's 1280 x 800?
6. **Publish as 0.6.1**, update `TODO.md` (per-item status, the cause found in
   1) and `CLAUDE.md`, commit and push.

**For Dan to check:** with the HOTAS, move up and down the Settings: do values
stay put? Is the Controls page readable and does it show only the HOTAS? Do
the in-game prompts switch to Xbox buttons when you pick up the controller?

---------------------------------------------------------------
Next Steps — queued rounds (Do Not Start Yet)
---------------------------------------------------------------------------

- **More biomes (after Round 4):** volcanic, crystal / fungal forest, ocean
  islands, each with its own Suno flight track.
- **Round 7 — Missions and more progression:** mission objectives (recover a
  flight recorder, knock out a turret cluster, timed rescues, no-damage runs),
  recovering your own wreck yourself, insurance, best run per planet.
- **Enemies more of a threat** (DEFERRED by Dan 2026-11-07 until he's used to
  them; not decided against): turrets that hit a moving ship sometimes on
  Normal, saucers that hunt, more fire. Round 5's tuning note
  (`turretLead`, `turretAimError`, `boltHitRadius`) applies.
- **Round 8 — Look and sound polish:** art pass (ship model, cockpit,
  mothership; pilots and aliens are done), sound pass, Steam Deck tuning,
  credits.
- **Round 9+ — VR:** OpenXR, PC VR through Virtual Desktop / SteamVR first:
  head-tracked cockpit, world-space menus, comfort options (all OFF by
  default: no snap turn, no fades, no vignette; Dan wants realism), VR performance
  pass. Native Quest decided afterwards.
- **Free Flight time trial** (DECIDED yes): gates, timer, best time per seed.
  After Round 7 unless Dan moves it.

---------------------------------------------------------------------------
Previous deliveries
---------------------------------------------------------------------------

**Round 6 — jump scare, easier-to-find enemies, upgrades, star map, Field Journal viewer (2026-11-07). Published: 0.6.0 (`game-600`,
Latest; soundtrack stays `soundtrack-401`).** Exe built 2026-11-07 02:10 (`BUILD_INFO.txt`). Evidence `Data/screenshots/round6/`:
`partA/` .. `partE/` (`--only round6a` .. `round6e`), `final/` (`--only round6`: the evidence for A, B and E; C and D read x1.00 / timed
out there because Part A's deaths leave the check flying the loaner, which correctly has no upgrades: their own runs are the evidence),
`launcher/`. Every run headless, muted (output 0.000 RMS from the first frame), no window, Below Normal. Commits: `56d00e9` planning,
`bf9faa4` A, `e6f7eb3` B, `4dbaeef` C, `7bf628e` D, `98449c3` E (all five parts were written before the first build, so the A commit
already holds B-E code; each part was then checked and fixed in turn).
- **A0 Commit first — done.** **A1 Boards like a pilot — done.** An alien let in walks in, "Pilot 1 aboard (1 / 6), brought 7 % energy",
  chime, thanks line, cabin count, a seat, music unchanged, full control (`PilotBoarded`; `Core/GameShell.Strike.cs`).
- **A2 Strike — done.** Seeded per alien: instant 40 % (1-4 s after the airlock shuts), take-off 35 % (2-6 s after lift-off, or a turn
  over 70 °/s), slow burn 25 % (8-40 s of flight; the old long sequence). Instant / take-off: hatch slam and burst, at your face in ~0.4 s,
  the lunge, death at 1.9 s; music cut, sting and scare sounds **on** the strike; INTRUDER, lights out and alarm 0.35 s **after** it. An
  alien still aboard strikes before docking / the beam / running dry. Over 600 planets: 40 / 35 / 25 %, mean 2.5 s / 4.0 s / 24 s.
- **A3 Canopy leap — done.** 50 % leap (5-15 s at the door) / 50 % the slow climb (25 s); the leap: 0.3 s from the ground onto the glass,
  `canopy_slam`, view shake; engines throw it off (checked: leapt at 7.6 s, thrown off, bounty).
- **A4 Red herrings — done.** Per boarding, real pilots and aliens alike (seeded per pilot): flicker 12 % (two short flickers +
  `power_dip`), thump 15 % (`cabin_thump`), crackle 12 % (the cabin feed stutters + `feed_crackle`); never the full lights-out.
- **A5 Fair — done** (tells, voice sign, Sleeper unchanged). **A6 VR rule 5 — done** (no camera takeover or fade; the creature comes at you).
- **A7 Check — done.** Event-log comparison (sounds, stings, music, voices, messages, cabin lights; HUD / displays / controls / airlock /
  creature state), B pressed to the strike: **instant 16 / 16 events IDENTICAL, take-off 20 / 20 IDENTICAL**; the first alien-only event is
  the strike itself (7.45 s / 9.48 s after B). Slow burn struck 22 s after lift-off. Pictures r6a_*_strike_0.1s / 0.5s / 1.0s.
- **B1 Radar — done.** Turrets within 3 km and saucers within 6 km on the radar unspotted, labelled "TURRET 1.4 km" / "SAUCER 1.9 km"
  (every biome: every enemy in range on the radar at launch).
- **B2 Brackets — done** (`UI/TargetBrackets.cs`, in the world): enemies in sight within 2.5 km, "TURRET 1.9 km", the nearest / locked
  one brighter ("LOCKED ON").
- **B3 Visible from afar — done.** Blinking red warning light on turrets and saucers (0.008 m per m: ~16 m at 2 km), muzzle flash
  (0.009), enemy bolts' glow kept readable (0.006) with a 0.12 s trail. Accuracy, fire rate and aggression unchanged.
- **B4 Check — done.** Cockpit pictures at 2 km and 1 km, turret and saucer, all four biomes (r6b_*): bracket and light in every one. First
  run: labels too small and the light read white; fixed (bigger labels, saturated red).
- **C1 Shop — done** (hangar > Upgrades; `Core/Upgrades.cs`, `GameShell.Upgrades.cs`). Prices (cr): Engine 6500 / 10500 / 16000, Hull
  6000 / 9500 / 14500, Shields 6000 / 9500 / 14500, Energy 6500 / 10500 / 16000, Radar 5500 / 8500 / 13000, Cabin 7000 / 12000 (6 -> 8 -> 10),
  Gun 6000 / 9500 / 14500. Steps a level: engine +8 %, hull +25 %, shield drain -20 % and +1 m catch radius, energy capacity +20 %, radar
  reach +30 % and exact bearings +1 km, gun rate +20 % / damage +25 %. Shields' "more hits blocked" = the same energy blocks for longer and
  a wider shell catches near misses (it already blocked every hit it caught).
- **C2 Value — done.** Ship value 6000 + the prices paid; recovery 1500 + 25 %: no upgrades 3000 cr, everything 53 500 cr. Loaner: none.
  Saved in `career.json` (`ship.upgrades`).
- **C3 Visible — done.** Seat cushions per seat (the cabin camera shows 8 / 10), a radar bezel ring, extra reticle rings (Gun), whiter
  nozzle glow (Engine), a stronger, larger shield shell.
- **C4 Balance — done.** Normal, all pilots, 4 sorties at 40 %: 5020 cr after planet 1, 7540 after planet 2; cheapest level 5500: after two
  planets. All levels vs none, measured in flight: top speed 450 -> 558 km/h, hover climb x1.13, flight drain x0.63, shield drain 2.0 -> 0.8
  %/s, a 10 % hit 10 -> 5.7 %, gun 6.0 -> 9.7 shots/s, enemy radar reach 3000 -> 5700 m, cabin 6 -> 10 (10 pilots seated).
- **D1 Star map — done** (replaces the planet list; hangar "Star map · Planets"): the star, every visited planet, today's and the next
  undiscovered one in biome colours with status (★ of the day, cleared, waiting, DISTRESS) and danger (tier, difficulty, turrets / saucers
  left, or an estimate); the selected row is ringed on the map; A jumps (live, as before). Drawn by `UI/ViewStage.cs` into the page.
- **D2 Coordinates — done.** Type a seed (keyboard or D-pad digits), "Go to the coordinates"; checked: typed 24 -> Tulra-VI, on the map.
- **D3 Distress calls — done.** A cleared, visited planet rolls every 3 h of real time (35 %): 1-3 new pilots (aliens by the usual rules,
  cap 14), marked on the map, Control's "New distress calls…" (`ctl_distress`) and a debrief line at the next dock; no second clear bonus.
  Checked with the clock moved 30 h: 3 new pilots, announced, jumped there, 3 waiting.
- **E1 Viewer — done** (`Core/GameShell.Journal.cs`). An index of the five kinds; a page each: the model large and lit, turn (right stick /
  right-mouse drag / hat: 132° in 1.2 s), zoom (triggers / Shift-Ctrl / wheel: 5.7 -> 2.0 m), animations idle / walk / run / attack /
  shriek and its own (leap, stagger, climb, circle, sit); unmet: a black silhouette on grey, "Unknown".
- **E2 Information — done.** Name, original lore (more after the 2nd meeting), behaviour, the tell hint (as before), record (met, killed,
  killed you, first seen on <planet> (date)).
- **E3 Pause menu — done** (time stopped; the creature still animates). Pictures r6e_*.
- **Publish — done.** 0.6.0 = build 600, `game-600` Latest (the first attempt hit the draft-listing delay; `--resume-draft` finished it).
  The `starter-200` package: "Updating to game 0.6.0… Updating the soundtrack… Update installed. Starting the game…", `versions/600`, exit 0.
  New sounds (ElevenLabs; nothing played): hatch_burst, canopy_slam, power_dip, cabin_thump, feed_crackle; voices ctl_distress, ctl_upgrade. Clean-up: disk 3.2 GB -> 3.2 GB (starter-check folder removed, Library/BuildHistory cleared).

**Decisions made:** an alien still aboard strikes before docking / the beam / running dry (it never reaches the mothership); the comparison
uses a check-only `Trace` log; Shields' "more hits blocked" as above; distress calls pay no second clear bonus; the star map shows the 16
most recent planets + today's + the next new one.
**For Dan to check:** let a few aliens in: do you still see it coming, does the instant one make you jump? Wait at the door for a leap. Can
you find the enemies now (brackets, lights, radar labels; is the radar too busy)? Upgrade prices and feel; is a 53 500 cr recovery with
everything bought too harsh? The star map (planets are small in the picture) and distress calls. Turn and zoom an alien in the journal.

**Round 5 — the ship rights itself; enemies and difficulty (2026-11-04). Published: 0.5.0 (`game-500`, Latest; soundtrack stays
`soundtrack-401`).** Exe built 2026-11-04 20:07 (`BUILD_INFO.txt`). Evidence `Data/screenshots/round5/`: `partA2/` (`--only round5a`),
`partB/` (`--only round5`, pictures r5_*), `deck/` (`--only round5perf -steamDeck 1`, 1280 x 800), `presets/`, `launcher/`. Every run
headless, muted (output 0.000 RMS from the first frame), no window, Below Normal. Commits: `329991a` planning, `2a2feb2` Part A, then Part B.
- **A0 Commit first — done.** **A1 Self-righting — done.** Full / Light, stick released .25 s: roll wings-level the short way, nose to the
  horizon (6° climb below 25 m), heading kept; any stick output stops it. Light: 60° bank level in 1.4 s, 120° 1.9 s, upside down 2.3 s
  (0 m lost), nose-down 45° 1.4 s (49 m lost), inverted + 30° down 2.4 s (31 m). Full: 1.1 / 1.5 / 1.9 / 1.2 s (Full's bank turn turns
  it 20-34° meanwhile). Slow 3.5 s / Fast 1.7 s from upside down (times include the .25 s wait).
- **A2 Level-out button — done.** L-stick click / L / HOTAS 14 (picked in Settings > Controls and HOTAS): upside down level in 1.1 s,
  with the stick held 0.85-1.1 s; works on Flight assist Off too.
- **A3 Settings — done.** Settings > Auto-level (On) and Righting speed (Slow / Normal / Fast); Graphics quality and Steam Deck moved to
  Settings > Display (the page holds 12 rows).
- **A4 Hover — done.** Landing mode from upside down: upright in 2.2 s, body turn capped at 140 °/s (0.4.x: up to ~400 °/s).
- **A5 Check — done.** Fix 2.5's list: while the stick is held only the stick (and Light's pull-only bank turn) turns the ship; the first
  run found righting acting on a 25 % pad roll (1.6 % after the deadzones), fixed (any output = steering), clean in the second run.
  Canyon run with 1.2 s releases between turns: 0 wall touches (Light, Full, and Light with Auto-level off as the baseline). Pictures
  r5a_low_inverted_before / _after (upside down over the Dead City -> level, same heading). **A6 Part A commit — done** (`2a2feb2`).
- **B1 Turrets — done.** On the high ground from the seed (canyon rims, roofs, ice cliffs; forest: giant stumps above the canopy);
  line of sight over the ground and through no tree crown; lock 1.2 s, a bolt every 1.8 s (230 m/s, lead .9, error 14 m at 1 km),
  7 % a hit, 4 gun hits to kill. **Not met: turrets rarely hit a ship crossing at 650 m** (0 hits in 4-14 shots per biome, straight or
  weaving; they did shoot the check's slow ship down from 85 % at 450 m). Tuned twice (lead .6 -> .9): the tuning limit; Dan decides
  (`turretLead`, `turretAimError`, `boltHitRadius` 3.5 m).
- **B2 Saucers — done.** From a career's second planet on; chase on sight or when you land / hover within 3 km; passes, bursts of 3;
  above the rock and the forest. Each biome: chasing in 0.3-1.9 s, 3-5 shots in 10 s, destroyed by the gun in 1.1-1.3 s.
- **B3 Gun — done.** Held A / Space / left mouse / HOTAS trigger (A still does the engines on the ground), 6 shots/s, .12 % energy a
  shot; reticle 60 m ahead in the world. Turret kills 1.2-1.7 s (one canyon turret on a rim edge took 60 shots, 0 hits: the rim hid
  its body from the check's angle). Bounty 150 (turret) / 300 (saucer) x pay.
- **B4 Shield — done.** D-pad left / Z / HOTAS 2; blocks bolts (1-3 blocked per biome), drains 2 %/s (17-19 % in ~9 s with flight);
  on with something at the door: a Mimic burnt (+200 bounty, journal), a real pilot killed (-400). Faint blue shell in the world.
- **B5 Damage — done.** Hits: hull %, shake, sparks, HIT -7% on the HUD / centre display; hull 0: "Shot down by a turret", the
  regeneration trip, the loaner.
- **B6 Warnings — done.** Red arrows round the HUD centre and red marks on the radar rim toward the shooter (2.5 s), LOCKED ON (HUD,
  centre display), computer "Warning. Locked on." / "Saucer. Saucer." / "Shield up / down" / "Target destroyed.", enemies on the radar
  once spotted, Control's "this one's hot" at launch; hangar "Defences: N turrets, M saucers".
- **B7 Pads — done.** Easy / Normal: no turret sees a pilot's pad (first planet: landed and picked up, 0 shots); Hard 2 of 5 pads
  covered, Nightmare 3 of 5 (test planet, tier 1).
- **B8 Difficulty — done.** Campaign page "Difficulty (new career)" + options; hangar "Difficulty: X" (changes apply at once; alien numbers
  to new planets). Presets (aggression x / tells / false tells / crash level / pay): Easy .6 / 95 % / 0 / Regenerating / x.8; Normal 1 /
  75 % / 0 / Lasting / x1; Hard 1.35 / 60 % / 10 % / Lasting / x1.25; Nightmare 1.7 / 50 % / 20 % / Lasting / x1.5. Measured over 300
  planets: tells 97 / 74 / 60 / 49 %, false tells 0 / 0 / 8 / 20 %; aliens 17 / 33 / 33 / 33 % (whole pilots on a 6-pilot planet).
- **B9 Ease-in — done.** First planet: 3 turrets, no saucers; second on: 6 turrets + 1 saucer (Normal, tier 0), +2 turrets / +.5 saucer a tier.
- **B10 Models and sound — done.** `Tools/Blender/enemies.py` (turret 3.0k triangles, saucer 4.2k); 12 ElevenLabs sounds, 7 voice lines.
- **B11 Performance — done.** Enemies active, 1080p average / 1 % low: Canyons 233 / 77, Forest 180 / 38, City 190 / 40, Ice 166 / 25;
  Deck setting 179 / 34, 245 / 60, 314 / 69, 209 / 29. Bolts and effects pooled (96 bolts, 16 sparks, 4 blasts).
- **B12 Check — done** (`Checks/ExeCheck.Round5.cs`). The first run's frame rates included ground loading after the check's teleport
  (fixed: it waits for the ground). The last build changed only Hard / Nightmare pad cover (re-checked: `presets/`).
- **Publish — done.** 0.5.0 = build 500, `game-500` Latest; the `starter-200` package, `--auto-check`: "Updating to game 0.5.0… Updating
  the soundtrack… Update installed. Starting the game", `versions/500`, ready, exit 0. Clean-up: 3.3 GB -> 2.8 GB.

**Control layout (Round 5):** gamepad: A fire (on the ground: engines), D-pad left shield, left-stick click level out (rest as before);
keyboard: Space / left mouse fire, Z shield, L level out; HOTAS: trigger fire, button 2 shield, button 14 level out (Settings).

**For Dan to check:** let go of the stick when turned around: does it come upright quickly and smoothly, never fighting you? The
level-out button. Can you see the shots coming and from where? Should turrets hit more (they rarely hit a moving ship now)? Is flying low
to stay out of sight fun; are saucers too much? The gun / shield buttons on the pad (A fires in the air), the HOTAS when you can.

**Fix 4.1 — Dan's tension and regeneration music (2026-11-04). Published: 0.4.1 (`game-401`, Latest) + soundtrack 0.4.1
(`soundtrack-401`).** Exe built 2026-11-04 16:41. Evidence `Data/screenshots/round4/fix41_music/` (`exe_check.txt`, launcher `auto-status.txt`).
- **The two stand-ins replaced — done.** Dan made them himself (the API key still refused music after he updated it): `rising_tension.mp3`
  (120 s) -> `BundleMusic/tension_low_energy.mp3`, loop 65.7 -> 107.3 s (match .94, 1 s crossfade; the first 65 s play once as its build-up);
  `regenerate.mp3` (30 s) -> `BundleMusic/regeneration.mp3`, a one-shot (0.4.0's stand-ins were 22 s / 10 s). No game code changed for it.
- **Check — done.** New `-exeCheckOnly music` (`Checks/ExeCheck.Music.cs`, 1 min; `Music.CheckWant / CheckTension` set the state): the tension
  layer loads the 120.6 s file under the flight track, moved to 104 s it crossfades back into the loop (68.4 s after 6 s); regeneration plays
  its 30.2 s once, then silence (no loop). Headless, muted (output 0.000 RMS), no window.
- **Publish — done.** `soundtrack-401` (only the two new files and music.json uploaded), then `game-401`. The `starter-200` ZIP from GitHub,
  `--auto-check`: "Updating to game 0.4.1… Updating the soundtrack… Update installed. Starting the game", `versions/401`,
  `shared/soundtracks/401` with both of Dan's files (same sizes), exit 0.

**For Dan to check:** the tension layer under low energy / RETURN NOW (its slow 65 s build-up plays first each time it comes in) and the
regeneration cue on the trip to the pod.

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
