"""Hostile Horizon, Round 8 (B1): helpers to put the cleaned Meshy models (Tools/Blender/hard_cleanup.py) into the game's
Blender scripts (kit.py's parts and export). A Meshy part's slot is "mx_<name>": the game gives it Resources/Hard/<name>.mat.
Clean-up frame: front -Y, floor at z 0, centred on x / y. Blender ship / enemy / mothership space: Unity (x, y, z) = Blender
(x, z, y), front +Y (so most models need half a turn).
"""
import bpy, os
from mathutils import Matrix
import kit

ROOT = kit.ROOT


def load(name, M4, group, slot=None):
    """Append SourceArt/Meshy/<name>/<name>.blend's object, place it with M4 (Blender space), register it as a kit part."""
    with bpy.data.libraries.load(os.path.join(ROOT, 'SourceArt', 'Meshy', name, name + '.blend')) as (src, dst):
        dst.objects = [name]
    ob = dst.objects[0]; bpy.context.collection.objects.link(ob)
    ob.data = ob.data.copy(); ob.matrix_world = M4 @ ob.matrix_world
    bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True); ob.select_set(False)
    slot = slot or 'mx_' + name
    m = bpy.data.materials.get(slot) or bpy.data.materials.new(slot); ob.data.materials.clear(); ob.data.materials.append(m)
    kit.PARTS.append((ob, group, slot))
    return ob


def unity(v):
    """A Blender-space vertex as Unity (x, y, z)."""
    return (v.x, v.z, v.y)


def split(ob, idx, group, slot=None):
    """Move faces idx of ob into a new kit part (group, slot: default ob's slot); slot False deletes them. Returns the new part."""
    own = [s for o, g, s in kit.PARTS if o == ob][0]
    bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='DESELECT'); bpy.ops.object.mode_set(mode='OBJECT')
    for i in idx: ob.data.polygons[i].select = True
    bpy.ops.object.mode_set(mode='EDIT')
    if slot is False:
        bpy.ops.mesh.delete(type='FACE'); bpy.ops.object.mode_set(mode='OBJECT'); return None
    bpy.ops.mesh.separate(type='SELECTED'); bpy.ops.object.mode_set(mode='OBJECT')
    part = [o for o in bpy.context.selected_objects if o != ob][0]
    slot = slot or own
    if slot != own: part.data.materials.clear(); part.data.materials.append(bpy.data.materials.get(slot) or kit.mat(slot))
    kit.PARTS.append((part, group, slot))
    return part


def shift(ob, unity_point):
    """Move a part's geometry so unity_point lands on the origin (a part built round its own pivot)."""
    ob.data.transform(Matrix.Translation(-kit.B(*unity_point)))
