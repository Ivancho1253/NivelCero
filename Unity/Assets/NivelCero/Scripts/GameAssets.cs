using UnityEngine;

namespace NivelCero
{
    [CreateAssetMenu(menuName = "Nivel Cero/Recursos del juego")]
    public sealed class GameAssets : ScriptableObject
    {
        public GameObject Zero, PulseBlaster, EnergyBlade, Guardian, Bit, Terminal, WallPanel, FloorPanel, Pillar, Pipe, Crate;
        public RuntimeAnimatorController ZeroAnimator;
        public Avatar ZeroAvatar;
        public static GameAssets Load() { return Resources.Load<GameAssets>("NivelCeroAssets"); }
        public static GameObject InstantiateVisual(GameObject prefab, Transform parent, string name)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab, parent); go.name = name;
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
            return go;
        }
    }
}
