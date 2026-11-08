"""Hostile Horizon (Round 3, item B11): the scare sounds, made with ElevenLabs' sound-effects API.

The key comes from ELEVENLABS_API_KEY (never printed or saved). Each sound is written to Assets/Resources/Sounds/<name>.mp3, which
the game loads by name (Audio/Sfx.cs falls back to a sound made in code when a file is missing). Docs/SOUNDS.md lists every prompt,
so a sound can be made again or replaced by hand (drop a file with the same name in the folder).
Usage: python Tools/make_sounds.py [--only name,name] [--force] [--list]
Nothing is played: making a sound never makes any noise on this PC (Dan's testing rule).
"""
import argparse, json, os, pathlib, sys, time, urllib.request, urllib.error

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/Sounds'
DOC = ROOT / 'Docs/SOUNDS.md'

# name, seconds, loop, what it is for, prompt
SOUNDS = [
    # Round 7 (B3, B4)
    ('crystal_shatter', 2.0, False, 'a thin crystal shard breaks when the ship flies through it',
     'A tall thin crystal shard shattering: a bright glassy crack, then a cascade of ringing chime-like tinkles as glass pieces fall. No music.'),
    ('water_splash', 1.5, False, 'the ship skims or slaps the sea',
     'A fast aircraft skimming and slapping the surface of the sea: a sharp watery splash and a hiss of spray. No music.'),
    ('alien_shriek', 2.5, False, 'an alien reveals itself (engines started on it, thrown off the canopy)',
     'A horrifying inhuman creature shriek, wet and rasping, rising to a piercing screech then choking off. Close up, dry, no music.'),
    ('alien_death', 3.0, False, 'an alien dies (burnt by the exhaust, killed in the cabin)',
     'A monstrous creature dying in agony: gurgling screech, wet choking, a body collapsing, a burning hiss. No music.'),
    ('canopy_scrape', 6.0, True, 'the alien on the canopy: claws on the glass (loop)',
     'Long hard claws slowly scraping and dragging across thick glass, squeaking and grinding, very close and unsettling. No music.'),
    ('canopy_breath', 5.0, True, 'the alien on the canopy: breathing against the glass (loop)',
     'Slow heavy wet breathing of a large creature pressed right against glass, rasping inhale, long fogging exhale, very close. No music.'),
    ('glass_tap', 1.5, False, 'the alien on the canopy taps the glass',
     'Three slow hard taps of a claw on thick glass, close, in a quiet small cockpit. No music.'),
    ('glass_crack', 2.0, False, 'the canopy cracks before the break-in',
     'Thick reinforced glass cracking under heavy pressure: a sharp splintering creak and a spreading crack. No music.'),
    ('glass_break', 2.5, False, 'the canopy gives way (break-in)',
     'A thick glass canopy shattering inward with force, heavy glass breaking and shards falling into a small cockpit. No music.'),
    ('cabin_move', 6.0, True, 'something moving in the cabin behind the cockpit (loop)',
     'Something heavy moving around in a small metal compartment behind a wall: slow thumps, dragging, metal scraping, muffled. No music.'),
    ('door_hit', 1.5, False, 'the cockpit door is hit from the cabin side',
     'A single heavy violent blow on a metal hatch door from the other side: a deep boom with a metal rattle. No music.'),
    ('door_break', 2.5, False, 'the cockpit door gives way',
     'A metal hatch door torn open by force: metal shrieking, hinges snapping, the door crashing to a metal floor. No music.'),
    ('alarm', 3.0, True, 'the ship\'s intruder alarm (loop)',
     'Spaceship interior intruder alarm: a low pulsing two-tone klaxon, urgent but not shrill, steady rhythm. No music.'),
    ('lights_die', 2.0, False, 'the cabin and cockpit lights die',
     'Electrical power failing in a cockpit: a sharp heavy clunk, a falling electrical whine, systems shutting down into silence. No music.'),
    ('sleeper_click', 4.0, True, 'the Sleeper\'s tell: wet clicking in the cabin (loop)',
     'Faint wet clicking and slow insect-like chittering, quiet, coming from behind a thin wall, unsettling. No music.'),
    ('sleeper_voice', 4.0, True, 'the Sleeper\'s tell: a pilot\'s voice that loops wrongly (loop)',
     'A tired man whispering "thank you... thank you for coming for me", the words repeating a little wrong, hollow and distorted, from behind a wall. No music.'),
    ('cabin_kill', 3.0, False, 'the Sleeper kills a pilot in the cabin',
     'A brief violent struggle behind a metal wall: a man\'s muffled scream cut short, a wet tearing sound, a heavy thud. No music.'),
    ('pilots_fight', 4.0, False, 'the pilots in the cabin fight the Sleeper off',
     'Several men fighting something in a small metal room behind a wall: shouts, heavy blows, a creature screeching, then silence. No music.'),
    ('security_shots', 3.0, False, 'mothership security kills the Sleeper in the bay',
     'Short bursts of energy weapon fire inside a metal hangar bay: sharp electric zaps, a creature shriek, echo. No music.'),
    ('creature_lunge', 2.0, False, 'the creature comes at the player in the cockpit',
     'A monster roaring right in your face: a sudden lunge, guttural and wet, extremely close. No music.'),
    # Round 5 (B10): weapons, shield, impacts, enemies
    ('gun_fire', 0.5, False, 'the ship\'s gun, one shot',
     'A single short punchy sci-fi energy cannon shot from a small spacecraft, a tight zap with a low thump. Dry, no music.'),
    ('turret_fire', 1.0, False, 'a ground turret fires a bolt',
     'A heavy alien energy cannon firing one bolt: a deep charged thump and a crackling electric whoosh. Outdoors, no music.'),
    ('saucer_fire', 0.6, False, 'a saucer fires a bolt',
     'A flying saucer firing a single plasma shot: a high warbling electric zap. No music.'),
    ('bolt_hit_hull', 1.0, False, 'an enemy bolt hits the ship\'s hull',
     'A plasma bolt slamming into a spaceship\'s metal hull from inside the cockpit: a loud metallic bang, sizzle and rattle. No music.'),
    ('bolt_hit_shield', 0.8, False, 'an enemy bolt hits the shield',
     'An energy bolt hitting a force field: a deep resonant electric thrum with a fizzing crackle. No music.'),
    ('bolt_hit_metal', 0.6, False, 'your bolt hits a turret or saucer',
     'A small energy bolt hitting armoured metal at a distance: a sharp clang with sparks. No music.'),
    ('shield_on', 1.2, False, 'the shield comes on',
     'A spaceship force field powering up: a rising electric hum that settles into a steady tone. No music.'),
    ('shield_off', 1.0, False, 'the shield goes off',
     'A force field powering down: a falling electric hum that fades out. No music.'),
    ('shield_hum', 4.0, True, 'the shield while it is on (loop)',
     'A steady low electric force field hum with a faint shimmer, heard from inside a small cockpit. Seamless, no music.'),
    ('enemy_explosion', 3.0, False, 'a turret or saucer blows up',
     'A heavy machine exploding: a big blast, metal debris flying and crashing, a burning roar fading. Outdoors, no music.'),
    ('saucer_hum', 4.0, True, 'a saucer flying (loop)',
     'An alien flying saucer hovering and moving: a pulsing, wavering electronic drone with a low throb. Seamless, no music.'),
    ('lock_warning', 1.2, False, 'a turret has locked on (cockpit tone)',
     'A spaceship cockpit missile-lock warning: fast urgent electronic beeps, then a steady tone. No music.'),
    # Round 6 (A): the jump scare and its red herrings
    ('hatch_burst', 1.5, False, 'the alien slams through the cockpit hatch in one blow (the instant / take-off strike)',
     'A sudden violent slam on a metal hatch right behind you that bursts it open at once: a huge metallic bang, metal tearing, the door crashing in. Very close, no music.'),
    ('canopy_slam', 1.2, False, 'the alien leaps onto the canopy with no warning',
     'A heavy body slamming hard onto a thick glass canopy right above your head: a loud thud and a deep glass shudder and rattle. Very close, no music.'),
    ('power_dip', 1.0, False, 'a harmless power flicker as the airlock cycles (red herring)',
     'A brief electrical power dip in a small spaceship cockpit: a short buzz, a click, a relay chattering, then normal hum. Quiet, no music.'),
    ('cabin_thump', 1.0, False, 'a pilot sitting down hard in the cabin (red herring)',
     'A single dull thump and a creak behind a metal wall, like someone dropping heavily into a seat. Muffled, no music.'),
    ('feed_crackle', 1.0, False, 'static on the cabin camera feed (red herring)',
     'A short burst of electronic video static and digital crackle from a small cockpit screen. Quiet, no music.'),
    # Round 8 (B2): the ship and the world, replacing Round 1's code-made sounds where they were weak, and new ones
    ('engine_idle', 8.0, True, 'the engines at low throttle and in the hover (loop; the game pitches and crossfades it)',
     'Steady low hum of a small spacecraft twin jet turbines idling, smooth airy whine over a deep soft rumble, heard from inside the cockpit, constant, no changes. No music.'),
    ('engine_cruise', 8.0, True, 'the engines at cruise / full throttle (loop)',
     'Powerful steady roar of a small spacecraft twin jet turbines at full thrust heard from inside the cockpit, deep rumble with a bright turbine whine, constant, no changes. No music.'),
    ('gear_down', 2.0, False, 'the landing legs come down',
     'Heavy hydraulic landing gear extending under a small spacecraft: a motor whine, servo hiss, and a solid metal clunk as the legs lock. No music.'),
    ('gear_up', 2.0, False, 'the landing legs go up',
     'Heavy hydraulic landing gear retracting into a small spacecraft: a motor whine, servo hiss, and a muffled thump as the doors close. No music.'),
    ('airlock_cycle', 2.0, False, 'the airlock door opens (replaces the code-made hiss)',
     'A spacecraft airlock door unlocking and sliding open: a pneumatic hiss of pressure release, a mechanical latch clack and a smooth slide. No music.'),
    ('dock_clamp', 1.5, False, 'the cradle clamps the ship in the mothership / an airlock shuts (replaces the code-made thunk)',
     'A heavy magnetic docking clamp locking onto a spacecraft hull: a deep metallic clang with a short hydraulic hiss, in a large hangar. No music.'),
    ('jump_drive', 5.0, False, 'the mothership jumps to another planet',
     'A huge starship jump drive spooling up and firing: a deep rising hum, a building electric whine, then a powerful low whoosh and boom that fades. No music.'),
    ('explosion', 4.0, False, 'the ship crashes / blows up (replaces the code-made boom)',
     'A small spacecraft exploding on impact with rock: a violent fiery blast, metal tearing, debris clattering down, a rumbling fade. No music.'),
    ('regen_bay', 10.0, True, 'the regeneration bay where you wake (loop)',
     'Quiet medical machine room on a starship: a soft electrical hum, slow gentle pump breathing, faint beeps of life monitors far away. Calm, no music.'),
    ('amb_canyons', 12.0, True, 'Canyons: wind in the rock (loop, quiet under the engines)',
     'Dry desert wind blowing through deep rock canyons, low hollow moaning gusts and a faint whistle over stone, empty and lonely. No music, no animals.'),
    ('amb_forest', 12.0, True, 'Giant Forest (loop)',
     'Wind in an enormous alien forest of giant trees, deep creaking trunks, rustling leaves high above, distant strange bird calls. No music.'),
    ('amb_city', 12.0, True, 'Dead City (loop)',
     'Wind through a dead abandoned city of empty towers, rattling loose metal, a distant creaking girder, a faint broken siren tone far away, eerie. No music.'),
    ('amb_ice', 12.0, True, 'Ice (loop)',
     'Cold arctic wind over ice fields and frozen cliffs, hissing blowing snow, deep occasional creaks and groans of shifting ice. No music.'),
    ('amb_volcanic', 12.0, True, 'Volcanic (loop)',
     'A volcanic landscape: low rumbling of the ground, bubbling and gurgling lava, hissing steam vents, crackling hot rock, hot wind. No music.'),
    ('amb_crystal', 12.0, True, 'Crystal Forest (loop)',
     'A strange alien forest of giant glass crystals: soft wind, faint resonant ringing hums and delicate glassy chimes as crystals vibrate, otherworldly. No music.'),
    ('amb_ocean', 12.0, True, 'Ocean Islands (loop)',
     'A wild ocean coast: waves rolling and breaking on rocks and beaches, sea wind, distant seabirds, spray. No music.'),
]


def write_doc():
    lines = ['# Hostile Horizon — sounds made with ElevenLabs (Round 3)', '',
             'Made by `python Tools/make_sounds.py` (ElevenLabs sound effects; key from `ELEVENLABS_API_KEY`). Each file is',
             '`Assets/Resources/Sounds/<name>.mp3`; the game loads it by name (`Assets/Scripts/Audio/Sfx.cs`) and falls back to a',
             'sound made in code if the file is missing. To replace one: generate it again (`--only <name> --force`), or drop your',
             'own file with the same name (mp3 / ogg / wav) into that folder and rebuild.', '',
             '| File | Length | Loop | For | Prompt |', '|---|---|---|---|---|']
    for n, sec, loop, what, prompt in SOUNDS:
        lines.append(f'| `{n}.mp3` | {sec:g} s | {"yes" if loop else "no"} | {what} | {prompt} |')
    DOC.parent.mkdir(parents=True, exist_ok=True)
    DOC.write_text('\n'.join(lines) + '\n', encoding='utf-8')


def make(key, name, seconds, loop, prompt):
    body = {'text': prompt, 'duration_seconds': seconds, 'prompt_influence': .55, 'model_id': 'eleven_text_to_sound_v2'}
    if loop: body['loop'] = True
    for attempt in range(3):
        req = urllib.request.Request('https://api.elevenlabs.io/v1/sound-generation?output_format=mp3_44100_128', data=json.dumps(body).encode(),
                                     headers={'xi-api-key': key, 'Content-Type': 'application/json'}, method='POST')
        try:
            with urllib.request.urlopen(req, timeout=120) as r: return r.read()
        except urllib.error.HTTPError as e:
            msg = e.read().decode('utf-8', 'replace')[:300]
            if e.code in (400, 422) and ('loop' in body or 'model_id' in body):
                body.pop('loop', None); body.pop('model_id', None); continue   # an older API: plain request
            print(f'  {name}: HTTP {e.code} {msg}'); time.sleep(3)
        except Exception as e:
            print(f'  {name}: {e}'); time.sleep(3)
    return None


def main():
    a = argparse.ArgumentParser(); a.add_argument('--only', default=''); a.add_argument('--force', action='store_true'); a.add_argument('--list', action='store_true')
    o = a.parse_args()
    write_doc()
    if o.list:
        for s in SOUNDS: print(s[0])
        return 0
    key = os.environ.get('ELEVENLABS_API_KEY', '')
    if not key: print('No ELEVENLABS_API_KEY: see Docs/SOUNDS.md for the prompts.'); return 1
    OUT.mkdir(parents=True, exist_ok=True)
    only = set(x for x in o.only.split(',') if x)
    ok = 0
    for n, sec, loop, what, prompt in SOUNDS:
        if only and n not in only: continue
        f = OUT / f'{n}.mp3'
        if f.exists() and not o.force: print(f'{n}: kept ({f.stat().st_size} bytes)'); ok += 1; continue
        data = make(key, n, sec, loop, prompt)
        if data and len(data) > 1000: f.write_bytes(data); ok += 1; print(f'{n}: {len(data)} bytes')
        else: print(f'{n}: FAILED')
    print(f'{ok} / {len(only) or len(SOUNDS)} sounds in {OUT}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
