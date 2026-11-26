using UnityEngine;

/// <summary>Procedural street dressing, varied buildings and the shrine gateway. Visual only.</summary>
public static class MashhadCityBuilder
{
    public const int SegmentLength = 96;
    public const int StepCount = 7;
    private const float GateHalfZone = 22f;
    private const float GateHalfLength = 18f;

    private static bool ready;
    private static Material[] facades;
    private static Material[] awnings;
    private static Material[] signs;
    private static Material trim, tile, gold, glassDark, windowLit, paving, curb, busLane, laneLine, groundExt;
    private static Material leaf, leafLight, bark, metal, wood, roofDark, tank, lantern, banner, bannerAlt;

    private static readonly float[] RowFront = { 17.2f, 27f, 39f };
    private static readonly float[] RowDepth = { 8f, 9f, 10f };
    private static readonly float[] RowWMin = { 9f, 12f, 14f };
    private static readonly float[] RowWMax = { 14f, 20f, 24f };
    private static readonly float[] RowHMin = { 11f, 22f, 34f };
    private static readonly float[] RowHMax = { 24f, 40f, 62f };

    public static bool IsGateSegment(int segment)
    {
        return segment >= 0 && segment % 8 == 5;
    }

    public static float GateCenter(int segment)
    {
        return segment * SegmentLength + SegmentLength * 0.5f;
    }

    public static bool InGateZone(float z, float margin)
    {
        int segment = Mathf.FloorToInt(z / SegmentLength);
        if (!IsGateSegment(segment)) return false;
        return Mathf.Abs(z - GateCenter(segment)) < GateHalfZone + margin;
    }

    public static bool NearStation(float z, float margin)
    {
        return Mathf.Abs(z - Mathf.Round(z / SegmentLength) * SegmentLength) < margin;
    }

    private static bool IntersectsGate(int segment, float z0, float z1)
    {
        if (!IsGateSegment(segment)) return false;
        float c = GateCenter(segment);
        return z1 > c - GateHalfZone && z0 < c + GateHalfZone;
    }

    private static bool NearShrine(float z0, float z1)
    {
        if (z1 > 72f && z0 < 112f) return true;
        int k = Mathf.FloorToInt((z0 - 132f) / 320f);
        for (int i = k; i <= k + 2; i++)
        {
            if (i < 1) continue;
            float sz = 132f + 320f * i;
            if (z1 > sz - 20f && z0 < sz + 20f) return true;
        }
        return false;
    }

    private static Material M(string name, float r, float g, float b)
    {
        return MashhadMaterials.Get(name, new Color(r, g, b), 0.04f, 0.25f, 0f);
    }

    private static Material Mx(string name, float r, float g, float b, float metallic, float smooth)
    {
        return MashhadMaterials.Get(name, new Color(r, g, b), metallic, smooth, 0f);
    }

    private static Material Em(string name, float r, float g, float b, float emission)
    {
        return MashhadMaterials.Get(name, new Color(r, g, b), 0.05f, 0.4f, emission);
    }

    private static void EnsureMaterials()
    {
        if (ready && trim != null) return;
        facades = new Material[]
        {
            M("MR_C_Sand", 0.84f, 0.74f, 0.58f), M("MR_C_Cream", 0.90f, 0.84f, 0.72f), M("MR_C_Brick", 0.66f, 0.38f, 0.28f), M("MR_C_Pink", 0.86f, 0.72f, 0.66f),
            M("MR_C_Concrete", 0.62f, 0.62f, 0.60f), M("MR_C_White", 0.93f, 0.91f, 0.86f), M("MR_C_Ochre", 0.78f, 0.60f, 0.30f), M("MR_C_BlueGrey", 0.48f, 0.55f, 0.60f)
        };
        awnings = new Material[]
        {
            M("MR_C_AwnRed", 0.75f, 0.18f, 0.15f), M("MR_C_AwnTeal", 0.05f, 0.50f, 0.52f), M("MR_C_AwnMustard", 0.90f, 0.65f, 0.15f),
            M("MR_C_AwnGreen", 0.12f, 0.40f, 0.25f), M("MR_C_AwnNavy", 0.15f, 0.22f, 0.48f), M("MR_C_AwnCream", 0.92f, 0.90f, 0.84f)
        };
        signs = new Material[]
        {
            M("MR_C_SignBlue", 0.12f, 0.30f, 0.60f), M("MR_C_SignRed", 0.70f, 0.14f, 0.14f), M("MR_C_SignGreen", 0.10f, 0.45f, 0.30f),
            M("MR_C_SignBlack", 0.10f, 0.10f, 0.12f), M("MR_C_SignOrange", 0.90f, 0.45f, 0.12f)
        };
        trim = M("MR_C_Trim", 0.17f, 0.14f, 0.12f);
        tile = Mx("MR_C_Tile", 0.02f, 0.42f, 0.43f, 0.3f, 0.55f);
        gold = Mx("MR_C_Gold", 0.85f, 0.58f, 0.16f, 0.7f, 0.6f);
        glassDark = Mx("MR_C_Glass", 0.07f, 0.14f, 0.20f, 0.3f, 0.8f);
        windowLit = Em("MR_C_WindowLit", 1f, 0.74f, 0.38f, 1.6f);
        paving = M("MR_C_Paving", 0.60f, 0.55f, 0.50f);
        curb = M("MR_C_Curb", 0.78f, 0.76f, 0.72f);
        busLane = M("MR_C_BusLane", 0.58f, 0.19f, 0.16f);
        laneLine = M("MR_C_LaneLine", 0.92f, 0.92f, 0.88f);
        groundExt = M("MR_C_Ground", 0.58f, 0.50f, 0.40f);
        leaf = M("MR_C_Leaf", 0.14f, 0.34f, 0.18f);
        leafLight = M("MR_C_LeafLight", 0.28f, 0.46f, 0.22f);
        bark = M("MR_C_Bark", 0.25f, 0.17f, 0.11f);
        metal = Mx("MR_C_Metal", 0.20f, 0.23f, 0.26f, 0.6f, 0.5f);
        wood = M("MR_C_Wood", 0.45f, 0.30f, 0.18f);
        roofDark = M("MR_C_Roof", 0.30f, 0.28f, 0.27f);
        tank = Mx("MR_C_Tank", 0.72f, 0.74f, 0.76f, 0.5f, 0.4f);
        lantern = Em("MR_C_Lantern", 1f, 0.78f, 0.40f, 2f);
        banner = M("MR_C_Banner", 0.62f, 0.10f, 0.12f);
        bannerAlt = M("MR_C_BannerAlt", 0.04f, 0.46f, 0.48f);
        ready = true;
    }

    public static void BuildStep(MashhadMeshBatch b, int s, int step)
    {
        EnsureMaterials();
        switch (step)
        {
            case 0: BuildStreet(b, s); break;
            case 1: BuildShops(b, s, 1); break;
            case 2: BuildRow(b, s, 1, 0); break;
            case 3: BuildRow(b, s, 1, 1); BuildRow(b, s, 1, 2); break;
            case 4: BuildShops(b, s, -1); break;
            case 5: BuildRow(b, s, -1, 0); break;
            default: BuildRow(b, s, -1, 1); BuildRow(b, s, -1, 2); break;
        }
        b.ResetTransform();
    }

    private static void BuildStreet(MashhadMeshBatch b, int s)
    {
        b.ResetTransform();
        float len = SegmentLength;
        float zc = s * len + len * 0.5f;
        b.Box(groundExt, new Vector3(40f, -0.6f, zc), new Vector3(60f, 1f, len), false);
        b.Box(groundExt, new Vector3(-40f, -0.6f, zc), new Vector3(60f, 1f, len), false);
        b.Box(curb, new Vector3(4.125f, 0.08f, zc), new Vector3(0.25f, 0.16f, len), false);
        b.Box(paving, new Vector3(7.625f, 0.07f, zc), new Vector3(6.75f, 0.14f, len), false);
        b.Box(busLane, new Vector3(-5.15f, 0f, zc), new Vector3(2.3f, 0.08f, len), false);
        b.Box(laneLine, new Vector3(-4.08f, 0.045f, zc), new Vector3(0.1f, 0.01f, len), false);
        b.Box(curb, new Vector3(-6.4f, 0.09f, zc), new Vector3(0.2f, 0.18f, len), false);
        b.Box(paving, new Vector3(-8.65f, 0.07f, zc), new Vector3(4.7f, 0.14f, len), false);
        if (IsGateSegment(s)) BuildGate(b, GateCenter(s));
    }

    private static int PickStyle(System.Random rng, int row)
    {
        double r = rng.NextDouble();
        if (row == 0)
        {
            if (r < 0.30) return 0;
            if (r < 0.58) return 1;
            if (r < 0.78) return 2;
            if (r < 0.90) return 4;
            return 3;
        }
        if (r < 0.45) return 1;
        if (r < 0.75) return 2;
        return 4;
    }

    private static void BuildRow(MashhadMeshBatch b, int s, int side, int row)
    {
        var rng = new System.Random(s * 7919 + (side + 2) * 104729 + row * 1299709 + 17);
        float z0 = s * SegmentLength;
        float z1 = z0 + SegmentLength;
        float z = z0;
        while (z < z1 - 2f)
        {
            float w = Mathf.Lerp(RowWMin[row], RowWMax[row], (float)rng.NextDouble());
            if (z + w > z1 - 5f) w = z1 - z;
            bool skip = IntersectsGate(s, z, z + w) || (side > 0 && NearShrine(z, z + w));
            int style = PickStyle(rng, row);
            float depth = RowDepth[row] * (0.85f + 0.3f * (float)rng.NextDouble());
            float h = Mathf.Lerp(RowHMin[row], RowHMax[row], (float)rng.NextDouble());
            if (style == 3) h = 8f;
            float bw = style == 3 ? Mathf.Min(w, 14f) : w;
            if (!skip)
            {
                float x = side * (RowFront[row] + depth * 0.5f);
                Building(b, rng, x, z + w * 0.5f, side > 0 ? 90f : -90f, bw - 0.4f, depth, h, style, row);
            }
            z += w;
        }
    }

    private static void Building(MashhadMeshBatch b, System.Random rng, float x, float z, float yaw, float width, float depth, float height, int style, int row)
    {
        b.SetTransform(new Vector3(x, 0f, z), yaw);
        Material wall = facades[rng.Next(facades.Length)];
        float hd = depth * 0.5f;
        int floors = Mathf.Max(2, Mathf.RoundToInt(height / 3.2f));
        float floorH = height / floors;

        b.Box(wall, new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, depth), false);
        b.Box(trim, new Vector3(0f, 0.45f, -hd - 0.04f), new Vector3(width + 0.1f, 0.9f, 0.1f), false);
        b.Box(wall, new Vector3(0f, height + 0.3f, 0f), new Vector3(width + 0.3f, 0.6f, depth + 0.3f));

        if (row == 0) NearBuilding(b, rng, style, width, depth, height, floors, floorH);
        else FarBuilding(b, rng, style, width, depth, height, floors, floorH);
        if (style != 3) Rooftop(b, rng, width, depth, height);
    }

    private static void NearBuilding(MashhadMeshBatch b, System.Random rng, int style, float width, float depth, float height, int floors, float floorH)
    {
        float hd = depth * 0.5f;
        int columns = Mathf.Max(2, Mathf.RoundToInt(width / 3f));
        switch (style)
        {
            case 0:
                Storefront(b, rng, width, hd);
                Windows(b, rng, width, -hd, 1, floors, floorH, columns, 0.35f, true);
                break;
            case 1:
                b.Box(glassDark, new Vector3(0f, 1.4f, -hd - 0.03f), new Vector3(width * 0.5f, 2.4f, 0.08f), false);
                b.Box(trim, new Vector3(width * 0.5f - 0.6f, height * 0.5f, -hd - 0.05f), new Vector3(0.5f, height, 0.06f), false);
                Windows(b, rng, width, -hd, 1, floors, floorH, columns, 0.4f, true);
                break;
            case 2:
                Windows(b, rng, width, -hd, 1, floors, floorH, columns, 0.4f, false);
                for (int f = 2; f < floors; f += 3)
                {
                    b.Box(tile, new Vector3(0f, f * floorH, -hd - 0.05f), new Vector3(width + 0.1f, 0.5f, 0.12f), false);
                }
                b.Box(tile, new Vector3(-width * 0.5f + 0.3f, height * 0.5f, -hd - 0.06f), new Vector3(0.5f, height, 0.14f), false);
                b.Box(tile, new Vector3(width * 0.5f - 0.3f, height * 0.5f, -hd - 0.06f), new Vector3(0.5f, height, 0.14f), false);
                b.Box(trim, new Vector3(0f, 1.3f, -hd - 0.05f), new Vector3(1.8f, 2.6f, 0.12f), false);
                b.Cylinder(gold, new Vector3(0f, height - 2.2f, -hd - 0.05f), new Vector3(0f, height - 2.2f, -hd - 0.25f), 1.1f, 1.1f, 12, true);
                b.Cylinder(tile, new Vector3(0f, height - 2.2f, -hd - 0.25f), new Vector3(0f, height - 2.2f, -hd - 0.32f), 0.75f, 0.75f, 12, true);
                break;
            case 3:
                Mosque(b, width, depth, height);
                break;
            default:
                b.Box(glassDark, new Vector3(0f, height * 0.5f + 0.5f, -hd - 0.03f), new Vector3(width - 0.8f, height - 1.4f, 0.1f), false);
                for (int f = 1; f < floors; f++)
                {
                    b.Box(trim, new Vector3(0f, f * floorH, -hd - 0.06f), new Vector3(width, 0.22f, 0.12f), false);
                    for (int k = -1; k <= 1; k++)
                    {
                        if (rng.NextDouble() < 0.3)
                        {
                            b.Box(windowLit, new Vector3(k * width * 0.28f, f * floorH + floorH * 0.5f, -hd - 0.1f), new Vector3(width * 0.22f, floorH * 0.6f, 0.05f), false);
                        }
                    }
                }
                break;
        }
    }

    private static void FarBuilding(MashhadMeshBatch b, System.Random rng, int style, float width, float depth, float height, int floors, float floorH)
    {
        float hd = depth * 0.5f;
        for (int f = 1; f < floors; f++)
        {
            float y = f * floorH + floorH * 0.45f;
            for (int k = -1; k <= 1; k++)
            {
                Material m = rng.NextDouble() < 0.33 ? windowLit : glassDark;
                b.Box(m, new Vector3(k * width * 0.3f, y, -hd - 0.03f), new Vector3(width * 0.22f, floorH * 0.42f, 0.07f), false);
            }
            if (style == 2 && f % 4 == 0)
            {
                b.Box(tile, new Vector3(0f, f * floorH, -hd - 0.05f), new Vector3(width + 0.1f, 0.45f, 0.12f), false);
            }
        }
        b.Box(trim, new Vector3(-width * 0.5f + 0.3f, height * 0.5f, -hd - 0.05f), new Vector3(0.5f, height, 0.12f), false);
        b.Box(trim, new Vector3(width * 0.5f - 0.3f, height * 0.5f, -hd - 0.05f), new Vector3(0.5f, height, 0.12f), false);
        if (style == 4) b.Box(windowLit, new Vector3(0f, height - 0.3f, -hd - 0.06f), new Vector3(width * 0.8f, 0.3f, 0.08f), false);
    }

    private static void Windows(MashhadMeshBatch b, System.Random rng, float width, float frontZ, int firstFloor, int floors, float floorH, int columns, float litChance, bool balconies)
    {
        float spacing = width / columns;
        for (int f = firstFloor; f < floors; f++)
        {
            float y = f * floorH + floorH * 0.55f;
            for (int c = 0; c < columns; c++)
            {
                float cx = -width * 0.5f + spacing * (c + 0.5f);
                Material m = rng.NextDouble() < litChance ? windowLit : glassDark;
                b.Box(m, new Vector3(cx, y, frontZ - 0.03f), new Vector3(spacing * 0.5f, floorH * 0.45f, 0.08f), false);
                if (balconies)
                {
                    b.Box(curb, new Vector3(cx, y - floorH * 0.3f, frontZ - 0.4f), new Vector3(spacing * 0.8f, 0.1f, 0.8f));
                    b.Box(metal, new Vector3(cx, y - floorH * 0.3f + 0.3f, frontZ - 0.78f), new Vector3(spacing * 0.8f, 0.5f, 0.04f), false);
                }
            }
        }
    }

    private static void Storefront(MashhadMeshBatch b, System.Random rng, float width, float hd)
    {
        Material awning = awnings[rng.Next(awnings.Length)];
        Material awning2 = awnings[rng.Next(awnings.Length)];
        b.Box(glassDark, new Vector3(0f, 1.5f, -hd - 0.03f), new Vector3(width - 1.6f, 2.5f, 0.08f), false);
        b.Box(trim, new Vector3(-width * 0.5f + 1.1f, 1.2f, -hd - 0.05f), new Vector3(1f, 2.4f, 0.1f), false);
        int n = 4;
        float seg = (width - 0.8f) / n;
        for (int i = 0; i < n; i++)
        {
            float cx = -(width - 0.8f) * 0.5f + seg * (i + 0.5f);
            Material m = i % 2 == 0 ? awning : awning2;
            b.Box(m, new Vector3(cx, 3f, -hd - 0.6f), new Vector3(seg - 0.02f, 0.1f, 1.2f));
            b.Box(m, new Vector3(cx, 2.85f, -hd - 1.18f), new Vector3(seg - 0.02f, 0.3f, 0.05f));
        }
        b.Box(signs[rng.Next(signs.Length)], new Vector3(0f, 3.75f, -hd - 0.08f), new Vector3(width * 0.6f, 0.6f, 0.12f));
        b.Box(laneLine, new Vector3(0f, 3.75f, -hd - 0.16f), new Vector3(width * 0.45f, 0.12f, 0.04f));
    }

    private static void Mosque(MashhadMeshBatch b, float width, float depth, float height)
    {
        float hd = depth * 0.5f;
        b.Box(tile, new Vector3(0f, height * 0.5f, -hd - 0.1f), new Vector3(width * 0.45f, height * 0.9f, 0.2f), false);
        b.Box(trim, new Vector3(0f, height * 0.4f, -hd - 0.25f), new Vector3(width * 0.3f, height * 0.7f, 0.12f), false);
        b.Box(gold, new Vector3(0f, 0.25f, -hd - 0.3f), new Vector3(width * 0.3f, 0.2f, 0.14f), false);
        float domeR = Mathf.Min(width, depth) * 0.3f;
        b.Cylinder(tile, new Vector3(0f, height + 0.6f, 0f), new Vector3(0f, height + 2.2f, 0f), domeR, domeR, 14, true);
        b.Ellipsoid(gold, new Vector3(0f, height + 2.2f, 0f), new Vector3(domeR, domeR * 0.95f, domeR), 5, 14, true);
        float top = height + 2.2f + domeR * 0.95f;
        b.Cylinder(gold, new Vector3(0f, top, 0f), new Vector3(0f, top + 2f, 0f), 0.18f, 0f, 6, true);
        for (int side = -1; side <= 1; side += 2)
        {
            float mx = side * (width * 0.5f - 1.1f);
            float mz = -hd + 1.1f;
            b.Cylinder(tile, new Vector3(mx, 0f, mz), new Vector3(mx, height + 7f, mz), 0.75f, 0.55f, 8, true);
            b.Cylinder(gold, new Vector3(mx, height + 4.5f, mz), new Vector3(mx, height + 4.9f, mz), 1.2f, 1.2f, 8, true);
            b.Cylinder(gold, new Vector3(mx, height + 7f, mz), new Vector3(mx, height + 9.4f, mz), 0.7f, 0f, 8, true);
        }
    }

    private static void Rooftop(MashhadMeshBatch b, System.Random rng, float width, float depth, float height)
    {
        float x1 = ((float)rng.NextDouble() - 0.5f) * width * 0.6f;
        float z1 = ((float)rng.NextDouble() - 0.5f) * depth * 0.5f;
        b.Box(roofDark, new Vector3(x1, height + 1f, z1), new Vector3(1.4f, 0.8f, 1f));
        float x2 = ((float)rng.NextDouble() - 0.5f) * width * 0.6f;
        b.Cylinder(tank, new Vector3(x2, height + 0.6f, 0f), new Vector3(x2, height + 2.2f, 0f), 0.9f, 0.9f, 8, true);
        float x3 = ((float)rng.NextDouble() - 0.5f) * width * 0.7f;
        b.Cylinder(metal, new Vector3(x3, height + 0.6f, 0f), new Vector3(x3, height + 5f, 0f), 0.05f, 0.03f, 4, true);
    }

    private static void BuildShops(MashhadMeshBatch b, int s, int side)
    {
        var rng = new System.Random(s * 5557 + (side + 2) * 911 + 3);
        float z0 = s * SegmentLength;
        float z1 = z0 + SegmentLength;
        float z = z0;
        while (z < z1 - 2f)
        {
            float w = Mathf.Lerp(5f, 8.5f, (float)rng.NextDouble());
            if (z + w > z1 - 3.5f) w = z1 - z;
            float h = 3.2f + (float)rng.NextDouble() * 1.4f;
            if (!IntersectsGate(s, z, z + w))
            {
                Shop(b, rng, side * 14.6f, z + w * 0.5f, side > 0 ? 90f : -90f, w - 0.3f, 3.6f, h);
            }
            z += w;
        }
        BuildProps(b, s, side, rng);
    }

    private static void Shop(MashhadMeshBatch b, System.Random rng, float x, float z, float yaw, float width, float depth, float height)
    {
        b.SetTransform(new Vector3(x, 0f, z), yaw);
        Material wall = facades[rng.Next(facades.Length)];
        float hd = depth * 0.5f;
        b.Box(wall, new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, depth), false);
        b.Box(wall, new Vector3(0f, height + 0.15f, 0f), new Vector3(width + 0.2f, 0.3f, depth + 0.2f));
        Storefront(b, rng, width, hd);
        if (rng.NextDouble() < 0.5) b.Box(roofDark, new Vector3(0f, height + 0.7f, 0f), new Vector3(1.2f, 0.8f, 1f));
    }

    private static void BuildProps(MashhadMeshBatch b, int s, int side, System.Random rng)
    {
        float z0 = s * SegmentLength;
        for (int k = 0; k < 8; k++)
        {
            float z = z0 + 6f + k * 12f + (float)rng.NextDouble() * 3f;
            int kind = rng.Next(6);
            if (IntersectsGate(s, z - 2f, z + 2f)) continue;
            if (side < 0 && NearStation(z, 7.5f)) continue;
            b.ResetTransform();
            switch (kind)
            {
                case 0:
                case 1:
                    Tree(b, side * 9.4f, z);
                    break;
                case 2:
                    Bench(b, side * 6.4f, z, side);
                    break;
                case 3:
                    Kiosk(b, rng, side * 8.8f, z, side);
                    break;
                case 4:
                    BannerPole(b, side * 4.9f, z, side);
                    break;
                default:
                    b.Cylinder(metal, new Vector3(side * 5f, 0.14f, z), new Vector3(side * 5f, 0.95f, z), 0.22f, 0.22f, 8, true);
                    b.Box(curb, new Vector3(side * 7.2f, 0.29f, z), new Vector3(1.2f, 0.3f, 1.2f));
                    b.Ellipsoid(leafLight, new Vector3(side * 7.2f, 0.9f, z), new Vector3(0.6f, 0.5f, 0.6f), 3, 7, true);
                    break;
            }
            b.ResetTransform();
        }
    }

    private static void Tree(MashhadMeshBatch b, float x, float z)
    {
        b.Box(curb, new Vector3(x, 0.29f, z), new Vector3(1.1f, 0.3f, 1.1f));
        b.Cylinder(bark, new Vector3(x, 0.3f, z), new Vector3(x, 2.7f, z), 0.15f, 0.1f, 6, true);
        b.Ellipsoid(leaf, new Vector3(x, 3.9f, z), new Vector3(1.8f, 1.5f, 1.8f), 4, 8, false);
        b.Ellipsoid(leafLight, new Vector3(x + 0.5f, 4.5f, z + 0.3f), new Vector3(1.1f, 0.9f, 1.1f), 3, 7, false);
    }

    private static void Bench(MashhadMeshBatch b, float x, float z, int side)
    {
        b.Box(wood, new Vector3(x, 0.5f, z), new Vector3(0.5f, 0.08f, 1.6f));
        b.Box(wood, new Vector3(x + side * 0.27f, 0.82f, z), new Vector3(0.06f, 0.5f, 1.6f));
        b.Box(metal, new Vector3(x, 0.3f, z - 0.65f), new Vector3(0.4f, 0.32f, 0.06f));
        b.Box(metal, new Vector3(x, 0.3f, z + 0.65f), new Vector3(0.4f, 0.32f, 0.06f));
    }

    private static void Kiosk(MashhadMeshBatch b, System.Random rng, float x, float z, int side)
    {
        b.SetTransform(new Vector3(x, 0.14f, z), side > 0 ? 90f : -90f);
        b.Box(signs[rng.Next(signs.Length)], new Vector3(0f, 1.2f, 0f), new Vector3(2.4f, 2.4f, 1.4f), false);
        b.Box(glassDark, new Vector3(0f, 1.4f, -0.72f), new Vector3(2f, 1.1f, 0.05f), false);
        b.Box(awnings[rng.Next(awnings.Length)], new Vector3(0f, 2.3f, -1f), new Vector3(2.6f, 0.1f, 0.9f));
        b.Box(roofDark, new Vector3(0f, 2.5f, 0f), new Vector3(2.6f, 0.15f, 1.6f));
        for (int i = 0; i < 6; i++)
        {
            b.Box(awnings[i % awnings.Length], new Vector3(-0.85f + i * 0.34f, 1.05f + (i % 2) * 0.3f, -0.76f), new Vector3(0.28f, 0.3f, 0.04f), false);
        }
    }

    private static void BannerPole(MashhadMeshBatch b, float x, float z, int side)
    {
        b.Cylinder(metal, new Vector3(x, 0f, z), new Vector3(x, 5.2f, z), 0.07f, 0.05f, 6, true);
        b.Box(metal, new Vector3(x - side * 0.45f, 5f, z), new Vector3(0.9f, 0.05f, 0.05f));
        Material bm = ((int)z / 12) % 2 == 0 ? banner : bannerAlt;
        b.Box(bm, new Vector3(x - side * 0.6f, 3.9f, z), new Vector3(0.04f, 1.9f, 0.9f));
        b.Box(gold, new Vector3(x - side * 0.6f, 4.9f, z), new Vector3(0.05f, 0.1f, 0.94f));
    }

    private static void BuildGate(MashhadMeshBatch b, float zc)
    {
        b.ResetTransform();
        Material wall = facades[1];
        float len = GateHalfLength * 2f;
        b.Box(wall, new Vector3(-14f, 7f, zc), new Vector3(12f, 14f, len), false);
        b.Box(wall, new Vector3(14f, 7f, zc), new Vector3(12f, 14f, len), false);
        b.Box(wall, new Vector3(0f, 12f, zc), new Vector3(16.2f, 4f, len));
        b.Box(tile, new Vector3(-7.9f, 1.8f, zc), new Vector3(0.12f, 3.6f, len - 0.4f), false);
        b.Box(tile, new Vector3(7.9f, 1.8f, zc), new Vector3(0.12f, 3.6f, len - 0.4f), false);
        b.Box(gold, new Vector3(-7.88f, 3.75f, zc), new Vector3(0.14f, 0.18f, len - 0.4f), false);
        b.Box(gold, new Vector3(7.88f, 3.75f, zc), new Vector3(0.14f, 0.18f, len - 0.4f), false);
        for (int i = -1; i <= 1; i++)
        {
            b.Box(lantern, new Vector3(i * 6.2f, 9.9f, zc), new Vector3(0.5f, 0.1f, len - 1f));
        }

        for (int end = 0; end < 2; end++)
        {
            float o = end == 0 ? -1f : 1f;
            float zf = zc + o * (GateHalfLength + 0.12f);
            b.Box(tile, new Vector3(0f, 12f, zf), new Vector3(16.2f, 4f, 0.24f), false);
            b.Box(tile, new Vector3(-14f, 9.2f, zf), new Vector3(12.2f, 0.7f, 0.24f), false);
            b.Box(tile, new Vector3(14f, 9.2f, zf), new Vector3(12.2f, 0.7f, 0.24f), false);
            for (int i = 0; i <= 12; i++)
            {
                float ang = Mathf.PI * i / 12f;
                b.Box(gold, new Vector3(Mathf.Cos(ang) * 7.6f, 10f + Mathf.Sin(ang) * 3.7f, zf + o * 0.15f), new Vector3(0.5f, 0.5f, 0.2f), false);
            }
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int k = -1; k <= 1; k++)
                {
                    float wx = sx * 14f + k * 3.6f;
                    b.Box(trim, new Vector3(wx, 5f, zf + o * 0.1f), new Vector3(1.4f, 3.2f, 0.1f), false);
                    b.Box(windowLit, new Vector3(wx, 5f, zf + o * 0.16f), new Vector3(1f, 2.6f, 0.06f), false);
                }
            }
        }

        b.Cylinder(tile, new Vector3(0f, 14f, zc), new Vector3(0f, 17f, zc), 7.6f, 7.6f, 20, true);
        b.Ellipsoid(gold, new Vector3(0f, 17f, zc), new Vector3(7.4f, 6.6f, 7.4f), 6, 20, true);
        b.Cylinder(gold, new Vector3(0f, 23.4f, zc), new Vector3(0f, 24f, zc), 1.2f, 1f, 10, true);
        b.Cylinder(gold, new Vector3(0f, 24f, zc), new Vector3(0f, 28f, zc), 0.3f, 0f, 6, true);
        for (int sx = -1; sx <= 1; sx += 2)
        {
            float x = sx * 14f;
            b.Cylinder(tile, new Vector3(x, 14f, zc), new Vector3(x, 15.6f, zc), 3.4f, 3.4f, 12, true);
            b.Ellipsoid(gold, new Vector3(x, 15.6f, zc), new Vector3(3.3f, 3f, 3.3f), 4, 12, true);
            b.Cylinder(gold, new Vector3(x, 18.6f, zc), new Vector3(x, 20.4f, zc), 0.15f, 0f, 6, true);
            for (int sz = -1; sz <= 1; sz += 2)
            {
                float mx = sx * 18.5f;
                float mz = zc + sz * 15f;
                b.Cylinder(tile, new Vector3(mx, 0f, mz), new Vector3(mx, 26f, mz), 1.1f, 0.8f, 10, true);
                b.Cylinder(gold, new Vector3(mx, 20f, mz), new Vector3(mx, 20.5f, mz), 1.8f, 1.8f, 10, true);
                b.Cylinder(gold, new Vector3(mx, 26f, mz), new Vector3(mx, 29.5f, mz), 1f, 0f, 10, true);
            }
        }
    }

    public static void AddWalkers(Transform parent, int s)
    {
        var rng = new System.Random(s * 31 + 7);
        float z0 = s * SegmentLength;
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 5; i++)
            {
                float z = z0 + 4f + (float)rng.NextDouble() * (SegmentLength - 8f);
                float xr = (float)rng.NextDouble();
                int variant = rng.Next(1000);
                float distance = 8f + (float)rng.NextDouble() * 8f;
                if (IntersectsGate(s, z - 8f, z + 8f)) continue;
                if (side < 0 && NearStation(z, 9f)) continue;
                float x = side > 0 ? 5.4f + xr * 4.8f : -(7f + xr * 3.6f);
                MashhadPeopleFactory.CreateWalker(parent, new Vector3(x, 0.14f, z), variant, distance);
            }
        }
    }
}
