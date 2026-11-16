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
    /// <summary>Art-review render of the 2D scene plus a check that Zero's feet touch the floor.</summary>
    public sealed class VisualReview2DTests
    {
        [UnityTest, Timeout(120000)]
        public IEnumerator ZeroStandsOnTheTilemapAndRenders()
        {
            yield return SceneManager.LoadSceneAsync("AE4_2D_SectorDeArranque", LoadSceneMode.Single);
            yield return null;
            var world = Object.FindFirstObjectByType<Academic2D>();
            world.PauseWhenUnfocused = false;
            world.StartGame();
            yield return new WaitForSeconds(1.5f); // game time: let the capsule settle on the floor
            var sprite = (SpriteRenderer)typeof(Academic2D).GetField("playerSprite", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(world);
            float transformY = sprite.transform.position.y, boundsBottom = sprite.bounds.min.y;
            TestContext.Out.WriteLine("body=" + world.PlayerPosition + " transformY=" + transformY + " spriteBottom=" + boundsBottom +
                " spriteSize=" + sprite.sprite.rect + " ppu=" + sprite.sprite.pixelsPerUnit + " pivot=" + sprite.sprite.pivot + " tex=" + sprite.sprite.texture.width + "x" + sprite.sprite.texture.height);
            Assert.That(world.PlayerPosition.y, Is.EqualTo(1.75f).Within(.05f), "Capsule must rest on the floor (top y = 1).");
            Assert.That(Mathf.Abs(transformY - world.PlayerPosition.y), Is.LessThan(.05f), "Sprite must follow the body.");
            // Feet are drawn 4 px (0.125 u) above the frame bottom: frame bottom ≈ 0.875 u under the centre.
            Assert.That(boundsBottom, Is.EqualTo(world.PlayerPosition.y - .875f).Within(.06f), "Sprite frame must be centred on the capsule.");
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Tests/results/review2d"));
            Directory.CreateDirectory(folder);
            var rt = new RenderTexture(1920, 1080, 24); rt.Create();
            RenderPipeline.SubmitRenderRequest(world.GameCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
            var old = RenderTexture.active; RenderTexture.active = rt;
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply(); RenderTexture.active = old;
            File.WriteAllBytes(Path.Combine(folder, "inicio.png"), pixels.EncodeToPNG());
            Object.Destroy(pixels); rt.Release(); Object.Destroy(rt);
        }
    }
}
