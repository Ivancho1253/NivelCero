using UnityEngine;

namespace NivelCero
{
    /// <summary>Player preferences shared by the 3D and 2D scenes (PlayerPrefs).</summary>
    public static class GameSettings
    {
        public static float Master
        {
            get { return PlayerPrefs.GetFloat("volume", .8f); }
            set { PlayerPrefs.SetFloat("volume", Mathf.Clamp01(value)); AudioListener.volume = Mathf.Clamp01(value); }
        }
        public static float Music
        {
            get { return PlayerPrefs.GetFloat("musicVolume", .65f); }
            set { PlayerPrefs.SetFloat("musicVolume", Mathf.Clamp01(value)); }
        }
        public static float Sfx
        {
            get { return PlayerPrefs.GetFloat("sfxVolume", .85f); }
            set { PlayerPrefs.SetFloat("sfxVolume", Mathf.Clamp01(value)); }
        }
        /// <summary>Disables camera shake, screen flashes and glitch displacement. Information stays.</summary>
        public static bool ReduceEffects
        {
            get { return PlayerPrefs.GetInt("reduceEffects", 0) == 1; }
            set { PlayerPrefs.SetInt("reduceEffects", value ? 1 : 0); }
        }
        public static void Apply() { AudioListener.volume = Master; }
        public static void Save() { PlayerPrefs.Save(); }
    }
}
