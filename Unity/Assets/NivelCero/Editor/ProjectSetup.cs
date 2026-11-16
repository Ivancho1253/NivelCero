using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NivelCero.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string Settings = "Assets/NivelCero/Settings";
        static ProjectSetup() { EditorApplication.delayCall += FirstImport; }
        static void FirstImport()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!File.Exists(Settings + "/NivelCeroURP.asset")) Configure();
        }
        [MenuItem("Nivel Cero/Preparar proyecto")]
        public static void Configure()
        {
            Directory.CreateDirectory(Settings); AssetDatabase.Refresh();
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Settings + "/NivelCeroURP.asset");
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create();
                AssetDatabase.CreateAsset(pipeline, Settings + "/NivelCeroURP.asset");
                pipeline.LoadBuiltinRendererData(RendererType.UniversalRenderer);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input != null && input.intValue != 1) { input.intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo(); }
            PlayerSettings.companyName = "Nivel Cero"; PlayerSettings.productName = "Nivel Cero - Escape del Glitch";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.bundleVersion = "0.7.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene("Assets/NivelCero/Scenes/SectorDeArranque3D.unity", true),
                new EditorBuildSettingsScene("Assets/NivelCero/Scenes/AE4_2D_SectorDeArranque.unity", true)
            };
            var importer = AssetImporter.GetAtPath("Assets/NivelCero/Art/Academic/tiles.png") as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.isReadable = true;
                importer.SaveAndReimport();
            }
            // Runtime-generated materials must survive shader stripping in the standalone player.
            var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
            foreach (var name in new[] {"Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Sprites/Default"})
            {
                var shader = Shader.Find(name); if (shader == null) throw new InvalidOperationException("Missing shader: " + name);
                bool exists = false;
                for (int i = 0; i < shaders.arraySize; i++) if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) exists = true;
                if (!exists) { int index = shaders.arraySize; shaders.InsertArrayElementAtIndex(index); shaders.GetArrayElementAtIndex(index).objectReferenceValue = shader; }
            }
            graphics.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
            ArtSetup.Import();
            Debug.Log("Nivel Cero: configuración preparada. Esto no constituye una prueba de gameplay.");
        }
        [MenuItem("Nivel Cero/Compilar Windows x64")]
        public static void BuildWindows()
        {
            Configure(); Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path),
                locationPathName = "Builds/Windows/NivelCero.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
        }
    }
}
