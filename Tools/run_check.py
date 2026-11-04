"""Hostile Horizon: start an exe check the way Dan's test rules require (CLAUDE.md, Process: "Testing on Dan's PC").
Muted (-mute; the game also mutes any -exeCheck run itself) and with no window at all (-batchmode: nothing pops up or takes focus;
the game draws its view off-screen for the screenshots, Core/TestRun.cs), Below Normal priority. Waits, then prints
<dir>/exe_check.txt.
Usage: python Tools/run_check.py <dir> [--only all|perf|campaign|home|fix24|fix25|fix26|aliens|mute|screen] [--minutes N] [--extra "-steamDeck 1"] [--size 1920x1080]"""
import argparse, pathlib, shutil, subprocess, sys
sys.stdout.reconfigure(encoding='utf-8', errors='replace')

ROOT = pathlib.Path(__file__).resolve().parents[1]
a = argparse.ArgumentParser(); a.add_argument('dir'); a.add_argument('--only', default='all'); a.add_argument('--minutes', type=float, default=25)
a.add_argument('--extra', default=''); a.add_argument('--size', default='1920x1080'); o = a.parse_args()
d = pathlib.Path(o.dir).resolve()
if d.exists(): shutil.rmtree(d)
d.mkdir(parents=True)
w, h = o.size.split('x')
args = [str(ROOT / 'Builds/HostileHorizon/HostileHorizon.exe'), '-batchmode', '-mute', '-exeCheck', str(d), '-exeCheckOnly', o.only, *o.extra.split(),
        '-screen-fullscreen', '0', '-screen-width', w, '-screen-height', h, '-logFile', str(d / 'Player.log')]
si = subprocess.STARTUPINFO(); si.dwFlags |= subprocess.STARTF_USESHOWWINDOW; si.wShowWindow = 7   # SW_SHOWMINNOACTIVE
p = subprocess.Popen(args, startupinfo=si, creationflags=subprocess.BELOW_NORMAL_PRIORITY_CLASS)
try: code = p.wait(timeout=o.minutes * 60)
except subprocess.TimeoutExpired: p.kill(); code = 'killed after %s min' % o.minutes
print('check exit', code)
f = d / 'exe_check.txt'
print(f.read_text(encoding='utf-8') if f.exists() else '\n'.join(l for l in (d / 'Player.log').read_text(encoding='utf-8', errors='replace').splitlines() if '[ExeCheck]' in l or 'Crash!!!' in l))
