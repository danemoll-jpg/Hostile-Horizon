"""Round 8 (B1): the hard-surface Meshy models' textures into the game.

python Tools/import_hard.py [--only ship,turret]

For each model in SourceArt/Meshy/<name>/ (Tools/meshy_generate.py): Assets/Resources/Hard/<name>_albedo.png, _normal.png and
_mask.png (URP Lit metallic / smoothness: R metallic, A smoothness = 1 - roughness), at most 2048 px. The materials are made
by Assets/Editor/HardImport.cs at build time. The meshes go into the game through the Blender scripts (ship.py, ...).
"""
import argparse
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets" / "Resources" / "Hard"
NAMES = ["ship", "cockpit_seat", "cockpit_dash", "cockpit_console", "mothership", "bay_wall", "bay_cradle", "regen_pod", "turret", "saucer"]
MAX = 2048


def fit(im):
    if max(im.size) > MAX:
        im = im.resize((MAX, MAX * im.size[1] // im.size[0]), Image.LANCZOS)
    return im


def main():
    ap = argparse.ArgumentParser(); ap.add_argument("--only", default=""); a = ap.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    for n in NAMES:
        if a.only and n not in a.only.split(","): continue
        d = ROOT / "SourceArt" / "Meshy" / n
        if not (d / "base_color.png").exists(): print(f"{n}: no textures"); continue
        fit(Image.open(d / "base_color.png").convert("RGB")).save(OUT / f"{n}_albedo.png")
        fit(Image.open(d / "normal.png").convert("RGB")).save(OUT / f"{n}_normal.png")
        metal = fit(Image.open(d / "metallic.png").convert("L"))
        smooth = fit(Image.eval(Image.open(d / "roughness.png").convert("L"), lambda v: 255 - v))
        Image.merge("RGBA", (metal, metal, metal, smooth)).save(OUT / f"{n}_mask.png")
        print(f"{n}: albedo / normal / mask {metal.size[0]} px")


if __name__ == "__main__":
    main()
