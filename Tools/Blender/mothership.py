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
# cut out the bay (x -14.5..14.5, y -6.5..9.5, z back to the stern) and the floor opening below it (x -9.5..9.5, z -10.5..10.5).
# Fix 8.3 (Round 9 A1): 0.8.x deleted whole faces by their centres, so both openings had saw-tooth edges. Now the hull is first
# sliced exactly along the openings' planes (bisect: every face lies wholly on one side), then the faces inside are deleted: the
# cut edges are straight lines on the planes. Each opening then gets a lip (hull plating turning inward), trim and edge lights.
import bmesh
from mathutils import Vector
BAY = (14.5, -6.5, 9.5, 14.5); FLOOR = (9.5, 10.5, -5.0)
def in_bay(c): return abs(c[0]) < BAY[0] and BAY[1] < c[1] < BAY[2] and c[2] < BAY[3]
def in_floor(c): return abs(c[0]) < FLOOR[0] and abs(c[2]) < FLOOR[1] and c[1] < FLOOR[2]
bm = bmesh.new(); bm.from_mesh(hullob.data)
for co, no in (((BAY[0], 0, 0), (1, 0, 0)), ((-BAY[0], 0, 0), (1, 0, 0)), ((0, BAY[1], 0), (0, 1, 0)), ((0, BAY[2], 0), (0, 1, 0)),
               ((0, 0, BAY[3]), (0, 0, 1)), ((FLOOR[0], 0, 0), (1, 0, 0)), ((-FLOOR[0], 0, 0), (1, 0, 0)),
               ((0, 0, FLOOR[1]), (0, 0, 1)), ((0, 0, -FLOOR[1]), (0, 0, 1)), ((0, FLOOR[2], 0), (0, 1, 0))):
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=kit.B(*co), plane_no=kit.B(*no), dist=1e-4)
dead = [f for f in bm.faces if (lambda c: in_bay(c) or in_floor(c))(meshykit.unity(f.calc_center_median()))]
bmesh.ops.delete(bm, geom=dead, context='FACES')
edge = [meshykit.unity(v.co) for v in bm.verts if v.is_boundary]
bm.to_mesh(hullob.data); bm.free(); hullob.data.update()
print(f'MOTHERSHIP bay cut: {len(dead)} faces, {len(edge)} edge vertices')
# where the cut edges lie: the stern opening's rim (on the planes x = +-14.5, y = -6.5 / 9.5) and the floor shaft's (x = +-9.5, z = +-10.5)
stern = [v for v in edge if v[2] < BAY[3] - .05 and (abs(abs(v[0]) - BAY[0]) < .02 or abs(v[1] - BAY[1]) < .02 or abs(v[1] - BAY[2]) < .02)]
shaft = [v for v in edge if v[1] < FLOOR[2] - .05 and (abs(abs(v[0]) - FLOOR[0]) < .02 or abs(abs(v[2]) - FLOOR[1]) < .02)]
sz0 = min(v[2] for v in stern); sz1 = max(v[2] for v in stern); fy0 = min(v[1] for v in shaft); fy1 = max(v[1] for v in shaft)
import collections; print('MOTHERSHIP rim z histogram', sorted(collections.Counter(int(v[2] // 5) * 5 for v in stern).items()))
print(f'MOTHERSHIP stern rim z {sz0:.1f}..{sz1:.1f} ({len(stern)} v); floor shaft y {fy0:.1f}..{fy1:.1f} ({len(shaft)} v)')

# ---- the stern opening's lip: a frame of plating from the hull's outer skin into the bay (covers the cut rim), trim, edge lights
L0 = sz0 - .4; L1 = -25.5; LL = L1 - L0; LC = (L0 + L1) / 2; T = 1.3   # from the stern skin to the bay liners (z -26): one built tunnel
for s in (-1, 1):
    box('stern lip side', (s * (BAY[0] + T / 2 - .5), (BAY[1] + BAY[2]) / 2, LC), (T, BAY[2] - BAY[1] + 2 * T - 1.0, LL), G, 'panel', .08)
    box('stern trim side', (s * (BAY[0] - .55), (BAY[1] + BAY[2]) / 2, L0 + .3), (.5, BAY[2] - BAY[1] - 1.0, .6), G, 'metal', .06)
    for k in range(5):   # lights down each side of the opening
        box('stern edge light', (s * (BAY[0] - .82), BAY[1] + 1.4 + k * (BAY[2] - BAY[1] - 2.8) / 4, L0 + .2), (.06, .5, .25), G, 'glow', 0)
for y, sy in ((BAY[1] - T / 2 + .5, -1), (BAY[2] + T / 2 - .5, 1)):
    box('stern lip ' + ('top' if sy > 0 else 'bottom'), (0, y, LC), (2 * BAY[0] + 2 * T - 1.0, T, LL), G, 'panel', .08)
    box('stern trim ' + ('top' if sy > 0 else 'bottom'), (0, y - sy * (T / 2 + .05), L0 + .3), (2 * BAY[0] - 1.0, .5, .6), G, 'metal', .06)
    box('stern light bar', (0, y - sy * (T / 2 + .32), L0 + .2), (2 * BAY[0] - 4, .06, .25), G, 'glow', 0)

# ---- the floor shaft's lip: a lining from the hull's belly up to the doors, a trim ring round the bottom, lights round it
S0 = fy0 - .4; S1 = -7.0; SH = S1 - S0; SC = (S0 + S1) / 2   # up to just under the floor doors (y -6.9 .. -6.1)
for s in (-1, 1):
    box('shaft lip side', (s * (FLOOR[0] + T / 2 - .5), SC, 0), (T, SH, 2 * FLOOR[1] + 2 * T - 1.0), G, 'panel', .08)
    box('shaft lip end', (0, SC, s * (FLOOR[1] + T / 2 - .5)), (2 * FLOOR[0] - 1.0, SH, T), G, 'panel', .08)
    box('shaft trim side', (s * (FLOOR[0] - .55), S0 + .3, 0), (.5, .6, 2 * FLOOR[1] - 1.0), G, 'metal', .06)
    box('shaft trim end', (0, S0 + .3, s * (FLOOR[1] - .55)), (2 * FLOOR[0] - 2.0, .6, .5), G, 'metal', .06)
    for k in range(6):
        box('shaft edge light', (s * (FLOOR[0] - .82), S0 + .2, -FLOOR[1] + 1.6 + k * (2 * FLOOR[1] - 3.2) / 5), (.06, .25, .5), G, 'glow', 0)

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
