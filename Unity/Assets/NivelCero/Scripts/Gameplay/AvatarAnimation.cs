using UnityEngine;
using UnityEngine.InputSystem;

namespace NivelCero
{
    /// <summary>In-place generic rig driver. Gameplay damage remains independent of animation.</summary>
    public sealed class AvatarAnimation : MonoBehaviour
    {
        public Animator Animator;
        public PlayerMotor Motor;
        public PlayerCombat Combat;
        public Health Health;
        private string playing;
        private float actionUntil;
        private bool wasDead;

        private void Start()
        {
            if (Motor == null) Motor = GetComponent<PlayerMotor>();
            if (Combat == null) Combat = GetComponent<PlayerCombat>();
            if (Health == null) Health = GetComponent<Health>();
            if (Animator == null) Animator = GetComponentInChildren<Animator>(true);
            if (Animator != null)
            {
                Animator.applyRootMotion = false;
                Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                // Death can finish while the defeat menu pauses gameplay.
                Animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
            if (Combat != null) Combat.Performed += OnPerformed;
            if (Health != null) { Health.Hurt += OnHurt; Health.Died += OnDeath; }
        }

        private void Update()
        {
            if (Animator == null || Animator.runtimeAnimatorController == null || Motor == null) return;
            if (Health != null && Health.Dead)
            {
                if (!wasDead) OnDeath();
                return;
            }
            if (wasDead)
            {
                wasDead = false;
                actionUntil = 0;
                playing = null;
            }
            if (Session.I != null && Session.I.Blocked)
            {
                Animator.speed = wasDead ? 1 : 0;
                return;
            }
            Animator.speed = 1;
            if (Time.unscaledTime < actionUntil) { Animator.speed = 1; return; }
            Keyboard keys = Keyboard.current;
            if (keys != null && keys.eKey.wasPressedThisFrame) { Action("Interact", .7f); return; }
            var body = Motor.GetComponent<CharacterController>();
            Vector3 speed = Motor.Velocity;
            float planarSpeed = new Vector2(speed.x, speed.z).magnitude;
            if (planarSpeed > 15) { Animator.speed = 1; Play("Dash", .04f); return; }
            if (body != null && !body.isGrounded)
            {
                Animator.speed = 1;
                Play(speed.y > .5f ? "Jump" : "Fall", .08f);
                return;
            }
            // Match leg cadence to ground speed to remove foot sliding. Natural speeds
            // measured from the Blender clips: Walk ≈1.35 m/s, Run ≈2.9 m/s per cycle.
            string state = planarSpeed < .15f ? "Idle" : planarSpeed < 2.2f ? "Walk" : "Run";
            Play(state, .13f);
            Animator.speed = state == "Walk" ? Mathf.Clamp(planarSpeed / 1.35f, .6f, 1.8f) : state == "Run" ? Mathf.Clamp(planarSpeed / 2.9f, .8f, 2.4f) : 1;
        }

        private void OnPerformed(string state)
        {
            Action(state, state == "Reload" ? 1.25f : state.StartsWith("Melee") ? .42f : .17f);
        }
        private void OnHurt(float amount, Vector3 origin)
        {
            if (Health != null && !Health.Dead) Action("Hurt", .28f);
        }
        private void OnDeath()
        {
            wasDead = true;
            if (Animator != null) Animator.speed = 1;
            Action("Death", 10);
        }
        private void Action(string state, float duration)
        {
            actionUntil = Time.unscaledTime + duration;
            // Repeated fire and combos restart the action intentionally.
            playing = null;
            Play(state, .055f);
        }
        private void Play(string state, float fade)
        {
            if (Animator == null || Animator.runtimeAnimatorController == null || playing == state) return;
            int hash = UnityEngine.Animator.StringToHash("Base Layer." + state);
            if (!Animator.HasState(0, hash)) return;
            Animator.CrossFadeInFixedTime(hash, fade, 0, 0);
            playing = state;
        }
        private void OnDestroy()
        {
            if (Combat != null) Combat.Performed -= OnPerformed;
            if (Health != null) { Health.Hurt -= OnHurt; Health.Died -= OnDeath; }
        }
    }
}
