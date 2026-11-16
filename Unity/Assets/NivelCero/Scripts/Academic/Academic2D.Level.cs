using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace NivelCero
{
    // Tilemaps, parallax and short-lived visual effects of the 2D scene.
    public sealed partial class Academic2D
    {
        sealed class Layer { public Transform[] Copies; public float Factor, FactorY, BaseY, Width; public SpriteRenderer[] Renderers; public Color Tint; }
        sealed class Fx { public SpriteRenderer Renderer; public SpriteAnimator2D Anim; public float Life; public Vector2 Velocity; public float Fade; }
        readonly List<Layer> layers = new List<Layer>();
        readonly List<Fx> effects = new List<Fx>();

        // Kept for compatibility with the 0.6 tests that inspect the builder.
        Tile floorTile;

        void BuildLevel()
        {
            bool baked = GroundMap != null && LevelGrid != null && GroundMap.GetUsedTilesCount() > 0;
            if (!baked && LevelGrid != null) { Destroy(LevelGrid.gameObject); LevelGrid = null; }
            if (!baked)
            {
                LevelGrid = new GameObject("AE4 Grid · Sector de Arranque").AddComponent<Grid>();
                LevelGrid.transform.SetParent(transform);
                GroundMap = NewMap("Suelo · colisión (Tilemap)", 0, true);
                PlatformMap = NewMap("Pasarela opcional · un sentido", 0, true);
                HazardMap = NewMap("Peligro · purga de fosos", -2, false);
                BackMap = NewMap("Pared de fondo", -8, false);
                DecorMap = NewMap("Decoración", -7, false);
                CorruptionMap = NewMap("Corrupción de GL1TCH", -6, false);
                Academic2DLevel.Paint(GroundMap, PlatformMap, HazardMap, BackMap, DecorMap, CorruptionMap, RuntimeTile);
                floorTile = runtimeTiles.ContainsKey("groundM") ? runtimeTiles["groundM"] : null;
            }
            // Physics is configured the same way for baked and runtime maps.
            if (GroundMap.GetComponent<TilemapCollider2D>() == null) GroundMap.gameObject.AddComponent<TilemapCollider2D>();
            if (PlatformMap != null && PlatformMap.GetComponent<TilemapCollider2D>() == null)
            {
                var platformCollider = PlatformMap.gameObject.AddComponent<TilemapCollider2D>();
                platformCollider.usedByEffector = true;
                var effector = PlatformMap.gameObject.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true; effector.surfaceArc = 160;
            }
            BuildParallax();
        }

        Tilemap NewMap(string label, int order, bool solid)
        {
            var go = new GameObject(label);
            go.transform.SetParent(LevelGrid.transform, false);
            var map = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            return map;
        }

        TileBase RuntimeTile(string name)
        {
            Tile tile;
            if (runtimeTiles.TryGetValue(name, out tile)) return tile;
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.sprite = art.Tiles[name];
            tile.colliderType = Academic2DLevel.IsSolid(name) ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            runtimeTiles[name] = tile;
            return tile;
        }

        void BuildParallax()
        {
            AddLayer("Parallax lejano · torres de servidores", art.Far, .9f, .92f, -3.5f, -30);
            AddLayer("Parallax medio · torres de refrigeración", art.Mid, .7f, .8f, -3.0f, -25);
            AddLayer("Parallax cercano · conductos", art.Near, .45f, .6f, -2.5f, -20);
        }

        void AddLayer(string label, Sprite sprite, float factor, float factorY, float baseY, int order)
        {
            if (sprite == null) return;
            var layer = new Layer { Factor = factor, FactorY = factorY, BaseY = baseY, Width = sprite.bounds.size.x, Copies = new Transform[3], Renderers = new SpriteRenderer[3], Tint = Color.white };
            for (int i = 0; i < 3; i++)
            {
                var r = Visual(label + " " + i, sprite, Vector3.zero, order);
                layer.Copies[i] = r.transform; layer.Renderers[i] = r;
            }
            layers.Add(layer);
        }

        void UpdateParallax()
        {
            if (view == null) return;
            Vector3 cam = view.transform.position;
            // Zone tint: the far layers cool down in the archive and redden in the arena.
            int zone = Academic2DLevel.Zone(cam.x);
            Color zoneTint = zone == 0 ? Color.white : zone == 1 ? new Color(.92f, .88f, 1f) : zone == 2 ? new Color(1f, .86f, .9f) : zone == 3 ? new Color(1f, .74f, .78f) : new Color(.85f, 1f, 1f);
            foreach (var layer in layers)
            {
                layer.Tint = Color.Lerp(layer.Tint, zoneTint, Time.unscaledDeltaTime * 1.5f);
                float originX = cam.x * layer.Factor;
                float start = originX + layer.Width * Mathf.Floor((cam.x - originX) / layer.Width) - layer.Width;
                float y = layer.BaseY + cam.y * layer.FactorY;
                for (int i = 0; i < 3; i++)
                {
                    layer.Copies[i].position = PixelSnap(new Vector3(start + i * layer.Width, y - 3.5f, 5 + layer.Factor));
                    layer.Renderers[i].color = layer.Tint;
                }
            }
        }

        // ------------------------------------------------------------ effects
        SpriteRenderer SpawnFx(string name, Vector2 position, float fps, bool centered, int order = 15, Color? tint = null)
        {
            Sprite[] frames;
            if (!art.Fx.TryGetValue(name, out frames)) return null;
            var r = Visual("FX " + name, frames[0], position, order);
            if (tint.HasValue) r.color = tint.Value;
            var anim = r.gameObject.AddComponent<SpriteAnimator2D>();
            anim.Play(name, frames, fps, false);
            effects.Add(new Fx { Renderer = r, Anim = anim, Life = frames.Length / fps + .02f });
            return r;
        }

        void SpawnBolt(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            var r = Visual("Pulso de Zero", art.Fx["bolt"][0], from, 14);
            r.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            float life = Mathf.Max(.03f, delta.magnitude / 70f);
            effects.Add(new Fx { Renderer = r, Life = life, Velocity = delta / life });
            SpawnFx("hitCyan", to, 20, true, 16);
        }

        void SpawnGhost()
        {
            var r = Visual("Estela de dash", playerSprite.sprite, playerSprite.transform.position, 9);
            r.flipX = playerSprite.flipX; r.color = new Color(.31f, .85f, .88f, .5f);
            effects.Add(new Fx { Renderer = r, Life = .22f, Fade = .5f / .22f });
        }

        float ghostClock;
        void UpdateEffects(float dt)
        {
            if (dashTime > 0) { ghostClock -= dt; if (ghostClock <= 0) { SpawnGhost(); ghostClock = .045f; } }
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var fx = effects[i];
                fx.Life -= dt;
                if (fx.Renderer != null)
                {
                    fx.Renderer.transform.position += (Vector3)(fx.Velocity * dt);
                    if (fx.Fade > 0) { var c = fx.Renderer.color; c.a = Mathf.Max(0, c.a - fx.Fade * dt); fx.Renderer.color = c; }
                }
                if (fx.Life <= 0)
                {
                    if (fx.Renderer != null) Destroy(fx.Renderer.gameObject);
                    effects.RemoveAt(i);
                }
            }
        }
    }
}
