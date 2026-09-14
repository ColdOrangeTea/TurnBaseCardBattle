
using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.IO;
public class StatusValueSetter
{
    public List<BattleStatusEffect> effects = new List<BattleStatusEffect>();

    #region"Value Setting"
    public const int passOneTurn = 1; // 經過一回合
    #endregion

    public bool IfGetDizzinessEffect(bool isSkipped)
    {
        return isSkipped;
    }

    public void StackTimes(BattleStatusEffect statusEffect, int turn, int times)
    {
        statusEffect.SetLastTurn(statusEffect.GetLastTurn() + turn); // 疊加回合數
        statusEffect.SetDotAddTimes(statusEffect.GetDotAddTimes() + times);
    }
    public int ReduceLastTimes(int turn) // 傳進來的是此狀態的回合數
    {
        int ReducedTurn = turn;
        ReducedTurn -= passOneTurn;
        BattleLog.Log("目前持續的回合數: " + turn + " 減-1後的回合: " + ReducedTurn);

        if (ReducedTurn < 0)
            ReducedTurn = 0;

        return ReducedTurn;
    }
    public BattleEffectInfo GetStatusInfo(BattleStatusEffect statusEffect)
    {
        return new BattleEffectInfo(

        statusEffect.GetEffectType(),
        statusEffect.GetHasTakenEffect(), // 效果是否有作用過
        statusEffect.GetEffectValue(),
        statusEffect.GetLastTurn(),
        statusEffect.GetDotAddTimes(), // 施加次數，可根據需求設置

        statusEffect.GetIsAbleToRemoveAfterEffect(), // (初始為false，持續回合數為0時這個如果設為True才能移除狀態)
        statusEffect.GetIsRemovable(), // 可否用卡移除的狀態

        statusEffect.GetIsSelfAffecting(), // 是否作用於自己
        statusEffect.GetIsRivalAffecting(), // 是否作用於敵人
        statusEffect.GetIsSkippedTurn(), // 是否跳過回合

        statusEffect.GetAffectedCalculation(), // 是否影響計算
        statusEffect.GetOperationType(), // 計算的類型(加減乘除)

        statusEffect.GetAffectValueTypeList(),

        statusEffect.GetAffectedDice(), // 是否影響骰子

        statusEffect.GetActivatesTimingList(),
        statusEffect.GetRemovesTimingList(),

        statusEffect.IsTurnBasedEffect, // 持續回合效果

        statusEffect.isEqual, // 等於
        statusEffect.isGreaterThan, // 大於
        statusEffect.isLessThan,  // 小於

        statusEffect.GetEffectActiveTurnList()

        );
    }



}
// // 初始化方法，用來從指定資料夾中載入 XML 檔案
// public void LoadEffectsFromXml(string folderPath)
// {
//     // effects = XmlLoader.LoadAllBattleStatusEffectsInFolder(folderPath);
//     BattleLog.Log("Loaded " + effects.Count + " effects from XML.");
// }

