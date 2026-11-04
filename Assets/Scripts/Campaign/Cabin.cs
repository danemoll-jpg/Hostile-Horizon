using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HostileHorizon
{
    // Round 3, items B7-B9: the cabin behind the cockpit (the Blender ship has only its hull there; Round 6 models it): floor,
    // walls, two benches, ceiling lights, the rescued pilots sitting in it, the low hatch in the bulkhead behind the pilot's left
    // shoulder (Tools/Blender/ship.py cuts it; its door is made here, so it can be hit and give way), a dim red emergency lamp in
    // the cockpit, and the cabin camera: a camera in the cabin's front corner drawn on a small cockpit display (and on the HUD when
    // asked), so the passengers can be watched. Ship space (metres), on the ship (not the turned model).
    public sealed class Cabin : MonoBehaviour
    {
        public const int W = 384, H = 216;
        public static readonly Vector3 HatchCentre = new Vector3(-.625f, .475f, 1.17f), HatchCockpitSide = new Vector3(-.625f, .1f, 1.6f), Inside = new Vector3(0, -.55f, -1.3f);
        static readonly Vector3 CamAt = new Vector3(1.15f, .76f, .75f), CamLook = new Vector3(-.5f, -.35f, -2.6f);
        static readonly Vector3[] Seats = { new Vector3(-1.08f, -.52f, .35f), new Vector3(1.08f, -.52f, .35f), new Vector3(-1.08f, -.52f, -.45f), new Vector3(1.08f, -.52f, -.75f), new Vector3(1.08f, -.52f, -1.85f), new Vector3(-1.08f, -.52f, -2.8f) };
        public Camera Cam { get; private set; }
        public RenderTexture Feed { get; private set; }
        public bool FeedOn;                  // set by the Campaign: drawn while it is shown
        public Transform Door { get; private set; }
        readonly List<Light> lamps = new List<Light>(); readonly List<Material> strips = new List<Material>();
        readonly List<(Material m, Color c)> surfaces = new List<(Material, Color)>();   // darkened with the lamps (the sky's ambient light would still light them)
        Light emergency; float emergencyLevel, emergencyWant;
        readonly List<Transform> figures = new List<Transform>(); readonly List<int> seated = new List<int>();
        int sleeperSeat = -1; bool sleeperTell; float clock; int frame;
        Vector3 doorRest; float doorShake; bool doorGone; Vector3 doorVel; Vector3 doorSpin;
        ShipController ship;

        public static Cabin Create(ShipController ship, Material lit)
        {
            var c = new GameObject("Cabin").AddComponent<Cabin>(); c.ship = ship; c.transform.SetParent(ship.transform, false);
            Material M(Color col, float smooth = .3f, float metal = 0) { var m = new Material(lit); m.SetColor("_BaseColor", col); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal); return m; }
            var wall = M(new Color(.22f, .23f, .24f)); var floor = M(new Color(.12f, .12f, .13f), .2f); var bench = M(new Color(.2f, .17f, .15f)); var door = M(new Color(.30f, .32f, .33f), .4f, .3f);
            void Box(string n, Vector3 at, Vector3 size, Material m) { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>()); g.name = n; g.transform.SetParent(c.transform, false); g.transform.localPosition = at; g.transform.localScale = size; g.GetComponent<Renderer>().sharedMaterial = m; }
            Box("Cabin floor", new Vector3(0, -.64f, -1.2f), new Vector3(2.9f, .05f, 4.7f), floor);
            Box("Cabin ceiling", new Vector3(0, .92f, -1.2f), new Vector3(2.9f, .05f, 4.7f), wall);
            Box("Cabin wall right", new Vector3(1.45f, .14f, -1.2f), new Vector3(.05f, 1.6f, 4.7f), wall);
            Box("Cabin wall left front", new Vector3(-1.45f, .14f, -.05f), new Vector3(.05f, 1.6f, 2.4f), wall);   // the airlock gap z -2.15 .. -1.05
            Box("Cabin wall left back", new Vector3(-1.45f, .14f, -2.85f), new Vector3(.05f, 1.6f, 1.4f), wall);
            Box("Cabin wall left over airlock", new Vector3(-1.45f, .82f, -1.6f), new Vector3(.05f, .2f, 1.1f), wall);
            Box("Cabin back wall", new Vector3(0, .14f, -3.55f), new Vector3(2.9f, 1.6f, .05f), wall);
            Box("Cabin step", new Vector3(-.625f, -.35f, 1.0f), new Vector3(.7f, .6f, .3f), floor);   // up to the hatch
            foreach (float x in new[] { -1.15f, 1.15f }) { Box("Bench", new Vector3(x, -.4f, -1.2f), new Vector3(.45f, .08f, 4.2f), bench); Box("Bench back", new Vector3(x + Mathf.Sign(x) * .2f, .0f, -1.2f), new Vector3(.06f, .7f, 4.2f), bench); }
            // the hatch's door, closed in the bulkhead's opening
            var d = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(d.GetComponent<Collider>()); d.name = "Cockpit hatch door";
            d.transform.SetParent(c.transform, false); d.transform.localPosition = HatchCentre; d.transform.localScale = new Vector3(.64f, 1.04f, .05f); d.GetComponent<Renderer>().sharedMaterial = door;
            c.Door = d.transform; c.doorRest = HatchCentre;
            foreach (var m in new[] { wall, floor, bench, door }) c.surfaces.Add((m, m.GetColor("_BaseColor")));
            // ceiling lamps: two warm lights and their glowing strips
            foreach (float z in new[] { .1f, -2.3f })
            {
                var l = new GameObject("Cabin lamp").AddComponent<Light>(); l.transform.SetParent(c.transform, false); l.transform.localPosition = new Vector3(0, .8f, z);
                l.type = LightType.Point; l.range = 3.8f; l.intensity = 1.6f; l.color = new Color(1f, .9f, .75f); l.shadows = LightShadows.None; c.lamps.Add(l);
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(c.transform, false); g.transform.localPosition = new Vector3(0, .89f, z); g.transform.localScale = new Vector3(.5f, .02f, .12f);
                var m = new Material(Shader.Find("HostileHorizon/Glow")); g.GetComponent<Renderer>().sharedMaterial = m; c.strips.Add(m);
            }
            c.emergency = new GameObject("Emergency lamp").AddComponent<Light>(); c.emergency.transform.SetParent(c.transform, false); c.emergency.transform.localPosition = new Vector3(0, 1.3f, 1.6f);
            c.emergency.type = LightType.Point; c.emergency.range = 3.5f; c.emergency.intensity = 0; c.emergency.color = new Color(1f, .12f, .08f); c.emergency.shadows = LightShadows.None;
            // the passengers (shown for the pilots aboard)
            for (int i = 0; i < Seats.Length; i++) c.figures.Add(Figure(c.transform, Seats[i], lit, i));
            // the cabin camera
            var cg = new GameObject("Cabin camera"); cg.transform.SetParent(c.transform, false); cg.transform.localPosition = CamAt; cg.transform.localRotation = Quaternion.LookRotation(CamLook - CamAt, Vector3.up);
            c.Feed = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { name = "Cabin camera feed" }; c.Feed.Create();
            c.Cam = cg.AddComponent<Camera>(); c.Cam.targetTexture = c.Feed; c.Cam.nearClipPlane = .05f; c.Cam.farClipPlane = 12f; c.Cam.fieldOfView = 80f; c.Cam.enabled = false;
            var cd = c.Cam.GetUniversalAdditionalCameraData(); cd.renderShadows = false; cd.renderPostProcessing = false;
            c.Lights(true);
            return c;
        }

        // A seated pilot in the corps' orange suit, facing across the cabin.
        static Transform Figure(Transform parent, Vector3 seat, Material lit, int i)
        {
            Material M(Color col, float smooth = .3f) { var m = new Material(lit); m.SetColor("_BaseColor", col); m.SetFloat("_Smoothness", smooth); return m; }
            var suit = M(new Color(.85f, .38f, .1f)); var helmet = M(new Color(.88f, .88f, .85f), .6f); var visor = M(new Color(.05f, .06f, .08f), .95f);
            var root = new GameObject("Passenger").transform; root.SetParent(parent, false); root.localPosition = seat; root.localRotation = Quaternion.Euler(0, seat.x < 0 ? 90 : -90, 0);
            void P(PrimitiveType t, Transform p, Vector3 at, Vector3 size, Material m, Vector3 rot = default) { var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(p, false); g.transform.localPosition = at; g.transform.localScale = size; g.transform.localRotation = Quaternion.Euler(rot); g.GetComponent<Renderer>().sharedMaterial = m; }
            P(PrimitiveType.Capsule, root, new Vector3(0, .45f, -.05f), new Vector3(.44f, .34f, .3f), suit);
            var head = new GameObject("Head").transform; head.SetParent(root, false); head.localPosition = new Vector3(0, .92f, -.03f);
            P(PrimitiveType.Sphere, head, Vector3.zero, Vector3.one * .29f, helmet); P(PrimitiveType.Sphere, head, new Vector3(0, .01f, .1f), new Vector3(.23f, .15f, .13f), visor);
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Capsule, root, new Vector3(s * .11f, .14f, .2f), new Vector3(.16f, .22f, .16f), suit, new Vector3(90, 0, 0));
                P(PrimitiveType.Capsule, root, new Vector3(s * .11f, -.12f, .42f), new Vector3(.15f, .2f, .15f), suit);
                P(PrimitiveType.Capsule, root, new Vector3(s * .27f, .4f, .1f), new Vector3(.12f, .26f, .12f), suit, new Vector3(-30, 0, 0));
            }
            root.gameObject.SetActive(false);
            return root;
        }

        public void Lights(bool on) { foreach (var (m, c) in surfaces) m.SetColor("_BaseColor", on ? c : c * .08f); foreach (var l in lamps) l.enabled = on; foreach (var m in strips) m.SetColor("_Color", on ? new Color(1f, .92f, .78f) * 1.6f : new Color(.02f, .02f, .02f)); LightsOn = on; }
        public bool LightsOn { get; private set; }
        public void Emergency(bool on) => emergencyWant = on ? .9f : 0;

        // Who sits where: one figure per pilot aboard; sleeper = the cabin index of the Sleeper (or -1), tell = it shows its tell.
        public void Seat(List<int> cabin, int sleeper, bool tell)
        {
            seated.Clear(); seated.AddRange(cabin); sleeperSeat = -1; sleeperTell = tell;
            for (int i = 0; i < figures.Count; i++)
            {
                bool on = i < cabin.Count; figures[i].gameObject.SetActive(on);
                if (on && cabin[i] == sleeper) sleeperSeat = i;
                figures[i].localRotation = Quaternion.Euler(0, Seats[i].x < 0 ? 90 : -90, 0);
                figures[i].Find("Head").localRotation = Quaternion.identity;
            }
        }
        public int SeatOf(int pilotIndex) => seated.IndexOf(pilotIndex);
        public Vector3 SeatPoint(int seat) => seat >= 0 && seat < Seats.Length ? Seats[seat] : Inside;
        public void HideSeat(int seat) { if (seat >= 0 && seat < figures.Count) figures[seat].gameObject.SetActive(false); }
        public void SlumpSeat(int seat) { if (seat >= 0 && seat < figures.Count) { figures[seat].localRotation *= Quaternion.Euler(0, 0, 70); figures[seat].Find("Head").localRotation = Quaternion.Euler(60, 0, 0); } }

        public void HitDoor() { doorShake = 1f; }
        public void BreakDoor() { doorGone = true; doorVel = new Vector3(.2f, 1.2f, 3.2f); doorSpin = new Vector3(-180f, 40f, 30f); }
        public void ResetDoor() { doorGone = false; doorShake = 0; Door.localPosition = doorRest; Door.localRotation = Quaternion.identity; Door.gameObject.SetActive(true); }

        void LateUpdate()
        {
            float dt = Time.deltaTime; clock += dt;
            emergencyLevel = Mathf.MoveTowards(emergencyLevel, emergencyWant, dt * .6f);
            emergency.intensity = emergencyLevel * (.75f + .25f * Mathf.Sin(clock * 3f));   // a slow red pulse (no strobe)
            // the passengers sway a little with the ship; the Sleeper, when it shows its tell, doesn't: it turns its head to the camera, too far
            for (int i = 0; i < figures.Count; i++)
            {
                if (!figures[i].gameObject.activeSelf) continue;
                var head = figures[i].Find("Head");
                if (i == sleeperSeat && sleeperTell)
                {
                    var to = transform.InverseTransformPoint(Cam.transform.position) - figures[i].localPosition; float yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg - figures[i].localEulerAngles.y;
                    head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(-8, yaw, 78f + Mathf.Sin(clock * .3f) * 6f), 1 - Mathf.Exp(-.5f * dt));
                }
                else head.localRotation = Quaternion.Euler(Mathf.Sin(clock * 1.1f + i) * 4f, Mathf.Sin(clock * .7f + i * 2) * 10f, 0);
            }
            // the hatch's door: shaken by a blow, or flying into the cockpit when it gives way
            if (doorGone)
            {
                doorVel += Vector3.down * 9.8f * dt; var p = Door.localPosition + doorVel * dt;
                if (p.y < -.03f) { p.y = -.03f; doorVel = Vector3.zero; doorSpin = Vector3.zero; Door.localRotation = Quaternion.Euler(90, 20, 0); }
                Door.localPosition = p; Door.localRotation *= Quaternion.Euler(doorSpin * dt);
            }
            else if (doorShake > 0)
            {
                doorShake = Mathf.MoveTowards(doorShake, 0, dt * 2.5f);
                Door.localPosition = doorRest + new Vector3(0, 0, doorShake * .06f * Mathf.Sin(clock * 70f) + doorShake * .04f);
                Door.localRotation = Quaternion.Euler(doorShake * 6f * Mathf.Sin(clock * 53f), 0, doorShake * 3f);
            }
            if (FeedOn && ship.gameObject.activeInHierarchy && (frame++ & 1) == 0) Cam.Render();   // every other frame
        }
    }
}
