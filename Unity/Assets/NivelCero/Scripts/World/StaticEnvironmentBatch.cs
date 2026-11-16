using System.Collections.Generic;
using UnityEngine;

namespace NivelCero
{
    public sealed class StaticEnvironmentBatch : MonoBehaviour
    {
        void Start()
        {
            var objects = new List<GameObject>();
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.GetComponent<MeshFilter>() == null || renderer.GetComponentInParent<CodeShard>() != null ||
                    renderer.GetComponentInParent<BitFollower>() != null || renderer.GetComponentInParent<Guardian>() != null) continue;
                if (renderer.gameObject.activeInHierarchy) objects.Add(renderer.gameObject);
            }
            if (objects.Count > 0) StaticBatchingUtility.Combine(objects.ToArray(), gameObject);
        }
    }
}
