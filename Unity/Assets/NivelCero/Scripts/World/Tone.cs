using UnityEngine;
using UnityEngine.InputSystem;

namespace NivelCero
{
    public static class Tone
    {
        public static void Play(float frequency, float length, Vector3? position = null)
        {
            var go = new GameObject("Audio sintético original");
            if (position.HasValue) go.transform.position = position.Value;
            var source = go.AddComponent<AudioSource>();
            int n = (int)(44100 * length); var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / 44100) * .08f * (1 - (float)i / n);
            var clip = AudioClip.Create("Pulso", n, 1, 44100, false); clip.SetData(data, 0);
            source.clip = clip; source.spatialBlend = position.HasValue ? 1 : 0; source.Play();
            Object.Destroy(go, length + .1f); Object.Destroy(clip, length + .2f);
        }
    }
}
