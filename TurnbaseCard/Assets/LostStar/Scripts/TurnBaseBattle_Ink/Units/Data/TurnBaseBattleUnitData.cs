using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
public class TurnBaseBattleUnitData
{
    [SerializeField] protected TurnBaseBattleOrderType turnOrder = TurnBaseBattleOrderType.Undefined;
    [SerializeField] protected List<BattleStatusEffect> statusEffects = new List<BattleStatusEffect>();
    [SerializeField] protected int OriginMaxHp;
    [SerializeField] public int currentHp = 0;
    [SerializeField] public string unitName = "";
    [SerializeField] public string unitTW_Name = "";

    [SerializeField] protected int OriginMaxCountOfDice = 2;
    [SerializeField] protected int CountOfDice = 2;

    public const string Player1 = "Player1";
    public const string Player2 = "Player2";
    public const string Enemy = "Enemy";
    #region  Get Set

    public void SetTurnOrder(TurnBaseBattleOrderType count) => turnOrder = count;
    public TurnBaseBattleOrderType GetTurnOrder() => turnOrder;
    public void SetOriginMaxCountOfDice(int count) => OriginMaxCountOfDice = count;
    public int GetOriginMaxCountOfDice() => OriginMaxCountOfDice;
    public int GetCountOfDice() => CountOfDice;
    public void SetCountOfDice(int count) => CountOfDice = count;
    public string GetUnitName() => unitName;
    public void SetUnitName(string name) => unitName = name;
    public string GetTW_UnitName() => unitTW_Name;
    public void SetTW_UnitName(string name) => unitTW_Name = name;

    public List<BattleStatusEffect> GetStatusEffects() => statusEffects;
    public void SetStatusEffects(List<BattleStatusEffect> status) => statusEffects = status;

    public int GetOriginMaxHp() => OriginMaxHp;
    public void SetOriginMaxHp(int curHp) => OriginMaxHp = curHp;

    public int GetCurHp() => currentHp;
    public void SetCurHp(int curHp) => currentHp = curHp;

    #endregion



    /// <summary>初始化指定種類單位的原始數值</summary>
    protected virtual T InitUnitInfo<T>(CharacterType characterType, EnemyType baseEnemyType, EnemyType enemyType) where T : TurnBaseBattleUnitData, new()
    {
        T unitData = new T();
        return unitData;
    }


    #region  ForCheat
    /// <summary>作弊用 初始化指定種類單位的原始數值</summary>
    protected virtual void Cheat_InitUnitInfo(CharacterType characterType, EnemyType enemyType) { }
    #endregion


}
