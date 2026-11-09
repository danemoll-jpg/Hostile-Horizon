"""Hostile Horizon, Round 10 (B3): capture the promo footage from the built exe, frame by frame, the way Dan's test rules require
(CLAUDE.md, "Testing on Dan's PC"): -batchmode (no window at all), -mute (silent from the first frame; sound is added in the edit),
Below Normal priority. The exe renders each frame at a fixed 1/60 s and pipes it to ffmpeg (Checks/ExeCheck.Promo.cs).

Usage: python Tools/promo_capture.py <group> [--size 3840x2160] [--out Promo/footage] [--day 20261109] [--seed 1984] [--minutes 90]
  groups: free | launch | combat | ending_<type> (hollow, stalker, knocker, mimic, sleeper) | test
Writes Promo/footage/<group>/<shot>.mp4, <shot>.events.txt, sfx_*.wav, exe_check.txt, Player.log."""
import os, argparse, pathlib, shutil, subprocess, sys
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
ROOT = pathlib.Path(__file__).resolve().parents[1]
a = argparse.ArgumentParser(); a.add_argument('group'); a.add_argument('--size', default='3840x2160'); a.add_argument('--out', default=str(ROOT / 'Promo/footage'))
a.add_argument('--day', default='20261109'); a.add_argument('--seed', default='1984'); a.add_argument('--minutes', type=float, default=90)
o = a.parse_args()
ff = shutil.which('ffmpeg')
if not ff: sys.exit('ffmpeg not found on PATH')
d = pathlib.Path(o.out).resolve() / o.group
if d.exists(): shutil.rmtree(d)
d.mkdir(parents=True)
w, h = o.size.split('x')
args = [str(ROOT / 'Builds/HostileHorizon/HostileHorizon.exe'), '-batchmode', '-mute', '-exeCheck', str(d), '-exeCheckOnly', 'promo:' + o.group,
        '-promoFfmpeg', ff, '-promoDay', o.day, '-promoSeed', o.seed, '-screen-fullscreen', '0', '-screen-width', w, '-screen-height', h, '-logFile', str(d / 'Player.log')]
si = subprocess.STARTUPINFO(); si.dwFlags |= subprocess.STARTF_USESHOWWINDOW; si.wShowWindow = 7   # SW_SHOWMINNOACTIVE
p = subprocess.Popen(args, startupinfo=si, creationflags=subprocess.BELOW_NORMAL_PRIORITY_CLASS)
try: code = p.wait(timeout=o.minutes * 60)
except subprocess.TimeoutExpired: p.kill(); code = 'killed after %s min' % o.minutes
print('capture exit', code)
f = d / 'exe_check.txt'
print(f.read_text(encoding='utf-8') if f.exists() else '\n'.join(l for l in (d / 'Player.log').read_text(encoding='utf-8', errors='replace').splitlines() if '[ExeCheck]' in l))
