using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace NivelCero.Editor
{
    /// <summary>
    /// Bakes the 2D level into the scene as editable Tilemaps backed by Tile assets
    /// (Assets/NivelCero/Art/Academic/Tiles). After baking, the level can be
    /// inspected and repainted in the Scene view; Academic2D uses the baked maps
    /// instead of generating them at runtime.
    /// </summary>
    public static class SceneAuthoring2D
    {
        const string ScenePath = "Assets/NivelCero/Scenes/AE4_2D_SectorDeArranque.unity";
        const string TileFolder = "Assets/NivelCero/Art/Academic/Tiles";
        const string Tileset = "Assets/NivelCero/Resources/Academic2D/tileset.png";

        [MenuItem("Nivel Cero/Construir escena 2D editable")]
        public static void BuildEditable2DScene()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Tileset);
            if (texture == null) throw new IOException("Falta " + Tileset + ". Ejecutar Tools/Art/generate_2d_art.py.");
            Directory.CreateDirectory(TileFolder);
            AssetDatabase.Refresh();
            var tiles = new Dictionary<string, Tile>();
            for (int i = 0; i < Academic2DArt.TileNames.Length; i++)
            {
                string name = Academic2DArt.TileNames[i];
                string path = TileFolder + "/" + name + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (existing != null) AssetDatabase.DeleteAsset(path);
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = name;
                var rect = new Rect((i % 8) * 32, texture.height - (i / 8 + 1) * 32, 32, 32);
                var sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), Academic2DArt.PixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = name;
                tile.sprite = sprite;
                tile.colliderType = Academic2DLevel.IsSolid(name) ? Tile.ColliderType.Grid : Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, path);
                AssetDatabase.AddObjectToAsset(sprite, tile);
                tiles[name] = tile;
            }
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            // Opening a scene unloads unreferenced objects: reload the saved Tile assets.
            foreach (string name in Academic2DArt.TileNames)
            {
                tiles[name] = AssetDatabase.LoadAssetAtPath<Tile>(TileFolder + "/" + name + ".asset");
                if (tiles[name] == null) throw new IOException("Tile no encontrado tras guardarlo: " + name);
            }
            var game = Object.FindFirstObjectByType<Academic2D>();
            if (game == null) throw new IOException("La escena 2D no contiene Academic2D.");
            if (game.LevelGrid != null) Object.DestroyImmediate(game.LevelGrid.gameObject);
            var grid = new GameObject("AE4 Grid · Sector de Arranque (editable)").AddComponent<Grid>();
            grid.transform.SetParent(game.transform, false);
            game.LevelGrid = grid;
            game.GroundMap = Map(grid, "Suelo · colisión (Tilemap)", 0);
            game.PlatformMap = Map(grid, "Pasarela opcional · un sentido", 0);
            game.HazardMap = Map(grid, "Peligro · purga de fosos", -2);
            game.BackMap = Map(grid, "Pared de fondo", -8);
            game.DecorMap = Map(grid, "Decoración", -7);
            game.CorruptionMap = Map(grid, "Corrupción de GL1TCH", -6);
            Academic2DLevel.Paint(game.GroundMap, game.PlatformMap, game.HazardMap, game.BackMap, game.DecorMap, game.CorruptionMap, n => tiles[n]);
            game.GroundMap.gameObject.AddComponent<TilemapCollider2D>();
            var platformCollider = game.PlatformMap.gameObject.AddComponent<TilemapCollider2D>();
            platformCollider.usedByEffector = true;
            var effector = game.PlatformMap.gameObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true; effector.surfaceArc = 160;
            int painted = game.GroundMap.GetUsedTilesCount();
            if (painted == 0 || game.GroundMap.GetTile(new Vector3Int(4, 0, 0)) == null) throw new IOException("El Tilemap de suelo quedó vacío.");
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("No se pudo guardar la escena 2D.");
            Debug.Log("Escena 2D editable: Tilemaps horneados con " + tiles.Count + " tiles en " + TileFolder);
        }

        static Tilemap Map(Grid grid, string label, int order)
        {
            var go = new GameObject(label);
            go.transform.SetParent(grid.transform, false);
            var map = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order; renderer.mode = TilemapRenderer.Mode.Chunk;
            return map;
        }
    }
}
