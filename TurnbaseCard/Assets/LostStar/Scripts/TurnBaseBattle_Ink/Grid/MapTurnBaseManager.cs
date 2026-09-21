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
    public LevelMapManager gridManager;   // 欄位名沿用 gridManager 以保留 prefab 序列化
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
    private Transform battleEnemy;     // 這場戰鬥對應的地圖敵人（勝利後移除）

    void OnEnable()  => MapTurnBaseEvent.OnTurnChanged += OnTurnChanged;
    void OnDisable() => MapTurnBaseEvent.OnTurnChanged -= OnTurnChanged;

    void Start()
    {
        // 先手/後手：讀 LevelMapInitializer 決定進場是玩家先動、還是敵人(怪物)先動；找不到初始化器則預設玩家先動。
        bool playerFirst = LevelMapInitializer.Instance == null || LevelMapInitializer.Instance.PlayerMovesFirst;
        if (playerFirst)
        {
            currentTurn = MapTurnBaseType.PlayerTurn;
            new MapTurnBaseEvent().SendManagerTurn(currentTurn); // 通知其他控制器目前是玩家回合
        }
        else
        {
            // 敵人先動：先跑一次敵人回合，跑完會自動切回玩家回合（無敵人時 OnTurnChanged 會直接回玩家回合）。
            currentTurn = MapTurnBaseType.EnemyTurn;
            float firstEnemySpeed = playerController != null ? playerController.moveSpeed : enemyMoveSpeed;
            new MapTurnBaseEvent().ChangeTurn(MapTurnBaseType.EnemyTurn, firstEnemySpeed);
        }

        // 訂閱 V2 戰鬥結束通知，戰鬥收尾後回到地圖（Instance 於 BattleController.Awake 設定，早於此 Start）
        if (BattleController.Instance != null)
        {
            BattleController.Instance.BattleFinished += OnBattleFinished;
            BattleController.Instance.SettlementConfirmed += OnSettlementConfirmed;
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
        {
            BattleController.Instance.BattleFinished -= OnBattleFinished;
            BattleController.Instance.SettlementConfirmed -= OnSettlementConfirmed;
        }
    }

    private bool settlementConfirmed;
    private void OnSettlementConfirmed() => settlementConfirmed = true;

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

        SetBattleEnemy(enemy); // 記住這場戰鬥的敵人，勝利後精準移除
        if (MapFlowController.Instance != null) MapFlowController.Instance.NotifyBattleStarted();
        if (playerController != null) playerController.EnableBlocking();
        if (BattleController.Instance != null)
            BattleController.Instance.StartStoryBattle(playerData, enemyType, true);
        else
            Debug.LogWarning("[MapTurnBaseManager] 場上找不到 BattleController，無法開始 V2 戰鬥。");
    }

    /// <summary>登記「這場戰鬥要打的地圖敵人」。由本管理器或 S001（玩家撞上敵人）在開戰前呼叫。</summary>
    public void SetBattleEnemy(Transform enemy) => battleEnemy = enemy;

    #region 戰鬥結束 → 回到地圖
    private void OnBattleFinished(bool playerWin)
    {
        // 戰鬥一結束就立刻處理地圖敵人：勝利即移除那隻敵人（不等結算動畫），
        // 這樣之後收起戰鬥畫面回到地圖時，玩家不會看到敵人殘留、過一會兒才消失。
        if (playerWin) RemoveBattleEnemy();
        settlementConfirmed = false; // 每場都要等玩家重新按一次結算確定
        StartCoroutine(ReturnToMapAfterBattle(playerWin));
    }

    /// <summary>戰鬥結束後：等玩家看完結算 → 收起戰鬥畫面 → 解除阻擋、切回玩家回合（敵人已在 OnBattleFinished 即時移除）。</summary>
    private IEnumerator ReturnToMapAfterBattle(bool playerWin)
    {
        if (!playerWin)
        {
            battleEnemy = null; // 失敗：不移除敵人
            if (gridManager != null && gridManager.player != null && gridManager.CurrentStage != null
                && gridManager.CurrentStage.startGrid != null)
            {
                // 把玩家退回本關起點，避免與原地敵人同格造成立即再戰的迴圈
                gridManager.player.position = gridManager.CurrentStage.startGrid.position;
            }
        }

        // 等玩家在結算面板按下「確定」才收起戰鬥（不再計時自動關閉）
        yield return new WaitUntil(() => settlementConfirmed);

        if (BattleController.Instance != null) BattleController.Instance.CloseBattle(); // 隱藏整個戰鬥 UI

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

    /// <summary>移除這場戰鬥登記的敵人（戰鬥勝利後呼叫）。用登記的實體，不靠格子位置比對，避免敵人停在格間而漏刪。</summary>
    private void RemoveBattleEnemy()
    {
        if (battleEnemy == null) return;
        if (gridManager != null)
            gridManager.GetEnemiesInCurrentLevel().Remove(battleEnemy);
        Destroy(battleEnemy.gameObject);
        battleEnemy = null;
    }
    #endregion
}
