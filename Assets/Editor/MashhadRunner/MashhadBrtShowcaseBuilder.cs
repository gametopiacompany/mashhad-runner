using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class MashhadBrtShowcaseBuilder
{
    private const string ScenePath = "Assets/Scenes/MashhadRunner_BRT_ArtShowcase.unity";
    static MashhadBrtShowcaseBuilder()
    {
        EditorApplication.delayCall += GenerateOnce;
    }

    [MenuItem("Mashhad Runner/Build BRT Mashhad Art Showcase")]
    public static void BuildFromMenu()
    {
        BuildScene();
        Debug.Log("[Mashhad Runner] BRT Mashhad art showcase saved to: " + ScenePath);
    }

    private static void GenerateOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            return;
        }
        BuildScene();
        Debug.Log("[Mashhad Runner] Created BRT Mashhad art showcase: " + ScenePath);
    }

    private static void BuildScene()
    {
        EnsureFolder("Assets/Scenes");
        Scene previous = SceneManager.GetActiveScene();
        bool preservePreviousScene = previous.IsValid() && !string.IsNullOrEmpty(previous.path);
        NewSceneMode mode = preservePreviousScene ? NewSceneMode.Additive : NewSceneMode.Single;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
        SceneManager.SetActiveScene(scene);

        Material asphalt = Mat("MR_Showcase_Asphalt", new Color(.105f,.125f,.15f), .08f, .28f);
        Material sidewalk = Mat("MR_Showcase_Sandstone", new Color(.56f,.49f,.39f), .04f, .3f);
        Material lane = Mat("MR_Showcase_LanePaint", new Color(.83f,.78f,.65f), .05f, .25f);
        Material median = Mat("MR_Showcase_Median", new Color(.18f,.28f,.27f), .1f, .35f);
        Material warm = Mat("MR_Showcase_WarmLED", new Color(1f,.64f,.3f), .1f, .45f, true);

        // Continuous urban corridor: pedestrian edges, central BRT carriageway and planted median.
        Box("Full Urban Ground", new Vector3(0,-.78f,76f), new Vector3(58,1.2f,190), sidewalk, true);
        Box("BRT Main Asphalt", new Vector3(0,-.08f,76f), new Vector3(18,.16f,190), asphalt, true);
        Box("BRT Inner Lane", new Vector3(0,.015f,76f), new Vector3(7.2f,.025f,190), asphalt, false);
        Box("Median Spine", new Vector3(0,.08f,76f), new Vector3(.42f,.18f,190), median, false);
        for (int z=0; z<190; z+=8)
        {
            Box("Lane Dash L", new Vector3(-3.65f,.045f,z+2f), new Vector3(.10f,.025f,3.8f), lane, false);
            Box("Lane Dash R", new Vector3(3.65f,.045f,z+2f), new Vector3(.10f,.025f,3.8f), lane, false);
        }
        for (int side=-1; side<=1; side+=2)
        {
            Box("Raised Pedestrian Walk", new Vector3(side*12.7f,-.12f,76f), new Vector3(7.2f,.32f,190), sidewalk, true);
            Box("Curb Edge", new Vector3(side*9.15f,.04f,76f), new Vector3(.28f,.35f,190), median, false);
            Box("Outer Road", new Vector3(side*22f,-.12f,76f), new Vector3(11f,.18f,190), asphalt, false);
        }

        // Two full-size shelters, with the existing detailed authored station and bus meshes.
        MashhadRunnerArtFactory.CreateBrtStation(new Vector3(-7.15f,0f,28f), 0);
        MashhadRunnerArtFactory.CreateBrtStation(new Vector3(-7.15f,0f,68f), 1);
        MashhadRunnerArtFactory.CreateBrtStation(new Vector3(7.15f,0f,108f), 0);
        MashhadRunnerArtFactory.CreateBrtBus(new Vector3(4.9f,0f,43f), 0);
        MashhadRunnerArtFactory.CreateBrtBus(new Vector3(-4.9f,0f,91f), 1);

        // Waiting passengers bring scale and life to all three platforms.
        Vector3[] passengerPositions =
        {
            new Vector3(-7.55f,.27f,25.6f), new Vector3(-6.85f,.27f,27.8f), new Vector3(-7.55f,.27f,30.1f),
            new Vector3(-7.55f,.27f,65.8f), new Vector3(-6.85f,.27f,68.2f), new Vector3(-7.55f,.27f,70.4f),
            new Vector3(7.55f,.27f,105.8f), new Vector3(6.85f,.27f,108.1f), new Vector3(7.55f,.27f,110.4f)
        };
        for (int i=0; i<passengerPositions.Length; i++)
            MashhadRunnerArtFactory.CreateStationPassenger(passengerPositions[i], i);

        // A few pedestrians move along the outer sidewalks, away from the playable BRT lane.
        for (int i=0; i<6; i++)
        {
            float z = 18f + i*25f;
            float side = (i%2==0) ? -1f : 1f;
            MashhadRunnerArtFactory.CreateWalkingPedestrian(new Vector3(side*13.1f,0f,z),i+2,2.4f);
        }

        // Mashhad-inspired street wall: varied heights, repeated cypress rhythm and wayfinding.
        for (int z=0; z<160; z+=24)
        {
            int v=(z/24)%3;
            float h=v==2 ? 15f : (v==1 ? 11f : 9f);
            MashhadRunnerArtFactory.CreateMashhadBuilding(new Vector3(-20f,0,z+12f),h,v,-1);
            MashhadRunnerArtFactory.CreateMashhadBuilding(new Vector3(20f,0,z+20f),h+2f,(v+1)%3,1);
            MashhadRunnerArtFactory.CreateCypressTree(new Vector3(-12.3f,0,z+8f),1f,(v+1)%2);
            MashhadRunnerArtFactory.CreateCypressTree(new Vector3(12.3f,0,z+18f),1.08f,v);
            MashhadRunnerArtFactory.CreateStreetLight(new Vector3((z/24)%2==0 ? -10.4f : 10.4f,0,z+3f),z/24);
            MashhadRunnerArtFactory.CreateBrtWayfindingSign(new Vector3((z/24)%2==0 ? 10.2f : -10.2f,0,z+13f),(z/24)%12+1,(z/24)%2==0 ? 1 : -1);
        }

        // Distant landmark is deliberately centered on the corridor's vanishing point.
        MashhadRunnerArtFactory.CreateShrineSkyline(new Vector3(0f,0f,166f),1.72f);

        // Street furniture and low warm practicals reinforce scale and depth.
        for (int z=12; z<180; z+=18)
        {
            for (int side=-1; side<=1; side+=2)
            {
                Box("Planter",new Vector3(side*15.5f,.12f,z),new Vector3(2.1f,.35f,3.2f),median,false);
                MashhadRunnerArtFactory.CreateCypressTree(new Vector3(side*15.5f,.28f,z),.62f,z/18);
            }
        }

        GameObject sunObject=new GameObject("Golden Hour Sun");
        Light sun=sunObject.AddComponent<Light>();
        sun.type=LightType.Directional;
        sun.color=new Color(1f,.79f,.59f);
        sun.intensity=1.25f;
        sun.shadows=LightShadows.Soft;
        sun.shadowStrength=.62f;
        sun.transform.rotation=Quaternion.Euler(34f,-28f,0f);

        GameObject fillObject=new GameObject("Cool Sky Fill");
        Light fill=fillObject.AddComponent<Light>();
        fill.type=LightType.Directional;
        fill.color=new Color(.52f,.68f,.78f);
        fill.intensity=.28f;
        fill.shadows=LightShadows.None;
        fill.transform.rotation=Quaternion.Euler(18f,145f,0f);

        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.48f,.57f,.66f);
        RenderSettings.ambientEquatorColor=new Color(.63f,.52f,.4f);
        RenderSettings.ambientGroundColor=new Color(.22f,.2f,.18f);
        RenderSettings.fog=true;
        RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogColor=new Color(.69f,.59f,.48f);
        RenderSettings.fogStartDistance=72f;
        RenderSettings.fogEndDistance=225f;
        RenderSettings.skybox=null;
        QualitySettings.shadowDistance=115f;

        GameObject cameraObject=new GameObject("BRT Showcase Camera");
        Camera camera=cameraObject.AddComponent<Camera>();
        camera.tag="MainCamera";
        camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.66f,.58f,.49f);
        camera.fieldOfView=62f;
        camera.farClipPlane=280f;
        cameraObject.transform.position=new Vector3(0f,5.2f,-12f);
        cameraObject.transform.LookAt(new Vector3(0f,4.2f,74f));

        GameObject title=new GameObject("Showcase Title");
        title.transform.position=new Vector3(0f,6.2f,5f);
        TextMesh tm=title.AddComponent<TextMesh>();
        tm.text="MASHHAD  /  BRT CORRIDOR";
        tm.font=Resources.GetBuiltinResource<Font>("Arial.ttf");
        tm.fontSize=64; tm.characterSize=.12f;
        tm.anchor=TextAnchor.MiddleCenter; tm.alignment=TextAlignment.Center;
        tm.color=new Color(.96f,.88f,.72f);
        title.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene,ScenePath);
        if (preservePreviousScene)
        {
            EditorSceneManager.CloseScene(scene,true);
            SceneManager.SetActiveScene(previous);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets","Scenes");
    }

    private static Material Mat(string name, Color color, float metallic, float smoothness, bool emission=false)
    {
        Shader shader=Shader.Find("Standard");
        if(shader==null) shader=Shader.Find("Universal Render Pipeline/Lit");
        Material m=new Material(shader); m.name=name; m.color=color;
        if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smoothness);
        if(emission && m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.3f);}
        return m;
    }

    private static GameObject Box(string name, Vector3 pos, Vector3 size, Material mat, bool collider)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name=name; go.transform.position=pos; go.transform.localScale=size;
        Renderer r=go.GetComponent<Renderer>(); r.sharedMaterial=mat;
        r.shadowCastingMode=ShadowCastingMode.On; r.receiveShadows=true;
        if(!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
}