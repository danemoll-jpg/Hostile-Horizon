using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HostileHorizon
{
    // Fix 3.1: import settings for the characters (Assets/Resources/Characters, copied there by Tools/import_characters.py from
    // Dan's Mixamo downloads and Meshy's textures). Every FBX is Humanoid (Mixamo's skeleton; Unity retargets any clip onto any of
    // the six bodies), no imported materials (CharacterRig makes them), Unity's Mesh LOD on the meshes (for the Deck and VR).
    // Clips: root motion baked into the pose (the game moves the figures itself); loops for the ones that repeat.
    public sealed class CharacterImport : AssetPostprocessor
    {
        const string Root = "Assets/Resources/Characters/";
        static readonly string[] Loops = { "idle", "look_around", "wave", "wave2", "run", "walk", "crawl", "circle", "crouch_walk", "stagger", "sit_idle", "attack", "roar" };

        static string ClipName(string path)
        {
            string n = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!path.Contains("/Clips/")) return "idle";
            int k = n.IndexOf('_'); return k > 0 ? n.Substring(k + 1) : n;
        }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root)) return;
            var mi = (ModelImporter)assetImporter;
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importAnimation = true;
            mi.importBlendShapes = false; mi.importCameras = false; mi.importLights = false;
            mi.optimizeGameObjects = false;   // the game reaches the head bone (the Sleeper's look, the face placement)
            bool clip = assetPath.Contains("/Clips/");
            mi.generateMeshLods = !clip; mi.maximumMeshLod = 3;
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Root)) return;
            var mi = (ModelImporter)assetImporter;
            string name = ClipName(assetPath); bool loop = Loops.Contains(name);
            var clips = mi.defaultClipAnimations;
            foreach (var c in clips)
            {
                c.name = name; c.loopTime = loop; c.loopPose = loop && name != "attack" && name != "roar";
                c.lockRootRotation = true; c.keepOriginalOrientation = true;
                c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
                c.lockRootHeightY = true; c.keepOriginalPositionY = true;
            }
            mi.clipAnimations = clips;
        }

        // The characters' materials as assets (so the build keeps URP Lit's normal-map / metallic-map variants): <name>.mat, and
        // pilot_hollow.mat (the Hollow's disguise: the dead visor). Called by PlayerBuild before every build.
        public static void MakeMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var n in new[] { "pilot", "pilot_hollow", "mimic", "hollow", "knocker", "stalker", "sleeper" })
            {
                bool hollowPilot = n == "pilot_hollow"; string b = hollowPilot ? "pilot" : n;
                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + (hollowPilot ? "pilot_albedo_hollow.png" : b + "_albedo.png"));
                if (!albedo) continue;
                string path = Root + n + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!m) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
                m.shader = lit;
                m.SetTexture("_BaseMap", albedo); m.SetColor("_BaseColor", Color.white);
                m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + b + "_normal.png")); m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + (hollowPilot ? "pilot_mask_hollow.png" : b + "_mask.png")));
                m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_Smoothness", 1f); m.SetFloat("_Metallic", 1f);
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var ti = (TextureImporter)assetImporter;
            bool journal = assetPath.Contains("/Journal/");
            ti.textureType = assetPath.EndsWith("_normal.png") ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !assetPath.Contains("_mask") && !assetPath.EndsWith("_normal.png");
            ti.maxTextureSize = journal ? 512 : assetPath.Contains("_mask") ? 1024 : 2048;
            ti.mipmapEnabled = !journal;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.alphaSource = assetPath.Contains("_mask") ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        }
    }
}
