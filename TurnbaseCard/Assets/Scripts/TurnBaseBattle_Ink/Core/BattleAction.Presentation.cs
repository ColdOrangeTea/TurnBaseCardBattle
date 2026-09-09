using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;

/// <summary>
/// <see cref="BattleAction"/> 的特效顯示部分：用卡或狀態觸發時，
/// 依傷害／治療／施加狀態，通知對應單位的 <see cref="BattleUnitProfile"/> 播放特效。
/// </summary>
public partial class BattleAction
{
    void DisplayEffectOnUseCard(TurnBaseBattleUnitDisplayData unitData, BattleStatusEffectType effectType, int targetFinalValue)
    {
        DisplaySE(unitData, temp_CardType, effectType, targetFinalValue);
    }
    void DisplayEffectOnTurn(TurnBaseBattleUnitDisplayData unitData, BattleStatusEffectType effectType, int targetFinalValue) // 作用在觸發狀態的單元上，不分敵我
    {
        temp_CardType = CardType.Undefined;  // 觸發狀態時不用檢查卡片，只檢查觸發的狀態
        if (targetFinalValue != 0)
        {
            DisplaySE(unitData, temp_CardType, effectType, targetFinalValue);
        }
    }
    void DisplaySE(TurnBaseBattleUnitDisplayData unitData, CardType cardType, BattleStatusEffectType effectType, int value)
    {
        bool isDamage = false;
        bool isApplyState = false;
        if (value != 0)
        {
            isDamage = value < 0 ? true : false;
        }
        else
        {
            isApplyState = true;
        }

        Debug.Log($"DisplaySE {unitData} 數值: {value} 傷害:{isDamage} 施加狀態:{isApplyState}");

        BattleUnitEnemyDisplayData enemyData = unitData as BattleUnitEnemyDisplayData;// 若不是 as BattleUnitEnemyData 會回傳 null
        if (enemyData != null)
        {
            enemyData.gameObject.GetComponent<BattleUnitProfile>().PlaySE(cardType, isDamage, isApplyState, effectType);
            return;
        }

        BattleUnitPlayerDisplayData playerData = unitData as BattleUnitPlayerDisplayData; // 若不是 as BattleUnitPlayerData 會回傳 null
        if (playerData != null)
        {
            playerData.gameObject.GetComponent<BattleUnitProfile>().PlaySE(cardType, isDamage, isApplyState, effectType);
            return;
        }
    }
}
