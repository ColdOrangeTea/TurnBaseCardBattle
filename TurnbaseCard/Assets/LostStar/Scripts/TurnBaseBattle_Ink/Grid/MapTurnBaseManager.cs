using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using TurnBaseBattleV2;

/// <summary>
/// 地圖回合管理器（深度重構版）。只負責「回合流程」與「敵人回合的接近移動」：
///   - 廣播/接收回合切換（<see cref="MapTurnBaseEvent"/>）；
///   - 敵人回合時，每個敵人沿相鄰格 BFS 往玩家走一步；走到與玩家同格則進入 V2 戰鬥。
///
/// 舊版混在這裡的碰撞位移(MovePlayer/objectsToDetect)已移除——那是玩家表現，屬 S001_PlayerController；
/// 每幀 log 洗屏、以及「玩家在起點格就強制切玩家回合」的怪異特例也一併清掉。
/// 保留 prefab 綁定所需的公開欄位（gridManager/playerController）與 GUID 不變。
/// </summary>
public class MapTurnBaseManager : MonoBehaviour
{
    [Header("引用")]
    public GridManager gridManager;
    public S001_PlayerController playerController;

    [Header("敵人移動")]
    [Tooltip("進入戰鬥前的緩衝秒數")]
    public float preBattleDelay = 0.1f;
    [Tooltip("敵人全部走完後、切回玩家回合前的緩衝秒數")]
    public float endTurnDelay = 1f;

    [Header("戰鬥返回")]
    [Tooltip("戰鬥結束後、收起戰鬥畫面回到地圖前的緩衝秒數（讓玩家看結算）")]
    public float battleReturnDelay = 3.5f;

    private MapTurnBaseType currentTurn;
    private float enemyMoveSpeed = 0f; // 由玩家回合傳入，敵人沿用相同速度
    private bool subscribedBattle;

    void OnEnable()  => MapTurnBaseEvent.OnTurnChanged += OnTurnChanged;
    void OnDisable() => MapTurnBaseEvent.OnTurnChanged -= OnTurnChanged;

    void Start()
    {
        currentTurn = MapTurnBaseType.PlayerTurn;
        new MapTurnBaseEvent().SendManagerTurn(currentTurn); // 通知其他控制器目前是玩家回合

        // 訂閱 V2 戰鬥結束通知，戰鬥收尾後回到地圖（Instance 於 BattleController.Awake 設定，早於此 Start）
        if (BattleController.Instance != null)
        {
            BattleController.Instance.BattleFinished += OnBattleFinished;
            subscribedBattle = true;
        }
        else
        {
            Debug.LogWarning("[MapTurnBaseManager] 場上找不到 BattleController，無法接管戰鬥結束返回地圖。");
        }
    }

    void OnDestroy()
    {
        if (subscribedBattle && BattleController.Instance != null)
            BattleController.Instance.BattleFinished -= OnBattleFinished;
    }

    // 對外保留：讓外部設定回合/速度（API 穩定）
    public void GetSpeedFromPlayer(float speed) => enemyMoveSpeed = speed;
    public void SetTurnType(MapTurnBaseType nextType) => currentTurn = nextType;

    // 回合切換：玩家回合結束 → 若有敵人則跑敵人回合，否則直接回玩家回合
    private void OnTurnChanged(MapTurnBaseType nextTurn, float speed)
    {
        SetTurnType(nextTurn);
        GetSpeedFromPlayer(speed);

        if (currentTurn == MapTurnBaseType.EnemyTurn && HasEnemies())
        {
            StartCoroutine(EnemyTurn());
        }
        else
        {
            BattleLog.Log("[MapTurn] 無敵人或非敵人回合，回到玩家回合");
            SetTurnType(MapTurnBaseType.PlayerTurn);
            if (playerController != null) playerController.ResetPlayerMove();
            new MapTurnBaseEvent().SendManagerTurn(MapTurnBaseType.PlayerTurn);
        }
    }

    private bool HasEnemies()
    {
        return gridManager != null && gridManager.GetEnemiesInCurrentLevel().Count > 0;
    }

    // 敵人回合：每個敵人沿 BFS 往玩家走一步；同格則進戰鬥
    private IEnumerator EnemyTurn()
    {
        BattleLog.Log("[MapTurn] 進入敵人回合");

        foreach (Transform enemy in gridManager.GetEnemiesInCurrentLevel())
        {
            if (enemy == null) continue;

            Transform playerGrid = gridManager.GetGridAtPosition(gridManager.player.position);
            List<Transform> path = gridManager.FindPath(enemy.position, playerGrid);

            if (path.Count > 1)
            {
                Vector3 next = path[1].position; // 往玩家方向走一格
                while (Vector3.Distance(enemy.position, next) > 0.1f)
                {
                    enemy.position = Vector3.MoveTowards(enemy.position, next, enemyMoveSpeed * Time.deltaTime);

                    if (gridManager.IsPlayerAndEnemyOnSameGrid(gridManager.player, enemy))
                    {
                        BattleLog.Log("[MapTurn] 敵人追上玩家，進入戰鬥");
                        yield return new WaitForSeconds(preBattleDelay);
                        PrepareBattleWithEnemy(enemy);
                        yield break; // 進戰鬥後結束敵人回合，回地圖時再繼續
                    }
                    yield return null;
                }
                enemy.position = next;
            }
            else
            {
                BattleLog.Log("[MapTurn] 敵人找不到通往玩家的路徑");
            }
        }

        yield return new WaitForSeconds(endTurnDelay);
        EndEnemyTurn();
    }

    private void EndEnemyTurn()
    {
        BattleLog.Log("[MapTurn] 敵人回合結束，切回玩家回合");
        new MapTurnBaseEvent().ChangeTurn(MapTurnBaseType.PlayerTurn, enemyMoveSpeed);
    }

    // 開啟 V2 戰鬥
    private void PrepareBattleWithEnemy(Transform enemy)
    {
        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent == null) return;

        EnemyType enemyType = enemyComponent.enemyType;
        BattleLog.Log($"[MapTurn] 準備進入戰鬥: {enemyType}");

        TurnBaseBattlePlayerData playerData = new TurnBaseBattlePlayerData().InitPlayerInfo(CharacterType.Seraphis);

        if (playerController != null) playerController.EnableBlocking();
        if (BattleController.Instance != null)
            BattleController.Instance.StartStoryBattle(playerData, enemyType, true);
        else
            Debug.LogWarning("[MapTurnBaseManager] 場上找不到 BattleController，無法開始 V2 戰鬥。");
    }

    #region 戰鬥結束 → 回到地圖
    private void OnBattleFinished(bool playerWin)
    {
        StartCoroutine(ReturnToMapAfterBattle(playerWin));
    }

    /// <summary>戰鬥結束後：等玩家看完結算 → 收起戰鬥畫面 → 解除阻擋、清理敵人、切回玩家回合。</summary>
    private IEnumerator ReturnToMapAfterBattle(bool playerWin)
    {
        yield return new WaitForSeconds(battleReturnDelay); // 讓結算面板演出、玩家看清勝負

        if (BattleController.Instance != null) BattleController.Instance.CloseBattle(); // 隱藏整個戰鬥 UI

        if (playerWin)
        {
            RemoveEnemyOnPlayerGrid(); // 勝利：移除剛打贏、與玩家同格的地圖敵人
        }
        else if (gridManager != null && gridManager.player != null && gridManager.CurrentStage != null
                 && gridManager.CurrentStage.startGrid != null)
        {
            // 失敗：把玩家退回本關起點，避免與原地敵人同格造成立即再戰的迴圈
            gridManager.player.position = gridManager.CurrentStage.startGrid.position;
        }

        // 解除阻擋並恢復地圖點擊，切回玩家回合
        if (playerController != null)
        {
            playerController.DisableBlocking();
            playerController.ResetPlayerMove();
        }
        SetTurnType(MapTurnBaseType.PlayerTurn);
        new MapTurnBaseEvent().SendManagerTurn(MapTurnBaseType.PlayerTurn);
        BattleLog.Log($"[MapTurn] 戰鬥結束（玩家{(playerWin ? "勝" : "敗")}），已回到地圖探索。");
    }

    /// <summary>移除目前與玩家同一格的敵人（戰鬥勝利後呼叫）。</summary>
    private void RemoveEnemyOnPlayerGrid()
    {
        if (gridManager == null || gridManager.player == null) return;
        List<Transform> enemies = gridManager.GetEnemiesInCurrentLevel();
        Transform playerGrid = gridManager.GetGridAtPosition(gridManager.player.position);
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Transform e = enemies[i];
            if (e == null) { enemies.RemoveAt(i); continue; }
            if (gridManager.GetGridAtPosition(e.position) == playerGrid)
            {
                Destroy(e.gameObject);
                enemies.RemoveAt(i);
            }
        }
    }
    #endregion
}
