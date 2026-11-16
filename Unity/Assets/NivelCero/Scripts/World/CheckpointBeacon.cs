using UnityEngine;
using UnityEngine.InputSystem;

namespace NivelCero
{
    public sealed class CheckpointBeacon : MonoBehaviour
    {
        public int Id;
        void Update()
        {
            if (Session.I != null && !Session.I.Blocked && Vector3.Distance(transform.position, Session.I.Player.transform.position) < 2)
                Session.I.Checkpoint(Id);
        }
    }
}
