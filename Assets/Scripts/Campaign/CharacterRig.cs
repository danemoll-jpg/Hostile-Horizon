using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace HostileHorizon
{
    // Fix 3.1: a character from Dan's art pipeline (concept -> Meshy -> Mixamo; Docs/MODELS-NEEDED.md): the rigged model
    // (Resources/Characters/<model>.fbx, Humanoid), its material (<model>.mat, made at build time) and its animations, played by name
    // with a short crossfade (a Playables mixer: no Animator controller asset). Clips are Humanoid, so any clip plays on any body:
    // "run" for the pilot is looked up as pilot_run, then creature_run (shared by the five creatures), then any other set.
    // Create returns null when the model file is missing: the callers keep their code-made figures as the fallback.
    public sealed class CharacterRig : MonoBehaviour
    {
        public static readonly string[] Models = { "pilot", "mimic", "hollow", "knocker", "stalker", "sleeper" };
        static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        static string[] clipSets;

        public string Model { get; private set; }
        public Animator Anim { get; private set; }
        public Renderer[] Renderers { get; private set; }
        public string Current { get; private set; } = "";
        public float CurrentTime => cur.IsValid() ? (float)cur.GetTime() : 0;
        public Transform Bone(HumanBodyBones b) => Anim.GetBoneTransform(b);
        public Transform Head => Anim.GetBoneTransform(HumanBodyBones.Head);

        PlayableGraph graph; AnimationMixerPlayable mixer; AnimationClipPlayable cur, prev;
        float fade, fadeT; string queued; float queuedAt, queuedSpeed = 1;

        public static string ModelFor(AlienType t) => t == AlienType.None ? "pilot" : Models[(int)t];

        public static bool Has(string model) => Prefab(model) != null;
        public static bool Disabled;   // checks: the code-made figures, for a frame-rate comparison
        static GameObject Prefab(string model)
        {
            if (Disabled) return null;
            if (!prefabs.TryGetValue(model, out var p)) prefabs[model] = p = Resources.Load<GameObject>("Characters/" + model);
            return p;
        }

        // A clip by name: <model>'s own first (its idle is in its model file), then the shared creature set, then any set.
        public static AnimationClip Clip(string model, string name)
        {
            string key = model + "/" + name;
            if (clips.TryGetValue(key, out var c)) return c;
            if (clipSets == null) clipSets = new[] { "pilot", "creature", "mimic", "hollow", "knocker", "stalker", "sleeper" };
            if (name == "idle") c = First(Resources.LoadAll<AnimationClip>("Characters/" + model));
            if (!c) c = First(Resources.LoadAll<AnimationClip>($"Characters/Clips/{model}_{name}"));
            if (!c && model != "pilot") c = First(Resources.LoadAll<AnimationClip>($"Characters/Clips/creature_{name}"));
            foreach (var s in clipSets) { if (c) break; c = First(Resources.LoadAll<AnimationClip>($"Characters/Clips/{s}_{name}")); }
            return clips[key] = c;
        }
        static AnimationClip First(AnimationClip[] a) { foreach (var c in a) if (!c.name.StartsWith("__preview__")) return c; return null; }

        // material: the model's own, or another (e.g. "pilot_hollow": the Hollow's disguise with its dead visor).
        public static CharacterRig Create(string model, Transform parent, string material = null)
        {
            var p = Prefab(model); if (!p) return null;
            var go = Instantiate(p, parent, false); go.name = model;
            var r = go.AddComponent<CharacterRig>(); r.Model = model;
            r.Anim = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
            r.Anim.applyRootMotion = false;
            r.Anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // culled animators left the creature undrawn right in front of the eye (the horror lunge); few figures live at once
            var mat = Resources.Load<Material>("Characters/" + (material ?? model));
            r.Renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var x in r.Renderers)
            {
                if (mat) x.sharedMaterial = mat;
                if (x is SkinnedMeshRenderer s) s.updateWhenOffscreen = false;
            }
            r.graph = PlayableGraph.Create("Character " + model);
            r.graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            r.mixer = AnimationMixerPlayable.Create(r.graph, 2);
            AnimationPlayableOutput.Create(r.graph, "Animation", r.Anim).SetSourcePlayable(r.mixer);
            r.graph.Play();
            r.Play("idle", 0);
            return r;
        }

        // Plays a clip (crossfade `fade` s). Asking for the clip already playing only changes its speed, unless restart.
        public bool Play(string name, float fadeTime = .2f, float speed = 1f, bool restart = false)
        {
            queued = null;
            if (!graph.IsValid()) return false;
            if (name == Current && cur.IsValid() && !restart) { cur.SetSpeed(speed); return true; }
            var c = Clip(Model, name); if (!c) return false;
            if (prev.IsValid()) { graph.Disconnect(mixer, 0); prev.Destroy(); }
            if (cur.IsValid()) { graph.Disconnect(mixer, 1); graph.Connect(cur, 0, mixer, 0); prev = cur; }
            cur = AnimationClipPlayable.Create(graph, c); cur.SetSpeed(speed); cur.SetApplyFootIK(true);
            graph.Connect(cur, 0, mixer, 1);
            Current = name; fade = fadeTime; fadeT = 0;
            Weights();
            return true;
        }

        // Plays `first`, then `then` once `first` has run its length (or `firstFor` s, to use only its start).
        public void PlayThen(string first, string then, float speed = 1f, float firstFor = -1)
        {
            if (!Play(first, .15f, speed, true)) { Play(then); return; }
            var c = Clip(Model, first); queued = then; queuedSpeed = 1; queuedAt = firstFor > 0 ? firstFor : c.length / Mathf.Max(.01f, speed) - .2f;
        }

        public float Length(string name) { var c = Clip(Model, name); return c ? c.length : 0; }
        public void Speed(float s) { if (cur.IsValid()) cur.SetSpeed(s); }

        public void ShadowMode(UnityEngine.Rendering.ShadowCastingMode m) { foreach (var x in Renderers) x.shadowCastingMode = m; }
        public void Tint(Color c) { foreach (var x in Renderers) { var m = new Material(x.sharedMaterial); m.SetColor("_BaseColor", c); x.sharedMaterial = m; } }

        void Weights()
        {
            float w = fade <= 0 ? 1 : Mathf.Clamp01(fadeT / fade);
            mixer.SetInputWeight(1, w); mixer.SetInputWeight(0, prev.IsValid() ? 1 - w : 0);
        }

        void Update()
        {
            if (!graph.IsValid()) return;
            float dt = Time.deltaTime;
            fadeT += dt; Weights();
            if (queued != null) { queuedAt -= dt; if (queuedAt <= 0) { var q = queued; queued = null; Play(q, .25f, queuedSpeed); } }
        }

        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
    }
}
