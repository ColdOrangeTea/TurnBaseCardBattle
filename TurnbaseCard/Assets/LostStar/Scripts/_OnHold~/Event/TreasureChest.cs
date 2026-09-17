using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

public class TreasureChest : MonoBehaviour
{
    public List<Item> possibleItems; // 可以隨機抽取的道具列表
    public int minGoldReward = 10;   // 金幣獎勵的下限
    public int maxGoldReward = 50;   // 金幣獎勵的上限
    public List<Effect> possibleEffects; // 可以隨機抽取的效果列表

    //教學對話用
    public DialogueManager manager;
    public DialogueOpenClose dialogueClose;
    public BattleButtonFunction battleButtonFunction;

    [SerializeField]
    private bool hasTriggeredDialogue = false; // 用來追蹤對話是否已經觸發過

    public GameObject TreasureUI;
    public Button button;
    private PlayerInventory playerInventory;
    private PlayerStatsManager playerStats;


    // TMP 變數
    public TextMeshProUGUI treasureUIText; // 引用 TMP 文本元件
    public TextMeshProUGUI treasureUIText2; // 引用 TMP 文本元件
    public TextMeshProUGUI treasureUIText3; // 引用 TMP 文本元件
    public Image ItemImage;

    public event Action TreasureEventEnd;

    private void Start()
    {
        TreasureUI.SetActive(false);

        // 使用 FindWithTag 找到標記為 "Bag" 的物件，並將其轉型為 PlayerInventory 類型
        GameObject inventoryObject = GameObject.FindWithTag("Bag");
        if (inventoryObject != null)
        {
            playerInventory = inventoryObject.GetComponent<PlayerInventory>();
        }
        else
        {
            Debug.LogWarning("未找到標記為 'Bag' 的 PlayerInventory 物件");
        }

        GameObject Playerstats = GameObject.FindWithTag("PlayerStatsManager");
        if (Playerstats != null)
        {
            playerStats = Playerstats.GetComponent<PlayerStatsManager>();
        }
        else
        {
            Debug.LogWarning("未找到標記為 'PlayerStats' 的 PlayerStatsManager 物件");
        }

        // 設置按鈕的 OnClick 事件
        if (button != null)
        {
            button.onClick.AddListener(OKButton);
        }
        else
        {
            Debug.LogWarning("未找到按鈕物件，請確保已將 Button 指定到 TreasureChest 腳本");
        }
    }

    // 觸發寶箱事件的方法
    public void TriggerTreasureChest()
    {

        triggerDialogue();

        TreasureUI.SetActive(true);
        treasureUIText.text = ""; // 清空之前的文本
        int randomIndex = UnityEngine.Random.Range(0, 2); // 0: 金幣, 1: 道具, 2: 效果

        switch (randomIndex)
        {
            case 0:
                GiveGoldReward();
                break;
            case 1:
                GiveRandomItem();
                break;
            //case 2:
            //    ApplyRandomEffect();
            //    break;
        }

    
    }
    public void TriggerTreasureChestForEndLess()
    {
        battleButtonFunction.BattleIsEnd =false;

        TreasureUI.SetActive(true);
        treasureUIText.text = ""; // 清空之前的文本
        int randomIndex = UnityEngine.Random.Range(0, 2); // 0: 金幣, 1: 道具, 2: 效果

        switch (randomIndex)
        {
            case 0:
                GiveGoldReward();
                break;
            case 1:
                GiveRandomItem();
                break;
            //case 2:
            //    ApplyRandomEffect();
            //    break;
        }


    }

    private void triggerDialogue()
    {
        if (hasTriggeredDialogue) return; // 如果已經觸發過，則不執行
        hasTriggeredDialogue = true;

        // 以下為 TriggerGuideDialogue2 的原有邏輯
        Debug.Log("觸發第二段教學");

        SO_DialogueData nextDialogueData = manager.GetDialogueData();

        if (nextDialogueData != null) // 確保下一段劇情存在
        {
            GameManager.DialogueManagerInstance.Temp_AssignL1MainStoryStage(9);
            dialogueClose.OpenDialogue();
        }
    }

    public void OKButton()
    {
        TreasureUI.SetActive(false);
        Debug.Log("寶箱事件觸發完成！");
        TreasureEventEnd?.Invoke();
    }

    // 隨機獲得金幣
    private void GiveGoldReward()
    {
        int goldReward = UnityEngine.Random.Range(minGoldReward, maxGoldReward);
        playerInventory.gold += goldReward;
        playerInventory.UpdateGoldText();

        treasureUIText2.gameObject.SetActive(false);
        treasureUIText.gameObject.SetActive(false);
        treasureUIText3.gameObject.SetActive(true);
        ItemImage.gameObject.SetActive(false);
        treasureUIText3.text = "獲得了 " + goldReward + " 金幣！"; // 更新 TMP 文本


        Debug.Log("玩家獲得了 " + goldReward + " 金幣！");
    }

    // 隨機獲得道具
    private void GiveRandomItem()
    {
        if (possibleItems.Count > 0)
        {
            int itemIndex = UnityEngine.Random.Range(0, possibleItems.Count);
            Item selectedItem = possibleItems[itemIndex];
            playerInventory.AddItem(selectedItem);

            ItemImage.gameObject.SetActive(true);
            treasureUIText2.gameObject.SetActive(true);
            treasureUIText.gameObject.SetActive(true);
            treasureUIText3.gameObject.SetActive(false);

            treasureUIText.text = "獲得了："; // 更新 TMP 文本
            Image ItemImage1 = GameObject.Find("ItemImage").GetComponent<Image>();
            ItemImage1.sprite = selectedItem.icon;
            treasureUIText2.text = selectedItem.itemName; // 更新 TMP 文本
            Debug.Log("玩家獲得了道具：" + selectedItem.itemName);
        }
        else
        {
            treasureUIText.text = "寶箱中沒有道具！"; // 更新 TMP 文本
            Debug.Log("寶箱中沒有道具！");
        }
    }

    // 隨機獲得效果
    //private void ApplyRandomEffect()
    //{
    //    if (possibleEffects.Count > 0)
    //    {
    //        int effectIndex = UnityEngine.Random.Range(0, possibleEffects.Count);
    //        Effect selectedEffect = possibleEffects[effectIndex];
    //        //playerStats.ApplyEffect(selectedEffect); // 假設 PlayerStatsManager 中有 ApplyEffect 方法
    //        //treasureUIText.text = "玩家獲得了效果：" + selectedEffect.effectName; // 更新 TMP 文本
    //        //Debug.Log("玩家獲得了效果：" + selectedEffect.effectName);
    //    }
    //    else
    //    {
    //        ItemImage.gameObject.SetActive(false);
    //        treasureUIText3.gameObject.SetActive(true);
    //        treasureUIText2.gameObject.SetActive(false);
    //        treasureUIText.gameObject.SetActive(false);
    //        treasureUIText3.text = "寶箱中沒有效果！"; // 更新 TMP 文本
    //        Debug.Log("寶箱中沒有效果！");
    //    }
    //}
}
