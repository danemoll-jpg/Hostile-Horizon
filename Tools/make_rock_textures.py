"""Round 11 (B1): the CC0 photo-scanned PBR sets for the terrain, from ambientCG (https://ambientcg.com, all CC0 1.0).

Downloads each set's 2K JPG zip into SourceArt/CC0/<id>/ (git-ignored) once, then writes the game's textures:
  Assets/Resources/Textures/Terrain/<slot>_albedo.jpg  colour with the set's ambient occlusion multiplied in (60 %)
  Assets/Resources/Textures/Terrain/<slot>_normal.jpg  the OpenGL normal map (+Y up, Unity's convention)
Import settings (normal map, sRGB, max size, mip-maps, compression): Assets/Editor/TerrainTextureImport.cs.
Credits: the sets are listed in GameShell's credits page (People / Made with / Licences).

python Tools/make_rock_textures.py [--force]
"""
import argparse, io, pathlib, sys, urllib.request, zipfile
from PIL import Image, ImageChops

ROOT = pathlib.Path(__file__).resolve().parent.parent
SETS = [  # slot, ambientCG id, size in the game
    ('rock', 'Rock029', 2048),       # red desert cliff: the canyon walls
    ('ground', 'Ground079S', 2048),  # dusty gravel and sand: the flats
    ('scree', 'Rocks011', 1024),     # broken slabs: rubble at the foot of the walls, steep slopes
]
OUT = ROOT / 'Assets/Resources/Textures/Terrain'

a = argparse.ArgumentParser(); a.add_argument('--force', action='store_true'); o = a.parse_args()
OUT.mkdir(parents=True, exist_ok=True)
for slot, aid, size in SETS:
    src = ROOT / 'SourceArt/CC0' / aid; src.mkdir(parents=True, exist_ok=True)
    z = src / f'{aid}_2K-JPG.zip'
    if not z.exists():
        url = f'https://ambientcg.com/get?file={aid}_2K-JPG.zip'
        print('download', url)
        req = urllib.request.Request(url, headers={'User-Agent': 'HostileHorizon-texture-tool'})
        z.write_bytes(urllib.request.urlopen(req, timeout=120).read())
    alb, nrm = OUT / f'{slot}_albedo.jpg', OUT / f'{slot}_normal.jpg'
    if alb.exists() and nrm.exists() and not o.force: print('have', slot); continue
    with zipfile.ZipFile(z) as zf:
        def img(suffix): return Image.open(io.BytesIO(zf.read(f'{aid}_2K-JPG_{suffix}.jpg')))
        col = img('Color').convert('RGB'); ao = img('AmbientOcclusion').convert('L'); n = img('NormalGL').convert('RGB')
    ao = ao.point(lambda v: int(255 - (255 - v) * .6))
    col = ImageChops.multiply(col, Image.merge('RGB', (ao, ao, ao)))
    if col.size[0] != size: col = col.resize((size, size), Image.LANCZOS); n = n.resize((size, size), Image.LANCZOS)
    col.save(alb, quality=92); n.save(nrm, quality=95)
    print(slot, aid, alb.stat().st_size // 1024, 'KB +', nrm.stat().st_size // 1024, 'KB')
(ROOT / 'SourceArt/CC0/README.txt').write_text('ambientCG sets (CC0 1.0, https://ambientcg.com): ' + ', '.join(f'{aid} ({slot})' for slot, aid, _ in SETS) + '\n', encoding='utf-8')
