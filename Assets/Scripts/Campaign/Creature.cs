using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 3, item B2: the creature an alien really is (Tools/Blender/creature.py; Joints mirrors its JOINTS: change both together).
    // The model's segments are hung on joints at their pivots, so the game poses it: standing, crawling, pressed on the canopy,
    // lunging, thrashing as it dies. Simple procedural animation (Round 6 is the art pass): it breathes, its head snaps to new angles
    // in jerks (never smoothly), the jaw splits open. Wet skin (smooth, dark), milky eyes.
    public sealed class Creature : MonoBehaviour
    {
        public enum Pose { Stand, Crawl, Canopy, Lunge, Thrash, Dead }
        static readonly (string name, string parent, Vector3 at)[] Joints =
        {
            ("Hips", null, new Vector3(0, 1.12f, 0)), ("Chest", "Hips", new Vector3(0, 1.55f, .12f)), ("Neck", "Chest", new Vector3(0, 1.92f, .34f)),
            ("Head", "Neck", new Vector3(0, 2.05f, .48f)), ("Jaw", "Head", new Vector3(0, 1.98f, .58f)),
            ("ArmL", "Chest", new Vector3(-.30f, 1.84f, .26f)), ("ElbowL", "ArmL", new Vector3(-.40f, 1.30f, .14f)), ("WristL", "ElbowL", new Vector3(-.44f, .72f, .34f)),
            ("ArmR", "Chest", new Vector3(.30f, 1.84f, .26f)), ("ElbowR", "ArmR", new Vector3(.40f, 1.30f, .14f)), ("WristR", "ElbowR", new Vector3(.44f, .72f, .34f)),
            ("LegL", "Hips", new Vector3(-.16f, 1.12f, 0)), ("KneeL", "LegL", new Vector3(-.20f, .74f, .22f)), ("HockL", "KneeL", new Vector3(-.20f, .36f, -.18f)),
            ("LegR", "Hips", new Vector3(.16f, 1.12f, 0)), ("KneeR", "LegR", new Vector3(.20f, .74f, .22f)), ("HockR", "KneeR", new Vector3(.20f, .36f, -.18f)),
        };
        public static Vector3 HeadAt => new Vector3(0, 2.1f, .62f);   // the face's middle, creature space (standing)

        readonly Dictionary<string, Transform> j = new Dictionary<string, Transform>();
        readonly Dictionary<string, Vector3> want = new Dictionary<string, Vector3>(), now = new Dictionary<string, Vector3>();
        Transform body; Vector3 bodyWant, bodyNow;   // the hips' offset (crouching)
        public Pose Current { get; private set; } = Pose.Stand;
        public float Jaw;            // 0 shut .. 1 wide open
        public float Speed = 6f;     // how fast poses blend
        float clock, nextTwitch; Vector3 twitch; System.Random rng;
        Renderer[] rs;

        // Fix 3.1: each type's own creature (Dan's art pipeline, CharacterRig), its poses played as animations: Stand idle, Crawl crawl
        // (the Stalker: its crouched walk), Canopy attack (clawing at the glass), Lunge roar, Thrash roar, Dead death; Act plays any
        // other clip at a beat (climb, leap, attack, hit). No jaw bone: Jaw does nothing on these. The code-made creature below if the
        // type's model is missing.
        CharacterRig rig;
        public CharacterRig Rig => rig;
        public AlienType Type { get; private set; }

        public static Creature Create(Material lit, int seed = 1, AlienType type = AlienType.None)
        {
            var root = new GameObject("Creature"); var c = root.AddComponent<Creature>(); c.rng = new System.Random(seed); c.Type = type;
            if (type != AlienType.None && (c.rig = CharacterRig.Create(CharacterRig.ModelFor(type), root.transform)))
            {
                root.name = "Creature (" + Aliens.Names[(int)type] + ")";
                c.rs = c.rig.Renderers; root.SetActive(false); return c;
            }
            var model = Instantiate(Resources.Load<GameObject>("Models/Creature"), root.transform); model.name = "Model";
            model.transform.localRotation = Quaternion.Euler(0, 180, 0);   // Blender FBX files import facing back
            c.body = new GameObject("Body").transform; c.body.SetParent(root.transform, false);
            foreach (var (name, parent, at) in Joints)
            {
                var t = new GameObject(name).transform; t.SetParent(parent == null ? c.body : c.j[parent], false);
                t.position = root.transform.TransformPoint(at); c.j[name] = t; c.want[name] = c.now[name] = Vector3.zero;
            }
            Material M(Color col, float smooth) { var m = new Material(lit); m.SetColor("_BaseColor", col); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0); return m; }
            var mats = new Dictionary<string, Material>
            {
                ["skin"] = M(new Color(.26f, .25f, .23f), .86f), ["hide"] = M(new Color(.08f, .075f, .075f), .8f), ["bone"] = M(new Color(.74f, .70f, .56f), .45f), ["gum"] = M(new Color(.28f, .03f, .045f), .85f),
            };
            var eye = new Material(Shader.Find("HostileHorizon/Glow")); eye.SetColor("_Color", new Color(.55f, .6f, .48f) * .9f);
            var list = new List<Renderer>();
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                string n = r.name; int k = n.IndexOf("__"); string seg = k > 0 ? n.Substring(0, k) : "Hips", slot = k > 0 ? n.Substring(k + 2) : "skin";
                r.sharedMaterial = slot == "eye" ? eye : mats.TryGetValue(slot, out var m) ? m : mats["skin"];
                if (c.j.TryGetValue(seg, out var joint)) r.transform.SetParent(joint, true);
                list.Add(r);
            }
            c.rs = list.ToArray();
            root.SetActive(false);
            return c;
        }

        public void Show(bool on) => gameObject.SetActive(on);
        public void Shadows(bool on) { foreach (var r in rs) r.shadowCastingMode = on ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off; }
        public Transform Joint(string name) => rig ? null : j[name];
        public Vector3 HeadWorld => rig ? rig.Head.position + transform.forward * .14f : j["Head"].position + j["Head"].rotation * new Vector3(0, .05f, .14f);   // the face

        void Set(string n, float x, float y = 0, float z = 0) => want[n] = new Vector3(x, y, z);

        public void Act(string clip, float speed = 1f) { if (rig) rig.Play(clip, .2f, speed, true); }

        // Joint angles (degrees about the creature's own axes; +x bends a part forward / down).
        public void SetPose(Pose p, bool instant = false)
        {
            Current = p;
            if (rig)
            {
                string clip = p == Pose.Stand ? "idle" : p == Pose.Crawl ? (Type == AlienType.Stalker ? "crouch_walk" : "crawl") : p == Pose.Canopy ? "attack" : p == Pose.Dead ? "death" : "roar";
                rig.Play(clip, instant ? 0 : .25f);
                return;
            }
            foreach (var k in new List<string>(want.Keys)) want[k] = Vector3.zero;
            bodyWant = Vector3.zero;
            switch (p)
            {
                case Pose.Stand:   // hunched, arms hanging, head pushed forward
                    Set("Chest", 14); Set("Neck", -18); Set("Head", 4); Set("ArmL", 4, 0, -6); Set("ArmR", 4, 0, 6); Set("ElbowL", -12); Set("ElbowR", -12);
                    break;
                case Pose.Crawl:   // low on all fours: the long arms reach forward, the head up and forward
                    bodyWant = new Vector3(0, -.45f, 0);
                    Set("Hips", 30); Set("Chest", 52); Set("Neck", -64); Set("Head", -24);
                    Set("ArmL", -86, 0, -14); Set("ArmR", -86, 0, 14); Set("ElbowL", 46); Set("ElbowR", 46); Set("WristL", 30); Set("WristR", 30);
                    Set("LegL", -70, 0, -12); Set("LegR", -70, 0, 12); Set("KneeL", 85); Set("KneeR", 85); Set("HockL", -40); Set("HockR", -40);
                    break;
                case Pose.Canopy:  // lying forward on the glass, arms spread wide, the face pressed to it
                    Set("Chest", 8); Set("Neck", -30); Set("Head", -22);
                    Set("ArmL", -110, 0, -50); Set("ArmR", -110, 0, 50); Set("ElbowL", 20); Set("ElbowR", 20); Set("WristL", 40); Set("WristR", 40);
                    Set("LegL", 10, 0, -10); Set("LegR", 10, 0, 10); Set("KneeL", 30); Set("KneeR", 30);
                    break;
                case Pose.Lunge:   // up, arms wide and forward, the head thrust out
                    Set("Chest", 30); Set("Neck", -40); Set("Head", -14);
                    Set("ArmL", -95, 0, -38); Set("ArmR", -95, 0, 38); Set("ElbowL", -30); Set("ElbowR", -30); Set("WristL", 25); Set("WristR", 25);
                    Set("LegL", -20); Set("LegR", -20); Set("KneeL", 30); Set("KneeR", 30);
                    break;
                case Pose.Thrash:  // up on its legs, arms thrown about (Update adds the jerks)
                    Set("Chest", -10); Set("Neck", -20); Set("Head", -30); Set("ArmL", -140, 0, -40); Set("ArmR", -60, 0, 60); Set("ElbowL", -40); Set("ElbowR", -70);
                    break;
                case Pose.Dead:    // folded on the ground
                    bodyWant = new Vector3(0, -.9f, 0);
                    Set("Hips", 80); Set("Chest", 40); Set("Neck", 30); Set("Head", 20); Set("ArmL", -20, 0, -70); Set("ArmR", 30, 0, 80); Set("ElbowL", 60); Set("ElbowR", 90);
                    Set("LegL", -60, 0, -20); Set("LegR", -30, 0, 25); Set("KneeL", 110); Set("KneeR", 90);
                    break;
            }
            if (instant) { foreach (var k in want.Keys) now[k] = want[k]; bodyNow = bodyWant; Apply(); }
        }

        void Update()
        {
            float dt = Time.deltaTime; if (dt <= 0 || rig) return;
            clock += dt;
            // jerks: the head (and while thrashing, the arms) snap to a new angle at random moments, then ease back
            if (clock > nextTwitch)
            {
                nextTwitch = clock + (Current == Pose.Thrash ? .12f : .6f + (float)rng.NextDouble() * 1.8f);
                twitch = new Vector3(((float)rng.NextDouble() - .5f) * 30f, ((float)rng.NextDouble() - .5f) * 50f, ((float)rng.NextDouble() - .5f) * 70f);
            }
            twitch = Vector3.Lerp(twitch, Vector3.zero, 1 - Mathf.Exp(-2.5f * dt));
            float k = 1 - Mathf.Exp(-Speed * dt);
            foreach (var n in new List<string>(want.Keys)) now[n] = Vector3.Lerp(now[n], want[n], k);
            bodyNow = Vector3.Lerp(bodyNow, bodyWant, k);
            Apply();
            // on top: breathing, the jerks, the jaw
            j["Head"].localRotation *= Quaternion.Euler(twitch * (Current == Pose.Dead ? 0 : 1));
            if (Current == Pose.Thrash) { j["ArmL"].localRotation *= Quaternion.Euler(twitch.x * 2.5f, 0, twitch.z); j["ArmR"].localRotation *= Quaternion.Euler(-twitch.z, 0, twitch.x * 2f); j["Chest"].localRotation *= Quaternion.Euler(twitch.y * .4f, 0, twitch.x * .5f); }
            float breath = Current == Pose.Dead ? 0 : Mathf.Sin(clock * 2.3f);
            j["Chest"].localScale = new Vector3(1 + breath * .03f, 1 + breath * .02f, 1 + breath * .05f);
            j["Jaw"].localRotation = Quaternion.Euler(Mathf.Clamp01(Jaw) * 42f + (Jaw > .5f ? Mathf.Sin(clock * 31f) * 3f : 0), 0, 0);
        }

        void Apply()
        {
            body.localPosition = bodyNow;
            foreach (var n in now.Keys) if (n != "Jaw") j[n].localRotation = Quaternion.Euler(now[n]);
        }
    }
}
