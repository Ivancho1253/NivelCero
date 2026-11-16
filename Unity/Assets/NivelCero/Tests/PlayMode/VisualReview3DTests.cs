using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NivelCero.Tests
{
    /// <summary>
    /// Art-review renders of the 3D sector from fixed viewpoints (Tests/results/review3d).
    /// Zero is placed with Respawn and the camera angle is set directly: these images
    /// are for visual inspection, not evidence of a played route.
    /// </summary>
    public sealed class VisualReview3DTests
    {
        static readonly FieldInfo Yaw = typeof(PlayerCamera).GetField("yaw", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo Pitch = typeof(PlayerCamera).GetField("pitch", BindingFlags.Instance | BindingFlags.NonPublic);

        [UnityTest, Timeout(300000)]
        public IEnumerator RenderReviewViewpoints()
        {
            yield return SceneManager.LoadSceneAsync("SectorDeArranque3D", LoadSceneMode.Single);
            yield return null; yield return null;
            var s = Session.I;
            s.StartGame(false);
            for (int i = 0; i < 10; i++) yield return null;
            s.PlayerHealth.Invulnerable = true; // review only: keep Zero standing while framing
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Tests/results/review3d"));
            Directory.CreateDirectory(folder);
            var shots = new (string name, Vector3 pos, float yaw, float pitch, bool fps)[] {
                ("01-inicio-tps", new Vector3(0, 1, 2), 0, 8, false),
                ("02-entrenamiento", new Vector3(0, 1, 14), 0, 10, false),
                ("03-seguridad-dron", new Vector3(0, 1, 36), 0, 6, false),
                ("04-terminal-firewall", new Vector3(1, 1, 47), 20, 8, false),
                ("05-exploracion", new Vector3(0, 1, 62), 15, 6, false),
                ("06-ruta-lateral", new Vector3(-9, 1, 22), 0, 8, false),
                ("07-checkpoint-arena", new Vector3(0, 1, 84), 0, 6, false),
                ("08-arena-guardian", new Vector3(0, 1, 97), 0, 2, false),
                ("09-arena-fps", new Vector3(0, 1, 97), 0, 4, true),
                ("10-portal", new Vector3(0, 1, 127), 0, 6, false),
                ("11-zero-retrato", new Vector3(0, 1, 14), 160, 12, false),
                ("12-fps-terminal", new Vector3(2.5f, 1, 47), 25, 10, true),
            };
            foreach (var shot in shots)
            {
                s.Player.Respawn(shot.pos);
                s.Player.View.FirstPerson = shot.fps;
                Yaw.SetValue(s.Player.View, shot.yaw); Pitch.SetValue(s.Player.View, shot.pitch);
                if (shot.name.StartsWith("08") || shot.name.StartsWith("09")) s.Boss.Active = true;
                for (int i = 0; i < 20; i++) yield return null;
                Capture(s.Player.View.Lens, Path.Combine(folder, shot.name + ".png"));
                if (shot.fps)
                    foreach (Transform child in s.Player.View.Lens.transform)
                    {
                        var r = child.GetComponentInChildren<Renderer>(true);
                        TestContext.Out.WriteLine("FPS child " + child.name + " active=" + child.gameObject.activeInHierarchy + " local=" + child.localPosition + " rot=" + child.localEulerAngles +
                            " bounds=" + (r != null ? r.bounds.size.ToString("F2") : "none") + " layer=" + child.gameObject.layer + " cull=" + s.Player.View.Lens.cullingMask);
                    }
            }
            s.Paused = true;
        }

        static void Capture(Camera camera, string path)
        {
            var rt = new RenderTexture(1600, 900, 24); rt.Create();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
            var old = RenderTexture.active; RenderTexture.active = rt;
            var pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); pixels.Apply();
            RenderTexture.active = old;
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            Object.Destroy(pixels); rt.Release(); Object.Destroy(rt);
        }
    }
}
