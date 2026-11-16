using UnityEngine;

namespace NivelCero
{
    /// <summary>
    /// Bit in 3D: floats behind Zero's shoulder (never between camera and goal),
    /// avoids clipping into walls, banks with its motion, turns towards nearby
    /// terminals as a hint, and reacts with a spin + chirp whenever it speaks.
    /// </summary>
    public sealed class BitFollower : MonoBehaviour
    {
        static BitFollower current;
        Vector3 velocity;
        float spin, spinTime;

        void OnEnable() { current = this; }
        void OnDisable() { if (current == this) current = null; }

        /// <summary>Called by Session.Notify for lines spoken by Bit.</summary>
        public static void React()
        {
            if (current == null) return;
            current.spinTime = .6f;
            SoundBank.PlayAt("bit_chirp", current.transform.position, .8f, 2, 25, .06f, .25f);
        }

        void Update()
        {
            var s = Session.I;
            if (s == null || s.Player == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            var p = s.Player.transform;
            Transform view = s.Player.View != null && s.Player.View.Lens != null ? s.Player.View.Lens.transform : p;
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = p.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 head = p.position + Vector3.up * 1.7f;
            bool firstPerson = s.Player.View != null && s.Player.View.FirstPerson;
            // Third person: behind the shoulder. First person: ahead and to the side, out of the aim line.
            Vector3 target = firstPerson
                ? p.position + forward * 2.4f + right * 3.1f + Vector3.up * (3.1f + Mathf.Sin(Time.time * 2.2f) * .1f)
                : p.position - forward * 1.1f + right * 1.25f + Vector3.up * (2.15f + Mathf.Sin(Time.time * 2.2f) * .12f);
            // Stay in open air: pull in if the preferred spot is inside geometry.
            Vector3 toTarget = target - head;
            if (Physics.SphereCast(head, .25f, toTarget.normalized, out var hit, toTarget.magnitude, 1, QueryTriggerInteraction.Ignore))
                target = head + toTarget.normalized * Mathf.Max(.3f, hit.distance - .2f);
            transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, .25f, 14, dt);

            // Look at the nearest terminal as a hint, otherwise where Zero is looking.
            Vector3 look = forward;
            float best = 7;
            foreach (var terminal in FindObjectsByType<Terminal>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(terminal.transform.position, p.position);
                if (d < best && Time.time >= terminal.LockedUntil) { best = d; look = (terminal.transform.position + Vector3.up - transform.position).normalized; }
            }
            Quaternion facing = Quaternion.LookRotation(look.sqrMagnitude > .01f ? look : forward, Vector3.up);
            Vector3 local = Quaternion.Inverse(facing) * velocity;
            Quaternion bank = Quaternion.Euler(Mathf.Clamp(local.z * 6, -20, 20), 0, Mathf.Clamp(-local.x * 8, -25, 25));
            if (spinTime > 0) { spinTime -= dt; spin += dt * 600; } else spin = Mathf.MoveTowards(spin, Mathf.Round(spin / 360) * 360, dt * 600);
            transform.rotation = Quaternion.Slerp(transform.rotation, facing * bank * Quaternion.Euler(0, spin, 0), 1 - Mathf.Exp(-dt * 8));
        }
    }
}
