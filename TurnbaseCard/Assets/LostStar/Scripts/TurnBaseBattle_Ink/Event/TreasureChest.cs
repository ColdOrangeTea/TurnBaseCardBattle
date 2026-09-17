using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 寶箱事件（改接 MapEventService 的重構版，由 A_Good_Ink 使用 AI 生成）。
///
/// 玩家走到「Treasure」事件格 → MapEventService 觸發 <see cref="MapEventService.TreasureRequested"/>
/// → 本元件開啟寶箱 UI、隨機給金幣或道具、按確定關閉。
///
/// 與舊版差異：不再依賴 V2 重構已移除的系統（Item / PlayerInventory / DialogueManager /
/// DialogueOpenClose / PlayerStatsManager / BattleButtonFunction / Effect）。獎勵發放改用事件
/// (<see cref="GoldRewarded"/> / <see cref="ItemRewarded"/>) 對外拋出，日後背包/存檔系統訂閱即可實際入袋，
/// 本元件本身不綁定背包；道具用輕量的 <see cref="ItemReward"/> 表示，不依賴舊 Item 類別。
/// </summary>
public class TreasureChest : MonoBehaviour
{
    [Header("接線（留空會在場上自動尋找）")]
    [SerializeField] private MapEventService mapEventService;
    [Tooltip("開寶箱時暫停地圖點擊、關閉後恢復；可留空。")]
    [SerializeField] private S001_PlayerController playerController;

    [Header("寶箱 UI")]
    [SerializeField] private GameObject treasureUI;   // 寶箱面板根（開/關）
    [SerializeField] private TMP_Text rewardText;     // 獎勵說明文字
    [SerializeField] private Image itemImage;         // 道具圖示（給金幣時隱藏）
    [SerializeField] private Button okButton;         // 確定關閉

    [Header("獎勵設定")]
    [SerializeField] private int minGoldReward = 10;
    [SerializeField] private int maxGoldReward = 50;
    [Tooltip("可能獲得的道具（輕量表示，不依賴舊 Item 系統）")]
    [SerializeField] private List<ItemReward> possibleItems = new List<ItemReward>();

    /// <summary>輕量道具獎勵：名稱＋圖示。之後接背包時再對應到真正的道具資料。</summary>
    [Serializable]
    public class ItemReward
    {
        public string itemName;
        public Sprite icon;
    }

    // ── 對外事件（日後背包/存檔系統訂閱即可實際發放；本元件只顯示與拋出）──
    public event Action<int> GoldRewarded;
    public event Action<ItemReward> ItemRewarded;
    public event Action TreasureClosed;

    private void Awake()
    {
        if (mapEventService == null) mapEventService = FindAnyObjectByType<MapEventService>();
        if (playerController == null) playerController = FindAnyObjectByType<S001_PlayerController>();
        if (treasureUI != null) treasureUI.SetActive(false);
        if (okButton != null) okButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        if (mapEventService != null) mapEventService.TreasureRequested += OnTreasureRequested;
    }

    private void OnDisable()
    {
        if (mapEventService != null) mapEventService.TreasureRequested -= OnTreasureRequested;
    }

    private void OnTreasureRequested(EventGrid grid) => Open();

    /// <summary>開啟寶箱：暫停地圖點擊、隨機給金幣或道具並顯示。</summary>
    public void Open()
    {
        if (treasureUI != null) treasureUI.SetActive(true);
        if (playerController != null) playerController.DisablePlayerInputForCheck(); // 看寶箱時先別讓玩家點格子

        bool giveItem = possibleItems != null && possibleItems.Count > 0 && UnityEngine.Random.value < 0.5f;
        if (giveItem) GiveItem();
        else GiveGold();
    }

    private void GiveGold()
    {
        int gold = UnityEngine.Random.Range(minGoldReward, maxGoldReward + 1);
        if (itemImage != null) itemImage.gameObject.SetActive(false);
        if (rewardText != null) rewardText.text = $"獲得 {gold} 金幣！";
        GoldRewarded?.Invoke(gold);
        BattleLog.Log($"[TreasureChest] 獲得 {gold} 金幣");
    }

    private void GiveItem()
    {
        ItemReward item = possibleItems[UnityEngine.Random.Range(0, possibleItems.Count)];
        if (itemImage != null)
        {
            itemImage.gameObject.SetActive(item.icon != null);
            if (item.icon != null) itemImage.sprite = item.icon;
        }
        if (rewardText != null) rewardText.text = $"獲得道具：{item.itemName}";
        ItemRewarded?.Invoke(item);
        BattleLog.Log($"[TreasureChest] 獲得道具：{item.itemName}");
    }

    /// <summary>確定：關閉寶箱 UI、恢復地圖點擊。</summary>
    public void Close()
    {
        if (treasureUI != null) treasureUI.SetActive(false);
        if (playerController != null) playerController.EnablePlayerInput();
        TreasureClosed?.Invoke();
        BattleLog.Log("[TreasureChest] 關閉寶箱");
    }
}
