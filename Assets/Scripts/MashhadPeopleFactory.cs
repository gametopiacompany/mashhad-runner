using UnityEngine;

/// <summary>
/// Stylised low-poly people for the BRT stations and sidewalks. Every body part is a tiny mesh that shares the
/// vertex colour material, so hundreds of people batch into a few draw calls. Visual only, no colliders.
/// </summary>
public static class MashhadPeopleFactory
{
    public enum Pose { Standing, Phone, Newspaper, SeatedPhone, SeatedNewspaper }

    private static readonly Color[] Coats =
    {
        new Color(0.12f, 0.28f, 0.38f), new Color(0.55f, 0.22f, 0.14f), new Color(0.18f, 0.34f, 0.25f),
        new Color(0.38f, 0.30f, 0.48f), new Color(0.72f, 0.48f, 0.22f), new Color(0.25f, 0.25f, 0.28f)
    };

    private static readonly Color[] Skins =
    {
        new Color(0.67f, 0.45f, 0.32f), new Color(0.80f, 0.58f, 0.42f), new Color(0.52f, 0.34f, 0.24f), new Color(0.88f, 0.68f, 0.52f)
    };

    private static Material Mat(string name, Color color, float emission)
    {
        return MashhadMaterials.Get(name, color, 0.02f, 0.2f, emission);
    }

    private static Transform Pivot(Transform parent, string name, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    private static void Place(Transform parent, string name, MashhadMeshBatch batch, Vector3 position, Vector3 euler)
    {
        GameObject go = batch.Build(name, parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(euler);
    }

    private static void Blob(Transform parent, string name, Vector3 position, Vector3 euler, Vector3 radii, Material material)
    {
        var batch = new MashhadMeshBatch();
        batch.Ellipsoid(material, Vector3.zero, radii, 4, 8, false);
        Place(parent, name, batch, position, euler);
    }

    private static void Slab(Transform parent, string name, Vector3 position, Vector3 euler, Vector3 size, Material material)
    {
        var batch = new MashhadMeshBatch();
        batch.Box(material, Vector3.zero, size);
        Place(parent, name, batch, position, euler);
    }

    public static GameObject Create(Transform parent, Vector3 localPosition, float localYaw, int variant, Pose pose)
    {
        return Build(parent, localPosition, localYaw, variant, pose, true);
    }

    private static GameObject Build(Transform parent, Vector3 localPosition, float localYaw, int variant, Pose pose, bool lifelike)
    {
        int v = Mathf.Abs(variant);
        Material coat = Mat("MR_P_Coat_" + (v % Coats.Length), Coats[v % Coats.Length], 0f);
        Material trousers = Mat("MR_P_Trousers", new Color(0.12f, 0.14f, 0.18f), 0f);
        Material skin = Mat("MR_P_Skin_" + (v % Skins.Length), Skins[v % Skins.Length], 0f);
        Material hair = Mat("MR_P_Hair", new Color(0.08f, 0.06f, 0.05f), 0f);
        Material shoe = Mat("MR_P_Shoe", new Color(0.04f, 0.04f, 0.05f), 0f);
        Material ink = Mat("MR_P_Ink", new Color(0.12f, 0.12f, 0.13f), 0f);
        Material paperMat = Mat("MR_P_Paper", new Color(0.9f, 0.88f, 0.82f), 0f);
        Material phoneMat = Mat("MR_P_Phone", new Color(0.04f, 0.04f, 0.05f), 0f);
        Material screenMat = Mat("MR_P_Screen", new Color(0.45f, 0.75f, 1f), 1.8f);

        var root = new GameObject("MR_BRT_Person");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.Euler(0f, localYaw, 0f);
        Transform t = root.transform;

        bool seated = pose == Pose.SeatedPhone || pose == Pose.SeatedNewspaper;
        bool phone = pose == Pose.Phone || pose == Pose.SeatedPhone;
        bool paper = pose == Pose.Newspaper || pose == Pose.SeatedNewspaper;
        bool bowed = phone || paper;
        float yo = seated ? -0.83f : 0f;

        for (int side = -1; side <= 1; side += 2)
        {
            string tag = side < 0 ? "L" : "R";
            if (seated)
            {
                Blob(t, "Thigh", new Vector3(side * 0.1f, 0.05f, 0.22f), new Vector3(90f, 0f, 0f), new Vector3(0.08f, 0.22f, 0.08f), trousers);
                Blob(t, "Shin", new Vector3(side * 0.1f, -0.25f, 0.44f), Vector3.zero, new Vector3(0.07f, 0.25f, 0.07f), trousers);
                Slab(t, "Shoe", new Vector3(side * 0.1f, -0.5f, 0.5f), Vector3.zero, new Vector3(0.14f, 0.08f, 0.26f), shoe);
            }
            else
            {
                Transform legPivot = Pivot(t, "LegPivot_" + tag, new Vector3(side * 0.1f, 0.9f, 0f));
                Blob(legPivot, "Leg", new Vector3(0f, -0.44f, 0f), Vector3.zero, new Vector3(0.08f, 0.43f, 0.08f), trousers);
                Slab(legPivot, "Shoe", new Vector3(0f, -0.85f, 0.05f), Vector3.zero, new Vector3(0.14f, 0.08f, 0.26f), shoe);
            }

            Vector3 shoulder = new Vector3(side * 0.25f, 1.45f + yo, 0f);
            if (!bowed)
            {
                Transform armPivot = Pivot(t, "ArmPivot_" + tag, shoulder);
                Blob(armPivot, "Arm", new Vector3(0f, -0.25f, 0f), Vector3.zero, new Vector3(0.055f, 0.3f, 0.055f), coat);
            }
            else
            {
                Vector3 hand = paper ? new Vector3(side * 0.2f, 1.26f + yo, 0.29f) : new Vector3(side * 0.07f, 1.27f + yo, 0.3f);
                Vector3 dir = hand - shoulder;
                Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);
                Blob(t, "Arm", (hand + shoulder) * 0.5f, q.eulerAngles, new Vector3(0.055f, dir.magnitude * 0.5f, 0.055f), coat);
                Blob(t, "Hand", hand, Vector3.zero, new Vector3(0.045f, 0.045f, 0.045f), skin);
            }
        }

        Blob(t, "Torso", new Vector3(0f, 1.18f + yo, 0f), Vector3.zero, new Vector3(0.19f, 0.34f, 0.12f), coat);
        Blob(t, "Head", new Vector3(0f, 1.63f + yo, bowed ? 0.04f : 0f), new Vector3(bowed ? 25f : 0f, 0f, 0f), new Vector3(0.115f, 0.13f, 0.115f), skin);
        Blob(t, "Hair", new Vector3(0f, 1.72f + yo, bowed ? 0.02f : -0.01f), Vector3.zero, new Vector3(0.12f, 0.06f, 0.12f), hair);

        if (phone)
        {
            Slab(t, "Phone", new Vector3(0f, 1.28f + yo, 0.31f), new Vector3(-20f, 0f, 0f), new Vector3(0.07f, 0.14f, 0.012f), phoneMat);
            Slab(t, "Screen", new Vector3(0f, 1.28f + yo, 0.303f), new Vector3(-20f, 0f, 0f), new Vector3(0.06f, 0.12f, 0.004f), screenMat);
        }
        if (paper)
        {
            Slab(t, "Newspaper", new Vector3(0f, 1.3f + yo, 0.3f), new Vector3(-12f, 0f, 0f), new Vector3(0.46f, 0.34f, 0.012f), paperMat);
            Slab(t, "Headline", new Vector3(0f, 1.4f + yo, 0.293f), new Vector3(-12f, 0f, 0f), new Vector3(0.38f, 0.04f, 0.004f), ink);
            for (int k = 0; k < 2; k++)
            {
                Slab(t, "Text", new Vector3(0f, 1.31f - k * 0.07f + yo, 0.293f), new Vector3(-12f, 0f, 0f), new Vector3(0.38f, 0.014f, 0.004f), ink);
            }
        }

        var motion = root.AddComponent<MashhadAmbientMotion>();
        motion.motionType = MashhadAmbientMotion.MotionType.Idle;
        motion.speed = 0.6f + (v % 3) * 0.12f;
        motion.amplitude = 0.012f;

        if (lifelike)
        {
            var life = root.AddComponent<MashhadPassengerLife>();
            life.seated = seated;
            life.mode = phone ? MashhadPassengerLife.Mode.Phone : (paper ? MashhadPassengerLife.Mode.Paper : MashhadPassengerLife.Mode.Standing);
        }
        return root;
    }

    public static GameObject CreateWalker(Transform parent, Vector3 localPosition, int variant, float distance)
    {
        GameObject person = Build(parent, localPosition, 0f, variant, Pose.Standing, false);
        var motion = person.GetComponent<MashhadAmbientMotion>();
        motion.motionType = MashhadAmbientMotion.MotionType.Walk;
        motion.travelDirection = Vector3.forward;
        motion.travelDistance = distance;
        motion.speed = 0.28f + (Mathf.Abs(variant) % 3) * 0.05f;
        person.AddComponent<MashhadWalkCycle>();
        return person;
    }
}
