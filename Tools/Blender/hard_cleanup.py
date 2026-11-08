"""Hostile Horizon, Round 8 (B1): clean up the hard-surface Meshy models and render them for Dan.

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/hard_cleanup.py -- [<name> ...]

Reads SourceArt/Meshy/<name>/model.glb (Tools/meshy_generate.py). Joins the meshes, turns the model so its front faces
Blender -Y (YAW, found from the first top-view renders), scales it to its size in metres (SIZE: along the model's height
'h' or its longest flat side 'l'), sits it on z = 0, centred on x / y. Writes SourceArt/Meshy/<name>/<name>.blend and
renders render_34.png, render_front.png, render_side.png, render_top.png (with a 1 m / 10 m scale bar in the top view's
corner), plus SourceArt/Meshy/sheet_round8.png with every model's three-quarter render.
The game-side cuts (canopy, turret parts, bay mouth), LODs and the FBX for Unity come after Dan has seen these.
"""
import bpy, math, os, sys
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SIZE = {'ship': ('l', 12.5), 'cockpit_seat': ('h', 1.1), 'cockpit_dash': ('l', 1.4), 'cockpit_console': ('l', 1.2),
        'mothership': ('l', 190), 'bay_wall': ('l', 10), 'bay_cradle': ('l', 8), 'regen_pod': ('h', 2.4),
        'turret': ('h', 4.5), 'saucer': ('l', 14)}
YAW = {'ship': 90, 'mothership': 90}   # degrees about z to bring the front to -Y (from the first top-view renders: both had the nose at -X)


def bounds(ob):
    vs = [ob.matrix_world @ v.co for v in ob.data.vertices]
    return (Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs))),
            Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs))))


def clean(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    d = os.path.join(ROOT, 'SourceArt', 'Meshy', name)
    bpy.ops.import_scene.gltf(filepath=os.path.join(d, 'model.glb'))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    for o in meshes:
        mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
    for o in [o for o in bpy.context.scene.objects if o.type != 'MESH']:
        bpy.data.objects.remove(o)
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1: bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active; ob.name = name
    ob.rotation_mode = 'XYZ'; ob.rotation_euler = (0, 0, math.radians(YAW.get(name, 0)))   # the glTF importer leaves quaternion mode on
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    lo, hi = bounds(ob)
    kind, size = SIZE[name]
    s = size / ((hi.z - lo.z) if kind == 'h' else max(hi.x - lo.x, hi.y - lo.y))
    ob.location = (-(lo.x + hi.x) / 2 * s, -(lo.y + hi.y) / 2 * s, -lo.z * s)
    ob.scale = (s, s, s)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    lo, hi = bounds(ob); dim = hi - lo
    tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(d, name + '.blend'))
    render(d, dim)
    print(f'CLEANUP {name}: {tris} triangles, {dim.x:.2f} x {dim.y:.2f} x {dim.z:.2f} m (x across, y front-back, z up)')


def render(d, dim):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1024, 768
    w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (.55, .56, .58, 1); w.node_tree.nodes['Background'].inputs[1].default_value = .9
    for rot, e in (((50, 0, 30), 3.0), ((60, 0, -140), 1.2)):
        l = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); l.data.energy = e
        l.rotation_euler = [math.radians(a) for a in rot]; sc.collection.objects.link(l)
    big = max(dim.x, dim.y, dim.z); mid = Vector((0, 0, dim.z / 2))
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = big * 1.2; cam.data.clip_end = big * 20
    views = {'34': (35, 25), 'front': (0, 0), 'side': (90, 0), 'top': (0, 89.9)}   # yaw from the front (-Y), elevation
    for tag, (yaw, el) in views.items():
        a, e = math.radians(yaw), math.radians(el); r = big * 4
        dirv = Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
        cam.location = mid + dirv * r
        cam.rotation_euler = (-dirv).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = os.path.join(d, f'render_{tag}.png')
        bpy.ops.render.render(write_still=True)


names = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv and sys.argv.index('--') + 1 < len(sys.argv) else list(SIZE)
done = []
for n in names:
    if os.path.exists(os.path.join(ROOT, 'SourceArt', 'Meshy', n, 'model.glb')):
        clean(n); done.append(n)
    else:
        print(f'CLEANUP {n}: no model.glb yet')
