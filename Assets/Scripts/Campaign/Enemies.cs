using System;
using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    public enum EnemyKind { Turret, Saucer }

    public sealed class Enemy
    {
        public EnemyKind Kind; public int Index;
        public Transform Root, Head, Gun, Rim;
        public Vector3 Pos, Vel, Home, Waypoint;
        public float Hp, LosAt, LockT, Cooldown, BurstGap, Lost, Yaw, Pitch, PassT, StumpTop, WaypointUntil;
        public int Burst, Pass;
        public bool Dead, Spotted, Los, Locked, Chasing, OnStump;
        public bool Nest;          // Round 9 (B1): one of the turret nest's (the side mission)
        public AudioSource Hum;
        public Transform Beacon;   // Round 6 (B3): the blinking red warning light
        public Vector3 Eye => Kind == EnemyKind.Turret ? Pos + Vector3.up * 3.47f : Pos;   // Round 8: the Meshy turret's barrel axis (3.3 before)
        public Vector3 Centre => Kind == EnemyKind.Turret ? Pos + Vector3.up * 2.4f : Pos;
        public float Radius => Kind == EnemyKind.Turret ? 3.3f : 7.5f;
    }

    // Round 5, Part B: the planet fights back (Campaign only). From the planet's seed and the difficulty:
    //  B1 turrets on the high ground (canyon rims, rooftops, ice cliffs; in the Giant Forest on broken giant stumps above the canopy):
    //     they track the ship while it is in their line of sight and range, lock on, and fire bright bolts with a short lead, so a
    //     ship that keeps moving can dodge them; flying low keeps you out of their sight.
    //  B2 saucers patrol; once they see you (or hear a ship landed / hovering) they chase and make firing passes; they keep clear of
    //     the rock (less agile than the ship, above the trees in the forest), so tight canyons are hard for them.
    //  B3 your gun's bolts; B5 hits on the ship (GameShell turns them into damage, shake, sparks); B7 pilots' pads out of turret sight
    //     on Easy / Normal, some covered on Hard / Nightmare; B9 the first planet has a few turrets only; B11 shots and effects pooled.
    public sealed class Enemies : MonoBehaviour
    {
        public static Enemies I { get; private set; }
        public readonly List<Enemy> All = new List<Enemy>();
        public CanyonField Field; public ShipController Ship;
        public bool Running;          // a Campaign flight is on (GameShell)
        public bool ShieldOn;         // GameShell
        public float ShieldRadius = 8f;   // Round 6 (C): m round the ship the shield catches bolts (the Shields upgrade)
        public event Action<Enemy> Destroyed, LockedOn, SaucerSpotted;
        public event Action<Enemy, float, bool, Vector3> HitShip;   // shooter, damage, blocked by the shield, where
        public event Action<Enemy> FiredAtShip;
        // checks
        public int EnemyShots, EnemyHits, Blocked, PlayerShots, PlayerHits;
        public readonly List<string> Placement = new List<string>();
        public readonly List<(Vector3 from, float until)> Threats = new List<(Vector3, float)>();

        sealed class Bolt { public Transform core, halo; public TrailRenderer trail; public Vector3 p, v; public float life, dmg; public bool player, on; public Enemy from; }
        // Round 6 (B3): muzzle flashes (pooled), sized with the distance from the eye so a shot reads at 1-2 km
        readonly List<(Transform t, float until, float size)> flashes = new List<(Transform, float, float)>();
        Material beaconMat, flashMat, enemyTrail, playerTrail;
        public int Flashes { get; private set; }   // checks
        readonly List<Bolt> bolts = new List<Bolt>();
        Material enemyCore, playerCore, enemyHalo, playerHalo, litArmor, litMetal, litDark, glow, eye, bark;
        Mesh quad;
        GameObject turretModel, saucerModel;

        public static Enemies Create(Material lit)
        {
            var e = new GameObject("Enemies").AddComponent<Enemies>(); I = e;
            e.enemyCore = Glow(new Color(1f, .22f, .3f) * 3f); e.playerCore = Glow(new Color(.35f, 1f, .75f) * 2.6f);
            var ph = Shader.Find("HostileHorizon/Particle");
            e.enemyHalo = new Material(ph) { name = "Enemy bolt halo" }; e.enemyHalo.SetFloat("_SrcBlend", 1); e.enemyHalo.SetFloat("_DstBlend", 1); e.enemyHalo.SetColor("_Color", new Color(1f, .3f, .3f, .9f));
            e.playerHalo = new Material(ph) { name = "Gun bolt halo" }; e.playerHalo.SetFloat("_SrcBlend", 1); e.playerHalo.SetFloat("_DstBlend", 1); e.playerHalo.SetColor("_Color", new Color(.4f, 1f, .7f, .6f));
            Material Lit(Color c, float sm, float me) { var m = new Material(lit); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", sm); m.SetFloat("_Metallic", me); return m; }
            e.litArmor = Lit(new Color(.34f, .31f, .28f), .3f, .2f); e.litMetal = Lit(new Color(.42f, .43f, .45f), .5f, .6f); e.litDark = Lit(new Color(.07f, .07f, .08f), .4f, .3f);
            e.bark = Lit(new Color(.16f, .12f, .09f), .1f, 0);
            e.glow = new Material(Shader.Find("HostileHorizon/Glow")); e.glow.SetColor("_Color", new Color(1f, .25f, .2f) * 2.4f);
            e.eye = new Material(Shader.Find("HostileHorizon/Glow")); e.eye.SetColor("_Color", new Color(1f, .15f, .1f) * 3f);
            e.turretModel = Resources.Load<GameObject>("Models/Turret"); e.saucerModel = Resources.Load<GameObject>("Models/Saucer");
            e.quad = new Mesh { name = "Halo quad", vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) },
                                uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }, colors = new[] { Color.white, Color.white, Color.white, Color.white }, triangles = new[] { 0, 2, 1, 0, 3, 2 } };
            e.quad.bounds = new Bounds(Vector3.zero, Vector3.one * 2);
            e.beaconMat = Glow(new Color(1f, .06f, .03f) * 1.8f);   // a saturated red (brighter turned it white) e.flashMat = Glow(new Color(1f, .55f, .35f) * 4f);
            var tr = Shader.Find("HostileHorizon/GlowAdd");
            e.enemyTrail = new Material(tr); e.enemyTrail.SetColor("_Color", new Color(1f, .25f, .2f) * 1.6f);
            e.playerTrail = new Material(tr); e.playerTrail.SetColor("_Color", new Color(.35f, 1f, .7f) * 1.2f);
            for (int i = 0; i < 96; i++) e.bolts.Add(e.NewBolt());
            for (int i = 0; i < 8; i++) { var f = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(f.GetComponent<Collider>()); f.name = "Muzzle flash"; f.transform.SetParent(e.transform, false); var fr = f.GetComponent<Renderer>(); fr.sharedMaterial = e.flashMat; fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; fr.receiveShadows = false; f.SetActive(false); e.flashes.Add((f.transform, 0, 1)); }
            ShipController.ExtraSolid = e.SolidAt;
            return e;
        }

        static Material Glow(Color c) { var m = new Material(Shader.Find("HostileHorizon/GlowAdd")); m.SetColor("_Color", c); return m; }

        Bolt NewBolt()
        {
            var b = new Bolt();
            var core = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(core.GetComponent<Collider>()); core.name = "Bolt";
            var r = core.GetComponent<MeshRenderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            core.transform.SetParent(transform, false); b.core = core.transform;
            var h = new GameObject("Halo"); h.transform.SetParent(core.transform, false);
            h.AddComponent<MeshFilter>().sharedMesh = quad; var hr = h.AddComponent<MeshRenderer>(); hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; hr.receiveShadows = false;
            b.halo = h.transform;
            // Round 6 (B3): a short glowing trail behind each bolt
            b.trail = core.AddComponent<TrailRenderer>(); b.trail.time = .12f; b.trail.minVertexDistance = 2f; b.trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; b.trail.receiveShadows = false;
            b.trail.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)); b.trail.emitting = false;
            core.SetActive(false);
            return b;
        }

        // ---- placing a planet's enemies (from the seed and the difficulty)

        public void Clear()
        {
            foreach (var e in All) if (e.Root) Destroy(e.Root.gameObject);
            All.Clear(); Threats.Clear(); Placement.Clear();
            foreach (var b in bolts) { b.on = false; b.trail.emitting = false; b.core.gameObject.SetActive(false); }
            EnemyShots = EnemyHits = Blocked = PlayerShots = PlayerHits = 0;
        }

        // pads: every pilot site's landing spot (B7); start: where the mothership arrives (launch area kept clear)
        public void Setup(CanyonField f, ShipController ship, int seed, int tier, bool firstPlanet, List<Vector3> pads, Vector3 start, Vector3? nest = null, int nestCount = 0)
        {
            Clear(); Field = f; Ship = ship;
            var t = Tuning.T; int mode = Difficulty.Enemies; float ag = Difficulty.Aggression;
            Placement.Add($"difficulty {Difficulty.Name(Difficulty.Current)}: {Difficulty.Summary(Difficulty.Current)}; tier {tier}, {(firstPlanet ? "a career's first planet (ease-in)" : "not the first planet")}");
            if (mode == 0) { Placement.Add("enemies off"); return; }
            int nT = firstPlanet ? Mathf.Max(1, Mathf.RoundToInt(t.turretsFirstPlanet * Mathf.Min(1f, ag))) : Mathf.Min(t.turretsMax, Mathf.RoundToInt((t.turretsBase + tier * t.turretsPerTier) * ag));
            float R = t.pilotRangeMax + tier * t.pilotRangePerTier + t.turretArea;
            var rng = new SeedRng(seed, 501);
            var spots = new List<Vector3>(); var stump = new List<bool>();
            float cover = firstPlanet ? 0 : Difficulty.CoveredPads;
            int covered = Mathf.RoundToInt(pads.Count * cover), madeCovering = 0;
            bool Spaced(Vector3 p) { if (Field.OverLiquid(p.x, p.z) || Field.HasLiquid && p.y < Field.Liquid + 6f) return false;   // Round 7: never on the sea or the lava
                 foreach (var s in spots) if (Flat(s - p).magnitude < t.turretSpacing) return false; foreach (var pd in pads) if (Flat(pd - p).magnitude < 160f) return false; return Flat(p - start).magnitude > t.turretMinFromStart; }
            // B7, Hard / Nightmare: a share of the pads is covered on purpose (a turret placed where it sees the pad)
            var order = new List<int>(); for (int i = 0; i < pads.Count; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (order[i], order[j]) = (order[j], order[i]); }
            for (int k = 0; k < covered && spots.Count < nT; k++)
            {
                var pad = pads[order[k]];
                for (int a = 0; a < 60; a++)
                {
                    float ang = rng.Value * Mathf.PI * 2, r = Mathf.Lerp(300f, 850f, rng.Value);
                    float qx = pad.x + Mathf.Cos(ang) * r, qz = pad.z + Mathf.Sin(ang) * r;
                    var c = HighPoint(qx, qz, out bool st);
                    if (!Sees(c + Vector3.up * 3.3f, pad + Vector3.up * 4f, 20f)) c = HighPoint(qx, qz, out st, false);   // the far rim can't see it: the high ground nearer
                    if (!Spaced(c) || !Sees(c + Vector3.up * 3.3f, pad + Vector3.up * 4f, 20f)) continue;
                    spots.Add(c); stump.Add(st); madeCovering++; break;
                }
            }
            // the rest: on the high ground anywhere in the area; on Easy / Normal never where it sees a pilot's pad
            for (int a = 0; a < nT * 80 && spots.Count < nT; a++)
            {
                float ang = rng.Value * Mathf.PI * 2, r = Mathf.Lerp(t.turretMinFromStart, R, Mathf.Sqrt(rng.Value));
                var c = HighPoint(start.x + Mathf.Cos(ang) * r, start.z + Mathf.Sin(ang) * r, out bool st);
                if (!Spaced(c)) continue;
                if (cover <= 0) { bool sees = false; foreach (var pd in pads) if (Flat(pd - c).magnitude < t.turretRange && Sees(c + Vector3.up * 3.3f, pd + Vector3.up * 4f, 20f)) { sees = true; break; } if (sees) continue; }
                spots.Add(c); stump.Add(st);
            }
            for (int i = 0; i < spots.Count; i++) All.Add(MakeTurret(i, spots[i], stump[i]));
            // Round 9 (B1): the turret nest: nestCount turrets on the high ground round one valley, each seeing its floor (on Easy /
            // Normal still never where they see a pilot's pad); indices after the others, so destroyed ones stay destroyed
            if (nest.HasValue && nestCount > 0)
            {
                var nr = new SeedRng(seed, 507); var ns = new List<Vector3>(); var valley = nest.Value + Vector3.up * 4f;
                // three passes, looser each time: the rim near the point, the high ground right by it, then the slope itself (the canyon walls)
                for (int a = 0; a < 900 && ns.Count < nestCount; a++)
                {
                    int pass = a / 300; float ring = pass == 0 ? t.missionNestRing : t.missionNestRing * 1.4f;
                    float ang = nr.Value * Mathf.PI * 2, r = Mathf.Lerp(120f, ring, Mathf.Sqrt(nr.Value));
                    float qx = valley.x + Mathf.Cos(ang) * r, qz = valley.z + Mathf.Sin(ang) * r; bool st = false;
                    var c = pass == 0 ? HighPoint(qx, qz, out st) : pass == 1 ? HighPoint(qx, qz, out st, false) : OnTop(qx, qz, Field.SurfaceHeight(qx, qz), out st);
                    if (Field.OverLiquid(c.x, c.z) || Field.HasLiquid && c.y < Field.Liquid + 6f || Flat(c - valley).magnitude > ring + 120f) continue;
                    bool close = false; foreach (var q in ns) if (Flat(q - c).magnitude < 90f) close = true; foreach (var q in spots) if (Flat(q - c).magnitude < 150f) close = true;
                    if (close || !Sees(c + Vector3.up * 3.3f, valley, 16f)) continue;
                    if (cover <= 0) { bool sees = false; foreach (var pd in pads) if (Flat(pd - c).magnitude < t.turretRange && Sees(c + Vector3.up * 3.3f, pd + Vector3.up * 4f, 20f)) { sees = true; break; } if (sees) continue; }
                    ns.Add(c); var e = MakeTurret(All.Count, c, st); e.Nest = true; All.Add(e);
                }
                Placement.Add($"turret nest: {ns.Count} (asked {nestCount}) round the valley at {nest.Value.ToString("0")}");
            }
            int padsSeen = 0; foreach (var pd in pads) { foreach (var s in spots) if (Flat(pd - s).magnitude < t.turretRange && Sees(s + Vector3.up * 3.3f, pd + Vector3.up * 4f, 20f)) { padsSeen++; break; } }
            Placement.Add($"turrets: {spots.Count} (asked {nT}), {madeCovering} placed to cover a pad; pads in a turret's sight {padsSeen} of {pads.Count}");
            // B2, B9: saucers from the second planet on
            if (mode == 2 && !firstPlanet)
            {
                int nS = Mathf.Clamp(Mathf.RoundToInt((t.saucersBase + tier * t.saucersPerTier) * ag), 1, t.saucersMax);
                for (int i = 0; i < nS; i++)
                {
                    float ang = rng.Value * Mathf.PI * 2, r = Mathf.Lerp(900f, R, Mathf.Sqrt(rng.Value));
                    var home = new Vector3(start.x + Mathf.Cos(ang) * r, 0, start.z + Mathf.Sin(ang) * r); home.y = Cruise(home);
                    All.Add(MakeSaucer(All.Count, home, rng.Value * 360f));
                }
                Placement.Add($"saucers: {nS}");
            }
            else Placement.Add(mode == 2 ? "saucers: none on a career's first planet (ease-in)" : "saucers: off");
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        // Round 6 (D1): the star map's estimate for a planet not yet surveyed (the same formulas as Setup)
        public static (int turrets, int saucers) Estimate(int tier, bool firstPlanet)
        {
            var t = Tuning.T; int mode = Difficulty.Enemies; float ag = Difficulty.Aggression;
            if (mode == 0) return (0, 0);
            int nT = firstPlanet ? Mathf.Max(1, Mathf.RoundToInt(t.turretsFirstPlanet * Mathf.Min(1f, ag))) : Mathf.Min(t.turretsMax, Mathf.RoundToInt((t.turretsBase + tier * t.turretsPerTier) * ag));
            int nS = mode == 2 && !firstPlanet ? Mathf.Clamp(Mathf.RoundToInt((t.saucersBase + tier * t.saucersPerTier) * ag), 1, t.saucersMax) : 0;
            return (nT, nS);
        }

        // The highest ground near a point (a rim, a roof, a cliff); in the Giant Forest a broken giant stump above the canopy.
        Vector3 HighPoint(float x, float z, out bool onStump, bool wide = true)
        {
            if (!wide)
            {
                float lx = x, lz = z, lh = float.MinValue;
                for (int i = -3; i <= 3; i++) for (int j = -3; j <= 3; j++) { float px = x + i * 20f, pz = z + j * 20f, h = Field.SurfaceHeight(px, pz); if (h > lh) { lh = h; lx = px; lz = pz; } }
                return OnTop(lx, lz, lh, out onStump);
            }
            // coarse (+/- 160 m: canyon walls are 300-600 m high, the rim is often further than a ledge), then fine round the best
            float bx = x, bz = z, bh = float.MinValue;
            for (int i = -4; i <= 4; i++) for (int j = -4; j <= 4; j++)
            {
                float px = x + i * 40f, pz = z + j * 40f, h = Field.SurfaceHeight(px, pz);
                if (h > bh) { bh = h; bx = px; bz = pz; }
            }
            float cx = bx, cz = bz;
            for (int i = -3; i <= 3; i++) for (int j = -3; j <= 3; j++)
            {
                float px = cx + i * 10f, pz = cz + j * 10f, h = Field.SurfaceHeight(px, pz);
                if (h > bh) { bh = h; bx = px; bz = pz; }
            }
            return OnTop(bx, bz, bh, out onStump);
        }

        Vector3 OnTop(float bx, float bz, float bh, out bool onStump)
        {
            onStump = Field.Biome == Biome.Forest;
            if (onStump)
            {
                float top = bh + 60f; int ti = Mathf.FloorToInt(bx / CanyonField.TreeCell), tj = Mathf.FloorToInt(bz / CanyonField.TreeCell);
                for (int di = -2; di <= 2; di++) for (int dj = -2; dj <= 2; dj++) { var tr = Field.TreeAt(ti + di, tj + dj); if (tr.ok) top = Mathf.Max(top, tr.basePos.y + tr.height); }
                return new Vector3(bx, top + 8f, bz);
            }
            return new Vector3(bx, bh, bz);
        }

        // Line of sight over the ground (and, in the forest, not through a tree crown).
        public bool Sees(Vector3 a, Vector3 b, float step = 12f)
        {
            var d = b - a; float len = d.magnitude; if (len < 1f) return true;
            var dir = d / len; bool forest = Field.Biome == Biome.Forest;
            for (float s = 6f; s < len - 8f; s += step)
            {
                var p = a + dir * s;
                if (Field.SurfaceHeight(p.x, p.z) > p.y + .5f) return false;
                if (forest && Field.InCanopy(p) > .05f) return false;
            }
            return true;
        }

        float Cruise(Vector3 p) => Field.SurfaceHeight(p.x, p.z) + Tuning.T.saucerHeight + (Field.Biome == Biome.Forest ? 120f : 0);

        // ---- models

        Material Slot(string name)
        {
            string s = name.Contains("__") ? name.Substring(name.IndexOf("__") + 2) : name;
            if (s.StartsWith("mx_")) { var hard = Resources.Load<Material>("Hard/" + s.Substring(3)); if (hard) return hard; }   // Round 8 (B1): Meshy parts
            return s == "armor" ? litArmor : s == "metal" ? litMetal : s == "glow" ? glow : s == "eye" ? eye : litDark;
        }

        // the FBX's parts by group onto pivots (each part built round its own pivot; FBX files import facing back: half a turn)
        void Parts(GameObject model, Dictionary<string, Transform> pivots)
        {
            var m = Instantiate(model);
            foreach (var r in m.GetComponentsInChildren<MeshRenderer>(true))
            {
                string n = r.name, g = n.Contains("__") ? n.Substring(0, n.IndexOf("__")) : n;
                if (!pivots.TryGetValue(g, out var pv)) continue;
                var rot = r.transform.localRotation; var sc = r.transform.lossyScale;
                r.transform.SetParent(pv, false); r.transform.localPosition = Vector3.zero; r.transform.localRotation = Quaternion.Euler(0, 180, 0) * rot; r.transform.localScale = sc;
                r.sharedMaterial = Slot(n); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            Destroy(m);
        }

        Enemy MakeTurret(int i, Vector3 at, bool onStump)
        {
            var e = new Enemy { Kind = EnemyKind.Turret, Index = i, Pos = at, Hp = Tuning.T.turretHp, OnStump = onStump, Yaw = (i * 73) % 360 };
            e.Root = new GameObject("Turret " + (i + 1)).transform; e.Root.SetParent(transform, false); e.Root.position = at;
            e.Head = new GameObject("Head").transform; e.Head.SetParent(e.Root, false); e.Head.localPosition = new Vector3(0, 2.4f, 0);
            e.Gun = new GameObject("Gun").transform; e.Gun.SetParent(e.Head, false); e.Gun.localPosition = new Vector3(0, 1.07f, 1.53f);   // Round 8: the Meshy turret's barrel root (enemies.py prints it); was (0, .9, .4)
            if (turretModel) Parts(turretModel, new Dictionary<string, Transform> { { "TurretBase", e.Root }, { "TurretHead", e.Head }, { "TurretGun", e.Gun } });
            if (onStump)
            {
                // a broken giant stump from the forest floor up to the turret
                float g = Field.Height(at.x, at.z), h = at.y - g + 1f;
                var s = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(s.GetComponent<Collider>()); s.name = "Stump";
                s.transform.SetParent(e.Root, false); s.transform.position = new Vector3(at.x, g + h / 2 - 1f, at.z); s.transform.localScale = new Vector3(8f, h / 2, 8f);
                s.GetComponent<Renderer>().sharedMaterial = bark;
                e.StumpTop = at.y;
            }
            e.Head.localRotation = Quaternion.Euler(0, e.Yaw, 0);
            e.Beacon = MakeBeacon(e.Root, new Vector3(0, 4.9f, 0));
            return e;
        }

        public Enemy CheckSaucer(Vector3 home) { var e = MakeSaucer(All.Count, home, 0); All.Add(e); return e; }   // checks (round8b): a saucer to look at

        Enemy MakeSaucer(int i, Vector3 home, float heading)
        {
            var e = new Enemy { Kind = EnemyKind.Saucer, Index = i, Pos = home, Home = home, Waypoint = home, Hp = Tuning.T.saucerHp, Vel = Quaternion.Euler(0, heading, 0) * Vector3.forward * Tuning.T.saucerPatrolSpeed };
            e.Root = new GameObject("Saucer " + (i + 1)).transform; e.Root.SetParent(transform, false); e.Root.position = home;
            e.Rim = new GameObject("Rim").transform; e.Rim.SetParent(e.Root, false);
            if (saucerModel) Parts(saucerModel, new Dictionary<string, Transform> { { "Saucer", e.Root }, { "SaucerRim", e.Rim } });
            e.Hum = Sfx.Loop(Sfx.Clip("saucer_hum"), e.Root, .9f, 25f);
            e.Beacon = MakeBeacon(e.Root, new Vector3(0, -2.2f, 0));
            return e;
        }

        Transform MakeBeacon(Transform parent, Vector3 at)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(b.GetComponent<Collider>()); b.name = "Warning light";
            b.transform.SetParent(parent, false); b.transform.localPosition = at;
            var r = b.GetComponent<Renderer>(); r.sharedMaterial = beaconMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return b.transform;
        }

        void Flash(Vector3 at)
        {
            for (int i = 0; i < flashes.Count; i++)
                if (!flashes[i].t.gameObject.activeSelf) { flashes[i].t.position = at; flashes[i].t.gameObject.SetActive(true); flashes[i] = (flashes[i].t, Time.time + .14f, 1); Flashes++; return; }
        }

        // Round 6 (B3): the warning lights blink (on .7 s of every 1.2 s) and, with the flashes and the bolts' glow, keep a size on
        // screen that reads at 1-2 km (never smaller than their real size)
        void Readable(Transform cam)
        {
            float blink = (Time.time % 1.2f) < .7f ? 1f : 0f;
            foreach (var e in All)
            {
                if (!e.Beacon) continue;
                bool on = !e.Dead && blink > 0; e.Beacon.gameObject.SetActive(on);
                if (on && cam) e.Beacon.localScale = Vector3.one * Mathf.Max(.7f, Vector3.Distance(cam.position, e.Beacon.position) * Tuning.T.beaconScale);
            }
            for (int i = 0; i < flashes.Count; i++)
            {
                var f = flashes[i]; if (!f.t.gameObject.activeSelf) continue;
                if (Time.time > f.until) { f.t.gameObject.SetActive(false); continue; }
                float k = (f.until - Time.time) / .14f, d = cam ? Vector3.Distance(cam.position, f.t.position) : 100;
                f.t.localScale = Vector3.one * Mathf.Max(2.5f, d * Tuning.T.flashScale) * (.5f + .5f * k);
            }
        }

        // B1 / Fix: tree trunks and bridges collide from the seed; turrets and their stumps collide too (ShipController.ExtraSolid)
        bool SolidAt(Vector3 p, out Vector3 n, out float depth)
        {
            n = Vector3.up; depth = 0;
            foreach (var e in All)
            {
                if (e.Kind != EnemyKind.Turret) continue;
                float dx = p.x - e.Pos.x, dz = p.z - e.Pos.z; if (dx * dx + dz * dz > 100f) continue;
                float bottom = e.OnStump ? Field.Height(e.Pos.x, e.Pos.z) - 2f : e.Pos.y - 1f, top = e.Pos.y + (e.Dead ? 2.3f : 4.4f);
                float r = e.OnStump && p.y < e.Pos.y ? 4f : 2.6f;
                if (p.y < bottom || p.y > top) continue;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                float side = r - d, up = top - p.y;
                if (side <= 0) continue;
                if (up < side) { if (up > depth) { depth = up; n = Vector3.up; } }
                else if (side > depth) { depth = side; n = d > .01f ? new Vector3(dx / d, 0, dz / d) : Vector3.forward; }
            }
            return depth > 0;
        }

        // ---- shooting

        void Shoot(Vector3 from, Vector3 vel, bool player, float dmg, Enemy by, float range)
        {
            Bolt b = null; foreach (var x in bolts) if (!x.on) { b = x; break; }
            if (b == null) return;   // the pool is full: no shot (never seen with 96)
            b.on = true; b.p = from; b.v = vel; b.player = player; b.dmg = dmg; b.from = by; b.life = range / vel.magnitude;
            var r = b.core.GetComponent<MeshRenderer>(); r.sharedMaterial = player ? playerCore : enemyCore;
            b.halo.GetComponent<MeshRenderer>().sharedMaterial = player ? playerHalo : enemyHalo;
            b.core.localScale = player ? new Vector3(.22f, .22f, 7f) : new Vector3(.5f, .5f, 9f);
            b.halo.localScale = player ? new Vector3(2f / .22f, 2f / .22f, 1) : new Vector3(5f / .5f, 5f / .5f, 1);
            b.core.SetPositionAndRotation(from, Quaternion.LookRotation(vel));
            b.trail.sharedMaterial = player ? playerTrail : enemyTrail; b.trail.widthMultiplier = player ? .35f : .9f;
            b.core.gameObject.SetActive(true); b.trail.Clear(); b.trail.emitting = true;
            if (!player) Flash(from);
        }

        // B3: one shot of the ship's gun (GameShell: rate, energy, sound)
        public void PlayerShot(Vector3 from, Vector3 dir, Vector3 inherit, float damage = 1)
        {
            var t = Tuning.T; PlayerShots++;
            Shoot(from, dir * t.gunBoltSpeed + inherit, true, damage, null, t.gunRange);
        }

        Vector3 Lead(Vector3 from, float speed, float leadShare, float err, SeedRng rng = null)
        {
            var sp = Ship.transform.position; float d = Vector3.Distance(from, sp); float tt = d / speed;
            if (Difficulty.TurretIntercept) for (int k = 0; k < 3; k++) tt = Vector3.Distance(from, sp + Ship.Velocity * tt) / speed;   // Round 9 (G): Hard / Nightmare aim where the bolt meets the ship
            var aim = sp + Ship.Velocity * tt * leadShare;
            float e = err * d / 1000f;
            aim += new Vector3(UnityEngine.Random.value - .5f, UnityEngine.Random.value - .5f, UnityEngine.Random.value - .5f) * 2f * e;
            return aim;
        }

        public bool AnyLock { get; private set; }
        public static bool CheckTargetKinematic;   // Round 9 (G) checks: a ship moved along a straight line by the check can be shot at
        public int Spotted { get { int n = 0; foreach (var e in All) if (!e.Dead && e.Spotted) n++; return n; } }

        void Update()
        {
            Fx.Tick();
            Readable(GameShell.I && GameShell.I.Rig ? GameShell.I.Rig.Head.transform : null);
            float dt = Time.deltaTime; if (dt <= 0) return;
            MoveBolts(dt);
            if (!Running || Ship == null || Field == null) { AnyLock = false; return; }
            var t = Tuning.T; float ag = Difficulty.Aggression; var sp = Ship.transform.position;
            bool targetable = !Ship.Crashed && (!Ship.Kinematic || CheckTargetKinematic) && Ship.gameObject.activeSelf;
            bool lockNow = false;
            float view = 3200f;
            for (int i = 0; i < All.Count; i++)
            {
                var e = All[i]; if (e.Dead) continue;
                float dist = Vector3.Distance(e.Centre, sp);
                e.Root.gameObject.SetActive(dist < view);
                if (e.Kind == EnemyKind.Turret) TurretTick(e, dist, dt, t, ag, targetable, ref lockNow);
                else SaucerTick(e, dist, dt, t, ag, targetable, ref lockNow);
            }
            AnyLock = lockNow;
            for (int i = Threats.Count - 1; i >= 0; i--) if (Time.time > Threats[i].until) Threats.RemoveAt(i);
        }

        void TurretTick(Enemy e, float dist, float dt, Tuning t, float ag, bool targetable, ref bool lockNow)
        {
            var sp = Ship.transform.position;
            if (dist > t.turretRange + 150f || !targetable) { e.Los = false; e.LockT = 0; e.Locked = false; return; }
            if (Time.time >= e.LosAt) { e.LosAt = Time.time + .2f + (e.Index % 5) * .02f; e.Los = dist < t.turretRange && Sees(e.Eye, sp); }
            // turn toward the ship (only while it is seen: a turret doesn't track what it can't see)
            if (e.Los)
            {
                if (!e.Spotted) e.Spotted = true;
                var aim = Lead(e.Gun.position, t.turretBoltSpeed, Difficulty.TurretLead, 0);
                var to = aim - e.Gun.position;
                float wantYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, wantPitch = Mathf.Clamp(Mathf.Atan2(to.y, Flat(to).magnitude) * Mathf.Rad2Deg, -15f, 80f);
                float rate = t.turretTurnRate * Mathf.Sqrt(ag);
                e.Yaw = Mathf.MoveTowardsAngle(e.Yaw, wantYaw, rate * dt); e.Pitch = Mathf.MoveTowards(e.Pitch, wantPitch, rate * dt);
                e.Head.localRotation = Quaternion.Euler(0, e.Yaw, 0); e.Gun.localRotation = Quaternion.Euler(-e.Pitch, 0, 0);
                e.LockT += dt;
                if (!e.Locked && e.LockT >= t.turretLockTime / ag) { e.Locked = true; LockedOn?.Invoke(e); }
                if (e.Locked)
                {
                    lockNow = true;
                    e.Cooldown -= dt;
                    bool onTarget = Mathf.Abs(Mathf.DeltaAngle(e.Yaw, wantYaw)) < 6f && Mathf.Abs(e.Pitch - wantPitch) < 6f;
                    if (e.Cooldown <= 0 && onTarget)
                    {
                        e.Cooldown = t.turretFireInterval / ag * UnityEngine.Random.Range(.8f, 1.2f);
                        var muzzle = e.Gun.position + e.Gun.forward * 3.1f;   // Round 8: the Meshy barrels' tips (3.9 before)
                        var at = Lead(muzzle, t.turretBoltSpeed, Difficulty.TurretLead, Difficulty.TurretAimError / ag);
                        Shoot(muzzle, (at - muzzle).normalized * t.turretBoltSpeed, false, t.turretDamage, e, t.turretRange * 1.25f);
                        EnemyShots++; Sfx.Play(Sfx.Clip("turret_fire"), muzzle, 1f, 40f);
                        Threats.Add((e.Pos, Time.time + 2.5f)); FiredAtShip?.Invoke(e);
                    }
                }
            }
            else { e.LockT = Mathf.Max(0, e.LockT - dt * 2f); if (e.LockT <= 0) e.Locked = false; }
        }

        void SaucerTick(Enemy e, float dist, float dt, Tuning t, float ag, bool targetable, ref bool lockNow)
        {
            var sp = Ship.transform.position;
            if (Time.time >= e.LosAt) { e.LosAt = Time.time + .25f + (e.Index % 4) * .03f; e.Los = targetable && dist < t.saucerDetect && Sees(e.Pos, sp); }
            bool hears = targetable && (Ship.Landed || Ship.Mode == FlightMode.Hover) && dist < t.saucerHearLanded;
            if (e.Los || hears)
            {
                e.Lost = 0;
                if (!e.Chasing)
                {
                    e.Chasing = true; e.Pass = 0; if (!e.Spotted) { e.Spotted = true; } SaucerSpotted?.Invoke(e);
                    if (Difficulty.SaucerPairs) Partner(e);   // Round 9 (G): Nightmare: it brings its wingman
                }
            }
            else if (e.Chasing) { e.Lost += dt; if (e.Lost > 15f || !targetable) e.Chasing = false; }
            Vector3 target; float speed;
            if (!e.Chasing)
            {
                if (Difficulty.SaucersHunt && targetable && (Time.time > e.WaypointUntil || Flat(e.Waypoint - e.Pos).magnitude < 80f))
                {   // Round 9 (G): Hard / Nightmare: searching, toward a point near where the ship is now
                    float a = UnityEngine.Random.value * Mathf.PI * 2, r = UnityEngine.Random.Range(0f, t.saucerHuntRadius);
                    e.Waypoint = sp + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); e.Waypoint.y = Cruise(e.Waypoint); e.WaypointUntil = Time.time + t.saucerHuntEvery; Hunts++;
                }
                else if (Time.time > e.WaypointUntil || Flat(e.Waypoint - e.Pos).magnitude < 80f)
                {
                    float a = UnityEngine.Random.value * Mathf.PI * 2, r = UnityEngine.Random.Range(200f, 1100f);
                    e.Waypoint = e.Home + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); e.Waypoint.y = Cruise(e.Waypoint); e.WaypointUntil = Time.time + 40f;
                }
                target = e.Waypoint; speed = Difficulty.SaucersHunt ? Mathf.Lerp(t.saucerPatrolSpeed, t.saucerSpeed, .6f) : t.saucerPatrolSpeed;
            }
            else
            {
                // firing passes: in toward the ship (a little above it), past it, out for a few seconds, round again
                speed = t.saucerSpeed;
                if (e.Pass == 0) { target = sp + Vector3.up * 25f; if (dist < 120f) { e.Pass = 1; e.PassT = 0; } }
                else { e.PassT += dt; target = e.Pos + (e.Vel.sqrMagnitude > 1 ? e.Vel.normalized : Vector3.forward) * 400f + Vector3.up * 20f; if (e.PassT > 3.5f) e.Pass = 0; }
            }
            // steer: limited turn rate (less agile than the ship), keeping clear of the rock (and above the forest)
            var want = (target - e.Pos); if (want.sqrMagnitude < 1) want = e.Vel;
            var dir = Vector3.RotateTowards(e.Vel.sqrMagnitude > .01f ? e.Vel.normalized : want.normalized, want.normalized, t.saucerTurnRate * Mathf.Deg2Rad * dt, 0);
            float s = Mathf.MoveTowards(e.Vel.magnitude, speed, 30f * dt);
            e.Vel = dir * s;
            float clear = t.saucerClearance + (Field.Biome == Biome.Forest ? 150f : 0);
            float g = Field.SurfaceHeight(e.Pos.x, e.Pos.z);
            for (int k = 1; k <= 3; k++) { var q = e.Pos + Flat(e.Vel) * (k * .9f); g = Mathf.Max(g, Field.SurfaceHeight(q.x, q.z)); }
            if (e.Pos.y < g + clear) e.Vel.y = Mathf.Max(e.Vel.y, (g + clear - e.Pos.y) * 1.5f);
            e.Pos += e.Vel * dt;
            e.Pos.y = Mathf.Max(e.Pos.y, Field.SurfaceHeight(e.Pos.x, e.Pos.z) + 8f);
            var fl = Flat(e.Vel); float bank = 0;
            e.Root.SetPositionAndRotation(e.Pos, (fl.sqrMagnitude > .1f ? Quaternion.LookRotation(fl) : e.Root.rotation) * Quaternion.Euler(Mathf.Clamp(-e.Vel.y * .4f, -12, 12), 0, bank));
            e.Rim.localRotation = Quaternion.Euler(0, Time.time * 90f, 0);
            // fire on the way in, when it sees the ship and faces it
            if (e.Chasing && e.Pass == 0 && dist < t.saucerFireRange * 1.3f) lockNow = true;
            e.Cooldown -= dt; e.BurstGap -= dt;
            if (e.Chasing && e.Pass == 0 && e.Los && dist < t.saucerFireRange && Vector3.Angle(e.Vel, sp - e.Pos) < 35f && e.Cooldown <= 0 && e.BurstGap <= 0)
            {
                var muzzle = e.Pos + e.Root.forward * 6.5f - Vector3.up * 1.3f;
                var at = Lead(muzzle, t.saucerBoltSpeed, Difficulty.TurretLead, Difficulty.TurretAimError * 1.4f / ag);
                Shoot(muzzle, (at - muzzle).normalized * t.saucerBoltSpeed + e.Vel * .5f, false, t.saucerDamage, e, t.saucerFireRange * 1.4f);
                EnemyShots++; Sfx.Play(Sfx.Clip("saucer_fire"), muzzle, 1f, 40f);
                Threats.Add((e.Pos, Time.time + 2.5f)); FiredAtShip?.Invoke(e);
                e.Burst++; e.BurstGap = t.saucerBurstGap;
                if (e.Burst >= t.saucerBurst) { e.Burst = 0; e.Cooldown = t.saucerCooldown / ag; }
            }
        }

        public int Hunts, Pairings;   // checks (Round 9 G)
        void Partner(Enemy e)
        {
            Enemy best = null; float bd = 3500f;
            foreach (var o in All) if (o != e && !o.Dead && o.Kind == EnemyKind.Saucer && !o.Chasing) { float d = Vector3.Distance(o.Pos, e.Pos); if (d < bd) { bd = d; best = o; } }
            if (best == null) return;
            best.Chasing = true; best.Pass = 0; best.Lost = 0; best.Spotted = true; Pairings++;
        }

        void MoveBolts(float dt)
        {
            var cam = GameShell.I && GameShell.I.Rig ? GameShell.I.Rig.Head.transform : null;
            for (int i = 0; i < bolts.Count; i++)
            {
                var b = bolts[i]; if (!b.on) continue;
                var p0 = b.p; b.p += b.v * dt; b.life -= dt;
                bool end = b.life <= 0;
                if (!end && b.player)
                {
                    foreach (var e in All)
                    {
                        if (e.Dead) continue;
                        if (SegDist(p0, b.p, e.Centre) < e.Radius)
                        {
                            PlayerHits++; e.Hp -= b.dmg; Fx.Sparks(Closest(p0, b.p, e.Centre), -b.v.normalized, new Color(.6f, 1f, .8f), 22);
                            Sfx.Play(Sfx.Clip("bolt_hit_metal"), e.Centre, .9f, 30f);
                            if (e.Hp <= 0) Kill(e);
                            end = true; break;
                        }
                    }
                }
                else if (!end && Ship && Ship.gameObject.activeSelf && !Ship.Crashed)
                {
                    var c = Ship.transform.position;
                    float r = ShieldOn ? ShieldRadius : Difficulty.BoltHitRadius;
                    if (SegDist(p0, b.p, c) < r)
                    {
                        var at = Closest(p0, b.p, c); var n = (at - c).normalized;
                        if (ShieldOn) { Blocked++; Fx.Sparks(c + n * ShieldRadius, n, new Color(.5f, .8f, 1f), 26); HitShip?.Invoke(b.from, 0, true, c + n * ShieldRadius); }
                        else { EnemyHits++; Fx.Sparks(c + n * 2.5f, n, new Color(1f, .7f, .35f), 30); HitShip?.Invoke(b.from, b.dmg, false, c + n * 2.5f); }
                        end = true;
                    }
                }
                if (!end && Field != null)
                {
                    if (b.p.y < Field.SurfaceHeight(b.p.x, b.p.z) || Field.SolidPush(b.p, out _, out _)) { Fx.Sparks(b.p, -b.v.normalized, b.player ? new Color(.6f, 1f, .8f) : new Color(1f, .45f, .35f), 12); end = true; }
                }
                if (end) { b.on = false; b.trail.emitting = false; b.core.gameObject.SetActive(false); continue; }
                b.core.SetPositionAndRotation(b.p, Quaternion.LookRotation(b.v));
                if (cam)
                {
                    b.halo.rotation = Quaternion.LookRotation(b.halo.position - cam.position);
                    // Round 6 (B3): an enemy bolt's glow keeps a readable size far off (5 m at least)
                    float k = b.player ? 1 : Mathf.Max(1, Vector3.Distance(cam.position, b.p) * Tuning.T.boltGlowScale / 5f);
                    b.halo.localScale = b.player ? new Vector3(2f / .22f, 2f / .22f, 1) : new Vector3(10f * k, 10f * k, 1);
                }
            }
        }

        static Vector3 Closest(Vector3 a, Vector3 b, Vector3 p) { var ab = b - a; float l = ab.sqrMagnitude; if (l < 1e-6f) return a; float k = Mathf.Clamp01(Vector3.Dot(p - a, ab) / l); return a + ab * k; }
        static float SegDist(Vector3 a, Vector3 b, Vector3 p) => Vector3.Distance(Closest(a, b, p), p);

        // an enemy destroyed: an explosion in the world; a turret leaves its burnt base, a saucer is gone
        public void Kill(Enemy e)
        {
            if (e.Dead) return;
            e.Dead = true; e.Locked = false; e.Chasing = false;
            Fx.Blast(e.Centre, e.Kind == EnemyKind.Saucer ? 2.2f : 1.8f);
            Sfx.Play(Sfx.Clip("enemy_explosion"), e.Centre, 1f, 60f);
            if (e.Hum) Destroy(e.Hum.gameObject);
            if (e.Kind == EnemyKind.Turret) { e.Head.gameObject.SetActive(false); foreach (var r in e.Root.GetComponentsInChildren<MeshRenderer>()) if (r.name.StartsWith("TurretBase")) r.sharedMaterial = litDark; }
            else e.Root.gameObject.SetActive(false);
            Destroyed?.Invoke(e);
        }

        // checks: the nearest enemy bolt in flight (m), and the most bolts in flight at once since the last call
        public float NearestEnemyBolt(Vector3 p) { float d = float.MaxValue; foreach (var x in bolts) if (x.on && !x.player) d = Mathf.Min(d, Vector3.Distance(x.p, p)); return d; }
        public bool HasLiveBolts { get { foreach (var b in bolts) if (b.on) return true; return false; } }
        public int LiveBolts { get { int n = 0; foreach (var b in bolts) if (b.on) n++; return n; } }
    }
}
