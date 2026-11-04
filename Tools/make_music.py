"""Hostile Horizon (Round 4, item B3): the ElevenLabs music loops and the short music cues (stingers).

The key comes from ELEVENLABS_API_KEY (never printed or saved).
 - The two music pieces (tension_low_energy, regeneration) come from the ElevenLabs music API and go to BundleMusic/ (they ship
   with Dan's Suno tracks in the launcher's soundtrack component).
 - The stingers (2-5 s) come from the sound-effects API and go to Assets/Resources/Sounds/ (built into the game, played by
   Audio/Music.cs through Sfx.Clip).
Prompts: Docs/MUSIC.md (the same text is below). Nothing is played: making a sound never makes any noise on this PC.
Usage: python Tools/make_music.py [--only name,name] [--force] [--list]
"""
import argparse, json, os, pathlib, sys, time, urllib.request, urllib.error

ROOT = pathlib.Path(__file__).resolve().parents[1]
MUSIC_OUT = ROOT / 'BundleMusic'
CUE_OUT = ROOT / 'Assets/Resources/Sounds'

# name, seconds, kind ('music' = music API into BundleMusic, 'cue' = sound-effects API into Resources/Sounds), prompt
TRACKS = [
    ('tension_low_energy', 28, 'music', 'rising tension underscore, ticking clock pulse, low string ostinato with synth, uneasy and urgent, no melody, builds slowly, 120 bpm, cinematic, instrumental, seamless loop'),
    ('regeneration', 10, 'music', 'somber sci-fi ambient, waking up in a cold medical bay, slow reversed piano, soft glassy pads, heartbeat-like low pulse, sad but steady, 60 bpm, instrumental'),
    ('sting_pilot_aboard', 2, 'cue', 'short warm sci-fi success chime, two rising synth notes with soft shimmer, no drums'),
    ('sting_planet_cleared', 5, 'cue', 'short triumphant sci-fi fanfare, synth brass and wide pads, rising to a bright resolved chord'),
    ('sting_alien_reveal', 3, 'cue', 'violent horror stinger, dissonant orchestral hit with screeching strings and a deep boom'),
    ('sting_death', 4, 'cue', 'dark descending horror sting, low brass and sub boom fading into silence'),
    ('sting_launch', 4, 'cue', 'heroic short sci-fi launch cue, driving synth pulse rising with a whoosh'),
    ('sting_docked', 3, 'cue', 'calm relieved sci-fi resolution, soft synth chord swell settling down'),
    ('sting_jump', 4, 'cue', 'sci-fi hyperspace jump, rising tonal whoosh into a deep warp boom'),
]


def post(url, key, body, name):
    for attempt in range(3):
        req = urllib.request.Request(url, data=json.dumps(body).encode(), headers={'xi-api-key': key, 'Content-Type': 'application/json'}, method='POST')
        try:
            with urllib.request.urlopen(req, timeout=300) as r: return r.read(), None
        except urllib.error.HTTPError as e:
            msg = e.read().decode('utf-8', 'replace')[:400]
            if e.code in (401, 402, 403): return None, f'HTTP {e.code} {msg}'   # the plan / key: no point retrying
            print(f'  {name}: HTTP {e.code} {msg}'); time.sleep(4)
        except Exception as e:
            print(f'  {name}: {e}'); time.sleep(4)
    return None, 'failed after 3 tries'


def make(key, name, seconds, kind, prompt):
    if kind == 'music':
        body = {'prompt': prompt, 'music_length_ms': int(seconds * 1000), 'model_id': 'music_v1', 'force_instrumental': True}
        data, err = post('https://api.elevenlabs.io/v1/music?output_format=mp3_44100_128', key, body, name)
        if data or not err or 'music_generation' not in err: return data, err
        # 2026-11-04: Dan's key lacks the music_generation permission. Stand-in: the sound-effects model (at most 22 s, can loop);
        # give the key that permission (ElevenLabs > API keys) and run with --force --only <name> for the real music.
        print(f'  {name}: the key has no music permission; making a stand-in with the sound-effects model')
        body = {'text': prompt, 'duration_seconds': min(22, seconds), 'prompt_influence': .6, 'model_id': 'eleven_text_to_sound_v2'}
        if name == 'tension_low_energy': body['loop'] = True
        return post('https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128', key, body, name)
    body = {'text': prompt + '. Music cue, no voice.', 'duration_seconds': seconds, 'prompt_influence': .6, 'model_id': 'eleven_text_to_sound_v2'}
    return post('https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128', key, body, name)


def main():
    a = argparse.ArgumentParser(); a.add_argument('--only', default=''); a.add_argument('--force', action='store_true'); a.add_argument('--list', action='store_true')
    o = a.parse_args()
    if o.list:
        for t in TRACKS: print(t[0], t[2])
        return 0
    key = os.environ.get('ELEVENLABS_API_KEY', '')
    if not key: print('No ELEVENLABS_API_KEY: see Docs/MUSIC.md for the prompts.'); return 1
    only = set(x for x in o.only.split(',') if x); ok = 0; failed = []
    for name, sec, kind, prompt in TRACKS:
        if only and name not in only: continue
        out = (MUSIC_OUT if kind == 'music' else CUE_OUT) / f'{name}.mp3'
        if out.exists() and not o.force: print(f'{name}: kept ({out.stat().st_size} bytes)'); ok += 1; continue
        data, err = make(key, name, sec, kind, prompt)
        if data and len(data) > 1000:
            out.parent.mkdir(parents=True, exist_ok=True); out.write_bytes(data); ok += 1; print(f'{name}: {len(data)} bytes -> {out.relative_to(ROOT)}')
        else: print(f'{name}: FAILED ({err})'); failed.append(name)
    print(f'{ok} made / kept; failed: {", ".join(failed) or "none"}')
    return 0 if not failed else 2


if __name__ == '__main__':
    sys.exit(main())
