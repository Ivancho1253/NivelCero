using UnityEngine;

namespace NivelCero
{
    public sealed class DashTrail : MonoBehaviour
    {
        PlayerMotor motor;
        TrailRenderer trail;
        void Start()
        {
            motor = GetComponent<PlayerMotor>();
            var anchor = new GameObject("Estela de dash"); anchor.transform.SetParent(transform, false); anchor.transform.localPosition = Vector3.up;
            trail = anchor.AddComponent<TrailRenderer>(); trail.sharedMaterial = SectorBuilder.Cyan;
            trail.time = .22f; trail.startWidth = .6f; trail.endWidth = .01f; trail.minVertexDistance = .08f;
            trail.startColor = new Color(.2f, 1, 1, .5f); trail.endColor = new Color(.05f, .3f, .6f, 0);
            trail.emitting = false;
        }
        void LateUpdate() { trail.emitting = motor.Velocity.magnitude > 15 && Session.I != null && !Session.I.Blocked; }
    }
}
