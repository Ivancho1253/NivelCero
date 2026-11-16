using UnityEngine;
using UnityEngine.InputSystem;

namespace NivelCero
{
    public sealed class Terminal : MonoBehaviour
    {
        public string Label = "ACCESO RESTRINGIDO";
        public int Seed = 42, Size = 4;
        public float TimeLimit = 90, LockedUntil;
        public bool BossTerminal;
        public void Succeed()
        {
            if (BossTerminal) { Session.I.Boss.ExposeCore(); Session.I.Notify("Bit: escudo caído. Atacá el núcleo durante 18 segundos."); }
            else { Session.I.Progress.accessUnlocked = true; Session.I.AccessDoor.SetActive(false); Session.I.Save(); Session.I.Notify("Ruta desbloqueada. Recuperá el segundo fragmento principal."); }
        }
    }
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
    public sealed class CodeShard : MonoBehaviour
    {
        public string Id;
        void Update()
        {
            if (Session.I == null || Session.I.Blocked) return;
            transform.Rotate(0, 65 * Time.deltaTime, 0);
            if (Vector3.Distance(transform.position, Session.I.Player.transform.position + Vector3.up) > 1.7f) return;
            Session.I.Progress.Collect(Id); Session.I.Save();
            Session.I.Notify(Id == "fragment-optional" ? "Archivo opcional: GL1TCH fue una herramienta de reparación. ¿Qué lo corrompió?" : "Fragmento de Código recuperado.");
            Tone.Play(950, .2f); gameObject.SetActive(false);
        }
    }
    public sealed class CheckpointBeacon : MonoBehaviour
    {
        public int Id;
        void Update()
        {
            if (Session.I != null && !Session.I.Blocked && Vector3.Distance(transform.position, Session.I.Player.transform.position) < 2)
                Session.I.Checkpoint(Id);
        }
    }
    public sealed class ExitPortal : MonoBehaviour
    {
        void Update()
        {
            if (Session.I != null && !Session.I.Blocked && Vector3.Distance(transform.position, Session.I.Player.transform.position + Vector3.up) < 2)
                Session.I.Complete();
        }
    }
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
    public static class Tone
    {
        public static void Play(float frequency, float length, Vector3? position = null)
        {
            var go = new GameObject("Audio sintético original");
            if (position.HasValue) go.transform.position = position.Value;
            var source = go.AddComponent<AudioSource>();
            int n = (int)(44100 * length); var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / 44100) * .08f * (1 - (float)i / n);
            var clip = AudioClip.Create("Pulso", n, 1, 44100, false); clip.SetData(data, 0);
            source.clip = clip; source.spatialBlend = position.HasValue ? 1 : 0; source.Play();
            Object.Destroy(go, length + .1f); Object.Destroy(clip, length + .2f);
        }
    }
}
