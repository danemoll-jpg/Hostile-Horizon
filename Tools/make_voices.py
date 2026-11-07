"""Hostile Horizon (Round 4, Part C): the voices — Control, the ship's computer and the downed pilots — made with ElevenLabs
text-to-speech. Every line is written here (original, short); the script writes:
 - Assets/Resources/Voices/<id>.mp3     (the game loads them by id; Audio/Voices.cs)
 - Assets/Resources/Voices/lines.json   (id, speaker, text: the game's subtitles and the line lists)
 - Docs/VOICES.md                       (every line, voice and file, for Dan)
The key comes from ELEVENLABS_API_KEY (never printed or saved). Nothing is played: making a line never makes a sound on this PC.
The computer's lines get a light synthetic edge with ffmpeg (a short chorus) after they are made.
Usage: python Tools/make_voices.py [--only id,id or a prefix like p3_] [--force] [--list] [--doc]
"""
import argparse, json, os, pathlib, shutil, subprocess, sys, tempfile, time, urllib.request, urllib.error

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/Voices'
DOC = ROOT / 'Docs/VOICES.md'
MODEL = 'eleven_multilingual_v2'

# speaker key -> (ElevenLabs voice id, display, who, settings)
CONTROL = ('onwK4e9ZLuTAKqWW03F9', 'Daniel (premade)', 'Control: the mothership\'s flight controller. Calm, dry, seen it all.', dict(stability=.55, similarity_boost=.8, style=.15))
COMPUTER = ('SAz9YHcvj6GT2YYXdXww', 'River (premade)', 'The ship\'s computer: short, level, synthetic.', dict(stability=.95, similarity_boost=.7, style=0))
# the downed pilots: voice id, voice, callsign (the pilot's surname on the radio and in the subtitles), who
PILOTS = [
    ('EXAVITQu4vr4xnSDxMaL', 'Sarah (premade)', 'Hale', 'woman, young, American'),
    ('IKne3meq5aSn9XLyUdCD', 'Charlie (premade)', 'Kade', 'man, young, Australian'),
    ('JBFqnCBsd6RMkjVDRZzb', 'George (premade)', 'Penrose', 'man, middle-aged, British'),
    ('XrExE9yKIg1WjnnlVkGX', 'Matilda (premade)', 'Vasquez', 'woman, middle-aged, American'),
    ('SOYHLrjzK2X1ezoPC6cr', 'Harry (premade)', 'Marlow', 'man, young, rough, American'),
    ('Xb7hH8MSUJpSbSDYk0k2', 'Alice (premade)', 'Ashby', 'woman, middle-aged, British'),
    ('x9G2ivoqdzPgvaOC8XUa', 'Ricky Johnson (library)', 'Tate', 'man, young, US Southern'),
    ('cgSgspJ2msm6clMCkdW9', 'Jessica (premade)', 'Okafor', 'woman, young, American'),
    ('pqHfZKP75CvOlQylNhV4', 'Bill (premade)', 'Grange', 'man, older, American'),
    ('pFZP5JQG7iQjIQuC4Bku', 'Lily (premade)', 'Quill', 'woman, middle-aged, British'),
    ('N2lVS1w4EtoT3dr4eOWO', 'Callum (premade)', 'Brannock', 'man, middle-aged, husky, American'),
    ('bIHbv24MWmeRgasZH58o', 'Will (premade)', 'Lund', 'man, young, American'),
]
PILOT_SETTINGS = dict(stability=.38, similarity_boost=.78, style=.35)

CONTROL_LINES = [
    ('launch_1', 'launch', 'Bay doors open. You\'re clear to drop.'),
    ('launch_2', 'launch', 'Clamps released. Go and find our people.'),
    ('day', 'planet of the day', 'This one\'s the planet of the day. The corps pays half again for anyone you bring home from here.'),
    ('brief_canyons', 'new planet: Canyons', 'New world. Deep canyons, thin air, and beacons down in the cracks. Stay low and mind the walls.'),
    ('brief_forest', 'new planet: Giant Forest', 'Forest world. Trees the size of towers, and a canopy you can\'t see through. Our people are in the clearings.'),
    ('brief_city', 'new planet: Dead City', 'There\'s a dead city down there. Nobody\'s home. Except our pilots, in the streets and on the roofs.'),
    ('brief_ice', 'new planet: Ice', 'Ice world. Crevasses, white-outs, and cold enough to crack a hull. When the snow comes in, trust your radar.'),
    ('found_1', 'a beacon on the radar', 'Beacon on your scope. That\'s one of ours.'),
    ('found_2', 'a beacon on the radar', 'Another beacon. Bring them home.'),
    ('full', 'cabin full', 'Cabin\'s full. Climb to dock height and we\'ll take it from there.'),
    ('everyone', 'no one left waiting', 'That\'s everyone down there. Climb up and come home.'),
    ('dock_1', 'docking clearance', 'I have you. Hold steady, bringing you in.'),
    ('dock_2', 'docking clearance', 'Docking clearance granted. Easy does it.'),
    ('docked_1', 'docked', 'Clamps on. Welcome home.'),
    ('docked_2', 'docked', 'You\'re in. Get them to the infirmary.'),
    ('tow', 'tow', 'You\'re out of power. Tow beam\'s on its way. That\'s going on your bill.'),
    ('recall', 'recall', 'Recall received. We\'re coming to get you. It isn\'t free.'),
    ('cleared', 'planet cleared', 'That\'s the last of them. Planet\'s clear. Good work down there.'),
    ('jump', 'jump', 'Jump drive spooling. Hold on to something.'),
    ('regen_1', 'regeneration', 'Welcome back. Again.'),
    ('regen_2', 'regeneration', 'Welcome back. Try to keep this body in one piece.'),
    ('regen_3', 'regeneration', 'You\'re awake. Good. Take a minute. Then get back out there.'),
    ('engines_off', 'landed near a pilot, engines still on', 'Cut your engines. They won\'t come near a hot ship.'),
    ('low_energy', 'energy low', 'Watch your energy. I can\'t tow everyone.'),
    ('alien_killed', 'an alien burnt by the exhaust', 'That was no pilot. Good call.'),
    ('pilot_killed', 'a real pilot burnt by the exhaust', 'You just burned one of ours. That comes out of your pay.'),
    ('security', 'security kills a Sleeper in the bay', 'Security\'s dealt with your passenger. Don\'t ask.'),
    ('lost_contact', 'the ship lost', 'I\'ve lost your signal. Regeneration bay, stand by.'),
    # Round 5 (B6): the planet fights back
    ('hot', 'launch: the planet has turrets', 'Heads up, this one\'s hot. Gun turrets on the high ground. Keep low and they can\'t see you.'),
    ('hot_saucers', 'launch: turrets and saucers', 'Heads up, this one\'s hot. Turrets on the high ground, and saucers in the air. Stay low, keep moving.'),
    # Round 6 (D3): new downed pilots on a planet you'd cleared
    ('distress', 'at the dock: new distress calls on an old planet', 'New distress calls coming in from a planet you\'d cleared. More of ours went down there. It\'s on the star map.'),
    ('upgrade', 'the hangar: an upgrade fitted', 'Workshop says she\'s ready. Try not to bend it.'),
]

COMPUTER_LINES = [
    ('energy_low', 'Energy low.'),
    ('return_now', 'Return now.'),
    ('cant_reach', 'Mothership out of reach.'),
    ('pull_up', 'Pull up.'),
    ('hull_damage', 'Hull damage.'),
    ('hull_critical', 'Hull critical.'),
    ('gear_down', 'Landing gear down.'),
    ('airlock_open', 'Airlock open.'),
    ('pilot_aboard', 'Pilot aboard.'),
    ('cabin_full', 'Cabin full.'),
    ('intruder', 'Intruder alert. Intruder alert.'),
    ('engines_off', 'Engines off.'),
    ('whiteout', 'Visibility low. Instruments only.'),
    # Round 5 (B4, B6)
    ('locked_on', 'Warning. Locked on.'),
    ('saucer', 'Saucer. Saucer.'),
    ('shield_up', 'Shield up.'),
    ('shield_down', 'Shield down.'),
    ('target_down', 'Target destroyed.'),
]

# per pilot voice; {n} = their callsign. *_wrong = the Mimic's voice sign (a word repeats, a breath in the wrong place).
PILOT_LINES = [
    ('mayday', 'mayday (any biome)', 'Mayday, mayday, this is Corps pilot {n}. I\'m down, beacon\'s on. Is anyone up there?'),
    ('mayday_canyons', 'mayday (Canyons)', 'Mayday, this is {n}. I\'m down on a canyon floor. Ship\'s gone. Please, come and get me.'),
    ('mayday_forest', 'mayday (Giant Forest)', 'Mayday, mayday, {n} here. I came down through the trees. I\'m in a clearing, I think. Please hurry.'),
    ('mayday_city', 'mayday (Dead City)', 'This is Corps pilot {n}, I\'m down in the city. It\'s so quiet here. Please, somebody.'),
    ('mayday_ice', 'mayday (Ice)', 'Mayday, this is {n}. I\'m down on the ice. It\'s cold. It\'s so cold. Come get me.'),
    ('mayday_wrong', 'mayday, wrong (Mimic sign)', 'Mayday, mayday, this is... this is Corps pilot {n}. I\'m down, I\'m down. Beacon\'s on. Is anyone... up there?'),
    ('see_1', 'sees the ship land', 'I see you! I see you! I\'m coming!'),
    ('see_2', 'sees the ship land', 'That\'s you! Hold on, I\'m on my way!'),
    ('door_1', 'at the airlock (through the hull)', 'It\'s me! Open up!'),
    ('door_2', 'at the airlock (through the hull)', 'Open the hatch! Please, open it!'),
    ('door_wrong', 'at the airlock, wrong (Mimic sign)', 'It\'s me. It\'s me. Open... open up.'),
    ('thanks_1', 'aboard', 'Oh, thank you. Thank you. I thought nobody was coming.'),
    ('thanks_2', 'aboard', 'You\'re a sight for sore eyes. Thanks for the lift.'),
    ('chat_1', 'cabin chatter', 'How long till we dock?'),
    ('chat_2', 'cabin chatter', 'I\'m never going back down there. Never.'),
    ('chat_3', 'cabin chatter', 'Does anyone else hear that?'),
    ('scream', 'the Sleeper attacks', 'No! No, get away from me! Get it off!'),
]


def lines():
    out = []
    for i, t, txt in CONTROL_LINES: out.append(dict(id='ctl_' + i, speaker='Control', voice=CONTROL[0], settings=CONTROL[3], text=txt, use=t))
    for i, txt in COMPUTER_LINES: out.append(dict(id='cpu_' + i, speaker='Computer', voice=COMPUTER[0], settings=COMPUTER[3], text=txt, use='ship\'s computer'))
    for v, (vid, vname, call, who) in enumerate(PILOTS):
        for i, t, txt in PILOT_LINES:
            out.append(dict(id=f'p{v}_{i}', speaker=call, voice=vid, settings=PILOT_SETTINGS, text=txt.format(n=call), use=t))
    return out


def write_doc(all_lines):
    L = ['# Hostile Horizon — voices (Round 4, Part C)', '',
         'Made by `python Tools/make_voices.py` (ElevenLabs text-to-speech, model `' + MODEL + '`; key from `ELEVENLABS_API_KEY`, never',
         'printed or saved). Files: `Assets/Resources/Voices/<id>.mp3`; the subtitles and line lists the game uses are in',
         '`Assets/Resources/Voices/lines.json` (written by the same script). Code: `Assets/Scripts/Audio/Voices.cs`.',
         'Re-make a line: `python Tools/make_voices.py --only <id> --force` (or a prefix: `--only p3_`). To replace one by hand, drop a',
         'file with the same name in that folder and rebuild.', '',
         '## How they sound in the game', '',
         '- **Control** and the **pilots on the radio**: a radio filter (band-limited 350-3200 Hz, a little drive, light crackle) with a',
         '  click in and a click out.',
         '- **The ship\'s computer**: clean and close (2D, no filter); made with a light chorus for a synthetic edge.',
         '- **A pilot at the airlock**: muffled through the hull (low-pass ~900 Hz), from the door, in 3D.',
         '- **Cabin chatter / screams**: from the cabin behind you, through the bulkhead.',
         '- One line at a time: a queue with priorities (computer warnings first, then Control, then pilots); music ducks under them.',
         '- Subtitles (Settings > Sound > Subtitles, On by default) at the bottom of the HUD and on the cockpit\'s centre display.',
         '', '## The aliens use the voices too', '',
         'An alien disguised as a pilot also radios its mayday, shouts "I see you" and talks at the airlock, in a pilot\'s voice, so a',
         'voice proves nothing. **The Mimic\'s voice sign** (some of the time, seeded; Round 3\'s tell rules): its mayday and its voice at',
         'the door are subtly wrong (a word repeats, a breath in the wrong place: the `*_wrong` lines), or it radios with the callsign',
         'and voice of a pilot already lost on that planet. No other type has a voice sign.', '',
         '## Voices', '', '| Speaker | ElevenLabs voice | Who |', '|---|---|---|',
         f'| Control | {CONTROL[1]} | {CONTROL[2]} |', f'| Computer | {COMPUTER[1]} | {COMPUTER[2]} |']
    for v, (vid, vname, call, who) in enumerate(PILOTS): L.append(f'| Pilot {call} (`p{v}`) | {vname} | {who} |')
    L += ['', '## Every line', '', '| File | Speaker | When | Line |', '|---|---|---|---|']
    for l in all_lines: L.append(f'| `{l["id"]}.mp3` | {l["speaker"]} | {l["use"]} | {l["text"]} |')
    DOC.write_text('\n'.join(L) + '\n', encoding='utf-8')


def tts(key, l):
    body = {'text': l['text'], 'model_id': MODEL, 'voice_settings': l['settings']}
    for attempt in range(3):
        req = urllib.request.Request(f'https://api.elevenlabs.io/v1/text-to-speech/{l["voice"]}?output_format=mp3_44100_128', data=json.dumps(body).encode(),
                                     headers={'xi-api-key': key, 'Content-Type': 'application/json', 'Accept': 'audio/mpeg'}, method='POST')
        try:
            with urllib.request.urlopen(req, timeout=120) as r: return r.read()
        except urllib.error.HTTPError as e:
            msg = e.read().decode('utf-8', 'replace')[:300]; print(f'  {l["id"]}: HTTP {e.code} {msg}')
            if e.code in (401, 402, 403): return None
            time.sleep(4)
        except Exception as e:
            print(f'  {l["id"]}: {e}'); time.sleep(4)
    return None


def synthetic(path):
    tmp = path.with_suffix('.tmp.mp3')
    r = subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(path), '-af', 'chorus=0.6:0.85:18|26:0.35|0.25:0.4|0.5:2|1.6,highpass=f=140', '-b:a', '128k', str(tmp)], capture_output=True)
    if r.returncode == 0 and tmp.exists(): shutil.move(str(tmp), str(path))


def main():
    a = argparse.ArgumentParser(); a.add_argument('--only', default=''); a.add_argument('--force', action='store_true')
    a.add_argument('--list', action='store_true'); a.add_argument('--doc', action='store_true'); o = a.parse_args()
    all_lines = lines()
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'lines.json').write_text(json.dumps(dict(schema=1, pilots=[p[2] for p in PILOTS],
        lines=[dict(id=l['id'], speaker=l['speaker'], text=l['text']) for l in all_lines]), indent=1), encoding='utf-8')
    write_doc(all_lines)
    if o.list:
        for l in all_lines: print(l['id'], '|', l['text'])
        print(len(all_lines), 'lines,', sum(len(l['text']) for l in all_lines), 'characters'); return 0
    if o.doc: return 0
    key = os.environ.get('ELEVENLABS_API_KEY', '')
    if not key: print('No ELEVENLABS_API_KEY'); return 1
    only = [x for x in o.only.split(',') if x]; made = kept = 0; failed = []
    for l in all_lines:
        if only and not any(l['id'] == x or (x.endswith('_') and l['id'].startswith(x)) for x in only): continue
        f = OUT / f'{l["id"]}.mp3'
        if f.exists() and not o.force: kept += 1; continue
        data = tts(key, l)
        if data and len(data) > 500:
            f.write_bytes(data); made += 1
            if l['speaker'] == 'Computer': synthetic(f)
        else: failed.append(l['id'])
    print(f'{made} made, {kept} kept, failed: {", ".join(failed) or "none"}')
    return 0 if not failed else 2


if __name__ == '__main__':
    sys.exit(main())
