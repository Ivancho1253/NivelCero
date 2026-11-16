using UnityEngine;
using UnityEngine.InputSystem;

namespace NivelCero
{
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
            SoundBank.Play("fragment"); gameObject.SetActive(false);
        }
    }
}
