"""Hostile Horizon, Round 8 (B1): look at the assembled ship in Blender (textures on): from the pilot's eye and from outside.

Run:  F:\blender\blender.exe --background --factory-startup --python Tools/Blender/render_ship.py -- <out dir>
Opens SourceArt/Blender/Ship.blend (Tools/Blender/ship.py). Writes eye_ahead.png, eye_down.png, eye_left.png, eye_back.png
(the cockpit camera's 60-degree view at ShipLayout.Eye), out_34.png, out_side.png, out_top.png.
"""
import bpy, math, os, sys
from mathutils import Vector
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
out = sys.argv[sys.argv.index('--') + 1]; os.makedirs(out, exist_ok=True)
MODEL = sys.argv[sys.argv.index('--') + 2] if len(sys.argv) > sys.argv.index('--') + 2 else 'Ship'
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, 'SourceArt', 'Blender', MODEL + '.blend'))
sc = bpy.context.scene
for ob in bpy.data.objects:
    if ob.type != 'MESH': continue
    slot = ob.name.split('__')[-1]; m = ob.data.materials[0] if ob.data.materials else None
    if not m: continue
    m.use_nodes = True; nt = m.node_tree; bsdf = nt.nodes.get('Principled BSDF')
    if slot.startswith('mx_'):
        tex = nt.nodes.new('ShaderNodeTexImage'); tex.image = bpy.data.images.load(os.path.join(ROOT, 'SourceArt', 'Meshy', slot[3:], 'base_color.png'))
        nt.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    if slot == 'glass':
        bsdf.inputs['Alpha'].default_value = .25; m.blend_method = 'BLEND'; m.use_backface_culling = True
    if slot in ('glow', 'lamp'):
        bsdf.inputs['Emission Color' if 'Emission Color' in bsdf.inputs else 'Emission'].default_value = (1, .55, .2, 1); bsdf.inputs['Emission Strength'].default_value = 5
sc.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
sc.render.resolution_x, sc.render.resolution_y = 1280, 720
w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True
w.node_tree.nodes['Background'].inputs[0].default_value = (.62, .55, .5, 1); w.node_tree.nodes['Background'].inputs[1].default_value = 1.0
for rot, e in (((50, 0, 30), 3.0), ((60, 0, -140), 1.2)):
    l = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); l.data.energy = e
    l.rotation_euler = [math.radians(a) for a in rot]; sc.collection.objects.link(l)
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
def B(x, y, z): return Vector((x, z, y))
def shoot(name, at, look, persp=True, lens_fov=60, ortho=0):
    cam.data.type = 'PERSP' if persp else 'ORTHO'
    if persp: cam.data.sensor_fit = 'VERTICAL'; cam.data.angle = math.radians(lens_fov)
    else: cam.data.ortho_scale = ortho
    cam.data.clip_start = .02; cam.data.clip_end = 2000
    cam.location = at; cam.rotation_euler = (look - at).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = os.path.join(out, name + '.png'); bpy.ops.render.render(write_still=True)
if MODEL == 'Mothership':
    eye = B(0, 1.05, -2.4)   # docked: the ship faces -z (the open stern)
    shoot('dock_ahead', eye, eye + B(0, 0, -1))
    shoot('dock_left', eye, eye + B(-1, -.1, .2))
    shoot('dock_right', eye, eye + B(1, -.1, .2))
    shoot('dock_up', eye, eye + B(0, 1, .3))
    pe = B(11.5, -4.3, 11.2); shoot('pod_wake', pe, B(2, -1.5, -8))
    shoot('out_34', B(160, 90, -170), B(0, 0, 40))
    shoot('out_below_stern', B(25, -60, -90), B(0, 0, -10))
    shoot('bay_from_stern', B(0, 0, -60), B(0, 0, 0))
else:
    eye = B(0, 1.05, 2.4)
    shoot('eye_ahead', eye, eye + B(0, 0, 1))
    shoot('eye_down', eye, eye + B(0, -1.1, .7))
    shoot('eye_left', eye, eye + B(-1, -.2, .1))
    shoot('eye_back', eye, eye + B(-.3, -.2, -1))
    shoot('out_34', B(9, 5, 11), B(0, 0, 0))
    shoot('out_back', B(2.5, 2.5, -11), B(0, 1.2, -4.5), True, 40)
    shoot('out_side', B(-20, .5, 0), B(0, .5, 0), False, 0, 13)
    shoot('out_top', B(0, 20, 0.01), B(0, 0, 0), False, 0, 13)
