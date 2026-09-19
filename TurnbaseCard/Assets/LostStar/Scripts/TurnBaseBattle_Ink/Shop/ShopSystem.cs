using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 商店事件（改接 MapEventService 的重構版，由 A_Good_Ink 使用 AI 生成）。
///
/// 玩家走到「Shop」事件格 → MapEventService 觸發 <see cref="MapEventService.ShopRequested"/>
/// → 本元件開啟商店 UI（沿用 ShopEmpty prefab），把商品填入各欄，可購買、可看 tooltip、按離開關閉。
///
/// 與舊版差異：不再依賴 V2 重構已移除/擱置的系統（Item / ItemManager / PlayerInventory /
/// DialogueManager / DialogueOpenClose / BattleButtonFunction / QuestManager）。
/// 金幣改為本元件本地持有（<see cref="gold"/>），購買時以事件 <see cref="ItemPurchased"/> 對外拋出，
/// 日後背包/存檔系統訂閱即可實際入袋與同步金幣；商品用輕量的 <see cref="ShopItem"/> 表示。
/// </summary>
public class ShopSystem : MonoBehaviour
{
    [Header("接線（留空會在場上自動尋找）")]
    [SerializeField] private MapEventService mapEventService;
    [Tooltip("開商店時暫停地圖點擊、關閉後恢復；可留空。")]
    [SerializeField] private S001_PlayerController playerController;

    [Header("UI")]
    [SerializeField] private GameObject shopUI;                 // 商店面板根（開/關）
    [SerializeField] private Button closeButton;                // 離開
    [SerializeField] private TMP_Text goldText;                 // 金幣顯示
    [SerializeField] private TMP_Text messageText;              // 店員訊息（可空）

    [Header("商品欄（動態生成）")]
    [Tooltip("商品欄樣板 prefab；子物件需含 Price(TMP)、Item_Picture(Image)、BuyButton(Button)")]
    [SerializeField] private GameObject itemSlotPrefab;
    [Tooltip("商品欄生成的容器（建議掛 Horizontal/GridLayoutGroup 排版）")]
    [SerializeField] private Transform itemSlotContainer;
    [Tooltip("每次開店上架的商品數量")]
    [SerializeField] private int itemCount = 3;

    // 執行期生成的商品欄（每次開店先清掉）
    private readonly List<GameObject> spawnedSlots = new List<GameObject>();

    [Header("Tooltip（可空）")]
    [SerializeField] private GameObject tooltipUI;
    [SerializeField] private TMP_Text tooltipNameText;
    [SerializeField] private TMP_Text tooltipDescriptionText;

    [Header("音效（可空）")]
    [SerializeField] private AudioSource buyAudio;
    [SerializeField] private AudioSource buyFailedAudio;

    [Header("經濟 / 商品")]
    [Tooltip("本地金幣（日後與背包/存檔同步）")]
    [SerializeField] private int gold = 100;
    [Tooltip("商品庫存；每次開店隨機取 shopItemSlots.Count 件上架")]
    [SerializeField] private List<ShopItem> stock = new List<ShopItem>();

    /// <summary>輕量商品：名稱／說明／圖示／價格。之後接背包時再對應到真正的道具資料。</summary>
    [Serializable]
    public class ShopItem
    {
        public string itemName;
        [TextArea] public string description;
        public Sprite icon;
        public int price = 10;
    }

    // ── 對外事件（日後背包/存檔系統訂閱即可實際發放與扣款）──
    public event Action<ShopItem> ItemPurchased;
    public event Action OnShopClosed;

    private bool isTooltipActive;

    private void Awake()
    {
        if (mapEventService == null) mapEventService = FindAnyObjectByType<MapEventService>();
        if (playerController == null) playerController = FindAnyObjectByType<S001_PlayerController>();
        if (shopUI != null) shopUI.SetActive(false);
        if (tooltipUI != null) tooltipUI.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(CloseShop);
        UpdateGoldText();
    }

    private void OnEnable()
    {
        if (mapEventService != null) mapEventService.ShopRequested += OnShopRequested;
    }

    private void OnDisable()
    {
        if (mapEventService != null) mapEventService.ShopRequested -= OnShopRequested;
    }

    private void Update()
    {
        if (isTooltipActive && tooltipUI != null)
            tooltipUI.transform.position = Input.mousePosition + new Vector3(10, 10, 0);
    }

    private void OnShopRequested(EventGrid grid) => OpenShop();

    /// <summary>開啟商店：暫停地圖點擊、上架商品。</summary>
    public void OpenShop()
    {
        if (shopUI != null) shopUI.SetActive(true);
        if (playerController != null) playerController.DisablePlayerInputForCheck();
        DisplayShopItems();
    }

    /// <summary>關閉商店：恢復地圖點擊。</summary>
    public void CloseShop()
    {
        HideTooltip();
        if (shopUI != null) shopUI.SetActive(false);
        if (playerController != null) playerController.EnablePlayerInput();
        OnShopClosed?.Invoke();
        BattleLog.Log("[ShopSystem] 關閉商店");
    }

    private void DisplayShopItems()
    {
        if (messageText != null) messageText.text = "歡迎光臨！挑挑看，選選看啊！";

        if (itemSlotPrefab == null || itemSlotContainer == null)
        {
            Debug.LogWarning($"[{name}] 未指派 itemSlotPrefab 或 itemSlotContainer，無法生成商品欄。");
            return;
        }

        // 清空容器內既有商品欄（含編輯器裡放的設計預覽 + 上次生成的）
        for (int i = itemSlotContainer.childCount - 1; i >= 0; i--)
        {
            var child = itemSlotContainer.GetChild(i).gameObject;
            child.SetActive(false);   // 立即隱藏，避免同幀被 Layout 一起排到
            Destroy(child);
        }
        spawnedSlots.Clear();

        List<ShopItem> forSale = GetItemsForSale(Mathf.Max(0, itemCount));
        foreach (ShopItem item in forSale)
        {
            GameObject slot = Instantiate(itemSlotPrefab, itemSlotContainer);
            slot.SetActive(true);
            spawnedSlots.Add(slot);

            var priceText = FindChild<TMP_Text>(slot.transform, "Price");
            var icon = FindChild<Image>(slot.transform, "Item_Picture");
            var buyButton = FindChild<Button>(slot.transform, "BuyButton");

            if (priceText != null) priceText.text = item.price.ToString();
            if (icon != null && item.icon != null) icon.sprite = item.icon;

            AddTooltipHandler(slot, item);

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                ShopItem captured = item;
                GameObject capturedSlot = slot;
                buyButton.onClick.AddListener(() => BuyItem(captured, capturedSlot));
            }
        }
    }

    public void BuyItem(ShopItem item, GameObject slot)
    {
        if (gold >= item.price)
        {
            gold -= item.price;
            UpdateGoldText();
            if (slot != null) slot.SetActive(false);
            if (messageText != null) messageText.text = "太好了，相信你一定會喜歡這個商品的！";
            if (buyAudio != null) buyAudio.Play();
            ItemPurchased?.Invoke(item);
            BattleLog.Log($"[ShopSystem] 購買 {item.itemName}（-{item.price}），剩餘金幣 {gold}");
            HideTooltip();
        }
        else
        {
            if (messageText != null) messageText.text = "要買不買的，還沒有足夠的錢啊！";
            if (buyFailedAudio != null) buyFailedAudio.Play();
            BattleLog.Log($"[ShopSystem] 金幣不足，無法購買 {item.itemName}");
        }
    }

    private void UpdateGoldText()
    {
        if (goldText != null) goldText.text = gold.ToString();
    }

    private List<ShopItem> GetItemsForSale(int count)
    {
        var source = (stock != null && stock.Count > 0) ? stock : DefaultStock();
        var result = new List<ShopItem>();
        for (int i = 0; i < count; i++)
            result.Add(source[UnityEngine.Random.Range(0, source.Count)]);
        return result;
    }

    // 未設定 stock 時的預設商品（讓範例商店即可運作；日後改用真正的道具資料）
    private static List<ShopItem> DefaultStock() => new List<ShopItem>
    {
        new ShopItem { itemName = "治療藥水", description = "恢復少量生命值。", price = 20 },
        new ShopItem { itemName = "骰子卷軸", description = "本場戰鬥多一顆骰子。", price = 35 },
        new ShopItem { itemName = "護身符", description = "抵擋一次負面狀態。", price = 50 },
    };

    #region Tooltip
    private void AddTooltipHandler(GameObject slot, ShopItem item)
    {
        if (tooltipUI == null) return;
        var trigger = slot.GetComponent<EventTrigger>() ?? slot.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowTooltip(item));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideTooltip());
        trigger.triggers.Add(exit);
    }

    private void ShowTooltip(ShopItem item)
    {
        if (tooltipUI == null) return;
        tooltipUI.SetActive(true);
        if (tooltipNameText != null) tooltipNameText.text = item.itemName;
        if (tooltipDescriptionText != null) tooltipDescriptionText.text = item.description;
        isTooltipActive = true;
    }

    private void HideTooltip()
    {
        if (tooltipUI != null) tooltipUI.SetActive(false);
        isTooltipActive = false;
    }
    #endregion

    private static T FindChild<T>(Transform parent, string childName) where T : Component
    {
        Transform t = parent.Find(childName);
        return t != null ? t.GetComponent<T>() : null;
    }
}
