using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using System;

public class TurnBaseBattleEnemyData : TurnBaseBattleUnitData
{
    [Header("敵人 UnitType需從人物或怪物的資料取得")]
    public CharacterType UnitType = CharacterType.Enemy;
    public EnemyType BaseEnemyType; // 基本的敵人資料型別
    public EnemyType EndlessModeEnemyType; // 對應的無盡模式敵人型別(是人形機器薯，不包含馬鈴薯型態)

    // 無盡模式的人形機器薯敵人
    public Dictionary<EnemyType, EnemyType> GetHumanPotatoToBaseMapping() => HumanPotatoToBaseMapping;

    private static readonly Dictionary<EnemyType, EnemyType> HumanPotatoToBaseMapping = new Dictionary<EnemyType, EnemyType>
    {
        { EnemyType.PotatoFarmer, EnemyType.Yarn },
        { EnemyType.PotatoFried, EnemyType.Boy },
        { EnemyType.PotatoChef, EnemyType.Swordsman },
        { EnemyType.PotatoSuki, EnemyType.Nun },
        { EnemyType.PotatoPoison, EnemyType.Preacher },
        { EnemyType.PotatoMoonlight, EnemyType.Godness }
    };

    public TurnBaseBattleEnemyData InitEnemyInfo(EnemyType enemyType) // 在戰鬥開始前用此方法取得敵人的資訊
    {
        (EnemyType temp_EnemyType, EnemyType temp_EndlessModeEnemyType) = (enemyType, enemyType);
        if (CheckIfEnemyInEndlessMode(temp_EnemyType) == true) // 檢查輸入的Type是否是無盡模式的人型薯
        {
            temp_EnemyType = MapToStoryModeType(enemyType);
            temp_EndlessModeEnemyType = enemyType;
        }
        else
        {
            temp_EnemyType = enemyType; // 可能是劇情模式的一般小怪、無盡模式的薯型薯
            temp_EndlessModeEnemyType = EnemyType.Undefined_Temp_ThisIsTypeEndNumber;
        }
        Debug.LogWarning($"InitEnemyInfo 是人形馬鈴薯: {CheckIfEnemyInEndlessMode(temp_EnemyType) == true} {enemyType} {temp_EnemyType} {temp_EndlessModeEnemyType} 取得敵人資料");

        TurnBaseBattleEnemyData enemyData = InitUnitInfo<TurnBaseBattleEnemyData>(CharacterType.Enemy, temp_EnemyType, temp_EndlessModeEnemyType);

        return enemyData;
    }

    protected override T InitUnitInfo<T>(CharacterType characterType, EnemyType baseEnemyType, EnemyType endlessModeEnemyType)
    {
        T unitData = new T(); // 型別是否相容
        if (unitData is TurnBaseBattleEnemyData enemyData)
        {
            (TurnBaseBattleOrderType turnOrder, List<BattleStatusEffect> battleStatusEffects,
            string EN_name, int OriMaxHp, int curHp, int OriginMaxCountOfDice, int CountOfDice) = GetInfo(baseEnemyType);

            // 初始化 BattleUnitEnemyData
            enemyData.UnitType = characterType;
            enemyData.BaseEnemyType = baseEnemyType;
            enemyData.EndlessModeEnemyType = endlessModeEnemyType;
            enemyData.turnOrder = turnOrder;
            enemyData.statusEffects = battleStatusEffects;
            enemyData.unitName = EN_name;
            enemyData.OriginMaxHp = OriMaxHp;
            enemyData.currentHp = curHp;
            enemyData.OriginMaxCountOfDice = OriginMaxCountOfDice;
            enemyData.CountOfDice = CountOfDice;
        }

        return unitData;

    }

    bool CheckIfEnemyInEndlessMode(EnemyType temp_EnemyType) // 檢查是否是無盡模式的人型薯
    {
        if (temp_EnemyType == EnemyType.Undefined_Temp_ThisIsTypeEndNumber)
        {
            return false;
        }
        else
        {
            return true;
        }

    }


    public EnemyType ConvertToStoryEnemy(EnemyType thisEnemy) => MapToStoryModeType(thisEnemy);
    EnemyType MapToStoryModeType(EnemyType thisEnemy) // 無盡模式映射至故事模式的敵人
    {
        EnemyType forCheckEnemyType = thisEnemy;

        // 檢查是否是 Potato 系列並進行映射
        if (HumanPotatoToBaseMapping.TryGetValue(forCheckEnemyType, out EnemyType baseEnemyType))
        {
            Debug.Log($"將 {forCheckEnemyType} 映射到基礎敵人類型 {baseEnemyType}");
            return baseEnemyType;// 無盡模式敵人的Type轉換成劇情模式的Type映射後的基本類型

        }
        Debug.Log($"返回 {forCheckEnemyType}，沒有對應的基礎敵人類型 ");
        return forCheckEnemyType;
    }

    /// <summary>參數順序：turnOrder, statusEffects, unitName, OriginMaxHp, currentHp, OriginMaxCountOfDice, CountOfDice</summary>
    (TurnBaseBattleOrderType, List<BattleStatusEffect>, String, int, int, int, int) GetInfo(EnemyType enemy)
    {
        // 數值改由 SO 資料表提供（Resources/SO_Battle/BattleUnitStats）。
        // 若要調整敵人 HP／骰子數，請改該資產，或執行選單「Tools/TurnBaseBattle/生成戰鬥資料 SO」重新生成。
        if (BattleDataProvider.TryGetEnemyStat(enemy, out int maxHp, out int diceCount))
        {
            return (TurnBaseBattleOrderType.Undefined, new List<BattleStatusEffect>(), enemy.ToString(), maxHp, maxHp, diceCount, diceCount);
        }
        else
        {
            Debug.LogWarning($"{enemy} EnemyType 指定不正確，無法取得敵人資料");
            return (TurnBaseBattleOrderType.Undefined, null, null, 0, 0, 0, 0);
        }
    }

}
