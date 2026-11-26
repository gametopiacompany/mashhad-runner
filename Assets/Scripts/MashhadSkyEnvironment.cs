using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Adds a procedural sky to the BRT corridor: a gradient dome that blends into the scene fog colour,
/// two layers of hazy mountains on the horizon and a ring of soft clouds. Visual only: no colliders,
/// no gameplay logic. The sky follows the camera, is drawn before all opaque geometry and stops
/// drifting while the game is paused.
/// </summary>
public sealed class MashhadSkyEnvironment : MonoBehaviour
{
    private const float DomeRadius = 450f;
    private const float FarRidgeRadius = 390f;
    private const float NearRidgeRadius = 330f;
    private const float CloudRadius = 360f;
    private const int DomeSegments = 48;
    private const int RidgeSegments = 144;
    private const int CloudCount = 14;
    private const int CloudRingPoints = 14;
    private const float CloudSpinDegreesPerSecond = 0.35f;
    private const float MinFarClip = 520f;
    private const float CheckInterval = 0.5f;

    private static readonly float[] DomeElevations = { -25f, -3f, 0f, 4f, 10f, 20f, 35f, 55f, 90f };
    private static readonly Color Zenith = new Color(0.34f, 0.56f, 0.79f, 1f);
    private static readonly Color Mid = new Color(0.64f, 0.75f, 0.84f, 1f);
    private static readonly Color FallbackFog = new Color(0.78f, 0.70f, 0.57f, 1f);
    private static readonly Color FarRidgeTint = new Color(0.52f, 0.50f, 0.52f, 1f);
    private static readonly Color NearRidgeTint = new Color(0.42f, 0.38f, 0.34f, 1f);
    private static readonly Color CloudColor = new Color(1f, 0.97f, 0.92f, 1f);

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

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh();
            mesh.name = name;
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
    private Camera viewCamera;
    private float nextCheck;
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
        bool gameActive = FindObjectOfType<RunnerGame>() != null;
        if (gameActive && skyRoot == null && enabled) Build();

        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera != null && viewCamera.farClipPlane < MinFarClip)
        {
            viewCamera.farClipPlane = MinFarClip;
        }

        shown = gameActive && skyRoot != null && viewCamera != null;
        if (skyRoot != null && skyRoot.gameObject.activeSelf != shown)
        {
            skyRoot.gameObject.SetActive(shown);
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

        Color fog = RenderSettings.fog ? RenderSettings.fogColor : FallbackFog;
        fog.a = 1f;

        skyRoot = new GameObject("MR_Sky_Root").transform;
        skyRoot.SetParent(transform, false);
        cloudRoot = new GameObject("MR_Sky_Clouds").transform;
        cloudRoot.SetParent(skyRoot, false);

        CreatePart("Sky_Dome", BuildDome(fog), CreateMaterial(shader, "MR_Sky_Dome", 1000), skyRoot);
        CreatePart("Sky_Far_Mountains", BuildRidge(FarRidgeRadius, 22f, 52f, 0.7f, FarRidgeTint, 0.35f, fog), CreateMaterial(shader, "MR_Sky_Far_Mountains", 1001), skyRoot);
        CreatePart("Sky_Near_Mountains", BuildRidge(NearRidgeRadius, 12f, 30f, 2.9f, NearRidgeTint, 0.22f, fog), CreateMaterial(shader, "MR_Sky_Near_Mountains", 1002), skyRoot);
        CreatePart("Sky_Clouds", BuildClouds(), CreateMaterial(shader, "MR_Sky_Clouds", 1003), cloudRoot);
    }

    private static Color SkyColor(float elevationDegrees, Color fog)
    {
        if (elevationDegrees <= 2f) return fog;
        float t = Mathf.Clamp01((elevationDegrees - 2f) / 88f);
        t = Mathf.Pow(t, 0.6f);
        return t < 0.35f
            ? Color.Lerp(fog, Mid, t / 0.35f)
            : Color.Lerp(Mid, Zenith, (t - 0.35f) / 0.65f);
    }

    private Mesh BuildDome(Color fog)
    {
        var builder = new MeshBuilder();
        int rings = DomeElevations.Length;
        for (int r = 0; r < rings; r++)
        {
            float elevation = DomeElevations[r];
            float radians = elevation * Mathf.Deg2Rad;
            Color color = SkyColor(elevation, fog);
            for (int s = 0; s <= DomeSegments; s++)
            {
                float angle = s * Mathf.PI * 2f / DomeSegments;
                var position = new Vector3(
                    Mathf.Cos(radians) * Mathf.Cos(angle),
                    Mathf.Sin(radians),
                    Mathf.Cos(radians) * Mathf.Sin(angle)) * DomeRadius;
                builder.Add(position, color);
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
                builder.Triangle(i0, i2, i1);
                builder.Triangle(i1, i2, i3);
            }
        }

        return Register(builder.ToMesh("MR_Sky_Dome"));
    }

    private Mesh BuildRidge(float radius, float minHeight, float maxHeight, float phase, Color tint, float tintAmount, Color fog)
    {
        var builder = new MeshBuilder();
        Color topColor = Color.Lerp(fog, tint, tintAmount * 2f);
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
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            int top = builder.Add(new Vector3(x, height, z), topColor);
            int bottom = builder.Add(new Vector3(x, -40f, z), fog);

            if (previousTop >= 0)
            {
                builder.Triangle(previousTop, previousBottom, top);
                builder.Triangle(top, previousBottom, bottom);
            }

            previousTop = top;
            previousBottom = bottom;
        }

        return Register(builder.ToMesh("MR_Sky_Ridge_" + radius));
    }

    private Mesh BuildClouds()
    {
        var builder = new MeshBuilder();
        for (int c = 0; c < CloudCount; c++)
        {
            float angle = (c + (c % 3) * 0.21f) * Mathf.PI * 2f / CloudCount;
            var center = new Vector3(Mathf.Cos(angle) * CloudRadius, 45f + (c % 4) * 16f, Mathf.Sin(angle) * CloudRadius);
            var right = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            float halfWidth = (60f + (c % 3) * 25f) * 0.5f;
            float halfHeight = (9f + (c % 2) * 4f) * 0.5f;

            int centerIndex = builder.Add(center, new Color(CloudColor.r, CloudColor.g, CloudColor.b, 0.75f));
            int firstRing = -1;
            for (int k = 0; k < CloudRingPoints; k++)
            {
                float theta = k * Mathf.PI * 2f / CloudRingPoints;
                Vector3 position = center + right * (Mathf.Cos(theta) * halfWidth) + Vector3.up * (Mathf.Sin(theta) * halfHeight);
                int index = builder.Add(position, new Color(CloudColor.r, CloudColor.g, CloudColor.b, 0f));
                if (firstRing < 0) firstRing = index;
            }

            for (int k = 0; k < CloudRingPoints; k++)
            {
                int a = firstRing + k;
                int b = firstRing + (k + 1) % CloudRingPoints;
                builder.Triangle(centerIndex, a, b);
            }
        }

        return Register(builder.ToMesh("MR_Sky_Clouds"));
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
