using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 一鍵組出「章節選擇 → 章節劇情節點圖 → 對話」的整合場景。
/// 把既有的三個 Canvas Prefab（ChapterCarouselCanvas / StoryNodeMapCanvas / DialogueCanvas）放進同一個場景，
/// 加上連接元件 (ChapterStoryMapBridge)，並自動接好所有引用：
/// （黑底不再由本工具處理：各 Canvas Prefab 自帶最底層的全螢幕純黑背景圖。）
///   ‧ 章節選擇畫面切成「交給外部接手」（選章不直接播對話）
///   ‧ 節點圖與對話系統接線（按節點「閱讀」時才播對話）
///   ‧ 連接元件依 ChapterListData 找該章節的 nodeGraph；章節沒指定時退回範例節點圖
///
/// 之所以需要本工具：兩個產生器各自建立「只含自己 Canvas」的範例場景，
/// 因此在章節選擇場景裡點章節不會進節點圖。用本工具產生的整合場景才有完整流程。
///
/// 使用方式：Unity 選單 → Tools → StoryNodeMap → Generate Chapter → Node Map Flow (整合場景)
/// 前置：先分別執行過
///   Tools → ChapterSelect → Generate Chapter Carousel UI
///   Tools → StoryNodeMap → Generate Story Node Map UI
/// 產出：Assets/Scenes/ChapterNodeMapFlowSample.unity
/// 可重複執行：會覆蓋更新既有場景。
///
/// 此工具是由 A_Good_Ink 使用 AI 生成的工具。
/// </summary>
public static class ChapterNodeMapFlowSetupTool
{
    private const string CarouselPrefabPath = "Assets/Prefabs/ChapterSelect/ChapterCarouselCanvas.prefab";
    private const string NodeMapPrefabPath = "Assets/Prefabs/StoryNodeMap/StoryNodeMapCanvas.prefab";
    private const string DialogueCanvasPrefabPath = "Assets/Prefabs/Dialogue/DialogueCanvas.prefab";
    private const string SampleGraphPath = "Assets/Data/StoryNodeMap/SampleStoryNodeGraph.asset";

    private const string SceneDir = "Assets/Scenes";
    private const string ScenePath = SceneDir + "/ChapterNodeMapFlowSample.unity";

    [MenuItem("Tools/StoryNodeMap/Generate Chapter → Node Map Flow (整合場景)")]
    public static void Generate()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var carouselPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarouselPrefabPath);
        var nodeMapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NodeMapPrefabPath);
        if (carouselPrefab == null || nodeMapPrefab == null)
        {
            EditorUtility.DisplayDialog("整合場景",
                "找不到必要的 Canvas Prefab：\n" +
                (carouselPrefab == null ? "‧ " + CarouselPrefabPath + "\n" : "") +
                (nodeMapPrefab == null ? "‧ " + NodeMapPrefabPath + "\n" : "") +
                "\n請先分別執行：\n" +
                "Tools → ChapterSelect → Generate Chapter Carousel UI\n" +
                "Tools → StoryNodeMap → Generate Story Node Map UI\n再重跑本工具。",
                "OK");
            return;
        }

        EnsureFolder(SceneDir);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 三個 Canvas
        var carouselGo = (GameObject)PrefabUtility.InstantiatePrefab(carouselPrefab);
        carouselGo.name = "ChapterCarouselCanvas";
        var nodeMapGo = (GameObject)PrefabUtility.InstantiatePrefab(nodeMapPrefab);
        nodeMapGo.name = "StoryNodeMapCanvas";
        GameObject dialogueGo = InstantiateDialogueCanvas();

        var carousel = carouselGo.GetComponent<ChapterCarouselController>();
        var nodeMap = nodeMapGo.GetComponent<StoryNodeMapController>();

        // 對話系統接線：節點圖按「閱讀」時要能播對話（Prefab 內是場景引用，實例化後為空，須在此接回）
        TriggerDialogue trigger = dialogueGo != null ? dialogueGo.GetComponent<TriggerDialogue>() : null;
        DialogueTypingEffect typer = dialogueGo != null ? dialogueGo.GetComponent<DialogueTypingEffect>() : null;

        BindDialogue(nodeMap, trigger, typer);
        BindDialogue(carousel, trigger, typer);

        // 章節選擇切成「交給外部接手」（連接元件執行期也會再設一次，這裡先寫進場景讓 Inspector 看得到）
        if (carousel != null)
        {
            var so = new SerializedObject(carousel);
            so.FindProperty("deferChapterToExternal").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 連接元件
        CreateBridge(carousel, nodeMap);

        new GameObject("EventSystem", typeof(InteractionOfUI), typeof(StandaloneInputModule));

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        bool anyChapterGraph = HasAnyChapterNodeGraph(carousel);
        EditorUtility.DisplayDialog("整合場景",
            "整合場景產生完成：\n" + ScenePath + "\n\n" +
            "流程：章節選擇 → 選章進入節點圖 → 點節點按「閱讀」播對話 → 返回。\n\n" +
            (anyChapterGraph
                ? "章節列表已有章節指定 nodeGraph，會開啟各自的節點圖。"
                : "提醒：章節列表 (ChapterListData) 目前沒有章節指定 nodeGraph，\n連接元件會先用範例節點圖 (SampleStoryNodeGraph) 當備援。\n實際內容請在各章節的 nodeGraph 欄位指定專屬節點圖。"),
            "OK");
    }

    private static void BindDialogue(ChapterCarouselController carousel, TriggerDialogue trigger, DialogueTypingEffect typer)
    {
        if (carousel == null) return;
        var so = new SerializedObject(carousel);
        so.FindProperty("dialogueTrigger").objectReferenceValue = trigger;
        so.FindProperty("dialogueTyper").objectReferenceValue = typer;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BindDialogue(StoryNodeMapController nodeMap, TriggerDialogue trigger, DialogueTypingEffect typer)
    {
        if (nodeMap == null) return;
        var so = new SerializedObject(nodeMap);
        so.FindProperty("dialogueTrigger").objectReferenceValue = trigger;
        so.FindProperty("dialogueTyper").objectReferenceValue = typer;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateBridge(ChapterCarouselController carousel, StoryNodeMapController nodeMap)
    {
        var go = new GameObject("ChapterStoryMapBridge", typeof(ChapterStoryMapBridge));
        var bridge = go.GetComponent<ChapterStoryMapBridge>();

        var sampleGraph = AssetDatabase.LoadAssetAtPath<StoryNodeGraphData>(SampleGraphPath);

        var so = new SerializedObject(bridge);
        so.FindProperty("carousel").objectReferenceValue = carousel;
        so.FindProperty("nodeMap").objectReferenceValue = nodeMap;
        so.FindProperty("fallbackGraph").objectReferenceValue = sampleGraph; // 章節沒指定 nodeGraph 時的備援，讓流程立即可跑
        so.FindProperty("forceDeferOnCarousel").boolValue = true;
        so.FindProperty("hideNodeMapOnStart").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject InstantiateDialogueCanvas()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueCanvasPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[ChapterNodeMapFlow] 找不到 " + DialogueCanvasPrefabPath +
                             "，整合場景已建立但節點圖按「閱讀」不會播對話。請先執行 Tools → Dialogue → Generate Dialogue UI。");
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "DialogueCanvas";

        var canvas = go.GetComponent<Canvas>();
        if (canvas != null) canvas.sortingOrder = 20;

        Transform openBtn = go.transform.Find("OpenButton");
        if (openBtn != null) openBtn.gameObject.SetActive(false);

        return go;
    }

    private static bool HasAnyChapterNodeGraph(ChapterCarouselController carousel)
    {
        ChapterListData list = carousel != null ? carousel.ChapterList : null;
        if (list == null) return false;
        for (int i = 0; i < list.ChapterCount; i++)
        {
            if (list.GetNodeGraph(i) != null) return true;
        }
        return false;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
