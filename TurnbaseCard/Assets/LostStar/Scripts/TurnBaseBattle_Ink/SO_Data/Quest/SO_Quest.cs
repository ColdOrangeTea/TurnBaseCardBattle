using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "SO/Quest System/Quest")]
public class QuestData : ScriptableObject
{
    public string questName;          // 任務名稱
    public string description;        // 任務描述

    [Header("任務獎勵")]
    public RewardData successReward;  // 任務成功獎勵
    public RewardData failureReward;  // 任務失敗獎勵

    // 可擴展更多屬性，例如任務狀態、目標數量、任務時間等
}

[System.Serializable]
public class RewardData
{
    public int gold;                  // 獎勵金錢數量
    public string item;               // 獎勵物品名稱
}
