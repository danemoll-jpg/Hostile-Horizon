using UnityEngine;

namespace HostileHorizon
{
    // Round 4 (A2-A5): the air of each biome, round the viewer: drifting dust (Canyons), glowing spores (Giant Forest), falling ash
    // (Dead City), snow (Ice). Ice also has blizzards: now and then (seeded per flight: every 2-4 min, 25-45 s long) the snow thickens
    // and the haze goes white and close (WorldLook.Weather), so the radar and the route trail matter more. The forest's canopy is soft:
    // flying through leaves throws out a burst of them (Leaves).
    // Round 7 (B2-B5): Volcanic: falling ash with embers, smoke plumes over the nearest vents, a heat haze rising off the lava under a
    // low ship (glowing, wavering motes: no screen distortion); Crystal Forest: slow cold glowing motes; Ocean Islands: a faint sea
    // mist, spray thrown up by a ship skimming the water, white spray at the islands' feet comes from the rock shader. Crystal shards
    // break into glassy debris (Shatter).
    public sealed class BiomeWeather : MonoBehaviour
    {
        public Biome Biome { get; private set; }
        public float Blizzard { get; private set; }          // 0..1 now
        public event System.Action OnBlizzard;
        ParticleSystem air, leaves, embers, heat, spray, debris; ParticleSystemRenderer airR; Material additive, blended;
        readonly ParticleSystem[] plumes = new ParticleSystem[6];
        SeedRng rng; float nextStorm = 99999, stormEnd, clock; bool storming;

        public static BiomeWeather Create()
        {
            var w = new GameObject("Weather").AddComponent<BiomeWeather>();
            w.additive = new Material(Shader.Find("HostileHorizon/Particle")); w.additive.SetFloat("_SrcBlend", 1); w.additive.SetFloat("_DstBlend", 1);
            w.blended = new Material(Shader.Find("HostileHorizon/Particle")); w.blended.SetFloat("_SrcBlend", 5); w.blended.SetFloat("_DstBlend", 10);
            w.air = w.MakeSystem("Air", 4000, out w.airR);
            w.leaves = w.MakeSystem("Leaves", 600, out var lr); lr.sharedMaterial = w.blended;
            var lm = w.leaves.main; lm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f); lm.startSize = new ParticleSystem.MinMaxCurve(.25f, .6f);
            lm.startColor = new ParticleSystem.MinMaxGradient(new Color(.22f, .34f, .1f, 1), new Color(.5f, .45f, .15f, 1)); lm.gravityModifier = .15f;
            var le = w.leaves.emission; le.rateOverTime = 0;
            // Round 7
            w.embers = w.MakeSystem("Embers", 600, out var er); er.sharedMaterial = w.additive;
            var em = w.embers.main; em.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .45f, .1f) * 1.2f, new Color(1f, .7f, .3f) * 1.4f); em.startSize = new ParticleSystem.MinMaxCurve(.06f, .16f); em.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            var ev = w.embers.velocityOverLifetime; ev.enabled = true; ev.space = ParticleSystemSimulationSpace.World; ev.x = new ParticleSystem.MinMaxCurve(-.5f, .5f); ev.y = new ParticleSystem.MinMaxCurve(.6f, 2.2f); ev.z = new ParticleSystem.MinMaxCurve(-.5f, .5f);
            var en = w.embers.noise; en.enabled = true; en.strength = 1.2f; en.frequency = .3f;
            w.heat = w.MakeSystem("Heat haze", 500, out var hr); hr.sharedMaterial = w.additive;
            var hm = w.heat.main; hm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .35f, .08f, 1) * .18f, new Color(1f, .55f, .2f, 1) * .25f); hm.startSize = new ParticleSystem.MinMaxCurve(2f, 5f); hm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            var hv = w.heat.velocityOverLifetime; hv.enabled = true; hv.space = ParticleSystemSimulationSpace.World; hv.y = new ParticleSystem.MinMaxCurve(4f, 9f); hv.x = new ParticleSystem.MinMaxCurve(-1f, 1f); hv.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
            var hn = w.heat.noise; hn.enabled = true; hn.strength = 2.5f; hn.frequency = .6f; var hsh = w.heat.shape; hsh.scale = new Vector3(70, 2, 70);
            var he = w.heat.emission; he.rateOverTime = 0;
            w.spray = w.MakeSystem("Spray", 900, out var sr); sr.sharedMaterial = w.blended;
            var sm = w.spray.main; sm.startColor = new ParticleSystem.MinMaxGradient(new Color(.92f, .96f, 1f, .75f), new Color(.8f, .88f, .92f, .5f)); sm.startSize = new ParticleSystem.MinMaxCurve(.4f, 1.6f); sm.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.8f); sm.gravityModifier = .6f;
            var se = w.spray.emission; se.rateOverTime = 0;
            w.debris = w.MakeSystem("Crystal debris", 400, out var dr); dr.sharedMaterial = w.additive;
            var dm = w.debris.main; dm.startColor = new ParticleSystem.MinMaxGradient(new Color(.5f, .8f, 1.2f), new Color(.9f, .6f, 1.2f)); dm.startSize = new ParticleSystem.MinMaxCurve(.15f, .6f); dm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f); dm.gravityModifier = .8f;
            var de = w.debris.emission; de.rateOverTime = 0;
            for (int i = 0; i < w.plumes.Length; i++)
            {
                var pl = w.plumes[i] = w.MakeSystem("Smoke plume " + i, 300, out var pr); pr.sharedMaterial = w.blended;
                var pm = pl.main; pm.startColor = new ParticleSystem.MinMaxGradient(new Color(.10f, .09f, .09f, .55f), new Color(.22f, .19f, .18f, .4f)); pm.startSize = new ParticleSystem.MinMaxCurve(8f, 16f); pm.startLifetime = new ParticleSystem.MinMaxCurve(10f, 16f);
                var psh = pl.shape; psh.shapeType = ParticleSystemShapeType.Circle; psh.radius = 4f; psh.rotation = new Vector3(-90, 0, 0);
                var pv = pl.velocityOverLifetime; pv.enabled = true; pv.space = ParticleSystemSimulationSpace.World; pv.y = new ParticleSystem.MinMaxCurve(9f, 14f); pv.x = new ParticleSystem.MinMaxCurve(2f, 4f); pv.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
                var ps = pl.sizeOverLifetime; ps.enabled = true; ps.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, .6f, 1, 3.5f));
                var pe = pl.emission; pe.rateOverTime = 7;
            }
            return w;
        }

        ParticleSystem MakeSystem(string name, int max, out ParticleSystemRenderer r)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = max; m.playOnAwake = false; m.startSpeed = 0; m.loop = true;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(160, 70, 160);
            r = go.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Billboard; r.sharedMaterial = additive;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return ps;
        }

        // the biome's particles; seed: the planet (the blizzards' timing)
        public void SetBiome(Biome b, int seed)
        {
            Biome = b; rng = new SeedRng(seed, 501); clock = 0; storming = false; Blizzard = 0; WorldLook.Weather = 0;
            nextStorm = b == Biome.Ice ? 60f + rng.Value * 90f : 99999f;
            air.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = air.main; var e = air.emission; var v = air.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
            var noise = air.noise; noise.enabled = true; noise.strength = .6f; noise.frequency = .15f;
            switch (b)
            {
                case Biome.Forest:   // spores: slow, glowing, drifting up
                    airR.sharedMaterial = additive; m.startColor = new ParticleSystem.MinMaxGradient(new Color(.35f, .55f, .25f, 1) * .6f, new Color(.7f, .6f, .25f, 1) * .7f);
                    m.startSize = new ParticleSystem.MinMaxCurve(.12f, .35f); m.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f); e.rateOverTime = 260;
                    v.x = new ParticleSystem.MinMaxCurve(-.3f, .3f); v.y = new ParticleSystem.MinMaxCurve(.05f, .4f); v.z = new ParticleSystem.MinMaxCurve(-.3f, .3f); break;
                case Biome.City:     // ash: grey flakes falling slowly
                    airR.sharedMaterial = blended; m.startColor = new ParticleSystem.MinMaxGradient(new Color(.25f, .24f, .24f, .8f), new Color(.45f, .42f, .40f, .9f));
                    m.startSize = new ParticleSystem.MinMaxCurve(.08f, .22f); m.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f); e.rateOverTime = 420;
                    v.x = new ParticleSystem.MinMaxCurve(.4f, 1.2f); v.y = new ParticleSystem.MinMaxCurve(-1.4f, -.6f); v.z = new ParticleSystem.MinMaxCurve(-.4f, .4f); break;
                case Biome.Volcanic: // ash: dark flakes falling, a little faster than the city's
                    airR.sharedMaterial = blended; m.startColor = new ParticleSystem.MinMaxGradient(new Color(.10f, .09f, .09f, .85f), new Color(.30f, .27f, .26f, .9f));
                    m.startSize = new ParticleSystem.MinMaxCurve(.08f, .24f); m.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f); e.rateOverTime = 650;
                    v.x = new ParticleSystem.MinMaxCurve(.6f, 1.8f); v.y = new ParticleSystem.MinMaxCurve(-2f, -.9f); v.z = new ParticleSystem.MinMaxCurve(-.4f, .4f); break;
                case Biome.Crystal:  // slow cold motes, glowing
                    airR.sharedMaterial = additive; m.startColor = new ParticleSystem.MinMaxGradient(new Color(.3f, .6f, .9f, 1) * .6f, new Color(.7f, .45f, .95f, 1) * .6f);
                    m.startSize = new ParticleSystem.MinMaxCurve(.1f, .3f); m.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f); e.rateOverTime = 200;
                    v.x = new ParticleSystem.MinMaxCurve(-.2f, .2f); v.y = new ParticleSystem.MinMaxCurve(-.1f, .25f); v.z = new ParticleSystem.MinMaxCurve(-.2f, .2f); break;
                case Biome.Ocean:    // a faint sea mist
                    airR.sharedMaterial = blended; m.startColor = new ParticleSystem.MinMaxGradient(new Color(.9f, .94f, .96f, .18f), new Color(.85f, .9f, .95f, .28f));
                    m.startSize = new ParticleSystem.MinMaxCurve(.08f, .18f); m.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f); e.rateOverTime = 140;
                    v.x = new ParticleSystem.MinMaxCurve(1.5f, 3f); v.y = new ParticleSystem.MinMaxCurve(-.2f, .3f); v.z = new ParticleSystem.MinMaxCurve(-.5f, .5f); break;
                case Biome.Ice:      // snow
                    airR.sharedMaterial = blended; m.startColor = new ParticleSystem.MinMaxGradient(new Color(.95f, .97f, 1f, .9f), new Color(.85f, .9f, 1f, .7f));
                    m.startSize = new ParticleSystem.MinMaxCurve(.1f, .25f); m.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f); e.rateOverTime = 500;
                    v.x = new ParticleSystem.MinMaxCurve(1f, 3f); v.y = new ParticleSystem.MinMaxCurve(-3f, -1.5f); v.z = new ParticleSystem.MinMaxCurve(-.5f, .5f); break;
                default:             // canyon dust motes, faint
                    airR.sharedMaterial = blended; m.startColor = new ParticleSystem.MinMaxGradient(new Color(.75f, .58f, .45f, .35f), new Color(.85f, .7f, .55f, .5f));
                    m.startSize = new ParticleSystem.MinMaxCurve(.06f, .14f); m.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f); e.rateOverTime = 120;
                    v.x = new ParticleSystem.MinMaxCurve(.5f, 1.5f); v.y = new ParticleSystem.MinMaxCurve(-.2f, .2f); v.z = new ParticleSystem.MinMaxCurve(-.3f, .3f); break;
            }
            air.Play();
            if (b == Biome.Volcanic) embers.Play(); else embers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var ee = embers.emission; ee.rateOverTime = 90;
            foreach (var pl in plumes) pl.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Round 7 (B2): smoke plumes over the nearest vents (the props' list, nearest first), within 2.5 km
        public void Plumes(System.Collections.Generic.List<Vector3> vents, Vector3 viewer)
        {
            for (int i = 0; i < plumes.Length; i++)
            {
                bool on = Biome == Biome.Volcanic && i < vents.Count && (vents[i] - viewer).sqrMagnitude < 2500f * 2500f;
                var pl = plumes[i];
                if (on) { if ((pl.transform.position - vents[i]).sqrMagnitude > 1f) { pl.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); pl.transform.position = vents[i]; } if (!pl.isPlaying) pl.Play(); }
                else if (pl.isPlaying) pl.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // Round 7 (B2): the heat rising off the lava under the ship (0 when not over lava or high up)
        public void Heat(Vector3 at, float amount)
        {
            heat.transform.position = at; var e = heat.emission; e.rateOverTime = 220f * amount;
            if (amount > 0 && !heat.isPlaying) heat.Play();
        }

        // Round 7 (B4): spray thrown up where the ship touches the water (more the harder and faster)
        public void Spray(Vector3 at, Vector3 vel, float strength)
        {
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false }; int n = Mathf.Clamp((int)(8 + strength * 6), 8, 120);
            for (int i = 0; i < n; i++)
            {
                ep.position = at + Random.insideUnitSphere * 4f + Vector3.down * 1f;
                ep.velocity = vel * .35f + new Vector3(Random.Range(-1f, 1f) * 7f, Random.Range(4f, 10f + strength), Random.Range(-1f, 1f) * 7f);
                spray.Emit(ep, 1);
            }
            if (!spray.isPlaying) spray.Play();
        }

        // Round 7 (B3): a shard shatters
        public void Shatter(Vector3 at, Vector3 vel)
        {
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < 80; i++) { ep.position = at + Random.insideUnitSphere * 2.5f; ep.velocity = vel * .4f + Random.insideUnitSphere * 12f + Vector3.up * 4f; debris.Emit(ep, 1); }
            if (!debris.isPlaying) debris.Play();
        }

        // every frame: follow the viewer; the blizzard clock runs while flying
        public void Tick(Vector3 viewer, bool flying, float dt)
        {
            air.transform.position = viewer; embers.transform.position = viewer;
            if (Biome != Biome.Ice) return;
            if (flying) clock += dt;
            if (!storming && clock >= nextStorm) { storming = true; stormEnd = clock + 25f + rng.Value * 20f; OnBlizzard?.Invoke(); }
            if (storming && clock >= stormEnd) { storming = false; nextStorm = clock + 120f + rng.Value * 120f; }
            float want = storming ? 1f : 0f;
            float b = Mathf.MoveTowards(Blizzard, want, dt / 8f);
            if (Mathf.Abs(b - Blizzard) > 1e-4f || b != WorldLook.Weather)
            {
                Blizzard = b; WorldLook.Weather = b; WorldLook.ApplyHaze();
                var e = air.emission; e.rateOverTime = Mathf.Lerp(500, 3800, b);
                var v = air.velocityOverLifetime; v.x = new ParticleSystem.MinMaxCurve(Mathf.Lerp(1, 14, b), Mathf.Lerp(3, 24, b));
                var sh = air.shape; sh.scale = new Vector3(Mathf.Lerp(160, 90, b), 60, Mathf.Lerp(160, 90, b));
            }
        }

        // checks: start a blizzard now
        public void StartBlizzard() { if (Biome == Biome.Ice) { nextStorm = clock; } }
        public void EndWeather() { storming = false; Blizzard = 0; WorldLook.Weather = 0; WorldLook.ApplyHaze(); nextStorm = Biome == Biome.Ice ? clock + 60f + rng.Value * 90f : 99999f; }

        // a burst of leaves where the ship brushes through a crown
        public void Leaves(Vector3 at, Vector3 vel, int count)
        {
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                ep.position = at + Random.insideUnitSphere * 5f; ep.velocity = vel * .6f + Random.insideUnitSphere * 6f;
                leaves.Emit(ep, 1);
            }
            if (!leaves.isPlaying) leaves.Play();
        }
    }
}
