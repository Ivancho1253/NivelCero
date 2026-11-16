using UnityEngine;

namespace NivelCero
{
    [RequireComponent(typeof(Health))]
    public sealed class Guardian : MonoBehaviour
    {
        public Health Health;
        public bool Active, Shielded = true, Telegraphing;
        public int Phase { get { return Health.Current > Health.Maximum * .66f ? 1 : Health.Current > Health.Maximum * .33f ? 2 : 3; } }
        public float VulnerableRemaining;
        float attackTime = 2;
        Vector3 markedTarget;
        public Renderer Core;
        void Start()
        {
            Health = GetComponent<Health>(); Health.Maximum = 360; Health.Restore(); Health.Invulnerable = true;
            Health.Died += () =>
            {
                Session.I.Progress.bossDefeated = true; Session.I.Save();
                Session.I.Notify("Guardián desactivado. Portal abierto: escapá del Sector de Arranque.");
                Tone.Play(120, .7f, transform.position); gameObject.SetActive(false);
            };
        }
        public void ResetEncounter()
        {
            if (Health == null) return;
            Health.Restore(); Shielded = true; Health.Invulnerable = true;
            VulnerableRemaining = 0; Active = false; Telegraphing = false; attackTime = 2;
        }
        public void ExposeCore()
        {
            if (!gameObject.activeSelf) return;
            Active = true; Shielded = false; Health.Invulnerable = false; VulnerableRemaining = 18;
        }
        void Update()
        {
            var s = Session.I;
            if (s == null || s.Blocked || Health.Dead) return;
            if (s.Player.transform.position.z > 89) Active = true;
            if (!Active) return;
            if (!Shielded)
            {
                VulnerableRemaining -= Time.deltaTime;
                if (VulnerableRemaining <= 0) { Shielded = true; Health.Invulnerable = true; s.Notify("Escudo restablecido. Reconectá un terminal de la arena."); }
            }
            Core.sharedMaterial = Shielded ? SectorBuilder.Red : SectorBuilder.Cyan;
            attackTime -= Time.deltaTime;
            if (attackTime > 0) return;
            if (!Telegraphing)
            {
                Telegraphing = true; markedTarget = s.Player.transform.position + Vector3.up;
                attackTime = .8f; s.Notify("Guardián: descarga marcada. ¡Movete!");
            }
            else
            {
                Telegraphing = false; attackTime = 2.7f - Phase * .4f;
                var origin = transform.position + Vector3.up * 2;
                for (int i = 0; i < Phase; i++) HostilePulse.Spawn(origin + Vector3.right * (i - 1) * .5f, markedTarget + Vector3.right * (i - (Phase - 1) * .5f) * 1.2f, 12 + Phase, 20);
            }
        }
    }
}
