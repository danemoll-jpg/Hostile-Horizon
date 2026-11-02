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
