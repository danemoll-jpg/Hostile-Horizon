using UnityEngine;

namespace HostileHorizon
{
    // Round 4 (A2-A5): the air of each biome, round the viewer: drifting dust (Canyons), glowing spores (Giant Forest), falling ash
    // (Dead City), snow (Ice). Ice also has blizzards: now and then (seeded per flight: every 2-4 min, 25-45 s long) the snow thickens
    // and the haze goes white and close (WorldLook.Weather), so the radar and the route trail matter more. The forest's canopy is soft:
    // flying through leaves throws out a burst of them (Leaves).
    public sealed class BiomeWeather : MonoBehaviour
    {
        public Biome Biome { get; private set; }
        public float Blizzard { get; private set; }          // 0..1 now
        public event System.Action OnBlizzard;
        ParticleSystem air, leaves; ParticleSystemRenderer airR; Material additive, blended;
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
        }

        // every frame: follow the viewer; the blizzard clock runs while flying
        public void Tick(Vector3 viewer, bool flying, float dt)
        {
            air.transform.position = viewer;
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
