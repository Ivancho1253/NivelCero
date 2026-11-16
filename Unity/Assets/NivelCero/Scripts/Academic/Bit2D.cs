using UnityEngine;

namespace NivelCero
{
    /// <summary>
    /// Bit, the companion NPC of the 2D scene (AE4: GameObject + SpriteRenderer +
    /// Collider2D + Rigidbody2D + autonomous C# behaviour + interaction).
    ///
    /// Physics: a Kinematic Rigidbody2D moved with MovePosition and a small trigger
    /// CircleCollider2D for its body, so it never pushes or blocks Zero. A child
    /// trigger ("zona de interacción") reports through OnTrigger events when Zero
    /// can talk to it. Bit and Zero share layer 2, which ground checks and shots ignore.
    ///
    /// Autonomy (state machine): Follow → Idle (orbits, looks around, blinks when
    /// Zero stops) → Guide (flies to an uncollected fragment or the locked terminal,
    /// pings it and returns) → Combat (keeps behind Zero, away from the Guardian,
    /// warns when Zero stands in a marked area) plus short reactions (celebrate,
    /// worry, alert). Talking (E) plays its voice and a contextual hint.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class Bit2D : MonoBehaviour
    {
        public enum Mode { Follow, Idle, Guide, Combat }
        public enum Reaction { None, Celebrate, Worry, Alert, Talk }
        public Mode State { get; private set; }
        public Reaction Mood { get; private set; }
        public bool PlayerInRange { get; private set; }
        public int PortraitMood { get { return Mood == Reaction.Celebrate || Mood == Reaction.Talk ? 1 : Mood == Reaction.Alert ? 2 : Mood == Reaction.Worry ? 3 : 0; } }
        public Rigidbody2D Body { get { return body; } }

        Academic2D owner;
        Academic2DArt art;
        Rigidbody2D body;
        CircleCollider2D shell;
        SpriteRenderer sprite, ping;
        SpriteAnimator2D anim, pingAnim;
        Vector2 velocity, guideTarget;
        float stillTime, reactionTime, blinkClock = 2, guideClock, warnCooldown, orbit;
        readonly bool[] guidedShard = new bool[3];
        bool guidedTerminal;

        public void Init(Academic2D game, Academic2DArt sheets)
        {
            owner = game; art = sheets;
            gameObject.layer = 2;
            sprite = GetComponent<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.useFullKinematicContacts = true;
            shell = GetComponent<CircleCollider2D>();
            shell.isTrigger = true; shell.radius = .35f;
            var zone = new GameObject("Zona de interacción de Bit");
            zone.transform.SetParent(transform, false); zone.layer = 2;
            var zoneCollider = zone.AddComponent<CircleCollider2D>();
            zoneCollider.isTrigger = true; zoneCollider.radius = 2.4f;
            zone.AddComponent<Bit2DZone>().Owner = this;
            anim = gameObject.AddComponent<SpriteAnimator2D>();
            anim.Play("idle", art.Bit["idle"], 6);
            var pingObject = new GameObject("Señal de Bit");
            pingObject.transform.SetParent(owner.transform, false);
            ping = pingObject.AddComponent<SpriteRenderer>(); ping.sortingOrder = 13; ping.enabled = false;
            pingAnim = pingObject.AddComponent<SpriteAnimator2D>();
        }

        internal void ZoneEnter(Collider2D other) { if (IsZero(other)) PlayerInRange = true; }
        internal void ZoneExit(Collider2D other) { if (IsZero(other)) PlayerInRange = false; }
        bool IsZero(Collider2D other) { return owner != null && other.attachedRigidbody != null && other.attachedRigidbody.gameObject.name == "Zero 2D"; }

        public void Celebrate() { React(Reaction.Celebrate, 1.4f); SoundBank.Play("bit_chirp", .7f, 1.15f, .05f, .3f); }
        public void Worry() { React(Reaction.Worry, 1.2f); }
        public void Alert() { React(Reaction.Alert, 1.6f); SoundBank.Play("bit_chirp", .8f, 1.3f, .05f, .3f); }
        public void Talk()
        {
            React(Reaction.Talk, 2.2f);
            owner.BitSays(owner.BitAdvice());
        }

        void React(Reaction reaction, float seconds) { Mood = reaction; reactionTime = seconds; }

        void Update()
        {
            if (owner == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            reactionTime -= dt; warnCooldown -= dt; blinkClock -= dt;
            if (reactionTime <= 0) Mood = Reaction.None;
            Vector2 zero = owner.PlayerPosition;
            stillTime = owner.PlayerVelocity.sqrMagnitude < .2f ? stillTime + dt : 0;

            // Choose a behaviour.
            bool combat = owner.Boss != Academic2D.BossState.Dormant && owner.Boss != Academic2D.BossState.Dead && owner.BossHealth > 0;
            if (combat) State = Mode.Combat;
            else if (State == Mode.Guide)
            {
                guideClock -= dt;
                if (guideClock <= 0 || Vector2.Distance(zero, guideTarget) < 2.2f) State = Mode.Follow;
            }
            else
            {
                Vector2 poi;
                if (FindPointOfInterest(zero, out poi)) { State = Mode.Guide; guideTarget = poi; guideClock = 2.6f; ShowPing(poi); SoundBank.Play("bit_chirp", .6f, 1.2f, .05f, .5f); }
                else State = stillTime > 2.5f ? Mode.Idle : Mode.Follow;
            }

            if (combat && warnCooldown <= 0 && !owner.IsPositionSafe(zero.x))
            {
                warnCooldown = 1.5f; React(Reaction.Alert, .8f);
                SoundBank.Play("bit_chirp", .7f, 1.4f, .03f, .3f);
            }

            // Animation follows mood first, then mode.
            switch (Mood)
            {
                case Reaction.Celebrate: case Reaction.Talk: anim.Play("talk", art.Bit["talk"], 8); break;
                case Reaction.Alert: anim.Play("alert", art.Bit["alert"], 6); break;
                case Reaction.Worry: anim.Play("worried", art.Bit["worried"], 5); break;
                default:
                    if (State == Mode.Guide) anim.Play("alert", art.Bit["alert"], 5);
                    else if (State == Mode.Combat) anim.Play("worried", art.Bit["worried"], 4);
                    else if (State == Mode.Idle) anim.Play("look", art.Bit["look"], 1.5f);
                    else if (blinkClock <= 0) { anim.Play("blink", art.Bit["blink"], 10, false, true); blinkClock = Random.Range(2.5f, 4.5f); }
                    else if (anim.Current != "blink" || anim.Finished) anim.Play("idle", art.Bit["idle"], 6);
                    break;
            }
            sprite.flipX = (guideTarget.x < transform.position.x && State == Mode.Guide) || (State != Mode.Guide && owner.Facing < 0);
            if (ping.enabled && (State != Mode.Guide)) ping.enabled = false;
        }

        void FixedUpdate()
        {
            if (owner == null || Time.fixedDeltaTime <= 0 || Time.timeScale <= 0) return;
            Vector2 zero = owner.PlayerPosition;
            float bob = Mathf.Sin(Time.time * 3.1f) * .14f;
            Vector2 target;
            float speed = 4.5f;
            switch (State)
            {
                case Mode.Guide: target = guideTarget + new Vector2(0, 1.3f + bob); speed = 7; break;
                case Mode.Idle:
                    orbit += Time.fixedDeltaTime * .9f;
                    target = zero + new Vector2(Mathf.Cos(orbit) * 1.4f, 1.7f + Mathf.Sin(orbit * 2) * .25f); speed = 3; break;
                case Mode.Combat:
                    float away = Mathf.Sign(zero.x - owner.GuardianPosition.x);
                    if (away == 0) away = -1;
                    target = zero + new Vector2(away * 1.6f, 2.3f + bob); speed = 6; break;
                default: target = zero + new Vector2(-owner.Facing * 1.3f, 1.6f + bob); break;
            }
            if (Mood == Reaction.Celebrate) target += new Vector2(Mathf.Cos(Time.time * 9) * .35f, Mathf.Sin(Time.time * 9) * .35f);
            // Keep clear of walls/floor: never park inside solid tiles.
            var hit = Physics2D.Raycast(target + Vector2.up * 2, Vector2.down, 2.4f, ~(1 << 2));
            if (hit.collider != null && !hit.collider.isTrigger) target.y = Mathf.Max(target.y, hit.point.y + .8f);
            Vector2 next = Vector2.SmoothDamp(body.position, target, ref velocity, .28f, speed * 2.2f, Time.fixedDeltaTime);
            body.MovePosition(next);
        }

        bool FindPointOfInterest(Vector2 zero, out Vector2 poi)
        {
            poi = Vector2.zero;
            if (stillTime > 0 && stillTime < .3f) return false;
            Vector2[] shardPositions = { new Vector2(25, 2), new Vector2(59, 2), new Vector2(42, 5.3f) };
            for (int i = 0; i < shardPositions.Length; i++)
            {
                if (guidedShard[i] || owner.HasFragment(i)) continue;
                float dx = shardPositions[i].x - zero.x;
                if (dx > 1.5f && dx < 9) { guidedShard[i] = true; poi = shardPositions[i]; return true; }
            }
            if (!guidedTerminal && !owner.GateUnlocked && owner.RequiredFragments >= 1)
            {
                float dx = owner.TerminalPosition.x - zero.x;
                if (dx > 1.5f && dx < 9) { guidedTerminal = true; poi = owner.TerminalPosition + Vector2.up; return true; }
            }
            return false;
        }

        void ShowPing(Vector2 at)
        {
            ping.enabled = true;
            ping.transform.position = at + Vector2.up * .2f;
            ping.color = new Color(.31f, .85f, .88f, 1);
            pingAnim.Play("ping", art.Fx["hitCyan"], 6, true, true);
        }
    }

    /// <summary>Forwards the interaction trigger of Bit's child collider.</summary>
    public sealed class Bit2DZone : MonoBehaviour
    {
        public Bit2D Owner;
        void OnTriggerEnter2D(Collider2D other) { if (Owner != null) Owner.ZoneEnter(other); }
        void OnTriggerExit2D(Collider2D other) { if (Owner != null) Owner.ZoneExit(other); }
    }
}
