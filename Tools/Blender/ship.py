"""Hostile Horizon, Round 1: the placeholder rescue ship, with a 1:1 cockpit you can sit in (VR-ready rule).

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/ship.py

Our own design: a stubby delta-wing rescue lander. A rounded fuselage with a bubble canopy at the front, two engine pods on
the wing roots, two canted fins, an airlock door on the left side behind the cockpit, landing lamps under the nose.
Ship space = Unity space (x right, y up, z forward; metres), origin at the ship's middle. The ship is about 12.5 m long and
11 m across the wing tips.

Cockpit (1:1): the pilot's eye at EYE; the seat under and behind it; side consoles up to the canopy sill (y 0.6); a dash
in front whose top is below the eye line so the view over the nose stays open. The game puts its instrument displays on
the dash (Assets/Scripts/Flight/ShipLayout.cs mirrors EYE and DASH; change both together).

Groups: Body (everything). Slots: hull, panel, metal, engine, glow, glass, lamp, interior, seat, dash.
Output: SourceArt/Blender/Ship.blend, Assets/Resources/Models/Ship.fbx
"""
import bpy, math, sys, os
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import box, hull, cyl, blob, tube, loft, ring

kit.COLORS.update({'hull': (.62, .63, .60), 'panel': (.30, .32, .33), 'engine': (.16, .17, .18), 'glow': (1.0, .55, .2),
                   'seat': (.18, .16, .14), 'dash': (.08, .085, .09)})

EYE = (0, 1.05, 2.4)        # ShipLayout.Eye
DASH = (0, .62, 3.25)       # ShipLayout.DashCentre

for ob in list(bpy.data.objects): bpy.data.objects.remove(ob)
G = 'Body'


def section(z, hw, y0, y1, n=16, square=.55):
    """A rounded-box cross-section at z: half-width hw, bottom y0, top y1 (a superellipse)."""
    cy, hh = (y0 + y1) / 2, (y1 - y0) / 2; pts = []
    for i in range(n):
        a = 2 * math.pi * i / n; c, s = math.cos(a), math.sin(a)
        pw = 2 / (2 + 4 * square)
        x = math.copysign(abs(c) ** pw, c) * hw; y = math.copysign(abs(s) ** pw, s) * hh
        pts.append((x, cy + y, z))
    return pts


# ---- fuselage: low in the cockpit (top at the canopy sill), a dorsal spine behind it
keys = [(6.25, .12, -.05, .08), (5.4, .75, -.45, .32), (4.4, 1.15, -.7, .55), (3.2, 1.4, -.85, .62), (1.5, 1.6, -.9, .66),
        (.6, 1.7, -.9, 1.05), (-1.5, 1.75, -.9, 1.15), (-4.0, 1.55, -.8, 1.0), (-5.6, 1.15, -.55, .7), (-6.2, .8, -.35, .45)]
fuselage = loft('fuselage', [section(z, hw, y0, y1) for z, hw, y0, y1 in keys], G, 'hull')

# ---- canopy: a glass bubble over the cockpit, with frame arches
blob('canopy', (0, .62, 2.95), (1.02, .98, 1.75), G, 'glass', segs=24, rings=14)
for z in (1.55, 2.95):          # no frame ahead of the pilot: the view forward stays clear
    t = (z - 2.95) / 1.75; s = math.sqrt(max(0, 1 - t * t))
    pts = [(math.cos(a) * 1.04 * s, .62 + math.sin(a) * 1.0 * s, z) for a in [math.pi * k / 8 for k in range(9)]]
    tube('canopy arch', pts, .035, G, 'metal')

# ---- wings (slight anhedral) and engine pods with glowing nozzles
for s in (-1, 1):
    hull('wing', [(s * 1.5, -.35, -4.6), (s * 5.5, -.75, -4.6), (s * 5.5, -.75, -2.9), (s * 1.5, -.35, 1.2),
                  (s * 1.5, -.05, -4.6), (s * 5.5, -.62, -4.6), (s * 5.5, -.62, -2.9), (s * 1.5, -.05, 1.2)], G, 'hull', .02)
    hull('wing tip', [(s * 5.45, -.8, -4.7), (s * 5.65, -.8, -4.7), (s * 5.65, -.8, -2.6), (s * 5.45, -.8, -2.8),
                      (s * 5.45, -.2, -4.5), (s * 5.65, -.2, -4.5), (s * 5.65, -.2, -3.6), (s * 5.45, -.2, -3.6)], G, 'panel', .01)
    cyl('pod', (s * 2.35, -.2, -6.0), (s * 2.35, -.2, -1.2), .62, G, 'engine', 16)
    cyl('pod nose', (s * 2.35, -.2, -1.2), (s * 2.35, -.2, -.4), .62, G, 'hull', 16, r1=.25)
    cyl('pod ring', (s * 2.35, -.2, -6.05), (s * 2.35, -.2, -5.6), .66, G, 'metal', 16)
    cyl('nozzle', (s * 2.35, -.2, -6.12), (s * 2.35, -.2, -6.02), .48, G, 'glow', 16)
    hull('fin', [(s * 2.2, .35, -5.9), (s * 2.5, .35, -5.9), (s * 2.5, .35, -3.6), (s * 2.2, .35, -3.6),
                 (s * 2.75, 2.0, -6.2), (s * 2.95, 2.0, -6.2), (s * 2.95, 2.0, -5.2), (s * 2.75, 2.0, -5.2)], G, 'panel', .015)
    box('landing lamp', (s * .45, -.82, 4.2), (.3, .06, .2), G, 'lamp', .005)

# ---- hull details: the airlock door (left side), a dorsal panel, an antenna
box('airlock door', (-1.73, .05, -1.6), (.08, 1.45, 1.1), G, 'panel', .01)
box('airlock handle', (-1.78, .05, -1.05), (.05, .5, .06), G, 'metal', .005)
box('dorsal panel', (0, 1.17, -2.6), (1.4, .06, 2.2), G, 'panel', .01)
cyl('antenna', (0, 1.15, -4.2), (0, 1.9, -4.6), .025, G, 'metal', 6)

# ---- cockpit interior (1:1): floor, consoles, bulkhead, seat, dash, throttle and stick
# Fix 2.5: glass in the floor at the pilot's feet and in the lower canopy beside the knees (the front of the side consoles), so
# the ground below can be seen by looking down (flat screen: the look control; VR: the head). The hull's own faces are one-sided
# (seen from outside only), so nothing else is in the way.
box('cockpit floor rear', (0, -.05, 1.7), (1.7, .06, 1.1), G, 'interior', .005)
box('cockpit floor front', (0, -.05, 3.95), (1.7, .06, .8), G, 'interior', .005)
for s in (-1, 1):
    box('cockpit floor side', (s * .725, -.05, 2.9), (.25, .06, 1.3), G, 'interior', .005)
box('floor glass', (0, -.05, 2.9), (1.2, .03, 1.3), G, 'glass', .002)
box('floor glass frame', (0, -.03, 2.25), (1.2, .05, .04), G, 'metal', .005)
box('floor glass frame', (0, -.03, 3.55), (1.2, .05, .04), G, 'metal', .005)
for s in (-1, 1):
    box('side console', (s * .88, .3, 2.075), (.32, .62, 1.35), G, 'interior', .02)
    box('console top', (s * .86, .62, 2.2), (.30, .03, 1.1), G, 'dash', .005)
    box('lower canopy glass', (s * .88, .3, 3.375), (.32, .62, 1.25), G, 'glass', .005)
# Round 3: the bulkhead has a low hatch on the left (x -0.95..-0.30, up to y 1.0) into the cabin behind; the game builds the cabin
# and the hatch's door (Assets/Scripts/Campaign/Cabin.cs: change both together)
box('rear bulkhead', (-1.025, .75, 1.2), (.15, 1.6, .1), G, 'interior', .01)
box('rear bulkhead', (.40, .75, 1.2), (1.4, 1.6, .1), G, 'interior', .01)
box('rear bulkhead', (-.625, 1.275, 1.2), (.65, .55, .1), G, 'interior', .01)
box('hatch frame', (-.625, 1.0, 1.25), (.71, .05, .04), G, 'metal', .005)
for x in (-.96, -.29): box('hatch frame', (x, .475, 1.25), (.04, 1.05, .04), G, 'metal', .005)
box('seat cushion', (0, .32, 1.95), (.58, .16, .55), G, 'seat', .04)
box('seat back', (0, .85, 1.62), (.58, .85, .14), G, 'seat', .04, rot=(-12, 0, 0))
box('headrest', (0, 1.38, 1.52), (.34, .22, .12), G, 'seat', .04, rot=(-12, 0, 0))
box('dash', DASH, (1.5, .36, .5), G, 'interior', .02, rot=(25, 0, 0))
box('dash hood', (0, .84, 3.5), (1.5, .04, .3), G, 'dash', .01)       # top 0.21 m under the eye line
box('throttle', (-.78, .72, 2.55), (.06, .18, .06), G, 'metal', .01, rot=(-20, 0, 0))
cyl('stick', (0, .05, 2.65), (0, .55, 2.6), .025, G, 'metal', 8)
box('stick grip', (0, .62, 2.6), (.06, .16, .07), G, 'dash', .015)

# Fix 2.5: windows cut in the hull's belly under the glass floor and under the lower canopy glass, glazed from outside
def cut_window(ob, c, s):
    bpy.ops.mesh.primitive_cube_add(location=kit.B(*c))
    cutter = bpy.context.active_object; cutter.scale = (s[0] / 2, s[2] / 2, s[1] / 2)
    m = ob.modifiers.new('window', 'BOOLEAN'); m.operation = 'DIFFERENCE'; m.object = cutter
    bpy.context.view_layer.objects.active = ob; bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.data.objects.remove(cutter)

# (through the whole fuselage: its top skin runs across the cockpit at the sill, y 0.63, under the canopy bubble)
cut_window(fuselage, (0, -.2, 2.9), (1.14, 1.9, 1.24))
for s in (-1, 1):
    cut_window(fuselage, (s * .9, -.2, 3.375), (.34, 1.9, 1.2))
box('belly glass', (0, -.87, 2.9), (1.2, .03, 1.3), G, 'glass', .002)
for s in (-1, 1):
    box('belly glass side', (s * .9, -.87, 3.375), (.4, .03, 1.26), G, 'glass', .002)

for ob in kit.join_all(): pass
kit.export('Ship')
