"""Hostile Horizon Round 1: the launcher's cover art (SourceArt/Title/HostileHorizonCover.png) from a menu-free title screenshot of the
game (the exe check writes 01_title_no_menu.png): a 3:4 crop around the canyon with the title on top.
Usage: python Tools/make_cover.py <screenshot.png>"""
import pathlib, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[1]
src = Image.open(sys.argv[1]).convert('RGB')
w, h = src.size; cw = int(h * .75); x0 = max(0, min(w - cw, w // 2 - cw // 2))
img = src.crop((x0, 0, x0 + cw, h)).resize((600, 800), Image.LANCZOS)
d = ImageDraw.Draw(img)
try: font = ImageFont.truetype('arialbd.ttf', 54)
except OSError: font = ImageFont.load_default()
for line, y in (('HOSTILE', 40), ('HORIZON', 102)):
    tw = d.textlength(line, font=font)
    d.text(((600 - tw) / 2 + 3, y + 3), line, font=font, fill=(0, 0, 0))
    d.text(((600 - tw) / 2, y), line, font=font, fill=(255, 238, 222))
out = ROOT / 'SourceArt/Title/HostileHorizonCover.png'; out.parent.mkdir(parents=True, exist_ok=True); img.save(out)
print('cover:', out)
