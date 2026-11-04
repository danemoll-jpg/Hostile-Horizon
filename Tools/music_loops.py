"""Hostile Horizon (Round 4, item B5): loop points for the music in BundleMusic/, written to BundleMusic/music.json.

For each track: the tempo (autocorrelation of the onset strength), the end of the intro and the start of the ending's fade (loudness),
then the loop: a jump from loopEnd back to loopStart, a whole number of bars apart, where the music sounds most alike (the spectrum
over the crossfade window before each point). The game (Audio/Music.cs) plays the intro once, then crossfades (equal power, over
`crossfade` seconds) from loopEnd - crossfade into loopStart - crossfade, so the ending's fade-out is never heard.
A track not in music.json still loops (Music.cs: the whole file with a 4 s crossfade). Needs ffmpeg and numpy. Nothing is played.
Usage: python Tools/music_loops.py [--dir BundleMusic]
"""
import argparse, json, pathlib, subprocess, sys
import numpy as np

ROOT = pathlib.Path(__file__).resolve().parents[1]
SR = 22050


def decode(path):
    raw = subprocess.run(['ffmpeg', '-v', 'error', '-i', str(path), '-ac', '1', '-ar', str(SR), '-f', 'f32le', '-'], capture_output=True, check=True).stdout
    return np.frombuffer(raw, dtype=np.float32)


def spectra(x, hop=512, n=2048):
    w = np.hanning(n).astype(np.float32); frames = 1 + (len(x) - n) // hop
    idx = np.arange(n)[None, :] + hop * np.arange(frames)[:, None]
    mag = np.abs(np.fft.rfft(x[idx] * w, axis=1)).astype(np.float32)
    # 48 log-spaced bands, log magnitude
    edges = np.unique(np.geomspace(2, mag.shape[1] - 1, 49).astype(int))
    bands = np.stack([mag[:, a:b].mean(axis=1) for a, b in zip(edges[:-1], edges[1:])], axis=1)
    return np.log1p(bands * 10), hop / SR


def tempo(spec, dt):
    onset = np.maximum(0, np.diff(spec, axis=0)).sum(axis=1); onset -= onset.mean()
    ac = np.correlate(onset, onset, 'full')[len(onset) - 1:]
    lags = np.arange(len(ac)) * dt; ok = (lags > 60 / 180) & (lags < 60 / 60)
    best = np.argmax(np.where(ok, ac, -np.inf)); beat = lags[best]
    return 60 / beat, beat, onset


def analyse(path, xfade):
    x = decode(path); dur = len(x) / SR
    spec, dt = spectra(x)
    bpm, beat, onset = tempo(spec, dt)
    bar = beat * 4
    # loudness per 0.5 s
    win = int(SR * .5); rms = np.sqrt(np.array([np.mean(x[i:i + win] ** 2) for i in range(0, len(x) - win, win)]) + 1e-12)
    med = np.median(rms)
    loud = np.where(rms > med * .6)[0]
    intro_end = loud[0] * .5 if len(loud) else 0
    fade_start = (loud[-1] + 1) * .5 if len(loud) else dur
    fade_start = min(fade_start, dur - 1)
    # beat phase: the offset that lines the beat grid up with the onsets
    phases = np.linspace(0, beat, 16, endpoint=False)
    def grid_score(ph):
        t = np.arange(ph, dur, beat); k = (t / dt).astype(int); k = k[k < len(onset)]; return onset[k].sum()
    phase = phases[np.argmax([grid_score(p) for p in phases])]
    nwin = max(4, int(xfade / dt))
    def window(t):
        k = int(t / dt); return spec[max(0, k - nwin):k].ravel()
    best = None
    end_hi = fade_start - .5; end_lo = max(intro_end + xfade + 30, end_hi - 45)
    ends = [phase + k * beat for k in range(int(end_lo / beat), int(end_hi / beat) + 1) if end_lo <= phase + k * beat <= end_hi]
    for le in ends:
        a = window(le)
        if len(a) == 0: continue
        n_bars_min = max(4, int(30 / bar))
        for nb in range(n_bars_min, int((le - xfade - intro_end) / bar) + 1):
            ls = le - nb * bar
            if ls - xfade < max(0, intro_end) or ls < xfade: continue
            b = window(ls)
            if len(b) != len(a): continue
            sim = float(np.dot(a - a.mean(), b - b.mean()) / (np.linalg.norm(a - a.mean()) * np.linalg.norm(b - b.mean()) + 1e-9))
            score = sim + .002 * nb * bar / 60   # a little in favour of longer loops
            if best is None or score > best[0]: best = (score, ls, le, sim)
    if best is None:   # too short for a bar-aligned loop: the whole file
        return dict(loopStart=round(xfade, 3), loopEnd=round(dur - .25, 3), crossfade=xfade, bpm=round(bpm, 1), length=round(dur, 2), match=None)
    _, ls, le, sim = best
    return dict(loopStart=round(ls, 3), loopEnd=round(le, 3), crossfade=xfade, bpm=round(bpm, 1), length=round(dur, 2), match=round(sim, 3),
                introEnd=round(intro_end, 1), fadeStart=round(fade_start, 1))


def main():
    a = argparse.ArgumentParser(); a.add_argument('--dir', default=str(ROOT / 'BundleMusic')); o = a.parse_args()
    d = pathlib.Path(o.dir); out = {}
    for f in sorted(d.glob('*.mp3')) + sorted(d.glob('*.ogg')) + sorted(d.glob('*.wav')):
        name = f.stem
        if name == 'regeneration': continue   # a one-shot cue: never loops
        xfade = 1.0 if name.startswith('tension') else 4.0
        r = analyse(f, xfade); out[name] = r
        print(f'{name}: {r["length"]:.0f} s, {r["bpm"]} bpm, loop {r["loopStart"]:.2f} -> {r["loopEnd"]:.2f} s ({r["loopEnd"] - r["loopStart"]:.1f} s), match {r["match"]}')
    (d / 'music.json').write_text(json.dumps(dict(schema=1, note='Loop points by Tools/music_loops.py; Audio/Music.cs reads them', tracks=out), indent=1))
    print(f'{len(out)} tracks -> {d / "music.json"}')


if __name__ == '__main__':
    sys.exit(main())
