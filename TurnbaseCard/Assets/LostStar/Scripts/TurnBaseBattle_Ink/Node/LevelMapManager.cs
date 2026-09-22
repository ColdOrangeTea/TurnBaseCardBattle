using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地圖關卡管理器。一個 LevelMap（大關卡）由多個「Stage（小區域）」組成，每個 Stage 的資料改由掛在
/// LevelMap_Stage prefab 上的 <see cref="StageInfo"/> 自帶（取代舊的內嵌 LevelInfo）。
///
/// 職責：
///   - Start 時自動蒐集場上所有 <see cref="StageInfo"/>，並從 <see cref="startStage"/> 進入大關卡起點；
///   - 進入某 Stage：把玩家放到其 entryNode、相機對焦；
///   - 依「出口(Exit)」跳關：ToStage → 進目標 Stage；EndLevel → 結束大關卡並觸發 <see cref="LevelCompleted"/>；
///   - 格子查詢：最近格、相鄰格 BFS 最短尋路、目前 Stage 的敵人清單、玩家/敵人是否同格。
///
/// 連接（哪個出口去哪個 Stage）由各 Stage 的場景實例在 Inspector 上連，不寫死在此。
/// </summary>
public class LevelMapManager : MonoBehaviour
{
    [Header("角色")]
    public Transform player;

    [Header("相機")]
    [Tooltip("留空則在 Start 自動抓 Camera.main 上的 CameraController。")]
    public CameraController cameraController;

    [Header("關卡")]
    [Tooltip("大關卡的起始 Stage（玩家從這個 Stage 的 entryNode 開始）。")]
    public StageInfo startStage;

    /// <summary>Start 時自動蒐集到的場上所有 Stage。</summary>
    public IReadOnlyList<StageInfo> AllStages => allStages;
    private readonly List<StageInfo> allStages = new List<StageInfo>();

    /// <summary>目前所在的 Stage。</summary>
    public StageInfo CurrentStage { get; private set; }

    /// <summary>走到大關卡盡頭(EndLevel 出口)時觸發；結算流程可訂閱（實際結算畫面之後再接）。</summary>
    public event Action LevelCompleted;

    private void Start()
    {
        if (cameraController == null && Camera.main != null)
            cameraController = Camera.main.GetComponent<CameraController>();

        CollectStages();

        if (startStage != null) SetCurrentStage(startStage);
        else Debug.LogWarning("[LevelMapManager] 未指定 startStage，無法決定大關卡起點。請在 Inspector 指定起始 Stage。");
    }

    /// <summary>自動蒐集場上所有 StageInfo（含未啟用）。</summary>
    private void CollectStages()
    {
        allStages.Clear();
        foreach (var s in FindObjectsByType<StageInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            allStages.Add(s);
        BattleLog.Log($"[LevelMapManager] 蒐集到 {allStages.Count} 個 Stage。");
    }

    /// <summary>進入指定 Stage：放置玩家（entryOverride 或該 Stage 的 entryNode）、相機對焦。</summary>
    public void SetCurrentStage(StageInfo stage, NodeData entryOverride = null)
    {
        if (stage == null) { BattleLog.Log("[LevelMapManager] SetCurrentStage：stage 為空。"); return; }
        CurrentStage = stage;

        NodeData entry = entryOverride != null ? entryOverride : stage.entryNode;
        if (player != null && entry != null) player.position = entry.transform.position;

        if (cameraController != null && stage.CameraFocus != null)
            cameraController.SetCameraTarget(stage.CameraFocus);

        BattleLog.Log($"[LevelMapManager] 進入 Stage「{stage.name}」，節點數 {stage.Nodes.Count}");
    }

    /// <summary>
    /// 依出口跳關（走到出口節點時呼叫）：
    ///   - ToStage：進入目標 Stage（落點為出口的 targetEntryNode，留空則目標 entryNode）；
    ///   - EndLevel：結束大關卡並觸發 <see cref="LevelCompleted"/>。
    /// 回傳 true 代表大關卡已結束。
    /// </summary>
    public bool EnterStageThroughExit(StageInfo.Exit exit)
    {
        if (exit == null) return false;

        if (exit.kind == StageInfo.ExitKind.EndLevel)
        {
            BattleLog.Log("[LevelMapManager] 到達大關卡盡頭，結束並觸發結算事件。");
            LevelCompleted?.Invoke();
            return true;
        }

        if (exit.targetStage == null)
        {
            Debug.LogWarning("[LevelMapManager] 此出口為 ToStage 但未指定 targetStage，無法跳關。");
            return false;
        }

        SetCurrentStage(exit.targetStage, exit.targetEntryNode);
        return false;
    }

    /// <summary>找出目前 Stage 上「走到某節點」對應的出口；沒有回傳 null。</summary>
    public StageInfo.Exit FindExitAt(Transform node) =>
        CurrentStage != null ? CurrentStage.FindExitAt(node) : null;

    #region 敵人
    /// <summary>目前 Stage 的敵人清單。</summary>
    public List<Transform> GetEnemiesInCurrentLevel() =>
        CurrentStage != null ? CurrentStage.Enemies : new List<Transform>();

    /// <summary>清除目前 Stage 的所有敵人（戰鬥勝利後）。</summary>
    public void ClearEnemiesFromBattleField()
    {
        List<Transform> enemies = GetEnemiesInCurrentLevel();
        foreach (Transform enemy in enemies)
            if (enemy != null) Destroy(enemy.gameObject);
        enemies.Clear();
        BattleLog.Log("[LevelMapManager] 已清除目前 Stage 的敵人。");
    }
    #endregion

    #region 格子查詢 / 尋路
    /// <summary>目前 Stage 內離指定座標最近的格。</summary>
    public Transform GetGridAtPosition(Vector3 position)
    {
        StageInfo stage = CurrentStage;
        if (stage == null) return null;

        Transform closest = null;
        float best = Mathf.Infinity;
        foreach (var node in stage.Nodes)
        {
            if (node == null) continue;
            float d = (node.transform.position - position).sqrMagnitude;
            if (d < best) { best = d; closest = node.transform; }
        }
        return closest;
    }

    /// <summary>目前 Stage 是否包含此節點(Transform)。</summary>
    private bool CurrentStageHasNode(Transform node)
    {
        if (CurrentStage == null || node == null) return false;
        foreach (var n in CurrentStage.Nodes)
            if (n != null && n.transform == node) return true;
        return false;
    }

    /// <summary>玩家/敵人是否在同一格。</summary>
    public bool IsPlayerAndEnemyOnSameGrid(Transform a, Transform b)
    {
        if (a == null || b == null) return false;
        return GetGridAtPosition(a.position) == GetGridAtPosition(b.position);
    }

    /// <summary>以相鄰格（<see cref="NodeData.connectedNodes"/>）做 BFS 的最短路徑，含起點與終點；無路徑回傳空清單。</summary>
    public List<Transform> FindPath(Vector3 fromPosition, Transform target)
    {
        var path = new List<Transform>();
        Transform start = GetGridAtPosition(fromPosition);
        if (start == null || target == null) return path;
        if (!CurrentStageHasNode(target)) return path;
        if (start == target) { path.Add(start); return path; }

        var came = new Dictionary<Transform, Transform> { { start, null } };
        var queue = new Queue<Transform>();
        queue.Enqueue(start);
        bool found = false;

        while (queue.Count > 0)
        {
            Transform cur = queue.Dequeue();
            if (cur == target) { found = true; break; }

            NodeData data = cur.GetComponent<NodeData>();
            if (data == null) continue;
            foreach (Transform n in data.connectedNodes)
            {
                if (n != null && !came.ContainsKey(n))
                {
                    came[n] = cur;
                    queue.Enqueue(n);
                }
            }
        }

        if (!found) return path;

        for (Transform g = target; g != null; g = came[g]) path.Add(g);
        path.Reverse();
        return path;
    }
    #endregion
}
