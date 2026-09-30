using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// LevelMap 道具背包 UI 控制器（掛在 InventaryEmpty）。由 A_Good_Ink 使用 AI 生成/重構。
///
/// 改為跨場景中樞 <see cref="LevelMapInitializer"/> 的 View：金幣與道具皆讀寫中樞，訂閱其事件自動刷新；
/// 背包上限 <see cref="LevelMapInitializer.BackpackCapacity"/> 由中樞把關，溢出自動放棄並跳「程式化提示」。
/// 點道具僅 Heal / Buff 可使用（使用即消耗）；商店開啟時可把道具拖到 <see cref="sellArea"/> 賣出。
/// 保留公開方法名（ToggleInventory / HideUI / UpdateGoldText / AddItem / SellItem）以相容既有按鈕綁定。
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    private static LevelMapInitializer Hub => LevelMapInitializer.Instance;

    [Header("教學")]
    public GameObject TeachUI;
    public S001_PlayerController PlayerController;
    [SerializeField]
    public static bool hasTriggeredTeach = false; // 追蹤是否已觸發教學

    [Header("效果處理")]
    public ItemEffectHandler ItemEffectHandler;

    [Header("金幣顯示")]
    public TMP_Text goldText;
    public TMP_Text goldTextInMap;

    [Header("背包格子 / 賣出區")]
    public List<GameObject> inventorySlots;
    public RectTransform sellArea; // 商店賣出區域（拖到此處賣出）

    [Header("Tooltip")]
    public GameObject tooltipUI;
    public TMP_Text tooltipNameText;
    public TMP_Text tooltipDescriptionText;
    public TMP_Text tooltipSellVallueText;

    [Header("背包側邊動畫")]
    public RectTransform inventoryUI;         // 背包 UI 的 RectTransform
    public Button openBagButton;              // 開啟背包按鈕
    public Button closeBagButton;             // 收回背包按鈕
    private bool isBagOpen = false;
    private Vector2 closedPosition = new Vector2(2375, 150); // 收回位置（螢幕右側外）
    private Vector2 openedPosition = new Vector2(1300, 150); // 展開位置（螢幕內）
    private float animationDuration = 0.5f;

    [Header("商店")]
    public bool isInShopMode = false; // 是否在商店模式（可拖曳賣出）

    [SerializeField]
    private int BagtriggerCount = 0;

    // 拖曳狀態
    private bool isDragging = false;
    private GameObject draggedItemIcon;
    private Item draggedItem;
    private bool isTooltipActive = false;

    // 溢出放棄提示（程式化建立）
    private GameObject discardNoticeGO;
    private CanvasGroup discardNoticeCanvasGroup;
    private TMP_Text discardNoticeText;
    private Coroutine discardNoticeRoutine;

    /// <summary>目前持有道具（來自中樞）。</summary>
    private IReadOnlyList<Item> Items => Hub != null ? Hub.HeldItems : System.Array.Empty<Item>();

    private void Start()
    {
        if (TeachUI != null) TeachUI.SetActive(false);
        UpdateGoldText();

        if (inventoryUI != null) inventoryUI.anchoredPosition = closedPosition; // 初始收回
        UpdateButtonVisibility();

        if (openBagButton != null) openBagButton.onClick.AddListener(() => ToggleInventory(true));
        if (closeBagButton != null) closeBagButton.onClick.AddListener(() => ToggleInventory(false));

        DisplayInventoryItems();
    }

    private void OnEnable()
    {
        if (Hub != null)
        {
            Hub.MoneyChanged += OnHubMoneyChanged;
            Hub.ItemsChanged += OnHubItemsChanged;
            Hub.ItemDiscarded += OnHubItemDiscarded;
        }
    }

    private void OnDisable()
    {
        if (Hub != null)
        {
            Hub.MoneyChanged -= OnHubMoneyChanged;
            Hub.ItemsChanged -= OnHubItemsChanged;
            Hub.ItemDiscarded -= OnHubItemDiscarded;
        }
    }

    private void OnHubMoneyChanged(int _) => UpdateGoldText();
    private void OnHubItemsChanged() => DisplayInventoryItems();
    private void OnHubItemDiscarded(Item item) => ShowDiscardNotice(item);

    void Update()
    {
        if (isTooltipActive)
            UpdateTooltipPosition();

        if (isDragging && draggedItemIcon != null)
            draggedItemIcon.transform.position = Input.mousePosition;

        if (!hasTriggeredTeach && BagtriggerCount == 1)
        {
            if (TeachUI != null) TeachUI.SetActive(true);
            if (PlayerController != null) PlayerController.EnableBlocking();
            hasTriggeredTeach = true;
            Debug.Log("開啟背包次數: " + BagtriggerCount);
        }
    }

    public void HideUI()
    {
        if (TeachUI != null) TeachUI.SetActive(false);
        if (PlayerController != null) PlayerController.EnableBlocking();
        Debug.Log("按下按鈕");
    }

    private void CountTrigger()
    {
        BagtriggerCount++;
        Debug.Log("開啟背包次數: " + BagtriggerCount);
    }

    // ── 背包顯示 ──
    void DisplayInventoryItems()
    {
        if (inventorySlots == null) return;
        var items = Items;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i] == null) continue;
            if (i < items.Count)
            {
                GameObject inventorySlot = inventorySlots[i];
                Image itemIcon = inventorySlot.transform.Find("Item_Image")?.GetComponent<Image>();
                Button selectButton = inventorySlot.GetComponent<Button>();

                Item item = items[i];
                if (itemIcon != null) itemIcon.sprite = item.icon;

                if (selectButton != null)
                {
                    selectButton.onClick.RemoveAllListeners();
                    selectButton.onClick.AddListener(() => UseItem(item)); // 使用（僅 Heal/Buff）
                }
                AddDragHandler(inventorySlot, item);   // 拖曳（賣出）
                inventorySlot.SetActive(true);
                AddTooltipHandler(inventorySlot, item);
            }
            else
            {
                inventorySlots[i].SetActive(false); // 無道具的格子隱藏
            }
        }
    }

    // 使用道具：僅 Heal / Buff 可用，使用即消耗
    void UseItem(Item item)
    {
        if (item == null) return;
        if (item.effectType != ItemEffectType.Heal && item.effectType != ItemEffectType.Buff)
        {
            Debug.Log($"道具「{item.itemName}」（{item.effectType}）無法在背包直接使用。");
            return;
        }

        if (ItemEffectHandler != null) ItemEffectHandler.TriggerEffect(item);
        else Debug.LogWarning("[PlayerInventory] 未指派 ItemEffectHandler，無法套用道具效果");

        if (Hub != null) Hub.RemoveItem(item); // 消耗（觸發 ItemsChanged → 自動重繪）
        HideTooltip();
    }

    public void ToggleInventory(bool open)
    {
        isBagOpen = open;
        UpdateButtonVisibility();
        DisplayInventoryItems();

        StopAllCoroutines();
        StartCoroutine(AnimateInventory(isBagOpen ? openedPosition : closedPosition));

        CountTrigger();
    }

    void UpdateButtonVisibility()
    {
        if (openBagButton != null) openBagButton.gameObject.SetActive(!isBagOpen);
        if (closeBagButton != null) closeBagButton.gameObject.SetActive(isBagOpen);
    }

    IEnumerator AnimateInventory(Vector2 targetPosition)
    {
        if (inventoryUI == null) yield break;
        Vector2 startPosition = inventoryUI.anchoredPosition;
        float elapsedTime = 0;
        while (elapsedTime < animationDuration)
        {
            inventoryUI.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        inventoryUI.anchoredPosition = targetPosition;
    }

    #region 動態資訊 Tooltip
    void AddTooltipHandler(GameObject InventorySlot, Item item)
    {
        EventTrigger trigger = InventorySlot.GetComponent<EventTrigger>() ?? InventorySlot.AddComponent<EventTrigger>();

        var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((eventData) => OnPointerEnter(item, InventorySlot));
        var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((eventData) => OnPointerExit());

        // 保留既有的拖曳事件，僅補上 tooltip（不清空 trigger）
        trigger.triggers.Add(entryEnter);
        trigger.triggers.Add(entryExit);
    }

    void UpdateTooltipPosition()
    {
        if (isTooltipActive && tooltipUI != null)
            tooltipUI.transform.position = Input.mousePosition + new Vector3(10, 10, 0);
    }

    void ShowTooltip(Item item)
    {
        if (isTooltipActive || tooltipUI == null) return;
        tooltipUI.SetActive(true);
        if (tooltipNameText != null) tooltipNameText.text = item.itemName;
        if (tooltipDescriptionText != null) tooltipDescriptionText.text = item.description;
        if (tooltipSellVallueText != null) tooltipSellVallueText.text = "售價 : " + item.Sellvalue;
        tooltipUI.transform.position = Input.mousePosition + new Vector3(10, 50, 0);
        isTooltipActive = true;
    }

    void HideTooltip()
    {
        if (tooltipUI != null) tooltipUI.SetActive(false);
        isTooltipActive = false;
    }

    void OnPointerEnter(Item item, GameObject shopItemSlot)
    {
        HideTooltip();
        ShowTooltip(item);
    }

    void OnPointerExit() => HideTooltip();
    #endregion

    #region 拖曳 / 賣出
    void AddDragHandler(GameObject inventorySlot, Item item)
    {
        EventTrigger trigger = inventorySlot.GetComponent<EventTrigger>() ?? inventorySlot.AddComponent<EventTrigger>();
        trigger.triggers.Clear(); // 重繪時先清掉舊事件，避免重複綁定（tooltip 由 AddTooltipHandler 於其後補回）

        var dragStart = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        dragStart.callback.AddListener((eventData) => OnDragStart(item, inventorySlot));
        trigger.triggers.Add(dragStart);

        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener((eventData) => OnDrag(item));
        trigger.triggers.Add(drag);

        var dragEnd = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        dragEnd.callback.AddListener((eventData) => OnDragEnd(inventorySlot));
        trigger.triggers.Add(dragEnd);
    }

    void OnDragStart(Item item, GameObject inventorySlot)
    {
        if (draggedItemIcon != null) Destroy(draggedItemIcon);
        isDragging = true;
        draggedItem = item;

        draggedItemIcon = new GameObject("Item_Image");
        var parent = GameObject.Find("ItemGrid_Panel");
        draggedItemIcon.transform.SetParent(parent != null ? parent.transform : transform, false);
        draggedItemIcon.transform.SetAsLastSibling();

        Image icon = draggedItemIcon.AddComponent<Image>();
        icon.sprite = item.icon;
        icon.raycastTarget = false;
    }

    void OnDrag(Item item)
    {
        if (isDragging && draggedItemIcon != null)
            draggedItemIcon.transform.position = Input.mousePosition;
    }

    void OnDragEnd(GameObject inventorySlot)
    {
        isDragging = false;

        if (isInShopMode && sellArea != null &&
            RectTransformUtility.RectangleContainsScreenPoint(sellArea, Input.mousePosition))
        {
            SellItem(draggedItem);
        }
        else if (inventorySlot != null)
        {
            inventorySlot.SetActive(true);
        }

        if (draggedItemIcon != null)
        {
            Destroy(draggedItemIcon);
            draggedItemIcon = null;
        }
    }
    #endregion

    // ── 金幣 / 增減（走中樞）──
    public void UpdateGoldText()
    {
        int gold = Hub != null ? Hub.Money : 0;
        if (goldText != null) goldText.text = gold.ToString();
        if (goldTextInMap != null) goldTextInMap.text = gold.ToString();
    }

    /// <summary>加入道具（走中樞；滿則中樞觸發 ItemDiscarded → 顯示放棄提示）。</summary>
    public void AddItem(Item item)
    {
        if (Hub == null) { Debug.LogWarning("[PlayerInventory] 找不到 LevelMapInitializer，無法加入道具"); return; }
        Hub.AddItem(item);
        Debug.Log("嘗試加入道具: " + (item != null ? item.itemName : "null"));
    }

    /// <summary>賣出道具：從中樞移除並加回金幣（Sellvalue）。</summary>
    public void SellItem(Item item)
    {
        if (item == null || Hub == null) return;
        if (Hub.RemoveItem(item))
        {
            Hub.ChangeMoney(item.Sellvalue);
            UpdateGoldText();
            HideTooltip();
            Debug.Log($"成功出售 {item.itemName}，+{item.Sellvalue} 金幣");
        }
    }

    // ── 溢出放棄提示（程式化建立、數秒後淡出）──
    private void ShowDiscardNotice(Item item)
    {
        EnsureDiscardNotice();
        if (discardNoticeText != null)
            discardNoticeText.text = $"背包已滿，已放棄「{(item != null ? item.itemName : "道具")}」";
        if (discardNoticeGO != null) discardNoticeGO.SetActive(true);
        if (discardNoticeRoutine != null) StopCoroutine(discardNoticeRoutine);
        discardNoticeRoutine = StartCoroutine(FadeOutDiscardNotice());
    }

    private void EnsureDiscardNotice()
    {
        if (discardNoticeGO != null) return;

        Transform parent = null;
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null) parent = canvas.transform;
        else if (inventoryUI != null && inventoryUI.parent != null) parent = inventoryUI.parent;
        if (parent == null) parent = transform;

        discardNoticeGO = new GameObject("DiscardNotice");
        discardNoticeGO.transform.SetParent(parent, false);
        var rt = discardNoticeGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -140);
        rt.sizeDelta = new Vector2(640, 64);

        var bg = discardNoticeGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        bg.raycastTarget = false;

        discardNoticeCanvasGroup = discardNoticeGO.AddComponent<CanvasGroup>();
        discardNoticeCanvasGroup.interactable = false;
        discardNoticeCanvasGroup.blocksRaycasts = false;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(discardNoticeGO.transform, false);
        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12, 6); trt.offsetMax = new Vector2(-12, -6);

        discardNoticeText = textGO.AddComponent<TextMeshProUGUI>();
        discardNoticeText.alignment = TextAlignmentOptions.Center;
        discardNoticeText.fontSize = 28;
        discardNoticeText.color = Color.white;
        discardNoticeText.enableWordWrapping = true;
        discardNoticeText.raycastTarget = false;

        // 沿用場上既有 TMP 的中文字型，避免中文顯示為 □□□
        var fontSource = tooltipNameText != null ? tooltipNameText : goldText;
        if (fontSource != null && fontSource.font != null) discardNoticeText.font = fontSource.font;

        discardNoticeGO.SetActive(false);
    }

    private IEnumerator FadeOutDiscardNotice()
    {
        if (discardNoticeCanvasGroup != null) discardNoticeCanvasGroup.alpha = 1f;
        yield return new WaitForSeconds(2f);
        float t = 0f, dur = 0.5f;
        while (t < dur)
        {
            t += Time.deltaTime;
            if (discardNoticeCanvasGroup != null) discardNoticeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / dur);
            yield return null;
        }
        if (discardNoticeGO != null) discardNoticeGO.SetActive(false);
    }
}
