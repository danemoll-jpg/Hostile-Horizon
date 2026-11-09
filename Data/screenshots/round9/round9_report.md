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
