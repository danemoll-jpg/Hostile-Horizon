# Hostile Horizon — voices (Round 4, Part C)

Made by `python Tools/make_voices.py` (ElevenLabs text-to-speech, model `eleven_multilingual_v2`; key from `ELEVENLABS_API_KEY`, never
printed or saved). Files: `Assets/Resources/Voices/<id>.mp3`; the subtitles and line lists the game uses are in
`Assets/Resources/Voices/lines.json` (written by the same script). Code: `Assets/Scripts/Audio/Voices.cs`.
Re-make a line: `python Tools/make_voices.py --only <id> --force` (or a prefix: `--only p3_`). To replace one by hand, drop a
file with the same name in that folder and rebuild.

## How they sound in the game

- **Control** and the **pilots on the radio**: a radio filter (band-limited 350-3200 Hz, a little drive, light crackle) with a
  click in and a click out.
- **The ship's computer**: clean and close (2D, no filter); made with a light chorus for a synthetic edge.
- **A pilot at the airlock**: muffled through the hull (low-pass ~900 Hz), from the door, in 3D.
- **Cabin chatter / screams**: from the cabin behind you, through the bulkhead.
- One line at a time: a queue with priorities (computer warnings first, then Control, then pilots); music ducks under them.
- Subtitles (Settings > Sound > Subtitles, On by default) at the bottom of the HUD and on the cockpit's centre display.

## The aliens use the voices too

An alien disguised as a pilot also radios its mayday, shouts "I see you" and talks at the airlock, in a pilot's voice, so a
voice proves nothing. **The Mimic's voice sign** (some of the time, seeded; Round 3's tell rules): its mayday and its voice at
the door are subtly wrong (a word repeats, a breath in the wrong place: the `*_wrong` lines), or it radios with the callsign
and voice of a pilot already lost on that planet. No other type has a voice sign.

## Voices

| Speaker | ElevenLabs voice | Who |
|---|---|---|
| Control | Daniel (premade) | Control: the mothership's flight controller. Calm, dry, seen it all. |
| Computer | River (premade) | The ship's computer: short, level, synthetic. |
| Pilot Hale (`p0`) | Sarah (premade) | woman, young, American |
| Pilot Kade (`p1`) | Charlie (premade) | man, young, Australian |
| Pilot Penrose (`p2`) | George (premade) | man, middle-aged, British |
| Pilot Vasquez (`p3`) | Matilda (premade) | woman, middle-aged, American |
| Pilot Marlow (`p4`) | Harry (premade) | man, young, rough, American |
| Pilot Ashby (`p5`) | Alice (premade) | woman, middle-aged, British |
| Pilot Tate (`p6`) | Ricky Johnson (library) | man, young, US Southern |
| Pilot Okafor (`p7`) | Jessica (premade) | woman, young, American |
| Pilot Grange (`p8`) | Bill (premade) | man, older, American |
| Pilot Quill (`p9`) | Lily (premade) | woman, middle-aged, British |
| Pilot Brannock (`p10`) | Callum (premade) | man, middle-aged, husky, American |
| Pilot Lund (`p11`) | Will (premade) | man, young, American |

## Every line

| File | Speaker | When | Line |
|---|---|---|---|
| `ctl_launch_1.mp3` | Control | launch | Bay doors open. You're clear to drop. |
| `ctl_launch_2.mp3` | Control | launch | Clamps released. Go and find our people. |
| `ctl_day.mp3` | Control | planet of the day | This one's the planet of the day. The corps pays half again for anyone you bring home from here. |
| `ctl_brief_canyons.mp3` | Control | new planet: Canyons | New world. Deep canyons, thin air, and beacons down in the cracks. Stay low and mind the walls. |
| `ctl_brief_forest.mp3` | Control | new planet: Giant Forest | Forest world. Trees the size of towers, and a canopy you can't see through. Our people are in the clearings. |
| `ctl_brief_city.mp3` | Control | new planet: Dead City | There's a dead city down there. Nobody's home. Except our pilots, in the streets and on the roofs. |
| `ctl_brief_ice.mp3` | Control | new planet: Ice | Ice world. Crevasses, white-outs, and cold enough to crack a hull. When the snow comes in, trust your radar. |
| `ctl_brief_volcanic.mp3` | Control | new planet: Volcanic | Volcanic world. Lava down in the valleys, ash in the air. Our people are up on the cooled shelves. Do not touch the lava. |
| `ctl_brief_crystal.mp3` | Control | new planet: Crystal Forest | Crystal forest down there. Spires as tall as towers. The big ones will stop you dead. The small ones break. |
| `ctl_brief_ocean.mp3` | Control | new planet: Ocean Islands | Ocean world. Islands, sea stacks, arches. Our pilots are on the islands. Skim the water if you like. Just don't dive into it. |
| `ctl_launch_volcanic.mp3` | Control | launch: Volcanic | Volcanic below. Keep your height over the lava. |
| `ctl_launch_crystal.mp3` | Control | launch: Crystal Forest | Crystal forest below. Mind the spires. |
| `ctl_launch_ocean.mp3` | Control | launch: Ocean Islands | Open sea below. Stay off the water. |
| `ctl_found_1.mp3` | Control | a beacon on the radar | Beacon on your scope. That's one of ours. |
| `ctl_found_2.mp3` | Control | a beacon on the radar | Another beacon. Bring them home. |
| `ctl_full.mp3` | Control | cabin full | Cabin's full. Climb to dock height and we'll take it from there. |
| `ctl_everyone.mp3` | Control | no one left waiting | That's everyone down there. Climb up and come home. |
| `ctl_dock_1.mp3` | Control | docking clearance | I have you. Hold steady, bringing you in. |
| `ctl_dock_2.mp3` | Control | docking clearance | Docking clearance granted. Easy does it. |
| `ctl_docked_1.mp3` | Control | docked | Clamps on. Welcome home. |
| `ctl_docked_2.mp3` | Control | docked | You're in. Get them to the infirmary. |
| `ctl_tow.mp3` | Control | tow | You're out of power. Tow beam's on its way. That's going on your bill. |
| `ctl_recall.mp3` | Control | recall | Recall received. We're coming to get you. It isn't free. |
| `ctl_cleared.mp3` | Control | planet cleared | That's the last of them. Planet's clear. Good work down there. |
| `ctl_jump.mp3` | Control | jump | Jump drive spooling. Hold on to something. |
| `ctl_regen_1.mp3` | Control | regeneration | Welcome back. Again. |
| `ctl_regen_2.mp3` | Control | regeneration | Welcome back. Try to keep this body in one piece. |
| `ctl_regen_3.mp3` | Control | regeneration | You're awake. Good. Take a minute. Then get back out there. |
| `ctl_engines_off.mp3` | Control | landed near a pilot, engines still on | Cut your engines. They won't come near a hot ship. |
| `ctl_low_energy.mp3` | Control | energy low | Watch your energy. I can't tow everyone. |
| `ctl_alien_killed.mp3` | Control | an alien burnt by the exhaust | That was no pilot. Good call. |
| `ctl_pilot_killed.mp3` | Control | a real pilot burnt by the exhaust | You just burned one of ours. That comes out of your pay. |
| `ctl_security.mp3` | Control | security kills a Sleeper in the bay | Security's dealt with your passenger. Don't ask. |
| `ctl_lost_contact.mp3` | Control | the ship lost | I've lost your signal. Regeneration bay, stand by. |
| `ctl_hot.mp3` | Control | launch: the planet has turrets | Heads up, this one's hot. Gun turrets on the high ground. Keep low and they can't see you. |
| `ctl_hot_saucers.mp3` | Control | launch: turrets and saucers | Heads up, this one's hot. Turrets on the high ground, and saucers in the air. Stay low, keep moving. |
| `ctl_distress.mp3` | Control | at the dock: new distress calls on an old planet | New distress calls coming in from a planet you'd cleared. More of ours went down there. It's on the star map. |
| `ctl_upgrade.mp3` | Control | the hangar: an upgrade fitted | Workshop says she's ready. Try not to bend it. |
| `cpu_energy_low.mp3` | Computer | ship's computer | Energy low. |
| `cpu_return_now.mp3` | Computer | ship's computer | Return now. |
| `cpu_cant_reach.mp3` | Computer | ship's computer | Mothership out of reach. |
| `cpu_pull_up.mp3` | Computer | ship's computer | Pull up. |
| `cpu_hull_damage.mp3` | Computer | ship's computer | Hull damage. |
| `cpu_hull_critical.mp3` | Computer | ship's computer | Hull critical. |
| `cpu_gear_down.mp3` | Computer | ship's computer | Landing gear down. |
| `cpu_airlock_open.mp3` | Computer | ship's computer | Airlock open. |
| `cpu_pilot_aboard.mp3` | Computer | ship's computer | Pilot aboard. |
| `cpu_cabin_full.mp3` | Computer | ship's computer | Cabin full. |
| `cpu_intruder.mp3` | Computer | ship's computer | Intruder alert. Intruder alert. |
| `cpu_engines_off.mp3` | Computer | ship's computer | Engines off. |
| `cpu_whiteout.mp3` | Computer | ship's computer | Visibility low. Instruments only. |
| `cpu_hull_heat.mp3` | Computer | ship's computer | Hull heat. Climb. |
| `cpu_locked_on.mp3` | Computer | ship's computer | Warning. Locked on. |
| `cpu_saucer.mp3` | Computer | ship's computer | Saucer. Saucer. |
| `cpu_shield_up.mp3` | Computer | ship's computer | Shield up. |
| `cpu_shield_down.mp3` | Computer | ship's computer | Shield down. |
| `cpu_target_down.mp3` | Computer | ship's computer | Target destroyed. |
| `p0_mayday.mp3` | Hale | mayday (any biome) | Mayday, mayday, this is Corps pilot Hale. I'm down, beacon's on. Is anyone up there? |
| `p0_mayday_canyons.mp3` | Hale | mayday (Canyons) | Mayday, this is Hale. I'm down on a canyon floor. Ship's gone. Please, come and get me. |
| `p0_mayday_forest.mp3` | Hale | mayday (Giant Forest) | Mayday, mayday, Hale here. I came down through the trees. I'm in a clearing, I think. Please hurry. |
| `p0_mayday_city.mp3` | Hale | mayday (Dead City) | This is Corps pilot Hale, I'm down in the city. It's so quiet here. Please, somebody. |
| `p0_mayday_ice.mp3` | Hale | mayday (Ice) | Mayday, this is Hale. I'm down on the ice. It's cold. It's so cold. Come get me. |
| `p0_mayday_volcanic.mp3` | Hale | mayday (Volcanic) | Mayday, this is Hale. I'm on a ledge over the lava. It's getting hot. Please hurry. |
| `p0_mayday_crystal.mp3` | Hale | mayday (Crystal Forest) | Mayday, mayday, Hale here. I'm down among the crystals. They hum. Come and get me. |
| `p0_mayday_ocean.mp3` | Hale | mayday (Ocean Islands) | Mayday, this is Hale. I'm down on an island. Water everywhere. Is anyone up there? |
| `p0_mayday_wrong.mp3` | Hale | mayday, wrong (Mimic sign) | Mayday, mayday, this is... this is Corps pilot Hale. I'm down, I'm down. Beacon's on. Is anyone... up there? |
| `p0_see_1.mp3` | Hale | sees the ship land | I see you! I see you! I'm coming! |
| `p0_see_2.mp3` | Hale | sees the ship land | That's you! Hold on, I'm on my way! |
| `p0_door_1.mp3` | Hale | at the airlock (through the hull) | It's me! Open up! |
| `p0_door_2.mp3` | Hale | at the airlock (through the hull) | Open the hatch! Please, open it! |
| `p0_door_wrong.mp3` | Hale | at the airlock, wrong (Mimic sign) | It's me. It's me. Open... open up. |
| `p0_thanks_1.mp3` | Hale | aboard | Oh, thank you. Thank you. I thought nobody was coming. |
| `p0_thanks_2.mp3` | Hale | aboard | You're a sight for sore eyes. Thanks for the lift. |
| `p0_chat_1.mp3` | Hale | cabin chatter | How long till we dock? |
| `p0_chat_2.mp3` | Hale | cabin chatter | I'm never going back down there. Never. |
| `p0_chat_3.mp3` | Hale | cabin chatter | Does anyone else hear that? |
| `p0_scream.mp3` | Hale | the Sleeper attacks | No! No, get away from me! Get it off! |
| `p1_mayday.mp3` | Kade | mayday (any biome) | Mayday, mayday, this is Corps pilot Kade. I'm down, beacon's on. Is anyone up there? |
| `p1_mayday_canyons.mp3` | Kade | mayday (Canyons) | Mayday, this is Kade. I'm down on a canyon floor. Ship's gone. Please, come and get me. |
| `p1_mayday_forest.mp3` | Kade | mayday (Giant Forest) | Mayday, mayday, Kade here. I came down through the trees. I'm in a clearing, I think. Please hurry. |
| `p1_mayday_city.mp3` | Kade | mayday (Dead City) | This is Corps pilot Kade, I'm down in the city. It's so quiet here. Please, somebody. |
| `p1_mayday_ice.mp3` | Kade | mayday (Ice) | Mayday, this is Kade. I'm down on the ice. It's cold. It's so cold. Come get me. |
| `p1_mayday_volcanic.mp3` | Kade | mayday (Volcanic) | Mayday, this is Kade. I'm on a ledge over the lava. It's getting hot. Please hurry. |
| `p1_mayday_crystal.mp3` | Kade | mayday (Crystal Forest) | Mayday, mayday, Kade here. I'm down among the crystals. They hum. Come and get me. |
| `p1_mayday_ocean.mp3` | Kade | mayday (Ocean Islands) | Mayday, this is Kade. I'm down on an island. Water everywhere. Is anyone up there? |
| `p1_mayday_wrong.mp3` | Kade | mayday, wrong (Mimic sign) | Mayday, mayday, this is... this is Corps pilot Kade. I'm down, I'm down. Beacon's on. Is anyone... up there? |
| `p1_see_1.mp3` | Kade | sees the ship land | I see you! I see you! I'm coming! |
| `p1_see_2.mp3` | Kade | sees the ship land | That's you! Hold on, I'm on my way! |
| `p1_door_1.mp3` | Kade | at the airlock (through the hull) | It's me! Open up! |
| `p1_door_2.mp3` | Kade | at the airlock (through the hull) | Open the hatch! Please, open it! |
| `p1_door_wrong.mp3` | Kade | at the airlock, wrong (Mimic sign) | It's me. It's me. Open... open up. |
| `p1_thanks_1.mp3` | Kade | aboard | Oh, thank you. Thank you. I thought nobody was coming. |
| `p1_thanks_2.mp3` | Kade | aboard | You're a sight for sore eyes. Thanks for the lift. |
| `p1_chat_1.mp3` | Kade | cabin chatter | How long till we dock? |
| `p1_chat_2.mp3` | Kade | cabin chatter | I'm never going back down there. Never. |
| `p1_chat_3.mp3` | Kade | cabin chatter | Does anyone else hear that? |
| `p1_scream.mp3` | Kade | the Sleeper attacks | No! No, get away from me! Get it off! |
| `p2_mayday.mp3` | Penrose | mayday (any biome) | Mayday, mayday, this is Corps pilot Penrose. I'm down, beacon's on. Is anyone up there? |
| `p2_mayday_canyons.mp3` | Penrose | mayday (Canyons) | Mayday, this is Penrose. I'm down on a canyon floor. Ship's gone. Please, come and get me. |
| `p2_mayday_forest.mp3` | Penrose | mayday (Giant Forest) | Mayday, mayday, Penrose here. I came down through the trees. I'm in a clearing, I think. Please hurry. |
| `p2_mayday_city.mp3` | Penrose | mayday (Dead City) | This is Corps pilot Penrose, I'm down in the city. It's so quiet here. Please, somebody. |
| `p2_mayday_ice.mp3` | Penrose | mayday (Ice) | Mayday, this is Penrose. I'm down on the ice. It's cold. It's so cold. Come get me. |
| `p2_mayday_volcanic.mp3` | Penrose | mayday (Volcanic) | Mayday, this is Penrose. I'm on a ledge over the lava. It's getting hot. Please hurry. |
| `p2_mayday_crystal.mp3` | Penrose | mayday (Crystal Forest) | Mayday, mayday, Penrose here. I'm down among the crystals. They hum. Come and get me. |
| `p2_mayday_ocean.mp3` | Penrose | mayday (Ocean Islands) | Mayday, this is Penrose. I'm down on an island. Water everywhere. Is anyone up there? |
| `p2_mayday_wrong.mp3` | Penrose | mayday, wrong (Mimic sign) | Mayday, mayday, this is... this is Corps pilot Penrose. I'm down, I'm down. Beacon's on. Is anyone... up there? |
| `p2_see_1.mp3` | Penrose | sees the ship land | I see you! I see you! I'm coming! |
| `p2_see_2.mp3` | Penrose | sees the ship land | That's you! Hold on, I'm on my way! |
| `p2_door_1.mp3` | Penrose | at the airlock (through the hull) | It's me! Open up! |
| `p2_door_2.mp3` | Penrose | at the airlock (through the hull) | Open the hatch! Please, open it! |
| `p2_door_wrong.mp3` | Penrose | at the airlock, wrong (Mimic sign) | It's me. It's me. Open... open up. |
| `p2_thanks_1.mp3` | Penrose | aboard | Oh, thank you. Thank you. I thought nobody was coming. |
| `p2_thanks_2.mp3` | Penrose | aboard | You're a sight for sore eyes. Thanks for the lift. |
| `p2_chat_1.mp3` | Penrose | cabin chatter | How long till we dock? |
| `p2_chat_2.mp3` | Penrose | cabin chatter | I'm never going back down there. Never. |
| `p2_chat_3.mp3` | Penrose | cabin chatter | Does anyone else hear that? |
| `p2_scream.mp3` | Penrose | the Sleeper attacks | No! No, get away from me! Get it off! |
| `p3_mayday.mp3` | Vasquez | mayday (any biome) | Mayday, mayday, this is Corps pilot Vasquez. I'm down, beacon's on. Is anyone up there? |
| `p3_mayday_canyons.mp3` | Vasquez | mayday (Canyons) | Mayday, this is Vasquez. I'm down on a canyon floor. Ship's gone. Please, come and get me. |
| `p3_mayday_forest.mp3` | Vasquez | mayday (Giant Forest) | Mayday, mayday, Vasquez here. I came down through the trees. I'm in a clearing, I think. Please hurry. |
| `p3_mayday_city.mp3` | Vasquez | mayday (Dead City) | This is Corps pilot Vasquez, I'm down in the city. It's so quiet here. Please, somebody. |
| `p3_mayday_ice.mp3` | Vasquez | mayday (Ice) | Mayday, this is Vasquez. I'm down on the ice. It's cold. It's so cold. Come get me. |
| `p3_mayday_volcanic.mp3` | Vasquez | mayday (Volcanic) | Mayday, this is Vasquez. I'm on a ledge over the lava. It's getting hot. Please hurry. |
| `p3_mayday_crystal.mp3` | Vasquez | mayday (Crystal Forest) | Mayday, mayday, Vasquez here. I'm down among the crystals. They hum. Come and get me. |
| `p3_mayday_ocean.mp3` | Vasquez | mayday (Ocean Islands) | Mayday, this is Vasquez. I'm down on an island. Water everywhere. Is anyone up there? |
| `p3_mayday_wrong.mp3` | Vasquez | mayday, wrong (Mimic sign) | Mayday, mayday, this is... this is Corps pilot Vasquez. I'm down, I'm down. Beacon's on. Is anyone... up there? |
| `p3_see_1.mp3` | Vasquez | sees the ship land | I see you! I see you! I'm coming! |
| `p3_see_2.mp3` | Vasquez | sees the ship land | That's you! Hold on, I'm on my way! |
| `p3_door_1.mp3` | Vasquez | at the airlock (through the hull) | It's me! Open up! |
| `p3_door_2.mp3` | Vasquez | at the airlock (through the hull) | Open the hatch! Please, open it! |
| `p3_door_wrong.mp3` | Vasquez | at the airlock, wrong (Mimic sign) | It's me. It's me. Open... open up. |
| `p3_thanks_1.mp3` | Vasquez | aboard | Oh, thank you. Thank you. I thought nobody was coming. |
| `p3_thanks_2.mp3` | Vasquez | aboard | You're a sight for sore eyes. Thanks for the lift. |
| `p3_chat_1.mp3` | Vasquez | cabin chatter | How long till we dock? |
| `p3_chat_2.mp3` | Vasquez | cabin chatter | I'm never going back down there. Never. |
| `p3_chat_3.mp3` | Vasquez | cabin chatter | Does anyone else hear that? |
| `p3_scream.mp3` | Vasquez | the Sleeper attacks | No! No, get away from me! Get it off! |
| `p4_mayday.mp3` | Marlow | mayday (any biome) | Mayday, mayday, this is Corps pilot Marlow. I'm down, beacon's on. Is anyone up there? |
| `p4_mayday_canyons.mp3` | Marlow | mayday (Canyons) | Mayday, this is Marlow. I'm down on a canyon floor. Ship's gone. Please, come and get me. |
| `p4_mayday_forest.mp3` | Marlow | mayday (Giant Forest) | Mayday, mayday, Marlow here. I came down through the trees. I'm in a clearing, I think. Please hurry. |
| `p4_mayday_city.mp3` | Marlow | mayday (Dead City) | This is Corps pilot Marlow, I'm down in the city. It's so quiet here. Please, somebody. |
| `p4_mayday_ice.mp3` | Marlow | mayday (Ice) | Mayday, this is Marlow. I'm down on the ice. It's cold. It's so cold. Come get me. |
| `p4_mayday_volcanic.mp3` | Marlow | mayday (Volcanic) | Mayday, this is Marlow. I'm on a ledge over the lava. It's getting hot. Please hurry. |
| `p4_mayday_crystal.mp3` | Marlow | mayday (Crystal Forest) | Mayday, mayday, Marlow here. I'm down among the crystals. They hum. Come and get me. |
| `p4_mayday_ocean.mp3` | Marlow | mayday (Ocean Islands) | Mayday, this is Marlow. I'm down on an island. Water everywhere. Is anyone up there? |
| `p4_mayday_wrong.mp3` | Marlow | mayday, wrong (Mimic sign) | Mayday, mayday, this is... this is Corps pilot Marlow. I'm down, I'm down. Beacon's on. Is anyone... up there? |
| `p4_see_1.mp3` | Marlow | sees the ship land | I see you! I see you! I'm coming! |
| `p4_see_2.mp3` | Marlow | sees the ship land | That's you! Hold on, I'm on my way! |
| `p4_door_1.mp3` | Marlow | at the airlock (through the hull) | It's me! Open up! |
| `p4_door_2.mp3` | Marlow | at the airlock (through the hull) | Open the hatch! Please, open it! |
| `p4_door_wrong.mp3` | Marlow | at the airlock, wrong (Mimic sign) | It's me. It's me. Open... open up. |
| `p4_thanks_1.mp3` | Marlow | aboard | Oh, thank you. Thank you. I thought nobody was coming. |
| `p4_thanks_2.mp3` | Marlow | aboard | You're a sight for sore eyes. Thanks for the lift. |
| `p4_chat_1.mp3` | Marlow | cabin chatter | How long till we dock? |
| `p4_chat_2.mp3` | Marlow | cabin chatter | I'm never going back down there. Never. |
| `p4_chat_3.mp3` | Marlow | cabin chatter | Does anyone else hear that? |
| `p4_scream.mp3` | Marlow | the Sleeper attacks | No! No, get away from me! Get it off! |
| `p5_mayday.mp3` | Ashby | mayday (any biome) | Mayday, mayday, this is Corps pilot Ashby. I'm down, beacon's on. Is anyone up there? |
| `p5_mayday_canyons.mp3` | Ashby | mayday (Canyons) | Mayday, this is Ashby. I'm down on a canyon floor. Ship's gone. Please, come and get me. |
| `p5_mayday_forest.mp3` | Ashby | mayday (Giant Forest) | Mayday, mayday, Ashby here. I came down through the trees. I'm in a clearing, I think. Please hurry. |
| `p5_mayday_city.mp3` | Ashby | mayday (Dead City) | This is Corps pilot Ashby, I'm down in the city. It's so quiet here. Please, somebody. |
| `p5_mayday_ice.mp3` | Ashby | mayday (Ice) | Mayday, this is Ashby. I'm down on the ice. It's cold. It's so cold. Come get me. |
| `p5_mayday_volcanic.mp3` | Ashby | mayday (Volcanic) | Mayday, this is Ashby. I'm on a ledge over the lava. It's getting hot. Please hurry. |
| `p5_mayday_crystal.mp3` | Ashby | mayday (Crystal Forest) | Mayday, mayday, Ashby here. I'm down among the crystals. They hum. Come and get me. |
| `p5_mayday_ocean.mp3` | Ashby | mayday (Ocean Islands) | Mayday, this is Ashby. I'm down on an island. Water everywhere. Is anyone up there? |
| `p5_mayday_wrong.mp3` | Ashby | mayday, wrong (Mimic sign) | Mayday, mayday, this is... this is Corps pilot Ashby. I'm down, I'm down. Beacon's on. Is anyone... up there? |
| `p5_see_1.mp3` | Ashby | sees the ship land | I see you! I see you! I'm coming! |
| `p5_see_2.mp3` | Ashby | sees the ship land | That's you! Hold on, I'm on my way! |
| `p5_door_1.mp3` | Ashby | at the airlock (through the hull) | It's me! Open up! |
| `p5_door_2.mp3` | Ashby | at the airlock (through the hull) | Open the hatch! Please, open it! |
| `p5_door_wrong.mp3` | Ashby | at the airlock, wrong (Mimic sign) | It's me. It's me. Open... open up. |
| `p5_thanks_1.mp3` | Ashby | aboard | Oh, thank you. Thank you. I thought nobody was coming. |
| `p5_thanks_2.mp3` | Ashby | aboard | You're a sight for sore eyes. Thanks for the lift. |
| `p5_chat_1.mp3` | Ashby | cabin chatter | How long till we dock? |
| `p5_chat_2.mp3` | Ashby | cabin chatter | I'm never going back down there. Never. |
| `p5_chat_3.mp3` | Ashby | cabin chatter | Does anyone else hear that? |
| `p5_scream.mp3` | Ashby | the Sleeper attacks | No! No, get away from me! Get it off! |
| `p6_mayday.mp3` | Tate | mayday (any biome) | Mayday, mayday, this is Corps pilot Tate. I'm down, beacon's on. Is anyone up there? |
| `p6_mayday_canyons.mp3` | Tate | mayday (Canyons) | Mayday, this is Tate. I'm down on a canyon floor. Ship's gone. Please, come and get me. |
| `p6_mayday_forest.mp3` | Tate | mayday (Giant Forest) | Mayday, mayday, Tate here. I came down through the trees. I'm in a clearing, I think. Please hurry. |
| `p6_mayday_city.mp3` | Tate | mayday (Dead City) | This is Corps pilot Tate, I'm down in the city. It's so quiet here. Please, somebody. |
| `p6_mayday_ice.mp3` | Tate | mayday (Ice) | Mayday, this is Tate. I'm down on the ice. It's cold. It's so cold. Come get me. |
| `p6_mayday_volcanic.mp3` | Tate | mayday (Volcanic) | Mayday, this is Tate. I'm on a ledge over the lava. It's getting hot. Please hurry. |
| `p6_mayday_crystal.mp3` | Tate | mayday (Crystal Forest) | Mayday, mayday, Tate here. I'm down among the crystals. They hum. Come and get me. |
| `p6_mayday_ocean.mp3` | Tate | mayday (Ocean Islands) | Mayday, this is Tate. I'm down on an island. Water everywhere. Is anyone up there? |
| `p6_mayday_wrong.mp3` | Tate | mayday, wrong (Mimic sign) | Mayday, mayday, this is... this is Corps pilot Tate. I'm down, I'm down. Beacon's on. Is anyone... up there? |
| `p6_see_1.mp3` | Tate | sees the ship land | I see you! I see you! I'm coming! |
| `p6_see_2.mp3` | Tate | sees the ship land | That's you! Hold on, I'm on my way! |
| `p6_door_1.mp3` | Tate | at the airlock (through the hull) | It's me! Open up! |
| `p6_door_2.mp3` | Tate | at the airlock (through the hull) | Open the hatch! Please, open it! |
| `p6_door_wrong.mp3` | Tate | at the airlock, wrong (Mimic sign) | It's me. It's me. Open... open up. |
| `p6_thanks_1.mp3` | Tate | aboard | Oh, thank you. Thank you. I thought nobody was coming. |
| `p6_thanks_2.mp3` | Tate | aboard | You're a sight for sore eyes. Thanks for the lift. |
| `p6_chat_1.mp3` | Tate | cabin chatter | How long till we dock? |
| `p6_chat_2.mp3` | Tate | cabin chatter | I'm never going back down there. Never. |
| `p6_chat_3.mp3` | Tate | cabin chatter | Does anyone else hear that? |
| `p6_scream.mp3` | Tate | the Sleeper attacks | No! No, get away from me! Get it off! |
| `p7_mayday.mp3` | Okafor | mayday (any biome) | Mayday, mayday, this is Corps pilot Okafor. I'm down, beacon's on. Is anyone up there? |
| `p7_mayday_canyons.mp3` | Okafor | mayday (Canyons) | Mayday, this is Okafor. I'm down on a canyon floor. Ship's gone. Please, come and get me. |
| `p7_mayday_forest.mp3` | Okafor | mayday (Giant Forest) | Mayday, mayday, Okafor here. I came down through the trees. I'm in a clearing, I think. Please hurry. |
| `p7_mayday_city.mp3` | Okafor | mayday (Dead City) | This is Corps pilot Okafor, I'm down in the city. It's so quiet here. Please, somebody. |
| `p7_mayday_ice.mp3` | Okafor | mayday (Ice) | Mayday, this is Okafor. I'm down on the ice. It's cold. It's so cold. Come get me. |
| `p7_mayday_volcanic.mp3` | Okafor | mayday (Volcanic) | Mayday, this is Okafor. I'm on a ledge over the lava. It's getting hot. Please hurry. |
| `p7_mayday_crystal.mp3` | Okafor | mayday (Crystal Forest) | Mayday, mayday, Okafor here. I'm down among the crystals. They hum. Come and get me. |
| `p7_mayday_ocean.mp3` | Okafor | mayday (Ocean Islands) | Mayday, this is Okafor. I'm down on an island. Water everywhere. Is anyone up there? |
| `p7_mayday_wrong.mp3` | Okafor | mayday, wrong (Mimic sign) | Mayday, mayday, this is... this is Corps pilot Okafor. I'm down, I'm down. Beacon's on. Is anyone... up there? |
| `p7_see_1.mp3` | Okafor | sees the ship land | I see you! I see you! I'm coming! |
| `p7_see_2.mp3` | Okafor | sees the ship land | That's you! Hold on, I'm on my way! |
| `p7_door_1.mp3` | Okafor | at the airlock (through the hull) | It's me! Open up! |
| `p7_door_2.mp3` | Okafor | at the airlock (through the hull) | Open the hatch! Please, open it! |
| `p7_door_wrong.mp3` | Okafor | at the airlock, wrong (Mimic sign) | It's me. It's me. Open... open up. |
| `p7_thanks_1.mp3` | Okafor | aboard | Oh, thank you. Thank you. I thought nobody was coming. |
| `p7_thanks_2.mp3` | Okafor | aboard | You're a sight for sore eyes. Thanks for the lift. |
| `p7_chat_1.mp3` | Okafor | cabin chatter | How long till we dock? |
| `p7_chat_2.mp3` | Okafor | cabin chatter | I'm never going back down there. Never. |
| `p7_chat_3.mp3` | Okafor | cabin chatter | Does anyone else hear that? |
| `p7_scream.mp3` | Okafor | the Sleeper attacks | No! No, get away from me! Get it off! |
| `p8_mayday.mp3` | Grange | mayday (any biome) | Mayday, mayday, this is Corps pilot Grange. I'm down, beacon's on. Is anyone up there? |
| `p8_mayday_canyons.mp3` | Grange | mayday (Canyons) | Mayday, this is Grange. I'm down on a canyon floor. Ship's gone. Please, come and get me. |
| `p8_mayday_forest.mp3` | Grange | mayday (Giant Forest) | Mayday, mayday, Grange here. I came down through the trees. I'm in a clearing, I think. Please hurry. |
| `p8_mayday_city.mp3` | Grange | mayday (Dead City) | This is Corps pilot Grange, I'm down in the city. It's so quiet here. Please, somebody. |
| `p8_mayday_ice.mp3` | Grange | mayday (Ice) | Mayday, this is Grange. I'm down on the ice. It's cold. It's so cold. Come get me. |
| `p8_mayday_volcanic.mp3` | Grange | mayday (Volcanic) | Mayday, this is Grange. I'm on a ledge over the lava. It's getting hot. Please hurry. |
| `p8_mayday_crystal.mp3` | Grange | mayday (Crystal Forest) | Mayday, mayday, Grange here. I'm down among the crystals. They hum. Come and get me. |
| `p8_mayday_ocean.mp3` | Grange | mayday (Ocean Islands) | Mayday, this is Grange. I'm down on an island. Water everywhere. Is anyone up there? |
| `p8_mayday_wrong.mp3` | Grange | mayday, wrong (Mimic sign) | Mayday, mayday, this is... this is Corps pilot Grange. I'm down, I'm down. Beacon's on. Is anyone... up there? |
| `p8_see_1.mp3` | Grange | sees the ship land | I see you! I see you! I'm coming! |
| `p8_see_2.mp3` | Grange | sees the ship land | That's you! Hold on, I'm on my way! |
| `p8_door_1.mp3` | Grange | at the airlock (through the hull) | It's me! Open up! |
| `p8_door_2.mp3` | Grange | at the airlock (through the hull) | Open the hatch! Please, open it! |
| `p8_door_wrong.mp3` | Grange | at the airlock, wrong (Mimic sign) | It's me. It's me. Open... open up. |
| `p8_thanks_1.mp3` | Grange | aboard | Oh, thank you. Thank you. I thought nobody was coming. |
| `p8_thanks_2.mp3` | Grange | aboard | You're a sight for sore eyes. Thanks for the lift. |
| `p8_chat_1.mp3` | Grange | cabin chatter | How long till we dock? |
| `p8_chat_2.mp3` | Grange | cabin chatter | I'm never going back down there. Never. |
| `p8_chat_3.mp3` | Grange | cabin chatter | Does anyone else hear that? |
| `p8_scream.mp3` | Grange | the Sleeper attacks | No! No, get away from me! Get it off! |
| `p9_mayday.mp3` | Quill | mayday (any biome) | Mayday, mayday, this is Corps pilot Quill. I'm down, beacon's on. Is anyone up there? |
| `p9_mayday_canyons.mp3` | Quill | mayday (Canyons) | Mayday, this is Quill. I'm down on a canyon floor. Ship's gone. Please, come and get me. |
| `p9_mayday_forest.mp3` | Quill | mayday (Giant Forest) | Mayday, mayday, Quill here. I came down through the trees. I'm in a clearing, I think. Please hurry. |
| `p9_mayday_city.mp3` | Quill | mayday (Dead City) | This is Corps pilot Quill, I'm down in the city. It's so quiet here. Please, somebody. |
| `p9_mayday_ice.mp3` | Quill | mayday (Ice) | Mayday, this is Quill. I'm down on the ice. It's cold. It's so cold. Come get me. |
| `p9_mayday_volcanic.mp3` | Quill | mayday (Volcanic) | Mayday, this is Quill. I'm on a ledge over the lava. It's getting hot. Please hurry. |
| `p9_mayday_crystal.mp3` | Quill | mayday (Crystal Forest) | Mayday, mayday, Quill here. I'm down among the crystals. They hum. Come and get me. |
| `p9_mayday_ocean.mp3` | Quill | mayday (Ocean Islands) | Mayday, this is Quill. I'm down on an island. Water everywhere. Is anyone up there? |
| `p9_mayday_wrong.mp3` | Quill | mayday, wrong (Mimic sign) | Mayday, mayday, this is... this is Corps pilot Quill. I'm down, I'm down. Beacon's on. Is anyone... up there? |
| `p9_see_1.mp3` | Quill | sees the ship land | I see you! I see you! I'm coming! |
| `p9_see_2.mp3` | Quill | sees the ship land | That's you! Hold on, I'm on my way! |
| `p9_door_1.mp3` | Quill | at the airlock (through the hull) | It's me! Open up! |
| `p9_door_2.mp3` | Quill | at the airlock (through the hull) | Open the hatch! Please, open it! |
| `p9_door_wrong.mp3` | Quill | at the airlock, wrong (Mimic sign) | It's me. It's me. Open... open up. |
| `p9_thanks_1.mp3` | Quill | aboard | Oh, thank you. Thank you. I thought nobody was coming. |
| `p9_thanks_2.mp3` | Quill | aboard | You're a sight for sore eyes. Thanks for the lift. |
| `p9_chat_1.mp3` | Quill | cabin chatter | How long till we dock? |
| `p9_chat_2.mp3` | Quill | cabin chatter | I'm never going back down there. Never. |
| `p9_chat_3.mp3` | Quill | cabin chatter | Does anyone else hear that? |
| `p9_scream.mp3` | Quill | the Sleeper attacks | No! No, get away from me! Get it off! |
| `p10_mayday.mp3` | Brannock | mayday (any biome) | Mayday, mayday, this is Corps pilot Brannock. I'm down, beacon's on. Is anyone up there? |
| `p10_mayday_canyons.mp3` | Brannock | mayday (Canyons) | Mayday, this is Brannock. I'm down on a canyon floor. Ship's gone. Please, come and get me. |
| `p10_mayday_forest.mp3` | Brannock | mayday (Giant Forest) | Mayday, mayday, Brannock here. I came down through the trees. I'm in a clearing, I think. Please hurry. |
| `p10_mayday_city.mp3` | Brannock | mayday (Dead City) | This is Corps pilot Brannock, I'm down in the city. It's so quiet here. Please, somebody. |
| `p10_mayday_ice.mp3` | Brannock | mayday (Ice) | Mayday, this is Brannock. I'm down on the ice. It's cold. It's so cold. Come get me. |
| `p10_mayday_volcanic.mp3` | Brannock | mayday (Volcanic) | Mayday, this is Brannock. I'm on a ledge over the lava. It's getting hot. Please hurry. |
| `p10_mayday_crystal.mp3` | Brannock | mayday (Crystal Forest) | Mayday, mayday, Brannock here. I'm down among the crystals. They hum. Come and get me. |
| `p10_mayday_ocean.mp3` | Brannock | mayday (Ocean Islands) | Mayday, this is Brannock. I'm down on an island. Water everywhere. Is anyone up there? |
| `p10_mayday_wrong.mp3` | Brannock | mayday, wrong (Mimic sign) | Mayday, mayday, this is... this is Corps pilot Brannock. I'm down, I'm down. Beacon's on. Is anyone... up there? |
| `p10_see_1.mp3` | Brannock | sees the ship land | I see you! I see you! I'm coming! |
| `p10_see_2.mp3` | Brannock | sees the ship land | That's you! Hold on, I'm on my way! |
| `p10_door_1.mp3` | Brannock | at the airlock (through the hull) | It's me! Open up! |
| `p10_door_2.mp3` | Brannock | at the airlock (through the hull) | Open the hatch! Please, open it! |
| `p10_door_wrong.mp3` | Brannock | at the airlock, wrong (Mimic sign) | It's me. It's me. Open... open up. |
| `p10_thanks_1.mp3` | Brannock | aboard | Oh, thank you. Thank you. I thought nobody was coming. |
| `p10_thanks_2.mp3` | Brannock | aboard | You're a sight for sore eyes. Thanks for the lift. |
| `p10_chat_1.mp3` | Brannock | cabin chatter | How long till we dock? |
| `p10_chat_2.mp3` | Brannock | cabin chatter | I'm never going back down there. Never. |
| `p10_chat_3.mp3` | Brannock | cabin chatter | Does anyone else hear that? |
| `p10_scream.mp3` | Brannock | the Sleeper attacks | No! No, get away from me! Get it off! |
| `p11_mayday.mp3` | Lund | mayday (any biome) | Mayday, mayday, this is Corps pilot Lund. I'm down, beacon's on. Is anyone up there? |
| `p11_mayday_canyons.mp3` | Lund | mayday (Canyons) | Mayday, this is Lund. I'm down on a canyon floor. Ship's gone. Please, come and get me. |
| `p11_mayday_forest.mp3` | Lund | mayday (Giant Forest) | Mayday, mayday, Lund here. I came down through the trees. I'm in a clearing, I think. Please hurry. |
| `p11_mayday_city.mp3` | Lund | mayday (Dead City) | This is Corps pilot Lund, I'm down in the city. It's so quiet here. Please, somebody. |
| `p11_mayday_ice.mp3` | Lund | mayday (Ice) | Mayday, this is Lund. I'm down on the ice. It's cold. It's so cold. Come get me. |
| `p11_mayday_volcanic.mp3` | Lund | mayday (Volcanic) | Mayday, this is Lund. I'm on a ledge over the lava. It's getting hot. Please hurry. |
| `p11_mayday_crystal.mp3` | Lund | mayday (Crystal Forest) | Mayday, mayday, Lund here. I'm down among the crystals. They hum. Come and get me. |
| `p11_mayday_ocean.mp3` | Lund | mayday (Ocean Islands) | Mayday, this is Lund. I'm down on an island. Water everywhere. Is anyone up there? |
| `p11_mayday_wrong.mp3` | Lund | mayday, wrong (Mimic sign) | Mayday, mayday, this is... this is Corps pilot Lund. I'm down, I'm down. Beacon's on. Is anyone... up there? |
| `p11_see_1.mp3` | Lund | sees the ship land | I see you! I see you! I'm coming! |
| `p11_see_2.mp3` | Lund | sees the ship land | That's you! Hold on, I'm on my way! |
| `p11_door_1.mp3` | Lund | at the airlock (through the hull) | It's me! Open up! |
| `p11_door_2.mp3` | Lund | at the airlock (through the hull) | Open the hatch! Please, open it! |
| `p11_door_wrong.mp3` | Lund | at the airlock, wrong (Mimic sign) | It's me. It's me. Open... open up. |
| `p11_thanks_1.mp3` | Lund | aboard | Oh, thank you. Thank you. I thought nobody was coming. |
| `p11_thanks_2.mp3` | Lund | aboard | You're a sight for sore eyes. Thanks for the lift. |
| `p11_chat_1.mp3` | Lund | cabin chatter | How long till we dock? |
| `p11_chat_2.mp3` | Lund | cabin chatter | I'm never going back down there. Never. |
| `p11_chat_3.mp3` | Lund | cabin chatter | Does anyone else hear that? |
| `p11_scream.mp3` | Lund | the Sleeper attacks | No! No, get away from me! Get it off! |
