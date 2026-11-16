using UnityEngine;

namespace NivelCero
{
    public sealed class ArenaHazard : MonoBehaviour
    {
        public Guardian Boss;
        LineRenderer line;
        float clock, hitCooldown;
        void Start()
        {
            line = gameObject.AddComponent<LineRenderer>(); line.positionCount = 2;
            line.sharedMaterial = SectorBuilder.Red; line.startWidth = line.endWidth = .08f;
            line.SetPosition(0, transform.position - Vector3.right * 8);
            line.SetPosition(1, transform.position + Vector3.right * 8);
        }
        void Update()
        {
            var s = Session.I;
            if (s == null || s.Blocked || Boss == null || !Boss.Active || s.Progress.bossDefeated || Boss.Phase < 2)
            { if (line != null) line.enabled = false; return; }
            clock += Time.deltaTime; hitCooldown -= Time.deltaTime;
            float phase = clock % 4;
            line.enabled = phase > 1.8f; // Warn for 1.2 seconds before the damaging pulse.
            line.startWidth = line.endWidth = phase < 3 ? .035f : .2f;
            if (phase < 3 || hitCooldown > 0) return;
            var p = s.Player.transform.position;
            if (Mathf.Abs(p.z - transform.position.z) < .35f && Mathf.Abs(p.x - transform.position.x) < 8 && p.y < 1)
            { s.PlayerHealth.Damage(18, transform.position); hitCooldown = 1; }
        }
    }
}
