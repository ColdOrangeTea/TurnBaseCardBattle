using System.Collections.Generic;

/// <summary>
/// 一場戰鬥的設定資料（先手、單位、背景、音樂、遊戲模式）。
///
/// 原本定義在 TurnBaseBattleSetUp.cs 內；為了讓 V2（BattleController）不必依賴舊的
/// TurnBaseBattleSetUp，於重構期間抽出成獨立檔。維持在全域命名空間，新舊程式都不需改 using。
/// </summary>
public struct SetBattleSetting
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
        IsCustomized = isCustomized;
        TB_BattleUnits = tB_BattleUnits;

        TB_OrderOfBackGround = tB_OrderOfBackGround;
        TB_BattleBackGroundMusic = tB_BattleBackGroundMusic;

        GameModes = gameModes;
    }
}
