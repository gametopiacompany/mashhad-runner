using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reshapes the BRT corridor at runtime without touching RunnerGame: thins out stations, removes the repeated
/// buildings, builds varied street segments ahead of the player, puts people in stations and swaps the plain
/// vehicle and sign boxes for varied models. Visual only.
/// </summary>
public sealed class MashhadCorridorDirector : MonoBehaviour
{
    private const float ScanInterval = 0.25f;
    private const float BuildAhead = 270f;
    private const float KeepBehind = 90f;

    private sealed class Pending
    {
        public int index;
        public int nextStep;
        public readonly MashhadMeshBatch batch = new MashhadMeshBatch();
    }

    private readonly Dictionary<int, GameObject> built = new Dictionary<int, GameObject>();
    private readonly HashSet<int> handled = new HashSet<int>();
    private readonly HashSet<int> seen = new HashSet<int>();
    private readonly List<int> removal = new List<int>();
    private Pending pending;
    private RunnerGame game;
    private Transform player;
    private float nextScan;
    private float lastPlayerZ;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<MashhadCorridorDirector>() != null) return;
        var host = new GameObject("MR_Corridor_Director");
        DontDestroyOnLoad(host);
        host.AddComponent<MashhadCorridorDirector>();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + ScanInterval;
            Scan();
        }
        StepBuild();
    }

    private void Scan()
    {
        if (game == null) game = FindObjectOfType<RunnerGame>();
        if (game == null)
        {
            ClearAll();
            player = null;
            return;
        }

        if (player == null)
        {
            GameObject go = GameObject.Find("Player");
            if (go == null) return;
            player = go.transform;
            ClearAll();
        }

        float z = player.position.z;
        if (z < lastPlayerZ - 150f) ClearAll();
        lastPlayerZ = z;

        ManageSegments(z);
        ProcessRoots();
    }

    private void ManageSegments(float z)
    {
        int first = Mathf.FloorToInt((z - KeepBehind) / MashhadCityBuilder.SegmentLength);
        int last = Mathf.FloorToInt((z + BuildAhead) / MashhadCityBuilder.SegmentLength);

        removal.Clear();
        foreach (var pair in built)
        {
            if (pair.Key < first) removal.Add(pair.Key);
        }
        for (int i = 0; i < removal.Count; i++)
        {
            GameObject old = built[removal[i]];
            if (old != null) Destroy(old);
            built.Remove(removal[i]);
        }

        if (pending != null) return;
        for (int s = Mathf.Max(first, -1); s <= last; s++)
        {
            if (built.ContainsKey(s)) continue;
            pending = new Pending();
            pending.index = s;
            break;
        }
    }

    private void StepBuild()
    {
        if (pending == null) return;
        MashhadCityBuilder.BuildStep(pending.batch, pending.index, pending.nextStep);
        pending.nextStep++;
        if (pending.nextStep < MashhadCityBuilder.StepCount) return;

        GameObject root = pending.batch.Build("MR_Corridor_Segment_" + pending.index, transform, false);
        MashhadCityBuilder.AddWalkers(root.transform, pending.index);
        built[pending.index] = root;
        pending = null;
    }

    private void ClearAll()
    {
        foreach (var pair in built)
        {
            if (pair.Value != null) Destroy(pair.Value);
        }
        built.Clear();
        handled.Clear();
        pending = null;
    }

    private static bool KeepStation(float z)
    {
        int index = Mathf.RoundToInt(z / 24f);
        return index % 4 == 0 && !MashhadCityBuilder.InGateZone(z, 9f);
    }

    private void ProcessRoots()
    {
        seen.Clear();
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            string n = root.name;
            if (n.Length < 8) continue;
            Transform t = root.transform;
            float z = t.position.z;
            int id = root.GetInstanceID();

            if (n == "MR_Original_Mashhad_City_Building" || n == "Vehicle Window")
            {
                Destroy(root);
            }
            else if (n == "MR_Original_BRT_Station")
            {
                if (!KeepStation(z))
                {
                    Destroy(root);
                    continue;
                }
                seen.Add(id);
                if (handled.Add(id)) DressStation(t);
            }
            else if (n == "BRT Right Pole")
            {
                int index = Mathf.RoundToInt(z / 24f);
                if (index % 2 != 0 || MashhadCityBuilder.InGateZone(z, 2f)) Destroy(root);
            }
            else if (n == "MR_Original_Street_Light" || n == "MR_Original_BRT_Wayfinding_Sign")
            {
                if (MashhadCityBuilder.InGateZone(z, 4f))
                {
                    Destroy(root);
                    continue;
                }
                Vector3 p = t.position;
                if (p.x < 0f && p.x > -6.6f) t.position = new Vector3(-6.9f, p.y, p.z);
            }
            else if (n == "MR_Original_Cypress_Street_Tree")
            {
                if (MashhadCityBuilder.InGateZone(z, 4f)) Destroy(root);
            }
            else if (n == "MR_Original_BRT_Bus")
            {
                if (MashhadCityBuilder.NearStation(z, 9.5f) || MashhadCityBuilder.InGateZone(z, 6f)) Destroy(root);
            }
            else if (n == "Simple Vehicle")
            {
                seen.Add(id);
                if (handled.Add(id)) MashhadVehicleFactory.DressVehicle(t);
            }
            else if (n == "Overhead Sign")
            {
                seen.Add(id);
                if (handled.Add(id)) MashhadVehicleFactory.DressOverhead(t);
            }
        }
        handled.IntersectWith(seen);
    }

    private static void DressStation(Transform station)
    {
        int seed = Mathf.Abs(Mathf.RoundToInt(station.position.z));
        MashhadPeopleFactory.Create(station, new Vector3(0.5f, 0.26f, -1.1f), 90f, seed, MashhadPeopleFactory.Pose.Phone);
        MashhadPeopleFactory.Create(station, new Vector3(0.52f, 0.26f, 0.4f), 90f, seed + 1, MashhadPeopleFactory.Pose.Newspaper);
        MashhadPeopleFactory.Create(station, new Vector3(-0.12f, 0.79f, 0.65f), 90f, seed + 2, MashhadPeopleFactory.Pose.SeatedNewspaper);
        MashhadPeopleFactory.Create(station, new Vector3(-0.12f, 0.79f, 1.65f), 90f, seed + 3, MashhadPeopleFactory.Pose.SeatedPhone);
        MashhadPeopleFactory.Create(station, new Vector3(0.55f, 0.26f, 2.3f), 90f, seed + 4, MashhadPeopleFactory.Pose.Standing);
    }
}
