// 此工具由 A_Good_Ink 使用 AI 生成。
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 定位 BattleEmpty Prefab 的共用小工具（由 A_Good_Ink 使用 AI 生成）。
///
/// 不綁死路徑：BattleEmpty 在專案內被搬動也找得到。
///   1. 先用 GUID 直接解析目前路徑（GUID 比路徑穩定，搬移/改名都不變）。
///   2. GUID 失效時（例如 prefab 被重建換了 GUID），退回「依名稱搜尋 Prefab」。
///
/// 供 BattleV2SceneGenerator / BattleEmptyCleanup 等 Editor 工具共用。
/// </summary>
public static class BattleEmptyLocator
{
    /// <summary>BattleEmpty.prefab 的 GUID（穩定不隨搬移改變）。</summary>
    const string BattleEmptyGuid = "31c3e9dc312c3a9459b1396fed2e1b8a";
    /// <summary>後備用：Prefab 檔名（不含副檔名）。</summary>
    const string BattleEmptyName = "BattleEmpty";

    /// <summary>找出 BattleEmpty.prefab 目前的資產路徑；找不到回傳 null。</summary>
    public static string FindPath()
    {
        // 1) 用 GUID 解析（最可靠）
        string path = AssetDatabase.GUIDToAssetPath(BattleEmptyGuid);
        if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab"))
            return path;

        // 2) 後備：依名稱搜尋 Prefab，取檔名完全相符者
        foreach (string guid in AssetDatabase.FindAssets($"{BattleEmptyName} t:Prefab"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p) == BattleEmptyName)
                return p;
        }

        return null;
    }

    /// <summary>載入 BattleEmpty Prefab 資產；找不到回傳 null。</summary>
    public static GameObject Load()
    {
        string path = FindPath();
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
}
