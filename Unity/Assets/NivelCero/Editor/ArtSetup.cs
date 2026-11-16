using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NivelCero.Editor
{
    public static class ArtSetup
    {
        [MenuItem("Nivel Cero/Importar arte original")]
        public static void Import()
        {
            const string modelRoot = "Assets/NivelCero/Art/Models/";
            if (!File.Exists(modelRoot + "Zero.fbx")) return;
            Directory.CreateDirectory("Assets/NivelCero/Art/Materials");
            Directory.CreateDirectory("Assets/NivelCero/Resources");
            AssetDatabase.Refresh();
            foreach (var path in Directory.GetFiles(modelRoot, "*.fbx"))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.importAnimation = path.EndsWith("Zero.fbx");
                importer.animationType = importer.importAnimation ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
                if (importer.importAnimation) importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                if (importer.importAnimation)
                {
                    var clips = importer.defaultClipAnimations;
                    foreach (var clip in clips)
                    {
                        var name = clip.name.Split('|').Last();
                        clip.loopTime = name == "Idle" || name == "Walk" || name == "Run" || name == "Fall";
                    }
                    if (clips.Length > 0) importer.clipAnimations = clips;
                }
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.SaveAndReimport();
                foreach (var source in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                {
                    var safeName = string.Concat(source.name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_'));
                    var output = "Assets/NivelCero/Art/Materials/" + safeName + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(output);
                    if (material == null)
                    {
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        material.color = source.HasProperty("_Color") ? source.color : Color.gray;
                        material.SetFloat("_Metallic", .65f); material.SetFloat("_Smoothness", .48f);
                        var lower = source.name.ToLowerInvariant();
                        if (lower.Contains("cyan") || lower.Contains("amber") || lower.Contains("red") || lower.Contains("emission"))
                        { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", material.color * 2); }
                        AssetDatabase.CreateAsset(material, output);
                    }
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), material);
                    material.enableInstancing = true; EditorUtility.SetDirty(material);
                }
                importer.SaveAndReimport();
            }
            var catalog = AssetDatabase.LoadAssetAtPath<GameAssets>("Assets/NivelCero/Resources/NivelCeroAssets.asset");
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<GameAssets>(); AssetDatabase.CreateAsset(catalog, "Assets/NivelCero/Resources/NivelCeroAssets.asset"); }
            foreach (var field in typeof(GameAssets).GetFields().Where(f => f.FieldType == typeof(GameObject)))
                field.SetValue(catalog, AssetDatabase.LoadAssetAtPath<GameObject>(modelRoot + field.Name + ".fbx"));
            const string controllerPath = "Assets/NivelCero/Art/Zero.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var machine = controller.layers[0].stateMachine;
                string[] names = {"Idle", "Walk", "Run", "Jump", "Fall", "Dash", "Shoot", "Reload", "Melee1", "Melee2", "Melee3", "Hurt", "Death", "Interact"};
                var clips = AssetDatabase.LoadAllAssetsAtPath(modelRoot + "Zero.fbx").OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
                foreach (string name in names)
                {
                    var clip = clips.FirstOrDefault(c => c.name == name || c.name.EndsWith("|" + name));
                    if (clip == null) { Debug.LogWarning("Falta clip original " + name); continue; }
                    var state = machine.AddState(name); state.motion = clip;
                    if (name == "Idle") machine.defaultState = state;
                }
            }
            catalog.ZeroAnimator = controller; EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            catalog.ZeroAvatar = AssetDatabase.LoadAllAssetsAtPath(modelRoot + "Zero.fbx").OfType<Avatar>().FirstOrDefault();
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        }
    }
}
