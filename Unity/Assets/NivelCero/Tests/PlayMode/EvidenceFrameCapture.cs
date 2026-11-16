using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NivelCero.Tests
{
    /// <summary>
    /// Test-only capture of Unity's own rendered framebuffer, including its IMGUI HUD.
    /// Never reads a desktop, native window, screen DC or another application's pixels.
    /// PNG encoding runs on the main thread; wall/simulation timings are recorded explicitly.
    /// </summary>
    public sealed class EvidenceFrameCapture : MonoBehaviour
    {
        [Serializable] public sealed class Frame
        {
            public string file;
            public int unityFrame;
            public double simulationSeconds, wallSeconds;
        }
        [Serializable] public sealed class Manifest
        {
            public string source = "Unity ScreenCapture.CaptureScreenshotAsTexture after WaitForEndOfFrame; includes IMGUI";
            public string startedUtc;
            public int frameRate = 30, simulationFrameRate = 60, everyUnityFrames = 2;
            public int width, height, frameCount;
            public double wallDurationSeconds, simulationDurationSeconds;
            public bool complete;
            public string error;
            public List<Frame> frames = new List<Frame>();
        }

        private string directory, framesDirectory;
        private Manifest manifest;
        private Coroutine routine;
        private bool recording;
        private bool finalized;
        private double startedWall, startedSimulation;

        public static EvidenceFrameCapture Begin(GameObject camera, string evidenceDirectory)
        {
            if (camera == null || camera.GetComponent<Camera>() == null)
                throw new ArgumentException("Frame evidence requires the game's Camera object.", "camera");
            if (String.IsNullOrWhiteSpace(evidenceDirectory))
                throw new ArgumentException("An evidence directory is required.", "evidenceDirectory");
            var capture = camera.AddComponent<EvidenceFrameCapture>();
            capture.directory = Path.GetFullPath(evidenceDirectory);
            capture.framesDirectory = Path.Combine(capture.directory, "frames");
            Directory.CreateDirectory(capture.framesDirectory);
            // Refuse old frames rather than mixing unrelated recordings or deleting evidence.
            if (Directory.GetFiles(capture.framesDirectory, "frame-*.png").Length != 0)
            {
                Destroy(capture);
                throw new IOException("Use a fresh evidence directory: numbered Unity PNG frames already exist.");
            }
            capture.manifest = new Manifest { startedUtc = DateTime.UtcNow.ToString("o") };
            capture.startedWall = Time.realtimeSinceStartupAsDouble;
            capture.startedSimulation = Time.unscaledTimeAsDouble;
            capture.recording = true;
            capture.routine = capture.StartCoroutine(capture.CaptureFrames());
            return capture;
        }

        private IEnumerator CaptureFrames()
        {
            int nextFrame = Time.frameCount;
            var endOfFrame = new WaitForEndOfFrame();
            while (recording)
            {
                yield return endOfFrame;
                if (!recording || Time.frameCount < nextFrame) continue;
                nextFrame = Time.frameCount + 2;
                Texture2D texture = null;
                double capturedWall = Time.realtimeSinceStartupAsDouble - startedWall;
                try
                {
                    texture = ScreenCapture.CaptureScreenshotAsTexture(1);
                    if (texture == null) throw new IOException("Unity did not return its framebuffer texture.");
                    if (manifest.frameCount == 0) { manifest.width = texture.width; manifest.height = texture.height; }
                    if (texture.width != manifest.width || texture.height != manifest.height)
                        throw new IOException("The game framebuffer changed resolution during recording.");
                    string name = "frame-" + manifest.frameCount.ToString("D6") + ".png";
                    // PNG encoding runs on worker threads so the main thread (and the game clock)
                    // is not stalled; the raw Unity framebuffer bytes are copied first.
                    byte[] raw = texture.GetRawTextureData<byte>().ToArray();
                    var format = texture.graphicsFormat; uint w = (uint)texture.width, h = (uint)texture.height;
                    string target = Path.Combine(framesDirectory, name);
                    while (pending.Count > 48) { pending[0].Wait(); pending.RemoveAt(0); }
                    pending.Add(System.Threading.Tasks.Task.Run(() =>
                    {
                        byte[] png = ImageConversion.EncodeArrayToPNG(raw, format, w, h);
                        if (png == null || png.Length < 8) throw new IOException("Unity PNG encoding failed.");
                        File.WriteAllBytes(target, png);
                    }));
                    pending.RemoveAll(t => t.IsCompleted && !t.IsFaulted);
                    manifest.frames.Add(new Frame { file = name, unityFrame = Time.frameCount,
                        simulationSeconds = Time.unscaledTimeAsDouble - startedSimulation,
                        wallSeconds = capturedWall });
                    manifest.frameCount++;
                }
                catch (Exception exception)
                {
                    manifest.error = exception.ToString(); recording = false;
                    Debug.LogError("Unity framebuffer evidence capture failed: " + exception.Message);
                }
                finally { if (texture != null) Destroy(texture); }
            }
            WriteManifest();
        }

        public void StopCapture()
        {
            if (manifest == null || finalized) return;
            recording = false;
            if (routine != null) { StopCoroutine(routine); routine = null; }
            WriteManifest();
        }

        private readonly List<System.Threading.Tasks.Task> pending = new List<System.Threading.Tasks.Task>();

        private void WriteManifest()
        {
            if (manifest == null || finalized) return;
            manifest.wallDurationSeconds = Time.realtimeSinceStartupAsDouble - startedWall;
            manifest.simulationDurationSeconds = Time.unscaledTimeAsDouble - startedSimulation;
            try { System.Threading.Tasks.Task.WaitAll(pending.ToArray()); }
            catch (Exception exception) { manifest.error = exception.ToString(); }
            pending.Clear();
            manifest.complete = String.IsNullOrEmpty(manifest.error) && manifest.frameCount > 0;
            File.WriteAllText(Path.Combine(directory, "frame-manifest.json"), JsonUtility.ToJson(manifest, true));
            finalized = true;
        }
        private void OnApplicationQuit() { StopCapture(); }
        private void OnDestroy() { StopCapture(); }
    }
}
