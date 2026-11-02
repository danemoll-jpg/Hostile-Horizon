"""Hostile Horizon, Round 1: draws the placeholder game icon (our own art): an alien sky over layered canyon walls, the sun low on the
horizon and the gas giant, with a small ship flying into the canyon.
Writes Assets/HandMade/Title/GameIcon.png (the exe icon), SourceArt/Title/GameIcon.png and Launcher/HostileHorizon.ico (the launcher).
Usage: python Tools/make_icon.py"""
import pathlib
from PIL import Image, ImageDraw, ImageFilter

ROOT = pathlib.Path(__file__).resolve().parents[1]
N = 512
img = Image.new('RGB', (N, N))
d = ImageDraw.Draw(img)
top, hor = (18, 48, 66), (214, 146, 112)
for y in range(N):
    t = min(1, y / (N * .62)) ** 1.6
    d.line([(0, y), (N, y)], fill=tuple(int(top[i] + (hor[i] - top[i]) * t) for i in range(3)))
# gas giant with bands, sun
d.ellipse([60, 60, 240, 240], fill=(150, 120, 108))
for i, y in enumerate(range(70, 236, 18)):
    d.line([(64, y), (236, y + 10)], fill=(118, 86, 84) if i % 2 else (184, 160, 136), width=7)
mask = Image.new('L', (N, N), 0); ImageDraw.Draw(mask).ellipse([60, 60, 240, 240], fill=255)
planet = img.copy(); img = Image.composite(planet, Image.new('RGB', (N, N)), mask)
sky = Image.new('RGB', (N, N)); sd = ImageDraw.Draw(sky)
for y in range(N):
    t = min(1, y / (N * .62)) ** 1.6
    sd.line([(0, y), (N, y)], fill=tuple(int(top[i] + (hor[i] - top[i]) * t) for i in range(3)))
img = Image.composite(img, sky, mask)
d = ImageDraw.Draw(img)
glow = Image.new('RGB', (N, N)); ImageDraw.Draw(glow).ellipse([330, 250, 430, 350], fill=(255, 220, 170))
img = Image.blend(img, Image.composite(glow.filter(ImageFilter.GaussianBlur(30)), img, Image.new('L', (N, N), 255)), .5)
d = ImageDraw.Draw(img); d.ellipse([360, 280, 400, 320], fill=(255, 246, 225))
# canyon walls: layers of rust and ochre, a V opening toward the horizon
layers = [(150, 74, 48), (186, 126, 82), (120, 62, 46), (204, 160, 120), (98, 50, 40), (160, 96, 66)]
for i, col in enumerate(layers):
    y0 = 300 + i * 36
    d.polygon([(0, y0), (205 - i * 14, y0 + 30), (215 - i * 14, N), (0, N)], fill=col)
    d.polygon([(N, y0 - 10), (300 + i * 14, y0 + 26), (290 + i * 14, N), (N, N)], fill=tuple(int(c * .8) for c in col))
d.polygon([(205, 330), (300, 326), (290, N), (215, N)], fill=(80, 46, 44))
# the ship: a small dark delta with two engine glows
sx, sy = 256, 400
d.polygon([(sx, sy - 22), (sx - 62, sy + 14), (sx - 12, sy + 8), (sx, sy + 16), (sx + 12, sy + 8), (sx + 62, sy + 14)], fill=(34, 36, 40))
for ex in (-20, 20): d.ellipse([sx + ex - 6, sy + 6, sx + ex + 6, sy + 16], fill=(255, 150, 70))
out = ROOT / 'Assets/HandMade/Title'; out.mkdir(parents=True, exist_ok=True); img.save(out / 'GameIcon.png')
(ROOT / 'SourceArt/Title').mkdir(parents=True, exist_ok=True); img.save(ROOT / 'SourceArt/Title/GameIcon.png')
img.save(ROOT / 'Launcher/HostileHorizon.ico', sizes=[(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)])
print('icon written')
