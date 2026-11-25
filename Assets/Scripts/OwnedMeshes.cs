using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of meshes created at runtime for one environment chunk and destroys them together
/// with the chunk, so long runs do not leak combined meshes.
/// </summary>
public sealed class OwnedMeshes : MonoBehaviour
{
    public readonly List<Mesh> Meshes = new List<Mesh>();

    private void OnDestroy()
    {
        foreach (var mesh in Meshes)
        {
            if (mesh != null)
            {
                Destroy(mesh);
            }
        }
        Meshes.Clear();
    }
}
