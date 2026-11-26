using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Sunset sky for the BRT corridor: gradient dome with a glow beside the shrine, sun disc, coloured clouds, two
/// mountain layers, an imaginary city skyline and a shrine silhouette at the end of the road. Also sets warm fog,
/// ambient and sun light. Visual only; follows the camera and stops drifting while the game is paused.
/// </summary>
public sealed class MashhadSkyEnvironment : MonoBehaviour
{
    private const float DomeRadius = 450f;
    private const float CloudRadius = 400f;
    private const float FarRidgeRadius = 390f;
    private const float NearRidgeRadius = 340f;
    private const float FarCityRadius = 310f;
    private const float NearCityRadius = 285f;
    private const float ShrineRadius = 265f;
    private const float ShrineScale = 0.62f;
    private const int DomeSegments = 96;
    private const int RidgeSegments = 144;
    private const int CloudCount = 30;
    private const float CloudSpinDegreesPerSecond = 0.1f;
    private const float MinFarClip = 560f;
    private const float CheckInterval = 0.5f;
    private const float ShrineAngle = Mathf.PI * 0.5f;
    private const float SunAngle = Mathf.PI * 0.5f - 0.42f;
    private const float SunElevation = 0.11f;

    private static readonly float[] DomeElevations = { -25f, -3f, 0f, 2f, 5f, 9f, 14f, 20f, 28f, 38f, 52f, 68f, 90f };
    private static readonly Color Haze = new Color(0.93f, 0.64f, 0.44f, 1f);
    private static readonly Color Peach = new Color(0.97f, 0.70f, 0.48f, 1f);
    private static readonly Color DuskPink = new Color(0.80f, 0.52f, 0.58f, 1f);
    private static readonly Color DuskBlue = new Color(0.38f, 0.40f, 0.66f, 1f);
    private static readonly Color Zenith = new Color(0.14f, 0.18f, 0.40f, 1f);
    private static readonly Color Glow = new Color(1f, 0.80f, 0.42f, 1f);
    private static readonly Color WindowWarm = new Color(1f, 0.78f, 0.42f, 1f);

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
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1200f);
            return mesh;
        }
    }

    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<Material> materials = new List<Material>();

    private Transform skyRoot;
    private Transform cloudRoot;
    private GameObject fillLight;
    private Camera viewCamera;
    private float nextCheck;
    private int lastGameId;
    private bool shown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<MashhadSkyEnvironment>() != null) return;
        var host = new GameObject("MR_Sky_Environment");
        DontDestroyOnLoad(host);
        host.AddComponent<MashhadSkyEnvironment>();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime >= nextCheck)
        {
            nextCheck = Time.unscaledTime + CheckInterval;
            Refresh();
        }

        if (!shown || viewCamera == null || skyRoot == null) return;

        skyRoot.position = viewCamera.transform.position;
        if (Time.deltaTime > 0f && cloudRoot != null)
        {
            cloudRoot.Rotate(0f, CloudSpinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
        }
    }

    private void Refresh()
    {
        RunnerGame game = FindObjectOfType<RunnerGame>();
        bool gameActive = game != null;

        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera != null && viewCamera.farClipPlane < MinFarClip)
        {
            viewCamera.farClipPlane = MinFarClip;
        }

        if (gameActive && game.GetInstanceID() != lastGameId)
        {
            lastGameId = game.GetInstanceID();
            ApplyLighting();
        }

        if (gameActive && skyRoot == null && enabled) Build();

        shown = gameActive && skyRoot != null && viewCamera != null;
        if (skyRoot != null && skyRoot.gameObject.activeSelf != shown)
        {
            skyRoot.gameObject.SetActive(shown);
        }
    }

    private static Vector3 SunDirection()
    {
        return new Vector3(
            Mathf.Cos(SunElevation) * Mathf.Cos(SunAngle),
            Mathf.Sin(SunElevation),
            Mathf.Cos(SunElevation) * Mathf.Sin(SunAngle));
    }

    private void ApplyLighting()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = Haze;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.52f, 0.66f);
        RenderSettings.ambientEquatorColor = new Color(0.78f, 0.60f, 0.48f);
        RenderSettings.ambientGroundColor = new Color(0.30f, 0.23f, 0.20f);
        if (viewCamera != null) viewCamera.backgroundColor = Haze;

        GameObject sun = GameObject.Find("Sun");
        if (sun != null)
        {
            Light light = sun.GetComponent<Light>();
            if (light != null)
            {
                light.color = new Color(1f, 0.70f, 0.42f);
                light.intensity = 1.15f;
            }
            sun.transform.rotation = Quaternion.LookRotation(-SunDirection());
        }

        if (fillLight == null)
        {
            fillLight = new GameObject("MR_Sunset_Fill");
            fillLight.transform.SetParent(transform, false);
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.62f, 0.68f, 0.95f);
            fill.intensity = 0.38f;
            fill.shadows = LightShadows.None;
            fillLight.transform.rotation = Quaternion.LookRotation(new Vector3(0.15f, -0.35f, 0.92f));
        }
    }

    private void Build()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            enabled = false;
            return;
        }

        skyRoot = new GameObject("MR_Sky_Root").transform;
        skyRoot.SetParent(transform, false);
        cloudRoot = new GameObject("MR_Sky_Clouds").transform;
        cloudRoot.SetParent(skyRoot, false);

        CreatePart("Sky_Dome", BuildDome(), CreateMaterial(shader, "MR_Sky_Dome", 1000), skyRoot);
        CreatePart("Sky_Sun", BuildSun(), CreateMaterial(shader, "MR_Sky_Sun", 1001), skyRoot);
        CreatePart("Sky_Clouds", BuildClouds(), CreateMaterial(shader, "MR_Sky_Clouds", 1002), cloudRoot);
        CreatePart("Sky_Far_Mountains", BuildRidge(FarRidgeRadius, 22f, 52f, 0.7f, new Color(0.62f, 0.42f, 0.52f), 0.55f), CreateMaterial(shader, "MR_Sky_Far_Mountains", 1003), skyRoot);
        CreatePart("Sky_Near_Mountains", BuildRidge(NearRidgeRadius, 12f, 34f, 2.9f, new Color(0.40f, 0.26f, 0.34f), 0.7f), CreateMaterial(shader, "MR_Sky_Near_Mountains", 1004), skyRoot);
        CreatePart("Sky_City_Far", BuildSkyline(FarCityRadius, 18f, 44f, 10f, 22f, new Color(0.46f, 0.30f, 0.42f, 1f), false, 101), CreateMaterial(shader, "MR_Sky_City_Far", 1005), skyRoot);
        CreatePart("Sky_City_Near", BuildSkyline(NearCityRadius, 14f, 34f, 9f, 20f, new Color(0.26f, 0.17f, 0.26f, 1f), true, 202), CreateMaterial(shader, "MR_Sky_City_Near", 1006), skyRoot);
        CreatePart("Sky_Shrine", BuildShrine(), CreateMaterial(shader, "MR_Sky_Shrine", 1007), skyRoot);
    }

    private static Vector3 Pol(float angle, float y, float radius)
    {
        return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
    }

    private static Color SkyColor(float elevationDegrees, float azimuth)
    {
        if (elevationDegrees <= 2f) return Haze;
        float e = Mathf.Clamp01((elevationDegrees - 2f) / 88f);
        Color baseColor;
        if (e < 0.12f) baseColor = Color.Lerp(Haze, Peach, e / 0.12f);
        else if (e < 0.35f) baseColor = Color.Lerp(Peach, DuskPink, (e - 0.12f) / 0.23f);
        else if (e < 0.65f) baseColor = Color.Lerp(DuskPink, DuskBlue, (e - 0.35f) / 0.3f);
        else baseColor = Color.Lerp(DuskBlue, Zenith, (e - 0.65f) / 0.35f);

        float dAz = Mathf.DeltaAngle(azimuth * Mathf.Rad2Deg, SunAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        float glow = Mathf.Exp(-(dAz * dAz) / 0.3f) * Mathf.Exp(-(elevationDegrees * elevationDegrees) / 484f);
        return Color.Lerp(baseColor, Glow, glow * 0.85f);
    }

    private Mesh BuildDome()
    {
        var m = new MeshBuilder();
        int rings = DomeElevations.Length;
        for (int r = 0; r < rings; r++)
        {
            float elevation = DomeElevations[r];
            float radians = elevation * Mathf.Deg2Rad;
            for (int s = 0; s <= DomeSegments; s++)
            {
                float angle = s * Mathf.PI * 2f / DomeSegments;
                Vector3 p = new Vector3(
                    Mathf.Cos(radians) * Mathf.Cos(angle),
                    Mathf.Sin(radians),
                    Mathf.Cos(radians) * Mathf.Sin(angle)) * DomeRadius;
                m.Add(p, SkyColor(elevation, angle));
            }
        }

        int stride = DomeSegments + 1;
        for (int r = 0; r < rings - 1; r++)
        {
            for (int s = 0; s < DomeSegments; s++)
            {
                int i0 = r * stride + s;
                int i1 = i0 + 1;
                int i2 = i0 + stride;
                int i3 = i2 + 1;
                m.Triangle(i0, i2, i1);
                m.Triangle(i1, i2, i3);
            }
        }
        return Register(m.ToMesh("MR_Sky_Dome"));
    }

    private static void AddDisc(MeshBuilder m, Vector3 center, Vector3 right, Vector3 up, float rx, float ry, Color centerColor, Color edgeColor, int points)
    {
        int c = m.Add(center, centerColor);
        int first = -1;
        for (int k = 0; k < points; k++)
        {
            float theta = k * Mathf.PI * 2f / points;
            int i = m.Add(center + right * (Mathf.Cos(theta) * rx) + up * (Mathf.Sin(theta) * ry), edgeColor);
            if (first < 0) first = i;
        }
        for (int k = 0; k < points; k++)
        {
            m.Triangle(c, first + k, first + (k + 1) % points);
        }
    }

    private Mesh BuildSun()
    {
        var m = new MeshBuilder();
        Vector3 dir = SunDirection();
        Vector3 center = dir * (DomeRadius * 0.96f);
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        Vector3 up = Vector3.Cross(dir, right);
        AddDisc(m, center, right, up, 80f, 80f, new Color(1f, 0.74f, 0.4f, 0.55f), new Color(1f, 0.74f, 0.4f, 0f), 24);
        AddDisc(m, center, right, up, 14f, 14f, new Color(1f, 0.97f, 0.82f, 1f), new Color(1f, 0.9f, 0.62f, 1f), 24);
        return Register(m.ToMesh("MR_Sky_Sun"));
    }

    private Mesh BuildRidge(float radius, float minHeight, float maxHeight, float phase, Color tint, float tintAmount)
    {
        var m = new MeshBuilder();
        Color topColor = Color.Lerp(Haze, tint, tintAmount);
        topColor.a = 1f;
        int previousTop = -1;
        int previousBottom = -1;
        for (int s = 0; s <= RidgeSegments; s++)
        {
            float angle = s * Mathf.PI * 2f / RidgeSegments;
            float noise = Mathf.Sin(angle * 3f + phase) * 0.5f
                + Mathf.Sin(angle * 7f + phase * 1.7f) * 0.3f
                + Mathf.Sin(angle * 13f + phase * 2.3f) * 0.2f;
            float height = Mathf.Lerp(minHeight, maxHeight, (noise + 1f) * 0.5f);
            int top = m.Add(Pol(angle, height, radius), topColor);
            int bottom = m.Add(Pol(angle, -40f, radius), Haze);
            if (previousTop >= 0)
            {
                m.Triangle(previousTop, previousBottom, top);
                m.Triangle(top, previousBottom, bottom);
            }
            previousTop = top;
            previousBottom = bottom;
        }
        return Register(m.ToMesh("MR_Sky_Ridge_" + radius));
    }

    private Mesh BuildSkyline(float radius, float minH, float maxH, float minW, float maxW, Color top, bool windows, int seed)
    {
        var m = new MeshBuilder();
        var rng = new System.Random(seed);
        float full = Mathf.PI * 2f;
        float angle = 0f;
        while (angle < full - 0.002f)
        {
            float width = Mathf.Lerp(minW, maxW, (float)rng.NextDouble());
            float da = Mathf.Min(width / radius, full - angle);
            float deg = Mathf.DeltaAngle(angle * Mathf.Rad2Deg, ShrineAngle * Mathf.Rad2Deg);
            float forward = Mathf.Exp(-(deg * deg) / 12100f);
            float height = Mathf.Lerp(minH, maxH, (float)rng.NextDouble()) * Mathf.Lerp(0.45f, 1.15f, forward);
            AddBlock(m, rng, radius, angle, angle + da * 0.92f, height, top, windows);
            angle += da;
        }
        return Register(m.ToMesh("MR_Sky_City_" + radius));
    }

    private static void AddBlock(MeshBuilder m, System.Random rng, float radius, float a0, float a1, float height, Color top, bool windows)
    {
        m.Quad(Pol(a0, -30f, radius), Pol(a0, height, radius), Pol(a1, height, radius), Pol(a1, -30f, radius), Haze, top, top, Haze);
        float widthM = (a1 - a0) * radius;
        float mid = (a0 + a1) * 0.5f;
        double roll = rng.NextDouble();
        if (roll < 0.28)
        {
            float inset = (a1 - a0) * 0.22f;
            float h2 = height * (1.08f + (float)rng.NextDouble() * 0.12f);
            m.Quad(Pol(a0 + inset, height - 1f, radius), Pol(a0 + inset, h2, radius), Pol(a1 - inset, h2, radius), Pol(a1 - inset, height - 1f, radius), top, top, top, top);
        }
        else if (roll < 0.40)
        {
            Vector3 c = Pol(mid, height, radius);
            Vector3 t = new Vector3(-Mathf.Sin(mid), 0f, Mathf.Cos(mid));
            float r = widthM * 0.32f;
            int ci = m.Add(c, new Color(0.75f, 0.48f, 0.25f, 1f));
            int first = -1;
            for (int k = 0; k <= 12; k++)
            {
                float theta = Mathf.PI * k / 12f;
                Color dc = Color.Lerp(new Color(0.5f, 0.3f, 0.2f, 1f), new Color(0.95f, 0.68f, 0.3f, 1f), Mathf.Sin(theta));
                int vi = m.Add(c + t * (Mathf.Cos(theta) * r) + Vector3.up * (Mathf.Sin(theta) * r * 0.9f), dc);
                if (first < 0) first = vi;
            }
            for (int k = 0; k < 12; k++) m.Triangle(ci, first + k, first + k + 1);
        }
        else if (roll < 0.50)
        {
            float mw = 0.7f / radius;
            float mh = height + 22f;
            m.Quad(Pol(a1 - mw * 2f, height - 1f, radius), Pol(a1 - mw * 2f, mh, radius), Pol(a1, mh, radius), Pol(a1, height - 1f, radius), top, top, top, top);
            m.Tri(Pol(a1 - mw * 2f, mh, radius), Pol(a1 - mw, mh + 6f, radius), Pol(a1, mh, radius), new Color(0.9f, 0.62f, 0.3f, 1f), new Color(0.9f, 0.62f, 0.3f, 1f), new Color(0.9f, 0.62f, 0.3f, 1f));
        }
        else
        {
            float aw = 0.25f / radius;
            m.Quad(Pol(mid - aw, height, radius), Pol(mid - aw, height + 7f, radius), Pol(mid + aw, height + 7f, radius), Pol(mid + aw, height, radius), top, top, top, top);
        }

        if (!windows) return;
        int cols = Mathf.Max(1, Mathf.FloorToInt(widthM / 5f));
        float colW = (a1 - a0) / cols;
        for (float y = 8f; y < height - 3f; y += 6f)
        {
            for (int c = 0; c < cols; c++)
            {
                if (rng.NextDouble() > 0.22) continue;
                float ac = a0 + colW * (c + 0.5f);
                float hw = colW * 0.2f;
                m.Quad(Pol(ac - hw, y, radius), Pol(ac - hw, y + 2f, radius), Pol(ac + hw, y + 2f, radius), Pol(ac + hw, y, radius), WindowWarm, WindowWarm, WindowWarm, WindowWarm);
            }
        }
    }

    private static Vector3 ShrinePoint(Vector3 origin, Vector3 tangent, float x, float y)
    {
        return origin + tangent * (x * ShrineScale) + Vector3.up * (y * ShrineScale);
    }

    private Mesh BuildShrine()
    {
        var m = new MeshBuilder();
        Vector3 o = Pol(ShrineAngle, 0f, ShrineRadius);
        Vector3 t = new Vector3(-Mathf.Sin(ShrineAngle), 0f, Mathf.Cos(ShrineAngle));
        Color dark = new Color(0.22f, 0.13f, 0.17f, 1f);
        Color tileC = new Color(0.08f, 0.28f, 0.32f, 1f);
        Color goldTop = new Color(1f, 0.74f, 0.32f, 1f);
        Color goldDark = new Color(0.52f, 0.32f, 0.18f, 1f);

        AddDisc(m, ShrinePoint(o, t, 0f, 50f), t, Vector3.up, 130f * ShrineScale, 110f * ShrineScale, new Color(1f, 0.84f, 0.52f, 0.5f), new Color(1f, 0.84f, 0.52f, 0f), 28);

        m.Quad(ShrinePoint(o, t, -80f, -20f), ShrinePoint(o, t, -80f, 6f), ShrinePoint(o, t, 80f, 6f), ShrinePoint(o, t, 80f, -20f), Haze, dark, dark, Haze);
        m.Quad(ShrinePoint(o, t, -26f, -20f), ShrinePoint(o, t, -26f, 36f), ShrinePoint(o, t, 26f, 36f), ShrinePoint(o, t, 26f, -20f), Haze, dark, dark, Haze);
        for (int sx = -1; sx <= 1; sx += 2)
        {
            m.Quad(ShrinePoint(o, t, sx * 26f, -20f), ShrinePoint(o, t, sx * 26f, 24f), ShrinePoint(o, t, sx * 48f, 24f), ShrinePoint(o, t, sx * 48f, -20f), Haze, dark, dark, Haze);
        }

        int ic = m.Add(ShrinePoint(o, t, 0f, 28f), tileC);
        int i0 = m.Add(ShrinePoint(o, t, -9f, 8f), tileC);
        m.Add(ShrinePoint(o, t, -9f, 38f), tileC);
        m.Add(ShrinePoint(o, t, 0f, 48f), tileC);
        m.Add(ShrinePoint(o, t, 9f, 38f), tileC);
        m.Add(ShrinePoint(o, t, 9f, 8f), tileC);
        for (int k = 0; k < 5; k++) m.Triangle(ic, i0 + k, i0 + (k + 1) % 5);

        m.Quad(ShrinePoint(o, t, -13f, 36f), ShrinePoint(o, t, -13f, 48f), ShrinePoint(o, t, 13f, 48f), ShrinePoint(o, t, 13f, 36f), tileC, tileC, tileC, tileC);
        AddDome(m, ShrinePoint(o, t, 0f, 48f), t, 15f * ShrineScale, 16f * ShrineScale, goldDark, goldTop);
        m.Quad(ShrinePoint(o, t, -0.6f, 64f), ShrinePoint(o, t, -0.6f, 76f), ShrinePoint(o, t, 0.6f, 76f), ShrinePoint(o, t, 0.6f, 64f), goldTop, goldTop, goldTop, goldTop);

        for (int sx = -1; sx <= 1; sx += 2)
        {
            AddDome(m, ShrinePoint(o, t, sx * 37f, 24f), t, 8f * ShrineScale, 9f * ShrineScale, goldDark, goldTop);
            float mx = sx * 58f;
            m.Quad(ShrinePoint(o, t, mx - 1.8f, -20f), ShrinePoint(o, t, mx - 1.8f, 56f), ShrinePoint(o, t, mx + 1.8f, 56f), ShrinePoint(o, t, mx + 1.8f, -20f), Haze, tileC, tileC, Haze);
            m.Quad(ShrinePoint(o, t, mx - 3.2f, 50f), ShrinePoint(o, t, mx - 3.2f, 53f), ShrinePoint(o, t, mx + 3.2f, 53f), ShrinePoint(o, t, mx + 3.2f, 50f), goldTop, goldTop, goldTop, goldTop);
            m.Tri(ShrinePoint(o, t, mx - 2f, 56f), ShrinePoint(o, t, mx, 76f), ShrinePoint(o, t, mx + 2f, 56f), goldTop, goldTop, goldTop);
        }

        for (int i = 0; i < 10; i++)
        {
            if (i % 3 == 0) continue;
            float wx = -22f + i * 4.5f;
            m.Quad(ShrinePoint(o, t, wx - 0.6f, 12f), ShrinePoint(o, t, wx - 0.6f, 17f), ShrinePoint(o, t, wx + 0.6f, 17f), ShrinePoint(o, t, wx + 0.6f, 12f), WindowWarm, WindowWarm, WindowWarm, WindowWarm);
        }
        return Register(m.ToMesh("MR_Sky_Shrine"));
    }

    private static void AddDome(MeshBuilder m, Vector3 center, Vector3 tangent, float rx, float ry, Color dark, Color bright)
    {
        int ci = m.Add(center, dark);
        int first = -1;
        for (int k = 0; k <= 24; k++)
        {
            float theta = Mathf.PI * k / 24f;
            Color c = Color.Lerp(dark, bright, Mathf.Sin(theta));
            int vi = m.Add(center + tangent * (Mathf.Cos(theta) * rx) + Vector3.up * (Mathf.Sin(theta) * ry), c);
            if (first < 0) first = vi;
        }
        for (int k = 0; k < 24; k++) m.Triangle(ci, first + k, first + k + 1);
    }

    private Mesh BuildClouds()
    {
        var m = new MeshBuilder();
        var rng = new System.Random(4242);
        for (int c = 0; c < CloudCount; c++)
        {
            float angle = (c + (float)rng.NextDouble() * 0.8f) * Mathf.PI * 2f / CloudCount;
            float y = 28f + Mathf.Pow((float)rng.NextDouble(), 1.4f) * 170f;
            bool low = y < 80f;
            float w = low ? 110f + (float)rng.NextDouble() * 110f : 70f + (float)rng.NextDouble() * 60f;
            float h = low ? 7f + (float)rng.NextDouble() * 6f : 16f + (float)rng.NextDouble() * 14f;
            Color color = CloudTint(y, angle);
            int blobs = low ? 1 : 3;
            for (int k = 0; k < blobs; k++)
            {
                float shift = (k - (blobs - 1) * 0.5f) * (w * 0.45f / CloudRadius);
                float scale = (blobs == 1 || k == 1) ? 1f : 0.65f;
                AddCloudBlob(m, angle + shift, y + (k % 2) * h * 0.3f, w * scale, h * scale, color);
            }
        }
        return Register(m.ToMesh("MR_Sky_Clouds"));
    }

    private static Color CloudTint(float y, float angle)
    {
        Color baseColor = Color.Lerp(new Color(1f, 0.58f, 0.32f), new Color(0.82f, 0.7f, 0.86f), Mathf.Clamp01((y - 30f) / 160f));
        float dAz = Mathf.DeltaAngle(angle * Mathf.Rad2Deg, SunAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        float glow = Mathf.Exp(-(dAz * dAz) / 0.6f) * (y < 90f ? 1f : 0.3f);
        return Color.Lerp(baseColor, new Color(1f, 0.82f, 0.5f), glow * 0.7f);
    }

    private static void AddCloudBlob(MeshBuilder m, float angle, float y, float width, float height, Color color)
    {
        Vector3 center = Pol(angle, y, CloudRadius);
        Vector3 right = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        Color centerColor = new Color(color.r, color.g, color.b, 0.8f);
        Color edgeColor = new Color(color.r, color.g, color.b, 0f);
        AddDisc(m, center, right, Vector3.up, width * 0.5f, height * 0.5f, centerColor, edgeColor, 14);
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

    private static void CreatePart(string partName, Mesh mesh, Material material, Transform parent)
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
    }

    private void OnDestroy()
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
    }
}
