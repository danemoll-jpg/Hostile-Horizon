"""Hostile Horizon, Round 10 (B4): cut the promo from the captured footage with ffmpeg.
An edit list (Promo/edit_trailer.json, Promo/edit_vertical.json) gives, in order: the shots (Promo/footage/<group>/<shot>.mp4 from
Tools/promo_capture.py; in point, length, the vertical cut's crop position), black, cards; on-screen text and images; the narration
(SourceArt/Promo/voice/<id>.mp3 from Tools/promo_voice.py, a light radio filter, ducking the music); music segments (Dan's Suno
tracks, BundleMusic/); sound effects at fixed times, and the game's own sounds placed on the frames the capture logged
(<shot>.events.txt: knocks, gun, turret fire, hits, explosions, touchdown, the strike) plus an engine bed under flight shots.
The ending plays the game's own strike: its lines from Assets/Resources/Voices ("game_voice": the computer with its chime, Control on
the game's radio filter; "stop" cuts a line where the game stops it) and its stings in full.
Subtitles (the narration's words) are burned in when the list asks (the vertical cut: phones play muted).
Usage: python Tools/promo_edit.py Promo/edit_trailer.json [--rough | --4k] [--out file.mp4] [--sheet]
  --rough: half size, quick encode (the previews in Promo/rough/); --sheet: a contact sheet of every shot's middle frame.
Loudness: two-pass loudnorm to -14 LUFS, true peak -1.5 dB (final); one pass for rough."""
import argparse, json, os, pathlib, re, subprocess, sys, tempfile
from PIL import Image, ImageDraw, ImageFont, ImageFilter
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
ROOT = pathlib.Path(__file__).resolve().parents[1]
FOOT = ROOT / 'Promo/footage'; VOICE = ROOT / 'SourceArt/Promo/voice'; SND = ROOT / 'Assets/Resources/Sounds'; BUILD = ROOT / 'Promo/build'; GVOICE = ROOT / 'Assets/Resources/Voices'
FONT = 'C:/Windows/Fonts/bahnschrift.ttf'

# the game's sounds for the capture's events: event prefix -> (file, gain dB)
EVENT_SFX = {'knock': ('FOOT/launch/sfx_knock.wav', -2), 'gun': ('gun_fire.mp3', -12), 'enemy_fire': ('turret_fire.mp3', -8), 'hit': ('bolt_hit_hull.mp3', -6),
             'shield_block': ('bolt_hit_shield.mp3', -6), 'enemy_destroyed': ('enemy_explosion.mp3', -2), 'touchdown': ('gear_down.mp3', -8),
             'shield_on': ('shield_on.mp3', -6), 'brake': ('FOOT/launch/sfx_brake.wav', -6), 'strike': ('creature_lunge.mp3', 0), 'boarded': ('airlock_cycle.mp3', -8)}


def font(size, weight='Bold'):
    f = ImageFont.truetype(FONT, size)
    try: f.set_variation_by_name(weight)
    except Exception: pass
    return f


def text_png(path, W, H, text, size, pos='center', weight='Bold', color=(255, 255, 255), box=False):
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im); f = font(size, weight)
    lines = text.split('\n'); sizes = [d.textbbox((0, 0), l, font=f) for l in lines]; lh = int(size * 1.2)
    th = lh * len(lines); y0 = {'center': (H - th) // 2, 'lower': int(H * .72) - th // 2, 'bottom': int(H * .80) - th // 2, 'top': int(H * .12),
                               'topleft': int(H * .07)}[pos]
    sh = Image.new('RGBA', (W, H), (0, 0, 0, 0)); ds = ImageDraw.Draw(sh)
    for i, (l, b) in enumerate(zip(lines, sizes)):
        w = b[2] - b[0]; x = int(W * .06) if pos == 'topleft' else (W - w) // 2; y = y0 + i * lh
        if box: ds.rounded_rectangle((x - size * .45, y - size * .18, x + w + size * .45, y + size * 1.12), radius=size // 4, fill=(0, 0, 0, 150))
        else: ds.text((x + 3, y + 4), l, font=f, fill=(0, 0, 0, 200))
        d.text((x, y), l, font=f, fill=color + (255,))
    if not box: sh = sh.filter(ImageFilter.GaussianBlur(size / 14))
    Image.alpha_composite(sh, im).save(path)


def image_png(path, W, H, src, width_share, pos='center'):
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); a = Image.open(ROOT / src).convert('RGBA')
    w = int(W * width_share); a = a.resize((w, int(a.height * w / a.width)), Image.LANCZOS)
    y = {'center': (H - a.height) // 2, 'top': int(H * .14), 'upper': int(H * .30) - a.height // 2}[pos]
    im.alpha_composite(a, ((W - w) // 2, y)); im.save(path)


def card_png(path, W, H, c):
    bg = Image.new('RGB', (W, H), (0, 0, 0))
    if c.get('art'):
        a = Image.open(ROOT / c['art']).convert('RGB'); s = max(W / a.width, H / a.height); a = a.resize((int(a.width * s) + 1, int(a.height * s) + 1), Image.LANCZOS)
        a = a.crop(((a.width - W) // 2, (a.height - H) // 2, (a.width - W) // 2 + W, (a.height - H) // 2 + H))
        bg = Image.blend(bg, a, c.get('art_level', .55))
    bg = bg.convert('RGBA')
    if c.get('logo'):
        lg = Image.open(ROOT / c['logo']).convert('RGBA'); w = int(W * c.get('logo_w', .55)); lg = lg.resize((w, int(lg.height * w / lg.width)), Image.LANCZOS)
        bg.alpha_composite(lg, ((W - w) // 2, int(H * c.get('logo_y', .38)) - lg.height // 2))
    if c.get('text'):
        t = Image.new('RGBA', (W, H)); text_png(str(path) + '.t.png', W, H, c['text'], int(H * c.get('text_h', .045)), c.get('text_pos', 'bottom'), 'SemiBold')
        bg.alpha_composite(Image.open(str(path) + '.t.png')); os.remove(str(path) + '.t.png')
    bg.convert('RGB').save(path)


def dur(p): return float(subprocess.run(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', str(p)], capture_output=True, text=True).stdout.strip())


def events(shot):
    p = FOOT / (shot + '.events.txt'); out = []
    if p.exists():
        for l in p.read_text(encoding='utf-8').splitlines():
            if '\t' in l: f, e = l.split('\t', 1); out.append((int(f) / 60.0, e))
    return out


def sound(name):
    if name.startswith('FOOT/'): return FOOT / name[5:]
    p = pathlib.Path(name)
    return p if p.is_absolute() else (SND / name if (SND / name).exists() else ROOT / name)


def game_clips(tmp):
    """The computer's chime and the radio's click, made the way Audio/Voices.cs makes them."""
    import wave, math, struct
    def save(name, d):
        with wave.open(str(tmp / name), 'wb') as w:
            w.setnchannels(1); w.setsampwidth(2); w.setframerate(48000); w.writeframes(b''.join(struct.pack('<h', int(max(-1, min(1, x)) * 32767)) for x in d))
    r = 48000; ch = []
    for i in range(int(r * .36)):
        t = i / r; f = 880 if t < .16 else 1318.5; t0 = t if t < .16 else t - .16
        ch.append(math.sin(t * 2 * math.pi * f) * min(1, t0 * 200) * math.exp(-t0 * 9) * .6)
    save('chime.wav', ch)
    save('click.wav', [math.exp(-i / r * 300) * math.sin(i / r * 2 * math.pi * 2400) * .8 + math.exp(-i / r * 900) * .6 for i in range(r // 20)])


def main():
    a = argparse.ArgumentParser(); a.add_argument('edit'); a.add_argument('--rough', action='store_true'); a.add_argument('--out', default=''); a.add_argument('--sheet', action='store_true'); a.add_argument('--4k', dest='k4', action='store_true')
    o = a.parse_args(); E = json.loads(pathlib.Path(o.edit).read_text(encoding='utf-8'))
    W, H = E['size']; vertical = H > W
    if o.rough: W, H = W // 2, H // 2
    if o.k4: W, H = W * 2, H * 2   # the trailer at 3840 x 2160 (the footage is 4K)
    name = pathlib.Path(o.edit).stem.replace('edit_', ''); tmp = BUILD / (name + ('_rough' if o.rough else '') + ('_4k' if o.k4 else '')); tmp.mkdir(parents=True, exist_ok=True)
    out = pathlib.Path(o.out) if o.out else ROOT / ('Promo/rough' if o.rough else 'Promo') / (('HostileHorizon_' + name.capitalize()) + ('_rough' if o.rough else '') + ('_4K' if o.k4 else '') + '.mp4')
    out.parent.mkdir(parents=True, exist_ok=True)
    ins, vf, af = [], [], []
    if E.get('game_voice'): game_clips(tmp)
    def inp(*args): ins.extend(args); return len([x for x in ins if x == '-i']) - 1
    # ---- video
    t = 0.0; vlabels = []; sfx = []; timeline = []
    for k, it in enumerate(E['video']):
        d = float(it['dur'])
        if 'shot' in it:
            src = FOOT / (it['shot'] + '.mp4'); i0 = float(it.get('in', 0))
            n = inp('-ss', f'{i0:.3f}', '-t', f'{d:.3f}', '-i', str(src))
            if vertical: cx = float(it.get('cx', .5)); geo = f"crop=ih*9/16:ih:(iw-ih*9/16)*{cx}:0,scale={W}:{H}:flags=lanczos"
            else: geo = f"scale={W}:{H}:flags=lanczos"
            ch = f"[{n}:v]{geo},fps=60,format=yuv420p,setsar=1"
            for e_t, e in events(it['shot']):
                if i0 <= e_t < i0 + d:
                    key = e.split(' ')[0]
                    if key in EVENT_SFX and key not in it.get('mute_events', []): sfx.append(dict(file=EVENT_SFX[key][0], t=t + e_t - i0, gain=EVENT_SFX[key][1] + it.get('event_gain', 0)))
            if it.get('engine', 0): sfx.append(dict(file='engine_cruise.mp3', t=t, dur=d, gain=it['engine'], loop=True, fade=.15))
            if it.get('amb'): sfx.append(dict(file=it['amb'], t=t, dur=d, gain=it.get('amb_gain', -10), loop=True, fade=.3))
            timeline.append((t, d, it['shot'], i0))
        elif 'card' in it:
            c = E['cards'][it['card']]; p = tmp / f"card_{it['card']}.png"; card_png(p, W, H, c)
            n = inp('-loop', '1', '-framerate', '60', '-t', f'{d:.3f}', '-i', str(p)); ch = f"[{n}:v]format=yuv420p,setsar=1"
        else:
            n = inp('-f', 'lavfi', '-t', f'{d:.3f}', '-i', f'color=c=black:s={W}x{H}:r=60'); ch = f"[{n}:v]format=yuv420p,setsar=1"
        if it.get('fi'): ch += f",fade=t=in:st=0:d={it['fi']}"
        if it.get('fo'): ch += f",fade=t=out:st={d - it['fo']:.3f}:d={it['fo']}"
        vf.append(ch + f"[v{k}]"); vlabels.append(f"[v{k}]"); t += d
    total = t
    vf.append(''.join(vlabels) + f"concat=n={len(vlabels)}:v=1:a=0[vc]"); cur = 'vc'
    # ---- overlays: text, images, subtitles
    overlays = list(E.get('text', []))
    lines = {x['id']: x['text'] for x in json.loads((VOICE / 'lines.json').read_text(encoding='utf-8'))}
    glines = {x['id']: x['text'] for x in json.loads((GVOICE / 'lines.json').read_text(encoding='utf-8'))['lines']} if E.get('game_voice') else {}
    if E.get('subtitles'):
        subs = [(nr['t'], dur(VOICE / (nr['id'] + '.mp3')) + .25, nr.get('sub', lines[nr['id']])) for nr in E['narration']]
        subs += [(g['t'] + (.42 if g['kind'] == 'computer' else .08), (g['stop'] - g['t'] - .42 if g.get('stop') else dur(GVOICE / (g['id'] + '.mp3')) + .25), g.get('sub', glines[g['id']])) for g in E.get('game_voice', [])]
        for st, sd, sx in subs:
            if sx: overlays.append(dict(t=st, dur=sd, text=sx, size=E.get('sub_size', .034), pos='bottom', weight='SemiBold', box=True, fade=.12))
    for j, ov in enumerate(overlays):
        p = tmp / f'ov_{j}.png'
        if 'image' in ov: image_png(p, W, H, ov['image'], ov.get('w', .5), ov.get('pos', 'center'))
        else:
            txt = ov['text']
            if vertical and len(txt) > 26 and '\n' not in txt:   # wrap long lines for a phone
                words = txt.split(); half = len(txt) // 2; acc = ''; out_l = []
                for w_ in words:
                    if len(acc) >= half and not out_l: out_l.append(acc.strip()); acc = ''
                    acc += w_ + ' '
                txt = '\n'.join(out_l + [acc.strip()])
            text_png(p, W, H, txt, int(H * ov.get('size', .07)), ov.get('pos', 'lower'), ov.get('weight', 'Bold'), box=ov.get('box', False))
        d = ov['dur']; fd = ov.get('fade', .25)
        n = inp('-loop', '1', '-framerate', '60', '-t', f'{d:.3f}', '-i', str(p))
        vf.append(f"[{n}:v]format=rgba,fade=t=in:st=0:d={fd}:alpha=1,fade=t=out:st={d - fd:.3f}:d={fd}:alpha=1,setpts=PTS+{ov['t']:.3f}/TB[o{j}]")
        vf.append(f"[{cur}][o{j}]overlay=eof_action=pass:format=auto[w{j}]"); cur = f'w{j}'
    vf.append(f"[{cur}]format=yuv420p[vout]")
    # ---- audio: narration (radio, light), music (ducked), effects
    nl = []
    for j, nr in enumerate(E['narration']):
        n = inp('-i', str(VOICE / (nr['id'] + '.mp3'))); ms = int(nr['t'] * 1000); g = nr.get('gain', 0)
        af.append(f"[{n}:a]aformat=sample_rates=48000:channel_layouts=stereo,highpass=f=240,lowpass=f=5200,equalizer=f=1800:t=q:w=1.2:g=3,acompressor=threshold=0.1:ratio=3:attack=5:release=90,volume={g}dB,adelay={ms}|{ms}[n{j}]"); nl.append(f'[n{j}]')
        cr = inp('-stream_loop', '-1', '-t', f"{dur(VOICE / (nr['id'] + '.mp3')) + .2:.3f}", '-i', str(SND / 'feed_crackle.mp3'))
        af.append(f"[{cr}:a]aformat=sample_rates=48000:channel_layouts=stereo,highpass=f=1500,volume=-34dB,afade=t=in:d=0.05,adelay={max(0, ms - 100)}|{max(0, ms - 100)}[c{j}]"); nl.append(f'[c{j}]')
    # the game's own lines, treated as the game does (Audio/Voices.cs): the computer clean with its chime .42 s first (warnings);
    # Control on the radio (high-pass 350 Hz, low-pass 3.2 kHz, a little drive, hiss, a click in and out). 'stop' = cut there (Voice.StopAll)
    for j, g in enumerate(E.get('game_voice', [])):
        f = GVOICE / (g['id'] + '.mp3'); L = dur(f); comp = g['kind'] == 'computer'; v0 = g['t'] + (.42 if comp else .08)
        if g.get('stop'): L = min(L, g['stop'] - v0)
        n = inp('-i', str(f)); ms = int(v0 * 1000)
        chain = '' if comp else ',highpass=f=350,highpass=f=350,lowpass=f=3200,lowpass=f=3200,volume=8dB,asoftclip=type=tanh,volume=-8dB'
        af.append(f"[{n}:a]aformat=sample_rates=48000:channel_layouts=stereo{chain},atrim=0:{L:.3f},afade=t=out:st={max(0, L - .03):.3f}:d=0.03,volume={g.get('gain', 0)}dB,adelay={ms}|{ms}[g{j}]"); nl.append(f'[g{j}]')
        if comp:
            n = inp('-i', str(tmp / 'chime.wav')); ms0 = int(g['t'] * 1000)
            af.append(f"[{n}:a]aformat=sample_rates=48000:channel_layouts=stereo,volume={g.get('gain', 0) + g.get('chime_gain', -6)}dB,adelay={ms0}|{ms0}[gc{j}]"); nl.append(f'[gc{j}]')
        else:
            for k, ct in enumerate((g['t'], v0 + L + .03)):
                n = inp('-i', str(tmp / 'click.wav')); mc = int(ct * 1000)
                af.append(f"[{n}:a]aformat=sample_rates=48000:channel_layouts=stereo,volume={g.get('gain', 0) - 14}dB,adelay={mc}|{mc}[gk{j}_{k}]"); nl.append(f'[gk{j}_{k}]')
            n = inp('-f', 'lavfi', '-t', f'{L + .1:.3f}', '-i', 'anoisesrc=r=48000:color=white:amplitude=1:seed=991'); mh = int((g['t'] + .03) * 1000)
            af.append(f"[{n}:a]aformat=sample_rates=48000:channel_layouts=stereo,highpass=f=350,lowpass=f=3200,volume={g.get('gain', 0) - 40}dB,afade=t=in:d=0.02,afade=t=out:st={L:.3f}:d=0.05,adelay={mh}|{mh}[gh{j}]"); nl.append(f'[gh{j}]')
    af.append(''.join(nl) + f"amix=inputs={len(nl)}:normalize=0,apad=whole_dur={total:.3f},atrim=0:{total:.3f},asplit=2[narr][nsc]")
    ml = []
    # ffmpeg's amix here stops when the last-listed input ends: list the pieces by end time
    for j, m in enumerate(sorted(E['music'], key=lambda m: m['at'] + m['to'] - m['from'])):
        L = m['to'] - m['from']; n = inp('-ss', f"{m['from']:.3f}", '-t', f'{L:.3f}', '-i', str(ROOT / m['file'])); ms = int(m['at'] * 1000)
        af.append(f"[{n}:a]aformat=sample_rates=48000:channel_layouts=stereo,afade=t=in:d={m.get('fi', .05)},afade=t=out:st={L - m.get('fo', .3):.3f}:d={m.get('fo', .3)},volume={m.get('gain', 0)}dB,adelay={ms}|{ms}[m{j}]"); ml.append(f'[m{j}]')
    af.append(''.join(ml) + f"amix=inputs={len(ml)}:normalize=0,apad=whole_dur={total:.3f},atrim=0:{total:.3f}[mus]")
    af.append(f"[mus][nsc]sidechaincompress=threshold=0.02:ratio=8:attack=15:release=450:makeup=1[musd]")
    sl = []
    for j, s in enumerate(sorted(sfx + E.get('sfx', []), key=lambda s: s['t'] + (s['dur'] if s.get('dur') else dur(sound(s['file'])) if sound(s['file']).exists() else 0))):
        f = sound(s['file'])
        if not f.exists(): print('  missing sound', f); continue
        if s.get('loop'): n = inp('-stream_loop', '-1', '-t', f"{s['dur']:.3f}", '-i', str(f))
        else: n = inp('-i', str(f))
        ms = int(max(0, s['t']) * 1000); fd = s.get('fade', 0); extra = f",afade=t=in:d={fd},afade=t=out:st={s['dur'] - fd:.3f}:d={fd}" if fd and s.get('dur') else ''
        if s.get('cut'): extra += f",atrim=0:{s['cut']:.3f},afade=t=out:st={max(0, s['cut'] - .02):.3f}:d=0.02"   # stops dead (a hard cut)
        af.append(f"[{n}:a]aformat=sample_rates=48000:channel_layouts=stereo{extra},volume={s.get('gain', 0)}dB,adelay={ms}|{ms}[s{j}]"); sl.append(f'[s{j}]')
    af.append(''.join(sl) + f"amix=inputs={max(1, len(sl))}:normalize=0,apad=whole_dur={total:.3f},atrim=0:{total:.3f}[fx]" if sl else f"anullsrc=r=48000:cl=stereo,atrim=0:{total:.3f}[fx]")
    af.append(f"[narr][musd][fx]amix=inputs=3:normalize=0,alimiter=limit=0.5:attack=3:release=60:level=false[mix]")   # peaks held well under 0 dBFS before the loudness pass
    graph = ';'.join(vf + af)
    gfile = tmp / 'graph.txt'; gfile.write_text(graph, encoding='utf-8')
    enc = ['-c:v', 'libx264', '-preset', 'veryfast' if o.rough else 'slow', '-crf', '23' if o.rough else '17', '-pix_fmt', 'yuv420p', '-r', '60', '-c:a', 'aac', '-b:a', '192k', '-movflags', '+faststart']
    pre = tmp / 'mix.wav'
    base = ['ffmpeg', '-v', 'error', '-y', *ins, '-/filter_complex', str(gfile)]
    print(f'{name}: {total:.2f} s, {W}x{H}, {len(E["video"])} pieces, {len(sfx)} sounds from the capture, {len(ins) // 2} inputs')
    # pass 1: picture + the raw mix; pass 2: loudness measured and set (-14 LUFS, true peak -1.5)
    r = subprocess.run(base + ['-map', '[vout]', *enc[:-6], str(tmp / 'video.mp4'), '-map', '[mix]', '-c:a', 'pcm_s24le', str(pre)], capture_output=True, text=True)
    if r.returncode: print(r.stderr[-3000:]); sys.exit('render failed')
    m = subprocess.run(['ffmpeg', '-hide_banner', '-i', str(pre), '-af', 'loudnorm=I=-14:TP=-1.5:LRA=11:print_format=json', '-f', 'null', '-'], capture_output=True, text=True).stderr
    j = json.loads(m[m.rindex('{'):m.rindex('}') + 1])
    ln = f"loudnorm=I=-14:TP=-1.5:LRA=11:measured_I={j['input_i']}:measured_TP={j['input_tp']}:measured_LRA={j['input_lra']}:measured_thresh={j['input_thresh']}:offset={j['target_offset']}:linear=true"
    r = subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(tmp / 'video.mp4'), '-i', str(pre), '-map', '0:v', '-map', '1:a', '-c:v', 'copy', '-af', ln + ',aresample=192000,alimiter=limit=0.7:attack=1:release=50:level=false,aresample=48000', '-c:a', 'aac', '-b:a', '192k', '-movflags', '+faststart', str(out)], capture_output=True, text=True)
    if r.returncode: print(r.stderr[-2000:]); sys.exit('mux failed')
    v = subprocess.run(['ffmpeg', '-hide_banner', '-i', str(out), '-af', 'loudnorm=I=-14:TP=-1.5:print_format=json', '-f', 'null', '-'], capture_output=True, text=True).stderr
    jj = json.loads(v[v.rindex('{'):v.rindex('}') + 1])
    print(f'  -> {out}  ({dur(out):.2f} s; loudness {jj["input_i"]} LUFS, true peak {jj["input_tp"]} dBTP; mix before {j["input_i"]} LUFS)')
    (tmp / 'timeline.txt').write_text('\n'.join(f'{a:7.2f} +{b:5.2f}  {c} @ {d:.2f}' for a, b, c, d in timeline), encoding='utf-8')
    if o.sheet:
        ims = []
        for a_, b_, c_, d_ in timeline:
            p = tmp / f'sheet_{len(ims)}.png'; subprocess.run(['ffmpeg', '-v', 'error', '-y', '-ss', f'{a_ + b_ / 2:.3f}', '-i', str(out), '-frames:v', '1', '-vf', 'scale=480:-2', str(p)]); ims.append(p)
        tiles = [Image.open(p) for p in ims]; cols = 6 if not vertical else 8; tw, th = tiles[0].size; rows = (len(tiles) + cols - 1) // cols
        sh = Image.new('RGB', (cols * tw, rows * th)); [sh.paste(im, ((i % cols) * tw, (i // cols) * th)) for i, im in enumerate(tiles)]
        sh.save(out.with_suffix('.sheet.jpg'), quality=85); [os.remove(p) for p in ims]


if __name__ == '__main__': main()
