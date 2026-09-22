// 此工具由 A_Good_Ink 使用 AI 生成。
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 依 LevelMap_Stage 內「手排的 Wire（連線視覺）」自動建立各 Grid 的相鄰關係（NodeData.connectedNodes）。
///
/// 做什麼：讀取 LevelMap_Stage prefab 內所有 Grid（含 Start/End）與 Wire 子物件，
/// 對每條 Wire 找出「中點最接近該 Wire 視覺中心」的一對 Grid，視為一條相鄰邊，
/// 雙向寫入兩顆 Grid 的 connectedNodes。這樣玩家/敵人就能沿著你手排的 Wire 路徑用 BFS 移動。
///
/// 為什麼需要：Wire 只是視覺，NodeData.connectedNodes 原本是空的；本工具把「視覺連線」轉成「邏輯相鄰」。
///
/// 使用方式：Unity 上方選單 Tools/TurnBaseBattle/依 Wire 建立 Grid 相鄰 (Link Grids By Wires)。
/// 產出位置：就地寫回 LevelMap_Stage.prefab（GUID 不變、引用不會斷）。
/// 可重複執行：每次執行會先清空既有 connectedNodes 再依目前 Wire 重建，重排 Wire 後再跑一次即可。
/// </summary>
public static class GridWireLinker
{
    const string StageGuid = "e47404ff569b93949857eccad10ebab8";
    const string DefaultStagePath = "Assets/LostStar/Prefabs/TurnBaseCardBattle/LevelMap_Stage.prefab";

    static string StagePath
    {
        get
        {
            string p = AssetDatabase.GUIDToAssetPath(StageGuid);
            return string.IsNullOrEmpty(p) ? DefaultStagePath : p;
        }
    }

    [MenuItem("Tools/TurnBaseBattle/依 Wire 建立 Grid 相鄰 (Link Grids By Wires)")]
    public static void Menu()
    {
        string report;
        bool ok = Link(out report);
        EditorUtility.DisplayDialog("依 Wire 建立 Grid 相鄰",
            (ok ? $"已更新：\n{StagePath}\n\n" : "失敗，詳見 Console。\n\n") + report, "好");
    }

    /// <summary>就地把 Wire 連線轉成 connectedNodes 寫回 prefab。可被 coplay 直接呼叫（不跳 dialog）。</summary>
    public static bool Link(out string report)
    {
        var log = new StringBuilder();
        string path = StagePath;
        GameObject root = null;
        try
        {
            root = PrefabUtility.LoadPrefabContents(path);

            // 蒐集 Grid（含 Start/End）
            var grids = new List<Transform>();
            foreach (var gd in root.GetComponentsInChildren<NodeData>(true))
            {
                if (gd.connectedNodes == null) gd.connectedNodes = new List<Transform>();
                gd.connectedNodes.Clear(); // 冪等：先清空再重建
                grids.Add(gd.transform);
            }
            if (grids.Count < 2) { report = "Grid 數量不足 2，無法建立相鄰。"; return false; }

            // 蒐集 Wire（"Wire" 底下、帶 Renderer 的子物件）
            var wireRoot = root.transform.Find("Wire");
            if (wireRoot == null) { report = "找不到 Wire 容器物件。"; return false; }

            var wires = new List<Renderer>();
            foreach (Transform w in wireRoot)
            {
                var r = w.GetComponentInChildren<Renderer>();
                if (r != null) wires.Add(r);
            }
            if (wires.Count == 0) { report = "Wire 底下找不到任何有 Renderer 的連線物件。"; return false; }

            // 每條 Wire → 中點最接近其視覺中心的一對 Grid
            int edges = 0;
            foreach (var wire in wires)
            {
                Vector2 wc = wire.bounds.center; // 取 XY
                Transform bestA = null, bestB = null;
                float best = float.MaxValue;
                for (int i = 0; i < grids.Count; i++)
                    for (int j = i + 1; j < grids.Count; j++)
                    {
                        Vector2 mid = 0.5f * ((Vector2)grids[i].position + (Vector2)grids[j].position);
                        float d = (mid - wc).sqrMagnitude;
                        if (d < best) { best = d; bestA = grids[i]; bestB = grids[j]; }
                    }

                if (bestA != null && bestB != null)
                {
                    if (AddNeighbor(bestA, bestB) & AddNeighbor(bestB, bestA)) { }
                    edges++;
                    log.AppendLine($"{wire.transform.parent.name}/{wire.name} → {bestA.name} ↔ {bestB.name}");
                }
            }

            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);

            log.Insert(0, $"共 {grids.Count} 顆 Grid、{wires.Count} 條 Wire，建立 {edges} 條相鄰邊：\n");
            report = log.ToString();
            Debug.Log($"[GridWireLinker] 完成：{path}\n{report}");
            return true;
        }
        catch (System.Exception ex)
        {
            report = log.ToString() + "\n例外：" + ex.Message;
            Debug.LogError($"[GridWireLinker] 失敗：{ex}");
            return false;
        }
        finally
        {
            if (root != null) PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static bool AddNeighbor(Transform grid, Transform neighbor)
    {
        var data = grid.GetComponent<NodeData>();
        if (data == null) return false;
        if (data.connectedNodes == null) data.connectedNodes = new List<Transform>();
        if (grid != neighbor && !data.connectedNodes.Contains(neighbor))
        {
            data.connectedNodes.Add(neighbor);
            return true;
        }
        return false;
    }
}
