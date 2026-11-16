using UnityEngine;

namespace NivelCero
{
    // Original synthesized score. Exploration pad and combat ostinato share tempo and key.
    public sealed class AdaptiveAudio : MonoBehaviour
    {
        AudioSource atmosphere, threat;
        AudioClip calmClip, fightClip;
        void Start()
        {
            atmosphere = gameObject.AddComponent<AudioSource>(); threat = gameObject.AddComponent<AudioSource>();
            calmClip = Compose(false); fightClip = Compose(true);
            atmosphere.clip = calmClip; threat.clip = fightClip;
            atmosphere.loop = threat.loop = true; atmosphere.volume = .35f; threat.volume = 0;
            atmosphere.Play(); threat.Play();
        }
        void Update()
        {
            var s = Session.I;
            bool boss = s != null && s.Boss != null && s.Boss.Active && !s.Progress.bossDefeated && !s.InMenu;
            float blend = Time.unscaledDeltaTime * .3f;
            threat.volume = Mathf.MoveTowards(threat.volume, boss ? .26f : 0, blend);
            atmosphere.volume = Mathf.MoveTowards(atmosphere.volume, boss ? .14f : .32f, blend);
            float pitch = s != null && s.Defeat ? .8f : 1;
            atmosphere.pitch = Mathf.Lerp(atmosphere.pitch, pitch, Time.unscaledDeltaTime);
        }
        AudioClip Compose(bool combat)
        {
            const int sampleRate = 22050, seconds = 32;
            var samples = new float[sampleRate * seconds];
            int[] bass = {38, 38, 34, 36}; int[] arpeggio = {0, 7, 12, 15, 12, 7, 3, 7};
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / sampleRate;
                int bar = (int)(t / 8) % 4;
                float hz = 440 * Mathf.Pow(2, (bass[bar] - 69) / 12f);
                float pad = Mathf.Sin(t * hz * 2 * Mathf.PI) * .13f + Mathf.Sin(t * hz * 3 * Mathf.PI) * .04f;
                pad *= .65f + .35f * Mathf.Sin(t * Mathf.PI / 8);
                float value = pad;
                if (combat)
                {
                    float phase = t % .5f; int step = (int)(t * 2) % 8;
                    float noteHz = hz * Mathf.Pow(2, (arpeggio[step] + 12) / 12f);
                    value = Mathf.Sin(t * noteHz * 2 * Mathf.PI) * Mathf.Exp(-phase * 13) * .2f;
                    float kick = t % 1;
                    value += Mathf.Sin(2 * Mathf.PI * (55 * kick + 15 * (1 - Mathf.Exp(-kick * 30)))) * Mathf.Exp(-kick * 16) * .2f;
                    value += pad * .5f;
                }
                // Fade the loop boundary without abrupt discontinuities.
                float edge = Mathf.Min(1, Mathf.Min(t, seconds - t) * 3);
                samples[i] = value * edge;
            }
            var clip = AudioClip.Create(combat ? "NC combate original" : "NC exploración original", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0); return clip;
        }
        void OnDestroy() { if (calmClip != null) Destroy(calmClip); if (fightClip != null) Destroy(fightClip); }
    }
}
