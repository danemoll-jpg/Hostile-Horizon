"""Hostile Horizon, Round 2: the placeholder mothership (Round 6 replaces the art).

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/mothership.py

Our own design: a long, blunt carrier hull, about 190 m long and 60 m wide, with two big engines at the stern. Between the
engines the stern is open into the rescue bay, so a docked ship looks out over the planet. The bay's floor is open in the
middle: the rescue ship rises into it from below and is held from the ceiling by a cradle; two floor doors (made by the game,
Assets/Scripts/Campaign/Mothership.cs) close under it. The regeneration pod stands in the bay's front corner.

Mothership space = Unity space (x right, y up, z forward; metres); the origin is the dock point: where the rescue ship's
origin sits when docked, the ship facing -z (towards the open stern). Mothership.cs mirrors these numbers (change both):
  bay inside x -14..14, y -6..9, z -26..14; floor opening x -9..9, z -10..10 (doors at y -7..-6);
  regeneration pod at (11.5, -6, 11.5), the waking eye at (11.5, -4.3, 11.2).

Slots: hull, panel, bay, metal, engine, glow, window, glass.
Output: SourceArt/Blender/Mothership.blend, Assets/Resources/Models/Mothership.fbx
"""
import bpy, math, sys, os
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import box, hull, cyl, loft

kit.COLORS.update({'hull': (.50, .52, .53), 'panel': (.33, .35, .37), 'bay': (.22, .23, .24), 'engine': (.14, .15, .16),
                   'glow': (1.0, .6, .25), 'window': (.6, .85, 1.0)})

for ob in list(bpy.data.objects): bpy.data.objects.remove(ob)
G = 'Body'


def section(z, hw, y0, y1):
    return [(-hw, y0, z), (hw, y0, z), (hw, y1, z), (-hw, y1, z)]


# ---- the fore hull and the blunt nose
box('fore hull', (0, 3, 50), (60, 30, 72), G, 'hull', .3)
loft('nose', [section(86, 30, -12, 18), section(110, 26, -10, 16), section(132, 18, -7, 11), section(150, 8, -3, 5)], G, 'hull')
# ---- around the bay: side blocks, roof, the floor strips round the opening
for s in (-1, 1):
    box('bay side', (s * 22, 3, -6), (16, 30, 40), G, 'hull', .3)
    box('floor side', (s * 11.5, -9, 0), (5, 6, 20), G, 'panel', .05)
box('bay roof', (0, 13.5, -6), (28, 9, 40), G, 'hull', .2)
box('floor front', (0, -9, 12), (28, 6, 4), G, 'panel', .05)
box('floor rear', (0, -9, -18), (28, 6, 16), G, 'panel', .05)
# ---- the bay's inside: darker liners, the cradle that holds the ship, light strips
for s in (-1, 1):
    box('bay wall', (s * 13.9, 1.5, -6), (.2, 15, 40), G, 'bay', .0)
    box('ceiling light', (s * 9, 8.8, -6), (.5, .15, 34), G, 'glow', .0)
    box('opening light', (s * 9.2, -5.95, 0), (.3, .1, 20), G, 'glow', .0)
box('bay ceiling', (0, 8.9, -6), (28, .2, 40), G, 'bay', .0)
box('bay front wall', (0, 1.5, 13.9), (28, 15, .2), G, 'bay', .0)
box('cradle arm', (0, 5.3, 1.5), (1.2, 7.4, 1.6), G, 'metal', .05)
box('cradle clamp', (0, 1.45, 1.5), (2.2, .3, 2.0), G, 'metal', .03)
box('cradle beam', (0, 8.4, 1.5), (16, .8, 1.2), G, 'metal', .05)
# ---- regeneration pod in the bay's front corner: a glass tube on a base, lit from inside
cyl('pod base', (11.5, -6, 11.5), (11.5, -5.7, 11.5), 1.3, G, 'metal', 20)
cyl('pod glass', (11.5, -5.7, 11.5), (11.5, -3.0, 11.5), 1.05, G, 'glass', 24, cap=False)
cyl('pod cap', (11.5, -3.0, 11.5), (11.5, -2.6, 11.5), 1.3, G, 'metal', 20)
cyl('pod light', (11.5, -2.62, 11.5), (11.5, -2.55, 11.5), .9, G, 'window', 20)
box('pod console', (9.6, -5.2, 12.8), (1.0, 1.6, .5), G, 'panel', .03)
# ---- stern engines either side of the bay mouth
for s in (-1, 1):
    cyl('engine', (s * 22, 3, -26), (s * 22, 3, -40), 9, G, 'engine', 24)
    cyl('engine ring', (s * 22, 3, -40), (s * 22, 3, -42), 9.6, G, 'metal', 24)
    cyl('nozzle', (s * 22, 3, -42.2), (s * 22, 3, -42.0), 7.5, G, 'glow', 24)
# ---- the bridge on top, lit windows, running lights
box('bridge', (0, 22, 44), (22, 8, 30), G, 'panel', .3)
box('bridge windows', (0, 23, 59.05), (18, 2, .2), G, 'window', .0)
for s in (-1, 1):
    box('side windows', (s * 30.05, 8, 50), (.2, 1.2, 60), G, 'window', .0)
    box('running light', (s * 30.1, -4, 20), (.3, .6, 8), G, 'glow', .0)
    box('wing fin', (s * 34, -2, 30), (8, 2, 40), G, 'panel', .2)
box('keel', (0, -13.5, 50), (12, 3, 70), G, 'panel', .2)

for ob in kit.join_all(): pass
kit.export('Mothership')
