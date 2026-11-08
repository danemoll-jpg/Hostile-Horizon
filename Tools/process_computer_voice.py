"""Fix 6.3: make the ship's computer cut through the engine: every cpu_*.mp3 in Assets/Resources/Voices gets a light clean-up
(high-pass 120 Hz, +4 dB presence at 3 kHz, gentle compression) and is levelled to the same loudness (-16 LUFS, peaks under -1 dBFS).

The originals (as make_voices.py wrote them) are kept once in SourceArt/Voices/computer_raw/ and always processed from there, so
running this again gives the same files. After `make_voices.py --force --only cpu_`, delete the raw copies and run this again.
Plays nothing. Needs ffmpeg on PATH.

    python Tools/process_computer_voice.py [--target -16]
"""
import argparse, re, shutil, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VOICES = ROOT / 'Assets/Resources/Voices'
RAW = ROOT / 'SourceArt/Voices/computer_raw'
CHAIN = 'highpass=f=120,equalizer=f=3000:t=o:w=1.4:g=4,acompressor=threshold=-22dB:ratio=3:attack=5:release=90:makeup=2'


def loudness(path, chain=None):
    af = (chain + ',' if chain else '') + 'ebur128=peak=true'
    out = subprocess.run(['ffmpeg', '-hide_banner', '-nostats', '-i', str(path), '-af', af, '-f', 'null', '-'], capture_output=True, text=True).stderr
    i = re.findall(r'I:\s+(-?[\d.]+) LUFS', out); p = re.findall(r'Peak:\s+(-?[\d.inf]+) dBFS', out)
    return float(i[-1]), float(p[-1])


def main():
    a = argparse.ArgumentParser(); a.add_argument('--target', type=float, default=-16.0); o = a.parse_args()
    RAW.mkdir(parents=True, exist_ok=True)
    files = sorted(VOICES.glob('cpu_*.mp3'))
    if not files: sys.exit('no cpu_*.mp3 in ' + str(VOICES))
    rows = []
    for f in files:
        raw = RAW / f.name
        if not raw.exists(): shutil.copy2(f, raw)
        before, _ = loudness(raw)
        mid, peak = loudness(raw, CHAIN)
        gain = min(o.target - mid, -1.0 - peak + 6)   # the limiter takes what is over -1 dBFS
        chain = f'{CHAIN},volume={gain:.2f}dB,alimiter=limit=0.89:attack=2:release=40:level=disabled'
        tmp = f.with_suffix('.tmp.mp3')
        subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(raw), '-af', chain, '-c:a', 'libmp3lame', '-b:a', '128k', str(tmp)], check=True)
        tmp.replace(f)
        after, apeak = loudness(f)
        rows.append((f.name, before, after, apeak))
    print(f'{"line":24} {"before LUFS":>12} {"after LUFS":>11} {"peak dBFS":>10}')
    for n, b, x, p in rows: print(f'{n:24} {b:12.1f} {x:11.1f} {p:10.1f}')
    bs = [r[1] for r in rows]; xs = [r[2] for r in rows]
    print(f'spread: before {max(bs) - min(bs):.1f} LU ({min(bs):.1f} .. {max(bs):.1f}), after {max(xs) - min(xs):.1f} LU ({min(xs):.1f} .. {max(xs):.1f})')


if __name__ == '__main__':
    main()
