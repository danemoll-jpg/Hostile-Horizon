"""Hostile Horizon, Fix 3.1 stage 3: clean up a Meshy model for Mixamo and render it for a look.

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/meshy_cleanup.py -- <name> [<name> ...]

Reads SourceArt/Meshy/<name>/model.glb (from Tools/meshy_generate.py). Joins the meshes, scales the model to its height
in metres, puts the origin on the floor between the feet, centres it, keeps it facing front (Blender -Y = Mixamo's and
Unity's front once imported). Writes SourceArt/Meshy/<name>/<name>.blend, <name>_mixamo.fbx (embedded textures; the file
Dan uploads to Mixamo) and front / side / three-quarter renders (render_front.png, render_side.png, render_34.png).
LODs are made in stage 5 from Mixamo's rigged file, so they keep the skin weights.
"""
import bpy, math, os, sys
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
HEIGHT = {'pilot': 1.8, 'mimic': 2.5, 'hollow': 2.1, 'knocker': 2.2, 'stalker': 2.3, 'sleeper': 1.9}


def clean(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    d = os.path.join(ROOT, 'SourceArt', 'Meshy', name)
    bpy.ops.import_scene.gltf(filepath=os.path.join(d, 'model.glb'))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    # apply parents' transforms (glTF importer adds an empty root), then join into one object
    for o in meshes:
        mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
    for o in [o for o in bpy.context.scene.objects if o.type != 'MESH']:
        bpy.data.objects.remove(o)
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1: bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    # scale to height, feet on z = 0, centred on x / y
    vs = [ob.matrix_world @ v.co for v in ob.data.vertices]
    lo = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
    hi = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
    s = HEIGHT[name] / (hi.z - lo.z)
    ob.location = (-(lo.x + hi.x) / 2 * s, -(lo.y + hi.y) / 2 * s, -lo.z * s)
    ob.scale = (s, s, s)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(d, name + '.blend'))
    bpy.ops.export_scene.fbx(filepath=os.path.join(d, name + '_mixamo.fbx'), use_selection=False, object_types={'MESH'},
                             path_mode='COPY', embed_textures=True, apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False)
    render(ob, d, name)
    width = (hi.x - lo.x) * s
    print(f'CLEANUP {name}: {tris} triangles, height {HEIGHT[name]} m, width {width:.2f} m (scale {s:.3f})')


def render(ob, d, name):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 768, 1024
    w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (.55, .56, .58, 1); w.node_tree.nodes['Background'].inputs[1].default_value = .9
    for rot, e in (((50, 0, 30), 3.0), ((60, 0, -140), 1.2)):
        l = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); l.data.energy = e
        l.rotation_euler = [math.radians(a) for a in rot]; sc.collection.objects.link(l)
    h = HEIGHT[name]
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = h * 1.15
    for tag, ang in (('front', 0), ('side', 90), ('34', 35)):
        a = math.radians(ang); r = 10
        cam.location = (r * math.sin(a), -r * math.cos(a), h / 2)
        cam.rotation_euler = (math.radians(90), 0, a)
        sc.render.filepath = os.path.join(d, f'render_{tag}.png')
        bpy.ops.render.render(write_still=True)


names = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else list(HEIGHT)
for n in names:
    if os.path.exists(os.path.join(ROOT, 'SourceArt', 'Meshy', n, 'model.glb')):
        clean(n)
    else:
        print(f'CLEANUP {n}: no model.glb yet')
