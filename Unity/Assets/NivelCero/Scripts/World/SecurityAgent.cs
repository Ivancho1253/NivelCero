using UnityEngine;
using UnityEngine.AI;

namespace NivelCero
{
    [RequireComponent(typeof(Health))]
    public sealed class SecurityAgent : MonoBehaviour
    {
        public enum Kind { Drone, Ground, Turret }
        public enum Mode { Patrol, Investigate, Pursue, Telegraph, Attack, Dead }
        public Kind Type;
        public Mode State;
        Vector3 home, lastSeen;
        Health health;
        float clock, lostTime, attackClock;
        NavMeshAgent navigation;
        void Start()
        {
            home = transform.position; health = GetComponent<Health>();
            lostTime = 99;
            if (Type == Kind.Ground)
            {
                navigation = gameObject.AddComponent<NavMeshAgent>();
                navigation.speed = 2.4f; navigation.angularSpeed = 240; navigation.acceleration = 14;
                navigation.radius = .4f; navigation.height = 1.8f; navigation.baseOffset = .5f;
                navigation.stoppingDistance = 1.8f;
                if (NavMesh.SamplePosition(transform.position, out var start, 3, NavMesh.AllAreas)) navigation.Warp(start.position);
            }
            health.Died += () => { State = Mode.Dead; SoundBank.PlayAt("block_impact", transform.position, 1, 4, 40); gameObject.SetActive(false); };
            health.Hurt += (amount, origin) => SoundBank.PlayAt("hit_enemy", transform.position, .9f, 3, 35);
        }
        public void ResetEncounter()
        {
            if (health == null) return;
            health.Restore(); transform.position = home; State = Mode.Patrol; lostTime = 99; attackClock = 1;
            if (navigation != null && navigation.isOnNavMesh) { navigation.Warp(home); navigation.ResetPath(); }
        }
        void Update()
        {
            var s = Session.I;
            if (s == null || s.Blocked || health.Dead) return;
            clock += Time.deltaTime; attackClock -= Time.deltaTime;
            var eye = transform.position + Vector3.up * .8f;
            var target = s.Player.transform.position + Vector3.up;
            var delta = target - eye;
            bool visible = delta.magnitude < 18 && !Physics.Raycast(eye, delta.normalized, delta.magnitude, 1 << 0);
            // Enemy roots use layer 8. Environment is layer 0; player layer 2 is handled separately.
            if (visible) { lastSeen = target; lostTime = 0; } else lostTime += Time.deltaTime;
            if (State == Mode.Telegraph)
            {
                if (navigation != null && navigation.isOnNavMesh) navigation.isStopped = true;
                if (attackClock <= 0)
                {
                    State = Mode.Attack;
                    if (Type == Kind.Ground)
                    {
                        SoundBank.PlayAt("boss_slam", transform.position, .5f, 3, 25);
                        if (delta.magnitude < 2.7f && visible) s.PlayerHealth.Damage(18, transform.position);
                    }
                    else { HostilePulse.Spawn(eye + delta.normalized, lastSeen, 11, 14); SoundBank.PlayAt("enemy_shot", eye, 1, 4, 40); }
                    attackClock = Type == Kind.Turret ? 1.6f : 2.2f;
                }
                return;
            }
            if (visible && delta.magnitude < (Type == Kind.Ground ? 2.3f : 15) && attackClock <= 0)
            {
                State = Mode.Telegraph; attackClock = .65f;
                SoundBank.PlayAt("boss_charge", eye, .55f, 3, 30, .05f); return;
            }
            State = visible ? Mode.Pursue : lostTime < 3 ? Mode.Investigate : Mode.Patrol;
            if (Type == Kind.Turret) return;
            var goal = State == Mode.Patrol ? home + new Vector3(Mathf.Sin(clock * .5f) * 2, 0, 0) : lastSeen;
            if (navigation != null && navigation.isOnNavMesh)
            {
                navigation.isStopped = false;
                if (NavMesh.SamplePosition(goal, out var reachable, 3, NavMesh.AllAreas)) navigation.SetDestination(reachable.position);
                return;
            }
            var direction = goal - transform.position; direction.y = 0;
            if (direction.magnitude < (Type == Kind.Drone && visible ? 7 : .4f)) return;
            direction.Normalize();
            var move = direction * (Type == Kind.Drone ? 3 : 2.4f) * Time.deltaTime;
            if (!Physics.SphereCast(transform.position, .6f, direction, out _, move.magnitude + .2f, 1 << 0)) transform.position += move;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5);
        }
    }
    public sealed class HostilePulse : MonoBehaviour
    {
        Vector3 velocity;
        float damage, lifetime = 6;
        public static void Spawn(Vector3 origin, Vector3 target, float speed, float damage)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "Pulso hostil";
            go.transform.position = origin; go.transform.localScale = Vector3.one * .3f;
            go.layer = 2; Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = SectorBuilder.Red;
            var pulse = go.AddComponent<HostilePulse>(); pulse.velocity = (target - origin).normalized * speed; pulse.damage = damage;
        }
        void Update()
        {
            if (Session.I == null || Session.I.Blocked) return;
            var displacement = velocity * Time.deltaTime;
            if (Physics.SphereCast(transform.position, .17f, velocity.normalized, out var hit, displacement.magnitude, (1 << 0) | (1 << 2), QueryTriggerInteraction.Ignore))
            {
                var health = hit.collider.GetComponentInParent<Health>();
                if (health != null && health == Session.I.PlayerHealth) health.Damage(damage, transform.position);
                Destroy(gameObject); return;
            }
            transform.position += displacement; lifetime -= Time.deltaTime;
            if (lifetime <= 0 || transform.position.y < -10) Destroy(gameObject);
        }
    }
}
