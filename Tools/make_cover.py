"""Hostile Horizon: the launcher's cover art (SourceArt/Title/HostileHorizonCover.png, Round 1) and the Steam library poster
(SourceArt/Title/HostileHorizonPoster.png, 600 x 900, Round 2: the starter package puts it beside the launcher) from a menu-free
title screenshot of the game (the exe check writes 01_title_no_menu.png): a crop around the canyon with the title on top.
Usage: python Tools/make_cover.py <screenshot.png>"""
import pathlib, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[1]
src = Image.open(sys.argv[1]).convert('RGB')


def make(name, size, title_size, top):
    w, h = src.size; cw = int(h * size[0] / size[1]); x0 = max(0, min(w - cw, w // 2 - cw // 2))
    img = src.crop((x0, 0, x0 + cw, h)).resize(size, Image.LANCZOS)
    d = ImageDraw.Draw(img)
    try: font = ImageFont.truetype('arialbd.ttf', title_size)
    except OSError: font = ImageFont.load_default()
    for i, line in enumerate(('HOSTILE', 'HORIZON')):
        y = top + i * int(title_size * 1.15); tw = d.textlength(line, font=font)
        d.text(((size[0] - tw) / 2 + 3, y + 3), line, font=font, fill=(0, 0, 0))
        d.text(((size[0] - tw) / 2, y), line, font=font, fill=(255, 238, 222))
    out = ROOT / 'SourceArt/Title' / name; out.parent.mkdir(parents=True, exist_ok=True); img.save(out)
    print(name, out)


make('HostileHorizonCover.png', (600, 800), 54, 40)
make('HostileHorizonPoster.png', (600, 900), 62, 60)
