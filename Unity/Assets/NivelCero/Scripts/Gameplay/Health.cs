using System;
using UnityEngine;

namespace NivelCero
{
    /// <summary>Shared damage receiver. World raycasts select the closest collider before damage.</summary>
    public sealed class Health : MonoBehaviour
    {
        [Min(1)] public float Maximum = 100;
        public bool Invulnerable;
        [Min(0)] public float DamageMultiplier = 1;
        public float Current { get; private set; }
        public bool Dead { get { return Current <= 0; } }
        public event Action Died;
        public event Action<float> Changed;
        public event Action<float, Vector3> Hurt;
        private float protectedUntil;

        private void Awake() { Restore(); }

        public void Restore()
        {
            Current = Mathf.Max(1, Maximum);
            protectedUntil = Time.time + .5f;
            if (Changed != null) Changed(Current);
        }

        public void Damage(float amount, Vector3 origin)
        {
            if (Dead || Invulnerable || amount <= 0 || Time.time < protectedUntil) return;
            PlayerCombat combat = GetComponent<PlayerCombat>();
            if (combat != null && combat.Blocking)
            {
                Vector3 incoming = origin - transform.position;
                incoming.y = 0;
                if (incoming.sqrMagnitude > .01f && Vector3.Dot(combat.AimForward, incoming.normalized) > .1f)
                    amount *= .18f;
            }
            amount *= Mathf.Max(0, DamageMultiplier);
            Current = Mathf.Max(0, Current - amount);
            if (GetComponent<PlayerMotor>() != null) protectedUntil = Time.time + .35f;
            if (Hurt != null) Hurt(amount, origin);
            if (Changed != null) Changed(Current);
            if (!Dead) return;
            if (Died != null) Died();
            if (GetComponent<PlayerMotor>() != null && Session.I != null) Session.I.PlayerDied();
        }

        public void Heal(float amount)
        {
            if (Dead || amount <= 0) return;
            Current = Mathf.Min(Maximum, Current + amount);
            if (Changed != null) Changed(Current);
        }
    }
}
