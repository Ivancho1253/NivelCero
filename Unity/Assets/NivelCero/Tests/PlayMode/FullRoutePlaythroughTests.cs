using System;
using System.Collections;
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
    /// A route bot using ordinary keyboard/mouse input. It never sets player pose,
    /// health, fragments, boss health, shield state or encounter state. Circuit
    /// actions use the same Rotate/Validate commands as the holographic UI.
    /// This is an automated completion check, not evidence of a human playtest.
    /// </summary>
    public sealed class FullRoutePlaythroughTests
    {
        readonly System.Collections.Generic.List<InputDevice> physicalDevices = new System.Collections.Generic.List<InputDevice>();
        Keyboard keyboard;
        Mouse mouse;
        Session session;
        string savePath;
        float previousCapture;
        bool previousBackground;
        InputSettings.BackgroundBehavior previousFocus;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode previousEditorFocus;
#endif
        InputSettings.UpdateMode previousUpdate;
        string objective;
        int frames, shots, hacks;
        bool recordMode;
        string evidenceDir;
        int previousFrameRate, previousVsync;
        EvidenceAudioCapture audioCapture;
        EvidenceFrameCapture frameCapture;
        PerformanceProbe performanceProbe;
        bool performanceMode;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            recordMode = Environment.GetEnvironmentVariable("NIVEL_CERO_RECORD_GAMEPLAY") == "1";
            performanceMode = Environment.GetEnvironmentVariable("NIVEL_CERO_PERFORMANCE_PROBE") == "1";
            Assert.That(recordMode && performanceMode, Is.False, "Performance sampling must run without PNG/audio recording overhead.");
            previousFrameRate = Application.targetFrameRate;
            previousVsync = QualitySettings.vSyncCount;
            if (recordMode || performanceMode)
            {
                evidenceDir = Environment.GetEnvironmentVariable("NIVEL_CERO_EVIDENCE_DIR");
                Assert.That(String.IsNullOrWhiteSpace(evidenceDir), Is.False, "Evidence capture needs NIVEL_CERO_EVIDENCE_DIR");
                evidenceDir = Path.GetFullPath(evidenceDir); Directory.CreateDirectory(evidenceDir);
                if (recordMode) { QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60; }
                if (performanceMode)
                {
                    Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
                    // Resolution changes apply at the end of the frame.
                    yield return null;
                    yield return null;
                    Assert.That(Screen.width, Is.EqualTo(1920), "Performance evidence requires actual 1080p output.");
                    Assert.That(Screen.height, Is.EqualTo(1080), "Performance evidence requires actual 1080p output.");
                }
            }
            previousCapture = Time.captureDeltaTime;
            previousBackground = Application.runInBackground;
            previousFocus = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
            previousEditorFocus = InputSystem.settings.editorInputBehaviorInPlayMode;
#endif
            previousUpdate = InputSystem.settings.updateMode;
            Time.captureDeltaTime = 1f / 60;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            // Physical devices are ignored so typing in other windows cannot steer the bot.
            foreach (var device in InputSystem.devices)
                if ((device is Keyboard || device is Mouse || device is Gamepad) && device.enabled) { InputSystem.DisableDevice(device); physicalDevices.Add(device); }
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(keyboard); InputSystem.EnableDevice(mouse);
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            yield return SceneManager.LoadSceneAsync("SectorDeArranque3D", LoadSceneMode.Single);
            yield return null;
            session = Session.I;
            if (session != null) session.PauseWhenUnfocused = false; // OS focus changes must not pause the recorded route
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Player, Is.Not.Null);
            savePath = Path.Combine(Path.GetTempPath(), "nivel-cero-route-" + Guid.NewGuid().ToString("N") + ".json");
            session.SavePathOverride = savePath;
            frames = shots = hacks = 0;
            session.Combat.Performed += action => { if (action == "Shoot") shots++; };
            if (recordMode)
            {
                audioCapture = EvidenceAudioCapture.Begin(session.Player.View.Lens.gameObject, Path.Combine(evidenceDir, "gameplay.wav"));
                frameCapture = EvidenceFrameCapture.Begin(session.Player.View.Lens.gameObject, evidenceDir);
                File.WriteAllText(Path.Combine(evidenceDir, "recording-ready.txt"), System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
                yield return new WaitForSecondsRealtime(3);
            }
            session.StartGame(false); // The exact command invoked by NUEVA PARTIDA.
            if (performanceMode)
                performanceProbe = PerformanceProbe.Begin(session.Player.View.Lens.gameObject, Path.Combine(evidenceDir, "performance.json"));
            yield return null;
            Assert.That(session.Blocked, Is.False, State());
            TestContext.Out.WriteLine("ROUTE INPUT: batch=" + Application.isBatchMode + " cursor=" + Cursor.lockState +
                " keyboard=" + (Keyboard.current == keyboard) + " mouse=" + (Mouse.current == mouse) +
                " focus=" + Application.isFocused);
            if (!Application.isBatchMode) Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (performanceProbe != null) performanceProbe.StopProbe(false);
            if (frameCapture != null) frameCapture.StopCapture();
            if (audioCapture != null) audioCapture.StopCapture();
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            foreach (var device in physicalDevices) if (device.added) InputSystem.EnableDevice(device);
            physicalDevices.Clear();
            var empty = SceneManager.CreateScene("Route test cleanup");
            SceneManager.SetActiveScene(empty);
            var world = SceneManager.GetSceneByName("SectorDeArranque3D");
            if (world.IsValid() && world.isLoaded) yield return SceneManager.UnloadSceneAsync(world);
            Time.captureDeltaTime = previousCapture;
            Application.targetFrameRate = previousFrameRate; QualitySettings.vSyncCount = previousVsync;
            Application.runInBackground = previousBackground;
            InputSystem.settings.backgroundBehavior = previousFocus;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorFocus;
#endif
            InputSystem.settings.updateMode = previousUpdate;
            Time.timeScale = 1;
            foreach (string suffix in new[] { "", ".tmp", ".bak" })
                if (!String.IsNullOrEmpty(savePath) && File.Exists(savePath + suffix)) File.Delete(savePath + suffix);
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator SectorDeArranqueCanBeCompletedThroughNormalActions()
        {
            yield return Press(Key.E); // Bit's actual interaction handler.
            yield return Press(Key.V);
            Assert.That(session.Player.View.FirstPerson, Is.True);
            yield return MoveTo(0, 18.5f, "Entrenamiento: aproximación al primer salto");
            yield return JumpTo(0, 25, "Salto y dash sobre el primer vacío");
            yield return MoveTo(0, 31, "Recuperar primer fragmento principal");
            Assert.That(session.Progress.HasFragment(ProgressData.MainFragmentOne), Is.True, State());
            yield return DestroyEnemy(SecurityAgent.Kind.Drone);
            yield return MoveTo(0, 33.5f, "Plataformas hacia seguridad");
            yield return JumpTo(0, 39, "Cruzar segundo salto");
            yield return MoveTo(0, 44, "Checkpoint de seguridad");
            yield return SettleOnGround("Aterrizar en el checkpoint de seguridad");
            Assert.That(session.Progress.checkpointId, Is.EqualTo(1), State());
            yield return Press(Key.V);
            Assert.That(session.Player.View.FirstPerson, Is.False);
            yield return MoveTo(2, 49, "Acceso restringido");
            yield return MoveTo(2, 51, "Terminal de acceso");
            yield return HackNearest("Abrir el firewall mediante circuitos");
            Assert.That(session.Progress.accessUnlocked, Is.True, State());
            Assert.That(session.AccessDoor.activeSelf, Is.False);
            yield return MoveTo(0, 54.2f, "Salida del firewall");
            yield return JumpTo(0, 61, "Salto al bloque de exploración");
            yield return Press(Key.V);
            Assert.That(session.Player.View.FirstPerson, Is.True);
            yield return DestroyEnemy(SecurityAgent.Kind.Ground);
            yield return MoveTo(4.3f, 62.5f, "Acceso a la ruta lateral entre pilares");
            yield return JumpTo(10, 62.5f, "Salto lateral por el hueco entre pilares");
            yield return MoveTo(12, 70, "Recuperar segundo fragmento principal");
            Assert.That(session.Progress.RequiredFragmentsRecovered, Is.True, State());
            Assert.That(session.Progress.HasFragment(ProgressData.OptionalFragment), Is.False, "El tercero debe ser opcional");
            yield return MoveTo(11, 78.5f, "Retorno por el puente lateral");
            yield return MoveTo(8, 79, "Despejar acceso al checkpoint");
            yield return MoveTo(0, 83, "Checkpoint anterior al Guardián");
            yield return SettleOnGround("Aterrizar en el checkpoint de la arena");
            Assert.That(session.Progress.checkpointId, Is.EqualTo(2), State());
            yield return DestroyEnemy(SecurityAgent.Kind.Turret);
            yield return MoveTo(-6, 95, "Entrada a la arena");
            yield return MoveTo(-7, 102, "Nodo ALFA");
            for (int phase = 1; phase <= 3 && !session.Progress.bossDefeated; phase++)
            {
                yield return HackNearest("Desactivar escudo de fase " + phase);
                Assert.That(session.Boss.Shielded, Is.False, State());
                yield return FightExposedCore();
                if (!session.Progress.bossDefeated) yield return MoveTo(-7, 102, "Regresar a ALFA para siguiente fase");
            }
            Assert.That(session.Progress.bossDefeated, Is.True, State());
            Assert.That(hacks, Is.EqualTo(4), "Acceso + tres fases del Guardián");
            yield return MoveTo(-6, 118, "Cruzar la arena restaurada");
            yield return MoveTo(0, 123, "Acceso al portal");
            yield return MoveTo(0, 136, "Escape del Sector de Arranque");
            Assert.That(session.Victory, Is.True, State());
            Assert.That(session.Progress.FragmentCount, Is.EqualTo(2));
            Assert.That(shots, Is.GreaterThanOrEqualTo(33), "15 impactos de enemigos + 18 del Guardián");
            TestContext.Out.WriteLine("ROUTE COMPLETE: " + State() + " shots=" + shots + " hacks=" + hacks + " frames=" + frames);
            if (performanceProbe != null) performanceProbe.StopProbe(true);
            if (recordMode)
            {
                Input(Vector3.zero, null, false);
                yield return new WaitForSecondsRealtime(5);
                frameCapture.StopCapture();
                audioCapture.StopCapture();
            }
        }

        IEnumerator MoveTo(float x, float z, string description)
        {
            objective = description; Log();
            var destination = new Vector3(x, 0, z);
            for (int f = 0; f < 900; f++)
            {
                EnsureAlive();
                if (session.Victory) yield break;
                var delta = destination - session.Player.transform.position; delta.y = 0;
                if (delta.magnitude < .45f) { Input(Vector3.zero, null, false); yield return null; yield break; }
                Input(delta, session.Player.transform.position + delta + Vector3.up * 1.6f, false);
                yield return null;
            }
            Assert.Fail("Waypoint not reached: " + State());
        }

        IEnumerator JumpTo(float x, float z, string description)
        {
            objective = description; Log();
            var destination = new Vector3(x, 0, z);
            for (int f = 0; f < 180; f++)
            {
                EnsureAlive();
                var delta = destination - session.Player.transform.position; delta.y = 0;
                if (delta.magnitude < .5f && f > 15) { Input(Vector3.zero, null, false); yield return null; yield break; }
                Input(delta, session.Player.transform.position + delta + Vector3.up * 1.6f, false,
                    f == 0 ? Key.Space : f == 5 ? Key.LeftShift : Key.None);
                yield return null;
            }
            Assert.Fail("Jump destination not reached: " + State());
        }

        IEnumerator SettleOnGround(string description)
        {
            objective = description; Log();
            var body = session.Player.GetComponent<CharacterController>();
            for (int f = 0; f < 180; f++)
            {
                EnsureAlive();
                Input(Vector3.zero, null, false);
                yield return null;
                if (body.isGrounded) { yield return null; yield break; }
            }
            Assert.Fail("No landing reached: " + State());
        }

        IEnumerator DestroyEnemy(SecurityAgent.Kind kind)
        {
            objective = "Combate contra " + kind; Log();
            SecurityAgent enemy = null;
            foreach (var candidate in UnityEngine.Object.FindObjectsByType<SecurityAgent>(FindObjectsSortMode.None))
                if (candidate.Type == kind) { enemy = candidate; break; }
            Assert.That(enemy, Is.Not.Null, State());
            for (int f = 0; f < 600 && enemy.gameObject.activeSelf; f++)
            {
                EnsureAlive();
                bool reload = session.Combat.Ammo == 0 && !session.Combat.Reloading;
                Input(Vector3.zero, enemy.transform.position + (kind == SecurityAgent.Kind.Ground ? Vector3.up * .35f : Vector3.zero), true, reload ? Key.R : Key.None);
                if (kind == SecurityAgent.Kind.Turret && (f == 0 || f == 30 || f == 90))
                    TestContext.Out.WriteLine("AIM frame=" + f + " target=" + enemy.transform.position + " " + ShotDiagnostics(enemy));
                yield return null;
            }
            Input(Vector3.zero, null, false); yield return null;
            if (enemy.gameObject.activeSelf) TestContext.Out.WriteLine(ShotDiagnostics(enemy));
            Assert.That(enemy.gameObject.activeSelf, Is.False, ShotDiagnostics(enemy));
        }

        IEnumerator HackNearest(string description)
        {
            objective = description; Log();
            yield return Press(Key.E);
            Assert.That(session.Puzzle, Is.Not.Null, "E must open the nearby terminal: " + State());
            if (recordMode)
            {
                var clone = new CircuitPuzzle(session.ActiveTerminal.Seed + session.Attempts, session.Puzzle.Size);
                Assert.That(SolveByRotations(clone, 0, 0, CircuitPuzzle.West, new bool[clone.Size, clone.Size]), Is.True);
                for (int y = 0; y < clone.Size; y++)
                for (int x = 0; x < clone.Size; x++)
                {
                    int turns = 0;
                    while (session.Puzzle.MaskAt(x, y) != clone.MaskAt(x, y))
                    {
                        Assert.That(turns++, Is.LessThan(4), "Cloned circuit must contain the same rotatable module");
                        session.Puzzle.Rotate(x, y);
                        yield return new WaitForSecondsRealtime(.5f);
                    }
                }
            }
            else Assert.That(SolveByRotations(session.Puzzle, 0, 0, CircuitPuzzle.West, new bool[session.Puzzle.Size, session.Puzzle.Size]), Is.True);
            Assert.That(session.Puzzle.IsSolved, Is.True);
            session.ValidateHack(); // Exact action of VALIDAR CIRCUITO.
            hacks++;
            yield return null;
            EnsureAlive();
        }

        IEnumerator FightExposedCore()
        {
            objective = "Atacar núcleo expuesto y esquivar descargas"; Log();
            float kite = 5;
            for (int f = 0; f < 1000 && !session.Progress.bossDefeated && !session.Boss.Shielded; f++)
            {
                EnsureAlive();
                Vector3 p = session.Player.transform.position;
                if (Mathf.Abs(p.x - kite) < .7f) kite *= -1;
                Vector3 wish = new Vector3(kite - p.x, 0, 102 - p.z);
                bool reload = session.Combat.Ammo == 0 && !session.Combat.Reloading;
                Input(wish, session.Boss.Core.bounds.center, true, reload ? Key.R : Key.None);
                yield return null;
            }
            Input(Vector3.zero, null, false); yield return null;
            Assert.That(session.Progress.bossDefeated || session.Boss.Shielded, Is.True, State());
        }

        IEnumerator Press(Key key)
        {
            Input(Vector3.zero, null, false, key); yield return null;
            Input(Vector3.zero, null, false); yield return null;
        }

        // Converts a world-space route intention to ordinary WASD and mouse deltas.
        // Reading pose for steering does not write to any gameplay state.
        void Input(Vector3 movement, Vector3? aim, bool fire, Key extra = Key.None)
        {
            frames++;
            Assert.That(frames, Is.LessThan(14000), State());
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            var keys = new System.Collections.Generic.List<Key>();
            var view = session.Player.View;
            if (movement.sqrMagnitude > .01f)
            {
                var forward = Vector3.ProjectOnPlane(view.Lens.transform.forward, Vector3.up).normalized;
                var right = Vector3.Cross(Vector3.up, forward);
                float f = Vector3.Dot(movement.normalized, forward), r = Vector3.Dot(movement.normalized, right);
                if (f > .3f) keys.Add(Key.W); else if (f < -.3f) keys.Add(Key.S);
                if (r > .3f) keys.Add(Key.D); else if (r < -.3f) keys.Add(Key.A);
            }
            if (extra != Key.None) keys.Add(extra);
            Vector2 mouseDelta = Vector2.zero;
            if (aim.HasValue)
            {
                Vector3 direction = aim.Value - view.Lens.transform.position;
                float wantedYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float currentYaw = view.Lens.transform.eulerAngles.y;
                float wantedPitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
                float currentPitch = Mathf.DeltaAngle(0, view.Lens.transform.eulerAngles.x);
                // Coroutine input is applied by a later dynamic input update and
                // camera LateUpdate. A fractional correction avoids chasing a
                // camera pose whose previous queued correction is still pending.
                const float steeringGain = .3f;
                mouseDelta.x = Mathf.Clamp(Mathf.DeltaAngle(currentYaw, wantedYaw) * steeringGain, -30, 30) / view.Sensitivity;
                mouseDelta.y = Mathf.Clamp((wantedPitch - currentPitch) * steeringGain, -20, 20) / (view.Sensitivity * (view.InvertY ? 1 : -1));
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = mouseDelta, buttons = (ushort)(fire ? 1 : 0) });
        }

        static bool SolveByRotations(CircuitPuzzle puzzle, int x, int y, int incoming, bool[,] seen)
        {
            seen[x, y] = true;
            int[] dx = { 0, 1, 0, -1 }, dy = { -1, 0, 1, 0 }, ports = { 1, 2, 4, 8 }, opposite = { 4, 8, 1, 2 };
            for (int turn = 0; turn < 4; turn++)
            {
                int mask = puzzle.MaskAt(x, y);
                if ((mask & incoming) != 0)
                {
                    if (x == puzzle.Size - 1 && y == puzzle.Size - 1 && (mask & 2) != 0) return true;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dx[d], ny = y + dy[d];
                        if (ports[d] == incoming || (mask & ports[d]) == 0 || nx < 0 || ny < 0 || nx >= puzzle.Size || ny >= puzzle.Size || seen[nx, ny]) continue;
                        if (SolveByRotations(puzzle, nx, ny, opposite[d], seen)) return true;
                    }
                }
                puzzle.Rotate(x, y);
            }
            seen[x, y] = false; return false;
        }

        void EnsureAlive()
        {
            Assert.That(session.Defeat, Is.False, "No respawn/cheats permitted: " + State());
            Assert.That(session.PlayerHealth.Dead, Is.False, State());
            Assert.That(session.Paused, Is.False, State());
        }
        string State()
        {
            if (session == null || session.Player == null) return objective ?? "setup";
            return objective + " pos=" + session.Player.transform.position.ToString("F2") + " hp=" + session.PlayerHealth.Current +
                " fragments=" + session.Progress.FragmentCount + " checkpoint=" + session.Progress.checkpointId +
                " bossHp=" + (session.Boss != null && session.Boss.Health != null ? session.Boss.Health.Current.ToString() : "n/a") +
                " shield=" + (session.Boss != null && session.Boss.Shielded) + " time=" + session.Progress.elapsedSeconds.ToString("F1");
        }
        void Log() { TestContext.Out.WriteLine("ROUTE: " + State()); }
        string ShotDiagnostics(SecurityAgent enemy)
        {
            var camera = session.Player.View.Lens.transform;
            var cameraRay = new Ray(camera.position, camera.forward);
            RaycastHit cameraHit, muzzleHit;
            bool cameraBlocked = Physics.Raycast(cameraRay, out cameraHit, 100, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Vector3 destination = cameraBlocked ? cameraHit.point : camera.position + camera.forward * 100;
            Vector3 muzzle = session.Player.transform.position + Vector3.up * 1.25f + session.Combat.AimForward * .32f;
            Vector3 toward = destination - muzzle;
            bool muzzleBlocked = Physics.Raycast(muzzle, toward.normalized, out muzzleHit, toward.magnitude + .05f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            return State() + " enemyHp=" + enemy.GetComponent<Health>().Current + " ammo=" + session.Combat.Ammo +
                " cameraPos=" + camera.position + " cameraForward=" + camera.forward +
                " cameraHit=" + (cameraBlocked ? cameraHit.collider.name + "@" + cameraHit.point : "none") +
                " muzzleHit=" + (muzzleBlocked ? muzzleHit.collider.name + "@" + muzzleHit.point : "none");
        }
    }
}
