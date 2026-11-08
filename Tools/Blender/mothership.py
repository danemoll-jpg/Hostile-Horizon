"""Hostile Horizon, Round 2: the mothership. Round 8 (B1): Dan's art via Meshy (hull, bay wall modules, cradle, regeneration pod);
the bay's liners, light strips, floor strips and the numbers below are Round 2's (Mothership.cs relies on them).

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/mothership.py

Our own design: a long, blunt carrier hull, about 190 m long and 60 m wide, with two big engines at the stern. Between the
engines the stern is open into the rescue bay, so a docked ship looks out over the planet. The bay's floor is open in the
middle: the rescue ship rises into it from below and is held from the ceiling by a cradle; two floor doors (made by the game,
Assets/Scripts/Campaign/Mothership.cs) close under it. The regeneration pod stands in the bay's front corner.

Mothership space = Unity space (x right, y up, z forward; metres); the origin is the dock point: where the rescue ship's
origin sits when docked, the ship facing -z (towards the open stern). Mothership.cs mirrors these numbers (change both):
  bay inside x -14..14, y -6..9, z -26..14; floor opening x -9..9, z -10..10 (doors at y -7..-6);
  regeneration pod at (11.5, -6, 11.5), the waking eye at (11.5, -4.3, 11.2).

Slots: hull, panel, bay, metal, engine, glow, window, glass.
Output: SourceArt/Blender/Mothership.blend, Assets/Resources/Models/Mothership.fbx
"""
import bpy, math, sys, os
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import box, hull, cyl, loft

kit.COLORS.update({'hull': (.50, .52, .53), 'panel': (.33, .35, .37), 'bay': (.22, .23, .24), 'engine': (.14, .15, .16),
                   'glow': (1.0, .6, .25), 'window': (.6, .85, 1.0)})

for ob in list(bpy.data.objects): bpy.data.objects.remove(ob)
G = 'Body'


def section(z, hw, y0, y1):
    return [(-hw, y0, z), (hw, y0, z), (hw, y1, z), (-hw, y1, z)]


import math, meshykit
from mathutils import Matrix

# ---- Round 8: the Meshy hull, half a turn (its nose to +z), its stern at the old engines' end (z -42), its keel at y -18
hullob = meshykit.load('mothership', Matrix.Translation((0, 0, 0)) @ Matrix.Rotation(math.pi, 4, 'Z'), G)
V = [meshykit.unity(v.co) for v in hullob.data.vertices]
dz = -42 - min(v[2] for v in V); dy = -18 - min(v[1] for v in V)
hullob.data.transform(Matrix.Translation(kit.B(0, dy, dz)))
print(f'MOTHERSHIP hull z {-42:.0f}..{max(v[2] for v in V) + dz:.0f}, y -18..{max(v[1] for v in V) + dy:.0f}, x +-{max(abs(v[0]) for v in V):.0f}')
# cut out the bay (x -14.5..14.5, y -6.5..9.5, z back to the stern) and the floor opening below it (x -9..9, z -10..10)
cut = [p.index for p in hullob.data.polygons if (lambda c: (abs(c[0]) < 14.5 and -6.5 < c[1] < 9.5 and c[2] < 14.5)
       or (abs(c[0]) < 9.5 and abs(c[2]) < 10.5 and c[1] < -5))(meshykit.unity(p.center))]
print(f'MOTHERSHIP bay cut: {len(cut)} faces')
meshykit.split(hullob, cut, G, False)

# ---- around the bay (Round 2): floor strips round the opening, the liners, light strips
for s in (-1, 1):
    box('floor side', (s * 11.5, -9, 0), (5, 6, 20), G, 'panel', .05)
box('floor front', (0, -9, 12), (28, 6, 4), G, 'panel', .05)
box('floor rear', (0, -9, -18), (28, 6, 16), G, 'panel', .05)
for s in (-1, 1):
    box('bay wall', (s * 14.0, 1.5, -6), (.2, 15, 40), G, 'bay', .0)
    box('ceiling light', (s * 9, 8.8, -6), (.5, .15, 34), G, 'glow', .0)
    box('opening light', (s * 9.2, -5.95, 0), (.3, .1, 20), G, 'glow', .0)
box('bay ceiling', (0, 9.0, -6), (28, .2, 40), G, 'bay', .0)
box('bay front wall', (0, 1.5, 14.0), (28, 15, .2), G, 'bay', .0)
# ---- Round 8: Meshy wall modules along the bay's sides (their backs on the liners; the right side stops short of the pod)
for s in (-1, 1):
    for k in range(4 if s < 0 else 3):
        zc = -21 + k * 10
        meshykit.load('bay_wall', Matrix.Translation(kit.B(s * 14.0, -6, zc)) @ Matrix.Rotation(-s * math.pi / 2, 4, 'Z') @ Matrix.Translation((0, -1.285, 0)), G)
# ---- the cradle that holds the ship, hanging from the ceiling over the dock point (1.2 x: its pads just over the fins)
cr = meshykit.load('bay_cradle', Matrix.Translation(kit.B(0, 9.0 - 5.1 * 1.2, 1.5)) @ Matrix.Scale(1.2, 4), G)
# ---- the regeneration pod in the bay's front corner, its window turned toward the bay (the waking view, Mothership.PodLook)
meshykit.load('regen_pod', Matrix.Translation(kit.B(11.5, -6, 11.5)) @ Matrix.Rotation(math.radians(-26), 4, 'Z'), G)
cyl('pod light', (11.5, -3.75, 11.5), (11.5, -3.7, 11.5), .45, G, 'window', 20)
box('pod console', (9.4, -5.2, 13.0), (1.0, 1.6, .5), G, 'panel', .03)

for ob in kit.join_all(): pass
kit.export('Mothership')
