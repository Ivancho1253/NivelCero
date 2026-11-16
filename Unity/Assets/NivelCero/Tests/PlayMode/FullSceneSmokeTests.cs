using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NivelCero.Tests
{
    public sealed class FullSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator OriginalArtAndEntire3DSceneInitializeAndRender()
        {
            yield return SceneManager.LoadSceneAsync("SectorDeArranque3D", LoadSceneMode.Single);
            yield return null; yield return null;
            var s = Session.I;
            Assert.IsNotNull(s); Assert.IsTrue(s.InMenu);
            Assert.IsNotNull(s.Player); Assert.IsNotNull(s.Combat); Assert.IsNotNull(s.Boss);
            Assert.AreEqual(3, Object.FindObjectsByType<CodeShard>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(3, Object.FindObjectsByType<Terminal>(FindObjectsSortMode.None).Length);
            var assets = GameAssets.Load();
            Assert.IsNotNull(assets); Assert.IsNotNull(assets.ZeroAnimator);
            Assert.GreaterOrEqual(assets.ZeroAnimator.animationClips.Length, 14);
            var animator = s.Player.GetComponentInChildren<Animator>(true);
            Assert.IsNotNull(animator); Assert.IsNotNull(animator.runtimeAnimatorController);
            s.StartGame(false);
            for (int i = 0; i < 15; i++) yield return null;
            Assert.IsFalse(s.PlayerHealth.Dead);
            var camera = s.Player.View.Lens;
            var rt = new RenderTexture(1280, 720, 24); rt.Create();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
            var old = RenderTexture.active; RenderTexture.active = rt;
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
            RenderTexture.active = old;
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Tests/results")); Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "sector3d-runtime.png"), pixels.EncodeToPNG());
            var colors = pixels.GetPixels(); float luminance = 0, maximum = 0;
            foreach (var color in colors) { luminance += color.grayscale; maximum = Mathf.Max(maximum, color.grayscale); }
            Assert.That(maximum, Is.GreaterThan(.1f), "Actual scene render must contain visible geometry.");
            Assert.That(luminance / colors.Length, Is.GreaterThan(.005f));
            Object.Destroy(pixels); rt.Release(); Object.Destroy(rt);
            s.Paused = true;
        }
    }
}
