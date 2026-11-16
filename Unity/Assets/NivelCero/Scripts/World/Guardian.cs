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
        /// <summary>HUD feedback timers: recent damage flash and phase-change banner.</summary>
        public float HitFlash, PhaseBanner;
        float attackTime = 2;
        Vector3 markedTarget;
        int exposedPhase;
        /// <summary>Seconds left of the activation sequence (HUD title card; no attacks).</summary>
        public float IntroTime;
        Light[] arenaLights; float[] arenaBase;
        void CollectArenaLights()
        {
            var found = new System.Collections.Generic.List<Light>();
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.name.Contains("arena") || l.name.Contains("Arena")) found.Add(l);
            arenaLights = found.ToArray(); arenaBase = new float[arenaLights.Length];
            for (int i = 0; i < arenaLights.Length; i++) arenaBase[i] = arenaLights[i].intensity;
        }
        Vector3 coreScale;
        public Renderer Core;
        void Start()
        {
            Health = GetComponent<Health>(); Health.Maximum = 360; Health.Restore(); Health.Invulnerable = true;
            Health.Hurt += (damage, origin) =>
            {
                if (!Health.Dead) { SoundBank.PlayAt("boss_hurt", transform.position, .8f, 6, 60, .05f, .3f); HitFlash = .1f; }
                if (!Health.Dead && !Shielded && Phase != exposedPhase)
                {
                    Shielded = true; Health.Invulnerable = true; VulnerableRemaining = 0;
                    SoundBank.PlayAt("boss_phase", transform.position, 1, 8, 80);
                    PhaseBanner = 2.5f;
                    Session.I.Notify("GL1TCH: restaurando defensa. Fase " + Phase + ". Reconectá un nodo para exponer el núcleo.");
                }
            };
            Health.Died += () =>
            {
                Session.I.Progress.bossDefeated = true; Session.I.Save();
                Session.I.Notify("Guardián desactivado. Portal abierto: escapá del Sector de Arranque.");
                SoundBank.PlayAt("boss_death", transform.position, 1, 10, 90); StartCoroutine(Collapse());
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
            VulnerableRemaining = 0; Active = false; Telegraphing = false; attackTime = 2; IntroTime = 0;
        }
        public void ExposeCore()
        {
            if (!gameObject.activeSelf) return;
            Active = true; Shielded = false; Health.Invulnerable = false; VulnerableRemaining = 18; exposedPhase = Phase;
            SoundBank.PlayAt("shield_open", transform.position, 1, 8, 70);
        }
        void Update()
        {
            var s = Session.I;
            if (s == null || s.Blocked || Health.Dead) return;
            if (s.Player.transform.position.z > 89 && !Active)
            {
                Active = true; IntroTime = 2.8f; attackTime = Mathf.Max(attackTime, 3.2f);
                SoundBank.Play("boss_activate");
                s.Notify("GL1TCH: esta memoria me pertenece. Guardián, despertá.");
            }
            if (arenaLights == null) CollectArenaLights();
            float wake = Active ? 1 - Mathf.Clamp01(IntroTime / 2.8f) : 0;
            for (int i = 0; i < arenaLights.Length; i++) if (arenaLights[i] != null) arenaLights[i].intensity = arenaBase[i] * Mathf.Lerp(.3f, 1, wake);
            if (!Active) return;
            IntroTime = Mathf.Max(0, IntroTime - Time.deltaTime);
            // Face Zero (yaw only): the Guardian tracks its target, readable from any angle.
            Vector3 look = s.Player.transform.position - transform.position; look.y = 0;
            if (look.sqrMagnitude > .1f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-look), Time.deltaTime * (IntroTime > 0 ? 1 : 2.5f));
            HitFlash -= Time.deltaTime; PhaseBanner -= Time.deltaTime;
            if (!Shielded)
            {
                VulnerableRemaining -= Time.deltaTime;
                if (VulnerableRemaining <= 0) { Shielded = true; Health.Invulnerable = true; s.Notify("Escudo restablecido. Reconectá un terminal de la arena."); SoundBank.PlayAt("shield_open", transform.position, .8f, 8, 70); }
            }
            // Shielded = red and small; telegraph = pulsing; exposed = amber-white and open.
            Core.sharedMaterial = Shielded ? SectorBuilder.Red : (SectorBuilder.Exposed != null ? SectorBuilder.Exposed : SectorBuilder.Cyan);
            float pulse = Telegraphing ? 1 + Mathf.Abs(Mathf.Sin(Time.time * 14)) * .25f : Shielded ? .85f : 1.15f;
            if (coreScale == Vector3.zero) coreScale = Core.transform.localScale;
            Core.transform.localScale = Vector3.Lerp(Core.transform.localScale, coreScale * pulse, Time.deltaTime * 12);
            attackTime -= Time.deltaTime;
            if (attackTime > 0) return;
            if (!Telegraphing)
            {
                Telegraphing = true; markedTarget = s.Player.transform.position + Vector3.up;
                attackTime = .8f; s.Notify("Guardián: descarga marcada. ¡Movete!");
                SoundBank.PlayAt("boss_charge", transform.position + Vector3.up * 2, 1, 8, 70);
            }
            else
            {
                Telegraphing = false; attackTime = 2.7f - Phase * .4f;
                var origin = transform.position + Vector3.up * 2;
                SoundBank.PlayAt("boss_fire", origin, 1, 8, 70);
                for (int i = 0; i < Phase; i++) HostilePulse.Spawn(origin + Vector3.right * (i - 1) * .5f, markedTarget + Vector3.right * (i - (Phase - 1) * .5f) * 1.2f, 12 + Phase, 20);
            }
        }
    }
}
