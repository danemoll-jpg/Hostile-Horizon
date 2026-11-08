# Hostile Horizon — models needed

- **Round 8 (B1): the ship, the cockpit, the mothership and its hangar bay, the turret, the saucer** — below, waiting for
  Dan's concept images.
- Fix 3.1: the pilot and the five creatures — done (further down, kept for reference).

---------------------------------------------------------------------------------------------------------------------------

# Round 8 (B1) — hard-surface models

Same pipeline as the characters (Dan's standing way): Code writes the prompts (this section) -> **Dan has the concept
images drawn** -> Code makes the models with the Meshy API (`MESHY_API_KEY`, never written down) and finishes them in
Blender (scale, pivots, LODs, simple collision shapes, materials) -> **Code shows Dan renders and waits** -> Code puts them
in the game. No Mixamo: nothing here has a skeleton.

All original designs, nothing from the 1984 game. Sizes are for Code to set in Blender; the images don't need to show them.

## What Meshy needs, and what the game keeps doing itself

Meshy turns one picture into **one solid, fused, textured shell**. So:
- **Moving or see-through parts are made by the game or by Code in Blender, not by the picture:** the canopy glass and the
  glass floor, the landing legs, the sliding airlock door, the engine glow, the screens' pictures, the mothership's floor
  doors, the saucer's spinning rim lamps. The pictures should show where these go (a canopy frame, a door outline, nozzles,
  dark screen recesses), drawn closed and solid.
- **The cockpit is made from parts** (seat, dash, side console), each its own picture, because Meshy builds objects, not the
  inside of a room. Code assembles them round the pilot's eye at 1:1 (the eye stays where it is now, `ShipLayout.Eye`), so
  the screens, the glass floor, the belly / cabin cameras and VR keep working.
- **The hangar bay is made from parts too** (wall section, docking cradle, regeneration pod); Code builds the room from
  them inside the new mothership.
- The **turret's** base, turning head and gun barrels are cut apart by Code in Blender (the game turns the head and lifts
  the gun), so draw them clearly distinct: a visible turning ring, barrels on a short axle.

## The images (Dan)

Paste each prompt as it is. Save each image as **`SourceArt/Concepts/<name>.png`** (names in the headings).

What makes an image usable (Code checks these when you've saved them):
- **one object, all of it in frame**, some space round it, nothing cropped;
- **three-quarter view from the front and a little above** (unless the prompt says otherwise), not a flat side view;
- **plain light-grey background**, even soft light, no strong shadows or reflections of a scene (Meshy paints them in);
- no ground, no people, no text, no extra views, no motion blur, no lens effects;
- solid, believable hard-surface detail: panels, bolts, vents, wear; nothing glowing;
- square or landscape, at least 1024 px.

If your tool has a negative prompt: `cropped, cut off, multiple views, people, pilot, text, logo, watermark, background
scenery, ground, strong shadows, reflections, motion blur, lens flare, depth of field, cartoon, low poly`.

Shared style (already in each prompt): our ship and mothership are **worn industrial rescue-corps hardware** (off-white and
pale grey paint, burnt-orange rescue markings, dark gunmetal machinery, scuffs and grime). The enemies are the opposite:
**dark, alien, rust-red and bronze**, no orange markings.

---

### 1. Rescue ship, outside — `ship.png` (12.5 m long, 11 m across the wings)

> Hard-surface concept art of a small sci-fi rescue lander, three-quarter front view from slightly above, the whole craft
> in frame. A stubby delta-wing ship about 12 metres long: a rounded, chunky fuselage; a large bubble canopy at the front
> made of a few big framed glass panes, drawn as dark tinted glass; short swept delta wings low on the body with a squared
> engine pod on each wing root, the nozzles at the back; two fins canted outward at the tail; a rectangular airlock door
> outline in the left side behind the canopy; small landing lamps under the nose; landing legs retracted (flush doors).
> Worn industrial rescue-corps hardware: off-white and pale grey painted metal with burnt-orange rescue markings and stripes,
> dark gunmetal machinery, scuffs, grime and heat staining around the engines, realistic hard-surface sci-fi, like a working
> vehicle that has flown a thousand missions. Plain light grey background, soft even studio lighting, no shadows, no text.

**Design note:** seen in every chase view, the landing view, the hangar and the launch. It keeps today's silhouette (stubby
delta, bubble canopy, two engine pods, two canted fins) so the cockpit fits inside. Code replaces the canopy panes with the
game's own glass; the airlock door, legs and engine glow stay made by the game; the loaner's yellow and the burnt wreck are
tints over the new texture.

### 2. Cockpit seat — `cockpit_seat.png` (seat back about 1.1 m tall)

> Hard-surface concept art of a single sci-fi pilot's seat from a small rescue ship's cockpit, three-quarter front view from
> slightly above, the whole seat in frame, no pilot. A rugged padded flight seat: a tall back with a headrest, side bolsters,
> five-point harness straps lying flat on the cushion, armrests with a flight-stick grip on the right armrest and a throttle
> lever on the left, a metal frame and rails underneath. Worn industrial rescue-corps hardware: dark grey padding with
> burnt-orange stitching and pull tabs, off-white painted metal frame, dark gunmetal rails, scuffs and wear on the edges,
> realistic hard-surface sci-fi. Plain light grey background, soft even studio lighting, no shadows, no text.

**Design note:** in VR you look down at it and your arms rest on it. Code places it so the eye point stays at
`ShipLayout.Eye` and the cabin hatch behind your left shoulder stays clear.

### 3. Cockpit dash — `cockpit_dash.png` (about 1.4 m wide, 0.5 m tall)

> Hard-surface concept art of the instrument dash of a small sci-fi rescue ship's cockpit, seen from the pilot's seat,
> slightly from above, the whole dash in frame. A low, wide, curved dash panel: three rectangular screen recesses in a row
> across the middle, the centre one largest, each screen plain flat black; around them rows of small toggle switches,
> guarded switches, push buttons and rotary knobs, a few small round analogue gauges, labels too small to read, grab handles
> at each end, a sloped glare shield over the top edge. The panel is low so the pilot sees over it, with an open space under
> it for the legs. Worn industrial rescue-corps hardware: off-white and pale grey painted panels, dark gunmetal bezels, small
> burnt-orange warning markings, scuffed paint around the switches, realistic hard-surface sci-fi. Plain light grey
> background, soft even studio lighting, no shadows, no text, no glowing screens.

**Design note:** the three black recesses are where the game's live screens go (flight numbers, the belly camera, the
radar); keep them flat and black so Code can line the screens up with them. The dash top stays below the eye line (the view
over the nose) and the footwell open (the glass floor is there).

### 4. Cockpit side console — `cockpit_console.png` (about 1.2 m long, 0.6 m tall)

> Hard-surface concept art of a side console from a small sci-fi rescue ship's cockpit, three-quarter view from above, the
> whole console in frame. A long, narrow console that runs along the cockpit wall beside the pilot's seat at elbow height:
> a sloped top covered in switch panels, circuit-breaker rows, a small keypad, two small round gauges, a recessed storage
> slot, a few cable runs and a hand rail on the inner edge; the side facing the pilot is plain panelling with vents.
> Worn industrial rescue-corps hardware: off-white and pale grey painted panels, dark gunmetal switch plates, small
> burnt-orange markings, scuffs and grime, realistic hard-surface sci-fi. Plain light grey background, soft even studio
> lighting, no shadows, no text, nothing glowing.

**Design note:** one picture for both sides (Code mirrors it for the right). The canopy frame and the bulkhead with the
cabin hatch are built by Code to fit the glass, in the same paint.

### 5. Mothership, outside — `mothership.png` (about 190 m long, 60 m wide)

> Hard-surface concept art of a huge sci-fi rescue carrier ship, three-quarter rear view from slightly above, the whole
> ship in frame. A long, heavy, blunt-nosed hull, about 190 metres long, built like an industrial hospital ship: stacked hull
> sections, radiator fins, antenna masts, rows of small windows, docking light housings. Two very large engines at the stern,
> one on each side, and between them a big open rectangular hangar mouth at the stern, dark inside, with a wide opening in
> the hangar floor underneath where small craft rise in. Worn industrial rescue-corps hardware: off-white and pale grey
> painted plating with large burnt-orange rescue stripes and markings, dark gunmetal machinery, grime and heat staining
> around the engines, realistic hard-surface sci-fi. Plain light grey background, soft even studio lighting, no shadows, no
> text.

**Design note:** you fly up to it from below and into the open stern on every docking, and see it hanging in the sky. Code
cuts the hangar mouth and floor opening clean (to the bay's numbers in `Mothership.cs`) and builds the bay inside from the
next three pictures.

### 6. Hangar bay wall section — `bay_wall.png` (a section about 10 m wide, 15 m tall)

> Hard-surface concept art of one section of the inside wall of a large spaceship hangar bay, front view, slightly from
> one side, the whole section in frame. A tall wall panel module: heavy structural ribs at both edges, a walkway with railings
> halfway up, pipes and cable trays running across, a recessed equipment locker, hazard-striped edges, floodlight housings
> near the top. Worn industrial rescue-corps hardware: pale grey painted plating, dark gunmetal ribs and machinery,
> burnt-orange and black hazard stripes, grime and fluid stains, realistic hard-surface sci-fi. Plain light grey background,
> soft even studio lighting, no shadows, no text, no people.

**Design note:** Code repeats this section round the bay. It's what you look at while docked in the hangar menus.

### 7. Docking cradle — `bay_cradle.png` (about 8 m across)

> Hard-surface concept art of a heavy docking cradle that hangs from a hangar ceiling and holds a small spaceship from above,
> three-quarter view from below, the whole cradle in frame. A big mechanical clamp: a ceiling mount with hydraulic rams, two
> long arms that reach down and out, padded grab pads at their ends, cable bundles and hoses, warning light housings
> (unlit). Worn industrial rescue-corps hardware: dark gunmetal machinery, off-white painted covers, burnt-orange and black
> hazard markings, oil and grime, realistic hard-surface sci-fi. Plain light grey background, soft even studio lighting, no
> shadows, no text, no spaceship.

**Design note:** it holds your ship after every docking and lets it go at every launch, over the floor opening.

### 8. Regeneration pod — `regen_pod.png` (about 2.4 m tall)

> Hard-surface concept art of a medical regeneration pod from a sci-fi rescue ship, three-quarter front view, the whole pod
> in frame. An upright capsule big enough for one person to stand in: a heavy base full of machinery and cables, a curved
> front hatch with a large window (drawn as dark tinted glass), side panels with small status displays (dark), coolant
> pipes, a top cap with a vent. Clinical but worn: off-white and pale grey painted panels, dark gunmetal machinery, a small
> burnt-orange medical cross marking, scuffs and grime, realistic hard-surface sci-fi. Plain light grey background, soft
> even studio lighting, no shadows, no text, no person inside.

**Design note:** you wake inside this after every death and look out through its window into the bay; Code keeps the
window open (the game's glass) and the waking eye point where it is now.

### 9. Turret — `turret.png` (about 5 m across, 4.5 m tall)

> Hard-surface concept art of a hostile alien ground gun turret, three-quarter front view from slightly above, the whole
> turret in frame. A squat armoured emplacement: a stepped, wide pedestal with braced feet, a clearly separate rotating
> armoured head on a visible turning ring, and twin heavy gun barrels mounted on a short axle at the front of the head, with
> a single round sensor eye between the barrels. Alien and hostile, not human-made: dark weathered metal in rust-red, bronze
> and black, organic ridged armour plates, corrosion and scorch marks, realistic hard-surface sci-fi. Plain light grey
> background, soft even studio lighting, no shadows, no text, no muzzle flash.

**Design note:** the game turns the head, lifts the barrels and lights the sensor eye red; the turning ring and the axle
must be clear so Code can cut base, head and gun apart. Seen at 100 m - 2 km, often against the sky.

### 10. Saucer — `saucer.png` (about 14 m across, 3.5 m deep)

> Hard-surface concept art of a hostile alien flying saucer drone, three-quarter view from slightly above, the whole craft
> in frame. A flat lens-shaped hull about 14 metres across: overlapping armour plates in rings, a raised dark sensor dome on
> top, a thick rim with a row of small round lamp sockets all the way round (unlit), and on the underside a circular emitter
> housing. Alien and hostile, not human-made: dark weathered metal in rust-red, bronze and black, ridged organic armour,
> scorch marks and corrosion, realistic hard-surface sci-fi. Plain light grey background, soft even studio lighting, no
> shadows, no text, no glow, no beams.

**Design note:** the game spins its own lamps round the rim and lights the emitter; a saucer is seen from below as much as
from above, so the underside matters.

---

## After the images (Code)

`python Tools/meshy_generate.py --only <names>` (image-to-3D, hard-surface settings; triangle budgets: ship 30k, dash 15k,
console 8k, seat 8k, mothership 40k, bay wall 8k, cradle 10k, pod 10k, turret 12k, saucer 12k; LODs at ~50 % and ~20 % in
Blender except the cockpit parts, which are always close) -> Blender clean-up (scale to the sizes above, pivots, the cuts
listed above, simple box / capsule collision) -> renders `SourceArt/Meshy/<name>/render_*.png` and a contact sheet for Dan
(**STOP**) -> into the game.

---------------------------------------------------------------------------------------------------------------------------

# Fix 3.1 — character models (done)

Six models: the **pilot** (also every alien's disguise) and **five alien reveal forms**, one per type. Pipeline (Dan's
standing way of working): Code writes the prompts (this file) -> **Dan has the concept images drawn** -> Code makes the 3D
models with the Meshy API -> **Dan rigs them in Mixamo and downloads the animations listed here** -> Code puts them in the game.

All original designs: nothing from the 1984 game. Sizes are for Code to set in Blender; the images don't need to show them.

## Stage 2 — the concept images (Dan)

Paste each prompt as it is into your image tool. Save each image as **`SourceArt/Concepts/<name>.png`**
(`pilot.png`, `mimic.png`, `hollow.png`, `knocker.png`, `stalker.png`, `sleeper.png`).

What makes an image usable for Meshy and Mixamo (Code checks these when you've saved them):
- **one figure, whole body**, head to feet with some space around it, nothing cropped;
- **front view, A-pose** (arms straight, about 40° out from the body), legs a little apart, fingers spread;
- **plain light-grey background**, even soft light, no strong shadows (Meshy paints shadows into the texture);
- no props, no ground, no text, no extra views;
- arms, legs and fingers **clear of the body** (anything touching gets fused together in 3D);
- no tails, wings, tentacles or loose cloth hanging free (Mixamo's humanoid skeleton can't move them);
- portrait or square, at least 1024 px.

If the tool allows a "negative prompt", use: `cropped, cut off feet, side view, back view, multiple views, props, weapon,
background scenery, text, watermark, strong shadows, motion blur, tail, wings, tentacles`.

---

### 1. Pilot — `pilot.png` (1.8 m)

> Full-body character concept art of a human rescue-corps pilot in a sealed sci-fi flight suit, standing in an A-pose, arms
> straight and angled 40 degrees away from the body, legs slightly apart, fingers spread, facing the camera, front view.
> Burnt-orange padded flight suit with dark grey armour plates on the chest, shoulders, knees and shins, a small life-support
> pack flat on the back, chunky dark grey boots and gloves. Rounded off-white helmet with the visor closed: a large dark
> mirrored visor covering the whole face. Practical, worn, a little scuffed, realistic proportions. Plain light grey
> background, soft even studio lighting, no shadows, whole body in frame from helmet to boots, no props, no text.
> Realistic 3D game character style.

**Design note:** the one thing every player must recognise at a glance from the cockpit and the airlock camera: bright
orange suit, white helmet, dark mirror visor. Every alien wears this look until it reveals itself, so it must be a
believable person, not a hero. The **visor must be its own clearly separate area** (one dark colour, edge to edge):
the game swaps in its own visor material, a live mirror for real pilots and dead black for the **Hollow** (that is the
Hollow's tell, so the reflection is the game's job, not the texture's). The pilot casts the normal shadow; the **Mimic's**
wrong shadow is made by the game. The knock (**Knocker** tell), the straight run to the door (**Stalker** tell) and the
cabin (**Sleeper**) all use this model's animations.

### 2. Mimic — `mimic.png` (2.5 m)

> Full-body concept art of a horrifying alien creature, standing in an A-pose, arms straight and angled 40 degrees away from
> the body, legs slightly apart, facing the camera, front view. Impossibly tall and thin humanoid, 2.5 metres, with arms so
> long the hands hang to the knees and long thin fingers. Its skin is stretched over the shape of a human body like a copy
> made by something that has only seen people from far away: rubbery burnt-orange skin with seams and folds where a flight
> suit's panels would be, as if the suit has grown into flesh. Its head is a smooth elongated off-white dome with no face,
> only a faint vertical slit. Wrong, too-long proportions, unsettling. Plain light grey background, soft even studio
> lighting, no shadows, whole body in frame from head to feet, no props, no text. Realistic 3D horror game creature style.

**Design note:** the Mimic copies the suit but gets the body wrong. Its tell is its **shadow**: the game gives the
disguised Mimic a shadow that is taller, thin, long-armed and a moment behind it — this reveal form is that shadow made
flesh, so the moment it reveals, the player sees what the shadow was. The orange "suit-skin" and white head make it read
as a pilot gone wrong. Scary because it is almost right: a person stretched.

### 3. Hollow — `hollow.png` (2.1 m)

> Full-body concept art of a horrifying alien creature, standing in an A-pose, arms straight and angled 40 degrees away from
> the body, legs slightly apart, facing the camera, front view. A gaunt grey-white humanoid, 2.1 metres, with dry cracked
> skin like old plaster and visible ribs. Where its face should be there is nothing: the front of the head is an open empty
> hollow, a smooth deep black cavity ringed with small needle teeth around the edge, no eyes, no nose. Long bony arms with
> four long fingers, thin legs. Dead, lightless, emaciated. Plain light grey background, soft even studio lighting, no
> shadows, whole body in frame from head to feet, no props, no text. Realistic 3D horror game creature style.

**Design note:** the Hollow's tell is a **visor that reflects nothing**. The reveal pays it off: there was never a face
behind that visor, only a hole that swallows light. The cavity must be a **flat deep black** in the texture (it must stay
black under the ship's landing lights). Pale and colourless so it reads differently from the orange Mimic in the Field
Journal. Its moment is the face: up close in the horror sequence and pressed against the canopy.

### 4. Knocker — `knocker.png` (2.2 m, heavy)

> Full-body concept art of a horrifying alien creature, standing in an A-pose, arms straight and angled 40 degrees away from
> the body, legs apart, facing the camera, front view. A hulking hunched humanoid, 2.2 metres, very broad and heavy, with a
> small low head sunk between huge shoulders. Its forearms are enormous and end in heavy fused knuckle-clubs of dark bone
> instead of hands, with three short thick claws. Thick dark brown-red hide with overlapping armour-like bony plates on the
> back and shoulders, scarred. Small deep-set yellowish eyes, a wide lipless mouth. Plain light grey background, soft even
> studio lighting, no shadows, whole body in frame from head to feet, no props, no text. Realistic 3D horror game creature
> style.

**Design note:** the Knocker's tell is the **wrong knock** (extra knock, wrong rhythm, from the wrong place on the hull).
Its body is built for pounding: the club-hands are what was knocking. The silhouette is the opposite of the Mimic's: short,
wide, heavy. On the canopy it should look like it can break the glass. Keep the clubs **apart from the body** in the
image so they can be rigged as hands.

### 5. Stalker — `stalker.png` (2.3 m, stands hunched)

> Full-body concept art of a horrifying alien predator, standing upright in an A-pose, arms straight and angled 40 degrees
> away from the body, legs slightly apart, facing the camera, front view. Lean, sinewy humanoid, 2.3 metres, built like a
> hunting animal: long powerful legs with an extra backward-bent joint like a dog's hind legs but with the feet still under
> the hips, long arms with hooked claws, a narrow chest. A long low eyeless head stretched forward, with rows of small dark
> sensing pits along the snout and a long jaw. Dusty sand-brown and grey mottled skin, the colour of desert rock, so it
> blends into the canyon. Plain light grey background, soft even studio lighting, no shadows, whole body in frame from head
> to feet, no props, no text. Realistic 3D horror game creature style.

**Design note:** the Stalker's tell is the **way it comes**: it circles the ship, drops off the radar, turns up from
behind. It is a hunter: rock-coloured (hard to see on the canyon floor), low and fast in its crouch-walk and crawl. The
backward-bent legs make it feel wrong, but the feet stay under the hips so Mixamo's humanoid skeleton still fits (the knee
marker goes on the main bend).

### 6. Sleeper — `sleeper.png` (1.9 m)

> Full-body concept art of a horrifying alien creature, standing in an A-pose, arms straight and angled 40 degrees away from
> the body, legs slightly apart, facing the camera, front view. Almost human, 1.9 metres, slim and pale. It wears the faded
> remains of a burnt-orange flight suit that has grown into its smooth grey waxy skin, as if suit and body are one. A single
> closed vertical seam runs down the centre of its face and chest from the chin to the stomach, like a long closed zipper of
> pale ridged skin. A bald grey human-like head with milky white eyes and a too-calm expression. Quiet, patient, eerie. Plain
> light grey background, soft even studio lighting, no shadows, whole body in frame from head to feet, no props, no text.
> Realistic 3D game creature style.

(Fix 3.1: the first version was refused by the image tool's violence filter; this one shows the seam closed. The game
animates it opening. If a prompt is refused, drop "chin to the stomach" here, or "needle teeth" in the Hollow's.)

**Design note:** the Sleeper passes every check at the airlock and rides in the cabin as a pilot, then turns in flight.
So its reveal form is the **most human** of the five: it was a pilot once, or wore one. The horror is the split seam from
chin to stomach opening. It is seen mostly **sitting in the cabin on the cabin camera** (its tell: off sounds, a glimpse)
and coming through the cockpit hatch, so the head and the seam must read well in a small dim picture.

---

## Stage 3 — Meshy (Code, for reference)

Image-to-3D from each concept with `MESHY_API_KEY` (never written down anywhere): textured PBR, A-pose, triangle budget
pilot ~15-20k, creatures ~20-30k; LODs at ~50 % and ~20 % in Blender. Saved to `SourceArt/Meshy/<name>/`, scaled to the
heights above, origin at the feet, facing +Z. **Code then gives Dan the exact FBX paths to upload to Mixamo.**

Done (2026-11-04): `python Tools/meshy_generate.py` (30 credits a model, Meshy "latest", A-pose, PBR, lighting removed) and
`blender --background --factory-startup --python Tools/Blender/meshy_cleanup.py -- <names>`. Renders: `SourceArt/Meshy/<name>/render_front|side|34.png`
and the sheets `SourceArt/Meshy/sheet_front|34|side.png`. LODs are made in stage 5 from Mixamo's rigged file (so they keep the
skin weights). **Upload these to Mixamo:**

| Model | File to upload | Triangles | Height |
|---|---|---|---|
| Pilot | `SourceArt/Meshy/pilot/pilot_mixamo.fbx` | 18.7k | 1.8 m |
| Mimic | `SourceArt/Meshy/mimic/mimic_mixamo.fbx` | 26.1k | 2.5 m |
| Hollow | `SourceArt/Meshy/hollow/hollow_mixamo.fbx` | 26.1k | 2.1 m |
| Knocker | `SourceArt/Meshy/knocker/knocker_mixamo.fbx` | 29.2k | 2.2 m |
| Stalker | `SourceArt/Meshy/stalker/stalker_mixamo.fbx` | 26.1k | 2.3 m |
| Sleeper | `SourceArt/Meshy/sleeper/sleeper_mixamo.fbx` | 25.9k | 1.9 m |

## Stage 4 — Mixamo animations (Dan)

For each model: upload the FBX Code gives you, auto-rig (Skeleton LOD: **Standard**, Symmetry on; for the creatures put the
markers where the shape suggests: chin under the jaw, knees on the main leg bend). Then download each animation below into
**`SourceArt/Mixamo/<name>/<file>`**, using the file name in the first column.

Download settings: **Format: FBX for Unity (.fbx)**, **30 frames per second**, keyframe reduction **none**. The **first
file of each model "With Skin"**, all others **"Without Skin"**. Tick **"In Place"** wherever it is offered (walks, runs,
crawls). Mixamo's names change now and then: if a search finds nothing, take the closest match and keep my file name.

### Pilot — `SourceArt/Mixamo/pilot/`

| File | Mixamo search | For |
|---|---|---|
| `idle.fbx` (**With Skin**) | Breathing Idle | standing by the wreck, waiting |
| `look_around.fbx` | Looking Around | waiting by the wreck, scanning the sky |
| `wave.fbx` | Waving (both arms over the head) | signalling the ship as it comes in to land |
| `run.fbx` | Running (In Place) | the run over the ground to the airlock |
| `run_stop.fbx` | Run To Stop | pulling up at the door |
| `knock.fbx` | Knocking (fallback: a one-hand Punching at chest height) | knocking on the hull; Code times each hit to a knock sound (the Knocker's tell is the rhythm) |
| `climb_in.fbx` | Climbing Ladder (fallback: Climbing Up Wall) | climbing up into the airlock |
| `sit_idle.fbx` | Sitting Idle | riding in the cabin (cabin camera) |
| `death.fbx` | Dying (falls backwards) | killed by the engine exhaust; killed by the Sleeper |
| `flinch.fbx` | Hit Reaction (or Flinching) | flinching from the exhaust / the engines starting |

### Each creature's rigged model — `SourceArt/Mixamo/<mimic|hollow|knocker|stalker|sleeper>/idle.fbx`

One download per creature, **With Skin**: Mutant Breathing Idle (or Zombie Idle), saved as `idle.fbx` in its own folder.

### The creature animations, once — `SourceArt/Mixamo/creature/`

(Dan, 2026-11-04: Mixamo gives every auto-rigged model the same skeleton, and Unity's Humanoid retargeting plays any clip
on any model, so these are downloaded **once, Without Skin**, from any one rigged creature — the Hollow is the most
standard body. Code adjusts in the game where a clip fits a body badly.)

| File | Mixamo search | For |
|---|---|---|
| `roar.fbx` | Mutant Roaring (or Zombie Scream) | the reveal moment (the disguise drops) |
| `walk.fbx` | Mutant Walking (or Zombie Walk), In Place | creepy approach to the ship |
| `run.fbx` | Mutant Run (or Zombie Running), In Place | lurching run at the ship |
| `crawl.fbx` | Running Crawl (or Zombie Crawl), In Place | crawling over the hull / canopy, the cabin floor |
| `climb.fbx` | Climbing Up Wall | climbing up onto the canopy |
| `attack.fbx` | Mutant Swiping (or Zombie Attack) | clawing at the canopy glass, breaking in |
| `bite.fbx` | Zombie Biting | the lunge at the player in the horror sequence |
| `hit.fbx` | Zombie Reaction Hit (or Hit Reaction) | thrown off the canopy, hit by the exhaust |
| `death.fbx` | Mutant Dying | burnt by the exhaust, killed by the pilots / security |

Plus the extras for each type, **Without Skin**, also once each (from that type's model if you can, so they fit its body),
in that type's own folder (`SourceArt/Mixamo/<type>/`):

| Model | File | Mixamo search | For |
|---|---|---|---|
| Mimic | `leap.fbx` | Mutant Jumping (or Jump Attack) | its long-limbed leap onto the canopy |
| Hollow | `stagger.fbx` (optional) | Injured Walk (or Drunk Walking), In Place; if neither: skip, the game slows the shared walk | its slow dead-thing approach |
| Knocker | `pound.fbx` | Zombie Punching (fallback: Punching Bag) | pounding the hull and the canopy with the club-hands |
| Stalker | `crouch_walk.fbx` | Crouched Walking (or Sneak Walk), In Place | the low hunting approach |
| Stalker | `circle.fbx` | Crouched Sneaking Left (In Place) | circling the ship before it comes to the door |
| Sleeper | `sit_idle.fbx` | Sitting Idle | sitting quietly in the cabin as a "pilot" |
| Sleeper | `sit_twitch.fbx` | Sitting Disbelief (or any sitting twitch / head shake) | the cabin-camera glimpse that something is wrong |
| Sleeper | `lunge.fbx` | Mutant Jump Attack (or Sit To Stand, fast) | the sudden turn: up off the seat and at the next pilot / the hatch |

That is 10 for the pilot, 5 rigged creatures, 9 shared creature clips and 8 extras (32 downloads). When they're all saved, tell Code: it checks the
folders and starts Stage 5.
