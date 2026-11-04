"""Fix 3.1 stage 5: copy the rigged Mixamo models, their animations and Meshy's textures into the Unity project.

python Tools/import_characters.py

From SourceArt/Mixamo/<name>/ (Dan's Mixamo downloads, Docs/MODELS-NEEDED.md) and SourceArt/Meshy/<name>/ (Meshy's textures)
to Assets/Resources/Characters/:
  <name>.fbx                    the rigged model (Mixamo "idle", With Skin; its idle clip comes with it)
  <name>_albedo.png             base colour
  <name>_normal.png             normal map
  <name>_mask.png               URP Lit metallic / smoothness map (R metallic, A smoothness = 1 - roughness)
  pilot_*_hollow.png            the Hollow's disguise: the visor made a matte, dead black (its tell); the real pilot's visor
                                is made a strong mirror (metallic .6, smoothness .97) in pilot_mask.png
  Clips/<set>_<clip>.fbx        animations (Without Skin); set = pilot, creature (shared by all five) or a type's own extras
  Journal/<type>.png            Field Journal pictures (the three-quarter renders from Tools/Blender/meshy_cleanup.py)
Assets/Editor/CharacterImport.cs sets the import (Humanoid, loops, LODs, texture types).
"""
import shutil
from pathlib import Path
from PIL import Image, ImageFilter, ImageChops

ROOT = Path(__file__).resolve().parent.parent
MIX, MESHY = ROOT / "SourceArt" / "Mixamo", ROOT / "SourceArt" / "Meshy"
OUT = ROOT / "Assets" / "Resources" / "Characters"
NAMES = ["pilot", "mimic", "hollow", "knocker", "stalker", "sleeper"]


def textures(n):
    d = MESHY / n
    shutil.copy2(d / "base_color.png", OUT / f"{n}_albedo.png")
    shutil.copy2(d / "normal.png", OUT / f"{n}_normal.png")
    metal = Image.open(d / "metallic.png").convert("L")
    smooth = Image.eval(Image.open(d / "roughness.png").convert("L"), lambda v: 255 - v)
    if n != "pilot":
        Image.merge("RGBA", (metal, metal, metal, smooth)).save(OUT / f"{n}_mask.png"); return
    # the visor: the near-black, very glossy patches of the helmet (nothing else in the suit is both)
    albedo = Image.open(d / "base_color.png").convert("RGB")
    dark = albedo.convert("L").point(lambda v: 255 if v < 22 else 0)
    gloss = smooth.point(lambda v: 255 if v > 200 else 0)
    visor = ImageChops.multiply(dark, gloss).filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.MinFilter(3))
    print(f"pilot visor: {sum(1 for v in visor.getdata() if v) / (visor.width * visor.height) * 100:.2f} % of the texture")
    real_m, real_s = metal.copy(), smooth.copy()
    real_m.paste(153, mask=visor); real_s.paste(247, mask=visor)
    Image.merge("RGBA", (real_m, real_m, real_m, real_s)).save(OUT / "pilot_mask.png")
    dead_m, dead_s = metal.copy(), smooth.copy()
    dead_m.paste(0, mask=visor); dead_s.paste(0, mask=visor)
    Image.merge("RGBA", (dead_m, dead_m, dead_m, dead_s)).save(OUT / "pilot_mask_hollow.png")
    dead_a = albedo.copy(); dead_a.paste((0, 0, 0), mask=visor); dead_a.save(OUT / "pilot_albedo_hollow.png")
    visor.save(OUT.parent.parent.parent / "SourceArt" / "Meshy" / "pilot" / "visor_mask.png")


def main():
    (OUT / "Clips").mkdir(parents=True, exist_ok=True); (OUT / "Journal").mkdir(exist_ok=True)
    for n in NAMES:
        shutil.copy2(MIX / n / "idle.fbx", OUT / f"{n}.fbx")
        textures(n)
        for f in sorted((MIX / n).glob("*.fbx")):
            if f.stem != "idle":
                shutil.copy2(f, OUT / "Clips" / f"{n}_{f.stem}.fbx")
        if n != "pilot":
            shutil.copy2(MESHY / n / "render_34.png", OUT / "Journal" / f"{n}.png")
    for f in sorted((MIX / "creature").glob("*.fbx")):
        shutil.copy2(f, OUT / "Clips" / f"creature_{f.stem}.fbx")
    print("copied:", len(list(OUT.rglob("*.fbx"))), "fbx,", len(list(OUT.rglob("*.png"))), "png")


if __name__ == "__main__":
    main()
