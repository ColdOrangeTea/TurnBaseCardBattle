using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;
using System;
public class BattleUnitEnemyDisplayData : TurnBaseBattleUnitDisplayData
{
    [Header("敵人 UnitType需從人物或怪物的資料取得")]
    // [SerializeField] private TurnBaseBattleType enemyOrder = TurnBaseBattleType.Undefined; // 敵人的順位
    public CharacterType UnitType = CharacterType.Enemy; // 
    public EnemyType BaseEnemyType; // 基本的敵人資料型別
    public EnemyType EndlessModeEnemyType;  // 對應的無盡模式敵人型別(是人形機器薯，不包含馬鈴薯型態)

    [SerializeField]
    private SO_BattleEnemyNameAndTitle SO_EnemyNameAndTitle;

    // [SerializeField]
    private List<EnemyType> SO_EnemyNameTypeList;
    // [SerializeField]
    private List<string> SO_EnemyNameStringList;

    #region "中文名稱"
    public const string Tw_Yarn = "偷走毛線球的人";
    public const string Tw_Swordsman = "神秘劍客";
    public const string Tw_Boy = "孩子王";
    public const string Tw_Nun = "修女";
    public const string Tw_Preacher = "瘋狂傳教者";
    public const string Tw_Godness = "聖女 奧蘭娜";

    #endregion

    // 怪物的卡組應該是固定的?

    // 掉落物品到時候要做個分類

    // 怪物應該不會升級吧...?

    public (CardType, int) EnemyBehavior(int diceValue)
    {
        CardType action = TurnBaseEnemyBehavior.GetEnemyAction(BaseEnemyType, diceValue);

        Debug.Log($"取得敵人行動卡片資料: {action}");
        return (action, diceValue);
    }

    #region  InitTurnBaseSystemUnitInfo Module

    protected override (CharacterType, EnemyType, EnemyType) SendUnitTypeToProfile()
    {
        return (UnitType, BaseEnemyType, EndlessModeEnemyType);
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
    protected override void GetTw_NameString()
    {
        EnemyType temp_EnemyType = BaseEnemyType;

        if (CheckIfEnemyInEndlessMode(EndlessModeEnemyType))
        {
            temp_EnemyType = EndlessModeEnemyType;
        }

        SetUnitName(EndlessModeEnemyType.ToString());
        if (SO_EnemyNameAndTitle != null)
        {
            SO_EnemyNameTypeList = SO_EnemyNameAndTitle.NameTypeList;
            SO_EnemyNameStringList = SO_EnemyNameAndTitle.Tw_EnemyNameList; // 人物的中文名稱
            foreach (EnemyType item in Enum.GetValues(typeof(EnemyType)))
            {
                int count = (int)item;
                // Debug.Log($"{item} {SO_EnemyNameStringList[count]}  {count}");

                if (count <= SO_EnemyNameTypeList.Count && temp_EnemyType == SO_EnemyNameTypeList[count])
                {
                    Debug.Log($"{temp_EnemyType} {SO_EnemyNameStringList[count]}  {count}");
                    SetTW_UnitName(SO_EnemyNameStringList[count]);
                    break;
                }
            }
        }
    }

    protected override void GetUnitData(TurnBaseBattleUnitData unit)
    {
        TurnBaseBattleEnemyData enemyData = unit as TurnBaseBattleEnemyData;// 若不是 as BattleUnitEnemyData 會回傳 null
        // TurnBaseBattlePlayerData playerData = unit as TurnBaseBattlePlayerData;// 若不是 as BattleUnitEnemyData 會回傳 null

        if (enemyData != null)
        {
            UnitType = enemyData.UnitType;
            BaseEnemyType = enemyData.BaseEnemyType;
            if (CheckIfEnemyInEndlessMode(enemyData.EndlessModeEnemyType)) // 取得的type
            {
                EndlessModeEnemyType = enemyData.EndlessModeEnemyType;
            }

        }
        else
        {
            Debug.LogWarning($"傳入的 TurnBaseBattleUnit 不正確 ");
        }

        Debug.Log($"傳入的 Type 為 {enemyData.UnitType} {enemyData.BaseEnemyType} {enemyData.EndlessModeEnemyType}"
                 + $"目前戰鬥系統接收到的Type {UnitType} {BaseEnemyType} {EndlessModeEnemyType} ");
        base.GetUnitData(unit);

    }

    public override void InitTurnBaseBattleSystemUnitInfo(TurnBaseBattleUnitData unit)
    {
        base.InitTurnBaseBattleSystemUnitInfo(unit);
        Debug.Log($"+敵人資料{BaseEnemyType.ToString()} {EndlessModeEnemyType.ToString()}");

    }

    #endregion

}