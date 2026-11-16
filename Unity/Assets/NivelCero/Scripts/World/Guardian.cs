using UnityEngine;
using System.Collections;

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
        int exposedPhase;
        public Renderer Core;
        void Start()
        {
            Health = GetComponent<Health>(); Health.Maximum = 360; Health.Restore(); Health.Invulnerable = true;
            Health.Hurt += (damage, origin) =>
            {
                if (!Health.Dead && !Shielded && Phase != exposedPhase)
                {
                    Shielded = true; Health.Invulnerable = true; VulnerableRemaining = 0;
                    Session.I.Notify("GL1TCH: restaurando defensa. Fase " + Phase + ". Reconectá un nodo para exponer el núcleo.");
                }
            };
            Health.Died += () =>
            {
                Session.I.Progress.bossDefeated = true; Session.I.Save();
                Session.I.Notify("Guardián desactivado. Portal abierto: escapá del Sector de Arranque.");
                Tone.Play(120, .7f, transform.position); StartCoroutine(Collapse());
            };
        }
        IEnumerator Collapse()
        {
            Vector3 initial = transform.localScale;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 1.5f)
            {
                transform.localScale = Vector3.Scale(initial, new Vector3(1, Mathf.Lerp(1, .3f, t), 1));
                transform.rotation = Quaternion.Euler(0, 0, t * 15);
                yield return null;
            }
            gameObject.SetActive(false);
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
            Active = true; Shielded = false; Health.Invulnerable = false; VulnerableRemaining = 18; exposedPhase = Phase;
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
