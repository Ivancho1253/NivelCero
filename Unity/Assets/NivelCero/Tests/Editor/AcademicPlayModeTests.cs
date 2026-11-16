using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using NivelCero.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace NivelCero.Tests
{
    // EditMode-hosted integration tests enter real Play Mode. Reflection avoids
    // an asmdef dependency on Assembly-CSharp; no product QA/cheat API is added.
    public sealed class AcademicPlayModeTests
    {
        Component scene;
        Type sceneType;
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            sceneType = Type.GetType("NivelCero.Academic2D, NivelCero.Runtime");
            Assert.IsNotNull(sceneType, "Academic2D must compile into NivelCero.Runtime.");
            scene = new GameObject("Academic integration fixture").AddComponent(sceneType);
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NivelCero/Art/Academic/tiles.png");
            Assert.IsNotNull(atlas);
            sceneType.GetField("Tilesheet").SetValue(scene, atlas);
            yield return null; // Runtime Start builds the actual Tilemap and entities.
            Assert.IsNotNull(Get("player"));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (scene != null) UnityEngine.Object.Destroy(scene.gameObject);
            Time.timeScale = 1;
            yield return null;
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator PlayerLandsOnTilemapAndPhysicalFirewallStopsMovement()
        {
            Set("started", true); Invoke("SetClock");
            Rigidbody2D player = (Rigidbody2D)Get("player");
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            Assert.That(player.position.y, Is.InRange(1.65f, 2.1f), "Rigidbody2D must stand on the Tilemap collider.");
            player.position = new Vector2(66.5f, 2);
            player.linearVelocity = Vector2.right * 20;
            for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            Assert.Less(player.position.x, 67.5f, "Closed firewall geometry must stop the body.");
        }

        [UnityTest]
        public IEnumerator CircuitMustConnectBeforeTerminalDisablesFirewall()
        {
            CircuitPuzzle circuit = new CircuitPuzzle(413, 3);
            Set("started", true); Set("hacking", true); Set("circuit", circuit); Set("hackTime", 60f);
            Invoke("SetClock");
            SpriteRenderer firewall = (SpriteRenderer)Get("firewall");
            Assert.IsFalse((bool)Invoke("CommitHack"));
            Assert.IsTrue(firewall.gameObject.activeSelf);
            Assert.IsFalse((bool)sceneType.GetProperty("GateUnlocked").GetValue(scene));
            Assert.AreEqual(55f, (float)Get("hackTime"));
            Assert.IsTrue(Solve(circuit, 0, 0, CircuitPuzzle.West, new bool[3, 3]), "Generated circuit must be solvable through actual module rotations.");
            Assert.IsTrue(circuit.IsSolved);
            Assert.IsTrue((bool)Invoke("CommitHack"));
            Assert.IsFalse(firewall.gameObject.activeSelf);
            Assert.IsTrue((bool)sceneType.GetProperty("GateUnlocked").GetValue(scene));
            Assert.IsFalse((bool)Get("hacking"));
            Assert.AreEqual(1f, Time.timeScale);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FailedHackClosesInterfaceAndKeepsDefensesActive()
        {
            Set("started", true); Set("hacking", true); Set("hackTime", 0f);
            Invoke("SetClock"); Invoke("FailHack");
            Assert.IsFalse((bool)Get("hacking"));
            Assert.IsFalse((bool)sceneType.GetProperty("GateUnlocked").GetValue(scene));
            Assert.IsTrue(((SpriteRenderer)Get("firewall")).gameObject.activeSelf);
            Assert.AreEqual(90f, (float)sceneType.GetProperty("CurrentHealth").GetValue(scene));
            Assert.Greater((float)Get("terminalCooldown"), 0);
            Assert.AreEqual(1f, Time.timeScale);
            yield return null;
        }

        object Get(string name) { return sceneType.GetField(name, Private).GetValue(scene); }
        void Set(string name, object value) { sceneType.GetField(name, Private).SetValue(scene, value); }
        object Invoke(string name) { return sceneType.GetMethod(name, Private).Invoke(scene, null); }

        static bool Solve(CircuitPuzzle puzzle, int x, int y, int incoming, bool[,] visited)
        {
            visited[x, y] = true;
            int[] ports = { CircuitPuzzle.North, CircuitPuzzle.East, CircuitPuzzle.South, CircuitPuzzle.West };
            int[] reciprocal = { CircuitPuzzle.South, CircuitPuzzle.West, CircuitPuzzle.North, CircuitPuzzle.East };
            int[] dx = { 0, 1, 0, -1 }, dy = { -1, 0, 1, 0 };
            for (int turn = 0; turn < 4; turn++)
            {
                int mask = puzzle.MaskAt(x, y);
                if ((mask & incoming) != 0)
                {
                    if (x == puzzle.Size - 1 && y == puzzle.Size - 1 && (mask & CircuitPuzzle.East) != 0) return true;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dx[d], ny = y + dy[d];
                        if ((mask & ports[d]) == 0 || nx < 0 || ny < 0 || nx >= puzzle.Size || ny >= puzzle.Size || visited[nx, ny]) continue;
                        if (Solve(puzzle, nx, ny, reciprocal[d], visited)) return true;
                    }
                }
                puzzle.Rotate(x, y);
            }
            visited[x, y] = false;
            return false;
        }
    }
}
