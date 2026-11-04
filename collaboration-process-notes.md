# How this collaboration works — process notes

Not project-specific. These are the habits worth carrying into any new
project using this same Claude-plans / coding-agent-executes pattern.

## Standard toolchain (Dan, 2026-11-02) — assume this for every game project
- **Engine:** Unity (currently 6000.6.1f1, URP, Input System only — same as
  Woodstock Rush / Chastain Rush, so systems can be ported between games).
- **3D models:** Blender (scripted where possible, e.g. `Tools/Blender/*.py`).
- **Music:** Suno for long songs; ElevenLabs for short songs/stings.
- **Sound effects:** ElevenLabs.
- **3D generation:** Meshy (paid, with API key). **Rigging/animation:** Mixamo.
- All of the above are paid licenses, so generated assets can be used freely.
  Don't ask about the toolchain again; just use it.

## Character / creature art pipeline (Dan, 2026-11-04) — every project
Code-made (scripted Blender) models are fine for simple hard-surface things,
not for people or creatures. For those, Dan's way of working is:
1. **Code writes the image prompts** (in `Docs/MODELS-NEEDED.md`): full body,
   A-pose or T-pose, front view, plain light background, no cropping, no
   props; plus a design note, the file name to save as, and the animation
   list. Code **stops** and tells Dan.
2. **Dan has the concept images drawn** and saves them where the doc says.
3. **Code generates the 3D models with the Meshy API** (image-to-3D, Dan's key
   from the `MESHY_API_KEY` environment variable — never written to the repo,
   logs or chat), cleans them up in Blender (scale, origin, facing, LODs),
   renders previews, and **stops** to show Dan the renders and the FBX files.
4. **Dan uploads them to Mixamo**, auto-rigs them and downloads the specific
   animations Code listed (real Mixamo search names, what each is for).
5. **Code imports them** (Humanoid rig), wires up the animations, replaces the
   placeholders, checks them in game, and keeps the old model as a fallback.
A round that uses this pipeline runs in stages with these stops for Dan.

## Standard launcher / auto-update (Dan, 2026-11-02) — reuse in every game
Every game ships with the same launcher Woodstock Rush (Racer repo) and
Chastain Rush use. Reference implementation: `C:\Users\danmo\chastain_rush`
(read-only — copy from it, never edit it).
- **What it does:** each time it opens it checks for a newer version, downloads
  and installs it, then starts the game. No internet / check fails → the
  installed game starts. B/Esc skips the update; any other key opens a menu
  (Check for updates, Update game and play, Update soundtrack, Restore previous
  version). Keeps the previous version for rollback and backs up saves before
  each update. Works on Windows and Steam Deck (Proton Experimental,
  non-Steam game).
- **Pieces to copy:** `Launcher/` (C++: core.cpp/hpp, launcher.cpp, store.cpp,
  core_cli.cpp, vendor/ json + miniz, launcher.rc, icon), the game-side
  `LauncherBridge.cs`, and `Tools/`: `Build-Launcher.ps1` (VS 18; SDK read
  from Racer's `Builds/LauncherSDK`), `New-LauncherKey.py`,
  `Prepare-LauncherRelease.py`, `Prepare-ComponentUpdate.py`,
  `Package-LauncherStarter.py`, `Publish-LauncherRelease.py`, plus the starter
  README (`Docs/Starter/README-LAUNCHER.md`) rewritten for the new game.
- **Per game:** rename the launcher (`<Game>Launcher.exe`), make a new signing
  key once with `python Tools/New-LauncherKey.py` (kept in
  `Builds/PublisherPrivate/`, git-ignored — never delete, print or replace it),
  and use a separate GitHub releases repo `danemoll-jpg/<game>-releases` as
  the update catalog (the code repo stays separate).
- **Publishing rule (updated 2026-11-02, Hostile Horizon):** publish a release
  every round, so the launcher picks it up and Dan can test on his Steam Deck
  (handy while his PC is busy). Chastain Rush used "local build unless asked";
  for a new game, ask Dan which one he wants, defaulting to publish every round.

## Round handoff (Dan, 2026-11-02) — every project
- **The planning chat writes each round into the repo.** Once Dan and the chat
  agree on a round, the chat updates `TODO.md` and `CLAUDE.md` in the game's
  local repo directly (through the linked computer), every round.
- **The handoff is a ready-to-paste prompt for Code.** At the end of each
  planning session the chat gives Dan one prompt that tells Code to:
  1. commit the repo as it is (including the chat's doc updates) before
     changing anything — a safety checkpoint;
  2. read `CLAUDE.md` and `TODO.md` and build the current round, item by item;
  3. at the end of the round, update `TODO.md` (per-item status: done / not
     done / deferred and why, plus "For Dan to check") and `CLAUDE.md`
     (commands, standing facts, "Where things stand"), then commit and push.
- After Code reports back, the chat reconciles the docs against what shipped
  before planning the next round.

## Standard process rules (carried from Chastain Rush's CLAUDE.md)
- `TODO.md` stays small: standing rules, current round, queued rounds, last
  three deliveries, technical notes. Older rounds move word-for-word to
  `TODO-ARCHIVE.md` (grep it, never read it in full).
- Short results: ~20 lines per round, one line per item + its one check, and a
  "For Dan to check" list.
- Targeted testing: one check per fix, in the built exe, played the way Dan
  plays it (pictures from the player's view that Code actually looks at).
- One heavy job at a time (build, map build, exe check), at Below Normal
  priority, so Dan's PC stays usable.
- Clean up the disk at the end of every round.

## Testing on Dan's PC — STANDING RULES (Dan, 2026-11-02) — every project
Dan uses his PC while Code tests. Code's test runs must not be heard or get in
his way.
1. **Always muted.** Every test run (exe checks, editor play runs, launcher
   checks) starts muted and stays muted, unless that step is a sound check.
   - Mute it in the game itself, from the first frame, through a command-line
     flag (e.g. `-mute`) that sets the master volume / `AudioListener.volume` to
     0 before any sound starts. Don't rely on a saved setting or on muting
     after start-up.
   - **Prove it:** the check reports the measured audio output level, and it
     must be 0 for a muted run. "I muted it" without that number is not done.
     (Hostile Horizon, Round 2: Code said it muted and Dan could still hear it.)
   - **Sound checks:** unmute only for the sound check itself, as briefly as
     possible, then mute again. Say in the status which step played sound and
     for how long.
2. **Never steal focus.** Test windows must not pop up over Dan's work or take
   keyboard/mouse focus.
   - Prefer runs with no window at all (batch mode, rendering to off-screen
     targets for screenshots). Otherwise start the window minimized / without
     activating it, with "run in background" on so it keeps working unfocused.
   - **If a test truly needs a visible or focused window, stop and ask Dan
     first**, and wait for his OK. Never do it on your own, even once.

## The core loop
1. You bring an idea, bug report, or piece of feedback to Claude in plain
   language — however casual or half-formed.
2. Claude scopes it: asks the one clarifying question that actually matters
   (not more), flags real design tensions before they become bugs, and writes
   the result into `TODO.md`/`CLAUDE.md` precisely enough that a coding agent
   can act on it without guessing.
3. Claude writes the agreed round into the repo's `TODO.md`/`CLAUDE.md` and
   gives you the handoff prompt to paste into your coding agent (see "Round
   handoff" above).
4. Your coding agent commits, builds it and reports back (as updated
   `TODO.md`/`CLAUDE.md` files).
5. Claude reconciles: syncs the docs, and — this part matters — checks every
   previously-open item off individually against what actually shipped,
   rather than accepting whatever the report happens to lead with.

## Habits worth keeping
- **Multi-item batches get numbered, not run together in prose.** A coding
  agent (or a person) is far more likely to silently drop one item out of a
  paragraph than one item out of a numbered list.
- **Silent partial completion is the failure mode to watch for**, not
  misunderstanding. A coding agent can know about a requested item and still
  choose not to build it without saying so. Ask for explicit per-item status
  (done / not done / deferred, and why) rather than assuming a deploy means
  everything requested got done.
- **Resolve open questions, don't let them drift.** When a design decision is
  genuinely open, say so plainly, give a recommended default, and get an
  actual answer — then mark it resolved and move on. A question that sits
  "open" for several rounds is a sign it needs to just be asked directly.
- **Distinguish "deferred" from "decided against."** A backlog item kept
  alive indefinitely as "later, maybe" is different from one that's been
  honestly assessed and closed. Say which one it is.
- **Track verification status separately from "built."** "Preview-verified"
  and "confirmed on the real device/environment" are different claims — don't
  let the first stand in for the second, especially for anything environment-
  specific (mobile browsers, real hardware, real third-party services).
- **When something fails repeatedly, get real diagnostic data before trying
  again — don't just guess harder.** A bug that survives several plausible-
  looking fixes is a sign the diagnosis is wrong, not that the next fix needs
  to be cleverer. Add instrumentation, capture real before/after evidence,
  and let the data pick the next move.
- **Sometimes the fix is avoiding the trigger, not solving the root cause.**
  Especially for gnarly environment-specific bugs (this project's whole
  scroll-bug saga, eventually solved by restructuring the UI to never trigger
  it, not by fixing the underlying platform issue) — a workaround that
  actually holds is a legitimate resolution, not a consolation prize. Just be
  honest in the docs about what's actually fixed vs. routed around.
- **If deploys/pushes are a limited resource**, ask your coding agent to
  batch a full round's changes into one push rather than pushing
  incrementally per change — this is worth stating explicitly, since
  incremental pushes can quietly burn through a quota with no one round ever
  being "the" round that used it up.
- **Casual, offhand feedback is often the most useful kind.** Some of the
  best fixes in this project came from throwaway lines like "it isn't size,
  the buttons aren't lined up" — plain-language descriptions of what's wrong
  are worth taking literally before assuming something more complex is going
  on.
