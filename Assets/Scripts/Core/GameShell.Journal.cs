using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HostileHorizon
{
    // Round 3 (B10): the Field Journal. Round 6 (E): every alien type has its own page with a large 3D view of its creature (the
    // ViewStage: lit by three lights of its own, shown as the page's picture; a world-space menu shows it the same): turn it (right
    // stick / drag with the mouse / HOTAS hat), zoom (triggers / Shift-Ctrl / the mouse wheel), and play its animations (the
    // Animation row). Unmet types: a dark silhouette and "Unknown". What the corps knows unlocks with encounters: name, lore,
    // behaviour, the tell hint (vague, then clearer, never the answer), your record with it. From the hangar and the pause menu.
    public sealed partial class GameShell
    {
        MenuPanel.Page journalPage, journalView, journalReturn;
        string journalNote = "";
        int journalType = 1, journalAnim;
        float viewYaw = 200, viewPitch = 8, viewDist = 3.4f, animClock, viewHeight = 2f;
        string viewKey = "";
        CharacterRig viewRig; GameObject viewFallback;
        public CharacterRig ViewRig => viewRig;   // checks
        public float ViewYaw => viewYaw; public float ViewDist => viewDist;

        // E2: original lore, by encounters (1: what the corps logged first; 2+: more)
        static readonly string[][] Lore =
        {
            new string[0],
            new[] { "Corps files call it the Mimic. It wears a pilot's shape well enough to fool a tired eye at a hundred metres, and it has learned to talk on the radio.",
                    "Whatever it copies, it copies from the outside in. Crews who got close say the light never seemed to sit on it right." },
            new[] { "An empty suit that walks. First logged as a pilot standing by a wreck, visor dark, who never answered a call.",
                    "Suits recovered afterwards were empty inside, lined with something grey that was not skin. Nothing looks back out of the visor." },
            new[] { "Named by the crews for what it does best. It knows the corps knocks at the airlock, and it knocks back.",
                    "It has the rhythm almost right. Almost. The knocks don't always come from where a pilot would stand." },
            new[] { "A hunter that takes its time. It circles a landed ship before it comes in, low and careful, and the radar loses it while it does.",
                    "Pilots run straight to a ship. This one never has. Watch how it comes, not what it looks like." },
            new[] { "The worst of them, because it does nothing wrong. It boards as a pilot, sits with the others, thanks you, and waits.",
                    "Security has pulled several from mothership bays, each sitting perfectly still. The pilots who flew with them heard it first." },
        };
        static readonly string[] Behaviour =
        {
            "",
            "Waits by a wreck, waves the ship down, runs to the airlock like any pilot. Burns like anything else in your exhaust.",
            "Slow and patient. Stands very still at the door and looks up into the airlock camera.",
            "Comes to the hull quickly and knocks. Left at the door, it does not stay on the ground.",
            "Circles the ship before it comes to the door; drops off the radar while it circles.",
            "Passes every check at the door. Turns in the cabin after some minutes of flight; enough pilots with it can fight it off.",
        };
        // E1: the animations each type's page plays (clip names; CharacterRig finds the type's own, then the creature set)
        static readonly string[][] Anims =
        {
            new string[0],
            new[] { "idle", "walk", "run", "attack", "roar", "leap" },
            new[] { "idle", "walk", "stagger", "attack", "roar" },
            new[] { "idle", "walk", "run", "attack", "roar", "climb" },
            new[] { "idle", "crouch_walk", "circle", "attack", "roar" },
            new[] { "idle", "sit_idle", "walk", "attack", "roar" },
        };
        static string AnimName(string clip) => clip == "roar" ? "shriek" : clip.Replace('_', ' ');

        JournalEntry Entry(int type) => Career.C?.journal.Find(j => j.type == type);

        void BuildJournalPage()
        {
            // the index: one row per type (met or not)
            journalPage = new MenuPanel.Page { Title = "FIELD JOURNAL", Subtitle = "What the corps knows about the things down there" };
            for (int t = 1; t <= 5; t++)
            {
                int type = t;
                journalPage.Add(new MenuPanel.Row
                {
                    Label = () => { var e = Entry(type); return e == null ? "Unknown  <size=22>(not met yet)</size>" : $"{Aliens.Names[type]}  <size=22>(met {e.encounters} time{(e.encounters == 1 ? "" : "s")})</size>"; },
                    Submit = () => OpenJournalType(type),
                });
            }
            journalPage.Add(Action("Back", () => Menu.Show(journalReturn ?? hangar)));
            journalPage.Back = () => Menu.Show(journalReturn ?? hangar);
            journalPage.Body = () =>
            {
                var c = Career.C; int met = c == null ? 0 : c.journal.Count;
                return $"{met} of 5 kinds met.\n\nEach page shows the creature in 3D: turn it, zoom in, play how it moves.\nWhat the corps knows grows with every encounter that shows what it was\n(killed by your exhaust or shield, thrown off the canopy, or it killed you).\n\nA tell is never certain: read the hints, then watch and listen at the door.";
            };

            // a type's page
            journalView = new MenuPanel.Page { Back = () => Menu.Show(journalPage, true) };
            journalView.Add(new MenuPanel.Row
            {
                Label = () => "Animation",
                Value = () => Entry(journalType) == null ? "—" : AnimName(Anims[journalType][journalAnim]),
                Left = () => StepAnim(-1), Right = () => StepAnim(1), Submit = () => StepAnim(1),
                Disabled = () => Entry(journalType) == null,
            })
            .Add(new MenuPanel.Row { Label = () => "Next kind", Value = () => Entry(NextType(1)) == null ? "Unknown" : Aliens.Names[NextType(1)], Left = () => OpenJournalType(NextType(-1)), Right = () => OpenJournalType(NextType(1)), Submit = () => OpenJournalType(NextType(1)) })
            .Add(Action("Reset view", () => { viewYaw = 200; viewPitch = 8; viewDist = viewHeight * 2.3f; }))
            .Add(Action("Back", () => Menu.Show(journalPage, true)));
            journalView.View = () => Stage.Tex;
            journalView.Body = JournalText;
            journalView.Hint = () => Controls.LastDevice == Controls.Device.Keyboard ? "Drag with the right mouse button: turn     Wheel / Shift-Ctrl: zoom     ← →: change     Esc: back"
                : Controls.LastDevice == Controls.Device.Hotas ? $"Hat: turn     Stick left / right: change     {Controls.Btn(Controls.Act.Back)}: back"
                : $"Right stick: turn     RT / LT: zoom     D-pad ← →: change     {Controls.Btn(Controls.Act.Back)}: back";
        }

        int NextType(int d) => (journalType - 1 + d + 5) % 5 + 1;
        void StepAnim(int d) { if (Entry(journalType) == null) return; var a = Anims[journalType]; journalAnim = (journalAnim + d + a.Length) % a.Length; animClock = 0; if (viewRig) viewRig.Play(a[journalAnim], .25f, 1f, true); }

        void OpenJournalType(int type)
        {
            journalType = type; journalAnim = 0; viewKey = "";
            journalView.Title = Entry(type) == null ? "UNKNOWN" : Aliens.Names[type].ToUpperInvariant();
            journalView.Subtitle = Entry(type) == null ? "Not met yet" : "Field Journal";
            Menu.Show(journalView);
        }

        public void ShowJournal(MenuPanel.Page from) { journalReturn = from; Menu.Show(journalPage); }
        public void CheckOpenJournalOf(AlienType type) => OpenJournalType((int)type);   // checks
        public void CheckViewInput(float yaw, float pitch, float dist) { viewYaw = yaw; viewPitch = pitch; viewDist = dist; }   // checks: a set view
        public void CheckAnim(int i) { journalAnim = i % Anims[journalType].Length; animClock = 0; if (viewRig) viewRig.Play(Anims[journalType][journalAnim], .1f, 1f, true); }

        string JournalText()
        {
            var e = Entry(journalType); int t = journalType;
            if (e == null) return "<b>Unknown</b>\nThe corps has reports of something else down there. Meet it, and live (or don't), and this page fills in.";
            var sb = new StringBuilder();
            sb.AppendLine($"<b>{Aliens.Names[t]}</b>   <size=20>met {e.encounters} · killed {e.killed} · killed you {e.killedYou}{(e.firstPlanet != null ? $" · first seen on {e.firstPlanet}{(e.firstSeen != null ? $" ({e.firstSeen})" : "")}" : "")}</size>");
            sb.AppendLine(Lore[t][0] + (e.encounters >= 2 ? " " + Lore[t][1] : ""));
            sb.AppendLine($"<color=#FFC873>Behaviour:</color> {Behaviour[t]}");
            sb.Append($"<color=#FFC873>Tell:</color> <i>{Aliens.Hint((AlienType)t, e.encounters)}</i>" + (e.encounters < 2 ? "  <size=20>(a clearer note after another encounter)</size>" : ""));
            return sb.ToString();
        }

        // every frame while a type's page is shown (StageTick): the creature on the stage, turned and zoomed by the player
        void JournalTick()
        {
            var st = Stage; bool met = Entry(journalType) != null;
            string key = journalType + (met ? "m" : "u");
            if (key != viewKey)
            {
                viewKey = key; st.Clear(); viewRig = null; viewFallback = null; st.Lights(true);
                st.Cam.fieldOfView = 32f; st.Cam.backgroundColor = met ? new Color(.015f, .02f, .03f) : new Color(.2f, .23f, .27f);   // a silhouette needs a lighter ground
                var holder = new GameObject("Creature").transform; holder.SetParent(st.Root, false);
                viewRig = CharacterRig.Create(CharacterRig.ModelFor((AlienType)journalType), holder);
                if (viewRig) { viewRig.Unscaled(); viewRig.Play(Anims[journalType][0], 0, 1f, true); }
                else { var c = Creature.Create(litTemplate, journalType * 31, (AlienType)journalType); c.transform.SetParent(holder, false); c.Show(true); viewFallback = c.gameObject; }   // the code-made figure if the model is missing
                // the floor: a dark disc under it
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(floor.GetComponent<Collider>()); floor.transform.SetParent(st.Root, false); floor.transform.localScale = new Vector3(2.4f, .01f, 2.4f);
                var fm = new Material(litTemplate); fm.SetColor("_BaseColor", met ? new Color(.08f, .09f, .1f) : new Color(.12f, .13f, .15f)); floor.GetComponent<Renderer>().sharedMaterial = fm;
                if (!met)
                {
                    var black = new Material(Shader.Find("HostileHorizon/Glow")); black.SetColor("_Color", Color.black);
                    foreach (var r in holder.GetComponentsInChildren<Renderer>(true)) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = black; r.sharedMaterials = ms; }
                }
                ViewStage.SetLayer(st.Root.gameObject); st.Root.gameObject.layer = 0;
                // frame the whole body: its height from what is drawn
                var rs = holder.GetComponentsInChildren<Renderer>(true); if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); viewHeight = Mathf.Clamp(b.max.y - st.transform.position.y, 1f, 4f); }
                viewDist = viewHeight * 2.3f;
            }
            // E1: turn, tilt, zoom (unscaled time: works in the pause menu)
            float dt = Time.unscaledDeltaTime;
            var look = Controls.Look; var mouse = Controls.MouseLook;
            viewYaw += (look.x * 110f) * dt + mouse.x * .25f;
            viewPitch = Mathf.Clamp(viewPitch - look.y * 70f * dt - mouse.y * .2f, -20f, 60f);
            float zoom = Controls.ThrottleChange + (UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y * .02f : 0);
            viewDist = Mathf.Clamp(viewDist - zoom * viewHeight * dt * (Mathf.Abs(zoom) > .5f ? 1 : 3), viewHeight * .45f, viewHeight * 3f);
            // the camera orbits the body's middle; closer in, it aims higher (the head)
            float h = viewHeight * Mathf.Lerp(.75f, .5f, Mathf.InverseLerp(viewHeight * .6f, viewHeight * 2.3f, viewDist));
            var at = st.transform.position + Vector3.up * h;
            var rot = Quaternion.Euler(viewPitch, viewYaw, 0);
            st.Cam.transform.SetPositionAndRotation(at - rot * Vector3.forward * viewDist, rot);
            // the lights follow the view a little (the key from the camera's upper right)
            st.Key.transform.position = at + rot * new Vector3(2.2f, 2.4f, -2.6f); st.Key.transform.LookAt(at);
            st.Fill.transform.position = at + rot * new Vector3(-2.8f, .6f, -1.8f); st.Fill.transform.LookAt(at);
            st.Back.transform.position = at + rot * new Vector3(0, 2.2f, 3.0f); st.Back.transform.LookAt(at);
            // a clip that doesn't loop plays again after a pause
            if (viewRig && met) { animClock += dt; float len = viewRig.CurrentLength; if (len > 0 && animClock > len + 1.2f) { animClock = 0; viewRig.Play(Anims[journalType][journalAnim], .2f, 1f, true); } }
        }
    }
}
