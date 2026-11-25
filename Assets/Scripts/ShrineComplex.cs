using UnityEngine;

/// <summary>
/// Builds the Imam Reza shrine skyline that stands on top of the underpass portal at the end of the
/// BRT section: sand-coloured hall, turquoise iwan, golden dome, four minarets and corner cupolas.
/// Everything is made from simple primitives. The static parts are merged into a few meshes; the dome,
/// sun-ray halo, pigeons and sparkles are separate objects driven by <see cref="ShrineAnimator"/>.
/// </summary>
public static class ShrineComplex
{
    public const float PodiumHeight = 11.64f;
    public const float CentreOffset = 24f;

    private static readonly Color Sand = new Color(0.84f, 0.74f, 0.55f);
    private static readonly Color SandDark = new Color(0.68f, 0.58f, 0.42f);
    private static readonly Color Teal = new Color(0.08f, 0.50f, 0.58f);
    private static readonly Color TealDark = new Color(0.05f, 0.32f, 0.45f);
    private static readonly Color Gold = new Color(0.95f, 0.72f, 0.2f);
    private static readonly Color GlowWindow = new Color(1f, 0.82f, 0.45f);
    private static readonly Color Recess = new Color(0.04f, 0.07f, 0.12f);

    /// <summary>Creates the shrine for the portal whose facade begins at <paramref name="portalZ"/>.</summary>
    public static GameObject Build(int portalZ)
    {
        float zc = portalZ + CentreOffset;
        float y0 = PodiumHeight;
        var centre = new Vector3(0f, 0f, zc);

        var root = new GameObject("Imam Reza Shrine");
        root.transform.position = centre;

        BuildStatic(root.transform, zc, y0);

        var animator = root.AddComponent<ShrineAnimator>();
        BuildAnimated(root.transform, animator, zc, y0);
        return root;
    }

    private static void BuildStatic(Transform parent, float zc, float y0)
    {
        var b = new MeshBatcher(new Vector3(0f, 0f, zc), false);

        // Main hall with plinth and a turquoise band.
        b.Box(new Vector3(0f, y0 + 0.4f, zc), new Vector3(24f, 0.8f, 24f), SandDark, 0.1f);
        b.Box(new Vector3(0f, y0 + 4f, zc), new Vector3(22f, 8f, 22f), Sand, 0.15f);
        b.Box(new Vector3(0f, y0 + 6.2f, zc - 11.05f), new Vector3(22.2f, 1.1f, 0.12f), Teal, 0.4f);
        b.Box(new Vector3(-11.05f, y0 + 6.2f, zc), new Vector3(0.12f, 1.1f, 22.2f), Teal, 0.4f);
        b.Box(new Vector3(11.05f, y0 + 6.2f, zc), new Vector3(0.12f, 1.1f, 22.2f), Teal, 0.4f);

        // Front iwan with a glowing entrance.
        b.Box(new Vector3(0f, y0 + 5.5f, zc - 11.6f), new Vector3(9.6f, 11f, 1.6f), Teal, 0.45f);
        b.Box(new Vector3(0f, y0 + 11.1f, zc - 11.6f), new Vector3(9.9f, 0.4f, 1.8f), Gold, 0.6f, 0.3f);
        b.Box(new Vector3(0f, y0 + 4.2f, zc - 12.45f), new Vector3(5.4f, 7.6f, 0.2f), Recess, 0.2f);
        b.CylinderRot(new Vector3(0f, y0 + 8f, zc - 12.45f), 2.7f, 0.2f, Quaternion.Euler(90f, 0f, 0f), Recess, 0.2f);
        b.Box(new Vector3(0f, y0 + 2.4f, zc - 12.6f), new Vector3(2.4f, 4.4f, 0.1f), GlowWindow, 0.2f, 1.6f);
        for (int i = -2; i <= 2; i++)
        {
            b.BoxRot(new Vector3(i * 1.7f, y0 + 9.6f, zc - 12.5f), new Vector3(0.7f, 0.7f, 0.12f), Quaternion.Euler(0f, 0f, 45f), Gold, 0.6f, 0.2f);
        }

        // Drum, gold rings and window lights.
        b.Cylinder(new Vector3(0f, y0 + 10.5f, zc), 7.4f, 5f, TealDark, 0.4f);
        b.Cylinder(new Vector3(0f, y0 + 8.1f, zc), 7.7f, 0.3f, Gold, 0.7f, 0.5f);
        b.Cylinder(new Vector3(0f, y0 + 13.05f, zc), 7.7f, 0.3f, Gold, 0.7f, 0.5f);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f;
            float rad = angle * Mathf.Deg2Rad;
            var position = new Vector3(Mathf.Sin(rad) * 7.45f, y0 + 10.5f, zc + Mathf.Cos(rad) * 7.45f);
            b.BoxRot(position, new Vector3(0.9f, 2.2f, 0.15f), Quaternion.Euler(0f, angle, 0f), GlowWindow, 0.2f, 1.4f);
        }

        // Finial spire above the dome.
        b.Cylinder(new Vector3(0f, y0 + 21.3f, zc), 0.22f, 4.2f, Gold, 0.8f, 0.6f);

        // Corner cupolas on the roof.
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
            {
                float x = sx * 8.5f;
                float z = zc + sz * 8.5f;
                b.Cylinder(new Vector3(x, y0 + 9f, z), 1.5f, 2f, Teal, 0.4f);
                b.Sphere(new Vector3(x, y0 + 10.2f, z), new Vector3(3.2f, 3.6f, 3.2f), Gold, 0.8f, 0.6f);
                b.Cylinder(new Vector3(x, y0 + 12.2f, z), 0.06f, 1.2f, Gold, 0.8f, 0.6f);
            }
        }

        // Four minarets.
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Minaret(b, sx * 12.8f, zc + sz * 9.5f, y0);
            }
        }

        b.Build("Shrine Static").transform.SetParent(parent, true);
    }

    private static void Minaret(MeshBatcher b, float x, float z, float y0)
    {
        b.Cylinder(new Vector3(x, y0 + 7.5f, z), 1.05f, 15f, Sand, 0.15f);
        b.Cylinder(new Vector3(x, y0 + 10f, z), 1.2f, 0.6f, Teal, 0.4f);
        b.Cylinder(new Vector3(x, y0 + 13f, z), 1.2f, 0.5f, Teal, 0.4f);
        b.Cylinder(new Vector3(x, y0 + 15.3f, z), 1.75f, 0.5f, Gold, 0.7f, 0.4f);
        b.Cylinder(new Vector3(x, y0 + 16.8f, z), 0.85f, 2.5f, Teal, 0.4f);
        b.Sphere(new Vector3(x, y0 + 19.2f, z), new Vector3(1.9f, 2.6f, 1.9f), Gold, 0.85f, 0.6f);
        b.Cylinder(new Vector3(x, y0 + 21f, z), 0.07f, 2.2f, Gold, 0.8f, 0.6f);
    }

    private static void BuildAnimated(Transform parent, ShrineAnimator animator, float zc, float y0)
    {
        // Golden dome and its glowing finial orb.
        animator.domeMaterial = ProcKit.CreateMaterial(Gold, 0.85f, 0.7f, 0.3f);
        var dome = ProcKit.Primitive(PrimitiveType.Sphere, "Golden Dome", parent, new Vector3(0f, y0 + 13f, zc), new Vector3(15f, 13f, 15f), animator.domeMaterial);
        animator.dome = dome.transform;

        animator.finialMaterial = ProcKit.CreateMaterial(new Color(1f, 0.9f, 0.5f), 0.8f, 0.5f, 1f);
        var orb = ProcKit.Primitive(PrimitiveType.Sphere, "Finial Orb", parent, new Vector3(0f, y0 + 23.8f, zc), Vector3.one * 0.9f, animator.finialMaterial);
        animator.finial = orb.transform;

        // Halo of sun rays turning behind the dome.
        var rays = new GameObject("Sun Rays").transform;
        rays.SetParent(parent, false);
        rays.position = new Vector3(0f, y0 + 15f, zc + 9f);
        var rayMaterial = ProcKit.GetMaterial(new Color(1f, 0.85f, 0.4f), 0.1f, 0f, 0.9f);
        const int rayCount = 18;
        for (int i = 0; i < rayCount; i++)
        {
            var arm = new GameObject("Ray Arm").transform;
            arm.SetParent(rays, false);
            arm.localPosition = Vector3.zero;
            arm.localRotation = Quaternion.Euler(0f, 0f, i * (360f / rayCount));

            float length = i % 2 == 0 ? 9f : 5.5f;
            var ray = ProcKit.Primitive(PrimitiveType.Cube, "Ray", arm, Vector3.zero, new Vector3(0.5f, length, 0.15f), rayMaterial);
            ray.transform.localPosition = new Vector3(0f, 17f, 0f);
            ray.transform.localRotation = Quaternion.identity;
        }
        animator.rays = rays;

        // Pigeons circling the minarets.
        const int pigeonCount = 10;
        var birdMaterial = ProcKit.GetMaterial(new Color(0.92f, 0.92f, 0.9f), 0.2f);
        animator.pigeons = new Transform[pigeonCount];
        animator.leftWings = new Transform[pigeonCount];
        animator.rightWings = new Transform[pigeonCount];
        for (int i = 0; i < pigeonCount; i++)
        {
            var bird = new GameObject("Pigeon").transform;
            bird.SetParent(parent, false);
            bird.position = new Vector3(0f, y0 + 20f, zc);
            var body = ProcKit.Primitive(PrimitiveType.Cube, "Body", bird, bird.position, new Vector3(0.28f, 0.16f, 0.5f), birdMaterial);
            body.transform.localPosition = Vector3.zero;
            animator.pigeons[i] = bird;
            animator.leftWings[i] = Wing(bird, -1f, birdMaterial);
            animator.rightWings[i] = Wing(bird, 1f, birdMaterial);
        }
        animator.orbitCenter = new Vector3(0f, y0 + 20f, zc);

        // Rising golden sparkles.
        const int sparkleCount = 14;
        var sparkleMaterial = ProcKit.GetMaterial(new Color(1f, 0.85f, 0.4f), 0.1f, 0f, 1.6f);
        animator.sparkles = new Transform[sparkleCount];
        for (int i = 0; i < sparkleCount; i++)
        {
            var sparkle = ProcKit.Primitive(PrimitiveType.Sphere, "Sparkle", parent, new Vector3(0f, y0, zc), Vector3.zero, sparkleMaterial);
            animator.sparkles[i] = sparkle.transform;
        }
        animator.sparkleOrigin = new Vector3(0f, y0 + 1f, zc - 4f);
        animator.sparkleHeight = 26f;

        // Warm light on the facade so the portal reads as lit from the shrine.
        var lightObject = new GameObject("Shrine Glow");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = new Vector3(0f, y0 + 6f, zc - 22f);
        var glow = lightObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.82f, 0.5f);
        glow.range = 55f;
        glow.intensity = 1.4f;
        glow.shadows = LightShadows.None;
        animator.glowLight = glow;
    }

    private static Transform Wing(Transform bird, float side, Material material)
    {
        var pivot = new GameObject(side < 0f ? "Left Wing" : "Right Wing").transform;
        pivot.SetParent(bird, false);
        pivot.localPosition = new Vector3(side * 0.14f, 0f, 0f);

        var wing = ProcKit.Primitive(PrimitiveType.Cube, "Wing Blade", pivot, Vector3.zero, new Vector3(0.45f, 0.03f, 0.3f), material);
        wing.transform.localPosition = new Vector3(side * 0.22f, 0f, 0f);
        wing.transform.localRotation = Quaternion.identity;
        return pivot;
    }
}
