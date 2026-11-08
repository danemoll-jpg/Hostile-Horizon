"""Round 8 (B2): level the ElevenLabs sounds to one loudness per kind (a plain gain, so loops stay seamless, and a peak limit).

python Tools/level_sounds.py [--only a,b]

One-shots -12 LUFS, the engine loops -16 LUFS, the ambiences and the regeneration bay -24 LUFS (the game plays them quietly under
the engine and the music); peaks under -1 dBFS. Originals are kept in SourceArt/Sounds_raw/ (run again from them: never levels
twice). Plays nothing. Needs ffmpeg on PATH.
"""
import argparse, re, shutil, subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SND = ROOT / 'Assets' / 'Resources' / 'Sounds'
RAW = ROOT / 'SourceArt' / 'Sounds_raw'
TARGET = {'engine_idle': -16, 'engine_cruise': -16, 'regen_bay': -24}
ONESHOT = ['gear_down', 'gear_up', 'airlock_cycle', 'dock_clamp', 'jump_drive', 'explosion']
AMB = ['amb_canyons', 'amb_forest', 'amb_city', 'amb_ice', 'amb_volcanic', 'amb_crystal', 'amb_ocean']
for n in ONESHOT: TARGET[n] = -12
for n in AMB: TARGET[n] = -24


def measure(path):
    out = subprocess.run(['ffmpeg', '-hide_banner', '-nostats', '-i', str(path), '-af', 'ebur128=peak=true', '-f', 'null', '-'], capture_output=True, text=True).stderr
    return float(re.findall(r'I:\s+(-?[\d.]+) LUFS', out)[-1]), float(re.findall(r'Peak:\s+(-?[\d.inf]+) dBFS', out)[-1])


def main():
    a = argparse.ArgumentParser(); a.add_argument('--only', default=''); o = a.parse_args()
    RAW.mkdir(parents=True, exist_ok=True)
    for n, t in TARGET.items():
        if o.only and n not in o.only.split(','): continue
        f = SND / f'{n}.mp3'; raw = RAW / f'{n}.mp3'
        if not raw.exists(): shutil.copy2(f, raw)
        i, p = measure(raw)
        gain = t - i
        tmp = f.with_suffix('.tmp.mp3')
        subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(raw), '-af', f'volume={gain:.2f}dB,alimiter=limit=0.89:attack=2:release=60:level=false',
                        '-c:a', 'libmp3lame', '-b:a', '128k', str(tmp)], check=True)
        tmp.replace(f)
        i2, p2 = measure(f)
        print(f'{n}: {i:.1f} LUFS (peak {p:.1f}) -> {i2:.1f} LUFS (peak {p2:.1f} dBFS), gain {gain:+.1f} dB')


if __name__ == '__main__':
    main()
