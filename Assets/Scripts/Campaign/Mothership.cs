using UnityEngine;

namespace HostileHorizon
{
    // Round 2, item 9: the mothership high above the planet (placeholder model: Tools/Blender/mothership.py, whose numbers this
    // mirrors). It keeps station over the area the rescue ship is working in (drifting after it, ahead of it, at its altitude),
    // and stops when the ship comes in to dock. The bay: open at the stern, the floor opening closed by two doors, the cradle
    // in the ceiling holding the docked ship (the ship faces the open stern), the regeneration pod in the front corner.
    public sealed class Mothership : MonoBehaviour
    {
        public static readonly Vector3 PodEye = new Vector3(11.5f, -4.3f, 11.2f);   // mothership.py
        const float DoorTravel = 9.5f, MaxSpeed = 70f, Accel = 4f;

        Transform doorL, doorR; float doors, doorsTarget;   // 0 shut .. 1 open
        Vector3 velocity;
        public bool Holding;
        public Bounds HullBox { get; private set; }         // Fix 9.1 (A2): the hull's extent, mothership space (it is made at the origin, unturned)
        public Vector3 Local(Vector3 world) => transform.InverseTransformPoint(world);
        public Vector3 World(Vector3 local) => transform.TransformPoint(local);
        // Does the segment a-b (mothership space) pass within `margin` of the hull's box?
        public bool Blocked(Vector3 a, Vector3 b, float margin)
        {
            var box = HullBox; box.Expand(2 * margin); var d = b - a; float len = d.magnitude; if (len < 1e-3f) return box.Contains(a);
            if (box.Contains(a)) return true;
            return box.IntersectRay(new Ray(a, d / len), out float t) && t <= len;
        }                                // stays where it is (docking, docked, tow)
        public float DoorsOpen => doors;

        public Quaternion DockRotation => transform.rotation * Quaternion.Euler(0, 180, 0);
        public Vector3 DockPosition => transform.position;
        public Vector3 BelowBay(float metres) => transform.position + Vector3.down * metres;
        public Vector3 PodEyeWorld => transform.TransformPoint(PodEye);
        public Quaternion PodLook => Quaternion.LookRotation(transform.TransformPoint(new Vector3(2, -1.5f, -8)) - PodEyeWorld, Vector3.up);

        public static Mothership Create(Material litTemplate)
        {
            var go = new GameObject("Mothership");
            var m = go.AddComponent<Mothership>();
            var model = Instantiate(Resources.Load<GameObject>("Models/Mothership"), go.transform); model.name = "Model";
            model.transform.localRotation = Quaternion.Euler(0, 180, 0);   // Blender FBX files import facing back
            var glass = new Material(Shader.Find("HostileHorizon/Glass"));
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                string slot = r.name.Contains("__") ? r.name.Substring(r.name.IndexOf("__") + 2) : r.name;
                Material mat;
                if (slot == "glass") mat = glass;
                else if (slot == "glow" || slot == "window") { mat = new Material(Shader.Find("HostileHorizon/Glow")); mat.SetColor("_Color", slot == "glow" ? new Color(1f, .55f, .2f) * 2f : new Color(.55f, .8f, 1f) * 1.4f); }
                else if (slot.StartsWith("mx_")) { var hard = Resources.Load<Material>("Hard/" + slot.Substring(3)); mat = hard ? new Material(hard) : Plain(litTemplate, slot); }   // Fix 9.1 (A3): the Meshy textures
                else mat = Textured(litTemplate, slot);
                Slots.Add($"{r.name} -> {mat.name} ({(mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") ? mat.GetTexture("_BaseMap").name : "no texture")})");
                var all = r.sharedMaterials; for (int i = 0; i < all.Length; i++) all[i] = mat; r.sharedMaterials = all.Length > 0 ? all : new[] { mat };   // every slot (as the ship, Fix 8.2)
            }
            // Fix 9.1 (A2): the hull's extent in mothership space (the docking route goes round it)
            var hb = new Bounds(); bool first = true;
            foreach (var r in model.GetComponentsInChildren<Renderer>()) { var b = r.bounds; if (first) { hb = b; first = false; } else hb.Encapsulate(b); }
            m.HullBox = new Bounds(go.transform.InverseTransformPoint(hb.center), hb.size);
            // floor doors: two slabs that slide out sideways into the hull
            Material doorMat = Plain(litTemplate, "door");   // Fix 9.1: plain (a cube's faces would squeeze the whole texture atlas)
            m.doorL = Slab(go.transform, "Door left", doorMat); m.doorR = Slab(go.transform, "Door right", doorMat);
            // bay lights
            Lamp(go.transform, new Vector3(0, 7.5f, -2), 30, 2.2f, new Color(1f, .9f, .8f));
            Lamp(go.transform, new Vector3(0, 7.5f, -18), 26, 1.6f, new Color(1f, .9f, .8f));
            Lamp(go.transform, new Vector3(11.5f, -3.4f, 11.5f), 6, 2.5f, new Color(.55f, .8f, 1f));   // inside the regeneration pod
            m.SetDoors(0, true);
            return m;
        }

        // Fix 9.1 (A3): 0.9.0's mothership never loaded the Meshy materials (Resources/Hard/*.mat, as the ship and enemies do): every part,
        // the Meshy hull, wall modules, cradle and pod included, was drawn plain grey. The code-built parts (the openings' lining and trim,
        // the bay's liners, floor strips and doors) now take the bay wall's / hull's Meshy textures, tinted, on their own UVs.
        public static readonly System.Collections.Generic.List<string> Slots = new System.Collections.Generic.List<string>();   // checks
        static Material Plain(Material litTemplate, string slot)
        {
            var mat = new Material(litTemplate) { name = "plain " + slot };
            Color c = slot == "hull" ? new Color(.46f, .48f, .5f) : slot == "panel" ? new Color(.3f, .32f, .34f) : slot == "bay" ? new Color(.2f, .21f, .22f) : slot == "metal" ? new Color(.5f, .52f, .55f) : slot == "door" ? new Color(.28f, .29f, .3f) : new Color(.13f, .14f, .15f);
            mat.SetColor("_BaseColor", c); mat.SetFloat("_Smoothness", slot == "metal" ? .55f : .3f); mat.SetFloat("_Metallic", slot == "metal" ? .6f : .15f);
            return mat;
        }
        static Material Textured(Material litTemplate, string slot)
        {
            var src = Resources.Load<Material>(slot == "metal" || slot == "hull" ? "Hard/mothership" : "Hard/bay_wall");
            if (!src) return Plain(litTemplate, slot);
            var mat = new Material(src) { name = "textured " + slot };
            Color tint = slot == "metal" ? new Color(.95f, .97f, 1f) : slot == "bay" ? new Color(.7f, .72f, .75f) : slot == "door" ? new Color(.85f, .86f, .88f) : Color.white;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mat.GetColor("_BaseColor") * tint);
            return mat;
        }

        static Transform Slab(Transform parent, string name, Material m)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(d.GetComponent<Collider>());
            d.name = name; d.transform.SetParent(parent, false); d.transform.localScale = new Vector3(9.05f, .8f, 20.1f);
            d.GetComponent<Renderer>().sharedMaterial = m; return d.transform;
        }

        static void Lamp(Transform parent, Vector3 at, float range, float intensity, Color c)
        {
            var l = new GameObject("Bay light").AddComponent<Light>(); l.transform.SetParent(parent, false); l.transform.localPosition = at;
            l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = c; l.shadows = LightShadows.None;
        }

        public void SetDoors(float open, bool now = false) { doorsTarget = open; if (now) { doors = open; PlaceDoors(); } }
        void PlaceDoors()
        {
            float e = doors * doors * (3 - 2 * doors);
            doorL.localPosition = new Vector3(-4.5f - e * DoorTravel, -6.5f, 0);
            doorR.localPosition = new Vector3(4.5f + e * DoorTravel, -6.5f, 0);
        }

        // Put it above a point (new sortie / Continue), facing a heading.
        public void Place(Vector3 over, float heading)
        {
            transform.SetPositionAndRotation(new Vector3(over.x, Tuning.T.mothershipAltitude, over.z), Quaternion.Euler(0, heading, 0));
            velocity = Vector3.zero;
        }

        // Keep station over the rescue ship: drift towards a point a little ahead of it, at the mothership's altitude.
        public void Follow(Vector3 shipPos, Vector3 shipForward, float dt)
        {
            Vector3 flat = Vector3.ProjectOnPlane(shipForward, Vector3.up); if (flat.sqrMagnitude > .01f) flat.Normalize();
            var target = shipPos + flat * 400f; target.y = Tuning.T.mothershipAltitude;
            Vector3 to = target - transform.position; to.y = 0;
            Vector3 want = Vector3.ClampMagnitude(to * .05f, MaxSpeed);
            velocity = Vector3.MoveTowards(velocity, want, Accel * dt);
            transform.position += velocity * dt;
        }

        // Fly to a point over something (the tow, the recall), quicker than station keeping. Fix 2.5: about 3x quicker, and it
        // brakes in time (speed from the distance left) so it never overshoots.
        public bool MoveOver(Vector3 point, float dt)
        {
            var t = Tuning.T;
            Vector3 to = point - transform.position; to.y = t.mothershipAltitude - transform.position.y;
            float d = to.magnitude;
            Vector3 want = d > .01f ? to / d * Mathf.Min(t.motherComeSpeed, Mathf.Sqrt(2f * t.motherComeAccel * .8f * d), d * 2f) : Vector3.zero;
            velocity = Vector3.MoveTowards(velocity, want, t.motherComeAccel * dt);
            transform.position += velocity * dt;
            return new Vector2(to.x, to.z).magnitude < 3f && velocity.magnitude < 3f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Holding) { velocity = Vector3.MoveTowards(velocity, Vector3.zero, 8f * dt); transform.position += velocity * dt; }
            if (doors != doorsTarget) { doors = Mathf.MoveTowards(doors, doorsTarget, dt / 2.2f); PlaceDoors(); }
        }
    }
}
