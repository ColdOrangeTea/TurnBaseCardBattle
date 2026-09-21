using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地圖關卡管理器（深度重構版）。
///
/// 一個 LevelMap 由多個「Stage（小區域）」組成，這裡以 <see cref="LevelInfo"/> 代表一個 Stage：
/// 保存該區域的起點格、終點格(Door)、格清單、敵人與相機錨點。NodeManager 負責：
///   - 進入某個 Stage：把玩家放到起點、相機看向該 Stage、敵人放到生成點；
///   - 格子查詢：最近格、相鄰格 BFS 最短尋路、目前 Stage 的敵人清單、玩家/敵人是否同格；
///   - 走到終點(Door)時切換到下一個 Stage。
///
/// 舊版塞在這裡的劇情/對話耦合（Jephthah、聖女 Spine、引導對話、跳關文本、任務 hook）已全部移除，
/// 交由專屬系統處理。為沿用既有 prefab（NodeManager/Grid/LevelMap_Stage），保留序列化欄位（levels /
/// LevelInfo 及其欄位、currentLevelIndex、player）與對外方法簽章，故本檔 GUID 與 prefab 綁定不變。
/// </summary>
public class NodeManager : MonoBehaviour
{
    [Header("角色")]
    public Transform player;

    [Header("相機")]
    [Tooltip("留空則在 Start 自動抓 Camera.main 上的 CameraController。")]
    public CameraController cameraController;

    [Header("各 Stage（小區域）")]
    [Tooltip("依序排列的 Stage；currentLevelIndex 指向目前所在。")]
    public List<LevelInfo> levels = new List<LevelInfo>();

    [Tooltip("目前所在的 Stage 索引")]
    public int currentLevelIndex = 0;

    /// <summary>一個 Stage（小區域）的資料。欄位名沿用舊版以保留 prefab 序列化。</summary>
    [System.Serializable]
    public class LevelInfo
    {
        public Transform startGrid;                                     // 進入點
        public Transform endGrid;                                       // 終點(Door)：走到就切下一個 Stage
        public Transform cameraTarget;                                  // 相機錨點（留空則看 startGrid）
        public List<Transform> gridList = new List<Transform>();        // 此 Stage 的所有格
        public List<Transform> enemySpawnPoints = new List<Transform>(); // 敵人生成點
        public List<Transform> enemies = new List<Transform>();          // 此 Stage 的敵人
        public bool isEnemyClearedCheckEnabled = false;                 // 保留欄位：之後接關卡推進系統時使用

        public Transform CameraFocus => cameraTarget != null ? cameraTarget : startGrid;
    }

    // 閱讀用別名（不影響序列化）
    public LevelInfo CurrentStage =>
        (currentLevelIndex >= 0 && currentLevelIndex < levels.Count) ? levels[currentLevelIndex] : null;

    private void Start()
    {
        if (cameraController == null && Camera.main != null)
            cameraController = Camera.main.GetComponent<CameraController>();

        SetCurrentLevel(currentLevelIndex);
    }

    /// <summary>進入指定 Stage：放置玩家與敵人、相機對焦。</summary>
    public void SetCurrentLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Count)
        {
            BattleLog.Log("[NodeManager] 沒有更多 Stage。");
            return;
        }
        currentLevelIndex = levelIndex;
        LevelInfo stage = levels[levelIndex];

        // 玩家放到起點
        if (player != null && stage.startGrid != null)
            player.position = stage.startGrid.position;

        // 相機對焦此 Stage
        if (cameraController != null && stage.CameraFocus != null)
            cameraController.SetCameraTarget(stage.CameraFocus);

        // 敵人放到生成點
        int count = Mathf.Min(stage.enemySpawnPoints.Count, stage.enemies.Count);
        for (int i = 0; i < count; i++)
        {
            if (stage.enemies[i] != null && stage.enemySpawnPoints[i] != null)
                stage.enemies[i].position = stage.enemySpawnPoints[i].position;
        }

        BattleLog.Log($"[NodeManager] 進入 Stage {levelIndex}，格數 {stage.gridList.Count}");
    }

    /// <summary>切換到下一個 Stage（走到 Door 時呼叫）。</summary>
    public void MoveToNextLevel()
    {
        if (currentLevelIndex + 1 < levels.Count)
            SetCurrentLevel(currentLevelIndex + 1);
        else
            BattleLog.Log("[NodeManager] 已是最後一個 Stage，關卡完成。");
    }

    #region 敵人
    /// <summary>目前 Stage 的敵人清單。</summary>
    public List<Transform> GetEnemiesInCurrentLevel()
    {
        return CurrentStage != null ? CurrentStage.enemies : new List<Transform>();
    }

    /// <summary>清除目前 Stage 的所有敵人（戰鬥勝利後）。</summary>
    public void ClearEnemiesFromBattleField()
    {
        List<Transform> enemies = GetEnemiesInCurrentLevel();
        foreach (Transform enemy in enemies)
            if (enemy != null) Destroy(enemy.gameObject);
        enemies.Clear();
        BattleLog.Log("[NodeManager] 已清除目前 Stage 的敵人。");
    }
    #endregion

    #region 格子查詢 / 尋路
    /// <summary>目前 Stage 內離指定座標最近的格。</summary>
    public Transform GetGridAtPosition(Vector3 position)
    {
        LevelInfo stage = CurrentStage;
        if (stage == null) return null;

        Transform closest = null;
        float best = Mathf.Infinity;
        foreach (var grid in stage.gridList)
        {
            if (grid == null) continue;
            float d = (grid.position - position).sqrMagnitude;
            if (d < best) { best = d; closest = grid; }
        }
        return closest;
    }

    /// <summary>玩家/敵人是否在同一格。</summary>
    public bool IsPlayerAndEnemyOnSameGrid(Transform a, Transform b)
    {
        if (a == null || b == null) return false;
        return GetGridAtPosition(a.position) == GetGridAtPosition(b.position);
    }

    /// <summary>
    /// 以相鄰格（<see cref="GridData.connectedGrids"/>）做 BFS 的最短路徑，含起點與終點；
    /// 無路徑回傳空清單。（舊版為 DFS 遞迴，這裡改 BFS 以取得最短路並避免深遞迴。）
    /// </summary>
    public List<Transform> FindPath(Vector3 fromPosition, Transform target)
    {
        var path = new List<Transform>();
        Transform start = GetGridAtPosition(fromPosition);
        if (start == null || target == null) return path;
        if (CurrentStage == null || !CurrentStage.gridList.Contains(target)) return path;
        if (start == target) { path.Add(start); return path; }

        var came = new Dictionary<Transform, Transform> { { start, null } };
        var queue = new Queue<Transform>();
        queue.Enqueue(start);
        bool found = false;

        while (queue.Count > 0)
        {
            Transform cur = queue.Dequeue();
            if (cur == target) { found = true; break; }

            GridData data = cur.GetComponent<GridData>();
            if (data == null) continue;
            foreach (Transform n in data.connectedGrids)
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
