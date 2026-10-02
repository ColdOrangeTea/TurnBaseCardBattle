// 此工具由 A_Good_Ink 使用 AI 生成。
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 「LevelMap 新手教學對話」場景接線工具（由 A_Good_Ink 使用 AI 生成）。
///
/// 做什麼：在目前開啟的 LevelMap 場景（如 LevelMapSample）中
///   1. 建立/更新 <c>DialogueCanvas_LevelMap</c>（Screen Space Overlay，sortingOrder 250：蓋過戰鬥 100、低於暫停選單 300），
///      底下放一個 <c>DialogueEmpty</c> prefab 實例（劇情對話 UI，Spine 立繪版）；
///      設定 TriggerDialogue「開場收起、跳過鈕隨對話顯示」，並隱藏舊的作弊/測試按鈕。
///   2. 建立/更新 <c>LevelMapTutorialDirector</c> 物件，接好 TriggerDialogue/LevelMapManager/玩家，
///      並依 GUID 填入 Resources/Dialogue 下的 L1-Stage1-* 對話資料與對應的 Stage。
///
/// 使用方式：開啟 LevelMapSample 場景 → Unity 上方選單 Tools/Tutorial/接上 LevelMap 教學對話 (Setup Tutorial Dialogues)。
/// 可重複執行：依名稱找到既有物件就地更新（不重建、不換 GUID、引用不會斷）；對話資料以 GUID 載入，搬資料夾也不影響。
/// </summary>
public static class LevelMapTutorialSetupTool
{
    const string DialoguePrefabGuid = "81fd412e08540e54faa1585aa932e9f9"; // Prefabs/Dialogue/DialogueEmpty.prefab
    const string CanvasName = "DialogueCanvas_LevelMap";
    const string DirectorName = "LevelMapTutorialDirector";
    const int CanvasSortingOrder = 250;

    // 對話資料（Resources/Dialogue，GUID 載入）
    const string Dlg_Stage1_1 = "0c6fdc180d8480443932bbc5d1ee5bcc";   // L1-Stage1-1
    const string Dlg_Stage1_1_1 = "b58bb1b7700e03c48bf5fa84b4f38317"; // L1-Stage1-1-1
    const string Dlg_Stage1_2 = "03dbb53192c1fcd4cac84b8140cc7f38";   // L1-Stage1-2
    const string Dlg_TeachQuest = "1f3f1bb8eb038d04bb9828048b5f11b0"; // L1-Stage1-3_teachQuest
    const string Dlg_TeachEnemy4 = "63515052b328ead4e966a4d15e3a29cf"; // L1-Stage1-4_teachEnemy
    const string Dlg_TeachEnemy6 = "c0a12a65d70256b4998cc33edbf3ecd8"; // L1-Stage1-6_teachEnemy
    const string Dlg_TeachEnemy7 = "a052a12b4f5c73042a5084b33214ed4b"; // L1-Stage1-7_teachEnemy
    const string Dlg_TeachEnemy8 = "4624136912bab1645aa131fd1100f0ff"; // L1-Stage1-8_teachEnemy
    const string Dlg_Stage1_9 = "363673336b84b4e4d948ccba847853a3";   // L1-Stage1-9
    const string Dlg_Stage1_10 = "63cba5ac02330e24d8f33c3c7712a674";  // L1-Stage1-10

    const string Stage1Name = "L1-Stage1-1";
    const string Stage2Name = "L1-Stage1-2";

    [MenuItem("Tools/Tutorial/接上 LevelMap 教學對話 (Setup Tutorial Dialogues)")]
    public static void Setup()
    {
        bool ok = Build(out string report);
        EditorUtility.DisplayDialog(ok ? "教學對話接線完成" : "教學對話接線失敗", report, "OK");
    }

    /// <summary>對目前開啟的場景執行接線；回傳是否成功與報告（供 coplay/腳本直接呼叫，不跳對話框）。</summary>
    public static bool Build(out string report)
    {
        var sb = new StringBuilder();
        Scene scene = SceneManager.GetActiveScene();

        var levelMap = Object.FindAnyObjectByType<LevelMapManager>(FindObjectsInactive.Include);
        if (levelMap == null)
        {
            report = "目前場景找不到 LevelMapManager，請先開啟 LevelMap 場景（如 LevelMapSample）。";
            return false;
        }

        // ── 1. 對話 Canvas + DialogueEmpty ──
        GameObject canvasGO = FindRoot(scene, CanvasName);
        if (canvasGO == null)
        {
            canvasGO = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasGO, scene);
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create " + CanvasName);
            sb.AppendLine($"＋ 建立 {CanvasName}");
        }
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortingOrder;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var triggers = canvasGO.GetComponentsInChildren<TriggerDialogue>(true);
        TriggerDialogue trigger = triggers.Length > 0 ? triggers[0] : null;
        if (trigger == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(DialoguePrefabGuid));
            if (prefab == null)
            {
                report = sb + $"找不到對話 UI prefab（DialogueEmpty，GUID {DialoguePrefabGuid}）。";
                return false;
            }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasGO.transform);
            Undo.RegisterCreatedObjectUndo(inst, "Instantiate DialogueEmpty");
            var rt = (RectTransform)inst.transform;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            trigger = inst.GetComponentInChildren<TriggerDialogue>(true);
            sb.AppendLine("＋ 放入 DialogueEmpty 對話 UI");
        }
        if (trigger == null)
        {
            report = sb + "對話 UI 內找不到 TriggerDialogue。";
            return false;
        }

        var tso = new SerializedObject(trigger);
        SetBool(tso, "hideOnStart", true, sb);
        SetBool(tso, "toggleSkipButtonWithDialogue", true, sb);
        tso.ApplyModifiedProperties();

        // 舊的作弊/測試按鈕（綁已移除的 DialogueOpenClose）在遊戲中不需要
        foreach (var t in canvasGO.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "ShowCheatButton" || t.name == "CheatButton")
            {
                if (t.gameObject.activeSelf) { Undo.RecordObject(t.gameObject, "Hide cheat"); t.gameObject.SetActive(false); sb.AppendLine($"－ 隱藏 {t.name}"); }
            }
        }

        // ── 2. 教學導演 ──
        GameObject dirGO = FindRoot(scene, DirectorName);
        if (dirGO == null)
        {
            dirGO = new GameObject(DirectorName);
            SceneManager.MoveGameObjectToScene(dirGO, scene);
            Undo.RegisterCreatedObjectUndo(dirGO, "Create " + DirectorName);
            sb.AppendLine($"＋ 建立 {DirectorName}");
        }
        var director = dirGO.GetComponent<LevelMapTutorialDirector>();
        if (director == null) director = Undo.AddComponent<LevelMapTutorialDirector>(dirGO);

        var player = Object.FindAnyObjectByType<S001_PlayerController>(FindObjectsInactive.Include);
        var dso = new SerializedObject(director);
        dso.FindProperty("dialogue").objectReferenceValue = trigger;
        dso.FindProperty("levelMap").objectReferenceValue = levelMap;
        dso.FindProperty("player").objectReferenceValue = player;
        if (player == null) sb.AppendLine("⚠ 找不到 S001_PlayerController（對話期間無法暫扣地圖點擊）");

        // Stage 對話
        var stageList = dso.FindProperty("stageDialogues");
        AddStage(stageList, levelMap, Stage1Name, new[] { Dlg_Stage1_1, Dlg_Stage1_1_1 }, sb);
        AddStage(stageList, levelMap, Stage2Name, new[] { Dlg_Stage1_2 }, sb);

        SetList(dso, "firstQuestAccepted", sb, Dlg_TeachQuest);
        SetList(dso, "firstEnemyEncounter", sb, Dlg_TeachEnemy4);
        SetList(dso, "tutorialBattleStart", sb, Dlg_TeachEnemy6);
        SetList(dso, "tutorialFirstCardUsed", sb, Dlg_TeachEnemy7);
        SetList(dso, "tutorialFirstTurnEnd", sb, Dlg_TeachEnemy8);
        SetList(dso, "tutorialBattleWon", sb, Dlg_Stage1_9);
        SetList(dso, "firstTreasure", sb, Dlg_Stage1_10);
        dso.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        sb.AppendLine("✔ 完成。請存檔場景（Ctrl+S）。");
        report = sb.ToString();
        return true;
    }

    static GameObject FindRoot(Scene scene, string name)
    {
        foreach (var go in scene.GetRootGameObjects()) if (go.name == name) return go;
        return null;
    }

    static void SetBool(SerializedObject so, string prop, bool value, StringBuilder sb)
    {
        var p = so.FindProperty(prop);
        if (p == null) { sb.AppendLine($"⚠ TriggerDialogue 缺少欄位 {prop}"); return; }
        p.boolValue = value;
    }

    static DialogueData LoadDialogue(string guid, StringBuilder sb)
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var d = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (d == null) sb.AppendLine($"⚠ 找不到對話資料 GUID {guid}");
        return d;
    }

    // 填入一組 DialogueSequence 的段落（只改 dialogues；keepUIOpenBetween 保留編輯者的選擇，不覆蓋）
    static void SetList(SerializedObject so, string prop, StringBuilder sb, params string[] guids)
    {
        var seq = so.FindProperty(prop);
        if (seq == null) { sb.AppendLine($"⚠ 導演缺少欄位 {prop}"); return; }
        FillDialogues(seq.FindPropertyRelative("dialogues"), guids, sb);
        sb.AppendLine($"・{prop}：{seq.FindPropertyRelative("dialogues").arraySize} 段（不收起UI={seq.FindPropertyRelative("keepUIOpenBetween").boolValue}）");
    }

    static void FillDialogues(SerializedProperty list, string[] guids, StringBuilder sb)
    {
        list.ClearArray();
        foreach (var g in guids)
        {
            var d = LoadDialogue(g, sb);
            if (d == null) continue;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = d;
        }
    }

    static void AddStage(SerializedProperty stageList, LevelMapManager levelMap, string stageName, string[] guids, StringBuilder sb)
    {
        StageInfo stage = null;
        foreach (var s in levelMap.stages) if (s != null && s.name == stageName) { stage = s; break; }
        if (stage == null)
            foreach (var s in Object.FindObjectsByType<StageInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (s.name == stageName) { stage = s; break; }
        if (stage == null) { sb.AppendLine($"⚠ 場景中找不到 Stage「{stageName}」，略過其進場對話"); return; }

        // 已有同一 Stage 的條目就沿用（保留 keepUIOpenBetween 等編輯者設定），沒有才新增
        SerializedProperty e = null;
        for (int i = 0; i < stageList.arraySize; i++)
        {
            var cur = stageList.GetArrayElementAtIndex(i);
            if (cur.FindPropertyRelative("stage").objectReferenceValue == stage) { e = cur; break; }
        }
        if (e == null)
        {
            stageList.InsertArrayElementAtIndex(stageList.arraySize);
            e = stageList.GetArrayElementAtIndex(stageList.arraySize - 1);
            e.FindPropertyRelative("stage").objectReferenceValue = stage;
            e.FindPropertyRelative("sequence.keepUIOpenBetween").boolValue = false;
        }
        var dl = e.FindPropertyRelative("sequence.dialogues");
        FillDialogues(dl, guids, sb);
        sb.AppendLine($"・Stage「{stageName}」：{dl.arraySize} 段（不收起UI={e.FindPropertyRelative("sequence.keepUIOpenBetween").boolValue}）");
    }
}
