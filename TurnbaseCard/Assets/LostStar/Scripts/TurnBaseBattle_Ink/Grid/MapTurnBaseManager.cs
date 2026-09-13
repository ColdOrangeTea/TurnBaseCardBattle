using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using TurnBaseBattleV2;

public class MapTurnBaseManager : MonoBehaviour
{
    public GridManager gridManager; // 用來管理格子
    MapTurnBaseType currentTurn;
    private float playerMoveSpeed = 0; // 玩家移動速度
    public S001_PlayerController playerController; //這個之後想修掉

    // 舊 BattleButtonFunction 已移除，改用 V2 的 BattleController.Instance 開戰。

    [Header("碰撞位移")]
    public float detectionRange = 0.01f; // 玩家靠近物件的距離（無限接近條件）
    public float backwardDistance = 2f; // 往後退的距離
    public float forwardDistance = 1f; // 往前進的距離
    public float HitSpeed = 5f; // 移動速度
    public bool Flip = false;
    public List<GameObject> objectsToDetect; // 需要偵測的物件列表
    private HashSet<GameObject> triggeredObjects = new HashSet<GameObject>(); // 記錄已觸發的物件


    void OnEnable()
    {
        MapTurnBaseEvent.OnTurnChanged += EndPlayerTurn; // 訂閱回合變更事件

    }

    void OnDisable()
    {
        MapTurnBaseEvent.OnTurnChanged -= EndPlayerTurn; // 取消訂閱回合變更事件
    }

    void Start()
    {
        currentTurn = MapTurnBaseType.PlayerTurn;
        MapTurnBaseEvent turnBaseEvent = new MapTurnBaseEvent();
        turnBaseEvent.SendManagerTurn(currentTurn); // 發送當前回合訊息給其他控制器


    }



    void Update()
    {
        CheckCurrentTurn();
        foreach (var obj in objectsToDetect)
        {
            if (!triggeredObjects.Contains(obj)) // 如果物件未被觸發過
            {
                float distance = Vector3.Distance(transform.position, obj.transform.position);

                if (distance <= detectionRange) // 距離足夠近
                {
                    triggeredObjects.Add(obj); // 記錄物件
                    StartCoroutine(MovePlayer()); // 觸發行為
                }
            }
        }
    }

    public void GetSpeedFromPlayer(float speed) => playerMoveSpeed = speed; // 從玩家那裡獲取速度
    public void SetTurnType(MapTurnBaseType nextType) => currentTurn = nextType; // 設置下一個回合類型

    void CheckCurrentTurn()
    {
        if (currentTurn == MapTurnBaseType.PlayerTurn)
        {
            Debug.Log("現在是玩家的回合");
            MapTurnBaseEvent turnBaseEvent = new MapTurnBaseEvent();
            turnBaseEvent.SendManagerTurn(currentTurn); // 發送玩家回合的訊息
        }
        else if (currentTurn == MapTurnBaseType.EnemyTurn)
        {
            Debug.Log("現在是敵人的回合");
        }
    }

    private void EndPlayerTurn(MapTurnBaseType nextTurn, float speed)
    {
        SetTurnType(nextTurn);
        GetSpeedFromPlayer(speed);

        // 檢查玩家是否位於當前關卡的起點格
        Transform currentLevelStartGrid = gridManager.levels[gridManager.currentLevelIndex].startGrid;
        if (gridManager.player.position == currentLevelStartGrid.position)
        {
            // 切換至玩家回合
            SetTurnType(MapTurnBaseType.PlayerTurn);
            Debug.Log(currentTurn);



            Debug.Log("玩家位於起點格，回合切換回玩家回合");
        }

        if (CheckForEnemiesInCurrentLevel() && currentTurn == MapTurnBaseType.EnemyTurn)
        {

            StartCoroutine(EnemyTurn());
        }
        else
        {
            Debug.Log("當前關卡沒有敵人，結束敵人回合");
            SetTurnType(MapTurnBaseType.PlayerTurn);
            playerController.ResetPlayerMove();

        }
    }

    // 檢查當前關卡中的敵人列表
    private bool CheckForEnemiesInCurrentLevel()
    {
        List<Transform> enemies = gridManager.GetEnemiesInCurrentLevel();
        return enemies.Count > 0;
    }



    // 敵人回合邏輯
    private IEnumerator EnemyTurn()
    {
        Debug.Log("現在進入敵人的回合");


        List<Transform> enemies = gridManager.GetEnemiesInCurrentLevel();

        foreach (Transform enemy in enemies)
        {
            Vector3 enemyPosition = enemy.position;
            Transform currentGrid = gridManager.GetGridAtPosition(enemyPosition);
            Vector3 playerPosition = gridManager.player.position;

            List<Transform> pathToPlayer = gridManager.FindPath(enemyPosition, gridManager.GetGridAtPosition(playerPosition));

            if (pathToPlayer.Count > 1)
            {
                Vector3 nextPosition = pathToPlayer[1].position;
                while (Vector3.Distance(enemy.position, nextPosition) > 0.1f)
                {
                    enemy.position = Vector3.MoveTowards(enemy.position, nextPosition, playerMoveSpeed * Time.deltaTime);

                    // 每幀移動時檢查是否與玩家在同一個格子上
                    if (gridManager.IsPlayerAndEnemyOnSameGrid(gridManager.player, enemy))
                    {
                        Debug.Log("玩家與敵人在同一個格子上!");
                        // 觸發對應事件，例如進入戰鬥
                        StartCoroutine(MovePlayer());

                        Debug.Log("進入戰鬥 !");
                        // 預留轉場動畫的時間或效果
                        yield return new WaitForSeconds(0.1f); // 假設轉場動畫播放 3 秒

                        // 準備戰鬥數據
                        PrepareBattleWithEnemy(enemy);

                        yield break; // 停止敵人移動並結束該敵人的回合
                    }

                    yield return null;
                }
                enemy.position = nextPosition;
            }
            else
            {
                Debug.Log("敵人找不到路徑到玩家!");
            }
        }

        yield return new WaitForSeconds(1f); // 等待敵人移動

        EndEnemyTurn();
    }

    private void EndEnemyTurn()
    {
        Debug.Log("切換到玩家回合");

        // 切換至玩家回合
        MapTurnBaseEvent PlayerTurnEvent = new MapTurnBaseEvent();
        PlayerTurnEvent.ChangeTurn(MapTurnBaseType.PlayerTurn, playerMoveSpeed);




    }

    private void PrepareBattleWithEnemy(Transform enemy)
    {
        // 假設敵人有一個 Enemy 類來獲取敵人的類型
        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            EnemyType enemyType = enemyComponent.enemyType; // 獲取敵人的類型

            // GameObject EnemyProfile = GameObject.FindWithTag("Player2");
            // GameObject enemyData = GameObject.FindWithTag("Player2");

            // if (EnemyProfile != null)
            // {
            //     BattleUnitProfile profile = EnemyProfile.GetComponent<BattleUnitProfile>();
            //     profile.GetUnitType(CharacterType.Enemy, enemyType);

            //     if (enemyData != null)
            //     {
            //         BattleUnitEnemyDisplayData enemyDataComponent = enemyData.GetComponent<BattleUnitEnemyDisplayData>();
            //         enemyDataComponent.EnemyType = enemyType;
            //         enemyDataComponent.SetData(enemyType);

            //         profile.SetUnitName(enemyDataComponent.unitName);
            //         profile.UpdateNameText();

            //         profile.InitProfile();
            //     }
            //     else
            //     {
            //         Debug.LogWarning("未找到標記Data物件");
            //     }
            // }
            // else
            // {
            //     Debug.LogWarning("未找到標記Profile物件");
            // }


            Debug.Log($"準備進入戰鬥: {enemyType}");
            TurnBaseBattlePlayerData battleUnitPlayerData = new TurnBaseBattlePlayerData();
            TurnBaseBattlePlayerData playerData = battleUnitPlayerData.InitPlayerInfo(CharacterType.Seraphis);

            // 開啟戰鬥（V2）：改用常駐的 BattleController 開一場劇情戰鬥
            if (playerController != null) playerController.EnableBlocking();
            if (BattleController.Instance != null)
                BattleController.Instance.StartStoryBattle(playerData, enemyType, true);
            else
                Debug.LogWarning("[MapTurnBaseManager] 場上找不到 BattleController，無法開始 V2 戰鬥。");
        }
    }

    private IEnumerator MovePlayer()
    {
        // 計算移動目標點
        if (!Flip)
        {
            Vector3 startPosition = transform.position;
            Vector3 backwardPosition = startPosition - transform.right * backwardDistance; // 往 X 軸負方向
            Vector3 forwardPosition = backwardPosition + transform.right * forwardDistance; // 往 X 軸正方向

            // 平滑後退
            yield return StartCoroutine(MoveToPosition(startPosition, backwardPosition));

            // 短暫等待
            yield return new WaitForSeconds(0.1f);

            // 平滑前進
            yield return StartCoroutine(MoveToPosition(backwardPosition, forwardPosition));
        }
        else
        {
            Vector3 startPosition = transform.position;
            Vector3 forwardPosition = startPosition + transform.right * backwardDistance; // 往 X 軸正方向
            Vector3 backwardPosition = forwardPosition - transform.right * forwardDistance; // 往 X 軸負方向
            // 平滑後退
            yield return StartCoroutine(MoveToPosition(startPosition, forwardPosition));

            // 短暫等待
            yield return new WaitForSeconds(0.1f);

            // 平滑前進
            yield return StartCoroutine(MoveToPosition(backwardPosition, backwardPosition));
        }
    }

    private IEnumerator MoveToPosition(Vector3 start, Vector3 end)
    {
        float elapsedTime = 0f; // 過去時間
        float duration = Vector3.Distance(start, end) / HitSpeed; // 移動所需時間

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(start, end, elapsedTime / duration); // 插值計算
            elapsedTime += Time.deltaTime; // 更新過去時間
            yield return null; // 等待下一幀
        }

        transform.position = end; // 確保最後到達目標位置
    }
}
