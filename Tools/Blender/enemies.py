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


# ---------------------------------------------------------------- the turret
clear()
G = 'TurretBase'
loft('pedestal', [octagon(0, 2.6), octagon(.5, 2.5), octagon(.7, 2.1), octagon(1.9, 1.7), octagon(2.3, 1.5)], G, 'armor', bevel=.04, smooth=False)
for i in range(4):
    a = i * math.pi / 2 + math.pi / 4; s, c = math.sin(a), math.cos(a)
    box('foot', (s * 2.5, .25, c * 2.5), (1.1, .5, 1.1), G, 'metal', .04, rot=(0, math.degrees(a), 0))
    box('brace', (s * 2.0, 1.0, c * 2.0), (.35, 1.5, .9), G, 'dark', .02, rot=(0, math.degrees(a), 0))
cyl('ring', (0, 2.15, 0), (0, 2.4, 0), 1.55, G, 'metal', 24)

G = 'TurretHead'
loft('housing', [octagon(0, 1.6), octagon(.25, 1.75), octagon(1.4, 1.55), octagon(1.8, 1.1)], G, 'armor', bevel=.04, smooth=False)
box('front plate', (0, .8, 1.35), (2.2, 1.2, .35), G, 'armor', .05, rot=(-18, 0, 0))
box('cheek L', (-1.25, .95, .4), (.3, 1.1, 1.6), G, 'metal', .03)
box('cheek R', (1.25, .95, .4), (.3, 1.1, 1.6), G, 'metal', .03)
box('sensor mast', (-.7, 2.1, -.6), (.2, .8, .2), G, 'dark', .01)
box('sensor', (-.7, 2.55, -.55), (.45, .25, .35), G, 'dark', .02)
box('sensor lamp', (-.7, 2.55, -.36), (.25, .12, .05), G, 'glow', .0)

G = 'TurretGun'
cyl('trunnion', (-1.1, 0, 0), (1.1, 0, 0), .32, G, 'metal', 16)
box('breech', (0, 0, .2), (1.5, .75, 1.3), G, 'dark', .04)
for s in (-1, 1):
    cyl('barrel', (s * .42, 0, .7), (s * .42, 0, 3.6), .17, G, 'metal', 12)
    cyl('muzzle', (s * .42, 0, 3.4), (s * .42, 0, 3.9), .24, G, 'dark', 12)
    cyl('shroud', (s * .42, 0, .7), (s * .42, 0, 1.7), .26, G, 'armor', 12)
blob('eye', (0, .05, .95), (.2, .2, .1), G, 'eye', 12, 8)
kit.join_all()
kit.export('Turret')

# ---------------------------------------------------------------- the saucer
clear()
G = 'Saucer'
R = 7.0
prof = [(0, -1.6, 1.2), (0, -1.4, 3.6), (0, -.9, 5.6), (0, -.35, 6.8), (0, 0, R), (0, .35, 6.8), (0, .9, 5.4), (0, 1.35, 3.4), (0, 1.55, 1.6)]
secs = []
for i in range(32):
    a = i * 2 * math.pi / 32
    secs.append([(math.sin(a) * r, y, math.cos(a) * r) for (_, y, r) in prof])
# lathe: sections are angles, points go along the profile; build as loft over the profile instead
lathe = [[(math.sin(i * 2 * math.pi / 32) * r, y, math.cos(i * 2 * math.pi / 32) * r) for i in range(32)] for (_, y, r) in prof]
loft('hull', lathe, G, 'armor', cap=True, smooth=True)
for i in range(8):
    a = i * 2 * math.pi / 8 + math.pi / 8
    box('plate', (math.sin(a) * 4.6, .95, math.cos(a) * 4.6), (2.2, .2, 1.6), G, 'metal', .03, rot=(0, math.degrees(a), 0))
    box('vent', (math.sin(a) * 4.4, -1.05, math.cos(a) * 4.4), (1.6, .2, .5), G, 'dark', .02, rot=(0, math.degrees(a), 0))
blob('dome', (0, 1.75, 0), (1.9, .9, 1.9), G, 'dark', 20, 10)
blob('dome eye', (0, 1.85, 1.55), (.5, .25, .35), G, 'eye', 12, 8)
cyl('emitter', (0, -1.75, 0), (0, -1.55, 0), 1.4, G, 'glow', 24)
cyl('emitter rim', (0, -1.7, 0), (0, -1.5, 0), 1.75, G, 'metal', 24)
cyl('gun', (0, -1.3, 4.0), (0, -1.3, 6.4), .2, G, 'dark', 10)

G = 'SaucerRim'
for i in range(12):
    a = i * 2 * math.pi / 12
    blob('rim lamp', (math.sin(a) * (R + .05), 0, math.cos(a) * (R + .05)), (.35, .22, .35), G, 'glow', 10, 6)
kit.join_all()
kit.export('Saucer')
