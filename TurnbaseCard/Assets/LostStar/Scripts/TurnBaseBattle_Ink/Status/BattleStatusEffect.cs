using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;
using System.Collections.Generic;


// 讓該類可以在 Inspector 中序列化
[System.Serializable]
public class BattleStatusEffect
{
    [SerializeField]
    private BattleStatusEffectType EffectType;
    private BattleEffectInfo effectInfo;
    [SerializeField]
    private bool HasTakenEffect;
    [SerializeField]
    private int EffectValue; // 效果值，比如傷害或骰子數減少
    [SerializeField]
    private int LastTurn; // 持續時間
    [SerializeField]
    private int DotAddTimes; // 施加次數

    [Header("是不是當持續回合數為到0且觸發過可以直接消除的狀態")] // 像火燒就是個觸發了但還是不能移除的狀態，要等回合結束才能消失
    [SerializeField]
    private bool IsAbleToRemoveAfterEffect = true;

    [Header("是不是可移除的狀態")]
    [SerializeField]
    private bool IsRemovable; // 可用卡消除

    [Header("施展對象")]
    [SerializeField]
    private bool IsSelfAffecting = false;
    [SerializeField]
    private bool IsRivalAffecting = false;

    [Header("狀態種類")]
    [SerializeField]
    private bool IsSkippedTurn = false;

    [Header("是影響用卡的傷害計算/減傷的功能")]

    [SerializeField]
    private bool AffectedCalculation = false;

    [SerializeField]
    private BattleValueOperation OperationType;
    [SerializeField] List<bool> AffectValueTypeList = new List<bool>();

    [Header("是影響骰子數量與其數值的功能")]
    [SerializeField]
    private bool AffectedDice = false;

    [Header("狀態效果發作的時機控制")]
    [SerializeField] List<bool> ActivatesTimingList = new List<bool>();

    [Header("狀態效果移除的時機控制")]
    [SerializeField] List<bool> RemovesTimingList = new List<bool>();

    [Header("回合持續性生效")]
    public bool IsTurnBasedEffect = false; // 是否是回合持續性生效

    [Header("回合持續性生效條件")]
    public bool isEqual = false, isGreaterThan = false, isLessThan = false;

    [Header("在誰的回合生效的控制")] // 是否是自己回合時生效，或者是否是敵人回合生效，影響回合持續時間的增減
    [SerializeField] List<bool> EffectActiveTurnList = new List<bool>();
    // public bool ActivatesOwnTurnOnly = false;
    // public bool ActivatesRivalTurnOnly = false;


    #region  "Get And Set Function"

    public BattleStatusEffectType GetEffectType() => EffectType;
    public void SetEffectType(BattleStatusEffectType effectType) => EffectType = effectType;

    public BattleEffectInfo GetBattleEffectInfo() => effectInfo;

    #region  基本數值
    public bool GetHasTakenEffect() => HasTakenEffect;
    public void SetHasTakenEffect(bool hasTaken) => HasTakenEffect = hasTaken;

    public int GetLastTurn() => LastTurn;
    public void SetLastTurn(int turn) => LastTurn = turn;

    public int GetDotAddTimes() => DotAddTimes;
    public void SetDotAddTimes(int times) => DotAddTimes = times;

    public int GetEffectValue() => EffectValue;
    public void SetEffectValue(int value) => EffectValue = value;
    #endregion

    #region  飾展對象
    public bool GetIsSelfAffecting() => IsSelfAffecting;
    public void SetIsSelfAffecting(bool isAffected) => IsSelfAffecting = isAffected;
    public bool GetIsRivalAffecting() => IsRivalAffecting;
    public void SetIsRivalAffecting(bool isAffected) => IsRivalAffecting = isAffected;
    #endregion

    #region 影響類型 

    #region  跳過回合  
    public bool GetIsSkippedTurn() => IsSkippedTurn;
    public void SetIsSkippedTurn(bool isSkipped) => IsSkippedTurn = isSkipped;
    #endregion

    #region  影響數值計算
    public bool GetAffectedCalculation() => AffectedCalculation;
    public void SetAffectedCalculation(bool isAffected) => AffectedCalculation = isAffected;

    public BattleValueOperation GetOperationType() => OperationType;
    public void SetOperationType(BattleValueOperation type) => OperationType = type;


    public List<bool> GetAffectValueTypeList() => AffectValueTypeList;
    public void SetAffectValueTypeList(List<bool> list) => AffectValueTypeList = list;
    #endregion

    #region  骰子
    public bool GetAffectedDice() => AffectedDice;
    public void SetAffectedDice(bool isAffected) => AffectedDice = isAffected;
    #endregion

    #endregion

    #region  狀態
    public List<bool> GetActivatesTimingList() => ActivatesTimingList;
    public void SetActivatesTimingList(List<bool> list) => ActivatesTimingList = list;

    public List<bool> GetRemovesTimingList() => RemovesTimingList;
    public void SetRemovesTimingList(List<bool> list) => RemovesTimingList = list;

    public List<bool> GetEffectActiveTurnList() => EffectActiveTurnList;
    public void SetEffectActiveTurnList(List<bool> list) => EffectActiveTurnList = list;

    #endregion

    public bool GetIsAbleToRemoveAfterEffect() => IsAbleToRemoveAfterEffect;
    public void SetIsAbleToRemoveAfterEffect(bool isAbleToRemove) => IsAbleToRemoveAfterEffect = isAbleToRemove;

    public bool GetIsRemovable() => IsRemovable;
    public void SetIsRemovable(bool isRemoveable) => IsRemovable = isRemoveable;

    #endregion

    #region"Value Setting"

    [Header("效果的影響值")]
    public const int PoisonedDamageValue = 2;
    public const int PoisonedTurnLastValue = 3;

    public const int DizzinessDamageValue = 2;
    public const int DizzinessTurnLastValue = 1;

    public const int BurntReduceDiceCountValue = 1;
    public const int BurntTurnLastValue = 1;

    public const int HolyProtectDamageDivideValue = 2;
    public const int HolyProtectTurnLastValue = 1;

    public const int StarThreatenValue = 2;
    public const int StarThreatenTurnLastValue = 1;

    public const int OathDamageMultipleValue = 2;
    public const int OathTurnLastValue = 1;

    public const int passOneTurn = 1; // 經過一回合

    #endregion

    public int ReduceLastTimes()
    {
        return LastTurn -= passOneTurn;
    }

    public BattleEffectInfo EffectOnTurnStart(BattleStatusEffectType effectType)
    {
        return SendEffectInfo(effectType);
    }

    /// <summary>
    /// 用卡加效果
    /// </summary>
    public static BattleEffectInfo SendEffectInfo(BattleStatusEffectType type)
    {
        // 狀態設定改由 SO 資料表提供（Resources/Battle/BattleStatusEffectData）。
        // 要調整狀態數值/行為，請改該資產，或執行選單「Tools/TurnBaseBattle/生成戰鬥資料 SO」重新生成。
        // 若資產缺失或查無此狀態，退回下方寫死的預設值，確保戰鬥流程不中斷。
        if (BattleDataProvider.TryGetStatusInfo(type, out BattleEffectInfo soInfo))
            return soInfo;

        switch (type)
        {
            case BattleStatusEffectType.Burnt:
                {
                    // 減少 1 顆骰子
                    return new BattleEffectInfo(
                    BattleStatusEffectType.Burnt,
                    false, // 效果是否有作用過
                    BurntReduceDiceCountValue,
                    BurntTurnLastValue,
                    1, // 施加次數，可根據需求設置
                    false, // 持續回合數為到0可以直接消除的狀態類型
                    true, // 可否用卡移除的狀態
                    false, // 是否作用於自己
                    true, // 是否作用於敵人
                    false, // 是否跳過回合
                    false, // 是否影響計算
                    BattleValueOperation.NormalOperation, // (如果有)影響計算的方式
                    new List<bool>()
                    {
                        false, // 是否影響傷害的數值
                        false, // 是否影響治療的數值
                        false, // 是否影響受擊數值
                        false, // 是否影響受治療數值
                    },
                    true, // 是否影響骰子
                    new List<bool>()
                    {
                        false, // 是否用卡的當下觸發狀態的效果
                        true, // 是否回合開始時觸發
                        false  // 是否回合結束時觸發
                    },
                    new List<bool>()
                    {
                        false, // 是否是生效時立即移除
                        false, // 是否是回合開始時移除
                        true, // 是否是回合結束時移除
                    },
                    true, // 持續回合效果
                    false, // 等於
                    false, // 大於
                    true,  // 小於
                    new List<bool>()
                    {
                        true, // 自己回合生效
                        false, // 敵人回合時生效
                        false, // 任何回合時生效
                    }
                );
                }
            case BattleStatusEffectType.Poisoned:
                {
                    // 施加時不造成傷害，但會在被施加者的行動回合開始時造成傷害，當敵人已中毒時，傷害及回合累加
                    return new BattleEffectInfo
                    (BattleStatusEffectType.Poisoned,
                    false, // 效果是否有作用過
                    PoisonedDamageValue,
                    PoisonedTurnLastValue,
                    1, // 施加次數，可根據需求設置
                    true, // 持續回合數為到0可以直接消除的狀態類型
                    true, // 可否用卡移除的狀態
                    false, // 是否作用於自己
                    true, // 是否作用於敵人
                    false, // 是否跳過回合
                    false, // 是否影響計算
                    BattleValueOperation.NormalOperation, // (如果有)影響計算的方式
                    new List<bool>()
                    {
                        false, // 是否影響傷害的數值
                        false, // 是否影響治療的數值
                        false, // 是否影響受擊數值
                        false, // 是否影響受治療數值
                    },
                    false, // 是否影響骰子
                    new List<bool>()
                    {
                        false, // 是否用卡的當下觸發狀態的效果
                        true, // 是否回合開始時觸發
                        false  // 是否回合結束時觸發
                    },
                    new List<bool>()
                    {
                        false, // 是否是生效時立即移除
                        true, // 是否是回合開始時移除
                        false, // 是否是回合結束時移除
                    },
                    true, // 持續回合效果
                    false, // 等於
                    false, // 大於
                    true,  // 小於
                    new List<bool>()
                    {
                        false, // 自己回合生效
                        false, // 敵人回合時生效
                        true, // 任何回合時生效
                    }
                    );
                }
            case BattleStatusEffectType.Dizziness:
                {
                    // 造成X點傷害，並暈眩對方一回合 當敵人已暈眩時，傷害及回合累加
                    return new BattleEffectInfo
                    (BattleStatusEffectType.Dizziness,
                    false, // 效果是否有作用過
                    DizzinessDamageValue,
                    DizzinessTurnLastValue,
                    1, // 施加次數，可根據需求設置
                    false, // 持續回合數為到0可以直接消除的狀態類型
                    true, // 可否用卡移除的狀態
                    false, // 是否作用於自己
                    true, // 是否作用於敵人
                    true, // 是否跳過回合
                    false, // 是否影響計算
                    BattleValueOperation.NormalOperation, // (如果有)影響計算的方式
                    new List<bool>()
                    {
                        false, // 是否影響傷害的數值
                        false, // 是否影響治療的數值
                        false, // 是否影響受擊數值
                        false, // 是否影響受治療數值
                    },
                    false, // 是否影響骰子
                    new List<bool>()
                    {
                        false, // 是否用卡的當下觸發狀態的效果
                        true, // 是否回合開始時觸發
                        false  // 是否回合結束時觸發
                    },
                    new List<bool>()
                    {
                        false, // 是否是生效時立即移除
                        false, // 是否是回合開始時移除
                        true, // 是否是回合結束時移除
                    },
                    true, // 持續回合效果
                    false, // 等於
                    false, // 大於
                    true,  // 小於
                    new List<bool>()
                    {
                        false, // 自己回合生效
                        false, // 敵人回合時生效
                        true, // 任何回合時生效
                    }
                    );
                }
            case BattleStatusEffectType.HolyProtect: // 下回合受到的傷害/2
                {
                    return new BattleEffectInfo
                    (BattleStatusEffectType.HolyProtect,
                    false, // 效果是否有作用過
                    HolyProtectDamageDivideValue,
                    HolyProtectTurnLastValue,
                    1, // 施加次數，可根據需求設置
                    true, // 持續回合數為到0可以直接消除的狀態類型
                    true, // 可否用卡移除的狀態
                    true, // 是否作用於自己
                    false, // 是否作用於敵人
                    false, // 是否跳過回合
                    true, // 是否影響計算
                    BattleValueOperation.Divide, // (如果有)影響計算的方式
                    new List<bool>()
                    {
                        false, // 是否影響傷害的數值
                        false, // 是否影響治療的數值
                        true, // 是否影響受擊數值
                        false, // 是否影響受治療數值
                    },
                    false, // 是否影響骰子
                    new List<bool>()
                    {
                        false, // 是否用卡的當下觸發狀態的效果
                        false, // 是否回合開始時觸發
                        false  // 是否回合結束時觸發
                    },
                    new List<bool>()
                    {
                        false, // 是否是生效時立即移除
                        false, // 是否是回合開始時移除
                        true, // 是否是回合結束時移除
                    },

                    true, // 持續回合效果
                    false, // 等於
                    false, // 大於
                    true,  // 小於
                    new List<bool>()
                    {
                        false, // 自己回合生效
                        true, // 敵人回合時生效
                        false, // 任何回合時生效
                    }
                    );
                }
            case BattleStatusEffectType.StarThreaten: // 使玩家只能骰出4
                {
                    return new BattleEffectInfo
                    (BattleStatusEffectType.StarThreaten,
                    false, // 效果是否有作用過
                    StarThreatenValue,
                    StarThreatenTurnLastValue,
                    1, // 施加次數，可根據需求設置
                    false, // 持續回合數為到0可以直接消除的狀態類型
                    true, // 可否用卡移除的狀態
                    false, // 是否作用於自己
                    true, // 是否作用於敵人
                    false, // 是否跳過回合
                    false, // 是否影響計算
                    BattleValueOperation.NormalOperation, // (如果有)影響計算的方式
                    new List<bool>()
                    {
                        false, // 是否影響傷害的數值
                        false, // 是否影響治療的數值
                        false, // 是否影響受擊數值
                        false, // 是否影響受治療數值
                    },
                    true, // 是否影響骰子
                    new List<bool>()
                    {
                        false, // 是否用卡的當下觸發狀態的效果
                        true, // 是否回合開始時觸發
                        false  // 是否回合結束時觸發
                    },
                    new List<bool>()
                    {
                        false, // 是否是生效時立即移除
                        false, // 是否是回合開始時移除
                        true, // 是否是回合結束時移除
                    },
                    true, // 持續回合效果
                    false, // 等於
                    false, // 大於
                    true,  // 小於
                    new List<bool>()
                    {
                        true, // 自己回合生效
                        false, // 敵人回合時生效
                        false, // 任何回合時生效
                    }
                   );
                }
            case BattleStatusEffectType.Oath: // 下回合受到的傷害x2
                {
                    return new BattleEffectInfo
                    (BattleStatusEffectType.Oath,
                    false, // 效果是否有作用過
                    OathDamageMultipleValue,
                    OathTurnLastValue,
                    1, // 施加次數，可根據需求設置
                    true, // 持續回合數為到0可以直接消除的狀態類型
                    true, // 可否用卡移除的狀態
                    true, // 是否作用於自己
                    false, // 是否作用於敵人
                    false, // 是否跳過回合
                    true, // 是否影響計算
                    BattleValueOperation.Multiply, // (如果有)影響計算的方式
                    new List<bool>()
                    {
                        false, // 是否影響傷害的數值
                        false, // 是否影響治療的數值
                        true, // 是否影響受擊數值
                        false, // 是否影響受治療數值
                    },
                    false, // 是否影響骰子
                    new List<bool>()
                    {
                        false, // 是否用卡的當下觸發狀態的效果
                        false, // 是否回合開始時觸發
                        false  // 是否回合結束時觸發
                    },
                    new List<bool>()
                    {
                        false, // 是否是生效時立即移除
                        false, // 是否是回合開始時移除
                        true, // 是否是回合結束時移除
                    },
                    true, // 持續回合效果
                    false, // 等於
                    false, // 大於
                    true,  // 小於
                    new List<bool>()
                    {
                        false, // 自己回合生效
                        true, // 敵人回合時生效
                        false, // 任何回合時生效
                    }
                    );
                }
            default:
                {
                    BattleLog.Log("施加狀態中 沒狀態");
                    return new BattleEffectInfo(BattleStatusEffectType.None,
                    false, // 效果是否有作用過
                    0, 0, 0, // 施加次數，可根據需求設置
                    true, //  持續回合數為到0可以直接消除的狀態類型
                    false, // 可否用卡移除的狀態
                    false, // 是否作用於自己
                    false, // 是否作用於敵人
                    false, // 是否跳過回合
                    false, // 是否影響計算
                    BattleValueOperation.NormalOperation, // (如果有)影響計算的方式
                    new List<bool>()
                    {
                        false, // 是否影響傷害的數值
                        false, // 是否影響治療的數值
                        false, // 是否影響受擊數值
                        false, // 是否影響受治療數值
                    },
                    false, // 是否影響骰子
                    new List<bool>()
                    {
                        false, // 是否用卡的當下觸發狀態的效果
                        false, // 是否回合開始時觸發
                        false  // 是否回合結束時觸發
                    },
                    new List<bool>()
                    {
                        false, // 是否是生效時立即移除
                        false, // 是否是回合開始時移除
                        false, // 是否是回合結束時移除
                    },
                    false, // 持續回合效果
                    false, // 等於
                    false, // 大於
                    false,  // 小於
                    new List<bool>()
                    {
                        false, // 自己回合生效
                        false, // 敵人回合時生效
                        false, // 任何回合時生效
                    }
                    );
                }
        }
    }

    public void StackTimes(int turn, int times)
    {
        LastTurn += turn; // 疊加回合數
        DotAddTimes += times; // 疊加效果次數
    }

    /// <summary>
    /// 參數數值: 效果類型、效果值、回合持續數、施加次數、是用來乘使用卡造成的數值的、是否跳過回合、是否在施加效果後生效、是否在指定人物回合開始時生效、是否能移除(觸發完這個bool才能=true，true代表可以移除狀態效果)
    /// </summary>
    public BattleStatusEffect(BattleStatusEffectType effectType)
    {
        effectInfo = SendEffectInfo(effectType);
        EffectType = effectInfo.EffectType;

        HasTakenEffect = effectInfo.HasTakenEffect;

        EffectValue = effectInfo.EffectValue;
        LastTurn = effectInfo.LastTurn; // 疊加回合數
        DotAddTimes = effectInfo.DotAddTimes; // 疊加效果次數 

        IsAbleToRemoveAfterEffect = effectInfo.IsAbleToRemoveEffect; // 控制能否移除狀態的bool值
        IsRemovable = effectInfo.IsRemovable; // 暫時，可用卡消除

        IsSelfAffecting = effectInfo.IsSelfAffecting;
        IsRivalAffecting = effectInfo.IsRivalAffecting;

        IsSkippedTurn = effectInfo.IsSkippedTurn;

        AffectedCalculation = effectInfo.AffectedCalculation;
        OperationType = effectInfo.OperationType;
        AffectValueTypeList = effectInfo.AffectValueTypeList;

        AffectedDice = effectInfo.AffectedDice;

        ActivatesTimingList = effectInfo.ActivatesTimingList;
        RemovesTimingList = effectInfo.RemovesTimingList;
        IsTurnBasedEffect = effectInfo.IsTurnBasedEffect; // 是否是回合持續性生效

        if (IsTurnBasedEffect == false)
        {
            isEqual = false;
            isGreaterThan = false;
            isLessThan = false;
        }
        else
        {
            isEqual = effectInfo.IsEqual;
            isGreaterThan = effectInfo.IsGreaterThan;
            isLessThan = effectInfo.IsLessThan;
        }

        EffectActiveTurnList = effectInfo.EffectActiveTurnList;

        // ActivatesOwnTurnOnly = effectInfo.ActivatesOwnTurnOnly; // 是否是限定自己回合生效
        // ActivatesRivalTurnOnly = effectInfo.ActivatesRivalTurnOnly; // 是否是限定敵人回合生效

    }
    public BattleStatusEffect() { EffectType = BattleStatusEffectType.None; }
}
public struct BattleEffectInfo
{
    public BattleStatusEffectType EffectType;
    public bool HasTakenEffect;
    public int EffectValue; // 效果值，比如傷害或骰子數減少
    public int LastTurn; // 持續時間
    public int DotAddTimes; // 施加次數

    public bool IsAbleToRemoveEffect;
    public bool IsRemovable; // 可用卡消除

    public bool IsSelfAffecting;
    public bool IsRivalAffecting;

    public bool IsSkippedTurn;
    public bool AffectedCalculation;
    public BattleValueOperation OperationType;

    public List<bool> AffectValueTypeList;

    // public bool AffectDamage;
    // public bool AffectHeal;
    // public bool AffectReceivedDamage;
    // public bool AffectReceivedHealed;

    public bool AffectedDice;

    public List<bool> ActivatesTimingList;
    public List<bool> RemovesTimingList;
    public bool IsTurnBasedEffect; // 是否是回合持續性生效

    public bool IsEqual;
    public bool IsGreaterThan;
    public bool IsLessThan;
    public List<bool> EffectActiveTurnList;


    public BattleEffectInfo(BattleStatusEffectType effectType, bool hasTakenEffect, int effectValue, int lastTurn, int times, bool isAbleToRemoveEffect, bool isRemovable,
    bool isSelfAffecting, bool isRivalAffecting, bool skipTurn, bool affectsCalculation,
    BattleValueOperation operationType, List<bool> affectValueTypeList, bool affectsDice,
    List<bool> activatesTimingList, List<bool> removesTimingList,
    bool isTurnBasedEffect, bool equals, bool greaterThan, bool lessThan, List<bool> effectActiveTurnList)
    {
        EffectType = effectType;

        HasTakenEffect = hasTakenEffect;

        EffectValue = effectValue;
        LastTurn = lastTurn;
        DotAddTimes = times;

        IsAbleToRemoveEffect = isAbleToRemoveEffect;
        IsRemovable = isRemovable;

        IsSelfAffecting = isSelfAffecting;
        IsRivalAffecting = isRivalAffecting;

        IsSkippedTurn = skipTurn;

        AffectedCalculation = affectsCalculation;
        OperationType = operationType;

        AffectValueTypeList = affectValueTypeList;

        AffectedDice = affectsDice;

        ActivatesTimingList = activatesTimingList;
        RemovesTimingList = removesTimingList;

        IsTurnBasedEffect = isTurnBasedEffect;

        IsEqual = equals;
        IsGreaterThan = greaterThan;
        IsLessThan = lessThan;

        EffectActiveTurnList = effectActiveTurnList;

    }
}
