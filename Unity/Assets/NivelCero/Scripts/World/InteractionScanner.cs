using UnityEngine;
using UnityEngine.InputSystem;

namespace NivelCero
{
    public sealed class InteractionScanner : MonoBehaviour
    {
        public string Prompt;
        Terminal nearest;
        void Update()
        {
            if (Session.I == null || Session.I.Blocked) { Prompt = ""; return; }
            nearest = null; float distance = 3.5f;
            foreach (var terminal in FindObjectsByType<Terminal>(FindObjectsSortMode.None))
            {
                var delta = terminal.transform.position + Vector3.up - transform.position - Vector3.up;
                var d = delta.magnitude;
                if (d >= distance) continue;
                if (Physics.Raycast(transform.position + Vector3.up, delta.normalized, out var hit, d, ~(1 << 2)) && hit.transform != terminal.transform && !hit.transform.IsChildOf(terminal.transform)) continue;
                nearest = terminal; distance = d;
            }
            Prompt = nearest == null ? "E · HABLAR CON BIT" : "E · " + nearest.Label;
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (nearest != null)
                {
                    if (Time.time < nearest.LockedUntil) Session.I.Notify("Terminal en enfriamiento.");
                    else if (nearest.BossTerminal && !Session.I.Progress.RequiredFragmentsRecovered) Session.I.Notify("Bit: necesitás los dos fragmentos principales; el lateral es opcional.");
                    else Session.I.BeginHack(nearest);
                }
                else Session.I.Notify(Session.I.Progress.RequiredFragmentsRecovered ? "Bit: el Guardián protege la salida. Hackeá ALFA o BETA para bajar el escudo." : "Bit: seguí las luces cian. Los circuitos deben unir ORIGEN y DESTINO; hacé clic para rotar cada módulo.");
            }
        }
    }
}
