using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 進入地圖時的「玩家跑關狀態」初始化與單一來源（由 A_Good_Ink 使用 AI 生成）。
///
/// 一個地方集中指定進場的：血量、金錢、道具，以及「主角在地圖上先移動還是後移動」。
/// 建議掛在地圖主控物件（LevelMapManager）上，屬地圖主要控制功能之一。
///
/// 設計為「來源(source of truth) + 讀取」：本元件持有數值並對外開放讀寫，其他系統各自向它取值初始化：
///   - <see cref="MapTurnBaseManager"/> 於 Start 讀 <see cref="PlayerMovesFirst"/> 決定地圖先手/後手；
///   - ShopSystem 於 Awake 讀 <see cref="Money"/> 當起始金錢；
///   - PlayerMapStatus_UI 讀 <see cref="Hp"/>/<see cref="MaxHp"/> 顯示血量；
///   - 日後背包系統可讀 <see cref="Items"/> 放入起始道具。
/// 用 <see cref="DefaultExecutionOrder"/> 讓本元件的 Awake 早於其他，確保 Instance 先就緒。
/// </summary>
[DefaultExecutionOrder(-100)]
public class LevelMapInitializer : MonoBehaviour
{
    public static LevelMapInitializer Instance { get; private set; }

    /// <summary>地圖上誰先移動。</summary>
    public enum MapFirstMover { Player, Enemy }

    [Header("血量")]
    [Tooltip("最大血量")]
    [SerializeField] private int maxHp = 16;
    [Tooltip("進場當前血量（會被夾在 0~最大血量）")]
    [SerializeField] private int hp = 16;

    [Header("金錢")]
    [SerializeField] private int money = 100;

    [Header("道具（起始）")]
    [Tooltip("進場時放入的道具；日後背包系統讀 Items 放進去。")]
    [SerializeField] private List<StartItem> items = new List<StartItem>();

    [Header("地圖先手 / 後手")]
    [Tooltip("進入地圖時，主角先移動(Player) 還是敵人/怪物先移動(Enemy)。")]
    [SerializeField] private MapFirstMover firstMover = MapFirstMover.Player;

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
    public IReadOnlyList<StartItem> Items => items;
    public bool PlayerMovesFirst => firstMover == MapFirstMover.Player;

    /// <summary>初始化就緒（Awake 後）觸發；日後系統可訂閱以重讀初值。</summary>
    public event Action Initialized;
    /// <summary>金錢變動時觸發（供 UI/存檔同步）。</summary>
    public event Action<int> MoneyChanged;
    /// <summary>血量變動時觸發（供 UI 同步）。</summary>
    public event Action<int, int> HpChanged; // (current, max)

    // ── 執行期可變（給拾取/購買/受傷等改狀態）──
    public void SetHp(int value) { hp = Mathf.Clamp(value, 0, maxHp); HpChanged?.Invoke(hp, maxHp); }
    public void ChangeHp(int delta) => SetHp(hp + delta);
    public void SetMoney(int value) { money = Mathf.Max(0, value); MoneyChanged?.Invoke(money); }
    public void ChangeMoney(int delta) => SetMoney(money + delta);

    public void AddItem(string itemName, int count = 1, Sprite icon = null)
    {
        if (string.IsNullOrEmpty(itemName) || count <= 0) return;
        var found = items.Find(i => i.itemName == itemName);
        if (found != null) found.count += count;
        else items.Add(new StartItem { itemName = itemName, count = count, icon = icon });
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[LevelMapInitializer] 場上已有另一個實例，保留先出現的：{Instance.name}");
            return;
        }
        Instance = this;
        hp = Mathf.Clamp(hp, 0, Mathf.Max(1, maxHp)); // 保險：當前血量不超過最大
        Initialized?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
