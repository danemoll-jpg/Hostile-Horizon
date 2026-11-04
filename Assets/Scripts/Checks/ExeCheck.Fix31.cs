using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HostileHorizon
{
    // Fix 3.1 check, in the built exe:  HostileHorizon.exe -exeCheck <dir> -exeCheckOnly fix31
    // The Round 3 aliens check (AlienCheck) with the real character models, without the "tell hidden" visits, plus: a report of every
    // model (loaded, triangles, Mesh LOD levels, height, the clips each one finds), close-up pictures of each creature (idle and roar)
    // and of the real and the Hollow's visor side by side, and frame rates landed with a pilot coming and the cabin full, with the
    // models and with the code-made figures.
    public sealed partial class ExeCheck
    {
        static readonly Dictionary<string, string[]> Fix31Clips = new Dictionary<string, string[]>
        {
            ["pilot"] = new[] { "idle", "look_around", "wave", "wave2", "run", "run_stop", "knock", "climb", "hit", "death", "circle", "sit_idle" },
            ["mimic"] = new[] { "idle", "crawl", "attack", "roar", "death", "climb", "hit", "leap" },
            ["hollow"] = new[] { "idle", "crawl", "attack", "roar", "death", "climb", "hit", "stagger" },
            ["knocker"] = new[] { "idle", "crawl", "attack", "roar", "death", "climb", "hit" },
            ["stalker"] = new[] { "idle", "crouch_walk", "attack", "roar", "death", "climb", "hit", "circle" },
            ["sleeper"] = new[] { "idle", "crawl", "attack", "roar", "death", "climb", "hit", "sit_idle" },
        };

        void ModelReport()
        {
            foreach (var m in CharacterRig.Models)
            {
                var r = CharacterRig.Create(m, null);
                if (!r) { Log($"model {m}: MISSING (the code-made figure is used)"); continue; }
                var sb = new StringBuilder();
                int tris = 0, lods = 0; Bounds b = default; bool first = true;
                foreach (var x in r.Renderers)
                    if (x is SkinnedMeshRenderer s && s.sharedMesh)
                    {
                        tris += s.sharedMesh.GetIndexCount(0) > 0 ? (int)(s.sharedMesh.GetIndexCount(0) / 3) : 0; lods = Mathf.Max(lods, s.sharedMesh.lodCount);
                        if (first) { b = s.bounds; first = false; } else b.Encapsulate(s.bounds);
                    }
                var avatar = r.Anim.avatar;
                sb.Append($"model {m}: {tris} triangles, Mesh LOD levels {lods}, height {b.size.y:0.00} m, avatar {(avatar && avatar.isValid && avatar.isHuman ? "Humanoid OK" : "NOT HUMANOID")}, material {r.Renderers[0].sharedMaterial?.name}; clips:");
                foreach (var c in Fix31Clips[m]) { var clip = CharacterRig.Clip(m, c); sb.Append($" {c}{(clip ? $" {clip.length:0.0}s{(clip.isLooping ? " loop" : "")}" : " MISSING")},"); }
                Log(sb.ToString().TrimEnd(','));
                Destroy(r.gameObject);
            }
        }

        // A check-only camera (not the player's view) for close-ups of the models, by the landed ship.
        IEnumerator CloseUps()
        {
            var st = G.Ship.transform; var f = G.Ship.Field;
            var rt = new RenderTexture(768, 1024, 24); var cam = new GameObject("Check close-up camera").AddComponent<Camera>();
            cam.targetTexture = rt; cam.fieldOfView = 38; cam.nearClipPlane = .05f; cam.enabled = false;
            var lit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Vector3 spot = st.TransformPoint(new Vector3(-9f, 0, 4f)); spot.y = f.SurfaceHeight(spot.x, spot.z);
            Quaternion face = Quaternion.LookRotation(Flat(st.TransformPoint(new Vector3(-9f, 0, 14f)) - spot));
            void Render(string name) { cam.Render(); SaveRT(rt, name); }
            for (int t = 1; t <= 5; t++)
            {
                var type = (AlienType)t; var c = Creature.Create(lit, 1, type);
                c.transform.SetPositionAndRotation(spot, face); c.Show(true); c.SetPose(Creature.Pose.Stand, true);
                yield return new WaitForSecondsRealtime(1.2f);
                float h = c.Rig ? c.Rig.Head.position.y - spot.y + .25f : 2.2f;
                cam.transform.position = spot + face * new Vector3(.8f, h * .55f, h * 1.55f + 1f); cam.transform.LookAt(spot + Vector3.up * h * .5f);
                Render($"m_{t}_{type.ToString().ToLower()}_idle");
                c.SetPose(Creature.Pose.Lunge); yield return new WaitForSecondsRealtime(1.0f);
                cam.transform.position = c.HeadWorld + face * new Vector3(.25f, .1f, 1.6f); cam.transform.LookAt(c.HeadWorld);
                Render($"m_{t}_{type.ToString().ToLower()}_roar_face");
                Log($"  close-up {type}: {(c.Rig ? $"model {c.Rig.Model}, clip {c.Rig.Current}" : "CODE-MADE creature")}");
                Destroy(c.gameObject);
            }
            // the visor: a real pilot and the Hollow's disguise side by side, the ship's lamps and the sky in front of them
            var real = CharacterRig.Create("pilot", null); var dead = CharacterRig.Create("pilot", null, "pilot_hollow");
            if (real && dead)
            {
                real.transform.SetPositionAndRotation(spot + face * new Vector3(-.45f, 0, 0), face); dead.transform.SetPositionAndRotation(spot + face * new Vector3(.45f, 0, 0), face);
                yield return new WaitForSecondsRealtime(.8f);
                var mid = (real.Head.position + dead.Head.position) / 2;
                cam.transform.position = mid + face * new Vector3(0, .15f, 1.9f); cam.transform.LookAt(mid);
                Render("m_0_visors_hollow_left_real_right");
                cam.transform.position = spot + face * new Vector3(0, 1.1f, 4.2f); cam.transform.LookAt(spot + Vector3.up * .95f);
                Render("m_0_pilots_hollow_left_real_right");
                Log($"  visors: real pilot material {real.Renderers[0].sharedMaterial.name}, Hollow's disguise {dead.Renderers[0].sharedMaterial.name}");
            }
            if (real) Destroy(real.gameObject); if (dead) Destroy(dead.gameObject);
            Destroy(cam.gameObject); rt.Release();
        }

        // Frame times landed by a pilot's pad: a pilot running in and knocking, 4 pilots in the cabin, the cabin camera on the HUD.
        IEnumerator LandedFps(string what)
        {
            G.CheckSetup(5, new[] { 0, 0, 0, 0, 0 }, new[] { 0, 0, 0, 0, 0 });
            foreach (var q in G.Pilots.FindAll(x => x.Index >= 1)) G.CheckBoard(q);
            var p = G.Pilots.Find(x => x.Index == 0);
            yield return LandFor(p);
            yield return Press(UnityEngine.InputSystem.LowLevel.GamepadButton.DpadUp);   // cabin camera on the HUD
            var times = new List<float>(); float end = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < end) { times.Add(Time.unscaledDeltaTime); yield return null; }
            yield return Shot($"f_{what.Split(' ')[0]}_landed_cockpit");
            yield return Press(UnityEngine.InputSystem.LowLevel.GamepadButton.DpadUp);
            times.Sort(); float avg = times.Count / times.Sum(), low = 1f / times[(int)(times.Count * .99f)];
            Log($"fps landed, a pilot coming + 4 in the cabin + cabin camera, {what}: average {avg:0.0}, 1% low {low:0.0} over {times.Count} frames; pilot {p.Now}");
            yield return Press(UnityEngine.InputSystem.LowLevel.GamepadButton.South);   // engines on (the pilot dies if still out: fine, it's the check)
            yield return BackToFlight();
        }
    }
}
