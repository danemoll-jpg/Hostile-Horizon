"""Hostile Horizon, Round 5 (B10): the enemies, hard-surface, our own designs.

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/enemies.py

Turret.fbx: a squat armoured gun emplacement, about 5 m across and 4.5 m tall. Three groups, each built round its own pivot at
the origin (the game places them, Campaign/Enemies.cs):
  TurretBase: a stepped octagonal pedestal with braced feet, pivot on the ground;
  TurretHead: the turning housing (yaw), pivot at its turning ring (the game puts it 2.4 m up);
  TurretGun:  twin barrels with a sensor eye between them (pitch), pivot at the trunnion (0, 0.9, 0.4 in the head).
Saucer.fbx: a flying saucer, 14 m across, 3.5 m deep: a lens-shaped hull with armour plates, a dark sensor dome and a glowing
underside emitter (group Saucer), and a ring of lamps round the rim that the game spins (group SaucerRim).
Guns point along +z in Unity space (FBX files import facing back: the game turns each part half a turn).
Slots: armor, metal, dark, glow, eye.
"""
import bpy, math, sys, os
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import box, cyl, blob, loft, ring

kit.COLORS.update({'armor': (.36, .33, .30), 'metal': (.45, .46, .48), 'dark': (.08, .08, .09), 'glow': (1.0, .25, .2), 'eye': (1.0, .2, .15)})


def clear():
    for ob in list(bpy.data.objects): bpy.data.objects.remove(ob)


def octagon(y, r):
    return ring((0, y, 0), r, r, 8, 'y', math.pi / 8)


# ---------------------------------------------------------------- the turret (Round 8: Dan's art via Meshy, cut into its three parts)
import meshykit
from mathutils import Matrix
clear()
ob = meshykit.load('turret', Matrix.Rotation(math.pi, 4, 'Z'), 'TurretBase')   # half a turn: the barrels along +Y (Unity +z)
V = [meshykit.unity(v.co) for v in ob.data.vertices]
top = max(v[1] for v in V)
# the turning neck: the narrowest section between the base and the head
def reach(h): return max((math.hypot(v[0], v[2]) for v in V if abs(v[1] - h) < .06), default=99)
RING = min((h / 20 for h in range(int(top * 20 * .3), int(top * 20 * .7))), key=reach)
# the barrels: in front of the head the section shrinks to the barrel band (under 1.05 m tall; the housing is about 2 m)
def span(z):
    ys = [v[1] for v in V if v[1] > RING and abs(v[2] - z) < .1]
    return (max(ys) - min(ys)) if ys else 0
zmax = max(v[2] for v in V); ZCUT = zmax
z = zmax
while z > 0:
    if span(z) > 1.05: break
    ZCUT = z; z -= .05
GUNY = sum(v[1] for v in V if v[2] > ZCUT + .3 and v[1] > RING) / max(1, sum(1 for v in V if v[2] > ZCUT + .3 and v[1] > RING))
print(f'TURRET ring {RING:.2f} m, barrels from z {ZCUT:.2f}, barrel axis {GUNY:.2f} m (game: Head at (0, {RING:.2f}, 0), Gun at (0, {GUNY - RING:.2f}, {ZCUT:.2f}) in the head)')
faces = [(p.index, meshykit.unity(p.center)) for p in ob.data.polygons]
gun = meshykit.split(ob, [i for i, c in faces if c[1] > RING and c[2] > ZCUT], 'TurretGun')
faces = [(p.index, meshykit.unity(p.center)) for p in ob.data.polygons]
head = meshykit.split(ob, [i for i, c in faces if c[1] > RING], 'TurretHead')
meshykit.shift(head, (0, RING, 0)); meshykit.shift(gun, (0, GUNY, ZCUT))
blob('eye', (0, 0, .05), (.22, .22, .1), 'TurretGun', 'eye', 12, 8)       # the sensor eye the game lights, at the barrels' root
kit.join_all()
kit.export('Turret')

# ---------------------------------------------------------------- the saucer (Round 8: Meshy; the game's rim lamps and emitter kept)
clear()
ob = meshykit.load('saucer', Matrix.Identity(4), 'Saucer')
V = [meshykit.unity(v.co) for v in ob.data.vertices]
R = max(math.hypot(v[0], v[2]) for v in V)
RIMY = max(V, key=lambda v: math.hypot(v[0], v[2]))[1]
ob.data.transform(Matrix.Translation((0, 0, -RIMY)))                     # the rim at y 0, as before
low = min(v[1] for v in V) - RIMY
print(f'SAUCER radius {R:.2f} m, rim {RIMY:.2f} m up, bottom {low:.2f} m under the rim, top {max(v[1] for v in V) - RIMY:.2f} m over it')
cyl('emitter', (0, low - .02, 0), (0, low + .1, 0), 1.4, 'Saucer', 'glow', 24)
G = 'SaucerRim'
for i in range(12):
    a = i * 2 * math.pi / 12
    blob('rim lamp', (math.sin(a) * (R + .05), 0, math.cos(a) * (R + .05)), (.35, .22, .35), G, 'glow', 10, 6)
kit.join_all()
kit.export('Saucer')
