using System.Collections.Generic;
using UnityEngine;

/// <summary>Destroys runtime-combined meshes together with the object that owns them.</summary>
public sealed class ShrineMeshCleanup : MonoBehaviour
{
    private readonly List<Mesh> meshes = new List<Mesh>();

    public void Register(Mesh mesh)
    {
        if (mesh != null) meshes.Add(mesh);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < meshes.Count; i++)
        {
            if (meshes[i] != null) Destroy(meshes[i]);
        }
        meshes.Clear();
    }
}
