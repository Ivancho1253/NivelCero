using UnityEngine;

namespace NivelCero
{
    /// <summary>
    /// 3D scene adapter for MusicDirector: exploration loop, combat loop while the
    /// Guardian fight is active, muffled mix in menus/pause/hack and stingers on
    /// victory and defeat. The score is original (Tools/Audio/generate_audio.py).
    /// </summary>
    public sealed class AdaptiveAudio : MonoBehaviour
    {
        MusicDirector music;
        bool wasDefeat, wasVictory;

        void Start()
        {
            SoundBank.Ensure();
            music = MusicDirector.Ensure();
            GameSettings.Apply();
        }

        void Update()
        {
            var s = Session.I;
            if (s == null || music == null) return;
            bool boss = s.Boss != null && s.Boss.Active && !s.Progress.bossDefeated && !s.InMenu;
            if (s.Victory) music.SetMood(MusicDirector.Mood.Victory);
            else if (s.Defeat) music.SetMood(MusicDirector.Mood.Silent);
            else music.SetMood(boss ? MusicDirector.Mood.Boss : MusicDirector.Mood.Explore);
            music.Muffled = s.InMenu || s.Paused || s.Puzzle != null;
            if (s.Defeat && !wasDefeat) music.Stinger("stinger_defeat");
            if (s.Victory && !wasVictory) music.Stinger("stinger_victory");
            wasDefeat = s.Defeat; wasVictory = s.Victory;
        }
    }
}
