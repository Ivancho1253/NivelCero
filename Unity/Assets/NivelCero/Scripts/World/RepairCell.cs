using UnityEngine;

namespace NivelCero
{
    public sealed class RepairCell : MonoBehaviour
    {
        void Update()
        {
            var s = Session.I;
            if (s == null || s.Blocked) return;
            transform.Rotate(0, 45 * Time.deltaTime, 0);
            if (Vector3.Distance(transform.position, s.Player.transform.position + Vector3.up) > 1.5f || s.PlayerHealth.Current >= s.PlayerHealth.Maximum) return;
            s.PlayerHealth.Heal(35); s.Notify("Celda de reparación: +35 de integridad."); Tone.Play(550, .18f); gameObject.SetActive(false);
        }
    }
}
