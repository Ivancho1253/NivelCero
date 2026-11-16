using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace NivelCero.Tests
{
    /// <summary>Test-player frame cadence sampling. Never uses simulation delta time.</summary>
    public sealed class PerformanceProbe : MonoBehaviour
    {
        readonly List<double> intervals = new List<double>();
        Stopwatch clock;
        string outputPath;
        double previousMilliseconds;
        int observedFrames, warmupFrames;
        bool stopped;

        [Serializable]
        sealed class Report
        {
            public string source = "System.Diagnostics.Stopwatch elapsed between MonoBehaviour.Update calls";
            public string limitation = "Frame intervals include the frame limiter; this is not a GPU profiler. Simulation captureDeltaTime is fixed. Results describe only this machine, scene, resolution and route.";
            public string capturedUtc;
            public string cpu, gpu, graphicsApi, operatingSystem;
            public int screenWidth, screenHeight, targetFrameRate, vSyncCount;
            public int observedFrameCount, warmupFrameCount, sampleFrameCount;
            public double realElapsedSeconds, measuredRealSeconds, meanFps;
            public double meanFrameMs, p50FrameMs, p95FrameMs, p99FrameMs, maxFrameMs;
            public bool routeCompleted;
            public string error;
        }

        public static PerformanceProbe Begin(GameObject host, string jsonPath, int skipFrames = 120)
        {
            if (host == null) throw new ArgumentNullException("host");
            if (String.IsNullOrWhiteSpace(jsonPath)) throw new ArgumentException("Output path is required.", "jsonPath");
            var probe = host.AddComponent<PerformanceProbe>();
            probe.outputPath = Path.GetFullPath(jsonPath);
            probe.warmupFrames = Math.Max(0, skipFrames);
            probe.clock = Stopwatch.StartNew();
            return probe;
        }

        void Update()
        {
            if (clock == null || stopped) return;
            double now = clock.Elapsed.TotalMilliseconds;
            double interval = now - previousMilliseconds;
            previousMilliseconds = now;
            observedFrames++;
            if (observedFrames <= warmupFrames) return;
            if (interval >= 0 && !Double.IsNaN(interval) && !Double.IsInfinity(interval)) intervals.Add(interval);
        }

        public void StopProbe(bool routeCompleted)
        {
            if (clock == null || stopped) return;
            stopped = true;
            clock.Stop();
            double[] sorted = intervals.ToArray(); Array.Sort(sorted);
            double sum = 0;
            foreach (double interval in sorted) sum += interval;
            var report = new Report
            {
                capturedUtc = DateTime.UtcNow.ToString("O"),
                cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(), operatingSystem = SystemInfo.operatingSystem,
                screenWidth = Screen.width, screenHeight = Screen.height,
                targetFrameRate = Application.targetFrameRate, vSyncCount = QualitySettings.vSyncCount,
                observedFrameCount = observedFrames, warmupFrameCount = Math.Min(warmupFrames, observedFrames),
                sampleFrameCount = sorted.Length, realElapsedSeconds = clock.Elapsed.TotalSeconds,
                measuredRealSeconds = sum / 1000,
                meanFps = sum > 0 ? sorted.Length * 1000 / sum : 0,
                meanFrameMs = sorted.Length > 0 ? sum / sorted.Length : 0,
                p50FrameMs = Percentile(sorted, .5), p95FrameMs = Percentile(sorted, .95),
                p99FrameMs = Percentile(sorted, .99), maxFrameMs = sorted.Length > 0 ? sorted[sorted.Length - 1] : 0,
                routeCompleted = routeCompleted,
                error = sorted.Length == 0 ? "No frame intervals remained after warmup." : ""
            };
            string directory = Path.GetDirectoryName(outputPath);
            if (!String.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(outputPath, JsonUtility.ToJson(report, true));
        }

        static double Percentile(double[] sorted, double fraction)
        {
            if (sorted.Length == 0) return 0;
            double position = fraction * (sorted.Length - 1);
            int lower = (int)Math.Floor(position), upper = (int)Math.Ceiling(position);
            return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
        }

        void OnDestroy() { StopProbe(false); }
    }
}
