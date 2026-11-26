using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Horizon shrine in the style of the Imam Reza shrine: a lit golden ribbed dome with two golden minarets beside it,
/// a golden tiled iwan, turquoise bands and a warm arcade, plus a flock of pigeons circling the dome. If a texture
/// named Resources/MashhadSky/SunsetPanorama (ordinary LDR equirectangular image) exists, it is used as the sky
/// background on top of the generated gradient. Attaches to the sky built by MashhadSkyEnvironment. Visual only.
/// </summary>
public sealed class MashhadSkyDetails : MonoBehaviour
{
    private const float ShrineAngle = Mathf.PI * 0.5f;
    private const float ShrineRadius = 265f;
    private const float Scale = 0.64f;
    private const float PanoramaRadius = 440f;
    private const int BirdCount = 16;

    private static readonly Color Haze = new Color(0.93f, 0.64f, 0.44f, 1f);
    private static readonly Color WallCream = new Color(0.97f, 0.89f, 0.72f, 1f);
    private static readonly Color GoldBright = new Color(1f, 0.84f, 0.38f, 1f);
    private static readonly Color GoldMid = new Color(0.92f, 0.68f, 0.24f, 1f);
    private static readonly Color GoldShadow = new Color(0.60f, 0.38f, 0.14f, 1f);
    private static readonly Color TileC = new Color(0.10f, 0.52f, 0.58f, 1f);
    private static readonly Color TileLight = new Color(0.30f, 0.76f, 0.80f, 1f);
    private static readonly Color Warm = new Color(1f, 0.86f, 0.5f, 1f);
    private static readonly Color WarmDim = new Color(0.95f, 0.62f, 0.3f, 1f);
    private static readonly Color Recess = new Color(0.36f, 0.22f, 0.18f, 1f);
    private static readonly Color RibColor = new Color(0.62f, 0.4f, 0.16f, 1f);
    private static readonly Color Cream = new Color(0.98f, 0.94f, 0.88f, 1f);

    private static readonly float[] PanoramaElevations = { -2f, 4f, 10f, 16f, 24f, 34f, 46f, 60f, 75f, 90f };

    private sealed class MeshBuilder
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<Color> colors = new List<Color>();
        public readonly List<Vector2> uvs = new List<Vector2>();
        public readonly List<int> triangles = new List<int>();

        public int Add(Vector3 position, Color color)
        {
            return AddUv(position, color, Vector2.zero);
        }

        public int AddUv(Vector3 position, Color color, Vector2 uv)
        {
            vertices.Add(position);
            colors.Add(color);
            uvs.Add(uv);
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
            mesh.SetUVs(0, uvs);
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
        Texture2D panorama = Resources.Load<Texture2D>("MashhadSky/SunsetPanorama");
        if (panorama != null)
        {
            panorama.wrapMode = TextureWrapMode.Repeat;
            Transform oldDome = sr.Find("Sky_Dome");
            if (oldDome != null)
            {
                MeshRenderer oldRenderer = oldDome.GetComponent<MeshRenderer>();
                if (oldRenderer != null) oldRenderer.sharedMaterial.renderQueue = 999;
            }
            Material panoramaMaterial = CreateMaterial(shader, "MR_Sky_Panorama", 1000);
            panoramaMaterial.mainTexture = panorama;
            CreatePart("Sky_Panorama", BuildPanorama(), panoramaMaterial, sr);
        }

        Transform old = sr.Find("Sky_Shrine");
        if (old != null) old.gameObject.SetActive(false);

        CreatePart("Sky_Shrine_Detail", BuildShrine(), CreateMaterial(shader, "MR_Sky_Shrine_Detail", 1008), sr);
        BuildFlock(shader, sr);
        built = true;
    }

    private Mesh BuildPanorama()
    {
        var m = new MeshBuilder();
        int rings = PanoramaElevations.Length;
        const int segments = 72;
        for (int r = 0; r < rings; r++)
        {
            float elevation = PanoramaElevations[r];
            float radians = elevation * Mathf.Deg2Rad;
            for (int s = 0; s <= segments; s++)
            {
                float angle = s * Mathf.PI * 2f / segments;
                Vector3 p = new Vector3(
                    Mathf.Cos(radians) * Mathf.Cos(angle),
                    Mathf.Sin(radians),
                    Mathf.Cos(radians) * Mathf.Sin(angle)) * PanoramaRadius;
                Vector2 uv = new Vector2(angle / (Mathf.PI * 2f) + 0.25f, (elevation + 90f) / 180f);
                m.AddUv(p, Color.white, uv);
            }
        }

        int stride = segments + 1;
        for (int r = 0; r < rings - 1; r++)
        {
            for (int s = 0; s < segments; s++)
            {
                int i0 = r * stride + s;
                int i1 = i0 + 1;
                int i2 = i0 + stride;
                int i3 = i2 + 1;
                m.Triangle(i0, i2, i1);
                m.Triangle(i1, i2, i3);
            }
        }
        return Register(m.ToMesh("MR_Sky_Panorama"));
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
        Rect(m, x - 2.8f, -20f, x + 2.8f, 56f, GoldShadow, GoldBright);
        for (float y = 4f; y < 54f; y += 9f)
        {
            Rect(m, x - 2.95f, y, x + 2.95f, y + 1f, GoldShadow, GoldMid);
        }
        Rect(m, x - 2.85f, 18f, x + 2.85f, 22.5f, TileC, TileLight);
        Rect(m, x - 2.85f, 36f, x + 2.85f, 40.5f, TileC, TileLight);
        Rect(m, x - 5.2f, 56f, x + 5.2f, 58f, GoldShadow, GoldMid);
        Rect(m, x - 4.2f, 58f, x + 4.2f, 59.2f, TileC, TileLight);
        Rect(m, x - 2.2f, 59.2f, x + 2.2f, 66f, GoldMid, GoldBright);
        Rect(m, x - 3.6f, 66f, x + 3.6f, 67.6f, GoldShadow, GoldMid);
        Rect(m, x - 1.7f, 67.6f, x + 1.7f, 72f, GoldMid, GoldBright);
        Rect(m, x - 0.6f, 68.6f, x + 0.6f, 71f, Warm, Warm);
        m.Tri(P(x - 2.5f, 72f), P(x, 79f), P(x + 2.5f, 72f), GoldMid, GoldBright, GoldMid);
        Disc(m, x, 79.8f, 1f, 1f, GoldBright, GoldBright, 8);
        Rect(m, x - 0.25f, 80.5f, x + 0.25f, 85f, GoldBright, GoldBright);
    }

    private Mesh BuildShrine()
    {
        var m = new MeshBuilder();
        Disc(m, 0f, 55f, 170f, 130f, new Color(1f, 0.88f, 0.55f, 0.6f), new Color(1f, 0.88f, 0.55f, 0f), 32);

        Rect(m, -100f, -20f, 100f, 8f, Haze, WallCream);
        Rect(m, -34f, -20f, 34f, 40f, Haze, WallCream);
        Rect(m, -34f, 27f, 34f, 30f, TileC, TileLight);
        for (int sx = -1; sx <= 1; sx += 2)
        {
            Rect(m, sx * 34f, -20f, sx * 64f, 28f, Haze, WallCream);
            Rect(m, sx * 34f, 22f, sx * 64f, 25f, TileC, TileLight);
            Rect(m, sx * 34f, 28f, sx * 64f, 29.5f, GoldShadow, GoldMid);
            for (int row = 0; row < 2; row++)
            {
                float y = 4f + row * 8f;
                for (int i = 0; i < 6; i++)
                {
                    float x = sx * (38f + i * 4.2f);
                    Rect(m, x - 0.9f, y, x + 0.9f, y + 4.5f, Warm, WarmDim);
                }
            }
            float dx = sx * 49f;
            Rect(m, dx - 6f, 29.5f, dx + 6f, 35f, TileC, TileLight);
            Dome(m, dx, 35f, 8.5f, 10f, GoldMid, GoldBright);
            Rect(m, dx - 0.25f, 45f, dx + 0.25f, 49f, GoldBright, GoldBright);
        }

        Poly(m, GoldBright, new float[] { -14f, 6f, -14f, 34f, 0f, 46f, 14f, 34f, 14f, 6f });
        Poly(m, GoldMid, new float[] { -12f, 6f, -12f, 33f, 0f, 44f, 12f, 33f, 12f, 6f });
        Poly(m, TileC, new float[] { -10f, 6f, -10f, 32f, 0f, 41.5f, 10f, 32f, 10f, 6f });
        Poly(m, Recess, new float[] { -6.5f, 8f, -6.5f, 28f, 0f, 35f, 6.5f, 28f, 6.5f, 8f });
        Rect(m, -6.5f, 8f, 6.5f, 13f, Warm, WarmDim);
        for (int i = -3; i <= 3; i++)
        {
            Rect(m, i * 3.2f - 0.8f, 29f, i * 3.2f + 0.8f, 33f, GoldMid, GoldBright);
        }

        Rect(m, -16f, 40f, 16f, 54f, TileC, TileLight);
        for (int i = -3; i <= 3; i++)
        {
            Rect(m, i * 3.8f - 0.9f, 43f, i * 3.8f + 0.9f, 50f, Warm, WarmDim);
        }
        Rect(m, -16.5f, 54f, 16.5f, 55.8f, GoldShadow, GoldMid);
        Dome(m, 0f, 55.8f, 19f, 21f, GoldMid, GoldBright);
        float[] ribs = { -1.2f, -0.8f, -0.4f, 0f, 0.4f, 0.8f, 1.2f };
        for (int i = 0; i < ribs.Length; i++) Meridian(m, 0f, 55.8f, 19f, 21f, ribs[i], RibColor);
        Disc(m, -6f, 68f, 5f, 8f, new Color(1f, 0.96f, 0.75f, 0.7f), new Color(1f, 0.96f, 0.75f, 0f), 14);
        Rect(m, -1.8f, 76.8f, 1.8f, 78.4f, GoldShadow, GoldMid);
        Disc(m, 0f, 79.6f, 1.5f, 1.5f, GoldBright, GoldBright, 10);
        Rect(m, -0.3f, 81f, 0.3f, 86f, GoldBright, GoldBright);

        Minaret(m, -41f);
        Minaret(m, 41f);

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
            radii[i] = 32f + (i % 5) * 8f;
            heights[i] = 4f + (i % 4) * 5f;
        }
        AnimateFlock();
    }

    private void AnimateFlock()
    {
        if (birds == null) return;
        Vector3 center = P(0f, 68f);
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
