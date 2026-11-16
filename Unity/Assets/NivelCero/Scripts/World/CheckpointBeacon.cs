using UnityEngine;
using UnityEngine.InputSystem;

namespace NivelCero
{
    public sealed class CheckpointBeacon : MonoBehaviour
    {
        public int Id;
        Renderer pad;
        bool lit;
        void Update()
        {
            if (pad == null) pad = GetComponent<Renderer>();
            bool synced = Session.I != null && Session.I.Progress.checkpointId >= Id;
            if (pad != null && synced != lit && SectorBuilder.Cyan != null) { lit = synced; if (lit) pad.sharedMaterial = SectorBuilder.Cyan; }
            if (Session.I != null && !Session.I.Blocked && Vector3.Distance(transform.position, Session.I.Player.transform.position) < 2)
                Session.I.Checkpoint(Id);
        }
    }
}
