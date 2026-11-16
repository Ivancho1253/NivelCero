using System.Collections;
using System.Collections.Generic;
using System.IO;
using NivelCero.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NivelCero.Tests
{
    /// <summary>
    /// Completes the actual academic scene through ordinary input and the same
    /// circuit commands exposed by its UI. No pose, life, pickup, checkpoint,
    /// boss, gate or victory state is written by this test.
    /// </summary>
    public sealed class AcademicFullRouteTests
    {
        readonly System.Collections.Generic.List<InputDevice> physicalDevices = new System.Collections.Generic.List<InputDevice>();
        Academic2D world;
        Keyboard keyboard;
        Mouse mouse;
        float previousCapture;
        bool previousBackground;
        InputSettings.BackgroundBehavior previousFocus;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorFocus;
        InputSettings.UpdateMode previousUpdate;
        int frames;
        string stage;
        bool recordMode;
        string evidenceDir;
        int previousFrameRate, previousVsync;
        EvidenceAudioCapture audioCapture;
        EvidenceFrameCapture frameCapture;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            recordMode = System.Environment.GetEnvironmentVariable("NIVEL_CERO_RECORD_GAMEPLAY") == "1";
            previousFrameRate = Application.targetFrameRate;
            previousVsync = QualitySettings.vSyncCount;
            if (recordMode)
            {
                evidenceDir = System.Environment.GetEnvironmentVariable("NIVEL_CERO_EVIDENCE_DIR");
                Assert.That(string.IsNullOrWhiteSpace(evidenceDir), Is.False, "Recording needs NIVEL_CERO_EVIDENCE_DIR");
                evidenceDir = Path.GetFullPath(evidenceDir); Directory.CreateDirectory(evidenceDir);
                QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            }
            previousCapture = Time.captureDeltaTime;
            previousBackground = Application.runInBackground;
            previousFocus = InputSystem.settings.backgroundBehavior;
            previousEditorFocus = InputSystem.settings.editorInputBehaviorInPlayMode;
            previousUpdate = InputSystem.settings.updateMode;
            Time.captureDeltaTime = 1f / 60;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            // Physical devices are ignored so typing in other windows cannot steer the bot.
            foreach (var device in InputSystem.devices)
                if ((device is Keyboard || device is Mouse || device is Gamepad) && device.enabled) { InputSystem.DisableDevice(device); physicalDevices.Add(device); }
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(keyboard); InputSystem.EnableDevice(mouse);
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            yield return SceneManager.LoadSceneAsync("AE4_2D_SectorDeArranque", LoadSceneMode.Single);
            yield return null;
            world = Object.FindFirstObjectByType<Academic2D>();
            Assert.That(world, Is.Not.Null);
            Assert.That(world.GameCamera, Is.Not.Null);
            world.PauseWhenUnfocused = false;
            frames = 0; stage = "Menú principal 2D";
            if (recordMode)
            {
                audioCapture = EvidenceAudioCapture.Begin(world.GameCamera.gameObject, Path.Combine(evidenceDir, "gameplay.wav"));
                frameCapture = EvidenceFrameCapture.Begin(world.GameCamera.gameObject, evidenceDir);
                File.WriteAllText(Path.Combine(evidenceDir, "recording-ready.txt"), System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
                yield return new WaitForSecondsRealtime(3);
            }
            world.StartGame(); // Same command invoked by INICIAR SECTOR 2D.
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (frameCapture != null) frameCapture.StopCapture();
            if (audioCapture != null) audioCapture.StopCapture();
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            foreach (var device in physicalDevices) if (device.added) InputSystem.EnableDevice(device);
            physicalDevices.Clear();
            var empty = SceneManager.CreateScene("Academic route cleanup");
            SceneManager.SetActiveScene(empty);
            var scene = SceneManager.GetSceneByName("AE4_2D_SectorDeArranque");
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.captureDeltaTime = previousCapture;
            Application.targetFrameRate = previousFrameRate; QualitySettings.vSyncCount = previousVsync;
            Application.runInBackground = previousBackground;
            InputSystem.settings.backgroundBehavior = previousFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorFocus;
            InputSystem.settings.updateMode = previousUpdate;
            Time.timeScale = 1;
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator AcademicSectorCanBeCompletedThroughNormalActionsWithoutRespawn()
        {
            yield return Settle("Despertar y apoyo sobre Tilemap");
            yield return Press(Key.E); // Bit's actual proximity interaction.
            yield return MoveTo(16.5f, "Aproximación al primer vacío");
            yield return JumpTo(24, "Salto y dash sobre vacío 18–21");
            yield return MoveTo(25, "Primer Fragmento de Código");
            Assert.That(world.RequiredFragments, Is.EqualTo(1), State());
            yield return MoveTo(46.5f, "Ruta principal bajo la plataforma opcional");
            yield return JumpTo(54, "Salto y dash sobre vacío 48–51");
            yield return MoveTo(59, "Segundo Fragmento de Código");
            Assert.That(world.RequiredFragments, Is.EqualTo(2), State());
            yield return MoveTo(63.5f, "Terminal de acceso restringido");
            yield return Press(Key.E);
            Assert.That(world.ActiveCircuit, Is.Not.Null, State());
            Assert.That(world.GateUnlocked, Is.False, State());
            Assert.That(world.ValidateCircuit(), Is.False, "A scrambled circuit must not unlock the physical firewall.");
            CircuitPuzzle circuit = world.ActiveCircuit;
            if (recordMode)
            {
                // Academic2D creates its first attempt with seed 413 + hackAttempts.
                // This route has no failed/cancelled attempts, so the seed is 413.
                var clone = new CircuitPuzzle(413, circuit.Size);
                Assert.That(SolveByRotations(clone, 0, 0, CircuitPuzzle.West, new bool[clone.Size, clone.Size]), Is.True);
                for (int y = 0; y < clone.Size; y++)
                for (int x = 0; x < clone.Size; x++)
                {
                    int turns = 0;
                    while (circuit.MaskAt(x, y) != clone.MaskAt(x, y))
                    {
                        Assert.That(turns++, Is.LessThan(4), "Cloned circuit must contain the same rotatable module");
                        circuit.Rotate(x, y);
                        yield return new WaitForSecondsRealtime(.5f);
                    }
                }
            }
            else Assert.That(SolveByRotations(circuit, 0, 0, CircuitPuzzle.West, new bool[circuit.Size, circuit.Size]), Is.True);
            Assert.That(circuit.IsSolved, Is.True);
            Assert.That(world.ValidateCircuit(), Is.True, "Validate must use the actual rotated circuit.");
            Assert.That(world.GateUnlocked, Is.True);
            Assert.That(world.ActiveCircuit, Is.Null);
            yield return MoveTo(73, "Checkpoint anterior al Guardián");
            Assert.That(world.ActiveCheckpoint.x, Is.EqualTo(73).Within(.01f), State());
            yield return MoveTo(84, "Entrada al Núcleo de Seguridad (presentación del Guardián)");
            Assert.That(world.Boss, Is.Not.EqualTo(Academic2D.BossState.Dormant), "Crossing the arena threshold must wake the Guardian. " + State());
            yield return Press(Key.Digit1);
            stage = "Combatir al Guardián: esquivar marcas, saltar ondas y disparar en las aperturas"; Log();
            int jumpHold = 0, exposedFrames = 0;
            float home = 83;
            for (int f = 0; f < 4200 && world.BossHealth > 0; f++)
            {
                EnsureAlive();
                float x = world.PlayerPosition.x;
                float? goal = null;
                // Only public, player-visible information: red markers and the travelling wave.
                if (!world.IsPositionSafe(x)) goal = NearestSafe(x);
                else if (Mathf.Abs(x - home) > 2.5f && world.IsPositionSafe(home)) goal = home;
                float axis = goal.HasValue && Mathf.Abs(goal.Value - x) > .15f ? Mathf.Sign(goal.Value - x) : 0;
                float wave = world.ShockwaveDistance;
                if (jumpHold <= 0 && world.IsGrounded && wave > .3f && wave < 2.2f) jumpHold = 18;
                Key action = jumpHold > 0 ? Key.Space : Key.None;
                if (jumpHold > 0) jumpHold--;
                if (world.BossVulnerable) exposedFrames++;
                Input(axis, action, true);
                yield return null;
            }
            Input(0);
            Assert.That(world.BossHealth, Is.EqualTo(0), State());
            Assert.That(exposedFrames, Is.GreaterThan(0), "The Guardian must open its core between attacks.");
            yield return MoveTo(102, "Atravesar el portal de salida");
            Assert.That(world.LevelCompleted, Is.True, State());
            Assert.That(world.DeathCount, Is.Zero, "A successful route must never invoke respawn.");
            Assert.That(world.RequiredFragments, Is.EqualTo(2));
            Assert.That(world.FragmentCount, Is.EqualTo(2), "The upper-route fragment must remain optional.");
            TestContext.Out.WriteLine("ACADEMIC ROUTE COMPLETE: " + State() + " frames=" + frames);
            if (recordMode)
            {
                Input(0);
                yield return new WaitForSecondsRealtime(5);
                frameCapture.StopCapture(); audioCapture.StopCapture();
            }
        }

        IEnumerator MoveTo(float x, string description)
        {
            stage = description; Log();
            for (int f = 0; f < 900; f++)
            {
                EnsureAlive();
                if (world.LevelCompleted) yield break;
                float difference = x - world.PlayerPosition.x;
                if (Mathf.Abs(difference) < .18f) { Input(0); yield return null; yield break; }
                Input(Mathf.Sign(difference));
                yield return null;
            }
            Assert.Fail("Waypoint not reached: " + State());
        }

        IEnumerator JumpTo(float destination, string description)
        {
            stage = description; Log();
            for (int f = 0; f < 240; f++)
            {
                EnsureAlive();
                float difference = destination - world.PlayerPosition.x;
                // Hold jump like a player does (variable jump height), dash mid-air.
                Key action = f < 20 ? Key.Space : Key.None;
                Input(Mathf.Abs(difference) < .25f ? 0 : Mathf.Sign(difference), action, false, f == 14 ? Key.LeftShift : Key.None);
                if (f > 20 && world.IsGrounded && Mathf.Abs(difference) < .4f)
                { Input(0); yield return null; yield break; }
                yield return null;
            }
            Assert.Fail("Jump did not land on its destination: " + State());
        }

        IEnumerator Settle(string description)
        {
            stage = description;
            for (int f = 0; f < 180; f++)
            {
                EnsureAlive(); Input(0);
                if (f > 5 && world.IsGrounded) yield break;
                yield return null;
            }
            Assert.Fail("No support detected: " + State());
        }
        IEnumerator Press(Key key)
        {
            Input(0, key); yield return null;
            Input(0); yield return null;
        }
        float NearestSafe(float x)
        {
            float best = x, bestDistance = float.MaxValue;
            for (float candidate = 78.6f; candidate <= 88.5f; candidate += .25f)
            {
                if (!world.IsPositionSafe(candidate)) continue;
                float d = Mathf.Abs(candidate - x);
                if (d < bestDistance) { bestDistance = d; best = candidate; }
            }
            return best;
        }

        void Input(float axis, Key extra = Key.None, bool fire = false, Key second = Key.None)
        {
            frames++;
            Assert.That(frames, Is.LessThan(12000), State());
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            var keys = new List<Key>();
            if (axis > .1f) keys.Add(Key.D); else if (axis < -.1f) keys.Add(Key.A);
            if (extra != Key.None) keys.Add(extra);
            if (second != Key.None) keys.Add(second);
            Vector3 target = world.GameCamera.WorldToScreenPoint(world.GuardianPosition);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(target.x, target.y), buttons = (ushort)(fire ? 1 : 0) });
        }
        void EnsureAlive()
        {
            Assert.That(world.CurrentHealth, Is.GreaterThan(0), "No health changes or respawn allowed: " + State());
            Assert.That(world.DeathCount, Is.Zero, State());
            Assert.That(world.IsPaused, Is.False, "OS focus must not silently consume the combat frame budget: " + State());
        }
        string State()
        {
            return stage + " pos=" + (world != null ? world.PlayerPosition.ToString("F2") : "n/a") +
                " hp=" + (world != null ? world.CurrentHealth.ToString("0.0") : "n/a") +
                " required=" + (world != null ? world.RequiredFragments : 0) +
                " gate=" + (world != null && world.GateUnlocked) +
                " bossHp=" + (world != null ? world.BossHealth.ToString("0.0") : "n/a") +
                " deaths=" + (world != null ? world.DeathCount : 0) +
                " paused=" + (world != null && world.IsPaused) + " timeScale=" + Time.timeScale + " focus=" + Application.isFocused +
                " keyboardCurrent=" + (Keyboard.current == keyboard) + " mouseCurrent=" + (Mouse.current == mouse) +
                " mouseEnabled=" + (mouse != null && mouse.enabled) + " syntheticFire=" + (mouse != null && mouse.leftButton.isPressed);
        }
        void Log() { TestContext.Out.WriteLine("ACADEMIC ROUTE: " + State()); }
        static bool SolveByRotations(CircuitPuzzle puzzle, int x, int y, int incoming, bool[,] seen)
        {
            seen[x, y] = true;
            int[] dx = { 0, 1, 0, -1 }, dy = { -1, 0, 1, 0 };
            int[] ports = { 1, 2, 4, 8 }, opposite = { 4, 8, 1, 2 };
            for (int turn = 0; turn < 4; turn++)
            {
                int mask = puzzle.MaskAt(x, y);
                if ((mask & incoming) != 0)
                {
                    if (x == puzzle.Size - 1 && y == puzzle.Size - 1 && (mask & 2) != 0) return true;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dx[d], ny = y + dy[d];
                        if ((mask & ports[d]) == 0 || nx < 0 || ny < 0 || nx >= puzzle.Size || ny >= puzzle.Size || seen[nx, ny]) continue;
                        if (SolveByRotations(puzzle, nx, ny, opposite[d], seen)) return true;
                    }
                }
                puzzle.Rotate(x, y);
            }
            seen[x, y] = false; return false;
        }
    }
}
