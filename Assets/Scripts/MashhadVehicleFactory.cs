using UnityEngine;

/// <summary>Replaces the plain yellow box vehicles and overhead signs with varied models. Visual only.</summary>
public static class MashhadVehicleFactory
{
    private static readonly Color[] Paints =
    {
        new Color(0.88f, 0.88f, 0.86f), new Color(0.10f, 0.22f, 0.45f), new Color(0.62f, 0.12f, 0.12f), new Color(0.22f, 0.24f, 0.26f),
        new Color(0.70f, 0.72f, 0.74f), new Color(0.05f, 0.40f, 0.40f), new Color(0.80f, 0.55f, 0.15f), new Color(0.30f, 0.45f, 0.30f)
    };

    private static Material M(string name, float r, float g, float b, float metallic, float smooth, float emission)
    {
        return MashhadMaterials.Get(name, new Color(r, g, b), metallic, smooth, emission);
    }

    public static void DressVehicle(Transform vehicle)
    {
        var original = vehicle.GetComponent<Renderer>();
        if (original != null) original.enabled = false;
        for (int i = 0; i < vehicle.childCount; i++)
        {
            Transform child = vehicle.GetChild(i);
            if (child.name == "Vehicle Window") child.gameObject.SetActive(false);
        }

        Vector3 s = vehicle.localScale;
        if (s.x < 0.01f || s.y < 0.01f || s.z < 0.01f) return;

        var holder = new GameObject("MR_Vehicle_Model").transform;
        holder.SetParent(vehicle, false);
        holder.localPosition = new Vector3(0f, -0.5f, 0f);
        holder.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);

        int seed = Mathf.Abs(Mathf.RoundToInt(vehicle.position.z * 3f + vehicle.position.x * 17f));
        var batch = new MashhadMeshBatch();
        BuildCar(batch, seed % 6, seed / 6);
        batch.Build("MR_Vehicle_Body", holder, true);
    }

    private static void BuildCar(MashhadMeshBatch b, int type, int colorIndex)
    {
        int ci = colorIndex % Paints.Length;
        Color paintColor = type == 1 ? new Color(0.95f, 0.76f, 0.12f) : Paints[ci];
        Material paint = MashhadMaterials.Get("MR_Car_Paint_" + (type == 1 ? 99 : ci), paintColor, 0.45f, 0.55f, 0f);
        Material glass = M("MR_Car_Glass", 0.05f, 0.09f, 0.12f, 0.3f, 0.85f, 0f);
        Material tire = M("MR_Car_Tire", 0.03f, 0.03f, 0.035f, 0.05f, 0.2f, 0f);
        Material rim = M("MR_Car_Rim", 0.62f, 0.64f, 0.66f, 0.8f, 0.6f, 0f);
        Material dark = M("MR_Car_Trim", 0.06f, 0.06f, 0.07f, 0.2f, 0.3f, 0f);
        Material head = M("MR_Car_Head", 1f, 0.93f, 0.72f, 0.1f, 0.5f, 1.6f);
        Material tail = M("MR_Car_Tail", 0.85f, 0.06f, 0.05f, 0.1f, 0.5f, 1.4f);
        Material amber = M("MR_Car_Amber", 1f, 0.7f, 0.2f, 0.1f, 0.5f, 1.5f);
        Material white = M("MR_Car_White", 0.92f, 0.92f, 0.9f, 0.05f, 0.3f, 0f);
        Material cargo = M("MR_Car_Cargo", 0.45f, 0.30f, 0.18f, 0.02f, 0.2f, 0f);

        for (int zi = -1; zi <= 1; zi += 2)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float z = zi * 1.05f;
                b.Cylinder(tire, new Vector3(side * 0.72f, 0.31f, z), new Vector3(side * 0.92f, 0.31f, z), 0.31f, 0.31f, 10, true);
                b.Cylinder(rim, new Vector3(side * 0.9f, 0.31f, z), new Vector3(side * 0.95f, 0.31f, z), 0.17f, 0.17f, 8, true);
            }
        }

        switch (type)
        {
            case 3:
                b.Box(paint, new Vector3(0f, 0.85f, 0f), new Vector3(1.78f, 1.1f, 3.2f));
                b.Box(glass, new Vector3(0f, 1.1f, 0f), new Vector3(1.8f, 0.36f, 2.7f));
                b.Box(glass, new Vector3(0f, 1.1f, 1.6f), new Vector3(1.5f, 0.36f, 0.04f));
                b.Box(paint, new Vector3(0f, 1.45f, 0f), new Vector3(1.76f, 0.1f, 3.1f));
                break;
            case 4:
                b.Box(paint, new Vector3(0f, 0.52f, 0f), new Vector3(1.76f, 0.5f, 3.2f));
                b.Box(glass, new Vector3(0f, 1.05f, 0.6f), new Vector3(1.56f, 0.42f, 1.1f));
                b.Box(paint, new Vector3(0f, 1.3f, 0.6f), new Vector3(1.5f, 0.07f, 1f));
                b.Box(paint, new Vector3(0f, 0.84f, 1.35f), new Vector3(1.7f, 0.08f, 0.5f));
                b.Box(paint, new Vector3(-0.86f, 0.95f, -0.75f), new Vector3(0.06f, 0.35f, 1.5f));
                b.Box(paint, new Vector3(0.86f, 0.95f, -0.75f), new Vector3(0.06f, 0.35f, 1.5f));
                b.Box(paint, new Vector3(0f, 0.95f, -1.5f), new Vector3(1.7f, 0.35f, 0.06f));
                b.Box(cargo, new Vector3(0f, 1f, -0.75f), new Vector3(1.2f, 0.45f, 1f));
                break;
            case 5:
                b.Box(paint, new Vector3(0f, 0.62f, 0f), new Vector3(1.78f, 0.7f, 3.2f));
                b.Box(glass, new Vector3(0f, 1.2f, -0.1f), new Vector3(1.58f, 0.4f, 2.4f));
                b.Box(paint, new Vector3(0f, 1.45f, -0.1f), new Vector3(1.52f, 0.07f, 2.3f));
                break;
            case 2:
                b.Box(paint, new Vector3(0f, 0.52f, 0f), new Vector3(1.76f, 0.5f, 3.2f));
                b.Box(glass, new Vector3(0f, 1.05f, -0.45f), new Vector3(1.56f, 0.42f, 1.9f));
                b.Box(paint, new Vector3(0f, 1.3f, -0.45f), new Vector3(1.5f, 0.07f, 1.75f));
                b.Box(paint, new Vector3(0f, 0.84f, 1f), new Vector3(1.7f, 0.08f, 1.1f));
                break;
            default:
                b.Box(paint, new Vector3(0f, 0.52f, 0f), new Vector3(1.76f, 0.5f, 3.2f));
                b.Box(glass, new Vector3(0f, 1.05f, -0.15f), new Vector3(1.56f, 0.42f, 1.7f));
                b.Box(paint, new Vector3(0f, 1.3f, -0.15f), new Vector3(1.5f, 0.07f, 1.55f));
                b.Box(paint, new Vector3(0f, 0.84f, 0.95f), new Vector3(1.7f, 0.08f, 0.9f));
                b.Box(paint, new Vector3(0f, 0.84f, -1.2f), new Vector3(1.7f, 0.08f, 0.75f));
                if (type == 1)
                {
                    b.Box(amber, new Vector3(0f, 1.42f, -0.15f), new Vector3(0.5f, 0.12f, 0.2f));
                    b.Box(white, new Vector3(-0.89f, 0.62f, 0f), new Vector3(0.02f, 0.12f, 1.8f));
                    b.Box(white, new Vector3(0.89f, 0.62f, 0f), new Vector3(0.02f, 0.12f, 1.8f));
                }
                break;
        }

        b.Box(dark, new Vector3(0f, 0.4f, 1.62f), new Vector3(1.7f, 0.2f, 0.1f));
        b.Box(dark, new Vector3(0f, 0.4f, -1.62f), new Vector3(1.7f, 0.2f, 0.1f));
        b.Box(head, new Vector3(-0.6f, 0.62f, 1.61f), new Vector3(0.3f, 0.14f, 0.05f));
        b.Box(head, new Vector3(0.6f, 0.62f, 1.61f), new Vector3(0.3f, 0.14f, 0.05f));
        b.Box(tail, new Vector3(-0.62f, 0.65f, -1.61f), new Vector3(0.3f, 0.12f, 0.05f));
        b.Box(tail, new Vector3(0.62f, 0.65f, -1.61f), new Vector3(0.3f, 0.12f, 0.05f));
        b.Box(white, new Vector3(0f, 0.5f, -1.62f), new Vector3(0.5f, 0.12f, 0.02f));
    }

    public static void DressOverhead(Transform overhead)
    {
        var original = overhead.GetComponent<Renderer>();
        if (original != null) original.enabled = false;

        Vector3 s = overhead.localScale;
        if (s.x < 0.01f || s.y < 0.01f || s.z < 0.01f) return;

        var holder = new GameObject("MR_Overhead_Model").transform;
        holder.SetParent(overhead, false);
        holder.localPosition = new Vector3(0f, -1.65f / s.y, 0f);
        holder.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);

        int seed = Mathf.Abs(Mathf.RoundToInt(overhead.position.z * 2f + overhead.position.x * 13f));
        int variant = seed % 3;
        var b = new MashhadMeshBatch();
        Material metal = M("MR_Over_Metal", 0.2f, 0.23f, 0.26f, 0.6f, 0.5f, 0f);
        Material gold = M("MR_Over_Gold", 0.85f, 0.58f, 0.16f, 0.7f, 0.6f, 0f);
        Material white = M("MR_Over_White", 0.92f, 0.92f, 0.88f, 0.05f, 0.3f, 0f);

        if (variant == 0)
        {
            Material tile = M("MR_Over_Tile", 0.02f, 0.42f, 0.43f, 0.3f, 0.55f, 0f);
            Material lamp = M("MR_Over_Lamp", 1f, 0.7f, 0.2f, 0.1f, 0.5f, 1.5f);
            b.Cylinder(metal, new Vector3(-0.92f, 0f, 0f), new Vector3(-0.92f, 1.85f, 0f), 0.06f, 0.06f, 6, true);
            b.Cylinder(metal, new Vector3(0.92f, 0f, 0f), new Vector3(0.92f, 1.85f, 0f), 0.06f, 0.06f, 6, true);
            b.Box(tile, new Vector3(0f, 1.65f, 0f), new Vector3(1.8f, 0.34f, 0.1f));
            b.Box(white, new Vector3(0f, 1.65f, -0.06f), new Vector3(1.5f, 0.06f, 0.02f));
            b.Box(lamp, new Vector3(-0.82f, 1.65f, -0.06f), new Vector3(0.1f, 0.1f, 0.03f));
            b.Box(lamp, new Vector3(0.82f, 1.65f, -0.06f), new Vector3(0.1f, 0.1f, 0.03f));
        }
        else if (variant == 1)
        {
            Material red = M("MR_Over_Banner", 0.7f, 0.12f, 0.12f, 0.02f, 0.3f, 0f);
            b.Cylinder(metal, new Vector3(-0.92f, 0f, 0f), new Vector3(-0.92f, 1.9f, 0f), 0.05f, 0.05f, 6, true);
            b.Cylinder(metal, new Vector3(0.92f, 0f, 0f), new Vector3(0.92f, 1.9f, 0f), 0.05f, 0.05f, 6, true);
            b.Box(metal, new Vector3(0f, 1.82f, 0f), new Vector3(1.9f, 0.05f, 0.05f));
            b.Box(red, new Vector3(0f, 1.65f, 0f), new Vector3(1.6f, 0.34f, 0.04f));
            b.Box(gold, new Vector3(0f, 1.82f, 0f), new Vector3(1.66f, 0.04f, 0.05f));
            b.Box(gold, new Vector3(0f, 1.48f, 0f), new Vector3(1.66f, 0.04f, 0.05f));
        }
        else
        {
            Material wood = M("MR_Over_Wood", 0.45f, 0.30f, 0.18f, 0.02f, 0.2f, 0f);
            Material cloth1 = M("MR_Over_Cloth1", 0.75f, 0.18f, 0.15f, 0.02f, 0.3f, 0f);
            Material cloth2 = M("MR_Over_Cloth2", 0.9f, 0.65f, 0.15f, 0.02f, 0.3f, 0f);
            b.Cylinder(wood, new Vector3(-0.92f, 0f, 0f), new Vector3(-0.92f, 1.8f, 0f), 0.07f, 0.07f, 6, true);
            b.Cylinder(wood, new Vector3(0.92f, 0f, 0f), new Vector3(0.92f, 1.8f, 0f), 0.07f, 0.07f, 6, true);
            b.Box(wood, new Vector3(0f, 1.68f, 0f), new Vector3(1.8f, 0.2f, 0.4f));
            for (int i = 0; i < 6; i++)
            {
                b.Box(i % 2 == 0 ? cloth1 : cloth2, new Vector3(-0.75f + i * 0.3f, 1.55f, 0f), new Vector3(0.29f, 0.14f, 0.46f));
            }
        }
        b.Build("MR_Overhead_Body", holder, true);
    }
}
