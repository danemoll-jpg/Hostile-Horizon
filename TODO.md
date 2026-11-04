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
Current Objective — Round 5 (to be written by the planning chat)
---------------------------------------------------------------------------

Round 4 (planet biomes, music and voices) is delivered as 0.4.0 (status under "Previous deliveries"; its specification is in
`TODO-ARCHIVE.md`). **Next: Round 5 (Enemies + difficulty)**, to be written by the planning chat with Dan.
Open for Dan from Round 4: how the biomes fly and look (Fix 4.1 / 0.4.1: Dan's own tension and regeneration music are in),
the voices and how much chatter, the music and its loop points with his tracks, any hitch in the forest / city.
Dan's art pipeline (DECIDED, standing): Code writes image prompts -> Dan has the concepts drawn -> Meshy -> Mixamo -> Code wires them
(`Docs/MODELS-NEEDED.md`). Still open from 0.3.0: the route corridor width / height.

---------------------------------------------------------------------------
Next Steps — queued rounds (Do Not Start Yet)
---------------------------------------------------------------------------

- **Round 5 — Enemies + difficulty:** turrets on ridges, saucers, enemy fire,
  shields and hull, damage; Campaign difficulty presets + custom options.
- **More biomes (after Round 4):** volcanic, crystal / fungal forest, ocean
  islands, each with its own Suno flight track.
- **Round 6 — Modern progression:** the upgrade shop (credits), mission
  objectives, the full star map (builds on Fix 3.2's planet list; typed seed;
  maybe new distress calls appearing on visited planets over time), recovering
  your own wreck yourself, insurance, best run per planet.
- **Round 7 — Look and sound polish:** art pass (ship model, cockpit,
  mothership; pilots and aliens are done), sound pass, Steam Deck tuning,
  credits.
- **Round 8+ — VR:** OpenXR, PC VR through Virtual Desktop / SteamVR first:
  head-tracked cockpit, world-space menus, comfort options (all OFF by
  default: no snap turn, no fades, no vignette; Dan wants realism), VR performance
  pass. Native Quest decided afterwards.
- **Free Flight time trial** (DECIDED yes): gates, timer, best time per seed.
  After Round 6 unless Dan moves it.

---------------------------------------------------------------------------
Previous deliveries
---------------------------------------------------------------------------

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

**Round 4 — planet biomes, music and voices (2026-11-04). Published: 0.4.0 (`game-400`, Latest) + soundtrack 0.4.0
(`soundtrack-400`).** Exe built 2026-11-04 14:14 (`BUILD_INFO.txt`). Evidence `Data/screenshots/round4/`: `final/` (`--only round4`:
`exe_check.txt` + pictures a, b_* Free Flight per biome, c_* Campaign per biome), `deck/` (`--only round4perf -steamDeck 1`, 1280 x 800),
`mute_exe_check.txt`, `launcher/`. Every run headless, muted (output 0.000 RMS from the first frame to the end), no window, Below Normal.
Commits: `4785e13` planning, `9227ed5` Part A, `57e15d9` Part B, `f9a0c6a` Part C (the three parts were built and checked together; the
GameShell glue is in the Part C commit, so only that one builds on its own).
- **A1 Biome per planet — done.** `Biomes.ForSeed` (seeds 1-2000: 502 / 506 / 488 / 504); a new career's first planet is Canyons
  (check: "canyons" while today's seed is a Dead City); old saves get their biome on the next visit (`biomeSet`). Shown in the hangar,
  the Planets list (every row), the jump screen, the HUD place line, the title ("Today's planet: Zanveth-IV (Dead City)"); Free Flight
  has **Planet type** (By seed / Canyons / Giant Forest / Dead City / Ice). Same seed = same ground (identical samples, all four).
- **A2 Giant Forest — done.** Rolling ground, wide valleys; trees one per 64 m cell (86 %), trunks 5-15 m, 60-150 m tall, deep crowns;
  valley floors and clearings (the pads) open. Trunks solid (`CanyonField.SolidPush`: flown into at 210 km/h = one hit, pushed out, no
  crash on Bounce); crowns soft (leaf burst + rustle, no hit). Soft light shafts into the clearings, spores, green-gold haze. b_forest_*.
- **A3 Dead City — done.** Street grid in the height field (32 m streets, 64 m avenues), towers 60-500 m (downtown taller), broken tops,
  rubble blocks, plazas; facades with a window grid and a few flickering dead lights (shader); ash. Pads: flat roofs (81 of 117 in a
  test area) and plazas; the check landed on a roof 245 m up and picked a pilot up there. Sky bridges are solid boxes (a hit, logged).
- **A4 Ice — done.** Snow plains stepped by ice cliffs, narrow deep crevasses (the canyons), ice pads on crevasse floors; snow bridges
  over crevasses = short ice caves you fly under (solid); snow; blizzards every 2-4 min for 25-45 s (white haze to 450 m, the computer
  says "Visibility low. Instruments only."). b_ice_*.
- **A5 Look per biome — done** (`WorldLook.LookFor`: sky, sun, haze, ambient, strata palette, ground mode in `Rock.shader`, particles).
  Wrecks and pads use the biome's ground; the wreck model itself is unchanged.
- **A6 Performance — done, numbers for Dan.** Trees GPU-instanced (2 LODs, near ones with shadows), bridges per cell, props from the seed
  on worker threads; the Deck setting draws trees to 60 % of the view and the detailed ones only to 420 m (collisions never change).
  1660 Ti, 1080p, flying low, average / 1 % low fps: Canyons 862 / 289, Forest 489 / 120, City 592 / 157, Ice 497 / 143 (final run; a
  Chastain Rush build was running at the same time). An earlier run had Forest 301 / 34 and City 388 / 64: short hitches (worst frames
  0.3-0.7 s) while new props / music load. Deck setting (1280 x 800 on this PC, not a Deck): 1295 / 629, 1201 / 507, 1501 / 439, 1006 / 353.
- **A7 Art — done by script.** Trees, buildings and ice are made in code (no Meshy needed); no third-party textures (credits updated).
- **A8 Later biomes — queued** (volcanic, crystal / fungal, ocean islands; "Next Steps").
- **B1 Adaptive music — done** (`Audio/Music.cs`): title, hangar, planet list, flight per biome, tension layer at low energy / RETURN NOW,
  regeneration cue; 4 s crossfades; silence (wind) near a waiting pilot and landed for a pickup; no music in the horror sequence, the
  canopy alien or the Sleeper; ducks under voices. Check log: every change with its gain (final/exe_check.txt "--- music").
- **B2 Files — done.** `BundleMusic/` (git-ignored) -> `Music/` beside the exe (PlayerBuild) and the launcher's soundtrack component
  (`soundtrack-400`); the game reads the launcher's soundtrack folder (checked: the fresh install played from `shared/soundtracks/400`).
- **B3 ElevenLabs music and stingers — partly.** Stingers done (7, `Resources/Sounds/sting_*`, wired to launch, pilot aboard, docked,
  cleared, jump, alien reveal, death). **The two music loops are stand-ins: Dan's API key lacks the `music_generation` permission**
  (HTTP 401), so `tension_low_energy` (22 s loop) and `regeneration` (10 s) were made with the sound-effects model. Fix: allow Music on
  the key, then `python Tools/make_music.py --only tension_low_energy,regeneration --force` (Docs/MUSIC.md).
- **B4 Settings — done.** Settings > **Sound**: Master, Music (70 %), Effects, Voices, Subtitles.
- **B5 Dan's Suno tracks — done.** All 7 looped: `Tools/music_loops.py` finds tempo, intro, end fade and a bar-aligned loop where the music
  matches best (match .88-.95) -> `BundleMusic/music.json`; the game plays the intro once, then crossfades loopEnd into loopStart.
- **C1 Voices — done.** Control (Daniel), the computer (River + chorus), 12 pilot voices (6 women, 6 men; American, British, Australian,
  US Southern; young to old), each pilot's callsign = their voice. 245 lines (`Docs/VOICES.md`). Wired: launch, planet of the day,
  briefings per biome, beacons, cabin full / everyone found, docking, docked, tow, recall, cleared, jump, regeneration ("Welcome back.
  Again."), engines-off advice, low energy, alien / pilot killed by the exhaust, security; computer: energy low, RETURN NOW, can't
  reach, pull up, hull, gear, airlock, pilot aboard, cabin full, intruder, engines off, white-out; pilots: mayday (per biome), "I see
  you", at the door, thanks, cabin chatter, the Sleeper's victim screams.
- **C2 Aliens use the voices — done.** Aliens radio and talk at the door like pilots. **Mimic voice sign** (seeded, 75 % = the tell rate):
  wrong lines (a word repeats, a breath in the wrong place) or the voice and callsign of a pilot already lost there. Check: a Mimic
  radioed "Mayday, mayday, this is... this is Corps pilot Lund" and said "It's me. It's me. Open... open up." at the door; exhaust ->
  "That was no pilot." Over 400 planets the sign showed 300 times.
- **C3 Radio sound — done.** Radio: 350 Hz-3.2 kHz, drive, crackle, click in / out; computer clean; door voice 3D at the airlock,
  low-pass 900 Hz; one line at a time, warnings first (logged in order).
- **C4 Subtitles — done** (On by default): under the HUD prompt and on the cockpit's centre display (c_*_4 pictures).
- **C5 Files — done.** `Docs/VOICES.md`, `Tools/make_voices.py [--only id / prefix_] [--force]`, key from `ELEVENLABS_API_KEY`, never printed.
- **Publish — done.** 0.4.0 = build 400: `soundtrack-400`, then `game-400` (Latest; its catalog points at both). The `starter-200` ZIP from
  GitHub, `--auto-check`: "Updating to game 0.4.0… Updating the soundtrack… Update installed. Starting the game", `versions/400`,
  `shared/soundtracks/400`, ready, exit 0 (the game muted by the launcher's test-save flag).

**Decisions made:** the city is built in the height field (roofs are real ground: landing, collisions, the pilot's run); trees and bridges
collide from the seed, not from what is drawn (the Deck's thinner drawing never changes play); music stand-ins rather than none; pilots'
callsigns come from their voices (12 names). **For Dan to check:** do the forest, city and ice feel different and good to fly (and how the
tree crowns look)? Rooftop and clearing landings. Control, the pilots' voices, how much radio chatter. The music with your tracks: does
each place fit, are the loop points clean? Any hitch in the forest / city? The key's Music permission for the two real loops.

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
