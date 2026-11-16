using UnityEngine;

namespace NivelCero
{
    /// <summary>
    /// Music and ambience mixer: crossfades exploration and combat loops (same key
    /// and tempo), keeps the digital-plant ambience underneath and muffles both
    /// with a low-pass while the game is paused. Volumes follow GameSettings.
    /// </summary>
    public sealed class MusicDirector : MonoBehaviour
    {
        public enum Mood { Silent, Explore, Boss, Victory }
        static MusicDirector instance;
        AudioSource explore, boss, ambience, stinger;
        AudioLowPassFilter exploreFilter, bossFilter, ambienceFilter;
        Mood mood = Mood.Silent;
        public bool Muffled;
        public Mood Current { get { return mood; } }

        public static MusicDirector Ensure()
        {
            if (instance != null) return instance;
            var go = new GameObject("Audio · música y ambiente");
            instance = go.AddComponent<MusicDirector>();
            instance.explore = instance.Loop(go, "music_explore", out instance.exploreFilter);
            instance.boss = instance.Loop(go, "music_boss", out instance.bossFilter);
            instance.ambience = instance.Loop(go, "amb_system", out instance.ambienceFilter);
            instance.stinger = go.AddComponent<AudioSource>();
            instance.stinger.playOnAwake = false; instance.stinger.ignoreListenerPause = true;
            return instance;
        }

        AudioSource Loop(GameObject host, string id, out AudioLowPassFilter filter)
        {
            var child = new GameObject(id); child.transform.SetParent(host.transform, false);
            var source = child.AddComponent<AudioSource>();
            source.clip = Resources.Load<AudioClip>("Audio/" + id);
            source.loop = true; source.volume = 0; source.playOnAwake = false; source.ignoreListenerPause = true;
            filter = child.AddComponent<AudioLowPassFilter>(); filter.cutoffFrequency = 22000;
            if (source.clip != null) source.Play();
            return source;
        }

        public void SetMood(Mood next)
        {
            if (next == mood) return;
            mood = next;
            if (next == Mood.Boss && boss.clip != null) { boss.time = 0; if (!boss.isPlaying) boss.Play(); }
        }

        public void Stinger(string id)
        {
            var clip = Resources.Load<AudioClip>("Audio/" + id);
            if (clip != null) stinger.PlayOneShot(clip, GameSettings.Music);
        }

        void Update()
        {
            float music = GameSettings.Music, dt = Time.unscaledDeltaTime;
            float exploreTarget = mood == Mood.Explore ? .85f : mood == Mood.Boss ? 0 : 0;
            float bossTarget = mood == Mood.Boss ? .9f : 0;
            float ambienceTarget = mood == Mood.Silent ? .35f : mood == Mood.Boss ? .3f : .55f;
            // Slow fade-in, faster fade-out: transitions without abrupt cuts.
            explore.volume = Mathf.MoveTowards(explore.volume, exploreTarget * music, dt * (exploreTarget > 0 ? .35f : .7f));
            boss.volume = Mathf.MoveTowards(boss.volume, bossTarget * music, dt * (bossTarget > 0 ? .9f : .5f));
            ambience.volume = Mathf.MoveTowards(ambience.volume, ambienceTarget * Mathf.Max(.25f, music) * GameSettings.Sfx, dt * .4f);
            stinger.volume = 1;
            float cutoff = Muffled ? 900 : 22000;
            exploreFilter.cutoffFrequency = bossFilter.cutoffFrequency = ambienceFilter.cutoffFrequency =
                Mathf.Lerp(exploreFilter.cutoffFrequency, cutoff, 1 - Mathf.Exp(-dt * 8));
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
