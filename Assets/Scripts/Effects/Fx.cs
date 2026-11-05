using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 5 (B5, B11): small pooled effects for the fighting: sparks where a bolt hits (hull, shield, rock, an enemy) and an
    // enemy's explosion (a fireball, sparks, smoke and a short light; in the world, never a screen flash). Pooled: nothing is made
    // or destroyed while shots fly.
    public static class Fx
    {
        static Material add, smoke;
        static readonly List<ParticleSystem> sparks = new List<ParticleSystem>(), blasts = new List<ParticleSystem>();
        static int nextSpark, nextBlast;
        static Transform root;

        static void Init()
        {
            if (root) return;
            root = new GameObject("Fx pool").transform;
            var sh = Shader.Find("HostileHorizon/Particle");
            add = new Material(sh) { name = "Fx add" }; add.SetFloat("_SrcBlend", 1); add.SetFloat("_DstBlend", 1);
            smoke = new Material(sh) { name = "Fx smoke" }; smoke.SetFloat("_SrcBlend", 5); smoke.SetFloat("_DstBlend", 10); smoke.SetFloat("_Softness", 1.2f);
            for (int i = 0; i < 16; i++) sparks.Add(MakeSparks());
            for (int i = 0; i < 4; i++) blasts.Add(MakeBlast());
        }

        static ParticleSystem Sys(Transform parent, string name, Material m, int max)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = .5f; main.maxParticles = max; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 0;
            return ps;
        }

        static ParticleSystem MakeSparks()
        {
            var ps = Sys(root, "Sparks", add, 60);
            var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(.15f, .5f); main.startSpeed = new ParticleSystem.MinMaxCurve(8, 30);
            main.startSize = new ParticleSystem.MinMaxCurve(.25f, .7f); main.gravityModifier = .8f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = .3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }); col.color = g;
            return ps;
        }

        static ParticleSystem MakeBlast()
        {
            var ps = Sys(root, "Blast", add, 120);
            var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(.5f, 1.4f); main.startSpeed = new ParticleSystem.MinMaxCurve(4, 22);
            main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 7f); main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, .95f, .75f), 0), new GradientColorKey(new Color(1f, .5f, .15f), .3f), new GradientColorKey(new Color(.4f, .08f, .03f), 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.8f, .4f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 2.2f));
            var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.dampen = .15f; lim.limit = 2;
            // a smoke puff that rises after the fire
            var sm = Sys(ps.transform, "Smoke", smoke, 40);
            var m2 = sm.main; m2.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f); m2.startSpeed = new ParticleSystem.MinMaxCurve(2, 8); m2.startSize = new ParticleSystem.MinMaxCurve(5, 10); m2.gravityModifier = -.08f;
            var s2 = sm.shape; s2.shapeType = ParticleSystemShapeType.Sphere; s2.radius = 2.5f;
            var c2 = sm.colorOverLifetime; c2.enabled = true;
            var gk = new Gradient(); gk.SetKeys(new[] { new GradientColorKey(new Color(.12f, .1f, .09f), 0), new GradientColorKey(new Color(.3f, .28f, .26f), 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.75f, .15f), new GradientAlphaKey(0, 1) });
            c2.color = gk;
            var l = new GameObject("Light").AddComponent<Light>(); l.transform.SetParent(ps.transform, false); l.type = LightType.Point; l.range = 90; l.color = new Color(1f, .6f, .3f); l.intensity = 0;
            return ps;
        }

        // Sparks at a point, thrown out along a normal.
        public static void Sparks(Vector3 at, Vector3 normal, Color c, int count = 18)
        {
            Init();
            var ps = sparks[nextSpark]; nextSpark = (nextSpark + 1) % sparks.Count;
            ps.transform.SetPositionAndRotation(at, Quaternion.LookRotation(normal.sqrMagnitude > .01f ? normal : Vector3.up));
            var main = ps.main; main.startColor = c;
            ps.Emit(count);
        }

        static readonly List<(Light l, float t)> lights = new List<(Light, float)>();

        // An enemy blows up: fireball, smoke, a short light. Sparks of debris thrown out.
        public static void Blast(Vector3 at, float size = 1f)
        {
            Init();
            var ps = blasts[nextBlast]; nextBlast = (nextBlast + 1) % blasts.Count;
            ps.transform.position = at; ps.transform.localScale = Vector3.one * size;
            ps.Emit(Mathf.RoundToInt(45 * size)); ps.transform.GetChild(0).GetComponent<ParticleSystem>().Emit(Mathf.RoundToInt(16 * size));
            Sparks(at, Vector3.up, new Color(1f, .7f, .35f), 40);
            var l = ps.GetComponentInChildren<Light>(); l.intensity = 8; lights.Add((l, Time.time));
        }

        // the blast lights fade over 0.8 s (called by Enemies every frame)
        public static void Tick()
        {
            for (int i = lights.Count - 1; i >= 0; i--)
            {
                var (l, t) = lights[i]; float k = (Time.time - t) / .8f;
                if (!l || k >= 1) { if (l) l.intensity = 0; lights.RemoveAt(i); } else l.intensity = 8 * (1 - k);
            }
        }
    }
}
