# Hostile Horizon — sounds made with ElevenLabs (Round 3)

Made by `python Tools/make_sounds.py` (ElevenLabs sound effects; key from `ELEVENLABS_API_KEY`). Each file is
`Assets/Resources/Sounds/<name>.mp3`; the game loads it by name (`Assets/Scripts/Audio/Sfx.cs`) and falls back to a
sound made in code if the file is missing. To replace one: generate it again (`--only <name> --force`), or drop your
own file with the same name (mp3 / ogg / wav) into that folder and rebuild.

| File | Length | Loop | For | Prompt |
|---|---|---|---|---|
| `crystal_shatter.mp3` | 2 s | no | a thin crystal shard breaks when the ship flies through it | A tall thin crystal shard shattering: a bright glassy crack, then a cascade of ringing chime-like tinkles as glass pieces fall. No music. |
| `water_splash.mp3` | 1.5 s | no | the ship skims or slaps the sea | A fast aircraft skimming and slapping the surface of the sea: a sharp watery splash and a hiss of spray. No music. |
| `alien_shriek.mp3` | 2.5 s | no | an alien reveals itself (engines started on it, thrown off the canopy) | A horrifying inhuman creature shriek, wet and rasping, rising to a piercing screech then choking off. Close up, dry, no music. |
| `alien_death.mp3` | 3 s | no | an alien dies (burnt by the exhaust, killed in the cabin) | A monstrous creature dying in agony: gurgling screech, wet choking, a body collapsing, a burning hiss. No music. |
| `canopy_scrape.mp3` | 6 s | yes | the alien on the canopy: claws on the glass (loop) | Long hard claws slowly scraping and dragging across thick glass, squeaking and grinding, very close and unsettling. No music. |
| `canopy_breath.mp3` | 5 s | yes | the alien on the canopy: breathing against the glass (loop) | Slow heavy wet breathing of a large creature pressed right against glass, rasping inhale, long fogging exhale, very close. No music. |
| `glass_tap.mp3` | 1.5 s | no | the alien on the canopy taps the glass | Three slow hard taps of a claw on thick glass, close, in a quiet small cockpit. No music. |
| `glass_crack.mp3` | 2 s | no | the canopy cracks before the break-in | Thick reinforced glass cracking under heavy pressure: a sharp splintering creak and a spreading crack. No music. |
| `glass_break.mp3` | 2.5 s | no | the canopy gives way (break-in) | A thick glass canopy shattering inward with force, heavy glass breaking and shards falling into a small cockpit. No music. |
| `cabin_move.mp3` | 6 s | yes | something moving in the cabin behind the cockpit (loop) | Something heavy moving around in a small metal compartment behind a wall: slow thumps, dragging, metal scraping, muffled. No music. |
| `door_hit.mp3` | 1.5 s | no | the cockpit door is hit from the cabin side | A single heavy violent blow on a metal hatch door from the other side: a deep boom with a metal rattle. No music. |
| `door_break.mp3` | 2.5 s | no | the cockpit door gives way | A metal hatch door torn open by force: metal shrieking, hinges snapping, the door crashing to a metal floor. No music. |
| `alarm.mp3` | 3 s | yes | the ship's intruder alarm (loop) | Spaceship interior intruder alarm: a low pulsing two-tone klaxon, urgent but not shrill, steady rhythm. No music. |
| `lights_die.mp3` | 2 s | no | the cabin and cockpit lights die | Electrical power failing in a cockpit: a sharp heavy clunk, a falling electrical whine, systems shutting down into silence. No music. |
| `sleeper_click.mp3` | 4 s | yes | the Sleeper's tell: wet clicking in the cabin (loop) | Faint wet clicking and slow insect-like chittering, quiet, coming from behind a thin wall, unsettling. No music. |
| `sleeper_voice.mp3` | 4 s | yes | the Sleeper's tell: a pilot's voice that loops wrongly (loop) | A tired man whispering "thank you... thank you for coming for me", the words repeating a little wrong, hollow and distorted, from behind a wall. No music. |
| `cabin_kill.mp3` | 3 s | no | the Sleeper kills a pilot in the cabin | A brief violent struggle behind a metal wall: a man's muffled scream cut short, a wet tearing sound, a heavy thud. No music. |
| `pilots_fight.mp3` | 4 s | no | the pilots in the cabin fight the Sleeper off | Several men fighting something in a small metal room behind a wall: shouts, heavy blows, a creature screeching, then silence. No music. |
| `security_shots.mp3` | 3 s | no | mothership security kills the Sleeper in the bay | Short bursts of energy weapon fire inside a metal hangar bay: sharp electric zaps, a creature shriek, echo. No music. |
| `creature_lunge.mp3` | 2 s | no | the creature comes at the player in the cockpit | A monster roaring right in your face: a sudden lunge, guttural and wet, extremely close. No music. |
| `gun_fire.mp3` | 0.5 s | no | the ship's gun, one shot | A single short punchy sci-fi energy cannon shot from a small spacecraft, a tight zap with a low thump. Dry, no music. |
| `turret_fire.mp3` | 1 s | no | a ground turret fires a bolt | A heavy alien energy cannon firing one bolt: a deep charged thump and a crackling electric whoosh. Outdoors, no music. |
| `saucer_fire.mp3` | 0.6 s | no | a saucer fires a bolt | A flying saucer firing a single plasma shot: a high warbling electric zap. No music. |
| `bolt_hit_hull.mp3` | 1 s | no | an enemy bolt hits the ship's hull | A plasma bolt slamming into a spaceship's metal hull from inside the cockpit: a loud metallic bang, sizzle and rattle. No music. |
| `bolt_hit_shield.mp3` | 0.8 s | no | an enemy bolt hits the shield | An energy bolt hitting a force field: a deep resonant electric thrum with a fizzing crackle. No music. |
| `bolt_hit_metal.mp3` | 0.6 s | no | your bolt hits a turret or saucer | A small energy bolt hitting armoured metal at a distance: a sharp clang with sparks. No music. |
| `shield_on.mp3` | 1.2 s | no | the shield comes on | A spaceship force field powering up: a rising electric hum that settles into a steady tone. No music. |
| `shield_off.mp3` | 1 s | no | the shield goes off | A force field powering down: a falling electric hum that fades out. No music. |
| `shield_hum.mp3` | 4 s | yes | the shield while it is on (loop) | A steady low electric force field hum with a faint shimmer, heard from inside a small cockpit. Seamless, no music. |
| `enemy_explosion.mp3` | 3 s | no | a turret or saucer blows up | A heavy machine exploding: a big blast, metal debris flying and crashing, a burning roar fading. Outdoors, no music. |
| `saucer_hum.mp3` | 4 s | yes | a saucer flying (loop) | An alien flying saucer hovering and moving: a pulsing, wavering electronic drone with a low throb. Seamless, no music. |
| `lock_warning.mp3` | 1.2 s | no | a turret has locked on (cockpit tone) | A spaceship cockpit missile-lock warning: fast urgent electronic beeps, then a steady tone. No music. |
| `hatch_burst.mp3` | 1.5 s | no | the alien slams through the cockpit hatch in one blow (the instant / take-off strike) | A sudden violent slam on a metal hatch right behind you that bursts it open at once: a huge metallic bang, metal tearing, the door crashing in. Very close, no music. |
| `canopy_slam.mp3` | 1.2 s | no | the alien leaps onto the canopy with no warning | A heavy body slamming hard onto a thick glass canopy right above your head: a loud thud and a deep glass shudder and rattle. Very close, no music. |
| `power_dip.mp3` | 1 s | no | a harmless power flicker as the airlock cycles (red herring) | A brief electrical power dip in a small spaceship cockpit: a short buzz, a click, a relay chattering, then normal hum. Quiet, no music. |
| `cabin_thump.mp3` | 1 s | no | a pilot sitting down hard in the cabin (red herring) | A single dull thump and a creak behind a metal wall, like someone dropping heavily into a seat. Muffled, no music. |
| `feed_crackle.mp3` | 1 s | no | static on the cabin camera feed (red herring) | A short burst of electronic video static and digital crackle from a small cockpit screen. Quiet, no music. |
