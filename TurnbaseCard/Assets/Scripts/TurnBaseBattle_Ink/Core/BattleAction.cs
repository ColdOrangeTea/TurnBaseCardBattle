using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using System;
using Random = UnityEngine.Random;
using Unity.Mathematics;

/// <summary>
/// 戰鬥核心運算：接收用卡／回合事件，計算數值與狀態，並把結果寫回單位。
/// 依職責分散在數個 partial 檔：
/// 本檔＝核心流程（用卡、回合、單位資料同步）、
/// BattleAction.Effects＝狀態效果的觸發／移除／施加、
/// BattleAction.Calculation＝數值與 HP 計算、
/// BattleAction.Presentation＝特效顯示、
/// BattleAction.Cheat＝測試／作弊用方法。
/// </summary>
public partial class BattleAction : MonoBehaviour
{
    // private BattleTurnBaseEvent battleEvent = new BattleTurnBaseEvent();
    public TurnBaseBattleManager manager;
    CardCalculation calculation = new CardCalculation();
    StatusValueSetter valueSetter = new StatusValueSetter();
    // 這兩張表會透過 BattleDataProvider 讀取 Resources 內的 SO；
    // Unity 不允許在欄位初始化器（等同建構子）中呼叫 Resources.Load，故改在 Awake 才建立。
    List<BattleCardInfo> cardInfo;
    List<BattleEffectInfo> statusEffectInfo;

    [Header("以下是每回合暫存的生命值、傷害參數")]
    [Header("該回合行動者")]
    [SerializeField] private TurnBaseBattleUnitDisplayData temp_CardUser;
    [SerializeField] private int temp_UserMaxHp = 0;
    [SerializeField] private int temp_UserHp = 0;
    [SerializeField] private List<BattleStatusEffect> temp_UserStatusEffects = new List<BattleStatusEffect>();
    [SerializeField] bool temp_IsCurrentUserSkip;
    [SerializeField] private int temp_userDiceCount; // 使用者的目前骰子顆數  
    [SerializeField] private int turn_OriDiceCount; // 使用者這回合原本的目前骰子顆數  

    [Header("該回合的對手")]
    [SerializeField] private TurnBaseBattleUnitDisplayData temp_Target;
    [SerializeField] private int temp_TargetMaxHp = 0;
    [SerializeField] private int temp_TargetHp = 0;
    [SerializeField] private List<BattleStatusEffect> temp_TargetStatusEffects = new List<BattleStatusEffect>();
    [SerializeField] bool temp_IsCurrentTargetSkip;
    [SerializeField] private int temp_targetDiceCount; // 目前對手的目前骰子顆數  
    [Header("當前數值")]

    [SerializeField] private CardType temp_CardType = CardType.Undefined;
    [SerializeField] private BattleCardInfo temp_CardInfo;
    [SerializeField] private int temp_DiceValue; // 數值累加用  
    // [SerializeField] private CardData temp_CardData;

    void Awake()
    {
        // 在 Awake（而非欄位初始化器）建立資料表，避免 Resources.Load 於建構子被呼叫而報錯。
        cardInfo = GetBattleCardInfo();
        statusEffectInfo = GetBattleStatusEffectInfo();
        Init();
    }

    #region  "訂閱"

    void OnEnable()
    {
        DiceEvent.OnInfoOfCardAndDicesSent += ReadyToUseCard;
    }
    void OnDisable()
    {
        DiceEvent.OnInfoOfCardAndDicesSent -= ReadyToUseCard;
    }

    void ReadyToUseCard(CardData card, int value) // 玩家用卡
    {
        // temp_CardInfo = BattleCard.SendCardInfo(CardType.Undefined);
        (temp_CardType, temp_CardInfo, temp_DiceValue) = (card.cardType, card.GetBattleCardInfo(), value);
        OnUseCard();
    }

    #endregion

    #region  Turn Behavior

    public void OnTurnEnd() // 回合結束時觸發效果
    {
        ValueForOperation values = InitValueForOperation();

        values = OnEffectActivate(values, BattleStatusEffectWorkTiming.ACTIVATES_AT_TURN_END);
        SetStructValuesBackToTemp_Info(values);
        UpdateTemp_UnitData();
    }
    public void OnTurnStart() // 回合開始時觸發效果
    {
        ValueForOperation values = InitValueForOperation();

        values = OnEffectActivate(values, BattleStatusEffectWorkTiming.ACTIVATES_AT_TURN_START);
        SetStructValuesBackToTemp_Info(values);

        // Debug.Log($"回合行動者: {values.IsUserSkippedTurn} 目標: {values.IsTargetSkippedTurn}");

        manager.SetIsSkipped_Target(values.IsTargetSkippedTurn);
        manager.SetIsSkipped_CardUser(values.IsUserSkippedTurn);
        manager.SetIsAssignFixedDiceValue_CardUser(values.IsAssignFixedDiceValue_CardUser);
        manager.SetAssignFixedDiceValue(values.AssignFixedDiceValue);
        manager.SetAssignFixedDiceCount(values.AssignFixedDiceCount);


        UpdateTemp_UnitData();
    }

    #endregion

    #region "敵人行動"
    public BattleCardInfo InitBattleCardInfo(CardType cardType)
    {
        BattleCardInfo info = BattleCard.SendCardInfo(cardType);
        return info;
    }

    public void EnemyAttack(TurnBaseBattleUnitDisplayData unitData)
    {
        BattleUnitEnemyDisplayData enemyData = unitData as BattleUnitEnemyDisplayData; // 若不是 as BattleUnitEnemyDisplayData 會回傳 null

        if (enemyData != null)
        {
            if (temp_userDiceCount > 0) // 敵人有沒有色子
            {
                int useingDiceCount = 0;
                while (useingDiceCount < temp_userDiceCount)
                {
                    temp_DiceValue = Random.Range(1, 7);
                    (CardType cardType, int value) = enemyData.EnemyBehavior(temp_DiceValue);
                    (temp_CardType, temp_CardInfo, temp_DiceValue) = (cardType, InitBattleCardInfo(cardType), value);

                    OnUseCard();
                    useingDiceCount++;
                    Debug.Log($"敵人行動次數: {useingDiceCount} 卡種類: " + cardType + " 骰數:" + value);

                }

            }
            else
            {
                Debug.Log("敵人沒骰子，不行動");
            }
        }
    }
    #endregion

    #region  "玩家行動"

    ValueForOperation OnEffectActivate(ValueForOperation values, BattleStatusEffectWorkTiming timing)
    {
        // Debug.Log($"OnEffectActivate 1 目前回合時機: {timing}  回合者狀態數: {values.UserStatus.Count} 對象狀態數: {values.TargetStatus.Count}");

        values = AllUnitTriggerEffect(values, timing);
        // Debug.Log($"OnEffectActivate 2 目前回合時機: {timing}  回合者狀態數: {values.UserStatus.Count} 對象狀態數: {values.TargetStatus.Count}");
        values = AllUnitReduceEffectLevels(values, timing);
        values = AllUnitCalculate(values);


        return values;
    }

    #region  OnEffectActivate 

    ValueForOperation AllUnitTriggerEffect(ValueForOperation values, BattleStatusEffectWorkTiming timing)
    {
        ActivateEffectAtTurnTiming(temp_Target, ref values.TargetStatus, ref values.BaseValue_ToRival, ref values.TargetDiceCount, ref values.IsTargetSkippedTurn, false, timing, ref values.IsAssignFixedDiceValue_CardUser, ref values.AssignFixedDiceValue, ref values.AssignFixedDiceCount);
        ActivateEffectAtTurnTiming(temp_CardUser, ref values.UserStatus, ref values.BaseValue_ToUser, ref values.UserDiceCount, ref values.IsUserSkippedTurn, true, timing, ref values.IsAssignFixedDiceValue_CardUser, ref values.AssignFixedDiceValue, ref values.AssignFixedDiceCount);
        return values;
    }

    #region AllUnitTriggerEffect Module Functions

    void ActivateEffectAtTurnTiming(TurnBaseBattleUnitDisplayData unit, ref List<BattleStatusEffect> unitStatusEffect, ref int baseValue, ref int unitDiceCount, ref bool isUnitSkipped, bool isOnOwnTurn, BattleStatusEffectWorkTiming activateTiming,
     ref bool isAssignFixedDiceValue_CardUser, ref int assignFixedDiceValue, ref int assignFixedDiceCount)
    {
        if (unitStatusEffect == null) return; // 沒狀態就不用繼續了

        // BattleStatusEffectActiveTurnTiming activeTurnTiming = isOnOwnTurn == true ? BattleStatusEffectActiveTurnTiming.OWN_TURN_ONLY :
        // isOnRivalTurn == true ? BattleStatusEffectActiveTurnTiming.RIVAL_TURN_ONLY : BattleStatusEffectActiveTurnTiming.BOTH_TURN;

        // 找出在哪個時機點觸發的狀態
        List<BattleStatusEffect> triggerEffects = FindActivateStatusOnUnit(unitStatusEffect, activateTiming);

        if (triggerEffects == null) return;

        // 找出在誰的回合觸發的狀態
        List<BattleStatusEffect> WorkOnOwnTurnEffects = FindActivateStatusWorkOnWhoseTurn(triggerEffects, BattleStatusEffectActiveTurn.OWN_TURN_ONLY);
        List<BattleStatusEffect> WorkOnRivalTurnEffects = FindActivateStatusWorkOnWhoseTurn(triggerEffects, BattleStatusEffectActiveTurn.RIVAL_TURN_ONLY);
        List<BattleStatusEffect> WorkOnBothTurnEffects = FindActivateStatusWorkOnWhoseTurn(triggerEffects, BattleStatusEffectActiveTurn.BOTH_TURN);

        if (WorkOnBothTurnEffects != null && WorkOnBothTurnEffects.Count > 0)
        {
            foreach (BattleStatusEffect effect in WorkOnBothTurnEffects)
            {
                (baseValue, unitDiceCount, isUnitSkipped) = TriggerEffect(effect, ref unitDiceCount, ref isUnitSkipped, activateTiming, ref isAssignFixedDiceValue_CardUser, ref assignFixedDiceValue, ref assignFixedDiceCount);
                DisplayEffectOnTurn(unit, effect.GetEffectType(), baseValue);
                // Debug.Log($"WorkOnBothTurnEffects 觸發的狀態: {effect.GetEffectType()} 回合數: {effect.GetLastTurn()}");
                UpdateTemp_UnitData();
            }
        }
        if (!isOnOwnTurn && WorkOnRivalTurnEffects != null && WorkOnRivalTurnEffects.Count > 0)
        {
            foreach (BattleStatusEffect effect in WorkOnRivalTurnEffects)
            {
                (baseValue, unitDiceCount, isUnitSkipped) = TriggerEffect(effect, ref unitDiceCount, ref isUnitSkipped, activateTiming, ref isAssignFixedDiceValue_CardUser, ref assignFixedDiceValue, ref assignFixedDiceCount);
                UpdateTemp_UnitData();
            }
        }

        if (isOnOwnTurn && WorkOnOwnTurnEffects != null && WorkOnOwnTurnEffects.Count > 0)
        {
            foreach (BattleStatusEffect effect in WorkOnOwnTurnEffects)
            {
                (baseValue, unitDiceCount, isUnitSkipped) = TriggerEffect(effect, ref unitDiceCount, ref isUnitSkipped, activateTiming, ref isAssignFixedDiceValue_CardUser, ref assignFixedDiceValue, ref assignFixedDiceCount);
                UpdateTemp_UnitData();
            }
        }

        UpdateTemp_UnitData();
    }

    #region  ActivateEffectAtTurnTiming Module 

    public (int, int, bool) TriggerEffect(BattleStatusEffect effect, ref int unitDiceCount, ref bool isUnitSkipped, BattleStatusEffectWorkTiming timing,
     ref bool isAssignFixedDiceValue_CardUser, ref int assignFixedDiceValue, ref int assignFixedDiceCount)
    {
        int baseValue = 0;
        (baseValue, unitDiceCount, isUnitSkipped) = calculation.TriggerEffect(effect, ref unitDiceCount, ref isUnitSkipped, timing, ref isAssignFixedDiceValue_CardUser, ref assignFixedDiceValue, ref assignFixedDiceCount);
        effect.SetHasTakenEffect(true);
        // Debug.Log($"{effect.GetEffectType()} {effect.GetHasTakenEffect()} isUnitSkipped: {isUnitSkipped}");
        return (baseValue, unitDiceCount, isUnitSkipped);
    }
    // 找出在誰回合需操作的狀態List
    List<BattleStatusEffect> FindActivateStatusWorkOnWhoseTurn(List<BattleStatusEffect> unitStatus, BattleStatusEffectActiveTurn whoseTurn)
    {
        if (unitStatus == null || unitStatus.Count <= 0) return null; // 沒有狀態
        int type = Convert.ToInt32(whoseTurn); // 轉換成 int
        List<BattleStatusEffect> existedEffects = unitStatus.FindAll(e => e.GetEffectActiveTurnList()[type]);
        if (existedEffects == null || existedEffects.Count <= 0) return null; // 沒有會在時間點觸發效果的狀態
        return existedEffects;
    }

    // 找出指定時間點需操作的狀態List
    List<BattleStatusEffect> FindActivateStatusOnUnit(List<BattleStatusEffect> unitStatus, BattleStatusEffectWorkTiming timing)
    {
        if (unitStatus == null || unitStatus.Count <= 0) return null; // 沒有狀態
        int type = Convert.ToInt32(timing); // 轉換成 int
        List<BattleStatusEffect> existedEffects = unitStatus.FindAll(e => e.GetActivatesTimingList()[type]);
        if (existedEffects == null || existedEffects.Count <= 0) return null; // 沒有會在時間點觸發效果的狀態
        return existedEffects;
    }

    #endregion

    #endregion

    /// <summary>減少效果持續時間的層數</summary>
    ValueForOperation AllUnitReduceEffectLevels(ValueForOperation values, BattleStatusEffectWorkTiming timing)
    {
        // Debug.Log($"AllUnitReduceEffect 1 目前回合時機: {timing}  回合者狀態數: {values.UserStatus.Count} 對象狀態數: {values.TargetStatus.Count}");
        RemoveEffectAtTurnTiming(ref values.TargetStatus, ref values.TargetDiceCount, ref values.IsTargetSkippedTurn, false, timing);
        RemoveEffectAtTurnTiming(ref values.UserStatus, ref values.UserDiceCount, ref values.IsUserSkippedTurn, true, timing);
        return values;
    }

    #region AllUnitReduceEffectLevels Module Functions

    void RemoveEffectAtTurnTiming(ref List<BattleStatusEffect> unitStatusEffect, ref int unitDiceCount, ref bool isUnitSkipped, bool isOnOwnTurn, BattleStatusEffectWorkTiming turnTiming)
    {
        // Debug.Log($"是是否為NULL: {unitStatusEffect == null}");

        if (unitStatusEffect == null) return; // 沒狀態就不用繼續了

        // 找出在哪個時機點移除的狀態
        List<BattleStatusEffect> triggerEffects = FindRemoveStatusOnUnit(unitStatusEffect, turnTiming);

        if (triggerEffects == null) return;
        // Debug.Log($"是是否為NULL: {triggerEffects == null} 數量: {triggerEffects.Count}");

        // 找出在誰的回合移除的狀態
        List<BattleStatusEffect> WorkOnOwnTurnEffects = FindRemoveStatusWorkOnWhoseTurn(triggerEffects, BattleStatusEffectActiveTurn.OWN_TURN_ONLY);
        List<BattleStatusEffect> WorkOnRivalTurnEffects = FindRemoveStatusWorkOnWhoseTurn(triggerEffects, BattleStatusEffectActiveTurn.RIVAL_TURN_ONLY);
        List<BattleStatusEffect> WorkOnBothTurnEffects = FindRemoveStatusWorkOnWhoseTurn(triggerEffects, BattleStatusEffectActiveTurn.BOTH_TURN);
        // Debug.Log($" WorkOnBothTurnEffects NULL: {WorkOnBothTurnEffects == null} WorkOnOwnTurnEffects NULL: {WorkOnOwnTurnEffects == null} WorkOnRivalTurnEffects NULL: {WorkOnRivalTurnEffects == null} ");

        // Debug.Log($" WorkOnBothTurnEffects: {WorkOnBothTurnEffects.Count} WorkOnOwnTurnEffects: {WorkOnOwnTurnEffects.Count} WorkOnRivalTurnEffects: {WorkOnRivalTurnEffects.Count} ");

        if (WorkOnBothTurnEffects != null && WorkOnBothTurnEffects.Count > 0)
        {
            foreach (BattleStatusEffect effect in WorkOnBothTurnEffects)
            {
                ReduceEffectLastTurn(WorkOnBothTurnEffects, ref unitStatusEffect, isOnOwnTurn, turnTiming);
                Debug.Log($"WorkOnBothTurnEffects 移除的狀態為: {effect.GetEffectType()} {effect.GetHasTakenEffect()} isUnitSkipped: {isUnitSkipped}");

                (unitDiceCount, isUnitSkipped) = calculation.RemoveEffect(effect, ref unitDiceCount, ref isUnitSkipped, turnTiming);

                unitStatusEffect = RemoveEffectsLastTurn(ref unitStatusEffect);
                // Debug.Log($"{effect.GetEffectType()} {effect.GetHasTakenEffect()} isUnitSkipped: {isUnitSkipped}");

                UpdateTemp_UnitData();
            }
        }
        if (!isOnOwnTurn && WorkOnRivalTurnEffects != null && WorkOnRivalTurnEffects.Count > 0)
        {
            foreach (BattleStatusEffect effect in WorkOnRivalTurnEffects)
            {
                // (unitDiceCount, isUnitSkipped) = calculation.RemoveEffect(effect, ref unitDiceCount, ref isUnitSkipped, turnTiming);
                ReduceEffectLastTurn(WorkOnRivalTurnEffects, ref unitStatusEffect, isOnOwnTurn, turnTiming);

                (unitDiceCount, isUnitSkipped) = calculation.RemoveEffect(effect, ref unitDiceCount, ref isUnitSkipped, turnTiming);

                unitStatusEffect = RemoveEffectsLastTurn(ref unitStatusEffect);
                UpdateTemp_UnitData();
            }
        }

        if (isOnOwnTurn && WorkOnOwnTurnEffects != null && WorkOnOwnTurnEffects.Count > 0)
        {
            foreach (BattleStatusEffect effect in WorkOnOwnTurnEffects)
            {
                ReduceEffectLastTurn(WorkOnOwnTurnEffects, ref unitStatusEffect, isOnOwnTurn, turnTiming);

                (unitDiceCount, isUnitSkipped) = calculation.RemoveEffect(effect, ref unitDiceCount, ref isUnitSkipped, turnTiming);

                unitStatusEffect = RemoveEffectsLastTurn(ref unitStatusEffect);
                UpdateTemp_UnitData();
            }
        }


        UpdateTemp_UnitData();
    }

    #region RemoveEffectAtTurnTiming Module Functions

    List<BattleStatusEffect> RemoveEffectsLastTurn(ref List<BattleStatusEffect> unitStatusEffect)
    {

        // 移除 `LastTurn <= 0`、觸發了才能移除的狀態
        unitStatusEffect.RemoveAll(effect => effect.GetLastTurn() <= 0 && effect.GetIsAbleToRemoveAfterEffect() == false && effect.GetHasTakenEffect() == true);

        // 在回合結束時移除 `LastTurn <= 0` 且允許移除，不一定要觸發過才能移除的狀態
        unitStatusEffect.RemoveAll(effect => effect.GetLastTurn() <= 0 && effect.GetIsAbleToRemoveAfterEffect() == true);
        return unitStatusEffect;
    }

    void ReduceEffectLastTurn(List<BattleStatusEffect> removeStatusEffect, ref List<BattleStatusEffect> unitStatusEffect, bool isOnOwnTurn, BattleStatusEffectWorkTiming turnTiming)
    {
        if (removeStatusEffect == null && removeStatusEffect.Count <= 0) return; // 沒狀態就不用繼續了
        foreach (BattleStatusEffect effect in removeStatusEffect)
        {
            effect.SetLastTurn(valueSetter.ReduceLastTimes(effect.GetLastTurn()));// 傳進來的是此狀態的回合數， 減少持續回合，並移除
            // Debug.Log($"減少的狀態: {effect.GetEffectType()} 回合數: {effect.GetLastTurn()}");

            // 找到符合條件的索引
            int index = unitStatusEffect.FindIndex(e => e.GetEffectType() == effect.GetEffectType());

            // 若找到相符項目，則替換
            if (index >= 0)
            {
                unitStatusEffect[index] = effect;
            }
            else
            {
                Debug.LogError("Can Not Find the Effect in unitStatusEffect " + effect.GetEffectType());
            }

        }
        // unitStatusEffect = RemoveEffectsLastTurn(ref unitStatusEffect);
        UpdateTemp_UnitData();
        return;
    }

    // 找出在誰回合需操作的狀態List
    List<BattleStatusEffect> FindRemoveStatusWorkOnWhoseTurn(List<BattleStatusEffect> unitStatus, BattleStatusEffectActiveTurn whoseTurn)
    {
        if (unitStatus == null || unitStatus.Count <= 0) return null; // 沒有狀態
        int type = Convert.ToInt32(whoseTurn); // 轉換成 int
        List<BattleStatusEffect> existedEffects = unitStatus.FindAll(e => e.GetEffectActiveTurnList()[type]);
        if (existedEffects == null || existedEffects.Count <= 0) return null; // 沒有會在時間點觸發效果的狀態
        return existedEffects;
    }

    // 找出指定時間點需操作的狀態List
    List<BattleStatusEffect> FindRemoveStatusOnUnit(List<BattleStatusEffect> unitStatus, BattleStatusEffectWorkTiming timing)
    {
        if (unitStatus == null || unitStatus.Count <= 0) return null; // 沒有狀態
        int type = Convert.ToInt32(timing); // 轉換成 int
        List<BattleStatusEffect> existedEffects = unitStatus.FindAll(e => e.GetRemovesTimingList()[type]);
        if (existedEffects == null || existedEffects.Count <= 0) return null; // 沒有會在時間點觸發效果的狀態
        return existedEffects;
    }

    #endregion

    #endregion

    #endregion

    public void OnUseCard() // 用卡時呼叫
    {
        ValueForOperation values = UseCard(); // 使用卡片  
        if (temp_CardInfo.FunctionalType != BattleFunctionalCardType.Dice) // 純附加狀態可以直接跳過
        {
            values = AllUnitCalculate(values);
            if (temp_CardInfo.IsUsedToAttack)
            {
                DisplayEffectOnUseCard(temp_Target, temp_CardInfo.EffectType, values.FinalValue_ToRival);
                if (temp_CardInfo.CardType == CardType.HeavyAttack || temp_CardInfo.CardType == CardType.LazerGun || temp_CardInfo.CardType == CardType.Oath)
                {
                    DisplayEffectOnUseCard(temp_CardUser, temp_CardInfo.EffectType, values.FinalValue_ToUser);
                }

            }

            if (temp_CardInfo.IsFunctional)
            {
                if (GetStatusEffectInfo(temp_CardInfo.EffectType).IsRivalAffecting)
                {
                    DisplayEffectOnUseCard(temp_Target, temp_CardInfo.EffectType, values.FinalValue_ToRival);
                }

                if (GetStatusEffectInfo(temp_CardInfo.EffectType).IsSelfAffecting)
                {
                    DisplayEffectOnUseCard(temp_CardUser, temp_CardInfo.EffectType, values.FinalValue_ToUser);
                }
                if (temp_CardInfo.EffectType == BattleStatusEffectType.None) // 目前只有回復狀態
                {
                    DisplayEffectOnUseCard(temp_CardUser, temp_CardInfo.EffectType, values.FinalValue_ToUser);

                }
            }

            // DisplayEffectOnUseCard(temp_CardUser, temp_CardInfo.EffectType, values.FinalValue_ToUser);
        }
        else
        {
            if (temp_CardInfo.IsAddEffectStatus)
                DisplayEffectOnUseCard(temp_Target, temp_CardInfo.EffectType, values.FinalValue_ToRival);

        }

        SetStructValuesBackToTemp_Info(values);
        UpdateTemp_UnitData();

    }

    void SetStructValuesBackToTemp_Info(ValueForOperation values)
    {
        temp_UserStatusEffects = values.UserStatus;
        temp_userDiceCount = values.UserDiceCount;
        temp_IsCurrentUserSkip = values.IsUserSkippedTurn;

        temp_TargetStatusEffects = values.TargetStatus;
        temp_targetDiceCount = values.TargetDiceCount;
        temp_IsCurrentTargetSkip = values.IsTargetSkippedTurn;
    }

    ValueForOperation AddEffect(ValueForOperation values) // 造成卡片能力
    {
        BattleStatusEffect effect = FindStatusEffectByCardData(temp_CardInfo); // 找出卡片要施加的狀態

        if (effect == null || effect.GetEffectType() == BattleStatusEffectType.None)
        {
            // Debug.Log("無狀態需施加。");
            return values;
        }
        // Debug.Log($"需施加: {effect.GetEffectType()}");

        // 根據狀態作用的對象，分別處理 User 或 Target
        if (values.UserStatus != null && values.TargetStatus != null)
        {
            if (effect.GetIsSelfAffecting() && !effect.GetIsRivalAffecting())
            {
                values = ApplyEffectToTarget(values, effect, values.UserStatus, ref values.IsUserSkippedTurn);
            }
            else if (!effect.GetIsSelfAffecting() && effect.GetIsRivalAffecting())
            {
                values = ApplyEffectToTarget(values, effect, values.TargetStatus, ref values.IsTargetSkippedTurn);
            }
            else if (effect.GetIsSelfAffecting() && effect.GetIsRivalAffecting())
            {
                // Debug.Log("作用於雙方。");
                values = ApplyEffectToTarget(values, effect, values.UserStatus, ref values.IsUserSkippedTurn);
                values = ApplyEffectToTarget(values, effect, values.TargetStatus, ref values.IsTargetSkippedTurn);
            }
        }
        return values;
    }

    #region AddEffect Module Functions

    bool IfFoundCard(List<BattleCardInfo> specifiedCards)
    {
        foreach (BattleCardInfo info in specifiedCards) // 指定類型的卡片
        {
            // Debug.LogWarning($"IfFindCard {info.CardType} {temp_CardType}");

            if ((int)info.CardType == (int)temp_CardType)
            {
                // Debug.LogWarning($"IfFindCard {info.CardType} {temp_CardType}");
                return true;
            }
            else
            {
                return false;
            }
        }
        // Debug.LogWarning($"IfFindCard 輸入的資料不正確");
        return false;
    }

    // 把加狀態的程式模組化，處理狀態施加的核心邏輯
    ValueForOperation ApplyEffectToTarget(ValueForOperation values, BattleStatusEffect effect, List<BattleStatusEffect> statusList, ref bool isSkippedTurn)
    {
        // 查找目標已有的指定狀態
        BattleStatusEffect existingEffect = FindSpecifiedStatusInUnit(statusList, effect.GetEffectType());
        if (existingEffect != null) // 當前目標身上具有這個用卡施加的狀態
        {
            (values.EffectValue, values.LastTurn, values.AddTimes) = (existingEffect.GetEffectValue(), existingEffect.GetLastTurn(), existingEffect.GetDotAddTimes());
            valueSetter.StackTimes(existingEffect, existingEffect.GetLastTurn(), existingEffect.GetDotAddTimes());
            isSkippedTurn = valueSetter.IfGetDizzinessEffect(isSkippedTurn);

            (values.EffectValue, values.LastTurn, values.AddTimes) = (existingEffect.GetEffectValue(), existingEffect.GetLastTurn(), existingEffect.GetDotAddTimes());
            // Debug.Log($"已存在狀態 {effect.GetEffectType()} - LastTurn: {existingEffect.GetLastTurn()}, AddTimes: {existingEffect.GetDotAddTimes()}");

        }
        else // 當前目標身上沒有這個狀態
        {
            // 若無相符狀態則新增
            BattleStatusEffect newEffect = new BattleStatusEffect(effect.GetEffectType());
            statusList.Add(newEffect);
            isSkippedTurn = valueSetter.IfGetDizzinessEffect(values.IsTargetSkippedTurn);
            // Debug.Log("未中過，新增狀態。");
        }
        return values;
    }

    // 找出這個單位是否擁有參數傳入的狀態類型
    BattleStatusEffect FindSpecifiedStatusInUnit(List<BattleStatusEffect> statusEffect, BattleStatusEffectType effectType)
    {
        try
        {
            if (statusEffect != null && statusEffect.Count > 0)
            {
                // 查找是否有符合條件的效果
                BattleStatusEffect existedEffect = statusEffect.Find(e => e.GetEffectType() == effectType);

                // 如果找不到效果，則回傳空的效果
                if (existedEffect == null)
                {
                    // Debug.LogWarning("No matching effect found.");
                    return null;
                }

                // 嘗試獲取效果的值
                (int effectValue, int turn, int addTimes) = (existedEffect.GetEffectValue(), existedEffect.GetLastTurn(), existedEffect.GetDotAddTimes());

                // Debug.Log("existedEffect: " + existedEffect.GetEffectType());
                return existedEffect;
            }
            else
            {
                // Debug.Log("Unit doesn't have any effects.");
                return null;
            }
        }
        catch (NullReferenceException ex)
        {
            Debug.LogError("A null reference occurred: " + ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError("An unexpected error occurred: " + ex.Message);
            return null;
        }
    }

    public BattleStatusEffect FindStatusEffectByCardData(BattleCardInfo cardInfo) // 找出卡片要施加的狀態，用 卡的類別 決定何種 效果
    {
        BattleStatusEffect effect = new BattleStatusEffect(); // 空的
        if (!cardInfo.IsAddEffectStatus)
        {
            effect.SetEffectType(BattleStatusEffectType.None);
            return effect;
        }
        else
        {
            foreach (int type in Enum.GetValues(typeof(BattleStatusEffectType)))
            {
                if ((int)cardInfo.EffectType == type)
                {
                    effect = new BattleStatusEffect(cardInfo.EffectType);
                    return effect;
                }
            }
            return effect;
        }
    }

    #endregion

    private ValueForOperation UseCard()
    {
        ValueForOperation values = InitValueForOperation();

        if (temp_CardInfo.EffectType != BattleStatusEffectType.None)
        {
            FindUnitStatusWhenUseCard(values.UserStatus, values, temp_CardInfo);
            FindUnitStatusWhenUseCard(values.TargetStatus, values, temp_CardInfo);
        }
        if (temp_CardInfo.IsAddEffectStatus)
            values = AddEffect(values);

        if (temp_CardInfo.IsUsedToAttack && temp_CardInfo.IsFunctional)
        {
            values = HandleBothTypeCard(values);
        }
        else if (temp_CardInfo.IsUsedToAttack && !temp_CardInfo.IsFunctional)
        {
            values = HandleAttackCard(values);
        }
        else if (!temp_CardInfo.IsUsedToAttack && temp_CardInfo.IsFunctional)
        {
            values = HandleFunctionalCard(values);
        }
        else
        {
            // Debug.LogWarning("卡片不是攻擊型也不是功能型，也不是兩者具備，勾選類型不正確 加:" + temp_CardInfo.IsAddEffectStatus + " 攻:"
            //  + temp_CardInfo.IsUsedToAttack + " 功:" + temp_CardInfo.IsFunctional + " "
            //  + temp_CardInfo.CardType + " " + temp_CardInfo.FunctionalType);
        }
        // Debug.Log($"對敵人的數值: {values.BaseValue_ToRival} 對自己的數值{values.BaseValue_ToUser}");

        return values;
    }

    #region UseCard Module Functions

    void FindUnitStatusWhenUseCard(List<BattleStatusEffect> unitStatus, ValueForOperation values, BattleCardInfo curCardInfo)
    {
        // BattleEffectInfo info = GetStatusEffectInfo(BattleStatusEffectType.None); // 先建構空的資料
        if (curCardInfo.EffectType != BattleStatusEffectType.None)
        {
            BattleEffectInfo info = GetStatusEffectInfo(curCardInfo.EffectType);
            if (unitStatus != null)  // 根據卡片造成的狀態作用的對象，User 或 Target
            {
                BattleStatusEffect existingEffect = FindSpecifiedStatusInUnit(unitStatus, info.EffectType); // 查找目標已有的指定狀態 
                if (existingEffect != null)
                {
                    (values.EffectValue, values.LastTurn, values.AddTimes) = (existingEffect.GetEffectValue(), existingEffect.GetLastTurn(), existingEffect.GetDotAddTimes());
                }
            }
        }
    }
    // 處理同時具備攻擊與功能性的卡片
    private ValueForOperation HandleBothTypeCard(ValueForOperation values)
    {

        (values.BaseValue_ToRival, values.BaseValue_ToUser) = calculation.BothTypeCard(temp_CardType, values.UserDiceValue, values.EffectValue, values.LastTurn, values.AddTimes, values.UserDiceCount, values.TargetDiceCount);
        Debug.Log($" HandleBothTypeCard 對敵人的數值: {values.BaseValue_ToRival} 對自己的數值{values.BaseValue_ToUser}");

        return values;
        // Debug.LogWarning("卡片是攻擊型也是功能型" + temp_CardInfo.IsAddEffectStatus + " 攻:" + temp_CardInfo.IsUsedToAttack + " 功:" + temp_CardInfo.IsFunctional + " " + temp_CardInfo.CardType + " " + temp_CardInfo.FunctionalType);
    }

    // 處理單純攻擊型的卡片
    private ValueForOperation HandleAttackCard(ValueForOperation values)
    {
        (values.BaseValue_ToRival, values.BaseValue_ToUser) = calculation.AttackedCard(temp_CardType, values.UserDiceValue, values.LastTurn, values.AddTimes);
        return values;

        // Debug.LogWarning("卡片是攻擊型" + temp_CardInfo.IsAddEffectStatus + " 攻:" + temp_CardInfo.IsUsedToAttack + " 功:" + temp_CardInfo.IsFunctional + " " + temp_CardInfo.CardType + temp_CardInfo.FunctionalType);
    }

    // 處理單純功能型的卡片
    private ValueForOperation HandleFunctionalCard(ValueForOperation values)
    {
        // Debug.LogWarning("卡片是功能型!!!" + temp_CardInfo.IsAddEffectStatus + " 攻:" + temp_CardInfo.IsUsedToAttack + " 功:" + temp_CardInfo.IsFunctional + " " + temp_CardInfo.CardType + " " + temp_CardInfo.FunctionalType);

        switch (temp_CardInfo.FunctionalType)
        {
            case BattleFunctionalCardType.Heal:
                (values.BaseValue_ToRival, values.BaseValue_ToUser, values.UserDiceValue, values.UserDiceCount, values.TargetDiceCount) = calculation.HealFunctionalCard(temp_CardType, values.UserDiceValue, values.UserDiceCount, values.TargetDiceCount);
                break;
            case BattleFunctionalCardType.EffectStatus:
                (values.UserStatus, values.TargetStatus) = calculation.EffectStatusFunctionalCard(temp_CardType, values.UserDiceValue, values.UserDiceCount, values.TargetDiceCount, values.UserStatus, values.TargetStatus);
                break;
            case BattleFunctionalCardType.Dice:
                int oriDiceCount = values.UserDiceCount;
                (values.BaseValue_ToRival, values.BaseValue_ToUser, values.UserDiceValue, values.UserDiceCount, values.TargetDiceCount) = calculation.DiceFunctionalCard(temp_CardType, values.UserDiceValue, values.UserDiceCount, values.TargetDiceCount);
                temp_userDiceCount = values.UserDiceCount;
                manager.GetDiceSystem().DiceFunction(temp_CardType, values, oriDiceCount);
                break;
            case BattleFunctionalCardType.ValueCalculation:
                (values.BaseValue_ToRival, values.BaseValue_ToUser, values.UserDiceValue, values.UserDiceCount, values.TargetDiceCount) = calculation.ValueCalculationFunctionalCard(temp_CardType, values.UserDiceValue, values.UserDiceCount, values.TargetDiceCount);
                break;
        }
        return values;

    }
    #endregion

    ValueForOperation InitValueForOperation()
    {
        ValueForOperation values = new ValueForOperation(temp_CardUser.GetUnitName(), temp_Target.GetUnitName(), 0, 0, 0, 0,
        0, 0, 0, temp_IsCurrentUserSkip, temp_IsCurrentTargetSkip, temp_DiceValue,
        temp_userDiceCount, temp_targetDiceCount, temp_UserStatusEffects, temp_TargetStatusEffects, false, false, false, false, 0, 0);


        return values;
    }
    static List<BattleEffectInfo> GetBattleStatusEffectInfo()
    {
        List<BattleEffectInfo> temp_StatusInfo = new List<BattleEffectInfo>();
        // 先判別屬性的類別
        foreach (BattleStatusEffectType type in Enum.GetValues(typeof(BattleStatusEffectType)))
        {
            if (type != BattleStatusEffectType.None)
            {
                BattleStatusEffect battleStatus = new BattleStatusEffect(type);
                temp_StatusInfo.Add(battleStatus.GetBattleEffectInfo());
            }
        }
        return temp_StatusInfo;
    }
    static List<BattleCardInfo> GetBattleCardInfo()
    {
        List<BattleCardInfo> temp_BattleCardInfo = new List<BattleCardInfo>();
        // 先判別卡的類別
        foreach (CardType type in Enum.GetValues(typeof(CardType)))
        {
            if (type != CardType._EnemyCard_ && type != CardType.Undefined)
            {
                BattleCard battleCard = new BattleCard(type);
                temp_BattleCardInfo.Add(battleCard.GetBattleCardInfo());
            }
            // Debug.Log($"{type} 卡片 {battleCards.Count}");
        }
        return temp_BattleCardInfo;
    }
    BattleEffectInfo GetStatusEffectInfo(BattleStatusEffectType type)
    {
        foreach (BattleEffectInfo bei in statusEffectInfo)
        {
            if (bei.EffectType == type)
            {
                BattleEffectInfo i = bei;
                // Debug.LogWarning($"This type is {type}.Type Effect {i}.");
                return i;
            }

        }
        // Debug.LogWarning($"This Card cause NO StatusEffect (type of THIS statusEffect is {type}).Or Can not find the effect. Return None Type Effect OwO.");
        BattleStatusEffect battleStatus = new BattleStatusEffect(BattleStatusEffectType.None);
        BattleEffectInfo info = battleStatus.GetBattleEffectInfo();
        return info;
    }

    #endregion

    // 回合開始時，取得資料
    public void GetUnitsInfo(TurnBaseBattleUnitDisplayData unit) // 使用卡 要取得卡片、骰子大小 當回合行動者的資料
    {
        // 觸發抓目標
        if (manager.currentTurn == unit.turnOrder) // 抓當回合的玩家(自己)
        {
            temp_CardUser = unit;
            temp_UserMaxHp = unit.GetOriginMaxHp();
            temp_UserHp = unit.GetCurHp();

            temp_UserStatusEffects = unit.GetStatusEffects();
            temp_userDiceCount = unit.GetCountOfDice();
            // temp_IsCurrentUserSkip = unit.GetIsCurrentUnitSkip();

            turn_OriDiceCount = unit.GetCountOfDice();
            // Debug.Log("當前回合: " + manager.currentTurn + " 當前行動者: " + temp_CardUser.GetUnitName());
        }
        else if (manager.currentTurn != unit.turnOrder) // 抓對手
        {
            temp_Target = unit;
            temp_TargetMaxHp = unit.GetOriginMaxHp();
            temp_TargetHp = unit.GetCurHp();

            temp_TargetStatusEffects = unit.GetStatusEffects();
            temp_targetDiceCount = unit.GetCountOfDice();
            // temp_IsCurrentTargetSkip = unit.GetIsCurrentUnitSkip();
            // Debug.Log("當前回合: " + manager.currentTurn + " 當前對手: " + temp_Target.GetUnitName());
        }
    }

    void UpdateTemp_UnitData()
    {
        SetTemp_InfoBackToTemp_Unit();
        SetUnitsBackToManager(temp_CardUser, temp_Target);// 把資料傳回去系統端、並傳回Unit
        manager.IfUnitDead(temp_CardUser);
        manager.IfUnitDead(temp_Target);
    }

    void SetUnitsBackToManager(TurnBaseBattleUnitDisplayData player1, TurnBaseBattleUnitDisplayData player2)
    {
        manager.SetTemp_CardUser(player1);
        manager.SetTemp_Target(player2);
        manager.SendUnitInfoToUnit();
    }
    public void ResetTempValue()
    {
        temp_CardUser = null;

        temp_UserMaxHp = 0;
        temp_UserHp = 0;

        temp_UserStatusEffects = null;
        temp_IsCurrentUserSkip = false;
        temp_DiceValue = 0;
        temp_userDiceCount = 0;

        // SetTemp_UserDiceCount(0);

        temp_CardInfo = BattleCard.SendCardInfo(CardType.Undefined);

        temp_Target = null;

        temp_TargetMaxHp = 0;
        temp_TargetHp = 0;

        temp_TargetStatusEffects = null;
        temp_IsCurrentTargetSkip = false;
        temp_targetDiceCount = 0;

        // SetTemp_TargetDiceCount(0);

    }

    public void SetTemp_InfoBackToTemp_Unit()
    {
        temp_CardUser.SetOriginMaxHp(temp_UserMaxHp);
        temp_CardUser.SetCurHp(temp_UserHp);
        temp_CardUser.SetStatusEffects(temp_UserStatusEffects);

        temp_CardUser.SetCountOfDice(temp_userDiceCount);
        // temp_CardUser.SetCountOfDice(turn_OriDiceCount);

        // temp_CardUser.SetIsCurrentUnitSkip(temp_IsCurrentUserSkip);


        temp_Target.SetOriginMaxHp(temp_TargetMaxHp);
        temp_Target.SetCurHp(temp_TargetHp);
        temp_Target.SetStatusEffects(temp_TargetStatusEffects);

        temp_Target.SetCountOfDice(temp_targetDiceCount);

        // temp_Target.SetIsCurrentUnitSkip(temp_IsCurrentTargetSkip);
    }

    #region "Get Set function"

    // 取得玩家的目前骰子顆數  
    public int GetTemp_UserDiceCount() => temp_userDiceCount;
    public void SetTemp_UserDiceCount(int diceCount) => temp_userDiceCount = diceCount;

    // 取得玩家的骰數
    // public int GetTemp_DiceValue() => temp_DiceValue;
    // public void SetTemp_DiceValue(int value) => temp_DiceValue = value;

    // // 取得玩家的卡片種類
    // public CardType GetTemp_CardType() => temp_CardType;
    // public void SetTemp_CardType(CardType value) => temp_CardType = value;

    // 取得對手的目前骰子顆數  
    // public int GetTemp_TargetDiceCount() => temp_targetDiceCount;
    // public void SetTemp_TargetDiceCount(int diceCount) => temp_targetDiceCount = diceCount;

    #endregion

    void Init()
    {
        manager = GetComponent<TurnBaseBattleManager>();
    }
}
