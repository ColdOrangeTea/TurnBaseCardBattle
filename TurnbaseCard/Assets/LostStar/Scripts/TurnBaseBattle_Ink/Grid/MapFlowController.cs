using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums;
using TurnBaseBattleV2;

/// <summary>地圖探索的流程狀態。</summary>
public enum MapFlowState
{
    FreeControl,    // 自由探索：可點格移動（且輪到玩家回合）
    Moving,         // 移動中／忙碌但非事件：禁點
    InEvent,        // 事件中（戰鬥/寶箱/商店/任務）：禁點
    SceneTransition // 切關/轉場中：禁點
}

/// <summary>
/// 地圖探索的流程總控（單一權威）。集中管理「玩家現在能不能動」：
///   - 依狀態鎖 / 放 <see cref="S001_PlayerController"/> 的地圖點擊；
///   - 訂閱既有的「完成」訊號（<see cref="BattleController.BattleFinished"/>、
///     <see cref="ShopSystem.OnShopClosed"/>、<see cref="TreasureChest.TreasureClosed"/>）自動收尾，
///     不必再靠各事件各自記得放行玩家；
///   - 在流程各時機呼叫 <see cref="MapFlowHookBase"/> 掛件，插入動畫/UI/劇情等表現。
///
/// 相容性：若場上沒有本元件，<see cref="Instance"/> 為 null，S001 會自動走舊的分散式流程，
/// 因此可漸進導入、不影響未接入的場景。由 A_Good_Ink 使用 AI 生成。
/// </summary>
public class MapFlowController : MonoBehaviour
{
    public static MapFlowController Instance { get; private set; }

    [Header("接線（留空會在場上自動尋找）")]
    [SerializeField] private S001_PlayerController player;
    [SerializeField] private MapEventService eventService;
    [Tooltip("完成訊號來源；沒有商店/寶箱的場景可留空。")]
    [SerializeField] private ShopSystem shop;
    [SerializeField] private TreasureChest treasure;

    [Header("表現掛件（可空；依序執行）")]
    [Tooltip("流程各時機要插入的動畫/UI/劇情掛件；留空會自動抓場上所有 MapFlowHookBase。")]
    [SerializeField] private List<MapFlowHookBase> hooks = new List<MapFlowHookBase>();

    public MapFlowState State { get; private set; } = MapFlowState.FreeControl;

    // 事件完成旗標：由訂閱的完成事件設定，RunEvent 協程據此等待
    private bool battleFinishedFlag;
    private bool uiEventClosedFlag;

    private bool subscribedBattle;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[MapFlowController] 場上已有另一個實例，保留先出現的：{Instance.name}");
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        // 回合匯流排是靜態事件，OnEnable 訂閱最安全（早於各腳本 Start）
        MapTurnBaseEvent.OnTurnInfoSent += OnTurnInfoSent;
    }

    void OnDisable()
    {
        MapTurnBaseEvent.OnTurnInfoSent -= OnTurnInfoSent;
    }

    void Start()
    {
        if (player == null) player = FindAnyObjectByType<S001_PlayerController>();
        if (eventService == null) eventService = FindAnyObjectByType<MapEventService>();
        if (shop == null) shop = FindAnyObjectByType<ShopSystem>();
        if (treasure == null) treasure = FindAnyObjectByType<TreasureChest>();
        if (hooks == null || hooks.Count == 0)
            hooks = new List<MapFlowHookBase>(FindObjectsByType<MapFlowHookBase>(FindObjectsSortMode.None));

        // 訂閱各事件的「完成」訊號（Instance 於各自 Awake 設定，晚於本 Start 也沒關係——用實例事件）
        if (BattleController.Instance != null)
        {
            BattleController.Instance.BattleFinished += OnBattleFinished;
            subscribedBattle = true;
        }
        if (shop != null) shop.OnShopClosed += OnUiEventClosed;
        if (treasure != null) treasure.TreasureClosed += OnUiEventClosed;

        if (player == null) Debug.LogWarning("[MapFlowController] 找不到 S001_PlayerController，無法控制玩家輸入。");
    }

    void OnDestroy()
    {
        if (subscribedBattle && BattleController.Instance != null)
            BattleController.Instance.BattleFinished -= OnBattleFinished;
        if (shop != null) shop.OnShopClosed -= OnUiEventClosed;
        if (treasure != null) treasure.TreasureClosed -= OnUiEventClosed;
        if (Instance == this) Instance = null;
    }

    // ────────────────────────────────────────────────────────────────
    // 對外 API：由 S001 在流程各點呼叫
    // ────────────────────────────────────────────────────────────────

    /// <summary>玩家開始沿路徑移動。</summary>
    public void NotifyMoveStarted()
    {
        if (State == MapFlowState.InEvent) return; // 事件中不接受移動狀態覆寫
        SetState(MapFlowState.Moving);
    }

    /// <summary>玩家開始切換到下一個 Stage（切關轉場）。</summary>
    public void NotifySceneTransition() => SetState(MapFlowState.SceneTransition);

    /// <summary>撞敵/踏格開戰時呼叫：標記進入事件（戰鬥的收尾由 BattleFinished 帶動）。</summary>
    public void NotifyBattleStarted()
    {
        battleFinishedFlag = false;
        SetState(MapFlowState.InEvent);
        FireBattleStarted();
    }

    /// <summary>此事件類型是否需要「玩家介入、要等它結束」（會鎖住流程）。</summary>
    public static bool IsActionableEvent(GridEventType type)
        => type == GridEventType.BossCombat || type == GridEventType.Shop
        || type == GridEventType.Treasure || type == GridEventType.Event
        || type == GridEventType.quest;

    /// <summary>
    /// 執行一格的事件：鎖玩家 → 播 OnBeforeEvent 掛件 → 開事件 → 等它結束 → 播 OnAfterEvent →
    /// 收尾（戰鬥交由回合系統轉玩家回合；其餘事件結束後換敵人回合）。由 S001 以 yield return 呼叫。
    /// </summary>
    public IEnumerator RunEvent(EventGrid grid)
    {
        if (grid == null) yield break;
        GridEventType type = grid.eventType;

        SetState(MapFlowState.InEvent);
        yield return RunHooks(h => h.OnBeforeEvent(type, grid)); // 演出先播完，才真的開事件

        battleFinishedFlag = false;
        uiEventClosedFlag = false;

        bool isBattle = eventService != null && eventService.TriggerGridEvent(grid);

        if (isBattle)
        {
            FireBattleStarted();                                       // BossCombat 事件格開戰
            yield return new WaitUntil(() => battleFinishedFlag);      // 等戰鬥結束
        }
        else if (IsBlockingUiEvent(type))
            yield return new WaitUntil(() => uiEventClosedFlag);        // 等寶箱/商店關閉
        // 其餘（Event/quest 空殼、無 UI）不等待

        yield return RunHooks(h => h.OnAfterEvent(type, grid));

        // 離開事件狀態，交還給回合系統（必須在送回合訊號前，否則 OnTurnInfoSent 的 InEvent 防呆會擋掉放行）
        SetState(MapFlowState.Moving);

        // 收尾：戰鬥由 MapTurnBaseManager 在返回時送 PlayerTurn；非戰鬥事件結束後換敵人回合
        if (!isBattle)
            new MapTurnBaseEvent().ChangeTurn(MapTurnBaseType.EnemyTurn, player != null ? player.moveSpeed : 0f);
    }

    // ────────────────────────────────────────────────────────────────
    // 事件訂閱
    // ────────────────────────────────────────────────────────────────

    private void OnTurnInfoSent(MapTurnBaseType turn)
    {
        if (turn == MapTurnBaseType.PlayerTurn)
        {
            if (State == MapFlowState.InEvent) return; // 事件進行中，不因回合訊號提前放行
            SetState(MapFlowState.FreeControl);
        }
        else // EnemyTurn
        {
            SetState(MapFlowState.Moving); // 敵人回合期間鎖住玩家
        }
    }

    private void OnBattleFinished(bool playerWin)
    {
        battleFinishedFlag = true;
        FireBattleEnded(playerWin);   // 讓掛件收尾（如把戰鬥 BGM 還原成地圖/商店 BGM）
        // 戰鬥結束：離開 InEvent（改為忙碌待返回），讓稍後 MapTurnBaseManager 送出的 PlayerTurn 能放行玩家
        if (State == MapFlowState.InEvent) SetState(MapFlowState.Moving);
    }

    private void FireBattleStarted()
    {
        foreach (var h in hooks) if (h != null && h.isActiveAndEnabled) h.OnBattleStarted();
    }

    private void FireBattleEnded(bool playerWin)
    {
        foreach (var h in hooks) if (h != null && h.isActiveAndEnabled) h.OnBattleEnded(playerWin);
    }

    private void OnUiEventClosed() => uiEventClosedFlag = true;

    private static bool IsBlockingUiEvent(GridEventType type)
        => type == GridEventType.Shop || type == GridEventType.Treasure;

    // ────────────────────────────────────────────────────────────────
    // 狀態切換 + 掛件
    // ────────────────────────────────────────────────────────────────

    private void SetState(MapFlowState next)
    {
        if (State == next)
        {
            ApplyInputLock(next); // 保險：即使狀態沒變也維持正確的鎖/放
            return;
        }
        MapFlowState prev = State;
        State = next;
        ApplyInputLock(next);

        foreach (var h in hooks) if (h != null) h.OnStateChanged(prev, next);
        if (next == MapFlowState.FreeControl)
            foreach (var h in hooks) if (h != null) h.OnEnterFreeControl();
    }

    private void ApplyInputLock(MapFlowState s)
    {
        if (player == null) return;
        if (s == MapFlowState.FreeControl) player.EnablePlayerInput();
        else player.DisablePlayerInputForCheck();
    }

    // 依序執行所有掛件的同一階段（會等每個播完再跑下一個）
    private IEnumerator RunHooks(Func<MapFlowHookBase, IEnumerator> phase)
    {
        if (hooks == null) yield break;
        foreach (var h in hooks)
        {
            if (h == null || !h.isActiveAndEnabled) continue;
            IEnumerator routine = phase(h);
            if (routine != null) yield return routine;
        }
    }
}
