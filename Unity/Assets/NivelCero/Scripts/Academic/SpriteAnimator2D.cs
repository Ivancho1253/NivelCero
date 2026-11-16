using UnityEngine;

namespace NivelCero
{
    /// <summary>
    /// Frame-by-frame sprite player. Clips are arrays of sprites sliced from the
    /// original sheets; the owner chooses the state (idle, run, attack...) and this
    /// component only advances frames. Uses scaled time so pause freezes it.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteAnimator2D : MonoBehaviour
    {
        public SpriteRenderer Target;
        public string Current { get; private set; }
        public int Frame { get; private set; }
        public bool Finished { get; private set; }
        public float Speed = 1;
        public bool Unscaled;
        Sprite[] frames;
        float fps, clock;
        bool loop;

        void Awake() { if (Target == null) Target = GetComponent<SpriteRenderer>(); }

        public void Play(string name, Sprite[] clip, float framesPerSecond, bool looping = true, bool restart = false)
        {
            if (clip == null || clip.Length == 0) return;
            if (!restart && name == Current) { fps = framesPerSecond; return; }
            Current = name; frames = clip; fps = framesPerSecond; loop = looping;
            clock = 0; Frame = 0; Finished = false;
            if (Target == null) Target = GetComponent<SpriteRenderer>();
            Target.sprite = frames[0];
        }

        /// <summary>Shows one specific frame without advancing (e.g. jump by vertical speed).</summary>
        public void Hold(string name, Sprite[] clip, int frame)
        {
            if (clip == null || clip.Length == 0) return;
            Current = name; frames = clip; fps = 0; loop = false; Finished = true;
            Frame = Mathf.Clamp(frame, 0, clip.Length - 1);
            Target.sprite = frames[Frame];
        }

        void Update()
        {
            if (frames == null || fps <= 0) return;
            clock += (Unscaled ? Time.unscaledDeltaTime : Time.deltaTime) * Speed;
            int index = (int)(clock * fps);
            if (loop) index %= frames.Length;
            else if (index >= frames.Length) { index = frames.Length - 1; Finished = true; }
            if (index != Frame || Target.sprite != frames[index]) { Frame = index; Target.sprite = frames[index]; }
        }
    }
}
