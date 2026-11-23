using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Original, lightweight runtime art for Mashhad Runner.
/// The station is assembled from dedicated mesh pieces rather than flat material swaps.
/// Decorative meshes intentionally have no colliders and do not affect runner gameplay.
/// </summary>
public static class MashhadRunnerArtFactory
{
    private static void DestroyObject(Object target)
    {
        if (target == null) return;
        if (Application.isPlaying) Object.Destroy(target);
        else Object.DestroyImmediate(target);
    }

    private static Material CreateMaterial(string name, Color color, float metallic, float smoothness, bool emission = false)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Diffuse");
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

    private static readonly Material Structure = CreateMaterial("MR_Station_PearlConcrete", new Color(.72f,.76f,.75f), .12f, .32f);
    private static readonly Material Metal = CreateMaterial("MR_Station_GraphiteMetal", new Color(.075f,.11f,.14f), .72f, .58f);
    private static readonly Material Glass = CreateMaterial("MR_Station_TealGlass", new Color(.12f,.42f,.49f), .32f, .78f);
    private static readonly Material Accent = CreateMaterial("MR_Station_Turquoise", new Color(.02f,.58f,.57f), .48f, .62f);
    private static readonly Material Light = CreateMaterial("MR_Station_WarmLED", new Color(1f,.62f,.22f), .1f, .45f, true);
    private static readonly Material Seat = CreateMaterial("MR_Station_Seat", new Color(.08f,.23f,.28f), .38f, .42f);
    private static readonly Material BusPaint = CreateMaterial("MR_Bus_DeepBlue", new Color(.025f,.20f,.34f), .42f, .5f);
    private static readonly Material BusWhite = CreateMaterial("MR_Bus_Pearl", new Color(.86f,.88f,.83f), .18f, .36f);
    private static readonly Material BusGlass = CreateMaterial("MR_Bus_Glass", new Color(.035f,.13f,.19f), .42f, .82f);
    private static readonly Material BusRubber = CreateMaterial("MR_Bus_Rubber", new Color(.025f,.03f,.035f), .05f, .2f);
    private static readonly Material CitySand = CreateMaterial("MR_City_Warm_Limestone", new Color(.69f,.57f,.43f), .08f, .28f);
    private static readonly Material CityCream = CreateMaterial("MR_City_Cream_Plaster", new Color(.86f,.78f,.64f), .04f, .24f);
    private static readonly Material CityTile = CreateMaterial("MR_City_Turquoise_Tile", new Color(.015f,.36f,.37f), .35f, .58f);
    private static readonly Material CityWindow = CreateMaterial("MR_City_Deep_Window", new Color(.055f,.16f,.2f), .3f, .8f);
    private static readonly Material CityDark = CreateMaterial("MR_City_Dark_Bronze", new Color(.12f,.105f,.08f), .55f, .48f);
    private static readonly Material ShrineGold = CreateMaterial("MR_Shrine_Gold", new Color(.83f,.53f,.12f), .68f, .68f);
    private static readonly Material ShrineTile = CreateMaterial("MR_Shrine_Turquoise", new Color(.01f,.31f,.34f), .45f, .62f);

    private static readonly Material TreeBark = CreateMaterial("MR_City_Cypress_Bark", new Color(.22f,.16f,.1f), .02f, .18f);
    private static readonly Material TreeLeaf = CreateMaterial("MR_City_Cypress_Deep_Green", new Color(.055f,.22f,.16f), .04f, .3f);
    private static readonly Material TreeLeafLight = CreateMaterial("MR_City_Cypress_Light_Green", new Color(.12f,.34f,.22f), .04f, .3f);
    private static readonly Material SignFace = CreateMaterial("MR_BRT_Sign_Deep_Teal", new Color(.012f,.12f,.15f), .35f, .55f);
    private static readonly Material SignWhite = CreateMaterial("MR_BRT_Sign_White", new Color(.92f,.94f,.88f), .08f, .32f);
    private static readonly Material LampMetal = CreateMaterial("MR_Streetlight_Graphite", new Color(.08f,.12f,.14f), .72f, .55f);

    /// <summary>Creates a custom warm-white LED streetlight with a clean architectural silhouette.</summary>
    public static GameObject CreateStreetLight(Vector3 position, int variant)
    {
        var root = new GameObject("MR_Original_Street_Light");
        root.transform.position = position;
        float lean = variant % 2 == 0 ? -1f : 1f;
        Part(root.transform, "Tapered_Pole_Lower", new Vector3(0f,2f,0f), new Vector3(.18f,4f,.18f), LampMetal);
        Part(root.transform, "Pole_Base_Collar", new Vector3(0f,.18f,0f), new Vector3(.34f,.36f,.34f), Accent);
        Part(root.transform, "Pole_Upper", new Vector3(0f,4.15f,0f), new Vector3(.13f,.45f,.13f), LampMetal);
        Part(root.transform, "Cantilever_Arm", new Vector3(lean*.72f,4.42f,0f), new Vector3(1.55f,.12f,.13f), LampMetal);
        Part(root.transform, "LED_Housing", new Vector3(lean*1.38f,4.32f,0f), new Vector3(.68f,.2f,.42f), LampMetal);
        Part(root.transform, "LED_Diffuser", new Vector3(lean*1.38f,4.205f,0f), new Vector3(.56f,.035f,.31f), Light);
        CombineStaticParts(root);
        return root;
    }

    /// <summary>Creates a bilingual-ready BRT wayfinding structure; high-contrast Latin route label is used until a Persian font asset is supplied.</summary>
    public static GameObject CreateBrtWayfindingSign(Vector3 position, int routeNumber, int side)
    {
        var root = new GameObject("MR_Original_BRT_Wayfinding_Sign");
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, side > 0 ? 90f : -90f, 0f);

        Part(root.transform, "Sign_Post", new Vector3(0f,1.9f,0f), new Vector3(.14f,3.8f,.14f), LampMetal);
        Part(root.transform, "Sign_Header_Frame", new Vector3(0f,3.45f,0f), new Vector3(2.8f,1.15f,.2f), LampMetal);
        Part(root.transform, "Sign_Header_Face", new Vector3(0f,3.45f,-.115f), new Vector3(2.62f,.97f,.045f), SignFace);
        Part(root.transform, "Sign_Top_Accent", new Vector3(0f,3.96f,-.15f), new Vector3(2.62f,.055f,.04f), Accent);
        Part(root.transform, "Sign_Route_Disc", new Vector3(-.82f,3.48f,-.16f), new Vector3(.58f,.58f,.05f), Accent);

        var route = new GameObject("Route_Number");
        route.transform.SetParent(root.transform, false);
        route.transform.localPosition = new Vector3(-.82f,3.48f,-.205f);
        route.transform.localRotation = Quaternion.Euler(0f,180f,0f);
        var routeText = route.AddComponent<TextMesh>();
        routeText.text = routeNumber.ToString("00");
        routeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        routeText.fontSize = 64;
        routeText.characterSize = .009f;
        routeText.anchor = TextAnchor.MiddleCenter;
        routeText.alignment = TextAlignment.Center;
        routeText.color = SignWhite.color;
        route.GetComponent<MeshRenderer>().sharedMaterial = routeText.font.material;

        var label = new GameObject("BRT_Label");
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = new Vector3(.35f,3.52f,-.205f);
        label.transform.localRotation = Quaternion.Euler(0f,180f,0f);
        var labelText = label.AddComponent<TextMesh>();
        labelText.text = "BRT  /  MASHHAD";
        labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.fontSize = 52;
        labelText.characterSize = .009f;
        labelText.anchor = TextAnchor.MiddleCenter;
        labelText.alignment = TextAlignment.Center;
        labelText.color = SignWhite.color;
        label.GetComponent<MeshRenderer>().sharedMaterial = labelText.font.material;
        return root;
    }

    /// <summary>Creates a compact stylized cypress tree for Mashhad streets.</summary>
    public static GameObject CreateCypressTree(Vector3 position, float scale, int variant)
    {
        var root = new GameObject("MR_Original_Cypress_Street_Tree");
        root.transform.position = position;
        root.transform.localScale = Vector3.one * scale;

        Part(root.transform, "Cypress_Trunk", new Vector3(0f,1.35f,0f), new Vector3(.34f,2.7f,.34f), TreeBark);
        for (int tier = 0; tier < 4; tier++)
        {
            float y = 1.35f + tier * .82f;
            float radius = 1.12f - tier * .19f;
            var foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = "Cypress_Foliage_Tier";
            foliage.transform.SetParent(root.transform, false);
            foliage.transform.localPosition = new Vector3(0f,y,0f);
            foliage.transform.localScale = new Vector3(radius*2f,1.65f,radius*2f);
            DestroyObject(foliage.GetComponent<Collider>());
            foliage.GetComponent<Renderer>().sharedMaterial = ((tier + variant) % 2 == 0) ? TreeLeaf : TreeLeafLight;
        }
        CombineStaticParts(root);
        return root;
    }

    /// <summary>Creates a Mashhad-inspired mid-rise with layered facade, recessed windows and tiled details.</summary>
    public static GameObject CreateMashhadBuilding(Vector3 position, float height, int variant, int roadSide)
    {
        var root = new GameObject("MR_Original_Mashhad_City_Building");
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, roadSide > 0 ? 90f : -90f, 0f);

        float width = 6f;
        float depth = 6f;
        Material facade = variant % 3 == 0 ? CitySand : CityCream;
        Part(root.transform, "Limestone_Building_Mass", new Vector3(0f,height*.5f,0f), new Vector3(width,height,depth), facade);
        Part(root.transform, "Ground_Floor_Base", new Vector3(0f,.55f,-depth*.505f), new Vector3(width,.22f,.12f), CityDark);
        Part(root.transform, "Turquoise_Cornice", new Vector3(0f,height-.65f,-depth*.53f), new Vector3(width+.22f,.22f,.18f), CityTile);
        Part(root.transform, "Roof_Parapet_Front", new Vector3(0f,height+.16f,-depth*.48f), new Vector3(width,.42f,.18f), facade);
        Part(root.transform, "Roof_Parapet_Left", new Vector3(-width*.48f,height+.16f,0f), new Vector3(.18f,.42f,depth), facade);
        Part(root.transform, "Roof_Parapet_Right", new Vector3(width*.48f,height+.16f,0f), new Vector3(.18f,.42f,depth), facade);

        int floors = Mathf.Max(2, Mathf.FloorToInt(height / 2.7f));
        for (int floor = 0; floor < floors; floor++)
        {
            float y = 1.75f + floor * 2.45f;
            if (y > height - .7f) break;
            for (int bay = -1; bay <= 1; bay++)
            {
                float x = bay * 1.75f;
                Part(root.transform, "Recessed_Window_Frame", new Vector3(x,y,-depth*.515f), new Vector3(1.18f,1.42f,.11f), CityDark);
                Part(root.transform, "Deep_Glazed_Window", new Vector3(x,y,-depth*.54f), new Vector3(.94f,1.17f,.055f), CityWindow);
                Part(root.transform, "Window_Mullion", new Vector3(x,y,-depth*.56f), new Vector3(.045f,1.15f,.035f), CityTile);
                Part(root.transform, "Window_Sill", new Vector3(x,y-.76f,-depth*.56f), new Vector3(1.28f,.1f,.22f), facade);
                if (floor > 0)
                {
                    Part(root.transform, "Balcony_Slab", new Vector3(x,y-.84f,-depth*.62f), new Vector3(1.35f,.12f,.58f), CitySand);
                    Part(root.transform, "Balcony_Railing", new Vector3(x,y-.48f,-depth*.69f), new Vector3(1.3f,.48f,.07f), CityDark);
                    for (int post = -2; post <= 2; post++)
                        Part(root.transform, "Balcony_Baluster", new Vector3(x+post*.25f,y-.48f,-depth*.7f), new Vector3(.035f,.44f,.06f), ShrineGold);
                }
            }
        }

        // Arched street-level entrance, built from individual voussoir blocks.
        Part(root.transform, "Entrance_Door", new Vector3(0f,1.05f,-depth*.535f), new Vector3(1.25f,2.1f,.09f), CityDark);
        Part(root.transform, "Entrance_Inset", new Vector3(0f,1.05f,-depth*.56f), new Vector3(.92f,1.82f,.05f), CityTile);
        for (int segment = 0; segment <= 8; segment++)
        {
            float angle = Mathf.PI * segment / 8f;
            float x = Mathf.Cos(angle) * .7f;
            float y = 2.12f + Mathf.Sin(angle) * .7f;
            var voussoir = Part(root.transform, "Entrance_Arch_Block", new Vector3(x,y,-depth*.56f), new Vector3(.2f,.22f,.16f), CitySand);
            voussoir.transform.localRotation = Quaternion.Euler(0f,0f,-segment*180f/8f);
        }
        Part(root.transform, "Facade_Tile_Band", new Vector3(0f,3.05f,-depth*.555f), new Vector3(width,.14f,.07f), CityTile);
        Part(root.transform, "Facade_Left_Pilaster", new Vector3(-2.72f,2f,-depth*.55f), new Vector3(.16f,3.6f,.16f), CitySand);
        Part(root.transform, "Facade_Right_Pilaster", new Vector3(2.72f,2f,-depth*.55f), new Vector3(.16f,3.6f,.16f), CitySand);
        CombineStaticParts(root);
        return root;
    }

    /// <summary>Creates a more legible, layered shrine-inspired skyline landmark; visual only.</summary>
    public static GameObject CreateShrineSkyline(Vector3 position, float scale)
    {
        var root = new GameObject("MR_Distant_Shrine_Skyline_Landmark");
        root.transform.position = position;
        root.transform.localScale = Vector3.one * scale;

        // Broad sanctuary mass and two projecting side wings establish a recognizable silhouette.
        Part(root.transform, "Sanctuary_Main_Mass", new Vector3(0f,5.1f,0f), new Vector3(12f,10.2f,8f), CitySand);
        Part(root.transform, "Sanctuary_Left_Wing", new Vector3(-7.2f,3.5f,.7f), new Vector3(3f,7f,6.2f), CityCream);
        Part(root.transform, "Sanctuary_Right_Wing", new Vector3(7.2f,3.5f,.7f), new Vector3(3f,7f,6.2f), CityCream);
        Part(root.transform, "Sanctuary_Base_Course", new Vector3(0f,.42f,-4.08f), new Vector3(12.2f,.65f,.25f), ShrineTile);
        Part(root.transform, "Sanctuary_Cornice", new Vector3(0f,9.7f,-4.08f), new Vector3(12.2f,.38f,.3f), ShrineGold);

        // Tall turquoise iwan with nested reveal, golden border and a segmented pointed arch.
        Part(root.transform, "Iwan_Turquoise_Field", new Vector3(0f,4.65f,-4.16f), new Vector3(5.1f,8.5f,.24f), ShrineTile);
        Part(root.transform, "Iwan_Deep_Recess", new Vector3(0f,4.35f,-4.32f), new Vector3(3.8f,7.5f,.16f), CityDark);
        Part(root.transform, "Iwan_Inner_Glow", new Vector3(0f,3.85f,-4.43f), new Vector3(2.85f,6.15f,.07f), Glass);
        Part(root.transform, "Iwan_Gold_Threshold", new Vector3(0f,.68f,-4.49f), new Vector3(3.25f,.18f,.16f), ShrineGold);
        for (int segment = 0; segment <= 12; segment++)
        {
            float angle = Mathf.PI * segment / 12f;
            float x = Mathf.Cos(angle) * 1.62f;
            float y = 6.0f + Mathf.Sin(angle) * 1.72f;
            var arch = Part(root.transform, "Iwan_Arch_Gilded_Block",
                new Vector3(x,y,-4.5f), new Vector3(.24f,.34f,.14f), ShrineGold);
            arch.transform.localRotation = Quaternion.Euler(0f,0f,-segment*180f/12f);
        }

        // Repeating turquoise tile panels and gold separators give the distant facade scale and rhythm.
        for (int side = -1; side <= 1; side += 2)
        {
            for (int level = 0; level < 3; level++)
            {
                float y = 2.2f + level * 2.05f;
                Part(root.transform, "Facade_Tile_Panel",
                    new Vector3(side*4.25f,y,-4.12f), new Vector3(1.25f,1.28f,.12f), ShrineTile);
                Part(root.transform, "Facade_Gold_Panel_Border",
                    new Vector3(side*4.25f,y+.7f,-4.2f), new Vector3(1.42f,.09f,.13f), ShrineGold);
            }
            Part(root.transform, "Iwan_Flanking_Pilaster",
                new Vector3(side*2.85f,4.55f,-4.34f), new Vector3(.22f,8.7f,.22f), ShrineGold);
        }

        // Dome drum, golden dome, stepped crown and finial.
        Part(root.transform, "Dome_Drum_Turquoise", new Vector3(0f,10.65f,0f), new Vector3(6.1f,1.85f,6.1f), ShrineTile);
        Part(root.transform, "Dome_Drum_Gold_Band", new Vector3(0f,11.55f,0f), new Vector3(6.25f,.16f,6.25f), ShrineGold);
        var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.name = "Golden_Dome";
        dome.transform.SetParent(root.transform, false);
        dome.transform.localPosition = new Vector3(0f,13.25f,0f);
        dome.transform.localScale = new Vector3(5.6f,4.05f,5.6f);
        DestroyObject(dome.GetComponent<Collider>());
        dome.GetComponent<Renderer>().sharedMaterial = ShrineGold;
        Part(root.transform, "Dome_Crown_Ring", new Vector3(0f,15.18f,0f), new Vector3(.72f,.16f,.72f), ShrineGold);
        Part(root.transform, "Dome_Finial_Neck", new Vector3(0f,15.58f,0f), new Vector3(.13f,.72f,.13f), ShrineGold);
        Part(root.transform, "Dome_Finial_Ball", new Vector3(0f,15.98f,0f), new Vector3(.34f,.34f,.34f), ShrineGold);

        // Twin turquoise minarets with projecting gold balconies, crowns and slender finials.
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 5.1f;
            Part(root.transform, "Minaret_Foot", new Vector3(x,1.1f,0f), new Vector3(1.65f,2.2f,1.65f), ShrineTile);
            Part(root.transform, "Minaret_Shaft", new Vector3(x,7.35f,0f), new Vector3(.92f,10.4f,.92f), ShrineTile);
            Part(root.transform, "Minaret_Gold_Balcony_Lower", new Vector3(x,10.8f,0f), new Vector3(1.65f,.22f,1.65f), ShrineGold);
            Part(root.transform, "Minaret_Gold_Balcony_Upper", new Vector3(x,11.1f,0f), new Vector3(1.35f,.18f,1.35f), ShrineGold);
            Part(root.transform, "Minaret_Crown", new Vector3(x,13.0f,0f), new Vector3(1.18f,1.2f,1.18f), ShrineGold);
            Part(root.transform, "Minaret_Neck", new Vector3(x,13.72f,0f), new Vector3(.38f,.35f,.38f), ShrineGold);
            Part(root.transform, "Minaret_Spire", new Vector3(x,14.55f,0f), new Vector3(.12f,1.45f,.12f), ShrineGold);
        }

        // Forecourt arcade columns frame the portal without creating gameplay collision.
        for (int side = -1; side <= 1; side += 2)
        {
            for (int bay = 0; bay < 3; bay++)
            {
                float z = -3.4f + bay * 2.8f;
                Part(root.transform, "Forecourt_Arcade_Column",
                    new Vector3(side*7.35f,2.05f,z), new Vector3(.28f,4.1f,.28f), CityCream);
                Part(root.transform, "Forecourt_Column_Capital",
                    new Vector3(side*7.35f,4.12f,z), new Vector3(.52f,.22f,.52f), ShrineGold);
            }
        }

        CombineStaticParts(root);
        return root;
    }

    /// <summary>Creates a stylized waiting passenger with a lightweight idle animation.</summary>
    public static GameObject CreateStationPassenger(Vector3 position, int variant)
    {
        Color[] coats = { new Color(.12f,.28f,.38f), new Color(.55f,.22f,.14f), new Color(.18f,.34f,.25f), new Color(.38f,.3f,.48f), new Color(.72f,.48f,.22f) };
        Material coat = CreateMaterial("MR_Passenger_Coat_" + variant, coats[variant % coats.Length], .02f, .22f);
        Material trousers = CreateMaterial("MR_Passenger_Trousers_" + variant, new Color(.12f,.15f,.18f), .02f, .2f);
        Material skin = CreateMaterial("MR_Passenger_Skin_" + variant, new Color(.67f,.45f,.32f), 0f, .18f);
        Material shoes = BusRubber;
        var root = new GameObject("MR_BRT_Waiting_Passenger");
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, (variant * 67) % 360, 0f);
        PassengerShape(root.transform, "Torso", new Vector3(0f,1.12f,0f), new Vector3(.48f,.72f,.3f), coat, PrimitiveType.Capsule);
        PassengerShape(root.transform, "Head", new Vector3(0f,1.72f,0f), new Vector3(.27f,.31f,.27f), skin, PrimitiveType.Sphere);
        PassengerShape(root.transform, "Hair", new Vector3(0f,1.88f,0f), new Vector3(.28f,.13f,.28f), CityDark, PrimitiveType.Sphere);
        for (int side=-1; side<=1; side+=2)
        {
            PassengerShape(root.transform, "Leg", new Vector3(side*.13f,.48f,0f), new Vector3(.15f,.58f,.17f), trousers, PrimitiveType.Capsule);
            PassengerShape(root.transform, "Shoe", new Vector3(side*.13f,.16f,-.04f), new Vector3(.17f,.11f,.29f), shoes, PrimitiveType.Cube);
            var arm = PassengerShape(root.transform, "Arm", new Vector3(side*.31f,1.12f,.015f), new Vector3(.13f,.57f,.14f), coat, PrimitiveType.Capsule);
            arm.transform.localRotation = Quaternion.Euler(0f,0f,side*9f);
        }
        var motion = root.AddComponent<MashhadAmbientMotion>();
        motion.motionType = MashhadAmbientMotion.MotionType.Idle;
        motion.speed = .7f + (variant % 3) * .12f;
        motion.amplitude = .018f;
        return root;
    }

    private static GameObject PassengerShape(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material, PrimitiveType shape)
    {
        var part = GameObject.CreatePrimitive(shape);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = scale;
        var collider = part.GetComponent<Collider>();
        if (collider != null) DestroyObject(collider);
        part.GetComponent<Renderer>().sharedMaterial = material;
        part.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        return part;
    }

    /// <summary>Creates an animated pedestrian passing the station forecourt.</summary>
    public static GameObject CreateWalkingPedestrian(Vector3 position, int variant, float distance)
    {
        var person = CreateStationPassenger(position, variant);
        person.name = "MR_BRT_Walking_Pedestrian";
        var motion = person.GetComponent<MashhadAmbientMotion>();
        motion.motionType = MashhadAmbientMotion.MotionType.Walk;
        motion.travelDirection = Vector3.right;
        motion.travelDistance = distance;
        motion.speed = .32f + (variant % 3) * .06f;
        return person;
    }

    /// <summary>Creates one detailed 8m BRT shelter module at a world-space position.</summary>
    public static GameObject CreateBrtStation(Vector3 position, int variant)
    {
        var root = new GameObject("MR_Original_BRT_Station");
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, (variant % 2 == 0) ? 0f : 180f, 0f);

        // Raised platform and tactile guidance strip.
        Part(root.transform, "Platform_Base", new Vector3(0f,.13f,0f), new Vector3(2.7f,.26f,8.2f), Structure);
        Part(root.transform, "Platform_Edge_Turquoise", new Vector3(.98f,.29f,0f), new Vector3(.08f,.08f,8f), Accent);
        for (int i = -3; i <= 3; i++)
            Part(root.transform, "Tactile_Tile", new Vector3(.67f,.285f,i*1.05f), new Vector3(.34f,.025f,.72f), Light);

        // Four structural columns, rear glazing and a layered cantilever roof.
        for (int z = -3; z <= 3; z += 2)
        {
            Part(root.transform, "Graphite_Support", new Vector3(-.86f,1.72f,z), new Vector3(.16f,3.1f,.16f), Metal);
            Part(root.transform, "Front_Support", new Vector3(.86f,1.72f,z), new Vector3(.12f,3.1f,.12f), Metal);
        }
        Part(root.transform, "Rear_Glass_Wall", new Vector3(-.72f,1.63f,0f), new Vector3(.055f,2.55f,7.55f), Glass);
        Part(root.transform, "Roof_Slab", new Vector3(0f,3.34f,0f), new Vector3(2.55f,.22f,8.25f), Metal);
        Part(root.transform, "Roof_Fascia", new Vector3(.08f,3.22f,0f), new Vector3(2.5f,.12f,8.12f), Accent);
        Part(root.transform, "Roof_Light_Strip", new Vector3(.35f,3.205f,0f), new Vector3(1.6f,.035f,7.7f), Light);

        // Route information tower with a strong silhouette and illuminated header.
        Part(root.transform, "Route_Sign_Frame", new Vector3(.15f,2.55f,-3.62f), new Vector3(1.55f,.88f,.14f), Metal);
        Part(root.transform, "Route_Sign_Face", new Vector3(.15f,2.58f,-3.535f), new Vector3(1.38f,.68f,.035f), Accent);
        Part(root.transform, "Route_Sign_Header", new Vector3(.15f,3.08f,-3.53f), new Vector3(1.38f,.12f,.04f), Light);
        Part(root.transform, "Route_Sign_Post", new Vector3(.15f,1.7f,-3.62f), new Vector3(.11f,1.65f,.11f), Metal);

        // Seating, divider and discreet wayfinding pylons.
        Part(root.transform, "Bench_Seat", new Vector3(-.12f,.72f,1.1f), new Vector3(.58f,.13f,2.35f), Seat);
        Part(root.transform, "Bench_Back", new Vector3(-.39f,1.05f,1.1f), new Vector3(.1f,.58f,2.35f), Seat);
        Part(root.transform, "Bench_Leg_A", new Vector3(-.12f,.43f,.15f), new Vector3(.12f,.55f,.12f), Metal);
        Part(root.transform, "Bench_Leg_B", new Vector3(-.12f,.43f,2.05f), new Vector3(.12f,.55f,.12f), Metal);
        Part(root.transform, "Glass_Divider", new Vector3(.58f,1.3f,2.85f), new Vector3(.035f,1.9f,.9f), Glass);
        Part(root.transform, "Wayfinding_Pole", new Vector3(.72f,1.55f,-2.4f), new Vector3(.1f,2.8f,.1f), Metal);
        Part(root.transform, "Wayfinding_Panel", new Vector3(.72f,2.75f,-2.4f), new Vector3(.42f,.5f,.08f), Accent);

        CombineStaticParts(root);
        return root;
    }

    /// <summary>Creates a low-poly articulated-looking BRT bus as a roadside hero prop.</summary>
    public static GameObject CreateBrtBus(Vector3 position, int variant)
    {
        var root = new GameObject("MR_Original_BRT_Bus");
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, (variant % 2 == 0) ? 0f : 180f, 0f);

        Part(root.transform, "Bus_Main_Body", new Vector3(0f,1.28f,0f), new Vector3(2.25f,1.72f,8.8f), BusWhite);
        Part(root.transform, "Bus_Roof", new Vector3(0f,2.18f,.05f), new Vector3(2.12f,.16f,8.45f), BusPaint);
        Part(root.transform, "Bus_Lower_Livery", new Vector3(0f,.72f,0f), new Vector3(2.28f,.38f,8.72f), BusPaint);
        Part(root.transform, "Bus_Turquoise_Belt", new Vector3(0f,1.08f,0f), new Vector3(2.29f,.12f,8.74f), Accent);
        Part(root.transform, "Bus_Front_Windscreen", new Vector3(0f,1.72f,-4.43f), new Vector3(1.82f,.82f,.045f), BusGlass);
        Part(root.transform, "Bus_Front_Bumper", new Vector3(0f,.55f,-4.48f), new Vector3(2.1f,.22f,.18f), Metal);
        Part(root.transform, "Bus_Front_Grille", new Vector3(0f,1.02f,-4.49f), new Vector3(.85f,.22f,.04f), Metal);
        Part(root.transform, "Bus_Front_Lamp_L", new Vector3(-.82f,1.02f,-4.5f), new Vector3(.28f,.16f,.05f), Light);
        Part(root.transform, "Bus_Front_Lamp_R", new Vector3(.82f,1.02f,-4.5f), new Vector3(.28f,.16f,.05f), Light);
        Part(root.transform, "Bus_Front_Destination_Board", new Vector3(0f,2.13f,-4.505f), new Vector3(1.12f,.22f,.035f), SignFace);
        Part(root.transform, "Bus_Front_Lower_Turquoise_Trim", new Vector3(0f,.43f,-4.54f), new Vector3(1.65f,.055f,.035f), Accent);
        Part(root.transform, "Bus_Roof_AC_Unit", new Vector3(0f,2.38f,1.25f), new Vector3(.92f,.22f,1.55f), Metal);
        Part(root.transform, "Bus_Roof_AC_Grille", new Vector3(0f,2.505f,1.25f), new Vector3(.68f,.035f,1.12f), BusGlass);

        CreateBusText(root.transform, "Front_Route_Number", "01", new Vector3(0f,2.13f,-4.53f), Quaternion.Euler(0f,180f,0f), .012f, 72);
        CreateBusText(root.transform, "Front_BRT_Label", "BRT", new Vector3(0f,1.42f,-4.535f), Quaternion.Euler(0f,180f,0f), .012f, 58);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 1.14f;
            for (int z = -3; z <= 3; z += 2)
                Part(root.transform, "Bus_Side_Window", new Vector3(x,1.76f,z), new Vector3(.045f,.68f,1.55f), BusGlass);

            Part(root.transform, "Bus_Door_Glass", new Vector3(x,1.45f,-1.05f), new Vector3(.05f,1.12f,.82f), BusGlass);
            Part(root.transform, "Bus_Door_Frame_Top", new Vector3(x,2.04f,-1.05f), new Vector3(.08f,.07f,.92f), Metal);
            Part(root.transform, "Bus_Side_Lower_Accent", new Vector3(side*1.17f,.83f,.65f), new Vector3(.035f,.075f,5.8f), Accent);
            Part(root.transform, "Bus_Mirror_Arm", new Vector3(side*1.28f,1.95f,-4.05f), new Vector3(.32f,.07f,.08f), Metal);
            CreateBusText(root.transform, "Side_BRT_Identity", "BRT  MASHHAD", new Vector3(side*1.18f,1.12f,2.05f), Quaternion.Euler(0f,side*90f,0f), .009f, 42);
            Part(root.transform, "Bus_Mirror", new Vector3(side*1.42f,1.92f,-4.05f), new Vector3(.08f,.25f,.2f), BusGlass);

            for (int z = -2; z <= 2; z += 4)
            {
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Bus_Wheel";
                wheel.transform.SetParent(root.transform, false);
                wheel.transform.localPosition = new Vector3(side*1.12f,.52f,z);
                wheel.transform.localRotation = Quaternion.Euler(0f,0f,90f);
                wheel.transform.localScale = new Vector3(.58f,.24f,.58f);
                var wheelCollider = wheel.GetComponent<Collider>();
                if (wheelCollider != null) DestroyObject(wheelCollider);
                wheel.GetComponent<Renderer>().sharedMaterial = BusRubber;

                var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hub.name = "Bus_Wheel_Hub";
                hub.transform.SetParent(root.transform, false);
                hub.transform.localPosition = new Vector3(side*1.255f,.52f,z);
                hub.transform.localRotation = Quaternion.Euler(0f,0f,90f);
                hub.transform.localScale = new Vector3(.24f,.035f,.24f);
                DestroyObject(hub.GetComponent<Collider>());
                hub.GetComponent<Renderer>().sharedMaterial = Metal;
            }
        }

        Part(root.transform, "Bus_Rear_Window", new Vector3(0f,1.72f,4.43f), new Vector3(1.8f,.78f,.045f), BusGlass);
        Part(root.transform, "Bus_Route_Display", new Vector3(0f,2.02f,-4.49f), new Vector3(.72f,.16f,.035f), Light);
        CombineStaticParts(root);
        var idleMotion = root.AddComponent<MashhadAmbientMotion>();
        idleMotion.motionType = MashhadAmbientMotion.MotionType.BusIdle;
        idleMotion.speed = .55f;
        idleMotion.amplitude = .012f;
        return root;
    }

    private static void CreateBusText(Transform parent, string name, string value, Vector3 localPosition, Quaternion localRotation, float characterSize, int fontSize)
    {
        var label = new GameObject(name);
        label.transform.SetParent(parent, false);
        label.transform.localPosition = localPosition;
        label.transform.localRotation = localRotation;
        var text = label.AddComponent<TextMesh>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.characterSize = characterSize;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = SignWhite.color;
        var renderer = label.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = text.font.material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private static GameObject Part(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = scale;
        var collider = part.GetComponent<Collider>();
        if (collider != null) DestroyObject(collider);
        var renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return part;
    }

    // Merge decorative primitives by material to keep draw calls and hierarchy size low on mobile.
    private static void CombineStaticParts(GameObject root)
    {
        var groups = new System.Collections.Generic.Dictionary<Material, System.Collections.Generic.List<CombineInstance>>();
        var originals = new System.Collections.Generic.List<GameObject>();
        var filters = root.GetComponentsInChildren<MeshFilter>(true);

        foreach (var filter in filters)
        {
            if (filter.transform == root.transform) continue;
            var renderer = filter.GetComponent<MeshRenderer>();
            if (filter.GetComponent<TextMesh>() != null) continue;
            if (renderer == null || filter.sharedMesh == null || renderer.sharedMaterial == null) continue;

            Material material = renderer.sharedMaterial;
            if (!groups.TryGetValue(material, out var combines))
            {
                combines = new System.Collections.Generic.List<CombineInstance>();
                groups.Add(material, combines);
            }

            combines.Add(new CombineInstance
            {
                mesh = filter.sharedMesh,
                transform = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix
            });
            originals.Add(filter.gameObject);
        }

        foreach (var entry in groups)
        {
            var combinedObject = new GameObject("Combined_" + entry.Key.name);
            combinedObject.transform.SetParent(root.transform, false);
            var mesh = new Mesh();
            mesh.name = "MR_Combined_" + entry.Key.name;
            if (entry.Value.Count * 24 > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(entry.Value.ToArray(), true, true);
            combinedObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var combinedRenderer = combinedObject.AddComponent<MeshRenderer>();
            combinedRenderer.sharedMaterial = entry.Key;
            combinedRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            combinedRenderer.receiveShadows = true;
        }

        foreach (var original in originals)
            DestroyObject(original);
    }
}
