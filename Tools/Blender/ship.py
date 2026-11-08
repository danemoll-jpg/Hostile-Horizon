"""Hostile Horizon: the rescue ship, with a 1:1 cockpit you can sit in (VR-ready rule).

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/ship.py

Round 8 (B1): Dan's art (SourceArt/Concepts/ship.png, cockpit_*.png) made into models by Meshy (Tools/meshy_generate.py,
Tools/Blender/hard_cleanup.py) and assembled here round the pilot's eye. Round 1-7's placeholder is in git history.
- Hull (slot mx_ship): the Meshy ship at 0.8 of its 12.5 m clean-up size (10 m long, 9.2 m across the wings), placed so its own
  canopy surrounds the eye; its dark canopy panes become the game's glass (slot glass: clear from inside), the rest is drawn
  from both sides in the game (the hull's inside closes the cockpit). A window is cut in its belly under the glass floor.
- Cockpit (slots mx_cockpit_seat, mx_cockpit_dash, mx_cockpit_console): the Meshy seat, dash (tilted 20 degrees back; its three
  black screen recesses are where the game puts its live screens: ShipLayout.Screen*) and a side console each side.
- Kept from Round 1-7 (code-made): the cockpit floor, the glass floor and the belly glass, the bulkhead with the cabin hatch
  (Cabin.cs), the landing lamps and the engine nozzles' glow discs (slots lamp, glow).
Ship space = Unity space (x right, y up, z forward; metres), origin at the ship's middle. ShipLayout.cs mirrors EYE, DASH and
the screens (change both together).
Output: SourceArt/Blender/Ship.blend, Assets/Resources/Models/Ship.fbx
"""
import bpy, math, sys, os
import numpy as np
from mathutils import Matrix, Vector
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import box, cyl

ROOT = kit.ROOT
kit.COLORS.update({'hull': (.62, .63, .60), 'panel': (.30, .32, .33), 'engine': (.16, .17, .18), 'glow': (1.0, .55, .2),
                   'seat': (.18, .16, .14), 'dash': (.08, .085, .09)})

EYE = (0, 1.05, 2.4)        # ShipLayout.Eye
HULL_SCALE, HULL_DROP = .8, -1.24          # the Meshy hull: scale from its clean-up size, then down (its eye point lands on EYE)
DASH_TILT, DASH_SCREEN = 20, (0, .69, 2.98)  # the dash: tilt back (degrees), where its centre screen's middle goes
SCREEN_LOCAL = {'centre': (0, -.16, .43), 'left': (-.359, -.16, .415), 'right': (.359, -.16, .415)}   # in the clean-up dash (x, depth -y = toward the pilot, z up)
SEAT_AT = (0, -.05, 2.25)   # the seat's footprint centre on the floor
CONSOLE_X, CONSOLE_Z, CONSOLE_TOP = .88, 2.1, .62

for ob in list(bpy.data.objects): bpy.data.objects.remove(ob)
G = 'Body'


def meshy(name, M4):
    """Append the cleaned Meshy model (front -Y, floor 0, centred) and place it with M4 (Blender ship space)."""
    with bpy.data.libraries.load(os.path.join(ROOT, 'SourceArt', 'Meshy', name, name + '.blend')) as (src, dst):
        dst.objects = [name]
    ob = dst.objects[0]; bpy.context.collection.objects.link(ob)
    ob.data = ob.data.copy(); ob.matrix_world = M4 @ ob.matrix_world
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True); ob.select_set(False)
    m = bpy.data.materials.new('mx_' + name); ob.data.materials.clear(); ob.data.materials.append(m)
    kit.PARTS.append((ob, G, 'mx_' + name))
    return ob


def faces_where(ob, test):
    """Indices of faces whose centre (Unity ship space) passes test(x, y, z, albedo luminance)."""
    img = bpy.data.images.load(os.path.join(ROOT, 'SourceArt', 'Meshy', ob.name, 'base_color.png'), check_existing=True) if False else None
    return [p.index for p in ob.data.polygons if test(p.center.x, p.center.z, p.center.y)]


def split_off(ob, idx, slot):
    """Move faces idx into their own part with the given slot (None: delete them)."""
    bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='DESELECT'); bpy.ops.object.mode_set(mode='OBJECT')
    for i in idx: ob.data.polygons[i].select = True
    bpy.ops.object.mode_set(mode='EDIT')
    if slot is None: bpy.ops.mesh.delete(type='FACE')
    else: bpy.ops.mesh.separate(type='SELECTED')
    bpy.ops.object.mode_set(mode='OBJECT')
    if slot is not None:
        part = [o for o in bpy.context.selected_objects if o != ob][0]
        part.data.materials.clear(); part.data.materials.append(kit.mat(slot)); kit.PARTS.append((part, G, slot))
        return part


def luminance_of_faces(ob, name):
    img = bpy.data.images.load(os.path.join(ROOT, 'SourceArt', 'Meshy', name, 'base_color.png'))
    w, h = img.size; px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4); uv = ob.data.uv_layers.active.data
    out = []
    for p in ob.data.polygons:
        u = sum(uv[i].uv[0] for i in p.loop_indices) / p.loop_total; v = sum(uv[i].uv[1] for i in p.loop_indices) / p.loop_total
        c = px[min(h - 1, int(v * h)), min(w - 1, int(u * w))]; out.append(.3 * c[0] + .59 * c[1] + .11 * c[2])
    return out


# ---- the hull: half a turn (its nose to +Y = Unity +z), 0.8, down HULL_DROP
hullob = meshy('ship', Matrix.Translation((0, 0, HULL_DROP)) @ Matrix.Scale(HULL_SCALE, 4) @ Matrix.Rotation(math.pi, 4, 'Z'))
lum = luminance_of_faces(hullob, 'ship')
# the canopy panes: dark, glossy faces over the cockpit (Unity y above 0.3, z 1.0 - 4.6)
panes = [p.index for p in hullob.data.polygons if lum[p.index] < .07 and p.center.z > .3 and 1.0 < p.center.y < 4.6 and abs(p.center.x) < 1.6]
# the belly window under the glass floor: the hull's bottom skin there
belly = [p.index for p in hullob.data.polygons if abs(p.center.x) < .6 and 2.28 < p.center.y < 3.52 and p.center.z < -.3]
print('SHIP canopy panes', len(panes), 'faces; belly window', len(belly), 'faces')
split_off(hullob, panes, 'glass')
hullob = [o for o, g, s in kit.PARTS if s == 'mx_ship'][0]
# the belly under the glass floor becomes glass too (clear from inside, looking down; tinted from outside)
belly = [p.index for p in hullob.data.polygons if abs(p.center.x) < .6 and 2.28 < p.center.y < 3.52 and p.center.z < -.3]
split_off(hullob, belly, 'glass')

# ---- the dash: tilted back about its centre screen, which lands on DASH_SCREEN
P = Vector(SCREEN_LOCAL['centre'])
dash_M = Matrix.Translation(kit.B(*DASH_SCREEN)) @ Matrix.Rotation(math.radians(-DASH_TILT), 4, 'X') @ Matrix.Translation(-P)
meshy('cockpit_dash', dash_M)
for k, v in SCREEN_LOCAL.items():
    w = dash_M @ Vector(v); print(f'SCREEN {k} at Unity ({w.x:.3f}, {w.z:.3f}, {w.y:.3f})')   # for ShipLayout.Screen*

# ---- the seat (half a turn: it faces forward) and the two side consoles (turned along the walls, their panelled side inward)
meshy('cockpit_seat', Matrix.Translation(kit.B(*SEAT_AT)) @ Matrix.Rotation(math.pi, 4, 'Z'))
for s in (-1, 1):
    con = meshy('cockpit_console', Matrix.Translation(kit.B(s * CONSOLE_X, 0, CONSOLE_Z)) @ Matrix.Rotation(-s * math.pi / 2, 4, 'Z'))
    zs = [v.co.z for v in con.data.vertices]; con.data.transform(Matrix.Translation((0, 0, CONSOLE_TOP - max(zs))))
    box('console base', (s * CONSOLE_X, (CONSOLE_TOP - (max(zs) - min(zs)) - .05) / 2, CONSOLE_Z), (.30, CONSOLE_TOP - (max(zs) - min(zs)) + .05, 1.15), G, 'interior', .01)

# ---- kept: floor, glass floor, bulkhead with the cabin hatch (Cabin.cs), landing lamps
box('cockpit floor rear', (0, -.05, 1.7), (1.7, .06, 1.1), G, 'interior', .005)
box('cockpit floor front', (0, -.05, 3.95), (1.7, .06, .8), G, 'interior', .005)
for s in (-1, 1):
    box('cockpit floor side', (s * .725, -.05, 2.9), (.25, .06, 1.3), G, 'interior', .005)
box('floor glass', (0, -.05, 2.9), (1.2, .03, 1.3), G, 'glass', .002)
box('floor glass frame', (0, -.03, 2.25), (1.2, .05, .04), G, 'metal', .005)
box('floor glass frame', (0, -.03, 3.55), (1.2, .05, .04), G, 'metal', .005)
box('rear bulkhead', (-1.025, .75, 1.2), (.15, 1.6, .1), G, 'interior', .01)
box('rear bulkhead', (.40, .75, 1.2), (1.4, 1.6, .1), G, 'interior', .01)
box('rear bulkhead', (-.625, 1.275, 1.2), (.65, .55, .1), G, 'interior', .01)
box('hatch frame', (-.625, 1.0, 1.25), (.71, .05, .04), G, 'metal', .005)
for x in (-.96, -.29): box('hatch frame', (x, .475, 1.25), (.04, 1.05, .04), G, 'metal', .005)
LAMP_Y = -.5 + HULL_DROP + 1.04   # under the nose
for s in (-1, 1): box('landing lamp', (s * .45, LAMP_Y, 4.1), (.3, .06, .2), G, 'lamp', .005)
# the nozzles' glow: discs just inside the two engine pods' exits (read off the orthographic top / side renders of the hull)
for sx in (-1, 1):
    cyl('nozzle', (sx * 2.4, .95, -2.70), (sx * 2.4, .95, -2.76), .38, G, 'glow', 16)

for ob in kit.join_all(): pass
kit.export('Ship')
lo = [min(v.co[i] for o in bpy.data.objects if o.type == 'MESH' for v in o.data.vertices) for i in range(3)]
hi = [max(v.co[i] for o in bpy.data.objects if o.type == 'MESH' for v in o.data.vertices) for i in range(3)]
hv = [(v.co.x, v.co.z, v.co.y) for v in bpy.data.objects['Body__mx_ship'].data.vertices]
for name, f in (('nose', lambda v: v[2]), ('tail', lambda v: -v[2]), ('belly', lambda v: -v[1]), ('top', lambda v: v[1]), ('left tip', lambda v: -v[0]), ('right tip', lambda v: v[0]),
                ('belly front', lambda v: -v[1] + v[2] * .6), ('belly rear', lambda v: -v[1] - v[2] * .6), ('left low', lambda v: -v[0] - v[1]), ('right low', lambda v: v[0] - v[1])):
    e = max(hv, key=f); print(f'EXTREME {name}: ({e[0]:.2f}, {e[1]:.2f}, {e[2]:.2f})')
print(f'SHIP bounds Unity x {lo[0]:.2f}..{hi[0]:.2f}  y {lo[2]:.2f}..{hi[2]:.2f}  z {lo[1]:.2f}..{hi[1]:.2f}')
