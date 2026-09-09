using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;

public class BattleUnitPlayerDisplayData : TurnBaseBattleUnitDisplayData
{
    [Header("人物 UnitType需從人物或怪物的資料取得")]
    public CharacterType UnitType;
    [SerializeField]
    private SO_BattleCharacterNameAndTitle SO_CharaNameAndTitle;
    // [SerializeField]
    private List<CharacterType> SO_CharaNameTypeList;
    // [SerializeField]
    private List<string> SO_CharaNameStringList;

    // 卡組的資料 再太空船能調整 等卡片功能完工再補上

    [Header("升等增加的值")]
    public const int LevelUpAddHp = 10;
    public const int LevelUpAddDiceCount = 1;


    #region  InitTurnBaseSystemUnitInfo Module

    protected override (CharacterType, EnemyType, EnemyType) SendUnitTypeToProfile()
    {
        return (UnitType, EnemyType.Undefined_Temp_ThisIsTypeEndNumber, EnemyType.Undefined_Temp_ThisIsTypeEndNumber);
    }

    // 如果之後做選角自由戰鬥的模式 可以用 CharacterType 選腳色XD
    protected override void GetTw_NameString()
    {
        SetUnitName(UnitType.ToString());
        if (SO_CharaNameAndTitle != null)
        {
            SO_CharaNameTypeList = SO_CharaNameAndTitle.NameTypeList;
            SO_CharaNameStringList = SO_CharaNameAndTitle.Tw_CharacterNameList; // 人物的中文名稱
            foreach (CharacterType item in SO_CharaNameTypeList)
            {
                int count = (int)item;
                if (UnitType == SO_CharaNameTypeList[count])
                {
                    // Debug.Log($"{UnitType} {SO_CharaNameTypeList[count]}  {count}");

                    SetTW_UnitName(SO_CharaNameStringList[count]);
                }
            }
        }
    }

    protected override void GetUnitData(TurnBaseBattleUnitData unit)
    {
        TurnBaseBattleEnemyData enemyData = unit as TurnBaseBattleEnemyData;// 若不是 as BattleUnitEnemyData 會回傳 null
        TurnBaseBattlePlayerData playerData = unit as TurnBaseBattlePlayerData;// 若不是 as BattleUnitEnemyData 會回傳 null
        if (playerData != null)
        {
            UnitType = playerData.UnitType;
        }
        else
        {
            Debug.LogWarning($"傳入的 TurnBaseBattleUnit 不正確 ");
        }

        base.GetUnitData(unit);
    }

    public override void InitTurnBaseBattleSystemUnitInfo(TurnBaseBattleUnitData unit)
    {
        base.InitTurnBaseBattleSystemUnitInfo(unit);
        Debug.Log("+人物資料" + UnitType.ToString());
    }
    #endregion

}
