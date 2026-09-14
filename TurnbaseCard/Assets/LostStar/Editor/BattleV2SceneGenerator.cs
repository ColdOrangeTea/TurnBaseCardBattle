// 此工具由 A_Good_Ink 使用 AI 生成。
using System.Linq;
using System.Text;
using Assets.Scripts.GlobalEnums.BattleEnum;
using TMPro;
using TurnBaseBattleV2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 回合制卡牌戰鬥「V2 範例場景」生成器（由 A_Good_Ink 使用 AI 生成）。
///
/// 做什麼：組出一個用 V2 乾淨架構驅動的可跑戰鬥場景 —
///   - 相機、EventSystem、Canvas
///   - 重用專案內既有的 BattleEmpty Prefab（沿用它所有 UI 與已設定好的 S001/S002/S005/DicePoolManager 元件）
///   - 加上 V2 元件（BattleController / BattleView / BattleUnit x2 / BattleUnitView x2 / BattleSystemsV1Bridge / BattleV2Bootstrap）
///   - 用 SerializedObject 把所有引用「在編輯器一次接好」（執行期即為直接引用，不再有 GetChild/事件乒乓）
///   - 把「下一回合」按鈕 OnClick 綁到 BattleController.RequestNextTurn
///
/// 使用方式：Unity 上方選單 Tools/TurnBaseBattle/生成 V2 戰鬥範例場景 (Battle V2 Scene)。
///   生成後開啟 Assets/LostStar/Scenes/BattleV2Sample.unity 按 Play，會自動開一場 V2 測試戰鬥。
///
/// 可重複執行：會覆蓋更新同路徑的範例場景（GUID 不變、真正的 Prefab 不會被更動）。
///
/// 註：戰鬥組裝主體抽成 <see cref="BuildBattleV2"/>（internal），地圖範例生成器
///     （LevelMapSampleGenerator）會重用它，避免兩處重複維護 V2 接線。
/// </summary>
public static class BattleV2SceneGenerator
{
    // 不綁死路徑：以 GUID 解析既有場景路徑（搬動後仍存回同一個場景）；找不到才用預設位置
    const string SceneGuid = "9fa9f62aa6a94064292cbbfcf59a775c";
    const string DefaultScenePath = "Assets/LostStar/Scenes/BattleV2Sample.unity";
    static string ScenePath
    {
        get
        {
            string p = AssetDatabase.GUIDToAssetPath(SceneGuid);
            return string.IsNullOrEmpty(p) ? DefaultScenePath : p;
        }
    }

    // 不綁死路徑：以 GUID 解析 BattleResult 圖集（搬到 LostStar/ 後路徑會變）
    const string BattleResultGuid = "209f54f50137cfe4d982989a66ffafd8";

    [MenuItem("Tools/TurnBaseBattle/生成 V2 戰鬥範例場景 (Battle V2 Scene)")]
    public static void Generate()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        string report;
        bool ok = GenerateScene(out report);
        EditorUtility.DisplayDialog("V2 戰鬥範例場景",
            (ok ? $"已生成：\n{ScenePath}\n\n開啟後按 Play 開始 V2 測試戰鬥。\n\n接線報告：\n" : "生成失敗，詳見 Console。\n\n") + report,
            "好");
    }

    static bool GenerateScene(out string report)
    {
        var log = new StringBuilder();
        try
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 相機
            var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            camGO.transform.position = new Vector3(0, 0, -10);

            // EventSystem
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // Canvas
            var canvasGO = new GameObject("UICanva", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            BattleController controller;
            GameObject battleUI;
            var root = BuildBattleV2(canvasGO, canvas, log, out controller, out battleUI);
            if (root == null) { report = log.ToString(); return false; }

            EditorSceneManager.MarkSceneDirty(scene);
            string scenePath = ScenePath;
            EnsureFolder(System.IO.Path.GetDirectoryName(scenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report = log.ToString();
            Debug.Log($"[BattleV2SceneGenerator] 生成完成：{ScenePath}\n{report}");
            return true;
        }
        catch (System.Exception ex)
        {
            report = log.ToString() + "\n例外：" + ex.Message;
            Debug.LogError($"[BattleV2SceneGenerator] 生成失敗：{ex}");
            return false;
        }
    }

    /// <summary>
    /// 在指定 Canvas 底下組出完整的 V2 戰鬥（重用既有 BattleEmpty + 一次接好所有引用）。
    /// 回傳 BattleV2Root（失敗回 null），並以 out 提供 BattleController。可被其他生成器重用。
    /// </summary>
    internal static GameObject BuildBattleV2(GameObject canvasGO, Canvas canvas, StringBuilder log, out BattleController controller, out GameObject battleUI)
    {
        controller = null;
        battleUI = null;

        // 不綁死路徑：以 GUID/名稱定位 BattleEmpty，搬到哪都找得到
        var battleEmptyPrefab = BattleEmptyLocator.Load();
        if (battleEmptyPrefab == null)
        {
            log.AppendLine("✗ 找不到 BattleEmpty Prefab（GUID/名稱皆查無）。");
            Debug.LogError("[BattleV2SceneGenerator] 找不到 BattleEmpty Prefab。");
            return null;
        }

        // 重用 BattleEmpty
        var battleEmpty = (GameObject)PrefabUtility.InstantiatePrefab(battleEmptyPrefab);
        battleUI = battleEmpty;
        battleEmpty.transform.SetParent(canvasGO.transform, false);
        var beRt = battleEmpty.GetComponent<RectTransform>();
        if (beRt != null)
        {
            beRt.anchorMin = Vector2.zero; beRt.anchorMax = Vector2.one;
            beRt.offsetMin = Vector2.zero; beRt.offsetMax = Vector2.zero;
        }
        Transform be = battleEmpty.transform;

        // ── 從 BattleEmpty 既有固定結構取出需要的物件／元件 ──
        GameObject background = GO(C(be, 0, 0), "BackGround", log);
        GameObject groupCards = GO(C(be, 0, 3), "Group_Cards", log);
        GameObject player2Dices = GO(C(be, 0, 4, 1), "Player2Dices", log);
        GameObject player1Dices = GO(C(be, 0, 5, 1), "Player1Dices", log);
        GameObject turnCountText = GO(C(be, 0, 6, 1), "TurnCountText", log);
        GameObject whoseTurnText = GO(C(be, 0, 6, 2), "WhoseTurnText", log);
        GameObject settlementRoot = GO(C(be, 0, 7), "SettlementEmpty", log);
        GameObject settlementResult = GO(C(be, 0, 7, 2), "Text_Settlement", log);
        GameObject toNextTurnBtn = GO(C(be, 1, 0), "ToNextTurnButton", log);
        GameObject enemyActionPanel = GO(C(be, 2), "EnemyActionPanel", log);

        var diceSystem = Comp<S001_DiceSystem>(C(be, 3, 0), log);
        var dicePool = Comp<DicePoolManager>(C(be, 3, 0), log);
        var drawCard = Comp<S002_DrawCardsSystem>(C(be, 3, 1), log);
        var numCalc = Comp<S005_NumericalCalculation>(C(be, 3, 1), log);

        GameObject playerObj = GO(C(be, 4, 0), "UnitDatas/Player1", log);
        GameObject enemyObj = GO(C(be, 4, 1), "UnitDatas/Player2", log);
        var playerProfile = playerObj != null ? playerObj.GetComponent<BattleUnitProfile>() : null;
        var enemyProfile = enemyObj != null ? enemyObj.GetComponent<BattleUnitProfile>() : null;
        if (playerProfile == null) log.AppendLine("✗ Player1 上找不到 BattleUnitProfile");
        if (enemyProfile == null) log.AppendLine("✗ Player2 上找不到 BattleUnitProfile");

        // ── 在單位物件上加 V2 的 BattleUnit + BattleUnitView ──
        var playerUnit = GetOrAdd<BattleUnit>(playerObj);
        var enemyUnit = GetOrAdd<BattleUnit>(enemyObj);
        var playerView = GetOrAdd<BattleUnitView>(playerObj);
        var enemyView = GetOrAdd<BattleUnitView>(enemyObj);
        SetRef(playerView, "unit", playerUnit, log);
        SetRef(playerView, "profile", playerProfile, log);
        SetRef(enemyView, "unit", enemyUnit, log);
        SetRef(enemyView, "profile", enemyProfile, log);

        // ── V2 大腦根物件 ──
        var root = new GameObject("BattleV2Root");
        controller = root.AddComponent<BattleController>();
        var bridge = root.AddComponent<BattleSystemsV1Bridge>();
        var view = root.AddComponent<BattleView>();
        var bootstrap = root.AddComponent<BattleV2Bootstrap>();
        var screenShake = root.AddComponent<ScreenShake>();
        var popupSpawner = root.AddComponent<BattlePopupSpawner>();

        // 表現細節：震動 target = BattleEmpty；把 spawner/shake 接到兩個單位 View
        SetRef(screenShake, "target", battleEmpty.GetComponent<RectTransform>(), log);
        SetRef(playerView, "popupSpawner", popupSpawner, log);
        SetRef(playerView, "screenShake", screenShake, log);
        SetRef(enemyView, "popupSpawner", popupSpawner, log);
        SetRef(enemyView, "screenShake", screenShake, log);

        // BattleView 接線
        SetRef(view, "battleRoot", battleEmpty, log);
        SetRef(view, "playerView", playerView, log);
        SetRef(view, "enemyView", enemyView, log);
        SetRef(view, "background", Comp<Image>(background != null ? background.transform : null, log), log);
        SetRef(view, "turnCountText", Comp<TMP_Text>(turnCountText != null ? turnCountText.transform : null, log), log);
        SetRef(view, "whoseTurnText", Comp<TMP_Text>(whoseTurnText != null ? whoseTurnText.transform : null, log), log);
        SetRef(view, "toNextTurnButton", toNextTurnBtn, log);
        SetRef(view, "enemyActionPanel", enemyActionPanel, log);
        SetRef(view, "settlementRoot", settlementRoot, log);
        SetRef(view, "settlementResultText", Comp<TMP_Text>(settlementResult != null ? settlementResult.transform : null, log), log);

        // 結算三塊框圖
        SetRef(view, "settlementFrameUp", Comp<Image>(C(be, 0, 7, 1, 0), log), log);
        SetRef(view, "settlementInfoPanel", Comp<Image>(C(be, 0, 7, 1, 1), log), log);
        SetRef(view, "settlementFrameDown", Comp<Image>(C(be, 0, 7, 1, 2), log), log);

        // 從 BattleResult 圖集載入勝/敗 sprite；用 GUID 解析路徑，找不到再依名稱搜尋
        string battleResultPath = AssetDatabase.GUIDToAssetPath(BattleResultGuid);
        if (string.IsNullOrEmpty(battleResultPath))
        {
            var found = AssetDatabase.FindAssets("BattleResult t:Texture2D");
            if (found.Length > 0) battleResultPath = AssetDatabase.GUIDToAssetPath(found[0]);
        }
        var battleResult = string.IsNullOrEmpty(battleResultPath)
            ? new Sprite[0]
            : AssetDatabase.LoadAllAssetsAtPath(battleResultPath).OfType<Sprite>().ToArray();
        if (battleResult.Length == 0) log.AppendLine($"✗ 找不到 BattleResult sprite（GUID/名稱皆查無）");
        Sprite Spr(string n)
        {
            var s = battleResult.FirstOrDefault(x => x.name == n);
            if (s == null) log.AppendLine($"✗ BattleResult 內找不到 sprite：{n}");
            return s;
        }
        SetSpriteList(view, "victorySprites", new[] { Spr("LS2_VictoryBattle_1"), Spr("LS2_VictoryBattle_2"), Spr("LS2_VictoryBattle_3") }, log);
        SetSpriteList(view, "loseSprites", new[] { Spr("LS2_LossBattle_1"), Spr("LS2_LossBattle_2"), Spr("LS2_LossBattle_3") }, log);

        // 結算文字上色目標
        SetRef(view, "settlementPlayerNameText", Comp<TMP_Text>(C(be, 0, 7, 1, 1, 1), log), log);
        SetRef(view, "settlementLostConnectText", Comp<TMP_Text>(C(be, 0, 7, 1, 1, 3), log), log);
        SetRef(view, "settlementLootsScroll", GO(C(be, 0, 7, 1, 1, 2), "LootsScroll", log), log);

        SetColor(view, "victoryTextColor", new Color(0.3333f, 0.9961f, 1f), log);
        SetColor(view, "loseTextColor", new Color(0.851f, 0.149f, 0.149f), log);

        // BattleSystemsV1Bridge 接線
        SetRef(bridge, "dicePoolManager", dicePool, log);
        SetRef(bridge, "diceSystem", diceSystem, log);
        SetRef(bridge, "drawCardSystem", drawCard, log);
        SetRef(bridge, "numericalCalculation", numCalc, log);

        // BattleController 接線
        SetRef(controller, "view", view, log);
        SetRef(controller, "playerUnit", playerUnit, log);
        SetRef(controller, "enemyUnit", enemyUnit, log);
        SetRef(controller, "systems", bridge, log);

        // Bootstrap
        SetRef(bootstrap, "controller", controller, log);
        SetInt(bootstrap, "playerType", (int)CharacterType.Seraphis, log);
        SetInt(bootstrap, "enemyType", (int)EnemyType.Yarn, log);

        // 子系統的 V2 直接引用
        SetRef(diceSystem, "rootCanvasDirect", canvas, log);
        SetRef(dicePool, "Group_Cards", groupCards, log);
        SetRef(dicePool, "Player1_Group_Dices", player1Dices, log);
        SetRef(dicePool, "Player2_Group_Dices", player2Dices, log);
        SetRef(numCalc, "Group_Cards", groupCards, log);

        // 「下一回合」按鈕 OnClick → controller.RequestNextTurn（持久化綁定）
        var btn = toNextTurnBtn != null ? toNextTurnBtn.GetComponent<Button>() : null;
        if (btn != null)
        {
            for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, i);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, controller.RequestNextTurn);
            log.AppendLine("✓ ToNextTurn 按鈕 OnClick → RequestNextTurn（已清舊綁定）");
        }
        else log.AppendLine("✗ ToNextTurn 上找不到 Button，未綁定 OnClick");

        return root;
    }

    #region 輔助（internal 供其他生成器重用）
    /// <summary>依索引路徑逐層取子物件；任一層越界回傳 null。</summary>
    internal static Transform C(Transform t, params int[] path)
    {
        foreach (int i in path)
        {
            if (t == null || i < 0 || i >= t.childCount) return null;
            t = t.GetChild(i);
        }
        return t;
    }

    internal static GameObject GO(Transform t, string label, StringBuilder log)
    {
        if (t == null) { log.AppendLine($"✗ 找不到物件：{label}"); return null; }
        return t.gameObject;
    }

    internal static T Comp<T>(Transform t, StringBuilder log) where T : Component
    {
        if (t == null) return null;
        var c = t.GetComponent<T>();
        if (c == null) log.AppendLine($"✗ {t.name} 上找不到 {typeof(T).Name}");
        return c;
    }

    internal static T GetOrAdd<T>(GameObject go) where T : Component
    {
        if (go == null) return null;
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    internal static void SetRef(Object target, string field, Object value, StringBuilder log)
    {
        if (target == null) { log.AppendLine($"✗ 目標為 null，無法設定 {field}"); return; }
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) { log.AppendLine($"✗ {target.GetType().Name}.{field} 找不到序列化欄位"); return; }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        log.AppendLine(value != null ? $"✓ {target.GetType().Name}.{field}" : $"✗ {target.GetType().Name}.{field}（值為 null）");
    }

    internal static void SetInt(Object target, string field, int value, StringBuilder log)
    {
        if (target == null) { log.AppendLine($"✗ 目標為 null，無法設定 {field}"); return; }
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) { log.AppendLine($"✗ {target.GetType().Name}.{field} 找不到序列化欄位"); return; }
        p.intValue = value; // enum 以底層 int 儲存，直接設 intValue 即可
        so.ApplyModifiedPropertiesWithoutUndo();
        log.AppendLine($"✓ {target.GetType().Name}.{field} = {value}");
    }

    internal static void SetBool(Object target, string field, bool value, StringBuilder log)
    {
        if (target == null) { log.AppendLine($"✗ 目標為 null，無法設定 {field}"); return; }
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) { log.AppendLine($"✗ {target.GetType().Name}.{field} 找不到序列化欄位"); return; }
        p.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        log.AppendLine($"✓ {target.GetType().Name}.{field} = {value}");
    }

    internal static void SetColor(Object target, string field, Color value, StringBuilder log)
    {
        if (target == null) { log.AppendLine($"✗ 目標為 null，無法設定 {field}"); return; }
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) { log.AppendLine($"✗ {target.GetType().Name}.{field} 找不到序列化欄位"); return; }
        p.colorValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        log.AppendLine($"✓ {target.GetType().Name}.{field}");
    }

    internal static void SetSpriteList(Object target, string field, Sprite[] sprites, StringBuilder log)
    {
        if (target == null) { log.AppendLine($"✗ 目標為 null，無法設定 {field}"); return; }
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null || !p.isArray) { log.AppendLine($"✗ {target.GetType().Name}.{field} 不是可序列化的清單"); return; }
        p.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        int ok = sprites.Count(s => s != null);
        log.AppendLine($"{(ok == sprites.Length ? "✓" : "✗")} {target.GetType().Name}.{field}：{ok}/{sprites.Length} 張 sprite");
    }

    internal static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
    #endregion
}
