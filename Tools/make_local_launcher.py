"""Hostile Horizon Round 1 (item 9): a local launcher install of the current build, so Dan can start the game through the launcher
before anything is published. Builds/LauncherLocal/HostileHorizon/ gets the launcher, the cover and a signed local copy of
Builds/HostileHorizon (build 1). The launcher checks the real catalog (github.com/danemoll-jpg/Hostile-Horizon-releases); with no
release there yet it says it couldn't check and starts the installed game. Re-run after every build ("Play Hostile Horizon
(launcher).bat" does it for you when the build is newer). Usage: python Tools/make_local_launcher.py"""
import pathlib, shutil, subprocess, sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'Builds/LauncherLocal'


def main():
    if OUT.exists(): shutil.rmtree(OUT)
    work = OUT / 'release'; music = OUT / 'music'; music.mkdir(parents=True); (music / 'placeholder.txt').write_text('No soundtrack yet.')
    subprocess.run([sys.executable, str(ROOT / 'Tools/Prepare-LauncherRelease.py'), '--game', str(ROOT / 'Builds/HostileHorizon'), '--bundle-music', str(music),
                    '--out', str(work), '--version', '0.1.0', '--build', '1', '--music-build', '1', '--notes', 'Local build'], check=True)
    home = OUT / 'HostileHorizon'
    shutil.move(str(work / 'starter'), str(home))
    shutil.copy2(ROOT / 'Builds/Launcher/HostileHorizonLauncher.exe', home / 'HostileHorizonLauncher.exe')
    shutil.copy2(ROOT / 'SourceArt/Title/HostileHorizonCover.png', home / 'HostileHorizon-Cover.png')
    (home / 'Launcher-Licenses').mkdir()
    for name in ('JSON-LICENSE.txt', 'MINIZ-LICENSE.txt'): shutil.copy2(ROOT / 'Launcher/vendor' / name, home / 'Launcher-Licenses' / name)
    shutil.rmtree(work); shutil.rmtree(music)
    (home / 'made.txt').write_text('Made by Tools/make_local_launcher.py from Builds/HostileHorizon')
    print('local launcher install:', home / 'HostileHorizonLauncher.exe')


if __name__ == '__main__':
    main()
