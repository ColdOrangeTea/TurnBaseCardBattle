using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;

/// <summary>
/// <see cref="BattleAction"/> 的測試／作弊部分：直接把某方血量打到 0、
/// 指定骰數模擬敵人行動，以及施加中毒／造成傷害的測試方法。
/// 這些方法多半綁在測試用 UI 按鈕上，正式流程不會使用。
/// </summary>
public partial class BattleAction
{
    public void ReducePlayerHpToZero()
    {
        ValueForOperation values = UseCard(); // 使用卡片
        values.BaseValue_ToUser = -999;
        values = AllUnitCalculate(values);
        DisplayEffectOnUseCard(temp_CardUser, BattleStatusEffectType.None, values.BaseValue_ToUser);

        SetStructValuesBackToTemp_Info(values);
        UpdateTemp_UnitData();
    }
    public void ReduceRivalHpToZero()
    {
        ValueForOperation values = UseCard(); // 使用卡片
        values.BaseValue_ToRival = -999;
        values = AllUnitCalculate(values);
        DisplayEffectOnUseCard(temp_Target, BattleStatusEffectType.None, values.BaseValue_ToRival);

        SetStructValuesBackToTemp_Info(values);
        UpdateTemp_UnitData();
    }

    public void TestEnemyBehavior(TurnBaseBattleUnitDisplayData unitData, int diceValue)
    {
        BattleUnitEnemyDisplayData enemyData = unitData as BattleUnitEnemyDisplayData; // 若不是 as BattleUnitEnemyData 會回傳 null

        if (enemyData != null)
        {
            if (temp_userDiceCount > 0) // 敵人有沒有色子
            {
                temp_DiceValue = diceValue;
                (CardType cardType, int value) = enemyData.EnemyBehavior(temp_DiceValue);
                (temp_CardType, temp_CardInfo, temp_DiceValue) = (cardType, InitBattleCardInfo(cardType), value);

                OnUseCard();
                Debug.Log("敵人行動 卡種類: " + cardType + " 骰數:" + value);
            }
            else
            {
                Debug.Log("敵人沒骰子，不行動");
            }
        }
    }

    public void CausePoisoned() // 取得當回合的這個玩家攻擊力與對手的生命值 並造成攻擊
    {
        // 先設置狀態的變數資料，獲取中毒的相關數據
        BattleStatusEffect effect = new BattleStatusEffect();
        BattleEffectInfo info = GetStatusEffectInfo(BattleStatusEffectType.Poisoned);

        // 在被下毒者的 statusEffects 列表中尋找是否已經有 Poisoned 狀態
        List<BattleStatusEffect> existingEffects = temp_TargetStatusEffects.FindAll(e => e.GetEffectType() == BattleStatusEffectType.Poisoned);
        Debug.Log("狀態數: " + existingEffects.Count);
        if (existingEffects.Count > 0)
        {
            // 如果找到了一個或多個 Poisoned 狀態，造成基本中毒傷害值*疊加效果次數的傷害
            foreach (var poisonedEffect in existingEffects)
            {
                valueSetter.StackTimes(poisonedEffect, poisonedEffect.GetLastTurn(), poisonedEffect.GetDotAddTimes());
            }
        }
        else
        {
            // 如果沒有找到任何 Poisoned 狀態，新增一個新的中毒狀態
            BattleStatusEffect newEffect = new BattleStatusEffect(BattleStatusEffectType.Poisoned);
            temp_TargetStatusEffects.Add(newEffect);
            Debug.Log("目標未中毒，新增中毒狀態。");
        }
        UpdateTemp_UnitData();
    }
    public void CauseDamage() // 造成攻擊
    {
        if (manager.currentTurn == manager.GetPlayerOrder())
        {
            Debug.Log("觸發攻擊訊號");
            int damage = calculation.Attack(5);
            Debug.Log("計算完傷害 目前HP: " + temp_TargetHp);
        }
        else if (manager.currentTurn == manager.GetRivalOrder())
        {
            Debug.Log("觸發攻擊訊號");
            Debug.Log("計算完傷害 目前HP: " + temp_UserHp);
        }
        UpdateTemp_UnitData();
    }
}
