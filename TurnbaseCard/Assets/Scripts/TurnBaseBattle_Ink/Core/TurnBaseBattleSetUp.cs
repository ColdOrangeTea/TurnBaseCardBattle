using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
/// <summary>
/// 戰鬥的設置
/// </summary>
public struct SetBattleSetting // 用來整理用的Struct
{
    public bool IsPlayer1First;
    public bool IsCustomized; // false as Default Unit Setting
    public List<TurnBaseBattleUnitData> TB_BattleUnits; // player1：playerDatas[0] player2：playerDatas[1]

    public int TB_OrderOfBackGround;
    public int TB_BattleBackGroundMusic;


    /// <summary>gameModes[0] = IsStoryMode  gameModes[1] = IsMultiplayer gameModes[2] =IsEndlessMode</summary>
    public List<bool> GameModes;

    public SetBattleSetting(bool isPlayer1First, bool isCustomized, List<TurnBaseBattleUnitData> tB_BattleUnits,
    int tB_OrderOfBackGround, int tB_BattleBackGroundMusic, List<bool> gameModes)
    {
        IsPlayer1First = isPlayer1First;
        IsCustomized = isCustomized; // false as Default Unit Setting
        TB_BattleUnits = tB_BattleUnits; // player1：playerDatas[0] player2：playerDatas[1]

        TB_OrderOfBackGround = tB_OrderOfBackGround;
        TB_BattleBackGroundMusic = tB_BattleBackGroundMusic;

        GameModes = gameModes;
    }

}

public class TurnBaseBattleSetUp : MonoBehaviour // trying to describe Code in English (?)
{
    [SerializeField] TurnBaseBattleManager manager;
    [SerializeField] bool isPlayer1First = false;
    [SerializeField] bool isCustomized = false; // false as Default Unit Setting
    [SerializeField] List<TurnBaseBattleUnitData> tB_BattleUnits; // player1：playerDatas[0] player2：playerDatas[1]

    [SerializeField] int tB_OrderOfBackGround;
    [SerializeField] int tB_BattleBackGroundMusic;


    /// <summary>gameModes[0] = IsStoryMode  gameModes[1] = IsMultiplayer gameModes[2] =IsEndlessMode</summary>
    [SerializeField] List<bool> gameModes;

    [Header("Customized setup")]
    [Range(10, 50)] public List<int> playerMaxHps;
    [Range(1, 4)] public List<int> playerdiceCounts;


    #region  公共方法

    public void SetTurnBaseBattleManager(TurnBaseBattleManager manager) => this.manager = manager;

    /// <summary>初始化雙人模式戰鬥的資料 參數：玩家1是否優先、是否有客製化數值設定、玩家1客製化資料、玩家2客製化資料、戰鬥背景圖的編號、戰鬥背景音樂的編號、遊戲模式、玩家一角色Type、玩家二角色Type </summary>
    public void Temp_InitMultiplayerModeTBBSetUp(bool isPlayer1First, bool isCustomized, TurnBaseBattleUnitData playerOne, TurnBaseBattleUnitData playerTwo,
    int tB_OrderOfBackGround, int tB_BattleBackGroundMusic, List<bool> gameModes, CharacterType playerOneType, CharacterType playerTwoType)
    {
        List<TurnBaseBattleUnitData> tB_TurnBaseBattleUnits = new List<TurnBaseBattleUnitData>();
        if (isCustomized)
        {
            tB_TurnBaseBattleUnits = new List<TurnBaseBattleUnitData>()
            {
               playerOne,
               playerTwo
            };
        }
        else
        {
            tB_TurnBaseBattleUnits = new List<TurnBaseBattleUnitData>()
            {
                InitPlayer(playerOneType),
                InitPlayer( playerTwoType)
            };
        }
        int tB_OrderOfBG = tB_OrderOfBackGround;
        InitBattleSetUp(isPlayer1First, isCustomized, tB_TurnBaseBattleUnits, tB_OrderOfBG, tB_BattleBackGroundMusic, gameModes);
    }

    /// <summary>初始化劇情模式戰鬥的資料 參數：玩家是否優先、玩家的資料、可遊玩角色種類Type、敵人種類Type  </summary>
    public void Temp_InitStoryModeTBBSetUp(bool isPlayer1First, TurnBaseBattleUnitData playerData,
    CharacterType playerOneType, EnemyType enemyType)
    {
        List<TurnBaseBattleUnitData> tB_TurnBaseBattleUnits = new List<TurnBaseBattleUnitData>();
        tB_TurnBaseBattleUnits = new List<TurnBaseBattleUnitData>()
        {
            playerData,
            InitEnemy( enemyType)
        };
        Debug.Log($"Test_OpenBattle 傳入的 Type 為 {playerOneType} {enemyType}");
        int tB_OrderOfEnemy = Convert.ToInt32(enemyType);

        InitBattleSetUp(isPlayer1First, isCustomized, tB_TurnBaseBattleUnits, tB_OrderOfEnemy, tB_OrderOfEnemy,
        new List<bool>() // StoryMode gamemode
        {
            true,
            false,
            false,
        }
        );
    }

    public void Temp_InitEndlessModeTBBSetUp(bool isPlayer1First, TurnBaseBattleUnitData playerData,
      CharacterType playerOneType, EnemyType enemyType)
    {
        List<TurnBaseBattleUnitData> tB_TurnBaseBattleUnits = new List<TurnBaseBattleUnitData>();
        tB_TurnBaseBattleUnits = new List<TurnBaseBattleUnitData>()
        {
            playerData,
            InitEnemy(enemyType)
        };
        // Debug.Log($"Temp_InitEndlessModeTBBSetUp 傳入的 Type 為 {playerOneType} {enemyType}");
        int tB_OrderOfEnemy = Convert.ToInt32(enemyType);
        if (InitEnemy(enemyType).EndlessModeEnemyType != EnemyType.Undefined_Temp_ThisIsTypeEndNumber) // 是無盡模式的人形馬鈴薯
        {
            tB_OrderOfEnemy = Convert.ToInt32(InitEnemy(enemyType).BaseEnemyType); // 直接沿用背景設定
        }

        InitBattleSetUp(isPlayer1First, isCustomized, tB_TurnBaseBattleUnits, (int)BattleBackgroundType.FOR_ENDLESSMODE, tB_OrderOfEnemy,

        new List<bool>() // StoryMode gamemode
        {
            true,
            false,
            false,
        }
        );
    }

    /// <summary>初始化劇情模式戰鬥的資料 參數：玩家是否優先、
    /// 是否有客製化數值設定、玩家的資料、戰鬥背景圖的編號(Enum)、戰鬥背景音樂的編號、遊戲模式、可遊玩角色種類Type、敵人種類Type 
    /// </summary>
    public void Test_InitTBBSetUp(bool isPlayer1First, bool isCustomized, TurnBaseBattleUnitData playerData,
   int tB_BattleBG, int tB_BattleBGM, List<bool> gameModes, CharacterType playerOneType, EnemyType enemyType)
    {
        List<TurnBaseBattleUnitData> tB_TurnBaseBattleUnits = new List<TurnBaseBattleUnitData>();
        tB_TurnBaseBattleUnits = new List<TurnBaseBattleUnitData>()
        {
            playerData,
            InitEnemy(enemyType)
        };
        Debug.Log($"Test_OpenBattle 傳入的 Type 為 {playerOneType} {enemyType}");
        InitBattleSetUp(isPlayer1First, isCustomized, tB_TurnBaseBattleUnits, tB_BattleBG, tB_BattleBGM, gameModes);
    }

    #endregion


    /// <summary>透過結構 SetBattleSetting 賦值，將一場戰鬥需要的資料設定好 </summary>
    void InitBattleSetUp(bool isPlayer1First, bool isCustomized, List<TurnBaseBattleUnitData> tB_TurnBaseBattleUnits, int tB_OrderOfBackGround, int tB_BattleBackGroundMusic, List<bool> gameModes)
    {
        BattleTurnBaseEvent battleTurnBaseEvent = new BattleTurnBaseEvent();
        SetBattleSetting setBattleSetting = new SetBattleSetting
        (
            isPlayer1First, isCustomized, tB_TurnBaseBattleUnits, tB_OrderOfBackGround, tB_BattleBackGroundMusic, gameModes
        );

        battleTurnBaseEvent.SendBattleSetting(setBattleSetting);
    }


    #region InitUnits overloading Functions
    TurnBaseBattlePlayerData InitPlayer(CharacterType characterType)
    {
        TurnBaseBattlePlayerData battleUnitPlayerData = new TurnBaseBattlePlayerData();
        TurnBaseBattlePlayerData playerData = battleUnitPlayerData.InitPlayerInfo(characterType);
        return playerData;
    }

    /// <summary>根據指定的 EnemyType 獲取對應的敵人資料。</summary>
    /// <param name="enemyType">輸入的敵人類型</param><returns>對應的敵人資料</returns>
    TurnBaseBattleEnemyData InitEnemy(EnemyType enemyType)
    {
        TurnBaseBattleEnemyData battleUnitEnemyData = new TurnBaseBattleEnemyData();
        TurnBaseBattleEnemyData enemyData = battleUnitEnemyData.InitEnemyInfo(enemyType);
        // 檢查是否是 HumanPotato 系列並進行映射
        if (battleUnitEnemyData.GetHumanPotatoToBaseMapping().TryGetValue(enemyType, out EnemyType baseEnemyType))
        {
            enemyData.BaseEnemyType = battleUnitEnemyData.ConvertToStoryEnemy(enemyType);
            enemyData.EndlessModeEnemyType = enemyType;
        }
        else
        {
            enemyData.BaseEnemyType = enemyType;
            enemyData.EndlessModeEnemyType = EnemyType.Undefined_Temp_ThisIsTypeEndNumber;
        }

        Debug.Log($"InitEnemy 測試戰鬥 將 {enemyType} {baseEnemyType} 傳入");
        return enemyData;
    }
    #endregion
}
