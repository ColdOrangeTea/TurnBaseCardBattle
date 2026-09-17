using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class PlayerInventory : MonoBehaviour
{
    public GameObject TeachUI;
    public S001_PlayerController PlayerController;
    public Endless_GM_S001_PlayerController endlessplayercontroller;
    [SerializeField]
    public static bool hasTriggeredTeach = false; // 新增變數追蹤是否已經觸發教學

    public ItemEffectHandler ItemEffectHandler;

    public List<Item> items = new List<Item>();


    public int gold = 100;
    public TMP_Text goldText;
    public TMP_Text goldTextInMap;

    private bool isDragging = false; // 是否正在拖曳
    private GameObject draggedItemIcon; // 拖曳中的物品圖標
    private Item draggedItem; // 被拖曳的物品
    public RectTransform sellArea; // 販賣區域的 RectTransform


    public List<GameObject> inventorySlots;


    public GameObject tooltipUI;
    public TMP_Text tooltipNameText;
    public TMP_Text tooltipDescriptionText;
    public TMP_Text tooltipSellVallueText;

    private bool isTooltipActive = false; // 新增變數來追蹤 Tooltip 是否顯示中

    public RectTransform inventoryUI; // 背包 UI 的 RectTransform
    public Button openBagButton; // 開啟背包按鈕
    public Button closeBagButton; // 收回背包按鈕


    private bool isBagOpen = false; // 背包開啟狀態
    private Vector2 closedPosition = new Vector2(2375, 150); // 背包收回的位置（在螢幕右側）
    private Vector2 openedPosition = new Vector2(1300,150); // 背包展開的位置（螢幕內）
    private float animationDuration = 0.5f; // 動畫持續時間

    public bool isInShopMode = false; // 新增變數，用來判斷是否在商店模式

    private Item selectedItem;

    [SerializeField]
    private int BagtriggerCount = 0;     // 計算觸發次數

    private void Start()
    {
        TeachUI.SetActive(false);
        UpdateGoldText();
        //useItemButton.onClick.AddListener(UseSelectedItem);
        //sellButton.onClick.AddListener(() => SellItem(selectedItem)); // 直接綁定到 SellItem 方法

        // 初始化位置為收回狀態
        inventoryUI.anchoredPosition = closedPosition;
        UpdateButtonVisibility();

        // 綁定按鈕事件
        openBagButton.onClick.AddListener(() => ToggleInventory(true));
        closeBagButton.onClick.AddListener(() => ToggleInventory(false));

    }
    void Update()
    {
        if (isTooltipActive)
        {
            UpdateTooltipPosition();
        }

        if (isDragging && draggedItemIcon != null)
        {
            // 讓拖曳的物品跟隨滑鼠
            draggedItemIcon.transform.position = Input.mousePosition;
        }

        if (!hasTriggeredTeach && BagtriggerCount == 1)
        {
            TeachUI.SetActive(true);
            if(PlayerController !=null)
            {
                PlayerController.EnableBlocking();
            }
            else if (endlessplayercontroller != null)
            {
                endlessplayercontroller.EnableBlocking();
            }

            hasTriggeredTeach = true; // 設置為已觸發，避免重複執行
            Debug.Log("開啟背包次數: " + BagtriggerCount);
        }

    }

    public void HideUI()
    {
        TeachUI.SetActive(false);
        if (PlayerController != null)
        {
            PlayerController.EnableBlocking();
        }
        if (endlessplayercontroller != null)
        {
            endlessplayercontroller.EnableBlocking();
        }
        Debug.Log("按下按鈕");
    }


    private void CountTrigger()
    {
        BagtriggerCount++;
        Debug.Log("開啟背包次數: " + BagtriggerCount);
    }


    void DisplayInventoryItems()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (i < items.Count)
            {
                GameObject inventorySlot = inventorySlots[i];
                Image itemIcon = inventorySlot.transform.Find("Item_Image").GetComponent<Image>();
                Button selectButton = inventorySlot.GetComponent<Button>();

                Item item = items[i];
                itemIcon.sprite = item.icon;

                // 清除舊事件，防止重複綁定
                selectButton.onClick.RemoveAllListeners();

                // 動態綁定使用物品事件
                selectButton.onClick.AddListener(() => UseItem(item));
                // 動態綁定拖曳事件
                AddDragHandler(inventorySlot, item);

                // 顯示該格子
                inventorySlot.SetActive(true);

                // 新增滑鼠懸停事件（Tooltip）
                AddTooltipHandler(inventorySlot, item);
            }
            else
            {
                // 隱藏無物品的格子
                inventorySlots[i].SetActive(false);
            }
        }
    }

    void UseItem(Item item)
    {
        if (item == null)
            return;

        ItemEffectHandler.TriggerEffect(item);


        items.Remove(item); // 從背包移除物品
        HideTooltip();

        DisplayInventoryItems(); // 刷新背包顯示

        // 如果物品有特殊效果，可以在此處處理
        //// 示例：使用後移除該物品   

        // 根據需要觸發其他效果，例如回血、增加屬性等
         
    }

    public void ToggleInventory(bool open)
    {
        
        isBagOpen = open;
        UpdateButtonVisibility();
        DisplayInventoryItems();

        // 開啟或收回背包動畫
        StopAllCoroutines(); // 停止當前動畫（避免重複啟動 Coroutine）
        StartCoroutine(AnimateInventory(isBagOpen ? openedPosition : closedPosition));

        CountTrigger();
    }

    void UpdateButtonVisibility()
    {
        // 根據背包狀態顯示對應按鈕
        openBagButton.gameObject.SetActive(!isBagOpen);
        closeBagButton.gameObject.SetActive(isBagOpen);
    }

    IEnumerator AnimateInventory(Vector2 targetPosition)
    {
        Vector2 startPosition = inventoryUI.anchoredPosition;
        float elapsedTime = 0;

        while (elapsedTime < animationDuration)
        {
            // 線性插值計算位置
            inventoryUI.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, elapsedTime / animationDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 確保位置精確到目標點
        inventoryUI.anchoredPosition = targetPosition;
    }



    #region 動態資訊
    void AddTooltipHandler(GameObject InventorySlot, Item item)
    {
        // 確保 shopItemSlot 上有 Collider，或動態添加 BoxCollider
        if (InventorySlot.GetComponent<Collider>() == null)
        {
            InventorySlot.AddComponent<BoxCollider>();
        }

        // 添加滑鼠事件監聽
        EventTrigger trigger = InventorySlot.GetComponent<EventTrigger>() ?? InventorySlot.AddComponent<EventTrigger>();

        // 滑鼠進入事件
        EventTrigger.Entry entryEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        entryEnter.callback.AddListener((eventData) => OnPointerEnter(item, InventorySlot));
        trigger.triggers.Add(entryEnter);

        // 滑鼠離開事件
        EventTrigger.Entry entryExit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        entryExit.callback.AddListener((eventData) => OnPointerExit());
        trigger.triggers.Add(entryExit);
    }

    void UpdateTooltipPosition()
    {
        if (isTooltipActive)
        {
            // 更新 Tooltip 位置
            tooltipUI.transform.position = Input.mousePosition + new Vector3(10, 10, 0);
        }
    }

    void ShowTooltip(Item item)
    {
        // 如果 Tooltip 已經顯示，則不重新顯示
        if (isTooltipActive) return;

        tooltipUI.SetActive(true);
        tooltipNameText.text = item.itemName;
        tooltipDescriptionText.text = item.description;
        tooltipSellVallueText.text = "售價 : "+item.Sellvalue.ToString();


        // 更新 Tooltip 位置
        tooltipUI.transform.position = Input.mousePosition + new Vector3(10, 50, 0);

        isTooltipActive = true; // 設置為顯示中
    }

    void HideTooltip()
    {
        tooltipUI.SetActive(false);
        isTooltipActive = false; // 設置為隱藏狀態
    }


    void OnPointerEnter(Item item, GameObject shopItemSlot)
    {
        // 強制隱藏現有 Tooltip，確保顯示最新內容
        HideTooltip();

        // 顯示新的 Tooltip
        ShowTooltip(item);

    }

    void OnPointerExit()
    {
        HideTooltip();
    }
    #endregion

    #region  商店拖曳事件
    void AddDragHandler(GameObject inventorySlot, Item item)
    {
        EventTrigger trigger = inventorySlot.GetComponent<EventTrigger>() ?? inventorySlot.AddComponent<EventTrigger>();

        // 拖曳開始事件
        EventTrigger.Entry dragStart = new EventTrigger.Entry
        {
            eventID = EventTriggerType.BeginDrag
        };
        dragStart.callback.AddListener((eventData) => OnDragStart(item, inventorySlot));
        trigger.triggers.Add(dragStart);

        // 拖曳進行中事件
        EventTrigger.Entry drag = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Drag
        };
        drag.callback.AddListener((eventData) => OnDrag(item));
        trigger.triggers.Add(drag);

        // 拖曳結束事件
        EventTrigger.Entry dragEnd = new EventTrigger.Entry
        {
            eventID = EventTriggerType.EndDrag
        };
        dragEnd.callback.AddListener((eventData) => OnDragEnd(inventorySlot));
        trigger.triggers.Add(dragEnd);
    }

    void OnDragStart(Item item, GameObject inventorySlot)
    {

        // 如果正在拖曳，先銷毀舊的物品圖標
        if (draggedItemIcon != null)
        {
            Destroy(draggedItemIcon);
        }

        isDragging = true;
        draggedItem = item;

        // 創建一個物品圖標作為拖曳效果
        draggedItemIcon = new GameObject("Item_Image");
        draggedItemIcon.transform.SetParent(transform, false);
        draggedItemIcon.transform.SetParent(GameObject.Find("ItemGrid_Panel").transform, false); // 放到 Canvas 下
        draggedItemIcon.transform.SetAsLastSibling(); // 保證圖標顯示在最上層

        Image icon = draggedItemIcon.AddComponent<Image>();
        icon.sprite = item.icon;
        icon.raycastTarget = false; // 防止圖標遮擋拖曳事件

        //inventorySlot.SetActive(false); // 隱藏原始物品格
    }

    void OnDrag(Item item)
    {
        if (isDragging && draggedItemIcon != null)
        {
            draggedItemIcon.transform.position = Input.mousePosition;
        }
    }

    void OnDragEnd(GameObject inventorySlot)
    {
        isDragging = false;

        if (isInShopMode && RectTransformUtility.RectangleContainsScreenPoint(sellArea, Input.mousePosition))
        {
            SellItem(draggedItem);
        }
        else
        {
            inventorySlot.SetActive(true); // 顯示原始物品格
            
        }

        // 刪除拖曳物品圖標
        if (draggedItemIcon != null)
        {
            Destroy(draggedItemIcon);
            draggedItemIcon = null; // 確保清除引用
        }
    }

    #endregion

    public void UpdateGoldText()
    {
        if (goldText != null)
        {
            goldText.text = gold.ToString();
        }
        if (goldTextInMap != null)
        {
            goldTextInMap.text =gold.ToString();
        }
    }

    void UseSelectedItem()
    {
        Debug.Log("使用了 " + selectedItem.itemName);
    }

  

    public void AddItem(Item item)
    {
        items.Add(item);
        Debug.Log("物品 " + item.itemName + " 已添加到背包.");
    }

    // 處理物品出售
    public void SellItem(Item item)
    {
        if (items.Contains(item))
        {
            gold += item.Sellvalue;
            items.Remove(item); // 移除物品
            UpdateGoldText();
            DisplayInventoryItems(); // 更新背包界面
            Debug.Log("成功出售 " + item.itemName + "！");
        }
    }
}