using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Script.LostStar2.GlobalEnums;
using Assets.Script.LostStar2.GlobalEnums.BattleEnum;
using Spine.Unity;
using System.Diagnostics;
using DG.Tweening;
using static UnityEngine.UI.GraphicRaycaster;

public class Endless_GM_S001_PlayerController : MonoBehaviour
{
    [Header("玩家設定")]
    public float moveSpeed = 3.0f; // 移動速度
    public int maxMovesPerTurn = 1; // 每回合最多可移動次數
    public int remainingMoves = 1; // 剩餘的可移動次數

    [Header("回合制管理")]
    [SerializeField]
    private MapTurnBaseType currentTurn;

    [Header("音效、轉場")]
    public AudioSource playerMoveSFX; // 玩家移動音效
    public GameObject Transition_Player; // 过渡面板


    [Header("尋路")]
    public LayerMask gridLayer; // 網格層
    public LayerMask BlockLayer;  // 用於阻擋的透明圖層
    public Endless_GM_GridManager gridManager; // 網格管理器 
    private Coroutine moveCoroutine; // 保存當前移動協程
    public List<Transform> CurrentPath = new List<Transform>();

    [Header("轉場")]
    [SerializeField] private CanvasGroup canvasGroup; // 控制畫面透明度的CanvasGroup
    [SerializeField] private float fadeDuration = 1f; // 漸暗效果的持續時間

    [Header("女神對話正比")]

    //public GameObject Godness;
    public SkeletonGraphic skeletonAnimation2; // Spine 動畫控制器

    public GameObject shopUI;
    public BattleButtonFunction BattleButtonFunction;

    [SerializeField]
    bool hasTriggered = false;  // 標誌位，初始為 false

    public DialogueManager DialogueManager;
    public DialogueOpenClose GuideDialogue;
    //[SerializeField] private TurnBaseBattleUnit turnBaseBattleUnit; // 需要設置為 TurnBaseBattleUnit 類型

    public Endless_GM_MapTurnBaseManager turnBaseManager;
    // public MapTurnBaseManager turnBaseManager;

    [Header("馬鈴薯動畫")]
    public SkeletonAnimation skeletonAnimation; // Spine 動畫控制器


    public bool firstbattle = true;
    public TurnBaseBattleManager turnBaseBattleManager;

    private bool isPlayerInputEnabled = false; // 玩家輸入狀態
    private static bool hasTriggeredGuideDialogue1 = false;
    private static bool hasTriggeredGuideDialogue2 = false;

    private Vector3 originalScale;  // 存儲物體的原始大小

    [Header("碰撞位移")]
    public float detectionRange = 0.01f; // 玩家靠近物件的距離（無限接近條件）
    public float backwardDistance = 2f; // 往後退的距離
    public float forwardDistance = 1f; // 往前進的距離
    public float HitSpeed = 5f; // 移動速度
    public bool Flip = false;

    public List<GameObject> objectsToDetect; // 需要偵測的物件列表
    private HashSet<GameObject> triggeredObjects = new HashSet<GameObject>(); // 記錄已觸發的物件

    public GameObject blockingObject; // 透明物件

    // public EventGrid AOA;
    private void Start()
    {

        var gridManager = FindObjectOfType<Endless_GM_GridManager>();

        originalScale = transform.localScale;  // 初始化物體的原始大小
        previousPositionX = transform.position.x; // 初始化上一帧的x坐标
        skeletonAnimation2.enabled = false;
        // AOA = FindAnyObjectByType<EventGrid>();
    }


    private void OnEnable()
    {
        MapTurnBaseEvent.OnTurnInfoSent += GetPlayerControllerCurrentTurn;
    }

    private void OnDisable()
    {
        MapTurnBaseEvent.OnTurnInfoSent -= GetPlayerControllerCurrentTurn;
    }

    // 讓傳入的數值定義玩家端的回合數值
    public void GetPlayerControllerCurrentTurn(MapTurnBaseType type)
    {
        this.currentTurn = type;

        if (currentTurn == MapTurnBaseType.PlayerTurn)
        {
            // 只有當前回合是玩家回合，才重置可移動次數
            if (remainingMoves == maxMovesPerTurn)
            {
                remainingMoves = maxMovesPerTurn;
                UnityEngine.Debug.Log("玩家回合開始，可移動次數: " + remainingMoves);
                EnablePlayerInput(); // 啟用玩家輸入
            }
        }
        else
        {
            DisablePlayerInput(); // 禁用玩家輸入，當回合不是玩家時
        }
    }

    public void ResetPlayerMove()
    {
        remainingMoves = maxMovesPerTurn;
    }

    private void Update()
    {
        if (isPlayerInputEnabled && remainingMoves >= 0)
        {
            HandlePlayerInput();
        }

        DetectPositionChangeAndPlaySFX();

        foreach (var obj in objectsToDetect)
        {
            if (obj != null && !triggeredObjects.Contains(obj)) // 如果物件未被觸發過
            {
                float distance = Vector3.Distance(transform.position, obj.transform.position);

                if (distance <= detectionRange) // 距離足夠近
                {
                    triggeredObjects.Add(obj); // 記錄物件
                    StartCoroutine(MovePlayer()); // 觸發行為
                }
            }
        }

        if (BattleButtonFunction.BattleIsEnd == true && !hasTriggered)
        {
            hasTriggered = true;  // 設定已經觸發過
            StartCoroutine(MoveToNextLevel());
        }
        else if (BattleButtonFunction.BattleIsEnd == false)
        {
            hasTriggered = false;  // 重新設置標誌位，為 false，準備下一次觸發
        }

    }

    // 啟用玩家輸入
    public void EnablePlayerInput()
    {
        isPlayerInputEnabled = true;
    }

    // 禁用玩家輸入
    public void DisablePlayerInput()
    {
        if (remainingMoves <= 0)
        {
            isPlayerInputEnabled = false;
        }

    }
    public void DisablePlayerInputForCheck()
    {
        isPlayerInputEnabled = false;
    }

    public void EnableBlocking()
    {
        blockingObject.SetActive(true); // 啟用透明圖層
    }

    public void DisableBlocking()
    {
        blockingObject.SetActive(false); // 禁用透明圖層
    }

    // 處理玩家輸入
    private void HandlePlayerInput()
    {


        if (isPlayerInputEnabled == false) return;

        // 如果商店 UI 開啟或移動次數用完，直接返回
        if (shopUI.activeSelf || remainingMoves <= 0) return;

        if (Input.GetMouseButtonDown(0))
        {

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 檢查是否被透明圖層阻擋
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, BlockLayer))
            {
                UnityEngine.Debug.Log("被透明圖層阻擋，點擊無效");
                return; // 如果點擊到透明圖層，不執行其他邏輯
            }

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayer))
            {
                Transform targetGrid = gridManager.GetGridAtPosition(hit.point);
                if (targetGrid != null)
                {

                    UnityEngine.Debug.Log("目標網格位置: " + targetGrid.position);
                    // 停止協程
                    if (moveCoroutine != null)
                    {
                        StopCoroutine(moveCoroutine);
                    }

                    // 開始新的移動協程
                    moveCoroutine = StartCoroutine(MovePlayerToGrid(targetGrid));
                }
                else
                {
                    UnityEngine.Debug.Log("沒找到目標!");
                }
            }
        }
    }

    // 沿著格子移動玩家
    private IEnumerator MovePlayerToGrid(Transform targetGrid)
    {
        // 開始跑步動畫
        skeletonAnimation.AnimationState.SetAnimation(0, "Run", true); // 播放 run 動畫，並設置為循環播放

        if (targetGrid == null)
        {
            UnityEngine.Debug.LogError("Target grid is null or destroyed.");
            yield break;
        }

        // 查找路徑
        CurrentPath = gridManager.FindPath(transform.position, targetGrid);
        UnityEngine.Debug.Log("目前位置: " + transform.position + " 目標位置: " + targetGrid.position);

        // 沒有路徑直接返回
        if (CurrentPath.Count == 0) yield break;

        foreach (Transform grid in CurrentPath)
        {
            if (grid == null)
            {
                UnityEngine.Debug.LogWarning("A grid in the path has been destroyed. Skipping this grid.");
                continue;
            }

            Vector3 startPosition = transform.position;
            Vector3 targetPosition = grid.position;

            // **轉向判斷：根據 x 坐標比較來改變方向**
            UpdatePlayerDirection(targetPosition.x - startPosition.x);

            float journeyLength = Vector3.Distance(startPosition, targetPosition);
            float startTime = Time.time;

            // 移动完成后再进行敌人检测
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                // 移动玩家
                float distCovered = (Time.time - startTime) * moveSpeed;
                float fractionOfJourney = distCovered / journeyLength;
                transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);

                yield return null;
            }

            // 玩家到達格子後，再檢查是否觸發戰鬥
            foreach (var enemy in gridManager.GetEnemiesInCurrentLevel())
            {
                if (gridManager.IsPlayerAndEnemyOnSameGrid(this.transform, enemy))
                {
                    UnityEngine.Debug.Log("玩家与敌人在同一格！触发过渡效果。");
                    StartCoroutine(MovePlayer());
                    yield return new WaitForSeconds(1f);
                    // 使用 DOTween 缩放 Transition_Player 的 Scale
                    Transition_Player.transform.DOScale(new Vector3(1.0f, 2.0f, 1.0f), 0.5f)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            UnityEngine.Debug.Log("过渡动画完成，准备进入战斗。");

                            // 停止当前协程，避免进一步移动
                            StopCoroutine(moveCoroutine);

                            FadeToDark();

                            DOVirtual.DelayedCall(1f, () =>
                            {
                                // 准备战斗数据
                                PrepareBattleWithEnemy(enemy);
                                skeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);

                                Transition_Player.SetActive(false);
                                transform.position = targetPosition; // 確保到達目標位置
                                StartL1Transition();
                                BattleButtonFunction.BattleIsEnd = false;
                            });
                        });
                    yield break; // 停止移动并结束当前协程
                }
            }
            remainingMoves--;
            UnityEngine.Debug.Log("剩餘可移動次數: " + remainingMoves);


        }

        // 移動結束後切換回 idle 動畫
        skeletonAnimation.AnimationState.SetAnimation(0, "Idle", true); // 播放 idle 動畫，並設置為循環播放

        // 到達最終目標後檢查是否是終點格子
        if (targetGrid != null)
        {
            // 檢查是否為事件格
            Endless_GM_EventGrid eventGrid = targetGrid.GetComponent<Endless_GM_EventGrid>();
            if (eventGrid != null)
            {
                // 觸發事件
                eventGrid.TriggerEvent(transform);
                yield return new WaitUntil(() => eventGrid.EventCompleted); // 假設有個變量標示事件完成

                UnityEngine.Debug.Log("玩家回合事件完成 ");
            }

            UnityEngine.Debug.Log("玩家到達終點，傳送至下一關起點。");
            UnityEngine.Debug.Log("播放轉場動畫...");

        }


        // 在這裡添加一個檢查，確保只有當目標不是終點時才切換到敵人回合
        if (targetGrid != gridManager.levels[gridManager.currentLevelIndex].endGrid)
        {
            // 切換至敵人回合
            MapTurnBaseEvent enemyTurnEvent = new MapTurnBaseEvent();
            enemyTurnEvent.ChangeTurn(MapTurnBaseType.EnemyTurn, moveSpeed); // 切換到敵人回合
        }
    }

    // 縮小物體的協程
    private IEnumerator ScaleDownOverTime(float duration)
    {
        Vector3 originalScale = transform.localScale; // 保存原始大小
        Vector3 targetScale = originalScale * 0.6f;  // 設定縮小後的目標大小
        Vector3 originalPosition = transform.position; // 保存原始位置
        Vector3 targetPosition = originalPosition + new Vector3(0f, 0f, 0.5f);

        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, timeElapsed / duration);
            transform.position = Vector3.Lerp(originalPosition, targetPosition, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // 確保最後的大小是目標大小
        transform.localScale = targetScale;
    }

    private IEnumerator ScaleUpToOriginalSize(float duration)
    {
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // 確保最後的大小是原始大小
        transform.localScale = originalScale;
    }

    private void UpdatePlayerDirection(float directionX)
    {
        if (directionX > 0)
        {
            // 向右移動
            transform.localScale = new Vector3(1, 1, 1); // 設置玩家的 X 軸縮放為正值
            Flip = false;
        }
        else if (directionX < 0)
        {
            // 向左移動
            transform.localScale = new Vector3(-1, 1, 1); // 設置玩家的 X 軸縮放為負值
            Flip = true;
        }
    }
    public PlayerMapStatus_UI_EndLess playerstatus;

    private void PrepareBattleWithEnemy(Transform enemy)
    {

        // 假設敵人有一個 Enemy 類來獲取敵人的類型
        Enemy enemyComponent = enemy.GetComponent<Enemy>();

        gridManager?.MarkEnemyAsEncountered(enemy);

        if (enemyComponent != null)
        {
            EnemyType enemyType = enemyComponent.enemyType; // 獲取敵人的類型

            UnityEngine.Debug.Log($"準備進入戰鬥: {enemyType}");

            if (firstbattle == true)
            {
                // 開啟戰鬥
                TurnBaseBattlePlayerData battleUnitPlayerData = new TurnBaseBattlePlayerData();
                TurnBaseBattlePlayerData playerData = battleUnitPlayerData.InitPlayerInfo(CharacterType.Seraphis);

                BattleButtonFunction.OpenBattle_EndlessMode(true, playerData, playerData.UnitType, enemyType, 0);
            }
            else
            {
                // 開啟戰鬥
                TurnBaseBattlePlayerData battleUnitPlayerData = new TurnBaseBattlePlayerData();
                playerstatus = FindAnyObjectByType<PlayerMapStatus_UI_EndLess>();
                battleUnitPlayerData = playerstatus.playerDataInMap;
                BattleButtonFunction.OpenBattle_EndlessMode(true, battleUnitPlayerData, battleUnitPlayerData.UnitType, enemyType, 0);

                EnableBlocking();
            }

            // 開啟戰鬥
            // BattleButtonFunction.OpenBattle();

        }
    }



    private void HandlePlayerInputSFX()
    {
        if (shopUI.activeSelf || remainingMoves <= 0) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayer))
            {
                Transform targetGrid = gridManager.GetGridAtPosition(hit.point);
                if (targetGrid != null)
                {
                    if (moveCoroutine != null)
                    {
                        StopCoroutine(moveCoroutine);
                        StopMoveSFX(); // 停止移動音效
                    }

                    moveCoroutine = StartCoroutine(MovePlayerSFX(targetGrid));
                }
            }
        }
    }

    private IEnumerator MovePlayerSFX(Transform targetGrid)
    {
        skeletonAnimation.AnimationState.SetAnimation(0, "Run", true);
        PlayMoveSFX(); // 開始播放移動音效

        CurrentPath = gridManager.FindPath(transform.position, targetGrid);
        if (CurrentPath.Count == 0)
        {
            StopMoveSFX(); // 沒有路徑時停止音效
            yield break;
        }

        foreach (Transform grid in CurrentPath)
        {
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = grid.position;

            UpdatePlayerDirection(targetPosition.x - startPosition.x);

            float journeyLength = Vector3.Distance(startPosition, targetPosition);
            float startTime = Time.time;

            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                float distCovered = (Time.time - startTime) * moveSpeed;
                float fractionOfJourney = distCovered / journeyLength;

                transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
                yield return null;
            }

            transform.position = targetPosition;
            remainingMoves--;
        }

        skeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);
        StopMoveSFX(); // 停止移動音效
    }

    private void PlayMoveSFX()
    {
        if (playerMoveSFX != null && !playerMoveSFX.isPlaying)
        {
            playerMoveSFX.Play();
        }
    }

    private void StopMoveSFX()
    {
        if (playerMoveSFX != null && playerMoveSFX.isPlaying)
        {
            playerMoveSFX.Stop();
        }
    }

    // 新增一个变量用于记录上一帧的x坐标
    private float previousPositionX;




    private void DetectPositionChangeAndPlaySFX()
    {
        float currentPositionX = transform.position.x;

        if (!Mathf.Approximately(currentPositionX, previousPositionX))
        {
            // x 坐标发生变化，播放音效
            PlayMoveSFX();
        }
        else
        {
            // x 坐标没有变化，停止音效
            StopMoveSFX();
        }

        // 更新上一帧的x坐标
        previousPositionX = currentPositionX;
    }

    private IEnumerator MoveToNextLevel()
    {
        // 預留轉場動畫的時間或效果，並加入縮小動畫
        yield return StartCoroutine(ScaleDownOverTime(1.0f)); // 在1秒內逐步縮小

        // 假設轉場動畫播放 1 秒
        yield return new WaitForSeconds(1.0f);

        // 傳送到下一關
        gridManager.MoveToNextLevel();

        StartCoroutine(ScaleUpToOriginalSize(1.0f)); // 在1秒內恢復至原始大小


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

    private void StartL1Transition()
    {
        if (canvasGroup != null)
        {
            // 确保 canvasGroup 初始透明度为1（完全黑）
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // 使用 DOTween 淡入效果，将透明度变为0（完全透明）
            canvasGroup.DOFade(0f, fadeDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    // 动画完成后将 canvasGroup 设置为不可见
                    canvasGroup.gameObject.SetActive(false);
                });
        }
        else
        {
            UnityEngine.Debug.LogWarning("CanvasGroup 未设置，请检查赋值！");
        }
    }

    private void FadeToDark()
    {
        if (canvasGroup != null)
        {
            // 确保 canvasGroup 可见并初始化状态
            canvasGroup.alpha = 0f; // 初始透明度
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // 设置为可见
            canvasGroup.gameObject.SetActive(true);

            // 使用 DOTween 淡出效果，将透明度变为1（完全黑）
            canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.Linear);
        }
        else
        {
            UnityEngine.Debug.LogWarning("CanvasGroup 未设置，请检查赋值！");
        }
    }
}





