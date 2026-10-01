using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 進入地圖前的「玩家跑關狀態」初始化與單一來源（由 A_Good_Ink 使用 AI 生成）。
///
/// 使用情境：在「選擇地圖」的畫面設定好本關要帶入的：血量、金錢、道具，以及戰鬥時玩家先攻/後攻，
/// 再載入地圖場景。因此本元件設計為「跨場景保留」的常駐單例（<see cref="DontDestroyOnLoad"/>），
/// 不掛在地圖場景的物件上；建議做成獨立 prefab，放在選地圖／進入點的場景，載入地圖後仍存活。
///
/// 設計為「來源(source of truth) + 讀取」：本元件持有數值並對外開放讀寫，其他系統各自向它取值初始化：
///   - 戰鬥開場（MapEventService / MapTurnBaseManager 呼叫 BattleController.StartStoryBattle）讀
///     <see cref="PlayerAttacksFirst"/> 決定戰鬥內先攻/後攻；
///   - ShopSystem 於 Awake 讀 <see cref="Money"/> 當起始金錢；
///   - PlayerMapStatus_UI 讀 <see cref="Hp"/>/<see cref="MaxHp"/> 顯示血量；
///   - 日後背包系統可讀 <see cref="Items"/> 放入起始道具。
/// 找不到本元件（例如直接開地圖場景測試）時，各系統一律沿用自己的預設值。
/// </summary>
[DefaultExecutionOrder(-1000)] // 中樞的 Awake 先於各系統的 Awake/OnEnable，確保 Instance 先就緒可供訂閱
public class LevelMapInitializer : MonoBehaviour
{
    public static LevelMapInitializer Instance { get; private set; }

    /// <summary>戰鬥開場由誰先攻。</summary>
    public enum BattleFirstAttacker { Player, Enemy }

    [Header("血量")]
    [Tooltip("最大血量")]
    [SerializeField] private int maxHp = 16;
    [Tooltip("進場當前血量（會被夾在 0~最大血量）")]
    [SerializeField] private int hp = 16;

    [Header("金錢")]
    [SerializeField] private int money = 100;

    [Header("骰子")]
    [Tooltip("最大骰子數（戰鬥用；Buff-AddMaxDice 會增加）")]
    [SerializeField] private int maxDice = 2;

    [Header("道具（起始）")]
    [Tooltip("（舊·輕量）進場道具名稱/數量/圖示；已由下方 startingItems(Item SO) 取代供背包用，保留不動以相容舊資料。")]
    [SerializeField] private List<StartItem> items = new List<StartItem>();

    [Header("道具背包（Item SO，跨場景保留）")]
    [Tooltip("進場種子道具（真正的 Item SO）；Awake 時種入背包，超過上限的會被捨去。")]
    [SerializeField] private List<Item> startingItems = new List<Item>();

    /// <summary>背包持有上限。</summary>
    public const int BackpackCapacity = 5;

    // 執行期背包（跨場景保留）——本中樞為道具的單一真相源。
    private readonly List<Item> heldItems = new List<Item>();

    [Header("戰鬥先攻 / 後攻")]
    [Tooltip("戰鬥開場時，玩家先攻(Player) 還是敵人先攻(Enemy)。")]
    [SerializeField] private BattleFirstAttacker battleFirstAttacker = BattleFirstAttacker.Player;

    /// <summary>輕量起始道具：名稱／數量／圖示。之後接背包時再對應到真正的道具資料。</summary>
    [Serializable]
    public class StartItem
    {
        public string itemName;
        [Min(1)] public int count = 1;
        public Sprite icon;
    }

    // ── 對外讀取 ──
    public int MaxHp => maxHp;
    public int Hp => hp;
    public int Money => money;
    public int MaxDice => maxDice;
    public IReadOnlyList<StartItem> Items => items;
    /// <summary>戰鬥開場玩家是否先攻。</summary>
    public bool PlayerAttacksFirst => battleFirstAttacker == BattleFirstAttacker.Player;

    // ── 道具背包（Item SO；本中樞為唯一真相源）──
    /// <summary>目前持有的道具（唯讀）。</summary>
    public IReadOnlyList<Item> HeldItems => heldItems;
    /// <summary>目前持有數量。</summary>
    public int ItemCount => heldItems.Count;
    /// <summary>背包是否已滿。</summary>
    public bool IsBackpackFull => heldItems.Count >= BackpackCapacity;

    /// <summary>初始化就緒（Awake 後）觸發；日後系統可訂閱以重讀初值。</summary>
    public event Action Initialized;
    /// <summary>金錢變動時觸發（供 UI/存檔同步）。</summary>
    public event Action<int> MoneyChanged;
    /// <summary>血量變動時觸發（供 UI 同步）。</summary>
    public event Action<int, int> HpChanged; // (current, max)
    /// <summary>最大骰子數變動時觸發（供 UI 同步）。</summary>
    public event Action<int> DiceChanged; // (maxDice)
    /// <summary>背包內容變動時觸發（新增/移除，供背包 UI 重繪）。</summary>
    public event Action ItemsChanged;
    /// <summary>背包已滿、加入的道具被自動放棄時觸發（供提示 UI）。</summary>
    public event Action<Item> ItemDiscarded;

    // ── 執行期可變（給拾取/購買/受傷等改狀態）──
    public void SetHp(int value) { hp = Mathf.Clamp(value, 0, maxHp); HpChanged?.Invoke(hp, maxHp); }
    public void ChangeHp(int delta) => SetHp(hp + delta);
    public void SetMoney(int value) { money = Mathf.Max(0, value); MoneyChanged?.Invoke(money); }
    public void ChangeMoney(int delta) => SetMoney(money + delta);

    /// <summary>增加最大生命值（Buff-AddMaxHp）：提高上限，並同步回復相同的現有血量。</summary>
    public void ChangeMaxHp(int delta)
    {
        maxHp = Mathf.Max(1, maxHp + delta);
        hp = Mathf.Clamp(hp + Mathf.Max(0, delta), 0, maxHp); // 加最大血同時回該量血
        HpChanged?.Invoke(hp, maxHp);
    }

    /// <summary>增加最大骰子數（Buff-AddMaxDice）。</summary>
    public void ChangeMaxDice(int delta)
    {
        maxDice = Mathf.Max(1, maxDice + delta);
        DiceChanged?.Invoke(maxDice);
    }

    /// <summary>把中樞的 HP/最大血/骰子套到戰鬥開場的 playerData，讓地圖上的數值（含 Buff）帶進戰鬥。</summary>
    public void ApplyToBattleData(TurnBaseBattlePlayerData data)
    {
        if (data == null) return;
        data.SetOriginMaxHp(maxHp);
        data.SetCurHp(hp);
        data.SetOriginMaxCountOfDice(maxDice);
        data.SetCountOfDice(maxDice);
    }

    /// <summary>戰鬥結束後把玩家殘存 HP 寫回中樞（供戰後地圖顯示與下一戰延續）。</summary>
    public void WriteBackBattleHp(int remainingHp) => SetHp(remainingHp);

    public void AddItem(string itemName, int count = 1, Sprite icon = null)
    {
        if (string.IsNullOrEmpty(itemName) || count <= 0) return;
        var found = items.Find(i => i.itemName == itemName);
        if (found != null) found.count += count;
        else items.Add(new StartItem { itemName = itemName, count = count, icon = icon });
    }

    // ── 道具背包操作（Item SO）──
    /// <summary>
    /// 加入一個道具到背包。背包已滿（達 <see cref="BackpackCapacity"/>）時不加入、觸發
    /// <see cref="ItemDiscarded"/>（視為自動放棄）並回傳 false；成功加入回傳 true 並觸發 <see cref="ItemsChanged"/>。
    /// </summary>
    public bool AddItem(Item item)
    {
        if (item == null) return false;
        if (heldItems.Count >= BackpackCapacity)
        {
            ItemDiscarded?.Invoke(item); // 溢出：自動放棄，交由 UI 提示
            return false;
        }
        heldItems.Add(item);
        ItemsChanged?.Invoke();
        return true;
    }

    /// <summary>從背包移除指定道具（使用掉或賣出）；成功回傳 true 並觸發 <see cref="ItemsChanged"/>。</summary>
    public bool RemoveItem(Item item)
    {
        if (item == null) return false;
        if (heldItems.Remove(item))
        {
            ItemsChanged?.Invoke();
            return true;
        }
        return false;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 已有常駐實例（例如從選地圖畫面帶進來的），這個重複的就移除。
            Debug.LogWarning($"[LevelMapInitializer] 已存在常駐實例，移除重複的：{name}");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 跨場景保留：選地圖時設定，載入地圖後仍存活
        hp = Mathf.Clamp(hp, 0, Mathf.Max(1, maxHp)); // 保險：當前血量不超過最大

        // 種入起始道具（真正的 Item SO），夾在背包上限內
        heldItems.Clear();
        if (startingItems != null)
            foreach (var it in startingItems)
                if (it != null && heldItems.Count < BackpackCapacity) heldItems.Add(it);

        Initialized?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
