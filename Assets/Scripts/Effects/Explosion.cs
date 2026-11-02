using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 2, item 1: the crash. A fireball, a short light flash (a lit area in the world, never a full-screen strobe), flying
    // debris that bounces and comes to rest on the ground, a burst of smoke, then the burning wreck: fire on it, a smoke column
    // that keeps rising, a crackle. Everything stays in the scene (live) until the next flight is set up. All placeholder art
    // made in code (Round 6 replaces it).
    public sealed class Explosion : MonoBehaviour
    {
        static Material fireMat, smokeMat;
        CanyonField field;
        Light flash, fireLight;
        float age;
        readonly List<Debris> debris = new List<Debris>();

        sealed class Debris { public Transform t; public Vector3 v, spin; public bool resting; public ParticleSystem trail; }

        public static Explosion Create(Vector3 at, Vector3 shipVelocity, Transform wreck, CanyonField field, Material debrisMaterial, int seed)
        {
            if (!fireMat)
            {
                var sh = Shader.Find("HostileHorizon/Particle");
                fireMat = new Material(sh) { name = "Fire" }; fireMat.SetFloat("_SrcBlend", 1); fireMat.SetFloat("_DstBlend", 1);
                smokeMat = new Material(sh) { name = "Smoke" }; smokeMat.SetFloat("_SrcBlend", 5); smokeMat.SetFloat("_DstBlend", 10); smokeMat.SetFloat("_Softness", 1.2f);
            }
            var e = new GameObject("Explosion").AddComponent<Explosion>();
            e.transform.position = at; e.field = field;
            var rng = new SeedRng(seed, 99);

            e.flash = new GameObject("Flash").AddComponent<Light>(); e.flash.transform.SetParent(e.transform, false); e.flash.transform.localPosition = Vector3.up * 4;
            e.flash.type = LightType.Point; e.flash.range = 140; e.flash.color = new Color(1f, .62f, .3f); e.flash.intensity = 10;

            // fireball: bright core and orange flames, then dark red, gone in under two seconds
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, .95f, .75f), 0), new GradientColorKey(new Color(1f, .55f, .15f), .25f), new GradientColorKey(new Color(.6f, .12f, .03f), .7f), new GradientColorKey(new Color(.2f, .05f, .02f), 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.9f, .4f), new GradientAlphaKey(0, 1) });
            Burst(e.transform, "Fireball", fireMat, 70, new Vector2(.7f, 1.7f), new Vector2(5, 26), new Vector2(4, 11), g, 2.4f, 0, ParticleSystemShapeType.Sphere, 2.5f);
            // sparks
            var gs = new Gradient(); gs.SetKeys(new[] { new GradientColorKey(new Color(1f, .85f, .5f), 0), new GradientColorKey(new Color(1f, .4f, .1f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            Burst(e.transform, "Sparks", fireMat, 90, new Vector2(.8f, 2.2f), new Vector2(18, 45), new Vector2(.3f, .7f), gs, 1, 1.2f, ParticleSystemShapeType.Hemisphere, 1.5f);
            // the first black cloud
            var gk = new Gradient(); gk.SetKeys(new[] { new GradientColorKey(new Color(.12f, .1f, .09f), 0), new GradientColorKey(new Color(.25f, .22f, .2f), 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.85f, .12f), new GradientAlphaKey(0, 1) });
            Burst(e.transform, "Smoke burst", smokeMat, 34, new Vector2(3, 6), new Vector2(3, 10), new Vector2(7, 15), gk, 2.2f, -.15f, ParticleSystemShapeType.Sphere, 4f);

            // the wreck burns: flames on it and a smoke column that keeps rising, drifting with the wind
            var fireRoot = new GameObject("Wreck fire").transform; fireRoot.SetParent(wreck, false); fireRoot.localPosition = new Vector3(0, .6f, -1.5f);
            var gf = new Gradient(); gf.SetKeys(new[] { new GradientColorKey(new Color(1f, .8f, .4f), 0), new GradientColorKey(new Color(1f, .35f, .08f), .5f), new GradientColorKey(new Color(.4f, .06f, .02f), 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.9f, .15f), new GradientAlphaKey(0, 1) });
            var flames = Loop(fireRoot, "Flames", fireMat, 38, new Vector2(.5f, 1.1f), new Vector2(1.5f, 4), new Vector2(1.4f, 3.4f), gf, 1.3f, -.35f, 2.2f, ParticleSystemSimulationSpace.World);
            var gc = new Gradient(); gc.SetKeys(new[] { new GradientColorKey(new Color(.08f, .07f, .07f), 0), new GradientColorKey(new Color(.32f, .29f, .27f), 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.7f, .1f), new GradientAlphaKey(0, 1) });
            var column = Loop(fireRoot, "Smoke column", smokeMat, 7, new Vector2(9, 14), new Vector2(5, 8), new Vector2(4, 9), gc, 3.2f, -.12f, 1.5f, ParticleSystemSimulationSpace.World);
            var vel = column.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(1.5f, 3.5f); vel.y = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(.5f, 2f);
            e.fireLight = new GameObject("Fire light").AddComponent<Light>(); e.fireLight.transform.SetParent(fireRoot, false); e.fireLight.transform.localPosition = Vector3.up * 2;
            e.fireLight.type = LightType.Point; e.fireLight.range = 30; e.fireLight.color = new Color(1f, .5f, .2f); e.fireLight.intensity = 3;
            Sfx.Loop(Sfx.Fire, fireRoot, .5f, 12f);
            Sfx.Play(Sfx.Boom, at, 1f, 80f);

            // debris: pieces of hull thrown out, some trailing smoke
            for (int i = 0; i < 18; i++)
            {
                var d = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(d.GetComponent<Collider>());
                d.name = "Debris"; d.transform.SetParent(e.transform, false);
                d.transform.localScale = new Vector3(.3f + rng.Value * 1.3f, .1f + rng.Value * .3f, .3f + rng.Value * 1.0f);
                d.transform.rotation = Quaternion.Euler(rng.Value * 360, rng.Value * 360, rng.Value * 360);
                d.GetComponent<Renderer>().sharedMaterial = debrisMaterial;
                var dir = new Vector3(rng.Value * 2 - 1, .35f + rng.Value, rng.Value * 2 - 1).normalized;
                var piece = new Debris { t = d.transform, v = dir * (10 + rng.Value * 26) + shipVelocity * .25f, spin = new Vector3(rng.Value - .5f, rng.Value - .5f, rng.Value - .5f) * 900 };
                if (i < 6) piece.trail = Loop(d.transform, "Trail", smokeMat, 16, new Vector2(1.5f, 2.5f), new Vector2(0, .5f), new Vector2(.8f, 1.6f), gc, 2.5f, 0, .3f, ParticleSystemSimulationSpace.World);
                e.debris.Add(piece);
            }
            return e;
        }

        static ParticleSystem Make(Transform parent, string name, Material m, ParticleSystemSimulationSpace space)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; r.sortingFudge = m == smokeMat ? 10 : 0;
            var main = ps.main; main.simulationSpace = space; main.playOnAwake = false; main.maxParticles = 400;
            return ps;
        }

        static ParticleSystem Burst(Transform parent, string name, Material m, int count, Vector2 life, Vector2 speed, Vector2 size, Gradient g, float grow, float gravity, ParticleSystemShapeType shape, float radius)
        {
            var ps = Make(parent, name, m, ParticleSystemSimulationSpace.World);
            var main = ps.main; main.duration = 1; main.loop = false; main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y); main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.gravityModifier = gravity; main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
            var sh = ps.shape; sh.shapeType = shape; sh.radius = radius;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, grow));
            var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.dampen = .12f; lim.limit = 2;
            ps.Play(); return ps;
        }

        static ParticleSystem Loop(Transform parent, string name, Material m, float rate, Vector2 life, Vector2 speed, Vector2 size, Gradient g, float grow, float gravity, float radius, ParticleSystemSimulationSpace space)
        {
            var ps = Make(parent, name, m, space);
            var main = ps.main; main.duration = 5; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y); main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.gravityModifier = gravity; main.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12; sh.radius = radius; sh.rotation = new Vector3(-90, 0, 0);
            var col = ps.colorOverLifetime; col.enabled = true; col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, grow));
            ps.Play(); return ps;
        }

        void Update()
        {
            float dt = Time.deltaTime; age += dt;
            if (flash) { flash.intensity = Mathf.Max(0, 10 * (1 - age / .9f)); if (age > 1) Destroy(flash.gameObject); }
            if (fireLight) fireLight.intensity = 2.4f + Mathf.PerlinNoise(age * 7f, 0) * 2.2f;
            foreach (var d in debris)
            {
                if (d.resting || !d.t) continue;
                d.v += Physics.gravity * dt;
                d.t.position += d.v * dt;
                d.t.Rotate(d.spin * dt, Space.World);
                float g = field.SurfaceHeight(d.t.position.x, d.t.position.z, out var n) + d.t.localScale.y * .5f;
                if (d.t.position.y < g)
                {
                    d.t.position = new Vector3(d.t.position.x, g, d.t.position.z);
                    float into = -Vector3.Dot(d.v, n);
                    d.v = (d.v + n * into * 1.3f) * .45f; d.spin *= .5f;
                    if (d.v.magnitude < 1.5f)
                    {
                        d.resting = true; d.t.rotation = Quaternion.FromToRotation(d.t.up, n) * d.t.rotation;
                        if (d.trail) { var em = d.trail.emission; em.rateOverTime = 3; }
                    }
                }
            }
        }

        // The ship comes to rest where it hit: settled onto the ground, tipped over a little.
        public static void SettleWreck(Transform ship, CanyonField field, int seed)
        {
            var p = ship.position; float h = field.SurfaceHeight(p.x, p.z, out var n);
            var rng = new SeedRng(seed, 98);
            var fwd = Vector3.ProjectOnPlane(ship.forward, n); if (fwd.sqrMagnitude < .01f) fwd = Vector3.ProjectOnPlane(Vector3.forward, n);
            ship.rotation = Quaternion.LookRotation(fwd.normalized, n) * Quaternion.Euler(4 + rng.Value * 8, 0, (rng.Value * 2 - 1) * 22);
            ship.position = new Vector3(p.x, h + .35f, p.z);
        }
    }
}
