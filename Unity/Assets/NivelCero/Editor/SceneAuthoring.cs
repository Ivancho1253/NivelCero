using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace NivelCero.Editor
{
    public static class SceneAuthoring
    {
        [MenuItem("Nivel Cero/Construir escena 3D editable")]
        public static void BuildEditableScene()
        {
            ProjectSetup.Configure();
            const string folder = "Assets/NivelCero/World";
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var session = new GameObject("Nivel Cero · sesión").AddComponent<Session>();
            new SectorBuilder().Build(session);
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var materials = renderers.SelectMany(r => r.sharedMaterials)
                .Concat(new[] { session.CyanMaterial, session.RedMaterial, session.ExposedMaterial })
                .Where(m => m != null && !AssetDatabase.Contains(m)).Distinct().ToArray();
            int index = 0;
            foreach (var material in materials)
            {
                AssetDatabase.CreateAsset(material, folder + "/WorldMaterial" + index++ + ".mat");
            }
            foreach (var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
            {
                var profile = volume.sharedProfile;
                if (profile == null) profile = volume.profile;
                if (!AssetDatabase.Contains(profile))
                {
                    AssetDatabase.CreateAsset(profile, folder + "/Atmosphere.asset");
                    foreach (var component in profile.components) if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, profile);
                }
            }
            foreach (var surface in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None))
                if (surface.navMeshData != null && !AssetDatabase.Contains(surface.navMeshData)) AssetDatabase.CreateAsset(surface.navMeshData, folder + "/SecurityNavigation.asset");
            session.WorldIsBaked = true;
            EditorUtility.SetDirty(session); AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, "Assets/NivelCero/Scenes/SectorDeArranque3D.unity")) throw new IOException("No se pudo guardar la escena editable.");
            Debug.Log("Escena 3D editable guardada con geometría, modelos, luces, navegación y referencias serializadas.");
        }
    }
}
