using System;
using System.IO;
using UnityEngine;

namespace HostileHorizon
{
    // Round 11 (C1, C2): training's lessons and the progress kept between sessions (training.json beside settings.json; a check: in its
    // folder). Nothing here touches career.json: a lesson flies a throwaway in-memory career (Career.BeginTraining, never saved).
    [Serializable] public sealed class TrainingProgress { public bool[] done = new bool[Training.Count]; public bool offered; }

    public static class Training
    {
        public const int Count = 8;
        public const int PracticeSeed = 1984;   // the practice planet: Canyons, always the same
        public static readonly string[] Names = { "Flying", "Navigation", "Landing", "The pickup", "Aliens", "Combat", "Getting home", "VR: recentre" };
        public static readonly string[] About =
        {
            "Pitch, roll, yaw and throttle; let go and the ship rights itself; the level-out button.",
            "The radar and its distances, the route trail and its target, the way home.",
            "Air brakes into landing mode, the stop ring, the belly camera and floor window, touchdown.",
            "Engines off, the pilot runs over and knocks, the airlock, the cabin camera.",
            "A practice alien with its tell plain to see: keep the airlock shut, engines on. Then one on the canopy to burn off.",
            "The gun and the shield against a practice turret and a saucer.",
            "Low energy: RETURN NOW, the way home, the recall and the mothership's beam.",
            "In the headset: recentre your view (only in VR).",
        };
        public static readonly string[] VoiceIds = { "ctl_tr_fly", "ctl_tr_nav", "ctl_tr_land", "ctl_tr_pickup", "ctl_tr_alien", "ctl_tr_combat", "ctl_tr_home", "ctl_tr_vr" };

        public static bool Active;   // a lesson is running (GameShell.Training)
        public static int Lesson;

        static TrainingProgress p;
        public static TrainingProgress P { get { if (p == null) Load(); return p; } }
        static string FilePath => Path.Combine(CheckRun.Active ? CheckRun.Dir : GameSettings.Folder, "training.json");
        static void Load()
        {
            try { p = File.Exists(FilePath) ? JsonUtility.FromJson<TrainingProgress>(File.ReadAllText(FilePath)) : null; } catch { p = null; }
            if (p == null) p = new TrainingProgress();
            if (p.done == null || p.done.Length != Count) { var d = new bool[Count]; if (p.done != null) Array.Copy(p.done, d, Mathf.Min(Count, p.done.Length)); p.done = d; }
        }
        public static void Save()
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); File.WriteAllText(FilePath, JsonUtility.ToJson(P, true)); }
            catch (Exception e) { Debug.LogWarning("Training progress not saved: " + e.Message); }
        }
        public static int DoneCount { get { int n = 0; for (int i = 0; i < Count; i++) if (P.done[i]) n++; return n; } }
        public static bool Available(int lesson) => lesson != 7 || VrMode.Active;   // the VR lesson only in the headset
        public static int NextLesson(int after) { for (int i = after + 1; i < Count; i++) if (Available(i)) return i; return -1; }
    }
}
