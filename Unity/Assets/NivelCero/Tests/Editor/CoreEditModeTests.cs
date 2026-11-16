using System;
using System.Reflection;
using NivelCero.Core;
using NUnit.Framework;

namespace NivelCero.Tests
{
    public sealed class CoreEditModeTests
    {
        // Controlled topology isolates flow validation from generation. Reflection
        // is confined to tests and does not expose solution controls to gameplay.
        private static CircuitPuzzle KnownRoute()
        {
            CircuitPuzzle puzzle = new CircuitPuzzle(1, 2);
            FieldInfo field = typeof(CircuitPuzzle).GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            int[,] cells = (int[,])field.GetValue(puzzle);
            cells[0, 0] = CircuitPuzzle.West | CircuitPuzzle.East;
            cells[1, 0] = CircuitPuzzle.West | CircuitPuzzle.South;
            cells[0, 1] = CircuitPuzzle.North;
            cells[1, 1] = CircuitPuzzle.North | CircuitPuzzle.East;
            return puzzle;
        }

        [Test]
        public void ReciprocalRoutePowersDestinationAndCompletes()
        {
            CircuitPuzzle puzzle = KnownRoute();
            Assert.That(puzzle.IsPowered(1, 1), Is.True);
            Assert.That(puzzle.IsSolved, Is.True);
        }

        [Test]
        public void NonreciprocalNeighborDoesNotTransmitEnergy()
        {
            CircuitPuzzle puzzle = KnownRoute();
            puzzle.Rotate(1, 0); // W+S becomes N+W: no south connection.
            Assert.That(puzzle.IsPowered(1, 0), Is.True);
            Assert.That(puzzle.IsPowered(1, 1), Is.False);
            Assert.That(puzzle.IsSolved, Is.False);
        }

        [Test]
        public void PoweredDestinationStillNeedsAnEastFacingOutput()
        {
            CircuitPuzzle puzzle = KnownRoute();
            // N+E -> E+S -> S+W -> W+N. Input exists, exit does not.
            for (int i = 0; i < 3; i++) puzzle.Rotate(1, 1);
            Assert.That(puzzle.IsPowered(1, 1), Is.True);
            Assert.That(puzzle.IsSolved, Is.False);
        }

        [Test]
        public void NewPuzzlesAreScrambledDeterministicallyAndResetExactly()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                CircuitPuzzle puzzle = new CircuitPuzzle(seed);
                CircuitPuzzle reference = new CircuitPuzzle(seed);
                Assert.That(puzzle.IsSolved, Is.False, "seed " + seed);
                for (int y = 0; y < puzzle.Size; y++)
                    for (int x = 0; x < puzzle.Size; x++)
                    {
                        Assert.That(puzzle.MaskAt(x, y), Is.EqualTo(reference.MaskAt(x, y)));
                        puzzle.Rotate(x, y);
                    }
                puzzle.Reset();
                for (int y = 0; y < puzzle.Size; y++)
                    for (int x = 0; x < puzzle.Size; x++)
                        Assert.That(puzzle.MaskAt(x, y), Is.EqualTo(reference.MaskAt(x, y)));
            }
        }

        [Test]
        public void RotationAndCoordinateBoundsAreDefined()
        {
            CircuitPuzzle puzzle = KnownRoute();
            int original = puzzle.MaskAt(0, 0);
            for (int i = 0; i < 4; i++) puzzle.Rotate(0, 0);
            Assert.That(puzzle.MaskAt(0, 0), Is.EqualTo(original));
            Assert.Throws<ArgumentOutOfRangeException>(() => puzzle.Rotate(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => puzzle.IsPowered(2, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CircuitPuzzle(1, 1));
        }

        [Test]
        public void ExitRequiresBothMainFragmentsAndBossButOptionalIsOptional()
        {
            ProgressData progress = new ProgressData();
            progress.Collect(ProgressData.MainFragmentOne);
            progress.Collect(ProgressData.OptionalFragment);
            progress.bossDefeated = true;
            Assert.That(progress.CanExit, Is.False);
            progress.Collect(ProgressData.MainFragmentTwo);
            Assert.That(progress.CanExit, Is.True);
            progress.fragmentIds.Remove(ProgressData.OptionalFragment);
            Assert.That(progress.CanExit, Is.True);
            progress.bossDefeated = false;
            Assert.That(progress.CanExit, Is.False);
        }

        [Test]
        public void CollectionIsIdempotentAndCorruptProgressIsRejected()
        {
            ProgressData progress = new ProgressData();
            Assert.That(progress.Collect(ProgressData.MainFragmentOne), Is.True);
            Assert.That(progress.Collect(ProgressData.MainFragmentOne), Is.False);
            Assert.That(progress.FragmentCount, Is.EqualTo(1));
            Assert.That(progress.IsValid(), Is.True);
            progress.checkpointId = 99;
            Assert.That(progress.IsValid(), Is.False);
            progress.checkpointId = 0;
            progress.elapsedSeconds = Single.PositiveInfinity;
            Assert.That(progress.IsValid(), Is.False);
            progress.elapsedSeconds = 0;
            progress.fragmentIds.Add(ProgressData.MainFragmentOne);
            Assert.That(progress.IsValid(), Is.False);
        }
    }
}
