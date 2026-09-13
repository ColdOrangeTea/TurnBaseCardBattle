// 此工具由 A_Good_Ink 使用 AI 生成。
using UnityEditor;
using UnityEngine;

/// <summary>
/// 清除 BattleEmpty Prefab 上所有「Missing Script」空元件（由 A_Good_Ink 使用 AI 生成）。
///
/// 做什麼：V2 重構刪除舊腳本（TurnBaseBattleSetUp / BattleButtonFunction /
///   TurnBaseBattleUnitDisplayData 等）後，BattleEmpty 上原本掛這些腳本的子物件會殘留
///   空的 Missing Script 元件，導致 Inspector 警告、Prefab 難以乾淨儲存。本工具就地移除它們。
///
/// 使用方式：Unity 上方選單 Tools/TurnBaseBattle/清除 BattleEmpty Missing Script。
///
/// 可重複執行：以 PrefabUtility 就地載入 → 移除 → 存回同路徑（GUID 不變、引用不斷）；
///   已無 Missing Script 時再次執行只會回報移除 0 個。
/// </summary>
public static class BattleEmptyCleanup
{
    [MenuItem("Tools/TurnBaseBattle/清除 BattleEmpty Missing Script")]
    public static void CleanBattleEmpty()
    {
        // 不綁死路徑：以 GUID/名稱定位 BattleEmpty，搬到哪都找得到
        string battleEmptyPath = BattleEmptyLocator.FindPath();
        if (string.IsNullOrEmpty(battleEmptyPath))
        {
            EditorUtility.DisplayDialog("清除 Missing Script", "找不到 BattleEmpty Prefab（GUID/名稱皆查無）。", "好");
            return;
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(battleEmptyPath);
        if (prefabRoot == null)
        {
            EditorUtility.DisplayDialog("清除 Missing Script", $"無法載入 Prefab：\n{battleEmptyPath}", "好");
            return;
        }

        int removed = 0;
        int objectsAffected = 0;
        // 走訪所有子物件（含未啟用），移除各自的 Missing Script 元件
        foreach (Transform t in prefabRoot.GetComponentsInChildren<Transform>(true))
        {
            int n = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            if (n > 0)
            {
                removed += n;
                objectsAffected++;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, battleEmptyPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BattleEmptyCleanup] 已從 BattleEmpty 移除 {removed} 個 Missing Script（{objectsAffected} 個物件）。");
        EditorUtility.DisplayDialog("清除 Missing Script",
            $"已從 BattleEmpty 移除 {removed} 個 Missing Script 元件（{objectsAffected} 個物件）。\n\nPrefab 已就地存回，GUID 不變。", "好");
    }
}
