using System.Collections.Generic;
using UnityEngine;

namespace NivelCero
{
    /// <summary>
    /// Slices the original 2D sheets produced by Tools/Art/generate_2d_art.py
    /// (Resources/Academic2D). Cell sizes, row order and frame counts mirror the
    /// generator; change both together. 32 pixels = 1 world unit.
    /// </summary>
    public sealed class Academic2DArt
    {
        public const float PixelsPerUnit = 32;
        public static readonly string[] TileNames = {
            "groundL", "groundM", "groundM2", "groundR", "fill", "fill2", "fillL", "fillR",
            "platL", "platM", "platR", "wall", "wallCap", "hazard0", "hazard1", "back0",
            "back1", "back2", "back3", "girder", "pillar", "pipeH", "pipeV", "corruptA",
            "corruptB", "cable", "console", "rack", "signArrow", "signHazard", "arenaTop", "arenaFill" };
        static readonly string[] zeroRows = { "idle", "run", "jump", "fall", "land", "dash", "hurt", "death" };
        static readonly int[] zeroCounts = { 4, 8, 2, 2, 1, 2, 2, 6 };
        static readonly string[] bitRows = { "idle", "blink", "talk", "alert", "worried", "look" };
        static readonly int[] bitCounts = { 4, 2, 4, 2, 2, 4 };
        static readonly string[] fxRows = { "orb", "bolt", "hitCyan", "hitWhite", "hitRed", "wave", "block", "reticle", "column", "slash", "dust", "shard", "beacon", "terminal", "keyE", "warn" };
        static readonly int[] fxCounts = { 4, 2, 4, 4, 4, 4, 2, 2, 2, 4, 4, 8, 4, 4, 1, 1 };

        public readonly Dictionary<string, Sprite> Tiles = new Dictionary<string, Sprite>();
        public readonly Dictionary<string, Sprite[]> Zero = new Dictionary<string, Sprite[]>();
        public readonly Dictionary<string, Vector2[]> ZeroShoulders = new Dictionary<string, Vector2[]>();
        public readonly Dictionary<string, Sprite[]> Bit = new Dictionary<string, Sprite[]>();
        public readonly Dictionary<string, Sprite[]> Guardian = new Dictionary<string, Sprite[]>();
        public readonly Dictionary<string, Sprite[]> Fx = new Dictionary<string, Sprite[]>();
        public Sprite[] ArmBlaster, ArmBlade, Portal, Barrier, Portraits;
        public Sprite Far, Mid, Near;
        readonly List<Object> owned = new List<Object>();

        public static Academic2DArt Load()
        {
            var art = new Academic2DArt();
            Texture2D tiles = Tex("tileset"), zero = Tex("zero"), arm = Tex("zero_arm"), bit = Tex("bit"), guardian = Tex("guardian"),
                fx = Tex("fx"), portal = Tex("portal"), barrier = Tex("barrier"), portraits = Tex("portraits");
            if (tiles == null || zero == null || arm == null || bit == null || guardian == null || fx == null || portal == null || barrier == null)
            {
                Debug.LogError("Academic2DArt: faltan hojas en Resources/Academic2D. Ejecutar Tools/Art/generate_2d_art.py.");
                return null;
            }
            for (int i = 0; i < TileNames.Length; i++)
                art.Tiles[TileNames[i]] = art.Cut(tiles, i % 8, i / 8, 32, 32, new Vector2(.5f, .5f));
            for (int r = 0; r < zeroRows.Length; r++)
                art.Zero[zeroRows[r]] = art.Row(zero, r, zeroCounts[r], 48, 56, new Vector2(.5f, .5f));
            art.ArmBlaster = art.Row(arm, 0, 9, 48, 48, new Vector2(.5f, .5f));
            art.ArmBlade = art.Row(arm, 1, 9, 48, 48, new Vector2(.5f, .5f));
            for (int r = 0; r < bitRows.Length; r++)
                art.Bit[bitRows[r]] = art.Row(bit, r, bitCounts[r], 24, 24, new Vector2(.5f, .5f));
            for (int v = 0; v < 3; v++)
            {
                art.Guardian["idle" + v] = art.Row(guardian, v * 2, 4, 128, 128, new Vector2(.5f, .5f), 0);
                art.Guardian["charge" + v] = art.Row(guardian, v * 2, 3, 128, 128, new Vector2(.5f, .5f), 4);
                art.Guardian["fire" + v] = art.Row(guardian, v * 2, 2, 128, 128, new Vector2(.5f, .5f), 7);
                art.Guardian["exposed" + v] = art.Row(guardian, v * 2, 2, 128, 128, new Vector2(.5f, .5f), 9);
                art.Guardian["raise" + v] = art.Row(guardian, v * 2 + 1, 2, 128, 128, new Vector2(.5f, .5f), 0);
                art.Guardian["slam" + v] = art.Row(guardian, v * 2 + 1, 2, 128, 128, new Vector2(.5f, .5f), 2);
                art.Guardian["flash" + v] = art.Row(guardian, v * 2 + 1, 1, 128, 128, new Vector2(.5f, .5f), 4);
                art.Guardian["glitch" + v] = art.Row(guardian, v * 2 + 1, 2, 128, 128, new Vector2(.5f, .5f), 5);
            }
            art.Guardian["death"] = art.Row(guardian, 6, 8, 128, 128, new Vector2(.5f, .5f), 0);
            art.Guardian["dormant"] = art.Row(guardian, 6, 1, 128, 128, new Vector2(.5f, .5f), 8);
            for (int r = 0; r < fxRows.Length; r++)
            {
                string name = fxRows[r];
                // Ground-anchored effects pivot on their drawn base so they sit on the floor.
                Vector2 pivot = name == "beacon" || name == "terminal" ? new Vector2(.5f, 0) : name == "wave" ? new Vector2(.5f, .5f / 48) : name == "dust" ? new Vector2(.5f, 4f / 48) : new Vector2(.5f, .5f);
                art.Fx[name] = art.Row(fx, r, fxCounts[r], 48, 48, pivot);
            }
            art.Portal = art.Row(portal, 0, 5, 64, 128, new Vector2(.5f, 0));
            art.Barrier = art.Row(barrier, 0, 4, 32, 32, new Vector2(.5f, .5f));
            if (portraits != null) art.Portraits = art.Row(portraits, 0, 6, 48, 48, new Vector2(.5f, .5f));
            art.Far = art.Whole(Tex("parallax_far")); art.Mid = art.Whole(Tex("parallax_mid")); art.Near = art.Whole(Tex("parallax_near"));
            var anchors = Resources.Load<TextAsset>("Academic2D/zero_shoulders");
            art.ParseShoulders(anchors != null ? anchors.text : "");
            return art;
        }

        static Texture2D Tex(string name)
        {
            var texture = Resources.Load<Texture2D>("Academic2D/" + name);
            if (texture != null) { texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp; }
            return texture;
        }

        Sprite Cut(Texture2D texture, int column, int row, int width, int height, Vector2 pivot)
        {
            var rect = new Rect(column * width, texture.height - (row + 1) * height, width, height);
            var sprite = Sprite.Create(texture, rect, pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name + "_" + row + "_" + column;
            owned.Add(sprite);
            return sprite;
        }

        Sprite[] Row(Texture2D texture, int row, int count, int width, int height, Vector2 pivot, int start = 0)
        {
            var result = new Sprite[count];
            for (int i = 0; i < count; i++) result[i] = Cut(texture, start + i, row, width, height, pivot);
            return result;
        }

        Sprite Whole(Texture2D texture)
        {
            if (texture == null) return null;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, PixelsPerUnit, 0, SpriteMeshType.FullRect);
            owned.Add(sprite);
            return sprite;
        }

        void ParseShoulders(string text)
        {
            foreach (string line in text.Split('\n'))
            {
                int colon = line.IndexOf(':');
                if (colon < 0) continue;
                string name = line.Substring(0, colon).Trim();
                string[] items = line.Substring(colon + 1).Trim().Split(';');
                var points = new Vector2[items.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    string[] xy = items[i].Split(',');
                    float x, y;
                    float.TryParse(xy[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x);
                    float.TryParse(xy.Length > 1 ? xy[1] : "24", System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y);
                    // Pixel from top-left of a 48×56 frame -> offset from the sprite pivot (centre).
                    points[i] = new Vector2((x - 24) / PixelsPerUnit, (28 - y) / PixelsPerUnit);
                }
                ZeroShoulders[name] = points;
            }
        }

        public Vector2 Shoulder(string animation, int frame)
        {
            Vector2[] list;
            if (ZeroShoulders.TryGetValue(animation, out list) && list.Length > 0) return list[Mathf.Clamp(frame, 0, list.Length - 1)];
            return new Vector2(1f / 32, 3f / 32);
        }

        public void Dispose()
        {
            foreach (var item in owned) if (item != null) Object.Destroy(item);
            owned.Clear();
        }
    }
}
