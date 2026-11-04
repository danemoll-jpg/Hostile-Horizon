# Hostile Horizon — character models needed (Fix 3.1)

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

### Each creature — `SourceArt/Mixamo/<mimic|hollow|knocker|stalker|sleeper>/`

The same ten for all five creatures:

| File | Mixamo search | For |
|---|---|---|
| `idle.fbx` (**With Skin**) | Mutant Breathing Idle (or Zombie Idle) | standing, breathing, twitching after the reveal |
| `roar.fbx` | Mutant Roaring (or Zombie Scream) | the reveal moment (the disguise drops) |
| `walk.fbx` | Mutant Walking (or Zombie Walk), In Place | creepy approach to the ship |
| `run.fbx` | Mutant Run (or Zombie Running), In Place | lurching run at the ship |
| `crawl.fbx` | Running Crawl (or Zombie Crawl), In Place | crawling over the hull / canopy, the cabin floor |
| `climb.fbx` | Climbing Up Wall | climbing up onto the canopy |
| `attack.fbx` | Mutant Swiping (or Zombie Attack) | clawing at the canopy glass, breaking in |
| `bite.fbx` | Zombie Biting | the lunge at the player in the horror sequence |
| `hit.fbx` | Zombie Reaction Hit (or Hit Reaction) | thrown off the canopy, hit by the exhaust |
| `death.fbx` | Mutant Dying | burnt by the exhaust, killed by the pilots / security |

Plus the extras for its type, in the same folder:

| Model | File | Mixamo search | For |
|---|---|---|---|
| Mimic | `leap.fbx` | Mutant Jumping (or Jump Attack) | its long-limbed leap onto the canopy |
| Hollow | `stagger.fbx` | Zombie Stumbling (or Drunk Walk), In Place | its slow dead-thing approach |
| Knocker | `pound.fbx` | Zombie Punching (fallback: Punching Bag) | pounding the hull and the canopy with the club-hands |
| Stalker | `crouch_walk.fbx` | Crouched Walking (or Sneak Walk), In Place | the low hunting approach |
| Stalker | `circle.fbx` | Crouched Sneaking Left (In Place) | circling the ship before it comes to the door |
| Sleeper | `sit_idle.fbx` | Sitting Idle | sitting quietly in the cabin as a "pilot" |
| Sleeper | `sit_twitch.fbx` | Sitting Disbelief (or any sitting twitch / head shake) | the cabin-camera glimpse that something is wrong |
| Sleeper | `lunge.fbx` | Mutant Jump Attack (or Sit To Stand, fast) | the sudden turn: up off the seat and at the next pilot / the hatch |

That is 10 for the pilot and 10-12 per creature (about 63 downloads). When they're all saved, tell Code: it checks the
folders and starts Stage 5.
