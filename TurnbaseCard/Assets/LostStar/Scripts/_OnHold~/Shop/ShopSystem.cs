using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopSystem : MonoBehaviour
{
    public GameObject tooltipUI;
    public TMP_Text tooltipNameText;
    public TMP_Text tooltipDescriptionText;
    public GameObject OwnerDia;

    private bool isTooltipActive = false; // 新增變數來追蹤 Tooltip 是否顯示中

    public ItemManager itemManager;
    public PlayerInventory playerInventory;

    public GameObject shopUI;
    public Button closeButton;

    public List<GameObject> shopItemSlots;
    public TMP_Text messageText;
    public TMP_Text goldText;

    public GameObject Jephthah;

    public QuestManager questManager; // 引用 QuestManager
    public DialogueOpenClose dialogueClose;
    public BattleButtonFunction battleButtonFunction;

    // 新增兩個 AudioSource
    public AudioSource buyAudio;
    public AudioSource buyFailedAudio;

    // 販賣物品的介面
    public GameObject sellItemUI;

    private bool hasTriggeredDialogue = false; // 用來追蹤對話是否已經觸發過

    public event Action OnShopClosed;

    void Start()
    {
        shopUI.SetActive(false);
        OwnerDia.SetActive(false);
        closeButton.onClick.AddListener(CloseShop);
        UpdateGoldText();
    }

    // 這裡使用 Update 方法來持續更新 Tooltip 位置
    void Update()
    {
        if (isTooltipActive)
        {
            UpdateTooltipPosition();
        }
    }
    public void OpenShop()
    {
        shopUI.SetActive(true);
        playerInventory.isInShopMode = true; // 進入商店模式
        DisplayShopItems();

        // 檢查是否已經觸發過對話
        if (!hasTriggeredDialogue)
        {
            Debug.Log($"觸發商店教學");

            GameManager.DialogueManagerInstance.Temp_AssignL1MainStoryStage(11);
            dialogueClose.OpenDialogue();
            Jephthah.SetActive(false);
            hasTriggeredDialogue = true; // 設置為 true，表示已經觸發過對話
        }
    }

    public void OpenShopForEndless()
    {
        battleButtonFunction.BattleIsEnd = false;
        shopUI.SetActive(true);
        playerInventory.isInShopMode = true; // 進入商店模式
        DisplayShopItems();

    }

    public void CloseShop()
    {
        shopUI.SetActive(false);
        playerInventory.isInShopMode = false; // 離開商店模式
        OnShopClosed?.Invoke();
        Jephthah.SetActive(true);
    }

    void DisplayShopItems()
    {
        messageText.text = "歡迎光臨！挑挑看，選選看啊！";
        List<Item> itemsForSale = GetItemsForSale();

        for (int i = 0; i < shopItemSlots.Count; i++)
        {
            if (i < itemsForSale.Count)
            {
                GameObject shopItemSlot = shopItemSlots[i];
                //TMP_Text itemNameText = shopItemSlot.transform.Find("Name").GetComponent<TMP_Text>();
                //TMP_Text itemDesText = shopItemSlot.transform.Find("Description").GetComponent<TMP_Text>();
                TMP_Text itemPriceText = shopItemSlot.transform.Find("Price").GetComponent<TMP_Text>();
                Image itemIcon = shopItemSlot.transform.Find("Item_Picture").GetComponent<Image>();
                Button buyButton = shopItemSlot.transform.Find("BuyButton").GetComponent<Button>();

                Item item = itemsForSale[i];
                //itemNameText.text = item.itemName;
                //itemDesText.text = item.description;
                itemPriceText.text = item.value.ToString();
                itemIcon.sprite = item.icon;

                // 新增滑鼠懸停事件
                AddTooltipHandler(shopItemSlot, item);

                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => BuyItem(item, shopItemSlot));
                shopItemSlot.SetActive(true);
            }
            else
            {
                shopItemSlots[i].SetActive(false);
            }
        }
    }

    
    void AddTooltipHandler(GameObject shopItemSlot, Item item)
    {
        // 確保 shopItemSlot 上有 Collider，或動態添加 BoxCollider
        if (shopItemSlot.GetComponent<Collider>() == null)
        {
            shopItemSlot.AddComponent<BoxCollider>();
        }

        // 添加滑鼠事件監聽
        EventTrigger trigger = shopItemSlot.GetComponent<EventTrigger>() ?? shopItemSlot.AddComponent<EventTrigger>();

        // 滑鼠進入事件
        EventTrigger.Entry entryEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        entryEnter.callback.AddListener((eventData) => OnPointerEnter(item, shopItemSlot));
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

  

    public void BuyItem(Item item, GameObject shopItemSlot)
    {
        // 檢查包包是否滿了
        if (playerInventory.items.Count >= playerInventory.inventorySlots.Count)
        {
            messageText.text = "你的包包已滿，無法購買物品！";
            return;
        }

        if (playerInventory.gold >= item.value)
        {
            playerInventory.gold -= item.value;
            playerInventory.AddItem(item);
            UpdateGoldText();
            shopItemSlot.SetActive(false);
            messageText.text = "太好了，相信你一定會喜歡這個商品的！";

            buyAudio.Play();
            Debug.Log("成功購買 " + item.itemName);
            HideTooltip();
        }
        else
        {
            Debug.Log("金幣不足，無法購買 " + item.itemName);
            messageText.text = "要買不買的，還沒有足夠的錢跟空間！";

            buyFailedAudio.Play();
        }
    }

    public void SellSuccessDia()
    {
        messageText.text = "謝謝惠顧！";
    }

    // 開啟販賣介面
    public void OpenSellMenu()
    {
        // 直接開啟包包介面
        playerInventory.ToggleInventory(true);
    }

    void UpdateGoldText()
    {
        goldText.text =playerInventory.gold.ToString();
        playerInventory.UpdateGoldText();
    }

    List<Item> GetItemsForSale()
    {
        List<Item> itemsForSale = new List<Item>();
        for (int i = 0; i < 5; i++)
        {
            Item randomItem = itemManager.GetRandomItem();
            itemsForSale.Add(randomItem);
        }
        return itemsForSale;
    }
}