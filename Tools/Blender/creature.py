"""Hostile Horizon, Round 3 (item B2): the alien creature, our own design (nothing from any other game).

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/creature.py

About 2.2 m tall standing, thin and hunched, wrong-jointed: legs that bend backwards (a second, reversed knee), arms that hang
past the knees with an extra-long forearm, hands of four long jointed fingers, ribs showing through a wet grey skin, a long
eyeless head with a cluster of small milky eyes low on each side, and a jaw that splits open in two halves.
Creature space = Unity space (x right, y up, z forward; metres), feet on y = 0, facing +z.

Each body segment is its own group (<Segment>__<slot>), so the game can turn it about its joint:
Assets/Scripts/Campaign/Creature.cs mirrors the joint points below (JOINTS; change both together).
Slots: skin (wet grey), hide (darker, wet), bone (teeth, claws, ribs), gum (inside the mouth), eye (small milky eyes).
Output: SourceArt/Blender/Creature.blend, Assets/Resources/Models/Creature.fbx
"""
import bpy, math, sys, os
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import box, cyl, blob, limb

kit.COLORS.update({'skin': (.36, .34, .31), 'hide': (.13, .12, .12), 'bone': (.82, .78, .64), 'gum': (.35, .06, .07), 'eye': (.85, .88, .75)})
for ob in list(bpy.data.objects): bpy.data.objects.remove(ob)

# joints (Unity space) — Creature.cs Joints
J = {
    'Hips': (0, 1.12, 0), 'Chest': (0, 1.55, .12), 'Neck': (0, 1.92, .34), 'Head': (0, 2.05, .48), 'Jaw': (0, 1.98, .58),
    'ArmL': (-.30, 1.84, .26), 'ElbowL': (-.40, 1.30, .14), 'WristL': (-.44, .72, .34),
    'ArmR': (.30, 1.84, .26), 'ElbowR': (.40, 1.30, .14), 'WristR': (.44, .72, .34),
    'LegL': (-.16, 1.12, 0), 'KneeL': (-.20, .74, .22), 'HockL': (-.20, .36, -.18),
    'LegR': (.16, 1.12, 0), 'KneeR': (.20, .74, .22), 'HockR': (.20, .36, -.18),
}

# ---- body: pelvis, a thin hunched spine, ribs
G = 'Hips'
blob('pelvis', (0, 1.12, 0), (.2, .12, .14), G, 'hide', 12, 8)
limb('spine low', (0, 1.12, -.02), (0, 1.55, .1), .07, .06, G, 'skin', 8)
G = 'Chest'
blob('chest', (0, 1.66, .18), (.22, .22, .15), G, 'skin', 14, 10, rot=(-20, 0, 0))
limb('spine high', (0, 1.55, .1), (0, 1.92, .34), .06, .05, G, 'hide', 8)
for i in range(6):   # ribs showing through the skin, bent round the chest
    y = 1.5 + i * .065; w = .2 - abs(i - 2.5) * .02
    for s in (-1, 1):
        kit.tube('rib', [(0, y, .02 + i * .02), (s * w, y - .03, .14 + i * .02), (s * w * .8, y - .06, .3 + i * .015)], .012, G, 'bone', 3)
blob('shoulders', (0, 1.83, .24), (.33, .07, .1), G, 'hide', 12, 6)

# ---- neck and the long head
G = 'Neck'
limb('neck', (0, 1.9, .32), (0, 2.03, .46), .055, .045, G, 'skin', 8)
G = 'Head'
blob('skull', (0, 2.1, .58), (.13, .14, .26), G, 'skin', 16, 10, rot=(-12, 0, 0))
blob('brow ridge', (0, 2.18, .66), (.12, .05, .17), G, 'hide', 12, 6, rot=(-12, 0, 0))
blob('upper jaw', (0, 2.03, .72), (.09, .05, .17), G, 'hide', 12, 6, rot=(-8, 0, 0))
for s in (-1, 1):
    for k in range(4):   # small milky eyes low on each side, in a cluster
        blob('eye', (s * (.105 + k % 2 * .01), 2.07 + (k // 2) * .035, .66 + (k % 2) * .05), (.018, .018, .018), G, 'eye', 8, 5)
for k in range(9):       # upper teeth: long, uneven
    x = -.07 + k * .0175; L = .05 + (k % 3) * .02
    cyl('tooth', (x, 2.0, .8 - abs(x) * .6), (x * 1.05, 2.0 - L, .8 - abs(x) * .6 + .01), .006, G, 'bone', 5, r1=.001)
G = 'Jaw'
for s in (-1, 1):        # the jaw in two halves that split open sideways
    blob('jaw half', (s * .05, 1.95, .72), (.05, .035, .16), G, 'hide', 10, 6, rot=(10, s * 6, 0))
    for k in range(5):
        x = s * (.015 + k * .012); z = .82 - k * .035
        cyl('tooth', (x, 1.96, z), (x, 2.01 + (k % 2) * .015, z), .005, G, 'bone', 5, r1=.001)
blob('mouth', (0, 1.99, .7), (.07, .03, .14), G, 'gum', 10, 6)

# ---- arms: too long, the forearm longest, four long fingers
for side, sx in (('L', -1), ('R', 1)):
    G = 'Arm' + side
    limb('upper arm', J['Arm' + side], J['Elbow' + side], .045, .035, G, 'skin', 8)
    G = 'Elbow' + side
    limb('forearm', J['Elbow' + side], J['Wrist' + side], .035, .025, G, 'skin', 8)
    blob('elbow spur', (J['Elbow' + side][0] + sx * .01, J['Elbow' + side][1] + .02, J['Elbow' + side][2] - .05), (.02, .02, .06), G, 'bone', 8, 5, rot=(30, 0, 0))
    G = 'Wrist' + side
    w = J['Wrist' + side]
    blob('palm', (w[0], w[1] - .07, w[2] + .02), (.045, .07, .02), G, 'hide', 10, 6)
    for f in range(4):
        x0 = w[0] + sx * (-.03 + f * .022); a = (w[0], w[1] - .13, w[2] + .02)
        k1 = (x0 + sx * (f - 1.5) * .012, w[1] - .30, w[2] + .06 + f * .01); k2 = (x0 + sx * (f - 1.5) * .02, w[1] - .50, w[2] + .12 + f * .01)
        limb('finger', (x0, a[1], a[2]), k1, .011, .009, G, 'skin', 6)
        limb('finger tip', k1, k2, .009, .006, G, 'skin', 6)
        cyl('claw', k2, (k2[0], k2[1] - .06, k2[2] + .03), .006, G, 'bone', 5, r1=.0008)

# ---- legs: thigh forward to the knee, the shin back to a second, reversed joint (the hock), then a long foot
for side, sx in (('L', -1), ('R', 1)):
    G = 'Leg' + side
    limb('thigh', J['Leg' + side], J['Knee' + side], .07, .05, G, 'skin', 8)
    G = 'Knee' + side
    limb('shin', J['Knee' + side], J['Hock' + side], .045, .035, G, 'hide', 8)
    G = 'Hock' + side
    h = J['Hock' + side]
    limb('foot', h, (h[0], .04, h[2] + .26), .035, .03, G, 'skin', 8)
    for t in (-1, 0, 1):
        cyl('toe claw', (h[0] + t * .03, .03, h[2] + .27), (h[0] + t * .05, 0, h[2] + .38), .012, G, 'bone', 5, r1=.002)

objs = kit.join_all()
kit.export('Creature')
