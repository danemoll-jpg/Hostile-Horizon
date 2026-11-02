Hostile Horizon — TODO archive

Older rounds and finished round specifications, moved here word for word from `TODO.md`
(grep it, don't read it whole).

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

