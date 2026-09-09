using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;

public class TurnBaseBattlePlayerData : TurnBaseBattleUnitData
{
    [Header("人物 UnitType需從人物或怪物的資料取得")]
    public CharacterType UnitType;

    [Header("升等增加的值")]
    public const int LevelUpAddHp = 10;
    public const int LevelUpAddDiceCount = 1;

    void LevelUpAddValue()
    {

    }

    public TurnBaseBattlePlayerData InitPlayerInfo(CharacterType characterType) // 在戰鬥開始前用此方法取得敵人的資訊
    {
        TurnBaseBattlePlayerData playerData = InitUnitInfo<TurnBaseBattlePlayerData>(characterType, EnemyType.Undefined_Temp_ThisIsTypeEndNumber, EnemyType.Undefined_Temp_ThisIsTypeEndNumber);
        return playerData;

    }

    protected override T InitUnitInfo<T>(CharacterType characterType, EnemyType baseEnemyType, EnemyType enemyType)
    {
        (TurnBaseBattleOrderType turnOrder, List<BattleStatusEffect> battleStatusEffects,
              string EN_name, int OriMaxHp, int curHp, int OriginMaxCountOfDice, int CountOfDice) = GetInfo(characterType);

        T unitData = new T(); // 型別是否相容
        if (unitData is TurnBaseBattlePlayerData playerData)
        {
            if (UnitType == CharacterType.Seraphis)
            {
                playerData.UnitType = characterType;
                playerData.turnOrder = turnOrder;
                playerData.statusEffects = battleStatusEffects;
                playerData.unitName = EN_name;
                playerData.OriginMaxHp = OriMaxHp;
                playerData.currentHp = curHp;
                playerData.OriginMaxCountOfDice = OriginMaxCountOfDice;
                playerData.CountOfDice = CountOfDice;
            }
        }
        return unitData;
    }

    public int GetCurrentHp()
    {
        return currentHp;
    }
    public int GetCurrentMaxHp()
    {
        return OriginMaxHp;
    }

    public int GetCurrentCountOfDice()
    {
        return CountOfDice;
    }

    #region  ForCheat
    // /// <summary>創立單位的原始資料</summary>
    // protected override void Cheat_InitUnitInfo(CharacterType characterType, EnemyType enemyType)
    // {
    //     if (UnitType == CharacterType.Seraphis)
    //     {
    //         SetSeraphisData();
    //     }
    // }

    // void SetSeraphisData() // 每個腳色分別做一個
    // {
    //     this.turnOrder = TurnBaseBattleOrderType.Undefined;
    //     List<BattleStatusEffect> status = new List<BattleStatusEffect>();
    //     this.SetStatusEffects(status);
    //     this.SetOriginMaxHp(16);
    //     this.SetCurHp(16);
    //     this.SetCountOfDice(2);
    //     this.SetOriginMaxCountOfDice(2);
    // }

    #endregion

    /// <summary>參數順序：turnOrder, statusEffects, unitName, OriginMaxHp, currentHp, OriginMaxCountOfDice, CountOfDice</summary>
    (TurnBaseBattleOrderType, List<BattleStatusEffect>, string, int, int, int, int) GetInfo(CharacterType character)
    {
        // 數值改由 SO 資料表提供（Resources/SO_Battle/BattleUnitStats）。
        // 若要調整角色 HP／骰子數，請改該資產，或執行選單「Tools/TurnBaseBattle/生成戰鬥資料 SO」重新生成。
        if (BattleDataProvider.TryGetPlayerStat(character, out int maxHp, out int diceCount))
        {
            return (TurnBaseBattleOrderType.Undefined, new List<BattleStatusEffect>(), character.ToString(), maxHp, maxHp, diceCount, diceCount);
        }
        else
        {
            Debug.LogWarning("CharacterType 指定不正確，無法取得玩家資料");
            return (TurnBaseBattleOrderType.Undefined, null, null, 0, 0, 0, 0);
        }
    }


}
