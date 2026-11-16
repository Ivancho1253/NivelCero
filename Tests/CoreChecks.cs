using System;
using NivelCero.Core;

public static class CoreChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("FAIL: " + message);
    }

    public static string Run()
    {
        checks = 0;
        for (int size = 2; size <= 5; size++)
            for (int seed = 0; seed < 40; seed++)
            {
                CircuitPuzzle board = new CircuitPuzzle(seed, size);
                CircuitPuzzle same = new CircuitPuzzle(seed, size);
                Check(!board.IsSolved, "new puzzle needs input");
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        int mask = board.MaskAt(x, y);
                        Check(mask == same.MaskAt(x, y), "deterministic seed");
                        for (int r = 0; r < 4; r++) board.Rotate(x, y);
                        Check(board.MaskAt(x, y) == mask, "four turns restore orientation");
                    }
                Check(Solve(board, 0, 0, 8, new bool[size, size]), "independent solver finds route");
                Check(board.IsSolved, "reciprocal route accepted");
                Check(board.IsPowered(size - 1, size - 1), "destination receives source flow");
                while ((board.MaskAt(0, 0) & 8) != 0) board.Rotate(0, 0);
                Check(!board.IsSolved, "disconnected source rejects success");
                Check(!board.IsPowered(size - 1, size - 1), "unpowered source cannot energize destination");
                board.Reset();
                Check(!board.IsSolved, "reset restores scrambled state");
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        Check(board.MaskAt(x, y) == same.MaskAt(x, y), "reset restores each tile");
            }
        ExpectBounds(delegate { new CircuitPuzzle(1, 1); });
        ExpectBounds(delegate { new CircuitPuzzle(1, 13); });
        CircuitPuzzle bounds = new CircuitPuzzle(-23);
        ExpectBounds(delegate { bounds.Rotate(-1, 0); });
        ExpectBounds(delegate { bounds.MaskAt(0, 4); });
        ExpectBounds(delegate { bounds.IsPowered(4, 0); });

        ProgressData progress = new ProgressData();
        Check(progress.IsValid(), "new progress valid");
        Check(!progress.CanExit, "new player cannot finish");
        Check(progress.Collect(ProgressData.MainFragmentOne), "first collection succeeds");
        Check(!progress.Collect(ProgressData.MainFragmentOne), "duplicate collection ignored");
        Check(progress.FragmentCount == 1, "duplicate does not inflate count");
        progress.Collect(ProgressData.OptionalFragment);
        progress.bossDefeated = true;
        Check(!progress.CanExit, "optional cannot substitute for required fragment");
        progress.Collect(ProgressData.MainFragmentTwo);
        Check(progress.CanExit, "all required progress unlocks exit");
        ProgressData mainOnly = new ProgressData();
        mainOnly.Collect(ProgressData.MainFragmentOne);
        mainOnly.Collect(ProgressData.MainFragmentTwo);
        mainOnly.bossDefeated = true;
        Check(mainOnly.CanExit && mainOnly.FragmentCount == 2, "optional shard is genuinely optional");
        mainOnly.checkpointId = -1;
        Check(!mainOnly.IsValid(), "negative checkpoint rejected");
        mainOnly.checkpointId = 3;
        Check(!mainOnly.IsValid(), "unknown checkpoint rejected");
        mainOnly.checkpointId = 0;
        mainOnly.elapsedSeconds = Single.NaN;
        Check(!mainOnly.IsValid(), "corrupted elapsed time rejected");
        mainOnly.elapsedSeconds = 0;
        mainOnly.fragmentIds.Add(ProgressData.MainFragmentOne);
        Check(!mainOnly.IsValid(), "duplicate ids in loaded data rejected");
        mainOnly.fragmentIds.Clear();
        mainOnly.fragmentIds.Add("unknown");
        Check(!mainOnly.IsValid(), "unknown fragment in save rejected");
        bool badIdRejected = false;
        try { progress.Collect("unknown"); } catch (ArgumentException) { badIdRejected = true; }
        Check(badIdRejected, "unknown collection rejected");
        return "PASS: " + checks + " assertions; 160 independently solved circuit boards; progression invariants.";
    }

    // Search possible physical routes and rotate tiles to match each entry/exit.
    // This solver knows neither the generator's chosen route nor its RNG sequence.
    private static bool Solve(CircuitPuzzle board, int x, int y, int incoming, bool[,] seen)
    {
        int size = board.Size;
        seen[x, y] = true;
        int[] dx = { 0, 1, 0, -1 }, dy = { -1, 0, 1, 0 };
        int[] ports = { 1, 2, 4, 8 }, opposite = { 4, 8, 1, 2 };
        for (int r = 0; r < 4; r++)
        {
            int mask = board.MaskAt(x, y);
            if ((mask & incoming) != 0)
            {
                if (x == size - 1 && y == size - 1 && (mask & 2) != 0) return true;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + dx[d], ny = y + dy[d];
                    if (ports[d] == incoming || (mask & ports[d]) == 0 || nx < 0 || ny < 0 || nx >= size || ny >= size || seen[nx, ny]) continue;
                    if (Solve(board, nx, ny, opposite[d], seen)) return true;
                }
            }
            board.Rotate(x, y);
        }
        seen[x, y] = false;
        return false;
    }

    private static void ExpectBounds(Action action)
    {
        bool rejected = false;
        try { action(); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "out-of-range value rejected");
    }
}
