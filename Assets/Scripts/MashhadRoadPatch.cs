using UnityEngine;

/// <summary>
/// Draws asphalt, base ground and lane marks slightly below the originals for every corridor segment, so the road
/// stays continuous when RunnerGame removes an environment chunk while part of it is still ahead of the player.
/// </summary>
public static class MashhadRoadPatch
{
    public static void Build(MashhadMeshBatch b, int segment)
    {
        Material asphalt = MashhadMaterials.Get("MR_C_Asphalt", new Color(0.23f, 0.25f, 0.28f), 0.04f, 0.25f, 0f);
        Material baseGround = MashhadMaterials.Get("MR_C_RoadGround", new Color(0.22f, 0.24f, 0.26f), 0.04f, 0.25f, 0f);
        Material mark = MashhadMaterials.Get("MR_C_LaneLine", new Color(0.92f, 0.92f, 0.88f), 0.04f, 0.25f, 0f);

        b.ResetTransform();
        float len = MashhadCityBuilder.SegmentLength;
        float z0 = segment * len;
        float zc = z0 + len * 0.5f;
        b.Box(baseGround, new Vector3(0f, -0.605f, zc), new Vector3(20f, 1f, len), false);
        b.Box(asphalt, new Vector3(0f, -0.025f, zc), new Vector3(8f, 0.08f, len), false);
        for (float z = z0; z < z0 + len - 0.01f; z += 8f)
        {
            for (int k = 0; k < 4; k++)
            {
                b.Box(mark, new Vector3(-3.3f + k * 2.2f, 0.025f, z), new Vector3(0.06f, 0.02f, 3.8f), false);
            }
        }
    }
}
