// 此工具由 A_Good_Ink 使用 AI 生成。
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 回合制卡牌戰鬥「範例場景」生成器（使用專案內真正的戰鬥 Prefab）。
///
/// 做什麼：組出一個能跑真正戰鬥 UI 的範例場景 —
///   - 相機、EventSystem
///   - 一個標記 UI_Canva 的 Canvas（代替專案內已遺失的 UICanva Prefab）
///   - 放入真正的 BattleEmpty Prefab 實例（標記 BattleEmpty）
///   - 放入真正的 TurnBaseBattleManager Prefab 實例，並掛 BattleExampleBootstrap 自動開戰
///
/// 使用方式：Unity 上方選單 Tools/TurnBaseBattle/生成戰鬥範例場景 (Battle Example Scene)。
///   生成後開啟 Assets/Scenes/BattleExampleSample.unity 按 Play，會自動開一場測試戰鬥，
///   「下一回合」等按鈕由 BattleEmpty Prefab 本身綁定，可正常運作。
///
/// 可重複執行：會覆蓋更新同路徑的範例場景（真正的 Prefab 不會被更動）。
/// </summary>
public static class BattleExampleSceneGenerator
{
    const string ScenePath = "Assets/Scenes/BattleExampleSample.unity";
    const string BattleEmptyPrefabPath = "Assets/Prefabs/TurnBaseCardBattle/old/BattleEmpty.prefab";
    const string ManagerPrefabPath = "Assets/Prefabs/TurnBaseCardBattle/old/TurnBaseBattleManager.prefab";

    [MenuItem("Tools/TurnBaseBattle/生成戰鬥範例場景 (Battle Example Scene)")]
    public static void Generate()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        if (GenerateExample())
            EditorUtility.DisplayDialog("戰鬥範例場景",
                $"已生成範例場景：\n{ScenePath}\n\n開啟後按 Play 即可看到自動開始的測試戰鬥（使用真正的 BattleEmpty / Manager Prefab）。", "好");
        else
            EditorUtility.DisplayDialog("戰鬥範例場景", "生成失敗，詳見 Console 錯誤訊息。", "好");
    }

    /// <summary>實際生成流程（不含對話框，供自動化／指令呼叫）。成功回傳 true。</summary>
    public static bool GenerateExample()
    {
        try
        {
            EnsureTag("UI_Canva");
            EnsureTag("BattleEmpty");
            EnsureTag("Player1");
            EnsureTag("Player2");
            EnsureTag("TurnBaseBattleManager");

            var battleEmptyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BattleEmptyPrefabPath);
            var managerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPrefabPath);
            if (battleEmptyPrefab == null || managerPrefab == null)
            {
                Debug.LogError($"[BattleExampleSceneGenerator] 找不到真正的 Prefab：\n{BattleEmptyPrefabPath}\n{ManagerPrefabPath}");
                return false;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 相機
            var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            camGO.transform.position = new Vector3(0, 0, -10);

            // EventSystem（UI 點擊/拖曳）
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // UI_Canva：代替已遺失的 UICanva Prefab，讓 TurnBaseBattleUI 以 Tag 找得到、不需實例化遺失的 Prefab
            var canvasGO = new GameObject("UICanva", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.tag = "UI_Canva";
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // 真正的 BattleEmpty（掛在 Canvas 下、標記 BattleEmpty，供 Manager 以 Tag 找到）
            var battleEmpty = (GameObject)PrefabUtility.InstantiatePrefab(battleEmptyPrefab);
            battleEmpty.transform.SetParent(canvasGO.transform, false);
            battleEmpty.tag = "BattleEmpty";
            var beRt = battleEmpty.GetComponent<RectTransform>();
            if (beRt != null)
            {
                beRt.anchorMin = Vector2.zero; beRt.anchorMax = Vector2.one;
                beRt.offsetMin = Vector2.zero; beRt.offsetMax = Vector2.zero;
            }

            // 真正的 TurnBaseBattleManager，並掛自動開戰的 Bootstrap
            var managerInstance = (GameObject)PrefabUtility.InstantiatePrefab(managerPrefab);
            managerInstance.name = "TurnBaseBattleManager";
            if (managerInstance.GetComponent<BattleExampleBootstrap>() == null)
                managerInstance.AddComponent<BattleExampleBootstrap>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            CleanupPlaceholderPrefabs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BattleExampleSceneGenerator] 生成完成（使用真正 Prefab）：{ScenePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleExampleSceneGenerator] 生成失敗：{ex}");
            return false;
        }
    }

    /// <summary>移除舊版佔位產生的 Prefab（改用真正的 Prefab 後不再需要）。只刪自己產生的、不動 old/。</summary>
    static void CleanupPlaceholderPrefabs()
    {
        string[] placeholders =
        {
            "Assets/Prefabs/TurnBaseCardBattle/Dice.prefab",
            "Assets/Prefabs/TurnBaseCardBattle/UICanva.prefab",
            "Assets/Prefabs/TurnBaseCardBattle/BattleEmpty.prefab",
            "Assets/Prefabs/TurnBaseCardBattle/Card_Attack.prefab",
            "Assets/Prefabs/TurnBaseCardBattle/Card_Heal.prefab",
            "Assets/Prefabs/TurnBaseCardBattle/Card_HeavyAttack.prefab",
            "Assets/Prefabs/TurnBaseCardBattle/Card_Redice.prefab",
            "Assets/Prefabs/TurnBaseCardBattle/Card_Reverse.prefab",
        };
        foreach (var p in placeholders)
            if (AssetDatabase.LoadAssetAtPath<GameObject>(p) != null)
                AssetDatabase.DeleteAsset(p);
    }

    static void EnsureTag(string tag)
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (asset == null || asset.Length == 0) return;
        var so = new SerializedObject(asset[0]);
        var tagsProp = so.FindProperty("tags");
        for (int i = 0; i < tagsProp.arraySize; i++)
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        so.ApplyModifiedProperties();
    }
}
