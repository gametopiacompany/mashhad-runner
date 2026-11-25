using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Shared helpers for building low-cost, simple procedural scenery from Unity primitives.
/// </summary>
public static class ProcKit
{
    private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
    private static Mesh cubeMesh;
    private static Mesh sphereMesh;
    private static Mesh cylinderMesh;

    public static Mesh Cube
    {
        get
        {
            if (cubeMesh == null)
            {
                cubeMesh = ExtractMesh(PrimitiveType.Cube);
            }
            return cubeMesh;
        }
    }

    public static Mesh Sphere
    {
        get
        {
            if (sphereMesh == null)
            {
                sphereMesh = ExtractMesh(PrimitiveType.Sphere);
            }
            return sphereMesh;
        }
    }

    public static Mesh Cylinder
    {
        get
        {
            if (cylinderMesh == null)
            {
                cylinderMesh = ExtractMesh(PrimitiveType.Cylinder);
            }
            return cylinderMesh;
        }
    }

    private static Mesh ExtractMesh(PrimitiveType type)
    {
        var temp = GameObject.CreatePrimitive(type);
        var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(temp);
        return mesh;
    }

    /// <summary>Returns a cached material for the given look. Do not modify the result.</summary>
    public static Material GetMaterial(Color color, float smoothness = 0.12f, float metallic = 0f, float emission = 0f)
    {
        string key = ColorUtility.ToHtmlStringRGBA(color) + "|" + smoothness.ToString("F2") + "|" + metallic.ToString("F2") + "|" + emission.ToString("F2");
        if (MaterialCache.TryGetValue(key, out var cached) && cached != null)
        {
            return cached;
        }

        var material = CreateMaterial(color, smoothness, metallic, emission);
        MaterialCache[key] = material;
        return material;
    }

    /// <summary>Creates a new, uncached material that the caller may animate freely.</summary>
    public static Material CreateMaterial(Color color, float smoothness = 0.12f, float metallic = 0f, float emission = 0f)
    {
        var shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        if (shader == null)
        {
            throw new UnityException("No supported shader is available for procedural scenery.");
        }

        var material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", smoothness);
        }
        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }
        if (emission > 0f && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        return material;
    }

    /// <summary>Creates a collider-free primitive with its own scale, for parts that animate individually.</summary>
    public static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 worldPosition, Vector3 scale, Material material)
    {
        var instance = GameObject.CreatePrimitive(type);
        var collider = instance.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }
        instance.name = name;
        if (parent != null)
        {
            instance.transform.SetParent(parent, false);
        }
        instance.transform.position = worldPosition;
        instance.transform.localScale = scale;
        var meshRenderer = instance.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        return instance;
    }
}

/// <summary>
/// Collects boxes, spheres and cylinders and merges them into one mesh per material, so a whole
/// environment chunk costs a handful of draw calls instead of hundreds.
/// Positions are given in world space; the built object is pivoted at <c>pivot</c>.
/// </summary>
public sealed class MeshBatcher
{
    private readonly Vector3 pivot;
    private readonly bool castShadows;
    private readonly Dictionary<Material, List<CombineInstance>> groups = new Dictionary<Material, List<CombineInstance>>();

    public MeshBatcher(Vector3 pivot, bool castShadows)
    {
        this.pivot = pivot;
        this.castShadows = castShadows;
    }

    public int Count { get; private set; }

    private void Add(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        if (!groups.TryGetValue(material, out var list))
        {
            list = new List<CombineInstance>();
            groups.Add(material, list);
        }

        list.Add(new CombineInstance
        {
            mesh = mesh,
            transform = Matrix4x4.TRS(position - pivot, rotation, scale)
        });
        Count++;
    }

    public void Box(Vector3 center, Vector3 size, Color color, float smoothness = 0.12f, float emission = 0f)
    {
        Add(ProcKit.Cube, center, Quaternion.identity, size, ProcKit.GetMaterial(color, smoothness, 0f, emission));
    }

    public void BoxRot(Vector3 center, Vector3 size, Quaternion rotation, Color color, float smoothness = 0.12f, float emission = 0f)
    {
        Add(ProcKit.Cube, center, rotation, size, ProcKit.GetMaterial(color, smoothness, 0f, emission));
    }

    /// <summary>Ellipsoid; <paramref name="diameters"/> is the full size on each axis.</summary>
    public void Sphere(Vector3 center, Vector3 diameters, Color color, float smoothness = 0.12f, float metallic = 0f, float emission = 0f)
    {
        Add(ProcKit.Sphere, center, Quaternion.identity, diameters, ProcKit.GetMaterial(color, smoothness, metallic, emission));
    }

    /// <summary>Upright cylinder with a true height (Unity's own cylinder is 2 units tall).</summary>
    public void Cylinder(Vector3 center, float radius, float height, Color color, float smoothness = 0.12f, float metallic = 0f, float emission = 0f)
    {
        Add(ProcKit.Cylinder, center, Quaternion.identity, new Vector3(radius * 2f, height * 0.5f, radius * 2f), ProcKit.GetMaterial(color, smoothness, metallic, emission));
    }

    /// <summary>Cylinder whose axis follows <paramref name="rotation"/> (for example Euler(90,0,0) lies along Z).</summary>
    public void CylinderRot(Vector3 center, float radius, float length, Quaternion rotation, Color color, float smoothness = 0.12f, float metallic = 0f, float emission = 0f)
    {
        Add(ProcKit.Cylinder, center, rotation, new Vector3(radius * 2f, length * 0.5f, radius * 2f), ProcKit.GetMaterial(color, smoothness, metallic, emission));
    }

    public GameObject Build(string name)
    {
        var root = new GameObject(name);
        root.transform.position = pivot;
        var owned = root.AddComponent<OwnedMeshes>();

        foreach (var pair in groups)
        {
            var mesh = new Mesh();
            mesh.name = name + " Mesh";
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.CombineMeshes(pair.Value.ToArray(), true, true);
            mesh.RecalculateBounds();
            owned.Meshes.Add(mesh);

            var child = new GameObject("Batch");
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = child.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = pair.Key;
            meshRenderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        groups.Clear();
        return root;
    }
}

/// <summary>
/// 5x7 dot-matrix letters built from boxes, for station and road signs. Text reads left to right
/// when seen from the negative Z side, which is where the runner approaches from.
/// </summary>
public static class DotFont
{
    private static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
    {
        { 'B', new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" } },
        { 'R', new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" } },
        { 'T', new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" } },
        { 'M', new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" } },
        { 'U', new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" } },
        { 'I', new[] { "11111", "00100", "00100", "00100", "00100", "00100", "11111" } },
        { 'N', new[] { "10001", "11001", "10101", "10101", "10011", "10001", "10001" } },
        { 'G', new[] { "01111", "10000", "10000", "10011", "10001", "10001", "01111" } },
        { 'E', new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" } },
        { 'O', new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" } },
        { 'L', new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" } }
    };

    public static float Width(string text, float cell)
    {
        return text.Length * 6f * cell - cell;
    }

    public static float Height(float cell)
    {
        return 7f * cell;
    }

    public static void Draw(MeshBatcher batcher, string text, Vector3 center, float cell, float depth, Color color, float emission)
    {
        float startX = center.x - Width(text, cell) * 0.5f + cell * 0.5f;
        var cellSize = new Vector3(cell * 0.92f, cell * 0.92f, depth);

        for (int i = 0; i < text.Length; i++)
        {
            if (!Glyphs.TryGetValue(text[i], out var rows))
            {
                continue;
            }

            for (int row = 0; row < rows.Length; row++)
            {
                for (int column = 0; column < rows[row].Length; column++)
                {
                    if (rows[row][column] != '1')
                    {
                        continue;
                    }

                    var position = new Vector3(
                        startX + (i * 6 + column) * cell,
                        center.y + (3 - row) * cell,
                        center.z);
                    batcher.Box(position, cellSize, color, 0.1f, emission);
                }
            }
        }
    }
}
