using UnityEngine;

/// <summary>
/// Builds one 160 m environment chunk for a route zone from merged primitives.
/// Every zone uses the same 8 m road with three lanes (x = -2.2, 0, 2.2), so zones connect seamlessly.
/// Chunks are pivoted at their end Z so the game's "destroy when behind the runner" rule works unchanged.
/// </summary>
public static class EnvironmentFactory
{
    private const float PortalHeight = 11.64f;

    private static readonly Color Sand = new Color(0.84f, 0.74f, 0.55f);
    private static readonly Color Teal = new Color(0.08f, 0.50f, 0.58f);
    private static readonly Color Gold = new Color(0.95f, 0.72f, 0.2f);
    private static readonly Color Concrete = new Color(0.62f, 0.63f, 0.62f);
    private static readonly Color Amber = new Color(1f, 0.7f, 0.2f);
    private static readonly Color WarmWhite = new Color(1f, 0.95f, 0.85f);

    public static GameObject Build(RouteZone zone, int startZ, int endZ)
    {
        var batcher = new MeshBatcher(new Vector3(0f, 0f, endZ), false);
        switch (zone)
        {
            case RouteZone.Underpass:
                Underpass(batcher, startZ, endZ);
                break;
            case RouteZone.Metro:
                Metro(batcher, startZ, endZ);
                break;
            case RouteZone.RingRoad:
                RingRoad(batcher, startZ, endZ);
                break;
            default:
                Brt(batcher, startZ, endZ);
                break;
        }
        return batcher.Build(zone + " Chunk " + startZ);
    }

    // ---------------------------------------------------------------- helpers

    private static Vector3 V(float x, float y, float z)
    {
        return new Vector3(x, y, z);
    }

    private static float Rand(int seed)
    {
        float v = Mathf.Sin(seed * 12.9898f) * 43758.5453f;
        return v - Mathf.Floor(v);
    }

    private static void RoadBase(MeshBatcher b, int s, int e, Color asphalt, float groundWidth, Color ground, float smoothness)
    {
        float c = (s + e) * 0.5f;
        float length = e - s;
        b.Box(V(0f, -0.6f, c), V(groundWidth, 1f, length), ground, 0.05f);
        b.Box(V(0f, -0.02f, c), V(8f, 0.08f, length), asphalt, smoothness);
    }

    private static void LaneMarks(MeshBatcher b, int s, int e, Color paint)
    {
        float c = (s + e) * 0.5f;
        float length = e - s;
        b.Box(V(-3.3f, 0.03f, c), V(0.08f, 0.02f, length), paint);
        b.Box(V(3.3f, 0.03f, c), V(0.08f, 0.02f, length), paint);
        for (int z = s; z < e; z += 8)
        {
            b.Box(V(-1.1f, 0.03f, z + 2f), V(0.06f, 0.02f, 3.8f), paint);
            b.Box(V(1.1f, 0.03f, z + 2f), V(0.06f, 0.02f, 3.8f), paint);
        }
    }

    private static void Tree(MeshBatcher b, float x, float z, int seed)
    {
        float h = 3.2f + Rand(seed) * 1.4f;
        var leaf = Color.Lerp(new Color(0.12f, 0.36f, 0.18f), new Color(0.2f, 0.45f, 0.2f), Rand(seed + 1));
        b.Cylinder(V(x, 1.2f, z), 0.17f, 2.4f, new Color(0.35f, 0.24f, 0.15f), 0.05f);
        b.Sphere(V(x, 2.4f + h * 0.5f, z), V(2f, h, 2f), leaf, 0.05f);
    }

    /// <summary>Distant towers with window strips. Positions depend only on Z so neighbouring chunks line up.</summary>
    private static void Skyline(MeshBatcher b, int s, int e, float xNear, float hMin, float hMax, Color baseA, Color baseB, Color glass, float litChance, Color lit)
    {
        int first = Mathf.FloorToInt(s / 14f) * 14;
        for (int z = first; z < e; z += 14)
        {
            if (z < s)
            {
                continue;
            }

            for (int side = -1; side <= 1; side += 2)
            {
                int seed = z * 2 + (side > 0 ? 1 : 0);
                float w = 7f + Rand(seed) * 8f;
                float d = 9f + Rand(seed + 1) * 5f;
                float h = hMin + Rand(seed + 2) * (hMax - hMin);
                float x = side * (xNear + Rand(seed + 3) * 14f);
                float zc = z + Rand(seed + 5) * 4f;
                b.Box(V(x, h * 0.5f, zc), V(w, h, d), Color.Lerp(baseA, baseB, Rand(seed + 4)), 0.1f);

                float faceX = x - side * (w * 0.5f + 0.03f);
                int row = 0;
                for (float y = 3f; y < h - 2f; y += 4f)
                {
                    bool isLit = Rand(seed + 20 + row) < litChance;
                    b.Box(V(faceX, y, zc), V(0.06f, 1.2f, d * 0.85f), isLit ? lit : glass, 0.6f, isLit ? 0.9f : 0f);
                    row++;
                }
            }
        }
    }

    private static void Hills(MeshBatcher b, int s, int e, Color tint)
    {
        int first = Mathf.FloorToInt(s / 80f) * 80;
        for (int z = first; z < e; z += 80)
        {
            if (z < s)
            {
                continue;
            }

            for (int side = -1; side <= 1; side += 2)
            {
                int seed = z + (side > 0 ? 7 : 3);
                b.Sphere(
                    V(side * (110f + Rand(seed) * 30f), -3f, z + Rand(seed + 1) * 40f),
                    V(60f + Rand(seed + 2) * 30f, 24f + Rand(seed + 3) * 14f, 70f),
                    Color.Lerp(tint, tint * 0.7f, Rand(seed + 4)), 0.05f);
            }
        }
    }

    private static void Gantry(MeshBatcher b, string text, float z, Color board, float cell, float boardWidth)
    {
        var steel = new Color(0.78f, 0.8f, 0.82f);
        b.Box(V(-5.3f, 3.9f, z), V(0.4f, 7.8f, 0.4f), steel, 0.5f);
        b.Box(V(5.3f, 3.9f, z), V(0.4f, 7.8f, 0.4f), steel, 0.5f);
        b.Box(V(0f, 7.8f, z), V(11f, 0.9f, 0.5f), board * 0.8f, 0.4f);
        b.Box(V(0f, 9.4f, z), V(boardWidth, 2.4f, 0.3f), board, 0.3f);
        DotFont.Draw(b, text, V(0f, 9.4f, z - 0.22f), cell, 0.08f, Color.white, 0.8f);
    }

    // ---------------------------------------------------------------- BRT

    private static void Brt(MeshBatcher b, int s, int e)
    {
        RoadBase(b, s, e, new Color(0.22f, 0.24f, 0.27f), 26f, new Color(0.40f, 0.40f, 0.38f), 0.3f);
        LaneMarks(b, s, e, new Color(0.92f, 0.92f, 0.9f));
    }

    private static void Station(MeshBatcher b, float sz, int side)
    {
        float sx = side * 7.4f;
        var steel = new Color(0.8f, 0.82f, 0.84f);
        b.Box(V(sx, 3.3f, sz), V(4.2f, 0.22f, 12f), new Color(0.9f, 0.5f, 0.15f), 0.3f);
        b.Box(V(sx, 3.15f, sz), V(4.4f, 0.1f, 12.2f), Teal, 0.3f);
        foreach (float dz in new[] { -5.5f, -1.8f, 1.8f, 5.5f })
        {
            b.Box(V(sx - 1.9f, 1.7f, sz + dz), V(0.14f, 3.2f, 0.14f), steel, 0.5f);
            b.Box(V(sx + 1.9f, 1.7f, sz + dz), V(0.14f, 3.2f, 0.14f), steel, 0.5f);
        }
        b.Box(V(sx + side * 1.9f, 1.7f, sz), V(0.06f, 2.6f, 11f), new Color(0.6f, 0.85f, 0.92f), 0.85f);
        b.Box(V(sx + side * 1.2f, 0.55f, sz), V(0.5f, 0.1f, 4f), new Color(0.5f, 0.33f, 0.2f), 0.1f);
        b.Box(V(sx - side * 2.0f, 0.24f, sz), V(0.35f, 0.02f, 12f), new Color(0.95f, 0.8f, 0.15f), 0.2f, 0.2f);

        float poleX = sx - side * 1.95f;
        float poleZ = sz - 6.4f;
        b.Box(V(poleX, 2.2f, poleZ), V(0.12f, 4.4f, 0.12f), steel, 0.5f);
        b.Box(V(poleX, 4.6f, poleZ), V(1.9f, 0.9f, 0.12f), new Color(0.1f, 0.3f, 0.55f), 0.3f);
        DotFont.Draw(b, "BRT", V(poleX, 4.6f, poleZ - 0.08f), 0.09f, 0.05f, Color.white, 0.6f);
    }

    private static void Billboard(MeshBatcher b, float z, int side, int index)
    {
        // Ad slot placeholder: a bright, blank panel on two poles.
        Color[] palette =
        {
            new Color(0.9f, 0.3f, 0.25f), new Color(0.2f, 0.6f, 0.85f),
            new Color(0.95f, 0.75f, 0.2f), new Color(0.3f, 0.7f, 0.4f)
        };
        float x = side * 11.6f;
        var rotation = Quaternion.Euler(0f, side * 30f, 0f);
        b.Box(V(x, 2.2f, z), V(0.3f, 4.4f, 0.3f), new Color(0.3f, 0.3f, 0.32f), 0.3f);
        b.BoxRot(V(x, 5.2f, z), V(0.3f, 2.7f, 4.9f), rotation, new Color(0.18f, 0.18f, 0.2f), 0.3f);
        b.BoxRot(V(x - side * 0.2f, 5.2f, z), V(0.1f, 2.3f, 4.5f), rotation, palette[((index % 4) + 4) % 4], 0.3f, 0.5f);
    }

    // ---------------------------------------------------------------- Underpass

    private static void Underpass(MeshBatcher b, int s, int e)
    {
        float c = (s + e) * 0.5f;
        float length = e - s;
        RoadBase(b, s, e, new Color(0.15f, 0.16f, 0.18f), 36f, new Color(0.30f, 0.30f, 0.30f), 0.35f);
        LaneMarks(b, s, e, new Color(0.9f, 0.9f, 0.85f));

        for (int side = -1; side <= 1; side += 2)
        {
            b.Box(V(side * 5.1f, 3.8f, c), V(0.8f, 7.6f, length), Concrete, 0.15f);
            b.Box(V(side * 4.45f, 0.2f, c), V(0.7f, 0.4f, length), new Color(0.4f, 0.4f, 0.42f), 0.2f);
            b.Box(V(side * 4.68f, 1.4f, c), V(0.04f, 1.2f, length), Teal, 0.4f);
            b.Box(V(side * 4.68f, 5.0f, c), V(0.04f, 0.3f, length), new Color(0.9f, 0.5f, 0.1f), 0.3f, 0.3f);
            for (int z = s; z < e; z += 6)
            {
                b.Box(V(side * 4.12f, 0.3f, z + 1f), V(0.08f, 0.12f, 2f), Amber, 0.2f, 0.8f);
            }
            for (int z = s + 6; z < e; z += 12)
            {
                b.Box(V(side * 4.6f, 4.2f, z), V(0.2f, 0.35f, 0.9f), Amber, 0.2f, 1.2f);
            }
            for (int z = s; z < e; z += 32)
            {
                b.Box(V(side * 4.75f, 3.7f, z), V(0.5f, 7.4f, 0.9f), new Color(0.5f, 0.5f, 0.52f), 0.15f);
            }
        }

        b.Box(V(0f, 7.8f, c), V(11.2f, 0.8f, length), new Color(0.5f, 0.5f, 0.52f), 0.1f);
        for (int z = s; z < e; z += 12)
        {
            b.Box(V(0f, 7.35f, z + 4f), V(1.6f, 0.12f, 0.5f), WarmWhite, 0.2f, 1.4f);
        }
        for (int z = s; z < e; z += 32)
        {
            b.Box(V(0f, 7.3f, z), V(9.8f, 0.4f, 0.9f), new Color(0.46f, 0.46f, 0.48f), 0.15f);
        }
        for (int z = s + 16; z < e; z += 48)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                b.CylinderRot(V(side * 2f, 6.6f, z), 0.45f, 2.2f, Quaternion.Euler(90f, 0f, 0f), new Color(0.5f, 0.52f, 0.55f), 0.5f, 0.5f);
                b.Sphere(V(side * 2f, 6.2f, z - 1.2f), V(0.15f, 0.15f, 0.15f), new Color(1f, 0.2f, 0.15f), 0.2f, 0f, 1.5f);
            }
        }

        if (RouteMap.IsZoneStart(s))
        {
            Portal(b, s);
            HangingSign(b, "TUNNEL", s + 70f, new Color(0.08f, 0.28f, 0.5f), 0.14f);
        }
    }

    /// <summary>Facade of the underpass with the shrine's podium behind it. The shrine itself is built by <see cref="ShrineComplex"/>.</summary>
    private static void Portal(MeshBatcher b, int s)
    {
        float h = PortalHeight;
        float depth = 44f;
        float bodyCenterZ = s + 2f + depth * 0.5f;

        // Facade pieces around the tunnel opening (opening: x -4.7..4.7, y 0..7.4).
        b.Box(V(-10.85f, h * 0.5f, s + 1f), V(12.3f, h, 2f), Sand, 0.15f);
        b.Box(V(10.85f, h * 0.5f, s + 1f), V(12.3f, h, 2f), Sand, 0.15f);
        b.Box(V(0f, 9.5f, s + 1f), V(9.4f, 4.2f, 2f), Sand, 0.15f);

        // Solid body behind the facade, leaving the tunnel free.
        b.Box(V(-11.25f, h * 0.5f, bodyCenterZ), V(11.5f, h, depth), Sand, 0.1f);
        b.Box(V(11.25f, h * 0.5f, bodyCenterZ), V(11.5f, h, depth), Sand, 0.1f);
        b.Box(V(0f, 9.9f, bodyCenterZ), V(11f, 3.4f, depth), Sand, 0.1f);

        // Courtyard floor and parapet on top of the podium.
        b.Box(V(0f, h - 0.02f, bodyCenterZ), V(34f, 0.04f, depth), new Color(0.9f, 0.86f, 0.76f), 0.1f);
        b.Box(V(0f, h + 0.4f, s + 0.4f), V(34f, 0.8f, 0.8f), Sand, 0.1f);
        b.Box(V(-16.6f, h + 0.4f, bodyCenterZ), V(0.8f, 0.8f, depth), Sand, 0.1f);
        b.Box(V(16.6f, h + 0.4f, bodyCenterZ), V(0.8f, 0.8f, depth), Sand, 0.1f);
        b.Box(V(0f, h + 0.4f, s + 45.6f), V(34f, 0.8f, 0.8f), Sand, 0.1f);

        // Tile bands, gold frame and star row on the facade.
        b.Box(V(-10.85f, 8.6f, s - 0.05f), V(12.3f, 0.6f, 0.1f), Teal, 0.4f);
        b.Box(V(10.85f, 8.6f, s - 0.05f), V(12.3f, 0.6f, 0.1f), Teal, 0.4f);
        b.Box(V(-10.85f, 10.7f, s - 0.08f), V(12.3f, 0.3f, 0.14f), Gold, 0.6f, 0.2f);
        b.Box(V(10.85f, 10.7f, s - 0.08f), V(12.3f, 0.3f, 0.14f), Gold, 0.6f, 0.2f);
        b.Box(V(-4.85f, 3.7f, s - 0.08f), V(0.3f, 7.4f, 0.14f), Gold, 0.6f, 0.2f);
        b.Box(V(4.85f, 3.7f, s - 0.08f), V(0.3f, 7.4f, 0.14f), Gold, 0.6f, 0.2f);
        b.Box(V(0f, 7.55f, s - 0.08f), V(9.7f, 0.3f, 0.14f), Gold, 0.6f, 0.2f);
        b.Box(V(0f, 9.5f, s - 0.08f), V(8.6f, 1.8f, 0.1f), Teal, 0.4f);
        for (int i = -4; i <= 4; i++)
        {
            b.BoxRot(V(i * 1f, 9.5f, s - 0.16f), V(0.6f, 0.6f, 0.1f), Quaternion.Euler(0f, 0f, 45f), Gold, 0.6f, 0.3f);
        }
    }

    private static void HangingSign(MeshBatcher b, string text, float z, Color board, float cell)
    {
        float width = DotFont.Width(text, cell) + 0.8f;
        b.Box(V(0f, 6.6f, z), V(width, 1.3f, 0.2f), board, 0.3f);
        b.Box(V(-width * 0.4f, 7.2f, z), V(0.06f, 0.5f, 0.06f), Concrete, 0.4f);
        b.Box(V(width * 0.4f, 7.2f, z), V(0.06f, 0.5f, 0.06f), Concrete, 0.4f);
        DotFont.Draw(b, text, V(0f, 6.6f, z - 0.14f), cell, 0.06f, Color.white, 0.9f);
    }

    // ---------------------------------------------------------------- Metro

    private static void Metro(MeshBatcher b, int s, int e)
    {
        float c = (s + e) * 0.5f;
        float length = e - s;
        var tile = new Color(0.86f, 0.87f, 0.89f);
        var green = new Color(0.1f, 0.6f, 0.4f);

        RoadBase(b, s, e, new Color(0.28f, 0.30f, 0.32f), 30f, new Color(0.2f, 0.2f, 0.22f), 0.55f);
        LaneMarks(b, s, e, new Color(0.95f, 0.85f, 0.2f));

        for (int side = -1; side <= 1; side += 2)
        {
            // Platform, safety strip, track pit with rails, parked train and hall wall.
            b.Box(V(side * 6.4f, 0.2f, c), V(4.2f, 0.5f, length), new Color(0.72f, 0.72f, 0.7f), 0.5f);
            b.Box(V(side * 4.5f, 0.46f, c), V(0.4f, 0.04f, length), new Color(0.95f, 0.8f, 0.15f), 0.2f, 0.2f);
            b.Box(V(side * 10.8f, -0.5f, c), V(4.6f, 0.2f, length), new Color(0.15f, 0.15f, 0.16f), 0.2f);
            b.Box(V(side * 10.2f, -0.35f, c), V(0.12f, 0.1f, length), new Color(0.6f, 0.6f, 0.62f), 0.7f, 0.2f);
            b.Box(V(side * 11.4f, -0.35f, c), V(0.12f, 0.1f, length), new Color(0.6f, 0.6f, 0.62f), 0.7f, 0.2f);
            b.Box(V(side * 13.4f, 3.8f, c), V(0.6f, 7.6f, length), tile, 0.4f);
            b.Box(V(side * 13.05f, 1.2f, c), V(0.1f, 1.8f, length), green, 0.4f);

            for (int z = s; z < e; z += 16)
            {
                b.Box(V(side * 8f, 3.8f, z + 8f), V(0.9f, 7.6f, 0.9f), tile, 0.35f);
                b.Box(V(side * 8f, 0.8f, z + 8f), V(1f, 0.6f, 1f), green, 0.4f);
            }
            for (int z = s + 12; z < e; z += 24)
            {
                b.Box(V(side * 13f, 3.6f, z), V(0.1f, 1.2f, 6f), (z / 24) % 2 == 0 ? new Color(0.9f, 0.4f, 0.2f) : new Color(0.25f, 0.55f, 0.9f), 0.3f, 0.7f);
            }
        }

        // A train waits on one side of every chunk; sides alternate with the chunk index.
        int trainSide = (Mathf.Abs(s / RouteMap.ChunkLength) % 2 == 0) ? -1 : 1;
        float trainX = trainSide * 11f;
        b.Box(V(trainX, 2f, c), V(3f, 3.4f, length - 6f), new Color(0.9f, 0.92f, 0.95f), 0.5f);
        b.Box(V(trainX - trainSide * 1.52f, 2.4f, c), V(0.05f, 0.9f, length - 8f), new Color(0.85f, 0.95f, 1f), 0.8f, 0.9f);
        b.Box(V(trainX - trainSide * 1.52f, 1.0f, c), V(0.05f, 0.3f, length - 6f), green, 0.4f);
        for (float z = s + 8f; z < e - 8f; z += 11f)
        {
            b.Box(V(trainX - trainSide * 1.53f, 1.9f, z), V(0.05f, 2.4f, 0.1f), new Color(0.5f, 0.55f, 0.6f), 0.4f);
        }

        b.Box(V(0f, 7.8f, c), V(28.2f, 0.8f, length), new Color(0.8f, 0.82f, 0.85f), 0.2f);
        for (int z = s; z < e; z += 10)
        {
            b.Box(V(-3f, 7.35f, z + 3f), V(0.5f, 0.1f, 6f), WarmWhite, 0.2f, 1.5f);
            b.Box(V(3f, 7.35f, z + 3f), V(0.5f, 0.1f, 6f), WarmWhite, 0.2f, 1.5f);
        }

        if (RouteMap.IsZoneStart(s))
        {
            // Close the wider hall against the narrow underpass mouth.
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(V(side * 9.4f, 4.1f, s + 0.2f), V(7.8f, 8.2f, 0.4f), tile, 0.3f);
            }
            HangingSign(b, "METRO", s + 8f, green, 0.14f);
        }

        if (RouteMap.IsZoneEnd(e))
        {
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(V(side * 9.4f, 4.1f, e - 0.2f), V(7.8f, 8.2f, 0.4f), tile, 0.3f);
            }
        }
    }

    // ---------------------------------------------------------------- Ring road

    private static void RingRoad(MeshBatcher b, int s, int e)
    {
        float c = (s + e) * 0.5f;
        float length = e - s;
        var steel = new Color(0.6f, 0.62f, 0.66f);

        RoadBase(b, s, e, new Color(0.17f, 0.17f, 0.19f), 34f, new Color(0.30f, 0.28f, 0.26f), 0.35f);
        LaneMarks(b, s, e, new Color(0.95f, 0.93f, 0.85f));

        for (int side = -1; side <= 1; side += 2)
        {
            b.Box(V(side * 4.45f, 0.45f, c), V(0.7f, 0.9f, length), new Color(0.72f, 0.72f, 0.7f), 0.15f);
            b.Box(V(side * 12f, -0.08f, c), V(8f, 0.04f, length), new Color(0.3f, 0.34f, 0.22f), 0.05f);
            for (int z = s; z < e; z += 16)
            {
                b.Box(V(side * 4.1f, 0.6f, z + 4f), V(0.05f, 0.12f, 1.2f), Amber, 0.2f, 0.7f);
            }
        }

        for (int z = s; z < e; z += 40)
        {
            int side = (Mathf.Abs(z / 40) % 2 == 0) ? 1 : -1;
            b.Box(V(side * 5.6f, 4.2f, z), V(0.2f, 8.4f, 0.2f), steel, 0.5f);
            b.Box(V(side * 4.3f, 8.3f, z), V(2.8f, 0.15f, 0.15f), steel, 0.5f);
            b.Box(V(side * 3.0f, 8.2f, z), V(1.1f, 0.18f, 0.5f), new Color(1f, 0.7f, 0.3f), 0.2f, 2f);
        }

        for (int z = s; z < e; z += 20)
        {
            int side = (Mathf.Abs(z / 20) % 2 == 0) ? 1 : -1;
            Tree(b, side * 9.5f, z + 10f, z + 500);
        }

        // Overpass across the road once per chunk.
        float bz = s + 120f;
        b.Box(V(0f, 8.8f, bz), V(30f, 0.9f, 9f), new Color(0.55f, 0.55f, 0.56f), 0.1f);
        b.Box(V(0f, 9.6f, bz - 4.3f), V(30f, 0.5f, 0.15f), steel, 0.3f);
        b.Box(V(0f, 9.6f, bz + 4.3f), V(30f, 0.5f, 0.15f), steel, 0.3f);
        b.Box(V(-8f, 4.4f, bz), V(1.4f, 8.8f, 1.4f), new Color(0.5f, 0.5f, 0.52f), 0.1f);
        b.Box(V(8f, 4.4f, bz), V(1.4f, 8.8f, 1.4f), new Color(0.5f, 0.5f, 0.52f), 0.1f);
        b.Box(V(0f, 8.3f, bz - 3f), V(2f, 0.1f, 0.4f), WarmWhite, 0.2f, 1.4f);

        if (RouteMap.IsZoneStart(s) || s % (RouteMap.ChunkLength * 2) == 0)
        {
            Gantry(b, "RING", s + 50f, new Color(0.05f, 0.4f, 0.2f), 0.2f, 5.6f);
        }

        Skyline(b, s, e, 30f, 24f, 70f, new Color(0.35f, 0.3f, 0.4f), new Color(0.5f, 0.4f, 0.42f), new Color(0.3f, 0.3f, 0.4f), 0.45f, new Color(1f, 0.8f, 0.5f));
        Hills(b, s, e, new Color(0.45f, 0.32f, 0.36f));
    }
}
