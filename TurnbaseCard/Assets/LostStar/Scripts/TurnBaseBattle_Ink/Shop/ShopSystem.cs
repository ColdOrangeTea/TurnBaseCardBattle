using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 商店事件（接 MapEventService，改用真正的 Item SO；由 A_Good_Ink 使用 AI 生成/重構）。
///
/// 玩家走到「Shop」事件格 → MapEventService 觸發 <see cref="MapEventService.ShopRequested"/>
/// → 本元件開啟商店 UI（沿用 ShopEmpty prefab），從 <see cref="itemDatabase"/> 隨機上架商品
/// （排除 <see cref="ItemEffectType.Special"/>，特殊道具僅由通關 Boss 取得），可買可賣。
///
/// 資料一致性：金幣與道具皆以跨場景中樞 <see cref="LevelMapInitializer"/> 為單一真相源——
/// 買＝扣錢(ChangeMoney) + 入袋(AddItem)；賣＝由背包 <see cref="PlayerInventory"/> 拖到賣出區處理。
/// 音效由 ShopAudioHook 訂閱 <see cref="ItemPurchased"/>/<see cref="PurchaseFailed"/> 後走 AudioDirector。
/// </summary>
public class ShopSystem : MonoBehaviour
{
    private static LevelMapInitializer Hub => LevelMapInitializer.Instance;

    [Header("接線（留空會在場上自動尋找）")]
    [SerializeField] private MapEventService mapEventService;
    [Tooltip("開商店時暫停地圖點擊、關閉後恢復；可留空。")]
    [SerializeField] private S001_PlayerController playerController;
    [Tooltip("背包（開店時進入賣出模式、拖道具到賣出區賣出）；留空自動尋找。")]
    [SerializeField] private PlayerInventory playerInventory;

    [Header("商品資料")]
    [Tooltip("道具資料庫；留空會嘗試 Resources.Load(\"Item/ShopItemDatabase\")。")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("UI")]
    [SerializeField] private GameObject shopUI;                 // 商店面板根（開/關）
    [SerializeField] private Button closeButton;                // 離開
    [SerializeField] private TMP_Text goldText;                 // 金幣顯示
    [SerializeField] private TMP_Text messageText;              // 店員訊息（可空）
    [Tooltip("賣出區（拖背包道具到此賣出）；留空則以整個 shopUI 當賣出區。")]
    [SerializeField] private RectTransform sellArea;

    [Header("商店時暫移玩家狀態 UI 到左上（離開還原）")]
    [Tooltip("玩家狀態 UI 的 RectTransform；留空會自動找 PlayerMapStatus_UI。")]
    [SerializeField] private RectTransform mapStatusUI;
    [Tooltip("進商店時玩家狀態 UI 移到的 anchoredPosition（預設往上移，使左下主狀態移到左上）。")]
    [SerializeField] private Vector2 shopStatusPos = new Vector2(0, 810);
    private bool statusMoved;
    private Vector2 statusOrigPos;

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

    // ── 對外事件（ShopAudioHook 訂閱播音效；日後存檔系統可訂閱）──
    public event Action<Item> ItemPurchased;   // 購買成功（真正的 Item）
    public event Action PurchaseFailed;         // 金幣不足 / 背包已滿 / 購買失敗
    public event Action OnShopClosed;

    private bool isTooltipActive;

    private void Awake()
    {
        if (mapEventService == null) mapEventService = FindAnyObjectByType<MapEventService>();
        if (playerController == null) playerController = FindAnyObjectByType<S001_PlayerController>();
        if (playerInventory == null) playerInventory = FindAnyObjectByType<PlayerInventory>(FindObjectsInactive.Include);
        if (itemDatabase == null) itemDatabase = Resources.Load<ItemDatabase>("Item/ShopItemDatabase");
        if (mapStatusUI == null)
        {
            var st = FindAnyObjectByType<PlayerMapStatus_UI>(FindObjectsInactive.Include);
            if (st != null) mapStatusUI = st.GetComponent<RectTransform>();
        }

        if (shopUI != null) shopUI.SetActive(false);
        if (tooltipUI != null) tooltipUI.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(CloseShop);

        UpdateGoldText();
    }

    private void OnEnable()
    {
        if (mapEventService != null) mapEventService.ShopRequested += OnShopRequested;
        if (Hub != null) Hub.MoneyChanged += OnHubMoneyChanged;
    }

    private void OnDisable()
    {
        if (mapEventService != null) mapEventService.ShopRequested -= OnShopRequested;
        if (Hub != null) Hub.MoneyChanged -= OnHubMoneyChanged;
    }

    private void Update()
    {
        if (isTooltipActive && tooltipUI != null)
            tooltipUI.transform.position = Input.mousePosition + new Vector3(10, 10, 0);
    }

    private void OnHubMoneyChanged(int _) => UpdateGoldText();
    private void OnShopRequested(NodeEvent grid) => OpenShop();

    /// <summary>開啟商店：暫停地圖點擊、上架商品、開背包並進入賣出模式。</summary>
    public void OpenShop()
    {
        if (shopUI != null) shopUI.SetActive(true);
        if (playerController != null) playerController.DisablePlayerInputForCheck();

        // 玩家狀態 UI 暫移到左上（存原位，離開還原）
        if (mapStatusUI != null && !statusMoved)
        {
            statusOrigPos = mapStatusUI.anchoredPosition;
            mapStatusUI.anchoredPosition = shopStatusPos;
            statusMoved = true;
        }

        // 背包進入賣出模式：拖道具到賣出區即可賣出
        if (playerInventory != null)
        {
            playerInventory.isInShopMode = true;
            playerInventory.sellArea = sellArea != null ? sellArea
                                     : (shopUI != null ? shopUI.GetComponent<RectTransform>() : playerInventory.sellArea);
            playerInventory.ToggleInventory(true);
        }

        UpdateGoldText();
        DisplayShopItems();
    }

    /// <summary>關閉商店：恢復地圖點擊、退出賣出模式。</summary>
    public void CloseShop()
    {
        HideTooltip();
        if (shopUI != null) shopUI.SetActive(false);
        if (playerController != null) playerController.EnablePlayerInput();
        if (playerInventory != null) playerInventory.isInShopMode = false;

        // 還原玩家狀態 UI 位置
        if (mapStatusUI != null && statusMoved)
        {
            mapStatusUI.anchoredPosition = statusOrigPos;
            statusMoved = false;
        }
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
            child.SetActive(false);
            Destroy(child);
        }
        spawnedSlots.Clear();

        List<Item> forSale = GetItemsForSale(Mathf.Max(0, itemCount));
        foreach (Item item in forSale)
        {
            GameObject slot = Instantiate(itemSlotPrefab, itemSlotContainer);
            slot.SetActive(true);
            spawnedSlots.Add(slot);

            var priceText = FindChild<TMP_Text>(slot.transform, "Price");
            var icon = FindChild<Image>(slot.transform, "Item_Picture");
            var buyButton = FindChild<Button>(slot.transform, "BuyButton");

            if (priceText != null) priceText.text = item.value.ToString();
            if (icon != null && item.icon != null) icon.sprite = item.icon;

            AddTooltipHandler(slot, item);

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                Item captured = item;
                GameObject capturedSlot = slot;
                buyButton.onClick.AddListener(() => BuyItem(captured, capturedSlot));
            }
        }
    }

    public void BuyItem(Item item, GameObject slot)
    {
        if (item == null) return;

        if (Hub == null)
        {
            Debug.LogWarning("[ShopSystem] 找不到 LevelMapInitializer，無法購買");
            return;
        }

        if (Hub.IsBackpackFull)
        {
            if (messageText != null) messageText.text = "你的背包已經滿了喔！";
            PurchaseFailed?.Invoke();
            BattleLog.Log($"[ShopSystem] 背包已滿，無法購買 {item.itemName}");
            return;
        }

        if (Hub.Money >= item.value)
        {
            Hub.ChangeMoney(-item.value);      // 扣錢（觸發 MoneyChanged → UI 同步）
            Hub.AddItem(item);                 // 入袋（觸發 ItemsChanged → 背包重繪）
            if (slot != null) slot.SetActive(false);
            if (messageText != null) messageText.text = "太好了，相信你一定會喜歡這個商品的！";
            ItemPurchased?.Invoke(item);       // ShopAudioHook 收到後播購買音效
            BattleLog.Log($"[ShopSystem] 購買 {item.itemName}（-{item.value}），剩餘金幣 {Hub.Money}");
            HideTooltip();
        }
        else
        {
            if (messageText != null) messageText.text = "要買不買的，還沒有足夠的錢啊！";
            PurchaseFailed?.Invoke();          // ShopAudioHook 收到後播購買失敗音效
            BattleLog.Log($"[ShopSystem] 金幣不足，無法購買 {item.itemName}");
        }
    }

    private void UpdateGoldText()
    {
        if (goldText != null) goldText.text = (Hub != null ? Hub.Money : 0).ToString();
    }

    // 隨機取 count 件上架，排除 Special；盡量不重複（不足則允許重複補足）
    private List<Item> GetItemsForSale(int count)
    {
        var result = new List<Item>();
        if (itemDatabase == null || itemDatabase.items == null) return result;

        var pool = new List<Item>();
        foreach (var it in itemDatabase.items)
            if (it != null && it.effectType != ItemEffectType.Special) pool.Add(it);
        if (pool.Count == 0) return result;

        // 洗牌後取前 count 件（不重複）
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        for (int i = 0; i < count; i++)
            result.Add(pool[i % pool.Count]); // 不足時循環補足

        return result;
    }

    #region Tooltip
    private void AddTooltipHandler(GameObject slot, Item item)
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

    private void ShowTooltip(Item item)
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
