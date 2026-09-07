using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Процедурная карта пустыни для Unity 6000 (URP).
// Использование:
//  1. Открой сцену Assets/Scenes/DesertMap.unity
//  2. Выбери объект DesertMapGenerator -> в инспекторе кнопка Generate (или ПКМ -> Generate Desert)
//  3. Карта строится из примитивов, внешних ассетов не нужно.
//  4. Seed меняет раскладку дюн/камней/кактусов.
[ExecuteInEditMode]
public class DesertMapGenerator : MonoBehaviour
{
    [Header("Общие")]
    public int seed = 1337;
    public Vector2 mapSize = new Vector2(200f, 200f);
    public bool autoGenerateOnStart = true;
    public bool clearBeforeGenerate = true;

    [Header("Плотность")]
    public int duneCount = 26;
    public int rockCount = 14;
    public int cactusCount = 16;
    public int palmCount = 7;

    const string RootName = "DesertMap_Root";

    void Start()
    {
        if (autoGenerateOnStart && Application.isPlaying)
            Generate();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Не генерируем на каждый чих, только если уже есть рут — чтобы не спамить
    }
#endif

    [ContextMenu("Generate Desert")]
    public void Generate()
    {
        var rng = new System.Random(seed);
        Transform root = GetOrCreateRoot();

        if (clearBeforeGenerate)
            ClearChildren(root);

        // Материалы
        var sandMat = MakeMat("Sand", new Color(0.91f, 0.76f, 0.52f));
        var sandDarkMat = MakeMat("SandDark", new Color(0.82f, 0.64f, 0.38f));
        var sandLightMat = MakeMat("SandLight", new Color(0.96f, 0.85f, 0.62f));
        var stoneMat = MakeMat("Stone", new Color(0.62f, 0.58f, 0.53f));
        var rockMat = MakeMat("Rock", new Color(0.45f, 0.36f, 0.28f));
        var woodMat = MakeMat("Wood", new Color(0.42f, 0.27f, 0.14f));
        var greenMat = MakeMat("PalmLeaf", new Color(0.25f, 0.55f, 0.25f));
        var trunkMat = MakeMat("Trunk", new Color(0.5f, 0.34f, 0.18f));
        var cactusMat = MakeMat("Cactus", new Color(0.24f, 0.55f, 0.28f));
        var waterMat = MakeMat("Water", new Color(0.15f, 0.6f, 0.75f), 0.2f, 0.8f);
        var tentMat = MakeMat("Tent", new Color(0.75f, 0.68f, 0.55f));
        var tentDarkMat = MakeMat("TentDark", new Color(0.55f, 0.3f, 0.2f));
        var fireMat = MakeEmissiveMat("Fire", new Color(1f, 0.45f, 0.1f));
        var sandstoneMat = MakeMat("Sandstone", new Color(0.85f, 0.69f, 0.42f));
        var roadMat = MakeMat("Road", new Color(0.72f, 0.55f, 0.33f));

        // ---------- Земля ----------
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground_Sand";
        ground.transform.SetParent(root, false);
        ground.transform.localScale = new Vector3(mapSize.x / 10f, 1f, mapSize.y / 10f);
        ground.GetComponent<Renderer>().sharedMaterial = sandMat;

        // Дорога каравана: 3 отрезка
        MakeRoad(root, roadMat, new Vector3(-60, 0.05f, -60), new Vector3(0, 0.05f, 15), 4f, "Road_West_Oasis");
        MakeRoad(root, roadMat, new Vector3(0, 0.05f, 15), new Vector3(45, 0.05f, 40), 4f, "Road_Oasis_Pyramid");
        MakeRoad(root, roadMat, new Vector3(0, 0.05f, 15), new Vector3(35, 0.05f, -25), 3f, "Road_Oasis_Ruins");

        // ---------- Дюны ----------
        for (int i = 0; i < duneCount; i++)
        {
            float x = Rand(rng, -mapSize.x / 2f + 10f, mapSize.x / 2f - 10f);
            float z = Rand(rng, -mapSize.y / 2f + 10f, mapSize.y / 2f - 10f);
            // Не засыпаем оазис и лагерь
            if (Vector2.Distance(new Vector2(x, z), new Vector2(0, 15)) < 18f) continue;
            if (Vector2.Distance(new Vector2(x, z), new Vector2(-20, -30)) < 14f) continue;

            var dune = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dune.name = $"Dune_{i:00}";
            dune.transform.SetParent(root, false);
            dune.transform.position = new Vector3(x, -0.6f, z);
            dune.transform.rotation = Quaternion.Euler(0, Rand(rng, 0, 360), 0);
            dune.transform.localScale = new Vector3(Rand(rng, 10f, 24f), Rand(rng, 1.2f, 3.2f), Rand(rng, 6f, 12f));
            var m = (i % 3 == 0) ? sandDarkMat : (i % 3 == 1) ? sandMat : sandLightMat;
            dune.GetComponent<Renderer>().sharedMaterial = m;
            dune.GetComponent<Collider>().enabled = false;
        }

        // ---------- Оазис ----------
        Vector3 oasisPos = new Vector3(0, 0, 15);
        var oasisBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        oasisBase.name = "Oasis_SandRing";
        oasisBase.transform.SetParent(root, false);
        oasisBase.transform.position = oasisPos;
        oasisBase.transform.localScale = new Vector3(11f, 0.3f, 11f);
        oasisBase.GetComponent<Renderer>().sharedMaterial = sandDarkMat;

        var grass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        grass.name = "Oasis_Grass";
        grass.transform.SetParent(root, false);
        grass.transform.position = oasisPos + new Vector3(0, 0.15f, 0);
        grass.transform.localScale = new Vector3(9f, 0.35f, 9f);
        grass.GetComponent<Renderer>().sharedMaterial = greenMat;

        var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        water.name = "Oasis_Water";
        water.transform.SetParent(root, false);
        water.transform.position = oasisPos + new Vector3(0, 0.35f, 0);
        water.transform.localScale = new Vector3(5.5f, 0.3f, 5.5f);
        water.GetComponent<Renderer>().sharedMaterial = waterMat;

        for (int i = 0; i < palmCount; i++)
        {
            float a = (360f / palmCount) * i + Rand(rng, -12, 12);
            float r = Rand(rng, 7.5f, 10.5f);
            Vector3 p = oasisPos + new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * r, 0, Mathf.Sin(a * Mathf.Deg2Rad) * r);
            MakePalm(root, p, trunkMat, greenMat, woodMat, rng, $"Palm_{i:00}");
        }

        // ---------- Пирамида ----------
        MakePyramid(root, new Vector3(-45, 0, 42), 22f, 14f, sandstoneMat, "Pyramid_SaharRa");

        // ---------- Руины ----------
        MakeRuins(root, new Vector3(35, 0, -25), stoneMat, sandstoneMat);

        // ---------- Скалы ----------
        for (int i = 0; i < rockCount; i++)
        {
            Vector3 p = new Vector3(Rand(rng, -90, 90), 0.4f, Rand(rng, -90, 90));
            if (Vector3.Distance(p, oasisPos) < 16f) continue;
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = $"Rock_{i:00}";
            rock.transform.SetParent(root, false);
            rock.transform.position = p;
            rock.transform.rotation = Quaternion.Euler(Rand(rng, 0, 30), Rand(rng, 0, 360), Rand(rng, 0, 30));
            rock.transform.localScale = new Vector3(Rand(rng, 1f, 4f), Rand(rng, 0.8f, 3f), Rand(rng, 1f, 4f));
            rock.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? rockMat : stoneMat;
        }

        // ---------- Кактусы ----------
        for (int i = 0; i < cactusCount; i++)
        {
            Vector3 p = new Vector3(Rand(rng, -85, 85), 0, Rand(rng, -85, 85));
            if (Vector3.Distance(p, oasisPos) < 15f) continue;
            MakeCactus(root, p, cactusMat, rng, $"Cactus_{i:00}");
        }

        // ---------- Лагерь ----------
        MakeCamp(root, new Vector3(-20, 0, -30), tentMat, tentDarkMat, woodMat, stoneMat, fireMat);

        // ---------- Кости левиафана (декор) ----------
        var bones = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bones.name = "Leviathan_Bones";
        bones.transform.SetParent(root, false);
        bones.transform.position = new Vector3(28, 0.5f, 30);
        bones.transform.localScale = new Vector3(6f, 1.2f, 2.5f);
        bones.GetComponent<Renderer>().sharedMaterial = sandLightMat;

        // ---------- Спавн игрока ----------
        var spawn = new GameObject("PlayerSpawn");
        spawn.transform.SetParent(root, false);
        spawn.transform.position = new Vector3(0, 2f, -38);

        // ---------- Атмосфера ----------
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.92f, 0.82f, 0.64f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 60f;
        RenderSettings.fogEndDistance = 220f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.0f;

        var sun =
#if UNITY_2023_1_OR_NEWER
            Object.FindFirstObjectByType<Light>();
#else
            FindObjectOfType<Light>();
#endif
        if (sun != null && sun.type == LightType.Directional)
        {
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.intensity = 2.2f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        Debug.Log($"[DesertMap] Сгенерировано: дюн={duneCount}, скал={rockCount}, кактусов={cactusCount}, пальм={palmCount}, seed={seed}");
    }

    [ContextMenu("Clear Desert")]
    public void Clear()
    {
        Transform root = transform.Find(RootName);
        if (root == null)
        {
            var old = GameObject.Find(RootName);
            if (old != null) root = old.transform;
        }
        if (root != null) ClearChildren(root);
    }

    // ---------- Хелперы ----------

    Transform GetOrCreateRoot()
    {
        var go = GameObject.Find(RootName);
        if (go == null) go = new GameObject(RootName);
        return go.transform;
    }

    void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var c = root.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(c);
            else Destroy(c);
#else
            Destroy(c);
#endif
        }
    }

    static float Rand(System.Random rng, float min, float max)
    {
        return (float)(min + rng.NextDouble() * (max - min));
    }

    Material MakeMat(string name, Color color, float metallic = 0f, float smoothness = 0.4f)
    {
        var mat = new Material(GetLitShader());
        mat.name = name;
        mat.color = color;
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        return mat;
    }

    Material MakeEmissiveMat(string name, Color color)
    {
        var mat = new Material(GetLitShader());
        mat.name = name;
        mat.color = color;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.5f);
        }
        return mat;
    }

    static Shader GetLitShader()
    {
        var s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }

    void MakeRoad(Transform root, Material mat, Vector3 a, Vector3 b, float width, string name)
    {
        var mid = (a + b) / 2f;
        float len = Vector3.Distance(a, b);
        var road = GameObject.CreatePrimitive(PrimitiveType.Plane);
        road.name = name;
        road.transform.SetParent(root, false);
        road.transform.position = mid;
        road.transform.localScale = new Vector3(width / 10f, 1f, len / 10f);
        road.transform.rotation = Quaternion.FromToRotation(Vector3.forward, (b - a).normalized) * Quaternion.Euler(0, 0, 0);
        // Плоскость ориентируем вдоль направления
        Vector3 dir = (b - a).normalized;
        float ang = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        road.transform.rotation = Quaternion.Euler(0, ang, 0);
        road.GetComponent<Renderer>().sharedMaterial = mat;
        road.GetComponent<Collider>().enabled = false;
    }

    void MakePalm(Transform root, Vector3 pos, Material trunk, Material leaf, Material nut, System.Random rng, string name)
    {
        var palm = new GameObject(name);
        palm.transform.SetParent(root, false);
        palm.transform.position = pos;

        var trunkObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunkObj.name = "Trunk";
        trunkObj.transform.SetParent(palm.transform, false);
        trunkObj.transform.localPosition = new Vector3(0, 3f, 0);
        trunkObj.transform.localScale = new Vector3(0.5f, 3f, 0.5f);
        trunkObj.transform.rotation = Quaternion.Euler(Rand(rng, -6, 6), 0, Rand(rng, -6, 6));
        trunkObj.GetComponent<Renderer>().sharedMaterial = trunk;

        for (int i = 0; i < 5; i++)
        {
            var leafObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leafObj.name = $"Leaf_{i}";
            leafObj.transform.SetParent(palm.transform, false);
            float a = (360f / 5) * i;
            leafObj.transform.localPosition = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 1.6f, 6f, Mathf.Sin(a * Mathf.Deg2Rad) * 1.6f);
            leafObj.transform.localScale = new Vector3(2.6f, 0.35f, 1.2f);
            leafObj.transform.rotation = Quaternion.Euler(0, -a, -12f);
            leafObj.GetComponent<Renderer>().sharedMaterial = leaf;
            leafObj.GetComponent<Collider>().enabled = false;
        }

        var nutObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nutObj.name = "Coconuts";
        nutObj.transform.SetParent(palm.transform, false);
        nutObj.transform.localPosition = new Vector3(0.3f, 5.6f, 0);
        nutObj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        nutObj.GetComponent<Renderer>().sharedMaterial = nut;
    }

    void MakeCactus(Transform root, Vector3 pos, Material mat, System.Random rng, string name)
    {
        var c = new GameObject(name);
        c.transform.SetParent(root, false);
        c.transform.position = pos;
        float h = Rand(rng, 2f, 4.5f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(c.transform, false);
        body.transform.localPosition = new Vector3(0, h / 2f, 0);
        body.transform.localScale = new Vector3(0.9f, h / 2f, 0.9f);
        body.GetComponent<Renderer>().sharedMaterial = mat;

        if (rng.NextDouble() > 0.4)
        {
            var arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            arm.name = "Arm";
            arm.transform.SetParent(c.transform, false);
            arm.transform.localPosition = new Vector3(0.8f, h * 0.6f, 0);
            arm.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
            arm.transform.rotation = Quaternion.Euler(0, 0, -70f);
            arm.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    void MakePyramid(Transform root, Vector3 pos, float size, float height, Material mat, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = pos;

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        go.AddComponent<BoxCollider>().size = new Vector3(size, height, size);

        Mesh m = new Mesh();
        m.name = "PyramidMesh";
        Vector3[] v = {
            new Vector3(-size/2, 0, -size/2), new Vector3(size/2, 0, -size/2),
            new Vector3(size/2, 0, size/2), new Vector3(-size/2, 0, size/2),
            new Vector3(0, height, 0)
        };
        m.vertices = v;
        m.triangles = new int[] {
            0,2,1, 0,3,2,       // основание
            0,1,4, 1,2,4, 2,3,4, 3,0,4  // грани
        };
        m.RecalculateNormals();
        mf.sharedMesh = m;
    }

    void MakeRuins(Transform root, Vector3 pos, Material stone, Material sandstone)
    {
        var ruins = new GameObject("Ruins_ZerTul");
        ruins.transform.SetParent(root, false);
        ruins.transform.position = pos;

        for (int i = 0; i < 4; i++)
        {
            var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            col.name = $"Column_{i}";
            col.transform.SetParent(ruins.transform, false);
            col.transform.localPosition = new Vector3((i % 2) * 6f - 3f, 2.5f, (i / 2) * 6f - 3f);
            col.transform.localScale = new Vector3(1.2f, 2.5f, 1.2f);
            col.GetComponent<Renderer>().sharedMaterial = stone;
            // Две колонны сломаны
            if (i >= 2) col.transform.localScale = new Vector3(1.2f, 1.1f, 1.2f);
        }
        var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lintel.name = "Lintel";
        lintel.transform.SetParent(ruins.transform, false);
        lintel.transform.localPosition = new Vector3(-3f, 5.4f, -3f);
        lintel.transform.localScale = new Vector3(1.2f, 0.8f, 7f);
        lintel.GetComponent<Renderer>().sharedMaterial = sandstone;

        var fallen = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fallen.name = "FallenColumn";
        fallen.transform.SetParent(ruins.transform, false);
        fallen.transform.localPosition = new Vector3(4f, 0.7f, 2f);
        fallen.transform.rotation = Quaternion.Euler(90f, 0, 25f);
        fallen.transform.localScale = new Vector3(1.2f, 2.5f, 1.2f);
        fallen.GetComponent<Renderer>().sharedMaterial = stone;
    }

    void MakeCamp(Transform root, Vector3 pos, Material tent, Material tentDark, Material wood, Material stone, Material fire)
    {
        var camp = new GameObject("Camp_West");
        camp.transform.SetParent(root, false);
        camp.transform.position = pos;

        for (int i = 0; i < 3; i++)
        {
            Vector3 tp = new Vector3((i - 1) * 7f, 0, (i % 2) * 4f - 2f);
            var tentObj = new GameObject($"Tent_{i}");
            tentObj.transform.SetParent(camp.transform, false);
            tentObj.transform.localPosition = tp;

            var baseCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseCube.transform.SetParent(tentObj.transform, false);
            baseCube.transform.localPosition = new Vector3(0, 1f, 0);
            baseCube.transform.localScale = new Vector3(3.5f, 2f, 3.5f);
            baseCube.GetComponent<Renderer>().sharedMaterial = (i == 1) ? tentDark : tent;

            var roof = new GameObject("Roof");
            roof.transform.SetParent(tentObj.transform, false);
            roof.transform.localPosition = new Vector3(0, 2.9f, 0);
            var rmf = roof.AddComponent<MeshFilter>();
            var rmr = roof.AddComponent<MeshRenderer>();
            rmr.sharedMaterial = (i == 1) ? tentDark : tent;
            Mesh m = new Mesh();
            m.vertices = new Vector3[] {
                new Vector3(-2f, 0, -2f), new Vector3(2f, 0, -2f),
                new Vector3(2f, 0, 2f), new Vector3(-2f, 0, 2f),
                new Vector3(0, 1.6f, 0)
            };
            m.triangles = new int[] { 0,2,1, 0,3,2, 0,1,4, 1,2,4, 2,3,4, 3,0,4 };
            m.RecalculateNormals();
            rmf.sharedMesh = m;
        }

        // Костер
        var firePit = new GameObject("Campfire");
        firePit.transform.SetParent(camp.transform, false);
        firePit.transform.localPosition = new Vector3(0, 0, 6f);

        for (int i = 0; i < 8; i++)
        {
            float a = (360f / 8) * i;
            var st = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            st.transform.SetParent(firePit.transform, false);
            st.transform.localPosition = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 1.2f, 0.2f, Mathf.Sin(a * Mathf.Deg2Rad) * 1.2f);
            st.transform.localScale = Vector3.one * 0.45f;
            st.GetComponent<Renderer>().sharedMaterial = stone;
        }
        for (int i = 0; i < 3; i++)
        {
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.transform.SetParent(firePit.transform, false);
            log.transform.localPosition = new Vector3(0, 0.3f, 0);
            log.transform.rotation = Quaternion.Euler(90f, i * 60f, 0);
            log.transform.localScale = new Vector3(0.25f, 0.8f, 0.25f);
            log.GetComponent<Renderer>().sharedMaterial = wood;
        }
        var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flame.name = "Flame";
        flame.transform.SetParent(firePit.transform, false);
        flame.transform.localPosition = new Vector3(0, 0.9f, 0);
        flame.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);
        flame.GetComponent<Renderer>().sharedMaterial = fire;
        flame.GetComponent<Collider>().enabled = false;

        var light = new GameObject("FireLight");
        light.transform.SetParent(firePit.transform, false);
        light.transform.localPosition = new Vector3(0, 1.5f, 0);
        var pl = light.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.color = new Color(1f, 0.6f, 0.25f);
        pl.intensity = 8f;
        pl.range = 18f;
    }
}
