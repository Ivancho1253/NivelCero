using UnityEngine;
using UnityEngine.InputSystem;

namespace NivelCero
{
    public sealed class BitFollower : MonoBehaviour
    {
        void Update()
        {
            var s = Session.I;
            if (s == null || s.Player == null) return;
            var p = s.Player.transform;
            var target = p.position + p.right * 1.6f + Vector3.up * (2.3f + Mathf.Sin(Time.time * 2) * .12f);
            transform.position = Vector3.Lerp(transform.position, target, 1 - Mathf.Exp(-4 * Time.deltaTime));
        }
    }
}
