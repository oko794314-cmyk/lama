using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Меню Lama: одной кнопкой ставит геймплей в сцену.
// Использование: открой DesertMap -> Lama -> Setup Game -> Play.
public static class LamaSetup
{
    [MenuItem("Lama/Setup Game (Player+Llama+Dungeon+Mountains)")]
    public static void Setup()
    {
        var go = GameObject.Find("Game");
        if (go == null) go = new GameObject("Game");
        if (go.GetComponent<GameBootstrap>() == null)
            go.AddComponent<GameBootstrap>();
        if (go.GetComponent<DungeonGenerator>() == null)
            go.AddComponent<DungeonGenerator>();
        if (go.GetComponent<MountainBiomeGenerator>() == null)
            go.AddComponent<MountainBiomeGenerator>();
        if (go.GetComponent<VegetationGenerator>() == null)
            go.AddComponent<VegetationGenerator>();

        // Сразу сгенерировать в Edit Mode для предпросмотра
        var d = go.GetComponent<DungeonGenerator>();
        var m = go.GetComponent<MountainBiomeGenerator>();
        var v = go.GetComponent<VegetationGenerator>();
        try
        {
            d.Generate();
            m.Generate();
            v.Generate();
        }
        catch (System.Exception e) { Debug.LogWarning("[LamaSetup] generate skipped: " + e.Message); }

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("[Lama] Game готов. Нажми Play. WASD+мышь, E — лама, ЛКМ — плевок, данж на востоке (60,-70), горы на севере.");
    }

    [MenuItem("Lama/Clear Generated (Dungeon/Mountain/Vegetation)")]
    public static void Clear()
    {
        foreach (var n in new[] { "Dungeon_Root", "Mountain_Root", "Vegetation_Root" })
        {
            var o = GameObject.Find(n);
            if (o != null) Object.DestroyImmediate(o);
        }
        Debug.Log("[Lama] Сгенерированное очищено.");
    }
}
