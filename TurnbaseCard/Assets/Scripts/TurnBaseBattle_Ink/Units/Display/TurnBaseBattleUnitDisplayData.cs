using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Image = UnityEngine.UI.Image;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using Unity.VisualScripting;
public class TurnBaseBattleUnitDisplayData : MonoBehaviour
{
    public BattleTurnBaseEvent battleTurnBaseEvent = new BattleTurnBaseEvent();
    [Header("基本的共通數值")]
    public BattleUnitProfile profile;
    public TurnBaseBattleOrderType turnOrder = TurnBaseBattleOrderType.Undefined;
    [SerializeField] protected List<BattleStatusEffect> statusEffects = new List<BattleStatusEffect>();
    [SerializeField] protected int OriginMaxHp;
    [SerializeField] protected int currentHp = 0;
    [SerializeField] public string unitName = "";
    [SerializeField] public string unitTW_Name = "";

    public const string Player1 = "Player1";
    public const string Player2 = "Player2";
    public const string Enemy = "Enemy";
    [SerializeField] protected int OriginMaxCountOfDice = 2;
    [SerializeField] protected int CountOfDice = 2;
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
    #region  "訂閱"
    // 用來觸發Action以傳數值
    private void OnEnable()
    {
        BattleTurnBaseEvent.OnTurnOrderSent += GetTurnOrder;
        BattleTurnBaseEvent.OnReadyForInfo += ReadyForInfo;
        BattleTurnBaseEvent.OnUnitsInfoFromManagerSent += GetUnitInfoFromManager;
    }
    private void OnDisable()
    {
        BattleTurnBaseEvent.OnTurnOrderSent -= GetTurnOrder;
        BattleTurnBaseEvent.OnReadyForInfo -= ReadyForInfo;
        BattleTurnBaseEvent.OnUnitsInfoFromManagerSent -= GetUnitInfoFromManager;
    }

    void GetUnitInfoFromManager(TurnBaseBattleUnitDisplayData user, TurnBaseBattleUnitDisplayData target, TurnBaseBattleOrderType order)
    {
        if (order == turnOrder)// 單人(劇情模式) 玩家默認為Player1
        {
            SetOriginMaxHp(user.OriginMaxHp);
            SetCurHp(user.currentHp);
            SetStatusEffects(user.statusEffects);
            // SetIsCurrentUnitSkip(user.IsUnitSkip);

        }
        else if (order != turnOrder)
        {
            SetOriginMaxHp(target.OriginMaxHp);
            SetCurHp(target.currentHp);
            SetStatusEffects(target.statusEffects);
            // SetIsCurrentUnitSkip(target.IsUnitSkip);
        }
        UpdateProfileInfo();
        UpdateProfileUI();

    }

    void ReadyForInfo()
    {
        battleTurnBaseEvent.SendUnitInfo(this); // 調用 GetCurrentUnits
        // Debug.Log("ReadyForInfo!!!!!!! ");
    }

    void GetTurnOrder(TurnBaseBattleOrderType order, string tag) // 訂閱的事件 由Action觸發
    {
        if (this.gameObject.tag == tag)// 單人(劇情模式) 玩家默認為Player1
        {
            turnOrder = order;
            // Debug.Log("目前順序: " + turnOrder + "被指派順序: " + order + " 誰: " + tag);
        }
    }
    #endregion

    #region  InitTurnBaseSystemUnitInfo Module
    protected virtual (CharacterType, EnemyType, EnemyType) SendUnitTypeToProfile()
    { return (CharacterType.Undefined_Temp_ThisIsTypeEndNumber, EnemyType.Undefined_Temp_ThisIsTypeEndNumber, EnemyType.Undefined_Temp_ThisIsTypeEndNumber); }

    void UpdateProfileUI()
    {
        profile.UpdateProfileUI();
    }

    void UpdateProfileInfo()
    {
        // profile.SetUnitName(unitName); // En
        profile.SetUnitName(unitTW_Name); // Tw

        profile.SetUnitDiceCount(OriginMaxCountOfDice);
        profile.SetOriHPCount(OriginMaxHp);
        profile.SetCurHPCount(currentHp);

        List<BattleStatusEffectType> statusType = new List<BattleStatusEffectType>();
        foreach (var se in statusEffects)
        {
            statusType.Add(se.GetEffectType());
        }
        profile.SetStatus(statusType);
    }

    protected virtual void SetProfile()
    {
        profile = gameObject.GetComponent<BattleUnitProfile>();
        (CharacterType unit, EnemyType baseEnemyType, EnemyType endlessModeEnemyType) = SendUnitTypeToProfile();
        Debug.Log($"{baseEnemyType} {endlessModeEnemyType}");

        profile.GetUnitType(unit, baseEnemyType, endlessModeEnemyType);
        profile.GetOriginSpineAnimColor();
        profile.InitProfile(); // 對身分   

        profile.SetHPBarWidthBackToOriginalWidth();

        UpdateProfileInfo();
        UpdateProfileUI();
    }

    protected virtual void GetTw_NameString() { }

    /// <summary>
    /// 在觸發戰鬥時取得傳入的單位資料
    /// </summary>
    /// <param name="unit"></param>
    protected virtual void GetUnitData(TurnBaseBattleUnitData unit)
    {
        this.turnOrder = unit.GetTurnOrder();
        this.statusEffects = new List<BattleStatusEffect>();
        this.OriginMaxHp = unit.GetOriginMaxHp();
        this.currentHp = unit.GetCurHp();
        this.unitName = unit.unitName;
        // this.unitTW_Name = unit.unitTW_Name;
        this.OriginMaxCountOfDice = unit.GetOriginMaxCountOfDice();
        this.CountOfDice = unit.GetCountOfDice();
    }

    /// <summary>將戰鬥系統會顯示的「介面資料」初始化</summary>
    public virtual void InitTurnBaseBattleSystemUnitInfo(TurnBaseBattleUnitData unit)
    {
        GetUnitData(unit);
        GetTw_NameString();
        SetProfile();
        Debug.Log($"接收資料: 骰數: {OriginMaxCountOfDice} {CountOfDice}");
    }

    #endregion

}

