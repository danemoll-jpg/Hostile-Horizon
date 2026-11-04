using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HostileHorizon
{
    // Round 1, item 1: a one-step Windows player build, ported from Chastain Rush's PlayerBuild (Racer's SoloReleaseBuild): no map
    // build here; it makes the game's one scene (Assets/Scenes/Main.unity: a GameShell, which builds the rest at start) and writes
    // Builds/HostileHorizon/HostileHorizon.exe (git-ignored) with BUILD_INFO.txt beside it.
    //   Menu:  Hostile Horizon > Build Executable
    //   Batch: Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod HostileHorizon.PlayerBuild.BuildFromCommandLine -logFile <file>
    public static class PlayerBuild
    {
        public const string OutputDir = "Builds/HostileHorizon";
        public const string ExeName = "HostileHorizon.exe";
        public const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Hostile Horizon/Build Executable")]
        public static void BuildMenu() => Build();

        public static void BuildFromCommandLine()
        {
            try { var r = Build(); EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void MakeScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("GameShell").AddComponent<GameShell>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        public static BuildReport Build()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Leave Play mode first.");
            MakeScene();
            CharacterImport.MakeMaterials();   // Fix 3.1: the characters' materials as assets (keeps their shader variants)
            PlayerSettings.companyName = "Hostile Horizon";
            PlayerSettings.productName = "Hostile Horizon";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;   // borderless, at the desktop resolution
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.show = false;
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HandMade/Title/GameIcon.png");
            if (icon) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;

            string root = Path.GetDirectoryName(Application.dataPath);
            string dir = Path.Combine(root, OutputDir);
            Directory.CreateDirectory(dir);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(dir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            var s = report.summary;
            string persistent = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", PlayerSettings.companyName, PlayerSettings.productName);
            var lines = new[]
            {
                $"Hostile Horizon player build, {DateTime.Now:yyyy-MM-dd HH:mm}",
                $"Result {s.result}; errors {s.totalErrors}; warnings {s.totalWarnings}; output {s.totalSize / 1048576.0:0.0} MB; took {s.totalTime.TotalSeconds:0} s",
                $"Exe: {Path.Combine(dir, ExeName)}",
                $"Unity {Application.unityVersion}; Windows x64; development off",
                $"Settings and Player.log: {persistent}",
                "Self-check: HostileHorizon.exe -exeCheck <dir> [-exeCheckOnly perf] [-steamDeck 1]",
            };
            File.WriteAllLines(Path.Combine(dir, "BUILD_INFO.txt"), lines.Concat(report.steps.Select(st => $"  step {st.name}: {st.duration.TotalSeconds:0.0} s")));
            Debug.Log(string.Join("\n", lines));
            if (s.result != BuildResult.Succeeded) throw new Exception("Build failed: " + s.result);
            return report;
        }
    }
}
