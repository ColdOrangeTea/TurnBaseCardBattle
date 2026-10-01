using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum; // EnemyType

/// <summary>任務完成條件類型。目前支援 DefeatEnemyType；其餘為未來預留，QuestTracker 之後補判定。</summary>
public enum QuestConditionType
{
    DefeatEnemyType,   // 擊敗指定類型的敵人
    WinConsecutive,    // 連續打贏 N 場（未來）
    WinWithOnlyCards   // 一場戰鬥只用指定牌打贏（未來）
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "SO/Quest System/Quest")]
public class QuestData : ScriptableObject
{
    public string questName;          // 任務名稱
    public string description;        // 任務描述

    [Header("完成條件")]
    public QuestConditionType conditionType = QuestConditionType.DefeatEnemyType;
    [Tooltip("DefeatEnemyType：要擊敗的敵人類型")]
    public EnemyType targetEnemyType;
    [Tooltip("WinConsecutive：需要連勝的場數（未來）")]
    [Min(1)] public int requiredCount = 1;

    [Header("任務獎勵")]
    public RewardData successReward;  // 任務成功獎勵
    public RewardData failureReward;  // 任務失敗獎勵
}

[System.Serializable]
public class RewardData
{
    public int gold;                  // 獎勵金錢數量
    public string item;               // 獎勵物品名稱
}
