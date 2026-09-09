using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;

/// <summary>
/// <see cref="BattleAction"/> 的數值計算部分：HP 增減（含溢補與歸零）、
/// 以及依單位身上狀態對傷害／治療數值做的四則運算加乘。
/// 這裡只做數值運算，不觸發特效或改變回合流程。
/// </summary>
public partial class BattleAction
{
    void HpCalculation(ref int unitHp, int finalValue, int maxHp)
    {
        if (HpOverHeal(finalValue, unitHp, maxHp))
        {
            unitHp = maxHp;
            return;
        }

        if (HpToZero(finalValue, unitHp) > 0)
        {
            if (finalValue != 0)
                unitHp += finalValue;
        }
        else
        {
            unitHp = HpToZero(finalValue, unitHp);
        }
    }

    #region HpCalculation
    bool HpOverHeal(int value, int hp, int maxHp) // 如果要回傳bool 可以在這個方法中指派
    {
        if (hp + value > maxHp)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    int HpToZero(int value, int hp)
    {
        Debug.Log($"HpToZero 傷害: {value} 目前HP: {hp}  結果:{hp + value}");

        if (hp + value <= 0)
        {
            hp = 0;
            return hp;
        }
        return hp;
    }
    #endregion

    ValueForOperation AllUnitCalculate(ValueForOperation values)
    {
        values = UserPerformCalculate(values);
        values = UnitReceivedCalculate(values);
        Debug.Log($"AllUnitCalculate 對自己造成: {values.FinalValue_ToUser} 向對方造成: {values.FinalValue_ToRival}");
        HpCalculation(ref temp_TargetHp, values.FinalValue_ToRival, temp_TargetMaxHp);
        HpCalculation(ref temp_UserHp, values.FinalValue_ToUser, temp_UserMaxHp);
        return values;
    }

    #region AllUnitCalculate Module Functions

    ValueForOperation UnitReceivedCalculate(ValueForOperation values)
    {
        // 敵人接收到、自己接收到，需要的狀態是對方身上的狀態，或者自己的狀態
        CalculateProcess(values.BaseValue_ToRival, ref values.TargetNeedsOperation, values.TargetStatus, ref values.FinalValue_ToRival,
        BattleValueAffectType.AFFECT_RECEIVED_DAMAGE, BattleValueAffectType.AFFECT_RECEIVED_HEAL, values.UserName, values.TargetName); // 如有額外狀態加乘，則進行四則運算

        CalculateProcess(values.BaseValue_ToUser, ref values.UserNeedsOperation, values.UserStatus, ref values.FinalValue_ToUser,
        BattleValueAffectType.AFFECT_RECEIVED_DAMAGE, BattleValueAffectType.AFFECT_RECEIVED_HEAL, values.UserName, values.UserName); // 如有額外狀態加乘，則進行四則運算

        return values;
    }

    ValueForOperation UserPerformCalculate(ValueForOperation values)
    {
        // 對敵人造成、對自己造成，需要的狀態是自己身上的狀態
        CalculateProcess(values.BaseValue_ToRival, ref values.UserNeedsOperation, values.UserStatus, ref values.FinalValue_ToRival,
        BattleValueAffectType.AFFECT_DAMAGE, BattleValueAffectType.AFFECT_HEAL, values.UserName, values.TargetName); // 如有額外狀態加乘，則進行四則運算

        CalculateProcess(values.BaseValue_ToUser, ref values.UserNeedsOperation, values.UserStatus, ref values.FinalValue_ToUser,
        BattleValueAffectType.AFFECT_DAMAGE, BattleValueAffectType.AFFECT_HEAL, values.UserName, values.UserName); // 如有額外狀態加乘，則進行四則運算

        return values;
    }

    // 對造成的一個數值進行運算
    void CalculateProcess(int baseValue, ref bool isUnitNeedsOperation, List<BattleStatusEffect> unitStatusEffects, ref int modifiedValue, BattleValueAffectType typeIfTrue, BattleValueAffectType typeIfFalse, string uName, string tName)
    {
        // 先確定是傷害還是治療
        bool isDamage = false;

        CheckIfDamageOrHeal(baseValue, ref isDamage);

        modifiedValue = GetModifiedValue(isDamage, baseValue, ref isUnitNeedsOperation, unitStatusEffects, modifiedValue, typeIfTrue, typeIfFalse);
    }

    int GetModifiedValue(bool isDamage, int baseValue, ref bool isUnitNeedsOperation, List<BattleStatusEffect> unitStatusEffects, int modifiedValue, BattleValueAffectType typeIfTrue, BattleValueAffectType typeIfFalse)
    {
        BattleValueAffectType affectType = isDamage == true ? typeIfTrue : typeIfFalse;

        if (unitStatusEffects == null || unitStatusEffects.Count <= 0) return baseValue; // 沒有任何狀態，回傳原始數值

        List<BattleStatusEffect> unitDamageEffects = null;
        (isUnitNeedsOperation, unitDamageEffects) = GetAffectedCalculationEffectFromStatus(unitStatusEffects, affectType);

        if (!isUnitNeedsOperation) return baseValue; // 有會影響數值計算的狀態才繼續往下算

        List<BattleStatusEffect> unitAddDamageEffects = GatherOperationEffects(unitDamageEffects, BattleValueOperation.Add);
        List<BattleStatusEffect> unitSubtractDamageEffects = GatherOperationEffects(unitDamageEffects, BattleValueOperation.Subtract);
        List<BattleStatusEffect> unitMultiplyDamageEffects = GatherOperationEffects(unitDamageEffects, BattleValueOperation.Multiply);
        List<BattleStatusEffect> unitDivideDamageEffects = GatherOperationEffects(unitDamageEffects, BattleValueOperation.Divide);

        List<int> multiplyNums = new List<int>();
        GetOperationValue(unitMultiplyDamageEffects, ref multiplyNums);

        List<int> divideNums = new List<int>();
        GetOperationValue(unitDivideDamageEffects, ref divideNums);

        List<int> addNums = new List<int>();
        GetOperationValue(unitAddDamageEffects, ref addNums);

        List<int> subtractNums = new List<int>();
        GetOperationValue(unitSubtractDamageEffects, ref subtractNums);

        int temp_modifiedValue = baseValue;
        // 先乘除後加減
        CalculateValue(ref temp_modifiedValue, multiplyNums, BattleValueOperation.Multiply);
        CalculateValue(ref temp_modifiedValue, divideNums, BattleValueOperation.Divide);
        CalculateValue(ref temp_modifiedValue, addNums, BattleValueOperation.Add);
        CalculateValue(ref temp_modifiedValue, subtractNums, BattleValueOperation.Subtract);
        modifiedValue = temp_modifiedValue;

        return modifiedValue;
    }

    void CalculateValue(ref int value, List<int> numList, BattleValueOperation operationType)
    {
        foreach (int num in numList)
        {
            if (operationType == BattleValueOperation.Multiply)
            {
                value *= num;
            }
            if (operationType == BattleValueOperation.Divide)
            {
                float factor = 1.0f / num; // 計算乘法因子
                value = Mathf.RoundToInt(value * factor); // 使用乘法替代除法
            }
            if (operationType == BattleValueOperation.Add)
            {
                value += num;
            }
            if (operationType == BattleValueOperation.Subtract)
            {
                value += num;
            }
        }
    }
    void GetOperationValue(List<BattleStatusEffect> operationEffects, ref List<int> modifiedvalues)
    {
        if (operationEffects != null)
        {
            foreach (var effect in operationEffects)
            {
                modifiedvalues.Add(effect.GetEffectValue());
            }
        }
    }

    private List<BattleStatusEffect> GatherOperationEffects(List<BattleStatusEffect> statusEffects, BattleValueOperation operationType)
    {
        if (statusEffects == null) return null; // 檢查傳入的 statusEffects 是否為 null
        List<BattleStatusEffect> operationEffects = statusEffects.FindAll(e => e.GetOperationType() == operationType);

        return operationEffects.Count > 0 ? operationEffects : null;// 如果找不到符合條件的效果，則回傳 null
    }

    // 取得任何運算類型數值的指定作用單位狀態
    (bool, List<BattleStatusEffect>) GetAffectedCalculationEffectFromStatus(List<BattleStatusEffect> unitStatusEffects, BattleValueAffectType affectType)
    {
        bool unitNeedsOperation = false;
        int type = (int)affectType;

        // 檢查傳入的 unitStatusEffects 是否有任何 AffectedCalculation 為 true
        if (unitStatusEffects != null && unitStatusEffects.Exists(e => e.GetAffectedCalculation() == true))
        {
            unitNeedsOperation = true;

            // 使用 `FindAll` 找出符合條件的效果，並確保 `AffectValueType` 包含 `type`
            List<BattleStatusEffect> affectEffects = unitStatusEffects.FindAll(e => e.GetAffectedCalculation() == true && e.GetAffectValueTypeList() != null && e.GetAffectValueTypeList()[type]);

            return (unitNeedsOperation, affectEffects.Count > 0 ? affectEffects : null);
        }
        return (unitNeedsOperation, null);
    }

    void CheckIfDamageOrHeal(int value, ref bool isDamage)
    {
        isDamage = value < 0 ? true : false;
    }
    #endregion
}
