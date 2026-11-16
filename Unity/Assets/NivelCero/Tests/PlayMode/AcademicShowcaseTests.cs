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
    /// Longer automated playthrough of the 2D scene used for the gameplay video and
    /// visual review. Unlike the strict route test it also exercises the optional
    /// catwalk fragment, talking to Bit, a failed circuit validation, pause, a fall
    /// death with checkpoint respawn and the full Guardian encounter. Input is the
    /// keyboard/mouse device plus the same commands the menus/circuit UI call.
    /// With NIVEL_CERO_RECORD_GAMEPLAY=1 it records Unity's framebuffer and AudioListener;
    /// with NIVEL_CERO_STILLS_DIR it saves PNG stills at named moments.
    /// </summary>
    public sealed class AcademicShowcaseTests
    {
        Academic2D world;
        Keyboard keyboard;
        Mouse mouse;
        float previousCapture;
        bool previousBackground;
        InputSettings.BackgroundBehavior previousFocus;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorFocus;
        InputSettings.UpdateMode previousUpdate;
        int frames;
        bool recordMode;
        string evidenceDir, stillsDir;
        EvidenceAudioCapture audioCapture;
        EvidenceFrameCapture frameCapture;
        readonly List<string> log = new List<string>();
        readonly List<InputDevice> disabled = new List<InputDevice>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            recordMode = System.Environment.GetEnvironmentVariable("NIVEL_CERO_RECORD_GAMEPLAY") == "1";
            stillsDir = System.Environment.GetEnvironmentVariable("NIVEL_CERO_STILLS_DIR");
            if (!Application.isEditor) Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
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
            // Ignore physical devices while recording: typing elsewhere must not reach the game.
            disabled.Clear();
            foreach (var device in InputSystem.devices)
                if ((device is Keyboard || device is Mouse || device is Gamepad) && device.enabled) { InputSystem.DisableDevice(device); disabled.Add(device); }
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            yield return SceneManager.LoadSceneAsync("AE4_2D_SectorDeArranque", LoadSceneMode.Single);
            yield return null; yield return null;
            world = Object.FindFirstObjectByType<Academic2D>();
            Assert.That(world, Is.Not.Null);
            world.PauseWhenUnfocused = false;
            if (recordMode)
            {
                evidenceDir = Path.GetFullPath(System.Environment.GetEnvironmentVariable("NIVEL_CERO_EVIDENCE_DIR"));
                Directory.CreateDirectory(evidenceDir);
                audioCapture = EvidenceAudioCapture.Begin(world.GameCamera.gameObject, Path.Combine(evidenceDir, "gameplay.wav"));
                frameCapture = EvidenceFrameCapture.Begin(world.GameCamera.gameObject, evidenceDir);
                File.WriteAllText(Path.Combine(evidenceDir, "recording-ready.txt"), System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
            }
            if (!string.IsNullOrEmpty(stillsDir)) Directory.CreateDirectory(stillsDir);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (frameCapture != null) frameCapture.StopCapture();
            if (audioCapture != null) audioCapture.StopCapture();
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            foreach (var device in disabled) if (device.added) InputSystem.EnableDevice(device);
            var empty = SceneManager.CreateScene("Showcase cleanup");
            SceneManager.SetActiveScene(empty);
            var scene = SceneManager.GetSceneByName("AE4_2D_SectorDeArranque");
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.captureDeltaTime = previousCapture;
            Application.runInBackground = previousBackground;
            InputSystem.settings.backgroundBehavior = previousFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorFocus;
            InputSystem.settings.updateMode = previousUpdate;
            Time.timeScale = 1;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator ShowcaseOfTheWholeSector()
        {
            yield return Hold(3f, "menu");                       // main menu with the scene behind it
            world.StartGame();                                   // same command as INICIAR SECTOR 2D
            yield return Hold(2.0f, "start");
            yield return Press(Key.E);                           // talk to Bit (trigger zone)
            yield return Hold(4.0f, "bit-talk");
            yield return MoveTo(16.5f);
            yield return JumpTo(24, "jump-pit-1");
            yield return MoveTo(25);
            Assert.That(world.HasFragment(0), Is.True, State());
            yield return Hold(1.5f, "fragment-1");
            yield return MoveTo(32);
            yield return Hold(1.2f, "checkpoint-1");
            // Optional catwalk: one-way platform, reached by jumping from below.
            yield return MoveTo(40);
            yield return JumpUp("catwalk");
            Assert.That(world.PlayerPosition.y, Is.GreaterThan(4.3f), "Zero must land on the optional catwalk. " + State());
            yield return MoveTo(42.2f);
            Assert.That(world.HasFragment(2), Is.True, "Optional fragment on the catwalk. " + State());
            yield return Hold(2.0f, "optional-fragment");
            yield return MoveTo(46.6f);
            yield return Hold(.6f, "catwalk-drop");
            // A fall into the purge pit: death animation, defeat screen and checkpoint respawn.
            yield return MoveTo(49.5f, false);
            for (int f = 0; f < 400 && !world.IsDefeated; f++) { Input(0); yield return null; }
            Assert.That(world.IsDefeated, Is.True, "Falling into the pit must defeat Zero. " + State());
            yield return Hold(3.0f, "defeat-screen");
            world.RespawnFromMenu();                             // same command as REAPARECER EN CHECKPOINT
            yield return Hold(1.5f, "respawn");
            Assert.That(world.CurrentHealth, Is.EqualTo(100));
            Assert.That(world.HasFragment(0) && world.HasFragment(2), Is.True, "Fragments survive the respawn.");
            yield return MoveTo(46.5f);
            yield return JumpTo(54, "jump-pit-2");
            yield return MoveTo(59);
            Assert.That(world.RequiredFragments, Is.EqualTo(2), State());
            yield return MoveTo(63.5f);
            yield return Press(Key.E);                           // terminal
            yield return Hold(1.4f, "hack-open");
            Assert.That(world.ActiveCircuit, Is.Not.Null);
            Assert.That(world.ValidateCircuit(), Is.False);     // premature validation: penalty feedback
            yield return Hold(1.2f, "hack-error");
            CircuitPuzzle circuit = world.ActiveCircuit;
            var clone = new CircuitPuzzle(413, circuit.Size);
            Assert.That(Solve(clone, 0, 0, CircuitPuzzle.West, new bool[clone.Size, clone.Size]), Is.True);
            for (int y = 0; y < clone.Size; y++)
                for (int x = 0; x < clone.Size; x++)
                    for (int turns = 0; circuit.MaskAt(x, y) != clone.MaskAt(x, y) && turns < 4; turns++)
                    {
                        circuit.Rotate(x, y);
                        yield return new WaitForSecondsRealtime(.3f);
                    }
            yield return new WaitForSecondsRealtime(.8f);
            Still("hack-solved");
            Assert.That(world.ValidateCircuit(), Is.True);
            yield return Hold(3.0f, "firewall-down");
            yield return Press(Key.Escape);
            yield return HoldUnscaled(2.2f, "pause");
            yield return Press(Key.Escape);
            yield return MoveTo(73);
            yield return MoveTo(84);
            yield return Hold(.1f, "boss-intro");
            for (int f = 0; f < 180 && world.InCinematic; f++) { Input(0, Key.None, true); if (f == 90) Still("boss-title"); yield return null; }
            int jumpHold = 0, shots = 0;
            float home = 83;
            for (int f = 0; f < 5000 && world.BossHealth > 0; f++)
            {
                float x = world.PlayerPosition.x;
                float? goal = null;
                if (!world.IsPositionSafe(x)) goal = NearestSafe(x);
                else if (Mathf.Abs(x - home) > 2.5f && world.IsPositionSafe(home)) goal = home;
                float axis = goal.HasValue && Mathf.Abs(goal.Value - x) > .15f ? Mathf.Sign(goal.Value - x) : 0;
                float wave = world.ShockwaveDistance;
                if (jumpHold <= 0 && world.IsGrounded && wave > .3f && wave < 2.2f) jumpHold = 18;
                Key action = jumpHold > 0 ? Key.Space : Key.None;
                if (jumpHold > 0) jumpHold--;
                Input(axis, action, true);
                if (++shots % 240 == 0) Still("boss-" + world.Boss + "-p" + world.BossPhase);
                yield return null;
            }
            Assert.That(world.BossHealth, Is.EqualTo(0), State());
            yield return Hold(1.2f, "boss-defeated");
            yield return Hold(2.0f, "boss-collapse");
            yield return MoveTo(102);
            Assert.That(world.LevelCompleted, Is.True, State());
            yield return HoldUnscaled(7f, "victory");
            foreach (string line in log) TestContext.Out.WriteLine(line);
            TestContext.Out.WriteLine("SHOWCASE COMPLETE: " + State() + " frames=" + frames + " deaths=" + world.DeathCount);
            if (frameCapture != null) frameCapture.StopCapture();
            if (audioCapture != null) audioCapture.StopCapture();
        }

        // ------------------------------------------------------------ helpers
        IEnumerator Hold(float seconds, string still)
        {
            int n = Mathf.RoundToInt(seconds * 60);
            for (int f = 0; f < n; f++) { Input(0); if (f == n / 2) Still(still); yield return null; }
        }
        IEnumerator HoldUnscaled(float seconds, string still)
        {
            float end = Time.realtimeSinceStartup + seconds; bool taken = false;
            while (Time.realtimeSinceStartup < end) { Input(0); if (!taken && Time.realtimeSinceStartup > end - seconds * .5f) { Still(still); taken = true; } yield return null; }
        }
        IEnumerator MoveTo(float x, bool requireArrival = true)
        {
            for (int f = 0; f < 1200; f++)
            {
                if (world.LevelCompleted || world.IsDefeated || world.IsDying) yield break;
                float difference = x - world.PlayerPosition.x;
                if (Mathf.Abs(difference) < .18f) { Input(0); yield return null; yield break; }
                Input(Mathf.Sign(difference));
                yield return null;
            }
            if (requireArrival) Assert.Fail("Waypoint not reached: " + State());
        }
        IEnumerator JumpTo(float destination, string still)
        {
            for (int f = 0; f < 240; f++)
            {
                float difference = destination - world.PlayerPosition.x;
                Input(Mathf.Abs(difference) < .25f ? 0 : Mathf.Sign(difference), f < 20 ? Key.Space : Key.None, false, f == 14 ? Key.LeftShift : Key.None);
                if (f == 16) Still(still);
                if (f > 20 && world.IsGrounded && Mathf.Abs(difference) < .4f) { Input(0); yield return null; yield break; }
                yield return null;
            }
            Assert.Fail("Jump did not land: " + State());
        }
        IEnumerator JumpUp(string still)
        {
            for (int f = 0; f < 120; f++)
            {
                Input(0, f < 24 ? Key.Space : Key.None);
                if (f == 18) Still(still);
                if (f > 30 && world.IsGrounded) break;
                yield return null;
            }
        }
        IEnumerator Press(Key key) { Input(0, key); yield return null; Input(0); yield return null; }
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
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            var keys = new List<Key>();
            if (axis > .1f) keys.Add(Key.D); else if (axis < -.1f) keys.Add(Key.A);
            if (extra != Key.None) keys.Add(extra);
            if (second != Key.None) keys.Add(second);
            Vector3 target = world.GameCamera != null ? world.GameCamera.WorldToScreenPoint(fire ? (Vector3)world.GuardianPosition : (Vector3)(world.PlayerPosition + new Vector2(world.Facing * 4, .4f))) : Vector3.zero;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(target.x, target.y), buttons = (ushort)(fire ? 1 : 0) });
        }
        void Still(string name)
        {
            log.Add("t=" + Time.time.ToString("0.0") + " " + name + " " + State());
            if (string.IsNullOrEmpty(stillsDir) || string.IsNullOrEmpty(name)) return;
            ScreenCapture.CaptureScreenshot(Path.Combine(stillsDir, frames.ToString("00000") + "-" + name + ".png"));
        }
        string State()
        {
            return "pos=" + world.PlayerPosition.ToString("F2") + " hp=" + world.CurrentHealth + " frag=" + world.FragmentCount +
                " gate=" + world.GateUnlocked + " boss=" + world.Boss + "/" + world.BossHealth + " deaths=" + world.DeathCount;
        }
        static bool Solve(CircuitPuzzle puzzle, int x, int y, int incoming, bool[,] seen)
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
                        if (Solve(puzzle, nx, ny, opposite[d], seen)) return true;
                    }
                }
                puzzle.Rotate(x, y);
            }
            seen[x, y] = false; return false;
        }
    }
}
