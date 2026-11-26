using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class MashhadMaterials
{
    private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    private static Shader vertexShader;
    private static Material sharedVertexMaterial;
    private static bool vertexChecked;

    /// <summary>The single-material vertex colour shader, or null when it is missing or not supported on this device.</summary>
    public static Shader VertexShader
    {
        get
        {
            if (!vertexChecked)
            {
                vertexChecked = true;
                Shader s = Shader.Find("Mashhad/VertexColorLit");
                if (s != null && s.isSupported) vertexShader = s;
            }
            return vertexShader;
        }
    }

    public static Material SharedVertexMaterial
    {
        get
        {
            if (sharedVertexMaterial == null && VertexShader != null)
            {
                sharedVertexMaterial = new Material(VertexShader);
                sharedVertexMaterial.name = "MR_VertexColorLit";
            }
            return sharedVertexMaterial;
        }
    }

    public static Material Get(string name, Color color, float metallic, float smoothness, float emission)
    {
        Material existing;
        if (cache.TryGetValue(name, out existing) && existing != null) return existing;

        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Diffuse");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var material = new Material(shader);
        material.name = name;
        material.color = color;
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        if (emission > 0f && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        cache[name] = material;
        return material;
    }
}

/// <summary>
/// Collects boxes, cylinders and ellipsoids. Build() merges everything into ONE mesh that uses the shared
/// vertex colour material (one draw call, dynamic-batching friendly). If that shader is unavailable it falls back
/// to one mesh per material. Visual only.
/// </summary>
public sealed class MashhadMeshBatch
{
    private sealed class Part
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<int> triangles = new List<int>();
    }

    private readonly Dictionary<Material, Part> parts = new Dictionary<Material, Part>();
    private Matrix4x4 matrix = Matrix4x4.identity;

    public void SetTransform(Vector3 position, float yawDegrees)
    {
        matrix = Matrix4x4.TRS(position, Quaternion.Euler(0f, yawDegrees, 0f), Vector3.one);
    }

    public void ResetTransform()
    {
        matrix = Matrix4x4.identity;
    }

    private Part GetPart(Material material)
    {
        Part part;
        if (!parts.TryGetValue(material, out part))
        {
            part = new Part();
            parts[material] = part;
        }
        return part;
    }

    public void Tri(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
    {
        Part part = GetPart(material);
        int s = part.vertices.Count;
        part.vertices.Add(matrix.MultiplyPoint3x4(a));
        part.vertices.Add(matrix.MultiplyPoint3x4(b));
        part.vertices.Add(matrix.MultiplyPoint3x4(c));
        bool flip = Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f;
        part.triangles.Add(s);
        part.triangles.Add(flip ? s + 2 : s + 1);
        part.triangles.Add(flip ? s + 1 : s + 2);
    }

    public void Quad(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
    {
        Part part = GetPart(material);
        int s = part.vertices.Count;
        part.vertices.Add(matrix.MultiplyPoint3x4(a));
        part.vertices.Add(matrix.MultiplyPoint3x4(b));
        part.vertices.Add(matrix.MultiplyPoint3x4(c));
        part.vertices.Add(matrix.MultiplyPoint3x4(d));
        bool flip = Vector3.Dot(Vector3.Cross(c - a, d - b), outward) < 0f;
        if (!flip)
        {
            part.triangles.Add(s); part.triangles.Add(s + 1); part.triangles.Add(s + 2);
            part.triangles.Add(s); part.triangles.Add(s + 2); part.triangles.Add(s + 3);
        }
        else
        {
            part.triangles.Add(s); part.triangles.Add(s + 2); part.triangles.Add(s + 1);
            part.triangles.Add(s); part.triangles.Add(s + 3); part.triangles.Add(s + 2);
        }
    }

    public void Box(Material material, Vector3 center, Vector3 size)
    {
        Box(material, center, size, true);
    }

    public void Box(Material material, Vector3 center, Vector3 size, bool bottom)
    {
        if (size.x <= 0.001f || size.y <= 0.001f || size.z <= 0.001f) return;
        Vector3 h = size * 0.5f;
        Vector3 nnn = center + new Vector3(-h.x, -h.y, -h.z);
        Vector3 nnp = center + new Vector3(-h.x, -h.y, h.z);
        Vector3 npn = center + new Vector3(-h.x, h.y, -h.z);
        Vector3 npp = center + new Vector3(-h.x, h.y, h.z);
        Vector3 pnn = center + new Vector3(h.x, -h.y, -h.z);
        Vector3 pnp = center + new Vector3(h.x, -h.y, h.z);
        Vector3 ppn = center + new Vector3(h.x, h.y, -h.z);
        Vector3 ppp = center + new Vector3(h.x, h.y, h.z);
        Quad(material, nnn, npn, ppn, pnn, Vector3.back);
        Quad(material, nnp, pnp, ppp, npp, Vector3.forward);
        Quad(material, nnn, nnp, npp, npn, Vector3.left);
        Quad(material, pnn, ppn, ppp, pnp, Vector3.right);
        Quad(material, npn, npp, ppp, ppn, Vector3.up);
        if (bottom) Quad(material, nnn, pnn, pnp, nnp, Vector3.down);
    }

    public void Cylinder(Material material, Vector3 a, Vector3 b, float radiusA, float radiusB, int sides, bool capTop)
    {
        Vector3 axis = b - a;
        if (axis.sqrMagnitude < 0.000001f || sides < 3) return;
        axis.Normalize();
        Vector3 helper = Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right;
        Vector3 u = Vector3.Cross(axis, helper).normalized;
        Vector3 w = Vector3.Cross(axis, u);
        for (int i = 0; i < sides; i++)
        {
            float a0 = i * Mathf.PI * 2f / sides;
            float a1 = (i + 1) * Mathf.PI * 2f / sides;
            Vector3 d0 = u * Mathf.Cos(a0) + w * Mathf.Sin(a0);
            Vector3 d1 = u * Mathf.Cos(a1) + w * Mathf.Sin(a1);
            Quad(material, a + d0 * radiusA, b + d0 * radiusB, b + d1 * radiusB, a + d1 * radiusA, d0 + d1);
            if (capTop && radiusB > 0.0001f) Tri(material, b, b + d0 * radiusB, b + d1 * radiusB, axis);
        }
    }

    public void Ellipsoid(Material material, Vector3 center, Vector3 radii, int rings, int sides, bool upperOnly)
    {
        float latStart = upperOnly ? 0f : -90f;
        for (int i = 0; i < rings; i++)
        {
            float lat0 = Mathf.Lerp(latStart, 90f, (float)i / rings) * Mathf.Deg2Rad;
            float lat1 = Mathf.Lerp(latStart, 90f, (float)(i + 1) / rings) * Mathf.Deg2Rad;
            for (int j = 0; j < sides; j++)
            {
                float lon0 = j * Mathf.PI * 2f / sides;
                float lon1 = (j + 1) * Mathf.PI * 2f / sides;
                Vector3 p0 = Spherical(center, radii, lat0, lon0);
                Vector3 p1 = Spherical(center, radii, lat1, lon0);
                Vector3 p2 = Spherical(center, radii, lat1, lon1);
                Vector3 p3 = Spherical(center, radii, lat0, lon1);
                Quad(material, p0, p1, p2, p3, (p0 + p1 + p2 + p3) * 0.25f - center);
            }
        }
    }

    private static Vector3 Spherical(Vector3 center, Vector3 radii, float lat, float lon)
    {
        return center + new Vector3(
            Mathf.Cos(lat) * Mathf.Cos(lon) * radii.x,
            Mathf.Sin(lat) * radii.y,
            Mathf.Cos(lat) * Mathf.Sin(lon) * radii.z);
    }

    private static void Describe(Material m, out Color color, out float metallic, out float smooth, out float emission)
    {
        color = m.HasProperty("_Color") ? m.color : Color.white;
        metallic = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
        smooth = m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : 0.3f;
        emission = 0f;
        if (m.HasProperty("_EmissionColor"))
        {
            Color e = m.GetColor("_EmissionColor");
            float em = Mathf.Max(e.r, Mathf.Max(e.g, e.b));
            float cm = Mathf.Max(0.001f, Mathf.Max(color.r, Mathf.Max(color.g, color.b)));
            emission = Mathf.Clamp(em / cm, 0f, 3f);
        }
    }

    public GameObject Build(string name, Transform parent, bool castShadows)
    {
        var root = new GameObject(name);
        if (parent != null) root.transform.SetParent(parent, false);
        var cleanup = root.AddComponent<ShrineMeshCleanup>();

        Material shared = parts.Count > 0 ? MashhadMaterials.SharedVertexMaterial : null;
        if (shared != null)
        {
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            foreach (var pair in parts)
            {
                Color c;
                float metallic;
                float smooth;
                float emission;
                Describe(pair.Key, out c, out metallic, out smooth, out emission);
                Color vertexColor = new Color(c.r, c.g, c.b, 1f - Mathf.Clamp01(emission / 3f));
                Vector2 uv = new Vector2(metallic, smooth);
                int offset = vertices.Count;
                Part part = pair.Value;
                for (int i = 0; i < part.vertices.Count; i++)
                {
                    vertices.Add(part.vertices[i]);
                    colors.Add(vertexColor);
                    uvs.Add(uv);
                }
                for (int i = 0; i < part.triangles.Count; i++) triangles.Add(part.triangles[i] + offset);
            }

            var mesh = new Mesh();
            mesh.name = name;
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            cleanup.Register(mesh);

            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mergedRenderer = root.AddComponent<MeshRenderer>();
            mergedRenderer.sharedMaterial = shared;
            mergedRenderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mergedRenderer.receiveShadows = castShadows;
            parts.Clear();
            return root;
        }

        foreach (var pair in parts)
        {
            var mesh = new Mesh();
            mesh.name = name + "_" + pair.Key.name;
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(pair.Value.vertices);
            mesh.SetTriangles(pair.Value.triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            cleanup.Register(mesh);

            var go = new GameObject(pair.Key.name);
            go.transform.SetParent(root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = pair.Key;
            meshRenderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            meshRenderer.receiveShadows = castShadows;
        }
        parts.Clear();
        return root;
    }
}
