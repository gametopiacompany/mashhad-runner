using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class RunnerGame : MonoBehaviour
{
    private const float LaneWidth = 2.2f;
    private const float RunSpeed = 11f;
    private const int EnvironmentChunkLength = 160;

    private readonly List<WorldObject> worldObjects = new List<WorldObject>();
    private readonly List<GameObject> environmentObjects = new List<GameObject>();
    private readonly HashSet<int> collectedLetters = new HashSet<int>();
    private readonly Dictionary<string, GameObject> artPrefabs = new Dictionary<string, GameObject>();
    private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

    private Transform player;
    private Transform playerVisual;
    private Vector3 playerVisualDefaultScale;
    private Transform agent;
    private Transform agentVisual;
    private Vector3 agentVisualDefaultScale;
    private Transform cameraTransform;
    private Camera gameCamera;
    private Texture2D cityCommercialAtlas;

    private int lane;
    private int lives = 3;
    private int coins;
    private int bestDistance;
    private float distance;
    private float safeDistance;
    private float verticalVelocity;
    private float slideTimer;
    private float invulnerableTimer;
    private float agentDistance = 8f;
    private bool gameOver;
    private bool canContinue = true;
    private bool touchLeft;
    private bool touchRight;
    private bool touchJump;
    private bool touchSlide;
    private int nextEnvironmentZ = EnvironmentChunkLength;
    private float nextContentStart = 500f;

    private sealed class WorldObject
    {
        public GameObject gameObject;
        public WorldObjectKind kind;
        public int lane;
        public int letterIndex;
        public bool collected;
    }

    private void UpdateEnvironment()
    {
        while (player.position.z + 120f >= nextEnvironmentZ)
        {
            int endZ = nextEnvironmentZ + EnvironmentChunkLength;
            CreateEnvironmentChunk(nextEnvironmentZ, endZ);
            nextEnvironmentZ = endZ;
        }

        for (int i = environmentObjects.Count - 1; i >= 0; i--)
        {
            var environmentObject = environmentObjects[i];
            if (environmentObject == null || environmentObject.transform.position.z < player.position.z - 32f)
            {
                if (environmentObject != null)
                {
                    Destroy(environmentObject);
                }
                environmentObjects.RemoveAt(i);
            }
        }
    }

    private void UpdateContent()
    {
        while (player.position.z + 250f >= nextContentStart)
        {
            float end = nextContentStart + 480f;
            SpawnRunContent(nextContentStart, end);
            nextContentStart = end;
        }
    }

    private enum WorldObjectKind
    {
        Coin,
        Letter,
        Barrier,
        Overhead,
        Vehicle
    }

    private void Start()
    {
        bestDistance = PlayerPrefs.GetInt("MashhadRunnerBestDistance", 0);
        CreateEnvironment();
        CreatePlayer();
        CreateAgent();
        CreateCamera();
        SpawnRunContent(18f, nextContentStart);
    }

    private void CreateEnvironment()
    {
        RenderSettings.ambientLight = new Color(0.78f, 0.82f, 0.88f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 55f;
        RenderSettings.fogEndDistance = 150f;
        cityCommercialAtlas = Resources.Load<Texture2D>("KenneyCityCommercial/Textures/colormap");
        if (cityCommercialAtlas == null)
        {
            throw new UnityException("Missing Kenney City Commercial texture atlas.");
        }

        var lightObject = new GameObject("Sun");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.None;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        CreateEnvironmentChunk(-40, nextEnvironmentZ);
    }

    private void CreateEnvironmentChunk(int startZ, int endZ)
    {
        int chunkLength = endZ - startZ;
        float chunkCenterZ = (startZ + endZ) * 0.5f;
        environmentObjects.Add(CreateBox("Ground", new Vector3(0f, -0.6f, chunkCenterZ), new Vector3(20f, 1f, chunkLength), new Color(0.22f, 0.24f, 0.26f)));
        environmentObjects.Add(CreateBox("BRT Road", new Vector3(0f, -0.02f, chunkCenterZ), new Vector3(8f, 0.08f, chunkLength), new Color(0.23f, 0.25f, 0.28f)));

        for (int z = startZ; z < endZ; z += 8)
        {
            for (int boundaryIndex = 0; boundaryIndex < 4; boundaryIndex++)
            {
                float x = -1.5f * LaneWidth + boundaryIndex * LaneWidth;
                environmentObjects.Add(CreateBox("Lane Mark", new Vector3(x, 0.03f, z), new Vector3(0.06f, 0.02f, 3.8f), Color.white));
            }
        }

        for (int z = Mathf.Max(0, startZ); z < endZ; z += 24)
        {
            environmentObjects.Add(CreateBox("BRT Left Shelter", new Vector3(-7.2f, 1.35f, z), new Vector3(2f, 2.7f, 8f), new Color(0.25f, 0.48f, 0.62f)));
            environmentObjects.Add(CreateBox("BRT Left Roof", new Vector3(-7.2f, 3.05f, z), new Vector3(2.4f, 0.25f, 8.4f), new Color(0.9f, 0.55f, 0.18f)));
            environmentObjects.Add(CreateBox("BRT Right Pole", new Vector3(7.2f, 2f, z), new Vector3(0.18f, 4f, 0.18f), new Color(0.8f, 0.82f, 0.84f)));
        }

        int firstLampZ = Mathf.Max(0, ((startZ + 47) / 48) * 48);
        for (int z = firstLampZ; z < endZ; z += 48)
        {
            int side = (z / 48) % 2 == 0 ? 1 : -1;
            environmentObjects.Add(CreateArtAsset("KenneyCityRoads/light-square", "Street Light", new Vector3(side * 5.2f, 0f, z), new Vector3(0.7f, 4f, 0.7f)));

            if ((z / 48) % 2 == 0)
            {
                environmentObjects.Add(CreateArtAsset("KenneyCityRoads/traffic-light", "Traffic Light", new Vector3(-side * 5.2f, 0f, z + 12f), new Vector3(1.1f, 4f, 0.8f)));
            }
        }

        int firstBuildingZ = Mathf.Max(0, ((startZ + 47) / 48) * 48);
        for (int z = firstBuildingZ; z < endZ; z += 48)
        {
            int buildingIndex = (z / 48) % 3;
            int side = (z / 48) % 2 == 0 ? 1 : -1;
            string buildingAsset = "KenneyCityCommercial/building-" + (char)('a' + buildingIndex);
            float buildingHeight = buildingIndex == 2 ? 12f : 9f;
            Color buildingTint = buildingIndex == 0
                ? new Color(0.82f, 0.9f, 0.96f)
                : buildingIndex == 1
                    ? new Color(0.9f, 0.94f, 0.86f)
                    : new Color(0.98f, 0.83f, 0.65f);
            var building = CreateArtAsset(
                buildingAsset,
                "City Building",
                new Vector3(side * 16f, 0f, z + 64f),
                new Vector3(6f, buildingHeight, 6f));
            foreach (var renderer in building.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                var materials = renderer.materials;
                foreach (var material in materials)
                {
                    if (material.HasProperty("_MainTex"))
                    {
                        material.mainTexture = cityCommercialAtlas;
                    }
                    if (material.HasProperty("_Color"))
                    {
                        material.color = buildingTint;
                    }
                }
                renderer.materials = materials;
            }
            environmentObjects.Add(building);
        }
    }

    private void CreatePlayer()
    {
        var root = new GameObject("Player");
        root.transform.position = new Vector3(0f, 0f, 0f);

        playerVisual = CreateArtAsset("KenneyMiniCharacters/character-male-a", "Runner", Vector3.zero, new Vector3(0.9f, 1.8f, 0.7f)).transform;
        playerVisual.SetParent(root.transform, true);
        playerVisualDefaultScale = playerVisual.localScale;

        root.AddComponent<BoxCollider>().center = new Vector3(0f, 1f, 0f);
        root.GetComponent<BoxCollider>().size = new Vector3(0.75f, 1.9f, 0.7f);
        player = root.transform;
    }

    private void CreateAgent()
    {
        var root = new GameObject("Pursuing Agent");
        root.transform.position = new Vector3(0f, 0f, -agentDistance);

        agentVisual = CreateArtAsset("KenneyMiniCharacters/character-worker", "Pursuing Agent", root.transform.position, new Vector3(0.9f, 1.8f, 0.7f)).transform;
        agentVisual.SetParent(root.transform, true);
        agentVisualDefaultScale = agentVisual.localScale;

        agent = root.transform;
    }

    private void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        gameCamera = cameraObject.AddComponent<Camera>();
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color(0.62f, 0.78f, 0.86f);
        cameraObject.tag = "MainCamera";
        cameraTransform = cameraObject.transform;
        cameraTransform.position = new Vector3(0f, 5.5f, -9f);
        cameraTransform.rotation = Quaternion.Euler(15f, 0f, 0f);
    }

    private void SpawnRunContent(float startZ, float endZ)
    {
        for (float z = startZ; z < endZ; z += 9f)
        {
            int pattern = Mathf.FloorToInt(z / 9f) % 8;
            int firstLane = (pattern + 1) % 3 - 1;

            if (pattern == 0)
            {
                SpawnCoinLine(z, firstLane, 4);
            }
            else if (pattern == 1)
            {
                SpawnBarrier(z, firstLane);
                SpawnCoinLine(z + 2f, -firstLane, 3);
            }
            else if (pattern == 2)
            {
                SpawnLetter(z, firstLane, 0);
            }
            else if (pattern == 3)
            {
                SpawnOverhead(z, firstLane);
                SpawnCoinLine(z + 1f, -firstLane, 3);
            }
            else if (pattern == 4)
            {
                SpawnVehicle(z, firstLane);
                SpawnLetter(z + 2f, -firstLane, 1);
            }
            else if (pattern == 5)
            {
                SpawnCoinLine(z, -1, 3);
                SpawnCoinLine(z, 0, 3);
            }
            else if (pattern == 6)
            {
                SpawnBarrier(z, firstLane);
                SpawnLetter(z + 2f, 0, 2);
            }
            else
            {
                SpawnCoinLine(z, firstLane, 5);
                SpawnOverhead(z + 4f, -firstLane);
            }
        }
    }

    private void Update()
    {
        if (gameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartRun();
            }
            return;
        }

        HandleInput();
        UpdateRunner();
        UpdateAgent();
        UpdateEnvironment();
        UpdateContent();
        UpdateWorldObjects();

        if (invulnerableTimer > 0f)
        {
            invulnerableTimer -= Time.deltaTime;
        }
        if (distance >= safeDistance + 100f)
        {
            safeDistance = distance;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null || player == null)
        {
            return;
        }

        var target = new Vector3(0f, 5.5f, player.position.z - 9f);
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, target, Time.deltaTime * 8f);
        cameraTransform.LookAt(new Vector3(player.position.x, 1f, player.position.z + 6f));
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || touchLeft)
        {
            lane = Mathf.Max(-1, lane - 1);
        }
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || touchRight)
        {
            lane = Mathf.Min(1, lane + 1);
        }
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space) || touchJump)
            && player.position.y <= 0.01f)
        {
            verticalVelocity = 8.5f;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftControl) || touchSlide)
        {
            slideTimer = 0.55f;
        }

        touchLeft = false;
        touchRight = false;
        touchJump = false;
        touchSlide = false;
    }

    private void UpdateRunner()
    {
        distance += RunSpeed * Time.deltaTime;
        var targetPosition = new Vector3(lane * LaneWidth, player.position.y, distance);

        if (player.position.y > 0f || verticalVelocity > 0f)
        {
            verticalVelocity -= 20f * Time.deltaTime;
            targetPosition.y = Mathf.Max(0f, player.position.y + verticalVelocity * Time.deltaTime);
            if (targetPosition.y <= 0f)
            {
                targetPosition.y = 0f;
                verticalVelocity = 0f;
            }
        }

        if (slideTimer > 0f)
        {
            slideTimer -= Time.deltaTime;
            playerVisual.localScale = Vector3.Lerp(playerVisual.localScale, Vector3.Scale(playerVisualDefaultScale, new Vector3(1f, 0.55f, 1.2f)), Time.deltaTime * 14f);
            playerVisual.localPosition = Vector3.zero;
        }
        else
        {
            playerVisual.localScale = Vector3.Lerp(playerVisual.localScale, playerVisualDefaultScale, Time.deltaTime * 14f);
            playerVisual.localPosition = Vector3.zero;
        }

        player.position = Vector3.Lerp(player.position, targetPosition, Time.deltaTime * 12f);

        if (playerVisual != null)
        {
            playerVisual.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 12f) * 4f, Mathf.Sin(Time.time * 12f) * 3f);
        }
    }

    private void UpdateAgent()
    {
        float targetZ = player.position.z - agentDistance;
        float targetX = Mathf.Lerp(agent.position.x, player.position.x, Time.deltaTime * 2f);
        agent.position = new Vector3(targetX, 0f, targetZ);

        if (agentDistance > 3.2f)
        {
            agentDistance -= Time.deltaTime * 0.16f;
        }

        if (agent.position.z >= player.position.z - 3.2f && invulnerableTimer <= 0f)
        {
            HitObstacle();
        }

        agentVisual.localScale = Vector3.Scale(agentVisualDefaultScale, Vector3.one + Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 7f)) * 0.04f);
    }

    private void UpdateWorldObjects()
    {
        for (int i = worldObjects.Count - 1; i >= 0; i--)
        {
            var item = worldObjects[i];
            if (item.gameObject == null)
            {
                worldObjects.RemoveAt(i);
                continue;
            }

            if (item.collected || item.gameObject.transform.position.z < player.position.z - 20f)
            {
                Destroy(item.gameObject);
                worldObjects.RemoveAt(i);
                continue;
            }

            if (item.kind == WorldObjectKind.Coin)
            {
                item.gameObject.transform.Rotate(Vector3.up, 180f * Time.deltaTime);
            }

            if (item.gameObject.transform.position.z < player.position.z - 12f)
            {
                continue;
            }

            float laneDifference = Mathf.Abs(item.gameObject.transform.position.x - player.position.x);
            float zDifference = Mathf.Abs(item.gameObject.transform.position.z - player.position.z);
            if (laneDifference > 1f || zDifference > 1.15f)
            {
                continue;
            }

            if (item.kind == WorldObjectKind.Coin || item.kind == WorldObjectKind.Letter)
            {
                Collect(item);
                continue;
            }

            if (invulnerableTimer <= 0f && HasCollision(item))
            {
                HitObstacle();
            }
        }
    }

    private bool HasCollision(WorldObject item)
    {
        if (item.kind == WorldObjectKind.Vehicle || item.kind == WorldObjectKind.Barrier)
        {
            return player.position.y < 1.05f;
        }

        if (item.kind == WorldObjectKind.Overhead)
        {
            return slideTimer <= 0f && player.position.y < 1.8f;
        }
        return false;
    }

    private void HitObstacle()
    {
        invulnerableTimer = 1.5f;
        lives--;
        agentDistance = 8f;
        if (lives <= 0)
        {
            EndRun();
        }
    }

    private void Collect(WorldObject item)
    {
        item.collected = true;
        item.gameObject.SetActive(false);
        if (item.kind == WorldObjectKind.Coin)
        {
            coins++;
        }
        else if (!collectedLetters.Contains(item.letterIndex))
        {
            collectedLetters.Add(item.letterIndex);
            if (collectedLetters.Count >= 3)
            {
                lives = Mathf.Min(3, lives + 1);
                collectedLetters.Clear();
            }
        }
    }

    private void EndRun()
    {
        int finalDistance = Mathf.FloorToInt(distance);
        if (finalDistance > bestDistance)
        {
            bestDistance = finalDistance;
            PlayerPrefs.SetInt("MashhadRunnerBestDistance", bestDistance);
            PlayerPrefs.Save();
        }
        gameOver = true;
    }

    private void RestartRun()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void ContinueRun()
    {
        if (!canContinue)
        {
            return;
        }

        canContinue = false;
        lives = 1;
        distance = Mathf.Max(0f, safeDistance - 20f);
        player.position = new Vector3(player.position.x, 0f, distance);
        agentDistance = 8f;
        invulnerableTimer = 3f;
        gameOver = false;

        foreach (var item in worldObjects)
        {
            if (item.gameObject != null && item.gameObject.transform.position.z < safeDistance - 25f)
            {
                item.collected = true;
                item.gameObject.SetActive(false);
            }
        }
    }

    private void OnGUI()
    {
        float scale = Mathf.Min(Screen.width / 1080f, Screen.height / 1920f);
        scale = Mathf.Clamp(scale, 0.75f, 1.35f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        GUI.Label(new Rect(24, 24, 420, 44), "COINS  " + coins);
        GUI.Label(new Rect(24, 72, 420, 44), "LIVES  " + lives);
        GUI.Label(new Rect(24, 120, 420, 44), "WORD  " + (collectedLetters.Contains(0) ? "B" : "_") + " " + (collectedLetters.Contains(1) ? "R" : "_") + " " + (collectedLetters.Contains(2) ? "T" : "_"));
        GUI.Label(new Rect(24, 168, 420, 44), "DISTANCE  " + Mathf.FloorToInt(distance) + " m");
        GUI.Label(new Rect(24, 216, 420, 44), "BEST  " + bestDistance + " m");

        float buttonWidth = 170f;
        float buttonHeight = 96f;
        float bottom = 1920f - buttonHeight - 38f;
        if (GUI.RepeatButton(new Rect(20, bottom, buttonWidth, buttonHeight), "LEFT"))
        {
            touchLeft = true;
        }
        if (GUI.RepeatButton(new Rect(210, bottom, buttonWidth, buttonHeight), "RIGHT"))
        {
            touchRight = true;
        }
        if (GUI.Button(new Rect(720, bottom, buttonWidth, buttonHeight), "JUMP"))
        {
            touchJump = true;
        }
        if (GUI.Button(new Rect(890, bottom, buttonWidth, buttonHeight), "SLIDE"))
        {
            touchSlide = true;
        }

        if (gameOver)
        {
            GUI.Box(new Rect(Screen.width / scale * 0.5f - 260f, 650f, 520f, 520f), "");
            GUI.Label(new Rect(Screen.width / scale * 0.5f - 210f, 720f, 420f, 100f), "RUN OVER");
            if (canContinue && GUI.Button(new Rect(Screen.width / scale * 0.5f - 210f, 860f, 420f, 100f), "CONTINUE FROM SAFE POINT\n(rewarded ad placeholder)"))
            {
                ContinueRun();
            }
            if (GUI.Button(new Rect(Screen.width / scale * 0.5f - 210f, 1000f, 420f, 100f), "RESTART"))
            {
                RestartRun();
            }
        }
    }

    private void SpawnCoinLine(float z, int laneIndex, int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnCoin(new Vector3(laneIndex * LaneWidth, 1.1f, z + i * 1.5f));
        }
    }

    private void SpawnCoin(Vector3 position)
    {
        var visual = CreateCylinder("Coin", position, 0.32f, 0.08f, new Color(1f, 0.72f, 0.12f)).transform;
        visual.rotation = Quaternion.Euler(90f, 0f, 0f);
        worldObjects.Add(new WorldObject { gameObject = visual.gameObject, kind = WorldObjectKind.Coin, lane = LaneFromPosition(position.x) });
    }

    private void SpawnLetter(float z, int laneIndex, int letterIndex)
    {
        Color[] colors = { new Color(0.25f, 0.62f, 0.9f), new Color(0.84f, 0.28f, 0.28f), new Color(0.95f, 0.72f, 0.2f) };
        char[] letters = { 'B', 'R', 'T' };
        var letter = CreateBox("Letter " + letters[letterIndex], new Vector3(laneIndex * LaneWidth, 1.35f, z), new Vector3(0.8f, 0.8f, 0.35f), colors[letterIndex]).transform;
        worldObjects.Add(new WorldObject { gameObject = letter.gameObject, kind = WorldObjectKind.Letter, lane = laneIndex, letterIndex = letterIndex });
    }

    private void SpawnBarrier(float z, int laneIndex)
    {
        var barrier = CreateArtAsset("KenneyCityRoads/construction-barrier", "Barrier", new Vector3(laneIndex * LaneWidth, 0f, z), new Vector3(1.7f, 1.1f, 0.55f)).transform;
        worldObjects.Add(new WorldObject { gameObject = barrier.gameObject, kind = WorldObjectKind.Barrier, lane = laneIndex });
    }

    private void SpawnOverhead(float z, int laneIndex)
    {
        var overhead = CreateBox("Overhead Sign", new Vector3(laneIndex * LaneWidth, 1.65f, z), new Vector3(1.8f, 0.35f, 0.55f), new Color(0.25f, 0.28f, 0.82f)).transform;
        worldObjects.Add(new WorldObject { gameObject = overhead.gameObject, kind = WorldObjectKind.Overhead, lane = laneIndex });
    }

    private void SpawnVehicle(float z, int laneIndex)
    {
        var vehicle = CreateBox("Simple Vehicle", new Vector3(laneIndex * LaneWidth, 0.75f, z), new Vector3(1.8f, 1.5f, 3.2f), new Color(0.95f, 0.82f, 0.28f)).transform;
        CreateBox("Vehicle Window", new Vector3(laneIndex * LaneWidth, 1.25f, z - 0.3f), new Vector3(1.4f, 0.55f, 0.1f), new Color(0.18f, 0.32f, 0.42f));
        worldObjects.Add(new WorldObject { gameObject = vehicle.gameObject, kind = WorldObjectKind.Vehicle, lane = laneIndex });
    }

    private int LaneFromPosition(float x)
    {
        return Mathf.Clamp(Mathf.RoundToInt(x / LaneWidth), -1, 1);
    }

    private static GameObject CreateBox(string name, Vector3 position, Vector3 size, Color color)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(box.GetComponent<Collider>());
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().material = CreateMaterial(color);
        return box;
    }

    private static GameObject CreateSphere(string name, Vector3 position, float radius, Color color)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sphere.GetComponent<Collider>());
        sphere.name = name;
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * radius * 2f;
        sphere.GetComponent<Renderer>().material = CreateMaterial(color);
        return sphere;
    }

    private static GameObject CreateCylinder(string name, Vector3 position, float radius, float height, Color color)
    {
        var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(cylinder.GetComponent<Collider>());
        cylinder.name = name;
        cylinder.transform.position = position;
        cylinder.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);
        cylinder.GetComponent<Renderer>().material = CreateMaterial(color);
        return cylinder;
    }

    private GameObject CreateArtAsset(string assetName, string objectName, Vector3 position, Vector3 targetSize)
    {
        if (!artPrefabs.TryGetValue(assetName, out var prefab))
        {
            prefab = Resources.Load<GameObject>(assetName);
            if (prefab == null)
            {
                throw new UnityException("Missing Kenney asset: " + assetName);
            }
            artPrefabs.Add(assetName, prefab);
        }

        var instance = Instantiate(prefab, position, Quaternion.identity);
        instance.name = objectName;
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            throw new UnityException("Kenney asset has no renderer: " + assetName);
        }

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        if (bounds.size.x <= 0.0001f || bounds.size.z <= 0.0001f)
        {
            throw new UnityException("Kenney asset has invalid dimensions: " + assetName);
        }

        float yScale = bounds.size.y > 0.0001f ? targetSize.y / bounds.size.y : instance.transform.localScale.y;
        instance.transform.localScale = Vector3.Scale(
            instance.transform.localScale,
            new Vector3(targetSize.x / bounds.size.x, yScale, targetSize.z / bounds.size.z));

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        instance.transform.position += Vector3.up * (position.y - bounds.min.y);
        return instance;
    }

    private static Material CreateMaterial(Color color)
    {
        if (Materials.TryGetValue(color, out var cachedMaterial))
        {
            return cachedMaterial;
        }

        var shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        if (shader == null)
        {
            throw new UnityException("No supported shader is available for the runner prototype.");
        }
        var material = new Material(shader);
        material.color = color;
        Materials.Add(color, material);
        return material;
    }
}
