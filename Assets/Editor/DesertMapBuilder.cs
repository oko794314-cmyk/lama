using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public static class DesertMapBuilder
{
    const string ScenePath = "Assets/Scenes/DesertMap.unity";

    [MenuItem("Lama/Build Desert Map")]
    public static void BuildFromMenu()
    {
        Build();
    }

    public static void Build()
    {
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "DesertMap";

        System.Random rng = new System.Random(1337);

        // ---------- Materials ----------
        Material sandMat = GetOrCreateURPMat("Assets/Desert/Sand.mat", new Color(0.88f, 0.74f, 0.5f));
        Material rockMat = GetOrCreateURPMat("Assets/Desert/Rock.mat", new Color(0.45f, 0.36f, 0.29f));
        Material cactusMat = GetOrCreateURPMat("Assets/Desert/Cactus.mat", new Color(0.24f, 0.5f, 0.26f));
        Material trunkMat = GetOrCreateURPMat("Assets/Desert/Trunk.mat", new Color(0.42f, 0.29f, 0.17f));
        Material leafMat = GetOrCreateURPMat("Assets/Desert/Leaf.mat", new Color(0.3f, 0.55f, 0.25f));
        Material waterMat = GetOrCreateURPMat("Assets/Desert/Water.mat", new Color(0.2f, 0.55f, 0.75f));
        waterMat.SetFloat("_Smoothness", 0.9f);
        waterMat.SetFloat("_Metallic", 0.1f);
        Material pyramidMat = GetOrCreateURPMat("Assets/Desert/Pyramid.mat", new Color(0.82f, 0.68f, 0.45f));
        Material darkSandMat = GetOrCreateURPMat("Assets/Desert/DarkSand.mat", new Color(0.78f, 0.62f, 0.4f));

        // ---------- Terrain (dunes via heightmap) ----------
        TerrainData tData = new TerrainData();
        int res = 129;
        tData.heightmapResolution = res;
        tData.size = new Vector3(500, 28, 500);
        tData.baseMapResolution = 512;
        tData.SetDetailResolution(256, 8);

        float[,] heights = new float[res, res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = x / (float)res * 8f;
                float ny = y / (float)res * 8f;
                // Dunes: layered perlin + ridge
                float h = Mathf.PerlinNoise(nx, ny) * 0.55f;
                h += Mathf.PerlinNoise(nx * 2.7f + 10f, ny * 0.6f) * 0.3f; // wind ridges
                h += Mathf.Sin((x + y) * 0.08f) * 0.05f;
                // Flatten center for oasis
                float dx = (x - res / 2f) / res;
                float dy = (y - res / 2f - 0.12f * res) / res;
                float distCenter = Mathf.Sqrt(dx * dx + dy * dy);
                float flat = Mathf.Clamp01(distCenter / 0.12f);
                h *= Mathf.Lerp(0.05f, 1f, flat);
                heights[y, x] = h * 0.35f;
            }
        }
        tData.SetHeights(0, 0, heights);

        // Terrain layer - sand color
        TerrainLayer sandLayer = new TerrainLayer();
        sandLayer.diffuseTexture = MakeSandTexture();
        sandLayer.tileSize = new Vector2(40, 40);
        string layerPath = "Assets/Desert/SandLayer.terrainlayer";
        AssetDatabase.CreateAsset(sandLayer, layerPath);
        tData.terrainLayers = new TerrainLayer[] { sandLayer };

        GameObject terrainGO = Terrain.CreateTerrainGameObject(tData);
        terrainGO.name = "DesertTerrain";
        terrainGO.transform.position = new Vector3(-250, 0, -250);

        // Terrain material URP
        var terrain = terrainGO.GetComponent<Terrain>();
        terrain.materialTemplate = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
        terrainGO.GetComponent<TerrainCollider>().terrainData = tData;

        // Save TerrainData asset
        AssetDatabase.CreateAsset(tData, "Assets/Desert/DesertTerrainData.asset");

        // ---------- Lighting / sky / fog ----------
        GameObject lightGO = new GameObject("Sun");
        var sun = lightGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.94f, 0.82f);
        sun.intensity = 2.2f;
        sun.shadows = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(48, -30, 0);

        RenderSettings.sun = sun;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.9f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.92f, 0.82f, 0.65f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 90;
        RenderSettings.fogEndDistance = 480;

        // Simple gradient sky via procedural material
        Material sky = new Material(Shader.Find("Skybox/Procedural"));
        if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.5f, 0.7f, 0.95f));
        if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.85f, 0.7f, 0.5f));
        if (sky.HasProperty("_SunDiskSize")) sky.SetFloat("_SunDiskSize", 0.04f);
        if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.1f);
        AssetDatabase.CreateAsset(sky, "Assets/Desert/DesertSky.mat");
        RenderSettings.skybox = sky;

        // ---------- Camera ----------
        GameObject camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        camGO.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.farClipPlane = 2000;
        camGO.transform.position = new Vector3(0, 22, -70);
        camGO.transform.rotation = Quaternion.Euler(18, 0, 0);
        camGO.AddComponent<AudioListener>();

        // ---------- Scatter props ----------
        GameObject props = new GameObject("Props");

        // Rocks ~70
        for (int i = 0; i < 70; i++)
        {
            float px = (float)(rng.NextDouble() * 440 - 220);
            float pz = (float)(rng.NextDouble() * 440 - 220);
            if (Mathf.Abs(px) < 25 && Mathf.Abs(pz - 60) < 25) continue; // keep oasis clear
            float y = terrainGO.GetComponent<Terrain>().SampleHeight(new Vector3(px + 250, 0, pz + 250));
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Rock_" + i;
            rock.transform.parent = props.transform;
            rock.transform.position = new Vector3(px, y + 0.2f, pz);
            rock.transform.localScale = new Vector3(
                0.6f + (float)rng.NextDouble() * 2.4f,
                0.5f + (float)rng.NextDouble() * 1.4f,
                0.6f + (float)rng.NextDouble() * 2.4f);
            rock.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0);
            rock.GetComponent<Renderer>().sharedMaterial = (i % 5 == 0) ? darkSandMat : rockMat;
        }

        // Cacti ~28
        for (int i = 0; i < 28; i++)
        {
            float px = (float)(rng.NextDouble() * 400 - 200);
            float pz = (float)(rng.NextDouble() * 400 - 200);
            if (Mathf.Abs(px) < 30 && Mathf.Abs(pz - 60) < 30) continue;
            float y = terrain.SampleHeight(new Vector3(px + 250, 0, pz + 250));
            GameObject cactus = BuildCactus(cactusMat, 2f + (float)rng.NextDouble() * 2f);
            cactus.name = "Cactus_" + i;
            cactus.transform.parent = props.transform;
            cactus.transform.position = new Vector3(px, y, pz);
            cactus.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0);
        }

        // Dry bushes (tumbleweed-ish) ~40
        for (int i = 0; i < 40; i++)
        {
            float px = (float)(rng.NextDouble() * 440 - 220);
            float pz = (float)(rng.NextDouble() * 440 - 220);
            float y = terrain.SampleHeight(new Vector3(px + 250, 0, pz + 250));
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "Bush_" + i;
            bush.transform.parent = props.transform;
            bush.transform.position = new Vector3(px, y + 0.4f, pz);
            bush.transform.localScale = Vector3.one * (0.5f + (float)rng.NextDouble() * 0.9f);
            var r = bush.GetComponent<Renderer>();
            r.sharedMaterial = trunkMat;
        }

        // Pyramids - 2 landmarks far away
        BuildPyramid(pyramidMat, new Vector3(-140, 0, 160), 46, terrain);
        BuildPyramid(pyramidMat, new Vector3(-190, 0, 200), 30, terrain);

        // Oasis
        BuildOasis(waterMat, sandMat, trunkMat, leafMat, terrain, new Vector3(0, 0, 60));

        // Spawn point
        GameObject spawn = new GameObject("PlayerSpawn");
        float sy = terrain.SampleHeight(new Vector3(250, 0, 180)) + 2f;
        spawn.transform.position = new Vector3(0, sy, -40);
        spawn.transform.rotation = Quaternion.identity;

        // Wind / dust particles (simple)
        GameObject dustGO = new GameObject("Dust");
        var ps = dustGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new Color(0.9f, 0.8f, 0.6f, 0.5f);
        main.startSize = 1.5f;
        main.startLifetime = 6f;
        main.maxParticles = 300;
        var emission = ps.emission;
        emission.rateOverTime = 25;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(200, 8, 200);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = 4;
        dustGO.transform.position = new Vector3(0, 4, 0);

        // Save scene
        EditorSceneManager.MarkAllScenesDirty();
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("DesertMap saved=" + saved + " path=" + ScenePath);

        // Add to build settings
        var buildScenes = EditorBuildSettings.scenes;
        bool hasDesert = false, hasSample = false;
        foreach (var s in buildScenes)
        {
            if (s.path == ScenePath) hasDesert = true;
            if (s.path == "Assets/Scenes/SampleScene.unity") hasSample = true;
        }
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(buildScenes);
        if (!hasDesert) list.Add(new EditorBuildSettingsScene(ScenePath, true));
        if (!hasSample) list.Add(new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true));
        EditorBuildSettings.scenes = list.ToArray();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static Material GetOrCreateURPMat(string path, Color c)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            m = new Material(s);
            m.color = c;
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(m, path);
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        m.color = c;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D MakeSandTexture()
    {
        int s = 256;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.12f;
                Color c = new Color(0.92f + n, 0.80f + n, 0.60f + n * 0.8f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes("Assets/Desert/sand.png", png);
        AssetDatabase.Refresh();
        Texture2D imported = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Desert/sand.png");
        return imported;
    }

    static GameObject BuildCactus(Material mat, float h)
    {
        GameObject root = new GameObject("Cactus");
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        trunk.transform.parent = root.transform;
        trunk.transform.localPosition = new Vector3(0, h / 2f, 0);
        trunk.transform.localScale = new Vector3(0.8f, h / 2f, 0.8f);
        trunk.GetComponent<Renderer>().sharedMaterial = mat;
        // arms
        for (int a = -1; a <= 1; a += 2)
        {
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            arm.transform.parent = root.transform;
            arm.transform.localPosition = new Vector3(a * 0.7f, h * 0.6f, 0);
            arm.transform.localScale = new Vector3(0.5f, h * 0.22f, 0.5f);
            arm.transform.localRotation = Quaternion.Euler(0, 0, a * 35f);
            arm.GetComponent<Renderer>().sharedMaterial = mat;
        }
        return root;
    }

    static void BuildPyramid(Material mat, Vector3 pos, float size, Terrain terrain)
    {
        // 4-sided cone = pyramid
        GameObject p = new GameObject("Pyramid_" + size);
        var filter = p.AddComponent<MeshFilter>();
        var renderer = p.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
        Mesh mesh = new Mesh();
        float h = size * 0.65f;
        float hs = size / 2f;
        Vector3[] v = new Vector3[]
        {
            new Vector3(-hs,0,-hs), new Vector3(hs,0,-hs),
            new Vector3(hs,0,hs), new Vector3(-hs,0,hs),
            new Vector3(0,h,0)
        };
        int[] t = new int[] { 0,1,4, 1,2,4, 2,3,4, 3,0,4, 0,2,1, 0,3,2 };
        mesh.vertices = v;
        mesh.triangles = t;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        filter.mesh = mesh;
        p.AddComponent<MeshCollider>();
        float y = terrain.SampleHeight(new Vector3(pos.x + 250, 0, pos.z + 250));
        p.transform.position = new Vector3(pos.x, y - 1f, pos.z);
        p.transform.rotation = Quaternion.Euler(0, 45, 0);
    }

    static void BuildOasis(Material waterMat, Material sandMat, Material trunkMat, Material leafMat, Terrain terrain, Vector3 center)
    {
        float gy = terrain.SampleHeight(new Vector3(center.x + 250, 0, center.z + 250));
        GameObject oasis = new GameObject("Oasis");

        // water disc
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        water.name = "OasisWater";
        water.transform.parent = oasis.transform;
        water.transform.position = new Vector3(center.x, gy + 0.35f, center.z);
        water.transform.localScale = new Vector3(16, 0.25f, 16);
        water.GetComponent<Renderer>().sharedMaterial = waterMat;

        // sand ring
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "OasisRing";
        ring.transform.parent = oasis.transform;
        ring.transform.position = new Vector3(center.x, gy + 0.2f, center.z);
        ring.transform.localScale = new Vector3(19, 0.4f, 19);
        ring.GetComponent<Renderer>().sharedMaterial = sandMat;

        // palms x5 around
        for (int i = 0; i < 5; i++)
        {
            float ang = i / 5f * Mathf.PI * 2f;
            float px = center.x + Mathf.Cos(ang) * 13f;
            float pz = center.z + Mathf.Sin(ang) * 13f;
            float py = terrain.SampleHeight(new Vector3(px + 250, 0, pz + 250));
            GameObject palm = BuildPalm(trunkMat, leafMat);
            palm.transform.position = new Vector3(px, py, pz);
            palm.transform.rotation = Quaternion.Euler(0, ang * Mathf.Rad2Deg, 0);
            palm.transform.parent = oasis.transform;
        }
    }

    static GameObject BuildPalm(Material trunkMat, Material leafMat)
    {
        GameObject palm = new GameObject("Palm");
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.parent = palm.transform;
        trunk.transform.localPosition = new Vector3(0, 2.5f, 0);
        trunk.transform.localScale = new Vector3(0.5f, 2.5f, 0.5f);
        trunk.transform.localRotation = Quaternion.Euler(0, 0, 8f);
        trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
        for (int i = 0; i < 6; i++)
        {
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.transform.parent = palm.transform;
            float ang = i / 6f * 360f;
            leaf.transform.localPosition = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * 1.6f, 5.1f, Mathf.Sin(ang * Mathf.Deg2Rad) * 1.6f);
            leaf.transform.localScale = new Vector3(2.8f, 0.12f, 0.7f);
            leaf.transform.localRotation = Quaternion.Euler(-18, -ang, 0);
            leaf.GetComponent<Renderer>().sharedMaterial = leafMat;
        }
        return palm;
    }
}
