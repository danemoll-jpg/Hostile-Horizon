using System.Collections.Generic;
using UnityEngine;

namespace HostileHorizon
{
    // Round 2: the moving parts the Blender ship doesn't have yet (Round 6 models them): three landing legs that come down in
    // landing mode, the airlock door on the left side (it slides back to open), the engine nozzles' glow (dark with the engines
    // off), and the ship's paint: own ship, the corps loaner (yellow), the burnt wreck.
    public sealed class ShipExtras : MonoBehaviour
    {
        public enum Look { Own, Loaner, Wreck }
        public static readonly Vector3 DoorAt = new Vector3(-1.66f, .95f, .81f);   // Round 8: the Meshy hull's airlock door (ship.py's renders); was (-1.80, .05, -1.6)
        ShipController ship;
        readonly List<Transform> struts = new List<Transform>(), pads = new List<Transform>();
        Transform door, hole; float doorOpen, openUntil;
        readonly List<(Material m, Color c, float smooth, string slot)> paint = new List<(Material, Color, float, string)>();
        readonly List<Material> nozzles = new List<Material>();
        Look look;
        public int EngineLevel;   // Round 6 (C3): the Engine upgrade: the nozzles burn whiter

        public static ShipExtras Add(ShipController s, Material lit, IEnumerable<(Material m, string slot)> materials)
        {
            var x = s.gameObject.AddComponent<ShipExtras>(); x.ship = s;
            foreach (var (m, slot) in materials)
            {
                if (slot == "glow") x.nozzles.Add(m);
                else if (m.HasProperty("_BaseColor")) x.paint.Add((m, m.GetColor("_BaseColor"), m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : .3f, slot));
            }
            var metal = new Material(lit); metal.SetColor("_BaseColor", new Color(.45f, .46f, .48f)); metal.SetFloat("_Metallic", .6f); metal.SetFloat("_Smoothness", .5f);
            var panel = new Material(lit); panel.SetColor("_BaseColor", new Color(.30f, .32f, .33f));
            var black = new Material(lit); black.SetColor("_BaseColor", new Color(.02f, .02f, .02f)); black.SetFloat("_Smoothness", 0);
            foreach (var f in ShipController.Feet)
            {
                x.struts.Add(Part(PrimitiveType.Cylinder, s.transform, new Vector3(f.x, -.6f, f.z), new Vector3(.18f, .55f, .18f), metal));
                x.pads.Add(Part(PrimitiveType.Cylinder, s.transform, new Vector3(f.x, -.9f, f.z), new Vector3(.7f, .05f, .7f), panel));
            }
            x.hole = Part(PrimitiveType.Cube, s.transform, DoorAt + new Vector3(.015f, 0, 0), new Vector3(.02f, 1.18f, 1.0f), black);
            x.door = Part(PrimitiveType.Cube, s.transform, DoorAt, new Vector3(.04f, 1.22f, 1.05f), panel);
            x.paint.Add((panel, panel.GetColor("_BaseColor"), .3f, "panel")); x.paint.Add((metal, metal.GetColor("_BaseColor"), .5f, "metal"));
            return x;
        }

        static Transform Part(PrimitiveType t, Transform parent, Vector3 at, Vector3 size, Material m)
        {
            var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false); g.transform.localPosition = at; g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = m; return g.transform;
        }

        public void OpenAirlock(float seconds) { openUntil = Time.time + seconds; Sfx.Play(Sfx.Hiss, transform.TransformPoint(DoorAt), .9f, 3f); }
        public bool AirlockOpen => doorOpen > .9f;
        public bool AirlockShut => doorOpen <= 0 && Time.time >= openUntil;   // Round 6 (A2)
        public void CloseAirlock() => openUntil = 0;   // Round 3: it shuts behind what came in

        public void SetLook(Look l)
        {
            look = l;
            foreach (var (m, c, smooth, slot) in paint)
            {
                Color col = c; float sm = smooth;
                if (l == Look.Wreck) { col = new Color(c.r * .16f, c.g * .15f, c.b * .14f); sm = .08f; }
                else if (l == Look.Loaner && slot == "hull") col = new Color(.80f, .64f, .22f);
                else if (l == Look.Loaner && slot == "mx_ship") col = new Color(1f, .8f, .32f);   // Round 8: over the hull's own paint
                m.SetColor("_BaseColor", col); m.SetFloat("_Smoothness", sm);
            }
        }

        void LateUpdate()
        {
            float g = ship.Gear, e = g * g * (3 - 2 * g);
            for (int i = 0; i < struts.Count; i++)
            {
                var f = ShipController.Feet[i];
                struts[i].localPosition = new Vector3(f.x, Mathf.Lerp(-.55f, -1.25f, e), f.z);
                pads[i].localPosition = new Vector3(f.x, Mathf.Lerp(-.88f, -ShipController.GearDrop + .05f, e), f.z);
                struts[i].gameObject.SetActive(g > .01f); pads[i].gameObject.SetActive(g > .01f);
            }
            doorOpen = Mathf.MoveTowards(doorOpen, Time.time < openUntil ? 1 : 0, Time.deltaTime / .9f);
            door.localPosition = DoorAt + new Vector3(-doorOpen * .06f, 0, -doorOpen * 1.1f);
            door.gameObject.SetActive(doorOpen > 0); hole.gameObject.SetActive(doorOpen > 0);   // Round 8: shut, the hull's own painted door shows
            float glow = look == Look.Wreck || !ship.EnginesOn ? .04f : 1.2f + (ship.Mode == FlightMode.Hover ? .6f : ship.Throttle);
            var hot = Color.Lerp(new Color(1f, .5f, .18f), new Color(.75f, .85f, 1f), EngineLevel / 3f);
            foreach (var m in nozzles) m.SetColor("_Color", hot * glow);
        }
    }
}
