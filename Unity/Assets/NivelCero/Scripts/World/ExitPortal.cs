using UnityEngine;
using UnityEngine.InputSystem;

namespace NivelCero
{
    public sealed class ExitPortal : MonoBehaviour
    {
        Renderer visual;
        MaterialPropertyBlock properties;
        void Start() { visual = GetComponent<Renderer>(); properties = new MaterialPropertyBlock(); }
        void Update()
        {
            if (visual != null && Session.I != null)
            {
                var color = Session.I.Progress.CanExit ? new Color(.1f, 1, .8f) : new Color(.15f, .06f, .25f);
                properties.SetColor("_BaseColor", color); properties.SetColor("_EmissionColor", color * 2);
                visual.SetPropertyBlock(properties);
            }
            if (Session.I != null && !Session.I.Blocked && Vector3.Distance(transform.position, Session.I.Player.transform.position + Vector3.up) < 2)
                Session.I.Complete();
        }
    }
}
