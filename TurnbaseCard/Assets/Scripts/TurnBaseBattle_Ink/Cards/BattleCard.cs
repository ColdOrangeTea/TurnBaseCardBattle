using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;

[System.Serializable] // 讓該類可以在 Inspector 中序列化
public class BattleCard
{
    [SerializeField]
    private CardType cardType;
    private BattleCardInfo cardInfo;

    // [Header("不須經由傷害計算，直傷/直接造成的影響數值")]
    [SerializeField]
    private List<int> trueValues = new List<int>();

    [Header("卡片骰數條件")]
    [Header("累積")]
    [SerializeField] private bool requiredAccumulatedDiceValue = false; // 須累積骰子點數才可觸發
    [SerializeField] private int accu_DiceValue = 0;

    [Header("指定數值")]
    [SerializeField] private bool requireDesignatedDiceValue = false; // 需要指定的骰子數值
    [SerializeField] private int desi_DiceValue = 0;
    [SerializeField] private bool isEqualTo;
    [SerializeField] private bool isGreaterThan;
    [SerializeField] private bool isLessThan;

    [Header("偶奇數")]

    [SerializeField] private bool requiredOddDiceValue = false; // 奇數
    [SerializeField] private bool requiredEvenDiceValue = false; // 偶數

    [Header("狀態")]
    [SerializeField] private bool isAddEffectStatus = false;

    [SerializeField] private BattleStatusEffectType effectType;

    [Header("卡片種類")]
    [SerializeField] private bool isUsedToAttack = false; // 攻擊類
    [SerializeField] private bool isFunctional = false; // 效果類 Needed FunctionalCardType

    [Header("功能類子分類")]
    [SerializeField] private BattleFunctionalCardType functionalType; // 影響使用卡片時的數值計算(攻擊、治療等)

    #region  "Get And Set Function"

    public CardType GetCardType() => cardType;
    public void SetCardType(CardType type) => cardType = type;

    public BattleCardInfo GetBattleCardInfo() => cardInfo;

    public List<int> GetTrueValues() => trueValues;
    public void SetTrueValues(List<int> values) => trueValues = values;

    // 卡片骰數條件 - 累積
    public bool GetRequiredAccumulatedDiceValue() => requiredAccumulatedDiceValue;
    public void SetRequiredAccumulatedDiceValue(bool value) => requiredAccumulatedDiceValue = value;

    public int GetAccumulatedDiceValue() => accu_DiceValue;
    public void SetAccumulatedDiceValue(int value) => accu_DiceValue = value;

    // 卡片骰數條件 - 指定數值
    public bool GetRequireDesignatedDiceValue() => requireDesignatedDiceValue;
    public void SetRequireDesignatedDiceValue(bool value) => requireDesignatedDiceValue = value;

    public int GetDesignatedDiceValue() => desi_DiceValue;
    public void SetDesignatedDiceValue(int value) => desi_DiceValue = value;

    public bool GetIsEqualTo() => isEqualTo;
    public void SetIsEqualTo(bool value) => isEqualTo = value;

    public bool GetIsGreaterThan() => isGreaterThan;
    public void SetIsGreaterThan(bool value) => isGreaterThan = value;

    public bool GetIsLessThan() => isLessThan;
    public void SetIsLessThan(bool value) => isLessThan = value;

    // 卡片骰數條件 - 偶奇數
    public bool GetRequiredOddDiceValue() => requiredOddDiceValue;
    public void SetRequiredOddDiceValue(bool value) => requiredOddDiceValue = value;

    public bool GetRequiredEvenDiceValue() => requiredEvenDiceValue;
    public void SetRequiredEvenDiceValue(bool value) => requiredEvenDiceValue = value;

    // 狀態
    public bool GetIsAddEffectStatus() => isAddEffectStatus;
    public void SetIsAddEffectStatus(bool value) => isAddEffectStatus = value;

    public BattleStatusEffectType GetEffectType() => effectType;
    public void SetEffectType(BattleStatusEffectType value) => effectType = value;


    // 卡片種類
    public bool GetIsUsedToAttack() => isUsedToAttack;
    public void SetIsUsedToAttack(bool value) => isUsedToAttack = value;

    public bool GetIsFunctional() => isFunctional;
    public void SetIsFunctional(bool value) => isFunctional = value;

    public BattleFunctionalCardType GetFunctionalCardType() => functionalType;
    public void SetFunctionalCardType(BattleFunctionalCardType value) => functionalType = value;
    #endregion

    public static BattleCardInfo SendCardInfo(CardType cardType)
    {
        switch (cardType)
        {
            case CardType.Attack:
                {
                    return new BattleCardInfo(
                        CardType.Attack, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        false, // 偶數
                        false, // 是否要附加狀態
                        BattleStatusEffectType.None, // 狀態類型
                        true, // 攻擊類
                        false, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.None // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.Poisoned:
                {
                    return new BattleCardInfo(
                        CardType.Poisoned, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        true, // 奇數
                        false, // 偶數
                        true, // 是否要附加狀態
                        BattleStatusEffectType.Poisoned, // 狀態類型
                        false, // 攻擊類
                        true, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.EffectStatus // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.Dizziness:
                {
                    return new BattleCardInfo(
                        CardType.Dizziness, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        true, // 需要指定的骰子數值
                        3, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        true, // 小於
                        false, // 奇數
                        false, // 偶數
                        true, // 是否要附加狀態
                        BattleStatusEffectType.Dizziness, // 狀態類型
                        true, // 攻擊類
                        true, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.EffectStatus // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.HeavyAttack:
                {
                    return new BattleCardInfo(
                        CardType.HeavyAttack, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        false, // 偶數
                        false, // 是否要附加狀態
                        BattleStatusEffectType.None, // 狀態類型
                        true, // 攻擊類
                        false, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.None // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.LazerGun:
                {
                    return new BattleCardInfo(
                        CardType.LazerGun, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        true, // 須累積骰子點數才可觸發
                        10, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        false, // 偶數
                        false, // 是否要附加狀態
                        BattleStatusEffectType.None, // 狀態類型
                        true, // 攻擊類
                        false, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.None // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.Recover:
                {
                    return new BattleCardInfo(
                        CardType.Recover, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        true, // 偶數
                        false, // 是否要附加狀態
                        BattleStatusEffectType.None, // 狀態類型
                        false, // 攻擊類
                        true, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.EffectStatus // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.Burnt:
                {
                    return new BattleCardInfo(
                        CardType.Burnt, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        true, // 偶數
                        true, // 是否要附加狀態
                        BattleStatusEffectType.Burnt, // 狀態類型
                        false, // 攻擊類
                        true, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.Dice // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.Heal:
                {
                    return new BattleCardInfo(
                        CardType.Heal, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        false, // 偶數
                        false, // 是否要附加狀態
                        BattleStatusEffectType.None, // 狀態類型
                        false, // 攻擊類
                        true, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.Heal // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.Dismantle:
                {
                    return new BattleCardInfo(
                        CardType.Dismantle, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        false, // 偶數
                        false, // 是否要附加狀態
                        BattleStatusEffectType.None, // 狀態類型
                        false, // 攻擊類
                        true, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.Dice // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.Clone:
                {
                    return new BattleCardInfo(
                        CardType.Clone, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        false, // 偶數
                        false, // 是否要附加狀態
                        BattleStatusEffectType.None, // 狀態類型
                        false, // 攻擊類
                        true, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.Dice // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.Reverse:
                {
                    return new BattleCardInfo(
                        CardType.Reverse, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        false, // 偶數
                        false, // 是否要附加狀態
                        BattleStatusEffectType.None, // 狀態類型
                        false, // 攻擊類
                        true, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.Dice // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.Redice:
                {
                    return new BattleCardInfo(
                        CardType.Redice, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        false, // 偶數
                        false, // 是否要附加狀態
                        BattleStatusEffectType.None, // 狀態類型
                        false, // 攻擊類
                        true, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.Dice // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.HolyProtect:
                {
                    return new BattleCardInfo(
                        CardType.HolyProtect, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        false, // 偶數
                        true, // 是否要附加狀態
                        BattleStatusEffectType.HolyProtect, // 狀態類型
                        false, // 攻擊類
                        true, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.ValueCalculation // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.StarThreaten:
                {
                    return new BattleCardInfo(
                        CardType.StarThreaten, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        false, // 偶數
                        true, // 是否要附加狀態
                        BattleStatusEffectType.StarThreaten, // 狀態類型
                        false, // 攻擊類
                        true, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.Dice // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.Oath:
                {
                    return new BattleCardInfo(
                        CardType.Oath, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        false, // 偶數
                        true, // 是否要附加狀態
                        BattleStatusEffectType.Oath, // 狀態類型
                        true, // 攻擊類
                        true, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.ValueCalculation // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            case CardType.Purify:
                {
                    return new BattleCardInfo(
                        CardType.Purify, // 卡片名稱
                        new List<int>() { 0 }, // 真實數值
                        false, // 須累積骰子點數才可觸發
                        0, // 須累積的數值
                        false, // 需要指定的骰子數值
                        0, // 指定的骰子數值
                        false, // 等於
                        false, // 大於
                        false, // 小於
                        false, // 奇數
                        false, // 偶數
                        false, // 是否要附加狀態
                        BattleStatusEffectType.None, // 狀態類型
                        false, // 攻擊類
                        true, // 效果類 Needed FunctionalCardType
                        BattleFunctionalCardType.EffectStatus // 影響使用卡片時的數值計算(攻擊、治療等)
                    );
                }
            default:
                {
                    Debug.LogWarning(CardType.Undefined.ToString() + "卡片型別為 Undefined");
                    return new BattleCardInfo(
                      CardType.Undefined, // 卡片名稱
                      new List<int>() { 0 }, // 真實數值
                      false, // 須累積骰子點數才可觸發
                      0, // 須累積的數值
                      false, // 需要指定的骰子數值
                      0, // 指定的骰子數值
                      false, // 等於
                      false, // 大於
                      false, // 小於
                      false, // 奇數
                      false, // 偶數
                      false, // 是否要附加狀態
                      BattleStatusEffectType.None, // 狀態類型
                      false, // 攻擊類
                      false, // 效果類 Needed FunctionalCardType
                      BattleFunctionalCardType.None // 影響使用卡片時的數值計算(攻擊、治療等)
                  );
                }
        }
    }

    public BattleCard(CardType cardType)
    {
        cardInfo = SendCardInfo(cardType);

        // [Header("不須經由傷害計算，直傷/直接造成的影響數值")]
        this.cardType = cardInfo.CardType;
        trueValues = cardInfo.TrueValues;

        requiredAccumulatedDiceValue = cardInfo.RequiredAccumulatedDiceValue; // 須累積骰子點數才可觸發
        accu_DiceValue = cardInfo.Accu_DiceValue;

        requireDesignatedDiceValue = cardInfo.RequireDesignatedDiceValue; // 需要指定的骰子數值
        desi_DiceValue = cardInfo.Desi_DiceValue;
        isEqualTo = cardInfo.IsEqualTo;
        isGreaterThan = cardInfo.IsGreaterThan;
        isLessThan = cardInfo.IsLessThan;

        requiredOddDiceValue = cardInfo.RequiredOddDiceValue; // 奇數
        requiredEvenDiceValue = cardInfo.RequiredEvenDiceValue; // 偶數

        isAddEffectStatus = cardInfo.IsAddEffectStatus;
        effectType = cardInfo.EffectType;

        isUsedToAttack = cardInfo.IsUsedToAttack; // 攻擊類
        isFunctional = cardInfo.IsFunctional; // 效果類 Needed FunctionalCardType

        functionalType = cardInfo.FunctionalType; // 影響使用卡片時的數值計算(攻擊、治療等)


    }

    public BattleCard() { }

}

public struct BattleCardInfo
{
    public CardType CardType;
    public List<int> TrueValues;

    public bool RequiredAccumulatedDiceValue; // 須累積骰子點數才可觸發
    public int Accu_DiceValue;

    public bool RequireDesignatedDiceValue; // 需要指定的骰子數值
    public int Desi_DiceValue;
    public bool IsEqualTo;
    public bool IsGreaterThan;
    public bool IsLessThan;

    public bool RequiredOddDiceValue; // 奇數
    public bool RequiredEvenDiceValue; // 偶數

    public bool IsAddEffectStatus;
    public BattleStatusEffectType EffectType;

    public bool IsUsedToAttack; // 攻擊類

    public bool IsFunctional; // 效果類 Needed FunctionalCardType
    public BattleFunctionalCardType FunctionalType; // 影響使用卡片時的數值計算(攻擊、治療等)

    public BattleCardInfo(CardType cardType, List<int> trueValues, bool requiredAccumulatedDiceValue, int accu_DiceValue, // 須累積骰子點數才可觸發int Accu_DiceValue,bool RequireDesignatedDiceValue , // 需要指定的骰子數值
                            bool requireDesignatedDiceValue, int desi_DiceValue, bool isEqualTo, bool isGreaterThan,
                            bool isLessThan, bool requiredOddDiceValue, bool requiredEvenDiceValue,
                            bool isAddEffectStatus, BattleStatusEffectType effectType,
                            bool isUsedToAttack, bool isFunctional, BattleFunctionalCardType functionalType)
    {
        CardType = cardType;
        TrueValues = trueValues;

        RequiredAccumulatedDiceValue = requiredAccumulatedDiceValue; // 須累積骰子點數才可觸發
        Accu_DiceValue = accu_DiceValue;

        RequireDesignatedDiceValue = requireDesignatedDiceValue; // 需要指定的骰子數值
        Desi_DiceValue = desi_DiceValue;

        IsEqualTo = isEqualTo;
        IsGreaterThan = isGreaterThan;
        IsLessThan = isLessThan;

        RequiredOddDiceValue = requiredOddDiceValue; // 奇數
        RequiredEvenDiceValue = requiredEvenDiceValue; // 偶數

        IsAddEffectStatus = isAddEffectStatus;
        EffectType = effectType;

        IsUsedToAttack = isUsedToAttack; // 攻擊類
        IsFunctional = isFunctional; // 效果類 Needed FunctionalCardType

        FunctionalType = functionalType; // 影響使用卡片時的數值計算(攻擊、治療等)
    }
}
