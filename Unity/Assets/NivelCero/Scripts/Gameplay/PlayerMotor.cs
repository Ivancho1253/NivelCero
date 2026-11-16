using UnityEngine;
using UnityEngine.InputSystem;

namespace NivelCero
{
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public PlayerCamera View;
        public Transform Visual;
        public float Speed = 6.5f;
        public float JumpHeight = 1.8f;
        public bool DashReady { get { return dashCooldown <= 0 && (controller.isGrounded || airDash); } }
        public Vector3 Velocity { get; private set; }
        private CharacterController controller;
        private float vertical, coyote, jumpBuffer, dashRemaining, dashCooldown, stepCycle;
        private bool airDash = true;
        private Vector3 planar, dashDirection;
        private float standingHeight;
        private Vector3 standingCenter;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            gameObject.layer = 2; // Ignore Raycast: gameplay traces cannot hit their own player.
            standingHeight = controller.height;
            standingCenter = controller.center;
        }

        private void Update()
        {
            if (Session.I == null || Session.I.Blocked || GetComponent<Health>().Dead) return;
            Keyboard keys = Keyboard.current;
            if (keys == null || View == null || View.Lens == null) return;
            float dt = Time.deltaTime;
            Vector2 raw = new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            raw = Vector2.ClampMagnitude(raw, 1);
            Vector3 forward = Vector3.ProjectOnPlane(View.Lens.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 desired = forward * raw.y + right * raw.x;
            bool grounded = controller.isGrounded;
            if (grounded)
            {
                coyote = .12f;
                airDash = true;
                if (vertical < 0) vertical = -2;
            }
            else coyote = Mathf.Max(0, coyote - dt);
            jumpBuffer = Mathf.Max(0, jumpBuffer - dt);
            dashCooldown = Mathf.Max(0, dashCooldown - dt);
            if (keys.spaceKey.wasPressedThisFrame) jumpBuffer = .14f;

            bool crouch = keys.leftCtrlKey.isPressed || keys.rightCtrlKey.isPressed;
            float crouchHeight = Mathf.Max(controller.radius * 2 + .05f, standingHeight * .6f);
            // Test only the new head space; the current capsule and ground are excluded.
            if (!crouch && controller.height < standingHeight - .01f)
            {
                Vector3 lower = transform.position + standingCenter + Vector3.up * (controller.height * .5f - controller.radius);
                Vector3 upper = transform.position + standingCenter + Vector3.up * (standingHeight * .5f - controller.radius);
                crouch = Physics.CheckCapsule(lower, upper, controller.radius * .9f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            }
            controller.height = crouch ? crouchHeight : standingHeight;
            controller.center = standingCenter - Vector3.up * ((standingHeight - controller.height) * .5f);

            if (jumpBuffer > 0 && coyote > 0 && !crouch)
            {
                vertical = Mathf.Sqrt(2 * 24 * JumpHeight);
                coyote = jumpBuffer = 0;
            }
            if ((keys.leftShiftKey.wasPressedThisFrame || keys.rightShiftKey.wasPressedThisFrame) && DashReady)
            {
                dashDirection = desired.sqrMagnitude > .01f ? desired.normalized : forward;
                dashRemaining = .18f;
                dashCooldown = .85f;
                if (!grounded) airDash = false;
                vertical = Mathf.Max(vertical, 0);
            }
            if (dashRemaining > 0)
            {
                planar = dashDirection * 22;
                dashRemaining -= dt;
            }
            else
            {
                planar = Vector3.MoveTowards(planar, desired * Speed * (crouch ? .5f : 1),
                    (desired.sqrMagnitude > 0 ? 42 : 50) * dt);
                vertical = Mathf.Max(-35, vertical - 24 * dt);
            }
            Vector3 before = transform.position;
            CollisionFlags collisions = controller.Move((planar + Vector3.up * vertical) * dt);
            if ((collisions & CollisionFlags.Above) != 0 && vertical > 0) vertical = 0;
            Velocity = dt > 0 ? (transform.position - before) / dt : Vector3.zero;

            if (Visual != null)
            {
                Vector3 facing = desired.sqrMagnitude > .01f ? desired : forward;
                if (Mouse.current != null && Mouse.current.rightButton.isPressed) facing = forward;
                Visual.rotation = Quaternion.Slerp(Visual.rotation, Quaternion.LookRotation(facing), 16 * dt);
                stepCycle += planar.magnitude * dt;
            }
            if (transform.position.y < -18 && Session.I != null) Session.I.PlayerDied();
        }

        public void Respawn(Vector3 position)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            planar = Velocity = Vector3.zero;
            vertical = coyote = jumpBuffer = dashRemaining = dashCooldown = 0;
            airDash = true;
            GetComponent<Health>().Restore();
        }
    }
}
