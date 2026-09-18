using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using TurnBaseBattleV2;
using Spine.Unity;
using DG.Tweening;

/// <summary>
/// 地圖玩家控制器（深度重構版）。負責：
///   - 玩家回合時的點擊尋路移動（沿相鄰格 BFS，Spine Run/Idle 動畫、移動音效）；
///   - 移動途中撞到敵人 → 轉場並進入 V2 戰鬥；走到終點(Door) → 切下一個 Stage；
///   - 停在事件格 → 交給 <see cref="MapEventService"/>（戰鬥接 V2，其餘商店/事件/寶箱/任務為空殼）；
///   - 沒進戰鬥且非終點 → 切敵人回合。
///
/// 已移除：三段重複的開戰程式碼（原本會重複開戰）、未被呼叫的 SFX 版移動、女神/引導對話等劇情殘留。
/// 保留 prefab 綁定所需的公開欄位與 GUID 不變。
/// </summary>
public class S001_PlayerController : MonoBehaviour
{
    [Header("玩家設定")]
    public float moveSpeed = 3.0f;      // 移動速度
    public int maxMovesPerTurn = 1;     // 每回合最多可移動次數
    public int remainingMoves = 1;      // 剩餘的可移動次數

    [Header("回合制管理")]
    [SerializeField] private MapTurnBaseType currentTurn;

    [Header("音效、轉場")]
    public AudioSource playerMoveSFX;   // 玩家移動音效
    public GameObject Transition_Player; // 過渡面板
    [SerializeField] private CanvasGroup canvasGroup; // 控制畫面透明度
    [SerializeField] private float fadeDuration = 1f; // 漸暗持續時間

    [Header("尋路")]
    public LayerMask gridLayer;         // 網格層
    public LayerMask BlockLayer;        // 阻擋用的透明圖層
    public GridManager gridManager;     // 網格管理器
    public List<Transform> CurrentPath = new List<Transform>();
    private Coroutine moveCoroutine;    // 當前移動協程

    public GameObject shopUI;

    [Header("續戰旗標（保留 prefab 相容）")]
    public bool firstbattleInStory = true;
    public bool AgainbattleInStory = true;

    [Header("馬鈴薯動畫")]
    public SkeletonAnimation skeletonAnimation; // Spine 動畫控制器

    public bool isPlayerInputEnabled = false;   // 玩家輸入狀態
    private Vector3 originalScale;              // 物體原始大小

    [Header("碰撞位移")]
    public float detectionRange = 0.01f; // 靠近物件的距離
    public float backwardDistance = 2f;  // 往後退距離
    public float forwardDistance = 1f;   // 往前進距離
    public float HitSpeed = 5f;          // 位移速度
    public bool Flip = false;
    public List<GameObject> objectsToDetect;
    private readonly HashSet<GameObject> triggeredObjects = new HashSet<GameObject>();

    public GameObject blockingObject;    // 透明阻擋物件

    [Header("地圖事件")]
    [SerializeField] private MapEventService mapEventService; // 事件格觸發（戰鬥接 V2，其餘空殼）

    private float previousPositionX;

    private void Start()
    {
        originalScale = transform.localScale;
        previousPositionX = transform.position.x;
    }

    private void OnEnable()  => MapTurnBaseEvent.OnTurnInfoSent += GetPlayerControllerCurrentTurn;
    private void OnDisable() => MapTurnBaseEvent.OnTurnInfoSent -= GetPlayerControllerCurrentTurn;

    // 依傳入的回合類型設定玩家端狀態
    public void GetPlayerControllerCurrentTurn(MapTurnBaseType type)
    {
        currentTurn = type;

        if (currentTurn == MapTurnBaseType.PlayerTurn)
        {
            if (remainingMoves == maxMovesPerTurn)
            {
                remainingMoves = maxMovesPerTurn;
                BattleLog.Log("玩家回合開始，可移動次數: " + remainingMoves);
                EnablePlayerInput();
            }
        }
        else
        {
            DisablePlayerInput();
        }
    }

    public void ResetPlayerMove() => remainingMoves = maxMovesPerTurn;

    private void Update()
    {
        if (isPlayerInputEnabled && remainingMoves >= 0)
            HandlePlayerInput();

        DetectPositionChangeAndPlaySFX();

        foreach (var obj in objectsToDetect)
        {
            if (obj != null && !triggeredObjects.Contains(obj))
            {
                float distance = Vector3.Distance(transform.position, obj.transform.position);
                if (distance <= detectionRange)
                {
                    triggeredObjects.Add(obj);
                    StartCoroutine(MovePlayer());
                }
            }
        }
    }

    public void EnablePlayerInput() => isPlayerInputEnabled = true;

    public void DisablePlayerInput()
    {
        if (remainingMoves <= 0) isPlayerInputEnabled = false;
    }

    public void DisablePlayerInputForCheck() => isPlayerInputEnabled = false;

    public void EnableBlocking()
    {
        if (blockingObject != null) blockingObject.SetActive(true);
        DisablePlayerInputForCheck(); // 開戰期間停用地圖點擊，避免點到戰鬥畫面後方的格子（blockingObject 沒指定時也有效）
    }

    public void DisableBlocking()
    {
        if (blockingObject != null) blockingObject.SetActive(false);
        EnablePlayerInput(); // 戰鬥結束回到地圖後恢復點擊
    }

    // 處理玩家點擊輸入 → 尋路移動
    private void HandlePlayerInput()
    {
        if (!isPlayerInputEnabled) return;
        if ((shopUI != null && shopUI.activeSelf) || remainingMoves <= 0) return;

        // 暫停中（timeScale=0，如開暫停選單）不處理地圖點擊
        if (Time.timeScale == 0f) return;
        // 點在 UI 上（暫停選單/商店/寶箱等）時，不讓點擊穿透到後方地圖格子
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 被透明圖層阻擋則無效
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, BlockLayer))
            {
                BattleLog.Log("被透明圖層阻擋，點擊無效");
                return;
            }

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayer))
            {
                Transform targetGrid = gridManager.GetGridAtPosition(hit.point);
                if (targetGrid != null)
                {
                    BattleLog.Log("目標網格位置: " + targetGrid.position);
                    if (moveCoroutine != null) StopCoroutine(moveCoroutine);
                    moveCoroutine = StartCoroutine(MovePlayerToGrid(targetGrid));
                }
                else
                {
                    BattleLog.Log("沒找到目標!");
                }
            }
        }
    }

    // 沿格子移動玩家
    private IEnumerator MovePlayerToGrid(Transform targetGrid)
    {
        skeletonAnimation.AnimationState.SetAnimation(0, "Run", true);

        CurrentPath = gridManager.FindPath(transform.position, targetGrid);
        BattleLog.Log("目前位置: " + transform.position + " 目標位置: " + targetGrid.position);
        if (CurrentPath.Count == 0) yield break;

        foreach (Transform grid in CurrentPath)
        {
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = grid.position;
            UpdatePlayerDirection(targetPosition.x - startPosition.x);

            float journeyLength = Vector3.Distance(startPosition, targetPosition);
            float startTime = Time.time;

            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                float fractionOfJourney = ((Time.time - startTime) * moveSpeed) / journeyLength;
                transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);

                // 移動途中撞到敵人 → 轉場並進戰鬥
                foreach (var enemy in gridManager.GetEnemiesInCurrentLevel())
                {
                    if (gridManager.IsPlayerAndEnemyOnSameGrid(transform, enemy))
                    {
                        BattleLog.Log("玩家與敵人在同一格！觸發過渡效果。");
                        StartCoroutine(MovePlayer());
                        yield return new WaitForSeconds(0.1f);

                        // 有轉場面板就播縮放轉場，否則直接進戰鬥（沒綁 UI 的測試場也能跑）
                        if (Transition_Player != null)
                        {
                            Transition_Player.transform.DOScale(new Vector3(1.0f, 2.0f, 1.0f), 0.5f)
                                .SetEase(Ease.OutQuad)
                                .OnComplete(() =>
                                {
                                    BattleLog.Log("過渡動畫完成，準備進入戰鬥。");
                                    if (moveCoroutine != null) StopCoroutine(moveCoroutine);
                                    FadeToDark();
                                    DOVirtual.DelayedCall(1f, () =>
                                    {
                                        PrepareBattleWithEnemy(enemy);
                                        skeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);
                                        Transition_Player.SetActive(false);
                                        transform.position = targetPosition;
                                        StartL1Transition();
                                    });
                                });
                        }
                        else
                        {
                            transform.position = targetPosition;
                            PrepareBattleWithEnemy(enemy);
                            skeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);
                        }

                        yield break;
                    }
                }

                yield return null;
            }

            transform.position = targetPosition;
            remainingMoves--;
            BattleLog.Log("剩餘可移動次數: " + remainingMoves);
        }

        skeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);

        bool reachedEnd = CurrentStageEndGrid() != null && targetGrid == CurrentStageEndGrid();

        // 到達終點(Door) → 切下一個 Stage
        if (reachedEnd)
        {
            BattleLog.Log("玩家到達終點，切換至下一個 Stage。");
            yield return StartCoroutine(ScaleDownOverTime(1.0f));
            yield return new WaitForSeconds(1.0f);
            gridManager.MoveToNextLevel();
            StartCoroutine(ScaleUpToOriginalSize(1.0f));
        }

        // 事件格觸發（戰鬥接 V2，其餘為空殼）
        bool enteredBattle = false;
        EventGrid eventGrid = targetGrid.GetComponent<EventGrid>();
        if (eventGrid != null && mapEventService != null)
            enteredBattle = mapEventService.TriggerGridEvent(eventGrid);

        // 非終點且沒進戰鬥 → 切敵人回合
        if (!enteredBattle && !reachedEnd)
            new MapTurnBaseEvent().ChangeTurn(MapTurnBaseType.EnemyTurn, moveSpeed);
    }

    // 目前 Stage 的終點格（保護存取）
    private Transform CurrentStageEndGrid()
    {
        var stage = gridManager != null ? gridManager.CurrentStage : null;
        return stage != null ? stage.endGrid : null;
    }

    private IEnumerator ScaleDownOverTime(float duration)
    {
        Vector3 fromScale = transform.localScale;
        Vector3 toScale = fromScale * 0.6f;
        Vector3 fromPos = transform.position;
        Vector3 toPos = fromPos + new Vector3(0f, 0f, 0.5f);

        float t = 0f;
        while (t < duration)
        {
            transform.localScale = Vector3.Lerp(fromScale, toScale, t / duration);
            transform.position = Vector3.Lerp(fromPos, toPos, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = toScale;
    }

    private IEnumerator ScaleUpToOriginalSize(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;
    }

    private void UpdatePlayerDirection(float directionX)
    {
        if (directionX > 0)      { transform.localScale = new Vector3(1, 1, 1);  Flip = false; }
        else if (directionX < 0) { transform.localScale = new Vector3(-1, 1, 1); Flip = true; }
    }

    // 開啟 V2 戰鬥（單一入口，取代舊版三段重複開戰）
    private void PrepareBattleWithEnemy(Transform enemy)
    {
        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent == null) return;

        EnemyType enemyType = enemyComponent.enemyType;
        BattleLog.Log($"準備進入戰鬥: {enemyType}");

        TurnBaseBattlePlayerData playerData = new TurnBaseBattlePlayerData().InitPlayerInfo(CharacterType.Seraphis);

        // 登記這場戰鬥的敵人給地圖回合管理器（同物件），勝利後精準移除
        var mapTurn = GetComponent<MapTurnBaseManager>();
        if (mapTurn != null) mapTurn.SetBattleEnemy(enemy);

        EnableBlocking();
        if (BattleController.Instance != null)
            BattleController.Instance.StartStoryBattle(playerData, enemyType, true);
        else
            UnityEngine.Debug.LogWarning("[PlayerController] 場上找不到 BattleController，無法開始 V2 戰鬥。");

        AgainbattleInStory = false; // 首戰後標記
    }

    // 碰撞位移（撞擊時的前後小位移表現）
    private IEnumerator MovePlayer()
    {
        Vector3 startPosition = transform.position;
        if (!Flip)
        {
            Vector3 backwardPosition = startPosition - transform.right * backwardDistance;
            Vector3 forwardPosition = backwardPosition + transform.right * forwardDistance;
            yield return StartCoroutine(MoveToPosition(startPosition, backwardPosition));
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(MoveToPosition(backwardPosition, forwardPosition));
        }
        else
        {
            Vector3 forwardPosition = startPosition + transform.right * backwardDistance;
            Vector3 backwardPosition = forwardPosition - transform.right * forwardDistance;
            yield return StartCoroutine(MoveToPosition(startPosition, forwardPosition));
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(MoveToPosition(backwardPosition, backwardPosition));
        }
    }

    private IEnumerator MoveToPosition(Vector3 start, Vector3 end)
    {
        float elapsed = 0f;
        float duration = Vector3.Distance(start, end) / HitSpeed;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = end;
    }

    private void PlayMoveSFX()
    {
        if (playerMoveSFX != null && !playerMoveSFX.isPlaying) playerMoveSFX.Play();
    }

    private void StopMoveSFX()
    {
        if (playerMoveSFX != null && playerMoveSFX.isPlaying) playerMoveSFX.Stop();
    }

    // 依 x 位移變化播放/停止移動音效
    private void DetectPositionChangeAndPlaySFX()
    {
        float currentPositionX = transform.position.x;
        if (!Mathf.Approximately(currentPositionX, previousPositionX)) PlayMoveSFX();
        else StopMoveSFX();
        previousPositionX = currentPositionX;
    }

    private void FadeToDark()
    {
        if (canvasGroup == null) { UnityEngine.Debug.LogWarning("CanvasGroup 未設置！"); return; }
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.Linear);
    }

    private void StartL1Transition()
    {
        if (canvasGroup == null) { UnityEngine.Debug.LogWarning("CanvasGroup 未設置！"); return; }
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.DOFade(0f, fadeDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => canvasGroup.gameObject.SetActive(false));
    }
}
