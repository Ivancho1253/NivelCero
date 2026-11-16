using UnityEngine;
using UnityEngine.InputSystem;

namespace NivelCero
{
    /// <summary>FPS / shoulder camera. Rotation is also the movement and aiming reference.</summary>
    public sealed class PlayerCamera : MonoBehaviour
    {
        public Camera Lens;
        public Transform Target;
        public Transform Visual;
        public bool FirstPerson;
        public float Sensitivity = .12f;
        public float Fov = 78;
        public bool InvertY;
        public float EyeHeight = 1.6f;
        private float yaw, pitch, shoulder = 1;

        private void Awake()
        {
            if (Lens == null) Lens = GetComponent<Camera>();
            yaw = transform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (Target == null || Lens == null) return;
            bool blocked = Session.I == null || Session.I.Blocked;
            Keyboard keys = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (!blocked && (Cursor.lockState == CursorLockMode.Locked || Application.isBatchMode))
            {
                if (mouse != null)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    yaw += delta.x * Mathf.Clamp(Sensitivity, .02f, .5f);
                    pitch = Mathf.Clamp(pitch + delta.y * Sensitivity * (InvertY ? 1 : -1), -80, 80);
                }
                if (keys != null && keys.vKey.wasPressedThisFrame) FirstPerson = !FirstPerson;
                if (keys != null && keys.cKey.wasPressedThisFrame) shoulder *= -1;
            }
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            CharacterController character = Target.GetComponent<CharacterController>();
            float eye = character != null ? character.center.y + character.height * .5f - .2f : EyeHeight;
            Vector3 pivot = Target.position + Vector3.up * eye;
            bool aiming = !blocked && mouse != null && mouse.rightButton.isPressed;
            Vector3 offset = FirstPerson ? Vector3.zero : new Vector3(shoulder * .65f, .3f, aiming ? -2.1f : -3.8f);
            Vector3 candidate = pivot + rotation * offset;
            Vector3 travel = candidate - pivot;
            RaycastHit obstruction;
            if (!FirstPerson && travel.sqrMagnitude > .01f && Physics.SphereCast(pivot, .22f, travel.normalized,
                out obstruction, travel.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                candidate = pivot + travel.normalized * Mathf.Max(.05f, obstruction.distance - .06f);
            transform.SetPositionAndRotation(candidate, rotation);
            Lens.fieldOfView = Mathf.Lerp(Lens.fieldOfView, Mathf.Clamp(Fov, 60, 110) - (aiming ? 12 : 0),
                1 - Mathf.Exp(-14 * Time.unscaledDeltaTime));
            if (Visual != null && Visual.gameObject.activeSelf == FirstPerson) Visual.gameObject.SetActive(!FirstPerson);
        }

        public void Recoil(float amount) { pitch = Mathf.Clamp(pitch - amount, -80, 80); }
    }
}
