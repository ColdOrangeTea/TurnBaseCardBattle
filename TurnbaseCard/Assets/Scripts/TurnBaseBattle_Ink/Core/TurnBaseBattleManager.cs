using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using System.Collections;
using System;

public class TurnBaseBattleManager : MonoBehaviour
{
    private BattleTurnBaseEvent battleEvent = new BattleTurnBaseEvent();
    public TurnBaseBattleUI BattleUI;
    public BattleAction BattleAction;

    [Header("TurnBaseBattleFunctions 回合制戰鬥組件會引用到的程式")]

    [SerializeField]
    private GameObject turnBaseBattleScripts;
    [SerializeField]
    private S001_DiceSystem DiceSystem;
    [SerializeField]
    private DicePoolManager dicePoolManager;
    [SerializeField]
    private S002_DrawCardsSystem DrawCardSystem;
    [SerializeField]
    private S005_NumericalCalculation NumericalCalculation;

    [SerializeField]
    private BattleButtonFunction battleButtonFunction;

    [SerializeField]
    private TurnBaseBattleSetUp turnBaseBattleSetUp;
    [SerializeField]
    private GameObject UnitDatas;
    [SerializeField]
    private List<TurnBaseBattleUnitDisplayData> units; // [0]=player1 [1]=player2(Enemy)

    [Header("其他系統功能的管理器")]
    public GridManager gridmanager;
    public GameObject Player;

    [Header("回合制管理")]
    [SerializeField] bool IsPlayer1First = false;

    /// <summary>gameModes[0] = IsStoryMode    gameModes[1] = IsMultiplayer   gameModes[2] = IsEndlessMode</summary>
    [SerializeField] List<bool> gameModes;
    [SerializeField] List<bool> IsUnitDead = new List<bool>();
    [SerializeField] bool IsBattleOver = false;
    public TurnBaseBattleOrderType currentTurn = TurnBaseBattleOrderType.Undefined;
    [SerializeField] private string Player1Name = "";
    [SerializeField] private string Player2Name = "";
    [Header("單人(劇情模式)")]
    [SerializeField] private TurnBaseBattleOrderType playerOrder = TurnBaseBattleOrderType.Undefined; // (p1)玩家的順位
    [SerializeField] private TurnBaseBattleOrderType rivalOrder = TurnBaseBattleOrderType.Undefined; // (p2)對手的順位

    //英文的"Turn"跟"Round"雖然中文都可以翻譯為「回合」，但其實是2個不同的概念。round由好幾個turn組成
    //每個玩家執行完所有的玩家回合(turn)才進入下一遊戲回合(round)為了避免玩家誤解，在解說規則時可以用「玩家回合、遊戲回合」或「行動、回合」這樣不同的名詞來區分，比較方便理解喔！
    public int RoundCount = 0;
    public int TurnCount = 0;
    public string curWhoseTurn = "";

    [Header("該回合行動者")]
    [SerializeField] private TurnBaseBattleUnitDisplayData temp_CardUser;
    [SerializeField] private bool isSkipped_CardUser;
    [Header("指定骰數")]
    [SerializeField]
    private int assignFixedDiceValue = 0;
    [SerializeField]
    private int assignFixedDiceCount = 0;
    [SerializeField]
    private bool isAssignFixedDiceValue_CardUser = false;

    public bool IsplayerWin;


    [Header("該回合的對手")]
    [SerializeField] private TurnBaseBattleUnitDisplayData temp_Target;
    [SerializeField] private bool isSkipped_Target;

    /// <summary>
    /// 場上唯一的回合制戰鬥管理器。
    /// 原本繼承 ManagerMono&lt;T&gt; 由 GameManager 統一註冊並保證唯一，該系統移除後改為自行維護。
    /// </summary>
    public static TurnBaseBattleManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"場上已存在另一個 TurnBaseBattleManager，移除重複的 {name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // 原本由 GameManager 呼叫 InitFromGameManager()，GameManager 移除後改為自行初始化
        InitFromGameManager();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #region  "訂閱"
    private void OnEnable()
    {
        BattleTurnBaseEvent.OnUnitsInfoSent += GetCurrentUnits;
        BattleTurnBaseEvent.OnBattleSettingSent += GetBattleSetting;
    }
    private void OnDisable()
    {
        BattleTurnBaseEvent.OnUnitsInfoSent -= GetCurrentUnits;
        BattleTurnBaseEvent.OnBattleSettingSent -= GetBattleSetting;
    }
    /// <summary>
    /// 指定戰鬥的設定並開始第一回合。並把資料載入戰鬥系統裡。
    /// </summary>
    /// <param name="isFirst"></param>
    /// <param name="isMult"></param>
    void GetBattleSetting(SetBattleSetting battleSetting)
    {
        ResetTurnBaseBattleTempData();

        // units[0] = battleSetting.TB_BattleUnits[0]; // 資料載入戰鬥系統裡
        units[0].InitTurnBaseBattleSystemUnitInfo(battleSetting.TB_BattleUnits[0]); // 初始化戰鬥系統裡的版面與暫存資料

        // units[1] = battleSetting.TB_BattleUnits[1];
        units[1].InitTurnBaseBattleSystemUnitInfo(battleSetting.TB_BattleUnits[1]);

        IsPlayer1First = battleSetting.IsPlayer1First;
        gameModes = battleSetting.GameModes; // 載入遊戲模式
        BattleUI.SetBackGround(battleSetting.TB_OrderOfBackGround);
        // Debug.Log("設定先手並開始第一回合" + " 是否先手行動: " + isFirst + "是否多人: " + isMult);
        SetFirstTurn();

        // BattleUI.GetTurnCountText().text = "回合數：" + (RoundCount + 1);

        // curWhoseTurn = temp_CardUser.GetTW_UnitName();
        // BattleUI.SetWhoseTurn();

    }

    void SetTurnOrder(TurnBaseBattleOrderType order, string unit) => battleEvent.SendTurnOrder(order, unit);
    void ReadyForInfo() // 把玩家端和敵人端的資料傳來系統端 透過 ReadyForInfo() 觸發 GetCurrentUnits
    {
        BattleAction.ResetTempValue(); // Unit資料傳去 BattleAction
        battleEvent.GetReadyForInfo();
    }
    void SendUnitInfoFromManager(TurnBaseBattleUnitDisplayData player1, TurnBaseBattleUnitDisplayData player2, TurnBaseBattleOrderType order) => battleEvent.SendUnitsInfoFromManager(player1, player2, order);

    public void SendUnitInfoToUnit()
    {
        SendUnitInfoFromManager(GetTemp_CardUser(), GetTemp_Target(), currentTurn); // BattleAction使用 系統端Unit資料傳回Unit
    }

    // 回合開始時，取得資料
    void GetCurrentUnits(TurnBaseBattleUnitDisplayData unit) // 把玩家端和敵人端的資料傳來系統端
    {
        if (currentTurn == TurnBaseBattleOrderType.Undefined)
        {
            Debug.Log("未設置戰鬥順序");
            return;
        }
        if (TurnCount == 0 && RoundCount == 0)
        {
            if (unit.tag == TurnBaseBattleUnitDisplayData.Player1)
            {
                Player1Name = unit.GetUnitName();
            }
            else if (unit.tag == TurnBaseBattleUnitDisplayData.Player2)
            {
                Player2Name = unit.GetUnitName();
            }
        }
        if (currentTurn == unit.turnOrder) // 抓當回合的玩家(自己)
            SetTemp_CardUser(unit);
        else
            SetTemp_Target(unit);

        BattleAction.GetUnitsInfo(unit);
    }
    #endregion

    #region "Get Set function"
    public int GetAssignFixedDiceValue() => assignFixedDiceValue;
    public void SetAssignFixedDiceValue(int value) => assignFixedDiceValue = value;

    public int GetAssignFixedDiceCount() => assignFixedDiceCount;
    public void SetAssignFixedDiceCount(int count) => assignFixedDiceCount = count;

    public bool GetIsAssignFixedDiceValue_CardUser() => isAssignFixedDiceValue_CardUser;
    public void SetIsAssignFixedDiceValue_CardUser(bool isAssigned) => isAssignFixedDiceValue_CardUser = isAssigned;

    public bool GetIsSkipped_CardUser() => isSkipped_CardUser;
    public void SetIsSkipped_CardUser(bool isSkipped) => isSkipped_CardUser = isSkipped;
    public bool GetIsSkipped_Target() => isSkipped_Target;
    public void SetIsSkipped_Target(bool isSkipped) => isSkipped_Target = isSkipped;

    public TurnBaseBattleOrderType GetPlayerOrder() => playerOrder;
    public TurnBaseBattleOrderType GetRivalOrder() => rivalOrder;

    // 取得當回合行動的玩家
    public TurnBaseBattleUnitDisplayData GetTemp_CardUser() => temp_CardUser;
    public void SetTemp_CardUser(TurnBaseBattleUnitDisplayData user) => temp_CardUser = user;

    // 取得當回合的對手
    public TurnBaseBattleUnitDisplayData GetTemp_Target() => temp_Target;
    public void SetTemp_Target(TurnBaseBattleUnitDisplayData target) => temp_Target = target;

    void SetCurrentTurn(TurnBaseBattleOrderType turnType) => currentTurn = turnType;

    public S001_DiceSystem GetDiceSystem() => DiceSystem;
    public DicePoolManager GetDicePoolManager() => dicePoolManager;

    public S002_DrawCardsSystem GetDrawCardSystem() => DrawCardSystem;
    public S005_NumericalCalculation GetNumericalCalculation() => NumericalCalculation;
    public TurnBaseBattleSetUp GetTurnBaseBattleSetUp() => turnBaseBattleSetUp;

    #endregion

    #region  "敵人行為"

    Coroutine EnemyTurnPeriod;
    Coroutine CheckIfEnemyTurnActionPeriod;
    public void Test_ExchangeCardUserForPeriod() // 不觸發回合流動，只切換行動者
    {
        ResetDiceCount(); // 回復目前角色的骰子數量 
        SendUnitInfoFromManager(GetTemp_CardUser(), GetTemp_Target(), currentTurn); // 系統端Unit資料傳回Unit 
        SwitchCurrentTurn(); // 切換目前回合    
        ReadyForInfo(); // 換回合後，更新資料，把玩家端和敵人端的資料傳來系統端
    }
    IEnumerator EnemyTurn()
    {
        Debug.Log("敵人回合開始");
        // 敵人進行攻擊
        BattleAction.EnemyAttack(GetTemp_CardUser());
        BattleUI.GetEnemyActionPanel().SetActive(true);

        // 敵人行動後停頓 1.2 秒
        yield return new WaitForSeconds(3f);
        OnTurnEnd();
        Debug.Log("敵人行動結束，切換到玩家回合");
        BattleUI.GetEnemyActionPanel().SetActive(false);
    }

    IEnumerator CheckIfEnemyAct() // FirstTurnCheckIfEnemyTurnAction() 的 Coroutine 版本
    {
        yield return StartCoroutine(EnemyTurn()); // 執行完 EnemyTurn() 這個協程才會結束
        OnTurnSwitch();
        OnTurnBegin();

    }

    void FirstTurnCheckIfEnemyTurnAction()
    {
        if (gameModes[1] == true || currentTurn != rivalOrder) return; // 判斷當前回合是否為敵人行動
        Debug.Log("目前是哪個順位進行行動: " + currentTurn + " 敵人的行動順位: " + rivalOrder);

        if (EnemyTurnPeriod != null)
        {
            StopCoroutine(EnemyTurnPeriod);
        }

        // 啟動協程來處理敵人行動及其停頓
        EnemyTurnPeriod = StartCoroutine(EnemyTurn());
        return;
    }
    #endregion

    #region  回合制管理

    #region "公共事件"

    public void SetFirstTurn() // 戰鬥開始時要啟用
    {
        // ResetTurnOrder();
        currentTurn = TurnBaseBattleOrderType.FirstMember;
        if (IsPlayer1First)
        {
            playerOrder = TurnBaseBattleOrderType.FirstMember;
            rivalOrder = TurnBaseBattleOrderType.SecondMember;
            // SendUnitInfoFromManager(GetTemp_CardUser(), GetTemp_Target()); // 系統端Unit資料傳回Unit
        }
        else
        {
            playerOrder = TurnBaseBattleOrderType.SecondMember;
            rivalOrder = TurnBaseBattleOrderType.FirstMember;
            // SendUnitInfoFromManager(GetTemp_Target(), GetTemp_CardUser()); // 系統端Unit資料傳回Unit
        }
        SetTurnOrder(playerOrder, TurnBaseBattleUnitDisplayData.Player1);
        SetTurnOrder(rivalOrder, TurnBaseBattleUnitDisplayData.Player2);
        // Debug.Log("目前順序 玩家(第一個玩家): " + playerOrder + " 對手(第二個玩家): " + rivalOrder);

        ReadyForInfo(); // 把玩家端和敵人端的資料傳來系統端 

        BattleUI.GetTurnCountText().text = "回合數：" + (RoundCount + 1);

        curWhoseTurn = temp_CardUser.GetTW_UnitName();
        BattleUI.SetWhoseTurn();

        SendUnitInfoFromManager(GetTemp_CardUser(), GetTemp_Target(), currentTurn); // 系統端Unit資料傳回Unit

        FirstTurnCheckIfEnemyTurnAction();
        // Debug.Log("人:" + GetTemp_CardUser() + "骰子數:" + GetTemp_CardUser().GetNumOfDice());
        OnDrawCardAndDice();
    }

    public void ToNextTurn() // 回合結束的時候要調用這個方法更改目前回合
    {
        // 啟動回合流程
        if (TurnChange != null)
        {
            StopCoroutine(TurnChange);
        }
        TurnChange = StartCoroutine(OnSwitchToNextTurn());
        // BattleUI.IfPlayerCanAct(); // 不是玩家回合時，玩家不能操作UI

    }

    #endregion

    /// <summary>
    /// 戰鬥結束後的玩家資料。原本會直接寫進地圖狀態 UI (PlayerMapStatus_UI)，
    /// 該系統移除後改由此處保存，供地圖／存檔等外部系統在戰鬥結束後讀取。
    /// </summary>
    public TurnBaseBattlePlayerData LastBattlePlayerData { get; private set; }

    /// <summary>從戰鬥中的玩家單位取回結算後的數值，存入 <see cref="LastBattlePlayerData"/>。</summary>
    public void GetPlayerDataAfterBattle()
    {
        TurnBaseBattlePlayerData playerData = new TurnBaseBattlePlayerData();

        // 檢查 temp_CardUser 是否為 TurnBaseBattlePlayerData 類型
        if (temp_CardUser is TurnBaseBattleUnitDisplayData cardUserData)
        {
            playerData.SetTurnOrder(TurnBaseBattleOrderType.Undefined);
            playerData.SetOriginMaxCountOfDice(cardUserData.GetOriginMaxCountOfDice());
            playerData.SetCountOfDice(cardUserData.GetOriginMaxCountOfDice());
            playerData.SetStatusEffects(new List<BattleStatusEffect>());
            playerData.SetOriginMaxHp(cardUserData.GetOriginMaxHp());
            playerData.SetCurHp(cardUserData.GetCurHp());
        }
        else
        {
            Debug.LogWarning($"[{name}] GetPlayerDataAfterBattle：temp_CardUser 不是玩家單位，取回的資料為預設值。");
        }

        LastBattlePlayerData = playerData;
    }

    public void FinishBattle()
    {
        string result = "";

        if (IsUnitDead[0] == true) // player is Dead
        {
            BattleUI.SetSettlementUI(false);
            // result = "<color=#3C3C3C>Lose</color>";
            result = "失敗";

            IsplayerWin = false;

            try
            {
                Transform startGrid = gridmanager.levels[gridmanager.currentLevelIndex].startGrid; // 取得當前關卡的起點
                Player.transform.position = startGrid.position; // 傳送玩家到起點
                Debug.Log("玩家已被傳送回起點");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("gridmanager 為空物件!" + ex.Message);
            }

            // 原本在此呼叫地圖回合管理腳本切回玩家回合；MapTurnBaseManager 尚未移植，暫無對應處理。
        }
        else if (IsUnitDead[1] == true)
        {
            BattleUI.SetSettlementUI(true);
            // result = "<color=#C6A300>Victory</color>";
            result = "勝利";

            IsplayerWin = true;


            try
            {
                gridmanager.ClearEnemiesFromBattleField();

            }
            catch (Exception ex)
            {
                Debug.LogWarning("gridmanager 為空物件!" + ex.Message);
            }

            // 原本在此呼叫地圖回合管理腳本切回玩家回合；MapTurnBaseManager 尚未移植，暫無對應處理。
        }
        GetPlayerDataAfterBattle();
        BattleUI.OpenBattleSettlement(result);
        BattleUI.GetToNextTurnButton().SetActive(false);
        // BattleUI.ToNextTurnButton.SetActive(false);
    }




    public void IfUnitDead(TurnBaseBattleUnitDisplayData unit) // 之後優化
    {

        // Debug.Log("誰: " + unit + " HP: " + unit.GetCurHp());
        if (gameModes[1] == false)
        {
            if (unit.GetCurHp() <= 0)
            {
                if (unit.turnOrder == playerOrder) // 代表當前的行動者是玩家
                {
                    Debug.Log("行動者 " + unit + " 玩家掛了 Lose");
                    IsUnitDead[0] = true;
                }
                else if (unit.turnOrder == rivalOrder) //掛的不是玩家
                {
                    Debug.Log("行動者 " + unit + " 玩家贏了 Victory");
                    IsUnitDead[1] = true;
                }
                if (IsBattleOver == false)
                {
                    IsBattleOver = true;
                    if (currentTurn == temp_CardUser.turnOrder && temp_CardUser.gameObject.tag == TurnBaseBattleUnitDisplayData.Player1)
                    {
                        FinishBattle();
                    }
                }

            }
        }
    }

    #region "ToNextTurn Module"
    Coroutine TurnChange;
    Coroutine TurnCheckSkip;

    // 在方法中使用 Coroutine 要注意 StartCoroutine() 是非阻塞的，如果結束，它會立即返回控制權給主程式，然後繼續執行啟用Coroutine的方法中後續的其他程式。
    IEnumerator OnSwitchToNextTurn()
    {
        OnTurnEnd();
        OnTurnSwitch();
        OnTurnBegin();

        bool isSkipped = isSkipped_CardUser;

        // 判斷對手是否跳過回合
        if (TurnCheckSkip != null)
        {
            StopCoroutine(TurnCheckSkip);
        }
        yield return TurnCheckSkip = StartCoroutine(OnCheckTurnSkip(isSkipped));

        if (IsBattleOver == false) // 無人死亡
        {
            OnDrawCardAndDice(); // 敵人回合結束後，才進行抽牌和擲骰子的邏輯
        }
        else
        {
            FinishBattle();
        }

        Debug.Log("測結束");
    }

    // 在方法中使用 Coroutine 要注意 StartCoroutine() 是非阻塞的，如果結束，它會立即返回控制權給主程式，然後繼續執行啟用Coroutine的方法中後續的其他程式。
    IEnumerator OnCheckTurnSkip(bool isSkipped)
    {
        if (isSkipped == false)
        {
            // 判斷當前回合是否有對應的行動順位者要行動
            if (gameModes[1] == false && temp_CardUser.turnOrder == currentTurn)
            {
                // 判斷當前行動者是否為敵人、當前回合是否為敵人行動
                if (temp_CardUser.turnOrder == rivalOrder)
                {
                    if (CheckIfEnemyTurnActionPeriod != null)
                    {
                        StopCoroutine(CheckIfEnemyTurnActionPeriod);
                    }
                    yield return CheckIfEnemyTurnActionPeriod = StartCoroutine(CheckIfEnemyAct());
                }
            }
        }
        else
        {
            // 判斷當前回合是否有對應的行動順位者要行動
            if (temp_CardUser.turnOrder == currentTurn)
            {
                // 判斷當前行動者是否為敵人、當前回合是否為敵人行動
                if (temp_CardUser.turnOrder == rivalOrder)
                {
                    Debug.Log("temp_CardUser.turnOrder == rivalOrder");
                }
                else if (temp_CardUser.turnOrder == playerOrder)
                {
                    Debug.Log("temp_CardUser.turnOrder == playerOrder");

                }
                isSkipped_CardUser = false;
                // temp_CardUser.SetIsCurrentUnitSkip(false);
                // 切換到下一個回合
                OnTurnEnd();
                OnTurnSwitch();
                OnTurnBegin();
            }
        }
    }
    void OnTurnEnd()
    {
        BattleAction.OnTurnEnd();
        DiceSystem.RemoveUsableDices(); // 清除場上的骰子
        SendUnitInfoFromManager(GetTemp_CardUser(), GetTemp_Target(), currentTurn); // 系統端Unit資料傳回Unit 
        ReadyForInfo(); // 把玩家端和敵人端的資料傳來系統端
    }

    void ResetDiceCount() // 骰完色子後，回復目前角色的骰子數量
    {
        temp_CardUser.SetCountOfDice(temp_CardUser.GetOriginMaxCountOfDice());
        temp_Target.SetCountOfDice(temp_Target.GetOriginMaxCountOfDice());
    }

    void OnDrawCardAndDice()
    {
        DiceSystem.CurTurnDiceCount = GetTemp_CardUser().GetCountOfDice();
        DiceSystem.RollTheDice();
        DrawCardSystem.DrawCards();
        NumericalCalculation.ResetAreaDiceValue();

        Debug.Log("dice: " + temp_CardUser.GetCountOfDice());
        if (GetIsAssignFixedDiceValue_CardUser())
        {
            DiceSystem.OnRollDice(isAssignFixedDiceValue_CardUser, assignFixedDiceValue, assignFixedDiceCount);

            isAssignFixedDiceValue_CardUser = false;
            assignFixedDiceValue = 0;
            assignFixedDiceCount = 0;
        }

        SendUnitInfoFromManager(GetTemp_CardUser(), GetTemp_Target(), currentTurn); // 系統端Unit資料傳回Unit

    }

    void OnTurnSwitch() // 切換回合，改變回合行動者與回合對象
    {
        ResetDiceCount();// 回復目前角色的骰子數量 
        SendUnitInfoFromManager(GetTemp_CardUser(), GetTemp_Target(), currentTurn); // 系統端Unit資料傳回Unit  
        AddTurnCount(); // 加回合數 
        SwitchCurrentTurn(); // 切換目前回合 

        ReadyForInfo(); // 換回合後，更新資料，把玩家端和敵人端的資料傳來系統端  
        curWhoseTurn = temp_CardUser.GetTW_UnitName();
        BattleUI.SetWhoseTurn();
    }

    void OnTurnBegin() // 當回合角色開始行動前
    {
        BattleAction.OnTurnStart(); // 效果作用， BattleAction 的資料會回傳系統端
        SendUnitInfoFromManager(GetTemp_CardUser(), GetTemp_Target(), currentTurn); // 系統端Unit資料傳回Unit 
        ReadyForInfo(); // 把玩家端和敵人端的資料傳來系統端
    }

    void AddTurnCount()
    {
        TurnCount++;
        if (TurnCount >= 2)
        {
            RoundCount++;
            TurnCount = 0;
        }
    }

    void SwitchCurrentTurn()
    {
        // AddTurnCount(); // 加回合數 
        if (currentTurn == TurnBaseBattleOrderType.FirstMember)
        {
            SetCurrentTurn(TurnBaseBattleOrderType.SecondMember);
        }
        else if (currentTurn == TurnBaseBattleOrderType.SecondMember)
        {
            SetCurrentTurn(TurnBaseBattleOrderType.FirstMember);
        }
        BattleUI.SetTurnText();
        // Debug.Log("回合數：" + (RoundCount + 1) + currentTurn);
    }
    #endregion

    public void ResetTurnBaseBattleTempData()
    {
        ResetBattleSetting();
        ResetTurnAndRoundCount();
        ResetUnitInfo();
        ResetUnitData();
        ResetTurnOrder();
    }
    #region "ResetBattleTempData Module"
    void ResetBattleSetting()
    {
        IsPlayer1First = false;
        gameModes = null;
        gameModes = new List<bool>();
        IsUnitDead = null;
        IsUnitDead = new List<bool>();
        IsUnitDead.Add(false); // IsUnitDead[0] = player1
        IsUnitDead.Add(false); // IsUnitDead[1] = player2
        IsBattleOver = false;
        currentTurn = TurnBaseBattleOrderType.Undefined;
    }
    void ResetTurnOrder()
    {
        playerOrder = TurnBaseBattleOrderType.Undefined;
        rivalOrder = TurnBaseBattleOrderType.Undefined;
    }
    void ResetTurnAndRoundCount()
    {
        TurnCount = 0;
        RoundCount = 0;
    }
    void ResetUnitData()
    {
        temp_CardUser = null;
        temp_Target = null;
    }
    void ResetUnitInfo()
    {
        if (temp_CardUser == null || temp_Target == null)
        {
            Debug.Log("有沒有資料: " + (temp_CardUser != null) + " " + (temp_Target != null));
            return;
        }

        if (temp_CardUser.GetStatusEffects() != null && temp_CardUser.GetStatusEffects().Count > 0)
        {
            temp_CardUser.GetStatusEffects().Clear();
        }
        if (temp_Target.GetStatusEffects() != null && temp_Target.GetStatusEffects().Count > 0)
        {
            temp_Target.GetStatusEffects().Clear();
        }
        temp_CardUser.SetCurHp(temp_CardUser.GetOriginMaxHp());
        temp_Target.SetCurHp(temp_Target.GetOriginMaxHp());
        Debug.Log("重製 ");
    }
    #endregion

    #endregion

    /// <summary>
    /// 整個管理器的初始化方法。
    /// 原本由 GameManager 的管理器註冊機制呼叫，GameManager 移除後改由本身的 Start() 觸發。
    /// </summary>
    public void InitFromGameManager()
    {
        Init();
    }

    void Init()
    {
        InitBattleUI();
        BattleAction = GetComponent<BattleAction>();
        InitTurnBaseBattleFunctions();
        InitUnitDatas();
    }

    void InitUnitDatas()
    {
        UnitDatas = BattleUI.GetBattleEmpty().transform.GetChild(4).gameObject; // 從BattleUI的物件回來找
        units.Clear();
        units.Add(UnitDatas.transform.GetChild(0).GetComponent<TurnBaseBattleUnitDisplayData>());
        units.Add(UnitDatas.transform.GetChild(1).GetComponent<TurnBaseBattleUnitDisplayData>());
    }

    void InitTurnBaseBattleFunctions()
    {
        turnBaseBattleScripts = BattleUI.GetBattleEmpty().transform.GetChild(3).gameObject; // 從BattleUI的物件回來找

        GameObject GetTurnBaseBattleFunctionsChild(int index) => turnBaseBattleScripts.transform.GetChild(index).gameObject;

        DiceSystem = GetTurnBaseBattleFunctionsChild(0).GetComponent<S001_DiceSystem>();
        dicePoolManager = GetTurnBaseBattleFunctionsChild(0).GetComponent<DicePoolManager>();
        InitDiceSystem();

        DrawCardSystem = GetTurnBaseBattleFunctionsChild(1).GetComponent<S002_DrawCardsSystem>();
        NumericalCalculation = GetTurnBaseBattleFunctionsChild(1).GetComponent<S005_NumericalCalculation>();
        InitCardSystem();


        turnBaseBattleSetUp = GetTurnBaseBattleFunctionsChild(3).GetComponent<TurnBaseBattleSetUp>();
        turnBaseBattleSetUp.SetTurnBaseBattleManager(this);

        battleButtonFunction = GetTurnBaseBattleFunctionsChild(4).GetComponent<BattleButtonFunction>();
        battleButtonFunction.SetTurnBaseBattleManager(this);
        battleButtonFunction.battleUI = BattleUI;
    }

    void InitCardSystem()
    {
        DrawCardSystem.SetTurnBaseBattleUI(BattleUI);
        DrawCardSystem.InitFromTurnBaseBattleUI();

        NumericalCalculation.SetTurnBaseBattleUI(BattleUI);
        NumericalCalculation.InitFromTurnBaseBattleUI();
    }

    void InitDiceSystem()
    {
        // 色子物件池須先建立
        dicePoolManager.SetTurnBaseBattleUI(BattleUI);
        dicePoolManager.InitFromTurnBaseBattleUI();

        DiceSystem.SetTurnBaseBattleManager(this);
        DiceSystem.SetTurnBaseBattleUI(BattleUI);
        DiceSystem.InitFromTurnBaseBattleUI();

    }
    void InitBattleUI()
    {
        BattleUI = GetComponent<TurnBaseBattleUI>();
        BattleUI.manager = GetComponent<TurnBaseBattleManager>();
        BattleUI.InitFromTurnBaseBattleManager(); // Create BattleEmpty
    }

}
