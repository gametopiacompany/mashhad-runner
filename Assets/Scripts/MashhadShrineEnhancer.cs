using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds a stylized courtyard, secondary domes, warm glow and a small pigeon flock to the
/// distant shrine skyline landmark. Visual only: no colliders and no gameplay logic.
/// </summary>
public sealed class MashhadShrineEnhancer : MonoBehaviour
{
    private const string ShrineRootName = "MR_Distant_Shrine_Skyline_Landmark";
    private const string EnhancementName = "MR_Shrine_Enhancement";
    private const float ScanInterval = 1f;

    private static bool materialsReady;
    private static Material gold;
    private static Material goldGlow;
    private static Material tile;
    private static Material cream;
    private static Material sand;
    private static Material bronze;
    private static Material leaf;
    private static Material bark;
    private static Material pigeonBody;
    private static Material pigeonWing;

    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<MashhadShrineEnhancer>() != null) return;
        var host = new GameObject("MR_Shrine_Enhancer");
        DontDestroyOnLoad(host);
        host.AddComponent<MashhadShrineEnhancer>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + ScanInterval;

        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var root = roots[i];
            if (root.name != ShrineRootName || root.transform.Find(EnhancementName) != null) continue;
            Enhance(root.transform);
        }
    }

    private static void Enhance(Transform shrine)
    {
        EnsureMaterials();

        var marker = new GameObject(EnhancementName).transform;
        marker.SetParent(shrine, false);
        var build = new GameObject("Build").transform;
        build.SetParent(marker, false);

        BuildCourtyard(build);
        BuildWingDomes(build);
        BuildTreesAndLamps(build);
        BuildBackdrop(build);
        Cyl(build, new Vector3(0f, 11.62f, 0f), 3.2f, 0.08f, goldGlow);

        CombineByMaterial(build, marker);
        AddAmbientLife(marker);
    }

    private static void BuildCourtyard(Transform b)
    {
        Part(b, PrimitiveType.Cube, new Vector3(0f, 0.12f, -1f), new Vector3(23f, 0.24f, 17f), sand);
        Part(b, PrimitiveType.Cube, new Vector3(0f, 0.30f, -1f), new Vector3(21f, 0.12f, 15f), cream);
        Part(b, PrimitiveType.Cube, new Vector3(0f, 0.10f, -10.3f), new Vector3(6f, 0.2f, 1.2f), cream);

        for (int side = -1; side <= 1; side += 2)
        {
            Part(b, PrimitiveType.Cube, new Vector3(side * 6.35f, 0.9f, -9f), new Vector3(8.3f, 1.1f, 0.35f), cream);
            Part(b, PrimitiveType.Cube, new Vector3(side * 6.35f, 1.52f, -9f), new Vector3(8.5f, 0.14f, 0.46f), tile);
            Part(b, PrimitiveType.Cube, new Vector3(side * 10.5f, 0.9f, -2f), new Vector3(0.35f, 1.1f, 14f), cream);
            Part(b, PrimitiveType.Cube, new Vector3(side * 10.5f, 1.52f, -2f), new Vector3(0.46f, 0.14f, 14.2f), tile);
            GatePost(b, new Vector3(side * 10.5f, 0f, -9f));
            GatePost(b, new Vector3(side * 2.2f, 0f, -9f));
        }
    }

    private static void GatePost(Transform b, Vector3 basePosition)
    {
        Cyl(b, basePosition + new Vector3(0f, 1f, 0f), 0.22f, 2f, gold);
        Part(b, PrimitiveType.Sphere, basePosition + new Vector3(0f, 2.15f, 0f), Vector3.one * 0.42f, gold);
    }

    private static void BuildWingDomes(Transform b)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 7.2f;
            Cyl(b, new Vector3(x, 7.5f, 0.7f), 1.15f, 1f, tile);
            Cyl(b, new Vector3(x, 8.05f, 0.7f), 1.2f, 0.1f, gold);
            Part(b, PrimitiveType.Sphere, new Vector3(x, 8.7f, 0.7f), new Vector3(2.3f, 1.7f, 2.3f), gold);
            Cyl(b, new Vector3(x, 9.9f, 0.7f), 0.06f, 1f, gold);
            Part(b, PrimitiveType.Sphere, new Vector3(x, 10.45f, 0.7f), Vector3.one * 0.22f, gold);
        }
    }

    private static void BuildTreesAndLamps(Transform b)
    {
        float[] treeX = { -10f, -6.8f, -3.6f, 3.6f, 6.8f, 10f };
        for (int i = 0; i < treeX.Length; i++)
        {
            Cyl(b, new Vector3(treeX[i], 0.65f, -11.6f), 0.12f, 1.3f, bark);
            Part(b, PrimitiveType.Sphere, new Vector3(treeX[i], 2.5f, -11.6f), new Vector3(1.1f, 2.8f, 1.1f), leaf);
        }

        float[] lampZ = { -8f, 3f };
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < lampZ.Length; i++)
            {
                float x = side * 9.5f;
                Cyl(b, new Vector3(x, 1.6f, lampZ[i]), 0.07f, 2.6f, bronze);
                Part(b, PrimitiveType.Sphere, new Vector3(x, 3f, lampZ[i]), Vector3.one * 0.4f, goldGlow);
            }
        }
    }

    private static void BuildBackdrop(Transform b)
    {
        float[,] blocks =
        {
            { -12f, 10f, 5f, 5f, 6f },
            { -5f, 12.5f, 6f, 5f, 8.5f },
            { 4f, 12f, 5f, 5f, 5.5f },
            { 11f, 10.5f, 6f, 5f, 7.5f }
        };

        for (int i = 0; i < blocks.GetLength(0); i++)
        {
            float x = blocks[i, 0];
            float z = blocks[i, 1];
            float w = blocks[i, 2];
            float d = blocks[i, 3];
            float h = blocks[i, 4];
            Part(b, PrimitiveType.Cube, new Vector3(x, h * 0.5f, z), new Vector3(w, h, d), i % 2 == 0 ? cream : sand);
            Part(b, PrimitiveType.Cube, new Vector3(x, h + 0.1f, z), new Vector3(w + 0.2f, 0.2f, d + 0.2f), tile);
        }
    }

    private static void AddAmbientLife(Transform marker)
    {
        var lifeObject = new GameObject("MR_Shrine_Ambient_Life");
        lifeObject.transform.SetParent(marker, false);

        var glint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glint.name = "Dome_Glint";
        glint.transform.SetParent(lifeObject.transform, false);
        glint.transform.localPosition = new Vector3(0f, 16f, 0f);
        glint.transform.localScale = Vector3.one * 0.5f;
        RemoveCollider(glint);
        var glintRenderer = glint.GetComponent<Renderer>();
        glintRenderer.sharedMaterial = goldGlow;
        glintRenderer.shadowCastingMode = ShadowCastingMode.Off;
        glintRenderer.receiveShadows = false;

        const int count = 12;
        var birds = new Transform[count];
        var leftWings = new Transform[count];
        var rightWings = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            var bird = new GameObject("Pigeon").transform;
            bird.SetParent(lifeObject.transform, false);
            bird.localScale = Vector3.one * 4.2f;
            Part(bird, PrimitiveType.Cube, Vector3.zero, new Vector3(0.16f, 0.13f, 0.34f), pigeonBody);

            var left = new GameObject("Wing_Pivot_L").transform;
            left.SetParent(bird, false);
            left.localPosition = new Vector3(-0.08f, 0.03f, 0f);
            Part(left, PrimitiveType.Cube, new Vector3(-0.15f, 0f, 0f), new Vector3(0.3f, 0.025f, 0.16f), pigeonWing);

            var right = new GameObject("Wing_Pivot_R").transform;
            right.SetParent(bird, false);
            right.localPosition = new Vector3(0.08f, 0.03f, 0f);
            Part(right, PrimitiveType.Cube, new Vector3(0.15f, 0f, 0f), new Vector3(0.3f, 0.025f, 0.16f), pigeonWing);

            birds[i] = bird;
            leftWings[i] = left;
            rightWings[i] = right;
        }

        var life = lifeObject.AddComponent<ShrineAmbientLife>();
        life.Setup(birds, leftWings, rightWings, glint.transform);
    }

    private static void CombineByMaterial(Transform build, Transform parent)
    {
        var groups = new Dictionary<Material, List<CombineInstance>>();
        Matrix4x4 toLocal = parent.worldToLocalMatrix;
        var filters = build.GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < filters.Length; i++)
        {
            var filter = filters[i];
            var meshRenderer = filter.GetComponent<MeshRenderer>();
            if (meshRenderer == null || filter.sharedMesh == null) continue;

            List<CombineInstance> list;
            if (!groups.TryGetValue(meshRenderer.sharedMaterial, out list))
            {
                list = new List<CombineInstance>();
                groups[meshRenderer.sharedMaterial] = list;
            }
            list.Add(new CombineInstance { mesh = filter.sharedMesh, transform = toLocal * filter.transform.localToWorldMatrix });
        }

        var cleanup = parent.gameObject.AddComponent<ShrineMeshCleanup>();
        foreach (var pair in groups)
        {
            var mesh = new Mesh();
            mesh.name = "MR_Shrine_Enhancement_" + pair.Key.name;
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.CombineMeshes(pair.Value.ToArray(), true, true);
            cleanup.Register(mesh);

            var combined = new GameObject("Combined_" + pair.Key.name);
            combined.transform.SetParent(parent, false);
            combined.AddComponent<MeshFilter>().sharedMesh = mesh;
            var combinedRenderer = combined.AddComponent<MeshRenderer>();
            combinedRenderer.sharedMaterial = pair.Key;
            combinedRenderer.shadowCastingMode = ShadowCastingMode.Off;
            combinedRenderer.receiveShadows = false;
        }

        build.gameObject.SetActive(false);
        Destroy(build.gameObject);
    }

    private static void Part(Transform parent, PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(shape);
        go.name = "Part";
        var t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = position;
        t.localScale = scale;
        RemoveCollider(go);
        var partRenderer = go.GetComponent<Renderer>();
        partRenderer.sharedMaterial = material;
        partRenderer.shadowCastingMode = ShadowCastingMode.Off;
        partRenderer.receiveShadows = false;
    }

    private static void Cyl(Transform parent, Vector3 center, float radius, float height, Material material)
    {
        Part(parent, PrimitiveType.Cylinder, center, new Vector3(radius * 2f, height * 0.5f, radius * 2f), material);
    }

    private static void RemoveCollider(GameObject go)
    {
        var collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
    }

    private static void EnsureMaterials()
    {
        if (materialsReady && gold != null) return;
        gold = MakeMaterial("MR_ShrineExt_Gold", new Color(0.83f, 0.53f, 0.12f), 0.68f, 0.68f, false);
        goldGlow = MakeMaterial("MR_ShrineExt_Glow", new Color(1f, 0.72f, 0.25f), 0.2f, 0.5f, true);
        tile = MakeMaterial("MR_ShrineExt_Turquoise", new Color(0.01f, 0.31f, 0.34f), 0.45f, 0.62f, false);
        cream = MakeMaterial("MR_ShrineExt_Cream", new Color(0.86f, 0.78f, 0.64f), 0.04f, 0.24f, false);
        sand = MakeMaterial("MR_ShrineExt_Limestone", new Color(0.69f, 0.57f, 0.43f), 0.08f, 0.28f, false);
        bronze = MakeMaterial("MR_ShrineExt_Bronze", new Color(0.12f, 0.105f, 0.08f), 0.55f, 0.48f, false);
        leaf = MakeMaterial("MR_ShrineExt_Cypress", new Color(0.055f, 0.22f, 0.16f), 0.04f, 0.3f, false);
        bark = MakeMaterial("MR_ShrineExt_Bark", new Color(0.22f, 0.16f, 0.1f), 0.02f, 0.18f, false);
        pigeonBody = MakeMaterial("MR_ShrineExt_PigeonBody", new Color(0.88f, 0.88f, 0.86f), 0f, 0.2f, false);
        pigeonWing = MakeMaterial("MR_ShrineExt_PigeonWing", new Color(0.72f, 0.74f, 0.76f), 0f, 0.2f, false);
        materialsReady = true;
    }

    private static Material MakeMaterial(string name, Color color, float metallic, float smoothness, bool emission)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Diffuse");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var material = new Material(shader);
        material.name = name;
        material.color = color;
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.35f);
        }
        return material;
    }
}
