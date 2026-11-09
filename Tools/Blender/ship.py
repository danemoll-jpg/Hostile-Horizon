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
import bpy, bmesh, math, sys, os
import numpy as np
from mathutils import Matrix, Vector
sys.path.insert(0, os.path.dirname(__file__))
import kit, meshykit
from kit import box, cyl

ROOT = kit.ROOT
kit.COLORS.update({'hull': (.62, .63, .60), 'panel': (.30, .32, .33), 'engine': (.16, .17, .18), 'glow': (1.0, .55, .2),
                   'seat': (.18, .16, .14), 'dash': (.08, .085, .09), 'window': (.10, .16, .15)})

EYE = (0, 1.05, 2.4)        # ShipLayout.Eye
HULL_SCALE, HULL_DROP = .8, -1.24          # the Meshy hull: scale from its clean-up size, then down (its eye point lands on EYE)
DASH_TILT, DASH_SCREEN = 20, (0, .69, 2.98)  # the dash: tilt back (degrees), where its centre screen's middle goes
SCREEN_LOCAL = {'centre': (0, -.16, .43), 'left': (-.359, -.16, .415), 'right': (.359, -.16, .415)}   # in the clean-up dash (x, depth -y = toward the pilot, z up)
SEAT_AT = (0, -.05, 2.25)   # the seat's footprint centre on the floor
# Fix 8.1: the floor window between the pilot's feet: centre (x, z), size across / along, corner radius; the floor plate it sits in
WIN_C, WIN_W, WIN_L, WIN_R = (0, 3.02), .42, .70, .08
WIN_FLARE = (.62, .52, .18)   # the shaft widens toward the belly (across, along, corner): more ground seen through the window
PLATE = (-.6, .6, 2.25, 3.55)   # x0, x1, z0, z1 (was the old glass floor)
FLOOR_TOP, FLOOR_BOT = -.02, -.08
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
print('SHIP canopy panes', len(panes), 'faces')
split_off(hullob, panes, 'glass')
hullob = [o for o, g, s in kit.PARTS if s == 'mx_ship'][0]

# Fix 8.1: the floor window's shaft through the belly. Round 8 turned the belly faces whose centres fell in a box into glass, so the
# edge followed the Meshy triangles (Dan: "all jagged"). Now the belly is cut along four straight planes round the window and the
# faces inside are removed; a lining from the floor down to the hull, widening as it goes, makes the shaft.
BW, BL = WIN_W + WIN_FLARE[0], WIN_L + WIN_FLARE[1]   # the shaft's bottom outline
CUT = (WIN_C[0] - BW / 2 - .01, WIN_C[0] + BW / 2 + .01, WIN_C[1] - BL / 2 - .01, WIN_C[1] + BL / 2 + .01)
bm = bmesh.new(); bm.from_mesh(hullob.data)


def near(f):
    c = f.calc_center_median()
    return c.z < -.15 and CUT[0] - .3 < c.x < CUT[1] + .3 and CUT[2] - .3 < c.y < CUT[3] + .3


for co, no in (((CUT[0], 0, 0), (1, 0, 0)), ((CUT[1], 0, 0), (1, 0, 0)), ((0, CUT[2], 0), (0, 1, 0)), ((0, CUT[3], 0), (0, 1, 0))):
    fs = [f for f in bm.faces if near(f)]
    geom = list({e for f in fs for e in f.edges}) + list({v for f in fs for v in f.verts}) + fs
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no)
inside = [f for f in bm.faces if f.calc_center_median().z < -.15 and CUT[0] < f.calc_center_median().x < CUT[1] and CUT[2] < f.calc_center_median().y < CUT[3]]
hull_lo = min(v.co.z for f in inside for v in f.verts) if inside else -.9
hull_hi = max(v.co.z for f in inside for v in f.verts) if inside else -.9
bmesh.ops.delete(bm, geom=inside, context='FACES'); bm.to_mesh(hullob.data); bm.free()
print(f'SHIP belly cut: {len(inside)} faces removed, a clean {CUT[1] - CUT[0]:.2f} x {CUT[3] - CUT[2]:.2f} m opening; hull there y {hull_lo:.3f}..{hull_hi:.3f}')

# ---- the dash: tilted back about its centre screen, which lands on DASH_SCREEN
P = Vector(SCREEN_LOCAL['centre'])
dash_M = Matrix.Translation(kit.B(*DASH_SCREEN)) @ Matrix.Rotation(math.radians(-DASH_TILT), 4, 'X') @ Matrix.Translation(-P)
meshy('cockpit_dash', dash_M)
for k, v in SCREEN_LOCAL.items():
    w = dash_M @ Vector(v); print(f'SCREEN {k} at Unity ({w.x:.3f}, {w.z:.3f}, {w.y:.3f})')   # for ShipLayout.Screen*

# ---- the seat (half a turn: it faces forward) and the two side consoles (turned along the walls, their panelled side inward)
seat = meshy('cockpit_seat', Matrix.Translation(kit.B(*SEAT_AT)) @ Matrix.Rotation(math.pi, 4, 'Z'))
# Fix 8.1: mirrored left / right (x -> -x about the seat's middle, x 0) so the stick is on the right and the throttle on the left, where
# Dan's hands are on his HOTAS. Mirroring turns the faces inside out, so their winding is reversed; the texture has no lettering
# (checked), and Unity recalculates the tangents for the normal map at import.
SC = Vector((0, SEAT_AT[2], .5))
out0 = sum((p.center - SC).dot(p.normal) > 0 for p in seat.data.polygons)
seat.data.transform(Matrix.Scale(-1, 4, (1, 0, 0)))
bm = bmesh.new(); bm.from_mesh(seat.data); bmesh.ops.reverse_faces(bm, faces=bm.faces[:]); bm.to_mesh(seat.data); bm.free(); seat.data.update()
out1 = sum((p.center - SC).dot(p.normal) > 0 for p in seat.data.polygons)
print(f'SEAT mirrored: faces pointing out {out0} before, {out1} after (of {len(seat.data.polygons)}); custom normals {seat.data.has_custom_normals}')
# the stick (right armrest's front) and the throttle (left armrest, the T-handle on two posts): the faces above the armrest top, in their
# own groups (Stick, Throttle) so the game can move them; the game finds their pivots from their bounds (PilotBody.cs)
for grp, (x0, x1, z0, z1, ylo) in (('Stick', (.285, .40, SEAT_AT[2] + .21, SEAT_AT[2] + .38, .453)),
                                    ('Throttle', (-.415, -.245, SEAT_AT[2] - .13, SEAT_AT[2] + .09, .452))):
    idx = [p.index for p in seat.data.polygons if x0 < p.center.x < x1 and z0 < p.center.y < z1 and p.center.z > ylo]
    part = meshykit.split(seat, idx, grp)
    if grp == 'Throttle': part.data.transform(Matrix.Translation(kit.B(0, -.012, .15)))   # 15 cm forward along the armrest: the seat's own
    # place put the hand 0.1-0.3 m behind the eye, the arm bent backwards (0.8.1's first pictures); now it rests beside the hip
    vs = [v.co for v in part.data.vertices]
    print(f'{grp.upper()} {len(idx)} faces: x {min(v.x for v in vs):.3f}..{max(v.x for v in vs):.3f}  y {min(v.z for v in vs):.3f}..{max(v.z for v in vs):.3f}  '
          f'z {min(v.y for v in vs):.3f}..{max(v.y for v in vs):.3f}')
for s in (-1, 1):
    con = meshy('cockpit_console', Matrix.Translation(kit.B(s * CONSOLE_X, 0, CONSOLE_Z)) @ Matrix.Rotation(-s * math.pi / 2, 4, 'Z'))
    zs = [v.co.z for v in con.data.vertices]; con.data.transform(Matrix.Translation((0, 0, CONSOLE_TOP - max(zs))))
    box('console base', (s * CONSOLE_X, (CONSOLE_TOP - (max(zs) - min(zs)) - .05) / 2, CONSOLE_Z), (.30, CONSOLE_TOP - (max(zs) - min(zs)) + .05, 1.15), G, 'interior', .01)

# ---- kept: floor, glass floor, bulkhead with the cabin hatch (Cabin.cs), landing lamps
box('cockpit floor rear', (0, -.05, 1.7), (1.7, .06, 1.1), G, 'interior', .005)
box('cockpit floor front', (0, -.05, 3.95), (1.7, .06, .8), G, 'interior', .005)
for s in (-1, 1):
    box('cockpit floor side', (s * .725, -.05, 2.9), (.25, .06, 1.3), G, 'interior', .005)


# ---- Fix 8.1: the floor window (replaces Round 8's glass floor over the ragged belly hole): a plate with a rounded opening, a raised
# frame with bolts, 30 mm glass (slot window: HostileHorizon/Window), the shaft's lining down to the clean cut in the belly
def rrect(cx, cz, w, l, r, seg=6):
    """A rounded rectangle's outline (x, z) counter-clockwise seen from above, as 4 corners' arcs."""
    out = []
    for k, (sx, sz) in enumerate(((1, 1), (-1, 1), (-1, -1), (1, -1))):
        ccx, ccz = cx + sx * (w / 2 - r), cz + sz * (l / 2 - r); a0 = k * math.pi / 2
        out.append([(ccx + r * math.cos(a0 + math.pi / 2 * i / seg), ccz + r * math.sin(a0 + math.pi / 2 * i / seg)) for i in range(seg + 1)])
    return out


def mesh_part(name, build, slot):
    bm = bmesh.new(); build(bm); bm.normal_update()
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob)
    me.materials.append(kit.mat(slot)); kit.PARTS.append((ob, G, slot)); return ob


def V(bm, p, y): return bm.verts.new(kit.B(p[0], y, p[1]))


hole = rrect(WIN_C[0], WIN_C[1], WIN_W, WIN_L, WIN_R)
inner = [p for c in hole for p in c]
owner = [k for k, c in enumerate(hole) for _ in c]
corners = ((PLATE[1], PLATE[3]), (PLATE[0], PLATE[3]), (PLATE[0], PLATE[2]), (PLATE[1], PLATE[2]))


def orient(bm, f, want):
    """Turn face f so its normal (Blender space) points along want (Blender vector)."""
    f.normal_update()
    if f.normal.dot(want) < 0: f.normal_flip()


def plate(bm):
    n = len(inner)
    for y, up in ((FLOOR_TOP, 1), (FLOOR_BOT, -1)):
        cv = [V(bm, c, y) for c in corners]; iv = [V(bm, p, y) for p in inner]
        for i in range(n):
            j = (i + 1) % n; a, b = cv[owner[i]], cv[owner[j]]
            f = bm.faces.new([a, b, iv[j], iv[i]] if a is not b else [a, iv[j], iv[i]])
            orient(bm, f, Vector((0, 0, up)))
        if up == 1: top = iv
        else: bot = iv
    for i in range(n):   # the opening's wall, facing into the opening
        j = (i + 1) % n; f = bm.faces.new((top[i], top[j], bot[j], bot[i]))
        mx = (inner[i][0] + inner[j][0]) / 2 - WIN_C[0]; mz = (inner[i][1] + inner[j][1]) / 2 - WIN_C[1]
        orient(bm, f, -Vector((mx, mz, 0)))


def ring(bm, a, b, y0, y1):
    """A raised ring between outline a (inner) and b (outer) of the same length, from y0 to y1; faces point out of the solid."""
    n = len(a)
    at, bt, ab, bb = [V(bm, p, y1) for p in a], [V(bm, p, y1) for p in b], [V(bm, p, y0) for p in a], [V(bm, p, y0) for p in b]
    for i in range(n):
        j = (i + 1) % n
        orient(bm, bm.faces.new((at[i], at[j], bt[j], bt[i])), Vector((0, 0, 1)))
        mx = (a[i][0] + a[j][0]) / 2 - WIN_C[0]; mz = (a[i][1] + a[j][1]) / 2 - WIN_C[1]
        orient(bm, bm.faces.new((at[i], at[j], ab[j], ab[i])), -Vector((mx, mz, 0)))
        orient(bm, bm.faces.new((bt[i], bt[j], bb[j], bb[i])), Vector((mx, mz, 0)))


mesh_part('floor plate', plate, 'interior')
trim_out = [p for c in rrect(WIN_C[0], WIN_C[1], WIN_W + .09, WIN_L + .09, WIN_R + .045) for p in c]
mesh_part('window frame', lambda bm: ring(bm, inner, trim_out, FLOOR_TOP - .004, FLOOR_TOP + .016), 'metal')
mid = [p for c in rrect(WIN_C[0], WIN_C[1], WIN_W + .045, WIN_L + .045, WIN_R + .022, 12) for p in c]
per = [0.0]
for i in range(len(mid) - 1): per.append(per[-1] + math.dist(mid[i], mid[i + 1]))
for k in range(14):   # bolts evenly round the frame
    d = per[-1] * (k + .5) / 14; i = max(i for i in range(len(per)) if per[i] <= d); p = mid[i]
    cyl('window bolt', (p[0], FLOOR_TOP + .016, p[1]), (p[0], FLOOR_TOP + .023, p[1]), .0075, G, 'chrome', 8)


def glass(bm):
    top = [V(bm, p, FLOOR_TOP - .006) for p in inner]; bot = [V(bm, p, FLOOR_TOP - .036) for p in inner]   # 30 mm thick
    orient(bm, bm.faces.new(top), Vector((0, 0, 1))); orient(bm, bm.faces.new(bot), Vector((0, 0, -1)))
    for i in range(len(top)):
        j = (i + 1) % len(top); mx = (inner[i][0] + inner[j][0]) / 2 - WIN_C[0]; mz = (inner[i][1] + inner[j][1]) / 2 - WIN_C[1]
        orient(bm, bm.faces.new((top[i], top[j], bot[j], bot[i])), Vector((mx, mz, 0)))


mesh_part('floor window', glass, 'window')


def shaft(bm):
    low = [p for c in rrect(WIN_C[0], WIN_C[1], BW, BL, WIN_R + WIN_FLARE[2]) for p in c]
    t = [V(bm, p, FLOOR_BOT) for p in inner]; b = [V(bm, p, hull_lo - .01) for p in low]
    for i in range(len(t)):
        j = (i + 1) % len(t); mx = (inner[i][0] + inner[j][0]) / 2 - WIN_C[0]; mz = (inner[i][1] + inner[j][1]) / 2 - WIN_C[1]
        orient(bm, bm.faces.new((t[i], t[j], b[j], b[i])), -Vector((mx, mz, 0)))


mesh_part('window shaft', shaft, 'metal')
print(f'WINDOW centre ({WIN_C[0]}, {FLOOR_TOP}, {WIN_C[1]}), {WIN_W} x {WIN_L} m, corners {WIN_R} m; shaft down to y {hull_lo - .01:.3f}')
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
