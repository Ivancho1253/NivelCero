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
}
