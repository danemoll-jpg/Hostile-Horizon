"""Hostile Horizon, Fix 8.3 (Round 9 A, item 2b): the Steam library artwork from Dan's images in SourceArt/Title/SteamDeck/.
Writes SourceArt/Title/Steam/:
  HostileHorizon-Portrait.png  600 x 900   library capsule / grid (Dan's portrait export, as is)
  HostileHorizon-Cover.png     600 x 900   the launcher's cover (the same portrait)
  HostileHorizon-Wide.png      920 x 430   wide capsule / header (Dan's landscape export, as is)
  HostileHorizon-Hero.png      1920 x 620  the library's big banner: the landscape master with the title painted out of the sky
                                           (no crop drops the title without cutting the ship), so Steam's logo can sit on it
  HostileHorizon-Logo.png      transparent: Dan's logo with its dark-teal background un-mixed into alpha (clean anti-aliased edges)
  HostileHorizon-Icon.png      the game icon (SourceArt/Title/GameIcon.png)
  logo_over_hero.png           a check picture: the logo laid over the hero as Steam shows it (not shipped)
The build copies the HostileHorizon-*.png files into the game folder (SteamArt/); the game copies them up beside the launcher.
Usage: python Tools/make_steam_art.py
"""
import pathlib, shutil
import numpy as np, cv2
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[1]
SRC = ROOT / 'SourceArt/Title/SteamDeck'; OUT = ROOT / 'SourceArt/Title/Steam'; OUT.mkdir(parents=True, exist_ok=True)

portrait = Image.open(SRC / 'hostile-horizon-portrait-600x900.png').convert('RGB'); assert portrait.size == (600, 900)
portrait.save(OUT / 'HostileHorizon-Portrait.png'); portrait.save(OUT / 'HostileHorizon-Cover.png')
wide = Image.open(SRC / 'hostile-horizon-landscape-920x430.png').convert('RGB'); assert wide.size == (920, 430)
wide.save(OUT / 'HostileHorizon-Wide.png')
shutil.copy2(ROOT / 'SourceArt/Title/GameIcon.png', OUT / 'HostileHorizon-Icon.png')

# ---- hero: paint the title out of the sky, then a 1920 x 620 crop (ship whole, pilot and creature mostly in)
m = cv2.imread(str(SRC / 'hostile-horizon-landscape-master.png'))   # BGR, 1833 x 858
h, w = m.shape[:2]
ivory = np.array([201, 234, 253], np.float32)                                   # the title's flat paint (BGR)
dist = np.linalg.norm(m.astype(np.float32) - ivory, axis=2)
letters = (dist < 60).astype(np.uint8) * 255                                     # the wing's grey-white paint is further off
region = np.zeros_like(letters); region[40:300, 40:716] = 255                    # the title's box over the sky
title = cv2.bitwise_and(letters, region)
# the letters' soft anti-aliased rims: grow into pixels between the paint and the sky, never onto the wing (x >= 704 below y 244)
mask = cv2.dilate(title, np.ones((9, 9), np.uint8), iterations=2)
wing = np.zeros_like(mask); wing[244:, 704:] = 1
mask[(wing == 1) & (title == 0)] = 0

clean = cv2.inpaint(m, mask, 21, cv2.INPAINT_TELEA)
# soften the painted area a little more so no letter shapes show through the smear
blur = cv2.GaussianBlur(clean, (0, 0), 9); soft = cv2.GaussianBlur(mask, (0, 0), 6).astype(np.float32)[..., None] / 255
clean = (clean * (1 - soft) + blur * soft).astype(np.uint8)
ch = round(w * 620 / 1920); y0 = 64                                             # 592 tall: y 64 .. 656
hero = cv2.resize(clean[y0:y0 + ch, :], (1920, 620), interpolation=cv2.INTER_CUBIC)
cv2.imwrite(str(OUT / 'HostileHorizon-Hero.png'), hero)
print('hero: title mask', int((mask > 0).sum()), 'px; crop y', y0, '..', y0 + ch)

# ---- logo: un-mix the flat teal background (alpha = how far each pixel is from the background toward the ivory letters)
lg = np.asarray(Image.open(SRC / 'hostile-horizon-logo.png').convert('RGB')).astype(np.float32)
bg = np.median(np.concatenate([lg[:20].reshape(-1, 3), lg[-20:].reshape(-1, 3)]), axis=0)
fg = np.median(lg[lg.sum(axis=2) > 600], axis=0)
d = fg - bg; t = np.clip(((lg - bg) @ d) / (d @ d), 0, 1)
t = np.clip((t - .04) / .92, 0, 1)                                              # background noise -> 0, letter body -> 1
rgb = np.where(t[..., None] > .02, np.clip((lg - (1 - t[..., None]) * bg) / np.maximum(t[..., None], .02), 0, 255), fg)
rgba = np.dstack([rgb, t * 255]).astype(np.uint8)
ys, xs = np.nonzero(t > .02); pad = 24
rgba = rgba[max(0, ys.min() - pad):ys.max() + pad, max(0, xs.min() - pad):xs.max() + pad]
logo = Image.fromarray(rgba, 'RGBA'); logo.save(OUT / 'HostileHorizon-Logo.png')
print('logo', logo.size, 'background', bg.round(), 'letters', fg.round())

# ---- check picture: the logo over the hero (Steam puts it bottom-left by default)
h2 = Image.open(OUT / 'HostileHorizon-Hero.png').convert('RGBA'); L = logo.copy(); L.thumbnail((640, 320), Image.LANCZOS)
h2.alpha_composite(L, (60, 620 - L.size[1] - 50)); h2.convert('RGB').save(OUT / 'logo_over_hero.png')
for f in sorted(OUT.glob('*.png')): print(f.name, Image.open(f).size, Image.open(f).mode)
