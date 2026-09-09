// 此 ScriptableObject 由 A_Good_Ink 使用 AI 生成。
// 用途：集中管理回合制戰鬥中各種「狀態效果」（燒傷、中毒、暈眩、聖光庇護、星之威嚇、誓約…）的設定值，
//       取代原本寫死在 BattleStatusEffect.SendEffectInfo() 的 switch。
// 執行期由 BattleDataProvider 從 Resources/SO_Battle 載入；ToBattleEffectInfo() 會轉回舊有的
// BattleEffectInfo（含 List<bool> 排列），因此不需更動戰鬥流程既有的資料結構。
// 資產由 Editor 工具「Tools/TurnBaseBattle/生成戰鬥資料 SO」自動生成。
using System;
using System.Collections.Generic;
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEngine;

/// <summary>回合制戰鬥狀態效果的設定資料表。</summary>
[CreateAssetMenu(fileName = "BattleStatusEffectData", menuName = "SO/Battle/Create SO_BattleStatusEffectData", order = 3)]
public class SO_BattleStatusEffectData : ScriptableObject
{
    /// <summary>單一狀態效果的完整設定。欄位以具名 bool 呈現，較 List&lt;bool&gt; 索引易讀；
    /// 轉回 <see cref="BattleEffectInfo"/> 時會依戰鬥流程期望的順序組回清單。</summary>
    [Serializable]
    public class StatusEffectEntry
    {
        [Header("狀態種類")]
        public BattleStatusEffectType effectType = BattleStatusEffectType.None;

        [Header("基本數值")]
        [Tooltip("效果值（傷害、減骰數、除數/乘數等）")] public int effectValue;
        [Tooltip("持續回合數")] public int lastTurn;
        [Tooltip("施加次數（可累加）")] public int addTimes = 1;

        [Header("移除規則")]
        [Tooltip("持續回合數歸零且觸發過後，是否可直接消除（如火燒需等回合結束才消）")]
        public bool isAbleToRemoveAfterEffect = true;
        [Tooltip("是否可用卡片消除")] public bool isRemovable;

        [Header("施展對象")]
        [Tooltip("是否作用於自己")] public bool isSelfAffecting;
        [Tooltip("是否作用於對手")] public bool isRivalAffecting;

        [Header("效果類型")]
        [Tooltip("是否跳過對象的回合")] public bool isSkippedTurn;
        [Tooltip("是否影響用卡的傷害/減傷計算")] public bool affectedCalculation;
        [Tooltip("影響計算時採用的運算方式")] public BattleValueOperation operationType = BattleValueOperation.NormalOperation;
        [Tooltip("是否影響骰子數量/數值")] public bool affectedDice;

        [Header("影響的數值種類")]
        public bool affectDamage;
        public bool affectHeal;
        public bool affectReceivedDamage;
        public bool affectReceivedHeal;

        [Header("生效時機")]
        public bool activateImmediately;
        public bool activateAtTurnStart;
        public bool activateAtTurnEnd;

        [Header("移除時機")]
        public bool removeImmediately;
        public bool removeAtTurnStart;
        public bool removeAtTurnEnd;

        [Header("回合持續性生效")]
        public bool isTurnBasedEffect;
        [Tooltip("持續回合條件：等於 / 大於 / 小於")]
        public bool isEqual;
        public bool isGreaterThan;
        public bool isLessThan;

        [Header("在誰的回合生效")]
        public bool activeOwnTurn;
        public bool activeRivalTurn;
        public bool activeAnyTurn;

        /// <summary>轉回戰鬥流程使用的 <see cref="BattleEffectInfo"/>。HasTakenEffect 一律以 false 起始（執行期狀態，非設定值）。</summary>
        public BattleEffectInfo ToBattleEffectInfo()
        {
            return new BattleEffectInfo(
                effectType,
                false, // HasTakenEffect：執行期起始一律 false
                effectValue,
                lastTurn,
                addTimes,
                isAbleToRemoveAfterEffect,
                isRemovable,
                isSelfAffecting,
                isRivalAffecting,
                isSkippedTurn,
                affectedCalculation,
                operationType,
                new List<bool> { affectDamage, affectHeal, affectReceivedDamage, affectReceivedHeal },
                affectedDice,
                new List<bool> { activateImmediately, activateAtTurnStart, activateAtTurnEnd },
                new List<bool> { removeImmediately, removeAtTurnStart, removeAtTurnEnd },
                isTurnBasedEffect,
                isEqual,
                isGreaterThan,
                isLessThan,
                new List<bool> { activeOwnTurn, activeRivalTurn, activeAnyTurn }
            );
        }
    }

    [Header("各狀態效果設定")]
    public List<StatusEffectEntry> effects = new List<StatusEffectEntry>();

    /// <summary>依狀態種類取得設定；找不到回傳 false。</summary>
    public bool TryGet(BattleStatusEffectType type, out StatusEffectEntry entry)
    {
        if (effects != null)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] != null && effects[i].effectType == type)
                {
                    entry = effects[i];
                    return true;
                }
            }
        }
        entry = null;
        return false;
    }
}
