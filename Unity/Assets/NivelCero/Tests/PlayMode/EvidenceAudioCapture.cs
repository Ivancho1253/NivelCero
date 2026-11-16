using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace NivelCero.Tests
{
    /// <summary>
    /// Test-only PCM recording of the game's AudioListener mix. No microphone or desktop audio.
    /// File access and finalization are serialized against Unity's audio callback thread.
    /// </summary>
    public sealed class EvidenceAudioCapture : MonoBehaviour
    {
        private readonly object gate = new object();
        private FileStream stream;
        private byte[] pcm;
        private long dataBytes;
        private int sampleRate, channelCount;
        private bool recording;
        private string failure;

        public static EvidenceAudioCapture Begin(GameObject camera, string path)
        {
            if (camera == null || camera.GetComponent<AudioListener>() == null)
                throw new ArgumentException("Audio capture requires the camera with the game's AudioListener.", "camera");
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("A WAV path is required.", "path");
            var capture = camera.AddComponent<EvidenceAudioCapture>();
            capture.sampleRate = AudioSettings.outputSampleRate;
            if (capture.sampleRate <= 0) { Destroy(capture); throw new InvalidOperationException("Unity audio output has no sample rate."); }
            string absolute = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            capture.stream = new FileStream(absolute, FileMode.Create, FileAccess.Write, FileShare.Read, 65536);
            capture.stream.Write(new byte[44], 0, 44);
            lock (capture.gate) capture.recording = true;
            return capture;
        }

        // Unity invokes this on its audio thread. Never call a Unity object API here.
        private void OnAudioFilterRead(float[] data, int channels)
        {
            lock (gate)
            {
                if (!recording || stream == null || channels < 1 || data == null) return;
                if (channelCount == 0) channelCount = channels;
                if (channelCount != channels)
                {
                    failure = "Unity changed channel layout while recording; the WAV was closed at that point.";
                    FinalizeLocked();
                    return;
                }
                int length = data.Length * 2;
                if (dataBytes + length > UInt32.MaxValue - 36L)
                {
                    failure = "The WAV reached its RIFF size limit.";
                    FinalizeLocked();
                    return;
                }
                if (pcm == null || pcm.Length < length) pcm = new byte[length];
                for (int i = 0; i < data.Length; i++)
                {
                    float value = data[i];
                    if (Single.IsNaN(value) || Single.IsInfinity(value)) value = 0;
                    int sample = value >= 1 ? 32767 : value <= -1 ? -32768 : (int)(value * 32767);
                    pcm[i * 2] = (byte)(sample & 255);
                    pcm[i * 2 + 1] = (byte)((sample >> 8) & 255);
                }
                try { stream.Write(pcm, 0, length); dataBytes += length; }
                catch (IOException exception)
                {
                    failure = "Game audio recording failed: " + exception.Message;
                    FinalizeLocked();
                }
            }
        }

        private void Update()
        {
            string message = null;
            lock (gate) { if (failure != null) { message = failure; failure = null; } }
            if (message != null) Debug.LogError(message);
        }

        public void StopCapture()
        {
            lock (gate) FinalizeLocked();
        }

        private void FinalizeLocked()
        {
            recording = false;
            if (stream == null) return;
            try
            {
                stream.Position = 0;
                int channels = channelCount > 0 ? channelCount : 2;
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write((uint)(36 + dataBytes));
                    writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write((uint)16);
                    writer.Write((ushort)1); writer.Write((ushort)channels); writer.Write((uint)sampleRate);
                    writer.Write((uint)(sampleRate * channels * 2)); writer.Write((ushort)(channels * 2));
                    writer.Write((ushort)16); writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write((uint)dataBytes);
                    writer.Flush();
                }
                stream.Flush();
            }
            catch (IOException exception) { failure = "Could not finalize game WAV: " + exception.Message; }
            finally { stream.Dispose(); stream = null; }
        }

        private void OnApplicationQuit() { StopCapture(); }
        private void OnDestroy() { StopCapture(); }
    }
}
