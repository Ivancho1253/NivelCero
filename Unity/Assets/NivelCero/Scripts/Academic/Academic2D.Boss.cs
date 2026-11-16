using System.Collections.Generic;
using UnityEngine;

namespace NivelCero
{
    /// <summary>
    /// Guardián del Sistema (2D). Encounter design (GDD §14):
    /// - Intro: dormant shell boots up, arena shutter closes, title card.
    /// - Armoured states (idle, raise, slam) deflect 80 % of damage; after every
    ///   attack the core opens ("Exposed") and takes full damage. The window is
    ///   always granted: difficulty comes from reading patterns, not from HP.
    /// - Phase 1 (180–120): marked pulse (one target).
    ///   Phase 2 (120–60): triple marked pulse + floor slam with a jumpable wave.
    ///   Phase 3 (60–0): adds falling corrupted blocks on marked columns.
    /// - Each phase change staggers the Guardian (invulnerable, glitch, loses a plate).
    /// Every attack is telegraphed by animation, a red marker and its own sound.
    /// </summary>
    public sealed partial class Academic2D
    {
        public enum BossState { Dormant, Intro, Idle, Charge, Fire, Exposed, Raise, Slam, Rain, PhaseShift, Dying, Dead }
        enum HostileKind { Orb, Wave, Block }
        sealed class Hostile
        {
            public HostileKind Kind; public SpriteRenderer Renderer, Marker; public SpriteAnimator2D Anim;
            public Vector2 From, To; public float T, Duration, Delay, Damage; public bool Live = true;
        }

        public const float BossMaxHealth = 180;
        public BossState Boss { get { return bossState; } }
        public bool BossVulnerable { get { return bossState == BossState.Exposed; } }
        public int BossPhase { get { return bossHP > 120 ? 1 : bossHP > 60 ? 2 : 3; } }
        public float BossStateTime { get { return bossTimer; } }

        SpriteRenderer guardian;
        SpriteAnimator2D guardianAnim;
        BoxCollider2D guardianBody;
        readonly List<Hostile> hostiles = new List<Hostile>();
        BossState bossState = BossState.Dormant;
        float bossHP = BossMaxHealth, bossTimer, bossFlash, bossDrift, introClock, phaseBanner, telegraph;
        bool bossActive;
        int patternStep, bossVariant, shownPhase = 1;
        readonly Vector3 bossHome = new Vector3(92, 4.4f, 0);
        readonly HashSet<string> taughtAttacks = new HashSet<string>();

        void BuildBoss()
        {
            guardian = Visual("Guardian del Sistema", art.Guardian["dormant"][0], bossHome + Vector3.down * .3f, 8);
            guardianAnim = guardian.gameObject.AddComponent<SpriteAnimator2D>();
            guardianBody = guardian.gameObject.AddComponent<BoxCollider2D>();
            guardianBody.size = new Vector2(2.3f, 2.4f); guardianBody.offset = new Vector2(0, .15f);
            guardianBody.isTrigger = true; // shots hit it; Zero can pass under its hover.
            guardianAnim.Hold("dormant", art.Guardian["dormant"], 0);
        }

        // ------------------------------------------------------------ queries (Bit and automation)
        /// <summary>True when x is outside every pending marked impact (pulse targets and rain columns).</summary>
        public bool IsPositionSafe(float x)
        {
            foreach (var h in hostiles)
            {
                if (!h.Live) continue;
                if (h.Kind == HostileKind.Orb && Mathf.Abs(h.To.x - x) < 1.45f) return false;
                if (h.Kind == HostileKind.Block && Mathf.Abs(h.To.x - x) < 1.05f) return false;
            }
            return true;
        }

        /// <summary>Distance from the nearest ground wave to Zero along its travel; +∞ if none approaches.</summary>
        public float ShockwaveDistance
        {
            get
            {
                float best = float.PositiveInfinity;
                foreach (var h in hostiles)
                    if (h.Live && h.Kind == HostileKind.Wave && h.Renderer != null)
                    {
                        float d = h.Renderer.transform.position.x - player.position.x;
                        if (d > -.3f && d < best) best = d;
                    }
                return best;
            }
        }

        // ------------------------------------------------------------ state machine
        void UpdateBoss(float dt)
        {
            bossFlash -= dt; phaseBanner -= dt;
            FixedDoor();
            if (bossState == BossState.Dead) return;
            if (bossState == BossState.Dormant)
            {
                guardian.transform.position = bossHome + Vector3.down * .3f + Vector3.up * Mathf.Sin(elapsed * .8f) * .04f;
                if (gateUnlocked && bossHP > 0 && player.position.x > 80) StartIntro();
                return;
            }
            bossTimer -= dt;
            int phase = BossPhase;
            // Gentle hover and drift: alive, but never moving out of the readable centre.
            bossDrift += dt * (bossState == BossState.Idle ? 1 : .3f);
            Vector3 hover = new Vector3(Mathf.Sin(bossDrift * .9f) * 1.2f, Mathf.Sin(elapsed * 1.7f) * .18f, 0);
            if (bossState == BossState.Intro) hover = Vector3.down * .3f * Mathf.Clamp01(bossTimer / 2.8f);
            if (bossState != BossState.Dying) guardian.transform.position = Vector3.Lerp(guardian.transform.position, bossHome + hover, 1 - Mathf.Exp(-dt * 4));

            switch (bossState)
            {
                case BossState.Intro:
                    introClock += dt;
                    if (introClock < 1.0f) guardianAnim.Hold("dormant", art.Guardian["dormant"], 0);
                    else if (introClock < 1.9f) guardianAnim.Play("charge", art.Guardian["charge0"], 4, false);
                    else guardianAnim.Play("idle", art.Guardian["idle0"], 6);
                    if (bossTimer <= 0)
                    {
                        Say(Speaker.Guardian, "PROTOCOLO DE SEGURIDAD 01. INTRUSO DETECTADO. ELIMINAR.");
                        Enter(BossState.Idle, 1.2f);
                    }
                    break;
                case BossState.Idle:
                    guardianAnim.Play("idle", art.Guardian["idle" + bossVariant], 6);
                    if (bossTimer <= 0) NextAttack(phase);
                    break;
                case BossState.Charge:
                    telegraph = bossTimer;
                    guardianAnim.Play("charge", art.Guardian["charge" + bossVariant], 3.6f / .9f, false);
                    if (bossTimer <= 0) FireMarkedPulse(phase);
                    break;
                case BossState.Fire:
                    guardianAnim.Play("fire", art.Guardian["fire" + bossVariant], 8, false);
                    if (bossTimer <= 0) Enter(BossState.Exposed, phase == 1 ? 1.5f : phase == 2 ? 1.35f : 1.4f);
                    break;
                case BossState.Raise:
                    telegraph = bossTimer;
                    guardianAnim.Play("raise", art.Guardian["raise" + bossVariant], 3, false);
                    if (bossTimer <= 0)
                    {
                        if (patternStep % 3 == 2 && phase == 3) BeginRain(); else Slam();
                    }
                    break;
                case BossState.Slam:
                    guardianAnim.Play("slam", art.Guardian["slam" + bossVariant], 8, false);
                    if (bossTimer <= 0) Enter(BossState.Exposed, 1.5f);
                    break;
                case BossState.Rain:
                    guardianAnim.Play("raise", art.Guardian["raise" + bossVariant], 2, false);
                    if (bossTimer <= 0) Enter(BossState.Exposed, 1.6f);
                    break;
                case BossState.Exposed:
                    guardianAnim.Play("exposed", art.Guardian["exposed" + bossVariant], 5);
                    if (bossTimer <= 0)
                    {
                        SoundBank.Play("shield_open", .7f, .8f);
                        Enter(BossState.Idle, phase == 1 ? 1.1f : phase == 2 ? .9f : .75f);
                    }
                    break;
                case BossState.PhaseShift:
                    guardianAnim.Play("glitch", art.Guardian["glitch" + bossVariant], 10);
                    if (bossTimer <= 0)
                    {
                        bossVariant = Mathf.Clamp(phase - 1, 0, 2);
                        Say(Speaker.Glitch, phase == 2 ? "¿Creés que podés reparar lo que yo reescribí? Guardián: más potencia." : "Esta memoria me pertenece. Si caigo, me llevo el sector conmigo.");
                        Enter(BossState.Idle, .8f);
                    }
                    break;
                case BossState.Dying:
                    guardianAnim.Play("death", art.Guardian["death"], 3.4f, false);
                    guardian.transform.position += Vector3.down * dt * .35f;
                    if (Random.value < dt * 10) SpawnFx(Random.value < .5f ? "hitRed" : "hitWhite", (Vector2)guardian.transform.position + Random.insideUnitCircle * 1.5f, 16, true, 12);
                    if (bossTimer <= 0)
                    {
                        bossState = BossState.Dead;
                        guardian.gameObject.SetActive(false);
                        OpenArena();
                        music.SetMood(MusicDirector.Mood.Explore);
                        Say(Speaker.Bit, RequiredFragments < 2 ? "¡Lo desactivamos! Pero sin los dos fragmentos el portal no compila." : "¡Lo desactivamos! El portal se está estabilizando.");
                    }
                    break;
            }
            if (bossFlash > 0 && bossState != BossState.Dying) guardian.sprite = art.Guardian["flash" + bossVariant][0];

            // Contact with the hovering hull hurts and pushes Zero away.
            if (bossState != BossState.Dying && bossState != BossState.Intro && guardianBody.OverlapPoint(player.position + Vector2.up * .4f))
                ReceiveDamage(10, guardian.transform.position);
        }

        void StartIntro()
        {
            bossActive = true; introClock = 0;
            Enter(BossState.Intro, 2.8f);
            CloseArena();
            SoundBank.Play("boss_activate");
            music.SetMood(MusicDirector.Mood.Boss);
            ClearHostiles();
            if (bitBrain != null) bitBrain.Alert();
            Say(Speaker.Glitch, "Esta memoria me pertenece. Guardián, despertá.");
        }

        void Enter(BossState next, float duration)
        {
            bossState = next; bossTimer = duration; telegraph = 0;
        }

        void NextAttack(int phase)
        {
            patternStep++;
            if (phase == 1) { BeginCharge(); return; }
            if (phase == 2) { if (patternStep % 2 == 0) BeginCharge(); else BeginRaise(); return; }
            // Phase 3 cycle: pulse, slam, rain.
            if (patternStep % 3 == 0) BeginCharge(); else BeginRaise();
        }

        void BeginCharge()
        {
            int phase = BossPhase;
            Enter(BossState.Charge, phase == 3 ? .8f : .9f);
            SoundBank.Play("boss_charge");
            // Targets are marked now and do not track: moving out of the reticle is the answer.
            float x = Mathf.Clamp(player.position.x, 78.6f, 106);
            var targets = new List<float> { x };
            if (phase >= 2) { targets.Add(x - 2.4f); targets.Add(x + 2.4f); }
            foreach (float tx in targets)
            {
                float clamped = Mathf.Clamp(tx, 78.4f, 107);
                var marker = Visual("Retícula de impacto", art.Fx["reticle"][0], new Vector3(clamped, 1.25f, 0), 13);
                marker.gameObject.AddComponent<SpriteAnimator2D>().Play("reticle", art.Fx["reticle"], 6);
                hostiles.Add(new Hostile { Kind = HostileKind.Orb, Marker = marker, To = new Vector2(clamped, 1.4f), Delay = 99, Damage = 15, Duration = .55f });
            }
            Teach("pulse", "Bit: ¡Marca roja en el suelo! Salí de la retícula antes de la descarga.");
        }

        void FireMarkedPulse(int phase)
        {
            Enter(BossState.Fire, .3f);
            SoundBank.Play("boss_fire");
            shake = Mathf.Max(shake, .15f);
            Vector2 core = guardian.transform.position + new Vector3(0, .2f, 0);
            foreach (var h in hostiles)
            {
                if (h.Kind != HostileKind.Orb || h.Renderer != null || !h.Live) continue;
                h.From = core; h.Delay = 0; h.T = 0;
                h.Renderer = Visual("Descarga marcada", art.Fx["orb"][0], core, 14);
                h.Anim = h.Renderer.gameObject.AddComponent<SpriteAnimator2D>();
                h.Anim.Play("orb", art.Fx["orb"], 14);
            }
        }

        void BeginRaise()
        {
            Enter(BossState.Raise, .9f);
            SoundBank.Play("boss_charge", .8f, .7f);
        }

        void Slam()
        {
            Enter(BossState.Slam, .4f);
            SoundBank.Play("boss_slam"); SoundBank.Play("shockwave", .8f);
            shake = Mathf.Max(shake, .45f);
            var wave = Visual("Onda de choque", art.Fx["wave"][0], new Vector3(guardian.transform.position.x - 1.4f, 1, 0), 13);
            var anim = wave.gameObject.AddComponent<SpriteAnimator2D>(); anim.Play("wave", art.Fx["wave"], 12);
            wave.flipX = true;
            hostiles.Add(new Hostile { Kind = HostileKind.Wave, Renderer = wave, Anim = anim, From = wave.transform.position, Damage = 15 });
            Teach("wave", "Bit: ¡Onda por el suelo! Saltala.");
        }

        void BeginRain()
        {
            Enter(BossState.Rain, 1.9f);
            SoundBank.Play("glitch_voice", .7f);
            float x = Mathf.Clamp(player.position.x, 79.5f, 105);
            foreach (float offset in new[] { -2.6f, 0f, 2.6f })
            {
                float cx = Mathf.Clamp(x + offset, 78.4f, 107);
                var column = Visual("Columna marcada", art.Fx["column"][0], new Vector3(cx, 4.5f, 0), 3);
                column.transform.localScale = new Vector3(1, 1, 1);
                column.drawMode = SpriteDrawMode.Tiled; column.size = new Vector2(1.5f, 7.5f);
                var anim = column.gameObject.AddComponent<SpriteAnimator2D>(); anim.Play("column", art.Fx["column"], 6);
                hostiles.Add(new Hostile { Kind = HostileKind.Block, Marker = column, To = new Vector2(cx, 1.5f), From = new Vector2(cx, 11), Delay = 1.1f, Duration = .5f, Damage = 20 });
            }
            Teach("rain", "Bit: ¡Columnas marcadas! Quedate en el hueco entre las franjas.");
        }

        void Teach(string key, string line)
        {
            if (!taughtAttacks.Add(key)) return;
            Say(Speaker.Bit, line.StartsWith("Bit: ") ? line.Substring(5) : line);
        }

        // ------------------------------------------------------------ projectiles and hazards
        void UpdateHostiles(float dt)
        {
            Vector2 p = player != null ? player.position : Vector2.zero;
            for (int i = hostiles.Count - 1; i >= 0; i--)
            {
                var h = hostiles[i];
                switch (h.Kind)
                {
                    case HostileKind.Orb:
                        if (h.Renderer == null) break; // still a marked target, waiting for the volley
                        h.T += dt / h.Duration;
                        Vector2 pos = Vector2.Lerp(h.From, h.To, h.T) + Vector2.up * Mathf.Sin(Mathf.PI * Mathf.Clamp01(h.T)) * 1.6f;
                        h.Renderer.transform.position = pos;
                        if (Vector2.Distance(pos, p) < .55f) { ReceiveDamage(h.Damage, pos); Detonate(h, pos, false); break; }
                        if (h.T >= 1) Detonate(h, h.To, true);
                        break;
                    case HostileKind.Wave:
                        var t = h.Renderer.transform;
                        t.position += Vector3.left * 8.5f * dt;
                        if (Mathf.Abs(p.x - t.position.x) < .6f && p.y < 2.35f) ReceiveDamage(h.Damage, t.position + Vector3.right);
                        if (t.position.x < Academic2DLevel.ArenaDoorX + .8f) { SpawnFx("dust", t.position, 12, false, 12); Remove(h); }
                        break;
                    case HostileKind.Block:
                        if (h.Delay > 0)
                        {
                            h.Delay -= dt;
                            if (h.Delay <= 0)
                            {
                                h.Renderer = Visual("Bloque corrupto", art.Fx["block"][0], h.From, 14);
                                h.Anim = h.Renderer.gameObject.AddComponent<SpriteAnimator2D>(); h.Anim.Play("block", art.Fx["block"], 8);
                                SoundBank.Play("block_fall", .8f);
                            }
                            break;
                        }
                        var bt = h.Renderer.transform;
                        bt.position += Vector3.down * 18 * dt;
                        if (Mathf.Abs(p.x - bt.position.x) < .75f && Mathf.Abs(p.y - bt.position.y) < 1.2f) ReceiveDamage(h.Damage, bt.position);
                        if (bt.position.y <= h.To.y)
                        {
                            SoundBank.Play("block_impact", .9f);
                            SpawnFx("hitRed", h.To, 14, true, 14);
                            shake = Mathf.Max(shake, .15f);
                            Remove(h);
                        }
                        break;
                }
            }
            hostiles.RemoveAll(h => !h.Live);
        }

        void Detonate(Hostile h, Vector2 at, bool area)
        {
            SpawnFx("hitRed", at, 16, true, 15);
            SoundBank.Play("block_impact", .7f, 1.25f);
            if (area && Vector2.Distance(at, player.position + Vector2.down * .5f) < 1.15f) ReceiveDamage(h.Damage, at);
            Remove(h);
        }

        void Remove(Hostile h)
        {
            h.Live = false;
            if (h.Renderer != null) Destroy(h.Renderer.gameObject);
            if (h.Marker != null) Destroy(h.Marker.gameObject);
        }

        void ClearHostiles()
        {
            foreach (var h in hostiles) Remove(h);
            hostiles.Clear();
        }

        // ------------------------------------------------------------ damage
        void GuardianHit(float damage, Vector2 point)
        {
            if (bossHP <= 0) return;
            bool armoured = bossState != BossState.Exposed;
            bool immune = bossState == BossState.Dormant || bossState == BossState.Intro || bossState == BossState.PhaseShift || bossState == BossState.Dying || bossState == BossState.Dead;
            if (immune)
            {
                SpawnFx("hitWhite", point, 18, true, 16);
                SoundBank.Play("deflect", .7f, 1, .05f, .12f);
                return;
            }
            int phaseBefore = BossPhase;
            float applied = armoured ? damage * .2f : damage;
            bossHP = Mathf.Max(0, bossHP - applied);
            hitFeedback = .15f;
            if (armoured)
            {
                SpawnFx("hitWhite", point, 18, true, 16);
                SoundBank.Play("deflect", .8f, 1, .06f, .09f);
            }
            else
            {
                bossFlash = .06f;
                SpawnFx("hitCyan", point, 18, true, 16);
                SoundBank.Play("hit_enemy", 1, 1, .05f, .06f);
                SoundBank.Play("boss_hurt", .8f, 1, .05f, .35f);
            }
            // Never skip a phase with one big hit: clamp to the threshold and stagger.
            int phaseAfter = BossPhase;
            if (bossHP > 0 && phaseAfter != phaseBefore)
            {
                bossHP = phaseBefore == 1 ? 120 : 60;
                ClearHostiles();
                Enter(BossState.PhaseShift, 1.8f);
                phaseBanner = 2.2f; shownPhase = BossPhase;
                SoundBank.Play("boss_phase");
                shake = Mathf.Max(shake, .6f);
                SpawnFx("hitRed", guardian.transform.position + new Vector3(-1.4f, .8f, 0), 12, true, 14);
            }
            if (bossHP <= 0)
            {
                ClearHostiles();
                Enter(BossState.Dying, 2.6f);
                SoundBank.Play("boss_death");
                shake = Mathf.Max(shake, .9f);
                Say(Speaker.Glitch, "Esto... no termina... acá.");
            }
        }

        void ResetBoss()
        {
            bossHP = BossMaxHealth; bossActive = false; bossVariant = 0; patternStep = 0; shownPhase = 1;
            bossState = BossState.Dormant; bossTimer = 0; telegraph = 0;
            guardian.gameObject.SetActive(true);
            guardian.transform.position = bossHome + Vector3.down * .3f;
            guardianAnim.Hold("dormant", art.Guardian["dormant"], 0);
            OpenArena();
        }

        void CloseArena()
        {
            doorTarget = 0;
            door.GetComponent<BoxCollider2D>().enabled = true;
            SoundBank.Play("door_close");
        }

        void OpenArena()
        {
            doorTarget = 9;
            door.GetComponent<BoxCollider2D>().enabled = false;
        }

        float doorTarget = 9;
        void FixedDoor()
        {
            doorOffset = Mathf.MoveTowards(doorOffset, doorTarget, Time.deltaTime * (doorTarget < doorOffset ? 30 : 8));
            door.transform.position = new Vector3(Academic2DLevel.ArenaDoorX, 5 + doorOffset, 0);
        }
    }
}
