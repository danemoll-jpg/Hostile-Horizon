using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace HostileHorizon
{
    // Round 1, item 9: Chastain Rush's LauncherBridge (from Woodstock Rush), for Hostile Horizon's launcher (Launcher/). The launcher
    // starts the game with -racerManagedMusic <its soundtrack> -racerPersonalMusic <songs kept from earlier soundtracks>
    // -racerLauncherReady <file> -racerLauncherNonce <text> (the same argument names as the other games' launcher); once the game is up
    // (GameShell running a few frames) this writes the nonce to the ready file, so the launcher knows the start worked. Without those
    // arguments (the game started directly) nothing happens.
    // Hostile Horizon addition: the launcher's self-check starts the game hidden with -batchmode -racerTestSave <dir>; there the game
    // closes itself (exit code 0) a few seconds after the ready signal, which the self-check counts as a clean run.
    public sealed class LauncherBridge : MonoBehaviour
    {
        public static string Argument(string key) { var args = System.Environment.GetCommandLineArgs(); for (int i = 1; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1]; return null; }
        public static string ManagedMusic => Argument("-racerManagedMusic");
        public static string PersonalMusic => Argument("-racerPersonalMusic");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartBridge()
        {
            if (Argument("-racerLauncherReady") == null) return;
            var go = new GameObject("Launcher startup handshake"); DontDestroyOnLoad(go); go.AddComponent<LauncherBridge>();
        }

        IEnumerator Start()
        {
            var path = Argument("-racerLauncherReady"); var nonce = Argument("-racerLauncherNonce");
            if (string.IsNullOrEmpty(nonce) || nonce.Length > 128 || !Path.IsPathRooted(path)) { Destroy(gameObject); yield break; }
            float end = Time.realtimeSinceStartup + 90f; GameShell shell = null;
            while (Time.realtimeSinceStartup < end && !(shell = FindAnyObjectByType<GameShell>())) yield return null;
            for (int i = 0; i < 3; i++) yield return null;
            if (shell)
            {
                if (!Application.isBatchMode) yield return new WaitForEndOfFrame();
                try { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path + ".tmp", nonce); File.Move(path + ".tmp", path); }
                catch (Exception e) { Debug.LogWarning("Launcher ready signal unavailable: " + e.Message); }
                Debug.Log("[Launcher] ready signal written" + (ManagedMusic != null ? $"; soundtrack {ManagedMusic}, personal music {PersonalMusic}" : ""));
                if (Argument("-racerTestSave") != null && Application.isBatchMode)
                {
                    yield return new WaitForSecondsRealtime(3f);
                    Debug.Log("[Launcher] self-check run: closing");
                    Application.Quit(0);
                }
            }
            Destroy(gameObject);
        }
    }
}
