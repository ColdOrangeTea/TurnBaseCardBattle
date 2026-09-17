using System.Collections.Generic;
using UnityEngine;
using System;

public class QuestManager : MonoBehaviour
{
    public List<QuestData> activeQuests; // 當前進行中的任務列表
    public QuestUI questUI;               // 任務 UI 參考
    public DialogueOpenClose dialogueClose;
    public PlayerInventory playerInventory;

    public event Action QuestEventEnd;

    private void Start()
    {
        // 確保在啟動時不顯示任何任務
        questUI.HideQuest();
        questUI.HideQuestList();
    }

    public void AddQuest(QuestData newQuest)
    {
        if (!activeQuests.Contains(newQuest))
        {
            activeQuests.Add(newQuest);
            questUI.DisplayQuest(newQuest); // 顯示新增的任務
        }
    }

    public void CompleteQuest(QuestData quest, bool isSuccess)
    {
        questUI.ShowQuest();
        questUI.HideQuestBG();
        questUI.ShowQuestResult();

        if (isSuccess)
        {
            RewardPlayer(quest.successReward);
            Debug.Log("任務成功！獲得獎勵");
        }
        else
        {
            RewardPlayer(quest.failureReward);
            Debug.Log("任務失敗！獲得懲罰或替代獎勵");
        }

        activeQuests.Remove(quest);
        
        questUI.HideQuestList();
    }

    private void RewardPlayer(RewardData reward)
    {
        // 這裡處理獎勵邏輯，比如更新玩家金幣、增加經驗等
        Debug.Log($"獲得獎勵: 金幣 {reward.gold}, 物品 {reward.item}");

        playerInventory.gold = playerInventory.gold + reward.gold;
        playerInventory.UpdateGoldText();
    }

    public void AcceptCurrentQuest()
    {
        // dialogueClose.OpenDialogue();
        GameManager.DialogueManagerInstance.Temp_AssignL1MainStoryStage(3);

        dialogueClose.OpenDialogue();

        if (questUI.currentQuest != null) // 確保當前任務存在
        {
            AddQuest(questUI.currentQuest); // 將當前任務添加到活動任務列表
            questUI.HideQuest(); // 隱藏任務面板
        }

        questUI.ShowQuestList();
       QuestEventEnd?.Invoke();

    }

    public QuestData GetQuest()
    {
        if (activeQuests.Count > 0)
        {
            // 隨機選擇一個任務，您也可以根據需求進行其他選擇邏輯
            return activeQuests[UnityEngine.Random.Range(0, activeQuests.Count)];
        }
        return null; // 沒有可用的任務
    }
}
