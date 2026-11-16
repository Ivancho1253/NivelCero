using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using NivelCero.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NivelCero.Tests
{
    // These tests execute real Unity physics and the production MonoBehaviours.
    // Input devices and scenes are isolated; no user's progress file is touched.
    public sealed class RuntimeIntegrationTests
    {
        Session session;
        PlayerMotor motor;
        PlayerCamera view;
        Keyboard keyboard;
        Mouse mouse;
        Scene testScene, previousScene;
        string savePath;
        InputSettings.BackgroundBehavior previousBackgroundBehavior;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;
#endif
        InputSettings.UpdateMode previousInputUpdateMode;
        bool previousRunInBackground;
        float previousCaptureDeltaTime;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousScene = SceneManager.GetActiveScene();
            testScene = SceneManager.CreateScene("NivelCero runtime integration");
            SceneManager.SetActiveScene(testScene);
            Time.timeScale = 1;
            previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            // A batch-mode editor has no focused Game View. Route queued synthetic
            // device events to the player loop rather than the editor state buffer.
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
            previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
#endif
            previousInputUpdateMode = InputSystem.settings.updateMode;
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(keyboard); InputSystem.EnableDevice(mouse);
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            session = new GameObject("Isolated test session").AddComponent<Session>();
            session.enabled = false; // Prevent the production level builder and file saves.
            savePath = Path.Combine(Path.GetTempPath(), "nivel-cero-test-" + System.Guid.NewGuid().ToString("N") + ".json");
            session.SavePathOverride = savePath;
            session.InMenu = false;
            session.Paused = session.Defeat = session.Victory = false;
            var player = new GameObject("Test Zero");
            var character = player.AddComponent<CharacterController>();
            character.height = 1.8f; character.radius = .32f; character.center = Vector3.up * .9f;
            var health = player.AddComponent<Health>();
            motor = player.AddComponent<PlayerMotor>();
            var cameraObject = new GameObject("Test camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            cameraObject.AddComponent<AudioListener>();
            view = cameraObject.AddComponent<PlayerCamera>();
            view.Lens = camera; view.Target = player.transform; view.enabled = false;
            motor.View = view;
            session.Player = motor; session.PlayerHealth = health;
            CreateBox("Ground", new Vector3(0, -.5f, 0), new Vector3(100, 1, 100));
            motor.Respawn(new Vector3(0, .05f, 0));
            Physics.SyncTransforms();
            yield return null;
            Assert.That(Keyboard.current, Is.SameAs(keyboard), "synthetic keyboard must drive production input");
            Assert.That(keyboard.enabled, Is.True, "batch-mode focus must not disable synthetic input");
            Assert.That(Session.I, Is.SameAs(session));
            Assert.That(session.Blocked, Is.False);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
#endif
            InputSystem.settings.updateMode = previousInputUpdateMode;
            Application.runInBackground = previousRunInBackground;
            Time.captureDeltaTime = previousCaptureDeltaTime;
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            if (testScene.IsValid() && testScene.isLoaded) yield return SceneManager.UnloadSceneAsync(testScene);
            foreach (string suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(savePath + suffix)) File.Delete(savePath + suffix);
        }

        [UnityTest]
        public IEnumerator WasdMovesInTheCameraPlaneInBothPerspectives()
        {
            Key[] keys = { Key.W, Key.S, Key.A, Key.D };
            Vector3[] expected = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            foreach (bool firstPerson in new[] { false, true })
            {
                view.FirstPerson = firstPerson;
                for (int i = 0; i < keys.Length; i++)
                {
                    SetKeys(); motor.Respawn(new Vector3(0, .05f, 0));
                    view.transform.rotation = Quaternion.Euler(0, 90, 0);
                    yield return null;
                    Vector3 before = motor.transform.position;
                    TestContext.Out.WriteLine(Diagnostics("WASD before " + keys[i]));
                    SetKeys(keys[i]);
                    yield return new WaitForSeconds(.3f);
                    Assert.That(keyboard[keys[i]].isPressed, Is.True, "queued key must reach dynamic input state");
                    Vector3 displacement = motor.transform.position - before; displacement.y = 0;
                    Vector3 worldExpected = Quaternion.Euler(0, 90, 0) * expected[i];
                    string state = Diagnostics("WASD after " + keys[i]);
                    TestContext.Out.WriteLine(state);
                    Assert.That(displacement.magnitude, Is.GreaterThan(.65f), state);
                    Assert.That(Vector3.Dot(displacement.normalized, worldExpected), Is.GreaterThan(.98f), keys[i] + " FPS=" + firstPerson);
                }
            }
            SetKeys();
        }

        [UnityTest]
        public IEnumerator DiagonalSpeedIsNormalizedAndWallsBlockMovement()
        {
            SetKeys(Key.W);
            yield return new WaitForSeconds(.35f);
            Assert.That(keyboard.wKey.isPressed, Is.True, "queued W must reach dynamic input state");
            float straightSpeed = Vector3.ProjectOnPlane(motor.Velocity, Vector3.up).magnitude;
            string straightState = Diagnostics("straight speed");
            TestContext.Out.WriteLine(straightState);
            Assert.That(straightSpeed, Is.GreaterThan(5), straightState);
            SetKeys(); motor.Respawn(new Vector3(0, .05f, 0));
            yield return null;
            SetKeys(Key.W, Key.D);
            yield return new WaitForSeconds(.35f);
            float diagonalSpeed = Vector3.ProjectOnPlane(motor.Velocity, Vector3.up).magnitude;
            Assert.That(diagonalSpeed, Is.EqualTo(straightSpeed).Within(.2f));
            SetKeys(); motor.Respawn(new Vector3(0, .05f, 0));
            CreateBox("Solid wall", new Vector3(0, 1.5f, 2), new Vector3(8, 3, .3f));
            Physics.SyncTransforms();
            yield return null;
            SetKeys(Key.W);
            yield return new WaitForSeconds(.65f);
            Assert.That(motor.transform.position.z, Is.LessThan(1.7f));
            Assert.That(motor.transform.position.z, Is.GreaterThan(.8f));
            SetKeys();
        }

        [UnityTest]
        public IEnumerator JumpLeavesGroundAndAirDashRequiresLandingToRecharge()
        {
            yield return new WaitForSeconds(.1f);
            float beforeJump = motor.transform.position.y;
            TestContext.Out.WriteLine(Diagnostics("jump before"));
            SetKeys(Key.Space);
            yield return new WaitForSeconds(.08f);
            string jumpState = Diagnostics("jump after");
            TestContext.Out.WriteLine(jumpState);
            Assert.That(motor.transform.position.y, Is.GreaterThan(beforeJump + .2f), jumpState);
            SetKeys(); motor.Respawn(new Vector3(0, 100, 0));
            yield return null;
            SetKeys(Key.LeftShift);
            yield return new WaitForSeconds(.1f);
            Assert.That(motor.Velocity.z, Is.GreaterThan(15));
            SetKeys();
            yield return new WaitForSeconds(.9f);
            Assert.That(motor.DashReady, Is.False, "air charge stays consumed after cooldown");
            SetKeys(Key.LeftShift);
            yield return new WaitForSeconds(.1f);
            Assert.That(motor.Velocity.z, Is.LessThan(8));
            SetKeys(); motor.Respawn(new Vector3(0, .05f, 0));
            yield return new WaitForSeconds(.1f);
            Assert.That(motor.DashReady, Is.True);
        }

        [UnityTest]
        public IEnumerator CameraAvoidsWallAndPerspectiveSwitchDoesNotMovePlayer()
        {
            motor.enabled = false;
            view.enabled = true;
            CreateBox("Camera obstruction", new Vector3(0, 1.6f, -1.5f), new Vector3(5, 3, .3f));
            Physics.SyncTransforms();
            Vector3 playerPosition = motor.transform.position;
            view.FirstPerson = false;
            yield return null;
            Assert.That(view.transform.position.z, Is.GreaterThan(-1.2f));
            view.FirstPerson = true;
            yield return null;
            Assert.That(view.transform.position.x, Is.EqualTo(playerPosition.x).Within(.01f));
            Assert.That(view.transform.position.z, Is.EqualTo(playerPosition.z).Within(.01f));
            view.FirstPerson = false;
            yield return null;
            Assert.That(motor.transform.position, Is.EqualTo(playerPosition));
        }

        [UnityTest]
        public IEnumerator GunConsumesAmmoDamagesEnemyAndCannotShootThroughCover()
        {
            motor.enabled = false;
            view.transform.SetPositionAndRotation(new Vector3(0, 1.6f, 0), Quaternion.identity);
            var combat = motor.gameObject.AddComponent<PlayerCombat>(); combat.View = view; session.Combat = combat;
            var target = CreateBox("Target", new Vector3(0, 1.6f, 8), new Vector3(1, 2, 1)).AddComponent<Health>();
            Unprotect(target);
            yield return null; // Production weapon Start creates materials/visuals.
            Physics.SyncTransforms();
            int initialAmmo = combat.Ammo;
            Invoke(combat, "Shoot");
            Assert.That(combat.Ammo, Is.EqualTo(initialAmmo - 1));
            Assert.That(target.Current, Is.EqualTo(80).Within(.01f));
            CreateBox("Ballistic cover", new Vector3(0, 1.5f, 4), new Vector3(3, 3, .5f));
            Physics.SyncTransforms();
            Invoke(combat, "Shoot");
            Assert.That(target.Current, Is.EqualTo(80).Within(.01f));
            Assert.That(combat.Ammo, Is.EqualTo(initialAmmo - 2));
            SetKeys(Key.R);
            yield return new WaitForSeconds(1.4f);
            Assert.That(combat.Reloading, Is.False);
            Assert.That(combat.Ammo, Is.EqualTo(combat.Magazine));
            SetKeys();
        }

        [UnityTest]
        public IEnumerator BladeHasDamageRangeAndObstructionChecks()
        {
            motor.enabled = false;
            view.transform.SetPositionAndRotation(new Vector3(0, 1.6f, 0), Quaternion.identity);
            var combat = motor.gameObject.AddComponent<PlayerCombat>(); combat.View = view;
            var near = CreateBox("Near target", new Vector3(0, 1, 1.7f), new Vector3(.6f, 1.5f, .6f)).AddComponent<Health>();
            var far = CreateBox("Far target", new Vector3(0, 1, 5), Vector3.one).AddComponent<Health>();
            Unprotect(near); Unprotect(far);
            yield return null;
            Physics.SyncTransforms(); Invoke(combat, "Swing");
            Assert.That(near.Current, Is.EqualTo(72).Within(.01f));
            Assert.That(far.Current, Is.EqualTo(100).Within(.01f));
            CreateBox("Melee cover", new Vector3(0, 1, .8f), new Vector3(3, 2, .2f));
            Physics.SyncTransforms(); Invoke(combat, "Swing");
            Assert.That(near.Current, Is.EqualTo(72).Within(.01f));
        }

        [UnityTest]
        public IEnumerator HackRequiresValidCircuitAndFailureLocksTerminal()
        {
            motor.enabled = false;
            var terminal = new GameObject("Test terminal").AddComponent<Terminal>();
            terminal.Seed = 123; terminal.Size = 2;
            session.BeginHack(terminal);
            Assert.That(session.Puzzle, Is.Not.Null);
            Assert.That(session.Blocked, Is.True);
            Assert.That(session.Puzzle.IsSolved, Is.False);
            session.ValidateHack();
            Assert.That(session.Puzzle, Is.Not.Null, "incorrect attempt cannot unlock target");
            Assert.That(session.Progress.accessUnlocked, Is.False);
            session.CancelHack(true);
            Assert.That(session.Puzzle, Is.Null);
            Assert.That(terminal.LockedUntil, Is.GreaterThan(Time.time));
            session.BeginHack(terminal);
            Assert.That(session.Puzzle, Is.Null, "cooldown rejects immediate restart");
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossShieldOnlyDropsAfterAValidatedCircuit()
        {
            motor.enabled = false;
            var bossObject = new GameObject("Test Guardian");
            bossObject.transform.position = Vector3.forward * 100;
            bossObject.AddComponent<Health>();
            var boss = bossObject.AddComponent<Guardian>(); boss.enabled = false;
            session.Boss = boss;
            Invoke(boss, "Start");
            var terminal = new GameObject("Shield terminal").AddComponent<Terminal>();
            terminal.BossTerminal = true; terminal.Size = 2;
            session.Progress.Collect(ProgressData.MainFragmentOne);
            session.Progress.Collect(ProgressData.MainFragmentTwo);
            session.BeginHack(terminal);
            session.ValidateHack();
            Assert.That(boss.Shielded, Is.True);
            Assert.That(boss.Health.Invulnerable, Is.True);
            SetKnownSolvedRoute(session.Puzzle);
            session.ValidateHack();
            Assert.That(session.Puzzle, Is.Null);
            Assert.That(boss.Shielded, Is.False);
            Assert.That(boss.Health.Invulnerable, Is.False);
            Assert.That(boss.VulnerableRemaining, Is.GreaterThan(0));
            Unprotect(boss.Health); boss.Health.Damage(20, motor.transform.position);
            Assert.That(boss.Health.Current, Is.EqualTo(340).Within(.01f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SaveAndLoadRestoreCheckpointFragmentsAndDoorState()
        {
            motor.enabled = false;
            session.AccessDoor = new GameObject("Saved access door");
            session.Boss = new GameObject("Saved boss").AddComponent<Guardian>();
            session.Boss.enabled = false;
            session.Progress.Collect(ProgressData.MainFragmentOne);
            session.Progress.checkpointId = 2;
            session.Progress.accessUnlocked = true;
            session.Progress.elapsedSeconds = 35.5f;
            session.Save();
            Assert.That(File.Exists(savePath), Is.True);
            session.Progress = new ProgressData();
            session.StartGame(true);
            Assert.That(session.Progress.HasFragment(ProgressData.MainFragmentOne), Is.True);
            Assert.That(session.Progress.HasFragment(ProgressData.MainFragmentTwo), Is.False);
            Assert.That(session.Progress.checkpointId, Is.EqualTo(2));
            Assert.That(session.Progress.elapsedSeconds, Is.EqualTo(35.5f));
            Assert.That(session.AccessDoor.activeSelf, Is.False);
            Assert.That(motor.transform.position, Is.EqualTo(session.SpawnPoints[2]));
            session.Progress.elapsedSeconds = 40;
            session.Save();
            Assert.That(File.Exists(savePath + ".bak"), Is.True, "second save retains the previous version");
            yield return null;
        }

        [UnityTest]
        public IEnumerator CorruptSaveFallsBackToFreshProgressWithoutApplyingInvalidCheckpoint()
        {
            motor.enabled = false;
            session.AccessDoor = new GameObject("Default access door");
            session.Boss = new GameObject("Default boss").AddComponent<Guardian>();
            session.Boss.enabled = false;
            File.WriteAllText(savePath, "{\"schemaVersion\":1,\"checkpointId\":999,\"elapsedSeconds\":0}");
            session.StartGame(true);
            Assert.That(session.Progress.IsValid(), Is.True);
            Assert.That(session.Progress.checkpointId, Is.EqualTo(0));
            Assert.That(session.AccessDoor.activeSelf, Is.True);
            Assert.That(session.Message, Does.Contain("No se pudo cargar"));
            Assert.That(motor.transform.position, Is.EqualTo(session.SpawnPoints[0]));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LethalDamageEntersDefeatAndRespawnUsesSavedCheckpoint()
        {
            motor.enabled = false;
            session.Boss = new GameObject("Respawn boss").AddComponent<Guardian>();
            session.Boss.enabled = false;
            session.Progress.checkpointId = 1;
            Unprotect(session.PlayerHealth);
            session.PlayerHealth.Damage(1000, Vector3.forward);
            Assert.That(session.PlayerHealth.Dead, Is.True);
            Assert.That(session.Defeat, Is.True);
            Assert.That(session.Blocked, Is.True);
            session.Respawn();
            Assert.That(session.PlayerHealth.Current, Is.EqualTo(session.PlayerHealth.Maximum));
            Assert.That(session.Defeat, Is.False);
            Assert.That(session.Blocked, Is.False);
            Assert.That(motor.transform.position, Is.EqualTo(session.SpawnPoints[1]));
            yield return null;
        }

        static void SetKnownSolvedRoute(CircuitPuzzle puzzle)
        {
            var cells = (int[,])typeof(CircuitPuzzle).GetField("cells", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(puzzle);
            cells[0, 0] = 10; cells[1, 0] = 12; cells[0, 1] = 1; cells[1, 1] = 3;
        }
        static void Unprotect(Health health)
        {
            typeof(Health).GetField("protectedUntil", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(health, -1f);
        }
        static void Invoke(object target, string method)
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }
        void SetKeys(params Key[] keys)
        {
            keyboard.MakeCurrent();
            Assert.That(keyboard.enabled, Is.True);
            Assert.That(Session.I, Is.SameAs(session));
            Assert.That(session.Blocked, Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        }
        string Diagnostics(string label)
        {
            var body = motor.GetComponent<CharacterController>();
            var health = motor.GetComponent<Health>();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var state = new StringBuilder(label);
            state.AppendFormat(" | sessionMatch={0} blocked={1} dead={2} motorEnabled={3} bodyEnabled={4} grounded={5}",
                Session.I == session, session.Blocked, health.Dead, motor.enabled, body.enabled, body.isGrounded);
            state.AppendFormat(" | height={0} center={1} minMove={2} position={3} velocity={4} planar={5} vertical={6}",
                body.height, body.center, body.minMoveDistance, motor.transform.position.ToString("F5"), motor.Velocity.ToString("F5"),
                typeof(PlayerMotor).GetField("planar", flags).GetValue(motor), typeof(PlayerMotor).GetField("vertical", flags).GetValue(motor));
            state.AppendFormat(" | keyboardCurrent={0} enabled={1} W={2} A={3} S={4} D={5} Space={6}",
                Keyboard.current == keyboard, keyboard.enabled, keyboard.wKey.isPressed, keyboard.aKey.isPressed,
                keyboard.sKey.isPressed, keyboard.dKey.isPressed, keyboard.spaceKey.isPressed);
            state.AppendFormat(" | dt={0:F8} fixedDt={1} captureDt={2} scale={3} focus={4}",
                Time.deltaTime, Time.fixedDeltaTime, Time.captureDeltaTime, Time.timeScale, Application.isFocused);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                state.Append(" | scene=").Append(scene.name).Append(" roots=[");
                if (scene.isLoaded)
                    foreach (var root in scene.GetRootGameObjects()) state.Append(root.name).Append(";");
                state.Append("]");
            }
            state.Append(" | nearbyColliders=[");
            foreach (var collider in Physics.OverlapSphere(motor.transform.position + Vector3.up, 3, ~0, QueryTriggerInteraction.Collide))
                state.Append(collider.name).Append("@").Append(collider.transform.position).Append(" scene=")
                    .Append(collider.gameObject.scene.name).Append(" layer=").Append(collider.gameObject.layer).Append(";");
            state.Append("]");
            return state.ToString();
        }
        static GameObject CreateBox(string name, Vector3 position, Vector3 size)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name;
            box.transform.position = position; box.transform.localScale = size; return box;
        }
    }
}
