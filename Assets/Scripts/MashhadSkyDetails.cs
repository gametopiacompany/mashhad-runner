using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Replaces the horizon shrine silhouette with a richer one (ribbed dome, tiled iwan, banded minarets, arcade with
/// lit arches) and adds a flock of pigeons circling its dome. Attaches to the sky built by MashhadSkyEnvironment.
/// Visual only.
/// </summary>
public sealed class MashhadSkyDetails : MonoBehaviour
{
    private const float ShrineAngle = Mathf.PI * 0.5f;
    private const float ShrineRadius = 265f;
    private const float Scale = 0.7f;
    private const int BirdCount = 16;

    private static readonly Color Haze = new Color(0.93f, 0.64f, 0.44f, 1f);
    private static readonly Color Dark = new Color(0.20f, 0.12f, 0.16f, 1f);
    private static readonly Color TileC = new Color(0.08f, 0.30f, 0.34f, 1f);
    private static readonly Color TileLight = new Color(0.12f, 0.46f, 0.50f, 1f);
    private static readonly Color GoldTop = new Color(1f, 0.76f, 0.34f, 1f);
    private static readonly Color GoldDark = new Color(0.52f, 0.32f, 0.18f, 1f);
    private static readonly Color RibColor = new Color(0.40f, 0.24f, 0.13f, 1f);
    private static readonly Color Warm = new Color(1f, 0.78f, 0.42f, 1f);
    private static readonly Color WarmDim = new Color(0.85f, 0.55f, 0.28f, 1f);
    private static readonly Color Cream = new Color(0.98f, 0.94f, 0.88f, 1f);

    private sealed class MeshBuilder
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<Color> colors = new List<Color>();
        public readonly List<int> triangles = new List<int>();

        public int Add(Vector3 position, Color color)
        {
            vertices.Add(position);
            colors.Add(color);
            return vertices.Count - 1;
        }

        public void Triangle(int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
        {
            int i = Add(a, ca);
            Add(b, cb);
            Add(c, cc);
            Triangle(i, i + 1, i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color ca, Color cb, Color cc, Color cd)
        {
            int i = Add(a, ca);
            Add(b, cb);
            Add(c, cc);
            Add(d, cd);
            Triangle(i, i + 1, i + 2);
            Triangle(i, i + 2, i + 3);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh();
            mesh.name = name;
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<Material> materials = new List<Material>();
    private Transform skyRoot;
    private Transform[] birds;
    private Transform[] wingsL;
    private Transform[] wingsR;
    private float[] phases;
    private float[] speeds;
    private float[] radii;
    private float[] heights;
    private Vector3 origin;
    private Vector3 tangent;
    private Vector3 inward;
    private float flockTime;
    private float nextCheck;
    private bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<MashhadSkyDetails>() != null) return;
        var host = new GameObject("MR_Sky_Details");
        DontDestroyOnLoad(host);
        host.AddComponent<MashhadSkyDetails>();
    }

    private void Update()
    {
        if (!built)
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.5f;
            TryBuild();
            return;
        }

        if (skyRoot == null)
        {
            Cleanup();
            built = false;
            return;
        }
        if (!skyRoot.gameObject.activeInHierarchy) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        flockTime += dt;
        AnimateFlock();
    }

    private void TryBuild()
    {
        GameObject host = GameObject.Find("MR_Sky_Environment");
        if (host == null) return;
        Transform sr = host.transform.Find("MR_Sky_Root");
        if (sr == null) return;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            enabled = false;
            return;
        }

        origin = new Vector3(Mathf.Cos(ShrineAngle) * ShrineRadius, 0f, Mathf.Sin(ShrineAngle) * ShrineRadius);
        tangent = new Vector3(-Mathf.Sin(ShrineAngle), 0f, Mathf.Cos(ShrineAngle));
        inward = new Vector3(-origin.x, 0f, -origin.z).normalized;

        skyRoot = sr;
        Transform old = sr.Find("Sky_Shrine");
        if (old != null) old.gameObject.SetActive(false);

        CreatePart("Sky_Shrine_Detail", BuildShrine(), CreateMaterial(shader, "MR_Sky_Shrine_Detail", 1008), sr);
        BuildFlock(shader, sr);
        built = true;
    }

    private Vector3 P(float x, float y)
    {
        return origin + tangent * (x * Scale) + Vector3.up * (y * Scale);
    }

    private void Rect(MeshBuilder m, float x0, float y0, float x1, float y1, Color bottom, Color top)
    {
        m.Quad(P(x0, y0), P(x0, y1), P(x1, y1), P(x1, y0), bottom, top, top, bottom);
    }

    private void Poly(MeshBuilder m, Color color, float[] xy)
    {
        int n = xy.Length / 2;
        float cx = 0f;
        float cy = 0f;
        for (int i = 0; i < n; i++)
        {
            cx += xy[i * 2];
            cy += xy[i * 2 + 1];
        }
        cx /= n;
        cy /= n;
        int ci = m.Add(P(cx, cy), color);
        int first = -1;
        for (int i = 0; i < n; i++)
        {
            int vi = m.Add(P(xy[i * 2], xy[i * 2 + 1]), color);
            if (first < 0) first = vi;
        }
        for (int i = 0; i < n; i++) m.Triangle(ci, first + i, first + (i + 1) % n);
    }

    private void Dome(MeshBuilder m, float cx, float cy, float rx, float ry, Color dark, Color bright)
    {
        int ci = m.Add(P(cx, cy), dark);
        int first = -1;
        for (int k = 0; k <= 24; k++)
        {
            float theta = Mathf.PI * k / 24f;
            Color c = Color.Lerp(dark, bright, Mathf.Sin(theta));
            int vi = m.Add(P(cx + Mathf.Cos(theta) * rx, cy + Mathf.Sin(theta) * ry), c);
            if (first < 0) first = vi;
        }
        for (int k = 0; k < 24; k++) m.Triangle(ci, first + k, first + k + 1);
    }

    private void Disc(MeshBuilder m, float cx, float cy, float rx, float ry, Color center, Color edge, int points)
    {
        int ci = m.Add(P(cx, cy), center);
        int first = -1;
        for (int k = 0; k < points; k++)
        {
            float theta = k * Mathf.PI * 2f / points;
            int vi = m.Add(P(cx + Mathf.Cos(theta) * rx, cy + Mathf.Sin(theta) * ry), edge);
            if (first < 0) first = vi;
        }
        for (int k = 0; k < points; k++) m.Triangle(ci, first + k, first + (k + 1) % points);
    }

    private void Meridian(MeshBuilder m, float cx, float cy, float rx, float ry, float phi, Color color)
    {
        for (int s = 0; s < 12; s++)
        {
            float a0 = Mathf.PI * 0.5f * s / 12f;
            float a1 = Mathf.PI * 0.5f * (s + 1) / 12f;
            float x0 = cx + rx * Mathf.Sin(a0) * Mathf.Sin(phi);
            float y0 = cy + ry * Mathf.Cos(a0);
            float x1 = cx + rx * Mathf.Sin(a1) * Mathf.Sin(phi);
            float y1 = cy + ry * Mathf.Cos(a1);
            m.Quad(P(x0 - 0.3f, y0), P(x0 + 0.3f, y0), P(x1 + 0.3f, y1), P(x1 - 0.3f, y1), color, color, color, color);
        }
    }

    private void Minaret(MeshBuilder m, float x)
    {
        Rect(m, x - 2.8f, -20f, x + 2.8f, 50f, Haze, TileC);
        for (float y = 2f; y < 46f; y += 11f)
        {
            Rect(m, x - 2.9f, y, x + 2.9f, y + 1.1f, GoldDark, GoldTop);
            Rect(m, x - 0.5f, y + 4f, x + 0.5f, y + 7f, Warm, Warm);
        }
        Rect(m, x - 5f, 50f, x + 5f, 52f, GoldDark, GoldTop);
        Rect(m, x - 2.2f, 52f, x + 2.2f, 60f, TileC, TileLight);
        Rect(m, x - 3.6f, 60f, x + 3.6f, 61.6f, GoldDark, GoldTop);
        Rect(m, x - 1.6f, 61.6f, x + 1.6f, 66f, TileC, TileLight);
        Rect(m, x - 0.6f, 62.6f, x + 0.6f, 65f, Warm, Warm);
        m.Tri(P(x - 2.4f, 66f), P(x, 73f), P(x + 2.4f, 66f), GoldDark, GoldTop, GoldDark);
        Disc(m, x, 73.8f, 0.9f, 0.9f, GoldTop, GoldTop, 8);
        Rect(m, x - 0.25f, 74f, x + 0.25f, 80f, GoldTop, GoldTop);
    }

    private Mesh BuildShrine()
    {
        var m = new MeshBuilder();
        Disc(m, 0f, 52f, 150f, 120f, new Color(1f, 0.82f, 0.5f, 0.5f), new Color(1f, 0.82f, 0.5f, 0f), 32);

        Rect(m, -100f, -20f, 100f, 6f, Haze, Dark);
        Rect(m, -32f, -20f, 32f, 38f, Haze, Dark);
        for (int sx = -1; sx <= 1; sx += 2)
        {
            Rect(m, sx * 32f, -20f, sx * 60f, 26f, Haze, Dark);
            for (int row = 0; row < 2; row++)
            {
                float y = 8f + row * 8f;
                for (int i = 0; i < 6; i++)
                {
                    float x = sx * (36f + i * 4.2f);
                    Rect(m, x - 0.8f, y, x + 0.8f, y + 4f, Warm, Warm);
                }
            }
            float dx = sx * 46f;
            Rect(m, dx - 6f, 26f, dx + 6f, 31f, TileC, TileC);
            Dome(m, dx, 31f, 8f, 9f, GoldDark, GoldTop);
            Rect(m, dx - 0.25f, 40f, dx + 0.25f, 44f, GoldTop, GoldTop);
        }

        Poly(m, GoldTop, new float[] { -12.5f, 6f, -12.5f, 30f, 0f, 40f, 12.5f, 30f, 12.5f, 6f });
        Poly(m, TileC, new float[] { -10.5f, 6f, -10.5f, 29.5f, 0f, 38.5f, 10.5f, 29.5f, 10.5f, 6f });
        Poly(m, Dark, new float[] { -7f, 8f, -7f, 28f, 0f, 34f, 7f, 28f, 7f, 8f });
        Rect(m, -7f, 8f, 7f, 12f, Warm, Warm);
        for (int i = -2; i <= 2; i++)
        {
            Rect(m, i * 2.6f - 0.7f, 24f, i * 2.6f + 0.7f, 27f, GoldDark, GoldTop);
        }

        Rect(m, -15f, 38f, 15f, 50f, TileC, TileLight);
        for (int i = -3; i <= 3; i++)
        {
            Rect(m, i * 3.6f - 0.8f, 41f, i * 3.6f + 0.8f, 47f, Warm, WarmDim);
        }
        Rect(m, -15.5f, 50f, 15.5f, 51.5f, GoldDark, GoldTop);
        Dome(m, 0f, 51.5f, 17f, 19f, GoldDark, GoldTop);
        float[] ribs = { -1.1f, -0.7f, -0.3f, 0f, 0.3f, 0.7f, 1.1f };
        for (int i = 0; i < ribs.Length; i++) Meridian(m, 0f, 51.5f, 17f, 19f, ribs[i], RibColor);
        Rect(m, -1.6f, 70.5f, 1.6f, 72f, GoldDark, GoldTop);
        Disc(m, 0f, 73.2f, 1.3f, 1.3f, GoldTop, GoldTop, 10);
        Rect(m, -0.3f, 74f, 0.3f, 79f, GoldTop, GoldTop);

        Minaret(m, -72f);
        Minaret(m, 72f);

        for (int i = -9; i <= 9; i++)
        {
            float x = i * 9.5f;
            Rect(m, x - 2f, -4f, x + 2f, 2.5f, WarmDim, Warm);
            Dome(m, x, 2.5f, 2f, 2.5f, WarmDim, Warm);
        }
        return Register(m.ToMesh("MR_Sky_Shrine_Detail"));
    }

    private void BuildFlock(Shader shader, Transform parent)
    {
        Mesh bodyMesh = BuildBirdBody();
        Mesh wingMesh = BuildBirdWing();
        Material material = CreateMaterial(shader, "MR_Sky_Birds", 1009);

        birds = new Transform[BirdCount];
        wingsL = new Transform[BirdCount];
        wingsR = new Transform[BirdCount];
        phases = new float[BirdCount];
        speeds = new float[BirdCount];
        radii = new float[BirdCount];
        heights = new float[BirdCount];
        for (int i = 0; i < BirdCount; i++)
        {
            var bird = new GameObject("Sky_Pigeon").transform;
            bird.SetParent(parent, false);
            CreatePart("Body", bodyMesh, material, bird);
            wingsL[i] = CreatePart("WingL", wingMesh, material, bird);
            wingsR[i] = CreatePart("WingR", wingMesh, material, bird);
            wingsR[i].localScale = new Vector3(-1f, 1f, 1f);
            birds[i] = bird;
            phases[i] = i * 0.7f;
            speeds[i] = 0.16f + (i % 4) * 0.03f;
            radii[i] = 35f + (i % 5) * 9f;
            heights[i] = 6f + (i % 4) * 6f;
        }
        AnimateFlock();
    }

    private void AnimateFlock()
    {
        if (birds == null) return;
        Vector3 center = P(0f, 62f);
        for (int i = 0; i < birds.Length; i++)
        {
            if (birds[i] == null) continue;
            float a = phases[i] + flockTime * speeds[i];
            Vector3 pos = center
                + tangent * (Mathf.Cos(a) * radii[i])
                + inward * (Mathf.Sin(a) * 25f)
                + Vector3.up * (heights[i] + Mathf.Sin(a * 2f) * 3f);
            Vector3 velocity = tangent * (-Mathf.Sin(a) * radii[i]) + inward * (Mathf.Cos(a) * 25f);
            birds[i].localPosition = pos;
            if (velocity.sqrMagnitude > 0.001f)
            {
                birds[i].localRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            }
            float flap = 22f + Mathf.Sin(flockTime * (8f + i % 3) + phases[i]) * 38f;
            wingsL[i].localRotation = Quaternion.Euler(0f, 0f, -flap);
            wingsR[i].localRotation = Quaternion.Euler(0f, 0f, flap);
        }
    }

    private Mesh BuildBirdBody()
    {
        var m = new MeshBuilder();
        Color tail = new Color(0.85f, 0.82f, 0.78f, 1f);
        m.Tri(new Vector3(-0.5f, 0f, -1.3f), new Vector3(0.5f, 0f, -1.3f), new Vector3(0f, 0f, 1.5f), Cream, Cream, Cream);
        m.Tri(new Vector3(-0.6f, 0f, -1.3f), new Vector3(0.6f, 0f, -1.3f), new Vector3(0f, 0f, -2.4f), tail, tail, tail);
        m.Quad(new Vector3(0f, 0f, 1.5f), new Vector3(0f, 0.55f, 0.3f), new Vector3(0f, 0.35f, -1.4f), new Vector3(0f, -0.45f, -0.6f), Cream, Cream, tail, tail);
        return Register(m.ToMesh("MR_Sky_Bird_Body"));
    }

    private Mesh BuildBirdWing()
    {
        var m = new MeshBuilder();
        Color edge = new Color(0.88f, 0.82f, 0.74f, 1f);
        m.Quad(new Vector3(0f, 0f, 0.9f), new Vector3(-3.2f, 0f, 0.5f), new Vector3(-3f, 0f, -0.6f), new Vector3(0f, 0f, -1f), Cream, edge, edge, Cream);
        return Register(m.ToMesh("MR_Sky_Bird_Wing"));
    }

    private Mesh Register(Mesh mesh)
    {
        meshes.Add(mesh);
        return mesh;
    }

    private Material CreateMaterial(Shader shader, string materialName, int queue)
    {
        var material = new Material(shader);
        material.name = materialName;
        material.renderQueue = queue;
        materials.Add(material);
        return material;
    }

    private static Transform CreatePart(string partName, Mesh mesh, Material material, Transform parent)
    {
        var go = new GameObject(partName);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var partRenderer = go.AddComponent<MeshRenderer>();
        partRenderer.sharedMaterial = material;
        partRenderer.shadowCastingMode = ShadowCastingMode.Off;
        partRenderer.receiveShadows = false;
        partRenderer.lightProbeUsage = LightProbeUsage.Off;
        partRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return go.transform;
    }

    private void Cleanup()
    {
        for (int i = 0; i < meshes.Count; i++)
        {
            if (meshes[i] != null) Destroy(meshes[i]);
        }
        for (int i = 0; i < materials.Count; i++)
        {
            if (materials[i] != null) Destroy(materials[i]);
        }
        meshes.Clear();
        materials.Clear();
        birds = null;
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
