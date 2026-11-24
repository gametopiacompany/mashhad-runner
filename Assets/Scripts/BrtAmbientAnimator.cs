using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds lightweight ambient animation to the BRT corridor: cypress sway, waving pennant flags on the
/// roadside poles and chase lights on station platforms. Visual only: no colliders, no gameplay logic.
/// Objects are found by name; objects that are missing are simply skipped. Entries farther than
/// ActiveDistance from the camera are not updated, and motion stops while the game is paused.
/// </summary>
public sealed class BrtAmbientAnimator : MonoBehaviour
{
    private const string TreeName = "MR_Original_Cypress_Street_Tree";
    private const string PoleName = "BRT Right Pole";
    private const string StationName = "MR_Original_BRT_Station";
    private const string FlagName = "MR_BRT_Pennant";
    private const string ChaseName = "MR_BRT_Platform_Chase";
    private const float ScanInterval = 1f;
    private const float ActivityInterval = 0.25f;
    private const float ActiveDistance = 75f;
    private const float ChaseStep = 0.05f;
    private const int FlagSegments = 4;
    private const int ChevronCount = 6;

    private static readonly Color ChevronDim = new Color(0.16f, 0.1f, 0.05f);
    private static readonly Color ChevronLit = new Color(1f, 0.66f, 0.22f);

    private sealed class TreeEntry
    {
        public int id;
        public Transform tree;
        public Quaternion baseRotation;
        public float phase;
        public bool active = true;
    }

    private sealed class FlagEntry
    {
        public int id;
        public Transform pole;
        public Transform root;
        public Transform[] segments;
        public float phase;
        public bool active = true;
    }

    private sealed class StationEntry
    {
        public int id;
        public Transform station;
        public Renderer[] chevrons;
        public float phase;
        public bool active = true;
    }

    private static Material flagTeal;
    private static Material flagGold;
    private static Material chevronMaterial;

    private readonly List<TreeEntry> trees = new List<TreeEntry>();
    private readonly List<FlagEntry> flags = new List<FlagEntry>();
    private readonly List<StationEntry> stations = new List<StationEntry>();
    private readonly HashSet<int> known = new HashSet<int>();
    private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

    private Camera viewCamera;
    private float nextScan;
    private float nextActivity;
    private float chaseTimer;
    private float time;
    private int flagCounter;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<BrtAmbientAnimator>() != null) return;
        var host = new GameObject("MR_BRT_Ambient_Animator");
        DontDestroyOnLoad(host);
        host.AddComponent<BrtAmbientAnimator>();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + ScanInterval;
            Scan();
        }

        if (Time.unscaledTime >= nextActivity)
        {
            nextActivity = Time.unscaledTime + ActivityInterval;
            RefreshActivity();
        }

        if (Time.deltaTime <= 0f) return;
        time += Time.deltaTime;

        AnimateTrees();
        AnimateFlags();

        chaseTimer += Time.deltaTime;
        if (chaseTimer >= ChaseStep)
        {
            chaseTimer = 0f;
            AnimateStations();
        }
    }

    private void Scan()
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var root = roots[i];
            string rootName = root.name;
            if (rootName != TreeName && rootName != PoleName && rootName != StationName) continue;

            int id = root.GetInstanceID();
            if (!known.Add(id)) continue;

            if (rootName == TreeName) AddTree(root.transform, id);
            else if (rootName == PoleName) AddFlag(root.transform, id);
            else AddStation(root.transform, id);
        }
    }

    private static float PhaseFor(int id)
    {
        return (id & 1023) * 0.017f;
    }

    private void AddTree(Transform tree, int id)
    {
        trees.Add(new TreeEntry
        {
            id = id,
            tree = tree,
            baseRotation = tree.localRotation,
            phase = PhaseFor(id)
        });
    }

    private void AddFlag(Transform pole, int id)
    {
        EnsureMaterials();

        var root = new GameObject(FlagName).transform;
        root.position = pole.position + new Vector3(0f, 1.55f, 0f);
        root.rotation = Quaternion.Euler(0f, 180f, 0f);

        var segments = new Transform[FlagSegments];
        Transform parent = root;
        for (int s = 0; s < FlagSegments; s++)
        {
            var segment = new GameObject("Segment").transform;
            segment.SetParent(parent, false);
            segment.localPosition = new Vector3(s == 0 ? 0.09f : 0.28f, 0f, 0f);

            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel";
            panel.transform.SetParent(segment, false);
            panel.transform.localPosition = new Vector3(0.14f, 0f, 0f);
            panel.transform.localScale = new Vector3(0.28f, 0.5f - s * 0.07f, 0.02f);
            PrepareRenderer(panel, (s + flagCounter) % 2 == 0 ? flagTeal : flagGold);

            segments[s] = segment;
            parent = segment;
        }
        flagCounter++;

        flags.Add(new FlagEntry
        {
            id = id,
            pole = pole,
            root = root,
            segments = segments,
            phase = PhaseFor(id)
        });
    }

    private void AddStation(Transform station, int id)
    {
        EnsureMaterials();

        var holder = new GameObject(ChaseName).transform;
        holder.SetParent(station, false);

        var chevrons = new Renderer[ChevronCount];
        for (int c = 0; c < ChevronCount; c++)
        {
            var chevron = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chevron.name = "Chevron";
            chevron.transform.SetParent(holder, false);
            chevron.transform.localPosition = new Vector3(1.12f, 0.275f, -3f + c * 1.2f);
            chevron.transform.localScale = new Vector3(0.14f, 0.03f, 0.5f);
            chevrons[c] = PrepareRenderer(chevron, chevronMaterial);
        }

        stations.Add(new StationEntry
        {
            id = id,
            station = station,
            chevrons = chevrons,
            phase = PhaseFor(id)
        });
    }

    private void RefreshActivity()
    {
        if (viewCamera == null) viewCamera = Camera.main;
        bool hasCamera = viewCamera != null;
        Vector3 camPos = hasCamera ? viewCamera.transform.position : Vector3.zero;
        float limit = ActiveDistance * ActiveDistance;

        for (int i = trees.Count - 1; i >= 0; i--)
        {
            var entry = trees[i];
            if (entry.tree == null)
            {
                known.Remove(entry.id);
                trees.RemoveAt(i);
                continue;
            }
            entry.active = !hasCamera || (entry.tree.position - camPos).sqrMagnitude < limit;
        }

        for (int i = flags.Count - 1; i >= 0; i--)
        {
            var entry = flags[i];
            if (entry.pole == null)
            {
                if (entry.root != null) Destroy(entry.root.gameObject);
                known.Remove(entry.id);
                flags.RemoveAt(i);
                continue;
            }
            entry.active = !hasCamera || (entry.pole.position - camPos).sqrMagnitude < limit;
        }

        for (int i = stations.Count - 1; i >= 0; i--)
        {
            var entry = stations[i];
            if (entry.station == null)
            {
                known.Remove(entry.id);
                stations.RemoveAt(i);
                continue;
            }
            entry.active = !hasCamera || (entry.station.position - camPos).sqrMagnitude < limit;
        }
    }

    private void AnimateTrees()
    {
        for (int i = 0; i < trees.Count; i++)
        {
            var entry = trees[i];
            if (!entry.active || entry.tree == null) continue;

            float a = time * 0.9f + entry.phase;
            float b = time * 0.65f + entry.phase * 1.7f;
            entry.tree.localRotation = entry.baseRotation * Quaternion.Euler(Mathf.Sin(a) * 0.9f, 0f, Mathf.Sin(b) * 1.4f);
        }
    }

    private void AnimateFlags()
    {
        for (int i = 0; i < flags.Count; i++)
        {
            var entry = flags[i];
            if (!entry.active || entry.pole == null) continue;

            for (int s = 0; s < entry.segments.Length; s++)
            {
                float swing = Mathf.Sin(time * 3.2f - s * 0.9f + entry.phase) * (10f + s * 5f);
                entry.segments[s].localRotation = Quaternion.Euler(0f, swing, 0f);
            }
        }
    }

    private void AnimateStations()
    {
        for (int i = 0; i < stations.Count; i++)
        {
            var entry = stations[i];
            if (!entry.active || entry.station == null) continue;

            int lit = Mathf.FloorToInt(time * 5f + entry.phase * 10f) % (ChevronCount + 2);
            for (int c = 0; c < entry.chevrons.Length; c++)
            {
                float intensity = c == lit ? 1f : (c == lit - 1 ? 0.35f : 0f);
                SetChevron(entry.chevrons[c], intensity);
            }
        }
    }

    private void SetChevron(Renderer chevronRenderer, float intensity)
    {
        if (chevronRenderer == null) return;
        chevronRenderer.GetPropertyBlock(block);
        block.SetColor("_Color", Color.Lerp(ChevronDim, ChevronLit, intensity));
        block.SetColor("_EmissionColor", ChevronLit * (intensity * 2.2f));
        chevronRenderer.SetPropertyBlock(block);
    }

    private static Renderer PrepareRenderer(GameObject go, Material material)
    {
        var collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        var partRenderer = go.GetComponent<Renderer>();
        partRenderer.sharedMaterial = material;
        partRenderer.shadowCastingMode = ShadowCastingMode.Off;
        partRenderer.receiveShadows = false;
        return partRenderer;
    }

    private static void EnsureMaterials()
    {
        if (flagTeal != null && flagGold != null && chevronMaterial != null) return;
        flagTeal = MakeMaterial("MR_BRT_Flag_Turquoise", new Color(0.02f, 0.58f, 0.57f), 0.1f, 0.35f, false);
        flagGold = MakeMaterial("MR_BRT_Flag_Gold", new Color(0.83f, 0.53f, 0.12f), 0.3f, 0.45f, false);
        chevronMaterial = MakeMaterial("MR_BRT_Platform_Chevron", ChevronDim, 0.05f, 0.3f, true);
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
            material.SetColor("_EmissionColor", Color.black);
        }
        return material;
    }
}
