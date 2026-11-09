"""Hostile Horizon, Round 10 (B2): the promo narration by Control (the in-game voice: make_voices.py's CONTROL voice id, model and
settings), one take per line so the edit can time each one. The approved script (Dan, 2026-11-08) + "Rescue them." (n10, Dan's
notes on the rough cut); the ending lines e01-e03 are no longer used (the ending plays the game's own lines: promo_edit.py "game_voice"). The key comes from ELEVENLABS_API_KEY (process or Windows user environment; never printed or written). Making a line plays
nothing. The radio filter is added in the edit (Tools/promo_edit.py), lighter than the game's.
Usage: python Tools/promo_voice.py [--force] [--only n01,e02]   ->  SourceArt/Promo/voice/<id>.mp3 + lines.json"""
import argparse, json, os, pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import make_voices as mv

OUT = mv.ROOT / 'SourceArt/Promo/voice'
LINES = [
    ('n01', "This is Control."),
    ('n02', "The colony fleet went down over the frontier."),
    ('n03', "Hundreds of pilots, scattered across worlds that want them dead."),
    ('n04', "You fly in low."),
    ('n05', "Below the guns, through the canyons, through the forests, between the towers of dead cities."),
    ('n06', "Find them. Land. Open the airlock."),
    ('n07', "Just make sure it's really them."),
    ('n08', "Fly it with a HOTAS. Step into the cockpit in VR."),
    ('n09', "Hostile Horizon."),
    ('n10', "Rescue them."),   # Dan's notes on the rough cut (2026-11-09)
    ('e01', "Rescue Seven, respond."),
    ('e02', "Rescue Seven?"),
    ('e03', "We've lost contact."),
]
ENDING = dict(stability=.38, similarity_boost=.8, style=.3)


def key():
    k = os.environ.get('ELEVENLABS_API_KEY', '')
    if not k and os.name == 'nt':
        try:
            import winreg
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, 'Environment') as h: k = winreg.QueryValueEx(h, 'ELEVENLABS_API_KEY')[0]
        except OSError: pass
    return k


def main():
    a = argparse.ArgumentParser(); a.add_argument('--force', action='store_true'); a.add_argument('--only', default='')
    o = a.parse_args(); only = set(x for x in o.only.split(',') if x)
    k = key()
    if not k: sys.exit('ELEVENLABS_API_KEY is not set')
    OUT.mkdir(parents=True, exist_ok=True)
    for i, text in LINES:
        if only and i not in only: continue
        p = OUT / (i + '.mp3')
        if p.exists() and not o.force: print(f'{i}: kept'); continue
        l = dict(id=i, text=text, voice=mv.CONTROL[0], settings=ENDING if i.startswith('e') else mv.CONTROL[3])
        data = mv.tts(k, l)
        if not data: sys.exit(f'{i}: not made')
        p.write_bytes(data); print(f'{i}: {len(data)} bytes')
    (OUT / 'lines.json').write_text(json.dumps([dict(id=i, text=t) for i, t in LINES], indent=1), encoding='utf-8')


if __name__ == '__main__': main()
