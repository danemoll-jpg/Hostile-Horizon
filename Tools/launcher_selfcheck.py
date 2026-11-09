"""Hostile Horizon Round 1 (item 9): the launcher's self-check against a local test catalog. Nothing is uploaded; the real catalog
(github.com/danemoll-jpg/Hostile-Horizon-releases) is never touched.

1. Two signed local releases of the current build (Builds/HostileHorizon) with Prepare-LauncherRelease.py: build 1 (the installed
   version) and build 2 (the "update"), served from http://127.0.0.1:<port>/ by a small web server in this script.
2. An install folder made from build 1's starter, with the launcher beside it.
3. The launcher run as a player would start it, in its isolated check mode:
   HostileHorizonLauncher.exe --auto-check --root <install> --test-save <saves> --evidence <evidence> --catalog <local catalog>
   It checks the catalog, downloads and verifies build 2, installs it, starts the game (hidden, -batchmode), waits for the game's
   ready signal (LauncherBridge), and the game closes itself.
4. Prints what happened: the launcher's status lines, the startup handshake, the installed build.
Output: Builds/LauncherCheck/ (deleted by the round clean-up). Usage: python Tools/launcher_selfcheck.py"""
import functools, http.server, json, pathlib, shutil, socketserver, subprocess, sys, threading

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'Builds/LauncherCheck'


def main():
    if OUT.exists(): shutil.rmtree(OUT)
    OUT.mkdir(parents=True)
    music = OUT / 'music'; music.mkdir(); (music / 'placeholder.txt').write_text('No soundtrack yet (Round 1).')
    handler = functools.partial(http.server.SimpleHTTPRequestHandler, directory=str(OUT))
    http.server.SimpleHTTPRequestHandler.log_message = lambda *a: None
    server = socketserver.TCPServer(('127.0.0.1', 0), handler); port = server.server_address[1]
    threading.Thread(target=server.serve_forever, daemon=True).start()
    for build, version in ((1, '0.1.0'), (2, '0.1.1')):
        subprocess.run([sys.executable, str(ROOT / 'Tools/Prepare-LauncherRelease.py'), '--game', str(ROOT / 'Builds/HostileHorizon'), '--bundle-music', str(music),
                        '--out', str(OUT / f'r{build}'), '--version', version, '--build', str(build), '--music-build', str(build),
                        '--notes', f'Launcher self-check build {build}', '--base-url', f'http://127.0.0.1:{port}/r{build}/assets'], check=True)
    install = OUT / 'install'
    shutil.copytree(OUT / 'r1/starter', install)
    shutil.copy2(ROOT / 'Builds/Launcher/HostileHorizonLauncher.exe', install / 'HostileHorizonLauncher.exe')
    cover = ROOT / 'SourceArt/Title/Steam/HostileHorizon-Cover.png'
    if cover.exists(): shutil.copy2(cover, install / 'HostileHorizon-Cover.png')
    evidence = OUT / 'evidence'; saves = OUT / 'saves'
    cmd = [str(install / 'HostileHorizonLauncher.exe'), '--auto-check', '--root', str(install), '--test-save', str(saves), '--evidence', str(evidence),
           '--catalog', f'http://127.0.0.1:{port}/r2/assets/update-catalog.json']
    p = subprocess.Popen(cmd, creationflags=0x00004000)   # BELOW_NORMAL_PRIORITY_CLASS
    try: code = p.wait(timeout=600)
    except subprocess.TimeoutExpired: p.kill(); code = 'timeout'
    server.shutdown()
    print('launcher exit:', code)
    for name in ('auto-status.txt', 'startup.json', 'failure.txt'):
        f = evidence / name
        if f.exists(): print(f'--- {name}\n' + f.read_text(encoding='utf-8', errors='replace').strip())
    state = json.loads((install / 'state.json').read_text())
    print('installed game build:', state['game']['manifest']['build'], 'in', state['game']['directory'])
    log = evidence / 'game.log'
    if log.exists(): print('game log ready line:', [l for l in log.read_text(errors='replace').splitlines() if '[Launcher]' in l])
    print('evidence:', evidence)


if __name__ == '__main__':
    main()
