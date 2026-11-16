using System;
using System.Collections.Generic;

namespace NivelCero.Core
{
    /// <summary>
    /// Rotating-pipe puzzle. Bits: north 1, east 2, south 4, west 8.
    /// Source enters (0,0) from the west; exit leaves the southeast tile eastward.
    /// Flow requires reciprocal connections. Unconnected branches are allowed:
    /// this is a connectivity puzzle, not a sealed-pressure simulation.
    /// </summary>
    public sealed class CircuitPuzzle
    {
        public const int North = 1, East = 2, South = 4, West = 8;
        private readonly int[,] initial;
        private readonly int[,] cells;
        public int Size { get; private set; }

        public CircuitPuzzle(int seed, int size = 4)
        {
            if (size < 2 || size > 12) throw new ArgumentOutOfRangeException("size");
            Size = size;
            cells = new int[size, size];
            initial = new int[size, size];
            Random random = new Random(seed);
            int[] shapes = { North | East, North | South, North | East | West, North };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    cells[x, y] = shapes[random.Next(shapes.Length)];

            // A random monotone route guarantees a solution while retaining
            // optional branches and modules that do not belong to the route.
            int px = 0, py = 0, incoming = West;
            while (px != size - 1 || py != size - 1)
            {
                bool east = py == size - 1 || (px < size - 1 && random.Next(2) == 0);
                int outgoing = east ? East : South;
                cells[px, py] = incoming | outgoing;
                if (east) { px++; incoming = West; }
                else { py++; incoming = North; }
            }
            cells[px, py] = incoming | East;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int turns = random.Next(4);
                    for (int t = 0; t < turns; t++) cells[x, y] = Turn(cells[x, y]);
                }
            // Even when the random scramble happens to connect, the opening
            // board must demand player input. The source always has two ports.
            while (IsSolved) cells[0, 0] = Turn(cells[0, 0]);
            Array.Copy(cells, initial, cells.Length);
        }

        public int MaskAt(int x, int y)
        {
            CheckBounds(x, y);
            return cells[x, y];
        }

        public void Rotate(int x, int y)
        {
            CheckBounds(x, y);
            cells[x, y] = Turn(cells[x, y]);
        }

        public void Reset() { Array.Copy(initial, cells, cells.Length); }

        public bool IsPowered(int x, int y)
        {
            CheckBounds(x, y);
            return Reachable()[x, y];
        }

        public bool IsSolved
        {
            get { return (cells[Size - 1, Size - 1] & East) != 0 && Reachable()[Size - 1, Size - 1]; }
        }

        private bool[,] Reachable()
        {
            bool[,] seen = new bool[Size, Size];
            if ((cells[0, 0] & West) == 0) return seen;
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(0);
            seen[0, 0] = true;
            int[] dx = { 0, 1, 0, -1 }, dy = { -1, 0, 1, 0 };
            int[] port = { North, East, South, West }, reciprocal = { South, West, North, East };
            while (queue.Count != 0)
            {
                int cell = queue.Dequeue(), x = cell % Size, y = cell / Size;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + dx[d], ny = y + dy[d];
                    if (nx < 0 || ny < 0 || nx >= Size || ny >= Size || seen[nx, ny]) continue;
                    if ((cells[x, y] & port[d]) == 0 || (cells[nx, ny] & reciprocal[d]) == 0) continue;
                    seen[nx, ny] = true;
                    queue.Enqueue(ny * Size + nx);
                }
            }
            return seen;
        }

        private static int Turn(int mask) { return ((mask << 1) & 15) | ((mask >> 3) & 1); }
        private void CheckBounds(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Size || y >= Size) throw new ArgumentOutOfRangeException("coordinates");
        }
    }
}
