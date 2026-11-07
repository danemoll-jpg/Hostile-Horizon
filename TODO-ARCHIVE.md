Hostile Horizon — TODO archive

Older rounds and finished round specifications, moved here word for word from `TODO.md`
(grep it, don't read it whole).

=== Fix 2.1 specification (as the planning chat wrote it, moved from TODO.md at the end of Fix 2.1) ===

---------------------------------------------------------------------------
Current Objective — Fix 2.1: full screen on the Steam Deck (do this now)
---------------------------------------------------------------------------

Dan (2026-11-02, on the Steam Deck, 0.2.0): "the screen is not fullscreen".
Cause found by the planning chat: Chastain Rush's `ScreenSetting.cs` was never
ported. Hostile Horizon sets nothing at start-up, so it opens at the project's
default 1920 x 1080 (or whatever size Unity last saved), not at the Deck's own
1280 x 800 screen.

0. **Commit first** (the repo as it is, with this text).
1. **Port `ScreenSetting.cs` from Chastain Rush** (`Assets/Scripts/ScreenSetting.cs`
   there): at start-up the game goes full screen (borderless full-screen window)
   at the display's own resolution (`Display.main.systemWidth/Height`), which
   wins over the size Unity remembers from earlier runs. The saved choice is in
   its own file in the save folder. Test runs never apply or save it (their
   `-screen-*` arguments stand, and they stay muted and unfocused per the
   testing rules).
2. **Settings > Display:** a Screen row (Full screen / Windowed) and a
   Resolution row (the display's own first), as in Chastain Rush.
3. **Check:** run the exe at a 1280 x 800 desktop-sized start (simulate the
   Deck: the screen-size code path with 1280 x 800) and show it fills the whole
   screen with nothing cut off and the HUD/menus inside it; plus a normal start
   on Dan's PC fills his screen. No visible or focused test window without
   asking Dan first.
4. **Publish as 0.2.1** (the launcher on the Deck picks it up on its next start).
5. **Update `TODO.md` / `CLAUDE.md`**, commit and push. Per-item status as usual.

**For Dan to check:** start the game on the Deck from the same Steam shortcut:
it updates to 0.2.1 and fills the screen. If it still doesn't: in Settings >
Display, what do Screen and Resolution show?

Then Round 3 (Aliens), which the planning chat writes next.

=== Round 2 specification (as the planning chat wrote it, moved from TODO.md at the end of Round 2) ===

---------------------------------------------------------------------------
Current Objective — Round 2: Campaign core (rescue loop, credits, ongoing career)
---------------------------------------------------------------------------

Round 1 reconciled by the planning chat (2026-11-02): all 12 items done as
reported. Dan played it on PC ("ran well"). Still for Dan to check: HOTAS 4,
Steam Deck. Placeholder sounds are made in code (ElevenLabs comes in Round 6).

**Goal:** Dan can start a Campaign career, fly today's planet, find downed
pilots on the radar, land like a helicopter, pick them up, climb to the
mothership and dock (live, no fade), get paid in credits, pay for fuel and
repairs, die, get regenerated and carry on. Everything saves. All pilots are
real this round (aliens are Round 3), no enemies (Round 4).

0. **Commit first.** Commit the repo as it is (it holds the planning chat's
   doc updates) before changing anything.
1. **Crash = explosion, wreck stays (all modes, Free Flight too).** A real
   explosion (fireball, debris, smoke, sound) where the ship hits; the burning
   wreck stays in the scene and the camera stays on it live. The Ship Lost /
   death menu appears over the live scene. Remove Round 1's fade to black.
2. **Save file (career).** One career save: credits, the ship (hull, energy,
   wrecked or not, upgrades list — empty for now), the home system, every
   planet visited and its state, career stats. Autosave on dock, death and quit.
   Keep it where the launcher's save backup covers it (check `LauncherBridge` /
   Chastain Rush's save location), so updates never touch it. Campaign menu:
   **Continue** / **New career** (New asks to confirm before replacing a save).
3. **Home system + planets with generated names.** A new career generates its
   home star system and its name once. Today's planet comes from the seed of
   the day and gets a name generated from that seed (e.g. "Vessra-IV"); the
   same day gives the same planet and name. A planet keeps its state between
   visits: pilots rescued or lost stay that way. A planet is "cleared" when no
   pilots are left to rescue. (Star map and older planets: Round 5.)
4. **Downed pilots.** Seeded: each pilot sits by their crashed ship, which
   sends a beacon, with landable ground within reach. The first planet has 3
   pilots; later planets have more and spread them wider (tier rises with
   planets cleared in the career). Numbers in the tuning file (item 11).
5. **Radar.** Bearing and distance to each beacon, more detail as you close in
   (rough direction far out, exact spot near). On a cockpit display and on the
   flat-screen HUD (VR rule 2). Rescued and lost pilots drop off it.
6. **Helicopter landing.** A landing mode (one button on HOTAS / gamepad /
   keyboard): the ship can slow to a hover, turn on the spot, go straight down
   and up, gear down, and settle on fairly flat ground. Landing too hard or on
   a slope does damage by the crash level. Take off again by climbing out of
   the hover and going back to flight mode. Round 1's minimum speed applies
   only outside landing mode.
7. **The pickup.** Landed + engines off: the pilot leaves their wreck, runs to
   the ship and knocks the three-knock code on the hull. Airlock button opens
   the airlock and the pilot boards (seen and heard from the cockpit; the cabin
   count goes up). Engines started while a pilot is within the exhaust range
   kills them: lost for good on that planet, credit penalty. Cabin holds 6.
8. **Energy.** Drains with flight (more at high throttle and in hover). Gauge
   in the cockpit and HUD, warning at 15 %. At 0 the ship can only come down;
   once landed, call a **mothership tow** for a big fee (the lift plays live).
9. **Mothership + docking, live.** A mothership high above the planet (Blender
   placeholder model). With pilots aboard, climbing above the docking height
   starts the approach: the ship flies itself into the bay, live and
   continuous, and the player can look around the whole time (CLAUDE.md VR
   rule 5: no fade, no cut). Docked = the hangar (item 12). Launching again
   plays the drop out of the bay live.
10. **Credits (no score).** Paid per pilot delivered, plus a bonus when a
    planet is cleared. At docking: pilots paid, fuel used is charged, hull
    damage repair is charged (repair can be skipped and paid later). If the
    player can't afford fuel, the corps gives a free minimum fill, so a career
    can never get stuck.
11. **Tuning file.** All numbers (pilot pay, clear bonus, starting credits,
    fuel price, repair price, tow fee, recovery fee, pilots per planet tier,
    exhaust kill range) in one JSON file Code names and lists in `CLAUDE.md`.
    Starting values are Code's call; report them.
12. **Hangar screen (between flights).** Inside the mothership, built to the
    world-space panel rule: credits, ship hull and energy, today's planet name
    and pilots left (rescued / lost / waiting), career stats (pilots rescued,
    pilots lost, credits earned, deaths, planets cleared). Buttons: Launch,
    Repair, Refuel, Back to title.
13. **Death and regeneration.** On death (crash at any crash level that ends a
    flight): the explosion (item 1), then pilots aboard are lost (no pay, lost
    for good on that planet). You wake in the mothership's regeneration bay and
    keep credits, upgrades and stats. Your ship is a wreck on the planet; choose:
    - **Pay recovery:** the ship is hauled back and repaired, upgrades kept;
      fee grows with the ship's value.
    - **Take the loaner:** a free basic ship with no upgrades; your own ship
      stays wrecked until you pay its recovery later from the hangar.
    (Recovering the wreck yourself: Round 5.)
14. **Campaign crash level.** A Campaign setting using the Free Flight scale;
    default **Lasting damage**.
15. **Publish the round.** After the round's checks pass, publish it as a
    release to `danemoll-jpg/Hostile-Horizon-releases` with the launcher tools
    (version 0.2.0). This is the first real release, so also publish the
    starter package (launcher + game, `Package-LauncherStarter.py`, with the
    rewritten `README-LAUNCHER.md`) as a downloadable asset Dan can install on
    the Steam Deck. Check: a launcher installed from that starter package finds
    and installs the release from GitHub (not a local test catalog). Give Dan
    the starter download link in the status.
16. **End of round.** Update `TODO.md` (per-item status: done / not done /
    deferred and why, never skip one silently; "For Dan to check") and
    `CLAUDE.md` (Commands, tuning file, save location, Where things stand).
    Clean up the disk, commit and push.

**For Dan to check (after Round 2):** install on the Steam Deck from the
starter package (steps in its README) and play through the launcher; landing feels like a helicopter and is
fair in tight canyons; finding pilots with the radar; the pickup (knock,
airlock); docking live (does it feel real?); credits feel right for fuel and
repairs; the crash explosion and the death / recovery / loaner choice; the
planet name; Continue after quitting keeps everything.

=== Fix 2.3 specification (as the planning chat wrote it, moved from TODO.md at the end of Fix 2.3) ===

---------------------------------------------------------------------------
Current Objective — Fix 2.3: getting home (warnings, rescue in the air, recall, pilot fuel)
---------------------------------------------------------------------------

Fixes 2.1 / 2.2 (full screen) are done and published (status under "Previous
deliveries"). Round 3 (Aliens) comes after this fix.

Dan (2026-11-02, playing 0.2.x): he had one pilot aboard and was running out of
energy. He didn't understand how to get home; he flew toward the mothership but
ran dry before docking started. "There should be warnings." Root cause: docking
only starts above 1000 m, the climb costs energy, nothing says how far up that
is or whether the energy left is enough, and at 0 energy in the air the only way
out is sink, land, pay the tow.

0. **Commit first** (the repo as it is, with this text).
1. **A clear way home on the displays.** On the cockpit display and the HUD,
   always while flying in the Campaign: the mothership's direction and a
   "DOCK ↑ <metres>" readout (height still to climb to the docking height),
   and the Advice line says plainly "Climb to dock" when docking is allowed.
   A sky marker on the mothership so it can be seen and flown toward.
2. **Energy-to-get-home warnings.** Work out the energy needed to climb from
   here to the docking height (plus a small margin). When the energy left gets
   near it: a "RETURN NOW" warning (display, HUD, a distinct warning sound,
   not the 15 % beep). When it's below it: "CAN'T REACH THE MOTHERSHIP: recall
   or land" (see 3 and 4). The 15 % warning stays.
3. **Rescue in the air.** At 0 energy while flying (not landed) in the
   Campaign: the mothership comes to you and its tow beam catches the ship in
   mid-air, live (no fade, VR rule 5), then the normal docking ending. Pilots
   aboard are delivered and paid. Same tow fee as now (tuning file).
4. **Recall button.** Call the mothership to you any time in the Campaign, in
   the air or landed, for a recall fee (tuning file; less than the tow fee,
   starting value Code's call). It comes over and lifts you up on the beam,
   live, then the normal docking ending (pilots delivered and paid). Bound on
   HOTAS / gamepad / keyboard; listed on the Advice line when energy is low.
   Not while a pilot is running to the ship or at the airlock.
5. **Pilots bring fuel (Dan's idea).** Each downed pilot's wreck still has a
   little fuel: when they board, they bring a random amount of energy, rolled
   fresh each time a pilot boards — NOT from the planet's seed (Dan: "random
   amounts so you never know how much you get"; this is the one deliberate
   exception to "the seed decides everything"). Starting range 3-15 % (tuning
   file). Energy goes up with a short message: "Pilot 2 aboard (1 / 6),
   brought 8 % energy". Energy never goes above 100 %.
6. **Check (muted, no focus, as the testing rules say):** (a) climb with one
   pilot: the DOCK readout counts down and docking starts at the height;
   (b) energy set low over the canyon floor: RETURN NOW, then CAN'T REACH
   appear at the right points; (c) run dry in the air: caught by the beam, docks,
   pilot paid, tow fee charged; (d) recall from the air and from landed: fee
   charged, docks; (e) a pilot boards: energy goes up by their amount, and the
   amount differs between two runs of the same planet. Pictures of (a)-(c) from the
   cockpit.
7. **Publish as 0.2.3**, update `TODO.md` (per-item status) and `CLAUDE.md`
   (new tuning values, the recall control), commit and push.

**For Dan to check:** with a pilot aboard and low energy, is it clear how to get
home, and do the warnings come early enough? Run dry in the air: does the beam
catch you? Try the recall. Does pilot fuel help enough?

=== Round 1 delivery (moved from TODO.md "Previous deliveries" at the end of Fix 2.3) ===

**Round 1 — project setup + Free Flight (2026-11-02). Local build only, not
published.** Exe `Builds/HostileHorizon/HostileHorizon.exe` (built 2026-11-02
03:34, see its `BUILD_INFO.txt`). Checked in the built exe from the title screen
through the game's own input actions (a virtual gamepad added to the Input
System), 1920x1080 on Dan's PC (GTX 1660 Ti): `Data/screenshots/round1/`
(`exe_check.txt` and pictures). HOTAS: not checked (code can't hold it).

0. **Commit first — done.** "Round 1 planning docs" committed and pushed before
   any change.
1. **Unity project — done.** Unity 6000.6.1f1, URP 17.6.0, Input System 1.20.0
   only (ProjectSettings, URP assets and package list copied from Chastain Rush;
   `com.unity.modules.screencapture` added for screenshots). Folders
   `Assets/Scripts/<System>/`, `Assets/Editor/`, `Tools/`, `Docs/`, `SourceArt/`;
   `.gitignore` from Chastain Rush minus its map lines. **Hostile Horizon > Build
   Executable** + batch command; `Play Hostile Horizon.bat`. Check: batch build
   succeeds (0 errors, 98 MB, about 35 s once imported).
2. **Seeded canyon terrain — done.** `Terrain/CanyonField.cs` (two warped noise
   fields give winding, forking canyons; wider basins; terraced walls 150-350 m;
   ridges), 256 m chunks streamed around the camera at 4/8/16/32 m detail with
   skirts (`TerrainStreamer.cs`). Look: own triplanar rock shader with sediment
   layers on the walls, dust on flats, darker canyon floors, sun shadows, height
   haze; teal-to-amber alien sky with sun and a banded gas giant. Textures are
   generated by the game (no third-party art). Checks: same seed twice gives
   identical heights and start, the next seed differs; restart starts at the same
   place; height map `00_heightmap.jpg`; **415-424 fps average, 1 % low about
   183 fps** flying low at 1080p High (target 60).
3. **Ship + flight model — done.** `Tools/Blender/ship.py` (Chastain Rush's kit)
   makes `Assets/Resources/Models/Ship.fbx`, 4.1k triangles, 1:1 cockpit (seat,
   consoles, dash, canopy). Arcade model: pitch / roll / yaw / throttle, 108-450
   km/h, no hovering (landing is Round 2), bank turns, wings level slowly when the
   stick is centred. Speed, altitude, height over ground, heading, throttle, hull,
   seed, crash level and view on three cockpit displays and on a screen HUD.
   Check: `03_cockpit.jpg`; RT held 2 s: throttle 30 to 100 %, 369 km/h.
4. **Cameras — done.** Cockpit (default), Chase, Close chase, Canyon (low,
   behind and below); one button cycles (Y / C / HOTAS button 3); look on hat /
   right stick / arrows / right mouse. Camera rig per the VR rule (code moves the
   rig root only). Check: four presses go round to Cockpit; pictures `04`-`07`.
5. **Input — done; HOTAS for Dan to check.** One asset
   (`Assets/Resources/Input/Controls.inputactions`) with HOTAS / Gamepad /
   Keyboard+mouse binding sets; deadzone (8 %), invert pitch, HOTAS throttle axis
   picker (Auto = slider if present, else z) and invert lever (on); Settings >
   Axis display shows every joystick axis, held buttons and what the ship reads.
   Check: menus, flight, view, look, throttle and pause all driven through the
   actions by a virtual gamepad; `10_axis_display.jpg`. HOTAS button numbers are
   guesses (see "For Dan to check").
6. **Free Flight + crash levels — done.** Checks (nose pushed into the ground):
   Ghost: through the rock (62 m under the surface), no hits; Bounce: 3 hits, no
   damage; Regenerating: one hit -32 %, healed to -18 % 4 s later; Lasting: -32 %
   stays -32 %; Instant: first hit ends it, SHIP LOST (fade, then Restart same
   seed / Return to title). Pictures `11_*`, `12_ship_lost_4.jpg`.
7. **Title menu + settings — done.** Title (Free Flight, Campaign "coming
   soon", Settings, Credits, Quit); Free Flight page (seed of the day / random /
   typed in, crash level, Start); Settings (invert pitch, deadzone, HOTAS throttle
   axis + invert, axis display, default view, graphics quality, Steam Deck,
   volume); pause menu. The same panel shown as a world-space board in
   `13_menu_world_space.jpg` (VR rule 3). Typed seed checked: D-pad digits plus a
   typed "5", B deletes it, result 1987. Credits page added (CLAUDE.md licensing
   rule).
8. **Steam Deck setting — done.** Chastain Rush's `SteamDeck.cs` ported (Auto /
   On / Off; Auto = the Deck's screen or GPU): view and shadow distance 60 %, no
   MSAA, one shadow cascade. Check `-steamDeck 1` at 1280x800: view 1920 m,
   `14_steam_deck_cockpit_1280x800.jpg` (frame rate measured on the 1660 Ti, not
   on a Deck).
9. **Launcher — done, not published.** Ported and renamed
   (`HostileHorizonLauncher.exe`, catalog `danemoll-jpg/Hostile-Horizon-releases`),
   new signing key made once (key ID `0c9c29ff...`, private key in
   `Builds/PublisherPrivate/`), built with VS 18. Self-check against a local test
   catalog (`Tools/launcher_selfcheck.py`): found build 2, downloaded, verified,
   installed, started the game, got its ready signal, both exited cleanly
   (`launcher_self_check.txt`, `15_launcher_self_check.jpg`). Added: in the
   self-check's hidden batch run the game closes itself after the ready signal.
   `Play Hostile Horizon (launcher).bat` starts a local launcher install of the
   newest build (`Tools/make_local_launcher.py`); the real catalog has no release
   yet (404), so the launcher says it couldn't check and starts the game.
10. **Placeholder audio — done (made in code, not ElevenLabs).** Engine hum
    (pitch with throttle), wind (with speed) and a thud on hits, synthesized at
    run time; no sound files this round. Check: output level 0.07-0.09 RMS while
    flying (silent = 0). Nobody has listened to it yet.
11. **End of round — done.** TODO.md / CLAUDE.md updated, disk cleaned,
    committed and pushed.

Decisions made: the scene is one GameShell that builds everything in code (no
hand-edited scenes or prefabs); ground contact is tested against the drawn
terrain (no physics engine); Blender models are turned 180 degrees on load (as
Chastain Rush does); flight assist = bank turns + slow wing levelling; two start
files (the exe, and through the launcher). Ported from Chastain Rush: project
settings, URP assets, package list, `.gitignore`, PlayerBuild, the Play bat,
SteamDeck.cs, LauncherBridge.cs, `Launcher/` with its vendor licences, the six
launcher tools, `cleanup_round.py`, README-LAUNCHER (rewritten), Blender `kit.py`.
Known: the full exe check ran on the build before last; the last build only
shortened one cockpit display's text and was checked with the short run
(`exe_check_final_build.txt`). The wavy strata "drips" and the plain ship are
placeholder art (Round 6).

**For Dan to check (Round 1):**
- HOTAS 4: in Settings > Axis display, do stick, twist, throttle lever and hat
  move the bars? Which button numbers show for the buttons you want for View,
  Menu, Select (trigger) and Back? If the lever works backwards, toggle "Invert
  throttle lever"; if it doesn't move the bar, try another "HOTAS throttle axis".
- Fly with a gamepad (LB / RB yaw, RT / LT throttle, Y view, Start menu).
- Does the canyon look "a little more realistic"? Is flying low through it fun?
- The five crash levels.
- Sound: engine hum and wind (placeholders): too loud or annoying?
- The launcher: `Play Hostile Horizon (launcher).bat` should show the launcher,
  say it couldn't check for updates, then start the game.

=== Round 2 delivery (moved from TODO.md "Previous deliveries" at the end of Fix 2.4) ===

**Round 2 — Campaign core (2026-11-02). Published: 0.2.0 (`build-200`, Latest)
and the starter package (`starter-200`).** Exe `Builds/HostileHorizon/HostileHorizon.exe`
(built 2026-11-02 07:33, `BUILD_INFO.txt`). Checked in the built exe from the
title through the game's own input actions (virtual gamepad), 1920x1080, GTX
1660 Ti: `Data/screenshots/round2/` (`exe_check_campaign.txt`,
`exe_check_free_flight.txt`, pictures). HOTAS: not checked (code can't hold it).
**Test rules (Dan, mid-round, now in CLAUDE.md Process):** the first checks were
audible and windowed; one window took focus (the first mute test, about 15 s).
Since then every check runs with `Tools/run_check.py`: `-batchmode` (no window,
the view drawn off-screen for screenshots) and `-mute` (master volume 0 before
the first scene, the listener's final mix zeroed): measured output 0.000 RMS in
every run (the game itself produced about 0.08 RMS). No step played sound.

0. **Commit first — done.** "Round 2 planning docs" committed and pushed first.
1. **Crash = explosion, wreck stays — done.** `Effects/Explosion.cs`: fireball,
   sparks, a light flash in the world (no screen strobe), 18 debris pieces that
   bounce and rest, black smoke, then flames, a smoke column, firelight and
   crackle on the charred wreck; boom sound. The view eases out of the cockpit and
   circles the wreck live; the menu comes up over it. Round 1's fade is removed.
   Check: Free Flight Instant crash and Campaign crash (396 km/h), both "page over
   the live scene, camera continuous" (`35`-`38`, `12_ship_lost_4`).
2. **Save file — done.** `Core/Career.cs`, `career.json` beside `settings.json`
   (LocalLow, inside the launcher's save backup). Saved on dock, death, quit to
   title, closing the game. Campaign menu: Continue / New career (confirm page if a
   career exists) / Crash level. Check: quit to title + Continue gave the same
   credits, stats, planet and ship.
3. **Home system + planets — done.** `Core/Names.cs`: a new career names its home
   system (e.g. "Vera"); today's planet is named from the seed of the day:
   20261102 = **Bryshra-IV** (twice the same), 20261103 = Zanir-IV. Planets keep
   rescued / lost pilots; cleared when none wait (bonus paid once).
4. **Downed pilots — done.** `Campaign/PlanetSites.cs`, `DownedPilot.cs`: seeded
   flat landing patches on canyon floors (bumps under 2 m over 32 m), pilot 30 m
   off, their wreck + blinking beacon beyond; 3 on the first planet, +1 per
   planet cleared, wider spread per tier. Check: same spots twice; today 2.2-2.9 km out.
5. **Radar — done.** `UI/Radar.cs`: far (>3 km) rim blip to 30°, mid to 10° /
   0.5 km, within 1 km the exact spot; on the cockpit's right display and the HUD,
   with a list; the mothership shows when you can dock. Check: "PILOT 3 020° 2.0
   km" -> "029° 460 m" -> "004° 50 m".
6. **Helicopter landing — done.** Land button (X / G / HOTAS 4): brake to a hover,
   stick moves and turns on the spot, throttle climbs / sinks, gear legs come down,
   settles if slow and flat; too fast / too steep / gear up = a hit at the crash
   level. Check: 3 landings at 2.4-2.5 m/s, 4 m from the spot, no hits. Fixed on
   the way: turning in hover shrank with frame rate.
7. **The pickup — done.** Landed + engines off (A / K / HOTAS 5): the pilot runs,
   knocks three times (sound at the hull) every 7 s; airlock (B / O / HOTAS 6)
   opens the door and they board. Engines started with a pilot within 25 m kill
   them. Check: knock 4 s after engines off, boarded, cabin 1; engines on at 21 m:
   pilot dead, lost on the planet, -400 cr.
8. **Energy + tow — done.** Drain in flight (more with throttle) and hover, none
   with engines off; warning + beep under 15 %; at 0 a slow powerless descent that
   eases to a safe touchdown; landed with none: the tow page; the mothership comes
   over, a beam lifts the ship into the bay, live. Check (energy set to 0.5 % over
   a landing spot, logged as a check shortcut): landed 2.5 m/s, tow live (top 45
   m/s), -1500 cr fee.
9. **Mothership + live docking — done.** `Tools/Blender/mothership.py` (2.7k
   triangles, open-stern bay, floor doors, cradle, regeneration pod),
   `Campaign/Mothership.cs` (keeps station over you at 1500 m). Launch: doors open,
   the cradle lets go, the ship drops and its engines catch it. Docking (above 1000
   m with pilots aboard, or none left, or under 25 % energy): it flies itself round,
   under the bay and up into the cradle; you can look round (118° checked). Check:
   launch top 73 m/s, docking top 125 m/s, no frame jump.
10. **Credits — done.** Pilot pay, clear bonus, fuel charged at the dock (free fill
    to 35 % if you can't pay), repair in the hangar, tow fee; credits never go
    below 0. Check: 3000 -> 3322 (+500 pilot, -178 fuel for 30 %).
11. **Tuning file — done.** `Assets/Resources/Tuning/CampaignTuning.json` (see
    "Tuning (starting values)" below).
12. **Hangar — done.** A menu page over the live bay, seen from the cockpit
    (world-space-ready panel): credits, ship, hull, energy, today's planet and its
    pilots, career stats, last flight; Launch / Repair / Refuel / Recover your own
    ship (when flying the loaner) / Back to title.
13. **Death + regeneration — done.** Pilots aboard lost, death counted, ship
    wrecked; "Wake up": the view rises from the wreck and flies in through the open
    stern to the pod (live; top speed 221 m/s, no frame jump, moving with the
    carrier); Pay recovery (3000 cr) or Take the loaner
    (yellow, free): the ship rises in on the tow beam while you watch, then you
    step into the cockpit. Check: 1 pilot lost with the ship; loaner; recovery from
    the hangar (credits topped up by the check to afford it, logged): 3500 -> 500.
14. **Campaign crash level — done.** Campaign menu option, default Lasting damage
    (`settings.json` `campaignCrashLevel`).
15. **Publish — done.** 0.2.0 = build 200, Latest:
    https://github.com/danemoll-jpg/Hostile-Horizon-releases/releases/tag/build-200 .
    **Starter (download this):**
    https://github.com/danemoll-jpg/Hostile-Horizon-releases/releases/download/starter-200/HostileHorizon-Starter.zip
    (42 MB; launcher + the Round 1 game 0.1.0, so its first start must update).
    Check: that ZIP downloaded from GitHub into an empty folder, its launcher run
    against the real catalog: "Updating to game 0.2.0", downloaded 38 MiB,
    "Game activated 0.2.0", started `versions/200`, ready handshake, exit 0
    (`50_fresh_install_updating_from_github.jpg`, `fresh_install_*`). The releases
    repo was empty, so GitHub refused to tag: Code added a README as its first commit.
16. **End of round — done.** TODO / CLAUDE updated, disk cleaned (repo folder 1.92 GB -> 1.42 GB: check output removed after copying the evidence),
    committed and pushed.

Frame rate (1080p High, 1660 Ti, measured while the PC was otherwise idle,
windowed run 06:12): Free Flight low 439 fps (1 % low 214); Campaign with beacons +
mothership 373 fps (1 % low 157); hangar 435 fps. Final headless runs (07:35-07:50,
while another session's Chastain Rush check ran): Free Flight 416 fps (1 % low 57),
Campaign 159 fps with hitches up to 0.8 s, hangar 531 fps; Steam Deck setting at
1280x800: 1113 fps (1 % low 314). The typed-seed step can't type in a headless run
(no keyboard without a window); it passed windowed (1987).

**Tuning (starting values, Code's call):** starting credits 3000; pilot pay 500;
clear bonus 1000; fuel 6 cr per %; free fill to 35 %; repair 12 cr per %; tow
1500; recovery 1500 + 25 % of ship value (6000) = 3000; exhaust kill 25 m,
penalty 400; cabin 6; pilots 3 + 1 per tier (max 10), 900-3000 m out (+500 m per
tier), 700 m apart; pickup range 160 m; run 5.5 m/s; energy drain flight 0.10 +
0.22 x throttle %/s, hover 0.35 %/s (about 6 min of flight); warning 15 %; hover
18 m/s, climb / sink 14 m/s, turn 50°/s, brake 12 m/s²; safe touchdown 3.5 m/s,
slope 14°; mothership 1500 m, docking above 1000 m.

Decisions made: docking also starts with no pilots aboard if none are left or
energy is under 25 % (else you could be stuck up there); the regeneration pod is in
the bay, so every move to it is a real camera flight; quitting mid-flight puts the
ship back in the hangar with its energy and hull, and pilots in the cabin go back
to wait on the planet; the out-of-energy page has only "Call the tow" (no free way
out); the starter ZIP carries the Round 1 game (0.1.0) so its first start must
update from GitHub; landing mode also works in Free Flight. New: Steam poster
(`make_cover.py`). Known: one native Unity crash (access violation in the engine,
no game code on the stack, D3D12) while a check started a new career; 9 other
runs passed that point; not reproduced. Placeholder art and code-made sounds.

**For Dan to check (Round 2):**
- Steam Deck: install from the starter ZIP (steps in its README); the first start
  should update to 0.2.0 by itself.
- Landing like a helicopter: is it fair in tight canyons? Hover sink 14 m/s: too slow from 1500 m?
- Finding pilots with the radar; the pickup (knock, airlock — the pilot at the
  door is hidden by the hull from the cockpit; the knock is the cue).
- Docking and launch live: do they feel real? The trip to the regeneration pod.
- Credits for fuel, repairs, tow, recovery; the crash explosion; the planet name.
- HOTAS: buttons 4 / 5 / 6 are guesses for landing mode / engines / airlock.
- Continue after quitting keeps everything.

=== Fix 2.4 specification (as the planning chat wrote it, moved from TODO.md at the end of Fix 2.4) ===

---------------------------------------------------------------------------
Current Objective — Fix 2.4: flying feel, landing, pause / reset, deeper canyons
---------------------------------------------------------------------------

Fix 2.3 (getting home) is done and published as 0.2.3 (status under "Previous
deliveries"). Round 3 (Aliens) comes after this fix.

Dan (2026-11-02, playing 0.2.3, gamepad / Steam Deck; HOTAS still untried):
- "I just feel it is too clunky to fly right now."
- "I hit X and I don't understand why it isn't landing but then I go flying by
  the pilot I am trying to reach." (Cause, as the planning chat reads the code:
  X starts a brake at `hoverBrake` 12 m/s², so from 300+ km/h it takes about
  10 s and most of a kilometre to stop, with nothing showing that it's braking
  or where it will stop.)
- "There has to be some sort of menu so I can pause and quit." (A pause menu
  exists on Start / Esc, but Dan didn't find it, and it can't open during the
  automatic parts: docking, tow, recall, mid-air rescue.)
- "There needs to be a reset button in case you get up too far in the air or
  something. This happened to me when I had to do work and couldn't pause."
- "Valleys should be deep and more compact, not lots of open space."

0. **Commit first** (the repo as it is, with this text).
1. **Pause anywhere, and easy to find.**
   - Start / Esc pauses in every state of play, including docking, tow, recall,
     the mid-air rescue and launch (time stops; resume continues them).
   - **View / Back (the Deck's ⧉ button) also opens the pause menu**, and on the
     HOTAS one button Dan picks (default: a base button; see the axis display).
   - **Auto-pause when the game loses focus** (alt-tab, the Steam overlay, the
     Deck's Steam / ... buttons) in players' builds. Test runs keep their
     run-in-background behaviour.
   - Show "☰ / Esc: Pause" small in a corner of the HUD for the first minute of
     each flight.
   - Pause menu (Campaign and Free Flight): Resume, **Reset position** (item 2),
     Recall (Campaign), Settings, Quit to title (Campaign: saves), Quit game.
2. **Reset position.** From the pause menu: puts the ship back in a steady
   hover about 60 m above open, landable ground near where it was (Free Flight:
   or back at the start), engines on, same cabin, same energy, no charge. For
   "got too high / lost / stuck". Not available while a pilot is running to the
   ship or at the airlock.
3. **Landing that does what you expect.**
   - Much harder braking when X is pressed: from top speed to a hover in about
     3 s (air brakes; tuning file), with a brake sound and a "BRAKING" /
     "HOVER" state on the displays.
   - A **stop-point marker**: while flying near a pilot, show on the ground
     where you'd come to a hover if you pressed X now, so you can time it.
   - Radar cue when a pilot is close: "Slow down — press X" at the distance
     where the brake would stop you near them.
   - In hover: descend faster high up (e.g. 30 m/s above 150 m over the
     ground), slowing down near the ground, so setting down from altitude
     doesn't take ages. A height-over-ground readout in big type in hover.
   - Make the hover → settled → landed steps obvious on the displays (gear
     down, "TOUCHDOWN", "LANDED — engines off (A) so the pilot comes").
4. **Less clunky flying (Dan's main complaint; first pass).** The ship should
   feel quick and direct on a gamepad and the Deck:
   - faster, more direct pitch and roll response (less lag and smoothing),
     yaw that actually helps in tight canyons;
   - the wing-levelling and bank-turn assists made lighter, with a
     **Flight assist** setting: Full / Light (new default) / Off;
   - **Stick sensitivity** setting (and a response curve so small moves stay
     fine).
   Code reports the before / after numbers (response times, turn rates).
   This will need Dan's play to tune: keep values in the tuning file.
5. **Deeper, more compact canyons.** Narrower canyons with taller, steeper
   walls (about twice as deep as now, roughly 300-700 m), packed closer
   together with more branching, and much less open basin and flat plain. Keep
   small flat landing spots at every pilot's site (and a few others), since you
   now land in narrow places. Same seed still = same world (the worlds change
   once with this fix; existing saved planets regenerate, which is fine at this
   stage). Keep 60+ fps on Dan's PC and the Deck setting's frame rate.
6. **Check (muted, no focus, as the testing rules say):** pause works in
   flight and during docking / tow / recall; View/Back opens it; focus loss
   pauses a players' build (simulate); Reset position from high up and from a
   canyon; braking from top speed stops within the target and the stop-point
   marker matches where it stops (within 50 m); hover descent from 1000 m time;
   flight response numbers before / after; pictures from the cockpit flying a
   new canyon low, and a height map of the new terrain next to the old one.
7. **Publish as 0.2.4**, update `TODO.md` (per-item status) and `CLAUDE.md`
   (controls, new settings, tuning), commit and push.

**For Dan to check:** does flying still feel clunky (and if so, what exactly:
too slow to turn, too twitchy, drifts, levels itself)? Try Flight assist Light
and Off. Can you land next to a pilot first try using the stop-point marker?
Pause with ☰ and ⧉; Reset position. Do the canyons feel deep and tight enough?

---------------------------------------------------------------------------
Current Objective — Fix 2.5: tight control, seeing the ground, the pickup, launch warning, faster lift
---------------------------------------------------------------------------

Fix 2.4 is done and published as 0.2.4 (status under "Previous deliveries").
Round 3 (Aliens) comes after this fix.

Dan (2026-11-02, playing 0.2.4, gamepad / Steam Deck; HOTAS still untried):
- "It is too loose, I feel I don't really have control over it. It easily gets
  turned in directions I don't want."
- "When landing for a pilot, it is hard to get a good angle, there should be
  either a monitor or some way I can see what's going on (glass floorboards)."
- "Landed, the pilot came towards me, he didn't even knock and I didn't see a
  button for airlock."
- "The game should warn you if you are leaving the mothership and have not a
  lot of fuel."
- "When the mothership lifts you it should be much quicker."

0. **Commit first** (the repo as it is, with this text).
1. **Tight, predictable control (Dan's main complaint).** Fix 2.4 made the ship
   faster and the assists lighter; Dan now finds it loose and it turns where he
   doesn't want. Goal: the ship goes exactly where the stick points and nowhere
   else.
   a. **Find every source of rotation or drift the player didn't ask for, first,
      with data:** log, per frame, the stick input and each contribution to the
      ship's rotation and velocity (stick, assist bank-turn, assist levelling,
      collisions / wall bounces, ground effect, anything else). Fly the check's
      route with the stick centred and with simple inputs, and list what moved
      the ship without input and by how much. Report it.
   b. **Remove or tame those sources:** stick centred = the ship holds its
      attitude (no slow turning from the bank-turn assist on Light, no
      roll-over); the flight path follows the nose closely (little or no side
      slide / momentum carrying it the old way); rotation stops when the stick
      is released (no overshoot or wobble); wall bounces push the ship away
      from the wall, never spin it.
   c. **Calmer default rates and finer centre:** bring the default pitch / roll
      rates back down (about midway between 0.2.3 and 0.2.4), a larger centre
      deadzone on gamepad / Deck sticks (Deck sticks drift), and a stronger
      stick curve so small corrections are small. Keep Stick sensitivity and
      Flight assist settings (Light default).
   d. Report before / after numbers, including the "uncommanded movement" list
      from (a).
2. **See the ground when landing.**
   - A **belly camera** feed on a cockpit display that switches on by itself in
     hover (and can be called up any time): looking straight down with the
     stop-point / landing ring, the pad, the pilot and a height readout. Also a
     picture-in-picture on the flat-screen HUD while hovering.
   - **Glass floor panels** in the cockpit at the pilot's feet (and the lower
     canopy) so you can look down and see the ground, in flat-screen cockpit view
     (look down with the look control) and later in VR.
   - A **landing view** added to the camera cycle: high over the ship looking
     down at it, for flat-screen players.
3. **The pickup must be obvious and must work.**
   - **Find out why the pilot didn't knock** in Dan's run: reproduce on the new
     0.2.4 terrain (narrow pads, pilot path from the wreck to the door, the
     ship's landed position and heading relative to the pad). Fix the cause
     and report what it was. A pilot who can't reach the door must say so
     (Advice line) instead of silently stopping.
   - On-screen and in the cockpit, always: **"PILOT AT AIRLOCK — press B to
     open"** (the real binding for the device in use: gamepad / Deck, keyboard,
     HOTAS) the moment the pilot is at the door, plus a lit AIRLOCK button on the
     cockpit panel. While they run: "PILOT COMING — keep engines off".
   - The knock: a visible "KNOCK KNOCK KNOCK" on the displays as well as the
     sound; and the pilot visible on the belly camera / an airlock camera on the
     display when at the door (he's hidden by the hull from the cockpit).
   - Make sure players' builds are NOT muted (the test `-mute` flag only applies
     to test runs): confirm the knock sound plays in a players' build by logging
     its AudioSource playing (no unmuted run needed); if a real sound check is
     needed, follow the testing rule (brief, report it).
4. **Low-fuel warning when launching.** In the hangar, if energy is low (below
   a tuning value, starting 50 %), Launch asks first: "Energy 22 %. Refuel
   first? (cost N credits)" with **Refuel and launch** (default) / Launch
   anyway. Also a "Refuel" reminder on the hangar screen whenever energy is
   under 100 %.
5. **Much quicker mothership lift.** The tow, the recall lift and the mid-air
   beam rescue: about 3x faster than now, with the lift itself taking at most
   about 10 s from the ground into the bay. Still live and continuous (VR rule
   5), no jump. Shorten the docking approach the same way if it takes more
   than about 15 s. Tuning values in the file.
6. **Check (muted, no focus, as the testing rules say):** the uncommanded-
   movement list before / after; stick-centred hold test; a full pickup on
   at least three different pads of the new terrain (pilot knocks, prompt
   shows, airlock, boards), with cockpit pictures of the prompt and the belly
   camera; the launch warning; lift and docking times before / after.
7. **Publish as 0.2.5**, update `TODO.md` (per-item status) and `CLAUDE.md`,
   commit and push.

**For Dan to check:** does the ship go where you point it now? Land with the
belly camera / glass floor; does the pilot knock and is the airlock prompt
clear? Launch with low fuel; the lift speed. HOTAS when you get a chance.

**Fix 2.1 — full screen on the Steam Deck (2026-11-02). Published: 0.2.1
(`game-201`), then 0.2.2 (`game-202`, Latest) with Dan's 1080 default.** Exe built 2026-11-02 13:08 (`BUILD_INFO.txt`). Evidence
`Data/screenshots/fix2.1/`. Every check headless (`-batchmode`, no window) and
muted: measured output 0.000 RMS; no step played sound; no window opened.

0. **Commit first — done.** "Fix 2.1 planning" (d4bfb66).
1. **ScreenSetting ported — done.** `Core/ScreenSetting.cs` (Chastain Rush's):
   before the first scene the game asks for a borderless full-screen window at
   `Display.main.systemWidth/Height`, overriding the size Unity remembers; choice
   saved in `display.json` beside `settings.json`. Test runs (`-mute`,
   `-exeCheck`, `-racerTestSave`) never apply or save it. Check: "check run
   applied nothing: Screen 1280 x 800 Windowed" (its `-screen-*` size stood).
2. **Settings > Display — done.** New "Display" row under Axis display: Screen
   (Full screen / Windowed) and Resolution (the display's own first, then its
   sizes from 1280 wide, largest first). Check: `s3_display.jpg`.
3. **Check — done (Deck simulated headless; PC in a real window, Dan's OK).** New `-exeCheckOnly screen`
   (`python Tools/run_check.py <dir> --only screen --size 1280x800 --extra
   "-steamDeck 1"`): the size code path for a 1280 x 800 desktop asks for 1280 x
   800 FullScreenWindow (fills the screen); title, settings, display, cockpit,
   chase, pause played at 1280 x 800: every image and text glyph measured inside
   the view, "nothing cut off" on all six (`s1`-`s6`, `exe_check_screen.txt`).
   Dan's PC: the same path for his display (3840 x 2160) asks for 3840 x 2160
   FullScreenWindow. **Real start on Dan's PC (visible, focused window, run once
   with his OK, about 30 s, muted: output 0.000 RMS):** new `-exeCheckOnly
   fullscreen` (applies the default setting, reads / saves no `display.json`):
   "window 3840 x 2160 FullScreenWindow; display 3840 x 2160 -> fills the
   screen", title and cockpit HUD nothing cut off (`f1_title.jpg`,
   `f2_cockpit.jpg`, `exe_check_pc_fullscreen.txt`). **But only 13.6 fps (1 %
   low 10.4) at 3840 x 2160** in the cockpit, at Below Normal priority; the
   performance target is 60 fps at 1080p. **Dan decided: "Default to 1920 x
   1080" -> 0.2.2:** the default full-screen size is the display's own, at most
   1080 tall, same shape (`ScreenSetting.DefaultSize`): 4K -> 1920 x 1080
   stretched to fill, Deck 1280 x 800 unchanged; Resolution offers "Default
   (1920 x 1080)" first and the display's own 3840 x 2160 among the rest.
   Check (headless, muted, exe built 13:42): Deck 1280 x 800 -> 1280 x 800,
   simulated 4K and this PC -> 1920 x 1080 FullScreenWindow; six screens at 1280
   x 800 nothing cut off (`exe_check_screen_v022.txt`, `v022_s3_display.jpg`).
   Not re-run in a visible window (one OK, used once).
4. **Publish — done.** 0.2.1 = build 201, tag `game-201`, Latest:
   https://github.com/danemoll-jpg/Hostile-Horizon-releases/releases/tag/game-201 .
   Check: the `starter-200` ZIP extracted to an empty folder, its launcher run
   with `--auto-check` against the real GitHub catalog (hidden, game in batch
   mode): "Updating to game 0.2.1", "Game activated 0.2.1", started
   `versions/201`, ready handshake, exit 0 (`launcher/`). Found on the way: the
   first try, from a 144-character folder, failed with "Cannot create extracted
   file" (Windows' 260-character path limit inside Unity's data folders) and
   started the old version; from a short folder it worked. Not changed (launcher
   code, not this fix); keep installs in short paths. 0.2.2 = build 202, tag
   `game-202`, Latest; the same starter check updated to 0.2.2 and started
   `versions/202` (`launcher-202/`).
5. **TODO.md / CLAUDE.md — done.** Committed and pushed. Clean-up: disk use 1.7 GB -> 1.5 GB.

**For Dan to check:** start the game on the Deck from the same Steam shortcut:
it updates to 0.2.1 and fills the screen. If not: what do Settings > Display
Screen and Resolution show? On the PC (0.2.2): full screen at 1920 x 1080
stretched over the 4K screen; is the frame rate back to normal, and is it sharp
enough?

**Fix 2.3 — getting home (2026-11-02). Published: 0.2.3 (`game-203`, Latest).**
Exe built 2026-11-02 14:27 (`BUILD_INFO.txt`). Evidence `Data/screenshots/fix2.3/`
(`run1`-`run3`: `exe_check.txt` + pictures; `launcher/`). Every check headless
(`-batchmode`, no window) and muted: measured output 0.000 RMS at start and end;
no step played sound; no window opened. New check: `python Tools/run_check.py <dir> --only home`.

0. **Commit first — done.** "Fix 2.3 planning" (d36c2d7).
1. **A clear way home — done.** Cockpit left display (top line: the lower lines sit below the
   flat view) and HUD: `HOME <bearing>° <km> · DOCK ↑ <m>`; the mothership is always on the
   radar; Advice says "Climb to dock (↑ 450 m)" whenever docking is allowed; a blinking blue
   sky marker under the mothership, same size on screen at any distance. Check: the readout
   counted 953 -> 850 -> 700 -> ... -> 100 -> 0 m, docking started at 1001 m (`h02`, `h03b`).
2. **Energy-to-get-home warnings — done.** Needed = height to climb / 30 m/s x full-throttle
   drain + 3 %. RETURN NOW within 8 % of it, CAN'T REACH below it (display, HUD, Advice with
   the recall keys, a new falling two-tone warning sound, repeated every 10 s / 6 s); 1 % of
   hysteresis; the 15 % beep stays. Check over the canyon floor (44 m): need 13.2 %, RETURN NOW
   at 21.2 %, CAN'T REACH at 13.2 % (`h03_return_now`, `h04_cant_reach`). Low energy also
   lets you dock with no pilots aboard (as below 25 %).
3. **Rescue in the air — done.** At 0 energy in the air the mothership comes (faster than
   station keeping) and its beam catches the ship within 600 m level distance, live, then the
   normal docking. Check: caught at 36-38 m above ground (not landed), docked, pilot paid
   +500, tow -1500 cr; camera continuous (run 1: fastest 73 m/s; runs 2-3 show a "JUMP" only
   because the check presses the camera button for the chase picture `h05b`).
4. **Recall — done.** Hold 1 s: gamepad View, keyboard H, HOTAS button 7 (`Recall` action),
   fee 900 cr at the dock (tow 1500). A short press only shows "Keep holding…". Check: 0.4 s
   did nothing; from the air (801 m up) and from the ground both docked, -900 cr each, camera
   continuous; refused while a pilot runs to the ship ("Not now: a pilot is coming").
5. **Pilots bring fuel — done.** 3-15 % per boarding, `System.Random` (not the seed), never
   above 100 %, message "Pilot 3 aboard (1 / 6), brought 9 % energy". Check, same planet and
   pilots in three runs: pilot 3 9 / 5 / 5 %, pilot 2 5 / 13 / 7 %, pilot 1 13 / 11 / 11 %.
6. **Checks — done** (above; `-exeCheckOnly home`, `Checks/ExeCheck.Home.cs`). Fixed on the
   way: the HUD flashed "OUT OF ENERGY" / "ENERGY LOW" during the beam lift (now the lift's
   own prompt); `run_check.py` crashed printing "↑" to the console (now UTF-8).
7. **Publish — done.** 0.2.3 = build 203, tag `game-203`, Latest. Check: the `starter-200`
   ZIP extracted to `Builds/StarterCheck`, `--auto-check` against GitHub: "Updating to game
   0.2.3", installed `versions/203`, started, exit 0 (`launcher/`). TODO / CLAUDE updated, pushed. Clean-up:
   disk use 1.9 GB -> 1.6 GB (incl. the 0.2 GB starter test install).

**New tuning (`CampaignTuning.json`):** recallFee 900, recallHoldSeconds 1, homeClimbRate 30,
homeMargin 3, returnNowWarning 8, beamReach 600, pilotFuelMin 3, pilotFuelMax 15.

**For Dan to check:** with a pilot aboard and low energy, is it clear how to get home, and do
the warnings come early enough (30 m/s is a cautious climb)? Run dry in the air: does the
beam catch you? Try the recall (hold View on the Deck / H / HOTAS 7: is button 7 a good
one on your HOTAS?). Is the sky marker big enough? Does pilot fuel help enough?

---------------------------------------------------------------------------
Current Objective — Fix 2.6 + Round 3 (one round, Dan 2026-11-03: "let's fold in round 3")
---------------------------------------------------------------------------

Fix 2.5 is done and published as 0.2.5 (status under "Previous deliveries").
This round does Part A (Fix 2.6: radar distances + route trail) and then
Part B (Round 3: Aliens), and publishes both together as **0.3.0**. Part A's
items keep their numbers; Part B's are B1-B12.

**Part A — Fix 2.6: pilot distances on the radar + a route trail**

Dan (2026-11-03): "I want to put distance markers for the pilots on the minimap
so it is more obvious who is closer and by how much. I would also love a trail
marker to show you the route."

0. **Commit first** (the repo as it is, with this text).
1. **Distances on the radar / minimap.** Every pilot's blip carries its
   distance as a label right next to it (e.g. "1.2 km", "460 m"), on the
   cockpit display and the HUD. The **nearest** pilot's blip is highlighted
   (brighter / larger, a "NEAREST" tag), so who is closer and by how much is
   obvious at a glance. Range rings with distance labels on the radar (e.g.
   1 / 2 / 3 km, scaling with the radar's zoom). The mothership keeps its blip
   and gets a distance too.
2. **Route trail.** A trail showing the way to the current target: by default
   the nearest waiting pilot; once the cabin is full or no pilots are left, the
   mothership (climb point). Optionally the player can cycle the target
   (a button: next pilot / mothership).
   - The route follows the canyons, not a straight line through walls:
     path-find over the terrain (e.g. along canyon floors / below the rims at a
     flyable height, on a coarse grid) from the ship to the target's landing
     pad, re-planned every few seconds or when the ship leaves the route.
   - Shown in the world as a line of glowing markers (floating gates / chevrons
     every ~100 m, fading out a few hundred metres ahead) so it can be followed
     at speed, visible in the cockpit view (VR rule 4: in the 3D world), plus the
     route drawn on the radar.
   - It ends at the pilot's pad with the landing ring (Fix 2.4).
   - Settings: Route trail On (default) / Off.
3. **Check (muted, no focus, per the testing rules):** pictures of the radar
   with labels and the nearest highlight; the trail from the start to the
   nearest pilot through at least one canyon bend (cockpit pictures at three
   points) and that following it reaches the pad without hitting a wall (fly it
   with the check's pilot); re-plan after leaving the route; target cycling;
   frame rate with the trail on (PC and Deck setting).
4. (Publishing happens once, at the end of Part B.) Commit Part A on its own
   before starting Part B.

**Part B — Round 3: Aliens** (design: "Aliens" and the Sleeper at the top of
this file; read them first). Goal: some "pilots" are aliens; each type has its
own tell that the game never explains; sometimes they fool you; letting one in
is terrifying and kills you (death, regeneration, recovery as in Round 2).
Make it scary as hell, with no strobe flashes.

B1. **Which pilots are aliens.** Decided by the planet's seed (same planet =
    same aliens; pilot fuel stays the one random exception). The first planet
    of a new career has none, so the player learns the rescue first. After
    that, a share of the pilots are aliens (starting 20 %, rising with the
    planet tier to about 35 %; tuning file), and every type can appear.
    Difficulty presets come in Round 4: use "Normal" values now, all in the
    tuning file.
B2. **Disguise and reveal models.** An alien looks like a pilot (same model and
    suit) until it reveals itself. A Blender-scripted creature model (thin,
    wrong-jointed, wet, about 2.2 m tall; original design, nothing from the
    1984 game) for the reveal, the canopy and the cabin. Simple animation is
    enough this round (Round 6 is the art pass), but it must read as horrible
    up close in the cockpit view.
B3. **The four outside types and their tells** (each tell is in the 3D world,
    VR rule 4):
    1. **Mimic:** its shadow is wrong (lags its body, or the wrong shape).
    2. **Hollow:** its visor reflects nothing (no landing lights, no sky).
    3. **Knocker:** knocks wrong (four knocks, the wrong rhythm, or from the
       wrong place on the hull) instead of the pilots' three-knock code.
    4. **Stalker:** doesn't come straight to the airlock: circles the ship,
       drops off the radar for a moment, or comes from behind.
    **Tell reliability:** a type shows its tell only some of the time (Normal
    75 %, decided per alien by the seed). **False tells** (a real pilot with a
    tell-like quirk): off on Normal (0 %), the setting exists for Round 4.
    The belly / airlock camera (Fix 2.5) must show enough of the figure at the
    door to judge tells 1, 2 and 4; tell 3 is heard.
B4. **What the player can do with something at the door.** Don't open the
    airlock; **start the engines** (the exhaust kills anything within range,
    as now; an alien shrieks and reveals as it dies, so the player learns what
    it was); or take off. Killing a real pilot keeps Round 2's rule (lost,
    credit penalty). Killing an alien pays a small bounty (tuning file,
    starting 200 credits) so catching tells is rewarded.
B5. **Ignored alien breaks in.** An alien left at the door: after a while
    (tuning, starting 25 s) it climbs onto the canopy, where you see it pressed
    against the glass (the big scare beat; scraping, breathing, knocking on the
    glass). If the player still does nothing (starting 10 s more), it breaks in
    (B7). Starting the engines or taking off while it's on the canopy throws it
    off and kills it.
B6. **Real pilots never do this:** a real pilot left waiting keeps knocking
    and the Advice line keeps saying so. They never climb the canopy.
B7. **Letting one in: the horror sequence, then death.** Airlock opened to an
    alien (or a break-in): the airlock closes behind it, the cabin lights die,
    alarms, sounds moving in the cabin behind the cockpit, the cockpit door is
    hit and gives way, and the creature comes at the player in the cockpit.
    Built live in the cockpit with the player still able to look around (VR
    rule 5: no camera takeover, no fades; darkness, sound and timing do the
    work; no full-screen strobe). Then the normal death: explosion is not
    needed here — the ship is lost on the planet with the creature in it,
    pilots aboard are lost, regeneration bay, recovery fee or loaner. The death
    menu says what got you ("Hollow").
B8. **The Sleeper.** Boards like a real pilot and passes every check at the
    door. After a random time in flight (starting 40-120 s, seeded), it turns:
    - with other pilots aboard, it kills one (heard, and glimpsed on the cabin
      camera, item B9, not shown in full). If at least 2 pilots are left
      (tuning), they kill it: the ship survives, that pilot is lost, a message
      tells you what happened. If fewer are left, it breaks into the cockpit:
      the horror sequence (B7) and death;
    - with no other pilots aboard, it goes straight for the cockpit (B7).
    **Its tell** (after boarding, only some of the time like the others):
    cabin sounds that are off (a wet clicking, a pilot's voice that loops) and
    a wrong-looking passenger on the cabin camera. **The answer:** dock before
    it turns: the mothership's security kills it in the bay (a short live
    moment from the cockpit: shots and a shriek in the cabin), the other pilots
    are delivered and paid, and the Field Journal records it.
B9. **Cabin camera.** A cockpit display (and HUD picture-in-picture on
    request) showing the cabin and the pilots aboard, so the Sleeper's tell can
    be seen. Part of the cockpit, like the belly camera.
B10. **Field Journal.** Unlocks a type's entry the first time the player meets
    it in a way that reveals it (it killed you, you killed it, the pilots
    killed it, security killed it). First entry: its name and a vague hint
    ("Something about the way the light falls on it..."); after a second
    encounter: a clearer hint (never a full explanation). In the hangar and the
    pause menu. Saved with the career.
B11. **Scare sound.** The sounds this round needs: alien knocks (wrong
    rhythms), shriek / death, canopy scraping and breathing, glass cracking and
    breaking, cabin movement, the door being hit and giving way, alarms, the
    Sleeper's cabin sounds, security in the bay. Use ElevenLabs if Dan's API key
    is available to Code (ask Dan once for it if not set); otherwise make code
    placeholders and write `Docs/SOUNDS-NEEDED.md` with one ready-to-paste
    ElevenLabs prompt and target file name per sound, so Dan can generate them
    and drop them in. Sound checks follow the testing rule (muted; confirm by
    logging the AudioSource events; any audible check brief and reported).
B12. **Check (muted, no focus, per the testing rules):** the same seed gives the
    same aliens; one run per type with its tell shown (cockpit / belly camera
    pictures that Code looks at and describes: can a player see it?) and with
    it hidden; engines kill an alien (reveal, bounty, journal entry) and a real
    pilot (penalty); canopy climb and break-in timing; the horror sequence
    (cockpit pictures at its beats, no fade, the player can look round, ends in
    the regeneration bay); Sleeper: pilots win with 3+ aboard, cockpit death with
    1, docked in time = security kill; Field Journal first and second entries;
    the first planet of a new career has no aliens; frame rate.

**End of the round:** publish **0.3.0** (Part A + Part B), update `TODO.md`
(per-item status for 1-3 and B1-B12: done / not done / deferred and why; the
new tuning values) and `CLAUDE.md` (Where things stand, controls, tuning),
clean up, commit and push.

**For Dan to check:** radar distances and the route trail; then the aliens:
can you learn the tells by playing, do they still fool you sometimes, is the
canopy and the horror sequence scary, does the Sleeper's cabin fight work?

=== Fix 2.4 delivery (moved from TODO.md "Previous deliveries" at the end of Fix 3.1) ===

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

=== Fix 3.1 specification (as the planning chat wrote it, moved from TODO.md at the end of Fix 3.1) ===

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

=== Fix 2.5 delivery (moved from TODO.md "Previous deliveries" at the end of Fix 3.2) ===

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

=== Fix 3.2 specification (as the planning chat wrote it, moved from TODO.md at the end of Fix 3.2) ===

---------------------------------------------------------------------------
Current Objective — Fix 3.2: always another planet to fly
---------------------------------------------------------------------------

Fix 3.1 (real character models) is delivered as 0.3.1 (status under "Previous deliveries"; its specification is in
`TODO-ARCHIVE.md`). **Dan's art pipeline (DECIDED, his standing way of working):** Code writes the image prompts -> Dan has the
concept images drawn -> Code makes the 3D models with the Meshy API (Dan's key) -> Dan rigs them in Mixamo and downloads the
animations Code lists -> Code imports and wires them up; in stages with a STOP for Dan at each. How-to: `Docs/MODELS-NEEDED.md`.
Still open for Dan from 0.3.0: the route corridor width / height, and a second sign for the Mimic's tell in shade.


Round 4 (Enemies + difficulty) comes after this fix.

Dan (2026-11-04): "There should be something to do once you clear all the
pilots from a planet. Shouldn't have to wait another day before there are any
more pilots."

**Decided (planning chat default, Dan to confirm by playing):** the home system
has as many planets as you want. The seed of the day stays the **featured
"planet of the day"**; clearing a planet unlocks a jump to a **new planet** at
once. A simple planet list now; the full star map stays in Round 5.

0. **Commit first** (the repo as it is, with this text).
1. **New planets on demand.** From the hangar: **Jump to a new planet**. It
   generates the next planet of the career's home system from the career's
   seed and a planet counter (so a career's planets are always the same
   ones, in order), with its own generated name. Tier (more pilots, wider
   spread, more aliens) rises with planets cleared, as now. Available as soon
   as the current planet is cleared; also available before that (leaving
   pilots behind for later). Free (no credits) for now; tuning value exists.
2. **The planet list (simple star map).** Hangar page **Planets**: today's
   planet (marked "Planet of the day"), every planet the career has visited
   with its name, biome placeholder ("canyons" for now), pilots rescued / lost
   / still waiting, cleared or not; and "New planet". Pick one and launch
   there. Saved with the career.
3. **Planet of the day bonus.** Pilots rescued on today's planet pay more
   (tuning, starting +50 %), shown in the hangar, so the daily planet stays
   special. A new day = a new planet of the day; the old one stays in the list.
4. **When a planet is cleared:** the docking ending / hangar says so plainly
   ("Vessra-IV cleared! +1000 credits") and offers Jump to a new planet as the
   default next action.
5. **Check (muted, no focus):** clear the first planet -> jump -> new planet
   with a different name and pilots; the same career gives the same second
   planet twice; leave a planet with pilots waiting, come back from the list,
   they are still there; the planet-of-the-day bonus is paid; saves survive a
   quit and Continue.
6. **Publish as 0.3.2**, update `TODO.md` (per-item status) and `CLAUDE.md`,
   commit and push.

**For Dan to check:** after clearing a planet, is jumping to a new one obvious
and quick? Is the planet list clear?

=== Round 4 specification (as the planning chat wrote it, moved from TODO.md at the end of Round 4) ===

---------------------------------------------------------------------------
Current Objective — Round 4: planet biomes, music and voices
---------------------------------------------------------------------------

Fix 3.2 (always another planet to fly) is delivered as 0.3.2 (status under "Previous deliveries"; its specification is in
`TODO-ARCHIVE.md`). **Dan's art pipeline (DECIDED, his standing way of working):** Code writes the image prompts -> Dan has the
concept images drawn -> Code makes the 3D models with the Meshy API (Dan's key) -> Dan rigs them in Mixamo and downloads the
animations Code lists -> Code imports and wires them up; in stages with a STOP for Dan at each. How-to: `Docs/MODELS-NEEDED.md`.
Still open for Dan from 0.3.0: the route corridor width / height, and a second sign for the Mimic's tell in shade.
**Planets (Fix 3.2, planning chat default, Dan to confirm by playing):** the home system has as many planets as you want; the seed
of the day stays the featured "planet of the day" (pilots +50 %); Jump to a new planet from the hangar (free), a Planets list.


Dan (2026-11-04): "Not sure if we put planet variety in yet... let's do this.
Also we need some music. ... Do we need any voices for anything? Like some
communications? I think this could be interesting." Enemies + difficulty move
to Round 5 (the later rounds shift by one). One publish at the end: **0.4.0**.
Commit after each part.

**Part A — Planet biomes**

A1. **Biome per planet.** Each planet is one biome, chosen by its seed (same
    planet = same biome); a new career's first planet is always Canyons. The
    Planets list, the hangar and the planet's name screen show the biome. This
    round builds four: **Canyons** (as now), **Giant Forest**, **Dead City**,
    **Ice**. Same rules in all: landing pads, pilots, aliens, radar, route
    trail, recall / tow, crash levels, Free Flight (Free Flight gets a biome
    picker).
A2. **Giant Forest.** Huge alien trees (trunks 5-15 m wide, 60-150 m tall)
    over rolling ground and valleys; you fly under, between and over the canopy.
    Clearings are the landing pads (pilots crashed through the canopy). Dim,
    green-gold light under the canopy, shafts of light, mist, drifting spores.
    Tree trunks are solid (crash levels apply); leaves / canopy can be flown
    through with a brush effect, not a crash.
A3. **Dead City.** A ruined alien city: streets and avenues are the canyons
    between towers 80-500 m tall; broken skyscrapers, sky bridges, collapsed
    blocks, plazas and **rooftops as landing pads** (some pilots are on roofs).
    Dark, smoky, flickering dead lights, rain or ash. Buildings are made in code
    / Blender scripts (hard surface is fine for scripts); varied but seeded.
A4. **Ice.** Glaciers, deep blue crevasses as the canyons, ice cliffs, snow
    plains between them, ice caves you can fly through; weather: drifting snow,
    occasional blizzard that cuts visibility (the radar and route trail matter
    more). Ice / snow materials that look cold and real.
A5. **Look and weather per biome:** its own sky, sun, fog / haze colour, light,
    particles and terrain materials; the pilot wrecks and pads fit the biome.
A6. **Performance plan:** GPU instancing, LODs and distance culling for trees
    and buildings; the Steam Deck setting thins them (fewer trees / simpler
    buildings / shorter view). Keep 60 fps on Dan's PC and the Deck setting's
    frame rate (report per biome); keep VR headroom in mind.
A7. **Art:** buildings, ice and rocks by script; trees by script if they look
    good, otherwise use Dan's art pipeline (Meshy; STOP for Dan only if
    needed). Any CC0 textures noted in the credits.
A8. Later biomes stay queued: volcanic, crystal / fungal, ocean islands.

**Part B — Music** (design and Suno prompts: `Docs/MUSIC.md`, written by the
planning chat; Dan makes the Suno tracks)

B1. **Adaptive music system:** plays the track for the game state (title,
    hangar, planet list, flight per biome, low-energy tension, regeneration)
    with slow crossfades; flight music fades out near a pilot and while landed
    waiting (silence + wind for the scares); ducks under voice messages; the
    horror sequence has no music (sound only). Any missing track is silent.
B2. **Where the files go:** `BundleMusic/` (git-ignored, as in Chastain Rush),
    copied into the build, shipped as the launcher's **soundtrack** component
    (as Chastain Rush does). Tell Dan exactly where to drop them.
B3. **ElevenLabs music and stingers:** make the two music loops
    (`tension_low_energy`, `regeneration`; moved from Suno, Dan 2026-11-04) with
    the ElevenLabs music API and the short cues, all listed in `Docs/MUSIC.md`
    (`ELEVENLABS_API_KEY`), wired to their moments. If the plan doesn't allow
    music through the API, tell Dan.
B4. **Settings:** Music / Effects / Voices volume sliders (Master stays).
B5. **Dan's Suno tracks are in** (2026-11-04, put there by the planning chat):
    `BundleMusic/title_theme.mp3` (2:25), `hangar.mp3` (3:19),
    `planet_select.mp3` (3:20), `flight_canyons.mp3` (2:43), `flight_forest.mp3`
    (3:43), `flight_city.mp3` (3:39), `flight_ice.mp3` (3:23). Loop them with a
    clean crossfade at the loop point. Add `BundleMusic/` to `.gitignore`
    (as in Chastain Rush; it isn't ignored yet): the music ships through the
    launcher's soundtrack component, not git. If any track is missing later, build and check the system with
    placeholders; Dan drops the tracks in later and a soundtrack update ships.

**Part C — Voices and communications** (ElevenLabs text-to-speech, made by
Code with the API; all lines written by Code, original, short, natural)

C1. **Voices:** (1) **Control** — the mothership's flight controller (one calm,
    dry, experienced voice): launch, planet-of-the-day callouts, pilots found,
    "climb to dock", docking clearance, tow / recall, planet cleared, a new
    planet briefing, regeneration ("Welcome back. Again."). (2) **The ship's
    computer** — short synthetic warnings: energy low, RETURN NOW, can't reach
    the mothership, terrain / pull up, hull damage, landing gear, airlock open,
    pilot aboard, cabin full, intruder alert. (3) **Downed pilots** — a pool of
    at least 8 different voices (men and women, different ages and accents):
    mayday calls on the radio when you come in range ("Mayday, mayday, this is
    Corps pilot <name>, I'm down near...", seeded name), "I see you! Coming to
    you" when you land, a voice through the hull at the door ("It's me, open
    up!"), thanks when aboard, nervous cabin chatter, screams for the Sleeper.
C2. **The aliens use the voices too.** An alien disguised as a pilot also
    radios and talks at the door, so a voice proves nothing. **Suggested
    second sign for the Mimic in shade (answers 0.3.0's open question):** the
    Mimic's voice is subtly wrong some of the time (a word repeats, a breath in
    the wrong place, the callsign of a pilot already lost). Same tell rules
    (only some of the time). Other types keep their tells; no type gets a
    voice tell that's always on.
C3. **Radio sound:** comms through a radio filter (band-limited, light
    crackle) with a click in / out; the computer is clean and close; the door
    voice is muffled through the hull. Lines don't overlap (a queue with
    priorities: warnings first).
C4. **Subtitles** (Settings: On by default) at the bottom of the HUD and on a
    cockpit display (VR rule 2).
C5. **Files:** `Docs/VOICES.md` lists every line, voice and file; a script
    makes them (`--only`, `--force` as `make_sounds.py`); the key is never
    printed or written.

**Check (muted, no focus, per the testing rules):** each biome: a cockpit
picture flying low, a landing at a pilot (forest clearing, a rooftop, an ice
pad), the route trail, frame rate on PC and the Deck setting; the same seed
gives the same biome; the music system switches by state (log the track and
volumes; muted run); stingers fire; voice lines play in order with
subtitles (logged AudioSource events, no audible run); an alien's radio call
and door voice; the Mimic's voice sign appears only some of the time.

**End of the round:** publish **0.4.0**, update `TODO.md` (per-item status for
A1-A8, B1-B5, C1-C5: done / not done / deferred and why) and `CLAUDE.md`, clean
up, commit and push. Tell Dan where to put the Suno files.

**For Dan to check:** do the forest, city and ice feel different and good to
fly? Rooftop and clearing landings. The voices: does Control sound right, do the
pilots sound different from each other, is the radio chatter fun or too much?
Drop in the Suno tracks: does the music fit each place?

=== Previous delivery moved from TODO.md at the end of Round 4 ===

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

=== Previous delivery moved from TODO.md at Fix 4.1 ===

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

=== Previous delivery moved from TODO.md at Round 5 ===

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

=== Round 5 specification moved from TODO.md at the end of Round 5 ===

Round 4 (planet biomes, music and voices) is delivered as 0.4.0 (status under "Previous deliveries"; its specification is in
`TODO-ARCHIVE.md`). **Next: Round 5 (Enemies + difficulty)**, to be written by the planning chat with Dan.
Open for Dan from Round 4: how the biomes fly and look (Fix 4.1 / 0.4.1: Dan's own tension and regeneration music are in),
the voices and how much chatter, the music and its loop points with his tracks, any hitch in the forest / city.
Dan's art pipeline (DECIDED, standing): Code writes image prompts -> Dan has the concepts drawn -> Meshy -> Mixamo -> Code wires them
(`Docs/MODELS-NEEDED.md`). Still open from 0.3.0: the route corridor width / height.


Round 4 reviewed by Dan (2026-11-04): "all fine" (biomes, voices, music);
HOTAS still untried. Dan (2026-11-04): fold Fix 4.2 into Round 5. Part A is
Fix 4.2 (items 0-5, commit when done); Part B is enemies + difficulty (B1-B12).
One publish at the end: **0.5.0**.

**Part A — the ship rights itself (was Fix 4.2)**

Dan (2026-11-04): "I want the vehicle to right itself. I get turned around and
upside down too often when flying."

Background: Fix 2.5 made Light assist do no levelling at all (stick centred
holds the attitude), to stop the ship turning on its own. That fixed "loose",
but now nothing ever brings the ship back upright. The answer is righting that
only happens when the player lets go, so it never fights the stick.

0. **Commit first** (the repo as it is, with this text).
1. **Self-righting when the stick is released** (Light and Full assist; Off
   keeps none): when pitch and roll input have been centred for a short moment
   (starting 0.25 s, tuning), the ship smoothly rolls wings-level and eases its
   nose toward the horizon (or a gentle climb when close to the ground), taking
   about 1-1.5 s from a bank, about 2 s from fully upside down (always rolling
   the short way round, never through a dive into the ground). It keeps flying
   in its current heading (no sudden turn). The moment the stick moves, righting
   stops and the player has full control (no blend fighting the stick). Full
   assist rights faster and also does the bank-turn; Light rights but adds no
   turning of its own.
2. **A "level out" button** for an instant recovery: one press rights the ship
   at a faster rate even while the stick is held (gamepad: left-stick click;
   keyboard: L; HOTAS: a button picked in Settings > Controls and HOTAS).
3. **Settings:** "Auto-level" On (default) / Off, and the righting speed
   (Slow / Normal / Fast), separate from Flight assist.
4. **Hover:** the ship already stays level in hover; make sure switching to
   landing mode from any attitude (even inverted) rights it first, smoothly.
5. **Check (muted, no focus):** from a 60° bank, a 120° bank, upside down and
   nose-down 45°: release the stick, measure time to level and height lost;
   prove no uncommanded movement while the stick is held (Fix 2.5's list stays
   clean); a low canyon run with releases between turns doesn't hit the
   walls. Cockpit pictures before / after a recovery.
6. Commit Part A on its own (publishing happens at the end of Part B).

**Part B — Enemies and difficulty**

Goal: the planet fights back, as in the original: gun turrets on high ground,
flying saucers, enemy fire you can dodge, a shield that costs energy, and a gun
of your own. Difficulty presets decide how hard all of it (and the aliens) is.
Campaign only (Free Flight stays peaceful).

B1. **Turrets.** Gun emplacements on ridges and rims (Canyons), on rooftops and
    tower ledges (Dead City), on ice cliffs (Ice), on giant branches / stumps
    above the canopy (Giant Forest). Placed by the planet's seed. They track
    the ship when it is in line of sight and in range, fire visible, dodgeable
    shots (bright bolts with a short lead), and can be destroyed. Flying low in
    the canyons keeps you out of their sight: that's the point.
B2. **Saucers.** Flying enemies that patrol and, once they spot you, chase and
    make firing passes. Fewer of them; dangerous in the open, harder for them
    in tight canyons (they avoid walls but are less agile than the ship).
    Can be destroyed.
B3. **Your gun.** A forward gun (trigger / fire button on all devices; HOTAS
    trigger, gamepad RB or A as fits the layout — Code proposes a layout that
    doesn't clash, Dan checks). Each shot costs a little energy. Destroying a
    turret or saucer pays a bounty (tuning). A simple aiming reticle in the
    cockpit and HUD (VR rule 2).
B4. **Shields.** A shield toggle (as in the original): blocks enemy shots while
    on, drains energy fast while on, shows on the cockpit and HUD. A shield
    turned on with something at the hull also kills it (the original's answer
    to the aliens, alongside starting the engines; same rules for real
    pilots: a pilot at the door dies too).
B5. **Damage.** Enemy hits damage the hull (repair at the hangar, as now); hull
    at 0 = death (explosion, regeneration, recovery / loaner, as now). Hits shake
    the ship and the cockpit (no screen flashes), with sparks and a damage
    readout.
B6. **Warnings.** Incoming-fire direction markers on the HUD and cockpit
    display; "LOCKED ON" / "SAUCER" calls from the ship's computer (new voice
    lines, ElevenLabs as in Round 4); turrets and saucers on the radar once
    spotted; Control warns on launch if the planet is "hot".
B7. **Landing pads under fire.** On Easy / Normal, pilots' pads are out of
    turret sight lines (a landed ship isn't a sitting duck); on Hard /
    Nightmare some are covered, so you clear the turret first or risk it.
    Saucers can find you anywhere (they make waiting for a pilot tense).
B8. **Difficulty presets** chosen when starting a career and changeable any time
    from the hangar: **Easy / Normal (default) / Hard / Nightmare / Custom**.
    Custom options: enemies (Off / Turrets / Turrets + Saucers), enemy
    aggression (density, accuracy, fire rate), alien share, how often alien
    tells show (Easy 95 %, Normal 75 %, Hard 60 %, Nightmare 50 %), false tells
    on real pilots (Easy 0, Normal 0, Hard 10 %, Nightmare 20 %), crash level,
    pay multiplier (harder pays more). The alien surprise stays on in every
    setting (decided earlier). All numbers in the tuning file.
B9. **Ease-in:** a new career's first planet has turrets only and few of them,
    on every setting except Custom with them off; saucers from the second
    planet on. Enemy numbers rise with the planet tier.
B10. **Models and sound.** Turrets, bolts and saucers: hard-surface, made by
    Blender script (fine for these; the art pipeline only if they look poor —
    STOP for Dan then). Original designs, nothing from the 1984 game. Weapon,
    shield, impact and explosion sounds with ElevenLabs.
B11. **Performance:** keep 60 fps on Dan's PC and the Deck setting's frame
    rate with enemies active in every biome (pooling for shots and effects).
B12. **Check (muted, no focus, per the testing rules):** per biome, a turret
    tracks and fires, the ship dodges / takes hits, the gun destroys it
    (bounty); a saucer chase and kill; shield blocks shots and drains energy;
    shield kills an alien at the door; hull to 0 = death flow; pads out of
    turret sight on Normal; each preset's numbers; the first planet's ease-in;
    frame rate. Cockpit pictures that Code looks at and describes (can a first-
    time player see the shots coming and where from?).

**End of the round:** publish **0.5.0**, update `TODO.md` (per-item status for
Part A 0-6 and B1-B12: done / not done / deferred and why; new tuning values
and controls) and `CLAUDE.md`, clean up, commit and push.

**For Dan to check:** let go of the stick when you're turned around or upside
down: does it come back upright quickly and smoothly without fighting you? The
level-out button. Then the enemies: can you see the shots coming, is flying low
to stay out of turret sight fun, are saucers too much, how do the presets feel?
The gun / shield controls on the gamepad, and the HOTAS when you get a chance.

---------------------------------------------------------------------------
Round 6 specification (moved from TODO.md at the end of Round 6, 2026-11-07)
---------------------------------------------------------------------------

Dan (2026-11-07) folded the jump-scare fix, easier-to-find enemies, the
upgrade shop, the full star map and a Field Journal viewer into one round:
**Round 6**, one publish at the end: **0.6.0**. Commit after each part.

**Part A — make the alien attack a real jump scare (was Fix 5.1)**

Dan (2026-11-07): "The problem I see with the attack: in the original, it was
more of a jump scare. In this one the ship shuts down and the music starts,
letting you know to expect something."

What gives it away now (from the code, `GameShell.Aliens.cs` `Horror`): the
instant the airlock opens to an alien, the game reacts differently from a real
pilot: input is taken away, the computer says "intruder", the displays go dark,
the airlock closes, the lights die, the alarm starts — a long, announced
build-up. The player knows the moment they press the button.

**The rule from now on: the player must not be able to tell from the game's
reaction that it was an alien until the instant it strikes.** Nothing changes
(sound, music, lights, displays, controls, messages) before the strike.

0. **Commit first** (the repo as it is, with this text).
1. **Boarding looks exactly like a real pilot boarding.** An alien let in walks
   in like a pilot: the same animation, the same "Pilot N aboard (2 / 6)" message
   and chime, the cabin count goes up, the cabin camera shows a pilot, the
   player keeps full control, the music carries on as a real pickup would. No
   intruder line, no blackout, no alarm, no input lock.
2. **Then it strikes, at an unpredictable moment, with no build-up.** Each
   alien let in rolls one of these (seeded per alien, weights in the tuning
   file):
   - **Instant (about 40 %):** 1-4 s after the airlock closes, while the player
     is still sitting there: a sudden slam on the cockpit hatch, it bursts
     through and lunges at the player's face, all in about one second.
   - **On take-off (about 35 %):** a few seconds after lift-off or at the first
     hard turn: the same sudden burst through the hatch.
   - **Slow burn (about 25 %):** the current long sequence (lights, cabin
     noises, the hatch hit three times), kept for variety but started at a
     random moment in flight, not at the airlock.
   The scare sound and the alien-reveal stinger land **on** the strike, never
   before it; the music cuts **at** the strike. The computer's "INTRUDER"
   call comes after the strike, not before.
3. **The canopy leap (the original's scare).** An alien left at the door
   sometimes doesn't climb slowly: with no warning it **leaps and slams onto
   the canopy** (a hard bang, glass shudder, the creature filling the view)
   while the player is waiting. Seeded per alien (about 50 % leap / 50 % the
   current slow climb), at a random time from 5 s after it reaches the door.
   Same answers as now (engines / shield / take off throw it off).
4. **Red herrings, so nothing is a sure sign:** harmless things that sometimes
   happen with real pilots too: a light flicker or power dip when the airlock
   cycles, a thump from the cabin as a pilot sits down, a crackle on the cabin
   camera. Rare (tuning), never the full lights-out.
5. **Keep it fair:** after the strike the outcome is as now (death,
   regeneration, recovery). The tells at the door (Round 3) and the voice sign
   (Round 4) are still how a careful player avoids it; this fix only removes
   the warning *after* the airlock is opened. The Sleeper is unchanged (it
   turns much later and fights the other pilots); regular aliens never wait
   that long.
6. **Still VR rule 5:** no camera takeover, no fade, no full-screen flash; the
   creature physically comes at the player in the cockpit; the player can look
   around.
7. **Check (muted, no focus):** an alien boards: log every sound / music /
   light / display / input / message event from the airlock opening to the
   strike and compare it with a real pilot boarding: identical until the
   strike. Each strike type once (timing logged); the canopy leap; red herrings
   with real pilots; cockpit pictures at the strike moment.
8. Commit Part A (publishing happens at the end of the round).

**For Dan to check (Part A):** let a few aliens in. Do you still see it coming?
Does the instant one make you jump? The canopy leap while you wait.

**Part B — enemies that are easier to find** (Dan, 2026-11-07: "I had a hard
time finding the enemies to even shoot at." He also asked to **hold off on
making them more of a threat** until he's used to them: DEFERRED, not decided
against. Don't change accuracy, fire rate or aggression this round.)

B1. **On the radar always:** turrets within radar range and saucers within
    their own (longer) range show on the radar without being spotted first,
    with their distance, like pilots.
B2. **Target brackets:** enemies in line of sight within about 2.5 km get a
    bracket in the world (cockpit view and HUD, VR rule 4) with distance; the
    nearest / the one locked on is highlighted.
B3. **Visible from afar:** a red blinking warning light on each turret, a clear
    muzzle flash when it fires, and glowing bolts with a short trail, readable at
    1-2 km against every biome's sky and ground.
B4. **Check:** pictures from the cockpit at 2 km and 1 km in each biome: can a
    first-time player see the turret and the saucer? Commit Part B.

**Part C — the upgrade shop** (credits, in the hangar)

C1. Upgrades in levels (starting 3 levels each; prices and effects in the tuning
    file), bought with credits, shown in the hangar with what each level does:
    **Engine** (top speed, acceleration, climb), **Hull** (max hull), **Shields**
    (less energy drain, more hits blocked), **Energy** (capacity, less drain in
    flight), **Radar** (range, earlier exact positions), **Cabin** (6 -> 8 -> 10
    seats), **Gun** (rate and damage).
C2. Upgrades raise the ship's value, so the recovery fee grows (as already
    decided); the free loaner has none. Saved with the career.
C3. A small visible change on the ship / cockpit per category where easy (e.g.
    extra cabin seats on the cabin camera, a bigger radar display ring).
C4. Balance: the first upgrade affordable after about two planets on Normal;
    report the numbers. **Check:** buy each, prove the effect in flight. Commit.

**Part D — the full star map** (replaces Fix 3.2's planet list)

D1. A star map of the career's home system in the hangar (world-space panel
    rule): the star, the planets visited and the next undiscovered one,
    each drawn in its biome's colours with its name, biome, status (planet of
    the day, cleared, pilots waiting, lost), and its danger (difficulty tier,
    turrets / saucers). Select one and **Jump** (the jump plays live, as now).
D2. **Coordinates:** type a seed to visit any planet (Fix 3.2's typed seed
    idea), added to the map once visited.
D3. **New distress calls on old planets** (decided default, tuning): a visited,
    cleared planet sometimes gets new downed pilots after real time passes
    (starting: a chance every few hours, 1-3 pilots), marked on the map and
    announced by Control at the next dock. Gives old planets a reason to visit.
D4. **Check:** map pictures; jump to a visited planet and to a typed seed; a
    distress call appears after a simulated time skip. Commit.

**Part E — the Field Journal viewer**

E1. Each met alien type opens its own page with a **large 3D view of its
    model**: rotate it (right stick / mouse drag / HOTAS hat), zoom in for
    detail, and play its animations (idle, walk, attack, shriek); good
    lighting so the model reads in detail. Unmet types show a dark silhouette
    and "Unknown".
E2. More information per type, unlocked by encounters: its name, a short piece
    of original lore written by Code (where it was first seen, what the corps
    knows), how it behaves, the tell hints (as now: vague first, clearer after
    the second meeting, never the full answer), and your record with it (met,
    killed, killed you, first seen on <planet>).
E3. Also reachable from the pause menu; works with gamepad, keyboard, HOTAS.
    **Check:** pictures of a page rotated and zoomed. Commit.

**End of the round:** publish **0.6.0** (Parts A-E), update `TODO.md`
(per-item status for every part: done / not done / deferred and why; tuning
values) and `CLAUDE.md`, clean up, commit and push.

**For Dan to check (Parts B-E):** can you find the enemies now? Upgrades: are
prices fair and do you feel them? The star map and new distress calls. The
Field Journal pages: rotate and zoom an alien.

---------------------------------------------------------------------------
Round 4 delivery (moved from TODO.md at the end of Round 6)
---------------------------------------------------------------------------

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
