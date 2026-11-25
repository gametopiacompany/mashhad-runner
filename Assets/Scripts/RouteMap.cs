using UnityEngine;

public enum RouteZone
{
    Brt,
    Underpass,
    Metro,
    RingRoad
}

/// <summary>Colours, fog and sun settings of one route zone. Blended while the runner travels between zones.</summary>
public struct Atmosphere
{
    public Color fog;
    public Color ambientSky;
    public Color ambientEquator;
    public Color ambientGround;
    public Color sunColor;
    public float fogStart;
    public float fogEnd;
    public float sunIntensity;

    public static Atmosphere Lerp(Atmosphere a, Atmosphere b, float t)
    {
        return new Atmosphere
        {
            fog = Color.Lerp(a.fog, b.fog, t),
            ambientSky = Color.Lerp(a.ambientSky, b.ambientSky, t),
            ambientEquator = Color.Lerp(a.ambientEquator, b.ambientEquator, t),
            ambientGround = Color.Lerp(a.ambientGround, b.ambientGround, t),
            sunColor = Color.Lerp(a.sunColor, b.sunColor, t),
            fogStart = Mathf.Lerp(a.fogStart, b.fogStart, t),
            fogEnd = Mathf.Lerp(a.fogEnd, b.fogEnd, t),
            sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t)
        };
    }
}

/// <summary>
/// Describes the endless route as a repeating cycle of zones, one environment chunk (160 m) at a time.
/// Every zone keeps the same road width, lane count and run speed, so passing from one zone to the next
/// never changes the controls: only scenery, light and fog change.
/// The Imam Reza shrine stands above the portal where the BRT section ends and the underpass begins.
/// </summary>
public static class RouteMap
{
    public const int ChunkLength = 160;

    /// <summary>All zones share one three-lane road. Lane counts per zone are a design decision still open.</summary>
    public const int LaneCount = 3;

    /// <summary>The contract lists three environments, the GDD draft four. Set to false to drop the metro zone.</summary>
    public static readonly bool IncludeMetro = true;

    private static readonly RouteZone[] Cycle = BuildCycle();

    private static RouteZone[] BuildCycle()
    {
        var list = new System.Collections.Generic.List<RouteZone>();
        for (int i = 0; i < 5; i++) list.Add(RouteZone.Brt);
        for (int i = 0; i < 3; i++) list.Add(RouteZone.Underpass);
        if (IncludeMetro)
        {
            for (int i = 0; i < 3; i++) list.Add(RouteZone.Metro);
        }
        for (int i = 0; i < 4; i++) list.Add(RouteZone.RingRoad);
        return list.ToArray();
    }

    private static int ChunkIndex(float z)
    {
        return Mathf.Max(0, Mathf.FloorToInt(z / ChunkLength));
    }

    public static RouteZone ZoneAt(float z)
    {
        return Cycle[ChunkIndex(z) % Cycle.Length];
    }

    /// <summary>True when the chunk that begins at <paramref name="startZ"/> is the first of its zone.</summary>
    public static bool IsZoneStart(int startZ)
    {
        int index = ChunkIndex(startZ);
        return index == 0 || Cycle[index % Cycle.Length] != Cycle[(index - 1) % Cycle.Length];
    }

    /// <summary>True when the chunk that ends at <paramref name="endZ"/> is the last of its zone.</summary>
    public static bool IsZoneEnd(int endZ)
    {
        int index = Mathf.Max(1, Mathf.FloorToInt((float)endZ / ChunkLength));
        return Cycle[index % Cycle.Length] != Cycle[(index - 1) % Cycle.Length];
    }

    public static RouteZone ZoneAfter(int endZ)
    {
        int index = Mathf.Max(1, Mathf.FloorToInt((float)endZ / ChunkLength));
        return Cycle[index % Cycle.Length];
    }

    /// <summary>The shrine stands over the portal at the start of every underpass.</summary>
    public static bool HasShrine(int startZ)
    {
        return ZoneAt(startZ) == RouteZone.Underpass && IsZoneStart(startZ);
    }

    /// <summary>Z of the next shrine portal at or after the chunk following <paramref name="z"/>, or -1.</summary>
    public static int NextPortalZ(float z)
    {
        int first = ChunkIndex(z) + 1;
        for (int i = 0; i < Cycle.Length * 2; i++)
        {
            int portal = (first + i) * ChunkLength;
            if (HasShrine(portal))
            {
                return portal;
            }
        }
        return -1;
    }

    /// <summary>Distance in metres to the nearest zone boundary within the margin, or a large number.</summary>
    public static float DistanceToBoundary(float z)
    {
        int near = Mathf.RoundToInt(z / ChunkLength);
        float best = float.MaxValue;
        for (int i = near - 1; i <= near + 1; i++)
        {
            if (i <= 0 || Cycle[i % Cycle.Length] == Cycle[(i - 1) % Cycle.Length])
            {
                continue;
            }
            best = Mathf.Min(best, Mathf.Abs(z - i * ChunkLength));
        }
        return best;
    }

    /// <summary>Obstacles are kept away from zone transitions so the change of scenery is never a trap.</summary>
    public static bool IsTransitionSafe(float z)
    {
        return DistanceToBoundary(z) < 14f;
    }

    public static string Name(RouteZone zone)
    {
        switch (zone)
        {
            case RouteZone.Brt: return "BRT";
            case RouteZone.Underpass: return "UNDERPASS";
            case RouteZone.Metro: return "METRO";
            default: return "RING ROAD";
        }
    }

    public static Atmosphere Profile(RouteZone zone)
    {
        switch (zone)
        {
            case RouteZone.Underpass:
                return new Atmosphere
                {
                    fog = new Color(0.10f, 0.11f, 0.14f),
                    ambientSky = new Color(0.30f, 0.32f, 0.38f),
                    ambientEquator = new Color(0.28f, 0.29f, 0.33f),
                    ambientGround = new Color(0.15f, 0.15f, 0.16f),
                    sunColor = new Color(0.9f, 0.9f, 1f),
                    fogStart = 22f, fogEnd = 120f, sunIntensity = 0.25f
                };
            case RouteZone.Metro:
                return new Atmosphere
                {
                    fog = new Color(0.12f, 0.17f, 0.18f),
                    ambientSky = new Color(0.46f, 0.50f, 0.52f),
                    ambientEquator = new Color(0.42f, 0.46f, 0.48f),
                    ambientGround = new Color(0.20f, 0.22f, 0.22f),
                    sunColor = new Color(0.95f, 1f, 1f),
                    fogStart = 26f, fogEnd = 130f, sunIntensity = 0.3f
                };
            case RouteZone.RingRoad:
                return new Atmosphere
                {
                    fog = new Color(0.93f, 0.58f, 0.42f),
                    ambientSky = new Color(0.60f, 0.50f, 0.55f),
                    ambientEquator = new Color(0.55f, 0.45f, 0.42f),
                    ambientGround = new Color(0.25f, 0.20f, 0.20f),
                    sunColor = new Color(1f, 0.62f, 0.38f),
                    fogStart = 50f, fogEnd = 210f, sunIntensity = 0.8f
                };
            default:
                return new Atmosphere
                {
                    fog = new Color(0.62f, 0.78f, 0.86f),
                    ambientSky = new Color(0.72f, 0.82f, 0.95f),
                    ambientEquator = new Color(0.62f, 0.64f, 0.66f),
                    ambientGround = new Color(0.32f, 0.30f, 0.28f),
                    sunColor = new Color(1f, 0.96f, 0.88f),
                    fogStart = 70f, fogEnd = 270f, sunIntensity = 1.15f
                };
        }
    }
}
