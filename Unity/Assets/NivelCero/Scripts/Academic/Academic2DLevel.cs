using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace NivelCero
{
    /// <summary>
    /// Layout of the 2D Sector de Arranque. The same painter fills the runtime
    /// tilemaps and the editable ones baked into the scene by
    /// "Nivel Cero/Construir escena 2D editable", so both stay identical.
    /// Collision lives only in Ground and Platforms; the rest is decoration.
    /// </summary>
    public static class Academic2DLevel
    {
        public const int MinX = -2, MaxX = 108, ArenaStart = 74, ArenaDoorX = 77;
        public static readonly int[] PillarColumns = { 4, 16, 28, 36, 47, 58, 70, 80, 90, 100 };

        public static bool IsPit(int x) { return (x >= 18 && x <= 21) || (x >= 48 && x <= 51); }

        /// <summary>0 Arranque · 1 Archivo · 2 Acceso restringido · 3 Núcleo de Seguridad · 4 Salida.</summary>
        public static int Zone(float x) { return x < 30 ? 0 : x < 62 ? 1 : x < ArenaStart ? 2 : x < 98 ? 3 : 4; }
        public static readonly string[] ZoneNames = { "ARRANQUE", "ARCHIVO", "ACCESO RESTRINGIDO", "NÚCLEO DE SEGURIDAD", "SALIDA" };

        static int Hash(int x, int y) { unchecked { int h = x * 73856093 ^ y * 19349663; h ^= h >> 13; return Mathf.Abs(h * 1274126177) % 1000; } }

        public static void Paint(Tilemap ground, Tilemap platforms, Tilemap hazards, Tilemap back, Tilemap decor, Tilemap corruption, Func<string, TileBase> tile)
        {
            for (int x = MinX; x <= MaxX; x++)
            {
                if (IsPit(x)) continue;
                bool arena = x >= ArenaStart, left = IsPit(x - 1) || x == MinX, right = IsPit(x + 1) || x == MaxX;
                string top = arena ? "arenaTop" : left ? "groundL" : right ? "groundR" : Hash(x, 0) % 5 == 0 ? "groundM2" : "groundM";
                ground.SetTile(new Vector3Int(x, 0, 0), tile(top));
                for (int y = -1; y >= -4; y--)
                {
                    string fill = left ? "fillL" : right ? "fillR" : arena ? "arenaFill" : Hash(x, y) % 4 == 0 ? "fill2" : "fill";
                    ground.SetTile(new Vector3Int(x, y, 0), tile(fill));
                }
            }
            for (int y = 1; y <= 10; y++)
            {
                ground.SetTile(new Vector3Int(MinX, y, 0), tile(y == 10 ? "wallCap" : "wall"));
                ground.SetTile(new Vector3Int(MaxX, y, 0), tile(y == 10 ? "wallCap" : "wall"));
            }
            // Optional upper route: a one-way catwalk reached by a deliberate side jump.
            for (int x = 38; x <= 45; x++) platforms.SetTile(new Vector3Int(x, 3, 0), tile(x == 38 ? "platL" : x == 45 ? "platR" : "platM"));
            // Pit floors: purge grid (hazard language: red + teeth + stripes).
            for (int x = MinX; x <= MaxX; x++)
                if (IsPit(x)) { hazards.SetTile(new Vector3Int(x, -1, 0), tile(x % 2 == 0 ? "hazard0" : "hazard1")); hazards.SetTile(new Vector3Int(x, -2, 0), tile("hazard1")); }

            // Back wall: machinery band, ceiling truss and pillars; open gaps show the parallax.
            int[][] bands = { new[] { -1, 14 }, new[] { 23, 35 }, new[] { 53, 66 }, new[] { 74, 107 } };
            foreach (int[] band in bands)
                for (int x = band[0]; x <= band[1]; x++)
                    for (int y = 1; y <= (x >= ArenaStart ? 3 : 2); y++)
                    {
                        int h = Hash(x, y + 40);
                        back.SetTile(new Vector3Int(x, y, 0), tile(h % 7 == 0 ? "back2" : h % 5 == 0 ? "back1" : h % 11 == 0 ? "back3" : "back0"));
                    }
            for (int x = MinX + 1; x < MaxX; x++) back.SetTile(new Vector3Int(x, 10, 0), tile("girder"));
            foreach (int x in PillarColumns)
                for (int y = 1; y <= 9; y++) back.SetTile(new Vector3Int(x, y, 0), tile("pillar"));

            for (int x = 0; x <= 30; x++) decor.SetTile(new Vector3Int(x, 7, 0), tile("pipeH"));
            for (int x = 52; x <= 66; x++) decor.SetTile(new Vector3Int(x, 7, 0), tile("pipeH"));
            foreach (int x in new[] { 12, 59 }) for (int y = 3; y <= 6; y++) decor.SetTile(new Vector3Int(x, y, 0), tile("pipeV"));
            for (int x = MinX + 1; x < MaxX; x++) if (Hash(x, 9) % 9 == 0) decor.SetTile(new Vector3Int(x, 9, 0), tile("cable"));
            foreach (int x in new[] { 9, 26, 61, 84 }) decor.SetTile(new Vector3Int(x, 1, 0), tile("console"));
            foreach (int x in new[] { 31, 32, 33, 55, 104 }) { decor.SetTile(new Vector3Int(x, 1, 0), tile("rack")); decor.SetTile(new Vector3Int(x, 2, 0), tile("rack")); }
            foreach (int x in new[] { 1, 23, 53 }) decor.SetTile(new Vector3Int(x, 2, 0), tile("signArrow"));
            foreach (int x in new[] { 16, 46, 75 }) decor.SetTile(new Vector3Int(x, 1, 0), tile("signHazard"));

            // GL1TCH's corruption spreads deeper into the sector.
            for (int x = 28; x < MaxX; x++)
            {
                int zone = Zone(x);
                int threshold = zone == 1 ? 140 : zone == 2 ? 230 : zone == 3 ? 300 : 120;
                for (int y = 1; y <= 9; y++)
                    if (Hash(x, y + 90) < threshold && Hash(x + 3, y) % 3 == 0)
                        corruption.SetTile(new Vector3Int(x, y, 0), tile(Hash(x, y) % 2 == 0 ? "corruptA" : "corruptB"));
                if (zone >= 2 && !IsPit(x) && Hash(x, -3) < threshold)
                    corruption.SetTile(new Vector3Int(x, -2, 0), tile("corruptB"));
            }
        }

        public static bool IsSolid(string tileName)
        {
            return tileName.StartsWith("ground") || tileName.StartsWith("fill") || tileName.StartsWith("arena") || tileName.StartsWith("wall") || tileName.StartsWith("plat");
        }
    }
}
