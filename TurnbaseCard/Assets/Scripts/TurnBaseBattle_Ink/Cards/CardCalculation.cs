using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using Unity.VisualScripting;
using UnityEngine;

public class CardCalculation  // 這個腳本就純計算了吧
{
    public const int passOneTurn = 1; // 經過一回合

    #region Common Calculation
    /// <summary>
    /// 移除狀態後觸發的影響。
    /// 回傳值：(對造成效果的目標的骰數)diceCount, (目標是不是跳過回合)isSkipped
    /// </summary>
    /// 
    public (int, bool) RemoveEffect(BattleStatusEffect effect, ref int unitDiceCount, ref bool isUnitSkipped, BattleStatusEffectWorkTiming timing)
    {
        switch (effect.GetEffectType())
        {
            case BattleStatusEffectType.Burnt:
                {
                    unitDiceCount = unitDiceCount += 1;
                    Debug.Log($"RemoveEffect 數值: {unitDiceCount} 狀態: {effect.GetEffectType()}");
                    return (unitDiceCount, isUnitSkipped);
                }
            case BattleStatusEffectType.Dizziness:
                {
                    isUnitSkipped = Dizziness(effect.GetLastTurn(), effect.GetDotAddTimes(), isUnitSkipped, effect.GetHasTakenEffect());
                    Debug.Log($"RemoveEffect 數值: {isUnitSkipped} 狀態: {effect.GetEffectType()}");
                    return (unitDiceCount, isUnitSkipped);
                }
        }
        return (unitDiceCount, isUnitSkipped);

    }

    #region  TriggerEffect   
    /// <summary>
    /// 回合開始時，觸發此回合行動者的狀態。
    /// 回傳值：(對造成效果的目標)baseValue,(對造成效果的目標的骰數)diceCount, (目標是不是跳過回合)isSkipped
    /// </summary>
    /// 
    public (int, int, bool) TriggerEffect(BattleStatusEffect effect, ref int unitDiceCount, ref bool isUnitSkipped, BattleStatusEffectWorkTiming timing,
     ref bool isAssignFixedDiceValue_CardUser, ref int assignFixedDiceValue, ref int assignFixedDiceCount)
    {
        int baseValue_ToUnit = 0;
        if (timing == BattleStatusEffectWorkTiming.ACTIVATES_IMMEDIATELY)
        {

        }
        if (timing == BattleStatusEffectWorkTiming.ACTIVATES_AT_TURN_START)
        {
            (baseValue_ToUnit, unitDiceCount, isUnitSkipped) =
                         TriggerEffectAtTurnStart(baseValue_ToUnit, effect, ref unitDiceCount, ref isUnitSkipped, ref isAssignFixedDiceValue_CardUser, ref assignFixedDiceValue, ref assignFixedDiceCount);
            return (baseValue_ToUnit, unitDiceCount, isUnitSkipped);
        }
        if (timing == BattleStatusEffectWorkTiming.ACTIVATES_AT_TURN_END)
        {

        }


        return (baseValue_ToUnit, unitDiceCount, isUnitSkipped);
    }

    (int, int, bool) TriggerEffectAtTurnStart(int baseValue_ToUnit, BattleStatusEffect effect, ref int unitDiceCount,
    ref bool isUnitSkipped, ref bool isAssignFixedDiceValue_CardUser, ref int assignFixedDiceValue, ref int assignFixedDiceCount)
    {
        switch (effect.GetEffectType())
        {
            case BattleStatusEffectType.Poisoned:
                {
                    baseValue_ToUnit = Poisoned(effect.GetEffectValue(), effect.GetLastTurn(), effect.GetDotAddTimes());
                    Debug.Log($"TriggerEffect 數值: {baseValue_ToUnit} 狀態: {effect.GetEffectType()}");
                    return (baseValue_ToUnit, unitDiceCount, isUnitSkipped);
                }
            case BattleStatusEffectType.Dizziness:
                {
                    isUnitSkipped = Dizziness(effect.GetLastTurn(), effect.GetDotAddTimes(), isUnitSkipped, effect.GetHasTakenEffect());
                    Debug.Log($"TriggerEffect 數值: {baseValue_ToUnit} 狀態: {effect.GetEffectType()}");
                    return (baseValue_ToUnit, unitDiceCount, isUnitSkipped);
                }
            case BattleStatusEffectType.Burnt:
                {
                    unitDiceCount = Burnt(unitDiceCount, 1);
                    Debug.Log($"TriggerEffect 數值: {baseValue_ToUnit} 狀態: {effect.GetEffectType()}");
                    return (baseValue_ToUnit, unitDiceCount, isUnitSkipped);
                }
            case BattleStatusEffectType.StarThreaten:
                {
                    (isAssignFixedDiceValue_CardUser, assignFixedDiceValue, assignFixedDiceCount) = StarThreaten(effect.GetLastTurn(), effect.GetHasTakenEffect());
                    Debug.Log($"TriggerEffect 數值: {baseValue_ToUnit} 狀態: {effect.GetEffectType()}");
                    return (baseValue_ToUnit, unitDiceCount, isUnitSkipped);
                }


        }

        return (baseValue_ToUnit, unitDiceCount, isUnitSkipped);
    }

    #endregion

    #region  Both

    public (int, int) BothTypeCard(CardType cardType, int baseValue, int effectValue, int turn, int addTimes, int userDiceCount, int targetDiceCount)
    {
        (int baseValue_ToRival, int baseValue_ToUser) = (0, 0);
        switch (cardType)
        {
            case CardType.Dizziness:
                {
                    baseValue_ToRival = DizzinessAttack(baseValue, turn, addTimes);
                    Debug.Log($"Dizziness 對敵人的數值: {baseValue_ToRival} 對自己的數值{baseValue_ToUser}");
                    return (baseValue_ToRival, baseValue_ToUser);
                }
            case CardType.Oath:
                {
                    baseValue_ToRival = OathAttack(baseValue_ToRival);
                    break;
                }
        }

        return (baseValue_ToRival, baseValue_ToUser);
    }

    #endregion

    #region  Functional

    public (int, int, int, int, int) HealFunctionalCard(CardType cardType, int baseValue, int userDiceCount, int targetDiceCount)
    {
        (int baseValue_ToRival, int baseValue_ToUser) = (0, 0);
        switch (cardType)
        {
            case CardType.Heal:
                {
                    baseValue_ToUser = Heal(baseValue);
                    break;
                }
        }
        return (baseValue_ToRival, baseValue_ToUser, baseValue, userDiceCount, targetDiceCount);
    }

    public (List<BattleStatusEffect>, List<BattleStatusEffect>) EffectStatusFunctionalCard(CardType cardType, int baseValue, int userDiceCount, int targetDiceCount, List<BattleStatusEffect> userStatus, List<BattleStatusEffect> targetStatus)
    {
        switch (cardType)
        {
            case CardType.Recover:
                {
                    userStatus = Recover(userStatus);
                    break;
                }
            case CardType.Purify:
                {
                    userStatus = Purify(userStatus);
                    break;
                }
        }
        return (userStatus, targetStatus);
    }

    public (int, int, int, int, int) DiceFunctionalCard(CardType cardType, int baseValue, int userDiceCount, int targetDiceCount)
    {
        (int baseValue_ToRival, int baseValue_ToUser) = (0, 0);
        switch (cardType)
        {

            case CardType.Dismantle:
                {
                    (baseValue, userDiceCount) = Dismantle(baseValue, userDiceCount);
                    break;
                }
            case CardType.Clone:
                {
                    (baseValue, userDiceCount) = Clone(baseValue, userDiceCount);
                    break;
                }
            case CardType.Reverse:
                {
                    (baseValue, userDiceCount) = Reverse(baseValue, userDiceCount);
                    break;
                }
            case CardType.Redice:
                {
                    (baseValue, userDiceCount) = Redice(baseValue, userDiceCount);
                    break;
                }
        }
        return (baseValue_ToRival, baseValue_ToUser, baseValue, userDiceCount, targetDiceCount);
    }

    public (int, int, int, int, int) ValueCalculationFunctionalCard(CardType cardType, int baseValue, int userDiceCount, int targetDiceCount)
    {
        (int baseValue_ToRival, int baseValue_ToUser) = (0, 0);
        switch (cardType)
        {
            case CardType.HolyProtect:
                {
                    // userDiceCount = (baseValue, userDiceCount);
                    break;
                }
            case CardType.StarThreaten:
                {
                    // userDiceCount = Burnt(baseValue, userDiceCount);
                    break;
                }
                // case CardType.Oath:
                //     {
                //         // userDiceCount = Burnt(baseValue, userDiceCount);
                //         break;
                //     }
        }

        return (baseValue_ToRival, baseValue_ToUser, baseValue, userDiceCount, targetDiceCount);
    }
    #endregion

    #region Attacked
    public (int, int) AttackedCard(CardType cardType, int baseValue, int turn, int addTimes)
    {
        (int baseValue_ToRival, int baseValue_ToUser) = (0, 0);
        switch (cardType)
        {
            case CardType.Attack:
                {
                    baseValue_ToRival = Attack(baseValue);
                    return (baseValue_ToRival, baseValue_ToUser);
                }
            // case CardType.Poisoned:
            //     {
            //         // baseValue_ToRival = Poisoned(baseValue, turn, addTimes);
            //         Debug.Log($"Need To Add {CardType.Poisoned.ToString()} Status");
            //         return (baseValue_ToRival, baseValue_ToUser);
            //     }
            // case CardType.Dizziness:
            //     {
            //         baseValue_ToRival = StunAttack(baseValue, turn, addTimes);
            //         return (baseValue_ToRival, baseValue_ToUser);
            //     }
            case CardType.HeavyAttack:
                {
                    (baseValue_ToRival, baseValue_ToUser) = HeavyAttack(baseValue);
                    return (baseValue_ToRival, baseValue_ToUser);
                }
            case CardType.LazerGun:
                {
                    (baseValue_ToRival, baseValue_ToUser) = LazerGunAttack();

                    return (baseValue_ToRival, baseValue_ToUser);
                }
            default:
                {
                    return (baseValue_ToRival, baseValue_ToUser);
                }
        }
    }
    #endregion

    /// <summary>
    /// DOT: Damage Over Time 持續性傷害。參數：骰數、持續回合、效果施加次數。
    /// </summary><param name="value">骰數</param><param name="lastTurn">持續回合</param><param name="DOTAddTimes">效果施加次數</param><returns></returns>
    int DOTCalculate(int value, int lastTurn, int DOTAddTimes)
    {
        // 傷害疊加計算
        if (DOTAddTimes > 0) // 效果疊加次數>0
        {
            // Debug.Log("數值: " + DOTAddTimes + " value: " + value);

            value = value * DOTAddTimes;
        }
        Debug.Log("數值: " + DOTAddTimes + " value: " + value);

        return value;
    }

    #endregion

    #region "Boss敵人 聖女卡片"
    /// <summary>
    /// 回傳是否要指定骰子數值的bool、色子數值、指定的骰子顆數
    /// </summary>
    public (bool, int, int) StarThreaten(int lastTurn, bool hasTaken)
    {
        bool isAssigned = false;
        int value = 4, count = 99; // 99為有多少骰子即指定多少骰子
        if (lastTurn > 0)
            isAssigned = true;
        else
        {
            if (hasTaken == true)
                isAssigned = false;
            else
                isAssigned = true;
        }
        return (isAssigned, value, count);
    }
    /// <summary>
    /// 若再骰3顆骰子出來的結果相加 >10 則對玩家造成6點傷害。  參數：damage
    /// </summary>
    int OathAttack(int baseValue) // 再骰三顆骰子，若相加>10則對玩家造成6點傷害，但下回合受到的傷害x2
    {
        (List<int> diceValues, int temp_Total, bool isAttack, int conditionValue) = (new List<int>(), 0, false, 10);
        int valueForCheck = 3;
        diceValues = BonusRoll(valueForCheck);
        foreach (int dice in diceValues)
        {
            temp_Total += dice;
            if (temp_Total > conditionValue)
            {
                isAttack = true;
                break;
            }
        }
        Debug.Log("結果 :" + temp_Total + " bool: " + isAttack);
        if (isAttack)
        {
            Debug.Log("毒誓計算: " + "是否>10能攻擊: " + isAttack);
            baseValue = -6;
            return baseValue;
            // 攻擊
        }
        else
        {
            Debug.Log("毒誓計算: " + "是否>10能攻擊: " + isAttack);
            return baseValue;
        }
    }

    List<int> BonusRoll(int userDiceCount) // 有幾顆就重骰幾顆
    {
        (int value, int tempCount, List<int> dices) = (0, 0, new List<int>());

        while (tempCount < userDiceCount)
        {
            value = Random.Range(1, 7);
            dices.Add(value);
            tempCount++;
            if (tempCount >= userDiceCount)
            {
                break;
            }
        }
        return dices;
    }

    public List<BattleStatusEffect> Purify(List<BattleStatusEffect> userEffects) // 去除自己身上所有效果（無論好壞）
    {
        Debug.Log("b 狀態數: " + userEffects.Count);

        while (userEffects.Count > 0)
        {
            Debug.Log("m 狀態數: " + userEffects.Count);

            userEffects = ReduceStatus(userEffects);
            if (userEffects.Count <= 0)
            {
                Debug.Log("a 狀態數: " + userEffects.Count);
                break;
            }
        }
        return userEffects;
    }

    #endregion

    #region "效果類卡片"

    /// <summary>
    /// 重骰骰子。注意!!這個回傳值是「骰數和骰子的數量」
    /// </summary>
    public (int, int) Redice(int baseValue, int count)
    {

        int newValue;
        do
        {
            newValue = Random.Range(1, 7);
        } while (newValue == baseValue); // 確保新值與 baseValue 不相同
        count += 1; // 加用卡耗掉的骰子 
        return (newValue, count);
        // return (Random.Range(1, 7), count);
    }

    /// <summary>
    /// 顛倒骰子數字（7-X）。注意!!這個回傳值是「骰數和骰子的數量」
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public (int, int) Reverse(int value, int count)
    {
        count += 1; // 加用卡耗掉的骰子

        return (7 - value, count);
    }

    /// <summary>
    /// 再得一顆點數為X的骰子。注意!!這個回傳值是「骰數和骰子的數量」
    /// </summary>
    /// <param name="value">骰數</param>
    /// <param name="count">骰子數量</param>
    /// <returns></returns>
    public (int, int) Clone(int value, int count)
    {
        count += 2; // 加用卡耗掉的、以及複製的骰子
        return (value, count);
    }

    /// <summary>
    /// 換成X個1點。注意!!這個回傳值是「骰數和轉換後骰子應有的數量」
    /// </summary>
    /// <param name="value">骰數</param>
    /// <param name="count">骰子數量</param>
    public (int, int) Dismantle(int value, int count) // 要傳回給玩家
    {
        count += value;
        value = 1;
        return (value, count);
    }

    /// <summary>
    /// 回復自身X點生命
    /// </summary>
    /// <param name="cure">骰數</param>
    /// <param name="owmHp">使用者的生命值</param>
    public int Heal(int cure)
    {
        // ownHp = CommonHeal(cure);
        return cure;
    }

    // /// <summary>
    // /// 回復自身X點生命
    // /// </summary>
    // /// <param name="cure">骰數</param>
    // /// <param name="owmHp">使用者的生命值</param>
    // public int CommonHeal(int cure, int ownHp, int ownMaxHp)
    // {
    //     ownHp += cure;
    //     if (ownHp > ownMaxHp)
    //         ownHp = ownMaxHp;
    //     return ownHp;
    // }

    /// <summary>
    /// 使被影響者骰子總數減少1顆。注意!!這個回傳值是「扣除一顆骰子後的數量」
    /// </summary>
    /// <param name="diceCount">骰子的數量</param>
    /// <param name="lastTurn">持續回合</param>
    /// <returns></returns>
    public int Burnt(int diceCount, int numOfReduce)
    {
        if (diceCount > 0)
        {
            diceCount += numOfReduce * (-1);
        }
        return diceCount;
    }

    /// <summary>
    /// 去除自身一種異常狀態。注意!!這個回傳值是「狀態List」
    /// </summary>
    /// <param name="userEffects">被恢復者有的狀態列表</param>
    public List<BattleStatusEffect> Recover(List<BattleStatusEffect> userEffects)
    {
        userEffects = ReduceStatus(userEffects);
        return userEffects;
    }

    List<BattleStatusEffect> ReduceStatus(List<BattleStatusEffect> userEffects)
    {
        if (userEffects != null)
        {
            if (userEffects.Count > 0 && userEffects[0].GetEffectType() != BattleStatusEffectType.None)
                userEffects.Remove(userEffects[0]); // 移除最先有的異常狀態
        }
        else
        {
            Debug.Log("沒有異常狀態");
        }
        return userEffects;
    }

    #endregion

    #region "攻擊類卡片"

    /// <summary>竊取對手10點血量，並回復自己5點血量。回傳值：對敵人傷害值、對自己的回復值</summary>
    /// <param name="damage">骰數</param><param name="heal">作用目標生命值</param>
    public (int, int) LazerGunAttack()
    {
        int damageRival = 10; int healSelf = 5;
        damageRival *= (-1);
        // hp = DamageCalculation(LazerGunDamage, hp);
        // ownHp = CommonHeal(LazerGunHeal, ownHp, ownMaxHp);
        // Debug.Log("雷射槍: " + hp + " " + ownMaxHp + " " + ownHp);
        return (damageRival, healSelf);
    }

    /// <summary>給予對手2X點傷害，並造成自己X/2傷害，回傳值：對敵人傷害值、對自己的傷害值</summary>
    /// <param name="damage">骰數</param>
    public (int, int) HeavyAttack(int damage)
    {
        int damToSelf = Mathf.RoundToInt(damage / 2) * (-1); // 敵人受的傷害 
        int damToRival = damage * 2 * (-1);
        return (damToRival, damToSelf);
    }

    /// <summary>造成X點傷害，並暈眩對方一回合；當敵人已暈眩時，傷害及回合累加。 回傳值：是否暈眩(跳回合)</summary>
    /// <param name="lastTurn">持續回合</param><param name="isSkip">是否暈眩</param>
    public bool Dizziness(int lastTurn, int DOTAddTimes, bool isSkip, bool hasTaken) // 可能要加個管理暈眩的bool值
    {
        if (lastTurn > 0)
            isSkip = true;
        else
        {
            if (hasTaken == true)
                isSkip = false;
            else
                isSkip = true;
        }

        Debug.Log("暈眩計算:  " + "是否跳回合: " + isSkip + " 持續回合: " + lastTurn + " 施加次數: " + DOTAddTimes + " 以觸發: " + hasTaken);
        return isSkip;
    }
    /// <summary>造成X點傷害，並暈眩對方一回合；當敵人已暈眩時，傷害及回合累加。回傳值：傷害值</summary> 
    /// <param name="damage">骰數</param><param name="lastTurn">持續回合</param><param name="DOTAddTimes">效果施加次數</param>
    /// <param name="isSkip">是否暈眩</param>
    public int DizzinessAttack(int damage, int lastTurn, int DOTAddTimes)
    {
        damage = DOTCalculate(damage, lastTurn, DOTAddTimes);
        damage *= (-1);
        // Debug.Log("數值: " + damage + " ");

        // hp = DamageCalculation(damage, hp);
        // Debug.Log("暈眩計算:  " + " 持續回合: " + lastTurn + " 施加次數: " + DOTAddTimes);
        return damage;
    }
    /// <summary>在敵人回合開始時，造成敵人２點傷害，持續３回合。被施加者中毒後，在「被施加者每次回合開始時」觸發。
    /// 參數：骰數、持續回合、效果施加次數。回傳值：傷害值</summary>
    /// <param name="damage">骰數</param><param name="hp">作用目標生命值</param>/// <param name="lastTurn">持續回合</param><param name="DOTAddTimes">效果施加次數</param><returns></returns>
    public int Poisoned(int damage, int lastTurn, int DOTAddTimes)
    {
        // 用 DOTCalculate 計算疊加效果與回合
        damage = DOTCalculate(damage, lastTurn, DOTAddTimes);
        damage *= (-1);
        // Debug.Log("中毒計算:  傷害: " + damage + " HP: " + hp + " 持續回合: " + lastTurn + " 施加次數: " + DOTAddTimes);
        // hp = DamageCalculation(damage, hp);
        // Debug.Log("中毒傷害計算: 傷害: " + damage + " HP: " + hp);
        return damage;
    }

    /// <summary>
    /// 普通攻擊，需要骰數與作用目標生命值。參數：骰數、作用目標生命值。
    /// </summary><param name="damage">骰數</param>
    public int Attack(int damage)
    {
        damage *= (-1);
        return damage;
    }
    #endregion
}
