# Hostile Horizon — music (Suno) and short cues (ElevenLabs)

Written by the planning chat (2026-11-04) for Round 4. **Dan makes the Suno
tracks** (Suno has no API; 7 tracks) and drops them in `BundleMusic/` with the file names
below (mp3 or wav). Code wires them in; any missing track is simply silent.
The music ships as the launcher's **soundtrack** component, so it updates
separately from the game.

All tracks: **Instrumental ON** (no vocals), original music, no artist names
in prompts. Make 2 versions of each in Suno and keep the better one. Length is
a guide: the game loops and crossfades them.

## Where the files go (Round 4, B2 / B5)

- **Drop the tracks in `BundleMusic/`** in the repo root (`C:\Users\danmo\Hostile-Horizon\BundleMusic\`), named as in the
  table below (`title_theme.mp3` ...; mp3, ogg or wav). The folder is git-ignored: the music ships through the launcher's
  **soundtrack** component, not git.
- Then run `python Tools/music_loops.py`: it finds each track's loop (tempo, the end of the intro, the start of the ending's
  fade, a jump a whole number of bars back where the music matches best) and writes `BundleMusic/music.json`. The game plays the
  intro once, then loops with a 4 s equal-power crossfade (1 s for the tension loop), so the fade-out at the end is never heard.
  A track missing from `music.json` still loops (the whole file, 4 s crossfade). A missing track is silence.
- The exe build copies `BundleMusic/` into `Builds/HostileHorizon/Music/`; a release ships the same folder as the soundtrack
  component (`Tools/Prepare-ComponentUpdate.py soundtrack --source BundleMusic ...`), and the launcher starts the game with it.
- Volumes: Settings > Sound (Master, Music, Effects, Voices, Subtitles).

**ElevenLabs music API (2026-11-04):** Dan's API key is missing the `music_generation` permission (HTTP 401 "missing the
permission music_generation"), so `tension_low_energy` and `regeneration` were made as stand-ins with the sound-effects model
(a 22 s loop and 10 s). For the real music: ElevenLabs > Developers > API keys > edit the key > allow Music, then
`python Tools/make_music.py --only tension_low_energy,regeneration --force` and `python Tools/music_loops.py` (or make them in
the ElevenLabs app with the prompts below and drop them in `BundleMusic/`). The stingers are made with the sound-effects API
into `Assets/Resources/Sounds/sting_*.mp3` (built into the game).

## How the game uses them (adaptive)
- The music follows the game state with slow crossfades (3-6 s).
- **Flight music drops away near a pilot and while landed waiting**: silence
  and wind make the pickup tense (the scares work better without music).
- Music ducks under voice messages.
- Each biome has its own flight track; tension layers sit on top.

## Suno tracks (long)

| # | File | Title | Length | Style prompt (paste into Suno "Style of Music") |
|---|---|---|---|---|
| 1 | `title_theme.mp3` | Hostile Horizon | 2-3 min | cinematic sci-fi main theme, lonely and heroic, slow build, warm analog synth pads, deep sub bass, distant choir pads, soft taiko hits building to a big wide brass-and-synth climax, then fading to a single synth line, 80 bpm, epic but melancholy, instrumental |
| 2 | `hangar.mp3` | The Bay | 3 min, loops | calm ambient sci-fi, inside a huge orbiting mothership, warm evolving synth pads, gentle low hum, soft plucked arpeggio, quiet and safe, slightly hopeful, slow, no drums, 60 bpm, seamless loop, instrumental |
| 3 | `flight_canyons.mp3` | Red Walls | 3-4 min, loops | tense driving sci-fi flight music, low flying through deep alien desert canyons, pulsing analog synth bass ostinato, tight electronic percussion, distant metallic hits, dusty wide reverb, sense of speed and danger, minor key, 110 bpm, cinematic, instrumental |
| 4 | `flight_forest.mp3` | Under the Canopy | 3-4 min, loops | mysterious atmospheric sci-fi, flying through a giant alien forest, organic textures, deep wooden percussion, breathy flutes processed with synth, slow pulsing bass, bioluminescent shimmer, eerie and beautiful, 95 bpm, cinematic, instrumental |
| 5 | `flight_city.mp3` | Dead City | 3-4 min, loops | dark cyber-noir sci-fi, flying between ruined alien skyscrapers, heavy distorted synth bass, cold metallic percussion, echoing industrial hits, lonely detuned lead melody, abandoned and huge, 105 bpm, cinematic, instrumental |
| 6 | `flight_ice.mp3` | White Silence | 3-4 min, loops | cold minimal sci-fi ambient, flying over frozen glaciers and crevasses, glassy high synth bells, icy granular textures, deep slow sub bass, sparse heartbeat kick, wind-like noise swells, vast and lonely, 90 bpm, cinematic, instrumental |
| 9 | `planet_select.mp3` | The Home System | 2-3 min, loops | wonder and scale, sci-fi star map music, slow shimmering arpeggios, warm wide pads, soft cinematic percussion, curious and adventurous, 85 bpm, instrumental |

Later biomes (when they are built) get their own flight track in the same
style: volcanic ("heavy, rumbling, ritual drums, hot distorted bass"), crystal /
fungal ("strange, glassy, microtonal, alien"), ocean ("wide, rolling, sea
swell, lonely horn").

## ElevenLabs music loops (Code makes these with the ElevenLabs music API)

Moved from Suno (Dan, 2026-11-04: his Suno song generations are limited; these
are short loops). Code generates them with `ELEVENLABS_API_KEY` (ElevenLabs
music, instrumental). Kept short (Dan, 2026-11-04): the tension loop is
20-30 s and loops cleanly (Code trims / crossfades the loop point); the
regeneration piece is a short one-shot cue, not a loop. If the account's plan doesn't allow music generation through
the API, Code tells Dan and he makes them in the ElevenLabs app.

| File | Length | Prompt |
|---|---|---|
| `tension_low_energy.mp3` | 20-30 s, loops | rising tension underscore, ticking clock pulse, low string ostinato with synth, uneasy and urgent, no melody, builds slowly, 120 bpm, cinematic, instrumental |
| `regeneration.mp3` | 8-10 s, one-shot | somber sci-fi ambient, waking up in a cold medical bay, slow reversed piano, soft glassy pads, heartbeat-like low pulse, sad but steady, 60 bpm, instrumental |

## ElevenLabs short cues (Code makes these with the API)

| File | Length | Prompt |
|---|---|---|
| `sting_pilot_aboard.mp3` | 2 s | short warm sci-fi success chime, two rising synth notes with soft shimmer, no drums |
| `sting_planet_cleared.mp3` | 5 s | short triumphant sci-fi fanfare, synth brass and wide pads, rising to a bright resolved chord |
| `sting_alien_reveal.mp3` | 3 s | violent horror stinger, dissonant orchestral hit with screeching strings and a deep boom |
| `sting_death.mp3` | 4 s | dark descending horror sting, low brass and sub boom fading into silence |
| `sting_launch.mp3` | 4 s | heroic short sci-fi launch cue, driving synth pulse rising with a whoosh |
| `sting_docked.mp3` | 3 s | calm relieved sci-fi resolution, soft synth chord swell settling down |
| `sting_jump.mp3` | 4 s | sci-fi hyperspace jump, rising tonal whoosh into a deep warp boom |
