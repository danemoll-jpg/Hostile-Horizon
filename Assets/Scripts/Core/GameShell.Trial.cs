using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HostileHorizon
{
    // Round 9 (F1-F3): the Free Flight time trial. A course of glowing gates through the planet, made from its seed and biome (the same
    // seed and planet type always make the same course): legs between landing pads found ahead of the start, each leg the route
    // trail's own path finder (RouteTrail.Plan: down in the canyons, at a flyable height), a gate every trialGateSpacing m, about
    // trialLength m in all (2-3 minutes). The clock starts at the first gate; a split at every gate against your best; a gate missed
    // (flown past outside its ring) costs trialMissPenalty s; the best time per seed and planet type is kept in trials.json (beside
    // settings.json) with its ghost: a translucent ship flying your best run (on by default: Free Flight > Ghost). The crash level
    // applies (a crash ends the run: Restart races again).
    [Serializable] public sealed class TrialRecord { public string key; public float best; public int missed; public string date; public List<float> splits = new List<float>(); public List<float> ghost = new List<float>(); }
    [Serializable] public sealed class TrialBook { public List<TrialRecord> records = new List<TrialRecord>(); }

    public sealed partial class GameShell
    {
        public bool TrialMode { get; private set; }
        public readonly List<Vector3> TrialPath = new List<Vector3>();                  // the course's line (checks follow it)
        public readonly List<(Vector3 at, Vector3 dir)> Gates = new List<(Vector3, Vector3)>();
        public int NextGate { get; private set; }
        public float TrialTime { get; private set; }
        public int TrialMissed { get; private set; }
        public bool TrialRunning { get; private set; }
        public bool TrialFinished { get; private set; }
        public float TrialResult { get; private set; }
        public TrialRecord TrialBest { get; private set; }
        public Transform Ghost { get; private set; }
        public readonly List<float> MissBy = new List<float>();   // checks: m from the gate's centre for each gate flown past outside it
        readonly List<LineRenderer> gateRings = new List<LineRenderer>(); readonly List<float> splits = new List<float>(), ghostRec = new List<float>();
        float lastSide = -1, ghostSampleAt; MenuPanel.Page trialDone; Material gateNext, gateLater, gateFinish;
        const float GhostStep = .1f;

        static string TrialFile => Path.Combine(CheckRun.Active ? CheckRun.Dir : GameSettings.Folder, "trials.json");
        static TrialBook LoadBook() { try { if (File.Exists(TrialFile)) return JsonUtility.FromJson<TrialBook>(File.ReadAllText(TrialFile)) ?? new TrialBook(); } catch (Exception e) { Debug.LogWarning("trials.json unreadable: " + e.Message); } return new TrialBook(); }
        static string TrialKey(int seed, Biome b) => $"{seed}/{Biomes.Keys[(int)b]}";
        public TrialRecord BestFor(int seed, Biome b) => LoadBook().records.Find(r => r.key == TrialKey(seed, b));

        public void StartTrial() { TrialMode = true; StartFlight(ChosenSeed, (CrashLevel)GameSettings.S.crashLevel); }
        void EndTrialMode() { TrialMode = false; ClearTrial(); }

        // the course (called while loading, after the world is set)
        void BuildCourse(Vector3 start, float heading)
        {
            ClearTrial();
            var t = Tuning.T; var f = Terrain.Field; var rng = new SeedRng(f.SeedNumber ^ ((int)WorldBiome * 7919), 311);
            TrialPath.Clear();
            Vector3 at = start; float dir = heading, length = 0; var usedPads = new List<Vector2>();
            TrialPath.Add(at);
            for (int leg = 0; leg < 30 && length < t.trialLength; leg++)
            {
                // the next landing pad ahead: 1.1-1.9 km away, as straight on as the planet allows (wider each try)
                bool found = false; Vector3 to = Vector3.zero;
                for (int widen = 0; widen < 4 && !found; widen++)
                {
                    float cone = 40f + widen * 35f, best = float.MaxValue;
                    for (int a = 0; a < 60; a++)
                    {
                        float ang = dir + (rng.Value * 2 - 1) * cone, d = Mathf.Lerp(t.trialLegMin, t.trialLegMax, rng.Value);
                        var q = at + Quaternion.Euler(0, ang, 0) * Vector3.forward * d;
                        var pad = f.PadAt(Mathf.FloorToInt(q.x / CanyonField.PadCell), Mathf.FloorToInt(q.z / CanyonField.PadCell));
                        if (!pad.ok) continue;
                        var p2 = new Vector2(pad.x, pad.z); float dist = (p2 - new Vector2(at.x, at.z)).magnitude;
                        if (dist < t.trialLegMin * .7f || usedPads.Exists(u => (u - p2).magnitude < 300f)) continue;
                        float off = Mathf.Abs(Mathf.DeltaAngle(dir, Mathf.Atan2(pad.x - at.x, pad.z - at.z) * Mathf.Rad2Deg));
                        if (off < best) { best = off; to = new Vector3(pad.x, f.SurfaceHeight(pad.x, pad.z) + RouteTrail.Clearance, pad.z); found = true; }
                    }
                }
                if (!found) break;
                var legPath = RouteTrail.Plan(f, at, to);
                if (legPath == null || legPath.Count < 2) { usedPads.Add(new Vector2(to.x, to.z)); continue; }
                for (int i = 1; i < legPath.Count && length < t.trialLength; i++) { length += Vector3.Distance(TrialPath[TrialPath.Count - 1], legPath[i]); TrialPath.Add(legPath[i]); }   // cut at the course length
                usedPads.Add(new Vector2(to.x, to.z));
                var last = legPath[legPath.Count - 1] - legPath[Mathf.Max(0, legPath.Count - 4)]; dir = Mathf.Atan2(last.x, last.z) * Mathf.Rad2Deg; at = to;
            }
            // gates along it: the first trialGateFirst m in (the flying start), then every trialGateSpacing m; the last one is the finish
            Gates.Clear(); float along = 0, next = t.trialGateFirst;
            for (int i = 1; i < TrialPath.Count; i++)
            {
                var a = TrialPath[i - 1]; var b = TrialPath[i]; float seg = Vector3.Distance(a, b);
                while (next <= along + seg) { var p = Vector3.Lerp(a, b, (next - along) / Mathf.Max(seg, .01f)); var dd = Flat(b - a).normalized; Gates.Add((p, dd)); next += t.trialGateSpacing; }
                along += seg;
            }
            var end = TrialPath[TrialPath.Count - 1]; var endDir = Flat(end - TrialPath[Mathf.Max(0, TrialPath.Count - 3)]).normalized;
            if (Gates.Count == 0 || Vector3.Distance(Gates[Gates.Count - 1].at, end) > t.trialGateSpacing * .4f) Gates.Add((end, endDir));
            MakeGateRings();
            NextGate = 0; TrialTime = 0; TrialMissed = 0; MissBy.Clear(); TrialRunning = TrialFinished = false; TrialResult = 0; splits.Clear(); ghostRec.Clear(); lastSide = -1; ghostSampleAt = 0;
            TrialBest = BestFor(f.SeedNumber, WorldBiome);
            MakeGhost();
            PlayLog.Add($"time trial: course {length / 1000f:0.0} km, {Gates.Count} gates ({TrialKey(f.SeedNumber, WorldBiome)}); best {(TrialBest != null ? Missions.Clock(TrialBest.best) : "none")}");
        }

        void MakeGateRings()
        {
            if (gateNext == null)
            {
                gateNext = new Material(Shader.Find("HostileHorizon/Glow")); gateNext.SetColor("_Color", new Color(.45f, 1f, 1f) * 2.4f);
                gateLater = new Material(Shader.Find("HostileHorizon/Glow")); gateLater.SetColor("_Color", new Color(.3f, .7f, 1f) * 1.1f);
                gateFinish = new Material(Shader.Find("HostileHorizon/Glow")); gateFinish.SetColor("_Color", new Color(.5f, 1f, .45f) * 2.4f);
            }
            float r = Tuning.T.trialGateRadius;
            for (int g = 0; g < Gates.Count; g++)
            {
                var l = new GameObject($"Gate {g + 1}").AddComponent<LineRenderer>(); l.positionCount = 48; l.loop = true; l.widthMultiplier = 1.6f; l.useWorldSpace = true;
                l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; l.receiveShadows = false;
                var (at, dir) = Gates[g]; var right = Vector3.Cross(Vector3.up, dir).normalized;
                for (int i = 0; i < 48; i++) { float a = i * Mathf.PI * 2 / 48f; l.SetPosition(i, at + (right * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * r); }
                l.gameObject.SetActive(false); gateRings.Add(l);
            }
            ShowGates();
        }

        void ShowGates()
        {
            for (int g = 0; g < gateRings.Count; g++)
            {
                bool on = g >= NextGate && g < NextGate + 3 && !TrialFinished;
                gateRings[g].gameObject.SetActive(on);
                if (on) gateRings[g].sharedMaterial = g == Gates.Count - 1 ? gateFinish : g == NextGate ? gateNext : gateLater;
            }
        }

        void ClearTrial()
        {
            foreach (var l in gateRings) if (l) Destroy(l.gameObject);
            gateRings.Clear(); Gates.Clear(); TrialPath.Clear();
            if (Ghost) Destroy(Ghost.gameObject); Ghost = null;
            TrialRunning = false;
            if (Displays) Displays.Objectives = "";
        }

        // F2: the ghost: your ship's model, translucent blue, flying your best run
        void MakeGhost()
        {
            if (Ghost) Destroy(Ghost.gameObject); Ghost = null;
            if (!GameSettings.S.trialGhost || TrialBest == null || TrialBest.ghost.Count < 16) return;
            var prefab = Resources.Load<GameObject>("Models/Ship"); if (!prefab) return;
            var root = new GameObject("Ghost").transform; var m = Instantiate(prefab, root); m.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var mat = new Material(Shader.Find("HostileHorizon/GlowAdd")); mat.SetColor("_Color", new Color(.25f, .55f, 1f) * .35f);
            foreach (var r in m.GetComponentsInChildren<Renderer>()) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = mat; r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
            Ghost = root; PlaceGhost(0);
        }

        void PlaceGhost(float time)
        {
            if (!Ghost || TrialBest == null) return;
            var g = TrialBest.ghost; int n = g.Count / 7; if (n < 2) return;
            float k = Mathf.Clamp(time / GhostStep, 0, n - 1); int i = Mathf.Min((int)k, n - 2); float f = k - i;
            Vector3 P(int j) => new Vector3(g[j * 7], g[j * 7 + 1], g[j * 7 + 2]);
            Quaternion Q(int j) => new Quaternion(g[j * 7 + 3], g[j * 7 + 4], g[j * 7 + 5], g[j * 7 + 6]);
            Ghost.SetPositionAndRotation(Vector3.Lerp(P(i), P(i + 1), f), Quaternion.Slerp(Q(i), Q(i + 1), f));
            Ghost.gameObject.SetActive(time <= (n - 1) * GhostStep + .5f);
        }

        // every frame in Free Flight with a course (GameShell.Update, Flying)
        void TrialTick(float dt)
        {
            if (!TrialMode || InCampaign || Gates.Count == 0) return;
            var t = Tuning.T; var s = Ship.transform.position;
            if (TrialRunning)
            {
                TrialTime += dt;
                while (TrialTime >= ghostSampleAt) { var r = Ship.transform.rotation; ghostRec.AddRange(new[] { s.x, s.y, s.z, r.x, r.y, r.z, r.w }); ghostSampleAt += GhostStep; }
            }
            PlaceGhost(TrialRunning || TrialFinished ? TrialTime : 0);
            if (!TrialFinished && NextGate < Gates.Count)
            {
                var (at, dir) = Gates[NextGate];
                float side = Vector3.Dot(s - at, dir);
                if (lastSide < 0 && side >= 0)
                {
                    var off = s - at - dir * side; bool through = off.magnitude <= t.trialGateRadius * 1.1f;
                    if (NextGate == 0) { TrialRunning = true; TrialTime = 0; ghostRec.Clear(); ghostSampleAt = 0; }
                    if (!through) { TrialMissed++; MissBy.Add(off.magnitude); Sfx.Play(Sfx.Warn, Ship.transform.position, .5f, 4f); }
                    else Sfx.Play(Sfx.Beep, Ship.transform.position, .45f, 4f);
                    float now = TrialTime + TrialMissed * t.trialMissPenalty; splits.Add(now);
                    string vs = TrialBest != null && NextGate < TrialBest.splits.Count ? $"   {(now - TrialBest.splits[NextGate] <= 0 ? "<color=#7DFF9A>" : "<color=#FF8A70>")}{now - TrialBest.splits[NextGate]:+0.0;-0.0} s</color>" : "";
                    if (NextGate > 0) Flash($"GATE {NextGate + 1} / {Gates.Count}   {Clock10(now)}{vs}{(through ? "" : $"   MISSED +{t.trialMissPenalty:0} s")}", 2.5f);
                    else Flash("GO! The clock is running", 2f);
                    NextGate++;
                    if (NextGate >= Gates.Count) FinishTrial();
                    ShowGates();
                    side = NextGate < Gates.Count ? Vector3.Dot(s - Gates[NextGate].at, Gates[NextGate].dir) : -1;
                }
                lastSide = side;
                // a gate passed by far to the side (never crossed: flown round it): counted missed once the next one is nearer
                if (!TrialFinished && NextGate > 0 && NextGate + 1 < Gates.Count && Vector3.Distance(s, Gates[NextGate + 1].at) + 50f < Vector3.Distance(s, Gates[NextGate].at) && Vector3.Dot(s - Gates[NextGate].at, Gates[NextGate].dir) > -10f)
                { TrialMissed++; splits.Add(TrialTime + TrialMissed * t.trialMissPenalty); NextGate++; lastSide = Vector3.Dot(s - Gates[NextGate].at, Gates[NextGate].dir); ShowGates(); Flash($"GATE MISSED +{t.trialMissPenalty:0} s", 2f); }
            }
            float total = TrialTime + TrialMissed * t.trialMissPenalty;
            Displays.Objectives = TrialFinished ? $"TIME TRIAL  <b>{Clock10(TrialResult)}</b>\n{(TrialBest != null ? $"BEST {Clock10(TrialBest.best)}" : "")}" :
                $"TIME TRIAL  <b>{Clock10(total)}</b>\nGATE {Mathf.Min(NextGate + 1, Gates.Count)} / {Gates.Count}{(TrialMissed > 0 ? $"   MISSED {TrialMissed} (+{TrialMissed * t.trialMissPenalty:0} s)" : "")}\n" +
                (TrialBest != null ? $"BEST {Clock10(TrialBest.best)}{(Ghost ? "  · ghost" : "")}" : "no best yet") + (TrialRunning ? "" : "\nfly through the first gate to start");
        }

        string TrialBestLine()
        {
            int seed = ChosenSeed; var b = FreeBiome(seed); var r = BestFor(seed, b);
            return $"\n\n<b>Time trial</b> ({Biomes.Name(b)}, seed {seed}): " + (r != null ? $"best {Clock10(r.best)} ({r.date})" : "no time yet") + $". Ghost {(GameSettings.S.trialGhost ? "on" : "off")}.";
        }

        static string Clock10(float s) { s = Mathf.Max(0, s); return $"{(int)(s / 60)}:{s % 60:00.0}"; }

        void FinishTrial()
        {
            var t = Tuning.T; TrialFinished = true; TrialRunning = false;
            TrialResult = TrialTime + TrialMissed * t.trialMissPenalty;
            var book = LoadBook(); string key = TrialKey(Terrain.Field.SeedNumber, WorldBiome);
            var rec = book.records.Find(r => r.key == key);
            bool isBest = rec == null || TrialResult < rec.best;
            float before = rec?.best ?? 0;
            if (isBest)
            {
                if (rec == null) { rec = new TrialRecord { key = key }; book.records.Add(rec); }
                rec.best = TrialResult; rec.missed = TrialMissed; rec.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"); rec.splits = new List<float>(splits); rec.ghost = new List<float>(ghostRec);
                try { Directory.CreateDirectory(Path.GetDirectoryName(TrialFile)); File.WriteAllText(TrialFile, JsonUtility.ToJson(book)); } catch (Exception e) { Debug.LogWarning("trials.json not saved: " + e.Message); }
            }
            Music.Sting("sting_planet_cleared");
            PlayLog.Add($"time trial finished: {Clock10(TrialResult)} ({TrialMissed} missed){(isBest ? (before > 0 ? $", new best (was {Clock10(before)})" : ", first best") : $", best {Clock10(before)}")}");
            trialDone.Body = () => $"<b>{Clock10(TrialResult)}</b>{(TrialMissed > 0 ? $"  ({TrialMissed} gate{(TrialMissed == 1 ? "" : "s")} missed: +{TrialMissed * t.trialMissPenalty:0} s)" : "")}\n" +
                (isBest ? (before > 0 ? $"<color=#7DFF9A>New best! (was {Clock10(before)})</color>" : "<color=#7DFF9A>Your first time on this course.</color>") : $"Best {Clock10(before)} ({TrialResult - before:+0.0} s)") +
                $"\nSeed {FlightSeed} · {Biomes.Name(WorldBiome)} · {Gates.Count} gates" + (isBest ? "\nYour ghost will fly this run next time." : "");
            StartCoroutine(ShowTrialDone());
        }

        System.Collections.IEnumerator ShowTrialDone()
        {
            float end = Time.realtimeSinceStartup + 1.6f; while (Time.realtimeSinceStartup < end) yield return null;
            if (Now != State.Flying) yield break;
            pausedFrom = Now; hudWasOn = Displays.Visible; Now = State.Paused; Time.timeScale = 0; Displays.SetVisible(false); Menu.SetBackdrop(.35f); Menu.Show(trialDone);
        }

        void BuildTrialPages()
        {
            trialDone = new MenuPanel.Page { Title = "TIME TRIAL", Back = Resume };
            trialDone.Add(Action("Race again", () => { Time.timeScale = 1; StartTrial(); }))
                     .Add(Action("Fly on", () => { ClearTrial(); TrialMode = false; Resume(); }))
                     .Add(Action("Return to title", () => { EndTrialMode(); ToTitle(); }));
        }
    }
}
